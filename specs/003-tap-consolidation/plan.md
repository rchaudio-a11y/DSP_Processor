# Implementation Plan: Tap Monitoring Consolidation & Audio Core Test Foundation

**Branch**: `003-tap-consolidation` | **Date**: 2026-07-12 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-tap-consolidation/spec.md`

## Summary

Establish the project's first automated test suite (MSTest) over the pure-logic
audio core and global state machine, then replace the multi-reader monitor
buffer's Mod-based reader positions with monotonic total-bytes Long counters —
one change that both eliminates silent lap-aliasing and yields exact loss
amounts — and finally migrate all monitor consumers onto the single TapLocation
reader API so the four deprecated per-tap read/available method pairs can be
deleted. Sequencing is fixed by FR-015: behavior-locking tests land first.

## Technical Context

**Language/Version**: VB.NET on .NET 10 (`net10.0-windows`)

**Primary Dependencies**: NAudio 2.2.1 (WaveFormat construction in prod and tests); MSTest (Microsoft.NET.Test.Sdk + MSTest.TestAdapter + MSTest.TestFramework)

**Storage**: N/A (no persistence changes; tests must produce zero file I/O)

**Testing**: MSTest — new project `DSP_Processor.Tests` referencing `DSP_Processor`; run via `dotnet test`, headless, no audio hardware

**Target Platform**: Windows desktop (WinForms host app; tests are plain library code)

**Project Type**: Desktop app + new test project (first in the solution)

**Performance Goals**: No regression to audio-path budgets (callback < 10 ms); monitor `Write` path keeps its current cost profile (single lock, zero allocations)

**Constraints**: MultiReaderRingBuffer single-lock design is retained by explicit scope decision (contention fix deferred — documented, not fixed); overrun reporting must add no work to the DSP write path beyond integer counter math; unit tests must not touch `Utils.Logger` file output

**Scale/Scope**: 2 classes refactored (MultiReaderRingBuffer, thin DSPThread surface), 2 consumer files migrated (AudioRouter, RecordingManager), 8 legacy methods deleted, 1 extracted conversion utility, ~5 test classes (~60–80 test cases incl. 64-pair GSM matrix)

**Out of scope** (per feature description): GSM re-entrant queue semantics (feature 002-state-machine-hardening); sample-format migration (feature 001-float32-pipeline); MultiReaderRingBuffer lock-contention redesign (review P4)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*
*Constitution v1.1.0 (2026-07-12)*

| # | Principle | Gate assessment |
|---|-----------|-----------------|
| I | Single Ownership | PASS — overrun state (bytes lost, event count) is owned per-reader inside MultiReaderRingBuffer; no state duplicated to consumers. DSPThread remains the sole owner of tap buffers. |
| II | State Machine Architecture | PASS — no transition rules change. The GSM transition matrix is *encoded as a test oracle*, strengthening this principle. Re-entrant queue semantics explicitly untouched (feature 002). |
| III | No Circular Dependencies | PASS — `DSP_Processor.Tests` → `DSP_Processor` is a one-way edge. Extracting `SampleConversion` into `Utils` removes an AudioIO-owned utility that DSP-layer tests would otherwise need. |
| IV | Real-Time Audio Discipline | PASS — counter refactor replaces Mod arithmetic with Long subtraction inside the existing lock; no allocations added to `Write`. Overrun *detection and logging happen on the reader side* (UI-thread calls), never in the write path. Rate-limited logging per FR-008. |
| V | Cross-Thread Safety | PASS — the Long counters are read/written only under the existing `readerLock` (single-lock design retained by scope decision), so no torn Long reads and no new bare cross-thread fields. The Logger test-suppress switch is set once in test AssemblyInitialize before any logger access. |
| VI | Designer-First UI | N/A — no UI construction changes; meters/FFT panels keep their code paths, only the data source call changes. |
| VII | RDF Methodology | PASS — this feature is the Validation Loop (Phase 5) the review called for; boundaries/invariants are documented in data-model.md before code (Phase 2 discipline). |
| VIII | Task-Aligned Versioning | PASS — story checkpoints map to version increments on the v1.3.3.x lineage (new SubPhase per Architect ruling 2026-07-12; granularity: tracker task = user story). |

**Post-design re-check (after Phase 1)**: no new violations introduced by the
design artifacts. The single-lock retention is a documented known limitation,
not a constitution violation (Principle IV governs the audio thread's write
path, which is unchanged).

## Project Structure

### Documentation (this feature)

```text
specs/003-tap-consolidation/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── monitor-reader-api.md   # Phase 1 output
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
DSP_Processor/                        # existing app project
├── DSP/
│   └── DSPThread.vb                  # MODIFIED: delete 8 legacy monitor methods;
│                                     #   add overrun query pass-through on tap API
├── Utils/
│   ├── MultiReaderRingBuffer.vb      # MODIFIED: Long total-bytes counters,
│   │                                 #   overrun detection/resync, per-reader stats
│   ├── SampleConversion.vb           # NEW: Public Shared float32->PCM16 conversion
│   │                                 #   (extracted from AudioRouter)
│   └── Logger.vb                     # MODIFIED: test-suppress mode (no file I/O)
├── AudioIO/
│   └── AudioRouter.vb                # MODIFIED: 6 legacy call sites -> tap API;
│                                     #   ConvertFloatToPCM16 delegates to Utils
├── Managers/
│   └── RecordingManager.vb           # MODIFIED: 2 legacy call sites -> tap API
└── State/
    └── GlobalStateMachine.vb         # UNCHANGED (matrix is test oracle only)

DSP_Processor.Tests/                  # NEW MSTest project (net10.0-windows, VB)
├── DSP_Processor.Tests.vbproj
├── TestSetup.vb                      # AssemblyInitialize: Logger suppress mode
├── RingBufferTests.vb                # FR-010
├── MultiReaderRingBufferTests.vb     # FR-011 (independence, overrun, resync)
├── SampleConversionTests.vb          # FR-012
├── GainProcessorTests.vb             # FR-013
└── GlobalStateMachineTests.vb        # FR-014 (exhaustive 8x8 matrix)
```

**Structure Decision**: single new sibling test project beside the app project,
added to the existing solution. No production project splits; the only new
production file is the extracted `Utils/SampleConversion.vb`.

## Complexity Tracking

> No constitution violations to justify. One consciously retained known
> limitation (not a violation): MultiReaderRingBuffer keeps its single shared
> lock; the class-level comment will be corrected to describe the actual
> contention behavior (review finding P4, deferred by scope decision).
