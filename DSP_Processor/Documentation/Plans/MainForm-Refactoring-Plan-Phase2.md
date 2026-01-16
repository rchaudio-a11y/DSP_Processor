# MainForm Refactoring Plan - Phase 2 (Pre-DSP)

**Date:** January 15, 2026  
**Status:** ?? **PLANNING**  
**Priority:** ?? **HIGH** (Must complete before DSP implementation)

---

## ?? **Executive Summary**

MainForm has become a "god object" handling:
- UI updates
- Audio coordination
- Pipeline routing
- Playback control
- Recording management
- Settings application
- FFT processing
- Event wiring
- Timer management

**Goal:** Extract responsibilities into proper coordinators/managers, making MainForm a thin UI layer that only handles:
- UI event wiring
- UI updates
- Form lifecycle

---

## ?? **Current Problems**

### **1. Too Many Responsibilities**
MainForm currently has ~1500+ lines handling:
- 8 different subsystems
- 15+ timers and events
- 20+ event handlers
- Complex state management

### **2. Poor Testability**
- Cannot test audio logic without UI
- Cannot test playback without MainForm
- Cannot reuse coordinators

### **3. Tight Coupling**
- MainForm knows implementation details
- Hard to change one thing without affecting others
- Difficult to add new features

### **4. Hard to Maintain**
- Finding code is difficult
- Changes require touching MainForm
- High risk of breaking things

---

## ?? **Target Architecture**

### **After Refactoring:**

```
??????????????????????????????????????????????????????????????
?                      MainForm (Thin UI Layer)              ?
?  - Wires events                                            ?
?  - Updates labels/LEDs                                     ?
?  - Handles form lifecycle                                  ?
??????????????????????????????????????????????????????????????
         ?                       ?
         ?                       ?
????????????????????    ????????????????????
? PlaybackCoordinator?  ? RecordingCoordinator?
? - Start/Stop      ?    ? - Arm/Disarm      ?
? - Position track  ?    ? - Start/Stop rec  ?
? - Timer mgmt      ?    ? - Time tracking   ?
??????????????????????    ?????????????????????
         ?                         ?
         ?                         ?
???????????????????????????????????????????
?         AudioSystemController           ?
?  - Device management                    ?
?  - Routing coordination                 ?
?  - Settings application                 ?
???????????????????????????????????????????
         ?
         ?
???????????????????  ????????????????  ??????????????
?   AudioRouter   ?  ? PipelineRouter?  ? FFTMonitor ?
?   (DSP)         ?  ? (Routing)     ?  ? (Analysis) ?
???????????????????  ????????????????  ??????????????
```

---

## ?? **Refactoring Tasks**

**IMPORTANT:** See [MainForm-Refactoring-Task-List-2026-01-15.md](../Tasks/MainForm-Refactoring-Task-List-2026-01-15.md) for detailed step-by-step task breakdown.

### **Phase 2.0: Delegation Pass (Week 1)** ? NEW! START HERE!

**Strategy:** Move responsibilities from MainForm into **existing** files BEFORE creating new coordinators.

#### **Why This Phase Matters:**
Many responsibilities in MainForm already have a natural home in existing classes. We should delegate to what already exists before creating new files.

**Tasks:**
1. **Task 2.0.1:** Move recording lifecycle to RecordingManager (2-3 hours)
2. **Task 2.0.2:** Move playback lifecycle to AudioRouter (3-4 hours)
3. **Task 2.0.3:** Move buffer routing to AudioPipelineRouter (2-3 hours)
4. **Task 2.0.4:** Move FFT management to SpectrumManager (2-3 hours)
5. **Task 2.0.5:** Move settings application to SettingsManager (2-3 hours)
6. **Task 2.0.6:** Move file operations to FileManager (1-2 hours)
7. **Task 2.0.7:** Make TransportControl self-contained (2-3 hours)
8. **Task 2.0.8:** Clean up timer logic (1-2 hours)

**Total:** 15-21 hours

---

### **Phase 2.1: Create Coordinators (Week 2)**

#### **Task 2.1.1: Create PlaybackCoordinator**
**Priority:** ?? **CRITICAL**  
**Estimated Time:** 4-6 hours

**What it does:**
- Wraps AudioRouter and PlaybackManager
- Provides unified playback interface
- Manages playback state and timing
- Handles microphone disarm/re-arm during playback

