# Feature Specification: Tap Monitoring Consolidation & Audio Core Test Foundation

**Feature Branch**: `003-tap-consolidation`

**Created**: 2026-07-12

**Status**: Draft

**Input**: User description: "Consolidate DSP tap-point monitoring onto a single API and establish a test foundation for the audio core. Today there are two parallel ways to read monitor audio (four deprecated per-tap read/available method pairs, plus the TapLocation reader API) and consumers use both; there must be exactly one. The multi-reader monitor buffer currently fails silently when a slow reader is lapped by the writer — readers must be able to detect overrun/data loss instead of receiving corrupted audio undetected. Finally, the project has zero automated tests; the audio core and state machine are pure logic and must be provably correct before the upcoming sample-format migration: ring buffer write/read/wraparound behavior, multi-reader independence and overrun detection, float-to-PCM16 conversion round-trip accuracy, gain/pan math correctness, and exhaustive validation of the global state machine transition matrix."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Provably Correct Audio Core (Priority: P1)

As the developer, I can run one command and get a pass/fail verdict on every
piece of pure logic the audio path depends on — buffer behavior, sample-format
conversion accuracy, gain/pan math, and the complete state-transition rule set —
so that the upcoming sample-format migration (and any future change) starts
from locked-in, verified behavior instead of hope.

**Why this priority**: Every other change in this feature (and the entire
float32 migration after it) is only safe if current behavior is captured first.
The test suite delivers standalone value even if nothing else in this feature
ships: it converts "believed correct" into "demonstrated correct" for the
hardest-to-debug components in the system.

**Independent Test**: Run the test suite on the current, unmodified codebase.
It passes, completes quickly, and requires no audio hardware. Deliberately
introducing a known defect (e.g., an off-by-one in buffer wraparound) causes a
test failure that names the broken behavior.

**Acceptance Scenarios**:

1. **Given** the current codebase, **When** the developer runs the test suite
   with a single command on a machine with no audio devices, **Then** all tests
   execute and pass without user interaction.
2. **Given** the state machine's transition rules, **When** the suite runs,
   **Then** every possible from-state/to-state pair has been exercised, valid
   transitions are confirmed to change state, and invalid transitions are
   confirmed to be rejected with the state left unchanged.
3. **Given** a full-amplitude audio signal, **When** it is converted from
   floating-point to 16-bit integer form and analyzed, **Then** the conversion
   is accurate within 1 least-significant bit across the full amplitude range,
   including exact full-scale and clipped inputs.
4. **Given** the gain processor at unity gain, center pan, and normal width,
   **When** audio passes through it, **Then** the output is bit-identical to
   the input.

---

### User Story 2 - Detected, Never Silent, Data Loss (Priority: P2)

As a monitoring consumer (level meter, spectrum display, or any future
instrument), when I fall behind and the producer overwrites audio I have not
yet read, I am told that data was lost and how much, and I can resynchronize
and continue — instead of unknowingly receiving a corrupted mixture of old and
new audio that looks like a processing bug.

**Why this priority**: This is a live correctness bug today — a stalled reader
silently receives torn audio, which surfaces as inexplicable meter glitches or
spectral garbage and burns debugging time on phantom DSP problems. It changes
observable behavior, so it must land after the safety net (US1) exists.

**Independent Test**: Create a reader, write more data than the buffer holds
without servicing the reader, then read. The read reports data loss (with the
amount) instead of returning audio, and a subsequent read after
resynchronization returns only intact, contiguous audio.

**Acceptance Scenarios**:

1. **Given** a reader that has not consumed data while the writer wrote more
   than one full buffer of new audio, **When** the reader next interacts with
   the buffer, **Then** it is informed that data was lost and by how much.
2. **Given** a reader that was informed of data loss, **When** it
   resynchronizes, **Then** subsequent reads return only intact audio written
   after the resynchronization point.
3. **Given** two readers on the same tap where one is serviced regularly and
   one has stalled, **When** the stalled reader is lapped, **Then** the healthy
   reader's data stream is unaffected and contains no loss events.
4. **Given** any sequence of writes and reads, **When** a read returns audio,
   **Then** that audio is never a splice of data from two different points in
   time without an intervening loss notification.
5. **Given** a permanently stalled reader, **When** overruns occur repeatedly,
   **Then** loss events appear in the diagnostic log at a bounded rate (no log
   flooding), and real-time audio processing is never delayed by the reporting.

