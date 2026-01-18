# Master Task List v1.3.2.1
## State Machine Architecture Implementation Roadmap

**Date:** 2026-01-17  
**Version:** 1.3.2.1  
**Status:** ?? READY FOR IMPLEMENTATION  
**Purpose:** Master task list with design document references for complete implementation

---

## ?? **OVERVIEW**

This is the **master implementation roadmap** for DSP Processor v1.3.2.1 State Machine Architecture. Every task references its design specification, ensuring implementation matches architecture.

**Phase 1 (Design): ? COMPLETE (8/8 = 100%)**

**Remaining Phases:**
- **Phase 2:** State Machine Implementation (Steps 9-15) - 7 tasks
- **Phase 3:** Thread Safety Fixes (Steps 16-17) - 2 tasks
- **Phase 4:** Monitoring Implementation (Steps 18-20) - 3 tasks
- **Phase 5:** Integration & Wiring (Steps 21-24) - 4 tasks
- **Phase 6:** Testing & Validation (Steps 25-28) - 4 tasks
- **Phase 7:** Documentation (Steps 29-30) - 2 tasks

**Total Implementation Tasks:** 22

---

## ?? **PHASE 1: DESIGN (COMPLETE) ?**

| Step | Task | Design Doc | Status |
|------|------|------------|--------|
| 1 | Architecture Assessment | `Architecture-Assessment-v1_3_2_1.md` | ? Complete |
| 2 | State Machine Design | `State-Machine-Design.md` | ? Complete |
| 3 | Satellite State Machines | `Satellite-State-Machines.md` | ? Complete |
| 4 | State Coordinator Design | `State-Coordinator-Design.md` | ? Complete |
| 5 | Thread Safety Audit | `Thread-Safety-Audit.md` | ? Complete |
| 6 | Thread Safety Patterns | `Thread-Safety-Patterns.md` (v2.0.0) | ? Complete |
| 7 | MonitoringController Design | `MonitoringController-Design.md` (v2.0.0) | ? Complete |
| 8 | Reader Management Design | `Reader-Management-Design.md` (v1.0.0) | ? Complete |

**Phase 1 Deliverables:**
- ? 8 comprehensive design documents (~150 pages)
- ? Production-grade architecture specification
- ? Thread safety patterns (11 patterns)
- ? State machine architecture (1 GSM + 3 SSMs + 1 UI)

---

## ?? **PHASE 2: STATE MACHINE IMPLEMENTATION**

### **Step 9: Implement IStateMachine Interface** ?

**Design Reference:** `State-Machine-Design.md` (Part 3)

**Location:** `State\IStateMachine.vb` (new file)

**Tasks:**
- [ ] Create `IStateMachine(Of TState)` generic interface
- [ ] Define `CurrentState` property
- [ ] Define `TransitionTo(newState, reason)` method
- [ ] Define `IsValidTransition(fromState, toState)` method
- [ ] Define `StateChanged` event
- [ ] Add XML documentation
- [ ] Add to `State` namespace

**Acceptance Criteria:**
- Interface compiles without errors
- All methods/properties defined per spec
- Generic constraint allows enum states
- Event signature matches design

**Estimated Time:** 30 minutes

**Design Sections:**
- State-Machine-Design.md ? Part 3: IStateMachine Interface
- State-Machine-Design.md ? Part 4: Base Implementation

---

### **Step 10: Implement GlobalStateMachine** ?

**Design Reference:** `State-Machine-Design.md` (Part 5)

**Location:** `State\GlobalStateMachine.vb` (new file)

**Tasks:**
- [ ] Create `GlobalStateMachine` class implementing `IStateMachine(Of GlobalState)`
- [ ] Define `GlobalState` enum (Uninitialized, Idle, Arming, Armed, Recording, Stopping, Playing, Error)
- [ ] Implement transition validation rules per state diagram
- [ ] Implement state entry/exit actions
- [ ] Add thread-safe state transitions (SyncLock + Volatile)
- [ ] Implement `StateChanged` event firing
- [ ] Add transition history tracking
- [ ] Add XML documentation

**Acceptance Criteria:**
- All 8 states defined
- State diagram transitions enforced
- Thread-safe (Part 4 patterns applied)
- Invalid transitions rejected
- Events fire correctly
- Compiles and builds

