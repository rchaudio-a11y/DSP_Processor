# Implementation Plan: Float32 Processing Pipeline

**Branch**: `001-float32-pipeline` (real feature branch per Architect ruling; story checkpoints v1.3.5.x committed here, merged to master at feature end) | **Date**: 2026-07-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-float32-pipeline/spec.md` (clarified 2026-07-13: balance pan law, no dither, preserve-durations buffer policy)

## Summary

Swap the engine's processing format from 16-bit PCM to IEEE float32 at the
format declaration (`WaveFormat.CreateIeeeFloatWaveFormat`), let every
byte-oriented buffer carry the wider frames unchanged (time semantics
preserved automatically — buffer sizes already derive from
`AverageBytesPerSecond`), give `AudioBuffer` a zero-alloc `Span(Of Single)`
view so processors never touch bytes, and collapse all integer↔float
conversion into the canonical `Utils.SampleConversion` pair invoked only at
real boundaries. The playback path ends up with ZERO conversions (file reader
is already float; WaveOut accepts float natively); the record path converts
once at device entry (only for int16 devices) and once at file write.
GainProcessor is re-derived in the float domain with the balance pan law
(favored channel unity, cosine taper on the opposite channel) and no internal
clamping. The test contract is executed as specified: three suites survive
with empty diffs, processor math tests are superseded with recorded
supersession, and four new test groups land (null, headroom, non-finite,
allocation audit).

## Technical Context

**Language/Version**: VB.NET on .NET 10 (`net10.0-windows`); `Span(Of Single)` via `MemoryMarshal.Cast` (fine in VB for synchronous methods)

**Primary Dependencies**: NAudio 2.2.1 (`AudioFileReader` already delivers float; `WaveOutEvent` plays `IeeeFloat` WaveFormat; `WaveFileWriter` writes the 16-bit target); MSTest per the established suite

**Storage**: recorded output remains 16-bit PCM WAV (clarified: no dither, deterministic boundary)

**Testing**: MSTest, headless; FR-012 empty-diff gates enforced by `git diff` on the three surviving suites

**Target Platform**: Windows desktop (WinForms host)

**Project Type**: Desktop app + existing test project

**Performance Goals**: audio callback < 10 ms unchanged or better (per-stage double conversions deleted); steady-state hot loop zero allocations per block (SC-006, Constitution IV)

**Constraints**: ring buffers and monitor taps stay byte-based and format-agnostic (ruling 1 — the MRRB suite's survival depends on it); buffer capacities keep deriving from the format's `AverageBytesPerSecond` (ruling 5 — float format doubles it, preserving durations by construction); no dither (ruling 6)

**Scale/Scope**: ~12 production files (verified inventory in research R6/data-model migration map): DSPThread, AudioBuffer, ProcessorChain surface, GainProcessor, AudioRouter (format + feeder + taps + orphan-reader cleanup), RecordingManager, WasapiEngine, WaveInEngine/MicInputSource entry, RecordingEngine write exit, FFTProcessor, AudioLevelMeter, SampleConversion (canonical pair). Test project: 1 superseded class re-derived + 4 new test groups.

**Out of scope** (per spec): ASIO/multichannel/loopback (feature 004), new processor types, 24-bit export + dither.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*
*Constitution v1.1.0 (2026-07-12)*

| # | Principle | Gate assessment |
|---|-----------|-----------------|
| I | Single Ownership | PASS — ownership map untouched; the canonical conversion becomes the single owner of the int↔float boundary (today that responsibility is smeared across AudioRouter, WasapiEngine, and every consumer's read loop). |
| II | State Machine Architecture | PASS — no state machine changes; GSM matrix suite survives with empty diff (FR-012). |
| III | No Circular Dependencies | PASS — dependency edges unchanged; consumers lose their conversion code, gaining nothing new. |
| IV | Real-Time Audio Discipline | PASS, and this feature is its direct payoff: per-stage conversions (2 per processor per block) are deleted; `Span(Of Single)` views allocate nothing; the NEW allocation-audit test (FR-014) makes the zero-alloc hot loop a regression-tested property instead of a review claim. Feeder-thread per-block conversion allocation disappears on the playback path entirely. |
| V | Cross-Thread Safety | PASS — threading model untouched; buffers keep their locks/atomics; format is immutable after construction. |
| VI | Designer-First UI | N/A — meters/FFT panels change data-source math only, no control construction. |
| VII | RDF Methodology | PASS — this discharges the constitution's own named migration (Audio Constraints clause); boundaries/invariants in data-model.md precede code. |
| VIII | Task-Aligned Versioning | PASS — SubPhase 3.5 (v1.3.5.x), story-per-version checkpoints on the feature branch, merge to master at feature completion. |

**Post-design re-check (after Phase 1)**: no violations. One deliberate,
Architect-ruled behavior change (balance pan law) is governed by FR-011's
recorded-supersession process — the first intentional release of a
feature-003 behavior lock, handled explicitly, never silently.

## Project Structure

### Documentation (this feature)

```text
specs/001-float32-pipeline/
├── plan.md              # This file
├── spec.md              # + Clarifications session 2026-07-13
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (incl. the migration file map)
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── float-domain.md  # Phase 1 output
├── checklists/requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
DSP_Processor/
├── Utils/
│   ├── SampleConversion.vb      # CANONICAL PAIR: FloatToPcm16 (locked contract,
│   │                            #   + non-finite guard) + NEW Pcm16ToFloat
│   └── AudioLevelMeter.vb       # 32-bit float input path
├── DSP/
│   ├── AudioBuffer.vb           # NEW SampleSpan() As Span(Of Single); SampleCount = bytes\4
│   ├── DSPThread.vb             # format-agnostic already; constructed with float format
│   ├── GainProcessor.vb         # RE-DERIVED: Span math, balance pan, NO internal clamp
│   └── FFT/FFTProcessor.vb      # 32-bit float AddSamples path
├── AudioIO/
│   ├── AudioRouter.vb           # float format at DSPThread creation; feeder passes float
│   │                            #   through (entry conversion DELETED); taps reinterpret
│   │                            #   float bytes; orphan _default_* reader creation DELETED
│   ├── WasapiEngine.vb          # private ConvertFloatToPCM16 DELETED; float pass-through
│   ├── WaveInEngine.vb          # int16 device entry → canonical Pcm16ToFloat
│   └── MicInputSource.vb        # capture format plumbing
├── Managers/RecordingManager.vb # float format at DSPThread creation; tap reads reinterpret
└── Recording/RecordingEngine.vb # write exit: canonical FloatToPcm16 → 16-bit WAV

DSP_Processor.Tests/
├── SampleConversionTests.vb     # SURVIVING (empty diff) + new Pcm16ToFloat/non-finite group
├── GlobalStateMachineTests.vb   # SURVIVING (empty diff)
├── MultiReaderRingBufferTests.vb# SURVIVING (empty diff)
├── GainProcessorTests.vb        # SUPERSEDED: re-derived in float domain (recorded, FR-011)
└── FloatPipelineTests.vb        # NEW: null test, headroom, balance-pan, allocation audit
```

**Structure Decision**: no new projects; one new test class; the canonical
conversion grows its inverse in place.

## Complexity Tracking

> No constitution violations to justify. Two consciously accepted notes:
> (1) `Span(Of Single)` in VB is used only in synchronous processor methods
> (VB cannot use Span in Async/Iterator contexts — none exist on these paths).
> (2) The playback device is fed IEEE float directly (zero-conversion path);
> if a legacy driver ever rejects it, the fallback is one boundary conversion
> before the output provider — deferred until observed (YAGNI), noted in
> research R2 and the manual smoke checklist.
