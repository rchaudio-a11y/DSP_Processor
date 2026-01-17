# Implementation Priority Summary - v1.3.2.0

**Created:** 2026-01-16  
**Task Plan:** [Task-v1_3_2_0-TapPointManager-Implementation.md](Task-v1_3_2_0-TapPointManager-Implementation.md)

---

## ?? Executive Summary

Implementing **TWO major architectural systems** in one release:
1. **TapPointManager** - Fixes user-visible meter bugs
2. **State Machine** - Adds centralized state control

**Total Time:** 10-12 hours (3-4 sessions)  
**Current Version:** v1.3.1.3 ? **Target:** v1.3.2.0

---

## ?? Priority Breakdown

### **?? PRIORITY 1: TapPointManager (5-6 hours)**

**User Impact:** HIGH - Fixes broken UI feedback  
**Technical Impact:** Architectural improvement

**Phases:**
- Phase 1: Core Infrastructure (2 hours)
- Phase 2: DSPThread Integration (45 min)
- Phase 3: MainForm Integration (2.25 hours)

**Deliverables:**
? Meters display DSP-processed audio  
? Gain sliders affect meters immediately  
? Pan control shows L/R stereo imaging  
? Input/output stages distinguishable

**Issues Resolved:** #001, #002, #003, #005

---

### **?? PRIORITY 2: State Machine (3-4 hours)**

**User Impact:** MEDIUM - Better stability, clearer feedback  
**Technical Impact:** Prevents future bugs

**Phases:**
- Phase 4: State Machine Architecture (3.3 hours)

**Deliverables:**
? Centralized ApplicationState enum  
? StateManager singleton with validation  
? UI syncs with state automatically  
? Invalid operations prevented  
? LED colors reflect state  

**Why After TapPointManager:** 
- TapPointManager fixes immediate user pain
- State machine adds stability layer on top
- Both tested together ensures compatibility

---

### **?? PRIORITY 3: Testing & Documentation (3-4 hours)**

**Phases:**
- Phase 5: Testing (2 hours)
- Phase 6: Documentation (2 hours)

**Deliverables:**
? Manual integration tests pass  
? State machine transitions validated  
? Architecture docs updated  
? Session notes created  
? Changelog updated  
? Git tagged v1.3.2.0

---

## ??? Recommended Session Plan

### **Session 1: Foundation (3 hours)**
**Goal:** Build core infrastructure  
**Phases:** Phase 1 + Phase 2
- Create TapPoint enum
- Create TapPointManager class
- Integrate with DSPThread

**Checkpoint:** TapPointManager compiles, no runtime integration yet

---

### **Session 2: Integration (4 hours)**
**Goal:** Wire up meters + add state control  
**Phases:** Phase 3 + Phase 4
- Create tap readers in MainForm
- Update meters to use tap points
- Implement stereo L/R separation
- Create StateManager
- Integrate state machine into MainForm

**Checkpoint:** Meters show processed audio, states transition

---

### **Session 3: Validation (3 hours)**
**Goal:** Test everything together  
**Phases:** Phase 5 + Phase 6
- Manual integration testing
- State machine validation
- Update all documentation
- Git commit v1.3.2.0

**Checkpoint:** Release candidate ready

---

## ?? Progress Tracking

### **Completion Checklist:**

**Phase 1: Core Infrastructure** [ ]
- [ ] Task 1.1: TapPoint enum (15 min)
- [ ] Task 1.2: TapPointReader class (20 min)
- [ ] Task 1.3: TapPointManager class (1.5 hours)

**Phase 2: DSPThread Integration** [ ]
- [ ] Task 2.1: Expose buffers (15 min)
- [ ] Task 2.2: Add to RecordingManager (30 min)

**Phase 3: MainForm Integration** [ ]
- [ ] Task 3.1: Create readers (45 min)
- [ ] Task 3.2: Update meter code (1 hour)
- [ ] Task 3.3: Stereo separation (30 min)

**Phase 4: State Machine** [ ]
- [ ] Task 4.0: ApplicationState enum (20 min)
- [ ] Task 4.1: StateManager class (1.5 hours)
- [ ] Task 4.2: MainForm integration (1 hour)
- [ ] Task 4.3: State guards (30 min)

**Phase 5: Testing** [ ]
- [ ] Task 5.1: Unit tests (30 min) - OPTIONAL
- [ ] Task 5.2: Manual testing (1 hour)
- [ ] Task 5.3: State testing (30 min)

**Phase 6: Documentation** [ ]
- [ ] Task 6.1: Architecture docs (45 min)
- [ ] Task 6.2: Session notes (30 min)
- [ ] Task 6.3: Update issues (10 min)
- [ ] Task 6.4: Changelog (20 min)
- [ ] Task 6.5: Git commit (15 min)

---

## ?? Success Criteria

### **Must Have (Release Blocking):**
1. ? Meters show DSP-processed audio
2. ? Sliders affect meters immediately
3. ? State transitions work correctly
4. ? No crashes or exceptions
5. ? Recording still works

### **Should Have (Quality):**
6. ? Stereo L/R separation visible
7. ? LED colors sync with state
8. ? Invalid operations blocked
9. ? Documentation complete

### **Nice to Have (Polish):**
10. ? Unit tests written
11. ? Performance benchmarks
12. ? State machine diagram

---

## ?? Related Documents

**Issues:**
- [#001 - Meters Bypass DSP](Issues/v1_3_1_3-001-Meters-Bypass-DSP.md)
- [#002 - Meters Same Value](Issues/v1_3_1_3-002-Meters-Show-Same-Value.md)
- [#003 - Tap Points Unused](Issues/v1_3_1_3-003-Tap-Points-Unused.md)
- [#005 - No TapPointManager](Issues/v1_3_1_3-005-No-TapPointManager.md)

**Architecture:**
- [Audio Signal Flow](../Architecture/Audio-Signal-Flow-v1_3_1_3.md)

**Tasks:**
- [Full Implementation Plan](Task-v1_3_2_0-TapPointManager-Implementation.md)

---

**Created By:** Rick + GitHub Copilot (RDF Planning)  
**Status:** Ready to Execute  
**Next Step:** Begin Phase 1 - Create TapPoint enum
