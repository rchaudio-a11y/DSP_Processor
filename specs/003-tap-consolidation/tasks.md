# Tasks: Tap Monitoring Consolidation & Audio Core Test Foundation

**Input**: Design documents from `/specs/003-tap-consolidation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/monitor-reader-api.md, quickstart.md

**Tests**: This feature's P1 user story IS the test suite — test tasks are core scope, not optional. FR-015 sequencing is enforced by the phase structure below: behavior-locking tests (US1) must be green against unmodified production code before US2/US3 production changes merge.

**Organization**: Tasks are grouped by user story (US1 = P1 test foundation, US2 = P2 overrun detection, US3 = P3 API consolidation).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

## Path Conventions

Paths are repository-relative. App project: `DSP_Processor/`. New test project: `DSP_Processor.Tests/` (sibling, per plan.md Structure Decision).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the test project skeleton and wire it into the build

- [X] T001 Create `DSP_Processor.Tests/DSP_Processor.Tests.vbproj` — SDK-style VB project, `<TargetFramework>net10.0-windows</TargetFramework>`, `<UseWindowsForms>true</UseWindowsForms>`, PackageReferences: `Microsoft.NET.Test.Sdk`, `MSTest.TestAdapter`, `MSTest.TestFramework`, `NAudio` 2.2.1; ProjectReference to `DSP_Processor/DSP_Processor.vbproj` (research.md R1)
- [X] T002 Add test project to solution: `dotnet sln DSP_Processor.slnx add DSP_Processor.Tests/DSP_Processor.Tests.vbproj`, then verify `dotnet build` of the solution succeeds with a placeholder test class

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Make production code testable without file I/O and expose the conversion function — every US1 test class depends on this phase

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Add `SuppressForTesting` null-logger mode to `DSP_Processor/Utils/Logger.vb` — Shared Boolean property; when True before first `Instance` access: constructor skips `EnsureLogDirectoryExists()`/`OpenLogFile()` and does not start the async writer thread; `Log(...)` becomes a no-op; unset behavior byte-for-byte unchanged (research.md R4)
- [X] T004 [P] Extract float→PCM16 conversion to new `DSP_Processor/Utils/SampleConversion.vb` — `Public Module SampleConversion` with `FloatToPcm16(floatBuffer As Byte(), byteCount As Integer, channels As Integer) As Byte()` moved verbatim from `AudioRouter.ConvertFloatToPCM16` (`DSP_Processor/AudioIO/AudioRouter.vb:768`); AudioRouter delegates to it; leave `WasapiEngine.ConvertFloatToPCM16` untouched with a comment pointing at feature 001 (research.md R5)
- [X] T005 Create `DSP_Processor.Tests/TestSetup.vb` — `<AssemblyInitialize>` sets `Utils.Logger.SuppressForTesting = True`; add a canary test asserting no `Logs/` directory is created by touching `Logger.Instance`
- [X] T006 Build solution + run `dotnet test` (canary only) to prove the harness executes headless with zero file I/O

**Checkpoint**: Test harness runs clean — behavior-locking test authoring can begin

---

## Phase 3: User Story 1 — Provably Correct Audio Core (Priority: P1) 🎯 MVP

**Goal**: Behavior-locking test suite over ring buffer, conversion, gain/pan math, and the full GSM transition matrix — green against UNMODIFIED production code (FR-015)

**Independent Test**: `dotnet test` passes on current production code in < 30 s with no audio hardware; injecting a deliberate off-by-one into buffer wraparound makes a named test fail (spec US1 Independent Test)

- [X] T007 [P] [US1] Write `DSP_Processor.Tests/RingBufferTests.vb` (FR-010) — write/read round-trip integrity, wraparound correctness across the boundary, Available/FreeSpace accounting, full-buffer and empty-buffer edge cases, partial reads (data-model.md test map)
- [X] T008 [P] [US1] Write `DSP_Processor.Tests/SampleConversionTests.vb` (FR-012) — ±full-scale mapping and clamping, over-range clipping (never wraps), exact-zero silence, round-trip accuracy ≤ 1 LSB across an amplitude sweep, mono and stereo interleaving (data-model.md SampleConversion rules)
- [X] T009 [P] [US1] Write `DSP_Processor.Tests/GainProcessorTests.vb` (FR-013) — unity-settings bypass is bit-identical, clamp at ±32768/32767 with max gain, mute floor (`GainDB` = −60 at zero linear gain), constant-power pan preserves L²+R² within relative tolerance 1e-3 on linear power at pan ∈ {−1, −0.5, 0, +0.5, +1} (analysis B2), stereo-width extremes (0.0 mono-collapse, 2.0 wide), mono and multi-channel paths (data-model.md test map)
- [X] T010 [P] [US1] Write `DSP_Processor.Tests/GlobalStateMachineTests.vb` (FR-014) — data-driven iteration over all 64 from/to pairs against the oracle table in data-model.md: `IsValidTransition` result, `TransitionTo` return, post-call `CurrentState` (changed on accept / unchanged on reject); assert 26-valid/38-invalid totals as tamper check; from-state setup via the shortest-path helper table; Error→Idle recovery case
- [X] T011 [US1] Run full suite against unmodified production code — all green, runtime < 30 s, zero files created (SC-003; FR-015 gate satisfied). One-time manual mutation check: introduce an off-by-one into ring-buffer wraparound, observe the named test fail, revert (analysis C3). Record runtime in `specs/003-tap-consolidation/quickstart.md` results note

**Checkpoint**: FR-015 gate PASSED — production changes may now merge. Version v1.3.3.1 (Architect ruling 2026-07-12: new SubPhase)

---

## Phase 4: User Story 2 — Detected, Never Silent, Data Loss (Priority: P2)

**Goal**: Monotonic Long counters replace Mod-based reader positions in MultiReaderRingBuffer — one work item fixing lap-aliasing AND providing loss amounts (research.md R2); overrun-aware read contract per contracts/monitor-reader-api.md

**Independent Test**: stall a reader, write > capacity, next interaction reports loss with exact byte count; post-resync reads contiguous; healthy reader unaffected (spec US2 Independent Test)

- [X] T012 [US2] Write `DSP_Processor.Tests/MultiReaderRingBufferTests.vb` against the NEW contract (FR-011) — reader independence, create-at-live-edge (no phantom backlog), exact-1×-capacity lap detected (the old aliasing case), multi-lap loss totals, resync-then-contiguous-reads, `Available` clamp to capacity, remove-reader-leaves-others-intact, partial read returns exact available count with no padding (analysis C2), `ReaderStats` accounting (data-model.md invariants 1–5), rate-limit/episode assertions — `OverrunEpisodeCount` increments only on transition into overrun after a clean read, `LastOverrunLogEmitted` respects the 1 s per-reader interval (FR-008 test-verified; analysis E1). Independence/lifecycle cases pass against current code; overrun cases FAIL until T013 — expected TDD state
- [X] T013 [US2] Refactor `DSP_Processor/Utils/MultiReaderRingBuffer.vb` — `totalBytesWritten As Long` + per-reader `TotalBytesRead As Long` (monotonic, Mod only for physical indexing); overrun predicate/resync per data-model.md invariant 3; new `Utils.ReaderStats` structure; `Read` ByRef-`bytesLost` overload; `Available` detect-and-resync; `GetReaderStats` — `ReaderStats` includes `OverrunEpisodeCount` and `LastOverrunLogEmitted` (analysis E1); rate-limited overrun logging on reader-side calls only (never `Write`) — interval 1 s per reader, emitted on transition into an overrun episode, i.e. first overrun after a clean read (analysis B1); correct the class-level "without contention" comment to document the single-lock design (plan Complexity Tracking)
- [X] T014 [US2] Add tap-API pass-throughs to `DSP_Processor/DSP/DSPThread.vb` — `ReadFromTap` ByRef-`bytesLost` overload and `GetTapReaderStats(tapLocation, readerName)` per contracts/monitor-reader-api.md; confirm `Write` path in `WorkerLoop` untouched (Constitution IV gate)
- [X] T015 [US2] Run full suite — T012 overrun cases now green alongside all US1 tests; verify no US1 test needed modification (behavior-lock held)

**Checkpoint**: Overrun detection live and proven. Version v1.3.3.2

---

## Phase 5: User Story 3 — One Way to Monitor (Priority: P3)

**Goal**: All consumers on the TapLocation reader API; 8 legacy methods deleted; tap semantics documented (FR-001/002/003)

**Independent Test**: API-surface grep audit returns zero legacy-member hits; meters/spectrum visually unchanged during record + playback (spec US3 Independent Test)

- [ ] T016 [P] [US3] Migrate 6 legacy call sites in `DSP_Processor/AudioIO/AudioRouter.vb` to `CreateTapReader`/`TapAvailable`/`ReadFromTap` with named readers per the research.md R6 map (`Router.InputSamples`, `Router.PostGainSamples`, `Router.PostOutputGainSamples`, `Router.OutputSamples`, `Router.OutputEvents`, `Router.InputEvents`); lazy reader creation on first use to match current behavior; consumers use the default (4-arg) `ReadFromTap` signature — FR-005-conformant, loss recorded at the reader (analysis F1)
- [ ] T017 [P] [US3] Migrate 2 legacy call sites in `DSP_Processor/Managers/RecordingManager.vb` (`RecMgr.PostGain`, `RecMgr.PostOutputGain`) per research.md R6 map; default (4-arg) `ReadFromTap` signature — FR-005-conformant per analysis F1
- [ ] T018 [US3] Delete the 8 legacy members from `DSP_Processor/DSP/DSPThread.vb` (`ReadInputMonitor`, `ReadOutputMonitor`, `ReadPostGainMonitor`, `ReadPostOutputGainMonitor`, `InputMonitorAvailable`, `OutputMonitorAvailable`, `PostGainMonitorAvailable`, `PostOutputGainMonitorAvailable`) and their hidden `"_default_*"` reader creation; run the SC-001 grep audit from quickstart.md — zero hits required
- [ ] T019 [US3] Write `Documentation/Architecture/Tap-Point-Semantics.md` (FR-003) — the four TapLocation positions from data-model.md's mapping table, the PostDSP≈PreOutput equivalence today, and the documented divergence conditions; link from `Documentation/Architecture/`
- [ ] T020 [US3] Full validation — `dotnet test` green; manual smoke per quickstart.md (arm mic → meters/FFT unchanged; file playback → output meters unchanged; State Debugger Panel pre-arm; log shows named readers) (SC-006)

**Checkpoint**: Single monitoring API achieved. Version v1.3.3.3

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation truth and constitution VIII bookkeeping

- [ ] T021 [P] Update `.github/instructions/audio.md` — replace stale `CreateMonitorReader`/`TapPoint` examples with the real consolidated API names (`CreateTapReader`/`TapLocation`/`ReadFromTap`) and add the overrun-stats query to the tap-point examples
- [ ] T022 [P] Per-story version bookkeeping in `DSP_Processor/Documentation/Active/Tasks.md` and `DSP_Processor/Documentation/Changelog/CURRENT.md` — one entry per completed story checkpoint on the v1.3.3.x lineage (Architect ruling 2026-07-12: new SubPhase), commit + tag per Constitution VIII; record the "tracker task = user story" granularity mapping in `Active/Tasks.md` (analysis D1)
- [ ] T023 Run complete quickstart.md validation end-to-end (build, test, audit, smoke) and record results in `specs/003-tap-consolidation/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: none — start immediately (T001 → T002)
- **Foundational (Phase 2)**: needs Phase 1; T003 and T004 are parallel; T005 needs T003; T006 needs T001–T005. BLOCKS all stories
- **US1 (Phase 3)**: needs Phase 2. T007–T010 all parallel (different files); T011 needs all four
- **US2 (Phase 4)**: needs US1's T011 gate (FR-015). T012 → T013 → T014 → T015 sequential (same-contract TDD chain; T013/T014 touch coupled files)
- **US3 (Phase 5)**: needs US2 complete (consumers migrate onto the upgraded API). T016 ∥ T017, then T018 → T019/T020
- **Polish (Phase 6)**: T021 anytime after US3; T022 rides each checkpoint; T023 last

