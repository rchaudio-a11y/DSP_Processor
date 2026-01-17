# Versioning Instructions

**Reference:** Main [copilot-instructions.md](../copilot-instructions.md)

---

## ?? Task-Aligned Versioning Scheme

**Format:** `v[Major].[Phase].[SubPhase].[Task]`

### **Structure**
```
v1.3.1.3
? ? ? ?
? ? ? ?? Task 3 (Wire DSP UI)
? ? ???? Sub-phase 1 (UI Foundation)
? ?????? Phase 3 (DSP Signal Flow)
???????? Major Version 1 (Project Iteration)
```

**Benefits:**
- ? Self-documenting progress in version number
- ? Aligns with RDF phases and task structure
- ? Explicit tracking without consulting docs
- ? Matches `Active/Tasks.md` structure perfectly

---

## ?? Version Component Definitions

### **Major Version (v[X].x.x.x)**
**What it represents:** Complete project iteration or major architectural shift

**Examples:**
- v1.x.x.x = DSP_Processor Initial Release
- v2.x.x.x = Major architectural rewrite (if ever needed)

**When to increment:**
- Complete rebuild
- Breaking API changes
- Major methodology shift
- RDF Meta-Phase recursion triggers complete redesign

---

### **Phase (vx.[X].x.x)**
**What it represents:** Major feature phase or architectural milestone

**Current Phases:**
- Phase 0 = Foundation (initial setup, basic structure)
- Phase 1 = Core Audio Engine
- Phase 2 = Refactoring & Architecture
- Phase 3 = DSP Signal Flow
- Phase 4 = (Future) Advanced DSP
- Phase 5 = (Future) Plugin System
- etc.

**When to increment:**
- Starting new major feature area
- Completing RDF Phase 6 (Synthesis) for major milestone
- Beginning new architectural component

**Aligns with:** `Active/Tasks.md` Phase headers

---

### **Sub-Phase (vx.x.[X].x)**
**What it represents:** Feature sub-section within a phase

**Examples (Phase 3):**
- 3.1 = UI Foundation (Task 3.1.x)
- 3.2 = Filters (Task 3.2.x)
- 3.3 = Output Mixer (Task 3.3.x)
- 3.4 = Monitoring (Task 3.4.x)
- 3.5 = Integration (Task 3.5.x)

**When to increment:**
- Moving to next sub-section in `Active/Tasks.md`
- Completing sub-phase goals

---

### **Task (vx.x.x.[X])**
**What it represents:** Individual task completion

**Examples (Phase 3.1):**
- 3.1.1 = Verify Stereo Processing
- 3.1.2 = Add Pan Control
- 3.1.3 = Wire DSP UI
- 3.1.4 = Filter Curve Overlay

**When to increment:**
- Completing each task in `Active/Tasks.md`
- After documentation/testing for that task

---

## ?? Versioning Workflow

### **Starting a New Task**

1. **Check `Active/Tasks.md`** for current task number
2. **Identify version:** v[Major].[Phase].[SubPhase].[Task]
3. **Work on task** following RDF phases

### **Completing a Task**

1. **Mark task complete** in `Active/Tasks.md`
2. **Update Changelog** with version entry
3. **Create session notes** (if significant work)
4. **Git commit** with version in message:
   ```bash
   git commit -m "v1.3.1.3 - Wire DSP UI
   
   - Event-driven meter updates
   - Double-click reset on sliders
   - DSPSignalFlowPanel wiring complete
   
   RDF Phase 4-6"
   ```
5. **Tag release:**
   ```bash
   git tag -a v1.3.1.3 -m "Task 3.1.3 complete"
   ```

### **Moving to Next Task**

1. **Increment task number:** v1.3.1.3 ? v1.3.1.4
2. **Update `Active/Tasks.md`** "In Progress" section
3. **Reference previous version** in new work

### **Moving to Next Sub-Phase**

1. **Increment sub-phase, reset task:** v1.3.1.4 ? v1.3.2.1
2. **Update `Active/Tasks.md`** Phase section
3. **Document sub-phase completion** if significant

### **Moving to Next Phase**

1. **Increment phase, reset sub-phase/task:** v1.3.5.2 ? v1.4.0.0
2. **Major Changelog entry**
3. **Phase completion documentation**
4. **Consider major Git tag:** `v1.4-phase4-complete`

---

## ?? Documentation Requirements

### **Every Version (Task Completion)**
- [ ] Mark task complete in `Active/Tasks.md`
- [ ] Update time tracking in `Active/Tasks.md`
- [ ] Add entry to `Changelog/CURRENT.md`

