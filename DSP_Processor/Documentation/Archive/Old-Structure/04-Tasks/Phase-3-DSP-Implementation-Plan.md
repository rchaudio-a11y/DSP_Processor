# DSP Feature Implementation Plan - Essentials Only

**Date:** January 15, 2026  
**Status:** ?? **IN PROGRESS - TASK 3.1.3 NEXT**  
**Strategy:** Build clean DSP chain with monitoring points

---

## ?? **TONIGHT'S WORK - TASK 3.1.3**

**Goal:** Build DSP Signal Flow UI Tab (3-4 hours)

**You Have Open:** ? MainForm.vb, MainForm.Designer.vb, MainForm.resx

**Quick Start:**
1. Open MainForm.Designer.vb in Design view
2. Add new TabPage to `visualizationTabs` ? Name: "DSP Signal Flow"
3. Create new UserControl: `UI\DSPSignalFlowPanel.vb`
4. Build vertical layout (Input Meters ? Controls ? Output Meters)
5. Wire events, test with audio

**See Task 3.1.3 below for detailed specs** ??

---

## ?? **GOAL: Essential DSP Chain**

### **Signal Flow:**
```
INPUT (Stereo)
  ?
[GAIN + PAN] ? Tap Point 1 (FFT, Meters)
  ?
[HIGH-PASS FILTER] ? Tap Point 2 (FFT, Meters)
  ?
[LOW-PASS FILTER] ? Tap Point 3 (FFT, Meters)
  ?
[OUTPUT MIXER] ? Tap Point 4 (FFT, Meters)
  ?
OUTPUT (Stereo)
```

### **Architecture Principles:**
- ? Use existing DSPThread infrastructure
- ? Add processors to existing ProcessorChain
- ? Leverage existing FFTMonitorThread for taps
- ? Keep it simple - no over-engineering
- ? **NEW:** Add DSP controls tab to visualizationTabs
- ? **NEW:** Overlay filter response curves on FFT display

---

## ?? **PHASE 3.1: STEREO FOUNDATION**

### **TASK 3.1.1: Enable Stereo Processing** ?
**Priority:** CRITICAL  
**Time:** ~10 minutes (verification only)  
**Status:** ? **COMPLETE** (Jan 15, 2026)

**Current State:**
- ? System already supports stereo input
- ? DSPThread already handles multi-channel
- ? GainProcessor processes L/R independently (verified)
- ? AudioLevelMeter calculates L/R peaks separately (verified)
- ? No channel crosstalk (verified)

**Verification Results:**
See `Documentation\Testing\Stereo-Processing-Verification-Test.md` for full details.

**What We Found:**
1. ? GainProcessor handles stereo correctly (line 86-102)
2. ? AudioLevelMeter has `PeakLeftDB` and `PeakRightDB` (lines 222-224)
3. ? All processing is channel-independent
4. ? Backend is fully stereo-ready!

**What to Do:**
1. [ ] Verify GainProcessor handles stereo (left/right independent)
2. [ ] Add stereo test to ensure no channel crosstalk
3. [ ] Update AudioLevelMeter to show L/R channels separately
4. [ ] Test with stereo file playback

**Success Criteria:**
- ? Left/right channels process independently
- ? No channel crosstalk
- ? Meters show L/R correctly
- ? FFT shows stereo spectrum

**Files to Touch:**
- `DSP\GainProcessor.vb` (verify stereo handling)
- `AudioIO\AudioLevelMeter.vb` (add L/R separation)
- `UI\AudioLevelMeterControl.vb` (show L/R bars)

---

### **TASK 3.1.2: Add Pan Control to GainProcessor** ?
**Priority:** HIGH  
**Time:** ~30 minutes  
**Status:** ? **COMPLETE** (Jan 15, 2026)

**What to Add:**
```visualbasic
' In GainProcessor.vb
Public Property PanPosition As Single ' -1.0 (left) to +1.0 (right), 0 = center

' Pan law: constant power panning
Private Function ApplyPan(leftSample As Single, rightSample As Single) As (Single, Single)
    Dim angle = (PanPosition + 1.0F) * Math.PI / 4.0F ' 0 to ?/2
    Dim leftGain = Math.Cos(angle)
    Dim rightGain = Math.Sin(angle)
    Return (leftSample * leftGain, rightSample * rightGain)
End Function
```

