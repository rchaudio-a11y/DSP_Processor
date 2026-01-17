# Task List v1.3.2.1 - State Machine Architecture + Thread Safety Overhaul

**Date:** 2026-01-17  
**Version:** 1.3.2.1  
**Status:** ?? PLANNING  
**Methodology:** Analyze ? Document ? Execute

---

## ?? **OBJECTIVES**

1. **Implement Hierarchical State Machine** - Replace boolean flag soup with formal state management
2. **Fix Thread Safety Issues** - Eliminate race conditions and cross-thread access bugs
3. **Implement MonitoringController** - Add enable/disable control for FFT/meters
4. **Standardize Reader Management** - Replace `_default_` pattern with proper registry
5. **Resolve All Open Issues** - Close issues 001-005 through architectural fixes

---

## ?? **SCOPE**

### **New Components:**
- GlobalStateMachine (GSM) - Master state controller
- 4 Satellite State Machines (SSMs) - Recording, DSP, UI, Playback
- StateCoordinator - Event propagation hub
- MonitoringController - Reader lifecycle and monitoring control
- ReaderRegistry - Reader metadata and cleanup
- Thread-safe access patterns

### **Modified Components:**
- RecordingManager - State-driven lifecycle
- MainForm - State-driven UI updates
- AudioRouter - State-driven playback
- DSPThread - Thread-safe flags and state control

### **Documentation:**
- 8 new architecture design documents
- 1 thread safety audit report
- 1 session documentation

---

## ?? **PHASE 1: ANALYSIS & DOCUMENTATION (Steps 1-8)**

### ? **STEP 1: Architecture Assessment**
**File:** `Documentation/Active/Issues/Architecture-Assessment-v1_3_2_1.md`

**Tasks:**
- [ ] Review all open issues (001-005)
- [ ] Document current state management approach
- [ ] Identify all boolean flags used for state
- [ ] Document current threading model
- [ ] List all cross-thread access points
- [ ] Analyze reader creation/cleanup patterns
- [ ] Document monitoring enable/disable gaps
- [ ] Create issue priority matrix

**Output:** Comprehensive assessment document listing all problems and root causes

---

### ? **STEP 2: GlobalStateMachine Design**
**File:** `Documentation/Architecture/State-Machine-Design.md`

**Tasks:**
- [ ] Define global states enum
  - Uninitialized, Idle, Arming, Armed, Recording, Stopping, Playing, Error
- [ ] Create state transition table (from ? to validity)
- [ ] Design StateChanged event signature
- [ ] Document transition validation rules
- [ ] Create state transition diagrams (Mermaid)
- [ ] Design state history tracking
- [ ] Document error state handling

**Output:** Complete GSM specification with diagrams

---

### ? **STEP 3: Satellite State Machines Design**
**File:** `Documentation/Architecture/Satellite-State-Machines.md`

**Tasks:**
- [ ] **RecordingManagerSSM:**
  - States: Uninitialized, DeviceSelecting, DeviceReady, Arming, Armed, Recording, Stopping, Error
  - Map to GSM states
  - Define behavior hooks
- [ ] **DSPThreadSSM:**
  - States: Stopped, Starting, Running, Stopping, Error
  - Worker thread lifecycle rules
  - Map to GSM states
- [ ] **UIStateMachine:**
  - States: IdleUI, ArmingUI, ArmedUI, RecordingUI, StoppingUI, PlayingUI, ErrorUI
  - Panel visibility rules
  - Button enable/disable matrix
- [ ] **PlaybackStateMachine:**
  - States: Idle, Loading, Ready, Playing, Paused, Stopping, Error
  - AudioRouter behavior mapping
  - File loading lifecycle

**Output:** Complete SSM specifications for all 4 machines

---

### ? **STEP 4: StateCoordinator Design**
**File:** `Documentation/Architecture/State-Coordinator-Design.md`

**Tasks:**
- [ ] Design initialization sequence
- [ ] Design GSM ? SSM event propagation
- [ ] Design SSM ? GSM request flow
- [ ] Design error recovery strategies
- [ ] Document coordinator lifecycle
- [ ] Create event flow diagrams
- [ ] Design debugging/telemetry hooks

