# UI/Forms Instructions

**Reference:** Main [copilot-instructions.md](../copilot-instructions.md)

---

## ?? Event Wiring Patterns

### WithEvents vs AddHandler

**RULE:** Use the correct pattern for each scenario

#### WithEvents (Designer Controls)
```vb
' In Designer.vb:
Friend WithEvents btnPlay As Button

' In code file:
Private Sub btnPlay_Click(sender As Object, e As EventArgs) Handles btnPlay.Click
    ' Event handler
End Sub
```

**Use when:**
- Control created in Visual Studio Designer
- Control declared in Designer.vb file
- Using `Handles` keyword

#### AddHandler (Programmatic Controls)
```vb
' Control created in code:
Private btnDynamic As New Button()

' Wire event:
AddHandler btnDynamic.Click, AddressOf btnDynamic_Click

' Handler (NO Handles keyword):
Private Sub btnDynamic_Click(sender As Object, e As EventArgs)
    ' Event handler
End Sub
```

**Use when:**
- Control created programmatically (New Button(), etc.)
- Control NOT in Designer.vb
- Cannot use `Handles` keyword

### ?? Common Mistake
```vb
' ? WRONG - Mixing patterns causes silent failure:
Private Sub OnClick(sender As Object, e As EventArgs) Handles myButton.Click
    ' This won't fire if myButton isn't WithEvents!
End Sub

' ? CORRECT - Use AddHandler instead:
AddHandler myButton.Click, AddressOf OnClick
Private Sub OnClick(sender As Object, e As EventArgs)
    ' This works!
End Sub
```

---

## ??? User Interaction Patterns

### Double-Click Reset (TrackBar Controls)

**RULE:** All sliders support double-click to reset to default

```vb
' Wire event:
AddHandler trackVolume.DoubleClick, AddressOf trackVolume_DoubleClick

' Handler:
Private Sub trackVolume_DoubleClick(sender As Object, e As EventArgs)
    trackVolume.Value = 100  ' Default for volume/gain
    Logger.Instance.Info("Volume reset to default (100%)", "ControlName")
End Sub
```

**Default Values:**
- **Volume/Gain sliders:** 100% (unity gain)
- **Pan sliders:** 50% or 0 (center)
- **Frequency sliders:** Sensible defaults (e.g., 80 Hz for high-pass)
- **Width sliders:** 100% (normal stereo)

**User Experience:**
- User double-clicks track background (not thumb)
- Immediate visual feedback (value updates)
- Log the reset action

---

## ?? Event-Driven Updates (REQUIRED)

### Timer-Driven ? (AVOID)
```vb
' ? BAD - Polling wastes CPU:
Private Sub Timer_Tick(sender As Object, e As EventArgs)
    UpdateMeters()  ' Called every 16ms even if no data!
End Sub
```

### Event-Driven ? (PREFERRED)
```vb
' ? GOOD - React to actual data:
Private Sub OnAudioDataAvailable(sender As Object, e As AudioBufferEventArgs)
    UpdateMeters(e.Buffer, e.BitsPerSample, e.Channels)
End Sub
```

**When to Use Events:**
- Audio data updates
- State changes
- Meter updates
- FFT data available
- User interactions

**When Timers Are OK:**
- UI animations (smooth progress bars)
- Timeout detection
- Periodic cleanup (not time-critical)

---

## ?? Control Naming Conventions

### Standard Prefixes
- `btn` - Button (btnPlay, btnStop)
- `lbl` - Label (lblStatus, lblVolume)
- `txt` - TextBox (txtFilename)
- `cmb` - ComboBox (cmbSampleRate)
- `chk` - CheckBox (chkEnableDSP)
- `track` - TrackBar (trackVolume, trackGain)
- `panel` - Panel (panelLED, panelMeters)
- `grp` - GroupBox (grpProcessing)
- `lst` - ListBox (lstRecordings)
- `meter` - Custom meter control (meterRecording)

### Descriptive Names
```vb
' ? GOOD:
Private btnStartRecording As Button
Private trackInputGain As TrackBar
Private meterInputLeft As VolumeMeterControl

' ? BAD:
Private Button1 As Button
Private TrackBar2 As TrackBar
Private Control3 As VolumeMeterControl
```

---

## ??? Control State Management

### Use State Machine Pattern

```vb
' Centralized state:
Public Enum AppState
    Idle
    Armed
    Recording
    Playing
End Enum

Private currentState As AppState = AppState.Idle

' State-driven UI updates:
Private Sub UpdateUIForState(newState As AppState)
    Select Case newState
        Case AppState.Armed
            panelLED.BackColor = Color.Yellow
            btnRecord.Enabled = True
            btnPlay.Enabled = True
            
        Case AppState.Recording
            panelLED.BackColor = Color.Red
            btnRecord.Enabled = False
            btnPlay.Enabled = False
    End Select
    
    currentState = newState
End Sub
```

**Benefits:**
- Single source of truth
- No inconsistent UI states
- Easy to reason about
- Testable

---

## ?? Thread-Safe UI Updates

### Always Use Invoke for Cross-Thread

```vb
Public Sub UpdateMeter(levelDb As Single)
    If InvokeRequired Then
        BeginInvoke(Sub() UpdateMeter(levelDb))
        Return
    End If
    
    ' Now safe to update UI:
    meterDisplay.Level = levelDb
End Sub
```

**RULE:** Audio callbacks run on audio thread, NOT UI thread!

---

## ?? Visual Feedback

### LED Indicators (Color Coding)
- **Gray:** Idle/Disabled
- **Yellow:** Armed/Ready
- **Red:** Recording
- **Green:** Playback (simple)
- **RoyalBlue:** Playback (DSP processing)
- **Magenta:** Special processing state

### Progress Indication
- Use `progressPlayback.Value` for position
- Range: 0-1000 (for smooth updates)
- Update on PositionChanged event, NOT timer

---

## ?? UI Documentation

When creating/modifying UI:

1. **Document event wiring** in code comments
2. **Log state changes** for debugging
3. **Add tooltips** for user guidance
4. **Include RDF Phase notes** in related docs

---

**Last Updated:** 2026-01-16  
**Version:** 1.0.0
