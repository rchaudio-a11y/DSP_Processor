# Phase 0 Research: State Machine Hardening & Coordinator Lifecycle

**Date**: 2026-07-12 | **Plan**: [plan.md](plan.md)

Codebase facts verified at v1.3.3.3 (`5f08494`).

## R1. Core design: single-drainer with commit-through-delivery windows

- **Decision** *(amended 2026-07-13, analysis F1)*: define "transition in
  progress" as the window from commit through the completed delivery of that
  transition's event. One thread — the one that starts a transition while the
  machine is idle — becomes the **drainer**.
  - **Same-thread re-entrancy** (a request issued from inside a handler the
    drainer is currently invoking): appended to a FIFO **deferral queue**,
    caller gets `Deferred`. This is the ONLY path that produces `Deferred`.
  - **Cross-thread callers** arriving mid-window: **block until the window
    closes** (Monitor wait on the lock, loop-checked against the
    window-open flag), then proceed normally — the woken caller becomes the
    next drainer and receives `Performed`/`Rejected`.
  - The drainer loop: commit under `_stateLock` → build event args → release
    lock → deliver event (all subscribers, in order) → re-acquire lock →
    dequeue next deferred request → validate against *now-current* state →
    repeat until the queue is empty → clear window flag → pulse waiters.
- **F1 amendment rationale**: cross-thread blocking preserves today's
  semantics EXACTLY — under the current code, cross-thread callers already
  wait on `SyncLock` through the entire transition *including delivery*
  (the lock is held throughout), then perform. Deferring them instead would
  be a user-visible regression: `MainForm.vb:988` raises a **modal error
  dialog** when `TransitionTo` returns False, so a user click landing during
  an SSM cascade's delivery would show an error while the transition
  executed anyway. Blocking is safe because delivery is non-blocking per
  Constitution V — `UIStateMachine` posts via `BeginInvoke` and returns, so
  the window always closes without needing the blocked thread.
- **Rationale**:
  1. Preserves today's cascade semantics: a handler-triggered transition
     executes *after* the current transition completes (the current queue does
     this too) — minimizing behavioral drift for the SSM cascade paths.
  2. Gives `Deferred` real, testable meaning (spec FR-002/FR-013b).
  3. Yields strict linear event order for free: one drainer delivers all
     events in commit order; transition IDs (assigned at commit under the
     lock) remain sequential in delivery order (State Registry intact).
  4. Kills the deadlock: the GSM lock is **never held while any foreign code
     runs**; a subscriber holding its own lock can never form a cycle with
     `_stateLock` because `_stateLock` is only ever held around pure
     bookkeeping.
- **Alternatives considered**: immediate nested execution of handler-triggered
  transitions (rejected: changes cascade timing observed by every SSM, and
  produces nested/interleaved event delivery); background dispatcher thread
  (rejected: spec assumption keeps delivery synchronous in-process; adds a
  thread + marshaling for no requirement).

## R2. Outcome API: `RequestTransition` + `TransitionOutcome`; Boolean surface stays

- **Decision**:
  - `Public Enum TransitionOutcome : Performed / Deferred / Rejected`
  - `Public Function RequestTransition(newState, reason) As TransitionOutcome`
    — the new truthful surface.
  - `Public Function TransitionTo(newState, reason) As Boolean` (interface
    member, retained) = `RequestTransition(...) = TransitionOutcome.Performed`.
    True means performed; False means *not performed* (rejected or deferred).
  - `RequestTransition` lives on `GlobalStateMachine` only — NOT on
    `IStateMachine(Of T)` (SSMs untouched, see plan Complexity Tracking).