**Steps:**
1. [ ] Add `PanPosition` property to GainProcessor
2. [ ] Implement constant-power pan law
3. [ ] Add pan control to UI (slider -100% to +100%)
4. [ ] Test panning with stereo file
5. [ ] Verify no volume drop at center

**Success Criteria:**
- ? Pan from full left to full right smoothly
- ? Center position = no level change
- ? Constant power (no volume drop during pan)
- ? UI updates in real-time

**Files to Touch:**
- `DSP\GainProcessor.vb` (add pan)
- `UI\AudioSettingsPanel.vb` (add pan slider)
- `Models\AudioDeviceSettings.vb` (add PanPosition property)

---

### **TASK 3.1.3: Add DSP Signal Flow Tab** ???
**Priority:** HIGH  
**Time:** 3-4 hours  
**Status:** ? Not Started  
**Depends On:** Task 3.1.2

**DESIGN CHANGE:** Vertical signal flow layout (like mixing console channel strip)

**What to Create:**
- New tab page in `visualizationTabs` called "DSP Signal Flow"
- **VERTICAL LAYOUT** - Signal flows top to bottom
- **INPUT METERS + CONTROLS + OUTPUT METERS** all visible at once
- Never forget where a control is set!

**Tab Structure (Vertical Flow):**
```
?? DSP SIGNAL FLOW ???????????????????????????
?                                            ?
?  ?? INPUT ????????????????????????????    ?
?  ?  [L Meter] [R Meter]              ?    ?
?  ?  ?????????  ?????????              ?    ?
?  ?  -20 dB    -18 dB                 ?    ?
?  ?????????????????????????????????????    ?
?                   ?                        ?
?  ?? GAIN / PAN ???????????????????????    ?
?  ?  Gain:  [=====|=====] 0.0 dB      ?    ?
?  ?  Pan:   [=====|=====] Center      ?    ?
?  ?????????????????????????????????????    ?
?                   ?                        ?
?  ?? HIGH-PASS FILTER ?????????????????    ?
?  ?  [?] Enable                       ?    ?
?  ?  Freq: [==|========] 30 Hz        ?    ?
?  ?  (Removes DC, rumble, stage noise)?    ?
?  ?????????????????????????????????????    ?
?                   ?                        ?
?  ?? LOW-PASS FILTER ??????????????????    ?
?  ?  [ ] Enable                       ?    ?
?  ?  Freq: [==========|] 18 kHz       ?    ?
?  ?  (Gentle hiss reduction)          ?    ?
?  ?????????????????????????????????????    ?
?                   ?                        ?
?  ?? OUTPUT MIXER ??????????????????????   ?
?  ?  Master: [=====|=====] 0.0 dB     ?   ?
?  ?  Width:  [=====|=====] 100%       ?   ?
?  ?????????????????????????????????????   ?
?                   ?                        ?
?  ?? OUTPUT ???????????????????????????    ?
?  ?  [L Meter] [R Meter]              ?    ?
?  ?  ?????????  ?????????              ?    ?
?  ?  -15 dB    -14 dB                 ?    ?
?  ?????????????????????????????????????    ?
?                                            ?
??????????????????????????????????????????????
```

**Key Features:**
- ? **VERTICAL FLOW** - Top to bottom (like mixing console)
- ? **INPUT METERS** - See input level at top
- ? **OUTPUT METERS** - See output level at bottom
- ? **ALL CONTROLS VISIBLE** - Never forget a setting!
- ? **SIGNAL PATH OBVIOUS** - Arrows show flow
- ? **PROFESSIONAL LAYOUT** - Matches industry standards

**Steps:**
1. [ ] Add new TabPage "DSP Signal Flow" to visualizationTabs in Designer
2. [ ] Create `UI\DSPSignalFlowPanel.vb` UserControl
3. [ ] Add TableLayoutPanel with vertical flow
4. [ ] Add Input Meter section (top):
   - [ ] Two VolumeMeterControl instances (L/R)
   - [ ] Labels: "L" and "R"
