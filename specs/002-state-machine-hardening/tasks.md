# Tasks: State Machine Hardening & Coordinator Lifecycle

**Input**: Design documents from `/specs/002-state-machine-hardening/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/transition-api.md, quickstart.md

**Tests**: Test tasks are core scope — FR-013 mandates them and FR-014 makes the existing suite a hard behavior lock (the `GlobalStateMachineTests.vb` diff must be EMPTY at completion).

**Organization**: Tasks grouped by user story (US1 = P1 truthful lossless deferral, US2 = P2 event delivery + subscriber audit, US3 = P3 coordinator lifecycle).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

## Path Conventions

Repository-relative. Production changes concentrate in `DSP_Processor/State/` and `DSP_Processor/Cognitive/`; tests in `DSP_Processor.Tests/`.

**Structural note (analog of feature 003's sequencing note)**: the single-drainer restructure (research R1) implements US1's queue/outcomes AND US2's out-of-lock delivery in ONE rewrite of the same lock-critical method — splitting it would mean rewriting a critical section twice. It therefore lands in US1's phase; US2's phase *proves* the delivery guarantees and performs the subscriber audit. FR-012's scaffolding deletion also lands physically in that rewrite (the commit path is where the scaffolding is called), with its verification recorded in US3's phase.

---

## Phase 1: Setup (Baseline Gate)

**Purpose**: Freeze the pre-change baseline that FR-014 protects

- [X] T001 Run the full v1.3.3.3 suite and record the baseline (55/55 green, runtime) in `specs/002-state-machine-hardening/quickstart.md` Validation results; confirm working tree is clean so the SC-002 empty-diff check on `DSP_Processor.Tests/GlobalStateMachineTests.vb` is meaningful

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: None required — the v1.3.3.1 test harness (MSTest, `Logger.SuppressForTesting`) and the 64-pair matrix behavior lock already exist from feature 003.

*(No tasks.)*

---

## Phase 3: User Story 1 — Truthful, Lossless Transition Requests (Priority: P1) 🎯 MVP

**Goal**: Single-drainer restructure of `GlobalStateMachine`: FIFO deferral queue (lossless, late-validated), truthful `TransitionOutcome` via new `RequestTransition`, Boolean `TransitionTo` that never lies, out-of-lock ordered delivery with per-subscriber containment (mechanism only — proven in US2), scaffolding deleted with error-log preserved

**Independent Test**: from within a state-change handler (the deferral window), issue several further requests; all are held FIFO, validated at execution, every fate observable, none lost — while the untouched 64-pair matrix suite stays green

- [X] T002 [US1] Add US1 test group to new `DSP_Processor.Tests/StateMachineHardeningTests.vb` per data-model.md test map: `Deferral_ThreeRequestsInWindow_AllExecuteInOrder`, `Outcomes_Direct_Performed_Rejected_TruthfulOnBothSurfaces`, `Outcome_InWindow_DeferredThenExecuted`, `DeferredRequest_InvalidAtExecution_RejectedVisibly`, `QueueDepthDiagnostic_LoudWarningPast16` (17-deep cascade: assert nothing dropped, all 17 execute, AND `DepthWarningCount` incremented — the observable seam per analysis E1; log itself is suppressed). Also `CrossThread_CallerBlocksThenPerforms_NeverDeferred` — a second thread requesting mid-window receives Performed/Rejected, never Deferred (analysis F1). NOTE: compile-blocked until T003 exists — T002+T003 are one logical merge group (same TDD state as feature 003's T012/T013)
- [X] T003 [US1] Restructure `DSP_Processor/State/GlobalStateMachine.vb` per research R1/R2/R3/R4 and contracts/transition-api.md: add `TransitionOutcome` enum + `RequestTransition(newState, reason) As TransitionOutcome`; single-drainer loop (commit under `_stateLock` → deliver outside lock → drain FIFO `Queue(Of TransitionRequest)` with late validation); `Deferred` ONLY for same-thread in-handler re-entrancy — cross-thread callers arriving mid-window block on a Monitor wait (loop-checked window flag) until the window closes, then proceed as the next drainer (analysis F1: preserves today's SyncLock block-then-perform semantics; MainForm.vb:988 shows a modal error dialog on False, so deferring them would be user-visible); `Friend ReadOnly DepthWarningCount` observable seam for the depth diagnostic (analysis E1); `TransitionTo` = `RequestTransition(...) = Performed`; DELETE `_pendingTransition`/`_pendingReason` single slot and `_isTransitioning` re-entry guard; per-subscriber delivery via `StateChangedEvent?.GetInvocationList()` with per-handler Try/Catch + `Logger.Error` on throw; rejection log keeps State-Registry format with `(deferred; originally requested during GSM_Txx)` marker for deferred executions; depth>16 loud warning; DELETE `OnStateExiting`/`OnStateEntering` with error-state entry logging inlined into the commit path (FR-012); transition IDs still assigned at commit (strictly ascending in delivery order)
- [X] T004 [US1] Run full suite — new US1 tests green; `git diff DSP_Processor.Tests/GlobalStateMachineTests.vb` EMPTY (SC-002 gate); all pre-existing tests green with zero assertion changes (FR-014). Record in quickstart.md Validation results

**Checkpoint**: Truthful lossless deferral live; matrix behavior-lock held. Version v1.3.4.1 (SubPhase 3.4 per Architect granularity ruling)

---

## Phase 4: User Story 2 — Deadlock-Free, Ordered Event Delivery (Priority: P2)

**Goal**: Prove the delivery guarantees (out-of-lock, strict commit order, containment, no deadlock) and audit all 11 GSM subscribers to payload-only usage

**Independent Test**: a test subscriber asserts the lock is free during delivery, requests a transition in-handler, and records payload order across a 100-transition burst — no deadlock, no reorder

- [X] T005 [P] [US2] Add US2 test group to `DSP_Processor.Tests/StateMachineHardeningTests.vb` per research R5: `Delivery_LockNotHeld_CrossThreadProbe` (in-handler thread calls `GetTransitionHistory()` with bounded Join — timeout only bounds failure), `Deadlock_SubscriberOwnLock_CrossThreadTransition_Completes` (pre-fix AB-BA scenario, bounded-time success), `Ordering_100TransitionsWithCascade_StrictCommitOrder` (Idle→Playing→Stopping→Idle cycles + cascading subscriber — the cascade fires only on a designated transition (Playing→Stopping) and defers a request that returns the machine to a known state, keeping the single-threaded driver script deterministic per analysis C1; transition IDs strictly ascending, zero reorders), `Containment_ThrowingSubscriber_OthersStillNotified`
- [X] T006 [P] [US2] Audit the 3 state-layer subscribers for live-state reads inside handlers, per research R7 method — `DSP_Processor/State/UIStateMachine.vb` (`OnGlobalStateChanged`, :63), `DSP_Processor/State/RecordingManagerSSM.vb` (:71), `DSP_Processor/State/PlaybackSSM.vb` (:61); classify each (payload-only / live-read-fix / live-read-justified) and apply fixes to use only `args.OldState/NewState/Reason/TransitionID`
- [X] T007 [P] [US2] Audit the 8 cognitive-layer subscribers, same method — `DSP_Processor/Cognitive/ConflictDetector.vb` (:59), `PredictionEngine.vb` (:64), `HabitLoopAnalyzer.vb` (:66), `NarrativeGenerator.vb` (:69), `AnomalyDetector.vb` (:60), `CognitiveLayer.vb` (:511), `AdaptiveThresholdManager.vb` (:58), `AttentionSpotlight.vb` (:100); classify and fix
- [X] T008 [US2] Write `Documentation/Architecture/GSM-Subscriber-Audit.md` — one row per subscriber (finding + action + verified payload-only) from T006/T007 results, plus the explicit second-order boundary note (DSPThreadSSM←RecordingManagerSSM, MainForm←UIStateMachine, SSM-internal delivery out of scope — risk recorded) (FR-009, SC-005)
- [X] T009 [US2] Run full suite — US2 tests green alongside everything else; matrix diff still EMPTY. Record in quickstart.md

**Checkpoint**: Delivery guarantees proven; audit complete and recorded. Version v1.3.4.2

---

## Phase 5: User Story 3 — Process-Lifetime Coordinator (Priority: P3)

**Goal**: Coordinator loses its disposal surface entirely; scaffolding deletion (physically done in T003) verified and recorded

**Independent Test**: reflection shows `StateCoordinator` no longer implements `IDisposable`; grep shows zero disposal references; app shutdown still tears subsystems down via their owners

- [ ] T010 [US3] Remove the disposal surface from `DSP_Processor/State/StateCoordinator.vb` per research R6: `Implements IDisposable` (class header), `Dispose()` method (including the 50 ms barrier sleep and SSM null-outs), `CheckDisposed()` method, `_disposed` field, and all ~27 `CheckDisposed()` guard calls; leave `Initialize` and all property/query semantics otherwise untouched; do NOT touch `Managers/MonitoringController.vb` (different class, out of scope)
- [ ] T011 [US3] Add US3 test group to `DSP_Processor.Tests/StateMachineHardeningTests.vb`: `Coordinator_HasNoDisposalSurface` (assert `GetType(StateCoordinator).GetInterface("IDisposable") Is Nothing`) and `ErrorStateEntry_StillLogged_AfterScaffoldingDeletion` (drive a GSM instance to Error; assert transition performs and machine reaches Error — the FR-012 observability record; log content itself is suppressed in tests)
- [ ] T012 [US3] Verification pass — run the quickstart SC-006 grep (zero disposal references in production code); full suite green; matrix diff EMPTY; record results in quickstart.md. Manual shutdown smoke (launch app, close it, confirm clean exit with subsystem teardown by owners) flagged for the user if no GUI available

**Checkpoint**: Coordinator is process-lifetime. Version v1.3.4.3

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Constitution VIII bookkeeping and end-to-end validation record

- [ ] T013 [P] Per-story version bookkeeping in `DSP_Processor/Documentation/Active/Tasks.md` and `DSP_Processor/Documentation/Changelog/CURRENT.md` — one entry per story checkpoint on v1.3.4.x (SubPhase 3.4; tracker task = user story), commit + tag per Constitution VIII
- [ ] T014 Run complete quickstart.md validation end-to-end (build, full suite, SC-002 empty-diff check, SC-006 grep) and record results; list the manual app-level smoke items (record/playback cycle watching State Debugger + log; clean shutdown) for the user

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 first — establishes the FR-014 baseline
- **Foundational (Phase 2)**: none (harness exists from feature 003)
- **US1 (Phase 3)**: needs T001. T002+T003 are one merge group (tests compile-blocked on the enum/API); T004 gates the checkpoint
- **US2 (Phase 4)**: needs US1 complete (tests exercise the new delivery mechanics). T005 ∥ T006 ∥ T007 (different files); T008 needs T006+T007; T009 gates
- **US3 (Phase 5)**: T010 independent of US2 (different file) but sequenced after for checkpoint discipline; T011 needs T010; T012 gates
- **Polish (Phase 6)**: T013 rides each checkpoint; T014 last

### User Story Dependencies

Sequential by design (like feature 003): US1 builds the mechanism, US2 proves and audits it, US3 is checkpoint-ordered cleanup. US3's code (StateCoordinator) is actually file-independent of US1/US2 and could run in parallel after T001 if desired — the sequential order is checkpoint/version discipline, not a hard dependency.

### Parallel Opportunities

- T005 ∥ T006 ∥ T007 — the big one: delivery tests + both audit sweeps touch disjoint files
- T010 could start any time after T001 (file-independent) if checkpoint order is relaxed
- T013 ∥ T014 at the end

## Parallel Example: User Story 2

```text
# After T004 (US1 checkpoint), launch together:
Task: "US2 delivery-guarantee tests in StateMachineHardeningTests.vb (R5)"
Task: "Audit 3 state-layer subscribers (UIStateMachine, RecordingManagerSSM, PlaybackSSM)"
Task: "Audit 8 cognitive-layer subscribers (ConflictDetector ... AttentionSpotlight)"
```

## Implementation Strategy

**MVP = Phase 1 + US1 (T001–T004).** That alone fixes the active lying-to-callers defect and the silent overwrite, with the matrix suite proving no regression. Stop, validate, tag v1.3.4.1.

Then: US2 (prove delivery + audit — the deadlock-risk elimination becomes *demonstrated* rather than designed) → v1.3.4.2; US3 (surface removal) → v1.3.4.3. Commit at every checkpoint per Constitution VIII. The riskiest change (T003, a lock-critical rewrite) happens earliest, under the strongest safety net, with the least accumulated diff.