- **Rationale**: FR-003/FR-014 forbid touching the matrix tests, which assert
  Boolean True/False for direct performs/rejects — no deferral occurs in those
  paths, so their assertions hold byte-for-byte. Today's lie (deferred→True)
  becomes deferred→False on the Boolean surface plus a precise answer on the
  new surface. Production Boolean callers (MainForm at :986/:1146/:1178/:1193/
  :1215/:1423) never call from inside handlers, and per the F1 amendment
  cross-thread callers block-then-perform rather than defer — so **no
  production Boolean caller can ever observe Deferred by construction**
  (`MainForm.vb:988` raises a modal error dialog on False, which is exactly
  why the pre-amendment design was rejected; analysis F1). Deferred→False
  remains the truthful mapping for the only callers who can see it:
  in-handler cascade code.
- **Alternatives considered**: changing `TransitionTo` to return the enum
  (rejected: breaks interface + all SSM implementations + matrix tests);
  Boolean deferred→True with post-hoc validation (rejected: that is exactly
  the current lie).

## R3. Event delivery mechanics: ordered queue + per-subscriber containment

- **Decision**: events are appended (with their commit-assigned transition
  IDs) to an internal ordered delivery list consumed only by the drainer,
  outside the lock. Delivery enumerates the event's invocation list
  (`StateChangedEvent?.GetInvocationList()` — VB's compiler-generated hidden
  field for a declared event) and invokes each handler inside its own
  `Try/Catch`; a handler exception is logged (`Logger.Error`, context
  `GlobalStateMachine`) and delivery continues with the next subscriber
  (FR-008).
- **Rationale**: plain `RaiseEvent` aborts remaining handlers on the first
  exception — insufficient for FR-008. Enumerating the invocation list is the
  standard .NET pattern for containment and costs one array allocation per
  transition (UI-rate, not audio-rate — acceptable).
- **Alternatives considered**: `RaiseEvent` inside Try/Catch (rejected: whole-
  delivery abort, violates FR-008); AggregateException rethrow (rejected: an
  event handler failure must never look like a transition failure).

## R4. Rejection visibility & history semantics

- **Decision**: rejected requests (direct AND deferred-then-rejected) are
  logged in the existing State-Registry rejection format
  (`Invalid transition rejected: X -> Y (Reason: ...)`), with deferred
  executions additionally marked (`... (deferred; originally requested during
  GSM_Txx)`). The transition **history keeps its current success-only
  semantics** — this is the "record" of FR-004's "history/record" phrasing.
- **Rationale**: the history feeds the State Debugger panel and
  `GetTransitionHistory` consumers; injecting rejection entries would change
  what "history count" means to them and drift v1.3.3.3-locked expectations.
  The grep-friendly log already carries rejections (State Registry rejection
  tracking); extending its format string is additive.
- **Alternatives considered**: history entries with a rejected flag (rejected:
  silently changes `GetTransitionHistory` semantics for existing consumers);
  separate rejection history (rejected: YAGNI — the log serves the need).

## R5. Test strategy for "outside the lock" and ordering (FR-013/SC-003/SC-004)

- **Decision** (all headless, deterministic, no sleeps in assertions):
  1. **Out-of-lock probe**: a subscriber handler spawns a thread that calls
     `GetTransitionHistory()` (which acquires `_stateLock`) and `Join`s it
     with a generous timeout (5 s). If delivery held the lock, the probe
     blocks and the join times out → named failure.
  2. **Deadlock reproduction**: subscriber takes its own lock in-handler
     while a second thread, holding that same lock, calls
     `RequestTransition` — the pre-fix AB-BA scenario. Bounded-time success
     proves the cycle is gone.
  3. **Losslessness**: a handler defers 3 further requests during one
     transition; assert all 3 execute in arrival order (event sequence) and
     outcomes were `Deferred` at request time.
  4. **Truthfulness**: direct valid → `Performed`/True; direct invalid →
     `Rejected`/False; in-window request → `Deferred`/False with eventual
     execution visible.
  5. **Deferred-then-invalid**: handler defers a request that is invalid from
     the state reached by an earlier deferred request; assert `Rejected` at
     execution, state unchanged by it, rejection logged.
  6. **Ordering stress**: ≥100 transitions (valid cycle Idle→Playing→Stopping
     →Idle…) with a cascading subscriber; assert observed event payload
     sequence exactly equals commit sequence (transition IDs strictly
     ascending, no gaps/reorders).
  7. **Containment**: subscriber 1 throws; subscriber 2 still receives; state
     consistent.
