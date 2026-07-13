# Implementation Plan: State Machine Hardening & Coordinator Lifecycle

**Branch**: `002-state-machine-hardening` | **Date**: 2026-07-12 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-state-machine-hardening/spec.md`

## Summary

Restructure `GlobalStateMachine.TransitionTo` around a single-drainer design:
transitions commit under the lock, but state-change events are delivered
OUTSIDE it, in strict commit order, with per-subscriber exception containment.
Requests arriving during a transition's commit-plus-delivery window are held
in a lossless FIFO deferral queue and executed by the drainer, each validated
at execution time; every caller receives a truthful three-valued outcome
(performed / deferred / rejected) via a new `RequestTransition` API while the
legacy Boolean `TransitionTo` becomes truthful (True = performed, only).
All 11 direct GSM event subscribers are audited for live-state reads.
`StateCoordinator` loses its disposal surface entirely (grep-verified: zero
production callers dispose it), and the GSM's empty entry/exit scaffolding is
deleted with its one real behavior (error-state logging) inlined.

## Technical Context

**Language/Version**: VB.NET on .NET 10 (`net10.0-windows`)

**Primary Dependencies**: none new — existing MSTest harness from feature 003 (`DSP_Processor.Tests`, `Logger.SuppressForTesting`)

**Storage**: N/A

**Testing**: MSTest, headless; existing 55-test suite is the behavior lock (FR-014); new `StateMachineHardeningTests` class covers FR-013

**Target Platform**: Windows desktop (WinForms host)

**Project Type**: Desktop app + existing test project

**Performance Goals**: transition commit path stays allocation-light; no change to audio-path code (this feature is UI/State-layer only)

**Constraints**: existing `GlobalStateMachineTests` (64-pair matrix) and all other v1.3.3.3 tests MUST pass with zero assertion changes; `IStateMachine(Of T)` interface signature for `TransitionTo` is retained; event delivery stays synchronous in-process (spec assumption — no async dispatcher)

**Scale/Scope**: 2 production classes restructured (GlobalStateMachine, StateCoordinator), 11 subscriber handlers audited (3 SSM/UI + 8 cognitive), 1 new test class (the data-model test map's 11 named tests plus audit-driven additions), 1 audit document

**Out of scope** (per spec): SSM-internal locking/queueing semantics; transition matrix changes; async event dispatch; features 001/003 concerns

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*
*Constitution v1.1.0 (2026-07-12)*

| # | Principle | Gate assessment |
|---|-----------|-----------------|
| I | Single Ownership | PASS — the GSM remains sole owner of global state, and gains sole ownership of the deferral + event-delivery queues. Removing coordinator disposal *clarifies* ownership: subsystem teardown stays with subsystem owners (the coordinator never owned them — its Dispose already said so). |
| II | State Machine Architecture | PASS — transition rules unchanged (matrix-locked by tests); transitions remain explicit, named, validated. Truthful outcomes strengthen the "single auditable code path" property. |
| III | No Circular Dependencies | PASS — no new dependency edges; the event-delivery change *removes* a runtime cycle hazard (lock-ordering loop between GSM and subscribers). |
| IV | Real-Time Audio Discipline | N/A — no audio-path code touched. Event delivery is UI/state-layer. |
| V | Cross-Thread Safety | PASS — this feature is the direct implementation of Principle V's lock rule: "blocking Invoke is prohibited from any path that can hold a state-machine lock" generalizes to "no foreign code runs under the machine's lock." Queues are guarded by the existing `_stateLock`; `CurrentState` stays Interlocked. |
| VI | Designer-First UI | N/A — no UI construction changes. |
| VII | RDF Methodology | PASS — this is the Phase 2→4 recursion the 2026-07-12 review prescribed (P2: "raising outside the lock removes the disease rather than treating the symptom"). Boundaries/invariants documented in data-model.md before code. |
| VIII | Task-Aligned Versioning | PASS — story checkpoints map to v1.3.4.x (new SubPhase 3.4, same granularity ruling as 3.3: tracker task = user story). |

**Post-design re-check (after Phase 1)**: no violations introduced. The
deferral queue is bounded only by a diagnostic (spec assumption); its state
is owned entirely by the GSM (Principle I), mutated only under `_stateLock`
(Principle V). The F1 amendment's cross-thread blocking is Principle-V-safe:
a caller waits only for subscriber delivery that is itself non-blocking
(`UIStateMachine` posts via `BeginInvoke` and returns), so the window always
closes without needing the blocked thread — bounded wait, no cycle.

## Project Structure

### Documentation (this feature)

```text
specs/002-state-machine-hardening/
├── plan.md              # This file
├── spec.md
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── transition-api.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
DSP_Processor/
├── State/
│   ├── GlobalStateMachine.vb        # RESTRUCTURED: single-drainer TransitionTo,
│   │                                #   RequestTransition + TransitionOutcome,
│   │                                #   FIFO deferral queue, out-of-lock ordered
│   │                                #   delivery, per-subscriber catch;
│   │                                #   OnStateExiting/OnStateEntering deleted
│   │                                #   (Error-entry log inlined)
│   ├── StateCoordinator.vb          # IDisposable/Dispose/CheckDisposed/_disposed
│   │                                #   REMOVED (~27 guard sites + barrier sleep)
│   ├── UIStateMachine.vb            # AUDIT (subscriber)
│   ├── RecordingManagerSSM.vb       # AUDIT (subscriber)
│   └── PlaybackSSM.vb               # AUDIT (subscriber)
├── Cognitive/                       # AUDIT (8 subscriber classes, see research R7)
└── Documentation/Architecture/
    └── GSM-Subscriber-Audit.md      # NEW: FR-009 audit record

DSP_Processor.Tests/
├── StateMachineHardeningTests.vb    # NEW: FR-013 (losslessness, outcomes,
│                                    #   out-of-lock delivery, ordering, containment)
└── GlobalStateMachineTests.vb       # UNCHANGED (FR-014 behavior lock)
```

**Structure Decision**: no new projects; one new test class; one new audit
document. All production changes concentrate in `State/`.

## Complexity Tracking

> No constitution violations to justify. One consciously accepted asymmetry:
> `RequestTransition`/`TransitionOutcome` are added to `GlobalStateMachine`
> only — NOT to the `IStateMachine(Of T)` interface — so the five SSMs are
> untouched (their internal semantics are out of scope). If SSMs later need
> truthful outcomes, promoting the member to the interface is a separate,
> deliberate change.
