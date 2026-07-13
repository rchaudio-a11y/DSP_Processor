# Phase 1 Data Model: Float32 Processing Pipeline

**Date**: 2026-07-13 | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)

## Processing Domain

| Property | Value |
|----------|-------|
| Sample type | IEEE 754 binary32 (`Single`), normalized ±1.0 nominal, excursions permitted |
| Frame layout | interleaved, channel order unchanged from today |
| WaveFormat | `WaveFormat.CreateIeeeFloatWaveFormat(rate, channels)` — BlockAlign = 4 × channels |
| Byte carrier | unchanged `Byte()` infrastructure (ring buffers, taps, AudioBuffer storage) |
| Range limiting | ONLY in `FloatToPcm16` at boundaries (clamp-never-wrap); processors never clamp |
| Non-finite policy | NaN → 0, ±Inf → ±full-scale at the boundary; `NonFiniteCount` seam + first-occurrence log |

## Boundary map (the ONLY integer↔float sites)

| Boundary | Direction | Conversion | Site |
|----------|-----------|-----------|------|
| File playback in | (none — already float) | — | `AudioFileReader` output feeds domain directly |
| Playback device out | (none — device takes float) | — | `DSPOutputProvider` reports float format to `WaveOutEvent` |
| WASAPI capture in | (none — already float) | — | pass-through; private duplicate DELETED |
| WaveIn/int16 capture in | int16 → float | `Pcm16ToFloat` (÷32768) | device entry in the capture engine |
| Recorded file out | float → int16 | `FloatToPcm16` (×32767, locked contract + non-finite guard) | `RecordingEngine` writer, before `wavOut.Write` |

### Invariants (sacred — RDF Phase 2)

1. No integer sample representation exists between a boundary-in and a
   boundary-out (FR-001).
2. `Utils.SampleConversion` is the only code that converts sample formats;
   consumers reinterpret bytes, never convert (FR-002/005).
3. Scale factors: in ÷32768 (agrees with NAudio's reader), out ×32767
   (locked 003 contract). Max asymmetric round-trip error = 1 LSB = the
   null-test allowance.
4. Byte infrastructure is format-blind: no ring buffer, tap, or overrun
   arithmetic changes (FR-012's survival precondition).
5. Buffer time semantics preserved: capacities derive from
   `AverageBytesPerSecond` (doubles automatically), block = 256 samples ×
   BlockAlign (2048 bytes stereo float).
6. Hot loop allocates nothing per block: `SampleSpan()` is a reinterpret
   view; conversions use reusable scratch buffers (Constitution IV, SC-006).

## AudioBuffer surface changes

| Member | Change |
|--------|--------|
| `Buffer As Byte()` / `ByteCount` / `CopyFrom` / `Clear` | UNCHANGED (byte interop with rings) |
| `SampleSpan() As Span(Of Single)` | NEW — `MemoryMarshal.Cast` over `(Buffer, 0, ByteCount)`; the ONLY sample access processors use |
| `SampleCount` | derives from format BlockAlign (verify existing math generalizes — it uses format-aware bytes/sample) |

## Balance pan law (clarify Q1 + AR-4)

```
favoredGain  = 1.0                          (always)
oppositeGain = Cos(|pan| * PI / 2)          (1.0 at center → 0.0 at hard pan)
pan < 0: favored = Left;  pan > 0: favored = Right;  pan = 0: both ×1.0
```

Properties (test oracle, SC-004): center transparent (both ×1.0 — with unity
gain/width, byte-identical output); favored channel = input × gain at every
position; opposite strictly monotonic decreasing in |pan|; no step > 0.01 dB
across the bypass boundary; no boost anywhere (all factors ≤ 1.0 before
user gain).

**Superseded behavior (FR-011)**: uncompensated constant-power law
(cos/sin(π/4·(pan+1)) → ~−3 dB both channels at center under non-unity
settings). Locked by 003 tests `Stereo_CenterPan_AppliesConstantPowerAttenuation`
and `HardPan_FullyAttenuatesOppositeChannel`; released by Architect ruling
(feature description §5), recorded in the re-derived test file header and the
changelog.

## Migration file map (verified inventory, research R6)

| # | File | Change class |
|---|------|-------------|
| 1 | `Utils/SampleConversion.vb` | + `Pcm16ToFloat`, + non-finite guard & `NonFiniteCount` seam in `FloatToPcm16` |
| 2 | `DSP/AudioBuffer.vb` | + `SampleSpan()` |
| 3 | `DSP/GainProcessor.vb` | re-derive: Span math, balance pan, clamps deleted |
| 4 | `AudioIO/AudioRouter.vb` | float format at :456; feeder conversion deleted; `ReadTapSamples` reinterprets; args BitsPerSample 32; orphan `_default_*` creation deleted (:471-479) |
| 5 | `Managers/RecordingManager.vb` | float format at :384; `ReadTapSamples` reinterprets |
| 6 | `AudioIO/WasapiEngine.vb` | private conversion deleted; float pass-through |
| 7 | `AudioIO/WaveInEngine.vb` + `MicInputSource.vb` | `Pcm16ToFloat` at int16 device entry |
| 8 | `Recording/RecordingEngine.vb` | writer converts via canonical `FloatToPcm16` (scratch buffer) before `wavOut.Write` |
| 9 | `DSP/FFT/FFTProcessor.vb` | 32-bit float `AddSamples` path |
| 10 | `Utils/AudioLevelMeter.vb` | 32-bit float analysis branch |
| 11 | `MainForm.vb` | float payload display math (2 sites) |
| 12 | `Audio/Routing/AudioPipeline.vb` | comment/constant only (inactive path) |
| 13 | `AudioIO/DSPOutputProvider.vb` | **VERIFIED — no changes required** (review of record, 2026-07-13, analysis C1): `WaveFormat` is sourced from `DSPThread.Format` at construction (float format flows through automatically); `Read` is byte pass-through with zero format assumptions; the silence-pad remains valid in float (all-zero bytes = 0.0f). T007 confirms only. |

## Test contract partition (FR-012/013/014 → files)

| Partition | Files | Gate |
|-----------|-------|------|
| SURVIVING (empty diff) | `GlobalStateMachineTests.vb`, `MultiReaderRingBufferTests.vb`, `SampleConversionTests.vb` | `git diff --stat` prints nothing, every checkpoint |
| SUPERSEDED (recorded) | `GainProcessorTests.vb` | re-derived with supersession header citing the Architect ruling + date |
| NEW | `FloatPipelineTests.vb` | null test (SC-001), headroom (SC-003), balance-pan group (SC-004), non-finite (FR-009), allocation audit (SC-006), `Pcm16ToFloat` accuracy group |
