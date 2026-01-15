# Audio Pipeline Architecture: Current vs Intended

**Date:** January 15, 2026  
**Status:** ?? **ARCHITECTURE MISMATCH IDENTIFIED**  
**Purpose:** Document current implementation vs intended design to guide refactoring

---

## ?? User's Intended Architecture

### **Core Concept:**
```
MicInputSource.OnDataAvailable() (every 20ms)
    ?
    OPTION A: Bypass DSP (Direct)
        ?
        Split to 2 buffers:
        ??? Recording Engine Buffer
        ??? Meters Buffer (Level + FFT Pre)
    
    OR
    
    OPTION B: Through DSP Pipeline
        ?
        Gain Stage (input level control)
        ?
        Split to 2 buffers:
        ??? Recording Engine Buffer
        ??? Meters Buffer (Level + FFT Pre)
```

### **Key Design Principles:**
1. ? **Single path** - Bypass DSP OR use DSP (NOT both simultaneously)
2. ? **Simple gain stage** - Set input levels
3. ? **Two output buffers** - Recording + Meters (separate concerns)
4. ? **Clean separation** - Recording independent from metering

---

## ?? Current Implementation (ACTUAL)

### **What Actually Happens:**
```
MicInputSource.OnDataAvailable() (every 20ms)
    ?
    Array.Copy ? bufferQueue.Enqueue()     [Recording Queue]
    ?
    Array.Copy ? fftQueue.Enqueue()        [FFT Queue]
    ? (callback returns)

RecordingManager.ProcessingTimer_Tick() (every 20ms)
    ?
    recorder.Process()
        ?
        InputSource.Read(bufferQueue)      [Consumes Recording Queue]
        ?
        wavOut.Write()                      [Writes to file]
    ?
    ReadForFFT(fftQueue)
        ?
        RaiseEvent BufferAvailable()
            ?
            MainForm.OnRecordingBufferAvailable()
                ??? AudioLevelMeter.AnalyzeSamples()  [SYNCHRONOUS]
                ??? Array.Copy (buffer clone!)
                ??? Task.Run(FFT calculation)         [ASYNC]
```

### **Problems with Current Implementation:**

1. **? No DSP Integration**
   - Recording goes directly to file (no DSP!)
   - No gain stage
   - No processing pipeline

2. **? Multiple Buffer Copies**
   - Copy #1: OnDataAvailable ? bufferQueue
   - Copy #2: OnDataAvailable ? fftQueue
   - Copy #3: MainForm ? FFT async buffer
   - **Total: 3 buffer copies per callback!**

3. **? Event-Based Architecture**
   - RecordingManager raises BufferAvailable event
   - MainForm handles synchronously
   - Can block if handlers are slow
   - No control over when metering happens

4. **? Competing Timers**
   - RecordingManager timer: 20ms
   - Audio callback: 20ms
   - MainForm processing: Variable
   - **Race conditions possible!**

5. **? No Bypass Option**
   - Can't choose DSP vs Direct
   - Recording is always direct
   - No path through DSP at all

---

## ?? Architecture Comparison

### **Recording Path:**

**INTENDED:**
```
Mic ? [DSP Option] ? Gain Stage ? Recording Buffer ? File
```

**ACTUAL:**
```
Mic ? bufferQueue ? RecordingEngine.Process() ? File
(No DSP, No Gain, No choice!)
```

### **Metering Path:**

**INTENDED:**
```
Mic ? [DSP Option] ? Gain Stage ? Meters Buffer ? Level/FFT Display
```

**ACTUAL:**
```
Mic ? fftQueue ? Event ? MainForm ? Level Meter (sync) + FFT (async)
(Separate queue, no DSP, event-based!)
```

### **DSP Pipeline:**

**INTENDED:**
```
Active during recording, optional bypass
```

**ACTUAL:**
```
Only active during PLAYBACK!
Not connected to recording at all!
```

---

## ?? Intended Architecture (Detailed)

### **Phase 1: Audio Input**
```
Windows Audio Callback (every 20ms)
    ?
MicInputSource.OnDataAvailable(buffer)
    ?
    [No immediate copies!]
    ?
Place in INPUT BUFFER (single buffer, not queue)
```

