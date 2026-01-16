# Timer Inventory and Architecture

**Date:** January 15, 2026  
**Purpose:** Complete inventory of all timers in the DSP_Processor application  
**Status:** ?? **REFERENCE DOCUMENT**

---

## ?? **Overview**

This document tracks all timers in the application, their purpose, intervals, and performance implications. Understanding timer interactions is critical for diagnosing audio glitches and ensuring smooth operation.

---

## ?? **Timer Summary Table**

| Timer Name | Location | Interval | Purpose | Thread | Performance Impact |
|------------|----------|----------|---------|--------|-------------------|
| **TimerAudio** | MainForm | 10ms | *DEPRECATED* (Legacy, unused) | UI Thread | ?? None (disabled) |
| **TimerPlayback** | MainForm | 100ms | Update playback progress bar | UI Thread | ? Low (UI only) |
| **TimerMeters** | MainForm | 20ms | Update VU meters display | UI Thread | ?? Medium (50 FPS) |
| **processingTimer** | RecordingManager | 10ms | **CRITICAL** Recording buffer processing | Timer Thread | ?? **HIGH** |
| **flushTimer** | Logger | 1000ms | Flush log buffer to disk | Background | ? Low |
| **animationTimer** | WaveformDisplayControl | 16ms | Waveform scroll animation | UI Thread | ?? Medium (60 FPS) |
| **updateTimer** | PlaybackEngine | 50ms | Update playback position | UI Thread | ? Low |

---

## ?? **Detailed Timer Analysis**

### **1. TimerAudio** ?? DEPRECATED

**Location:** `MainForm.Designer.vb` line 26, 92  
**Declaration:**
```visualbasic
TimerAudio = New Timer(components)
TimerAudio.Interval = 10  ' 10ms
```

**Status:** ?? **DEPRECATED - NO LONGER USED**

**Original Purpose:**
- Phase 0/1: Used for polling audio buffers
- Legacy audio processing loop

**Current Status:**
- Timer exists but handler is disconnected
- Left in Designer for backward compatibility
- **Does NOT fire during normal operation**

**Handler:** None (removed)

**Performance Impact:** ? None (disabled)

**Action:** Can be removed in future cleanup

---

### **2. TimerPlayback** ? LOW PRIORITY

**Location:** `MainForm.Designer.vb` line 35  
**Interval:** 100ms (10 FPS)

**Purpose:**
- Update playback progress bar position
- Update playback time display
- Non-critical UI visualization

**Handler:** `MainForm.TimerPlayback_Tick()`
```visualbasic
Private Sub TimerPlayback_Tick(sender As Object, e As EventArgs)
    ' Update progress bar
    ' Update time label
    ' Check if playback finished
End Sub
```

**Thread:** UI Thread (safe)

**Performance Impact:** ? **LOW**
- Only runs during playback
- 100ms interval (slow, UI-only)
- No audio processing
- No disk I/O

**When Active:**
- Only while playing back a file
- Disabled when not playing

---

### **3. TimerMeters** ?? MEDIUM PRIORITY

**Location:** `MainForm.Designer.vb` line 36  
**Interval:** 20ms (50 FPS)

**Purpose:**
- Update VU meter displays (meterRecording, meterPlayback)
- Refresh peak/RMS indicators
- Visual feedback for audio levels

**Handler:** `MainForm.TimerMeters_Tick()`
```visualbasic
Private Sub TimerMeters_Tick(sender As Object, e As EventArgs)
    ' Update meters
    ' Decay peak indicators
    ' Refresh clip indicators
End Sub
```

**Thread:** UI Thread (safe)

**Performance Impact:** ?? **MEDIUM**
- 50 FPS refresh rate
- 20ms interval = frequent redraws
- GDI+ drawing overhead
- CPU: ~1-2% per meter

**When Active:**
- Always running (from Form_Load)
- Even when not recording/playing

**Optimization Notes:**
- Could be throttled to 30 FPS (33ms) if needed
- Only redraws when levels change (current optimization)
- Uses double-buffering to prevent flicker

---

### **4. processingTimer** ?? **CRITICAL**

**Location:** `RecordingManager.vb` line 42  
**Interval:** 10ms (100 FPS)

**Purpose:** ?? **AUDIO RECORDING CRITICAL PATH**
- Read audio buffers from NAudio queue
- Write buffers to WAV file (disk I/O)
- Process multiple buffers per tick (4-8 iterations)
- Raise BufferAvailable events for FFT/metering

**Declaration:**
```visualbasic
Private processingTimer As Timer
processingTimer = New Timer()
processingTimer.Interval = 10  ' 10ms
AddHandler processingTimer.Tick, AddressOf OnProcessingTimerElapsed
```

