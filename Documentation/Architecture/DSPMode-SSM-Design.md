# DSP Mode SSM - Design Document

**Version:** v1.4.0-beta  
**Date:** 2026-01-19  
**Purpose:** Control DSP enable/disable mode with proper state machine architecture  
**Status:** ? DESIGN COMPLETE - Ready for Implementation

---

## ?? **EXECUTIVE SUMMARY**

**What It Controls:**
- DSP enable/disable mode (not the DSP thread itself - that's DSPThreadSSM)
- DSP mode affects routing, monitoring, FFT, gain staging, tap points
- Validation of DSP mode changes
- Coordination with AudioRouting SSM and DSPThread SSM

**Why It's Needed:**
- DSP mode is a **MODE**, not a parameter
- Mode change requires pipeline reconfiguration
- Must validate: Cannot disable during recording
- Cognitive significance: DSP on/off is a major system mode
- Affects: Routing topology, monitoring, FFT analysis, gain processing

---

## ??? **SUBSYSTEM OWNERSHIP**

**Clear Ownership Boundaries:**
- ? **DSP Mode SSM owns DSP mode (enabled/disabled)**
- ? **DSPThreadSSM owns DSP thread lifecycle** (NOT mode)
- ? **AudioRouting SSM owns routing topology** (NOT DSP mode)
- ? **AudioPipelinePanel is an event emitter only** (no direct DSP control)
- ? **GlobalStateMachine is the authority for validation**
- ? **RecordingManagerSSM owns recording lifecycle** (NOT DSP mode)

**Critical Rules:**
- ? DSP Mode SSM must NOT start/stop DSP thread directly
- ? DSP Mode SSM must SIGNAL DSPThreadSSM to start/stop
- ? AudioPipelinePanel must NOT enable/disable DSP directly
- ? All DSP mode changes flow through DSP Mode SSM
- ? DSP Mode SSM is the decision-maker, DSPThreadSSM is the executor

**This clarifies that DSP Mode SSM is the DECISION-MAKER, not the executor.**

---

## ?? **CROSS-SSM INTERACTION MATRIX**

| Subsystem | Interaction | Purpose |
|-----------|-------------|---------|
| **DSPThreadSSM** | Starts/stops thread based on DSP mode | Execution |
| **AudioRouting SSM** | Reconfigures routing based on DSP mode | Coordination |
| **RecordingManagerSSM** | Blocks enabling DSP during recording | Validation |
| **GlobalStateMachine** | Provides validation context | Validation authority |
| **PlaybackSSM** | DSP can be enabled during playback | No blocking |
| **UIStateMachine** | Reflects DSP mode in UI | User feedback |
| **AudioPipelinePanel** | Receives checkbox events, emits requests | UI integration |

**Relationship Type:** DSP Mode SSM is a **decision-maker** that commands DSPThreadSSM but doesn't execute thread operations directly.

---

## ?? **STATE DIAGRAM**

```
UNINITIALIZED
    ? Initialize
DISABLED ? ENABLED
    ? Error
ERROR
    ? Recover
(back to DISABLED or ENABLED)
```

**Simple 3-state machine!**

---

## ?? **STATES**

### **1. DSPMODE_UNINITIALIZED**
**Description:** Initial state before DSP mode is determined

**Entry Actions:**
- None

**Exit Actions:**
- Determine initial DSP mode (default: DISABLED for safety)

**Valid Transitions:**
- ? DSPMODE_DISABLED (default startup)
- ? DSPMODE_ENABLED (if user preference saved)
- ? DSPMODE_ERROR (initialization failed)

**When This State Occurs:**
- Application startup (before DSP subsystem initialized)
- After catastrophic DSP failure requiring reset

---

### **2. DSPMODE_DISABLED**
**Description:** DSP processing is disabled - audio passes through unprocessed

**Entry Actions:**
- Signal DSPThreadSSM to stop DSP thread (if running)
- Signal AudioRouting SSM: "DSP disabled"
- Update AudioPipelinePanel: chkEnableDSP.Checked = False
- Log: "DSP Mode: DISABLED"
- Disable DSP-related UI controls (gain sliders, etc.)

**Exit Actions:**
- None (enabling DSP handled by next state's entry)

**Valid Transitions:**
- ? DSPMODE_ENABLED (user enables DSP, validation passes)
- ? DSPMODE_ERROR (system error)

**Validation Rules:**
- ? Always safe to DISABLE DSP (no restrictions)

**What This Means:**
- Audio flows directly: Input ? Output (no processing)
- Tap points still work (raw audio monitoring)
- Meters show input/output levels (no gain processing)
- FFT shows unprocessed audio
- Recording captures raw audio
- Playback plays raw audio

---

### **3. DSPMODE_ENABLED**
**Description:** DSP processing is active - audio goes through DSP pipeline

**Entry Actions:**
- Validate preconditions (see validation rules)
- Signal DSPThreadSSM to start DSP thread
- Signal AudioRouting SSM: "DSP enabled"
- Update AudioPipelinePanel: chkEnableDSP.Checked = True
- Log: "DSP Mode: ENABLED"
- Enable DSP-related UI controls (gain sliders, effects, etc.)
- Initialize DSP processors (GainProcessor, etc.)

**Exit Actions:**
- None (disabling DSP handled by DISABLED state's entry)

**Valid Transitions:**
- ? DSPMODE_DISABLED (user disables DSP, validation passes)
- ? DSPMODE_ERROR (DSP thread crash, processor failure)

**Validation Rules:**
- ? Cannot enable DSP if recording is active (safety: don't change processing mid-recording)
- ? Cannot enable DSP if AudioRouting SSM is in error state
- ? Can enable DSP while idle
- ? Can enable DSP during playback (applies DSP to playback)

**What This Means:**
- Audio flows through DSP: Input ? GainProcessor ? Other processors ? Output
- Tap points show processed audio
- Meters show gain-adjusted levels
- FFT shows processed audio
- Recording captures processed audio
- Playback plays through DSP

---

### **4. DSPMODE_ERROR**
**Description:** DSP initialization or operation failed

**Entry Actions:**
- Log error details
- Display error message to user
- Disable DSP operations
- Signal DSPThreadSSM to stop thread
- Signal AudioRouting SSM: "DSP error"

**Exit Actions:**
- Clear error state

**Valid Transitions:**
- ? DSPMODE_DISABLED (safe fallback)
- ? DSPMODE_ENABLED (retry initialization)
- ? DSPMODE_UNINITIALIZED (reset system)

**Validation Rules:**
- ? Always allowed (error recovery)

**Error Types:**
- DSP thread failed to start
- DSP processor initialization failed
- DSP processing exception
- Resource allocation failed

---

## ?? **TRANSITIONS**

### **T01: UNINITIALIZED ? DISABLED** (Default Startup)
**Trigger:** Application startup, default safe mode
**Validation:** None (initial state)
**Actions:**
1. Set DSP mode to DISABLED
2. Log: "DSP Mode: DISABLED (default)"
3. Update UI: chkEnableDSP.Checked = False
4. Signal AudioRouting SSM

---

### **T02: DISABLED ? ENABLED** (User Enables DSP)
**Trigger:** User checks "Enable DSP" checkbox in AudioPipelinePanel
**Validation:**
- ? GlobalStateMachine.CurrentState != Recording
- ? AudioRouting SSM not in error state
**Actions:**
1. Validate preconditions
2. Initialize DSP processors (GainProcessor, etc.)
3. Signal DSPThreadSSM to start thread
4. Wait for DSPThread initialization (max 500ms)
5. If successful:
   - Transition to DSPMODE_ENABLED
   - Update UI controls (enable gain sliders)
   - Log: "DSP Mode: ENABLED"
6. If failed:
   - Transition to DSPMODE_ERROR
   - Display error message
   - Revert checkbox

**On Validation Failure:**
- Log: "Cannot enable DSP: [reason]"
- Display MessageBox: "Cannot enable DSP while recording"
- Revert checkbox: chkEnableDSP.Checked = False
- Stay in DSPMODE_DISABLED

---

### **T03: ENABLED ? DISABLED** (User Disables DSP)
**Trigger:** User unchecks "Enable DSP" checkbox in AudioPipelinePanel
**Validation:** None (always safe to disable)
**Actions:**
1. Signal DSPThreadSSM to stop thread
2. Wait for DSPThread cleanup (max 500ms)
3. Transition to DSPMODE_DISABLED
4. Update UI controls (disable gain sliders)
5. Log: "DSP Mode: DISABLED"
6. Signal AudioRouting SSM

**No Validation Required:**
- ? Can disable DSP at any time (including during recording)
- If recording: Recording continues with raw audio
- If playing: Playback continues without DSP

---

### **T04: ENABLED ? ERROR** (DSP Failure)
**Trigger:** DSP thread crash, processor exception, resource failure
**Validation:** None (error condition)
**Actions:**
1. Detect DSP failure (thread crash, exception)
2. Capture error details (exception, stack trace)
3. Log error: "DSP Mode: ERROR - [details]"
4. Signal DSPThreadSSM to emergency stop
5. Display error to user
6. Transition to DSPMODE_ERROR
7. Update UI: chkEnableDSP.Checked = False
8. Suggest recovery: "DSP failed. Try disabling and re-enabling DSP."

---

### **T05: ERROR ? DISABLED** (Safe Fallback)
**Trigger:** User clicks "OK" on error dialog, or automatic fallback
**Validation:** None (always allow)
**Actions:**
1. Clear error state
2. Ensure DSP thread stopped
3. Transition to DSPMODE_DISABLED
4. Log: "DSP Mode: ERROR ? DISABLED (recovery)"

---

### **T06: ERROR ? ENABLED** (Retry)
**Trigger:** User attempts to re-enable DSP after error
**Validation:** Same as T02
**Actions:**
1. Clear error state
2. **Follow same actions as T02** (DISABLED ? ENABLED)
3. If successful: Log: "DSP Mode: ERROR ? ENABLED (retry successful)"
4. If fails again: Stay in ERROR, suggest system restart

---

## ?? **INTEGRATION POINTS**

### **1. AudioPipelinePanel (UI)**
**Current Code Location:** `AudioPipelinePanel.vb`

**Current Behavior:**
```visualbasic
Private Sub chkEnableDSP_CheckedChanged(sender As Object, e As EventArgs)
    ' Directly enables/disables DSP
    If chkEnableDSP.Checked Then
        ' Start DSP processing
    Else
        ' Stop DSP processing
    End If
End Sub
```

**New Behavior (Phase 7):**
```visualbasic
Private Sub chkEnableDSP_CheckedChanged(sender As Object, e As EventArgs)
    If suppressEvents Then Return
    
    ' Emit event - SSM handles validation and state transition
    RaiseEvent DSPModeChangeRequested(Me, chkEnableDSP.Checked)
End Sub

' SSM calls back with result
Private Sub OnDSPModeChangeCompleted(success As Boolean, enabled As Boolean, message As String)
    If Not success Then
        MessageBox.Show(message, "DSP Mode Change Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        ' Revert checkbox
        suppressEvents = True
        chkEnableDSP.Checked = Not enabled
        suppressEvents = False
    Else
        ' Update UI controls based on DSP mode
        EnableDSPControls(enabled)
    End If
End Sub

Private Sub EnableDSPControls(enabled As Boolean)
    ' Enable/disable gain sliders, effect controls, etc.
    sldInputGain.Enabled = enabled
    sldOutputGain.Enabled = enabled
    ' ... other DSP controls ...
End Sub
```

---

### **2. DSPThreadSSM**
**Wire:** DSP Mode SSM ? DSPThreadSSM

**Coordination:**
```visualbasic
' In DSP Mode SSM
Private Sub OnDSPModeChanged(enabled As Boolean)
    If enabled Then
        ' Signal DSPThreadSSM to start
        DSPThreadSSM.RequestStart()
    Else
        ' Signal DSPThreadSSM to stop
        DSPThreadSSM.RequestStop()
    End If
End Sub
```

**DSPThreadSSM responds:**
```visualbasic
' In DSPThreadSSM
Public Sub RequestStart()
    ' Validate and start DSP thread
    If CanStart() Then
        TransitionTo(DSPThreadState.Running)
    Else
        ' Notify DSP Mode SSM of failure
        DSPModeSSM.NotifyStartFailed("Thread start validation failed")
    End If
End Sub
```

---

### **3. AudioRouting SSM**
**Wire:** DSP Mode SSM ? AudioRouting SSM

**Notification:**
```visualbasic
' In DSP Mode SSM
Private Sub OnDSPModeChanged(enabled As Boolean)
    ' Notify AudioRouting SSM
    AudioRoutingSSM.NotifyDSPModeChanged(enabled)
End Sub
```

**AudioRouting SSM uses this information:**
- If DSP disabled: Route audio directly (Input ? Output)
- If DSP enabled: Route audio through DSP pipeline (Input ? DSP ? Output)
- Update tap point wiring accordingly

---

### **4. GlobalStateMachine**
**Wire:** DSP Mode SSM subscribes to GlobalStateMachine.StateChanged

**Validation:**
```visualbasic
Private Function CanEnableDSP() As Boolean
    Dim globalState = StateCoordinator.Instance.GlobalStateMachine.CurrentState
    
    Select Case globalState
        Case GlobalState.Recording
            Return False  ' Cannot enable DSP during recording (safety)
        Case GlobalState.Idle, GlobalState.Playing
            Return True  ' Safe to enable DSP
        Case Else
            Return False  ' Conservative - deny unknown states
    End Select
End Function
```

**Why Can't Enable During Recording?**
- Safety: Don't change audio processing mid-recording
- Consistency: Recording should use same processing throughout
- Cognitive: User expects consistent recording parameters

**Why CAN Enable During Playback?**
- Non-destructive: Playback is real-time, not capturing
- User might want to hear processed vs unprocessed
- Allows DSP experimentation during playback

---

### **5. RecordingManagerSSM**
**Wire:** DSP Mode SSM checks RecordingManagerSSM state

**Additional Validation:**
```visualbasic
Private Function CanEnableDSP() As Boolean
    ' Check GlobalStateMachine first
    If Not ValidateGlobalState() Then Return False
    
    ' Check if actively recording
    Dim recSSM = StateCoordinator.Instance.RecordingManagerSSM
    If recSSM IsNot Nothing AndAlso recSSM.CurrentState = RecordingManagerState.Recording Then
        Return False  ' Cannot enable DSP while recording
    End If
    
    Return True
End Function
```

---

## ?? **IMPLEMENTATION CHECKLIST**

### **Phase 7.1: Design (Current Step)**
- [x] Define states
- [x] Define transitions
- [x] Define validation rules
- [x] Identify integration points
- [x] Define coordination with DSPThreadSSM
- [x] Define coordination with AudioRouting SSM
- [ ] Review design with Rick
- [ ] Update StateRegistry.yaml

### **Phase 7.2: Implementation**
- [ ] Create `State/DSPModeSSM.vb`
- [ ] Implement IStateMachine interface
- [ ] Wire to GlobalStateMachine
- [ ] Wire to DSPThreadSSM (control thread lifecycle)
- [ ] Wire to AudioRouting SSM (notify mode changes)
- [ ] Wire to RecordingManagerSSM (validation)
- [ ] Update AudioPipelinePanel (event emitter)
- [ ] Test DSP enable/disable with validation

### **Phase 7.3: Testing**
- [ ] Test DSP enable on startup (default: disabled)
- [ ] Test DSP enable/disable (idle state)
- [ ] Test DSP enable blocked during recording
- [ ] Test DSP disable during recording (should work)
- [ ] Test DSP enable during playback (should work)
- [ ] Test ERROR state (simulate DSP thread crash)
- [ ] Test recovery from ERROR
- [ ] Test cognitive introspection

---

## ?? **SUCCESS CRITERIA**

**Complete when:**
- ? DSP Mode SSM controls all DSP enable/disable operations
- ? AudioPipelinePanel emits events (not direct control)
- ? Validation prevents enabling DSP during recording
- ? DSP can be disabled at any time (no restrictions)
- ? DSPThreadSSM starts/stops based on DSP mode
- ? AudioRouting SSM adjusts routing based on DSP mode
- ? Error states handled gracefully
- ? Cognitive layer can introspect DSP mode
- ? All transitions logged with TransitionID

---

## ?? **CODE STRUCTURE**

**File:** `State/DSPModeSSM.vb`

**Structure:**
```visualbasic
Namespace State

    ''' <summary>
    ''' Controls DSP enable/disable mode
    ''' Coordinates with DSPThreadSSM for thread lifecycle
    ''' Coordinates with AudioRouting SSM for routing changes
    ''' </summary>
    Public Class DSPModeSSM
        Implements IStateMachine(Of DSPModeState)

        ' States
        Public Enum DSPModeState
            Uninitialized = 0
            Disabled = 1
            Enabled = 2
            [Error] = 99
        End Enum

        ' Properties
        Public ReadOnly Property CurrentState As DSPModeState Implements IStateMachine.CurrentState
        Public ReadOnly Property IsDSPEnabled As Boolean  ' Convenience property

        ' Methods
        Public Function RequestDSPModeChange(enable As Boolean) As Boolean
        Private Function ValidateDSPEnable() As Boolean
        Private Sub OnDSPModeChanged(enabled As Boolean)
        
        ' Transitions
        Private Sub TransitionTo(newState As DSPModeState, reason As String)
        
        ' Integration
        Private Sub NotifyDSPThreadSSM(start As Boolean)
        Private Sub NotifyAudioRoutingSSM(enabled As Boolean)
        Private Sub SubscribeToGlobalStateMachine()
        
        ' Error Handling
        Public Sub NotifyDSPFailure(error As String)
    End Class

End Namespace
```

---

## ?? **COORDINATION EXAMPLE: Enable DSP**

**Scenario:** User enables DSP while idle

**State Flow:**
```
User: Checks "Enable DSP" checkbox in AudioPipelinePanel
    ?
AudioPipelinePanel: RaiseEvent DSPModeChangeRequested(True)
    ?
DSP Mode SSM: Receives enable request
    ?
DSP Mode SSM: Validates (GlobalState = Idle ?, Not Recording ?)
    ?
DSP Mode SSM: DISABLED ? ENABLED
    ?
DSP Mode SSM: Calls DSPThreadSSM.RequestStart()
    ?
DSPThreadSSM: Validates and starts thread
    ?
DSPThreadSSM: STOPPED ? RUNNING
    ?
DSPThreadSSM: Signals success back to DSP Mode SSM
    ?
DSP Mode SSM: Calls AudioRoutingSSM.NotifyDSPModeChanged(True)
    ?
AudioRouting SSM: Updates routing (Input ? DSP ? Output)
    ?
DSP Mode SSM: Calls AudioPipelinePanel.OnDSPModeChangeCompleted(True)
    ?
AudioPipelinePanel: Enables DSP controls (gain sliders, etc.)
```

**Logs:**
```
[AudioPipelinePanel] User checked Enable DSP
[DSP Mode SSM] Validating DSP enable request...
[DSP Mode SSM] Validation passed: GlobalState=Idle, Not Recording
[DSP Mode SSM] Transition: DISABLED ? ENABLED (User enabled DSP)
[DSP Mode SSM] Requesting DSPThreadSSM to start...
[DSPThreadSSM] Start request received
[DSPThreadSSM] Transition: STOPPED ? RUNNING (DSP Mode enabled)
[DSPThread] Thread started successfully
[DSP Mode SSM] DSPThreadSSM started successfully
[DSP Mode SSM] Notifying AudioRouting SSM: DSP enabled
[AudioRouting SSM] DSP mode changed: DISABLED ? ENABLED
[AudioRouting SSM] Updating routing topology for DSP processing
[AudioPipelinePanel] DSP mode changed successfully, enabling controls
```

---

## ?? **IMPORTANT DESIGN DECISIONS**

### **1. Why Can't Enable DSP During Recording?**
**Safety and Consistency:**
- Changing processing mid-recording creates inconsistent audio
- User expects consistent recording parameters throughout
- Prevents "oops, I changed settings mid-recording" scenarios

**Alternative (Rejected):**
- Allow DSP enable during recording but log warning
- **Problem:** Creates cognitive confusion, inconsistent files

**Chosen Solution:**
- Block enable, show clear message: "Cannot enable DSP while recording"
- User can enable DSP, THEN start recording

---

### **2. Why CAN Disable DSP During Recording?**
**Non-Destructive:**
- Disabling DSP just passes audio through unprocessed
- User might realize they don't want processing mid-recording
- Emergency: "Oops, I don't want DSP, disable it now!"

**Behavior:**
- Before disable: Audio processed (e.g., with gain boost)
- After disable: Audio raw (no processing)
- Result: Recording file has mixed content (first part processed, second part raw)
- **User responsibility:** They explicitly disabled it, they know what they did

---

### **3. Why CAN Enable DSP During Playback?**
**Real-Time Experimentation:**
- Playback is real-time, not capturing to file
- User might want to hear difference between processed/unprocessed
- Allows DSP experimentation: "How does this sound with gain boost?"

**Behavior:**
- Enable DSP: Playback immediately goes through DSP pipeline
- Disable DSP: Playback immediately bypasses DSP
- No data loss, no file corruption, just real-time audio routing

---

## ?? **RELATIONSHIP TO DSPThreadSSM**

**IMPORTANT:** DSP Mode SSM and DSPThreadSSM are SEPARATE!

**DSP Mode SSM:**
- Controls: WHAT mode (enabled/disabled)
- Scope: User-facing mode selection
- Lifecycle: Application-wide setting
- Validates: User requests, global state

**DSPThreadSSM:**
- Controls: Thread lifecycle (stopped/starting/running/stopping)
- Scope: Thread management, resource allocation
- Lifecycle: Started/stopped by DSP Mode SSM
- Validates: Thread state, resource availability

**Coordination:**
```
DSP Mode SSM = "The Boss" (decides mode)
DSPThreadSSM = "The Worker" (manages thread)

DSP Mode SSM: "Start DSP processing"
    ?
DSPThreadSSM: "Yes sir, starting thread..."
```

**Both Subscribe to GlobalStateMachine:**
- DSP Mode SSM: For validation (can't enable during recording)
- DSPThreadSSM: For lifecycle (stop thread when application shuts down)

---

**Status:** ?? Design Complete ? Ready for Review  
**Next Step:** Get Rick's approval, then move to AudioRouting SSM design (the big one!)

