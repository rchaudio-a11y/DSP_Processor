# Task List: Audio Signal Flow Fixes v1.3.1.3

**Created:** 2026-01-16  
**Version:** v1.3.1.3  
**RDF Phase:** Phase 3-6 (Build ? Synthesis)  
**Estimated Time:** 6-8 hours

---

## ?? Overview

Fix audio signal flow issues discovered in debugging session. Root cause: meters display raw audio bypassing DSP chain, making gain/pan controls appear non-functional.

**Issues to Resolve:**
- ? Issue #001: Meters bypass DSP (HIGH priority)
- ? Issue #002: Stereo meters show same value (MEDIUM priority)  
- ? Issue #003: Tap points unused (MEDIUM priority)
- ? Issue #004: Master/Width not implemented (LOW priority - defer)

---

## ?? Task Breakdown

### **Phase 1: Quick Fix (drainBuffer Approach)**

**Goal:** Get sliders working TODAY by using drainBuffer for meters

#### **Task 1.1: Update RecordingBufferAvailable Event**
**File:** `Managers/RecordingManager.vb`  
**Lines:** 659-665  
**Time:** 15 minutes  
**Priority:** HIGH

**Changes:**
```visualbasic
' OLD:
Dim args As New AudioBufferEventArgs With {
    .Buffer = e.Buffer,  ' RAW

' NEW:
Dim args As New AudioBufferEventArgs With {
    .Buffer = drainBuffer,  ' PROCESSED
    .ByteCount = bytesRead
```

**Validation:**
- [ ] Build successful
- [ ] No null reference exceptions
- [ ] Moving trackGain slider changes meter levels
- [ ] Recording still works

---

#### **Task 1.2: Add ByteCount to AudioBufferEventArgs**
**File:** `AudioIO/AudioCallbackEventArgs.vb` (or wherever defined)  
**Time:** 5 minutes  
**Priority:** HIGH

**Changes:**
```visualbasic
Public Class AudioBufferEventArgs
    Inherits EventArgs
    
    Public Property Buffer As Byte()
    Public Property ByteCount As Integer  ' ADD THIS
    Public Property BitsPerSample As Integer
    Public Property Channels As Integer
    Public Property SampleRate As Integer
End Class
```

**Validation:**
- [ ] Build successful
- [ ] No breaking changes to existing code

---

#### **Task 1.3: Update OnRecordingBufferAvailable to Use ByteCount**
**File:** `MainForm.vb`  
**Lines:** 420-436  
**Time:** 10 minutes  
**Priority:** HIGH

**Changes:**
```visualbasic
Private Sub OnRecordingBufferAvailable(sender As Object, e As AudioBufferEventArgs)
    ' Use e.ByteCount instead of assuming full buffer
    Dim levelData = AudioLevelMeter.AnalyzeSamples(
        e.Buffer, 
        e.BitsPerSample, 
        e.Channels,
        e.ByteCount)  ' ADD THIS PARAMETER (if supported)
```

**Validation:**
- [ ] Meters update correctly
- [ ] No array index out of bounds
- [ ] Slider changes visible in meters

---

### **Phase 2: Stereo Meter Separation**

**Goal:** Make DSPSignalFlowPanel meters show true L/R stereo imaging

#### **Task 2.1: Create Stereo Analysis Helper**
**File:** `Utils/AudioLevelMeter.vb` (or MainForm.vb)  
**Time:** 30 minutes  
**Priority:** MEDIUM

**Create method:**
```visualbasic
Public Shared Function AnalyzeStereoChannels(
    buffer As Byte(), 
    bitsPerSample As Integer, 
    byteCount As Integer
) As (leftPeakDb As Single, rightPeakDb As Single)
    
    ' Separate interleaved stereo samples
    ' Calculate peak for left channel
    ' Calculate peak for right channel
    ' Return tuple
End Function
```

**Validation:**
- [ ] Returns correct L/R values
- [ ] Handles mono input gracefully
- [ ] Panning left/right shows asymmetry

---

#### **Task 2.2: Update DSPSignalFlowPanel Meter Update**
**File:** `MainForm.vb`  
**Lines:** 429-433  
**Time:** 20 minutes  
**Priority:** MEDIUM  
**Depends On:** Task 2.1