5. [ ] Add Gain/Pan section:
   - [ ] GroupBox "Gain / Pan"
   - [ ] TrackBar for Gain (-60 to +12 dB)
   - [ ] TrackBar for Pan (-100% to +100%)
   - [ ] Labels showing current values
6. [ ] Add HPF section:
   - [ ] GroupBox "High-Pass Filter"
   - [ ] CheckBox "Enable"
   - [ ] TrackBar for frequency (30-180 Hz)
   - [ ] Label showing frequency
7. [ ] Add LPF section:
   - [ ] GroupBox "Low-Pass Filter"
   - [ ] CheckBox "Enable"
   - [ ] TrackBar for frequency (8-20 kHz)
   - [ ] Label showing frequency
8. [ ] Add Output Mixer section:
   - [ ] GroupBox "Output Mixer"
   - [ ] TrackBar for Master Gain (-60 to +12 dB)
   - [ ] TrackBar for Stereo Width (0-200%)
   - [ ] Labels showing values
9. [ ] Add Output Meter section (bottom):
   - [ ] Two VolumeMeterControl instances (L/R)
   - [ ] Labels: "L" and "R"
10. [ ] Wire events to DSP processors
11. [ ] Apply dark theme
12. [ ] Test all controls update in real-time

**Success Criteria:**
- ? Entire signal flow visible at once
- ? Input AND output meters always visible
- ? Controls update processors in real-time
- ? Professional mixing console layout
- ? Dark theme applied throughout
- ? Never forget where a control is set!

**Files to Create:**
- `UI\DSPSignalFlowPanel.vb` (UserControl)

**Files to Touch:**
- `MainForm.Designer.vb` (add tab page)
- `MainForm.vb` (wire events, update meters)

**UI Notes:**
- Use `TableLayoutPanel` with 7 rows (vertical)
- Row heights: Auto-size for controls, fixed for meters
- Spacing: 10px between sections
- GroupBox colors: Dark gray backgrounds
- Meter colors: Green (safe), Yellow (caution), Red (clip)
- Arrow graphics: Optional, use labels "?" between sections

---

### **TASK 3.1.4: Add Filter Curve Overlay to FFT** ??
**Priority:** MEDIUM (Cool feature!)  
**Time:** 2-3 hours  
**Status:** ? Not Started  
**Depends On:** Task 3.2.2

**What to Add:**
- Draw filter frequency response as curve over FFT spectrum
- Show high-pass and low-pass curves
- Yellow/orange color for visibility
- Toggle on/off

**Visual Result:**
```
FFT Display with Overlay:
????????????????????????????????????
?    ????????????                  ? ? FFT bars (input)
?    ???????????                   ?
?    ?????????                     ?
?    ?????  ????????????????       ? ? Filter curve (yellow)
?        ????                      ?    (shows HPF + LPF response)
????????????????????????????????????
```

**Steps:**
1. [ ] Add `ShowFilterCurve` property to SpectrumDisplayControl
2. [ ] Add `CalculateFilterResponse()` method
3. [ ] Calculate high-pass response at each FFT bin
4. [ ] Calculate low-pass response at each FFT bin
5. [ ] Combine responses (multiply for cascade)
6. [ ] Draw as smooth curve in OnPaint
7. [ ] Add toggle checkbox to Spectrum tab
8. [ ] Test curve matches actual filter behavior

**Success Criteria:**
- ? Filter curve visible over FFT
- ? Curve updates when filter settings change
- ? Matches actual filter frequency response
- ? Can toggle on/off
- ? Doesn't obscure FFT spectrum

**Technical Notes:**
```visualbasic
' Calculate 2nd-order high-pass magnitude response
Private Function CalculateHPFMagnitude(freq As Single) As Single
    Dim omega = 2 * Math.PI * freq / sampleRate
    Dim omega0 = 2 * Math.PI * cutoffFreq / sampleRate
    ' 2nd order has magnitude^2 = (omega/omega0)^4 / (1 + (omega/omega0)^4)
    ' Simplified for visualization
End Function

' Draw curve
For Each bin In fftBins
    Dim freq = BinToFrequency(bin)
    Dim hpfMag = CalculateHPFMagnitude(freq)
    Dim lpfMag = CalculateLPFMagnitude(freq)
    Dim totalMag = hpfMag * lpfMag ' Cascade
    ' Convert to dB and plot
Next
```

