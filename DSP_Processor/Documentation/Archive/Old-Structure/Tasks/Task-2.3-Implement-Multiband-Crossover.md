# Task 2.3: Implement Multiband Crossover

**Priority:** ?? **HIGH**  
**Status:** ? Not Started  
**Estimated Time:** 3-5 days  
**Dependencies:** Task 2.2.1 (Biquad Filters)

---

## ?? Task Overview

Implement a 5-band multiband crossover using Linkwitz-Riley filters. This splits the audio spectrum into separate frequency bands for independent processing (compression, EQ, etc.).

---

## ?? Objectives

1. Create `MultibandCrossover` class
2. Implement 5-band frequency splitting
3. Use Linkwitz-Riley filters (4th order = 24dB/octave)
4. Ensure phase coherent reconstruction
5. Add configurable crossover frequencies
6. Implement per-band mute/solo controls

---

## ?? Architecture

### **5-Band Split:**
```
Input Signal
?
?? Band 1: Sub-bass    (20 Hz - 80 Hz)    [Linkwitz-Riley LPF @ 80Hz]
?? Band 2: Bass        (80 Hz - 250 Hz)   [LR BPF 80-250Hz]
?? Band 3: Low-mid     (250 Hz - 2kHz)    [LR BPF 250-2000Hz]
?? Band 4: High-mid    (2 kHz - 6kHz)     [LR BPF 2k-6kHz]
?? Band 5: Treble      (6 kHz - 20kHz)    [Linkwitz-Riley HPF @ 6kHz]
```

### **Linkwitz-Riley Filter:**
- 4th order (24dB/octave slope)
- Implemented as 2 cascaded 2nd order Butterworth filters
- Sum of squared responses = unity (perfect reconstruction)
- Phase coherent (critical for multiband processing)

---

## ?? Implementation Checklist

### **Step 1: Create Class Structure**
- [ ] Create `DSP\Multiband\MultibandCrossover.vb`
- [ ] Inherit from `ProcessorBase`
- [ ] Define 5 output bands:
  ```vb
  Public Class Band
      Public Property Enabled As Boolean
      Public Property Muted As Boolean
      Public Property Soloed As Boolean
      Public Property Buffer As AudioBuffer
  End Class
  ```

### **Step 2: Define Crossover Frequencies**
- [ ] Add configurable crossover points:
  - [ ] `CrossoverFreq1` (default: 80 Hz) - Sub/Bass split
  - [ ] `CrossoverFreq2` (default: 250 Hz) - Bass/Low-mid split
  - [ ] `CrossoverFreq3` (default: 2000 Hz) - Low-mid/High-mid split
  - [ ] `CrossoverFreq4` (default: 6000 Hz) - High-mid/Treble split
- [ ] Add validation (each freq must be higher than previous)
- [ ] Add Nyquist frequency check

### **Step 3: Implement Linkwitz-Riley Filters**
- [ ] Create helper method `CreateLinkwitzRileyLPF(frequency)`
  - [ ] Calculate Q = 0.707 (Butterworth Q)
  - [ ] Create two cascaded LPF biquads
  - [ ] Return array of 2 filters
- [ ] Create helper method `CreateLinkwitzRileyHPF(frequency)`
  - [ ] Same as LPF but with HPF
- [ ] Create helper method `CreateLinkwitzRileyBPF(lowFreq, highFreq)`
  - [ ] HPF at lowFreq (removes below)
  - [ ] LPF at highFreq (removes above)
  - [ ] Return array of 4 filters (2 HPF + 2 LPF)

### **Step 4: Initialize Filter Banks**
- [ ] Band 1 (Sub-bass):
  - [ ] 2x LPF biquads @ CrossoverFreq1
- [ ] Band 2 (Bass):
  - [ ] 2x HPF biquads @ CrossoverFreq1
  - [ ] 2x LPF biquads @ CrossoverFreq2
- [ ] Band 3 (Low-mid):
  - [ ] 2x HPF biquads @ CrossoverFreq2
  - [ ] 2x LPF biquads @ CrossoverFreq3
- [ ] Band 4 (High-mid):
  - [ ] 2x HPF biquads @ CrossoverFreq3
  - [ ] 2x LPF biquads @ CrossoverFreq4
- [ ] Band 5 (Treble):
  - [ ] 2x HPF biquads @ CrossoverFreq4

### **Step 5: Implement Process() Method**
- [ ] Override `Process(buffer As AudioBuffer)`
- [ ] Create temporary buffers for each band
- [ ] For each band:
  - [ ] Copy input buffer to band buffer
  - [ ] Apply filter bank (cascade all filters)
  - [ ] Store result in band's output buffer
- [ ] Handle mute/solo:
  - [ ] If any band is soloed, mute all others
  - [ ] Apply mute to individual bands

### **Step 6: Implement Band Mixing**
- [ ] Create `Mix()` method to sum all bands
- [ ] Sum band buffers back to single output
- [ ] Apply per-band gain if needed
- [ ] Handle solo/mute states

### **Step 7: Add Latency Compensation**
- [ ] Calculate total filter latency
  - [ ] Each biquad = 2 samples
  - [ ] Band 1 = 4 samples (2 biquads)
  - [ ] Band 2 = 8 samples (4 biquads)
  - [ ] Band 3 = 8 samples
  - [ ] Band 4 = 8 samples
  - [ ] Band 5 = 4 samples
- [ ] Add delay lines to align all bands
- [ ] Maximum latency = 8 samples
- [ ] Add delays to bands 1 and 5 (4 samples each)

---

## ?? Testing Checklist

### **Frequency Response Tests:**
- [ ] Verify flat response when all bands summed
- [ ] Test crossover slopes (24dB/octave)
- [ ] Verify phase coherence at crossover points
- [ ] Test with pink noise (should be flat)

### **Functionality Tests:**
- [ ] Test mute on individual bands
- [ ] Test solo on individual bands
- [ ] Test frequency adjustment
- [ ] Verify latency compensation

### **Performance Tests:**
- [ ] Measure CPU usage (5 bands + filters)
- [ ] Target: <10ms for 2048-sample buffer
- [ ] Test with real-time audio

---

## ?? Expected Performance

| Metric | Target | Notes |
|--------|--------|-------|
| **CPU Usage** | <10ms per buffer | 5 bands × multiple filters |
| **Total Latency** | 8 samples | With compensation |
| **Frequency Response** | ±0.5dB | When summed |
| **Crossover Slope** | 24dB/octave | Linkwitz-Riley 4th order |

---

## ?? Implementation Tips

### **Phase Coherence:**
- Linkwitz-Riley filters maintain phase coherence
- Sum of squared responses = 1
- Critical for multiband processing

### **Butterworth Q:**
- Q = 0.707 for each biquad stage
- Creates 4th order Linkwitz-Riley when cascaded

### **Optimization:**
- Consider SIMD for parallel band processing
- Process all bands simultaneously if possible

---

## ? Definition of Done

- [ ] All 5 bands implemented
- [ ] Flat frequency response when summed
- [ ] Phase coherent at crossovers
- [ ] Mute/solo working
- [ ] Latency compensation applied
- [ ] CPU usage <10ms per buffer
- [ ] Tests passing
- [ ] Documentation complete

---

**Task Created:** January 14, 2026  
**Target Start:** After Task 2.2.1  
**Target Completion:** 3-5 days after start  
**Dependencies:** Biquad Filter (Task 2.2.1)  
**Blocks:** Task 2.4 (Per-Band Processing)
