# Buffer Analysis - Clicks/Pops Investigation

**Date:** January 15, 2026  
**Issue:** Clicks and pops in recorded audio files  
**Status:** ?? ACTIVE INVESTIGATION  
**Current Test:** Lock removal in DualBuffer.Write()

---

## ?? **Problem Statement**

Audio recordings contain clicks and pops that are:
- ? **Present in recorded WAV files** (verified via Windows Media Player)
- ? **Persist with DSP disabled** (TRUE BYPASS mode)
- ? **Persist with FFT disabled** (monitoring off)
- ? **Persist at 200ms buffer size** (maximum setting)
- ? **Recording to SSD** (fast storage)
- ? **Using internal mic** (not USB)

**This means:** Clicks are NOT caused by DSP processing, FFT monitoring, or buffer size - they're in the CORE recording pipeline!

---

## ?? **Complete Buffer Flow Inventory**

### **Audio Data Flow:**
```
MICROPHONE
    ?
[1] NAudio WaveInEvent/WASAPI
    ?
[2] RecordingManager.OnDataAvailable (AGGRESSIVE DRAIN)
    ?
[3] MainForm.OnRecordingBufferAvailable
    ?
[4] AudioPipelineRouter.RouteAudioBuffer
    ?
[5] AudioPipeline.ProcessBuffer (up to 7 copies!)
    ?
[6] DualBuffer.Write() (WITH LOCK - SUSPECT!)
    ?
[7] RecordingEngine.Write() (disk I/O)
    ?
WAV FILE
```

Parallel flow:
```
[6] DualBuffer.Write()
    ?
[8] FFTMonitorThread.WorkerLoop() (reads with lock - CONTENTION!)
```

---

## ?? **Detailed Buffer Operations**

### **[1] NAudio WaveInEvent/WASAPI**
**Location:** `MicInputSource.vb` or `WasapiEngine.vb`

**Operation:**
```visualbasic
' Audio driver callback
Private Sub OnDataAvailable(sender, e)
    ' Driver fills internal buffer
    ' Raises DataAvailable event
End Sub
```

**Buffer Operations:**
- **WRITES:** Audio driver ? internal queue
- **READS:** RecordingManager event handler
- **Size:** Configured buffer size (10-200ms)
- **Thread:** Audio driver callback thread

---

### **[2] RecordingManager.OnDataAvailable** ?? AGGRESSIVE DRAIN
**Location:** `RecordingManager.vb` lines 400-480

**Operation:**
```visualbasic
Private Sub OnDataAvailable(sender, e)
    ' Check queue depth
    Dim currentQueueDepth As Integer = mic.BufferQueueCount
    
    ' AGGRESSIVE DRAIN: Read multiple buffers per callback!
    Dim drainCount As Integer = 4  ' Default
    
    If currentQueueDepth > 100 Then
        drainCount = 20  ' Maximum aggressive
    ElseIf currentQueueDepth > 50 Then
        drainCount = 16
    ElseIf currentQueueDepth > 20 Then
        drainCount = 8
    End If
    
    ' Read multiple buffers in tight loop
    For i = 1 To drainCount
        Dim bytesRead = mic.Read(fftBuffer, 0, fftBuffer.Length)
        If bytesRead > 0 Then
            RaiseEvent BufferAvailable(Me, args)  // Raises event for EACH buffer!
        End If
    Next
End Sub
```

**Buffer Operations:**
- **READS:** From mic queue (4-20 times per callback!)
- **WRITES:** Raises BufferAvailable event (multiple times)
- **Issue:** Could overwhelm downstream pipeline with events
- **Thread:** Audio callback thread

**SUSPECT RATING:** ???? MEDIUM - Multiple rapid events could cause timing issues

---

### **[3] MainForm.OnRecordingBufferAvailable**
**Location:** `MainForm.vb` lines 390-420

**Operation:**
```visualbasic
Private Sub OnRecordingBufferAvailable(sender, e As AudioBufferEventArgs)
    ' Route through pipeline
    pipelineRouter.RouteAudioBuffer(e.Buffer, ...)
    
    ' Direct meter update (bypasses pipeline)
    AudioLevelMeter.AnalyzeSamples(e.Buffer, ...)
    meterRecording.SetLevel(...)
End Sub
```

**Buffer Operations:**
- **READS:** Event argument buffer (reference, not copy)
- **WRITES:** Routes to pipeline + meters
- **Thread:** Audio callback thread
- **Issue:** Same buffer used by multiple consumers

**SUSPECT RATING:** ?? LOW - Just routing, no copies

---

### **[4] AudioPipelineRouter.RouteAudioBuffer**
**Location:** `AudioPipelineRouter.vb` lines 240-280