---

### User Story 3 - One Way to Monitor (Priority: P3)

As the developer, there is exactly one mechanism for reading monitor audio
from any tap point in the signal chain. Every consumer — meters, spectrum
displays, future instruments — uses it, the retired duplicate mechanism is
gone, and the documentation states what each tap point carries.

**Why this priority**: Two parallel APIs doing the same job is pure defect
surface — fixes and improvements (like US2's overrun detection) must be made
twice or silently diverge. It is P3 because removing the duplicate is safe
only after consumers are migrated and the safety net (US1) plus the improved
behavior (US2) are in place.

**Independent Test**: An API-surface audit shows a single read mechanism and
zero deprecated monitor-access methods; the application's meters and spectrum
displays behave identically to before the migration.

**Acceptance Scenarios**:

1. **Given** the completed feature, **When** the public API surface is
   audited, **Then** exactly one mechanism exists for reading monitor audio
   and the four legacy per-tap read/available method pairs no longer exist.
2. **Given** the application running with microphone armed or a file playing,
   **When** the user watches input/output meters and spectrum displays,
   **Then** they update exactly as they did before the consolidation.
3. **Given** a developer adding a new monitoring instrument, **When** they
   consult the documentation, **Then** each tap point's position in the signal
   flow is stated, including which taps currently carry identical content and
   under what future conditions they will diverge.

---

### Edge Cases

- A reader is created while the writer is mid-stream: it must start from the
  current write position (no phantom backlog, no immediate false overrun).
- A reader is removed while a write is in progress: the write completes
  unaffected and no other reader is disturbed.
- An overrun lands exactly on the buffer wrap boundary; detection must not
  depend on position aliasing (writer lapping a reader by exactly one or
  multiple full buffer lengths must still be detected).
- The writer laps a stalled reader multiple times between that reader's reads:
  the reported loss reflects the total, not just the most recent lap.
- A read requests more data than is available: the reader receives what exists
  with a correct count, never padding or stale bytes.
- Sample-format conversion at exact full scale (±1.0) and beyond (over-range
  input must clip predictably, not wrap or overflow).
- Gain at mute (zero) and at maximum boost with full-scale input: output must
  clamp to the valid sample range, never wrap.
- Hard-left/hard-right pan and stereo-width extremes (mono-collapse and
  maximum width): output stays within valid range and preserves the
  constant-power level relationship.
- State machine: a rejected transition must leave the current state unchanged;
  a same-state transition is a harmless no-op; recovery from the error state
  is possible only to the designated recovery state.
- Tests must run headless: no audio hardware, no UI message pump, no timing
  dependence that makes them flaky on slow machines.

## Requirements *(mandatory)*

### Functional Requirements

**Monitoring consolidation**

- **FR-001**: The system MUST expose exactly one mechanism for reading monitor
  audio from a tap point. The four legacy per-tap read/available method pairs
  MUST be removed.
- **FR-002**: All existing monitoring consumers MUST be migrated to the single
  mechanism before the legacy mechanism is removed, with no observable change
  in meter or spectrum behavior.
- **FR-003**: Each tap point's position in the signal flow MUST be documented,
  including an explicit statement of which taps currently carry identical
  content and when they are expected to diverge.

**Overrun detection**

- **FR-004**: Each monitor reader MUST maintain an independent read position;
  reading through one reader MUST NOT affect any other reader.
- **FR-005**: When the writer overwrites data a reader has not yet consumed,
  that reader's next interaction with the buffer MUST report that data was
  lost and the amount lost. "Report" means recorded at the reader and
  queryable via its statistics (ReaderStats) — consumers migrated on the
  default read signature are conformant. The no-splice guarantee (FR-007) is
  unchanged by this definition. *(Amended per analysis F1, 2026-07-12.)*
- **FR-006**: A reader that has been notified of data loss MUST be able to
  resynchronize to the oldest still-valid data and continue reading.
- **FR-007**: A read MUST never return audio spanning lost data without a loss
  notification; silently spliced old/new audio is prohibited in all cases,
  including laps of exactly one or multiple full buffer lengths.
- **FR-008**: Overrun events MUST be observable in the diagnostic log,
  rate-limited so a permanently stalled reader cannot flood the log, and
  reported without adding work to the real-time audio path.

**Test foundation**

- **FR-009**: An automated test suite MUST exist, runnable to completion with
  a single command, headless, with no audio hardware and no user interaction.
- **FR-010**: The suite MUST verify single-reader buffer behavior: write/read
  round-trip integrity, wraparound correctness, available/free-space
  accounting, and full/empty boundary conditions.
- **FR-011**: The suite MUST verify multi-reader behavior: reader
  independence, per-reader positions, and every overrun-detection path
  (including exact-wrap and multi-lap cases).
- **FR-012**: The suite MUST verify sample-format conversion (floating-point
  to 16-bit integer): round-trip accuracy within 1 least-significant bit,
  correct clipping at and beyond full scale, and exact silence preservation.
- **FR-013**: The suite MUST verify gain processing math: unity-settings
  bypass produces bit-identical output, output clamping at extremes, the mute
  floor, constant-power pan level preservation, and both mono and stereo
  paths.
- **FR-014**: The suite MUST verify the global state machine exhaustively:
  every from-state/to-state pair is exercised; valid transitions succeed and
  update state; invalid transitions are rejected and leave state unchanged;
  the error-recovery path works.
- **FR-015**: The behavior-locking tests (FR-010, FR-012, FR-013, FR-014)
  MUST pass against the current implementation before any consolidation or
  overrun-detection change is merged, so that regressions introduced by this
  feature are distinguishable from pre-existing behavior.

### Key Entities

- **Tap Point**: A named location in the audio signal chain (before
  processing, after input gain, after processing, before output) from which
  monitor audio can be read without disturbing the main signal path.
- **Monitor Reader**: A named, independent cursor over a tap point's audio
  stream, owned by one consumer (meter, spectrum display, recorder). Carries
  its own position and its own data-loss state.
- **Overrun Event**: The fact that a specific reader lost a specific amount of
  audio because the producer overwrote it before it was read. Belongs to
  exactly one reader; visible to that reader and to the diagnostic log.
- **Transition Matrix**: The complete rule set of which global-state changes
  are legal. Finite and enumerable: every from/to pair has a defined
  expected outcome.
- **Test Suite**: The automated, headless collection of checks covering the
  entities above plus sample-format conversion and gain math.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An API-surface audit finds exactly one mechanism for reading
  monitor audio and zero legacy monitor-access methods.
- **SC-002**: In a stress scenario where a consumer stalls and resumes
  repeatedly while audio flows, 100% of data-loss events are reported to that
  consumer, and zero reads return spliced/corrupted audio.
- **SC-003**: The full test suite completes headless in under 30 seconds and
  passes on the current hardware-free development machine.
- **SC-004**: 100% of the global state transition matrix (every from/to pair)
  is exercised with its expected outcome verified.
- **SC-005**: Sample-format conversion accuracy is demonstrated to within
  1 least-significant bit across the full amplitude range, including
  full-scale and over-range inputs.
- **SC-006**: After migration, existing meters and spectrum displays are
  visually indistinguishable from their pre-migration behavior during
  recording and playback.
- **SC-007**: Every code area the upcoming sample-format migration will touch
  (buffers, conversion, gain math) has locked-in behavioral tests before that
  migration begins.

## Assumptions

- **Overrun response model**: report-and-resynchronize was chosen (the reader
  learns of the loss, then jumps forward to the oldest valid data). Monitoring
  consumers such as meters and spectrum displays prefer fresh data over
  historical completeness; no consumer needs gap-free history.
- **Redundant taps are documented, not removed**: two tap points currently
  carry essentially identical content. They are retained because future
  processors inserted between them will make them diverge; the documentation
  requirement (FR-003) makes the current equivalence explicit instead of
  surprising.
- **Legacy mechanism has no external consumers**: this is a single-developer
  application with no published API, so the legacy methods can be removed
  outright rather than deprecated over time.
- **Test scope is pure logic only**: audio device I/O, file I/O, UI, and
  hardware-dependent threading are out of scope for this suite. The
  components under test are deterministic and hardware-independent by design.
- **Sequencing**: behavior-locking tests (US1) land first, then overrun
  detection (US2), then consolidation and legacy removal (US3). FR-015 makes
  this ordering a requirement, not a preference.
- **Dependency**: this feature is a prerequisite for the planned sample-format
  migration (feature 001-float32-pipeline, named in the project constitution);
  that migration must not begin until SC-007 is met.