- **Rationale**: each maps 1:1 to an SC; timeouts only bound failure, never
  gate success, so the suite stays non-flaky on slow machines (SC-007).

## R6. Coordinator surface removal — verified zero-migration

- **Facts** (grep-verified 2026-07-12):
  - **No production code calls `StateCoordinator.Dispose()`** — the only
    `Dispose` implementations/guards are internal to the class. FR-011's
    call-site migration set is EMPTY; removal is compile-safe.
  - To remove from `State/StateCoordinator.vb`: `Implements IDisposable`
    (class header), the `Dispose` method (:395-435, including the 50 ms
    shutdown-barrier sleep and the null-out of `_dspThreadSSM` — which was
    half of quick-win B1's NRE window), `CheckDisposed` (:440-444), the
    `_disposed` field, and ~27 `CheckDisposed()` guard calls (:64-361).
  - GSM scaffolding: `OnStateExiting` (fully empty) deleted;
    `OnStateEntering` deleted with its ONE real behavior — error-state entry
    logging (`GlobalStateMachine.vb:277`) — inlined into the commit path
    (`If newState = GlobalState.Error Then Logger.Error(...)`), preserving
    FR-012's observability requirement.
  - `MonitoringController.CheckDisposed` (Managers) is a DIFFERENT class —
    untouched, out of scope.
- **Alternatives considered**: keep Dispose but make it a no-op (rejected:
  Architect ruling says the surface itself is the defect); revive-after-
  dispose support (rejected: process-lifetime ruling makes it moot).

## R7. Subscriber audit inventory (FR-009 scope, verified)

Direct subscribers to **GlobalStateMachine.StateChanged** (11):

| # | Subscriber | Handler | Layer |
|---|-----------|---------|-------|
| 1 | `UIStateMachine.vb:63` | `OnGlobalStateChanged` | State |
| 2 | `RecordingManagerSSM.vb:71` | `OnGlobalStateChanged` | State |
| 3 | `PlaybackSSM.vb:61` | `OnGlobalStateChanged` | State |
| 4 | `Cognitive/ConflictDetector.vb:59` | `OnGlobalStateChanged` | Cognitive |
| 5 | `Cognitive/PredictionEngine.vb:64` | `OnStateChanged` | Cognitive |
| 6 | `Cognitive/HabitLoopAnalyzer.vb:66` | `OnTransition` | Cognitive |
| 7 | `Cognitive/NarrativeGenerator.vb:69` | `OnGlobalStateChanged` | Cognitive |
| 8 | `Cognitive/AnomalyDetector.vb:60` | `OnStateChanged` | Cognitive |
| 9 | `Cognitive/CognitiveLayer.vb:511` | `OnGlobalStateChanged` | Cognitive |
| 10 | `Cognitive/AdaptiveThresholdManager.vb:58` | `OnStateChanged` | Cognitive |
| 11 | `Cognitive/AttentionSpotlight.vb:100` | `_globalHandler` | Cognitive |

- **Audit method**: inspect each handler (and anything it calls synchronously)
  for reads of `CurrentState` / `StateCoordinator.Instance.*State` / any live
  machine state; classify: `payload-only (clean)` / `live-read (fix)` /
  `live-read (justified — document why)`. Record per-subscriber finding +
  action in `Documentation/Architecture/GSM-Subscriber-Audit.md` (SC-005).
- **Second-order note**: `DSPThreadSSM` (subscribes to RecordingManagerSSM)
  and `MainForm` (subscribes to UIStateMachine) receive *SSM* events — SSM
  internal delivery is out of scope (spec assumption), but the audit document
  must note this boundary explicitly so the deferred risk stays visible.
