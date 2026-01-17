# Issue: Meters Display Raw Audio Bypassing DSP Chain

**Issue ID:** v1.3.1.3-001  
**Created:** 2026-01-16  
**Status:** Open - Root Cause Identified  
**Priority:** HIGH  
**Affects:** All volume meters, FFT display, UI feedback  
**RDF Phase:** Phase 4 (Debugging)

---

## ?? Summary

UI meters display raw microphone audio instead of DSP-processed audio, making gain/pan controls appear non-functional to the user.

---

## ?? Description

**What's Wrong:**
- Moving `trackGain`, `trackPan`, or AudioPipelinePanel sliders does NOT change what user sees on meters
- Meters show raw microphone levels (before DSP processing)
- Users cannot see the effect of their adjustments in real-time

**Expected Behavior:**
- Meters should display audio AFTER DSP processing (InputGainProcessor + OutputGainProcessor)
- Moving gain sliders should immediately reflect in meter levels
- Moving pan sliders should show L/R channel differences

**Actual Behavior:**
- Meters always show the same level regardless of slider positions
- DSP processing IS happening (recorded files have gain applied)
- But metering bypasses the DSP chain entirely

---

## ?? Root Cause Analysis

### **Location:** `Managers/RecordingManager.vb:659-665`

```visualbasic
' Always raise BufferAvailable for FFT/metering (use raw buffer)
Dim args As New AudioBufferEventArgs With {
    .Buffer = e.Buffer,  ' ? PROBLEM: RAW buffer from mic, NOT processed!
    .BitsPerSample = mic.BitsPerSample,
    .Channels = mic.Channels,
    .SampleRate = mic.SampleRate
}
RaiseEvent RecordingBufferAvailable(Me, args)
```

**Why This Breaks:**
1. `e.Buffer` is the RAW buffer directly from microphone
2. This buffer is passed to `OnAudioDataAvailable` BEFORE DSP processing
3. Event subscribers (meters, FFT) receive this raw buffer
4. DSP chain processes a COPY in background but meters never see it

**Evidence:**
- Line 635: `dspThread.ReadOutput(drainBuffer)` reads PROCESSED audio
- Line 638: Recording uses `drainBuffer` (processed) and works correctly
- Line 661: Event uses `e.Buffer` (raw) - inconsistency!

---

## ?? Impact

### **User Experience:**
- ? Sliders feel broken (no visual feedback)
- ? Cannot verify gain adjustments before recording
- ? Pan control appears non-functional
- ? FFT spectrum shows unprocessed audio

### **Developer Experience:**
- ?? Confusing: recorded files DO have gain applied
- ?? Debugging is hard (sliders work but don't appear to)

### **System Integrity:**
- ?? Tap point buffers exist but unused (wasted resources)
- ?? Architectural invariant violated: "Meters show processed audio"

---

## ?? Proposed Solutions

### **Option A: Use drainBuffer (Quick Fix)** ?

**Change Line 660-665:**
```visualbasic
' OLD (raw):
Dim args As New AudioBufferEventArgs With {
    .Buffer = e.Buffer,

' NEW (processed):
Dim args As New AudioBufferEventArgs With {
    .Buffer = drainBuffer,
    .ByteCount = bytesRead  ' Add this field!
```

**Pros:**
- ? One-line change
- ? Immediate fix (meters show processed audio)
- ? Minimal risk

**Cons:**
- ? Not architecturally "correct" (couples metering to output drain)
- ? Tap point buffers still unused

---

### **Option B: Use Tap Point Buffers (Proper Fix)**

**Implement proper tap point reads:**
```visualbasic
' Read from postOutputGainMonitorBuffer (AFTER both gain stages)
Dim processedBuffer(4095) As Byte
Dim bytesRead = dspThread.postOutputGainMonitorBuffer.Read(processedBuffer, 0, processedBuffer.Length)

Dim args As New AudioBufferEventArgs With {
    .Buffer = processedBuffer,
    .ByteCount = bytesRead
```

**Pros:**
- ? Architecturally correct (tap point pattern)
- ? Buffers are designed for this purpose
- ? Multi-reader support built-in

**Cons:**
- ?? More code to implement
- ?? Need to handle buffer underruns

---

## ?? Recommendation

**Implement Option A (drainBuffer) in v1.3.1.3, then refactor to Option B (tap points) in v1.3.2.0**

**Rationale:**
- Gets sliders working TODAY
- Proves the hypothesis
- Refactor to proper pattern in next phase

---

## ? Acceptance Criteria

1. Moving `trackGain` slider changes meter levels immediately
2. Moving `trackPan` slider shows L/R channel difference
3. AudioPipelinePanel sliders affect meters
4. FFT display shows processed audio spectrum
5. No regression in recording functionality

---

## ?? Related Issues

- v1.3.1.3-002: DSPSignalFlowPanel meters show same value for all channels
- v1.3.1.3-003: Tap point buffers unused

---

## ?? References

- [Audio-Signal-Flow-v1_3_1_3.md](Audio-Signal-Flow-v1_3_1_3.md) - Complete signal flow analysis
- `Managers/RecordingManager.vb:620-665` - OnAudioDataAvailable
- `MainForm.vb:409-437` - OnRecordingBufferAvailable

---

**Created By:** Rick + GitHub Copilot (RDF Debugging Session)  
**Version:** 1.0  
**Template:** Issue-v1_0_0.md
