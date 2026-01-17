# Task 2.2.1: Implement Biquad Filter Core

**Priority:** ?? **CRITICAL**  
**Status:** ? Not Started  
**Estimated Time:** 1-2 days  
**Blocking:** Phase 2 completion

---

## ?? Task Overview

Create a production-ready Biquad filter implementation that supports all common filter types (LPF, HPF, BPF, Notch, Peaking, Shelf, All-pass). This is the foundation for all frequency-based DSP processing.

---

## ?? Objectives

1. Create `DSP\Filters\` folder structure
2. Implement `BiquadFilter` class inheriting from `ProcessorBase`
3. Define `FilterType` enum with all filter types
4. Implement Audio EQ Cookbook coefficient calculations
5. Implement Direct Form II biquad processing
6. Add per-channel state management
7. Implement `Reset()` for state clearing
8. Validate frequency response and stability

---

## ?? File Structure

```
DSP\Filters\
?? BiquadFilter.vb
   ?? BiquadFilter class (inherits ProcessorBase)
   ?? FilterType enum
   ?? Coefficient calculation methods
   ?? Process() implementation
   ?? State management
```

---

## ?? Implementation Checklist

### **Step 1: Create Folder and File**
- [ ] Create `DSP\Filters\` folder
- [ ] Create `BiquadFilter.vb` file
- [ ] Add namespace `DSP.Filters`

### **Step 2: Define FilterType Enum**
- [ ] Create `FilterType` enum with values:
  - [ ] `LowPass` - Low-pass filter (most common)
  - [ ] `HighPass` - High-pass filter (rumble removal)
  - [ ] `BandPass` - Band-pass filter (frequency isolation)
  - [ ] `Notch` - Notch filter (frequency removal)
  - [ ] `Peaking` - Parametric EQ
  - [ ] `LowShelf` - Bass adjustment
  - [ ] `HighShelf` - Treble adjustment
  - [ ] `AllPass` - Phase adjustment

### **Step 3: Create BiquadFilter Class Structure**
- [ ] Class inherits from `ProcessorBase`
- [ ] Add filter coefficient fields:
  - [ ] `a0`, `a1`, `a2` (feedforward)
  - [ ] `b1`, `b2` (feedback)
- [ ] Add state variables (per channel):
  - [ ] `x1`, `x2`, `y1`, `y2` (left/mono)
  - [ ] `x1r`, `x2r`, `y1r`, `y2r` (right)
- [ ] Add parameter fields:
  - [ ] `_type As FilterType`
  - [ ] `_frequency As Double`
  - [ ] `_q As Double`
  - [ ] `_gain As Double` (for peaking/shelf)

### **Step 4: Implement Constructor**
- [ ] Accept parameters: `format`, `type`, `frequency`, `q`, `gain`
- [ ] Call base constructor with format
- [ ] Store parameters
- [ ] Call `CalculateCoefficients()`

### **Step 5: Implement Properties**
- [ ] `FilterType` property with `CalculateCoefficients()` on change
- [ ] `Frequency` property with validation and recalculation
- [ ] `Q` property with validation and recalculation
- [ ] `Gain` property with validation and recalculation
- [ ] Override `Name` property to return "Biquad Filter"
- [ ] Override `LatencySamples` to return 2

### **Step 6: Implement Coefficient Calculation**
- [ ] Create `CalculateCoefficients()` private method
- [ ] Calculate intermediate values:
  - [ ] `w0 = 2? * frequency / sampleRate`
  - [ ] `cosW0 = cos(w0)`
  - [ ] `sinW0 = sin(w0)`
  - [ ] `alpha = sinW0 / (2 * Q)`
  - [ ] `A = 10^(gain/40)` for peaking/shelf
- [ ] Implement coefficient calculation for each filter type:
  - [ ] **LowPass:**
    ```vb
    b0 = (1 - cosW0) / 2
    b1 = 1 - cosW0
    b2 = (1 - cosW0) / 2
    a0 = 1 + alpha
    a1 = -2 * cosW0
    a2 = 1 - alpha
    ```
  - [ ] **HighPass:**
    ```vb
    b0 = (1 + cosW0) / 2
    b1 = -(1 + cosW0)
    b2 = (1 + cosW0) / 2
    a0 = 1 + alpha
    a1 = -2 * cosW0
    a2 = 1 - alpha
    ```
  - [ ] **BandPass:** (constant skirt gain, peak gain = Q)
    ```vb
    b0 = sinW0 / 2 = Q * alpha
    b1 = 0
    b2 = -sinW0 / 2 = -Q * alpha
    a0 = 1 + alpha
    a1 = -2 * cosW0
    a2 = 1 - alpha
    ```
  - [ ] **Notch:**
    ```vb
    b0 = 1
    b1 = -2 * cosW0
    b2 = 1
    a0 = 1 + alpha
    a1 = -2 * cosW0
    a2 = 1 - alpha
    ```
  - [ ] **Peaking:**
    ```vb
    b0 = 1 + alpha * A
    b1 = -2 * cosW0
    b2 = 1 - alpha * A
    a0 = 1 + alpha / A
    a1 = -2 * cosW0
    a2 = 1 - alpha / A
    ```
  - [ ] **LowShelf:**
    ```vb
    b0 = A * ((A + 1) - (A - 1) * cosW0 + 2 * sqrt(A) * alpha)
    b1 = 2 * A * ((A - 1) - (A + 1) * cosW0)
    b2 = A * ((A + 1) - (A - 1) * cosW0 - 2 * sqrt(A) * alpha)
    a0 = (A + 1) + (A - 1) * cosW0 + 2 * sqrt(A) * alpha
    a1 = -2 * ((A - 1) + (A + 1) * cosW0)
    a2 = (A + 1) + (A - 1) * cosW0 - 2 * sqrt(A) * alpha
    ```
  - [ ] **HighShelf:**
    ```vb
    b0 = A * ((A + 1) + (A - 1) * cosW0 + 2 * sqrt(A) * alpha)
    b1 = -2 * A * ((A - 1) + (A + 1) * cosW0)
    b2 = A * ((A + 1) + (A - 1) * cosW0 - 2 * sqrt(A) * alpha)
    a0 = (A + 1) - (A - 1) * cosW0 + 2 * sqrt(A) * alpha
    a1 = 2 * ((A - 1) - (A + 1) * cosW0)
    a2 = (A + 1) - (A - 1) * cosW0 - 2 * sqrt(A) * alpha
    ```
  - [ ] **AllPass:**
    ```vb
    b0 = 1 - alpha
    b1 = -2 * cosW0
    b2 = 1 + alpha
    a0 = 1 + alpha
    a1 = -2 * cosW0
    a2 = 1 - alpha
    ```
- [ ] Normalize all coefficients (divide by a0):
  ```vb
  b1 = b1 / a0
  b2 = b2 / a0
  a1 = a1 / a0
  a2 = a2 / a0
  b0 = b0 / a0
  ```

### **Step 7: Implement Process() Method**
- [ ] Override `Process(buffer As AudioBuffer)` method
- [ ] Check bypass flag, return early if bypassed
- [ ] Get audio data from buffer
- [ ] Loop through each sample:
  - [ ] **Left/Mono channel:**
    ```vb
    ' Get input sample
    xn = GetSample(i, 0)
    
    ' Direct Form II biquad
    yn = b0 * xn + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
    
    ' Update state variables
    x2 = x1
    x1 = xn
    y2 = y1
    y1 = yn
    
    ' Write output
    SetSample(i, 0, yn)
    ```
  - [ ] **Right channel (if stereo):**
    ```vb
    ' Same as left but with separate state variables
    ' xnr, x1r, x2r, y1r, y2r
    ```
- [ ] Handle denormal numbers (optional but recommended):
  ```vb
  If Math.Abs(yn) < 1.0E-20 Then yn = 0.0
  ```

### **Step 8: Implement Reset() Method**
- [ ] Override `Reset()` method
- [ ] Clear all state variables:
  ```vb
  x1 = 0 : x2 = 0 : y1 = 0 : y2 = 0
  x1r = 0 : x2r = 0 : y1r = 0 : y2r = 0
  ```

### **Step 9: Add Documentation**
- [ ] Add XML documentation for class
- [ ] Document all public properties
- [ ] Document all public methods
- [ ] Add usage examples in comments
- [ ] Document stability considerations

---

## ?? Testing Checklist

### **Unit Tests:**
- [ ] Test coefficient calculation for each filter type
- [ ] Verify frequency response at cutoff frequency
- [ ] Test Q factor effect on resonance
- [ ] Test gain parameter for peaking/shelf
- [ ] Verify state reset clears properly
- [ ] Test bypass functionality

### **Integration Tests:**
- [ ] Add to processor chain
- [ ] Process audio through filter
- [ ] Verify no clicks/pops on parameter changes
- [ ] Test with various buffer sizes
- [ ] Verify thread safety

### **Stability Tests:**
- [ ] Test extreme Q values (0.1 to 100)
- [ ] Test extreme frequencies (20Hz to 20kHz)
- [ ] Test extreme gain values (-30dB to +30dB)
- [ ] Verify no oscillation
- [ ] Test denormal number handling

### **Performance Tests:**
- [ ] Measure CPU usage per filter
- [ ] Verify <1ms processing time per 2048-sample buffer
- [ ] Test with multiple filters in chain
- [ ] Profile coefficient calculation overhead

---

## ?? Reference Materials

### **Audio EQ Cookbook:**
- Robert Bristow-Johnson's Audio EQ Cookbook
- URL: https://webaudio.github.io/Audio-EQ-Cookbook/audio-eq-cookbook.html

### **Key Formulas:**
```
w0 = 2? * f0 / Fs
cos(w0), sin(w0)
alpha = sin(w0) / (2 * Q)
A = 10^(dBgain / 40)
```

### **Direct Form II Structure:**
```
   x[n] ???(b0)?????? y[n]
              ?  ?
              ?  ?
            [z^-1]
              ?  ?
   (b1)?????????????(a1)
              ?  ?
            [z^-1]
              ?  ?
   (b2)?????????????(a2)
