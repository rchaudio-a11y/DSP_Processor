# SSM Expansion Roadmap - The Four Future State Machines
**Date:** 2026-01-19  
**Version:** 1.0.0  
**Status:** Planning Document  
**Target:** v1.4.0 - v1.6.0

---

## ?? **EXECUTIVE SUMMARY**

This document specifies 4 additional Satellite State Machines (SSMs) required to complete the DSP Processor state machine architecture.

**Current Status:**
- ? 5 SSMs implemented (GSM, REC, DSP, PLAY, UI)
- ? 4 modeful subsystems as UI state

**Target Status:**
- ? 9 SSMs total
- ? ALL modeful subsystems controlled
- ? Complete state machine coverage

**Discovered:** RDF Phase 4 analysis (2026-01-19)  
**Specification:** SYSTEM ARCHITECTURE REPORT.md  
**Implementation:** v1.4.0 - v1.6.0

---

## ?? **THE 4 REQUIRED SSMs:**

| SSM | Purpose | States | Priority | Target Version |
|-----|---------|--------|----------|----------------|
| **AudioDevice SSM** | Driver backend control | 5 | HIGH | v1.4.0 |
| **AudioInput SSM** | Physical device selection | 4 | HIGH | v1.4.0 |
| **DSP Mode SSM** | DSP enable/disable | 3 | MEDIUM | v1.5.0 |
| **AudioRouting SSM** | Routing topology | 5+ | LOW | v1.6.0 |

**Total Estimated Time:** 16-24 hours

---

## ??? **SSM #1: AudioDevice SSM**

### **Purpose:**
Controls the audio driver backend (WASAPI, ASIO, DirectSound).

### **Why It Must Exist:**
- Driver selection is **exclusive** (only one active at a time)
- Switching requires **teardown + reinit** of entire audio engine
- Driver changes affect device enumeration, buffer sizes, sample rates
- Must validate (cannot switch during recording/playback)
- Must log (grep-friendly, cognitive-aware)

### **States:**
```visualbasic
Public Enum AudioDeviceState
    <Description("AUDIODEV_IDLE")>
    Idle = 0  ' No driver selected
    
    <Description("AUDIODEV_WASAPI")>
    WASAPI = 1  ' Using WASAPI driver
    
    <Description("AUDIODEV_ASIO")>
    ASIO = 2  ' Using ASIO driver
    
    <Description("AUDIODEV_DIRECTSOUND")>
    DirectSound = 3  ' Using DirectSound driver
    
    <Description("AUDIODEV_ERROR")>
    [Error] = 4  ' Driver initialization failed
End Enum
```

### **Transitions:**
```
Idle ? WASAPI (User selects WASAPI)
Idle ? ASIO (User selects ASIO)
Idle ? DirectSound (User selects DirectSound)
WASAPI ? Idle (Teardown before switch)
ASIO ? Idle (Teardown before switch)
DirectSound ? Idle (Teardown before switch)
WASAPI ? ASIO (via Idle intermediate state)
* ? Error (Driver init failed)
Error ? Idle (Recovery)
```

### **Validation Rules:**
```visualbasic
Public Function IsValidTransition(fromState As AudioDeviceState, toState As AudioDeviceState) As Boolean
    ' Cannot switch driver during recording
    If StateCoordinator.Instance.GlobalStateMachine.CurrentState = GlobalState.Recording Then
        Return False
    End If
    
    ' Cannot switch driver during playback
    If StateCoordinator.Instance.GlobalStateMachine.CurrentState = GlobalState.Playing Then
        Return False
    End If
    
    ' Same state = no-op
    If fromState = toState Then Return True
    
    Select Case fromState
        Case AudioDeviceState.Idle
            ' Can transition to any driver
            Return toState <> AudioDeviceState.Idle
            
        Case AudioDeviceState.WASAPI, AudioDeviceState.ASIO, AudioDeviceState.DirectSound
            ' Must go to Idle first (teardown)
            Return toState = AudioDeviceState.Idle OrElse toState = AudioDeviceState.Error
            
        Case AudioDeviceState.Error
            ' Can recover to Idle
            Return toState = AudioDeviceState.Idle
    End Select
End Function
```