### **Phase 2: Processing Path Selection**
```
IF [DSP Bypass Enabled]:
    ?
    Input Buffer ? Direct Path
        ?
        [No processing]
        ?
        Split to outputs
        
ELSE [DSP Pipeline Enabled]:
    ?
    Input Buffer ? DSP Pipeline
        ?
        Gain Stage (input level adjustment)
        ?
        [Future: Filters, Effects]
        ?
        Split to outputs
```

### **Phase 3: Output Distribution**
```
Processed/Bypassed Audio
    ?
    Fork to TWO buffers (not queues!):
    ??? Recording Buffer
    ?   ??? RecordingEngine reads
    ?       ??? Writes to file
    ?
    ??? Meters Buffer
        ??? Metering thread reads
            ??? Level Meter (peak/RMS)
            ??? FFT Display (spectrum)
```

### **Key Differences:**

| Aspect | Current | Intended |
|--------|---------|----------|
| DSP | Only playback | Recording + Playback |
| Bypass Option | None | Yes (direct or DSP) |
| Buffer Copies | 3 per callback | 1 per callback |
| Queues | 2 (recording + FFT) | 0 (use buffers) |
| Gain Stage | None | Yes (in DSP) |
| Event-Based | Yes (slow) | No (polling) |
| Synchronization | Complex (timers + events) | Simple (single pipeline) |

---

## ?? Why Current Implementation Fails

### **Problem 1: Dual Queue System**
```
OnDataAvailable() creates 2 copies ? 2 queues
    ?
RecordingEngine drains bufferQueue
MainForm drains fftQueue
    ?
If MainForm is slow (FFT processing):
    ?
fftQueue backs up
    ?
But bufferQueue ALSO backs up (why?)
    ?
Because OnDataAvailable is BLOCKED waiting for queues!
```

**Root Cause:** Enqueuing in callback can block if queue operations are contended!

### **Problem 2: No Drain During Recording**
```
Our "aggressive drain" code:
    If recorder IsNot Nothing Then
        ' Drain recording queue
    ElseIf mic IsNot Nothing Then
        ' Drain armed queue
```

**Issue:** During recording, ONLY `recorder.Process()` drains!
- It reads 4KB per call
- Even with 16x calls = 64KB per 20ms tick
- But audio produces 176KB/sec ÷ 50 ticks/sec = 3.5KB per tick
- Should be enough, BUT...

**Hidden Issue:** `recorder.Process()` is TOO SLOW!
- InputSource.Read() ? 4KB
- wavOut.Write() ? **BLOCKS ON DISK I/O!**
- Even with async logging, write is synchronous!

### **Problem 3: Event Handler Blocking**
```
ProcessingTimer_Tick()
    ?
    RaiseEvent BufferAvailable()
        ?
        MainForm.OnRecordingBufferAvailable()
            ??? AudioLevelMeter.AnalyzeSamples() [SYNC - 1-2ms!]
            ??? Array.Copy() [1ms!]
            ??? Task.Run(FFT) [thread pool!]
        ?
        (Timer has to wait for event handler to complete!)
```

If event handler takes 5ms, timer is already behind by 5ms!
Next tick is late ? drain is late ? queue builds up!

---

## ?? Target Architecture (What We Need)

### **Option A: Minimal Changes (Quick Fix)**
```
Current architecture but:
1. Throttle MainForm.OnRecordingBufferAvailable() to 50ms intervals
2. Remove synchronous processing from event handler
3. Make RecordingEngine.Process() non-blocking
```

**Pros:** Small changes, quick to implement  
**Cons:** Still has fundamental architecture issues

### **Option B: User's Intended Architecture (Proper Fix)**
```
1. Remove dual queue system
2. Implement single-buffer pipeline:
   Mic ? [DSP Option] ? Gain ? Split(Recording, Meters)
3. Add bypass option to UI
4. Integrate DSP into recording path
5. Separate metering to dedicated thread
```

**Pros:** Clean architecture, future-proof, user's vision  
**Cons:** Significant refactoring, 4-6 hours work

