# Phase 1 Data Model: State Machine Hardening & Coordinator Lifecycle

**Date**: 2026-07-12 | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)

## TransitionOutcome (new public enum)

| Value | Meaning | Boolean surface maps to |
|-------|---------|------------------------|
| `Performed` | Transition validated and committed during this call | `True` |
| `Deferred` | Request arrived from within the current window's own delivery (same-thread re-entrancy — the ONLY producer of this value, analysis F1); queued FIFO; final fate observable in log | `False` |
| `Rejected` | Transition invalid from current state; state unchanged | `False` |

## Transition Request (internal queued record)

| Field | Type | Semantics |
|-------|------|-----------|
| `TargetState` | `GlobalState` | Requested destination |
| `Reason` | `String` | Caller's reason (flows into event args + log) |
| `OriginTransitionID` | `String` | The transition whose window this request arrived in (for the deferred-execution log marker, research R4) |

## Deferral Queue (GSM-internal)

- `Queue(Of TransitionRequest)` — guarded exclusively by `_stateLock`
  (Constitution I: owned by GSM; Constitution V: single guard).

### Invariants (sacred — RDF Phase 2)

1. **Lossless**: enqueue never overwrites; every enqueued request is
   eventually dequeued and either performed or rejected — exactly once.
2. **FIFO**: requests execute in arrival order. Per the F1 amendment the
   queue holds only same-thread in-window requests, so arrival order is
   simply program order within the drainer thread (cross-thread callers
   never enqueue — they block, see invariant 8).
3. **Late validation**: a dequeued request is validated against the state
   current at its execution, never the state at its arrival.
4. **Drain-to-empty**: the drainer exits only when the queue is empty and the
   last event is fully delivered; deferrals created during the drain extend
   the same drain.
5. **Diagnostic bound**: depth > 16 logs a loud warning naming the states
   involved (runaway-cascade indicator); nothing is ever dropped. Emission
   is test-observable via `Friend ReadOnly Property DepthWarningCount`
   (analysis E1, following the 003-E1 observable-seam precedent — the log
   itself is suppressed under test).

## Event Delivery (GSM-internal ordered delivery)

### Invariants

6. **Out-of-lock**: `_stateLock` is never held while any subscriber code
   executes. The lock guards only: state word, queues, history, counters.
7. **Commit order**: events are delivered in exactly the order transitions
   committed; transition IDs (assigned at commit) are strictly ascending in
   delivery order with no gaps or reorders.
8. **Single drainer**: at most one thread delivers events at any time (the
   thread that opened the processing window); this is what makes invariant 7
   cheap. Cross-thread callers arriving mid-window block (Monitor wait,
   loop-checked) until the window closes; a woken caller becomes the next
   drainer (analysis F1) — never a second concurrent one.
9. **Containment**: each subscriber is invoked in its own Try/Catch; a
   throwing subscriber is logged and skipped, never aborting delivery to the
   rest, never affecting committed state (a handler cannot un-commit).
10. **Payload authority**: by delivery time, live state may already have
    advanced; the event args (OldState, NewState, Reason, TransitionID) are
    the authoritative record of that transition. Subscribers act on payload
    only (FR-009 audit enforces).

### Processing-window lifecycle

```
Idle ──request──> Drainer: commit T1 under lock
                      │ (same-thread in-handler requests → Deferred, FIFO)
                      │ (cross-thread callers → block until window closes,
                      │  then proceed as next drainer — analysis F1)
                  release lock
                  deliver T1 event (all subscribers, contained)
                  re-acquire lock
                      ├─ queue non-empty → dequeue, validate:
                      │     valid → commit Tn, loop (deliver Tn ...)
                      │     invalid → log rejection (marked deferred), loop
                      └─ queue empty → close window → pulse waiters → Idle
```

## GlobalStateMachine surface changes

| Member | Change |
|--------|--------|
| `RequestTransition(newState, reason) As TransitionOutcome` | NEW — truthful surface |
| `TransitionTo(newState, reason) As Boolean` | KEPT (interface member) — now `= (RequestTransition(...) = Performed)`; never lies |
| `StateChanged` event + `StateChangedEventArgs(Of GlobalState)` | UNCHANGED payload; delivery semantics per invariants 6–10 |
| `IsValidTransition` | UNCHANGED (matrix-locked) |
| `CurrentState`, `GetTransitionHistory`, `ClearHistory` | UNCHANGED semantics (history stays success-only, research R4) |
| `_pendingTransition`/`_pendingReason` single slot | DELETED (replaced by queue) |
| `_isTransitioning` re-entry guard | DELETED (replaced by the window-open flag + Monitor wait; analysis F3) |
| `DepthWarningCount` (`Friend ReadOnly`) | NEW — observable seam for the depth>16 diagnostic (analysis E1) |
| `OnStateExiting` | DELETED (fully empty) |
| `OnStateEntering` | DELETED — error-state entry logging inlined into commit path (FR-012) |

## StateCoordinator surface changes

| Member | Change |
|--------|--------|
| `Implements IDisposable` + `Dispose()` | DELETED (Architect ruling; grep-verified zero callers) |
| `CheckDisposed()` + `_disposed` + ~27 guard calls | DELETED |
| 50 ms shutdown-barrier sleep | DELETED (goes with Dispose) |
| `Initialize`, all properties, `GetSystemState`, `DumpAllStates`, recovery | UNCHANGED semantics (minus guards) |

## Subscriber audit record (FR-009 / SC-005 deliverable)

`Documentation/Architecture/GSM-Subscriber-Audit.md` — one row per subscriber
from research R7's inventory:

| Subscriber | Handler | Live-state reads found? | Action taken | Verified payload-only |
|-----------|---------|------------------------|--------------|----------------------|
| (11 rows, filled during implementation) | | | | |

Plus an explicit boundary note: second-order subscribers (DSPThreadSSM ←
RecordingManagerSSM, MainForm ← UIStateMachine) and SSM-internal delivery
are feature-002-adjacent but out of scope; risk recorded, not fixed here.

## Test suite structure (FR-013 → class map)

| Test | Proves | SC |
|------|--------|----|
| `Deferral_ThreeRequestsInWindow_AllExecuteInOrder` | Invariants 1–4 | SC-001 |
| `Outcomes_Direct_Performed_Rejected_TruthfulOnBothSurfaces` | Outcome mapping | SC-001 |
| `Outcome_InWindow_DeferredThenExecuted` | Deferred truthfulness | SC-001 |
| `DeferredRequest_InvalidAtExecution_RejectedVisibly` | Invariant 3 + FR-004 | SC-001 |
| `CrossThread_CallerBlocksThenPerforms_NeverDeferred` | Invariant 8 / F1 amendment | SC-001 |
| `Delivery_LockNotHeld_CrossThreadProbe` | Invariant 6 | SC-004 |
| `Deadlock_SubscriberOwnLock_CrossThreadTransition_Completes` | Cycle eliminated | SC-003 |
| `Ordering_100TransitionsWithCascade_StrictCommitOrder` | Invariant 7 | SC-004 |
| `Containment_ThrowingSubscriber_OthersStillNotified` | Invariant 9 | FR-008 |
| `QueueDepthDiagnostic_LoudWarningPast16` | Invariant 5 (behavior + `DepthWarningCount` seam, E1) | FR-005 |
| `Coordinator_HasNoDisposalSurface` | reflection check | SC-006 |
| Existing `GlobalStateMachineTests` (64-pair matrix) | UNCHANGED | SC-002 |