### **Sub-Phase Completion (vx.x.[X].last)**
- [ ] All of the above
- [ ] Session notes documenting sub-phase
- [ ] Move detailed docs to `Completed/Tasks/`

### **Phase Completion (vx.[X].last.last)**
- [ ] All of the above
- [ ] Comprehensive phase documentation
- [ ] Move to `Completed/Features/`
- [ ] Update architecture docs if needed

---

## ?? Current Version Status

**Active Version:** v1.3.1.3  
**Status:** In Progress (Testing)  
**Task:** Wire DSP UI (Task 3.1.3)  
**Phase:** 3.1 - DSP Signal Flow UI Foundation

**Work Completed:**
- Event-driven meter updates
- Double-click reset implemented
- DSPSignalFlowPanel event wiring
- Volume control cleanup

**Next:** Task 3.1.4 - Filter Curve Overlay ? v1.3.1.4

---

## ?? Version History Examples

### **Phase 3.1 Progression**
```
v1.3.1.1 - Verify Stereo Processing (Jan 15, 2026)
v1.3.1.2 - Add Pan Control (Jan 15, 2026)
v1.3.1.3 - Wire DSP UI (Jan 16, 2026) ? Current
v1.3.1.4 - Filter Curve Overlay (Planned)
```

### **Phase 3.2 (Future)**
```
v1.3.2.1 - Implement High-Pass Filter
v1.3.2.2 - Implement Low-Pass Filter
```

### **Phase 4 (Future)**
```
v1.4.0.0 - Phase 4 Start
v1.4.1.1 - First task in phase 4
```

---

## ?? Integration with Other Tools

### **Git Commit Messages**
```bash
# Format:
git commit -m "v[version] - [Task Name]

[Detailed changes]
[Detailed changes]

RDF Phase [X]"

# Example:
git commit -m "v1.3.1.3 - Wire DSP UI

- Event-driven meter updates
- Double-click reset on sliders
- DSPSignalFlowPanel wiring complete

RDF Phase 4-6"
```

### **Git Tags**
```bash
# Format: v[Major].[Phase].[SubPhase].[Task]
git tag -a v1.3.1.3 -m "Task 3.1.3: Wire DSP UI complete"
```

### **Changelog Entries**
```markdown
## [v1.3.1.3] - 2026-01-16 - Wire DSP UI

**Task:** 3.1.3 (Phase 3.1 - DSP Signal Flow UI)  
**RDF Phase:** 4-6 (Debug ? Synthesis)

### Changes
- Event-driven meter updates
- Double-click reset on all sliders
- DSPSignalFlowPanel event wiring
```

---

## ?? RDF Phase Alignment

**Version increments align with RDF phases:**

| RDF Phase | Typical Version Change |
|-----------|----------------------|
| **Phase 1-2** (Curiosity ? Insight) | Planning, no version change |
| **Phase 3** (Build) | Task increment (vx.x.x.[+1]) |
| **Phase 4** (Debug) | Patch fixes within same task |
| **Phase 5** (Validate) | Testing within same task |
| **Phase 6** (Synthesis) | Task completion ? version tag |
| **Meta-Phase** (Recursion) | May trigger phase change |

---

## ?? Best Practices

### **DO:**
- ? Increment task number with EACH completed task
- ? Tag releases at task completion
- ? Document version in all commit messages
- ? Update `Active/Tasks.md` immediately
- ? Keep version synchronized with task progress

### **DON'T:**
- ? Skip version numbers (no jumping v1.3.1.2 ? v1.3.1.5)
- ? Use version for minor fixes (use patch versions or keep same)
- ? Forget to update `Active/Tasks.md`
- ? Create tags without completing task documentation

---

## ?? Quick Reference

**Current Version Format:** v1.3.1.3  
**Breakdown:**
- 1 = Major (Project Iteration 1)
- 3 = Phase 3 (DSP Signal Flow)
- 1 = Sub-Phase 1 (UI Foundation)
- 3 = Task 3 (Wire DSP UI)

**Find Current Task:** `Documentation/Active/Tasks.md` ? "IN PROGRESS" section  
**Version History:** `Documentation/Changelog/CURRENT.md`  
**Task Details:** `Documentation/Active/Tasks.md` ? Task section

---

**Last Updated:** 2026-01-16  
**Version:** 1.0.0  
**Current Project Version:** v1.3.1.3 (In Progress - Testing)