```

---

## ?? Implementation Tips

### **Performance:**
- Use `Double` for coefficients (precision)
- Use `Single` for audio samples (speed)
- Pre-calculate coefficients in setters
- Avoid allocations in `Process()` loop
- Use inline operations

### **Stability:**
- Always normalize coefficients
- Check for denormal numbers
- Clamp Q to reasonable range (0.1 to 100)
- Validate frequency is < Nyquist

### **Common Pitfalls:**
- ?? Forgetting to normalize coefficients
- ?? Not handling denormal numbers
- ?? Using same state for both channels
- ?? Calculating coefficients in Process() loop
- ?? Not resetting state on discontinuity

---

## ? Definition of Done

- [ ] All filter types implemented and tested
- [ ] Frequency response matches expected curves
- [ ] No oscillation or instability
- [ ] CPU usage <1ms per 2048-sample buffer
- [ ] Comprehensive XML documentation
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Code review completed
- [ ] Committed to repository

---

## ?? Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **Frequency Response Accuracy** | ±0.5dB at cutoff | Sweep test with oscillator |
| **CPU Usage** | <1ms per buffer | Performance profiling |
| **Latency** | 2 samples | Inherent to 2-pole filter |
| **Stability** | No oscillation | Test with extreme parameters |
| **Code Quality** | Pass code review | Manual review |

---

**Task Created:** January 14, 2026  
**Target Completion:** January 16, 2026  
**Dependencies:** Phase 2.1 (Complete ?)  
**Blocks:** Phase 2.3, 2.4, 2.5
