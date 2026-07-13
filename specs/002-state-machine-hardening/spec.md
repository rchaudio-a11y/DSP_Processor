# Feature Specification: State Machine Hardening & Coordinator Lifecycle

**Feature Branch**: `002-state-machine-hardening`

**Created**: 2026-07-12

**Status**: Draft

**Input**: User description: "Harden the global state machine's transition semantics and coordinator lifecycle. Three defects: (1) Transitions requested while a transition is in progress are held in a single pending slot — a second request silently overwrites the first, and deferred callers are told 'success' before their transition is validated, so callers can be misinformed about transitions that never occurred. Deferred transitions must be held losslessly in arrival order, and every caller must receive a truthful outcome: performed, deferred, or rejected. (2) State-change events are delivered while the machine's internal lock is held; subscribers acquire their own locks in response, creating cross-object lock-ordering deadlock risk. Events must be delivered outside the machine's critical section with strict transition-order preservation, and all existing subscribers must be audited to act only on the event payload (old state, new state, reason) — never by re-reading live machine state, which may have advanced by delivery time. (3) The state coordinator is a process-lifetime singleton that also implements disposal — once disposed it is permanently unusable while still globally reachable. Architect ruling: coordinator lifecycle is process-lifetime; disposal responsibility is removed from the coordinator; empty state entry/exit scaffolding is deleted. Existing transition-matrix tests (26 valid / 38 invalid) must pass unchanged; new tests must prove queue losslessness, truthful caller outcomes, and event delivery ordering."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Truthful, Lossless Transition Requests (Priority: P1)

As any component requesting a global state transition, I receive a truthful
answer about what happened to my request — it was performed, it was deferred
for execution after the in-progress transition completes, or it was rejected —
and no request is ever silently discarded, regardless of how many arrive while
a transition is in progress.

**Why this priority**: This is an active correctness defect in the system's
single source of truth. Today a second deferred request silently destroys the
first, and deferred callers are told "success" before validation — components
proceed on the belief that a transition happened when it never did. Every
other state-dependent behavior in the application sits downstream of this
contract.

**Independent Test**: From within a state-change notification (the deferral
window), issue several further transition requests; verify all are held in
arrival order, each is validated against the state current at its execution,
every request's fate is observable, and none is lost.

**Acceptance Scenarios**:

1. **Given** a transition is in progress, **When** two different transitions
   are requested during it, **Then** both are held and later executed in
   arrival order — neither is discarded.
2. **Given** a request was deferred, **When** the caller inspects the returned
   outcome, **Then** it says "deferred" — not "performed" — and the request's
   eventual execution result (performed or rejected) is observable in the
   transition history and log.
3. **Given** a deferred request whose transition is invalid from the state the
   machine reaches by the time it executes, **When** it is executed, **Then**
   it is rejected exactly as a direct invalid request would be, the rejection
   is logged, and the machine's state is unchanged by it.
4. **Given** a caller using the existing two-valued (true/false) entry point,
   **When** its transition is performed, it receives true; **When** its
   transition is rejected, it receives false; **Then** true is never returned
   for a transition that was not performed.
5. **Given** the existing 64-pair transition matrix test suite (26 valid /
   38 invalid), **When** this feature is complete, **Then** that suite passes
   without any modification to its assertions.

---

### User Story 2 - Deadlock-Free, Ordered Event Delivery (Priority: P2)

As a state-change subscriber (satellite state machines, UI state machine,
coordinator, cognitive layer), I receive state-change notifications outside
the state machine's critical section, in exactly the order the transitions
were committed, and I can safely take my own locks — or even request another
transition — without any possibility of a cross-object lock-ordering deadlock.

**Why this priority**: Delivering events while holding the machine's lock is
a structural deadlock trap: every subscriber takes its own lock in response,
so any subscriber path that ever calls back on a different thread closes the
loop. It also caused the pending-slot machinery that US1 repairs — fixing
delivery removes the disease rather than treating the symptom.

**Independent Test**: A test subscriber that (a) asserts the machine's lock
is not held during delivery, (b) requests a further transition from inside
the handler, and (c) records the sequence of (old, new) payloads across a
burst of transitions — verifying no deadlock, no re-entrancy failure, and
strict commit-order delivery.

**Acceptance Scenarios**:

1. **Given** a subscriber that acquires its own lock and requests a further
   transition inside its handler, **When** a transition fires, **Then** the
   cascade completes without deadlock and without corrupting state.
2. **Given** a rapid sequence of committed transitions, **When** subscribers
   receive events, **Then** the event payloads arrive in exactly the commit
   order — an event for a later transition is never delivered before an
   event for an earlier one.
