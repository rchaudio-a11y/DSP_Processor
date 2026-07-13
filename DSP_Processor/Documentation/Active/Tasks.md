# Active Tasks - DSP_Processor

**Last Updated:** July 12, 2026  
**Current Version:** v1.3.5.1 (v1.3.x lineage; SubPhase 3.5 = feature 001-float32-pipeline on branch `001-float32-pipeline`; granularity: one tracker task/version per spec-kit USER STORY)  
**Current Task:** Feature 001-float32-pipeline — US1 COMPLETE, US2 in progress

---

## CURRENT - SubPhase 3.5: Feature 001-float32-pipeline (spec-driven, FEATURE BRANCH)

**Spec:** `specs/001-float32-pipeline/` | **Branch:** `001-float32-pipeline` (merge to master gated on manual listening checks, T020)

| Version | Tracker task (= user story) | Status |
|---------|------------------------------|--------|
| v1.3.5.1 | US1 - One Domain, One Conversion Per Boundary (float32 migration) | ✅ COMPLETE 2026-07-13 |
| v1.3.5.2 | US2 - Inter-Stage Headroom, Exit-Only Clamping (proofs) | ✅ COMPLETE 2026-07-13 (0-alloc audit green) |
| v1.3.5.3 | US3 - Balance Pan Law (proofs + supersession record) | ✅ COMPLETE 2026-07-13 — feature CODE-COMPLETE; merge gated on manual listening (T020) |

---

## CURRENT - SubPhase 3.4: Feature 002-state-machine-hardening (spec-driven)

**Spec:** `specs/002-state-machine-hardening/`

| Version | Tracker task (= user story) | Status |
|---------|------------------------------|--------|
| v1.3.4.1 | US1 - Truthful, Lossless Transition Requests (single-drainer GSM) | ✅ COMPLETE 2026-07-13 |
| v1.3.4.2 | US2 - Deadlock-Free, Ordered Event Delivery (+ 11-subscriber audit) | ✅ COMPLETE 2026-07-13 (audit: 0 fixes) |
| v1.3.4.3 | US3 - Process-Lifetime Coordinator | ✅ COMPLETE 2026-07-13 (manual app smoke pending) |

---

## CURRENT - SubPhase 3.3: Feature 003-tap-consolidation (spec-driven)

**Spec:** `specs/003-tap-consolidation/` (spec, plan, tasks, research, data-model, contracts)

| Version | Tracker task (= user story) | Status |
|---------|------------------------------|--------|
| v1.3.3.1 | US1 - Provably Correct Audio Core (test foundation, 39 tests, FR-015 gate) | ✅ COMPLETE 2026-07-12 |
| v1.3.3.2 | US2 - Detected, Never Silent, Data Loss (monotonic counters, overrun detection) | ✅ COMPLETE 2026-07-12 |
| v1.3.3.3 | US3 - One Way to Monitor (tap API consolidation, legacy removal) | ✅ COMPLETE 2026-07-12 (manual smoke SC-006 pending) |

---

## CURRENT - v1.3.2.5: Review Quick-Win Punch List (2026-07-12) - COMPLETE

**Source:** Full project review (`Documentation/Active/Code-Review-2026-07-12-Full-Project-Claude.md` at repo root)  
**Scope:** B1, B2, B3, B4, B7, B8, P1, D4, D7 (logging half), RecordingEngine Thread.Abort crash fix, repo hygiene (30 files archived, 5 doc renames)  
**Details:** See `Changelog/CURRENT.md` entry for v1.3.2.5

**Next candidates (from review, per spec-kit flow):**
1. 002-state-machine-hardening (B5 + P2 + D5 - raise GSM events outside lock, delete pending queue)
2. 003-tap-consolidation (D3 + B6/P4 + D8 ring buffer tests)
3. 001-float32-pipeline (D1 - strategic, already named in constitution)

---

## STALE CONTENT BELOW - RECONCILIATION PENDING

> Everything below this line was last refreshed January 15, 2026 (Phase 3.1) and
> predates Phases 4-7 (WASAPI, SSM architecture, cognitive layer). It no longer
> reflects reality. Kept for history until the tracker is reconciled.