**Output:** Complete coordinator specification with event flow

---

### ? **STEP 5: Thread Safety Audit**
**File:** `Documentation/Active/Issues/Thread-Safety-Audit.md`

**Tasks:**
- [ ] Audit DSPThread boolean flags (_isRunning, shouldStop)
- [ ] Audit MainForm event handlers (OnRecordingBufferAvailable, etc.)
- [ ] Audit RecordingManager shared state
- [ ] Audit AudioRouter shared state
- [ ] Document all race conditions found
- [ ] Document missing InvokeRequired checks
- [ ] Prioritize fixes by severity
- [ ] Create before/after examples

**Output:** Complete thread safety audit with prioritized fix list

---

### ? **STEP 6: Thread Safety Patterns**
**File:** `Documentation/Architecture/Thread-Safety-Patterns.md`

**Tasks:**
- [ ] Document Volatile pattern for boolean flags
- [ ] Document Interlocked pattern for atomic operations
- [ ] Document SyncLock pattern for critical sections
- [ ] Document InvokeRequired pattern for UI marshalling
- [ ] Create code examples for each pattern
- [ ] Document when to use each pattern
- [ ] Create threading decision flowchart

**Output:** Thread safety pattern guide with examples

---

### ? **STEP 7: MonitoringController Design**
**File:** `Documentation/Architecture/MonitoringController-Design.md`

**Tasks:**
- [ ] Design enable/disable API
- [ ] Design reader lifecycle (create/destroy)
- [ ] Design integration with StateCoordinator
- [ ] Design monitoring state enum
- [ ] Document reader ownership tracking
- [ ] Design cleanup strategy
- [ ] Create usage examples

**Output:** Complete MonitoringController specification

---

### ? **STEP 8: Reader Management Design**
**File:** `Documentation/Architecture/Reader-Management-Design.md`

**Tasks:**
- [ ] Define naming convention: `{Owner}_{Purpose}`
- [ ] List all current readers and their new names
- [ ] Design ReaderRegistry schema (name, owner, tapLocation, timestamps)
- [ ] Design cleanup API
- [ ] Design orphan detection algorithm
- [ ] Document reader lifecycle
- [ ] Create migration plan from `_default_` pattern

**Output:** Reader management specification with naming convention

---

## ??? **PHASE 2: CORE IMPLEMENTATION (Steps 9-15)**

### ? **STEP 9-10: State Machine Core**
**Files:** `State\IStateMachine.vb`, `State\GlobalStateMachine.vb`

**Tasks:**
- [ ] Create IStateMachine interface
- [ ] Implement GlobalStateMachine class
- [ ] Add transition validation
- [ ] Add state history tracking
- [ ] Add StateChanged event
- [ ] Unit test transition rules

---

### ? **STEP 11-14: Satellite State Machines**
**Files:** `State\RecordingManagerStateMachine.vb`, `State\DSPThreadStateMachine.vb`, etc.

**Tasks:**
- [ ] Implement RecordingManagerSSM
- [ ] Implement DSPThreadSSM
- [ ] Implement UIStateMachine
- [ ] Implement PlaybackStateMachine
- [ ] Wire all to GlobalStateMachine
- [ ] Add state-specific behavior hooks

---

### ? **STEP 15: StateCoordinator**
**File:** `State\StateCoordinator.vb`

**Tasks:**
- [ ] Initialize all state machines
- [ ] Subscribe to GSM events
- [ ] Implement event propagation
- [ ] Add error recovery
- [ ] Add state dump method for debugging

---

## ?? **PHASE 3: THREAD SAFETY (Steps 16-17)**

### ? **STEP 16: DSPThread Thread Safety**
**File:** `DSP\DSPThread.vb`

**Tasks:**
- [ ] Add `Volatile` to `_isRunning` and `shouldStop`
- [ ] Implement thread-safe IsRunning property
- [ ] Add SyncLock to state checks
- [ ] Document changes in code comments

---

### ? **STEP 17: MainForm Thread Safety**
**File:** `MainForm.vb`

