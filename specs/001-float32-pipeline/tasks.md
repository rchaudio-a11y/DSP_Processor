# Tasks: Float32 Processing Pipeline

**Input**: Design documents from `/specs/001-float32-pipeline/`

**Prerequisites**: plan.md, spec.md (+ Clarifications 2026-07-13), research.md, data-model.md, contracts/float-domain.md, quickstart.md

**Tests**: Core scope — the spec defines an explicit three-way test contract (FR-012 surviving / FR-013 superseded / FR-014 new). Surviving-suite empty-diff gates run at EVERY checkpoint.

**Branch**: all work on `001-float32-pipeline` (Architect ruling); checkpoints v1.3.5.1/.2/.3 committed + tagged here; merge to master at feature end (T020).

**Organization**: US1 = domain migration (P1), US2 = headroom & domain-quality proofs (P2), US3 = balance pan law proofs & supersession record (P3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

## Structural note (002 precedent)

`GainProcessor` is rewritten ONCE, in US1's phase, with everything the float
domain implies: float-native math (US1/FR-004), no internal clamps
(US2/FR-007), and the balance pan law (US3/FR-010) — writing the old pan law
in float only to rewrite it a story later would be double work, and
float-domain clamps make no sense even transiently. Consequently the
superseded `GainProcessorTests` re-derivation (FR-011) also lands in US1
(the suite must be green at the v1.3.5.1 checkpoint). US2 and US3 then carry
their stories' PROOFS: headroom + allocation audit (US2), the full
balance-law assertion group + supersession record verification (US3).

---

## Phase 1: Setup (Branch Baseline)

- [X] T001 Commit the planning artifacts (`specs/001-float32-pipeline/`, `.specify/feature.json`) on branch `001-float32-pipeline` as a docs commit; run the full suite and record the baseline (expect 67/67 green) in `specs/001-float32-pipeline/quickstart.md` Validation results; confirm the three surviving-suite files are committed-clean so empty-diff gates are meaningful

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: the canonical conversion pair and the float view — every story builds on these

**⚠️ CRITICAL**: no story work until this phase is complete and green

- [X] T002 [P] Extend `DSP_Processor/Utils/SampleConversion.vb`: NEW `Pcm16ToFloat(pcmBuffer As Byte(), byteCount As Integer) As Byte()` (scale ÷32768 matching NAudio's reader, little-endian, + scratch-buffer overload for steady-state zero-alloc call sites); add non-finite guard to `FloatToPcm16` (NaN→0, ±Inf→±full-scale, `Friend NonFiniteCount` seam, first-occurrence log) WITHOUT altering any existing behavior the surviving `SampleConversionTests` lock (research R3, contract guarantees 1–5)
- [X] T003 [P] Add the processing-domain float view to `DSP_Processor/DSP/AudioBuffer.vb`; verify `SampleCount`/`UpdateSampleCount` generalize to 4-byte samples through the format's BlockAlign (research R4). IMPLEMENTATION NOTE (R4 deviation, recorded): `GetSample`/`SetSample` inlined accessors + `FloatSampleCount` instead of `SampleSpan() As Span(Of Single)` — VB's ref-struct support is unreliable; accessors are zero-alloc (`BitConverter.ToSingle` / `SingleToInt32Bits` + direct byte writes) and preserve AR-3's intent (processors never touch bytes)
- [X] T004 Create `DSP_Processor.Tests/FloatPipelineTests.vb` with the foundational groups: `Pcm16ToFloat` accuracy (÷32768 scale, round-trip-with-FloatToPcm16 ≤ 1 LSB, scratch overload parity), non-finite handling (NaN→0, ±Inf→clamp, `NonFiniteCount` increments), sample-accessor round-trip (write floats via accessors, verify byte-compatibility). Run full suite; surviving-suite `git diff --stat` prints NOTHING (quickstart gate)

**Checkpoint**: canonical pair + float view proven; stories may begin

---

## Phase 3: User Story 1 — One Domain, One Conversion Per Boundary (Priority: P1) 🎯 MVP

**Goal**: the migration — float format declared at both DSPThread creation sites, feeder/capture/write boundaries per the R2 map, processors on float-native math, consumers reading float directly, WASAPI duplicate and orphan readers deleted

**Independent Test**: null test (source → unity chain → output ≤ 1 LSB of one round trip) + conversion-site audit (canonical pair only) + zero conversion loops in consumers

- [X] T005 [US1] Re-derive `DSP_Processor/DSP/GainProcessor.vb` in the float domain (research R5): all math via `GetSample`/`SetSample` accessors (R4 deviation); gain = multiply, NO clamps anywhere (FR-007); balance pan — favored channel ×1.0, opposite ×`Cos(|pan|·π/2)` (clarify Q1/AR-4); width = unchanged M/S in float; mono path gain-only; unity bypass retained as pure optimization (center pan now transparent by construction); `SendToMonitor` unchanged
- [X] T006 [US1] Supersede `DSP_Processor.Tests/GainProcessorTests.vb` (FR-011): file header records the supersession (Architect ruling, feature 001, date 2026-07-13, superseded assertions named — `Stereo_CenterPan_AppliesConstantPowerAttenuation`, `HardPan_FullyAttenuatesOppositeChannel`); re-derive with float tolerances: gain accuracy (incl. values > 1.0 flowing unclamped), mono exact doubling, width M/S, unity-bypass byte-identity, balance-pan basics (center transparency, hard-pan favored-unity/opposite-silent). NOTE: T005+T006 are one logical merge group
- [X] T007 [P] [US1] Migrate the playback path in `DSP_Processor/AudioIO/AudioRouter.vb`: float format at :456 (`CreateIeeeFloatWaveFormat(fileReader rate/channels)`); feeder passes float bytes straight through — the `ConvertFloatToPCM16` wrapper and its call are DELETED (entry conversion eliminated, not relocated); confirm `DSPOutputProvider` needs no changes — VERIFIED by review of record 2026-07-13 (analysis C1): WaveFormat sourced from `DSPThread.Format` at construction, byte pass-through `Read`, float-valid silence pad (all-zero bytes = 0.0f) — confirm-only, evidence in data-model.md row 13; `ReadTapSamples` reinterprets float bytes (`Buffer.BlockCopy` to `Single()`, conversion loop deleted); `AudioSamplesEventArgs.BitsPerSample = 32`; diagnostics peak-scan on floats; DELETE orphan `_default_input`/`_default_output` reader creation (:471-479, 003 leftover)
- [X] T008 [P] [US1] Migrate the capture path: `DSP_Processor/AudioIO/WasapiEngine.vb` — private `ConvertFloatToPCM16` (:366) DELETED, float passes through un-quantized (FR-003); `DSP_Processor/AudioIO/WaveInEngine.vb` + `DSP_Processor/AudioIO/MicInputSource.vb` — int16 device entry converts once via canonical `Pcm16ToFloat` (scratch overload); `DSP_Processor/Managers/RecordingManager.vb` — float format at DSPThread creation (:384), `ReadTapSamples` reinterprets float bytes
- [X] T009 [P] [US1] Migrate the record write exit in `DSP_Processor/Recording/RecordingEngine.vb`: writer thread converts each dequeued float block via canonical `FloatToPcm16` into a reusable scratch buffer immediately before `wavOut.Write`; WAV writer format stays 16-bit PCM (research R7)
- [X] T010 [P] [US1] Migrate monitoring consumers: `DSP_Processor/DSP/FFT/FFTProcessor.vb` — 32-bit float `AddSamples` path (extend the `Select Case bitsPerSample` at :80); `DSP_Processor/Utils/AudioLevelMeter.vb` — float analysis branch; `DSP_Processor/MainForm.vb` — float payload display math (2 sites); `DSP_Processor/Audio/Routing/AudioPipeline.vb` — comment/constant only (inactive path). Case-32 branches assume IEEE float (no int32 sources exist); document this in the `AudioSamplesEventArgs` XML doc — an encoding field is deferred until an int32 source exists (analysis B1)
- [X] T011 [US1] Gate + checkpoint: add the null-test group to `DSP_Processor.Tests/FloatPipelineTests.vb` (SC-001, three variants per analysis C2): (1) unity-chain null — int16 pattern → `Pcm16ToFloat` → `ProcessorChain` at unity → `FloatToPcm16` → residual ≤ 1 LSB; (2) **full-math null** — gain ×2.0 stage → ×0.5 stage (exact powers of two: float multiply is exact), mathematically identity through the COMPLETE math path with a 2.0× inter-stage excursion, residual ≤ 1 LSB after the single boundary conversion — this variant cannot pass via the bypass fast-path; (3) float-source byte-identity null — float pattern through the unity chain on the zero-conversion playback path, output bytes = input bytes. Run the SC-002 conversion-site audit (quickstart script — only `SampleConversion.vb` converts); full suite green; surviving-suite diffs EMPTY; record results in quickstart.md. **Version v1.3.5.1, commit + tag on the feature branch**

**Checkpoint**: the domain is float end-to-end; playback path has zero conversions

---

## Phase 4: User Story 2 — Inter-Stage Headroom, Exit-Only Clamping (Priority: P2)

**Goal**: prove the headroom semantics the migration created (implementation landed with T005/T002; this story is its proof) — the Blumlein-enabler property

**Independent Test**: +6 dB through two stages arrives clean; hot signal at the boundary clips-never-wraps

- [X] T012 [US2] Add the headroom group to `DSP_Processor.Tests/FloatPipelineTests.vb` (SC-003): full-scale signal ×2.0 through stage one, ×0.5 through stage two → output equals input within float tolerance (no inter-stage clipping); the same ×2.0 signal sent directly to `FloatToPcm16` → clamps at full scale, never wraps; processor output above ±1.0 verified unclamped (FR-007) by reading the intermediate buffer
- [X] T013 [US2] Add the allocation audit to `DSP_Processor.Tests/FloatPipelineTests.vb` (SC-006, Constitution IV): warm up `ProcessorChain.Process` (non-unity settings so the full path runs), then assert `GC.GetAllocatedBytesForCurrentThread()` delta = 0 across ≥1000 processed blocks. Plus the denormal timing sanity (analysis C3, spec edge case): process N blocks of denormal-range decay-tail input and assert wall time within 5× of normal-range input — generous bound, catches pathology not noise
- [X] T014 [US2] Gate + checkpoint: full suite green; surviving diffs EMPTY; record in quickstart.md. **Version v1.3.5.2, commit + tag**

**Checkpoint**: headroom proven; hot loop proven allocation-free

---

## Phase 5: User Story 3 — Balance Pan Law (Priority: P3) ⚠️ BEHAVIOR CHANGE PROOFS

**Goal**: the full SC-004 assertion group and the recorded release of the feature-003 behavior lock

**Independent Test**: gain 2.0 at center = exactly ×2.0; no step > 0.01 dB across the bypass boundary; favored unity + monotonic taper across the pan sweep

- [X] T015 [US3] Add the balance-law group to `DSP_Processor.Tests/FloatPipelineTests.vb` (SC-004): `CenterPan_Gain2_ExactlyDoubles` (the hidden −3 dB is gone); `BypassBoundary_NoStep` (gain 1.0 vs 1.001 at center: level difference < 0.01 dB); `PanSweep_FavoredChannelUnity` (favored = input × gain at pan ∈ {−1, −0.5, −0.1, 0, 0.1, 0.5, 1}); `PanSweep_OppositeTaper_CosineMonotonic` (opposite = cos(|pan|·π/2) within float tolerance, strictly decreasing); no factor > 1.0 anywhere before user gain
- [X] T016 [US3] Verify the supersession record (FR-011): `DSP_Processor.Tests/GainProcessorTests.vb` header cites the Architect ruling + date + named superseded assertions (landed with T006 — verify present and accurate); add the supersession paragraph to `DSP_Processor/Documentation/Changelog/CURRENT.md` as part of the v1.3.5.3 entry (the first intentional release of a feature-003 behavior lock — the process is part of the contract)
- [X] T017 [US3] Gate + checkpoint: full suite green; surviving diffs EMPTY; SC-002 audit re-run; record in quickstart.md. **Version v1.3.5.3, commit + tag**

**Checkpoint**: feature complete on the branch; all three story proofs green

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T018 [P] Per-story version bookkeeping (rides each checkpoint): `DSP_Processor/Documentation/Active/Tasks.md` SubPhase 3.5 story table + `DSP_Processor/Documentation/Changelog/CURRENT.md` entries per Constitution VIII
- [X] T019 [P] Update `.github/instructions/audio.md`: replace the stale "All audio is 16-bit PCM internally" invariant with the float32 processing-domain invariant (this closes the constitution v1.1.0 Sync Impact Report's ⚠ follow-up — the code now matches the constitution); refresh the tap/metering examples to float payloads
- [X] T020 Run the complete quickstart.md validation end-to-end and record results (✅ code-level complete 2026-07-13: solution build 0 errors, 83/83 tests, FR-012 empty diffs end-to-end, SC-002 audit clean); **manual listening checklist HANDED OFF TO USER** (unity playback identical, pan sweep without center step, record + playback both capture engines, meters/FFT, float device compatibility) — **merge `001-float32-pipeline` to master ONLY after the user confirms the listening checks** (Architect branch ruling)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 first (branch baseline)
- **Foundational (Phase 2)**: T002 ∥ T003 (different files); T004 needs both. BLOCKS all stories
- **US1 (Phase 3)**: needs Phase 2. T005+T006 one merge group; T007 ∥ T008 ∥ T009 ∥ T010 after T005 (they feed/consume the domain the processor now expects); T011 gates
- **US2 (Phase 4)**: needs US1 (proofs exercise the migrated chain). T012 → T013 (same file, sequential); T014 gates
- **US3 (Phase 5)**: needs US1 (balance law landed in T005); independent of US2. T015 → T016 → T017
- **Polish (Phase 6)**: T018 rides checkpoints; T019 anytime after T011; T020 last, user-gated

### Parallel Opportunities

- T002 ∥ T003 (foundational pair)
- **The big one**: T007 ∥ T008 ∥ T009 ∥ T010 — four disjoint file groups covering the entire boundary migration
- T019 ∥ T018 in polish

## Parallel Example: User Story 1

```text
# After T005/T006 (processor + superseded tests merged), launch together:
Task: "Migrate playback path in AudioRouter.vb (float format, feeder pass-through, tap reinterpret, orphan cleanup)"
Task: "Migrate capture path (WasapiEngine delete, WaveIn/MicInputSource entry conversion, RecordingManager format)"
Task: "Migrate record write exit in RecordingEngine.vb (canonical conversion before wavOut.Write)"
Task: "Migrate monitoring consumers (FFTProcessor, AudioLevelMeter, MainForm, AudioPipeline)"
```

## Implementation Strategy

**MVP = Phases 1+2 + US1 (T001–T011)**: the domain is migrated, the null test
proves transparency, and the conversion audit proves the boundary discipline —
tag v1.3.5.1. US2 and US3 are proof-stories over the landed mechanism
(headroom/allocations, pan law), each cheap and independently valuable.

Risk shape mirrors feature 002: the heaviest change (T005 + the four-way
boundary migration) happens earliest, under the strongest safety net — the
foundational conversion tests (T004), the surviving suites' empty-diff gates,
and the null test as the story's own exit criterion. The deliberate behavior
change (pan law) is priced in from T005 but only *ships to master* after the
user's listening checks (T020 gate).
