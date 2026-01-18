# Logging Standards - DSP Processor
**Version:** 1.0.0  
**Date:** 2026-01-19  
**Status:** Official Standard  
**Strategy:** Strategy B (Selective Logging)

---

## ?? **PURPOSE**

Define consistent, searchable, cognitive-aware logging standards for the entire DSP Processor application.

**Goals:**
- ? Complete audit trail of user actions
- ? Debuggable state transitions
- ? Cognitive layer pattern detection
- ? Clean, noise-free production logs
- ? Unified format across all components

---

## ?? **THE 4-TIER LOGGING FRAMEWORK**

### **? TIER 1: STATE-CHANGING ACTIONS (Always Log - INFO)**

**What:** Actions that change system state or trigger workflows

**Format:**
```visualbasic
Logger.Info("[Component] Action: Details", "Component")
```

**Examples:**
```visualbasic
[UI] Play button clicked
[UI] Record button clicked
[UI] Stop button clicked
[UI] File selected: recording_001.wav
[UI] Routing changed: Input1 ? DSP ? Output1
```

**Log Level:** `INFO`

**Why:** Essential for debugging, state tracing, cognitive patterns

**Components:**
- Button clicks (Play, Record, Stop, Pause)
- File selection (Open, Save)
- Tab changes (Pipeline, DSP, FFT)
- Mode switches (Recording, Playback)
- Routing changes

---

### **? TIER 2: COMMITTED PARAMETER CHANGES (Log - INFO)**

**What:** Parameter changes when finalized (not during drag/edit)

**Format:**
```visualbasic
Logger.Info("[Component] Parameter adjusted: {newValue} (previous: {oldValue})", "Component")
```

**Examples:**
```visualbasic
[UI] Gain adjusted: -6.0dB (previous: 0.0dB)
[UI] FFT size changed: 2048 ? 4096
[UI] Sample rate selected: 48000 Hz
[UI] Effect applied: Compressor (threshold: -12dB, ratio: 4:1)
```

**When to Log:**
- **Slider released** (MouseUp event, not ValueChanged)
- **Dropdown selected** (SelectedIndexChanged, not hover)
- **Checkbox toggled** (CheckedChanged)
- **Settings dialog closed** (OK button, not every field change)
- **Effect applied** (Apply button, not every parameter adjustment)

**When NOT to Log:**
- During drag (50 events per second)
- During typing (every keystroke)
- During hover
- Preview values

**Log Level:** `INFO`

**Why:** Captures meaningful changes without noise

---

### **? TIER 3: TRANSIENT INTERACTIONS (Never Log)**

**What:** Transient UI interactions with no state change

**Examples (DO NOT LOG):**
```visualbasic
? Mouse moved over button
? Tab focus changed (keyboard navigation)
? Window resized
? Tooltip shown
? Slider dragging (ValueChanged event during drag)
? Hover effects
? TextBox_TextChanged during typing
? Preview rendering
```

**Why:** Creates noise, degrades log quality, impacts performance

---

### **?? TIER 4: COGNITIVE AGGREGATION (Pattern Level)**

**What:** Cognitive layer tracks patterns, not individual events

**Examples:**
```visualbasic
?? Habit detected: "User adjusts gain before recording" (15 occurrences)
?? Pattern: User toggles routing 3x per session (avg)
?? Anomaly: Gain adjusted 50x in 1 minute (unusual)
?? Workflow: "Record ? Playback ? Adjust Gain ? Re-record" (detected 4x)
```

**Why:** High-level insights without log pollution

**Implementation:** Cognitive layer automatically aggregates Tier 1 + Tier 2 logs

---

## ?? **SPECIAL CASE: TIMER LOGGING**

### **Timers are Implementation Details**

**Rule:** State transitions already tell the story - timers are internal mechanisms

### **Timer Lifecycle (DEBUG Level Only):**

