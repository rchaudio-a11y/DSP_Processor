# State Registry Implementation Plan
## Pattern #15: The State Registry Pattern

**Created:** 2026-01-17  
**Status:** ?? PLANNED  
**Implementation:** Phase 5 Step 24 (v1.3.2.1 - Lightweight) + Future v1.4.0+ (Full System)

---

## ?? **OVERVIEW**

The State Registry Pattern creates a **single source of truth** for all state machines, states, and transitions in the system. This document tracks the implementation roadmap from lightweight documentation (v1.3.2.1) to full code generation (v1.4.0+).

**Reference:** `Documentation\Architecture\Registry.md`

---

## ?? **IMPLEMENTATION PHASES**

### **Phase 1: Lightweight Registry (v1.3.2.1 - Current Release)**

**Goal:** Add UIDs and documentation without changing architecture

**Deliverables:**
1. ? State UIDs in all enums (Description attributes)
2. ? TransitionIDs in logging
3. ? StateRegistry.yaml (documentation)
4. ? State-Evolution-Log.md (why states exist)

**Time:** 1 hour (part of Step 24)  
**Risk:** Low (documentation + minor code changes)  
**Impact:** Huge improvement in debugging

---

### **Phase 2: Full Registry System (v1.4.0+ - Future)**

**Goal:** Code generation and validation from Registry.yaml

**Deliverables:**
1. ?? Code generator (YAML ? VB enums/classes)
2. ?? State validator (build-time transition checks)
3. ?? State dashboard (WPF/Blazor visualization)
4. ?? SSM template system

**Time:** 20-40 hours  
**Risk:** Medium (new tooling, build changes)  
**Impact:** Professional-grade state management

---

## ?? **PHASE 1 IMPLEMENTATION (v1.3.2.1)**

### **Task 1: Add State UIDs to Enums**

**Files to modify:**
- `State\GlobalStateMachine.vb`
- `State\RecordingManagerSSM.vb`
- `State\DSPThreadSSM.vb`
- `State\UIStateMachine.vb`
- `State\PlaybackSSM.vb`

**Before:**
```visualbasic
Public Enum GlobalState
    Uninitialized = 0
    Idle = 1
    Arming = 2
    Armed = 3
    Recording = 4
    Stopping = 5
    Playing = 6
    [Error] = 7
End Enum
```

**After:**
```visualbasic
Public Enum GlobalState
    <Description("GSM_UNINIT")> Uninitialized = 0
    <Description("GSM_IDLE")> Idle = 1
    <Description("GSM_ARMING")> Arming = 2
    <Description("GSM_ARMED")> Armed = 3
    <Description("GSM_RECORDING")> Recording = 4
    <Description("GSM_STOPPING")> Stopping = 5
    <Description("GSM_PLAYING")> Playing = 6
    <Description("GSM_ERROR")> [Error] = 7
End Enum
```

**Helper Method:**
```visualbasic
''' <summary>
''' Get UID for a state (reads Description attribute)
''' </summary>
Public Shared Function GetStateUID(Of TState As Structure)(state As TState) As String
    Dim field = state.GetType().GetField(state.ToString())
    Dim attr = CType(Attribute.GetCustomAttribute(field, GetType(DescriptionAttribute)), DescriptionAttribute)
    Return If(attr?.Description, state.ToString())
End Function
```

**UID Naming Convention:**
- GlobalStateMachine: `GSM_*`
- RecordingManagerSSM: `REC_*`
- DSPThreadSSM: `DSP_*`
- UIStateMachine: `UI_*`
- PlaybackSSM: `PLY_*`

**Examples:**
```
GSM_IDLE, GSM_ARMING, GSM_RECORDING
REC_IDLE, REC_ARMING, REC_RECORDING
DSP_IDLE, DSP_RUNNING, DSP_STOPPING
UI_IDLE, UI_RECORDING, UI_PLAYING
PLY_IDLE, PLY_PLAYING, PLY_STOPPING
```

---

### **Task 2: Add TransitionIDs to Logging**

**File to modify:** `State\IStateMachine.vb`

**Current StateChangedEventArgs:**
```visualbasic
Public Class StateChangedEventArgs(Of TState As Structure)
    Inherits EventArgs
    
    Public Property OldState As TState
    Public Property NewState As TState
    Public Property Reason As String
    Public Property Timestamp As DateTime
End Class
```

**Enhanced:**
```visualbasic
Public Class StateChangedEventArgs(Of TState As Structure)
    Inherits EventArgs
    
    Public Property OldState As TState
    Public Property NewState As TState
    Public Property Reason As String
    Public Property Timestamp As DateTime
    Public Property TransitionID As String ' NEW! "GSM_T01_IDLE_TO_ARMING"
    Public Property OldStateUID As String ' NEW! "GSM_IDLE"
    Public Property NewStateUID As String ' NEW! "GSM_ARMING"
End Class
```

