# Audio Pipeline Diagnostic Plan
**Date:** 2024-01-14
**Issue:** All audio plays at 2× speed after optimization changes
**Status:** INVESTIGATION IN PROGRESS

---

## Observation Summary

### What We Know:
1. **All files play at 2× speed** (not file-specific)
2. **File is correct:** 44.1kHz, 1ch, 16-bit PCM
3. **Worked before optimization changes**
4. **Logs show:** 
   - Input: 1024 bytes float ? 512 bytes PCM16 ? (conversion looks correct)
   - **375,248 samples DROPPED** (62% of audio!) ?
   - DSP processed 604,296 samples but dropped most

### Critical Clue:
**"Dropped: 375248"** means the output buffer is **constantly full**. This suggests:
- WaveOut isn't consuming fast enough, OR
- We're producing too much data too fast, OR
- The format WaveOut sees is wrong

---

## Diagnostic Strategy

### Phase 1: Verify Format Chain
Track the WaveFormat object through every stage:

```
AudioFileReader ? fileReader.WaveFormat (IEEE Float)
                      ?
              pcm16Format (created NEW)
                      ?
              DSPThread.Format
                      ?
              DSPOutputProvider.WaveFormat
                      ?
              WaveOutEvent sees format
```

**Question:** Is the format getting corrupted or changed anywhere?

### Phase 2: Verify Byte Flow
Track actual byte counts at each stage:

```
File Read:     ? bytes float
   ?
Conversion:    ? bytes PCM16
   ?
Ring Buffer:   ? bytes written/available
   ?
DSP Process:   ? bytes read/written
   ?
WaveOut Pull:  ? bytes requested/provided
```

**Question:** Are we producing bytes at the correct rate?

### Phase 3: Verify Sample Rate
Check what WaveOut actually sees:

```
waveOut.Init(dspOutputProvider)
    ?
waveOut sees: dspOutputProvider.WaveFormat
    ?
Which is: dspThread.Format
    ?
Which is: pcm16Format (44.1kHz, 1ch, 16-bit)
```

**Question:** Is WaveOut playing at 44.1kHz or something else?

---

## Diagnostic Code to Add

### 1. Log Format at Every Stage

```visualbasic
' In StartDSPPlayback():
Utils.Logger.Instance.Info($"FILE format: {fileReader.WaveFormat.SampleRate}Hz, {fileReader.WaveFormat.Channels}ch, {fileReader.WaveFormat.BitsPerSample}bit, {fileReader.WaveFormat.Encoding}, BlockAlign={fileReader.WaveFormat.BlockAlign}, AvgBytes/sec={fileReader.WaveFormat.AverageBytesPerSecond}", "AudioRouter")

Utils.Logger.Instance.Info($"PCM16 format: {pcm16Format.SampleRate}Hz, {pcm16Format.Channels}ch, {pcm16Format.BitsPerSample}bit, {pcm16Format.Encoding}, BlockAlign={pcm16Format.BlockAlign}, AvgBytes/sec={pcm16Format.AverageBytesPerSecond}", "AudioRouter")

Utils.Logger.Instance.Info($"DSP format: {dspThread.Format.SampleRate}Hz, {dspThread.Format.Channels}ch, {dspThread.Format.BitsPerSample}bit, {dspThread.Format.Encoding}, BlockAlign={dspThread.Format.BlockAlign}, AvgBytes/sec={dspThread.Format.AverageBytesPerSecond}", "AudioRouter")

Utils.Logger.Instance.Info($"WAVEOUT format: {dspOutputProvider.WaveFormat.SampleRate}Hz, {dspOutputProvider.WaveFormat.Channels}ch, {dspOutputProvider.WaveFormat.BitsPerSample}bit, {dspOutputProvider.WaveFormat.Encoding}, BlockAlign={dspOutputProvider.WaveFormat.BlockAlign}, AvgBytes/sec={dspOutputProvider.WaveFormat.AverageBytesPerSecond}", "AudioRouter")
```

### 2. Log DSPOutputProvider Read Calls

```visualbasic
' In DSPOutputProvider.Read():
Static callCount As Integer = 0
callCount += 1

If callCount <= 5 Then ' Log first 5 calls
    Utils.Logger.Instance.Info($"WaveOut READ #{callCount}: Requested={count} bytes, Available={dspThread.OutputAvailable()} bytes, Will return={count} bytes", "DSPOutputProvider")
End If
```

### 3. Log Ring Buffer State

```visualbasic
' In DSPThread.ProcessingLoop():
If cycleCount Mod 100 = 0 Then ' Every 100 cycles
    Utils.Logger.Instance.Info($"DSP Cycle {cycleCount}: InputAvail={inputBuffer.Available}/{inputBuffer.Capacity} ({(inputBuffer.Available*100\inputBuffer.Capacity)}%), OutputAvail={outputBuffer.Available}/{outputBuffer.Capacity} ({(outputBuffer.Available*100\outputBuffer.Capacity)}%)", "DSPThread")
End If
```

### 4. Compare Original vs Current Code

Find the EXACT differences in:
- How we calculate buffer sizes
- How we create WaveFormat objects
- How we feed data

---

## Hypothesis Tracking

### Hypothesis 1: Format Mismatch
**Theory:** `pcm16Format` doesn't match what `fileReader.WaveFormat` should be.

**Test:** Log both formats side-by-side. Check if `AverageBytesPerSecond` is different.

**Expected:** Both should show 88,200 bytes/sec for 44.1kHz mono 16-bit
- Calculation: 44100 samples/sec × 1 channel × 2 bytes = 88,200 bytes/sec

### Hypothesis 2: Double Buffering
**Theory:** We're somehow feeding samples twice (once in pre-fill, once in feeder).

**Test:** Count total bytes written to DSP input vs. file size.

**Expected:** Should match file size (minus any dropped samples).

### Hypothesis 3: WaveOut Sample Rate Override
**Theory:** WaveOut is playing at wrong sample rate (88.2kHz instead of 44.1kHz).

**Test:** Log what WaveOut actually initialized with. Check `waveOut.OutputWaveFormat`.

**Expected:** Should be 44.1kHz mono 16-bit.

### Hypothesis 4: Ring Buffer Size Issue
**Theory:** 8192-sample buffer is too small, causing constant overruns ? drops ? speeds through file.

**Test:** Increase buffer back to 2 seconds temporarily and see if speed normalizes.

**Expected:** If this fixes it, buffer size is the issue.

### Hypothesis 5: Block Size Mismatch
**Theory:** Feeder sends 256 samples, DSP expects different amount, timing gets messed up.

**Test:** Log every `WriteInput()` call - show bytes written vs. bytes requested.

**Expected:** Should always write full block (512 bytes for mono).

---

## Next Steps

1. **Add all diagnostic logging above**
2. **Run one playback test**
3. **Paste complete log output**
4. **Analyze logs to see:**
   - Are formats consistent?
   - Are byte counts correct?
   - Where are the 375k samples being dropped?
   - Is WaveOut requesting data at the right rate?

5. **Compare current code to Git history** - find exact commit where it broke

---

## Success Criteria

When fixed, logs should show:
- ? All formats identical: 44.1kHz, 1ch, 16-bit, 88200 bytes/sec
- ? Zero dropped samples
- ? WaveOut reads consume all produced data
- ? Playback duration matches file duration (20 seconds for test file)

