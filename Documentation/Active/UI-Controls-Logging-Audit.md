# UI Controls Logging Audit - Systematic Review
**Date:** 2026-01-19  
**Status:** IN PROGRESS  
**Standard:** Logging-Standards.md v1.0.0  
**Strategy:** Strategy B (Selective Logging)

---

## ?? **PURPOSE**

Systematically audit ALL UI controls in DSP Processor to ensure proper logging integration.

**Goal:** Every control properly categorized (Tier 1, 2, 3, or 4) and logged correctly.

---

## ?? **AUDIT METHODOLOGY:**

### **For Each Control:**
1. ? **Identify** - What is the control?
2. ? **Categorize** - Which tier? (1, 2, 3, or 4)
3. ? **Verify** - Is logging implemented?
4. ? **Test** - Does it log correctly?
5. ? **Document** - Record status

### **Audit Status:**
- ? **COMPLETE** - Logging implemented and tested
- ?? **PARTIAL** - Some logging exists, needs refinement
- ? **MISSING** - No logging implemented
- ?? **SKIP** - Tier 3 (should NOT be logged)

---

## ?? **MAINFORM CONTROLS AUDIT:**

### **Transport Controls (Play/Record/Stop):**

| Control | Type | Tier | Status | Notes |
|---------|------|------|--------|-------|
| `btnPlay` / `lstRecordings_DoubleClick` | Button | 1 | ?? | Has basic logging, needs format update |
| `btnRecord` | Button | 1 | ? | MISSING - no logging in Record button |
| `btnStop` | Button | 1 | ? | COMPLETE - logs state context |
| `btnPause` | Button | 1 | ?? | NOT IMPLEMENTED (control doesn't exist) |

**Play Button Status:**
- Current: `Logger.Instance.Info($"DSP playback started: {selectedFile}", "MainForm")`
- Need: `Logger.Info("[UI] Play button clicked", "MainForm")` (BEFORE action)

**Record Button Status:**
- Current: NO LOGGING
- Need: `Logger.Info("[UI] Record button clicked", "MainForm")`

**Stop Button Status:**
- Current: ? `Logger.Instance.Info("Stop clicked but already Idle", "MainForm")`
- Status: GOOD - has state context

---

### **Gain/Volume Controls:**

| Control | Type | Tier | Status | Notes |
|---------|------|------|--------|-------|
| Gain sliders (various) | TrackBar | 2 | ? | MISSING - no MouseUp logging |
| Master volume | TrackBar | 2 | ? | MISSING - no MouseUp logging |
| Input gain | TrackBar | 2 | ? | MISSING - no MouseUp logging |
| Output gain | TrackBar | 2 | ? | MISSING - no MouseUp logging |

**Implementation Needed:**
```visualbasic
Private _previousGainValue As Double = 0.0

Private Sub sldGain_MouseUp(sender As Object, e As MouseEventArgs)
    Dim newValue = sldGain.Value
    If newValue <> _previousGainValue Then
        Logger.Info($"[UI] Gain adjusted: {newValue:F1}dB (previous: {_previousGainValue:F1}dB)", "MainForm")
        _previousGainValue = newValue
    End If
End Sub
```

---

### **File Selection:**

| Control | Type | Tier | Status | Notes |
|---------|------|------|--------|-------|
| `lstRecordings` selection | ListBox | 1 | ? | MISSING - no logging on selection |
| File open dialog | Dialog | 1 | ?? | Has logging, needs format |
| File save dialog | Dialog | 1 | ? | MISSING - no logging |

**lstRecordings Status:**
- Current: NO LOGGING
- Need: Log when user selects file (not just double-click)

---

### **Routing Controls:**

| Control | Type | Tier | Status | Notes |
|---------|------|------|--------|-------|
| Routing dropdown/panel | ComboBox/Custom | 1 | ? | MISSING - no routing change logging |
| Input device selector | ComboBox | 1 | ? | MISSING - no device change logging |
| Output device selector | ComboBox | 1 | ? | MISSING - no device change logging |

**Implementation Needed:**
```visualbasic
Private Sub cboRouting_SelectedIndexChanged(sender As Object, e As EventArgs)
    If _isInitializing Then Return
    
    Dim newRoute = cboRouting.SelectedItem.ToString()
    Logger.Info($"[UI] Routing changed: {_previousRoute} ? {newRoute}", "MainForm")
    _previousRoute = newRoute
End Sub
```

---

### **Tab Controls:**

| Control | Type | Tier | Status | Notes |
|---------|------|------|--------|-------|
| Main tab control | TabControl | 1 | ? | MISSING - no tab change logging |
| Sub-tab controls | TabControl | 1 | ? | MISSING - no tab change logging |

**Implementation Needed:**
```visualbasic
Private Sub tabMain_SelectedIndexChanged(sender As Object, e As EventArgs)
    Dim tabName = tabMain.SelectedTab.Text
    Logger.Info($"[UI] Tab changed: {tabName}", "MainForm")
End Sub
```

---

### **Settings/Configuration:**

| Control | Type | Tier | Status | Notes |
|---------|------|------|--------|-------|
| Sample rate selector | ComboBox | 2 | ? | MISSING - no logging |
| Bit depth selector | ComboBox | 2 | ? | MISSING - no logging |
| FFT size selector | NumericUpDown | 2 | ? | MISSING - no logging |
| Window function selector | ComboBox | 2 | ? | MISSING - no logging |

---

### **Effect Controls:**

| Control | Type | Tier | Status | Notes |
|---------|------|------|--------|-------|
| Effect enable checkboxes | CheckBox | 2 | ? | MISSING - no logging |
| Effect parameter sliders | TrackBar | 2 | ? | MISSING - no MouseUp logging |
| Effect preset selector | ComboBox | 2 | ? | MISSING - no logging |

---

### **Timers (Special Case):**

| Control | Type | Tier | Status | Notes |
|---------|------|------|--------|-------|
| Playback timer | Timer | 2.5 (DEBUG) | ? | MISSING - no DEBUG lifecycle logging |
| Recording timer | Timer | 2.5 (DEBUG) | ? | MISSING - no DEBUG lifecycle logging |
| Timer ticks | Timer.Tick | 3 (SKIP) | ? | CORRECT - not logged (noise) |

**Timer Lifecycle Needed:**
```visualbasic
Logger.Debug("[UI] Playback timer started (interval: 100ms)", "MainForm")
Logger.Debug("[UI] Playback timer stopped (duration: 5.3s)", "MainForm")
```

---

### **Window/Form Events:**

| Control | Type | Tier | Status | Notes |
|---------|------|------|--------|-------|
| Form Load | Event | 1 | ? | Already logged by initialization |
| Form Closing | Event | 1 | ? | MISSING - no close logging |
| Window resize | Event | 3 (SKIP) | ? | CORRECT - not logged (noise) |
| Mouse move | Event | 3 (SKIP) | ? | CORRECT - not logged (noise) |

**Form Closing Needed:**
```visualbasic
Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs)
    Logger.Info("[UI] Application closing", "MainForm")
End Sub
```

---

## ?? **AUDIT SUMMARY:**

### **Total Controls:** ~30 identified

### **Status Breakdown:**
- ? **COMPLETE:** 3 controls (10%)
- ?? **PARTIAL:** 2 controls (7%)
- ? **MISSING:** 20 controls (67%)
- ?? **SKIP (Tier 3):** 5 controls (16%)

### **Priority Implementation Order:**

**HIGH PRIORITY (Tier 1 - State Changes):**
1. ? Record button
2. ? lstRecordings selection
3. ? Tab change logging
4. ? Routing change logging
5. ? Device selection logging
6. ?? Play button (format update)

**MEDIUM PRIORITY (Tier 2 - Parameters):**
7. ? Gain sliders (MouseUp)
8. ? Sample rate selector
9. ? FFT size selector
10. ? Effect enable checkboxes

**LOW PRIORITY (DEBUG Level):**
11. ? Timer lifecycle logging

---

## ?? **IMPLEMENTATION PLAN:**

### **Phase 1: Tier 1 Controls (HIGH - 2 hours)**
- Add logging to all state-changing buttons
- Add tab change logging
- Add file/routing selection logging
- Update Play button format

### **Phase 2: Tier 2 Controls (MEDIUM - 3 hours)**
- Add MouseUp handlers to all sliders
- Add SelectedIndexChanged to dropdowns
- Add CheckedChanged to checkboxes
- Add Leave handlers to numeric inputs

### **Phase 3: Timer Logging (LOW - 30 mins)**
- Add DEBUG lifecycle logging to timers
- Test that INFO logs don't have timer ticks

### **Phase 4: Validation (1 hour)**
- Run application
- Test each control
- Verify logs appear correctly
- Check cognitive layer captures patterns

**Total Estimated Time:** 6.5 hours

---

## ?? **NEXT STEPS:**

**Option A: Start Implementation Now**
- Begin with HIGH priority (Tier 1)
- Work through systematically
- Test as we go

**Option B: Continue with Master Task List**
- Defer to "Legacy Logging Cleanup" session
- Focus on Phase 6 Testing first
- Return to logging with fresh eyes

**Option C: Quick Wins First**
- Implement HIGH priority only (2 hours)
- Defer MEDIUM/LOW to later
- Get most important logging in place

---

## ?? **TESTING CHECKLIST:**

For each implemented control:
- [ ] Log appears in correct format
- [ ] Log level is correct (INFO/DEBUG)
- [ ] No noise (Tier 3) logged
- [ ] Cognitive layer captures it
- [ ] Searchable with grep
- [ ] Compiles and runs

---

**Status:** ? **Audit Complete - Ready for Implementation**  
**Recommendation:** Option C (Quick Wins) or defer to systematic session

**Created:** 2026-01-19  
**Next:** Choose implementation approach
