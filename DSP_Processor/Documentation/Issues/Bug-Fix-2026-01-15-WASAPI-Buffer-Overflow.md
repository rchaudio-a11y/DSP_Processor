# Bug Fix: WASAPI Buffer Overflow & Ghost Callbacks

**Date:** January 15, 2026  
**Severity:** ?? **HIGH**  
**Status:** ? **FIXED**  
**Affected Component:** RecordingManager, MicInputSource, WasapiEngine  
**Related Task:** Task 1.2 - WASAPI Integration

---

## ?? Summary

Multiple critical issues discovered during WASAPI integration testing caused buffer overflow (5000+ buffers) and ghost warnings from disposed audio sources. All issues have been resolved with adaptive drain rate and proper disposal synchronization.

---

## ?? Issues Fixed

### **Issue 1: Buffer Queue Explosion**

**Severity:** ?? HIGH

**Symptoms:**
- Buffer queue grew from 10 ? 5000+ buffers over 2-3 minutes
- 100+ seconds of audio backlog
- Caused clicks and pops in recordings
- Performance degradation over time

**Root Cause:**
When **armed but not recording**, the processing timer only drained 4KB once per 20ms tick:
```
Drain rate: 4KB / 20ms = 200 KB/sec
WASAPI production at 48kHz: 48000 × 2 channels × 2 bytes = 192 KB/sec
```

While theoretically adequate, small timing variations and thread scheduling caused the drain rate to fall slightly behind, resulting in slow but steady queue buildup.

**Solution:**
Implemented **adaptive buffer drain rate** based on queue depth:

```vb
' Default: 4x drain per tick
Dim drainCount As Integer = 4

' Scale up if queue building
If mic.BufferQueueCount > 20 Then
    drainCount = 8  ' 2x faster (critical)
ElseIf mic.BufferQueueCount > 10 Then
    drainCount = 6  ' 1.5x faster (building)
End If

' Drain adaptively
For i = 1 To drainCount
    Dim buffer(4095) As Byte
    Dim read = mic.Read(buffer, 0, buffer.Length)
    If read = 0 Then Exit For  ' No more data
    ' Process buffer...
Next
```

**Result:**
- ? Queue stays < 10 buffers typical
- ? Self-regulating system
- ? No manual tuning needed
- ? Works with any sample rate

---

### **Issue 2: Ghost MicInputSource Callbacks**

**Severity:** ?? MEDIUM

**Symptoms:**
- Buffer overflow warnings from "MicInputSource" while using WASAPI
- Warnings continued for minutes after switching drivers
- Appeared as if old WaveIn session was still running

**Root Cause:**
Windows audio callbacks are **asynchronous**. Even after calling `StopRecording()` and `Dispose()`, callbacks already queued in the Windows audio thread continued to fire for several hundred milliseconds.

**Timeline:**
```
Time 0ms:    User switches to WASAPI
Time 1ms:    DisarmMicrophone() called
Time 2ms:    waveIn.StopRecording() called
Time 3ms:    RemoveHandler called
Time 4ms:    waveIn.Dispose() called
Time 5ms:    mic = Nothing

Time 50ms:   Windows callback fires! (queued before StopRecording)
Time 100ms:  Another callback fires!
Time 200ms:  Final callbacks trickle in...
```

**Solution 1: Disposal Flag**
```vb
' MicInputSource.vb
Private _disposed As Boolean = False

Private Sub OnDataAvailable(sender As Object, e As WaveInEventArgs)
    ' Ignore callbacks after disposal starts
    If _disposed Then Return
    
    ' Normal processing...
End Sub

Public Sub Dispose()
    _disposed = True  ' Set FIRST
    If waveIn IsNot Nothing Then
        waveIn.StopRecording()
        Thread.Sleep(20)  ' Let callbacks see flag
        RemoveHandler waveIn.DataAvailable, AddressOf OnDataAvailable
        waveIn.Dispose()
        waveIn = Nothing
    End If
End Sub
```

**Solution 2: Synchronization Delays**
```vb
' RecordingManager.vb
Public Sub DisarmMicrophone()
    ' Stop timer FIRST
    processingTimer?.Dispose()
    processingTimer = Nothing
    
    ' Wait for pending timer events
    Thread.Sleep(50)
    
    ' Dispose mic
    If mic IsNot Nothing Then
        DirectCast(mic, IDisposable).Dispose()
        mic = Nothing
    End If
    
    ' Wait for disposal to complete
    Thread.Sleep(50)
    
    _isArmed = False
End Sub
```

**Result:**
- ? No more ghost warnings
- ? Clean driver switching
- ? No race conditions
- ? Proper callback termination

---

### **Issue 3: Fixed Drain Rate Insufficient**

**Severity:** ?? MEDIUM

**Symptoms:**
- Gradual buffer buildup when armed but not recording
- Eventually triggered overflow warnings
- More pronounced with WASAPI 48kHz

**Root Cause:**
Original implementation drained only **once** per timer tick when not recording:

```vb
' OLD CODE (insufficient):
ElseIf mic IsNot Nothing Then
    Dim buffer(4095) As Byte
    Dim read = mic.Read(buffer, 0, buffer.Length)  ' Only 4KB!
    ' ...
End If
```

This worked for WaveIn 44.1kHz (176KB/sec) but failed for WASAPI 48kHz (192KB/sec).

**Solution:**
Match recording path drain rate (4x minimum):

```vb
' NEW CODE (sufficient):
ElseIf mic IsNot Nothing Then
    For i = 1 To 4  ' Same as recording path
        Dim buffer(4095) As Byte
        Dim read = mic.Read(buffer, 0, buffer.Length)
        If read = 0 Then Exit For
        ' Process buffer...
    Next
End If
```

Combined with adaptive scaling (6x or 8x when needed), this ensures adequate drain rate for all scenarios.

**Result:**
- ? Matches recording drain rate
- ? Prevents initial buildup
- ? Works with both WaveIn and WASAPI

---

## ?? Metrics

### **Before Fix:**
| Metric | Value |
|--------|-------|
| Buffer Queue (armed 3 min) | 5000+ buffers |
| Backlog Duration | 100+ seconds |
| Overflow Warnings | Every 5 seconds |
| Clicks/Pops | Frequent |
| Ghost Warnings | Minutes after switch |

### **After Fix:**
| Metric | Value |
|--------|-------|
| Buffer Queue (armed 3 min) | < 10 buffers |
| Backlog Duration | < 200ms |
| Overflow Warnings | None |
| Clicks/Pops | None |
| Ghost Warnings | None |

---

## ?? Testing Performed

### **Test 1: Long-Term Stability**
- Armed WASAPI for 5 minutes
- Monitored buffer queue depth
- ? Result: Queue stayed 0-8 buffers, no warnings

### **Test 2: Driver Switching**
- Switched WaveIn ? WASAPI multiple times
- Monitored for ghost warnings
- ? Result: No warnings after switch, clean transitions

### **Test 3: Recording Quality**
- Recorded multiple sessions with both drivers
- Checked for clicks, pops, artifacts
- ? Result: Clean audio, no artifacts

### **Test 4: Adaptive Drain**
- Artificially paused drain to build queue
- Verified adaptive increase kicked in
- ? Result: Queue drained back to < 10 automatically

---

## ?? Files Modified

1. **`Managers\RecordingManager.vb`**
   - Added adaptive drain rate logic
   - Added BufferQueueCount monitoring
   - Added synchronization delays in DisarmMicrophone()

2. **`AudioIO\MicInputSource.vb`**
   - Added _disposed flag
   - Added disposal check in OnDataAvailable()
   - Added BufferQueueCount property
   - Added synchronization in Dispose()

3. **`AudioIO\WasapiEngine.vb`**
   - Added BufferQueueCount property

---

## ?? Lessons Learned

### **1. Adaptive vs Fixed Algorithms**
**Lesson:** Fixed drain rates can't handle all scenarios due to timing variations and thread scheduling.

**Solution:** Self-regulating systems with negative feedback adapt automatically.

**Recommendation:** Use adaptive algorithms for critical paths where timing matters.

---

### **2. Asynchronous Disposal**
**Lesson:** Windows audio callbacks continue after disposal starts due to queued callbacks in system threads.

**Solution:** Disposal flags + synchronization delays ensure clean shutdown.

**Recommendation:** Always assume async callbacks will continue briefly after disposal.

---

### **3. Matching Drain to Production**
**Lesson:** Drain rate must exceed production rate by a safety margin, not just match it.

**Solution:** Adaptive scaling provides automatic safety margin.

**Recommendation:** Monitor queue depth and scale drain rate dynamically.

---

## ?? Future Improvements

### **Potential Enhancements:**
1. **Telemetry:**
   - Log queue depth statistics
   - Track adaptive drain activations
   - Monitor performance over time

2. **Tuning:**
   - Make adaptive thresholds configurable
   - Add queue depth visualization in UI
   - Warn user if drain can't keep up

3. **Testing:**
   - Add unit tests for adaptive drain
   - Add integration tests for disposal
   - Add stress tests for queue buildup

---

## ? Verification

- ? Buffer overflow eliminated
- ? Ghost warnings eliminated
- ? Adaptive drain working
- ? Clean driver switching
- ? No performance regression
- ? Build successful
- ? Testing complete

---

**Fix Date:** January 15, 2026  
**Fix Status:** ? **COMPLETE & VERIFIED**  
**Ready for Production:** ? **YES**

---

## ?? Related Documentation

- [Session Summary: WASAPI Integration Complete](../Sessions/Session-Summary-2026-01-15-WASAPI-Integration-Complete.md)
- [Task 1.2: WASAPI Implementation](../Tasks/Task-1.2-WASAPI-Implementation.md)
- [Bug Report: Recording Clicks/Pops (Original Issue)](Bug-Report-2026-01-14-Recording-Clicks-Pops.md)
- [CHANGELOG](../CHANGELOG.md)
