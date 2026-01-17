# Known Issues - DSP_Processor

**Last Updated:** January 15, 2026  
**Status:** Active Tracking  

---

## ?? **ACTIVE ISSUES**

### **Currently No Active Critical Issues! ?**

All major bugs from Phase 2.0 refactoring have been resolved.

---

## ? **RECENTLY RESOLVED**

### **Issue #001: File Locking During Playback** ?
**Date Resolved:** January 14, 2026  
**Severity:** High  
**Status:** FIXED

**Problem:**
- Files remained locked after playback stopped
- Couldn't delete recordings
- "File in use" errors

**Root Cause:**
- `AudioFileReader` not properly disposed
- File handles leaked

**Solution:**
- Added proper disposal in `PlaybackManager.Cleanup()`
- Implemented `Using` statements
- Fixed resource management

**Files Changed:**
- `Audio\Managers\PlaybackManager.vb`

---

### **Issue #002: Recording Clicks/Pops** ?
**Date Resolved:** January 15, 2026  
**Severity:** High  
**Status:** FIXED

**Problem:**
- Audible clicks/pops during recording
- Irregular audio glitches
- Buffer underruns

**Root Cause:**
- Synchronous logging blocked audio thread
- Logging I/O caused timing issues

**Solution:**
- Implemented async logging system
- Background thread for log writing
- Eliminated I/O on audio thread

**Files Changed:**
- `Utils\LoggingManager.vb`
- All logging callsites

---

### **Issue #003: WASAPI Buffer Overflow** ?
**Date Resolved:** January 15, 2026  
**Severity:** Medium  
**Status:** FIXED

**Problem:**
- WASAPI buffer overflow warnings
- Audio dropouts in exclusive mode

**Root Cause:**
- Incorrect buffer size calculations
- Not accounting for sample format

**Solution:**
- Fixed buffer size calculations
- Proper format handling
- Better error handling

**Files Changed:**
- `AudioIO\WaveInEngine.vb`

---

### **Issue #004: FFT Settings Not Persisting** ?
**Date Resolved:** January 14, 2026  
**Severity:** Low  
**Status:** FIXED

**Problem:**
- FFT settings reset on restart
- User preferences not saved

**Solution:**
- Added FFT settings to config
- Proper save/load logic
- Settings persistence

---

## ?? **KNOWN LIMITATIONS**

### **Limitation #1: Windows Only**
**Impact:** Platform restriction  
**Status:** By Design

This application uses WASAPI which is Windows-only.

**Workaround:** None (Windows required)

---

### **Limitation #2: 16-bit Processing**
**Impact:** Audio quality  
**Status:** By Design (for now)

Internal DSP currently uses 16-bit PCM.

**Workaround:** None currently  
**Future:** May add 32-bit float processing

---

### **Limitation #3: Single Input Source**
**Impact:** Feature limitation  
**Status:** Not Implemented

Can only record from one input at a time.

**Workaround:** None  
**Future:** May add multi-input support

---

## ?? **UNDER INVESTIGATION**

Currently no issues under investigation! ??

---

## ?? **REPORTING NEW ISSUES**

### **Before Reporting:**
1. Check this document for known issues
2. Check [Resolved Issues](Resolved/) for solutions
3. Try latest version
4. Review logs (`Logs` tab in app)

### **How to Report:**
1. **Collect Information:**
   - Steps to reproduce
   - Expected vs actual behavior
   - Error messages/logs
   - System information
2. **Create Issue Document:**
   - Use template: `Bug-Report-YYYY-MM-DD-Title.md`
   - Place in `Documentation\06-Issues\`
3. **Include:**
   - Date and time
   - DSP_Processor version
   - Windows version
   - Audio device information
   - Log excerpts (from Logs tab)

### **Bug Report Template:**
```markdown
# Bug Report: [Title]

**Date:** YYYY-MM-DD  
**Severity:** [Low/Medium/High/Critical]  
**Status:** Under Investigation

## Description
Brief description of the issue

## Steps to Reproduce
1. Step 1
2. Step 2
3. ...

## Expected Behavior
What should happen

## Actual Behavior
What actually happens

## Environment
- DSP_Processor Version: [version]
- Windows Version: [version]
- Audio Device: [device name]

## Logs
[Paste relevant log excerpts]

## Additional Notes
[Any other relevant information]
```

---

## ?? **TECHNICAL DEBT**

### **None Currently!** ?

Phase 2.0 refactoring cleaned up major technical debt:
- ? MainForm refactored (150+ lines removed)
- ? Audio routing cleaned up
- ? Managers extracted
- ? Code duplication eliminated

---

## ?? **ISSUE STATISTICS**

### **Total Issues Tracked:** 4  
### **Resolved:** 4 (100%)  
### **Active:** 0 (0%)  
### **Under Investigation:** 0

### **By Severity:**
- Critical: 0
- High: 0
- Medium: 0
- Low: 0

### **Average Time to Resolve:**
- High Severity: ~1-2 hours
- Medium Severity: ~30-60 minutes
- Low Severity: ~15-30 minutes

---

## ?? **QUALITY METRICS**

### **Current State:**
- ? No known crashes
- ? No audio glitches
- ? No memory leaks
- ? No file locking issues
- ? Stable recording
- ? Stable playback

### **Since Phase 2.0:**
- 4 bugs fixed
- 0 regressions
- 150+ lines of code removed
- Architecture simplified

---

## ?? **RELATED DOCUMENTATION**

- [Resolved Issues](Resolved/) - Archive of fixed bugs
- [Technical Debt](Technical-Debt.md) - Code that needs refactoring
- [Active Tasks](../04-Tasks/Active-Tasks.md) - Current development work
- [Architecture](../01-Architecture/) - System design

---

**Last Updated:** January 15, 2026  
**Next Review:** As issues arise  
**Maintainer:** Project team

**Have an issue? Report it following the template above!**