### **Option C: Hybrid Approach (Recommended)**
```
Phase 1 (Now - 1 hour):
1. Fix immediate buffer overflow:
   - Throttle MainForm event handler
   - Move level meter to background
   - Keep FFT at 50ms intervals
2. Add DSP bypass option (UI only)
3. Get clean recordings

Phase 2 (Later - 4 hours):
1. Refactor to single-buffer pipeline
2. Integrate DSP into recording
3. Implement proper gain stage
4. Clean up architecture
```

**Pros:** Quick win now, proper fix later  
**Cons:** Temporary solution, still needs refactor

---

## ?? Architecture Mapping

### **Current Data Flow (Line-by-Line):**

**1. Audio Callback (MicInputSource.vb line 56):**
```vb
Private Sub OnDataAvailable(sender As Object, e As WaveInEventArgs)
    If _disposed Then Return  ' Line 58
    
    If e.BytesRecorded > 0 Then  ' Line 61
        ' RECORDING QUEUE
        Dim recordingBuffer(e.BytesRecorded - 1) As Byte  ' Line 66
        Array.Copy(e.Buffer, recordingBuffer, e.BytesRecorded)  ' Line 67
        bufferQueue.Enqueue(recordingBuffer)  ' Line 68 - COPY #1
        
        ' FFT QUEUE  
        If fftQueue.Count < MAX_FFT_QUEUE_DEPTH Then  ' Line 80
            Dim fftBuffer(e.BytesRecorded - 1) As Byte  ' Line 81
            Array.Copy(e.Buffer, fftBuffer, e.BytesRecorded)  ' Line 82
            fftQueue.Enqueue(fftBuffer)  ' Line 83 - COPY #2
        End If
    End If
End Sub
```

**2. Recording Drain (RecordingManager.vb line 381):**
```vb
If recorder IsNot Nothing AndAlso recorder.InputSource IsNot Nothing Then
    ' Aggressive drain attempts (our added code - DOESN'T WORK!)
    If currentQueueDepth > 10 Then
        For i = 1 To drainCalls
            Dim throwaway(4095) As Byte
            mic.Read(throwaway, 0, throwaway.Length)  ' Discard!
        Next
    End If
    
    ' Normal Process() calls
    For i = 1 To processCount
        recorder.Process()  ' RecordingEngine.vb line 185
    Next
End If
```

**3. Recording Engine (RecordingEngine.vb line 185):**
```vb
Dim buffer(4095) As Byte
Dim read = InputSource.Read(buffer, 0, buffer.Length)  ' MicInputSource.Read()
If read > 0 Then
    wavOut.Write(buffer, read)  ' BLOCKS ON DISK I/O!
End If
```

**4. MicInputSource.Read() (MicInputSource.vb line 169):**
```vb
Public Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
    Dim totalRead = 0
    While totalRead < count AndAlso bufferQueue.TryDequeue(sourceBuffer) Then
        ' Copy from queue to buffer
        Array.Copy(sourceBuffer, 0, buffer, offset + totalRead, bytesToCopy)
        totalRead += bytesToCopy
    End While
    Return totalRead
End Function
```

**5. FFT Path (RecordingManager.vb line 444):**
```vb
Dim fftRead = micSource.ReadForFFT(fftBuffer, 0, fftBuffer.Length)
If fftRead > 0 Then
    RaiseEvent BufferAvailable(Me, args)  ' ? MainForm
End If
```

**6. MainForm Handler (MainForm.vb line 295):**
```vb
Private Sub OnRecordingBufferAvailable(sender As Object, e As AudioBufferEventArgs)
    ' SYNCHRONOUS - BLOCKS TIMER!
    Dim levelData = AudioLevelMeter.AnalyzeSamples(e.Buffer, ...)  ' 1-2ms
    meterRecording.SetLevel(...)
    
    ' ASYNC - BUT STILL COPIES!
    If Not fftProcessingInProgress Then
        Dim bufferCopy(e.Buffer.Length - 1) As Byte  ' COPY #3
        Array.Copy(e.Buffer, bufferCopy, e.Buffer.Length)
        Task.Run(Sub() ProcessFFTAsync(bufferCopy, ...))
    End If
End Sub
```

---

