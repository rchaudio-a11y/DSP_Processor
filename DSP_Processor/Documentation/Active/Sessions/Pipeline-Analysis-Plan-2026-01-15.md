# Audio Pipeline Analysis & Investigation Plan

**Date:** January 15, 2026  
**Status:** ?? **CRITICAL - STOP ALL CHANGES**  
**Priority:** **HIGHEST**  
**Reason:** Multiple attempted fixes have made problem WORSE, not better

---

## ?? Current Crisis

### **Facts:**
1. Recording worked well at several points during development
2. Our "fixes" keep breaking it
3. Queue overflow grows to 4700+ buffers during recording
4. Aggressive drain code we added ISN'T RUNNING (proven by log)
5. We don't understand why

### **Log Evidence (DSP_Processor_20260115_133139.log):**
```
13:34:21 - Queue: 3169 buffers, 3218 overflows
13:34:26 - Queue: 3419 buffers, 3468 overflows
13:34:31 - Queue: 3669 buffers, 3718 overflows
...
13:35:11 - Recording stopped: 49.5s
13:35:11 - Queue: 4668 buffers, 4717 overflows (95 seconds of audio!)
```

**During 49-second recording, queue grew by 250 buffers every 5 seconds = 50 buffers/sec**

At 44.1kHz stereo 16-bit:
- 50 buffers/sec × 4KB/buffer = 200 KB/sec growth
- Production rate: 176 KB/sec
- **Queue growing faster than production = NOTHING IS DRAINING!**

---

## ?? What We KNOW Worked

### **Version That Worked (Before Today's Changes):**
Need to identify:
1. Which commit/version had clean recordings?
2. What was the buffer architecture?
3. What was the drain strategy?
4. Were there any overflow warnings?

**Action:** Review Git history to find last known-good version

---

## ?? Complete Pipeline Analysis

### **Audio Callback Thread (Windows Audio)**
```
Windows Audio Device
    ? (callback every 10-20ms)
MicInputSource.OnDataAvailable() / WasapiEngine.OnDataAvailable()
    ? Array.Copy #1
bufferQueue.Enqueue()  // Recording queue
    ? Array.Copy #2
fftQueue.Enqueue()     // FFT queue
    ? (callback returns)
```

**Timing:**
- WASAPI: 10ms buffer (960 bytes at 48kHz)
- WaveIn: 20ms buffer (1764 bytes at 44.1kHz)
- Array.Copy: ~1µs per buffer
- Enqueue: ~1µs
- **Total callback time: ~3-5µs (should be fine)**

**Questions:**
1. Are callbacks actually firing every 10/20ms?
2. Is callback thread being starved?
3. Are Array.Copy operations blocking?

---

### **Processing Timer Thread (RecordingManager)**
```
Timer fires every 20ms
    ?
ProcessingTimer_Tick()
    ?
If recording:
    ? SHOULD call aggressive drain (direct read)
    ? SHOULD call recorder.Process() multiple times
    ?
If armed only:
    ? SHOULD call aggressive drain (20x)
    ?
ReadForFFT() from fftQueue
    ?
Raise BufferAvailable event
```

**Questions:**
1. Is timer actually firing every 20ms?
2. Is ProcessingTimer_Tick() actually executing?
3. Are the if/else branches correct?
4. Is mic.Read() actually draining?
5. Is recorder.Process() actually draining?

---

### **Recording Thread (RecordingEngine)**
```
recorder.Process() called
    ?
InputSource.Read(buffer, 0, 4096)
    ?
wavOut.Write(buffer, read)
    ?
Returns
```

**Questions:**
1. Is InputSource.Read() getting data from bufferQueue?
2. Is Read() actually dequeuing buffers?
3. Is wavOut.Write() blocking?
4. How long does Process() actually take?

---

## ?? Suspected Issues

### **Hypothesis 1: Timer Not Firing**
**Evidence:** Queue grows linearly ? suggests no drain at all
**Test:** Add logging at start of ProcessingTimer_Tick()
**Impact:** If true, ALL drain code is never running

