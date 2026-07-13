# Contract: Global State Machine Transition API

**Date**: 2026-07-12 | **Feature**: 002-state-machine-hardening

## Surface (post-feature)

```vb
Public Enum TransitionOutcome
    Performed   ' validated and committed during this call
    Deferred    ' arrived during a commit-through-delivery window; queued FIFO
    Rejected    ' invalid from current state; state unchanged
End Enum

' NEW - the truthful surface
Public Function RequestTransition(newState As GlobalState, reason As String) As TransitionOutcome

' KEPT (IStateMachine member) - now truthful within its range:
' True IFF this call performed the transition. False = rejected OR deferred.
' It can never again claim success for a transition that did not occur.
Public Function TransitionTo(newState As GlobalState, reason As String) As Boolean
```

## Behavioral guarantees

| # | Guarantee | Spec ref |
|---|-----------|----------|
| 1 | No request is ever silently dropped: performed + rejected + deferred-then-(performed\|rejected) accounts for 100% of requests | FR-001, SC-001 |
| 2 | Deferred requests execute in FIFO arrival order, each validated against the state current at its execution | FR-004, FR-005 |
| 3 | Rejections (direct and deferred) are logged in State-Registry grep format; deferred executions carry an origin marker; transition history remains success-only (unchanged semantics) | FR-004, research R4 |
| 4 | `_stateLock` is never held while subscriber code runs | FR-006 |
| 5 | Events are delivered in exact commit order (transition IDs strictly ascending in delivery order); single drainer | FR-007 |
| 6 | A throwing subscriber is logged and skipped; remaining subscribers are still notified; the committed transition is unaffected | FR-008 |
| 7 | Event payload is authoritative for that transition; live `CurrentState` may have advanced by delivery time — subscribers must not re-read it (audited, FR-009) | US2-AS3 |
| 8 | Queue depth > 16 emits a loud diagnostic warning (observable via `Friend DepthWarningCount`, analysis E1); nothing is dropped at any depth | FR-005 |
| 9 | The 64-pair matrix contract (26 valid / 38 invalid) is byte-identical; `IsValidTransition` untouched | FR-003, SC-002 |
| 10 | `Deferred` is returned ONLY to same-thread (in-handler) requests. Cross-thread callers arriving mid-window block until the window closes, then execute normally (Performed/Rejected) — the woken caller becomes the next drainer. This preserves the pre-feature cross-thread blocking semantics exactly (analysis F1) | FR-002, research R1 |

## Removed surface

- `StateCoordinator`: `Implements IDisposable`, `Dispose()`, `CheckDisposed()`,
  `_disposed` — the coordinator is process-lifetime; disposal misuse becomes
  a compile error (grep-verified: zero existing callers).
- `GlobalStateMachine`: `OnStateExiting` / `OnStateEntering` scaffolding —
  error-state entry logging preserved inline; `_pendingTransition` /
  `_pendingReason` single slot replaced by the FIFO queue.

## Compatibility notes

- `IStateMachine(Of T)` is unchanged; SSMs are unchanged (out of scope).
- All existing Boolean call sites keep compiling. Callers that today receive
  a lying `True` from the queued path will receive `False` (deferred) — and
  per guarantee 10, only in-handler cascade code can ever see that. Production
  Boolean callers (MainForm user-action paths, incl. the modal error dialog
  at MainForm.vb:988) block-then-perform exactly as they do today, so their
  observed behavior is unchanged **by construction** (analysis F1).
- `StateChangedEventArgs(Of GlobalState)` payload is unchanged — no
  subscriber signature churn.
