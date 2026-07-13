# Phase 0 Research: Tap Monitoring Consolidation & Audio Core Test Foundation

**Date**: 2026-07-12 | **Plan**: [plan.md](plan.md)

All Technical Context unknowns are resolved below. Codebase facts were verified
directly against the source on this date (file:line references current as of
commit `6a4628a`, v1.3.2.5).

## R1. Test framework: MSTest (classic three-package layout)

- **Decision**: MSTest via `Microsoft.NET.Test.Sdk` + `MSTest.TestAdapter` +
  `MSTest.TestFramework` package references in an SDK-style `.vbproj` targeting
  `net10.0-windows` (must match the app project's TFM because the app assembly
  is WinForms-flavored). `<UseWindowsForms>true</UseWindowsForms>` in the test
  project so transitively-referenced types load without hassle.
- **Rationale**: User-directed (smoothest VB support on .NET 10). MSTest is
  first-party, fully supported for VB, and integrates with both `dotnet test`
  and VS Test Explorer. The classic package layout is chosen over the newer
  `MSTest.Sdk` project style because the SDK style's VB templates/tooling are
  less exercised, and this project favors boring-and-proven.
- **Alternatives considered**: xUnit (weaker VB ergonomics — no VB templates,
  analyzer friction); NUnit (fine, but no advantage here and MSTest was
  specified).

## R2. Overrun detection: monotonic total-bytes Long counters (ONE work item with the counter refactor)

- **Decision**: Replace Mod-based positions in `MultiReaderRingBuffer`
  (`Utils/MultiReaderRingBuffer.vb`) with monotonic counters:
  - Writer: `totalBytesWritten As Long` — never wraps, only grows.
  - Per reader: `totalBytesRead As Long` — never wraps, only grows.
  - Derived, all under the existing lock:
    - `pending = totalBytesWritten - reader.totalBytesRead`
    - **no overrun**: `pending <= capacity` → `Available = pending`
    - **overrun**: `pending > capacity` → `bytesLost = pending - capacity`;
      resync sets `reader.totalBytesRead = totalBytesWritten - capacity`
      (oldest still-valid byte), accumulates `reader.TotalBytesLost` and
      increments `reader.OverrunEvents`.
  - Physical array index for any monotonic position `p` is `p Mod capacity`
    (capacity remains the existing value; power-of-2 not required since Mod is
    computed under the lock, not in lock-free code).
- **Rationale**: User-directed design. A single representation change fixes
  both reported defects: (a) lap-aliasing is impossible because positions never
  wrap (a reader lapped by exactly N full buffers shows `pending = N*capacity +
  x`, unambiguously an overrun, where the old Mod arithmetic aliased it to
  `x`); (b) the loss amount falls out of the same subtraction for free.
  Long counters at audio byte rates (~176 KB/s) would take ~1.6 million years
  to overflow — no wrap handling needed.
- **Alternatives considered**: per-reader lap counters (more state, same
  information, two things to keep consistent); sequence-stamped blocks
  (overkill for a monitor path; changes the write format).

## R3. Read-path contract on overrun (consumer-facing behavior)

- **Decision**: dual surface, preserving the existing 4-arg signature:
  - `Read(readerName, buffer, offset, count)` — on detected overrun:
    resynchronizes the reader to the oldest valid data, records the loss
    (per-reader `OverrunEvents`/`TotalBytesLost`), then returns contiguous
    valid data. It can never return spliced audio because a read never spans
    the resync point.
  - New overload `Read(readerName, buffer, offset, count, ByRef bytesLost As
    Long)` — same behavior, reports the loss amount of *this* interaction at
    the call site (FR-005 "next interaction reports").
  - New query `GetReaderStats(readerName) As ReaderStats` (structure:
    `OverrunEvents As Integer`, `OverrunEpisodeCount As Integer`,
    `TotalBytesLost As Long`, `Pending As Long`, `LastOverrunLogEmitted As
    DateTime` — episode/log fields added per analysis E1 so FR-008
    rate-limiting is test-verifiable) for diagnostics panels and tests.
  - `Available(readerName)` — clamps to `capacity` and performs the same
    detect-and-resync so a stalled poller learns of loss on its usual path.