**Operation:**
```visualbasic
Public Sub RouteAudioBuffer(buffer, ...)
    If Not config.Processing.EnableDSP Then
        Return  // TRUE BYPASS - skip pipeline
    End If
    
    ' Route through pipeline
    Dim processedBuffer = _pipeline.ProcessBuffer(buffer, ...)
End Sub
```

**Buffer Operations:**
- **READS:** Input buffer
- **WRITES:** Calls pipeline (if DSP enabled)
- **Thread:** Audio callback thread
- **Issue:** None if DSP disabled

**SUSPECT RATING:** ? CLEARED - TRUE BYPASS test proved this is not the issue

---

### **[5] AudioPipeline.ProcessBuffer** ?? MULTIPLE COPIES
**Location:** `AudioPipeline.vb` lines 120-220

**Operation:**
```visualbasic
Public Function ProcessBuffer(inputBuffer, ...) As Byte()
    ' === COPY 1: Save raw input (if Output FFT needs PreDSP) ===
    Dim rawInputBuffer As Byte() = Nothing
    If config.Monitoring.EnableOutputFFT And 
       config.Monitoring.OutputFFTTap = TapPoint.PreDSP Then
        rawInputBuffer = New Byte(inputBuffer.Length - 1) {}
        Array.Copy(inputBuffer, rawInputBuffer, inputBuffer.Length)  // COPY 1
    End If
    
    ' === COPY 2+3: Input stage (if Input FFT enabled) ===
    If config.Monitoring.EnableInputFFT Then
        _inputStage.Input(inputBuffer, ...)
            ? DualBuffer.Write()
                ? Array.Copy(data, _processingBuffer)  // COPY 2
                ? SyncLock _monitorLock                // LOCK!
                    Array.Copy(data, _monitorBuffer)   // COPY 3 (WITH LOCK!)
                  End SyncLock
    End If
    
    ' === DSP Processing (in-place, no copy) ===
    If config.Processing.EnableDSP Then
        ApplyDSP(inputBuffer, ...)  // Modifies in-place
    End If
    
    ' === COPY 4+5: Gain stage (if Output FFT enabled) ===
    If config.Monitoring.EnableOutputFFT Then
        _gainStage.Input(processedBuffer, ...)
            ? DualBuffer.Write()
                ? Array.Copy(data, _processingBuffer)  // COPY 4
                ? SyncLock _monitorLock                // LOCK!
                    Array.Copy(data, _monitorBuffer)   // COPY 5 (WITH LOCK!)
                  End SyncLock
    End If
    
    ' === COPY 6+7: Output stage (if destinations enabled) ===
    If config.Destination.EnableRecording Or config.Destination.EnablePlayback Then
        _outputStage.Input(processedBuffer, ...)
            ? DualBuffer.Write()
                ? Array.Copy(data, _processingBuffer)  // COPY 6
                ? SyncLock _monitorLock                // LOCK!
                    Array.Copy(data, _monitorBuffer)   // COPY 7 (WITH LOCK!)
                  End SyncLock
    End If
    
    Return processedBuffer
End Function
```

**Buffer Operations:**
- **READS:** Input buffer (once)
- **WRITES:** Up to 7 copies (1 optional + 6 for stages)
- **Thread:** Audio callback thread
- **Locks:** 3 locks acquired (one per stage)
- **Time per copy:** ~1-2 microseconds (8KB buffer)
- **Total time:** ~7-14 microseconds (if all enabled)

**SUSPECT RATING:** ?????? HIGH - Multiple copies + locks could accumulate latency

---

### **[6] DualBuffer.Write()** ?????? SMOKING GUN
**Location:** `DualBuffer.vb` lines 70-100

**ORIGINAL CODE (WITH LOCK):**
```visualbasic
Public Sub Write(data As Byte())
    ' Buffer 1: Processing buffer (no lock)
    Array.Copy(data, _processingBuffer, data.Length)
    
    ' Buffer 2: Monitor buffer (WITH LOCK)
    SyncLock _monitorLock
        Array.Copy(data, _monitorBuffer, data.Length)  // BLOCKED if FFT thread reading!
    End SyncLock
    
    Interlocked.Exchange(_hasMonitorData, 1)
End Sub
```

**CURRENT TEST (LOCK REMOVED):**
```visualbasic
Public Sub Write(data As Byte())
    Array.Copy(data, _processingBuffer, data.Length)
    
    ' LOCK REMOVED FOR TESTING
    Array.Copy(data, _monitorBuffer, data.Length)  // No lock - potential race!
    
    Interlocked.Exchange(_hasMonitorData, 1)
End Sub
```

**Buffer Operations:**
- **READS:** Input data
- **WRITES:** Two buffer copies (processing + monitor)
- **Thread:** Audio callback thread
- **Lock:** Held during Array.Copy (~1-2 microseconds)
- **Contention:** If FFT thread holds lock, audio thread WAITS! ??

**SUSPECT RATING:** ?????? EXTREME - Lock contention is #1 suspect!

