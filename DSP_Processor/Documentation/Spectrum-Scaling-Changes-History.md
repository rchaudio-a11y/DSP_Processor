# Complete History of Spectrum Display Scaling Changes and Debugging Attempts

**Author:** GitHub Copilot  
**Date:** January 14, 2026  
**Project:** DSP_Processor  
**Issue:** Spectrum display showing incorrect amplitude levels

---

## Initial Context (Before Scaling Changes)

**Status:** The spectrum display was showing audio at approximately -50 dB when the actual audio level was -12 dBFS (as confirmed by the `CalculateTruePeakDB()` function in MainForm.vb).

**Problem Identified:** The FFT magnitude was being calculated correctly, but the vertical positioning (Y-coordinate) on the spectrum display was incorrect. The spectrum was drawn too low on the screen.

---

## Change #1: Clamping Implementation (First Attempt)

**File:** `DSP_Processor\Visualization\SpectrumDisplayControl.vb`  
**Method:** `DrawSpectrum()`  
**Date:** January 14, 2026

### What Was Changed
Added clamping to the Y-coordinate calculation to prevent values from going off-screen.

### Code Added
```visualbasic
' Clamp Y to visible range (prevent drawing outside control bounds)
y = Math.Max(topMargin, Math.Min(y, Me.ClientSize.Height - bottomMargin))
```

### Why This Was Done
The diagnostic logs showed Y coordinates like `Y=1296` when the control height was only `683` pixels, meaning the spectrum was being drawn completely off the bottom of the screen.

### Result
This clamping brought the spectrum back into the visible area but positioned it at the very bottom of the control at `Y=653.0` (near the bottom margin).

### Root Cause Identified
The clamping revealed that the underlying scaling formula was calculating Y values that were far too large, indicating the dB-to-pixel conversion was broken.

---

## Change #2: Y-Coordinate Formula Fix

**File:** `DSP_Processor\Visualization\SpectrumDisplayControl.vb`  
**Method:** `DrawSpectrum()`  
**Date:** January 14, 2026

### What Was Changed
Completely rewrote the Y-coordinate calculation formula to properly map dB values to screen pixels.

### Old Formula (Broken)
```visualbasic
' OLD: Inverted and wrong
Dim normalizedDB = (dbValue - _minDB) / (_maxDB - _minDB)
Dim y = topMargin + normalizedDB * drawHeight
```

### New Formula (Correct)
```visualbasic
' NEW: Proper dB-to-pixel mapping
Dim dbRange = _maxDB - _minDB  ' e.g., 0 - (-60) = 60 dB range
Dim dbFromMin = dbValue - _minDB  ' e.g., -12 - (-60) = 48 dB
Dim normalizedValue = dbFromMin / dbRange  ' e.g., 48/60 = 0.8 (80% up from bottom)
Dim y = topMargin + (1.0F - normalizedValue) * drawHeight  ' INVERTED for screen coords
```

### Why This Was Done
- The original formula didn't account for screen coordinates being inverted (Y=0 is top, Y=max is bottom)
- The normalization was mapping dB values incorrectly to pixel positions
- A signal at -12 dB (which is 48 dB above -60 dB minimum) should be 80% up from the bottom, which equals 20% down from the top

### Expected Result
A -12 dB signal should appear near the top of the display (at approximately 20% from the top).

### Actual Result (from logs)
Still showing `Y=653.0` (near the bottom), indicating the FFT was STILL reporting wrong dB values.

### Conclusion
The scaling formula is now CORRECT, but it revealed that the real problem is in the FFT magnitude calculation, not the display scaling.

---

## Change #3: Array Initialization Fix

**File:** `DSP_Processor\Visualization\SpectrumDisplayControl.vb`  
**Method:** `UpdateSpectrum()`  
**Date:** January 14, 2026

### What Was Changed
Modified the spectrum array initialization to use INCOMING values instead of `MinDB`.

### Old Code (Broken)
```visualbasic
' Initialize to MinDB (-60 dB)
For i = 0 To spectrum.Length - 1
    spectrum(i) = _minDB
    smoothedSpectrum(i) = _minDB
    peakHold(i) = _minDB
Next
```

