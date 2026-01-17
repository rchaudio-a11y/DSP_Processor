# Issue: DSPSignalFlowPanel Meters Show Identical Values

**Issue ID:** v1.3.1.3-002  
**Created:** 2026-01-16  
**Status:** Open - Root Cause Identified  
**Priority:** MEDIUM  
**Affects:** DSPSignalFlowPanel stereo meters  
**RDF Phase:** Phase 4 (Debugging)

---

## ?? Summary

DSPSignalFlowPanel has 4 separate meters (Input L/R, Output L/R) but all display the same mono value, defeating the purpose of stereo metering.

---

## ?? Description

**What's Wrong:**
- `meterInputLeft`, `meterInputRight`, `meterOutputLeft`, `meterOutputRight` all show identical levels
- Stereo image is not visible
- Cannot see pan control effects
- Cannot distinguish between input and output stages

**Expected Behavior:**
- Input meters show pre-gain levels (L/R separate)
- Output meters show post-gain levels (L/R separate)
- Panning left/right shows asymmetry in L/R meters

**Actual Behavior:**
- All 4 meters display same mono peak level
- No stereo imaging visible

---

## ?? Root Cause Analysis

### **Location:** `MainForm.vb:429-433`

```visualbasic
' DSP Signal Flow Panel meters (NEW - event-driven!)
' For now, use same levels for input and output (future: read from DSP tap points)
DspSignalFlowPanel1.UpdateMeters(
    levelData.PeakDB,  ' Input Left ? ALL SAME!
    levelData.PeakDB,  ' Input Right ? ALL SAME!
    levelData.PeakDB,  ' Output Left ? ALL SAME!
    levelData.PeakDB)  ' Output Right ? ALL SAME!
```

**Why This Breaks:**
1. `AudioLevelMeter.AnalyzeSamples()` returns mono peak from interleaved stereo
2. Same value passed to all 4 meter parameters
3. Comment admits: "For now... future: read from DSP tap points"

**Additional Problem:**
- Even with processed audio (Issue #001 fix), still need to:
  - Separate L/R channels
  - Read from INPUT tap (postGainMonitorBuffer) for input meters
  - Read from OUTPUT tap (postOutputGainMonitorBuffer) for output meters

---

## ?? Impact

### **User Experience:**
- ? Cannot see stereo image
- ? Pan control appears broken (no L/R difference visible)
- ? Cannot distinguish input vs output stages

### **Visual Design:**
- ?? 4 meters that all show same value looks like a bug
- ?? Defeats purpose of professional mixing console layout

---

## ?? Proposed Solution

### **Step 1: Separate L/R Channel Analysis**

Create helper method:
```visualbasic
Private Function AnalyzeStereoChannels(buffer As Byte(), bitsPerSample As Integer) As (leftDb As Single, rightDb As Single)
    ' Separate interleaved stereo into L/R channels
    ' Return separate peak dB for each channel
End Function
```

### **Step 2: Read from Tap Points**

```visualbasic
' Read from INPUT tap (after InputGainProcessor)
Dim inputBuffer(4095) As Byte
Dim inputBytes = recordingManager.dspThread.postGainMonitorBuffer.Read(inputBuffer, 0, 4096)
Dim (inputLeftDb, inputRightDb) = AnalyzeStereoChannels(inputBuffer, e.BitsPerSample)

' Read from OUTPUT tap (after OutputGainProcessor)
Dim outputBuffer(4095) As Byte
Dim outputBytes = recordingManager.dspThread.postOutputGainMonitorBuffer.Read(outputBuffer, 0, 4096)
Dim (outputLeftDb, outputRightDb) = AnalyzeStereoChannels(outputBuffer, e.BitsPerSample)

' Update meters with correct values
DspSignalFlowPanel1.UpdateMeters(inputLeftDb, inputRightDb, outputLeftDb, outputRightDb)
```

---

## ? Acceptance Criteria

1. Input L/R meters show different values when panned
2. Output L/R meters show different values when panned
3. Input meters differ from output meters when gain adjusted
4. Meters accurately reflect stereo image

---

## ?? Dependencies

- **Requires:** Issue #001 fix (use processed audio)
- **Blocks:** Pan control validation

---

## ?? References

- [Audio-Signal-Flow-v1_3_1_3.md](Audio-Signal-Flow-v1_3_1_3.md) - Tap point architecture
- `MainForm.vb:429-433` - Current meter update code
- `UI/DSPSignalFlowPanel.vb:195-209` - UpdateMeters method

---

**Created By:** Rick + GitHub Copilot  
**Version:** 1.0