### **Implementation:**
1. Create `AudioDeviceSSM.vb`
2. Wire to `AudioSettingsPanel` (driver dropdown)
3. Wire to `AudioInputManager` (device enumeration)
4. Wire to `GlobalStateMachine` (validation)
5. Add Description attributes
6. Add to StateRegistry.yaml
7. Test driver switching

**Estimated Time:** 4-6 hours

---

## ??? **SSM #2: AudioInput SSM**

### **Purpose:**
Controls physical input device selection (Scarlett, Realtek, USB mic, etc.).

### **Why It Must Exist:**
- Device selection is **exclusive**
- Device changes require stream teardown + reinit
- Device availability can change dynamically (USB unplugged)
- Must validate (cannot switch during recording)
- Must log (cognitive-aware)

### **States:**
```visualbasic
Public Enum AudioInputState
    <Description("AUDIOIN_UNINITIALIZED")>
    Uninitialized = 0  ' No device selected
    
    <Description("AUDIOIN_DEVICE_SELECTED")>
    DeviceSelected = 1  ' Device active
    
    <Description("AUDIOIN_DEVICE_UNAVAILABLE")>
    DeviceUnavailable = 2  ' Device unplugged/unavailable
    
    <Description("AUDIOIN_ERROR")>
    [Error] = 3  ' Device initialization failed
End Enum
```

### **Transitions:**
```
Uninitialized ? DeviceSelected (User selects device)
DeviceSelected ? Uninitialized (User deselects)
DeviceSelected ? DeviceUnavailable (USB unplugged)
DeviceUnavailable ? DeviceSelected (USB replugged)
* ? Error (Device init failed)
Error ? Uninitialized (Recovery)
```

### **Validation Rules:**
```visualbasic
Public Function IsValidTransition(fromState As AudioInputState, toState As AudioInputState) As Boolean
    ' Cannot switch device during recording
    If StateCoordinator.Instance.GlobalStateMachine.CurrentState = GlobalState.Recording Then
        Return False
    End If
    
    ' Allow dynamic device unavailability (USB unplug)
    If toState = AudioInputState.DeviceUnavailable Then Return True
    
    ' Same state = no-op
    If fromState = toState Then Return True
    
    Select Case fromState
        Case AudioInputState.Uninitialized
            Return toState = AudioInputState.DeviceSelected OrElse toState = AudioInputState.Error
            
        Case AudioInputState.DeviceSelected
            Return toState = AudioInputState.Uninitialized OrElse 
                   toState = AudioInputState.DeviceUnavailable OrElse 
                   toState = AudioInputState.Error
            
        Case AudioInputState.DeviceUnavailable
            Return toState = AudioInputState.DeviceSelected OrElse toState = AudioInputState.Error
            
        Case AudioInputState.Error
            Return toState = AudioInputState.Uninitialized
    End Select
End Function
```

### **Implementation:**
1. Create `AudioInputSSM.vb`
2. Wire to `AudioSettingsPanel` (device dropdown)
3. Wire to `AudioInputManager` (device enumeration)
4. Wire to `AudioDevice SSM` (driver changes)
5. Wire to `GlobalStateMachine` (validation)
6. Add USB hot-plug detection
7. Test device switching

**Estimated Time:** 4-6 hours

---

## ??? **SSM #3: DSP Mode SSM**

### **Purpose:**
Controls DSP processing pipeline mode (enabled vs disabled).

**Note:** This is NOT DSPThreadSSM (worker thread). This controls whether DSP effects are active in the signal path.

### **Why It Must Exist:**
- DSP mode is **exclusive** (enabled OR disabled)
- DSP mode affects routing, monitoring, FFT, gain, recording, playback
- Must validate (cannot disable DSP mid-recording)
- Must log (cognitive significance)

### **States:**
```visualbasic
Public Enum DSPModeState
    <Description("DSPMODE_DISABLED")>
    Disabled = 0  ' DSP bypassed
    
    <Description("DSPMODE_ENABLED")>
    Enabled = 1  ' DSP active
    
    <Description("DSPMODE_ERROR")>
    [Error] = 2  ' DSP initialization failed
End Enum
```