### User Story Dependencies

Unusually for spec-kit, the stories are intentionally SEQUENTIAL (US1 → US2 → US3): FR-015 makes US1 a merge gate for the others, and US3 migrates consumers onto the API surface US2 finishes. Each story still delivers independent, testable value at its checkpoint.

### Parallel Opportunities

- T003 ∥ T004 (different files)
- T007 ∥ T008 ∥ T009 ∥ T010 — the big one: all four US1 test classes are independent files against stable production code
- T016 ∥ T017 (different consumer files)
- T021 ∥ T022 in polish

## Parallel Example: User Story 1

```text
# After T006, launch all four behavior-locking test classes together:
Task: "Write RingBufferTests.vb per FR-010 oracle in data-model.md"
Task: "Write SampleConversionTests.vb per FR-012 oracle"
Task: "Write GainProcessorTests.vb per FR-013 oracle"
Task: "Write GlobalStateMachineTests.vb per FR-014 8x8 matrix oracle"
```

## Implementation Strategy

**MVP = Phase 1 + 2 + US1 (T001–T011).** That alone converts the audio core from "believed correct" to "demonstrated correct" and satisfies the float32-pipeline precondition (SC-007). Stop, validate, version, tag.

Then increments: US2 (counter refactor, one work item per Architect direction) → validate → version; US3 (migration + deletion) → validate → version; polish rides along. Commit at every checkpoint per Constitution VIII; each story maps to one version increment on the ruled lineage.