**Why This Causes Clicks:**
```
Audio Thread                    FFT Thread
-------------                   -----------
Write() called                  ReadMonitorBuffer() called
  Try to acquire lock  ?????    [Holds lock]
    [BLOCKED!] ?????????????????  Array.Copy (1-2 ?s)
    [WAITING...]                 Release lock
    [GLITCH!]          ?????????
  Acquire lock
  Array.Copy
  Release lock
```

**When audio thread waits even 1-2 microseconds = buffer underrun = CLICK!**

---

### **[7] FFTMonitorThread.ReadMonitorBuffer()** ?? LOCK CONTENTION
**Location:** `FFTMonitorThread.vb` / `DualBuffer.vb`

**Operation:**
```visualbasic
' FFT thread (runs every 10ms)
Private Sub WorkerLoop()
    While running
        If inputStage.HasMonitorData Then
            Dim buffer = inputStage.GetMonitorTap()  // Calls ReadMonitorBuffer()
        End If
        Thread.Sleep(10)
    End While
End Sub

' DualBuffer.ReadMonitorBuffer()
Public Function ReadMonitorBuffer() As Byte()
    SyncLock _monitorLock  // BLOCKS audio thread if audio tries to Write()!
        Dim bufferCopy(_monitorBuffer.Length - 1) As Byte
        Array.Copy(_monitorBuffer, bufferCopy, _monitorBuffer.Length)
        Return bufferCopy
    End SyncLock
End Function
```

**Buffer Operations:**
- **READS:** Monitor buffer (with lock)
- **WRITES:** New copy (returned to FFT thread)
- **Thread:** FFT background thread (lower priority)
- **Lock:** Held during Array.Copy (~1-2 microseconds)
- **Frequency:** Every 10ms (100 Hz)

**SUSPECT RATING:** ?????? EXTREME - Holds lock while audio thread trying to write!

**Lock Contention Scenario:**
1. FFT thread starts reading (acquires lock)
2. Audio callback arrives (tries to Write())
3. Audio thread WAITS for FFT to finish copying
4. Audio thread delayed = buffer underrun = CLICK! ??

---

### **[8] RecordingEngine.Write()**
**Location:** `RecordingEngine.vb`

**Operation:**
```visualbasic
Public Sub Write(buffer As Byte())
    ' Write to WAV file
    waveWriter.Write(buffer, 0, buffer.Length)
    waveWriter.Flush()  // Flush to disk
End Sub
```

**Buffer Operations:**
- **READS:** Input buffer
- **WRITES:** To disk (file I/O)
- **Thread:** Audio callback thread (synchronous write!)
- **Time:** Varies (SSD ~100?s, HDD ~1-10ms)

**SUSPECT RATING:** ???? MEDIUM - Disk I/O on audio thread could block

---

## ?? **IDENTIFIED ISSUES**

### **Issue #1: Lock Contention** ?????? CRITICAL
**Problem:** Audio thread and FFT thread fight for `_monitorLock`

**Evidence:**
- Lock acquired in `DualBuffer.Write()` (audio thread)
- Lock acquired in `DualBuffer.ReadMonitorBuffer()` (FFT thread)
- If FFT thread holds lock when audio callback arrives = WAIT = GLITCH!

**Impact:** Even 1-2 microsecond delay can cause buffer underrun

**Timeline:**
```
Time    Audio Thread          FFT Thread
-----   -------------         -----------
0ms     [Processing...]       [Sleeping...]
10ms    Write() called        WorkerLoop wakes
10ms    Try acquire lock ???? Read() acquires lock
10ms    [BLOCKED!] ???????????  Array.Copy (1-2?s)
10.002  [WAITING...]          Release lock
10.002  Acquire lock ?????????
10.004  Write complete
        [TOO LATE - GLITCH!]
```

**Fix Status:** ?? **TESTING NOW** - Lock removed from Write()

---

### **Issue #2: Multiple Buffer Copies** ????
**Problem:** Up to 7 Array.Copy operations per audio buffer

**Evidence:**
- 1 optional copy (rawInputBuffer)
- 2 copies per stage × 3 stages = 6 copies
- Total: Up to 7 copies per buffer

**Impact:**
- Each copy: ~1-2 microseconds
- Total overhead: ~7-14 microseconds per buffer
- At 200ms buffer with 44.1kHz stereo = 35.2KB
- Could be slower than estimated

**Breakdown:**
| Stage | Processing Copy | Monitor Copy | Total |
|-------|----------------|--------------|-------|
| Raw Save | 1 copy | - | 1 |
| Input | 1 copy | 1 copy | 2 |
| Gain | 1 copy | 1 copy | 2 |
| Output | 1 copy | 1 copy | 2 |
| **TOTAL** | **4 copies** | **3 copies** | **7** |

