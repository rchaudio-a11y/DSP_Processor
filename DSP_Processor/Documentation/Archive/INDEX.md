# ?? DSP_Processor Documentation Index

**Last Updated:** January 15, 2026  
**Project:** DSP_Processor Audio Engine  
**Version:** Phase 3.1 (DSP Signal Flow UI)

---

## ?? **Quick Start**

**New to the project?** Start here:
1. [Project Overview](00-Project/Project-Overview.md) - What is this project?
2. [Getting Started](00-Project/Getting-Started.md) - Setup and build
3. [Architecture Overview](01-Architecture/Audio-Pipeline.md) - How it works

**Working on tasks?** Go here:
- [**Active Tasks**](04-Tasks/Active-Tasks.md) ? **START HERE FOR CURRENT WORK**
- [Phase 3.1: DSP Signal Flow UI](04-Tasks/Phase-3.1-DSP-UI.md)
- [Known Issues](06-Issues/Known-Issues.md)

---

## ?? **Documentation by Category**

### ?? **00-Project** - Project Information
High-level project documentation and onboarding.

- [Project Overview](00-Project/Project-Overview.md) - Goals, features, roadmap
- [Getting Started](00-Project/Getting-Started.md) - Build, run, develop
- [Architecture Overview](00-Project/Architecture-Overview.md) - System design summary
- [Roadmap](00-Project/Roadmap.md) - Future plans and milestones
- [Changelog](00-Project/Changelog.md) - Version history

### ?? **01-Architecture** - System Design
Technical architecture and design decisions.

- [Audio Pipeline](01-Architecture/Audio-Pipeline.md) - Signal flow and routing
- [DSP Chain](01-Architecture/DSP-Chain.md) - Processor architecture
- [Threading Model](01-Architecture/Threading-Model.md) - Concurrency design
- [Component Diagram](01-Architecture/Component-Diagram.md) - Class relationships

### ?? **02-Implementation** - Development Guides
How to develop and extend the system.

- [Coding Standards](02-Implementation/Coding-Standards.md) - VB.NET conventions
- [Adding DSP Processors](02-Implementation/Adding-Processors.md) - Tutorial
- [UI Controls Guide](02-Implementation/UI-Controls-Guide.md) - Creating UI components
- [Testing Guide](02-Implementation/Testing-Guide.md) - Test procedures

### ?? **03-Features** - Feature Documentation
Detailed feature specifications and usage.

- [Audio Playback](03-Features/Audio-Playback.md) - Playback system
- [Recording](03-Features/Recording.md) - Recording features
- [DSP Processing](03-Features/DSP-Processing.md) - DSP features
- [Visualization](03-Features/Visualization.md) - Spectrum, waveform, meters
- [Input Volume Control](03-Features/Input-Volume-Control.md) - Input gain control
- [Logging System](03-Features/Logging-System.md) - Async logging

### ?? **04-Tasks** - Active Work Tracking
Current development tasks and plans.

- [**Active Tasks**](04-Tasks/Active-Tasks.md) ? **CURRENT WORK**
- [Phase 3.1: DSP Signal Flow UI](04-Tasks/Phase-3.1-DSP-UI.md) - Current phase
- [Phase 3.2: Filters](04-Tasks/Phase-3.2-Filters.md) - Next: HPF/LPF
- [Completed Tasks](04-Tasks/Completed/) - Archived completed work

### ?? **05-Testing** - Test Documentation
Test plans, procedures, and results.

- [Test Plan](05-Testing/Test-Plan.md) - Overall testing strategy
- [Stereo Verification](05-Testing/Stereo-Verification.md) - Stereo processing test
- [Performance Tests](05-Testing/Performance-Tests.md) - Benchmarks
- [Test Results](05-Testing/Test-Results/) - Test output logs

### ?? **06-Issues** - Bug Tracking
Known issues, bugs, and resolutions.

- [Known Issues](06-Issues/Known-Issues.md) - Current bugs and workarounds
- [Technical Debt](06-Issues/Technical-Debt.md) - Code that needs refactoring
- [Resolved Issues](06-Issues/Resolved/) - Fixed bugs (archived)

### ?? **07-Reference** - Technical References
Technical specifications and reference material.