### **Transitions:**
```
Disabled ? Enabled (User enables DSP)
Enabled ? Disabled (User disables DSP)
* ? Error (DSP init failed)
Error ? Disabled (Recovery)
```

### **Validation Rules:**
```visualbasic
Public Function IsValidTransition(fromState As DSPModeState, toState As DSPModeState) As Boolean
    ' Cannot disable DSP during recording (recording requires DSP)
    If fromState = DSPModeState.Enabled AndAlso toState = DSPModeState.Disabled Then
        If StateCoordinator.Instance.GlobalStateMachine.CurrentState = GlobalState.Recording Then
            Return False
        End If
    End If
    
    ' Same state = no-op
    If fromState = toState Then Return True
    
    Select Case fromState
        Case DSPModeState.Disabled
            Return toState = DSPModeState.Enabled OrElse toState = DSPModeState.Error
            
        Case DSPModeState.Enabled
            Return toState = DSPModeState.Disabled OrElse toState = DSPModeState.Error
            
        Case DSPModeState.Error
            Return toState = DSPModeState.Disabled
    End Select
End Function
```

### **Integration with DSPThreadSSM:**
```visualbasic
' In DSP Mode SSM OnStateEntering:
Select Case newState
    Case DSPModeState.Enabled
        ' Start DSPThread if not running
        If DSPThreadSSM.CurrentState = DSPThreadState.Idle Then
            DSPThreadSSM.TransitionTo(DSPThreadState.Running, "DSP Mode enabled")
        End If
        
    Case DSPModeState.Disabled
        ' Stop DSPThread if running (unless recording)
        If DSPThreadSSM.CurrentState = DSPThreadState.Running Then
            If GlobalStateMachine.CurrentState <> GlobalState.Recording Then
                DSPThreadSSM.TransitionTo(DSPThreadState.Stopping, "DSP Mode disabled")
            End If
        End If
End Select
```

### **Implementation:**
1. Create `DSPModeSSM.vb`
2. Wire to `AudioPipelinePanel` (`chkEnableDSP`)
3. Wire to `DSPThreadSSM` (start/stop worker)
4. Wire to `GlobalStateMachine` (validation)
5. Update routing logic
6. Test DSP enable/disable

**Estimated Time:** 3-4 hours

---

## ??? **SSM #4: AudioRouting SSM (COMPLEX)**

### **Purpose:**
Controls the routing topology of the entire audio system.

### **Why It Must Exist:**
Routing is the MOST COMPLEX modeful subsystem:
- Multiple exclusive routing paths
- Cross-subsystem dependencies (DSP, Recording, Playback, Monitoring, FFT)
- Side effects on ALL audio subsystems
- Validation rules (cannot change during recording)
- Cognitive significance (users care deeply about signal flow)

### **Current Problem:**
Routing is currently scattered across:
- RoutingPanel (UI)
- AudioPipelinePanel (UI)
- AudioRouter (implementation)
- RecordingManager (input routing)
- Playback logic (file routing)

**This is architectural debt that must be paid.**

### **Proposed States:**
```visualbasic
Public Enum AudioRoutingState
    <Description("ROUTING_UNINITIALIZED")>
    Uninitialized = 0
    
    <Description("ROUTING_MIC_DIRECT_OUTPUT")>
    MicDirectOutput = 1  ' Mic ? Output (no DSP)
    
    <Description("ROUTING_MIC_DSP_OUTPUT")>
    MicDSPOutput = 2  ' Mic ? DSP ? Output
    
    <Description("ROUTING_FILE_DIRECT_OUTPUT")>
    FileDirectOutput = 3  ' File ? Output (no DSP)
    
    <Description("ROUTING_FILE_DSP_OUTPUT")>
    FileDSPOutput = 4  ' File ? DSP ? Output
    
    <Description("ROUTING_MIC_DSP_RECORDING")>
    MicDSPRecording = 5  ' Mic ? DSP ? Recording + Monitoring
    
    <Description("ROUTING_ERROR")>
    [Error] = 6
End Enum
```