---

## ?? **CURRENT FOCUS**

### **Phase 3.1: DSP Signal Flow UI** ?

**Goal:** Build professional mixing console-style DSP controls with vertical signal flow.

**Status:** Layout complete, need to wire events and add meters.

---

## ? **COMPLETED TASKS**

### **Task 3.1.1: Verify Stereo Processing** ?
- **Time:** ~10 minutes
- **Result:** Backend fully stereo-ready!
- **Documentation:** [Stereo Verification Test](../05-Testing/Stereo-Verification.md)

### **Task 3.1.2: Add Pan Control to GainProcessor** ?
- **Time:** ~30 minutes
- **Result:** Constant-power pan law implemented
- **File:** `DSP\GainProcessor.vb`

### **Task 3.1.3: DSP Signal Flow UI Layout** ?
- **Time:** ~1 hour
- **Result:** 3-column mixing console layout complete
- **File:** `UI\DSPSignalFlowPanel.vb`
- **Design:** Left/right meter columns + center controls column

---

## ? **IN PROGRESS**

### **Task 3.1.3 (continued): Wire DSP UI** ?? **ACTIVE**
**Priority:** HIGH  
**Time Estimate:** 2-3 hours  
**Status:** Layout done, need event wiring

**Next Steps:**
1. Add VolumeMeterControl instances to left/right panels
2. Wire trackbar events to update labels
3. Wire controls to GainProcessor properties
4. Test sliders update in real-time
5. Apply dark theme polish

**Files to Touch:**
- `UI\DSPSignalFlowPanel.vb` (add event handlers)
- `MainForm.vb` (wire to processors)

---

## ?? **TODO - PHASE 3.1**

### **Task 3.1.4: Add Filter Curve Overlay to FFT**
**Priority:** MEDIUM  
**Time Estimate:** 2-3 hours  
**Dependencies:** Task 3.2.2 (need filters first)

Overlay filter frequency response on FFT spectrum analyzer.

---

## ?? **TODO - PHASE 3.2: FILTERS**

### **Task 3.2.1: Implement High-Pass Filter**
**Priority:** HIGH  
**Time Estimate:** 4-5 hours  
**Status:** Not Started

**Specs:**
- 2nd-order Butterworth high-pass
- 12 dB/octave roll-off
- Frequency range: 30-180 Hz
- Default: 30 Hz, enabled (DC protection)

**Files to Create:**
- `DSP\HighPassFilter.vb`

### **Task 3.2.2: Implement Low-Pass Filter**
**Priority:** HIGH  
**Time Estimate:** 2-3 hours  
**Dependencies:** Task 3.2.1

**Specs:**
- 1st-order IIR low-pass
- 6 dB/octave roll-off
- Frequency range: 8-20 kHz
- Default: 18 kHz, disabled

**Files to Create:**
- `DSP\LowPassFilter.vb`

---

## ?? **TODO - PHASE 3.3: OUTPUT MIXER**

### **Task 3.3.1: Create Output Mixer**
**Priority:** MEDIUM  
**Time Estimate:** 2-3 hours  
**Dependencies:** Task 3.2.2

- Master gain (-60 to +12 dB)
- Left/Right channel gains
- Stereo width control (0-200%)

**Files to Create:**
- `DSP\OutputMixer.vb`

---

## ?? **TODO - PHASE 3.4: MONITORING**

### **Task 3.4.1: Add FFT Tap Points**
**Priority:** HIGH  
**Time Estimate:** 2-3 hours

Add tap points at each DSP stage for FFT visualization.

### **Task 3.4.2: Add Level Meter Tap Points**
**Priority:** MEDIUM  
**Time Estimate:** 1-2 hours

Multi-meter display showing levels at each stage.

---

## ?? **TODO - PHASE 3.5: INTEGRATION**

### **Task 3.5.1: Wire Complete DSP Chain**
**Priority:** CRITICAL  
**Time Estimate:** 2-3 hours

Connect all processors in signal chain.