- [DSP Math & Algorithms](07-Reference/DSP-Math.md) - Filter equations
- [Audio Formats](07-Reference/Audio-Formats.md) - WAV, PCM specs
- [NAudio Reference](07-Reference/NAudio-Reference.md) - NAudio library usage
- [API Reference](07-Reference/API-Reference.md) - Public API documentation

---

## ?? **Current Status**

### **Phase 3.1: DSP Signal Flow UI**
**Progress:** 3/12 tasks complete (25%)  
**Status:** ? In Progress  
**Last Updated:** January 15, 2026

**Completed:**
- ? Task 3.1.1: Stereo processing verified
- ? Task 3.1.2: Pan control added to GainProcessor
- ? Task 3.1.3: DSP Signal Flow UI layout complete

**Next:**
- ? Task 3.1.3: Wire DSP UI events
- ? Task 3.2.1: Implement High-Pass Filter (Butterworth 2nd-order)
- ? Task 3.2.2: Implement Low-Pass Filter (1st-order)

See [Active Tasks](04-Tasks/Active-Tasks.md) for full details.

---

## ?? **By Document Type**

### ?? **Planning Documents**
- [Project Roadmap](00-Project/Roadmap.md)
- [Active Tasks](04-Tasks/Active-Tasks.md)
- Task Plans (in `04-Tasks/`)

### ?? **Feature Documentation**
- Feature Specifications (in `03-Features/`)
- User Guides
- Implementation Notes

### ??? **Architecture Documents**
- Architecture Overview (in `01-Architecture/`)
- Design Decisions
- Component Diagrams

### ?? **Test Documentation**
- Test Plans (in `05-Testing/`)
- Test Results
- Verification Reports

### ?? **Issue Tracking**
- Known Issues (in `06-Issues/`)
- Bug Reports
- Technical Debt Log

---

## ?? **Documentation Standards**

### **Creating New Documents:**
1. Choose the appropriate category folder
2. Use kebab-case file names: `my-document.md`
3. Include standard header (see template below)
4. Add link to this INDEX.md
5. Follow [Documentation Standards](02-Implementation/Coding-Standards.md#documentation)

### **Document Template:**
```markdown
# Document Title

**Date:** YYYY-MM-DD  
**Status:** [Planning/Active/Complete/Archived]  
**Author:** [Optional]

---

## Overview
Brief description of the document

## Contents
- Section 1
- Section 2

[Content here...]
```

---

## ?? **Finding Documents**

### **By Purpose:**
- **Starting new work?** ? [Active Tasks](04-Tasks/Active-Tasks.md)
- **Learning the system?** ? [Architecture](01-Architecture/)
- **Implementing a feature?** ? [Implementation Guides](02-Implementation/)
- **Fixing a bug?** ? [Known Issues](06-Issues/Known-Issues.md)
- **Writing tests?** ? [Testing Guide](02-Implementation/Testing-Guide.md)

### **By Phase:**
- **Phase 0:** Logging and diagnostics ? [Archive](Archive/)
- **Phase 1:** UI refactoring ? [Completed Tasks](04-Tasks/Completed/)
- **Phase 2.0:** MainForm refactoring ? [Completed Tasks](04-Tasks/Completed/)
- **Phase 3.1:** DSP Signal Flow UI ? [Active Tasks](04-Tasks/Active-Tasks.md)

---

## ??? **Maintenance**

### **Keeping Documentation Updated:**
- Update this INDEX.md when adding new documents
- Move completed tasks to `04-Tasks/Completed/`
- Archive obsolete docs to `Archive/`
- Update "Last Updated" dates
- Keep links working (use relative paths)

### **Documentation Workflow:**
1. Create document in appropriate folder
2. Add link to INDEX.md
3. Cross-reference related docs
4. Update status as work progresses
5. Archive when complete/obsolete

---

## ?? **Need Help?**

- **Can't find a document?** Use file search or ask!
- **Documentation outdated?** Update it or note in [Known Issues](06-Issues/Known-Issues.md)
- **Missing documentation?** Create it following standards above

---

**This index is the authoritative source for navigating project documentation.**  
**Keep it updated and organized!** ???
