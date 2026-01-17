# Documentation Migration Checklist v1.0.0

**Migration Target:** [Folder/Files Being Migrated]  
**Migration Phase:** [1, 2, 3, etc.]  
**Date Started:** [YYYY-MM-DD]  
**Date Completed:** [YYYY-MM-DD]  
**Version:** v[Major].[Minor].[Patch]

---

## ?? Migration Scope

**Source Location:** `[Path to old location]`  
**Destination:** `[Path to new location]`  
**File Count:** [Number of files]  
**Documentation Type:** [Architecture | Tasks | Issues | Reference | Other]

---

## ?? Pre-Migration Checklist

- [ ] Audit all files in source location
- [ ] Identify file types and destinations
- [ ] Check for cross-references that will break
- [ ] Backup current state (git commit)
- [ ] Review with templates to determine proper categorization

---

## ?? Migration Manifest

### Files to Move

| Source File | Destination | Type | Action | Status |
|------------|-------------|------|--------|--------|
| [filename.md] | [new/path/filename.md] | [Type] | [Move\|Rename\|Merge] | [ ] |
| [filename.md] | [new/path/filename.md] | [Type] | [Move\|Rename\|Merge] | [ ] |
| [filename.md] | [new/path/filename.md] | [Type] | [Move\|Rename\|Merge] | [ ] |

### Files to Archive

| Source File | Reason | Action | Status |
|------------|--------|--------|--------|
| [filename.md] | [Why archiving] | [Move to Archive\|Delete] | [ ] |

### Files to Update

| File | Update Needed | Status |
|------|---------------|--------|
| [filename.md] | [Fix links, update references] | [ ] |

---

## ?? Migration Steps

### Phase 1: Preparation
- [ ] Create destination folders if needed
- [ ] Document current cross-references
- [ ] Create redirect/mapping document

### Phase 2: Move Files
- [ ] Move files according to manifest
- [ ] Rename files to match conventions
- [ ] Update file headers with new paths

### Phase 3: Update References
- [ ] Update cross-references in moved files
- [ ] Update index files (Active/Tasks.md, etc.)
- [ ] Update README.md if needed

### Phase 4: Verification
- [ ] Verify all links work
- [ ] Check for orphaned files
- [ ] Build/test if applicable

### Phase 5: Cleanup
- [ ] Remove empty folders
- [ ] Archive old versions if needed
- [ ] Update .gitignore if needed

---

## ?? Cross-Reference Updates

### References TO Migrated Files
[List files that link TO the files being migrated - these need updating]

| Referencing File | Old Link | New Link | Status |
|-----------------|----------|----------|--------|
| [file.md] | [old/path] | [new/path] | [ ] |

### References FROM Migrated Files
[List files that the migrated files link to - may need updating]

| Migrated File | Referenced File | Link Status | Status |
|--------------|----------------|-------------|--------|
| [file.md] | [other.md] | [OK\|Needs Update] | [ ] |

---

## ?? Migration Notes

### Discoveries
- [Any issues found during migration]
- [Files that don't fit new structure]
- [Decisions made about ambiguous files]

### Deferred Items
- [ ] [Item deferred to future migration]
- [ ] [Item deferred to future migration]

---

## ? Post-Migration Checklist

- [ ] All files moved to correct locations
- [ ] All cross-references updated
- [ ] Index files updated (Active/Tasks.md, Active/Issues.md, README.md)
- [ ] Empty folders removed
- [ ] Git commit with descriptive message
- [ ] Update Changelog/CURRENT.md
- [ ] Create session notes for this migration
- [ ] Update this checklist's status to COMPLETE

---

## ?? Migration Metrics

**Total Files:** [X]  
**Files Moved:** [X]  
**Files Archived:** [X]  
**Files Updated:** [X]  
**Broken Links Fixed:** [X]  
**Time Spent:** [X hours]

---

## ?? RDF Phase: Phase 6 (Synthesis)

> *"Turn the working system into a polished, expressive tool"* — [RDF.md](../Reference/RDF.md)

**Meta-Insight:**
[What was learned about documentation structure during this migration]

**Next Migration:**
[Which folder/files should be migrated next and why]

---

**Status:** [Planning | In Progress | Complete]  
**Version:** v1.0.0  
**Template:** Migration-Checklist-v1_0_0.md
