# Bug Report: Recording Clicks During Active Recording

**Date:** January 15, 2026  
**Reporter:** User  
**Severity:** ?? MEDIUM  
**Status:** ?? OPEN - Investigation Needed  
**Component:** RecordingManager, RecordingEngine

---

## ?? Summary

Both WaveIn and WASAPI recordings contain audible clicks/pops during the recording session, even after buffer overflow fixes have been implemented.

---

## ?? Symptoms

1. **Clicks/pops audible in recordings** - Both drivers affected
2. **Not consistent timing** - Clicks appear randomly during recording
3. **Buffer warnings present** - Queue grows to 250-500 buffers during recording
4. **Post-recording overflow** - Additional overflow warnings after stopping

---

## ?? Observations from Log (DSP_Processor_20260115_002739.log)

### **Timeline:**
```
00:27:56.278 - Recording started
00:28:38.636 - Recording stopped (9.1 seconds)
00:28:38.824 - Buffer overflow warning (11 buffers) - AFTER stop!
00:28:43.844 - Buffer overflow warning (250 buffers)
00:28:48.864 - Buffer overflow warning (499 buffers)
```

### **Key Findings:**

**1. Overflow Happens AFTER Recording Stops**
- Recording stops at 00:28:38.636
- First overflow warning at 00:28:38.824 (188ms later)
- Queue continues growing: 11 ? 250 ? 499 buffers

**2. During Recording:**
- No explicit overflow warnings during 9.1 second recording
- But clicks were heard in the recording

**3. Playback Transition:**
- User clicked Play at 00:28:41.273
- Microphone was disarmed for playback
- This likely caused a click at playback start

---

## ?? Possible Root Causes

### **Hypothesis 1: Recording Drain Rate Insufficient** ?? LIKELY
**Issue:** During recording, `recorder.Process()` is called 4x per 20ms tick, but this might not drain the MicInputSource queue fast enough.

**Evidence:**
- Post-recording queue buildup suggests drain rate < production rate
- Queue reaches 499 buffers within 10 seconds after recording stops
- Clicks during recording suggest intermittent buffer skips

**Test Needed:**
- Log MicInputSource BufferQueueCount during recording
- Verify adaptive drain is active during recording
- Check if Process() actually drains MicInputSource

**Code Location:**
```vb
' RecordingManager.vb line 367-372
If recorder IsNot Nothing AndAlso recorder.InputSource IsNot Nothing Then
    For i = 1 To 4
        recorder.Process()  ' Does this drain MicInputSource queue?
    Next
```

---

### **Hypothesis 2: Process() Doesn't Drain Queue** ?? LIKELY
**Issue:** `recorder.Process()` might only read what RecordingEngine needs for the WAV file, NOT what's accumulated in the MicInputSource queue.

**Evidence:**
- Adaptive drain logic only applies when NOT recording
- During recording, only Process() is called
- No explicit MicInputSource.Read() during recording

**Potential Problem:**
```vb
' When NOT recording: Uses adaptive drain (4x to 8x)
ElseIf mic IsNot Nothing Then
    For i = 1 To drainCount  ' drainCount = 4, 6, or 8
        Dim read = mic.Read(buffer, 0, buffer.Length)

' When recording: Only calls Process()
If recorder IsNot Nothing Then
    For i = 1 To 4
        recorder.Process()  ' Might not fully drain!
```

**Test Needed:**
- Add logging inside RecordingEngine.Process() to see how much it reads
- Check if Process() calls mic.Read() with full buffer or partial
- Verify if leftover data accumulates in MicInputSource queue

---

### **Hypothesis 3: Mic Arm/Disarm Clicks** ?? POSSIBLE
**Issue:** Clicks at recording boundaries caused by microphone transitions, not buffer overflow.

**Evidence:**
- Playback starts by disarming microphone (line 276 in previous log)
- This causes audio device restart ? click
- Separate from buffer overflow issue

**Test Needed:**
- Record without playing back immediately
- Listen for clicks only during recording (not at start/stop)
- If clicks only at boundaries ? arm/disarm issue
- If clicks during recording ? buffer issue

---

### **Hypothesis 4: RecordingEngine Buffer Size** ?? POSSIBLE
**Issue:** RecordingEngine reads from MicInputSource but buffer size might not match production rate.

**Evidence:**
- WavFileOutput buffer size set somewhere in RecordingEngine
- Might be reading too little per Process() call
- Causes queue buildup even with 4x calls

**Test Needed:**
- Check WavFileOutput buffer configuration
- Verify bytes written per Process() call
- Compare to MicInputSource production rate

---

## ?? Proposed Solutions

### **Solution 1: Add Adaptive Drain During Recording** ? RECOMMENDED
```vb
' RecordingManager.vb ProcessingTimer_Tick()
If recorder IsNot Nothing AndAlso recorder.InputSource IsNot Nothing Then
    ' Call recorder.Process() for WAV writing
    For i = 1 To 4
        recorder.Process()
    Next
    
    ' ALSO drain any excess from MicInputSource queue
    ' (adaptive drain like non-recording path)
    If TypeOf mic Is MicInputSource Then
        Dim micSource = DirectCast(mic, MicInputSource)
        If micSource.BufferQueueCount > 10 Then
            ' Queue building during recording - drain excess
            Dim excessDrainCount = Math.Min(micSource.BufferQueueCount \ 10, 4)
            For i = 1 To excessDrainCount
                Dim buffer(4095) As Byte
                Dim read = micSource.Read(buffer, 0, buffer.Length)
                If read = 0 Then Exit For
                ' Discard excess (already processed by recorder.Process())
            Next
        End If
    End If
End If
```

