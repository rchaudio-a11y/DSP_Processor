# DSP_Processor Documentation

**Project:** Real-time Digital Signal Processing Engine  
**Methodology:** [Rick Development Framework (RDF)](Reference/RDF.md)  
**Repository:** https://github.com/rchaudio-a11y/DSP_Processor

---

## ??? Quick Navigation

### ?? Start Here: Templates
Creating new documentation? Start with a template:
- [Architecture Template v1.0.0](Templates/Architecture-v1_0_0.md) - System design documents
- [Task Template v1.0.0](Templates/Task-v1_0_0.md) - Implementation tasks
- [Issue Template v1.0.0](Templates/Issue-v1_0_0.md) - Bug reports
- [ChangeLog Template v1.0.0](Templates/ChangeLog-v1_0_0.md) - Version history
- [Session Notes Template v1.0.0](Templates/Session-Notes-v1_0_0.md) - Daily work logs

**Template Version:** v1.0.0  
**Naming Convention:** `[Type]-v[Major]_[Minor]_[Patch].md`

### ??? Architecture
System design and architectural decisions:
- **[Complete DSP Pipeline Architecture](Architecture/Complete-DSP-Pipeline-Architecture.md)** - Main architecture document
- [Multi-Tap Routing Decision](Architecture/Multi-Tap-Routing-Architecture-Decision.md) - Multi-reader ring buffer design
- [Phase 2.5: Output Gain](Architecture/Phase2-5-Output-Gain-Implementation.md) - Dual gain stage implementation
- [All Architecture Docs ?](Architecture/)

### ?? Active Work
What's being worked on RIGHT NOW:
- **[Active Tasks](Active/Tasks.md)** - Current implementation tasks
- **[Active Issues](Active/Issues.md)** - Current bugs/problems
- [Session Notes](Active/Sessions/) - Daily work logs (dated files)

### ? Completed Work
Finished features and resolved issues:
- [Completed Features](Completed/Features/) - Feature completion documentation
- [Completed Tasks](Completed/Tasks/) - Archived task documentation
- [Resolved Issues](Completed/Issues/) - Bug resolution reports

### ?? Reference
Timeless documentation and methodology:
- **[RDF Methodology](Reference/RDF.md)** - Rick Development Framework specification
- [RDF vs Agile Comparison](Reference/RDF-vs-Agile-Comparison.md) - Methodology comparison

### ?? Changelog
Version history and evolution:
- **[Current Changelog](Changelog/CURRENT.md)** - Active version log
- [Archived Changelogs](Changelog/Archive/) - Historical versions

---

## ?? Workflow (RDF Loop)

Following the [Rick Development Framework](Reference/RDF.md):

1. **Phase 1: Curiosity** ? Create session notes
2. **Phase 2: Insight** ? Use Architecture Template
3. **Phase 3: Build** ? Add to Active/Tasks.md
4. **Phase 4: Debug** ? Document in Active/Issues.md
5. **Phase 5: Validate** ? Test and verify
6. **Phase 6: Synthesize** ? Move to Completed/, update Changelog
7. **Meta: Recurse** ? Repeat with deeper understanding

---

## ?? Folder Structure

```
Documentation/
??? Templates/       ?? All document templates
??? Architecture/    ??? System design
??? Active/          ?? Current work
??? Completed/       ? Finished work
??? Reference/       ?? Timeless docs
??? Changelog/       ?? Version history
```

---

**Last Updated:** January 16, 2026  
**Documentation Version:** 3.0 (Hybrid)