- **Rationale**: meters/FFT poll `Available` then `Read`; making both paths
  overrun-aware satisfies "next interaction reports" without breaking existing
  call shapes during migration. Auto-resync matches the spec's
  report-and-resynchronize model (Assumptions).
- **Alternatives considered**: sentinel return (-1) on `Read` (breaks every
  `bytesRead > 0` consumer pattern); exception on overrun (overrun is expected
  operational behavior for a stalled UI, not an error); manual-resync API
  (consumers would all write the same recovery boilerplate).

## R4. Logging seam for tests: Logger suppress mode (null-logger), not ILoggingService injection

- **Decision**: add a `Public Shared Property SuppressForTesting As Boolean`
  (default `False`) to `Utils.Logger`. When set *before first `Instance`
  access*: the constructor skips `EnsureLogDirectoryExists()`/`OpenLogFile()`
  and does not start the async writer thread; `Log(...)` drops entries (or
  routes to `Debug.WriteLine`). The test project sets it in an
  `<AssemblyInitialize>` method (`TestSetup.vb`) before any test runs.
- **Rationale**: verified in `Utils/Logger.vb:101-105` — the singleton
  constructor opens the log file immediately, so *any* code path that logs
  (GSM transitions, MultiReaderRingBuffer reader creation) would create files
  from unit tests. The user offered two options (ILoggingService seam or
  null-logger mode); null-logger wins because:
  1. `ILoggingService` (`Services/Interfaces/ILoggingService.vb`) has no
     context/category parameter — routing GSM through it would change the
     grep-friendly State Registry log format (`Logger.Info(msg,
     "GlobalStateMachine")`), which v1.3.2.5 just invested in fixing.
  2. Constructor-injecting a logger into `MultiReaderRingBuffer` and
     `GlobalStateMachine` churns every construction site for zero production
     benefit.
  3. The suppress switch changes zero production call sites and zero runtime
     behavior when unset.
- **Alternatives considered**: ILoggingService constructor injection (rejected
  per above); making Logger lazy-open on first write (viable but changes
  production timing/behavior; can be revisited independently).

## R5. Exposing float32→PCM16 conversion for tests: extract, don't InternalsVisibleTo

- **Decision**: extract `AudioRouter.ConvertFloatToPCM16`
  (`AudioIO/AudioRouter.vb:768`, currently `Private`) into a new
  `Public Module SampleConversion` in `Utils/` (`FloatToPcm16(floatBuffer As
  Byte(), byteCount As Integer, channels As Integer) As Byte()` plus an
  in-place overload for future zero-alloc use). AudioRouter delegates to it.
- **Rationale**: the conversion is pure math that AudioIO happens to host; the
  test suite (FR-012) and the future float32 pipeline (feature 001) both need
  it as a first-class, named unit. Extraction beats `InternalsVisibleTo`
  because every other class under test (`RingBuffer`,
  `MultiReaderRingBuffer`, `GainProcessor`, `GlobalStateMachine`) is already
  `Public` — the conversion function is the *only* inaccessible test target,
  so widening assembly internals for one function is the wrong trade.
- **Known duplicate, deliberately left**: `WasapiEngine.ConvertFloatToPCM16`
  (`AudioIO/WasapiEngine.vb:366`) is a channel-agnostic variant on the capture
  path. Consolidating it belongs to feature 001 (which rewrites conversion
  entirely); noted in data-model.md so it isn't lost.

## R6. Legacy API removal map (verified consumer inventory)