**Responsibilities:**
```visualbasic
Public Class PlaybackCoordinator
    ' Events
    Public Event PlaybackStarted As EventHandler(Of String)
    Public Event PlaybackStopped As EventHandler
    Public Event PositionChanged As EventHandler(Of TimeSpan)
    
    ' Properties
    Public ReadOnly Property IsPlaying As Boolean
    Public ReadOnly Property CurrentPosition As TimeSpan
    Public ReadOnly Property TotalDuration As TimeSpan
    
    ' Methods
    Public Sub Play(filepath As String, useDSP As Boolean)
    Public Sub [Stop]()
    Public Sub Seek(position As TimeSpan)
    Public Sub UpdatePosition() ' Called from timer
End Class
```

**Moves from MainForm:**
- ? `OnAudioRouterPlaybackStarted()`
- ? `OnAudioRouterPlaybackStopped()`
- ? `OnAudioRouterPositionChanged()`
- ? Microphone disarm logic during playback
- ? Microphone re-arm logic after playback
- ? TimerPlayback management (start/stop)
- ? PlaybackManager + AudioRouter coordination

**Files to Create:**
- `Managers\PlaybackCoordinator.vb`

**Files to Modify:**
- `MainForm.vb` (remove playback logic)
- `MainForm.Designer.vb` (timer might move)

---

#### **Task 2.1.2: Create RecordingCoordinator**
**Priority:** ?? **HIGH**  
**Estimated Time:** 3-4 hours

**What it does:**
- Wraps RecordingManager
- Manages mic arming state
- Handles device switching
- Coordinates recording lifecycle

**Responsibilities:**
```visualbasic
Public Class RecordingCoordinator
    ' Events
    Public Event RecordingStarted As EventHandler
    Public Event RecordingStopped As EventHandler
    Public Event TimeUpdated As EventHandler(Of TimeSpan)
    Public Event MicrophoneArmed As EventHandler
    Public Event MicrophoneDisarmed As EventHandler
    
    ' Properties
    Public ReadOnly Property IsRecording As Boolean
    Public ReadOnly Property IsMicArmed As Boolean
    Public ReadOnly Property RecordingDuration As TimeSpan
    
    ' Methods
    Public Sub ArmMicrophone()
    Public Sub DisarmMicrophone()
    Public Sub StartRecording()
    Public Sub StopRecording()
End Class
```

**Moves from MainForm:**
- ? Microphone arming/disarming logic
- ? Device switching coordination
- ? Recording state management

**Files to Create:**
- `Managers\RecordingCoordinator.vb`

**Files to Modify:**
- `MainForm.vb` (remove recording logic)

---

#### **Task 2.1.3: Create AudioSystemController**
**Priority:** ?? **MEDIUM**  
**Estimated Time:** 4-6 hours

**What it does:**
- High-level audio system management
- Device enumeration and selection
- Settings application to all subsystems
- System-wide state coordination

**Responsibilities:**
```visualbasic
Public Class AudioSystemController
    ' Events
    Public Event InputDeviceChanged As EventHandler(Of Integer)
    Public Event OutputDeviceChanged As EventHandler(Of Integer)
    Public Event SettingsApplied As EventHandler
    
    ' Properties
    Public ReadOnly Property InputDevices As String()
    Public ReadOnly Property OutputDevices As String()
    Public Property SelectedInputDevice As Integer
    Public Property SelectedOutputDevice As Integer
    
    ' Methods
    Public Sub Initialize()
    Public Sub ApplySettings(settings As ApplicationSettings)
    Public Sub RefreshDevices()
    Public Sub Shutdown()
End Class
```

**Moves from MainForm:**
- ? Device enumeration
- ? Settings application logic
- ? System initialization
- ? System shutdown

**Files to Create:**
- `Managers\AudioSystemController.vb`

**Files to Modify:**
- `MainForm.vb` (remove system control logic)

---

### **Phase 2.2: Extract Specialized Managers (Week 2)**

#### **Task 2.2.1: Create FFTCoordinator**
**Priority:** ?? **MEDIUM**  
**Estimated Time:** 3-4 hours

**What it does:**
- Manages FFT processors (input/output)
- Handles spectrum settings
- Coordinates FFT updates
- Manages spectrum displays

