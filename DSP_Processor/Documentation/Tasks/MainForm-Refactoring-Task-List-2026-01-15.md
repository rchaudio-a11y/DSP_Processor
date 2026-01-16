# MainForm Refactoring - Task List (Step-by-Step)

**Date:** January 15, 2026  
**Status:** ?? **READY TO EXECUTE**  
**Strategy:** Delegate to existing files FIRST, then create new coordinators

---

## ?? **Execution Strategy**

### **Phase 2.0: Delegation Pass (Week 1)** ? START HERE!
Move responsibilities from MainForm into **existing** files.

### **Phase 2.1: Create Coordinators (Week 2)**
Create new files only for responsibilities that don't fit elsewhere.

### **Phase 2.2: Testing (Week 3)**
Verify everything still works.

---

## ?? **PHASE 2.0: DELEGATION PASS**

### **TASK 2.0.1: RecordingManager - Recording Lifecycle** ??
**Priority:** CRITICAL  
**Time:** 2-3 hours  
**Status:** ? Not Started

**What to Move:**
```visualbasic
' FROM MainForm TO RecordingManager
- OnAudioSettingsChanged() ? RecordingManager.ApplyAudioSettings()
- OnRecordingOptionsChanged() ? RecordingManager.ApplyRecordingOptions()
- Microphone arming logic ? RecordingManager.ArmMicrophone()
- Microphone disarming logic ? RecordingManager.DisarmMicrophone()
- Input volume application ? RecordingManager.SetInputVolume()
```

**Steps:**
1. [ ] Add `ApplyAudioSettings(settings)` method to RecordingManager
2. [ ] Add `ApplyRecordingOptions(options)` method to RecordingManager
3. [ ] Add `SetInputVolume(volume)` method to RecordingManager
4. [ ] Move arming/disarming logic to RecordingManager
5. [ ] Update MainForm to call RecordingManager methods
6. [ ] Test recording still works

**Success Criteria:**
- ? Recording starts/stops correctly
- ? Microphone arms/disarms correctly
- ? Settings apply correctly
- ? No compilation errors

---

### **TASK 2.0.2: AudioRouter - Playback Lifecycle** ??
**Priority:** CRITICAL  
**Time:** 3-4 hours  
**Status:** ? Not Started

**What to Move:**
```visualbasic
' FROM MainForm TO AudioRouter
- lstRecordings_DoubleClick() playback logic ? AudioRouter.PlayFile()
- OnRoutingPanelOutputDeviceChanged() ? AudioRouter.SetOutputDevice()
- PlaybackCompleted handling ? Internal AudioRouter logic
- Timer management ? AudioRouter internal timer
```

**Steps:**
1. [ ] Add `PlayFile(filepath)` method to AudioRouter
2. [ ] Add `SetOutputDevice(deviceIndex)` method to AudioRouter
3. [ ] Add internal timer to AudioRouter for position updates
4. [ ] Move PlaybackCompleted logic to AudioRouter
5. [ ] Update MainForm to call AudioRouter.PlayFile()
6. [ ] Test playback still works

**Success Criteria:**
- ? Playback starts/stops correctly
- ? Position updates correctly
- ? Device switching works
- ? EOF handling works

---

### **TASK 2.0.3: AudioPipelineRouter - Buffer Routing** ??
**Priority:** HIGH  
**Time:** 2-3 hours  
**Status:** ? Not Started

**What to Move:**
```visualbasic
' FROM MainForm TO AudioPipelineRouter
- OnRecordingBufferAvailable() routing logic ? PipelineRouter.RouteBuffer()
- OnPipelineConfigurationChanged() ? PipelineRouter.ApplyConfiguration()
- Pipeline initialization ? PipelineRouter.Initialize()
```

**Steps:**
1. [ ] Add `RouteBuffer(buffer, count)` method to AudioPipelineRouter
2. [ ] Add `ApplyConfiguration(config)` method to AudioPipelineRouter
3. [ ] Move buffer routing logic to AudioPipelineRouter
4. [ ] Update MainForm to call AudioPipelineRouter.RouteBuffer()
5. [ ] Test audio routing still works

**Success Criteria:**
- ? Recording buffers route correctly
- ? DSP processing works
- ? FFT taps work
- ? No audio clicks

---

### **TASK 2.0.4: SpectrumManager - FFT Management** ??
**Priority:** MEDIUM  
**Time:** 2-3 hours  
**Status:** ? Not Started