## ?? Intended Data Flow (User's Vision)

**1. Audio Callback:**
```vb
Private Sub OnDataAvailable(sender As Object, e As WaveInEventArgs)
    If _disposed Then Return
    
    ' Place in single INPUT buffer (not queue!)
    inputBuffer.Write(e.Buffer, 0, e.BytesRecorded)
    
    ' Signal processing thread
    processingSignal.Set()
End Sub
```

**2. Processing Thread:**
```vb
Private Sub ProcessingLoop()
    While isRunning
        processingSignal.WaitOne(20)  ' Wait for signal or timeout
        
        ' Read from input buffer
        Dim bytesRead = inputBuffer.Read(tempBuffer, 0, tempBuffer.Length)
        If bytesRead = 0 Then Continue
        
        ' Apply DSP or bypass
        Dim processedBuffer() As Byte
        If dspBypassEnabled Then
            processedBuffer = tempBuffer  ' Direct
        Else
            processedBuffer = ApplyDSP(tempBuffer)  ' Gain + effects
        End If
        
        ' Fork to outputs
        If isRecording Then
            recordingBuffer.Write(processedBuffer)
        End If
        metersBuffer.Write(processedBuffer)
    End While
End Sub
```

**3. Recording Consumer:**
```vb
' Separate thread/timer
Private Sub RecordingLoop()
    While isRecording
        Dim bytesRead = recordingBuffer.Read(buffer, 0, buffer.Length)
        If bytesRead > 0 Then
            wavOut.Write(buffer, bytesRead)
        End If
    End While
End Sub
```

**4. Metering Consumer:**
```vb
' Separate thread, 50ms intervals
Private Sub MeteringLoop()
    While isRunning
        Thread.Sleep(50)  ' Throttled to 20 FPS
        
        Dim bytesRead = metersBuffer.Read(buffer, 0, buffer.Length)
        If bytesRead > 0 Then
            ' Update level meter
            UpdateLevels(buffer)
            
            ' Update FFT (if enough data)
            If fftDataReady Then
                UpdateFFT(buffer)
            End If
        End If
    End While
End Sub
```

---

## ?? Immediate Action Plan

### **Phase 1: Stop the Bleeding (30 minutes)**

**Fix 1: Throttle MainForm Event Handler**
```vb
' MainForm.vb - Add throttling
Private lastMeteringTime As DateTime = DateTime.MinValue

Private Sub OnRecordingBufferAvailable(...)
    ' Throttle to 50ms (20 FPS)
    If DateTime.Now.Subtract(lastMeteringTime).TotalMilliseconds < 50 Then
        Return  ' Skip this buffer
    End If
    lastMeteringTime = DateTime.Now
    
    ' Rest of handler...
End Sub
```

**Fix 2: Move Level Meter to Background**
```vb
' Move AudioLevelMeter.AnalyzeSamples() to Task.Run()
Task.Run(Sub()
    Dim levelData = AudioLevelMeter.AnalyzeSamples(...)
    Me.BeginInvoke(Sub() meterRecording.SetLevel(...))
End Sub)
```

**Fix 3: Add DSP Bypass UI**
```vb
' AudioSettingsPanel.vb - Add checkbox
CheckBox "Enable DSP Pipeline"
Default: Unchecked (bypass for now)
```

**Expected Result:** Buffer overflow should stop, recordings clean!

### **Phase 2: Architecture Refactor (4-6 hours, future session)**

1. Remove dual queue system
2. Implement single-buffer pipeline
3. Add proper DSP integration
4. Separate metering thread
5. Implement gain stage

---

## ? Decision Matrix

### **What to Do NOW:**
1. ? Implement Phase 1 fixes (30 minutes)
2. ? Test all 4 configurations
3. ? Get clean recordings
4. ? Fix playback timer (bonus)

### **What to Do LATER:**
1. ?? Architecture refactor (Phase 2)
2. ?? DSP integration
3. ?? Proper gain stage

### **What to DOCUMENT:**
1. ? Current vs Intended architecture (this document)
2. ? Refactoring roadmap
3. ? Testing protocol

---

**Ready to implement Phase 1 fixes?** This should finally stop the buffer overflow! ??