**Responsibilities:**
```visualbasic
Public Class FFTCoordinator
    ' Events
    Public Event InputSpectrumUpdated As EventHandler(Of Single())
    Public Event OutputSpectrumUpdated As EventHandler(Of Single())
    
    ' Properties
    Public Property FFTSize As Integer
    Public Property WindowFunction As WindowType
    
    ' Methods
    Public Sub Initialize(sampleRate As Integer)
    Public Sub ProcessInputSamples(buffer As Byte(), count As Integer)
    Public Sub ProcessOutputSamples(buffer As Byte(), count As Integer)
    Public Sub Clear()
    Public Sub ApplySettings(settings As SpectrumSettings)
End Class
```

**Moves from MainForm:**
- ? FFT processor management
- ? Spectrum clearing
- ? Spectrum settings application
- ? `OnDSPInputSamples()`
- ? `OnDSPOutputSamples()`

**Files to Create:**
- `Managers\FFTCoordinator.vb`

**Files to Modify:**
- `MainForm.vb` (remove FFT logic)

---

#### **Task 2.2.2: Create TimerCoordinator**
**Priority:** ?? **MEDIUM-HIGH**  
**Estimated Time:** 2-3 hours

**What it does:**
- Manages all timers in one place
- Provides timer lifecycle
- Coordinates timer intervals
- Handles timer-driven updates

**Responsibilities:**
```visualbasic
Public Class TimerCoordinator
    ' Timers
    Private playbackTimer As Timer
    Private meterTimer As Timer
    Private ledTimer As Timer
    
    ' Events
    Public Event PlaybackTick As EventHandler
    Public Event MeterTick As EventHandler
    Public Event LedTick As EventHandler
    
    ' Methods
    Public Sub StartPlayback()
    Public Sub StopPlayback()
    Public Sub StartMeters()
    Public Sub StopMeters()
End Class
```

**Moves from MainForm:**
- ? TimerPlayback management
- ? TimerMeters management
- ? Timer tick logic coordination

**Files to Create:**
- `Managers\TimerCoordinator.vb`

**Files to Modify:**
- `MainForm.vb` (remove timer logic)
- `MainForm.Designer.vb` (timers move out)

---

### **Phase 2.3: Refactor MainForm (Week 2)**

#### **Task 2.3.1: Simplify MainForm Event Handlers**
**Priority:** ?? **HIGH**  
**Estimated Time:** 4-6 hours

**Goal:** Reduce MainForm to thin wrappers

**Before:**
```visualbasic
Private Sub OnAudioRouterPlaybackStarted(...)
    ' 20 lines of logic
    ' Timer management
    ' Microphone disarm
    ' TransportControl updates
    ' UI updates
End Sub
```

**After:**
```visualbasic
Private Sub OnPlaybackStarted(...)
    ' Update UI only
    panelLED.BackColor = Color.Magenta
    lblStatus.Text = "Playing..."
End Sub
```

**Tasks:**
- ? Remove playback logic from event handlers
- ? Remove recording logic from event handlers
- ? Remove FFT logic from event handlers
- ? Keep only UI updates

---

#### **Task 2.3.2: Refactor Initialization**
**Priority:** ?? **HIGH**  
**Estimated Time:** 3-4 hours

**Goal:** Move subsystem initialization to coordinators

**Before:**
```visualbasic
Private Sub InitializeManagers()
    ' 50+ lines
    ' Create all managers
    ' Configure everything
    ' Wire all events
End Sub
```

**After:**
```visualbasic
Private Sub InitializeManagers()
    audioSystem = New AudioSystemController()
    audioSystem.Initialize()
    
    playbackCoord = New PlaybackCoordinator(audioSystem)
    recordingCoord = New RecordingCoordinator(audioSystem)
    fftCoord = New FFTCoordinator()
End Sub
```

**Tasks:**
- ? Extract initialization logic
- ? Move to coordinators
- ? Simplify MainForm.Load()

---

#### **Task 2.3.3: Clean Up Timer Handlers**
**Priority:** ?? **MEDIUM**  
**Estimated Time:** 2-3 hours

**Goal:** Remove timer logic from MainForm

**Before:**
```visualbasic
Private Sub TimerPlayback_Tick(...)
    ' Position updates
    ' FFT updates
    ' Meter updates
    ' State checks
End Sub
```

**After:**
```visualbasic
' Timer moved to TimerCoordinator
' MainForm just subscribes to events
Private Sub OnPlaybackTick(...)
    ' Update UI only
End Sub
```

---

### **Phase 2.4: Testing & Validation (Week 3)**