**Tasks:**
- [ ] Add InvokeRequired to OnRecordingBufferAvailable
- [ ] Add InvokeRequired to OnDSPInputSamples
- [ ] Add InvokeRequired to OnDSPOutputSamples
- [ ] Create SafeStopRecording wrapper
- [ ] Add thread-safe manager access

---

## ??? **PHASE 4: MONITORING & READERS (Steps 18-20)**

### ? **STEP 18-19: Monitoring Infrastructure**
**Files:** `DSP\MonitoringController.vb`, `DSP\ReaderRegistry.vb`

**Tasks:**
- [ ] Implement MonitoringController
- [ ] Implement ReaderRegistry
- [ ] Wire to StateCoordinator
- [ ] Add enable/disable API

---

### ? **STEP 20: Reader Naming Refactor**
**Files:** Multiple (AudioRouter, TapPointManager, etc.)

**Tasks:**
- [ ] Replace `"_default_input"` ? `"AudioRouter_InputFFT"`
- [ ] Replace `"_default_output"` ? `"AudioRouter_OutputFFT"`
- [ ] Update TapPointManager readers to `"RecordingManager_*"` pattern
- [ ] Update all Available() checks
- [ ] Remove `_default_` pattern entirely

---

## ?? **PHASE 5: INTEGRATION (Steps 21-23)**

### ? **STEP 21-23: Wire State Machines**
**Files:** `RecordingManager.vb`, `MainForm.vb`, `AudioRouter.vb`

**Tasks:**
- [ ] Replace boolean flags with state checks in RecordingManager
- [ ] Wire UIStateMachine to MainForm button/panel controls
- [ ] Wire PlaybackStateMachine to AudioRouter
- [ ] Wire MonitoringController to playback lifecycle
- [ ] Remove duplicate state tracking

---

## ? **PHASE 6: VALIDATION (Steps 24-28)**

### ? **STEP 24: State Logging**
**Tasks:**
- [ ] Add transition logging to all SSMs
- [ ] Implement state history tracking
- [ ] Add debug state dump method
- [ ] Add invalid transition detection

---

### ? **STEP 25-28: Testing**
**Tasks:**
- [ ] Test recording flow: Idle ? Arming ? Armed ? Recording ? Stopping ? Idle
- [ ] Test playback flow: Idle ? Playing ? Stopping ? Idle
- [ ] Test error recovery: device failure, file not found, DSP crash
- [ ] Test invalid transitions: Recording ? Playing, etc.

---

## ?? **PHASE 7: DOCUMENTATION (Steps 29-30)**

### ? **STEP 29-30: Final Documentation**
**Files:** Architecture docs, session notes

**Tasks:**
- [ ] Update Audio-Signal-Flow diagram
- [ ] Create state transition diagrams
- [ ] Document thread safety patterns used
- [ ] Create session documentation
- [ ] List all issues resolved

---

## ?? **SUCCESS CRITERIA**

? **All open issues (001-005) resolved**  
? **Zero race conditions remaining**  
? **State machine controls all lifecycle**  
? **Monitoring cleanly enable/disable**  
? **Readers use proper naming convention**  
? **Full state transition logging**  
? **All tests pass**

---

## ?? **ESTIMATED TIME**

- **Phase 1 (Analysis):** 6-8 hours (Steps 1-8)
- **Phase 2 (Core):** 8-10 hours (Steps 9-15)
- **Phase 3 (Thread Safety):** 2-3 hours (Steps 16-17)
- **Phase 4 (Monitoring):** 4-5 hours (Steps 18-20)
- **Phase 5 (Integration):** 6-8 hours (Steps 21-23)
- **Phase 6 (Validation):** 4-5 hours (Steps 24-28)
- **Phase 7 (Documentation):** 2-3 hours (Steps 29-30)

**Total:** 32-42 hours (4-5 full days)

---

## ?? **NEXT STEPS**

1. **Start with Step 1** - Create Architecture Assessment document
2. **Review with team** - Validate approach before implementation
3. **Proceed phase by phase** - Don't skip analysis/documentation
4. **Test continuously** - Validate after each phase
5. **Document as you go** - Keep session notes up to date

---

**Status:** ?? Ready to begin Step 1  
**Methodology:** ? Analyze ? Document ? Execute