**TransitionID Format:**
```
{MachinePrefix}_T{Number}_{OldState}_TO_{NewState}

Examples:
GSM_T01_IDLE_TO_ARMING
GSM_T02_ARMING_TO_ARMED
GSM_T03_ARMED_TO_RECORDING
REC_T01_IDLE_TO_ARMING
DSP_T01_IDLE_TO_RUNNING
```

**Auto-generate in GlobalStateMachine.TransitionTo():**
```visualbasic
Private _transitionCounter As Integer = 0

Public Function TransitionTo(newState As GlobalState, reason As String) As Boolean
    ' ... existing code ...
    
    ' Generate TransitionID
    Dim transitionNum = Interlocked.Increment(_transitionCounter)
    Dim transitionID = $"GSM_T{transitionNum:D2}_{GetStateUID(oldState)}_TO_{GetStateUID(newState)}"
    
    Dim args As New StateChangedEventArgs(Of GlobalState) With {
        .OldState = oldState,
        .NewState = newState,
        .Reason = reason,
        .Timestamp = DateTime.Now,
        .TransitionID = transitionID,
        .OldStateUID = GetStateUID(oldState),
        .NewStateUID = GetStateUID(newState)
    }
    ' ...
End Function
```

---

### **Task 3: Update Logging Format**

**Current format:**
```
[INFO] State transition: Idle ? Arming (Reason: User clicked Arm)
```

**Enhanced format:**
```
[INFO] [GSM] T01: IDLE ? ARMING (trigger: UserClickedArm)
```

**Benefits:**
- Searchable by UID: `grep "GSM_T01"`
- Searchable by state: `grep "GSM_IDLE"`
- Searchable by machine: `grep "\[GSM\]"`
- Instant context in logs

**Example log sequence:**
```
[INFO] [GSM] T01: IDLE ? ARMING (trigger: UserClickedArm)
[INFO] [REC] T01: IDLE ? ARMING (trigger: GSM_StateChanged)
[INFO] [GSM] T02: ARMING ? ARMED (trigger: MicrophoneReady)
[INFO] [REC] T02: ARMING ? ARMED (trigger: GSM_StateChanged)
[INFO] [GSM] T03: ARMED ? RECORDING (trigger: UserClickedRecord)
[INFO] [REC] T03: ARMED ? RECORDING (trigger: GSM_StateChanged)
[INFO] [DSP] T01: IDLE ? RUNNING (trigger: REC_StateChanged)
[INFO] [UI] T01: IDLE ? RECORDING (trigger: GSM_StateChanged)
```

**Now you can grep:**
```bash
grep "GSM_T01" logs.txt  # All Idle?Arming transitions
grep "\[REC\]" logs.txt  # All RecordingManager transitions
grep "RECORDING" logs.txt # All transitions involving Recording state
```

---

### **Task 4: Create StateRegistry.yaml**

**File:** `State\StateRegistry.yaml`

**Purpose:** Master documentation of all states and transitions

**Format:**
```yaml
# State Registry - Single Source of Truth
# This is the authoritative reference for all state machines
# Future: Code generation will use this as input

GlobalStateMachine:
  prefix: GSM
  description: Master state machine controlling application-wide state
  states:
    - name: Uninitialized
      uid: GSM_UNINIT
      value: 0
      description: System not yet initialized
    - name: Idle
      uid: GSM_IDLE
      value: 1
      description: System idle, ready for user input
    - name: Arming
      uid: GSM_ARMING
      value: 2
      description: Arming microphone for recording
    # ... all states ...
  
  transitions:
    - id: GSM_T01
      from: Idle
      to: Arming
      trigger: ArmMicrophone
      description: User requests microphone arming
    - id: GSM_T02
      from: Arming
      to: Armed
      trigger: MicrophoneReady
      description: Microphone successfully armed
    # ... all transitions ...

RecordingManagerSSM:
  prefix: REC
  description: Controls RecordingManager lifecycle
  states:
    - name: Idle
      uid: REC_IDLE
      value: 1
      description: RecordingManager idle
    # ... all states ...
  
  transitions:
    - id: REC_T01
      from: Idle
      to: Arming
      trigger: GSM_StateChanged
      description: Follows GlobalStateMachine Idle?Arming
    # ... all transitions ...

# ... all other state machines ...
```

**Benefit:** Single place to see all states and transitions!

---

### **Task 5: Create State-Evolution-Log.md**

**File:** `Documentation\Architecture\State-Evolution-Log.md`