### **Hypothesis 2: Condition Branch Wrong**
**Evidence:** We have complex if/else in ProcessingTimer_Tick()
```vb
If recorder IsNot Nothing AndAlso recorder.InputSource IsNot Nothing Then
    ' Recording drain
ElseIf mic IsNot Nothing Then
    ' Armed drain
End If
```
**Test:** Log which branch executes
**Impact:** If wrong branch, aggressive drain never runs

### **Hypothesis 3: InputSource.Read() Not Draining**
**Evidence:** Calling Process() 16x should drain 64KB, but queue grows
**Test:** Add logging inside MicInputSource.Read() to show actual dequeue
**Impact:** If true, drain calls do nothing

### **Hypothesis 4: Multiple MicInputSource Instances**
**Evidence:** Settings changes might create new instances without disposing old
**Test:** Add logging in MicInputSource constructor/destructor
**Impact:** If true, we're draining wrong instance

### **Hypothesis 5: ConcurrentQueue Deadlock/Corruption**
**Evidence:** Unlikely but possible
**Test:** Add queue health checks
**Impact:** If true, need different queue implementation

---

## ?? Investigation Plan

### **Phase 1: Instrumentation (NO CODE CHANGES!)**

**Step 1.1: Add Diagnostic Logging**
```vb
' RecordingManager.ProcessingTimer_Tick() - First line
Logger.Instance.Debug($"Timer tick: recording={_isRecording}, armed={_isArmed}")

' MicInputSource.Read() - First line
Logger.Instance.Debug($"Read called: queue={bufferQueue.Count}")

' RecordingEngine.Process() - First line
Logger.Instance.Debug($"Process called: InputSource={InputSource IsNot Nothing}")
```

**Step 1.2: Run Test**
- Arm mic
- Wait 10 seconds
- Start recording
- Record 10 seconds
- Stop recording
- Analyze log

**Step 1.3: Analyze Results**
- Is timer ticking?
- Which branch executes?
- Is Read() being called?
- Is Process() being called?
- What are actual queue depths?

---

### **Phase 2: Identify Root Cause**

Based on Phase 1 results, determine:
1. **IF** timer is ticking ? Problem is in drain logic
2. **IF** timer NOT ticking ? Problem is timer setup
3. **IF** Read() not draining ? Problem is in queue implementation
4. **IF** wrong branch executing ? Problem is condition logic

---

### **Phase 3: Minimal Fix**

**DO NOT** make changes until we understand:
1. Exactly which component is failing
2. Exactly why it's failing
3. Exactly what the fix should be

**Possible fixes based on root cause:**
- Timer not firing ? Fix timer initialization
- Wrong branch ? Fix condition
- Read() not draining ? Fix queue implementation
- Multiple instances ? Fix lifecycle management

---

### **Phase 4: Verification**

After fix:
1. Test armed idle (2 minutes)
2. Test recording (1 minute)
3. Test driver switching
4. Verify queue stays < 10 buffers
5. Verify no overflow warnings
6. Verify clean audio

---

## ?? Known Configuration Matrix

### **What Works:**
- ? WASAPI format conversion (48kHz float ? 16-bit PCM)
- ? Driver-specific defaults (44.1kHz vs 48kHz)
- ? Async logging (no disk I/O blocking)
- ? Ghost callback prevention (disposal flag)

### **What's Broken:**
- ? Buffer draining during recording
- ? Buffer draining when armed
- ? Queue management (grows to 4700+ buffers)

### **What's Unknown:**
- ? Is timer firing?
- ? Is drain code executing?
- ? Is Read() actually dequeuing?
- ? Are we draining the correct instance?

---

## ?? Success Criteria

### **Minimal Viable Fix:**
1. Queue stays < 20 buffers (armed or recording)
2. No overflow warnings
3. Clean audio (no clicks/pops)
4. Works for all 4 configurations:
   - WaveIn Mic
   - WaveIn Stereo Mix
   - WASAPI Mic
   - WASAPI Stereo Mix