**What to Move:**
```visualbasic
' FROM MainForm TO SpectrumManager (or create FFTCoordinator)
- ApplySpectrumSettings() ? SpectrumManager.ApplySettings()
- FFT processor creation ? SpectrumManager.Initialize()
- FFT clearing ? SpectrumManager.Clear()
- OnDSPInputSamples() ? SpectrumManager.ProcessInputSamples()
- OnDSPOutputSamples() ? SpectrumManager.ProcessOutputSamples()
```

**Steps:**
1. [ ] Add `ApplySettings(settings)` to SpectrumManager
2. [ ] Add `Initialize(sampleRate)` to SpectrumManager
3. [ ] Add `Clear()` to SpectrumManager
4. [ ] Add `ProcessInputSamples(buffer)` to SpectrumManager
5. [ ] Add `ProcessOutputSamples(buffer)` to SpectrumManager
6. [ ] Move FFT logic to SpectrumManager
7. [ ] Update MainForm to call SpectrumManager methods
8. [ ] Test spectrum display still works

**Success Criteria:**
- ? Spectrum displays correctly
- ? Settings apply correctly
- ? FFT clears correctly
- ? Input/output spectrum comparison works

---

### **TASK 2.0.5: SettingsManager - Settings Application** ??
**Priority:** MEDIUM  
**Time:** 2-3 hours  
**Status:** ? Not Started

**What to Move:**
```visualbasic
' FROM MainForm TO SettingsManager
- ApplyMeterSettings() ? SettingsManager.ApplyMeterSettings()
- OnMeterSettingsChanged() ? SettingsManager internal
- OnSpectrumResetRequested() ? SettingsManager.ResetSpectrumSettings()
```

**Steps:**
1. [ ] Add `ApplyMeterSettings(settings)` to SettingsManager
2. [ ] Add `ResetSpectrumSettings()` to SettingsManager
3. [ ] Move settings application logic
4. [ ] Update MainForm to call SettingsManager methods
5. [ ] Test settings still apply correctly

**Success Criteria:**
- ? Meter settings apply correctly
- ? Spectrum settings apply correctly
- ? Reset works correctly

---

### **TASK 2.0.6: FileManager - File Operations** ??
**Priority:** MEDIUM  
**Time:** 1-2 hours  
**Status:** ? Not Started

**What to Move:**
```visualbasic
' FROM MainForm TO FileManager
- lstRecordings_DoubleClick() file lookup ? FileManager.GetFileInfo()
- OnFileListChanged() ? FileManager internal event
- File validation logic ? FileManager.ValidateFile()
```

**Steps:**
1. [ ] Add `GetFileInfo(displayName)` to FileManager
2. [ ] Add `ValidateFile(filepath)` to FileManager
3. [ ] Move file validation logic
4. [ ] Update MainForm to call FileManager methods
5. [ ] Test file operations still work

**Success Criteria:**
- ? File list updates correctly
- ? File playback works
- ? File validation works

---

### **TASK 2.0.7: TransportControl - Self-Contained Updates** ??
**Priority:** MEDIUM-LOW  
**Time:** 2-3 hours  
**Status:** ? Not Started

**What to Move:**
```visualbasic
' FROM MainForm TO TransportControl
- Direct property updates ? TransportControl subscribes to events directly
- transportControl.State = ... ? Event-driven
- transportControl.TrackPosition = ... ? Event-driven
- transportControl.RecordingTime = ... ? Event-driven
```

**Goal:** Make TransportControl subscribe to events directly instead of MainForm updating properties.

**Steps:**
1. [ ] Add references to AudioRouter and RecordingManager in TransportControl
2. [ ] Wire events in TransportControl.Initialize()
3. [ ] Move property updates to TransportControl event handlers
4. [ ] Remove property updates from MainForm
5. [ ] Test transport control still updates correctly

**Success Criteria:**
- ? Transport control updates automatically
- ? MainForm doesn't touch transport properties
- ? LEDs work
- ? Time displays work

---

### **TASK 2.0.8: Clean Up Timer Logic** ??
**Priority:** MEDIUM  
**Time:** 1-2 hours  
**Status:** ? Not Started

**What to Move:**
```visualbasic
' FROM MainForm TO AudioRouter/RecordingManager
- TimerPlayback logic ? AudioRouter internal timer
- Position updates ? AudioRouter.UpdatePosition()
- FFT updates ? SpectrumManager internal
```

**Steps:**
1. [ ] Move TimerPlayback logic to AudioRouter
2. [ ] Remove TimerPlayback from MainForm.Designer
3. [ ] Test position updates still work

**Success Criteria:**
- ? Playback position updates
- ? No timer in MainForm
- ? Recording time updates