**Estimated Time:** 2-3 hours

**Design Sections:**
- State-Machine-Design.md ? Part 5: Global State Machine
- State-Machine-Design.md ? Part 6: State Transitions
- Thread-Safety-Patterns.md ? Part 4: State Machines + Memory Barriers

---

### **Step 11: Implement RecordingManagerStateMachine (SSM)** ?

**Design Reference:** `Satellite-State-Machines.md` (Part 2)

**Location:** `State\RecordingManagerSSM.vb` (new file)

**Tasks:**
- [ ] Create `RecordingManagerSSM` class inheriting `SatelliteStateMachine(Of RecordingManagerState)`
- [ ] Define `RecordingManagerState` enum (Uninitialized, Idle, Arming, Armed, Recording, Stopping)
- [ ] Implement transition validation
- [ ] Implement `OnStateEntering` actions (calls RecordingManager methods)
- [ ] Implement `OnStateExiting` actions
- [ ] Subscribe to GSM.StateChanged
- [ ] Add thread safety (UI thread marshaling)
- [ ] Add XML documentation

**Acceptance Criteria:**
- SSM responds to GSM transitions
- RecordingManager lifecycle controlled
- Thread-safe state transitions
- Compiles and integrates with GSM

**Estimated Time:** 2 hours

**Design Sections:**
- Satellite-State-Machines.md ? Part 2: RecordingManagerSSM
- State-Coordinator-Design.md ? Part 4: SSM Integration

---

### **Step 12: Implement DSPThreadStateMachine (SSM)** ??

**Design Reference:** `Satellite-State-Machines.md` (Part 3)

**Location:** `State\DSPThreadSSM.vb` (new file)

**Tasks:**
- [ ] Create `DSPThreadSSM` class inheriting `SatelliteStateMachine(Of DSPThreadState)`
- [ ] Define `DSPThreadState` enum (Uninitialized, Idle, Running, Stopping, Error)
- [ ] Implement transition validation
- [ ] Implement `OnStateEntering` actions (calls DSPThread.Start/Stop)
- [ ] Subscribe to RecordingManagerSSM.StateChanged
- [ ] Add thread safety (worker thread control)
- [ ] Add error recovery logic
- [ ] Add XML documentation

**Acceptance Criteria:**
- SSM controls DSPThread lifecycle
- Responds to RecordingManagerSSM
- Thread-safe DSP control
- Error state handling

**Estimated Time:** 2 hours

**Design Sections:**
- Satellite-State-Machines.md ? Part 3: DSPThreadSSM
- Thread-Safety-Patterns.md ? Part 12: Thread Ownership

---

### **Step 13: Implement UIStateMachine** ??

**Design Reference:** `State-Machine-Design.md` (Part 7)

**Location:** `State\UIStateMachine.vb` (new file)

**Tasks:**
- [ ] Create `UIStateMachine` class implementing `IStateMachine(Of UIState)`
- [ ] Define `UIState` enum (Uninitialized, IdleUI, RecordingUI, PlayingUI, ErrorUI)
- [ ] Implement transition validation
- [ ] Subscribe to GSM.StateChanged
- [ ] Implement UI state mapping logic
- [ ] Add thread safety (must run on UI thread)
- [ ] Add XML documentation

**Acceptance Criteria:**
- UI states map to global states
- UI updates driven by state changes
- Thread-safe (InvokeRequired pattern)
- Compiles and integrates

**Estimated Time:** 1.5 hours

**Design Sections:**
- State-Machine-Design.md ? Part 7: UI State Machine
- Thread-Safety-Patterns.md ? Part 4: InvokeRequired Pattern

---

### **Step 14: Implement PlaybackStateMachine (SSM)** ??

**Design Reference:** `Satellite-State-Machines.md` (Part 4)

**Location:** `State\PlaybackSSM.vb` (new file)

**Tasks:**
- [ ] Create `PlaybackSSM` class inheriting `SatelliteStateMachine(Of PlaybackState)`
- [ ] Define `PlaybackState` enum (Uninitialized, Idle, Starting, Playing, Stopping)
- [ ] Implement transition validation
- [ ] Implement `OnStateEntering` actions (calls AudioRouter playback methods)
- [ ] Subscribe to GSM.StateChanged
- [ ] Add thread safety
- [ ] Add XML documentation