**Files to Touch:**
- `Visualization\SpectrumDisplayControl.vb` (add overlay)
- `DSP\HighPassFilter.vb` (expose frequency response calculation)
- `DSP\LowPassFilter.vb` (expose frequency response calculation)
- `UI\SpectrumSettingsPanel.vb` (add toggle)

---

## ?? **PHASE 3.2: FILTER STAGE**

### **TASK 3.2.1: Create HighPassFilter Processor** ??
**Priority:** HIGH  
**Time:** 4-5 hours (2nd order is more complex)  
**Status:** ? Not Started  
**Depends On:** Task 3.1.2

**Filter Specs:**
- **Type:** 2nd-order Butterworth high-pass (12 dB/octave roll-off)
- **Frequency Range:** 30 Hz (hard minimum) to 180 Hz (max)
- **Default:** 30 Hz (removes DC offset and subsonic rumble)
- **Purpose:** Remove rumble, stage noise, handling noise, DC offset

**What to Create:**
```visualbasic
' Create DSP\HighPassFilter.vb
Public Class HighPassFilter
    Inherits ProcessorBase
    
    Public Property CutoffFrequency As Single ' Hz (30-180)
    Public Property Enabled As Boolean = True ' Default ON for DC protection
    
    ' 2nd-order Butterworth requires 4 state variables (2 per channel)
    ' Left channel states
    Private leftX1, leftX2 As Single ' Input history
    Private leftY1, leftY2 As Single ' Output history
    
    ' Right channel states
    Private rightX1, rightX2 As Single
    Private rightY1, rightY2 As Single
    
    ' Biquad coefficients (calculated from cutoff frequency)
    Private a0, a1, a2 As Single ' Feedforward coefficients
    Private b1, b2 As Single     ' Feedback coefficients
    
    Public Overrides Sub Process(buffer As Byte(), ...)
        ' 2nd-order IIR filter (biquad):
        ' y[n] = a0*x[n] + a1*x[n-1] + a2*x[n-2] - b1*y[n-1] - b2*y[n-2]
        '
        ' Butterworth coefficients for high-pass:
        ' Q = 0.7071 (maximally flat passband)
        ' ?0 = 2?fc/fs (normalized frequency)
    End Sub
    
    Private Sub CalculateCoefficients()
        ' Butterworth 2nd-order high-pass coefficients
        ' These give 12 dB/octave roll-off below cutoff
        Dim omega = 2.0 * Math.PI * CutoffFrequency / SampleRate
        Dim sinOmega = Math.Sin(omega)
        Dim cosOmega = Math.Cos(omega)
        Dim Q = 0.7071 ' Butterworth Q factor (1/?2)
        Dim alpha = sinOmega / (2.0 * Q)
        
        ' Biquad coefficients
        Dim b0 = (1.0 + cosOmega) / 2.0
        Dim b1_coef = -(1.0 + cosOmega)
        Dim b2_coef = (1.0 + cosOmega) / 2.0
        Dim a0_coef = 1.0 + alpha
        Dim a1_coef = -2.0 * cosOmega
        Dim a2_coef = 1.0 - alpha
        
        ' Normalize by a0
        a0 = b0 / a0_coef
        a1 = b1_coef / a0_coef
        a2 = b2_coef / a0_coef
        b1 = a1_coef / a0_coef
        b2 = a2_coef / a0_coef
    End Sub
End Class
```

**Steps:**
1. [ ] Create `DSP\HighPassFilter.vb` inheriting from ProcessorBase
2. [ ] Implement 2nd-order Butterworth high-pass filter (biquad)
3. [ ] Add stereo support (independent L/R state variables)
4. [ ] Hard-code minimum frequency to 30 Hz (DC protection)
5. [ ] Limit maximum frequency to 180 Hz
6. [ ] Default to ENABLED at 30 Hz (always removes DC)
7. [ ] Calculate coefficients when frequency changes
8. [ ] Test with low-frequency tone (should attenuate 12 dB/octave)
9. [ ] Verify no DC offset passes through
10. [ ] Add UI controls (frequency slider 30-180 Hz, enable/disable)