### **Understanding Requirements:**
1. Know exactly which code path executes
2. Know exactly why drain isn't working
3. Know exactly what fix is needed
4. Have confidence fix won't break again

---

## ?? What NOT To Do

1. ? **Don't add more aggressive drain** - Current code should be enough
2. ? **Don't add more adaptive logic** - We have plenty already
3. ? **Don't change timer interval** - 20ms is correct
4. ? **Don't modify queue implementation** - ConcurrentQueue is fine
5. ? **Don't guess at solutions** - Diagnose first, fix second

---

## ?? Next Session

### **Preparation:**
1. Review last known-good version from Git
2. Compare current code to working version
3. Identify what changed
4. Prepare diagnostic logging strategy

### **Execution:**
1. Add minimal diagnostic logging (NO logic changes)
2. Run controlled test
3. Analyze log systematically
4. Identify root cause with certainty
5. Plan minimal fix
6. Implement fix
7. Verify thoroughly

### **Time Estimate:**
- Diagnostics: 30 minutes
- Root cause analysis: 30 minutes
- Fix implementation: 30 minutes
- Verification: 30 minutes
- **Total: 2 hours**

---

## ?? Key Insights

### **User's Observation (Critical!):**
> "its not the os, its not the hardware or drivers, its the program, I was recording very well at severel points in the design of the system, its only when we make changes that it comes back."

**This tells us:**
1. ? Hardware/OS/drivers are fine
2. ? Our architecture CAN work
3. ? Our recent changes broke it
4. ? We don't understand why

### **Pattern Recognition:**
Every "fix" we tried made it worse:
1. Added async logging ? Still clicks
2. Added adaptive drain ? Still clicks, queue grew
3. Added aggressive direct drain ? Even WORSE, queue to 4700+

**Conclusion:** We're not fixing the problem, we're adding code that doesn't execute or makes things worse.

---

## ?? Debugging Strategy

### **Scientific Method:**
1. **Observe** - Queue grows to 4700+ buffers
2. **Hypothesize** - Drain code not executing
3. **Test** - Add logging to prove/disprove
4. **Analyze** - Determine why not executing
5. **Fix** - Minimal change to fix root cause
6. **Verify** - Confirm fix works

### **NOT:**
1. ? Guess at solutions
2. ? Add more complex logic
3. ? Try different approaches hoping one works
4. ? Make changes without understanding

---

## ?? Documentation Reference

### **Related Documents:**
- [Bug-Report-2026-01-14-Recording-Clicks-Pops.md](../Issues/Bug-Report-2026-01-14-Recording-Clicks-Pops.md) - Original clicks fix (worked!)
- [Bug-Fix-2026-01-15-WASAPI-Buffer-Overflow.md](../Issues/Bug-Fix-2026-01-15-WASAPI-Buffer-Overflow.md) - Adaptive drain (didn't work)
- [Bug-Fix-2026-01-15-Async-Logging.md](../Issues/Bug-Fix-2026-01-15-Async-Logging.md) - Async logging (didn't fix clicks)

### **Code Locations:**
- `Managers\RecordingManager.vb` - ProcessingTimer_Tick() [Lines 376-480]
- `AudioIO\MicInputSource.vb` - Read() [Lines 169-183]
- `Recording\RecordingEngine.vb` - Process() [Lines 160-243]

---

## ? Agreement

**Before making ANY code changes:**
1. Add diagnostic logging
2. Run controlled test
3. Analyze results
4. Understand root cause
5. Get user confirmation on diagnosis
6. Plan minimal fix
7. Implement fix
8. Verify fix

**No more guessing. No more band-aids. Systematic diagnosis and targeted fix.**

---

**Analysis Date:** January 15, 2026 13:40  
**Status:** ?? **AWAITING DIAGNOSIS**  
**Next Action:** Add diagnostic logging, run test, analyze  
**Estimated Resolution:** 2 hours with proper diagnosis

---

**This is the right approach. Thank you for stopping us from making it worse.** ??