**Acceptance Criteria:**
- SSM controls playback lifecycle
- Responds to GSM transitions
- Thread-safe playback control
- Compiles and integrates

**Estimated Time:** 1.5 hours

**Design Sections:**
- Satellite-State-Machines.md ? Part 4: PlaybackSSM
- State-Coordinator-Design.md ? Part 4: SSM Integration

---

### **Step 15: Implement StateCoordinator** ??

**Design Reference:** `State-Coordinator-Design.md` (Complete)

**Location:** `State\StateCoordinator.vb` (new file)

**Tasks:**
- [ ] Create `StateCoordinator` singleton class
- [ ] Implement GSM instantiation and initialization
- [ ] Implement SSM instantiation (RecordingManagerSSM, DSPThreadSSM, PlaybackSSM)
- [ ] Implement UIStateMachine instantiation
- [ ] Subscribe to GSM.StateChanged
- [ ] Implement `PropagateToSatellites(globalState)` method
- [ ] Implement transition history tracking
- [ ] Add thread safety (UI thread marshaling - Option A)
- [ ] Implement disposal pattern (shutdown barrier)
- [ ] Add XML documentation

**Acceptance Criteria:**
- All state machines created and wired
- GSM transitions propagate to SSMs
- Thread-safe coordination
- Clean disposal
- Singleton pattern enforced
- Compiles and integrates

**Estimated Time:** 3-4 hours

**Design Sections:**
- State-Coordinator-Design.md ? All parts
- Thread-Safety-Patterns.md ? Part 7: StateCoordinator Threading Policy
- Thread-Safety-Patterns.md ? Part 13: Shutdown Barrier

---

## ?? **PHASE 3: THREAD SAFETY FIXES**

### **Step 16: Implement Thread Safety in DSPThread** ??

**Design Reference:** `Thread-Safety-Patterns.md` (Parts 1-6)

**Location:** `DSP\DSPThread.vb` (modify existing)

**Tasks:**
- [ ] Add `Volatile` flags for `_isRunning`, `_shouldStop`, `disposed`
- [ ] Replace boolean flags with `CancellationTokenSource`
- [ ] Add disposal guards (check disposed flag at method entry)
- [ ] Implement shutdown barrier pattern
- [ ] Add grace period in Dispose (50ms)
- [ ] Use `Interlocked` for counters (_processedSamples, _errorCount)
- [ ] Apply zero exceptions policy in hot path
- [ ] Add lock-free logging queue (optional)

**Acceptance Criteria:**
- No race conditions in worker thread
- Clean shutdown without crashes
- DSP hot path is lock-free
- Disposal guards prevent late access
- Compiles and builds

**Estimated Time:** 2-3 hours

**Design Sections:**
- Thread-Safety-Patterns.md ? Part 1: Volatile
- Thread-Safety-Patterns.md ? Part 2: CancellationToken
- Thread-Safety-Patterns.md ? Part 3: Real-Time Constraints
- Thread-Safety-Patterns.md ? Part 5: Disposal Guards
- Thread-Safety-Patterns.md ? Part 13: Shutdown Barrier

---

### **Step 17: Implement Thread Safety in MainForm Event Handlers** ??

**Design Reference:** `Thread-Safety-Patterns.md` (Part 4, Part 8)

**Location:** `MainForm.vb` (modify existing)

**Tasks:**
- [ ] Add `InvokeRequired` checks to all event handlers
- [ ] Use `BeginInvoke` for non-blocking UI updates
- [ ] Remove direct DSP/RecordingManager method calls
- [ ] Call StateCoordinator.RequestGlobalTransition instead
- [ ] Subscribe to UIStateMachine.StateChanged for UI updates
- [ ] Apply Pipeline UI rules (never read worker state directly)
- [ ] Remove System.Windows.Forms.Timer (if used for real-time data)

**Acceptance Criteria:**
- All event handlers use InvokeRequired
- No direct worker thread calls from UI
- UI updates driven by UIStateMachine
- No race conditions in event handlers
- Compiles and builds

**Estimated Time:** 2 hours

**Design Sections:**
- Thread-Safety-Patterns.md ? Part 4: InvokeRequired
- Thread-Safety-Patterns.md ? Part 8: Pipeline UI Rules

---

## ?? **PHASE 4: MONITORING IMPLEMENTATION**

