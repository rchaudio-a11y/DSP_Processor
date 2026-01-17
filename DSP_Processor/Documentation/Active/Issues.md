# Active Issues - DSP_Processor v1.3.1.3

**Last Updated:** 2026-01-16  
**Current Version:** v1.3.1.3 (In Progress - Testing)

---

## ?? Critical Issues

None currently.

---

## ?? High Priority

### **#001: Meters Display Raw Audio Bypassing DSP Chain**
**Status:** Open - Root Cause Identified  
**Created:** 2026-01-16  
**Affects:** All meters, gain/pan control feedback

**Summary:** UI meters show raw microphone audio instead of DSP-processed audio, making slider controls appear non-functional.

**Details:** [v1_3_1_3-001-Meters-Bypass-DSP.md](Issues/v1_3_1_3-001-Meters-Bypass-DSP.md)

---

## ?? Medium Priority

### **#002: DSPSignalFlowPanel Meters Show Identical Values**
**Status:** Open - Root Cause Identified  
**Created:** 2026-01-16

**Details:** [v1_3_1_3-002-Meters-Show-Same-Value.md](Issues/v1_3_1_3-002-Meters-Show-Same-Value.md)

### **#003: Tap Point Buffers Created But Never Used**
**Status:** Open  
**Created:** 2026-01-16

**Details:** [v1_3_1_3-003-Tap-Points-Unused.md](Issues/v1_3_1_3-003-Tap-Points-Unused.md)

---

## ?? Low Priority

### **#004: Master Volume and Stereo Width Not Implemented**
**Status:** Open - Deferred

**Details:** [v1_3_1_3-004-Master-Width-Not-Implemented.md](Issues/v1_3_1_3-004-Master-Width-Not-Implemented.md)

### **#005: No Centralized Tap Point Management Infrastructure**
**Status:** Open - Future Enhancement  
**Created:** 2026-01-16

**Summary:** Tap point buffers accessed as direct Friend fields with no manager, enum, or unified API. Creates tight coupling and maintenance burden.

**Recommendation:** Defer to v1.3.2.0 - Create TapPointManager for cleaner architecture

**Details:** [v1_3_1_3-005-No-TapPointManager.md](Issues/v1_3_1_3-005-No-TapPointManager.md)

---

## ?? Summary

| Priority | Count |
|----------|-------|
| High | 1 |
| Medium | 2 |
| Low | 2 |
| **Total** | **5** |

---

**Next:** [Task-List-v1_3_1_3-Audio-Flow-Fix.md](Task-List-v1_3_1_3-Audio-Flow-Fix.md)