```visualbasic
' Log timer start/stop at DEBUG level
Logger.Debug("[UI] Playback timer started (interval: 100ms)", "MainForm")
Logger.Debug("[UI] Playback timer stopped (duration: 5.3s)", "MainForm")
Logger.Debug("[UI] Recording timer started (interval: 100ms)", "MainForm")
Logger.Debug("[UI] Recording timer stopped (duration: 45.2s)", "MainForm")
```

**When:** Timer lifecycle events (start, stop, error)

**Why:** Useful for debugging timer issues, but not needed in production

### **Timer Ticks (NEVER LOG):**

```visualbasic
? Private Sub OnPlaybackTimerTick(sender As Object, e As EventArgs)
?     ' NO LOGGING HERE!
?     ' This fires 10x per second = 36,000 per hour
?     lblElapsedTime.Text = _elapsedTime.ToString("mm\:ss\.ff")
? End Sub
```

**Why:** 10 logs/second = 36,000/hour = log pollution

### **State Transitions (Already Logged):**

```visualbasic
? [GSM] GSM_T06_GSM_IDLE_TO_GSM_PLAYING: Idle ? Playing (User action)
? [GSM] GSM_T09_GSM_PLAYING_TO_GSM_IDLE: Playing ? Idle (EOF)
```

**Result:** State transitions tell us playback started/stopped - no need to log timer separately at INFO level

---

## ?? **LOGGING FORMAT STANDARDS**

### **Standard Format:**

```visualbasic
Logger.LogLevel("[Component] Action/Event: Details", "ComponentName")
```

### **Components:**

| Component | Prefix | Examples |
|-----------|--------|----------|
| MainForm | `[UI]` | `[UI] Play button clicked` |
| State Machines | `[GSM]`, `[REC]`, `[PLAY]`, etc. | `[GSM] GSM_T01_IDLE_TO_PLAYING` |
| AudioRouter | `[AudioRouter]` | `[AudioRouter] Playback started` |
| RecordingManager | `[RecordingManager]` | `[RecordingManager] Microphone armed` |
| DSPThread | `[DSPThread]` | `[DSPThread] Worker started` |
| TapPointManager | `[TapPoint]` | `[TapPoint] Reader registered` |
| Cognitive Layer | `??` | `?? Habit detected` |

### **State Transitions (Registry Pattern):**

```visualbasic
[Prefix] TransitionID: OldState ? NewState (Reason)
```

**Examples:**
```
[GSM] GSM_T01_GSM_IDLE_TO_GSM_PLAYING: Idle ? Playing (User action)
[PLAY] PLAY_T02_PLAY_IDLE_TO_PLAY_PLAYING: Idle ? Playing (Global: Playing)
[UI] UI_T03_UI_IDLE_TO_UI_PLAYING: IdleUI ? PlayingUI (Global: Playing)
```

---

## ?? **CONTROL-SPECIFIC LOGGING RULES**

### **Buttons:**

```visualbasic
' TIER 1: State-changing buttons
Private Sub btnPlay_Click(sender As Object, e As EventArgs)
    Logger.Info("[UI] Play button clicked", "MainForm")
    ' ... action ...
End Sub

Private Sub btnRecord_Click(sender As Object, e As EventArgs)
    Logger.Info("[UI] Record button clicked", "MainForm")
    ' ... action ...
End Sub

Private Sub btnStop_Click(sender As Object, e As EventArgs)
    Logger.Info("[UI] Stop button clicked (current state: {CurrentState})", "MainForm")
    ' ... action ...
End Sub
```

**Log:** Button click (BEFORE action)  
**Level:** INFO  
**Why:** Captures user intent

---

### **Sliders (Gain, Volume, etc.):**

```visualbasic
' TIER 2: Log on MouseUp (committed change)
Private _previousGainValue As Double = 0.0

Private Sub sldGain_MouseUp(sender As Object, e As MouseEventArgs)
    Dim newValue = sldGain.Value
    If newValue <> _previousGainValue Then
        Logger.Info($"[UI] Gain adjusted: {newValue:F1}dB (previous: {_previousGainValue:F1}dB)", "MainForm")
        _previousGainValue = newValue
    End If
End Sub

' TIER 3: Do NOT log ValueChanged during drag
? Private Sub sldGain_ValueChanged(sender As Object, e As EventArgs)
?     ' NO LOGGING - fires 50x per second during drag!
? End Sub
```

