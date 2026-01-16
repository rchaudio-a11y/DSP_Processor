# MainForm Refactoring - Task List (Step-by-Step)

**Date:** January 15, 2026  
**Status:** ? **50% COMPLETE - READY TO COMMIT!**  
**Strategy:** Delegate to existing files FIRST, then create new coordinators

---

## ?? **MAJOR PROGRESS TODAY!**

**Completed Tasks:** 4/8 core tasks (50%)  
**Time Spent:** ~6 hours  
**Lines Removed:** ~150 lines from MainForm  
**Bugs Fixed:** 3 critical bugs  
**Build Status:** ? Successful

### ? **What's Done:**
1. ? Task 2.0.1: RecordingManager lifecycle
2. ? Task 2.0.2: AudioRouter PlayFile encapsulation
3. ? Task 2.0.3: AudioPipelineRouter buffer routing
4. ? Task 2.0.4: SpectrumManager FFT processing

### ?? **What's Skippable:**
- Task 2.0.5: SettingsManager (already clean)
- Task 2.0.6: FileManager (already clean)
- Task 2.0.7: TransportControl (optional improvement)
- Task 2.0.8: Timer cleanup (optional improvement)

### ?? **NEXT STEPS:**
1. **COMMIT NOW** (clean milestone) ? **DO THIS**
2. Run quick smoke tests
3. **DECISION:** Skip to Phase 3 or finish Phase 2.0?

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

### **TASK 2.0.1: RecordingManager - Recording Lifecycle** ?
**Priority:** CRITICAL  
**Time:** 2-3 hours  
**Status:** ? **COMPLETED** (Jan 15, 2026)

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

### **TASK 2.0.2: AudioRouter - Playback Lifecycle** ?
**Priority:** CRITICAL  
**Time:** 3-4 hours  
**Status:** ? **COMPLETED** (Jan 15, 2026)

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

### **TASK 2.0.3: AudioPipelineRouter - Buffer Routing** ?
**Priority:** HIGH  
**Time:** 2-3 hours  
**Status:** ? **COMPLETED** (Jan 15, 2026)

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

### **TASK 2.0.4: SpectrumManager - FFT Management** ?
**Priority:** MEDIUM  
**Time:** 2-3 hours  
**Status:** ? **COMPLETED** (Jan 15, 2026)

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
**Priority:** MEDIUM ? LOW  
**Time:** 2-3 hours  
**Status:** ?? **SKIPPABLE** (Already clean enough)

**Recommendation:** SKIP - Current implementation is good

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
**Priority:** MEDIUM ? LOW  
**Time:** 1-2 hours  
**Status:** ?? **SKIPPABLE** (Already clean enough)

**Recommendation:** SKIP - Current implementation is good

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

### **Phase 2.0: Delegation Pass** (50% Complete!)
- [x] Task 2.0.1: RecordingManager ? **DONE** (Jan 15, 2026)
- [x] Task 2.0.2: AudioRouter ? **DONE** (Jan 15, 2026)
- [x] Task 2.0.3: AudioPipelineRouter ? **DONE** (Jan 15, 2026)
- [x] Task 2.0.4: SpectrumManager ? **DONE** (Jan 15, 2026)
- [ ] Task 2.0.5: SettingsManager ? (Skippable - already clean)
- [ ] Task 2.0.6: FileManager ? (Skippable - already clean)
- [ ] Task 2.0.7: TransportControl ? (Optional improvement)
- [ ] Task 2.0.8: Clean Up Timers ? (Optional improvement)

**Completed:** 4/8 (50%)  
**Time Spent:** ~6 hours  
**Estimated Remaining:** 2-4 hours (most tasks optional/skippable)

### **Phase 2.1: New Coordinators** (DEFERRED)
- [ ] Task 2.1.1: PlaybackCoordinator (Not needed - AudioRouter sufficient)
- [ ] Task 2.1.2: AudioSystemController (Not needed - current works)

**Status:** ?? Deferred until actual need arises

### **Phase 2.2: Testing** ?? RECOMMENDED BEFORE PHASE 3
- [ ] Task 2.2.1: Integration Testing (4-6 hours)
- [ ] Task 2.2.2: Regression Testing (2-3 hours)

**Estimated Total:** 6-9 hours

---

## ?? **Current Status (Updated Jan 15, 2026)**

**Last Completed:** Task 2.0.4 (SpectrumManager) ?  
**Next Action:** ?? **COMMIT & TEST** (before Phase 3)  
**Tasks Completed:** 4/12 core tasks  
**Time Spent Today:** ~6 hours  
**Phase Progress:** Phase 2.0 at 50%  

**?? Recommendation:** 
1. **COMMIT NOW** (clean milestone) ? DO THIS FIRST
2. **Run quick tests** (record, play, devices work)
3. **DECISION POINT:**
   - Option A: Skip remaining 2.0 tasks ? Start Phase 3 ? RECOMMENDED
   - Option B: Complete 2.0.5-2.0.8 (2-4 hours more)
   - Option C: Just commit and take a break

---

## ?? **Notes**

- Start with Phase 2.0 (delegation to existing files) ? DONE (4/8)
- Test after each task ? DONE
- Don't create new files until Phase 2.1 ? FOLLOWED
- Keep MainForm.vb open to track progress ? DONE
- Commit after each successful task ?? **COMMIT NOW!**

---

## ?? **REMAINING WORK (Optional)**

### **If You Want to Finish Phase 2.0 (2-4 hours):**
- [ ] Task 2.0.5: Move ApplyMeterSettings to SettingsManager
- [ ] Task 2.0.6: Improve FileManager methods
- [ ] Task 2.0.7: Make TransportControl self-contained
- [ ] Task 2.0.8: Remove TimerPlayback from MainForm

### **If You Want to Skip to Phase 3 (Recommended):**
Current code is **clean enough** to start building DSP features:
- ? MainForm is 150 lines smaller
- ? Good separation of concerns
- ? Manager pattern working well
- ? No critical issues

**Recommendation:** COMMIT ? TEST ? START PHASE 3! ??

---

**Date:** January 15, 2026  
**Status:** ? **50% COMPLETE - COMMIT & DECIDE NEXT STEP**  
**Last Updated:** January 15, 2026 (Evening session)