### **Step 18: Implement MonitoringController** ??

**Design Reference:** `MonitoringController-Design.md` (Complete)

**Location:** `Managers\MonitoringController.vb` (new file)

**Tasks:**
- [ ] Create `MonitoringController` class implementing `IDisposable`
- [ ] Define `MonitoringState` enum
- [ ] Define `ReaderHealth` enum
- [ ] Create reader registry (`Dictionary(Of String, ReaderInfo)`)
- [ ] Implement `Initialize()` method (eager reader creation)
- [ ] Implement `Enable()` method
- [ ] Implement `Disable()` method
- [ ] Implement `Dispose()` with shutdown barrier
- [ ] Implement `RegisterReader(name, owner, tapLocation)` with validation
- [ ] Implement `GetRegisteredReaders()` (immutable snapshot)
- [ ] Implement `GetSnapshot()` (MonitoringSnapshot)
- [ ] Add thread safety (SyncLock on all public methods)
- [ ] Add XML documentation

**Acceptance Criteria:**
- All 10 enhancements from v2.0.0 integrated
- Reader registry thread-safe
- Enable/disable without dispose
- Naming validation enforced
- Compiles and builds

**Estimated Time:** 4-5 hours

**Design Sections:**
- MonitoringController-Design.md ? All parts
- Thread-Safety-Patterns.md ? Parts 2.5, 5, 6, 12, 13, 14

---

### **Step 19: Implement ReaderInfo & MonitoringSnapshot Classes** ??

**Design Reference:** `MonitoringController-Design.md` (Part 4)

**Locations:**
- `Managers\ReaderInfo.vb` (new file)
- `Managers\MonitoringSnapshot.vb` (new file)
- `Managers\ReaderHealth.vb` (new file - enum)

**Tasks:**
- [ ] Create `ReaderInfo` immutable class with health tracking
- [ ] Implement `OwnerComponent`, `TapPoint`, `ReaderType` properties (parse from name)
- [ ] Use `Interlocked` for `LastAccess` and `Health` properties
- [ ] Create `MonitoringSnapshot` immutable class
- [ ] Create `ReaderHealth` enum (7 states)
- [ ] Add XML documentation

**Acceptance Criteria:**
- ReaderInfo is thread-safe (immutable + atomic updates)
- MonitoringSnapshot is immutable
- Health tracking ready for Phase 2 enhancements
- Compiles and builds

**Estimated Time:** 1 hour

**Design Sections:**
- MonitoringController-Design.md ? Part 4: ReaderInfo & MonitoringSnapshot

---

### **Step 20: Refactor Reader Names to Convention** ??

**Design Reference:** `Reader-Management-Design.md` (Part 1, Part 8)

**Locations:**
- `AudioIO\AudioRouter.vb` (modify)
- `Managers\RecordingManager.vb` (modify)
- `DSP\TapPointManager.vb` (verify - no changes needed)

**Tasks:**
- [ ] Find all `_default_` usage in codebase
- [ ] Replace with `{Owner}_{TapPoint}_{Type}` pattern
- [ ] AudioRouter: Register `AudioRouter_Input_FFT`, `AudioRouter_Output_FFT`
- [ ] RecordingManager: Register `RecordingManager_PreDSP_Meter`, `RecordingManager_PostDSP_Meter`
- [ ] Update all `TapPointManager.CreateReader()` calls with new names
- [ ] Test all readers still work
- [ ] Remove legacy `_default_` code

**Acceptance Criteria:**
- No `_default_` usage remains
- All readers follow naming convention
- Readers registered with MonitoringController
- FFT and meters still functional
- Compiles and builds

**Estimated Time:** 2 hours

**Design Sections:**
- Reader-Management-Design.md ? Part 1: Naming Convention
- Reader-Management-Design.md ? Part 8: Migration from Legacy

---

## ?? **PHASE 5: INTEGRATION & WIRING**

### **Step 21: Wire State Machines to RecordingManager** ??

**Design Reference:** `State-Coordinator-Design.md` (Part 6)

**Location:** `Managers\RecordingManager.vb` (modify)

**Tasks:**
- [ ] Remove direct state management code
- [ ] Replace with StateCoordinator transition requests
- [ ] ArmMicrophone: Request transition to Arming
- [ ] StartRecording: Handled by RecordingManagerSSM
- [ ] StopRecording: Request transition to Stopping
- [ ] Update event handlers to use StateCoordinator
- [ ] Remove old state flags (if any)