### New Code (Fixed)
```visualbasic
' Initialize to the INCOMING spectrum values, not MinDB!
For i = 0 To spectrum.Length - 1
    spectrum(i) = newSpectrum(i)
    smoothedSpectrum(i) = newSpectrum(i)
    peakHold(i) = newSpectrum(i)
    peakHoldTimer(i) = 0
Next
```

### Why This Was Done
With 70% smoothing enabled, starting from -60 dB meant it took many frames to rise to the actual value:
- Frame 1: `smoothedSpectrum = -60 * 0.7 + (-12) * 0.3 = -45.6 dB`
- Frame 2: `smoothedSpectrum = -45.6 * 0.7 + (-12) * 0.3 = -35.52 dB`
- Frame 20+: Finally reaches near -12 dB

### Expected Result
Spectrum should appear at the correct level immediately without slow rise time.

### Actual Result (from logs)
Logs still showed `Peak=-96.0 dB` and `Y=653.0`, confirming this wasn't a smoothing issue—the FFT itself was producing wrong values.

---

## Change #4: FFT Diagnostic Logging

**File:** `DSP_Processor\DSP\FFT\FFTProcessor.vb`  
**Method:** `CalculateSpectrum()`  
**Date:** January 14, 2026

### What Was Changed
Added comprehensive diagnostic logging to trace the FFT calculation pipeline.

### Code Added
```visualbasic
' DIAGNOSTIC: Check input sample levels
Dim maxInputSample As Single = 0
For i As Integer = startIndex To Math.Min(startIndex + 99, sampleBuffer.Count - 1)
    maxInputSample = Math.Max(maxInputSample, Math.Abs(sampleBuffer(i)))
Next

' ...after FFT calculation...

' DIAGNOSTIC LOG (every 30 calls = ~500ms)
Static callCount As Integer = 0
callCount += 1
If callCount Mod 30 = 0 Then
    Dim rawMagAfterNorm = maxMagnitude / (fftLength / 2.0F) / windowGain
    Dim peakDB = 20.0F * Math.Log10(Math.Max(rawMagAfterNorm, 0.000001F))
    Dim freq = GetFrequencyForBin(maxBin)
    Utils.Logger.Instance.Info($"FFT: MaxInputSample={20 * Math.Log10(maxInputSample):F1}dB, RawFFTMag={maxMagnitude:F1}, NormalizedMag={rawMagAfterNorm:F4}, PeakDB={peakDB:F1}dB @ {freq:F0}Hz (bin {maxBin}), FFTLen={fftLength}, WindowGain={windowGain}", "FFTProcessor")
End If
```

### Why This Was Done
To identify exactly where in the FFT calculation pipeline the magnitude values were being lost. The diagnostic logs would show:
1. `MaxInputSample`: The peak amplitude in the input buffer (should be ~0.25 for -12 dBFS)
2. `RawFFTMag`: The raw FFT output magnitude before normalization
3. `NormalizedMag`: After dividing by FFT length and window gain
4. `PeakDB`: The final dB value that gets displayed

### Result (from log DSP_Processor_20260114_163951.log)
```
[16:39:53] FFT: MaxInputSample=-90.3dB, RawFFTMag=0.0, NormalizedMag=0.0000, PeakDB=-120.0dB
```

### Critical Discovery
- `MaxInputSample=-90.3dB` ? The input buffer contains VERY WEAK samples (amplitude ? 0.00003)
- `RawFFTMag=0.0` ? The FFT is producing ZERO magnitudes
- Expected: `MaxInputSample` should be around -12 dB (amplitude ? 0.25)

**This proves the problem is NOT in the display scaling or FFT calculation—it's in the audio samples being fed to the FFT!**

---

## Change #5: FFT Normalization Reverted

**File:** `DSP_Processor\DSP\FFT\FFTProcessor.vb`  
**Method:** `CalculateSpectrum()`  
**Date:** January 14, 2026

### What Was Changed
Reverted FFT magnitude calculation back to original simple formula without normalization steps.

