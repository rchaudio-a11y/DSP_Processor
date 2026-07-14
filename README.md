# DSP_Processor

*A real-time stereo audio engine for Windows, built on an explicit state-machine architecture.*

---

## Overview

DSP_Processor is a VB.NET / .NET 10 WinForms application for real-time audio capture, processing, playback, and recording. It handles device input and file playback, runs audio through a dedicated DSP worker thread with lock-free ring buffers, records to WAV, and drives live meters and FFT visualization.

What makes it unusual is not the audio path — it's the control system wrapped around it. The application's behavior is governed by an explicit hierarchy of state machines (a `GlobalStateMachine` plus eight subsystem machines), coordinated centrally and catalogued in a State Registry. The result reads less like a typical hobby DAW and more like an industrial control system: every mode is a named state, every transition is validated against an interlock table, and the UI renders state rather than defining it.

The project is developed solo under a formal governance model — a ratified constitution, spec-driven features (SpecKit), and the Recursive Development Framework (RDF) — with a companion test suite that locks behavior before it changes.

---

## Architecture

### Layers

Audio I/O, DSP, State, Managers, and UI are real boundaries, not folder decoration. Communication flows in one direction by rule: **upward via events, downward via direct calls**, and no dependency cycles are permitted.

```
UI            WinForms panels — emit events, render state (no business logic)
Managers      Action coordinators (recording, monitoring) — stateless
State         GlobalStateMachine + 8 SSMs + StateCoordinator + State Registry
DSP           DSP worker thread, processor chain, FFT
AudioIO       AudioRouter, device/file readers, ring buffers, tap points
```

### State machines (GSM + SSMs)

A single `StateCoordinator` orchestrates nine machines total: the root `GlobalStateMachine` (GSM) and eight Subsystem State Machines (SSMs), each owning exactly one subsystem's state:

| Machine | Owns |
|---|---|
| `GlobalStateMachine` | Application-level state; the validation authority |
| `RecordingManagerSSM` | Recording lifecycle (arm → record → stop) |
| `PlaybackSSM` | File playback lifecycle |
| `DSPThreadSSM` | DSP worker thread lifecycle |
| `DSPModeSSM` | DSP enable/disable mode |
| `AudioDeviceSSM` | Driver backend (WASAPI today; ASIO/DirectSound states reserved) |
| `AudioInputSSM` | Physical input device selection + USB hot-plug detection |
| `AudioRoutingSSM` | Routing topology and tap-point lifecycle |
| `UIStateMachine` | UI state mapping (marshals via `BeginInvoke`) |

Transitions are explicit, named, validated, and logged with grep-friendly transition IDs. Illegal transitions are rejected by design — the transition table functions as an interlock matrix.

### Signal chain and tap points

Audio flows through four fixed observation points. Meters and FFT are **event-driven consumers** of these taps via named multi-reader ring-buffer cursors — they never receive copies of audio-thread buffers.

```
Input → [PreDSP] → Input Gain → [PostGain] → DSP → [PostDSP] → Output Gain → [PreOutput] → Output
```

### Cognitive layer

A self-observation layer (`Cognitive/`) sits on top of the State Registry — prediction, anomaly detection, and adaptive thresholds (the Introspective Engine v2.0 design) — giving the system runtime visibility into its own state behavior. It is an observability/introspection layer, not a general-purpose reasoning engine.

---

## Core principles

Governed by `.specify/memory/constitution.md` (v1.1.0). In brief:

1. **Single ownership per subsystem** — one owner for every piece of state; no shared mutable state.
2. **State-machine architecture** — all application/subsystem state is modeled as explicit, validated machines; UI is state-driven.
3. **No circular dependencies** — the dependency graph is acyclic; upward = events, downward = direct calls.
4. **Real-time audio discipline (non-negotiable)** — zero allocations/locks in hot loops; callbacks < 10 ms; event-driven audio flow, no polling.
5. **Cross-thread safety by construction** — `Interlocked`/`Volatile` for all shared flags; UI marshals via non-blocking `BeginInvoke`.
6. **Designer-first UI** — controls declared in the WinForms Designer; code only initializes (two documented carve-outs for owner-paint and explicit dynamic sections).
7. **RDF methodology** — architecture-first, bugs-as-teachers, documentation-as-synthesis.
8. **Task-aligned versioning** — `v[Major].[Phase].[SubPhase].[Task]`, synchronized with the task list and changelog.

---

## Tech stack

- **Language / runtime:** VB.NET, .NET 10 (`net10.0-windows`), Windows Forms
- **Audio:** [NAudio](https://github.com/naudio/NAudio) 2.2.1 (WASAPI / WaveOut)
- **Device monitoring:** `System.Management` 8.x (WMI-based USB hot-plug)
- **Serialization:** Newtonsoft.Json 13.0.3
- **Tests:** MSTest 3.6.3 (`InternalsVisibleTo` grants the suite friend access)

---

## Project structure

```
DSP_Processor/
  AudioIO/          AudioRouter, device/file readers, ring buffers, tap points
  Audio/Routing/    Routing support
  DSP/              DSP worker thread, processor chain
  DSP/FFT/          FFT / spectrum
  State/            GSM, 8 SSMs, StateCoordinator, IStateMachine, registry
  Managers/         MonitoringController and other action managers
  Recording/        RecordingEngine, WAV writing
  Cognitive/        Introspection: prediction, anomaly detection, thresholds
  Models/           Shared data types (AudioBuffer, etc.)
  Services/         Service interfaces + implementations
  UI/, UI/TabPanels/  WinForms panels
  Utils/            RingBuffer, Logger, helpers
  Visualization/    Meters, spectrum, waveform controls

DSP_Processor.Tests/   MSTest suite (ring buffers, transition matrix, conversion, gain, float pipeline)
Documentation/         Architecture, Active, Reference, Archive (see the roadmap below)
specs/                 SpecKit features (001-float32-pipeline, 002-state-machine-hardening, 003-tap-consolidation)
.specify/              Constitution, templates, workflows
```

---

## Build and test

Requires the .NET 10 SDK and Windows (WinForms + WASAPI). Visual Studio 2022+ or `dotnet` CLI.

```bash
dotnet build DSP_Processor.slnx
dotnet test DSP_Processor.slnx
```

The test suite is written as **behavior-locking (characterization) tests** — locking current behavior before a change so refactors like the float-pipeline migration can be verified against an unchanging contract.

---

## Governance and versioning

Development is spec-driven. Each feature is specified, planned against a **Constitution Check** gate, and implemented against that spec under RDF. Versions follow `v[Major].[Phase].[SubPhase].[Task]` and stay synchronized with the task list and changelog.

**Roles:** the Architect (Rick) holds final authority on structure and decisions; a Design Consultant / Documenter frames options and writes specs and docs; an Implementor writes code against the specs. This separation is deliberate and load-bearing.

---

## Status

Active development. Current line: **v1.3.4.x**, with feature **001-float32-pipeline** in flight — migrating internal DSP to float32 end-to-end, with integer PCM confined to I/O boundaries (the current Int16 pipeline is legacy). Phase 7's state-machine expansion (the four modeful SSMs) is complete.

This README is intentionally high-level and does not track live phase status. For the authoritative current state, the project arc, and the forward roadmap, see:

- **`Documentation/Architecture/Architecture-Roadmap-2026-07-13.md`** — the document index, project eras, and where things are going
- **`Documentation/Active/`** — current reviews, task lists, and session guides
- **`.specify/memory/constitution.md`** — the binding principles

---

*DSP_Processor is a solo project by Rick Haughton, developed under RCH Automation LLC.*
