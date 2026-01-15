# Task 2.4: Implement Per-Band Processing

**Priority:** ?? **HIGH**  
**Status:** ? Not Started  
**Estimated Time:** 2-3 days  
**Dependencies:** Task 2.3 (Multiband Crossover)

---

## ?? Task Overview

Implement independent DSP processing for each frequency band in the multiband crossover. Each band gets its own processor chain for EQ, dynamics, and effects.

---

## ?? Objectives

1. Create `BandProcessor` class for each band
2. Implement per-band processor chains
3. Add per-band gain/level controls
4. Implement per-band compression (optional)
5. Add per-band EQ (optional)
6. Create UI controls for band management

---

## ?? Architecture

```
Input ? Crossover ? [Band 1 Chain] ? 
                  ? [Band 2 Chain] ? 
                  ? [Band 3 Chain] ? Mixer ? Output
                  ? [Band 4 Chain] ?
                  ? [Band 5 Chain] ?

Each Band Chain:
????????????????????????????????????
? Band Processor                   ?
????????????????????????????????????
? 1. Input Gain                    ?
? 2. Parametric EQ (optional)      ?
? 3. Compressor (optional)         ?
? 4. Output Gain                   ?
????????????????????????????????????
```

---

## ?? Implementation Checklist

### **Step 1: Create BandProcessor Class**
- [ ] Create `DSP\Multiband\BandProcessor.vb`
- [ ] Inherit from `ProcessorBase`
- [ ] Add properties:
  - [ ] `BandNumber As Integer` (1-5)
  - [ ] `InputGain As Single` (dB)
  - [ ] `OutputGain As Single` (dB)
  - [ ] `Enabled As Boolean`
  - [ ] `Muted As Boolean`
  - [ ] `Soloed As Boolean`
  - [ ] `Chain As ProcessorChain`

### **Step 2: Implement Basic Gain Control**
- [ ] Add input gain processor (before chain)
- [ ] Add output gain processor (after chain)
- [ ] Range: -60dB to +20dB
- [ ] Default: 0dB (unity gain)

### **Step 3: Add Processor Chain Management**
- [ ] Create internal `ProcessorChain` for each band
- [ ] Methods:
  - [ ] `AddProcessor(processor As IProcessor)`
  - [ ] `RemoveProcessor(processor As IProcessor)`
  - [ ] `ClearProcessors()`
  - [ ] `GetProcessorCount() As Integer`

### **Step 4: Implement Process() Method**
- [ ] Override `Process(buffer As AudioBuffer)`
- [ ] Check if band is enabled
- [ ] Apply input gain
- [ ] Process through band's processor chain
- [ ] Apply output gain
- [ ] Handle mute/solo states

### **Step 5: Add Per-Band Compressor (Optional)**
- [ ] Create `BandCompressor` class
- [ ] Inherit from `ProcessorBase`
- [ ] Parameters:
  - [ ] Threshold (dB)
  - [ ] Ratio (1:1 to ?:1)
  - [ ] Attack time (ms)
  - [ ] Release time (ms)
  - [ ] Knee width (dB)
  - [ ] Makeup gain (dB)
- [ ] Implement RMS level detection
- [ ] Implement gain reduction calculation
- [ ] Add gain reduction smoothing

### **Step 6: Add Per-Band EQ (Optional)**
- [ ] Create array of parametric EQ bands
- [ ] Each EQ band:
  - [ ] Type: Peaking, Low Shelf, High Shelf
  - [ ] Frequency (Hz)
  - [ ] Q factor
  - [ ] Gain (dB)
- [ ] Default: 3 parametric bands per frequency band

### **Step 7: Integrate with MultibandCrossover**
- [ ] Update `MultibandCrossover.Process()`
- [ ] After splitting bands, process each through `BandProcessor`
- [ ] Before mixing, collect processed bands

---

## ??? UI Controls (Future Task)

### **Per-Band Controls:**
- [ ] Band enable/disable toggle
- [ ] Mute/Solo buttons
- [ ] Input gain slider (-60 to +20 dB)
- [ ] Output gain slider (-60 to +20 dB)
- [ ] Level meter (input and output)
- [ ] Processor chain list

### **Compressor Controls (if implemented):**
- [ ] Threshold slider
- [ ] Ratio selector
- [ ] Attack/Release time sliders
- [ ] Gain reduction meter

### **EQ Controls (if implemented):**
- [ ] Frequency/Gain/Q knobs for each band
- [ ] Frequency response graph
- [ ] Enable/Bypass per EQ band

---

## ?? Testing Checklist

### **Functionality Tests:**
- [ ] Test per-band gain control
- [ ] Verify processor chain execution
- [ ] Test mute/solo on each band
- [ ] Test enable/disable
- [ ] Verify gain staging (no clipping)

### **Integration Tests:**
- [ ] Process full multiband chain
- [ ] Verify latency compensation
- [ ] Test with multiple processors per band
- [ ] Test with real-time audio

### **Performance Tests:**
- [ ] Measure CPU usage per band
- [ ] Target: <2ms per band (10ms total)
- [ ] Test with full processor chains
- [ ] Profile memory usage

---

## ?? Expected Performance

| Metric | Target | Notes |
|--------|--------|-------|
| **CPU per Band** | <2ms | With basic gain only |
| **Total CPU** | <10ms | All 5 bands |
| **Latency per Band** | 0 samples | Gain is instantaneous |
| **Memory per Band** | <1MB | Buffer + processors |

---

## ?? Implementation Tips

### **Gain Staging:**
- Monitor levels at each stage
- Prevent clipping between processors
- Use headroom (keep below 0dBFS)

### **Processor Order:**
- EQ before compression (typical)
- Input gain ? EQ ? Compressor ? Output gain

### **Performance:**
- Bypass disabled bands early
- Reuse buffers where possible
- Profile each processor

---

## ?? Reference Materials

### **Multiband Processing:**
- Typical use cases:
  - Mastering (subtle multiband compression)
  - Mixing (aggressive frequency control)
  - Sound design (creative effects)

### **Compression Parameters:**
- **Threshold:** Level above which compression starts
- **Ratio:** Amount of gain reduction (4:1 typical)
- **Attack:** How fast compressor responds (5-50ms)
- **Release:** How fast compressor recovers (50-500ms)
- **Makeup Gain:** Compensate for gain reduction

---

## ? Definition of Done

- [ ] BandProcessor class implemented
- [ ] Per-band gain control working
- [ ] Processor chain management complete
- [ ] Mute/solo working per band
- [ ] Integration with crossover complete
- [ ] CPU usage within target
- [ ] Tests passing
- [ ] Documentation complete

---

## ?? Optional Enhancements

- [ ] Per-band compression with sidechain
- [ ] Per-band saturation/distortion
- [ ] Per-band stereo width control
- [ ] Per-band delay compensation
- [ ] Preset management per band

---

**Task Created:** January 14, 2026  
**Target Start:** After Task 2.3  
**Target Completion:** 2-3 days after start  
**Dependencies:** Multiband Crossover (Task 2.3)  
**Blocks:** Task 2.5 (Integration & Testing)