**Success Criteria:**
- ? 12 dB/octave roll-off below cutoff (2nd order)
- ? Nothing below 30 Hz passes (hard limit)
- ? Frequency range: 30-180 Hz
- ? Stereo channels process independently
- ? No clicks when enabling/disabling
- ? No phase issues between channels
- ? FFT shows sharp roll-off at cutoff
- ? Default ON at 30 Hz for DC protection

**Files to Create:**
- `DSP\HighPassFilter.vb`

**Files to Touch:**
- `DSP\ProcessorChain.vb` (add to chain)
- `UI\AudioPipelinePanel.vb` (add controls)
- `Models\PipelineConfiguration.vb` (add settings)

**Technical Notes:**
- 2nd order = 2 cascaded 1st-order filters
- Butterworth = maximally flat passband (no ripple)
- Q = 0.7071 = critical damping (no overshoot)
- 30 Hz minimum protects against DC offset and subsonic rumble
- 180 Hz maximum prevents cutting into vocal fundamentals

---

### **TASK 3.2.2: Create LowPassFilter Processor** ??
**Priority:** HIGH  
**Time:** 2-3 hours  
**Status:** ? Not Started  
**Depends On:** Task 3.2.1

**Filter Specs:**
- **Type:** 1st-order IIR low-pass (6 dB/octave roll-off)
- **Frequency Range:** 8 kHz to 20 kHz
- **Default:** 18 kHz (gentle roll-off, just removes hiss)
- **Purpose:** Gentle high-frequency roll-off for hiss reduction

**What to Create:**
```visualbasic
' Create DSP\LowPassFilter.vb
Public Class LowPassFilter
    Inherits ProcessorBase
    
    Public Property CutoffFrequency As Single ' Hz (8000-20000)
    Public Property Enabled As Boolean = False ' Default OFF
    
    ' Simple 1st-order filter (2 state variables for stereo)
    Private leftPrevOutput As Single
    Private rightPrevOutput As Single
    
    ' Single coefficient (calculated from cutoff)
    Private alpha As Single
    
    Public Overrides Sub Process(buffer As Byte(), ...)
        ' 1st-order IIR low-pass:
        ' y[n] = ? * x[n] + (1-?) * y[n-1]
        ' where ? = 2?fc / (2?fc + fs)
        '
        ' Simple, gentle roll-off (6 dB/octave)
    End Sub
    
    Private Sub CalculateCoefficient()
        ' Simple 1st-order coefficient
        Dim omega = 2.0 * Math.PI * CutoffFrequency
        alpha = omega / (omega + SampleRate)
    End Sub
End Class
```

**Steps:**
1. [ ] Create `DSP\LowPassFilter.vb` (copy structure from HighPassFilter)
2. [ ] Implement 1st-order IIR low-pass filter (simple)
3. [ ] Add stereo support (independent L/R state)
4. [ ] Set frequency range: 8 kHz to 20 kHz
5. [ ] Default to DISABLED (optional feature)
6. [ ] Calculate coefficient when frequency changes
7. [ ] Test with high-frequency tone (should attenuate gently)
8. [ ] Add UI controls (frequency slider 8-20 kHz, enable/disable)

**Success Criteria:**
- ? 6 dB/octave roll-off above cutoff (1st order)
- ? Frequency range: 8-20 kHz
- ? Gentle roll-off (not aggressive)
- ? Stereo channels process independently
- ? No clicks when enabling/disabling
- ? FFT shows gentle high-frequency roll-off
- ? Default OFF (optional feature)

**Files to Create:**
- `DSP\LowPassFilter.vb`

**Files to Touch:**
- Same as Task 3.2.1

**Technical Notes:**
- 1st order is sufficient for gentle hiss reduction
- 6 dB/octave = natural-sounding roll-off
- Higher cutoff (8-20 kHz) preserves air and brilliance
- Default OFF because it's optional (not essential like HPF)

---

## ?? **PHASE 3.3: OUTPUT MIXER**

### **TASK 3.3.1: Create OutputMixer Processor** ???
**Priority:** MEDIUM  
**Time:** 2-3 hours  
**Status:** ? Not Started  
**Depends On:** Task 3.2.2