**Acceptance Criteria:**
- RecordingManager no longer manages own state
- All lifecycle transitions go through StateCoordinator
- RecordingManagerSSM controls actual start/stop
- Compiles and builds

**Estimated Time:** 1.5 hours

**Design Sections:**
- State-Coordinator-Design.md ? Part 6: Integration Points
- Satellite-State-Machines.md ? Part 2: RecordingManagerSSM

---

### **Step 22: Wire State Machines to MainForm** ??

**Design Reference:** `State-Coordinator-Design.md` (Part 6)

**Location:** `MainForm.vb` (modify)

**Tasks:**
- [ ] Subscribe to UIStateMachine.StateChanged
- [ ] Update button states based on UIState
- [ ] Remove direct state checks (e.g., `If recordingManager.IsRecording Then`)
- [ ] Button clicks call StateCoordinator.RequestGlobalTransition
- [ ] Update status labels from UIStateMachine events
- [ ] Update Pipeline UI from UIStateMachine

**Acceptance Criteria:**
- UI driven entirely by UIStateMachine
- No direct state reads from workers
- Button enable/disable based on valid transitions
- Compiles and builds

**Estimated Time:** 2 hours

**Design Sections:**
- State-Coordinator-Design.md ? Part 6: Integration Points
- State-Machine-Design.md ? Part 7: UI State Machine
- Thread-Safety-Patterns.md ? Part 8: Pipeline UI Rules

---

### **Step 23: Wire MonitoringController to StateCoordinator** ??

**Design Reference:** `MonitoringController-Design.md` (Part 7)

**Location:** `State\StateCoordinator.vb` (modify)

**Tasks:**
- [ ] Create MonitoringController instance
- [ ] Call MonitoringController.Initialize() during StateCoordinator.Initialize()
- [ ] Subscribe to GSM.StateChanged for monitoring enable/disable
- [ ] Enable monitoring on Armed/Recording/Playing states
- [ ] Disable monitoring on Idle/Stopping states
- [ ] Dispose MonitoringController in StateCoordinator.Dispose()

**Acceptance Criteria:**
- MonitoringController integrates with state lifecycle
- Readers auto-enable during recording/playback
- Readers auto-disable when idle
- Clean disposal on shutdown
- Compiles and builds

**Estimated Time:** 1 hour

**Design Sections:**
- MonitoringController-Design.md ? Part 7: StateCoordinator Integration
- State-Coordinator-Design.md ? Part 5: Disposal

---

### **Step 24: Add State Validation, Logging, and Registry** ?

**Design Reference:** 
- `State-Coordinator-Design.md` (Part 3)
- `Registry.md` (State Registry Pattern - NEW!)

**Locations:**
- `State\StateCoordinator.vb` (modify)
- `State\GlobalStateMachine.vb` (modify)
- `State\StateRegistry.yaml` (create - documentation)
- All state enums (add Description attributes)
- `State\IStateMachine.vb` (modify StateChangedEventArgs)

**Tasks:**

**Part A: State Validation & Logging (Original):**
- [ ] Add transition history tracking to StateCoordinator
- [ ] Implement `RecordTransition(machine, oldState, newState, reason)` method
- [ ] Log all state transitions to console/file
- [ ] Add invalid transition logging (rejected transitions)
- [ ] Create debug method `DumpStateHistory()`
- [ ] Add state snapshot method for debugging
- [x] Add `GetSystemState()` method ? Already implemented!
- [x] Add `RecoverFromError()` method ? Already implemented!

**Part B: State Registry Pattern (NEW - Registry.md):**
- [ ] Add State UIDs to all enums using Description attributes
  - Example: `<Description("GSM_IDLE")> Idle = 1`
  - GlobalState, RecordingManagerState, DSPThreadState, UIState, PlaybackState
- [ ] Add TransitionID to StateChangedEventArgs(Of TState)
  - Format: "GSM_T01_IDLE_TO_ARMING"
- [ ] Create `StateRegistry.yaml` (master documentation)
- [ ] Create `State-Evolution-Log.md` (why states exist)
- [ ] Update logging format: "[GSM] T01: Idle ? Arming (trigger: ...)"
- [ ] Make logs searchable by UID/TransitionID