**Handler:** `RecordingManager.OnProcessingTimerElapsed()`
```visualbasic
Private Sub OnProcessingTimerElapsed(sender As Object, e As EventArgs)
    ' RECORDING PATH (critical!)
    For i = 1 To processCount  ' 2-8 iterations
        recorder.Process()  // Read mic buffer + write to disk
    Next
    
    ' FFT/METERING PATH
    ReadForFFT()
    RaiseEvent BufferAvailable()
End Sub
```

**Thread:** Timer Thread (NOT audio driver thread, NOT UI thread)

**Performance Impact:** ?? **CRITICAL - HIGHEST PRIORITY**
- **This is the main suspect for clicks/pops!**
- 10ms interval = very tight deadline
- Processes 2-8 buffers per tick
- Each buffer: disk I/O + NAudio queue read
- If takes > 10ms ? next tick delayed ? queue overflow ? CLICKS!

**Call Stack (per tick):**
```
1. Timer fires (every 10ms)
   ?
2. OnProcessingTimerElapsed()
   ?
3. Loop: recorder.Process() × (2-8 times)
   ?
4. RecordingEngine.Process()
   ?
5. mic.Read(buffer)           // Read from NAudio
   ?
6. wavOut.Write(buffer)       // Write to disk (SLOW!)
   ?
7. Return to loop
   ?
8. ReadForFFT() + RaiseEvent
```

**Timing Breakdown (per loop iteration):**
| Operation | Time (ms) | Notes |
|-----------|-----------|-------|
| mic.Read() | 0.01-0.05 | Fast (memory) |
| wavOut.Write() | 0.05-0.20 | Medium (buffered I/O) |
| **Total per call** | **0.06-0.25** | Per buffer |
| **Total loop (8×)** | **0.48-2.0** | Best case |
| **Worst case (disk lag)** | **8-16** | **EXCEEDS 10ms!** |

**When Active:**
- Starts when recording begins
- Stops when recording ends
- Also runs when armed (for metering, non-recording path)

**Performance Issues:** ??
- **Main suspect for audio clicks/pops**
- Loop can take > 10ms when:
  - High queue depth (8 iterations)
  - Slow disk I/O
  - CPU contention
- When > 10ms ? Timer tick delayed ? NAudio queue overflow ? **CLICKS!**

**Current Mitigations:**
- ? Reduced max processCount from 16 ? 8
- ? No Flush() calls (already optimized)
- ? Added timing instrumentation (diagnostics)
- ?? **TODO:** Move disk I/O to background thread (async)

---

### **5. flushTimer** ? LOW PRIORITY

**Location:** `Logger.vb` line ~150  
**Interval:** 1000ms (1 second)

**Purpose:**
- Flush log buffer to disk periodically
- Ensure logs are written even if app doesn't close cleanly
- Reduce disk I/O frequency

**Handler:** `Logger.FlushTimer_Tick()`
```visualbasic
Private Sub FlushTimer_Tick(sender As Object, e As EventArgs)
    FlushBuffer()  // Write buffered logs to file
End Sub
```

**Thread:** Background/Timer thread

**Performance Impact:** ? **LOW**
- 1 second interval (very slow)
- Only flushes if buffer has data
- Async file writing
- No impact on audio

**When Active:**
- Always running (from Logger initialization)
- Minimal overhead

---

### **6. animationTimer** ?? MEDIUM PRIORITY

**Location:** `WaveformDisplayControl.vb` line ~260  
**Interval:** 16ms (60 FPS)

**Purpose:**
- Smooth waveform scrolling animation
- Update waveform position during playback
- Visual polish for playback visualization

**Handler:** `WaveformDisplayControl.AnimationTimer_Tick()`
```visualbasic
Private Sub AnimationTimer_Tick(sender As Object, e As EventArgs)
    ' Update scroll position
    ' Invalidate() for repaint
End Sub
```

**Thread:** UI Thread

**Performance Impact:** ?? **MEDIUM**
- 60 FPS = frequent redraws
- GDI+ rendering overhead
- Only during playback
- Can cause frame drops if CPU limited

**When Active:**
- Only during file playback
- Stopped when playback ends

**Optimization Notes:**
- Could throttle to 30 FPS if needed
- Uses double-buffering
- Only redraws changed region

---

### **7. updateTimer** ? LOW PRIORITY

**Location:** `PlaybackEngine.vb` line ~170  
**Interval:** 50ms (20 FPS)

**Purpose:**
- Update playback position tracking
- Sync playback state with UI
- Non-critical position updates

**Handler:** `PlaybackEngine.UpdateTimer_Tick()`
```visualbasic
Private Sub UpdateTimer_Tick(sender As Object, e As EventArgs)
    ' Update current position
    ' Check for end of playback
    ' Raise position changed event
End Sub
```

**Thread:** Background/Timer thread

**Performance Impact:** ? **LOW**
- 50ms interval (slow)
- Simple position calculation
- No disk I/O
- No audio processing

**When Active:**
- Only during playback
- Disabled when not playing

