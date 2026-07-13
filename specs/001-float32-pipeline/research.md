# Phase 0 Research: Float32 Processing Pipeline

**Date**: 2026-07-13 | **Plan**: [plan.md](plan.md)

Codebase facts verified at v1.3.4.3 (feature branch `001-float32-pipeline`).
The six Architect rulings from the planning directive are numbered AR-1..6.

## R1. Processing format & buffer sizing (AR-1, AR-5)

- **Decision**: the processing domain is declared once per path as
  `WaveFormat.CreateIeeeFloatWaveFormat(sourceRate, sourceChannels)` and
  handed to `DSPThread`/`ProcessorChain`/`AudioBuffer` exactly where the
  PCM16 format is constructed today (`AudioRouter.vb:456`,
  `RecordingManager.vb:384`). Ring buffers, monitor taps, and the
  work-buffer block stay **byte-based and format-agnostic** — verified: both
  DSPThread buffer sizes already derive from
  `format.AverageBytesPerSecond * 2` (`AudioRouter.vb:465-466`) and the work
  buffer from `BLOCK_SIZE_SAMPLES * format.BlockAlign` (`DSPThread.vb:132`),
  so swapping the format doubles byte capacities automatically and preserves
  every time semantic (2 s depth, 50% refill, 0.5 s taps at
  `outputBufferSize \ 4`) — the preserve-durations clarification falls out of
  existing code by construction.
- **Rationale**: AR-1/AR-5 verbatim; it is also what keeps the MRRB suite's
  survival claim honest — no buffer code changes at all.
- **Alternatives considered**: float-native (`Single()`) ring buffers
  (rejected: rewrites proven lock-free code and voids the surviving suite);
  explicit size constants (rejected: reintroduces the B2 class of bug).

## R2. Boundary map — where conversion actually remains

- **Decision** (per-path):
  | Path | Entry | Exit |
  |------|-------|------|
  | File playback | NONE — `AudioFileReader` already emits IEEE float (`AudioRouter.vb:451-453`); today's float→PCM16 feeder conversion is DELETED, not relocated | NONE — `WaveOutEvent` accepts `IeeeFloat` WaveFormat natively (Windows mixes float since Vista); `DSPOutputProvider` reports the float format |
  | WASAPI capture | NONE — device delivers float; the private `WasapiEngine.ConvertFloatToPCM16` (:366) is DELETED (FR-003) | — |
  | WaveIn/int16 capture | canonical `Pcm16ToFloat` at device entry (`WaveInEngine`/`MicInputSource`, 16-bit formats at :170/:50) | — |
  | Recorded file | — | canonical `FloatToPcm16` in `RecordingEngine`'s writer thread, immediately before `wavOut.Write` (16-bit WAV target) |