**Changes:**
```visualbasic
' OLD: All meters get same value
DspSignalFlowPanel1.UpdateMeters(
    levelData.PeakDB,  ' All same!
    levelData.PeakDB,
    levelData.PeakDB,
    levelData.PeakDB)

' NEW: Separate L/R analysis
Dim (leftDb, rightDb) = AnalyzeStereoChannels(e.Buffer, e.BitsPerSample, e.ByteCount)
DspSignalFlowPanel1.UpdateMeters(
    leftDb,   ' Input Left
    rightDb,  ' Input Right
    leftDb,   ' Output Left (TODO: read from output tap)
    rightDb)  ' Output Right (TODO: read from output tap)
```

**Validation:**
- [ ] L/R meters show different values when panned
- [ ] Center pan shows equal L/R
- [ ] Full left pan shows high L, low R

---

### **Phase 3: Proper Tap Point Implementation** (Optional - Phase 6 Refactor)

**Goal:** Architecturally correct solution using DSPThread monitor buffers

#### **Task 3.1: Implement Tap Point Read Methods**
**File:** `DSP/DSPThread.vb`  
**Time:** 1 hour  
**Priority:** LOW (refactor)

**Add methods:**
```visualbasic
Public Function ReadInputTap(buffer As Byte(), offset As Integer, count As Integer) As Integer
    Return postGainMonitorBuffer.Read(buffer, offset, count)
End Function

Public Function ReadOutputTap(buffer As Byte(), offset As Integer, count As Integer) As Integer
    Return postOutputGainMonitorBuffer.Read(buffer, offset, count)
End Function
```

**Validation:**
- [ ] Thread-safe reads
- [ ] No blocking
- [ ] Returns actual bytes read

---

#### **Task 3.2: Update MainForm to Use Tap Points**
**File:** `MainForm.vb`  
**Time:** 30 minutes  
**Priority:** LOW (refactor)  
**Depends On:** Task 3.1

**Replace drainBuffer approach with tap point reads**

**Validation:**
- [ ] Input meters reflect InputGainProcessor output
- [ ] Output meters reflect OutputGainProcessor output
- [ ] Distinct input vs output levels visible

---

### **Phase 4: Testing & Validation**

#### **Task 4.1: Manual Testing**
**Time:** 30 minutes  
**Priority:** HIGH

**Test Cases:**
1. Arm microphone, speak, verify meters update
2. Move AudioPipelinePanel Input Gain ? meters change
3. Move AudioPipelinePanel Output Gain ? meters change
4. Move DSPSignalFlowPanel trackGain ? meters change
5. Move DSPSignalFlowPanel trackPan ? L/R asymmetry visible
6. Start recording ? verify processed audio recorded
7. Check FFT display ? verify shows processed spectrum

**Validation:**
- [ ] All test cases pass
- [ ] No crashes or exceptions
- [ ] Performance acceptable (no lag)

---

#### **Task 4.2: Record Test Files**
**Time:** 15 minutes  
**Priority:** HIGH

**Test:**
1. Record with 0 dB gain ? normal level
2. Record with -20 dB gain ? quiet file
3. Record with +6 dB gain ? louder file
4. Play back files ? verify gain was applied

**Validation:**
- [ ] Recorded files match expected levels
- [ ] No clipping or distortion
- [ ] Gain changes audible in playback

---

### **Phase 5: Documentation & Cleanup**

#### **Task 5.1: Update Architecture Document**
**File:** `Documentation/Architecture/Audio-Signal-Flow-v1_3_1_3.md`  
**Time:** 20 minutes  
**Priority:** MEDIUM

**Updates:**
- Mark Issue #001 as RESOLVED
- Mark Issue #002 as RESOLVED (if Phase 2 complete)
- Update signal flow diagram
- Document drainBuffer vs tap point trade-offs

---

#### **Task 5.2: Create Session Notes**
**File:** `Documentation/Active/Sessions/YYYY-MM-DD-Audio-Flow-Fix.md`  
**Time:** 30 minutes  
**Priority:** HIGH (RDF requirement)

**Use Session-Notes-v1_0_0.md template:**
- Document RDF phases (Phase 4 ? Phase 5 ? Phase 6)
- List discoveries and insights
- Record metrics (files changed, time spent)
- Meta-reflection: architectural learnings

---

#### **Task 5.3: Update Changelog**
**File:** `Documentation/Changelog/CURRENT.md`  
**Time:** 15 minutes  
**Priority:** HIGH