---

## ?? **Critical Timer Interactions**

### **Problem: Timer Contention**

**Scenario:** Multiple timers firing simultaneously

```
Time    TimerMeters    processingTimer    Impact
-----   -----------    ---------------    -------
0ms     TICK           -                  OK
10ms    -              TICK               OK
20ms    TICK           TICK               CONFLICT!
30ms    -              TICK               OK
40ms    TICK           TICK               CONFLICT!
```

**When TimerMeters (20ms) and processingTimer (10ms) fire together:**
- Both compete for UI thread resources
- Meter drawing blocks briefly
- Processing timer may be delayed by ~1-2ms
- Usually OK, but adds latency

**Mitigation:**
- TimerMeters uses BeginInvoke (async)
- Processing timer is on separate thread
- Usually no conflict

---

### **Problem: Processing Timer Exceeding Interval**

**Scenario:** Loop takes longer than 10ms

```
Time    Timer Event              NAudio Queue    Result
-----   ----------------------   ------------    -------
0ms     Tick ? Start loop        Depth: 50       Processing...
8ms     Still processing...      Depth: 65       +15 buffers
12ms    Loop done (LATE!)        Depth: 75       +10 more
12ms    SHOULD have ticked       Depth: 75       MISSED!
20ms    Next tick (delayed)      Depth: 95       Catching up...
```

**When loop takes > 10ms:**
1. Next timer tick is DELAYED
2. NAudio queue keeps filling
3. Queue depth grows
4. Eventually overflows
5. **Oldest buffers dropped = CLICK!**

**Current Status:** ?? **ACTIVE ISSUE - CAUSING CLICKS**

**Solutions in Progress:**
1. ? Reduced processCount (50% reduction)
2. ? Added timing diagnostics
3. ?? **TODO:** Async file writing (move to background thread)

---

## ?? **Timer Best Practices**

### **DO:**
? Use timers for UI updates (meters, progress bars)  
? Keep timer handlers SHORT (< 5ms)  
? Use BeginInvoke for UI updates from timers  
? Use background threads for I/O  
? Monitor timer performance with diagnostics  

### **DON'T:**
? Do disk I/O in timer handlers  
? Block UI thread in timers  
? Create tight loops in timers  
? Use multiple timers with same interval (phase conflicts)  
? Assume timers fire precisely  

---

## ?? **Diagnostic Commands**

### **Check Timer Performance:**

**In processingTimer handler:**
```visualbasic
' Already added:
_processingStopwatch.Restart()
recorder.Process()
_processingStopwatch.Stop()

' Log if > 1ms:
If elapsed > 1.0 Then
    Logger.Instance.Warning($"SLOW Process(): {elapsed:F2}ms")
End If
```

**Check logs for:**
- "SLOW Process() call" warnings
- "Process() loop EXCEEDED timer interval" warnings
- Performance stats every 10 seconds

---

## ?? **Timer Priority Levels**

| Priority | Timers | Interval Range | Purpose |
|----------|--------|----------------|---------|
| **CRITICAL** ?? | processingTimer | 10ms | Audio recording |
| **HIGH** ?? | TimerMeters, animationTimer | 16-20ms | Real-time UI |
| **MEDIUM** ? | updateTimer | 50ms | Position tracking |
| **LOW** ? | TimerPlayback, flushTimer | 100-1000ms | Non-critical updates |
| **DISABLED** ?? | TimerAudio | N/A | Deprecated |

---

## ?? **Recommendations**

### **Immediate Actions:**
1. ? **DONE:** Reduce processingTimer load (50% reduction)
2. ?? **IN PROGRESS:** Monitor timing with diagnostics
3. ?? **NEXT:** Implement async file writing

### **Future Optimizations:**
1. Consider consolidating TimerMeters + animationTimer (both ~20ms)
2. Make TimerPlayback adaptive (slower when not visible)
3. Move flushTimer to async Task.Delay pattern
4. Remove deprecated TimerAudio

### **Performance Monitoring:**
- Watch for "EXCEEDED timer interval" warnings
- Track average processing times
- Correlate slow calls with clicks in audio
- Log queue depths during recording

---

## ?? **Change Log**

**January 15, 2026:**
- Initial document created
- Identified processingTimer as click/pop suspect
- Reduced max processCount from 16 ? 8
- Added timing instrumentation
- Documented all 7 timers in application

---

## ?? **Related Documents**

- [Hypothesis: recorder.Process() Timing Issue](Hypothesis-RecorderProcess-Timing-2026-01-15.md)
- [Buffer Analysis: Clicks Investigation](Buffer-Analysis-Clicks-Investigation-2026-01-15.md)
- Task-Phase3-Audio-Integration.md

---

**Summary:** The application has 7 timers, with `processingTimer` being the most critical for audio quality. It's currently the main suspect for clicks/pops due to potential interval overruns during disk I/O operations.
