# Documentation Instructions

**Reference:** Main [copilot-instructions.md](../copilot-instructions.md)

---

## ?? Template Usage

### Template Naming Convention
`[Type]-v[Major]_[Minor]_[Patch].md`

**Example:** `Task-v1_0_0.md`

### Current Template Version
**v1.0.0**

### Available Templates
- `Architecture-v1_0_0.md` - System design
- `Task-v1_0_0.md` - Implementation tasks
- `Issue-v1_0_0.md` - Bug reports
- `ChangeLog-v1_0_0.md` - Version history
- `Session-Notes-v1_0_0.md` - Daily work logs
- `Migration-Checklist-v1_0_0.md` - Folder migrations

**Location:** `Documentation/Templates/`

---

## ?? Folder Structure

```
Documentation/
??? Templates/      # Versioned document templates
??? Architecture/   # System design (timeless)
??? Active/         # Current work
?   ??? Tasks.md    # Single-file task list
?   ??? Issues.md   # Single-file issue list
?   ??? Sessions/   # Session notes (dated: YYYY-MM-DD-Topic.md)
??? Completed/      # Archived work
?   ??? Features/   # Feature completion docs
?   ??? Tasks/      # Archived tasks
?   ??? Issues/     # Resolved issues
??? Reference/      # Timeless guides (RDF.md, standards)
??? Changelog/      # Version history
?   ??? CURRENT.md  # Active changelog
?   ??? Archive/    # Old versions
??? Archive/        # Historical preservation
```

---

## ?? Documentation Workflow

### Creating New Documentation

1. **Choose Template**
   ```bash
   cp Documentation/Templates/[Type]-v1_0_0.md [destination]
   ```

2. **Fill Sections**
   - Complete ALL sections
   - Include RDF phase annotations
   - Add cross-references

3. **Save to Proper Folder**
   - Active work ? `Active/`
   - Architecture ? `Architecture/`
   - Reference ? `Reference/`

4. **Update Index Files**
   - Add to `Active/Tasks.md` if task
   - Add to `Active/Issues.md` if issue
   - Link from relevant docs

### Completing Work

1. **Move to Completed/**
   - Tasks ? `Completed/Tasks/`
   - Issues ? `Completed/Issues/`

2. **Create Completion Doc**
   - Use template to create feature doc
   - Save to `Completed/Features/`

3. **Update Changelog**
   - Add entry to `Changelog/CURRENT.md`
   - Include version, date, summary

---

## ? Post-Plan Documentation (REQUIRED)

After EVERY plan phase:

1. **Create Session Notes**
   ```bash
   cp Templates/Session-Notes-v1_0_0.md Active/Sessions/YYYY-MM-DD-Topic.md
   ```

2. **Update Changelog**
   - Add to `CURRENT.md`
   - Increment version if needed

3. **Maintain Version Tracking**
   - Document template versions used
   - Track all doc changes

---

## ?? Single-File Lists (Preferred)

### Active/Tasks.md Format
```markdown
## ?? In Progress
- [ ] Task name (50%) ? [Docs](link)

## ?? Backlog
- [ ] Task name

## ? Recently Completed
- [x] Task name ? [Docs](../Completed/Features/name.md)
```

### Active/Issues.md Format
```markdown
## ?? Critical
- Issue description ? [Details](link)

## ? Recently Resolved
- [x] Issue ? [Resolution](../Completed/Issues/name.md)
```

**Why single-file:** Faster scanning, built-in prioritization

---

## ?? RDF Phase Annotations

Always include in documentation:

- **Phase 1 (Curiosity):** Problem statement
- **Phase 2 (Insight):** Architecture decisions
- **Phase 3 (Build):** Implementation notes
- **Phase 4 (Debug):** Bug discoveries, lessons learned
- **Phase 5 (Validate):** Test results
- **Phase 6 (Synthesis):** Completion docs, polish
- **Meta-Phase:** Recursion notes, next loop planning

---

## ?? Session Notes Best Practices

- **Timing:** Create DURING work, not after
- **Frequency:** One per work session
- **Naming:** `YYYY-MM-DD-Topic.md`
- **Content:** Goals, work done, discoveries, next steps
- **RDF Reflection:** Always include meta-reflection section

---

**Last Updated:** 2026-01-16  
**Version:** 1.0.0