**Log:** Final value on MouseUp  
**Level:** INFO  
**Why:** Captures committed change without noise

---

### **Dropdowns (ComboBox):**

```visualbasic
' TIER 2: Log selection change
Private Sub cboSampleRate_SelectedIndexChanged(sender As Object, e As EventArgs)
    If _isInitializing Then Return ' Don't log during form load
    
    Dim selectedRate = CInt(cboSampleRate.SelectedItem)
    Logger.Info($"[UI] Sample rate selected: {selectedRate} Hz", "MainForm")
End Sub
```

**Log:** Selection change (after initialization)  
**Level:** INFO  
**Why:** Captures user choice

---

### **Checkboxes:**

```visualbasic
' TIER 2: Log state change
Private Sub chkEnableEffect_CheckedChanged(sender As Object, e As EventArgs)
    If _isInitializing Then Return
    
    Dim state = If(chkEnableEffect.Checked, "enabled", "disabled")
    Logger.Info($"[UI] Effect {state}: {chkEnableEffect.Text}", "MainForm")
End Sub
```

**Log:** Checked/Unchecked state  
**Level:** INFO  
**Why:** Captures toggle

---

### **TextBoxes (Filenames, Paths):**

```visualbasic
' TIER 2: Log committed value (on LostFocus or Enter)
Private Sub txtFilename_LostFocus(sender As Object, e As EventArgs)
    If _previousFilename <> txtFilename.Text Then
        Logger.Info($"[UI] Filename changed: {txtFilename.Text}", "MainForm")
        _previousFilename = txtFilename.Text
    End If
End Sub

' TIER 3: Do NOT log TextChanged
? Private Sub txtFilename_TextChanged(sender As Object, e As EventArgs)
?     ' NO LOGGING - fires on every keystroke!
? End Sub
```

**Log:** Final value (LostFocus or Enter)  
**Level:** INFO  
**Why:** Captures committed text without keystroke noise

---

### **Tab Controls:**

```visualbasic
' TIER 1: Log tab change (workflow change)
Private Sub tabMain_SelectedIndexChanged(sender As Object, e As EventArgs)
    Dim tabName = tabMain.SelectedTab.Text
    Logger.Info($"[UI] Tab changed: {tabName}", "MainForm")
End Sub
```

**Log:** Tab name  
**Level:** INFO  
**Why:** Tracks workflow context

---

### **File Dialogs:**

```visualbasic
' TIER 1: Log file selection
Private Sub btnOpen_Click(sender As Object, e As EventArgs)
    If openFileDialog.ShowDialog() = DialogResult.OK Then
        Logger.Info($"[UI] File selected: {Path.GetFileName(openFileDialog.FileName)}", "MainForm")
    Else
        Logger.Info("[UI] File selection canceled", "MainForm")
    End If
End Sub
```

**Log:** File selected or canceled  
**Level:** INFO  
**Why:** Captures user choice

---

### **Radio Buttons (Groups):**

```visualbasic
' TIER 2: Log option selection
Private Sub rbOption1_CheckedChanged(sender As Object, e As EventArgs)
    If rbOption1.Checked AndAlso Not _isInitializing Then
        Logger.Info($"[UI] Option selected: {rbOption1.Text}", "MainForm")
    End If
End Sub
```

**Log:** Selected option  
**Level:** INFO  
**Why:** Captures choice from group

---

### **NumericUpDown:**

```visualbasic
' TIER 2: Log on Leave (committed value)
Private _previousNumericValue As Decimal = 0

Private Sub numFFTSize_Leave(sender As Object, e As EventArgs)
    If numFFTSize.Value <> _previousNumericValue Then
        Logger.Info($"[UI] FFT size changed: {numFFTSize.Value} (previous: {_previousNumericValue})", "MainForm")
        _previousNumericValue = numFFTSize.Value
    End If
End Sub
```