**Purpose:** Track why states exist (prevents ghost states)

**Format:**
```markdown
# State Evolution Log
## History of State Machine Changes

### GlobalStateMachine

#### 2026-01-17: Initial Implementation
**States Added:**
- Uninitialized, Idle, Arming, Armed, Recording, Stopping, Playing, Error

**Rationale:**
- Uninitialized: Needed to prevent operations before StateCoordinator.Initialize()
- Arming: Separate state to prevent race condition between arm and record
- Armed: Ready state before recording (allows abort without recording)
- Stopping: Prevents race between stop request and cleanup
- Error: Recovery state for all failures

**Problems Solved:**
- Issue #11: State duplication in RecordingManager
- Issue #12: Event conflicts during transitions

#### Future: If we add states
**Date:** TBD
**States Added:** (e.g., Paused)
**Rationale:** (Why it was needed)
**Problems Solved:** (What bug/feature it addresses)

### RecordingManagerSSM
...
```

**Benefit:** Future you (or other devs) know why states exist!

---

## ?? **TESTING STRATEGY (Phase 1)**

### **Manual Testing:**
1. Record ? Stop ? Disarm flow
2. Check logs for TransitionIDs
3. Grep logs by UID
4. Verify StateRegistry.yaml is complete

### **Automated Testing (Future):**
- State validator (checks Registry.yaml against code)
- Transition validator (illegal transitions throw errors)

---

## ?? **PHASE 2 IMPLEMENTATION (v1.4.0+ - Future)**

### **Code Generation System**

**Input:** `StateRegistry.yaml`  
**Output:** 
- Generated enums
- Generated state machine classes
- Generated transition tables
- Generated documentation

**Tools:**
- T4 templates (Visual Studio)
- PowerShell script
- MSBuild task (runs at build time)

**Example:**
```powershell
# Generate-StateMachines.ps1
$registry = Get-Content StateRegistry.yaml | ConvertFrom-Yaml
foreach ($machine in $registry.Machines) {
    # Generate enum
    # Generate class
    # Generate transitions
}
```

### **State Validator**

**Validates at build time:**
- No orphan states (in code but not Registry)
- No missing states (in Registry but not code)
- No invalid transitions (not in Registry)
- No duplicate UIDs
- No missing Enter/Exit handlers

**Runs as:** MSBuild task or pre-build event

### **State Dashboard**

**Technology:** WPF or Blazor  
**Features:**
- Visual state machine diagram
- Real-time state display
- Transition history viewer
- Invalid transition highlighter
- State health monitoring

**Use Case:** Development debugging tool

---

## ?? **REFERENCES**

**Design Documents:**
- `Documentation\Architecture\Registry.md` - Pattern definition
- `Documentation\Architecture\State-Machine-Design.md` - State machine architecture
- `Documentation\Architecture\State-Coordinator-Design.md` - Coordinator design

**Implementation:**
- Master-Task-List-v1_3_2_1.md - Step 24
- Phase-Tracking-v1_3_2_1.md - Phase 5 tracking
- Phase-5-Implementation-Log.md - Implementation log

**Related Patterns:**
- Pattern #1: State Machine Pattern
- Pattern #2: Satellite State Machine Pattern
- Pattern #3: State Coordinator Pattern
- Pattern #13: Deterministic Transition Pattern
- Pattern #14: Documentation-as-Architecture Pattern
- Pattern #15: State Registry Pattern (NEW!) ?

---

## ? **SUCCESS CRITERIA**

### **Phase 1 (v1.3.2.1):**
- [x] All state enums have UID Description attributes
- [x] StateChangedEventArgs has TransitionID property
- [x] Logs include UIDs and TransitionIDs
- [x] StateRegistry.yaml exists and is complete
- [x] State-Evolution-Log.md exists
- [x] Logs are grep-friendly
- [x] Debugging is 10x easier

### **Phase 2 (v1.4.0+):**
- [ ] Code generation from Registry.yaml works
- [ ] State validator runs at build time
- [ ] State dashboard visualizes state machines
- [ ] SSM template system generates new machines

---

## ?? **NEXT ACTIONS**

**Immediate (Phase 5 Step 24):**
1. Add Description attributes to all state enums
2. Add TransitionID to StateChangedEventArgs
3. Update GlobalStateMachine logging
4. Create StateRegistry.yaml
5. Create State-Evolution-Log.md

**Future (v1.4.0):**
1. Design code generator architecture
2. Implement YAML parser
3. Create T4 templates
4. Build state validator
5. Prototype state dashboard

---

**Last Updated:** 2026-01-17  
**Status:** Ready for Phase 5 Step 24 implementation