### **Task 3.5.2: Comprehensive Testing**
**Priority:** CRITICAL  
**Time Estimate:** 3-4 hours

Full end-to-end testing of DSP chain.

---

## ?? **PROGRESS SUMMARY**

### **Phase 3.1: Stereo Foundation** (2/4 tasks)
- [x] Task 3.1.1: Stereo verification ?
- [x] Task 3.1.2: Pan control ?
- [x] Task 3.1.3: DSP UI layout ?
- [ ] Task 3.1.3: Wire DSP UI ? **IN PROGRESS**
- [ ] Task 3.1.4: Filter curve overlay

**Progress:** 50% (UI layout done, event wiring in progress)

### **Phase 3.2: Filters** (0/2 tasks)
- [ ] Task 3.2.1: High-pass filter
- [ ] Task 3.2.2: Low-pass filter

### **Phase 3.3: Output Mixer** (0/1 tasks)
- [ ] Task 3.3.1: Output mixer

### **Phase 3.4: Monitoring** (0/2 tasks)
- [ ] Task 3.4.1: FFT tap points
- [ ] Task 3.4.2: Level meter taps

### **Phase 3.5: Integration** (0/2 tasks)
- [ ] Task 3.5.1: Wire DSP chain
- [ ] Task 3.5.2: Comprehensive testing

---

## ?? **TIME TRACKING**

### **Time Spent:**
- Phase 3.1.1: 10 min (verification)
- Phase 3.1.2: 30 min (pan control)
- Phase 3.1.3: 1 hour (UI layout)
- **Total:** ~1 hour 40 min

### **Time Remaining:**
- Phase 3.1: 2-3 hours (wire UI + overlay)
- Phase 3.2: 6-8 hours (filters)
- Phase 3.3: 2-3 hours (mixer)
- Phase 3.4: 3-5 hours (monitoring)
- Phase 3.5: 5-7 hours (integration)
- **Total:** ~20-30 hours

---

## ?? **NEXT SESSION**

**Priority 1:** Wire DSP UI events (Task 3.1.3)
- Add meter controls
- Wire sliders
- Test real-time updates

**Priority 2:** Start High-Pass Filter (Task 3.2.1)
- Implement Butterworth biquad
- Add stereo support
- Test with audio

---

## ?? **NOTES**

### **Design Decisions:**
- Vertical signal flow layout (mixing console style)
- 3-column layout: L meter | controls | R meter
- Constant-power pan law for GainProcessor
- 2nd-order HPF for sharp DC rejection
- 1st-order LPF for gentle hiss reduction

### **DSP Tap Point Pattern (Established Jan 15, 2026):**
**Every DSP processor should implement tap points for monitoring:**

1. **ProcessorBase** provides `SetMonitorOutputCallback()` and `SendToMonitor()` methods
2. Each processor calls `SendToMonitor(buffer)` after processing in `ProcessInternal()`
3. DSPThread provides parallel monitor buffers for each tap point (non-blocking)
4. AudioRouter exposes samples from each tap point for meters/analysis

**Current Tap Points:**
- `InputSamples` - Raw audio from file (PRE-DSP)
- `PostGainSamples` - After Gain/Pan processor (DSP Tap Point Pattern) ?
- `OutputSamples` - Final output after all DSP (POST-DSP)

**When adding new processors:**
- Call `SendToMonitor(buffer)` at end of `ProcessInternal()`
- Add monitor buffer to DSPThread if visualization needed
- Expose samples via AudioRouter property
- Update meters/FFT to use appropriate tap point

**Example (GainProcessor):**
```vb
Protected Overrides Sub ProcessInternal(buffer As AudioBuffer)
    ' ... do DSP work ...
    
    ' DSP TAP POINT PATTERN: Send to monitor
    SendToMonitor(buffer)
End Sub
```

### **Technical Debt:**
- None currently (Phase 2.0 refactoring complete!)

---

**For full task details, see:**
- [DSP Feature Implementation Plan](DSP-Feature-Implementation-Plan.md)
- [Phase 3.1 Detailed Plan](Phase-3.1-DSP-UI.md)

**Last Updated:** January 15, 2026
