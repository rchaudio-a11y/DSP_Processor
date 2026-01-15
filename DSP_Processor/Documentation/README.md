# DSP_Processor Documentation

**Project:** Audio Recording & DSP Processing Application  
**Repository:** https://github.com/rchaudio-a11y/DSP_Processor  
**Language:** Visual Basic .NET  
**Framework:** .NET Framework / NAudio

---

## ?? Documentation Structure

This documentation is organized into the following categories:

### **?? [Plans/](Plans/)** - Implementation Plans & Guides
Long-term roadmaps, phase implementation guides, and architectural planning documents.

**Key Files:**
- `Implementation-Plan-Update-2026.md` - ? **START HERE** - Current status & updates
- `Phase-2-Plus-DSP-Implementation-Guide.md` - Detailed DSP implementation guide
- `Phase-2-Implementation-Status-Report.md` - Phase 2 completion status
- See [Plans/README.md](Plans/README.md) for complete index (19 files)

### **? [Tasks/](Tasks/)** - Task Tracking & Specifications
Granular task breakdowns with checklists, dependencies, and acceptance criteria.

**Key Files:**
- `README.md` - Master task list with progress tracking
- `Task-0.x-*.md` - Phase 0 foundation tasks
- `Task-1.x-*.md` - Phase 1 input engine tasks
- `Task-2.x-*.md` - Phase 2 DSP engine tasks
- See [Tasks/README.md](Tasks/README.md) for complete list (8 files)

### **?? [Issues/](Issues/)** - Bug Reports & Resolutions
Detailed bug reports, investigations, root cause analysis, and fixes.

**Recent:**
- `Bug-Report-2026-01-14-Recording-Clicks-Pops.md` - ? RESOLVED (High severity)
- See [Issues/README.md](Issues/README.md) for complete index (11 files)

### **?? [Architecture/](Architecture/)** - System Architecture & Design
System architecture documentation, audio pipeline analysis, threading models.

**Key Files:**
- `Master-Architecture-Threading-And-Performance.md` - Core architecture
- `Audio-Pipeline-Analysis.md` - Audio data flow
- See [Architecture/README.md](Architecture/README.md) for details (4 files)

### **?? [Features/](Features/)** - Feature Specifications
Feature specifications, UI component docs, and system feature designs.

**Key Files:**
- `Record_Options.md` - Recording modes documentation
- `Unified-Logging-System.md` - Logging system spec
- See [Features/README.md](Features/README.md) for complete list (6 files)

### **?? [Project/](Project/)** - Project Documentation
High-level project outlines, scope, and foundational project information.

**Key Files:**
- `Project Outline.md` - Original project scope
- `Project outline 2.md` - Updated roadmap
- See [Project/README.md](Project/README.md) for overview (2 files)

### **?? [Sessions/](Sessions/)** - Work Session Summaries
Detailed summaries of development sessions, decisions made, and outcomes.

**Recent:**
- `Session-Summary-2026-01-14-Audio-Quality-Fix.md` - Buffer optimization (1 file)

### **? [Completed/](Completed/)** - Completed Milestones
Historical records of completed features and major milestones.

**Archive:**
- UI cleanup, Transport integration, FFT implementation, etc. (8 files)

---

## ?? Quick Start

### **For New Contributors:**
1. Read `Plans/Implementation-Plan-Update-2026.md` for current status
2. Review `Tasks/README.md` for active tasks
3. Check `Issues/README.md` for known issues
4. See **Current Priorities** section below

### **For Bug Reports:**
1. Check `Issues/` folder for existing reports
2. Follow template in `Issues/README.md`
3. Include reproduction steps and environment details

### **For Feature Implementation:**
1. Check if task exists in `Tasks/` folder
2. Follow task checklist step-by-step
3. Update task status as you progress
4. Document any deviations

---

## ?? Project Status

### **Current Phase:** Phase 0-1 (Foundation & Advanced Input)

**Overall Progress:** 19% Complete

| Phase | Status | Completion |
|-------|--------|------------|
| Phase 0: Foundation | ? Complete | 90% |
| Phase 1: Advanced Input | ?? In Progress | 15% |
| Phase 2: DSP Engine | ?? Ready to Start | 20% |
| Phase 3: UI Enhancements | ?? Not Started | 0% |
| Phase 4: Project System | ?? Not Started | 0% |

### **Recent Achievements (2026-01-14):**
- ? Fixed audio clicks/pops with dual freewheeling buffers
- ? Implemented async FFT processing for smooth visualization
- ? Resolved buffer queue overflow (1015 buffers ? 0-5)
- ? Achieved zero-latency audio thread (< 1ms per tick)