### Old Code (With Normalization - Added in Error)
```visualbasic
' Calculate magnitude
Dim magnitude = Math.Sqrt(real * real + imag * imag)

' STEP 1: Normalize by FFT length
magnitude /= (fftLength / 2.0F)

' STEP 2: Compensate for window coherent gain
magnitude /= windowGain

' STEP 3: Convert to dBFS
magnitude = Math.Max(magnitude, 0.000001F)
spectrum(i) = 20.0F * Math.Log10(magnitude)
```

### New Code (Reverted to Original)
```visualbasic
' Calculate magnitude
Dim magnitude = Math.Sqrt(real * real + imag * imag)

' Convert to dB (with floor to prevent log(0))
magnitude = Math.Max(magnitude, 0.0000001F)
spectrum(i) = 20.0F * Math.Log10(magnitude)
```

### Why This Was Done
The normalization steps (dividing by `fftLength/2` and `windowGain`) were added to make the FFT "properly calibrated to dBFS", but this:
1. Made the already-weak samples (~-90 dB) even weaker by dividing by 1024
2. Hid the real problem which was that the FFT input samples were already attenuated by ~76 dB
3. The original simple formula worked correctly before - the real bug is upstream in the sample flow

### Result
Removing normalization makes the spectrum visible again, but this is **masking the real problem** - the FFT input samples are still ~76 dB too weak. The raw FFT magnitudes are large enough to be visible even with weak input, but this is not a proper fix.

**Action Required:** The real bug must be found in the audio pipeline between `AudioRouter.UpdateOutputSamples()` and `FFTProcessor.AddSamples()`.

---

## Change #6: Monitor Buffer Diagnostic

**File:** `DSP_Processor\AudioIO\AudioRouter.vb`  
**Method:** `UpdateOutputSamples()`  
**Date:** January 14, 2026

### What Was Changed
Added diagnostic logging to check sample amplitude **immediately after** reading from the output monitor buffer, **before** passing to FFT.

### Code Added
```visualbasic
' DIAGNOSTIC: Check sample amplitude RIGHT AFTER reading from monitor buffer
Static lastDiagTime As DateTime = DateTime.MinValue
If (DateTime.Now - lastDiagTime).TotalSeconds >= 1.0 Then
    Dim maxSample As Single = 0.0F
    For i = 0 To bytesRead - 1 Step 2
        If i + 1 < bytesRead Then
            Dim sample = Math.Abs(BitConverter.ToInt16(buffer, i))
            If sample > maxSample Then maxSample = sample
        End If
    Next
    Dim maxSampleDB = 20.0F * Math.Log10(Math.Max(maxSample / 32768.0F, 0.00001F))
    Utils.Logger.Instance.Info($"MONITOR BUFFER CHECK: Peak={maxSampleDB:F1} dBFS from {bytesRead} bytes BEFORE FFT", "AudioRouter")
    lastDiagTime = DateTime.Now
End If
```

### Why This Was Done
This diagnostic will definitively show whether:
- **If peak ? -12 dB**: The monitor buffer is correct ? bug is in `FFTProcessor.AddSamples()` byte-to-float conversion
- **If peak ? -90 dB**: The monitor buffer already contains weak samples ? bug is in how `DSPThread` writes to monitor buffer

This pinpoints the exact location of the attenuation without changing any actual code.

### Expected Result
Will identify which of these two code paths contains the bug:
1. `DSPThread.WorkerLoop()` ? `outputMonitorBuffer.Write()` (monitor buffer fill)
2. `FFTProcessor.AddSamples()` ? PCM16 to float conversion

---

## Current Status (After All Changes)

### What We Know
1. ? **Audio IS playing correctly** - Confirmed by `TRUE Audio Peak: -12.0 dBFS` logs
2. ? **FFT detects correct frequencies** - Peak moves from 861 Hz ? 19746 Hz during frequency sweep
3. ? **FFT magnitude is WRONG** - Reports -88 to -96 dB instead of -12 dB
4. ? **Spectrum display scaling is CORRECT** - Y=653 is correct position for -88 dB signal
5. ? **Problem is in FFT input** - The `sampleBuffer` in FFTProcessor contains samples that are ~8,000x too small

