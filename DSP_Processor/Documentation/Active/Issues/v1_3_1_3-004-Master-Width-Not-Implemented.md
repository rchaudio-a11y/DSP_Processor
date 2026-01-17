# Issue: Master Volume and Stereo Width Not Implemented

**Issue ID:** v1.3.1.3-004  
**Created:** 2026-01-16  
**Status:** Open - Known Limitation  
**Priority:** LOW  
**Affects:** DSPSignalFlowPanel user experience  
**RDF Phase:** Phase 3 (Build - Future)

---

## ?? Summary

DSPSignalFlowPanel has `trackMaster` and `trackWidth` sliders that only update labels but don't control any audio processing.

---

## ?? Description

**What's Wrong:**
- `trackMaster` slider moves but doesn't affect master volume
- `trackWidth` slider moves but doesn't affect stereo width
- Both marked `TODO` in code

**Expected Behavior (Future):**
- `trackMaster` should control final output gain (after all processing)
- `trackWidth` should control stereo width (0% = mono, 100% = normal, 200% = wide)

**Actual Behavior:**
- Sliders only update label text
- No audio processing occurs

---

## ?? Root Cause

### **Location:** `UI/DSPSignalFlowPanel.vb:323-324`

```visualbasic
Private Sub OnMasterChanged(sender As Object, e As EventArgs)
    ' TODO: Implement master volume control
    Dim masterDb = trackMaster.Value / 10.0F
    lblMasterValue.Text = $"{masterDb:F1} dB"
End Sub
```

### **Location:** `UI/DSPSignalFlowPanel.vb:335-338`

```visualbasic
Private Sub OnWidthChanged(sender As Object, e As EventArgs)
    ' TODO: Implement stereo width control
    Dim width = trackWidth.Value
    lblWidthValue.Text = $"{width}%"
End Sub
```

**Why Not Implemented:**
- Master volume: Could wire to OutputGainProcessor, but that's already controlled by AudioPipelinePanel
- Stereo width: Requires new StereoWidthProcessor (not yet implemented)

---

## ?? Impact

### **User Experience:**
- ?? Sliders feel broken (no effect)
- ?? Confusing: some sliders work (gain/pan), others don't (master/width)

### **Design:**
- ?? Incomplete UI (mixing console should have master fader)

---

## ?? Proposed Solution

### **For Master Volume:**

**Option A:** Wire to existing OutputGainProcessor
```visualbasic
' In OnMasterChanged:
If outputGainProcessor IsNot Nothing Then
    outputGainProcessor.GainDB = masterDb
End If
```

**Problem:** Conflicts with AudioPipelinePanel.trkOutputGain

**Option B:** Add new MasterGainProcessor as LAST stage
```visualbasic
' In RecordingManager, after OutputGainProcessor:
_masterGainProcessor = New DSP.GainProcessor(pcm16Format)
dspThread.Chain.AddProcessor(_masterGainProcessor)
```

**Better:** Separate control, true master fader

---

### **For Stereo Width:**

**Requires:** New `StereoWidthProcessor`

```visualbasic
Public Class StereoWidthProcessor
    Inherits ProcessorBase
    
    Public Property Width As Single ' 0.0 = mono, 1.0 = normal, 2.0 = wide
    
    Protected Overrides Sub ProcessInternal(buffer As AudioBuffer)
        ' M/S matrix:
        ' Mid = (L + R) / 2
        ' Side = (L - R) / 2
        ' Adjusted Side = Side * Width
        ' Output L = Mid + Adjusted Side
        ' Output R = Mid - Adjusted Side
    End Sub
End Class
```

**Priority:** LOW (nice-to-have feature)

---

## ? Acceptance Criteria

### **For Master Volume:**
1. Moving `trackMaster` affects final output level
2. Works independently of InputGain and OutputGain
3. Range: -60 dB to +12 dB

### **For Stereo Width:**
1. Moving `trackWidth` to 0% creates mono output
2. 100% = normal stereo imaging
3. 200% = exaggerated stereo (M/S widening)

---

## ?? Recommendation

**Defer to v1.3.2.0 or later**

**Rationale:**
- Low priority compared to Issues #001 and #002
- Requires new processors (architectural work)
- Current sliders should be disabled or hidden until implemented

---

## ?? Related Issues

- None (standalone feature)

---

## ?? References

- `UI/DSPSignalFlowPanel.vb:323-338` - TODO comments
- M/S stereo widening algorithms (future research)

---

**Created By:** Rick + GitHub Copilot  
**Version:** 1.0