### **Next Priorities:**
1. **Task 1.2 - WASAPI Integration** (3-5 days) - Lower latency than WaveIn
2. **Task 2.2.1 - Biquad Filter** (1-2 days) - Blocks Phase 2 DSP work

---

## ??? Core Documentation Files

### **Master Plans:**
- [`Plans/Implementation-Plan-Update-2026.md`](Plans/Implementation-Plan-Update-2026.md) - **START HERE**
- [`CHANGELOG.md`](CHANGELOG.md) - Development history & recent changes
- [`Tasks/README.md`](Tasks/README.md) - Task list with progress tracking

### **Technical References:**
- `Plans/Phase-2-Plus-DSP-Implementation-Guide.md` - DSP algorithms & implementation
- `Plans/Master-Architecture-Threading-And-Performance.md` - Threading model
- `Plans/Phase-2-Documentation-Index.md` - Phase 2 documentation index

### **Project Metadata:**
- `Project Outline.md` - Original project scope
- `Project outline 2.md` - Updated scope
- `Record_Options.md` - Recording modes documentation
- `Log-Viewer-Tab.md` - Logging system documentation

---

## ?? Reading Order for New Developers

1. **Start with Overview:**
   - `README.md` (this file)
   - `Plans/Implementation-Plan-Update-2026.md`

2. **Understand Current State:**
   - `CHANGELOG.md` - Recent changes
   - `Tasks/README.md` - Active tasks
   - `Issues/README.md` - Known issues

3. **Deep Dive by Interest:**
   - **DSP Focus:** `Plans/Phase-2-Plus-DSP-Implementation-Guide.md`
   - **Architecture:** `Plans/Master-Architecture-Threading-And-Performance.md`
   - **Bug Fixes:** `Issues/Bug-Report-2026-01-14-Recording-Clicks-Pops.md`

4. **Start Contributing:**
   - Pick a task from `Tasks/README.md`
   - Follow task checklist
   - Update documentation as you go

---

## ?? Technical Stack

### **Languages & Frameworks:**
- **Language:** Visual Basic .NET
- **Framework:** .NET Framework 4.7.2+
- **UI:** Windows Forms

### **Key Libraries:**
- **NAudio** - Audio capture, playback, and processing
- **System.Numerics** - Complex FFT calculations
- **Newtonsoft.Json** - Settings persistence

### **Architecture:**
- Manager pattern (RecordingManager, PlaybackManager, etc.)
- Event-driven audio processing
- Lock-free concurrent queues
- Async FFT processing on background threads

---

## ?? Documentation Standards

### **File Naming:**
- Plans: `[Description]-Plan.md` or `Phase-X-[Description].md`
- Tasks: `Task-X.Y-[Description].md`
- Issues: `Bug-Report-YYYY-MM-DD-[Description].md`
- Sessions: `Session-Summary-YYYY-MM-DD-[Description].md`

### **Content Requirements:**
All documentation must include:
- Clear title and date
- Objective/purpose statement
- Current status
- Step-by-step details
- Results/outcomes

### **Update Frequency:**
- `CHANGELOG.md` - Every significant change
- `Tasks/README.md` - When task status changes
- Issue reports - When bugs are discovered/resolved
- Session summaries - After major work sessions

---

## ?? Key Concepts

### **Buffer Architecture (2026-01-14 Update):**
- **Dual Queue System:** Separate critical (recording) from non-critical (FFT) paths
- **Freewheeling:** Visualization can drop frames without affecting audio quality
- **Async Processing:** CPU-intensive FFT runs on background thread

### **Recording Modes:**
- **Manual Mode:** User controls start/stop
- **Timed Mode:** Auto-stop after duration
- **Loop Mode:** Automatic repeated recordings with delay

### **Audio Pipeline:**
```
Mic ? Dual Queue ? [Recording Path + FFT Path]
                   ?                    ?
              File I/O            Async FFT
                                       ?
                                  Visualization
```

---

## ?? Support & Contact

**Repository:** https://github.com/rchaudio-a11y/DSP_Processor  
**Branch:** master  
**Status:** Active development

**For Issues:**
1. Search existing issues in `Issues/` folder
2. Check `CHANGELOG.md` for recent fixes
3. Create new issue report using template

**For Questions:**
1. Check relevant plan/task documentation
2. Review implementation guides
3. Consult session summaries for context

---

## ?? Credits

**Developer:** rchaudio-a11y  
**AI Assistant:** GitHub Copilot  
**License:** [See repository]

---

**Last Updated:** January 14, 2026  
**Documentation Version:** 2.0 (Reorganized 2026-01-14)  
**Project Status:** Active Development