- **Decision**: delete these 8 `DSPThread` members after migration
  (`DSP/DSPThread.vb`): `ReadInputMonitor` (:218), `ReadOutputMonitor` (:234),
  `ReadPostGainMonitor` (:250), `ReadPostOutputGainMonitor` (:266),
  `InputMonitorAvailable` (:288), `OutputMonitorAvailable` (:299),
  `PostGainMonitorAvailable` (:310), `PostOutputGainMonitorAvailable` (:321).
- **Verified consumers to migrate** (grep-verified 2026-07-12; no others):
  | Consumer | Call sites | Tap | Proposed reader name |
  |----------|-----------|-----|----------------------|
  | `AudioRouter.InputSamples` property | :194, :199 | PreDSP | `"Router.InputSamples"` |
  | `AudioRouter.PostGainSamples` property | :221, :226 | PostGain | `"Router.PostGainSamples"` |
  | `AudioRouter.PostOutputGainSamples` property | :248, :253 | PostDSP | `"Router.PostOutputGainSamples"` |
  | `AudioRouter.OutputSamples` property | :275, :280 | PreOutput | `"Router.OutputSamples"` |
  | `AudioRouter.UpdateOutputSamples` | :907, :920 | PreOutput | `"Router.OutputEvents"` |
  | `AudioRouter.UpdateInputSamples` | :977, :990 | PreDSP | `"Router.InputEvents"` |
  | `RecordingManager` (post-gain path) | :167, :172 | PostGain | `"RecMgr.PostGain"` |
  | `RecordingManager` (post-output path) | :194, :199 | PostDSP | `"RecMgr.PostOutputGain"` |
- **Rationale**: the legacy methods internally create hidden `"_default_*"`
  readers on the same buffers, so migration is mechanical: create a named
  reader once (lazily, on first use, matching current behavior), call
  `ReadFromTap`/`TapAvailable`. The hidden `"_default_*"` readers disappear
  with the legacy methods.
- **Tap semantics to document (FR-003)**: `TapLocation` (`DSP/DSPThread.vb:365`)
  — PreDSP=raw input, PostGain=after input gain, PostDSP=after output gain
  (fed by processor callback), PreOutput=post-chain worker write. PostDSP and
  PreOutput currently carry near-identical content and will diverge when
  processors are inserted after the output-gain stage.

## R7. GSM exhaustive matrix test design

- **Decision**: encode the expected 8×8 matrix (64 pairs) as a table in the
  test (data-driven via `DataTestMethod`/`DataRow` or a loop over
  `[Enum].GetValues`), asserting for every pair: `IsValidTransition`
  correctness, `TransitionTo` return value, and post-call `CurrentState`
  (changed on accept, unchanged on reject). Fresh `GlobalStateMachine`
  instance per pair, driven to the required from-state via a shortest valid
  path helper (every state is reachable from Uninitialized in ≤ 4 hops).
- **Rationale**: `IsValidTransition` is pure and public; `TransitionTo` adds
  logging + events, both safe under Logger suppress mode. Driving to
  from-state through valid transitions (rather than reflection-poking the
  state field) tests the machine through its public contract only.
- **Expected matrix**: derived from `GlobalStateMachine.IsValidTransition`
  (`State/GlobalStateMachine.vb:154-208`) and recorded as the oracle table in
  [data-model.md](data-model.md#gsm-transition-matrix-oracle). Note the
  same-state rule (`fromState = toState` → always valid) and the double-coded
  Error→Idle recovery rule.

## R8. Test determinism constraints

- **Decision**: all tests single-threaded and synchronous; no `Thread.Sleep`,
  no timers, no reliance on the DSP worker thread. `MultiReaderRingBuffer` and
  `RingBuffer` are exercised by direct method calls (the classes do not spawn
  threads themselves — verified). GSM events are subscribed synchronously
  where event payloads are asserted.
- **Rationale**: FR-009/SC-003 (headless, < 30 s, not flaky). The threading
  *correctness* of these classes is a production concern handled by
  Interlocked/lock design (Constitution V), not something unit tests can prove;
  the suite proves *logic* (Constitution/RDF: validation loop scope).