- **Result**: the playback path has ZERO conversions end-to-end; the full
  system has at most one per boundary (FR-002 satisfied as "≤1, only at
  boundaries"; the audit counts sites, not calls).
- **Risk noted**: if a legacy output driver rejects float, fallback is one
  conversion before the output provider — deferred until observed (manual
  smoke checks playback on the user's device); recorded in plan Complexity.
- **Alternatives considered**: converting to int16 for WaveOut anyway
  (rejected: reintroduces a conversion and a quantization the spec exists to
  remove).

## R3. Canonical conversion pair (AR-2, AR-6) + scale-factor nuance

- **Decision**: `Utils.SampleConversion` grows the inverse:
  `Pcm16ToFloat(pcmBuffer As Byte(), byteCount As Integer) As Byte()`
  (float bytes out; plus an in-place/scratch overload for the capture hot
  path). Scale: **int16 → float divides by 32768** (matching NAudio's
  `AudioFileReader` scaling, so both float entries into the domain agree);
  **float → int16 keeps the locked ×32767 symmetric contract** (feature 003,
  surviving suite). Round-half-even, clamp-never-wrap, NO dither (AR-6).
- **Round-trip nuance (recorded for the null test)**: the asymmetric pair
  gives `s → s/32768 → round(s·32767/32768)` — max error 1 LSB, which is
  exactly SC-001's allowance ("within 1 LSB of a single conversion round
  trip"). The null-test reference IS this pair.
- **Non-finite guard (FR-009)**: `FloatToPcm16` maps NaN → 0 and ±Infinity →
  full-scale clamp, increments a `Friend NonFiniteCount` observable seam
  (E1-precedent pattern), and rate-limit-logs on first occurrence per
  session. The surviving round-trip tests never feed non-finite values, so
  they stay green unchanged.
- **Alternatives considered**: symmetric /32767 both ways (rejected: breaks
  the locked FloatToPcm16 contract AND disagrees with NAudio's reader
  scaling); dither (ruled out, clarification Q2).

## R4. AudioBuffer float view (AR-3)

- **Decision**: `AudioBuffer` keeps its `Byte()` storage (ring-buffer
  interop unchanged) and gains:
  - `Public Function SampleSpan() As Span(Of Single)` —
    `MemoryMarshal.Cast(Of Byte, Single)(New Span(Of Byte)(_buffer, 0, ByteCount))`,
    zero-alloc, valid in VB for synchronous methods;
  - `SampleCount` derives as `ByteCount \ 4` when the format is float
    (existing `UpdateSampleCount` uses `BlockAlign`-aware math — verify it
    already generalizes; it does: bytes per sample from format).
  Processors receive the same `AudioBuffer` and use only `SampleSpan()`
  (AR-3: "processors never touch bytes").
- **Alternatives considered**: `Single()` primary storage with byte
  marshaling at buffer edges (rejected: two copies per block, churns
  DSPThread's read/write paths); per-sample `BitConverter` accessors
  (rejected: bounds-check + call overhead per sample, and byte-poking is
  what AR-3 forbids).

## R5. GainProcessor re-derivation + balance pan law (AR-4)

- **Decision**: `ProcessInternal` rewritten over `SampleSpan()`:
  - **Gain**: `sample * _gainLinear` — no clamp (FR-007; today's
    `Math.Max(-32768, Math.Min(32767, …))` clamps are DELETED — range
    limiting lives only in the boundary conversion, FR-008).
  - **Balance pan** (clarify Q1 + AR-4): favored channel factor = 1.0
    always; opposite channel factor = `Cos(|pan| * PI/2)` (smooth, monotonic,
    1.0 at center, 0.0 at hard pan). No boost anywhere. Center pan is both
    channels ×1.0 — mathematically transparent, so the unity bypass becomes
    a pure optimization with no behavior cliff (the 003-discovered
    discontinuity is impossible by construction).
  - **Width**: unchanged M/S math, now in float without intermediate
    rounding.
  - Mono path: gain only, as today. `SendToMonitor` unchanged (taps carry
    the domain format, R6).
- **Supersession (FR-011)**: `GainProcessorTests.vb` is re-derived: the
  locked `Stereo_CenterPan_AppliesConstantPowerAttenuation` and
  `HardPan_FullyAttenuatesOppositeChannel` assertions are replaced by
  balance-law assertions (`CenterPan_IsTransparent_GainExact`,
  `HardPan_FavoredChannelUnity_OppositeSilent`, taper monotonicity, no-step
  bypass sweep). The file header records the supersession with the Architect
  ruling reference and date; the changelog entry repeats it. Never silently
  edited.
- **Alternatives considered**: linear taper (rejected: cosine specified by
  AR-4 and audibly smoother near center); constant-power variants (ruled out
  by clarification Q1).

## R6. Monitoring consumers — the int16-assumption inventory (verified by grep)

Sites that parse or normalize 16-bit samples and their disposition:

| Site | Today | Disposition |
|------|-------|-------------|
| `AudioRouter.ReadTapSamples` (shared helper, 003) | `BitConverter.ToInt16`, `/32768.0F` loop | reinterpret float bytes: `Buffer.BlockCopy` into `Single()` — loop DELETED (FR-005) |
| `RecordingManager.ReadTapSamples` (same shape) | same | same |
| `AudioRouter.UpdateOutput/InputSamples` diagnostics + `AudioSamplesEventArgs.BitsPerSample = 16` | int16 peak scan; args say 16 | float peak scan; args say 32 (+ `Encoding` hint if needed) |
| `FFTProcessor.AddSamples` (`Select Case bitsPerSample`, :80) | Case 16 mixes/normalizes int16 | ADD/VERIFY Case 32 float path (grep shows only Case 16 exercised; 7 int16-ish hits) |
| `Utils.AudioLevelMeter.AnalyzeSamples` | int16 branches (4 hits) | ADD/VERIFY 32-bit float branch |
| `WasapiEngine` (2 hits) | private float→PCM16 conversion | DELETED (FR-003) |
| `MicInputSource`/`WaveInEngine`/`AudioInputManager` 16-bit formats | device capture formats | device format stays int16 where hardware demands; canonical `Pcm16ToFloat` at entry |
| `GainProcessor` (4 hits) | int16 math + clamps | re-derived (R5) |
| `MainForm` (2 hits) | display-side normalization | switch to float payloads |
| `Audio/Routing/AudioPipeline.vb` (1 hit) | Phase-3 scaffolding, not on the active path | update comment/constant only; no behavior |
- **Cleanup rider (discovered during planning)**: `AudioRouter.vb:471-479`
  still creates orphan `_default_input`/`_default_output` readers — dead
  weight left when feature 003 deleted their consumers. DELETED in this
  feature's AudioRouter touch (no reader ever reads them; they'd sit
  permanently lapped).

## R7. Record path plumbing

- **Decision**: capture sources produce processing-domain float into the
  existing byte-oriented pipeline (WASAPI: native pass-through; WaveIn:
  `Pcm16ToFloat` at entry). `RecordingEngine`'s background writer converts
  each dequeued float block with canonical `FloatToPcm16` into a reusable
  scratch buffer immediately before `wavOut.Write` — one site, off the audio
  thread, zero steady-state allocation. The WAV writer keeps its 16-bit
  format (clarified: 24-bit + dither are the follow-on feature).
- **Alternatives considered**: converting at capture exit instead of write
  exit (rejected: that would put integer PCM back inside the domain,
  violating FR-001 for the monitoring taps on the record path).

## R8. Test strategy per the FR-012/013/014 contract

- **Surviving (empty-diff gates)**: `GlobalStateMachineTests.vb`,
  `MultiReaderRingBufferTests.vb`, `SampleConversionTests.vb` — checked with
  `git diff` per checkpoint, exactly like feature 002's SC-002 gate.
  (`SampleConversionTests` gains a NEW sibling test group for `Pcm16ToFloat`
  and non-finite handling — additions in a separate `#Region`, zero edits to
  existing tests; "empty diff" is asserted on the existing test bodies via
  review + the suite passing unchanged. If tooling demands strictness, the
  new group lands in `FloatPipelineTests.vb` instead — decided at task time.)
  → Decision: new conversion tests go in `FloatPipelineTests.vb`, keeping the
  surviving file literally untouched. Simpler gate.
- **Superseded**: `GainProcessorTests.vb` re-derived (R5) with a recorded
  supersession header.
- **New (`FloatPipelineTests.vb`)**:
  1. Null-test group (SC-001, three variants per analysis C2): (a)
     unity-chain — int16 pattern → `Pcm16ToFloat` → `ProcessorChain` at
     unity → `FloatToPcm16` → ≤ 1 LSB; (b) full-math — ×2.0 stage → ×0.5
     stage (exact in float), identity through the complete Span path with a
     2.0× inter-stage excursion, immune to the bypass fast-path; (c)
     float-source byte-identity on the zero-conversion playback path.
     Synchronous, chain-level (003 R8 precedent: no threads in unit tests);
     the threaded end-to-end is the manual smoke.
  2. Headroom (SC-003): +6 dB stage then −6 dB stage on a full-scale float
     signal → output equals input within float tolerance; same hot signal
     straight to `FloatToPcm16` → clamps, never wraps.
  3. Non-finite (FR-009): NaN/±Inf through the boundary → defined outputs +
     `NonFiniteCount` seam increments.
  4. Allocation audit (SC-006): warm up `ProcessorChain.Process`, then
     `GC.GetAllocatedBytesForCurrentThread()` around N processing iterations
     → delta 0 bytes (exact: no boxing, no span allocs on .NET 10).
  5. Balance-law group (SC-004): center transparency at gain 2.0, hard-pan
     favored-unity, taper monotonicity, bypass-boundary sweep < 0.01 dB.
- **Empty-diff mechanics**: `git diff --stat` on the three surviving files
  must print nothing at every checkpoint.