**What to Create:**
```visualbasic
' Create DSP\OutputMixer.vb
Public Class OutputMixer
    Inherits ProcessorBase
    
    Public Property MasterGain As Single = 0.0F ' dB (-60 to +12)
    Public Property LeftGain As Single = 0.0F ' dB
    Public Property RightGain As Single = 0.0F ' dB
    Public Property Width As Single = 1.0F ' Stereo width (0=mono, 1=normal, 2=wide)
    
    Public Overrides Sub Process(buffer As Byte(), ...)
        ' Apply stereo width (M/S processing)
        ' Apply individual channel gains
        ' Apply master gain
    End Sub
End Class
```

**Steps:**
1. [ ] Create `DSP\OutputMixer.vb`
2. [ ] Implement master gain
3. [ ] Implement L/R channel gains
4. [ ] Implement stereo width control (optional but cool)
5. [ ] Add UI controls
6. [ ] Test with stereo file

**Success Criteria:**
- ? Master gain controls overall level
- ? L/R gains work independently
- ? Stereo width adjusts image
- ? No clipping at unity gain
- ? Meters show final output level

**Files to Create:**
- `DSP\OutputMixer.vb`

**Files to Touch:**
- `DSP\ProcessorChain.vb`
- `UI\AudioPipelinePanel.vb`
- `Models\PipelineConfiguration.vb`

---

## ?? **PHASE 3.4: MONITORING POINTS**

### **TASK 3.4.1: Add FFT Tap Points** ??
**Priority:** HIGH  
**Time:** 2-3 hours  
**Status:** ? Not Started  
**Depends On:** Task 3.3.1

**Current State:**
- FFTMonitorThread already exists
- AudioPipelineRouter.RouteAudioBuffer() already has tap points
- Just need to add more tap points

**What to Add:**
```visualbasic
' In AudioPipelineRouter.vb or DSPThread.vb
Public Enum TapPoint
    PreGain = 0      ' Raw input
    PostGain = 1     ' After gain/pan
    PostHighPass = 2 ' After HPF
    PostLowPass = 3  ' After LPF
    PostMixer = 4    ' Final output
End Enum
```

**Steps:**
1. [ ] Define TapPoint enum
2. [ ] Add tap points after each processor
3. [ ] Wire to FFTMonitorThread
4. [ ] Add UI selector to choose which tap to display
5. [ ] Test FFT at each stage

**Success Criteria:**
- ? Can view FFT at any stage
- ? FFT updates smoothly (20 FPS)
- ? No performance impact
- ? Can switch taps in real-time

**Files to Touch:**
- `Audio\Routing\AudioPipelineRouter.vb` (add taps)
- `DSP\DSPThread.vb` (add tap buffers)
- `UI\SpectrumSettingsPanel.vb` (add tap selector)

---

### **TASK 3.4.2: Add Level Meter Tap Points** ??
**Priority:** MEDIUM  
**Time:** 1-2 hours  
**Status:** ? Not Started  
**Depends On:** Task 3.4.1

**What to Add:**
- Meter after each processor stage
- Show input level, gain stage level, filter output, final output
- Use existing AudioLevelMeter infrastructure

**Steps:**
1. [ ] Add meter tap after each processor
2. [ ] Create multi-meter display (4-5 meter pairs)
3. [ ] Label each meter (Input, Gain, HPF, LPF, Output)
4. [ ] Test meters show correct levels
5. [ ] Verify no performance impact

**Success Criteria:**
- ? Meters at all stages
- ? Can see gain structure
- ? Can identify where clipping occurs
- ? Updates smoothly

**Files to Touch:**
- `AudioIO\AudioLevelMeter.vb` (add multi-tap support)
- `UI\InputTabPanel.vb` (add multi-meter display)

---

## ?? **PHASE 3.5: INTEGRATION & TESTING**

### **TASK 3.5.1: Wire DSP Chain** ??
**Priority:** CRITICAL  
**Time:** 2-3 hours  
**Status:** ? Not Started  
**Depends On:** All previous tasks

