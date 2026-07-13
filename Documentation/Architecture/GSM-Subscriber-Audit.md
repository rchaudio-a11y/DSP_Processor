# GSM Subscriber Audit (Feature 002, FR-009 / SC-005)

**Date:** 2026-07-13
**Version:** v1.3.4.2
**Context:** Feature 002 moved state-change event delivery OUTSIDE the
GlobalStateMachine's lock. By delivery time, live machine state may already
have advanced past the event's payload — so every subscriber was audited for
reads of live machine state inside its handler. Contract: subscribers act on
the event payload (OldState, NewState, Reason, TransitionID, Timestamp) only.

## Audit method

For each direct subscriber to `GlobalStateMachine.StateChanged` (inventory:
research R7, grep-verified): inspect the handler and everything it calls
synchronously for reads of `GlobalStateMachine.CurrentState`,
`StateCoordinator.Instance.GlobalState`, or any other live machine state.
Classify: **payload-only (clean)** / **live-read (fix)** /
**live-read (justified — documented why)**.

## Findings — 11 subscribers, 0 fixes required

| # | Subscriber | Handler | Finding | Action | Verified payload-only |
|---|-----------|---------|---------|--------|----------------------|
| 1 | `State/UIStateMachine.vb:63` | `OnGlobalStateChanged` | Maps `e.NewState` → UIState; no live reads | none | ✅ |
| 2 | `State/RecordingManagerSSM.vb:71` | `OnGlobalStateChanged` | Reads its **own** `CurrentState` (:190, :208) to gate its own transitions — its own domain, not GSM live state; GSM data from payload only | none | ✅ justified (own state) |
| 3 | `State/PlaybackSSM.vb:61` | `OnGlobalStateChanged` | Same pattern: own `CurrentState` reads (:167–:192); GSM data from payload | none | ✅ justified (own state) |
| 4 | `Cognitive/ConflictDetector.vb:59` | `OnGlobalStateChanged` | Calls `CheckConsistency()` which reads LIVE `_coordinator.GlobalState` (:192) and live SSM states — **by design**: its function is comparing live machines for divergence | none — justified. NOTE: mid-cascade snapshots can transiently diverge (GSM advanced, SSMs not yet), so occasional false-positive consistency warnings during cascades are expected and acceptable (they also occurred pre-feature) | ✅ justified (live-by-design) |
| 5 | `Cognitive/PredictionEngine.vb:64` | `OnStateChanged` | Delegates payload (`e.TransitionID/OldState/NewState`) to prediction logic; reads habit stats, not machine state | none | ✅ |
| 6 | `Cognitive/HabitLoopAnalyzer.vb:66` | `OnTransition` | `RecordTransition(e.TransitionID)` only | none | ✅ |
| 7 | `Cognitive/NarrativeGenerator.vb:69` | `OnGlobalStateChanged` | Payload → `CountAction(oldState, newState)` | none | ✅ |
| 8 | `Cognitive/AnomalyDetector.vb:60` | `OnStateChanged` | Payload → `AnalyzeTransition`; reads working memory + habit patterns, not machine state | none | ✅ |
| 9 | `Cognitive/CognitiveLayer.vb:511` | `OnGlobalStateChanged` | Empty body (subsystems subscribe independently) | none | ✅ |
| 10 | `Cognitive/AdaptiveThresholdManager.vb:58` | `OnStateChanged` | Payload delegation → `AdaptThresholds()` reads habit statistics only | none | ✅ |
| 11 | `Cognitive/AttentionSpotlight.vb:100` | `_globalHandler` (lambda) | `RecordActivity("GlobalStateMachine", e.Timestamp, e.NewState.ToString())` — pure payload | none | ✅ |

## Boundary notes (explicitly out of scope, risk recorded)

1. **Second-order subscribers**: `DSPThreadSSM` (subscribes to
   RecordingManagerSSM) and `MainForm` (subscribes to UIStateMachine) receive
   *SSM* events, not GSM events. The SSMs still deliver their own events
   under their own locks (SSM-internal semantics are out of feature-002
   scope). The payload-authority contract applies transitively and both were
   spot-checked as payload-consumers, but SSM delivery mechanics remain
   pre-feature.
2. **R1-RESIDUAL discipline** (research.md): a caller must NOT hold a lock
   that a subscriber handler acquires while requesting a transition from
   another thread — the same discipline the pre-feature design required.
   Feature 002 cured the broader class (machine lock held during delivery);
   this narrower discipline is inherent to synchronous truthful outcomes.
   No current production caller violates it (MainForm user-action paths hold
   no subscriber locks; SSM cascades are same-thread and defer).