#### **Task 2.4.1: Unit Tests for Coordinators**
**Priority:** ?? **MEDIUM**  
**Estimated Time:** 4-6 hours

**Create tests for:**
- PlaybackCoordinator
- RecordingCoordinator
- AudioSystemController
- FFTCoordinator
- TimerCoordinator

---

#### **Task 2.4.2: Integration Testing**
**Priority:** ?? **HIGH**  
**Estimated Time:** 4-6 hours

**Test scenarios:**
- Record ? Stop ? Play ? Stop
- Device switching during operation
- Settings changes during playback
- Multiple rapid play/stop cycles
- FFT updates during playback

---

#### **Task 2.4.3: Regression Testing**
**Priority:** ?? **CRITICAL**  
**Estimated Time:** 3-4 hours

**Verify:**
- All existing features still work
- No performance degradation
- No new bugs introduced
- UI responsiveness maintained

---

## ?? **Refactoring Metrics**

### **Before Refactoring:**
| Metric | Value |
|--------|-------|
| MainForm Lines of Code | ~1500+ |
| Number of Event Handlers | ~25+ |
| Number of Timers | 3 |
| Responsibilities | 12+ |
| Testability | Low |
| Maintainability | Low |

### **After Refactoring (Target):**
| Metric | Target |
|--------|--------|
| MainForm Lines of Code | ~500-700 |
| Number of Event Handlers | ~15 (UI only) |
| Number of Timers | 0 (moved out) |
| Responsibilities | 3 (UI, wiring, lifecycle) |
| Testability | High |
| Maintainability | High |

---

## ?? **Success Criteria**

### **Must Have:**
- ? MainForm < 800 lines
- ? No business logic in MainForm
- ? All coordinators testable
- ? All existing features work
- ? No performance regression

### **Nice to Have:**
- ? Unit tests for coordinators
- ? Documentation for new architecture
- ? Migration guide

---

## ?? **Implementation Order**

### **Week 1: Core Coordinators**
1. **Day 1-2:** PlaybackCoordinator (most critical)
2. **Day 3:** RecordingCoordinator
3. **Day 4-5:** AudioSystemController

### **Week 2: Specialized + Refactor**
1. **Day 1:** FFTCoordinator
2. **Day 2:** TimerCoordinator
3. **Day 3-5:** Refactor MainForm

### **Week 3: Testing**
1. **Day 1-2:** Unit tests
2. **Day 3:** Integration testing
3. **Day 4:** Regression testing
4. **Day 5:** Documentation

---

## ?? **Risks & Mitigation**

### **Risk 1: Breaking Existing Features**
**Mitigation:**
- Comprehensive testing after each change
- Keep old code commented until verified
- Test each coordinator independently

### **Risk 2: Event Chain Complexity**
**Mitigation:**
- Document event flows
- Keep event chains simple
- Avoid circular dependencies

### **Risk 3: Performance Impact**
**Mitigation:**
- Profile before and after
- Measure event latency
- Optimize hot paths

---

## ?? **Task Checklist**

### **Phase 2.1: Coordinators**
- [ ] Task 2.1.1: PlaybackCoordinator
- [ ] Task 2.1.2: RecordingCoordinator
- [ ] Task 2.1.3: AudioSystemController

### **Phase 2.2: Specialized**
- [ ] Task 2.2.1: FFTCoordinator
- [ ] Task 2.2.2: TimerCoordinator

### **Phase 2.3: Refactor**
- [ ] Task 2.3.1: Simplify event handlers
- [ ] Task 2.3.2: Refactor initialization
- [ ] Task 2.3.3: Clean up timers

### **Phase 2.4: Testing**
- [ ] Task 2.4.1: Unit tests
- [ ] Task 2.4.2: Integration tests
- [ ] Task 2.4.3: Regression tests

---

## ?? **Related Documents**

- [MainForm Analysis](../Architecture/MainForm-Analysis.md)
- [TransportControl Analysis](../Architecture/TransportControl-Complete-Analysis-2026-01-15.md)
- [Recording Architecture](../Architecture/Recording-Architecture-Final-2026-01-15.md)
- [Testing Plan](../Testing/Comprehensive-Testing-Plan-Pre-DSP-2026-01-15.md)

---

**Status:** ?? **READY FOR IMPLEMENTATION**  
**Next Step:** Begin Task 2.1.1 (PlaybackCoordinator)  
**Estimated Total Time:** 40-60 hours (2-3 weeks)  
**Date:** January 15, 2026