**What to Do:**
```visualbasic
' In DSPThread or AudioPipelineRouter
Public Sub InitializeDSPChain()
    Chain.Clear()
    
    ' Stage 1: Gain + Pan
    Chain.AddProcessor(New GainProcessor(format) With {.GainDB = 0, .PanPosition = 0})
    
    ' Stage 2: High-Pass Filter
    Chain.AddProcessor(New HighPassFilter(format) With {.CutoffFrequency = 20, .Enabled = False})
    
    ' Stage 3: Low-Pass Filter
    Chain.AddProcessor(New LowPassFilter(format) With {.CutoffFrequency = 20000, .Enabled = False})
    
    ' Stage 4: Output Mixer
    Chain.AddProcessor(New OutputMixer(format) With {.MasterGain = 0})
End Sub
```

**Steps:**
1. [ ] Wire processors in correct order
2. [ ] Load/save settings for each processor
3. [ ] Test signal flow through entire chain
4. [ ] Verify bypass modes work
5. [ ] Test with real audio files

**Success Criteria:**
- ? Audio flows through entire chain
- ? All processors work together
- ? No clicks or pops
- ? Settings persist
- ? Can bypass entire chain

---

### **TASK 3.5.2: Comprehensive Testing** ??
**Priority:** CRITICAL  
**Time:** 3-4 hours  
**Status:** ? Not Started

**Test Scenarios:**
1. [ ] Stereo file playback ? verify L/R separation
2. [ ] Pan sweep ? verify no volume drop
3. [ ] High-pass filter ? verify bass cut
4. [ ] Low-pass filter ? verify treble cut
5. [ ] Both filters ? verify band-pass
6. [ ] Master gain ? verify output level
7. [ ] FFT taps ? verify spectrum at each stage
8. [ ] Meters ? verify levels at each stage
9. [ ] Bypass all ? verify dry signal
10. [ ] Save/load settings ? verify persistence

**Success Criteria:**
- ? All scenarios pass
- ? No crashes
- ? No audio artifacts
- ? UI responsive
- ? Settings work correctly

---

## ?? **PROGRESS TRACKING**

### **Phase 3.1: Stereo Foundation**
- [x] Task 3.1.1: Enable Stereo Processing ? **DONE** (~10 min verification)
- [x] Task 3.1.2: Add Pan Control ? **DONE** (~30 min)
- [ ] Task 3.1.3: Add DSP Controls Tab (2-3 hours) ? **NEXT!**
- [ ] Task 3.1.4: Add Filter Curve Overlay (2-3 hours)

**Estimated:** 4-6 hours remaining

### **Phase 3.2: Filter Stage**
- [ ] Task 3.2.1: High-Pass Filter (4-5 hours) ? **2nd order, 30-180 Hz**
- [ ] Task 3.2.2: Low-Pass Filter (2-3 hours) ? **1st order, 8-20 kHz**

**Estimated:** 6-8 hours

### **Phase 3.3: Output Mixer**
- [ ] Task 3.3.1: Output Mixer (2-3 hours)

**Estimated:** 2-3 hours

### **Phase 3.4: Monitoring**
- [ ] Task 3.4.1: FFT Tap Points (2-3 hours)
- [ ] Task 3.4.2: Level Meter Taps (1-2 hours)

**Estimated:** 3-5 hours

### **Phase 3.5: Integration**
- [ ] Task 3.5.1: Wire DSP Chain (2-3 hours)
- [ ] Task 3.5.2: Comprehensive Testing (3-4 hours)

**Estimated:** 5-7 hours

---

## ?? **TOTAL ESTIMATES**

**Total Time:** 23-33 hours (3-4 full days of focused work)

**Breakdown:**
- Stereo + UI: 7-11 hours (includes DSP tab + overlay)
- Filters: 6-8 hours (2nd order HPF takes longer)
- Mixer: 2-3 hours
- Monitoring: 3-5 hours
- Integration: 5-7 hours

---

## ?? **NEXT STEPS**

1. **START WITH:** Task 3.1.1 (Stereo Processing)
2. **Build incrementally** - test after each task
3. **Use existing infrastructure** - don't rebuild what works
4. **Keep it simple** - first-order filters are fine to start

**Ready to start building DSP features?** ??

---

**Date:** January 15, 2026  
**Status:** ?? **READY TO START PHASE 3**  
**Foundation:** ? Clean refactored code ready for features

 
 