**Acceptance Criteria:**
- All transitions logged with timestamp + UID + TransitionID
- Invalid transitions logged as warnings
- State history available for debugging
- GetSystemState() works ? (already done!)
- RecoverFromError() works ? (already done!)
- StateRegistry.yaml documents all states/transitions
- State-Evolution-Log.md explains why states exist
- Logs searchable by UID (grep "GSM_T01")
- Compiles and builds

**Estimated Time:** 2.5 hours (was 1.5 hours)
- Original: 1.5 hours (mostly done!)
- Registry: 1 hour (UIDs, TransitionIDs, docs)

**Design Sections:**
- State-Coordinator-Design.md ? Part 3: Transition Tracking
- Thread-Safety-Patterns.md ? Part 14: State Snapshot Pattern
- **Registry.md ? State Registry Pattern (NEW!)** ?
- StateMachineUI.md (for GetSystemState API)

**Future (v1.4.0+):**
- Code generation from Registry.yaml
- State validator (build-time)
- State dashboard (visualization)

---

## ? **PHASE 6: TESTING & VALIDATION**

### **Step 25: Test Normal Recording Flow** ??

**Design Reference:** `State-Machine-Design.md` (Part 6)

**Tasks:**
- [ ] Start application (Uninitialized ? Idle)
- [ ] Click Record button (Idle ? Arming ? Armed ? Recording)
- [ ] Verify FFT/meters enabled during recording
- [ ] Click Stop button (Recording ? Stopping ? Idle)
- [ ] Verify FFT/meters disabled when idle
- [ ] Check all state transitions logged
- [ ] Verify no crashes or errors

**Acceptance Criteria:**
- Recording flow works end-to-end
- State transitions as expected
- Monitoring enables/disables correctly
- No exceptions or crashes

**Estimated Time:** 30 minutes

---

### **Step 25.5: Implement State Debugger Panel** ?? ??

**Design Reference:** `StateMachineUI.md` (Complete)

**Location:** `UI\Panels\StateDebuggerPanel.vb` (new file)

**Tasks:**
- [ ] Create `StateDebuggerPanel` UserControl
- [ ] Add labels for Global State + all SSMs + UIStateMachine
- [ ] Add LED indicators (colored panels) for each state
- [ ] Add transition history viewer (multiline textbox, last 50)
- [ ] Add 250ms refresh timer
- [ ] Wire to `StateCoordinator.GetSystemState()`
- [ ] Add "Dump All" button ? `StateCoordinator.DumpStateHistory()`
- [ ] Add "Force Error" button (for testing error recovery)
- [ ] Add "Recover" button ? `StateCoordinator.RecoverFromError()`
- [ ] Add state color mapping (Red=Recording, Yellow=Arming, Green=Running, etc.)
- [ ] Add to MainForm as collapsible panel or separate window
- [ ] Add XML documentation

**Acceptance Criteria:**
- All state machines visible in real-time (GSM + RecordingManagerSSM + DSPThreadSSM + UIStateMachine + PlaybackSSM)
- LED indicators change color based on state
- Transition history updates every 250ms
- "Dump All" exports complete state history to file/console
- "Force Error" triggers Error state transition (for Step 27 testing)
- "Recover" successfully returns to Idle state
- Panel can be shown/hidden via MainForm menu
- Compiles and integrates with StateCoordinator

**Estimated Time:** 2-3 hours

**Design Sections:**
- StateMachineUI.md (Complete specification)
- State-Coordinator-Design.md ? Part 3: Transition Tracking
- Architecture-Documents-Analysis.md ? State Debugger Panel section

**Benefits:**
- ? Visual debugging tool for Steps 25-28
- ? See ALL state machines at once
- ? Force error scenarios (Step 27)
- ? Trust state transitions
- ? Catch integration bugs immediately

---

### **Step 26: Test Normal Playback Flow** ??

**Design Reference:** `State-Machine-Design.md` (Part 6)

**Tasks:**
- [ ] Start application
- [ ] Click Play button (Idle ? Playing)
- [ ] Verify playback starts
- [ ] Verify FFT/meters enabled during playback
- [ ] Click Stop button (Playing ? Stopping ? Idle)
- [ ] Verify FFT/meters disabled when idle
- [ ] Check state transitions