---

## ?? **PHASE 2.1: CREATE NEW COORDINATORS** (Only After Phase 2.0!)

### **TASK 2.1.1: Create PlaybackCoordinator** ??
**Priority:** HIGH  
**Time:** 3-4 hours  
**Status:** ? Not Started  
**Depends On:** Task 2.0.2

**Purpose:** Unified playback interface wrapping AudioRouter and PlaybackManager

**Responsibilities:**
- Wrap AudioRouter (DSP playback)
- Wrap PlaybackManager (direct playback)
- Provide unified Play/Stop/Seek interface
- Coordinate with RecordingManager (mic disarm)

**Steps:**
1. [ ] Create `Managers\PlaybackCoordinator.vb`
2. [ ] Add events (PlaybackStarted, Stopped, PositionChanged)
3. [ ] Add properties (IsPlaying, CurrentPosition, TotalDuration)
4. [ ] Add methods (Play, Stop, Seek, UpdatePosition)
5. [ ] Wire to AudioRouter and PlaybackManager
6. [ ] Update MainForm to use PlaybackCoordinator
7. [ ] Test playback still works

**Success Criteria:**
- ? Single playback interface
- ? DSP and direct playback work
- ? Microphone coordination works

---

### **TASK 2.1.2: Create AudioSystemController** ??
**Priority:** MEDIUM  
**Time:** 3-4 hours  
**Status:** ? Not Started

**Purpose:** High-level audio system management

**Responsibilities:**
- Device enumeration
- Device selection
- System initialization
- System shutdown
- Settings coordination

**Steps:**
1. [ ] Create `Managers\AudioSystemController.vb`
2. [ ] Add device management methods
3. [ ] Add initialization logic
4. [ ] Update MainForm to use AudioSystemController
5. [ ] Test device switching works

**Success Criteria:**
- ? Device enumeration works
- ? Device selection works
- ? System initialization works

---

## ?? **PHASE 2.2: TESTING**

### **TASK 2.2.1: Integration Testing** ??
**Priority:** CRITICAL  
**Time:** 4-6 hours  
**Status:** ? Not Started

**Test Scenarios:**
1. [ ] Record ? Stop ? Play ? Stop
2. [ ] Device switching during operation
3. [ ] Settings changes during playback
4. [ ] Multiple rapid play/stop cycles
5. [ ] FFT updates during playback
6. [ ] Microphone arming/disarming
7. [ ] File playback EOF handling
8. [ ] Timer stopping correctly

**Success Criteria:**
- ? All scenarios pass
- ? No crashes
- ? No audio clicks
- ? UI responsive

---

### **TASK 2.2.2: Regression Testing** ??
**Priority:** CRITICAL  
**Time:** 2-3 hours  
**Status:** ? Not Started

**Verify:**
1. [ ] All existing features still work
2. [ ] No performance degradation
3. [ ] No new bugs introduced
4. [ ] UI responsiveness maintained

---

## ?? **Progress Tracking**

### **Phase 2.0: Delegation Pass**
- [ ] Task 2.0.1: RecordingManager
- [ ] Task 2.0.2: AudioRouter
- [ ] Task 2.0.3: AudioPipelineRouter
- [ ] Task 2.0.4: SpectrumManager
- [ ] Task 2.0.5: SettingsManager
- [ ] Task 2.0.6: FileManager
- [ ] Task 2.0.7: TransportControl
- [ ] Task 2.0.8: Clean Up Timers

**Estimated Total:** 15-21 hours

### **Phase 2.1: New Coordinators**
- [ ] Task 2.1.1: PlaybackCoordinator
- [ ] Task 2.1.2: AudioSystemController

**Estimated Total:** 6-8 hours

### **Phase 2.2: Testing**
- [ ] Task 2.2.1: Integration Testing
- [ ] Task 2.2.2: Regression Testing

**Estimated Total:** 6-9 hours

---

## ?? **Current Status**

**Next Task:** Task 2.0.1 (RecordingManager - Recording Lifecycle)  
**Total Tasks:** 12  
**Total Estimated Time:** 27-38 hours  
**Current Phase:** Phase 2.0 (Delegation Pass)

---

## ?? **Notes**

- Start with Phase 2.0 (delegation to existing files)
- Test after each task
- Don't create new files until Phase 2.1
- Keep MainForm.vb open to track progress
- Commit after each successful task

---

**Date:** January 15, 2026  
**Status:** ?? **READY TO EXECUTE**  
**Next Step:** Begin Task 2.0.1