### The Real Bug Location
The issue is NOT in the spectrum display scaling (which is now working correctly). The issue is that the audio samples being passed to `FFTProcessor.AddSamples()` are either:
- Being scaled incorrectly during conversion (divide by wrong factor)
- Coming from the wrong audio buffer (monitor buffer vs. main buffer)
- Being attenuated somewhere in the audio pipeline before reaching the FFT

### Evidence from Logs (DSP_Processor_20260114_163249.log)
```
[16:33:38.628] UpdateSpectrum: Peak=-88.0 dB at bin 80 (861 Hz)  ? FFT says -88 dB
[16:33:38.909] TRUE Audio Peak: -12.0 dBFS (not FFT!)           ? Actual audio is -12 dB
```

The same audio buffer shows -12 dB when analyzed by `CalculateTruePeakDB()` but -88 dB when analyzed by the FFT, proving the samples reaching `AddSamples()` are corrupted or scaled incorrectly.

---

## Summary of All Changes

| Change # | File | Method | What Changed | Why | Result |
|----------|------|--------|--------------|-----|--------|
| 1 | SpectrumDisplayControl.vb | DrawSpectrum() | Added Y clamping | Prevent off-screen drawing | Brought spectrum into view at bottom |
| 2 | SpectrumDisplayControl.vb | DrawSpectrum() | Rewrote Y formula | Proper dB?pixel mapping | Correct scaling (revealed FFT bug) |
| 3 | SpectrumDisplayControl.vb | UpdateSpectrum() | Initialize arrays to incoming values | Eliminate slow rise with smoothing | No effect (not a smoothing issue) |
| 4 | FFTProcessor.vb | CalculateSpectrum() | Added diagnostic logging | Identify where magnitudes are lost | Discovered input samples are 76 dB too weak |
| 5 | FFTProcessor.vb | CalculateSpectrum() | Reverted FFT normalization | Remove normalization that masked the real bug | Spectrum visible again but input still wrong |
| 6 | AudioRouter.vb | UpdateOutputSamples() | Added monitor buffer diagnostic | Check samples before FFT processing | **NEXT: Run and check logs** |

---

## Next Steps Required

**?? IMMEDIATE ACTION: Run the application and play a test file**

The new diagnostic log will show:
```
MONITOR BUFFER CHECK: Peak=XX.X dBFS from XXXX bytes BEFORE FFT
```

**Then check the result:**

### If `Peak ? -12 dBFS`:
? **Monitor buffer is correct!**
- Bug is in: `FFTProcessor.AddSamples()` 
- Problem: Byte-to-float conversion is wrong
- Fix: Check the division by `32768.0F` or array indexing

### If `Peak ? -90 dBFS`:
? **Monitor buffer already has weak samples!**
- Bug is in: `DSPThread.WorkerLoop()` ? `outputMonitorBuffer.Write()`
- Problem: Wrong buffer being copied, wrong count, or buffer cleared too aggressively
- Fix: Check how `workBuffer` is copied to monitor buffer

---

## Key Files Remaining to Investigate

Based on diagnostic result:

**If bug is in FFT intake (`AddSamples`):**
- `FFTProcessor.vb` - Line ~70: `Dim normalized As Single = sample / 32768.0F`
- Check if `count` parameter is wrong (bytes vs. samples confusion)
- Check if stereo channels are being handled correctly

**If bug is in monitor buffer write:**
- `DSPThread.vb` - Line ~180: `outputMonitorBuffer.Write(workBuffer.Buffer, 0, workBuffer.ByteCount)`
- Check if `workBuffer` already contains attenuated samples before copying
- Check if monitor buffer size is too small (current: 0.5 seconds)
- Check if `workBuffer.ByteCount` is correct after DSP processing

---

## Lessons Learned

1. **Always verify your assumptions** - The display scaling was correct; the bug was upstream
2. **Add diagnostics early** - Should have added FFT input logging from the start
3. **Don't fix what isn't broken** - Changes #1-3 were fixing symptoms, not root cause
4. **Follow the data** - The TRUE peak calculation proved the audio was correct, pointing to FFT input as the problem

---

**End of Document**