**Acceptance Criteria:**
- Playback flow works end-to-end
- State transitions correct
- No crashes

**Estimated Time:** 20 minutes

---

### **Step 27: Test Error Recovery** ??

**Design Reference:** `State-Machine-Design.md` (Part 6)

**Tasks:**
- [ ] Trigger recording error (disconnect microphone during recording)
- [ ] Verify transition to Error state
- [ ] Verify recovery to Idle state
- [ ] Check error logged
- [ ] Test multiple error scenarios

**Acceptance Criteria:**
- Errors transition to Error state
- Recovery to Idle works
- No crashes on errors
- Error states logged

**Estimated Time:** 30 minutes

---

### **Step 28: Test Invalid Transition Prevention** ??

**Design Reference:** `State-Machine-Design.md` (Part 5)

**Tasks:**
- [ ] Attempt invalid transitions (e.g., Idle ? Recording without Arming)
- [ ] Verify transitions rejected
- [ ] Check rejection logged
- [ ] Verify system remains in valid state

**Acceptance Criteria:**
- Invalid transitions rejected
- System remains stable
- Rejections logged as warnings

**Estimated Time:** 20 minutes

---

## ?? **PHASE 7: DOCUMENTATION**

### **Step 29: Create Architecture Documentation** ??

**Tasks:**
- [ ] Document final architecture in README
- [ ] Create state machine diagrams (Mermaid)
- [ ] Document thread safety patterns used
- [ ] Create component interaction diagrams
- [ ] Document reader naming convention
- [ ] Add troubleshooting guide

**Estimated Time:** 2 hours

---

### **Step 30: Create Session Documentation** ??

**Tasks:**
- [ ] Create session summary document
- [ ] Document all changes made
- [ ] List all new files created
- [ ] List all files modified
- [ ] Document testing results
- [ ] Add lessons learned

**Estimated Time:** 1 hour

---

## ?? **TASK SUMMARY**

**Total Tasks:** 31 (was 30)
- **Phase 1 (Design):** 8 tasks ? COMPLETE
- **Phase 2 (State Machines):** 7 tasks (Steps 9-15)
- **Phase 3 (Thread Safety):** 2 tasks (Steps 16-17)
- **Phase 4 (Monitoring):** 3 tasks (Steps 18-20)
- **Phase 5 (Integration):** 4 tasks (Steps 21-24)
- **Phase 6 (Testing):** 5 tasks (Steps 25, **25.5 ??**, 26-28)
- **Phase 7 (Documentation):** 2 tasks (Steps 29-30)

**Implementation Estimate:** ~38-48 hours total (was 35-45)

**New in v1.3.2.1:**
- ? **Step 25.5: State Debugger Panel** (2-3 hours) - Developer tool for real-time state visualization

**Status:** Phase 1 Complete, Ready for Phase 2 Implementation

---

## ?? **NEXT ACTIONS**

**Immediate Next Steps:**
1. ? Review this master task list
2. ? Validate design document references
3. ?? Start **Step 9: Implement IStateMachine Interface** (30 min)
4. ?? Continue with **Step 10: Implement GlobalStateMachine** (2-3 hours)

**Recommended Approach:**
- Tackle one step at a time
- Validate each step compiles before moving to next
- Use design docs as specification
- Test integration points as you go
- Commit after each completed step

---

## ?? **DESIGN DOCUMENT REFERENCE**

**All Design Documents Location:** `Documentation\Architecture\`

1. **Architecture-Assessment-v1_3_2_1.md** - Problem analysis
2. **State-Machine-Design.md** - Core state machine architecture
3. **Satellite-State-Machines.md** - SSM specifications
4. **State-Coordinator-Design.md** - Coordinator pattern
5. **Thread-Safety-Audit.md** - Race condition analysis
6. **Thread-Safety-Patterns.md (v2.0.0)** - 11 thread safety patterns
7. **MonitoringController-Design.md (v2.0.0)** - Monitoring subsystem
8. **Reader-Management-Design.md (v1.0.0)** - Naming convention

---

**Created:** 2026-01-17  
**By:** Rick + GitHub Copilot  
**Phase 1:** ? COMPLETE  
**Status:** ?? READY FOR IMPLEMENTATION

**Let's build this! ??**