**Log:** Final value on Leave  
**Level:** INFO  
**Why:** Captures committed numeric change

---

## ?? **COGNITIVE LAYER INTEGRATION**

### **What Cognitive Layer Tracks Automatically:**

From Tier 1 + Tier 2 logs, cognitive layer detects:

1. **Habits:**
   - "User adjusts gain before recording" (repeated pattern)
   - "User plays file 3x before saving" (verification habit)
   - "User changes routing before playback" (setup habit)

2. **Workflows:**
   - "Record ? Playback ? Adjust ? Re-record" (iterative workflow)
   - "Load file ? Adjust gain ? Export" (processing workflow)

3. **Anomalies:**
   - "Gain adjusted 50x in 1 minute" (unusual)
   - "User clicked Stop 10x rapidly" (panic behavior)
   - "Tab switching 30x per minute" (confusion indicator)

4. **Attention Patterns:**
   - "User spent 80% of time in DSP tab" (focus area)
   - "User switched between Pipeline ? DSP 15x" (back-and-forth pattern)

### **Cognitive Export Example:**

```
Session Summary:
Duration: 15 minutes
Actions: 42 total (12 state changes, 18 parameter adjustments, 12 tab switches)

Habits Detected:
1. "Gain adjustment before recording" (5 occurrences)
2. "Tab switch: DSP ? Pipeline" (8 occurrences)

Workflows:
1. "Record ? Playback ? Adjust Gain ? Re-record" (3 complete cycles)

Anomalies:
None detected - normal session
```

---

## ?? **IMPLEMENTATION CHECKLIST**

### **For Each Control:**

- [ ] Identify tier (1, 2, 3, or 4)
- [ ] Determine appropriate event (Click, MouseUp, LostFocus, etc.)
- [ ] Add logging in correct format
- [ ] Use correct log level (INFO or DEBUG)
- [ ] Test that logs appear
- [ ] Verify cognitive layer captures it

### **Testing:**

1. Run application
2. Interact with control
3. Check logs for correct format
4. Check cognitive export includes pattern
5. Verify no noise (Tier 3) logged

---

## ?? **LOGGING LEVELS SUMMARY**

| Level | Purpose | When to Use | Example |
|-------|---------|-------------|---------|
| **DEBUG** | Implementation details | Timer lifecycle, internal state | `[DEBUG] Timer started` |
| **INFO** | User actions, state changes | Button clicks, parameters | `[INFO] Play clicked` |
| **WARNING** | Invalid transitions, recoverable errors | State machine rejections | `[WARNING] Invalid transition` |
| **ERROR** | Exceptions, failures | Crashes, file errors | `[ERROR] Failed to load` |

**Production:** INFO + WARNING + ERROR  
**Debug:** DEBUG + INFO + WARNING + ERROR

---

## ? **ACCEPTANCE CRITERIA**

**Logs are correct when:**
- [x] All Tier 1 actions logged (state changes)
- [x] All Tier 2 parameters logged (committed values)
- [x] No Tier 3 noise (transient interactions)
- [x] Format consistent (`[Component] Action: Details`)
- [x] Searchable (grep-friendly)
- [x] Cognitive layer detects patterns
- [x] Debug level used appropriately (timers)

---

## ?? **MAINTENANCE**

**When adding new controls:**
1. Determine tier (1, 2, 3, or 4)
2. Follow format standards
3. Add to this document
4. Update cognitive layer if needed

**When refactoring:**
- Keep logging format consistent
- Don't break grep searches
- Update documentation

---

## ?? **REFERENCES**

- `StateRegistry.yaml` - State machine TransitionIDs
- `State-Evolution-Log.md` - Why states exist
- `Cognitive-Patterns-Architecture.md` - Pattern detection
- `Next-Session-Task-List.md` - Implementation plan

---

**Status:** ? **Official Standard - Apply to All Controls**  
**Next:** Systematic control audit and implementation

---

**Created:** 2026-01-19  
**Version:** 1.0.0  
**Strategy:** Strategy B (Selective Logging)