3. **Given** event delivery happens after the machine's lock is released,
   **When** a subscriber reads the machine's live current state inside its
   handler, **Then** that state may already have advanced past the event's
   new-state — therefore **every existing subscriber is audited** and, where
   needed, corrected to act only on the event payload (old state, new state,
   reason), never on re-read live state.
4. **Given** one subscriber throws an exception during delivery, **When** the
   event fires, **Then** remaining subscribers still receive the event and
   the machine's state remains consistent.

---

### User Story 3 - Process-Lifetime Coordinator (Priority: P3)

As the application, the state coordinator exists for the life of the process:
it can never be put into a permanently broken "disposed" state while still
being globally reachable, and it carries no disposal surface that invites
that misuse. Subsystem teardown remains the responsibility of the subsystems'
owners.

**Why this priority**: Architect ruling (2026-07-12): a globally reachable
singleton that can be killed once and never revived is a lifecycle
contradiction. It is P3 because the defect is latent (only bites on a
dispose-then-use sequence) and the fix is largely removal.

**Independent Test**: The coordinator type exposes no disposal operation
(compile-level check); every call site that previously disposed it is
migrated; the application still shuts down cleanly with subsystems disposed
by their owners.

**Acceptance Scenarios**:

1. **Given** the completed feature, **When** the coordinator's public surface
   is inspected, **Then** it exposes no disposal operation and no
   disposed-state guards.
2. **Given** application shutdown, **When** the process exits, **Then**
   subsystems (recording manager, DSP thread, audio router) are still torn
   down by their existing owners, and no shutdown path attempts to dispose
   the coordinator.
3. **Given** the global state machine's empty per-state entry/exit
   scaffolding is deleted, **When** the system enters the error state,
   **Then** the error-state logging that currently lives inside that
   scaffolding is preserved (the one real behavior is retained; only the
   empty shell is removed).

---

### Edge Cases

- Multiple requests deferred during one transition: executed strictly in
  arrival order, each validated against the state current at its own
  execution — an earlier deferred transition may change state such that a
  later one becomes invalid; the later one is then rejected, visibly.
- A deferred transition triggers further deferrals when executed (cascade):
  processing continues in order until the queue is empty; a runaway cascade
  (queue depth beyond a sane threshold) is surfaced loudly as a defect
  diagnostic rather than silently absorbed.
- Same-state (no-op) transition requested during a transition: deferred and
  executed like any other; still a valid no-op.
- Two threads request transitions concurrently (no re-entrancy): both are
  serialized by the machine; each receives a truthful outcome; both events
  are delivered in commit order.
- Subscriber requests a transition from inside its handler after the lock is
  released: executes as a normal request (performed or rejected), and its
  event is delivered after the event that triggered it — never interleaved
  ahead of it.
- Subscriber throws during delivery: contained; remaining subscribers
  notified; transition already committed (an event handler failure can never
  un-commit a transition).
- Transition history and rejection logging remain grep-consistent with the
  State Registry pattern (transition IDs remain sequential per commit order).
- Shutdown while a deferred queue is non-empty: process-lifetime coordinator
  means no coordinator disposal path exists to race with; queue drain
  completes on the thread that owns it.

## Requirements *(mandatory)*

### Functional Requirements

**Truthful, lossless deferral (US1)**

- **FR-001**: Transition requests arriving while a transition is in progress
  MUST be held losslessly in arrival order; holding a second request MUST NOT
  discard or overwrite a previously held one.
- **FR-002**: Every transition request MUST yield exactly one truthful
  outcome to its caller: performed, deferred, or rejected. A caller MUST
  never be told a transition succeeded when it was not performed.
- **FR-003**: The existing two-valued entry point MUST be retained with
  truthful semantics: true only for a performed transition, false for a
  rejected one. The existing 64-pair transition matrix tests MUST pass
  without modification.
- **FR-004**: Deferred requests MUST be validated at execution time against
  the then-current state, exactly as direct requests are; rejected deferred
  requests MUST be logged and visible in the transition history/record.
- **FR-005**: Queue processing MUST drain in FIFO order until empty,
  including deferrals generated during the drain; queue depth beyond a
  defined diagnostic threshold MUST produce a loud log diagnostic (runaway
  cascade indicator), never silent truncation.

**Event delivery (US2)**

- **FR-006**: State-change events MUST be delivered outside the state
  machine's critical section — at no point during subscriber execution may
  the machine's internal lock be held by the delivering thread.
- **FR-007**: Events MUST be delivered in exactly the order their transitions
  were committed, with no interleaving or reordering, including across
  cascaded and concurrent transitions.