**Impact:**
- Prevents queue buildup during recording
- Self-regulating based on queue depth
- Doesn't interfere with normal Process() flow

---

### **Solution 2: Increase Process() Call Rate** 
```vb
' Increase from 4x to 6x or 8x
For i = 1 To 8  ' Was 4
    recorder.Process()
Next
```

**Pros:** Simple change  
**Cons:** Might not help if Process() doesn't drain queue  
**Risk:** LOW

---

### **Solution 3: Log & Monitor Queue During Recording**
```vb
' Add diagnostic logging
If _isRecording Then
    Logger.Instance.Debug($"Recording queue: {mic.BufferQueueCount} buffers")
End If
```

**Purpose:** Understand exact behavior during recording  
**Priority:** HIGH - Do this first!

---

## ?? Testing Protocol

### **Test 1: Queue Monitoring**
1. Add BufferQueueCount logging during recording
2. Record for 10-15 seconds
3. Check log for queue growth
4. **Expected:** Queue should stay < 10 buffers
5. **Actual:** TBD

### **Test 2: Adaptive Drain During Recording**
1. Implement Solution 1
2. Record with same test scenario
3. Monitor queue depth
4. **Expected:** Queue stays low, clicks eliminated
5. **Actual:** TBD

### **Test 3: Arm/Disarm Isolation**
1. Record without immediate playback
2. Listen to recording for clicks
3. **If clicks present:** Buffer issue
4. **If no clicks:** Arm/disarm issue

---

## ?? Files to Investigate

1. **`Managers\RecordingManager.vb`**
   - ProcessingTimer_Tick() - Lines 361-405
   - Recording path vs non-recording path drain logic

2. **`Recording\RecordingEngine.vb`**
   - Process() method - Check how much it reads
   - WavFileOutput buffer configuration
   - Actual drain amount per call

3. **`AudioIO\MicInputSource.vb`**
   - Read() method - Verify queue draining
   - BufferQueueCount property - Current queue depth

4. **`AudioIO\WasapiEngine.vb`**
   - Same monitoring for WASAPI

---

## ?? Metrics to Collect

### **During Recording:**
- Buffer queue depth (every 100ms)
- Bytes read per Process() call
- Total bytes produced vs consumed
- Overflow warnings timing

### **After Fix:**
- Queue depth should stay < 10
- No overflow warnings during recording
- Clean audio with no clicks

---

## ?? Success Criteria

- ? No buffer overflow during recording
- ? No buffer overflow after recording stops
- ? Queue stays < 10 buffers during recording
- ? No audible clicks in recordings
- ? Works for both WaveIn and WASAPI

---

## ?? Related Issues

- [Bug-Report-2026-01-14-Recording-Clicks-Pops.md](Bug-Report-2026-01-14-Recording-Clicks-Pops.md) - Original buffer overflow fix
- [Bug-Fix-2026-01-15-WASAPI-Buffer-Overflow.md](Bug-Fix-2026-01-15-WASAPI-Buffer-Overflow.md) - Adaptive drain implementation
- [Session-Summary-2026-01-15](../Sessions/Session-Summary-2026-01-15-WASAPI-Integration-Complete.md) - WASAPI integration work

---

## ?? Next Session Action Items

### **Priority 1: Diagnostic Logging** (15 minutes)
1. Add BufferQueueCount logging during recording
2. Add bytes-read logging in Process()
3. Run test recording
4. Analyze logs

### **Priority 2: Implement Solution** (30 minutes)
1. If queue grows during recording ? Add adaptive drain
2. If queue stable ? Investigate Process() drain amount
3. Test fix with both WaveIn and WASAPI

### **Priority 3: Validate** (15 minutes)
1. Multiple recording sessions
2. Various durations (5s, 30s, 2m)
3. Both drivers
4. Confirm no clicks

---

## ?? Notes

**Current Status:**
- WASAPI integration: ? COMPLETE
- Buffer overflow (armed, not recording): ? FIXED
- Buffer overflow (during recording): ?? NEEDS FIX
- Clicks during recording: ?? NEEDS FIX

**Session Time Investment:**
- This session: 7 hours (WASAPI + adaptive drain)
- Next session estimate: 1 hour (diagnostic + fix)

**Complexity:**
- LOW - Solution path is clear
- Just needs diagnostic confirmation and implementation

---

**Report Date:** January 15, 2026 06:45  
**Status:** ?? OPEN  
**Next Review:** Next session  
**Estimated Fix Time:** 1 hour

---

## ? What We Know Works

? Adaptive drain when NOT recording (fixed queue buildup)  
? WASAPI format conversion (fixed constant noise)  
? Driver-specific defaults (fixed format mismatch)  
? Ghost callback elimination (fixed lingering warnings)

## ? What Needs Investigation

? Does recorder.Process() fully drain MicInputSource queue?  
? What's the actual queue depth during recording?  
? Is adaptive drain needed during recording too?

---

**Ready for Next Session!** ??