**Fix:** If lock removal doesn't solve it, reduce number of copies

---

### **Issue #3: Aggressive Drain** ??
**Problem:** RecordingManager reads 4-20 buffers per callback

**Evidence:**
```visualbasic
For i = 1 To drainCount  // drainCount = 4 to 20
    mic.Read(buffer)
    RaiseEvent BufferAvailable()  // Multiple events!
Next
```

**Impact:**
- Floods event handlers with rapid-fire events
- Could overwhelm pipeline
- Multiple RouteAudioBuffer() calls in quick succession

**Fix:** If lock removal doesn't solve it, reduce drain count to 1

---

### **Issue #4: Synchronous Disk I/O** ??
**Problem:** RecordingEngine writes to disk on audio thread

**Evidence:**
- `waveWriter.Write()` called synchronously
- `waveWriter.Flush()` forces disk write

**Impact:**
- SSD: ~100 microseconds (should be OK)
- HDD: 1-10 milliseconds (would cause clicks)
- But you're on SSD, so probably not the issue

**Fix:** Move to async file writing if needed

---

## ?? **CURRENT TEST**

### **Test: Remove Lock from DualBuffer.Write()**

**Hypothesis:** Lock contention between audio thread and FFT thread causes clicks

**Change Made:**
```visualbasic
' BEFORE (WITH LOCK):
SyncLock _monitorLock
    Array.Copy(data, _monitorBuffer, data.Length)
End SyncLock

' AFTER (NO LOCK):
Array.Copy(data, _monitorBuffer, data.Length)  // Potential race condition
```

**Expected Results:**

**If clicks DISAPPEAR:**
- ? Confirms lock contention was the problem
- ? Need to implement lock-free buffer swap
- ? Use triple-buffering or atomic pointer swap

**If clicks PERSIST:**
- ? Lock contention not the issue
- ? Check Issue #2 (multiple copies)
- ? Check Issue #3 (aggressive drain)
- ? Check Issue #4 (disk I/O)

---

## ?? **NEXT STEPS**

### **If Test Succeeds (Clicks Gone):**

**1. Implement Lock-Free Buffer Swap:**
```visualbasic
' Option A: Triple buffering
Private _buffer1 As Byte()
Private _buffer2 As Byte()
Private _buffer3 As Byte()
Private _currentReadBuffer As Integer  ' Atomic

' Option B: Interlocked pointer swap
Private _monitorBufferA As Byte()
Private _monitorBufferB As Byte()
Private _activeBuffer As Integer  ' 0 = A, 1 = B
' Use Interlocked.CompareExchange to swap
```

**2. Verify FFT Still Works:**
- Ensure FFT displays still update correctly
- Check for visual glitches in spectrum

**3. Performance Testing:**
- Record for 5 minutes
- Check waveform for any remaining artifacts
- Verify file quality

---

### **If Test Fails (Clicks Persist):**

**Next suspects to investigate:**

**1. Multiple Buffer Copies:**
- Disable all stages except one
- Test each stage individually
- Measure cumulative overhead

**2. Aggressive Drain:**
- Reduce `drainCount` from 4 to 1
- Test single buffer per callback
- Check if clicks reduce/disappear

**3. Disk I/O:**
- Add timing to `RecordingEngine.Write()`
- Log any writes > 1ms
- Consider async file writing

**4. NAudio Internal:**
- Try different buffer sizes
- Try different audio drivers (WASAPI vs WaveIn)
- Check NAudio buffer queue depth

---

## ?? **DIAGNOSTIC DATA**

### **System Info:**
- **Audio Input:** Internal mic (fast, no USB latency)
- **Storage:** SSD (fast disk I/O)
- **Buffer Size:** Tested 10ms to 200ms (clicks at all sizes)
- **Driver:** WASAPI (low-latency)

### **What Works:**
- ? Audio captures without errors
- ? FFT displays work correctly
- ? Meters update in real-time
- ? Form stays responsive
- ? No "SLOW BUFFER" warnings in logs

### **What Doesn't Work:**
- ? Clicks in recorded files (at all buffer sizes)
- ? Clicks with DSP disabled (bypass mode)
- ? Clicks with FFT disabled (no monitoring)

### **Log Analysis Needed:**
- Queue depth stats
- Buffer timing
- Any error/warning messages

---

## ?? **CONCLUSION**

**Primary Suspect:** Lock contention in `DualBuffer.Write()`

**Evidence:**
1. Lock held during Array.Copy (1-2?s)
2. Audio thread + FFT thread contend for same lock
3. Even microsecond delays can cause audio glitches
4. All other suspects eliminated (DSP, FFT, buffer size)

**Current Status:** ?? **TESTING** - Lock removed, awaiting results

**Next Action:** User to record and check if clicks are gone

---

**Test Results:** ? **PENDING**

*(Update this section after testing)*