- **FR-008**: A subscriber exception during delivery MUST NOT prevent other
  subscribers from receiving the event, MUST NOT corrupt machine state, and
  MUST be logged.
- **FR-009**: Every existing subscriber to the global state machine's
  state-change event MUST be audited for reads of live machine state from
  inside its handler; any such read MUST be replaced with use of the event
  payload (old state, new state, reason, transition ID). The audit result
  (subscriber list, finding, action) MUST be recorded in the feature
  documentation.

**Coordinator lifecycle (US3)**

- **FR-010**: The state coordinator MUST be process-lifetime: its public
  surface MUST NOT offer a disposal operation, and no disposed-state
  checking may remain in its members.
- **FR-011**: All existing call sites that disposed the coordinator MUST be
  identified and migrated; subsystem teardown MUST remain with the
  subsystems' existing owners (no orphaned cleanup).
- **FR-012**: The global state machine's empty per-state entry/exit
  scaffolding MUST be deleted. The single real behavior currently inside it
  (error-state entry logging) MUST be preserved at equivalent observability.

**Verification (cross-cutting)**

- **FR-013**: New automated tests MUST prove: (a) no request loss under
  multi-request deferral, (b) truthful outcomes for performed / deferred /
  rejected paths, (c) event delivery outside the lock, and (d) strict
  commit-order event delivery — all headless, within the existing test
  harness (no file I/O).
- **FR-014**: The complete existing test suite (55 tests as of v1.3.3.3)
  MUST remain green with zero modifications to existing test assertions.

### Key Entities

- **Transition Request**: A demand to move the machine to a target state,
  with a reason. Has exactly one lifecycle: performed | deferred → (performed
  | rejected) | rejected.
- **Transition Outcome**: The truthful answer returned to a caller —
  performed, deferred, or rejected. The two-valued legacy surface maps
  performed→true, rejected→false, and can never claim success otherwise.
- **Deferral Queue**: Ordered, lossless holding area for requests that arrive
  during an in-progress transition. FIFO; drained to empty; depth-monitored.
- **Transition Event**: The notification payload (old state, new state,
  reason, transition ID) delivered to subscribers after commit, outside the
  critical section, in commit order. The payload is the authoritative record
  of *that* transition; live state may differ by delivery time.
- **Subscriber**: A component reacting to transition events. Post-audit
  contract: acts only on the payload.
- **Coordinator**: Process-lifetime owner of state-machine wiring. No
  disposal surface.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Under a scripted burst of transition requests including
  multiple deferrals within one transition window, 100% of requests are
  accounted for (performed + rejected + deferred-then-executed sums to the
  request count); zero silent losses.
- **SC-002**: The existing 64-pair transition matrix suite and the full
  v1.3.3.3 suite (55 tests) pass with zero assertion changes.
- **SC-003**: A subscriber that takes its own lock and requests a further
  transition inside its handler completes without deadlock in 100% of runs
  of the stress test.
- **SC-004**: Across a stress sequence of at least 100 committed transitions,
  events are observed in exact commit order (0 reordering) and the machine's
  lock is never observed held during any delivery.
- **SC-005**: The subscriber audit covers 100% of the global state machine's
  event subscribers, with each one's finding and action recorded.
- **SC-006**: The coordinator exposes no disposal operation (the removal is
  compile-enforced: previously-disposing call sites no longer compile until
  migrated), and application shutdown still tears down all subsystems.
- **SC-007**: Full suite (existing + new) remains headless and completes in
  under 30 seconds.

## Assumptions

- **Delivery mode**: event delivery remains synchronous and in-process on the
  transitioning thread (after lock release); introducing an asynchronous
  dispatcher is out of scope. Ordering is achieved by delivery discipline,
  not by a background pump.
- **Deferred-caller notification**: a deferred caller learns its request's
  final fate via the transition history and log, not via callback — no
  completion-callback machinery is added. This matches how existing callers
  actually use the machine (fire-and-observe).
- **Queue bound**: the deferral queue is logically unbounded; the diagnostic
  threshold (default assumption: depth > 16) only triggers a loud log, since
  a cascade that deep indicates a design defect, not a load condition.
- **Scope boundary**: satellite state machines' *internal* locking/queueing
  is out of scope except as touched by the subscriber audit (FR-009); the
  transition *matrix* (valid/invalid rules) is unchanged; tap/monitoring
  work is feature 003 (complete); sample-format migration is feature 001.
- **Test infrastructure**: reuses the v1.3.3.1 harness (MSTest,
  `Logger.SuppressForTesting`, headless).
- **Coordinator initialization semantics** (what `Initialize` does, when it
  runs) are unchanged — only the disposal surface and disposed-state guards
  are removed.