**Add entry:**
```markdown
## [v1.3.1.3] - 2026-01-XX - Audio Signal Flow Fix

**RDF Phase:** Phase 4-6 (Debug ? Validate ? Synthesize)

### Bug Fixes
- Fixed meters displaying raw audio bypassing DSP chain
- Fixed DSPSignalFlowPanel meters showing identical L/R values
- Gain and pan controls now provide immediate visual feedback

### Technical
- RecordingBufferAvailable event now passes processed buffer
- Added stereo channel separation for accurate L/R metering
```

---

#### **Task 5.4: Move Issues to Completed**
**Time:** 5 minutes  
**Priority:** LOW

- [ ] Move Issue #001 to `Completed/Issues/`
- [ ] Move Issue #002 to `Completed/Issues/` (if done)
- [ ] Leave Issue #003 open (tap point refactor - future)
- [ ] Leave Issue #004 open (master/width - future)

---

#### **Task 5.5: Git Commit & Tag**
**Time:** 10 minutes  
**Priority:** HIGH

**Commits:**
```bash
git add .
git commit -m "v1.3.1.3 - Fix audio signal flow (meters bypass DSP)

- Changed RecordingBufferAvailable to use processed drainBuffer
- Added stereo L/R channel separation for meters
- Gain/pan controls now show immediate visual feedback

Fixes: Issue #001, Issue #002
RDF Phase: 4-6 (Debug -> Synthesis)"

git tag -a v1.3.1.3 -m "Audio signal flow fixes"
git push origin master --tags
```

---

## ?? Progress Tracking

### **Phase 1: Quick Fix (drainBuffer)**
- [ ] Task 1.1: Update RecordingBufferAvailable (15 min)
- [ ] Task 1.2: Add ByteCount property (5 min)
- [ ] Task 1.3: Update OnRecordingBufferAvailable (10 min)

**Estimated:** 30 minutes  
**Actual:** _____

---

### **Phase 2: Stereo Meter Separation**
- [ ] Task 2.1: Create stereo analysis helper (30 min)
- [ ] Task 2.2: Update DSPSignalFlowPanel meters (20 min)

**Estimated:** 50 minutes  
**Actual:** _____

---

### **Phase 3: Tap Point Implementation** (Optional)
- [ ] Task 3.1: Implement tap point read methods (1 hour)
- [ ] Task 3.2: Update MainForm to use tap points (30 min)

**Estimated:** 1.5 hours  
**Actual:** _____  
**Status:** DEFERRED to v1.3.2.0

---

### **Phase 4: Testing**
- [ ] Task 4.1: Manual testing (30 min)
- [ ] Task 4.2: Record test files (15 min)

**Estimated:** 45 minutes  
**Actual:** _____

---

### **Phase 5: Documentation**
- [ ] Task 5.1: Update architecture doc (20 min)
- [ ] Task 5.2: Create session notes (30 min)
- [ ] Task 5.3: Update changelog (15 min)
- [ ] Task 5.4: Move issues to completed (5 min)
- [ ] Task 5.5: Git commit & tag (10 min)

**Estimated:** 1 hour 20 minutes  
**Actual:** _____

---

## ?? Total Estimates

**Minimum (Phase 1 + 2 + 4 + 5):** 3 hours 25 minutes  
**With Tap Points (Phase 1-5):** 4 hours 55 minutes  
**Full Documentation:** +30 minutes

**Recommended First Session:** Phase 1 + Phase 2 + Phase 4 + Task 5.5 (Git commit)  
**Estimated:** 2 hours 15 minutes

---

## ?? Related Documents

- [Audio-Signal-Flow-v1_3_1_3.md](../Architecture/Audio-Signal-Flow-v1_3_1_3.md)
- [Issue #001](../Active/Issues/v1_3_1_3-001-Meters-Bypass-DSP.md)
- [Issue #002](../Active/Issues/v1_3_1_3-002-Meters-Show-Same-Value.md)
- [Issue #003](../Active/Issues/v1_3_1_3-003-Tap-Points-Unused.md)
- [Issue #004](../Active/Issues/v1_3_1_3-004-Master-Width-Not-Implemented.md)

---

**Created By:** Rick + GitHub Copilot (RDF Task Planning)  
**Version:** 1.0  
**Status:** Ready to Execute