### **Transitions (Complex):**
This SSM requires careful design because routing affects:
- AudioInput SSM (mic vs file)
- DSP Mode SSM (DSP enabled/disabled)
- RecordingManagerSSM (recording active)
- PlaybackSSM (playback active)
- Monitoring (meters/FFT)

**Each routing change must:**
1. Validate against current GSM state
2. Teardown old routing
3. Initialize new routing
4. Update all dependent subsystems
5. Log transition

### **Implementation Strategy:**
This is TOO COMPLEX for immediate implementation.

**Recommended Approach:**
1. **v1.6.0 - Design Phase:** Create comprehensive routing design document
2. **v1.6.1 - Refactor:** Extract routing logic from AudioRouter into RoutingEngine
3. **v1.6.2 - SSM:** Implement AudioRoutingSSM
4. **v1.6.3 - Integration:** Wire to all subsystems
5. **v1.6.4 - Testing:** Extensive validation

**Estimated Time:** 8-12 hours (complex refactor)

---

## ?? **IMPLEMENTATION TIMELINE**

### **v1.4.0: Foundation SSMs (8-10 hours)**
- AudioDevice SSM (4-6 hours)
- AudioInput SSM (4-6 hours)
- Update cognitive layer
- Testing & validation

**Priority:** HIGH  
**Why:** Driver and device management are critical pain points

---

### **v1.5.0: Mode Control (3-4 hours)**
- DSP Mode SSM (3-4 hours)
- Integration with DSPThreadSSM
- Testing & validation

**Priority:** MEDIUM  
**Why:** DSP enable/disable affects many subsystems

---

### **v1.6.0: Routing Architecture (8-12 hours)**
- Design comprehensive routing spec
- Refactor AudioRouter
- Implement AudioRoutingSSM
- Integration & testing

**Priority:** LOW (but important)  
**Why:** Complex refactor, requires careful planning

---

## ? **ACCEPTANCE CRITERIA**

**For Each SSM:**
- [ ] Enum with Description attributes
- [ ] GetStateUID() method
- [ ] TransitionTo() with validation
- [ ] IsValidTransition() implementation
- [ ] Event firing with TransitionIDs
- [ ] Wired to UI components
- [ ] Wired to GlobalStateMachine
- [ ] Added to StateRegistry.yaml
- [ ] Added to State-Evolution-Log.md
- [ ] Cognitive layer tracks transitions
- [ ] Compiles and builds
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing complete

---

## ?? **BENEFITS (When Complete)**

**Current (5 SSMs):**
- GlobalStateMachine
- RecordingManagerSSM
- DSPThreadSSM
- PlaybackSSM
- UIStateMachine

**Some modeful subsystems as UI state** ?

**Future (9 SSMs):**
- GlobalStateMachine
- RecordingManagerSSM
- DSPThreadSSM
- PlaybackSSM
- UIStateMachine
- **AudioDeviceSSM** ?
- **AudioInputSSM** ?
- **DSPModeSSM** ?
- **AudioRoutingSSM** ?

**ALL modeful subsystems controlled** ?

**Benefits:**
- ? Complete state machine coverage
- ? ALL modes logged and searchable
- ? Invalid transitions prevented
- ? Cognitive layer tracks everything
- ? Debugging trivial
- ? Testing easier
- ? Architecture complete

---

## ?? **REFERENCES**

**Design Documents:**
- SYSTEM ARCHITECTURE REPORT.md - Original discovery
- State-Evolution-Log.md - Future SSMs section
- State-Machine-Design.md - Core architecture
- Satellite-State-Machines.md - SSM patterns

**Current Implementation:**
- GlobalStateMachine.vb - Reference implementation
- IStateMachine.vb - Interface
- StateCoordinator.vb - Coordinator pattern

**Registry:**
- StateRegistry.yaml - Add new SSMs here
- Logging-Standards.md - Logging format

---

**Created:** 2026-01-19  
**Author:** Rick + GitHub Copilot  
**Status:** Planning Document - Implementation starts v1.4.0

**This roadmap completes the state machine architecture!**
