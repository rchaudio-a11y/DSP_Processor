# Input Settings Tab - Simplified Plan (Volume & Metering Only) ?????

## ?? **Scope: Volume Control + Meter Parameters**

**Focus:** Raw input volume and visual meter display configuration  
**Exclude:** DSP processing (saved for dedicated DSP/Effects tabs)

---

## ?? **Simplified Tab Layout**

```
???????????????????????????????????????????????????????????
?  ??? Input Tab                                            ?
???????????????????????????????????????????????????????????
?                                                          ?
?  ????????????????????????????????????????????????????  ?
?  ? Input Volume Control                             ?  ?
?  ????????????????????????????????????????????????????  ?
?  ? Input Volume: [?????????????????] 100%          ?  ?
?  ?                                                   ?  ?
?  ? ? Link to Playback Volume                       ?  ?
?  ????????????????????????????????????????????????????  ?
?                                                          ?
?  ????????????????????????????????????????????????????  ?
?  ? Volume Meter Display                             ?  ?
?  ????????????????????????????????????????????????????  ?
?  ? Peak Behavior:                                   ?  ?
?  ?   Peak Hold Time:   [500ms ?]                   ?  ?
?  ?   Peak Decay Rate:  [3dB/sec ?]                 ?  ?
?  ?                                                   ?  ?
?  ? RMS Calculation:                                 ?  ?
?  ?   RMS Window Size:  [50ms ?]                    ?  ?
?  ?                                                   ?  ?
?  ? Meter Ballistics:                                ?  ?
?  ?   Attack Time:      [0ms ?] (instant)           ?  ?
?  ?   Release Time:     [300ms ?]                   ?  ?
?  ?                                                   ?  ?
?  ? Clipping Detection:                              ?  ?
?  ?   Clip Threshold:   [-0.1dB ?]                  ?  ?
?  ?   Clip Hold Time:   [2000ms ?]                  ?  ?
?  ?   Clip Color:       [?? Red    ?]               ?  ?
?  ????????????????????????????????????????????????????  ?
?                                                          ?
?  ????????????????????????????????????????????????????  ?
?  ? Meter Appearance                                 ?  ?
?  ????????????????????????????????????????????????????  ?
?  ? Meter Style:                                     ?  ?
?  ?   ? Classic (Green/Yellow/Red)                  ?  ?
?  ?   ? BBC (Green/Amber/Red)                       ?  ?
?  ?   ? Nordic (Blue/White/Red)                     ?  ?
?  ?   ? Broadcast (VU style)                        ?  ?
?  ?                                                   ?  ?
?  ? Scale Type:                                      ?  ?
?  ?   ? dBFS (Digital)                              ?  ?
?  ?   ? VU (Analog style)                           ?  ?
?  ?                                                   ?  ?
?  ? Update Rate:  [30fps ?]                         ?  ?
?  ????????????????????????????????????????????????????  ?
?                                                          ?
?  ????????????????????????????????????????????????????  ?
?  ? Presets                                          ?  ?
?  ????????????????????????????????????????????????????  ?
?  ? [Fast Response] [Slow Response] [Broadcast]     ?  ?
?  ? [Save Custom] [Load] [Reset to Defaults]        ?  ?
?  ????????????????????????????????????????????????????  ?
???????????????????????????????????????????????????????????
```

---

## ??? **Control Specifications**

### **Group 1: Input Volume Control**

| Control | Type | Range/Values | Default | Purpose |
|---------|------|--------------|---------|---------|
| Input Volume | TrackBar | 0-200% | 100% | Master input gain |
| Link to Playback | CheckBox | On/Off | Off | Sync input/output volume |

---

### **Group 2: Volume Meter Display - Peak Behavior**

| Control | Type | Values | Default | Purpose |
|---------|------|--------|---------|---------|
| Peak Hold Time | ComboBox | 100ms, 250ms, 500ms, 1000ms, 2000ms, Infinite | 500ms | Duration to hold peak value |
| Peak Decay Rate | ComboBox | 1dB/s, 3dB/s, 6dB/s, 12dB/s, 24dB/s, Instant | 3dB/s | Peak meter fall speed |

---

### **Group 3: Volume Meter Display - RMS Calculation**

| Control | Type | Values | Default | Purpose |
|---------|------|--------|---------|---------|
| RMS Window Size | ComboBox | 10ms, 30ms, 50ms, 100ms, 300ms, 500ms | 50ms | RMS averaging window |

---

### **Group 4: Volume Meter Display - Meter Ballistics**

| Control | Type | Values | Default | Purpose |
|---------|------|--------|---------|---------|
| Attack Time | ComboBox | 0ms (instant), 10ms, 30ms, 50ms, 100ms | 0ms | Meter rise speed |
| Release Time | ComboBox | 100ms, 300ms, 500ms, 1000ms, 2000ms | 300ms | Meter fall speed |

---

### **Group 5: Volume Meter Display - Clipping Detection**

| Control | Type | Values | Default | Purpose |
|---------|------|--------|---------|---------|
| Clip Threshold | ComboBox | -0.5dB, -0.3dB, -0.1dB, 0dB | -0.1dB | Level to trigger clip indicator |
| Clip Hold Time | ComboBox | 500ms, 1000ms, 2000ms, 5000ms, Infinite | 2000ms | Clip indicator hold duration |
| Clip Color | ComboBox | Red, Orange, Magenta, White | Red | Clip indicator color |

---

### **Group 6: Meter Appearance**

| Control | Type | Values | Default | Purpose |
|---------|------|--------|---------|---------|
| Meter Style | RadioButtons | Classic, BBC, Nordic, Broadcast | Classic | Color scheme |
| Scale Type | RadioButtons | dBFS, VU | dBFS | Meter scale type |
| Update Rate | ComboBox | 15fps, 30fps, 60fps | 30fps | Meter refresh rate |

---

### **Group 7: Presets**

| Button | Action |
|--------|--------|
| Fast Response | Quick attack/release for transient-heavy material |
| Slow Response | Smooth metering for broadcast-style |
| Broadcast | Professional broadcast standard (BBC PPM style) |
| Save Custom | Save current settings |
| Load | Load saved preset |
| Reset to Defaults | Factory reset |

---

## ?? **Preset Configurations**

### **Fast Response:**
```
Peak Hold: 250ms
Peak Decay: 12dB/s
RMS Window: 30ms
Attack: 0ms (instant)
Release: 100ms
Clip Threshold: -0.1dB
Style: Classic
```

### **Slow Response:**
```
Peak Hold: 1000ms
Peak Decay: 3dB/s
RMS Window: 100ms
Attack: 50ms
Release: 1000ms
Clip Threshold: -0.3dB
Style: Classic
```

### **Broadcast:**
```
Peak Hold: 500ms
Peak Decay: 24dB/s (instant fall after hold)
RMS Window: 50ms
Attack: 10ms
Release: 2000ms
Clip Threshold: 0dB
Style: BBC
```

---

## ??? **Implementation Steps**

### **Phase 1: Create Data Model** (30 min)

#### **Step 1.1: Create MeterSettings.vb**

```vb
Namespace Models

    ''' <summary>
    ''' Configuration for volume meter display and input volume
    ''' </summary>
    Public Class MeterSettings
        ' Input Volume
        Public Property InputVolumePercent As Integer = 100
        Public Property LinkToPlaybackVolume As Boolean = False
        
        ' Peak Behavior
        Public Property PeakHoldMs As Integer = 500
        Public Property PeakDecayDbPerSec As Single = 3.0F
        
        ' RMS Calculation
        Public Property RmsWindowMs As Integer = 50
        
        ' Meter Ballistics
        Public Property AttackMs As Integer = 0
        Public Property ReleaseMs As Integer = 300
        
        ' Clipping Detection
        Public Property ClipThresholdDb As Single = -0.1F
        Public Property ClipHoldMs As Integer = 2000
        Public Property ClipColor As Color = Color.Red
        
        ' Meter Appearance
        Public Property MeterStyle As MeterStyleType = MeterStyleType.Classic
        Public Property ScaleType As MeterScaleType = MeterScaleType.dBFS
        Public Property UpdateRateFps As Integer = 30
        
        ' Serialization
        Public Function ToJson() As String
            Return JsonConvert.SerializeObject(Me, Formatting.Indented)
        End Function
        
        Public Shared Function FromJson(json As String) As MeterSettings
            Return JsonConvert.DeserializeObject(Of MeterSettings)(json)
        End Function
        
        ' Presets
        Public Shared Function FastResponsePreset() As MeterSettings
            Return New MeterSettings() With {
                .PeakHoldMs = 250,
                .PeakDecayDbPerSec = 12.0F,
                .RmsWindowMs = 30,
                .AttackMs = 0,
                .ReleaseMs = 100,
                .ClipThresholdDb = -0.1F,
                .MeterStyle = MeterStyleType.Classic
            }
        End Function
        
        Public Shared Function SlowResponsePreset() As MeterSettings
            Return New MeterSettings() With {
                .PeakHoldMs = 1000,
                .PeakDecayDbPerSec = 3.0F,
                .RmsWindowMs = 100,
                .AttackMs = 50,
                .ReleaseMs = 1000,
                .ClipThresholdDb = -0.3F,
                .MeterStyle = MeterStyleType.Classic
            }
        End Function
        
        Public Shared Function BroadcastPreset() As MeterSettings
            Return New MeterSettings() With {
                .PeakHoldMs = 500,
                .PeakDecayDbPerSec = 24.0F,
                .RmsWindowMs = 50,
                .AttackMs = 10,
                .ReleaseMs = 2000,
                .ClipThresholdDb = 0.0F,
                .MeterStyle = MeterStyleType.BBC
            }
        End Function
    End Class
    
    Public Enum MeterStyleType
        Classic = 0  ' Green/Yellow/Red
        BBC = 1      ' Green/Amber/Red (PPM)
        Nordic = 2   ' Blue/White/Red
        Broadcast = 3 ' VU style
    End Enum
    
    Public Enum MeterScaleType
        dBFS = 0     ' Digital Full Scale
        VU = 1       ' Volume Unit (analog style)
    End Enum

End Namespace
```

---

### **Phase 2: Update AudioLevelMeter.vb** (30 min)

#### **Step 2.1: Add Configurable Parameters**

```vb
Namespace Utils

    Public Class AudioLevelMeter
        ' NEW: Static configuration (shared across all metering)
        Public Shared Property PeakHoldMs As Integer = 500
        Public Shared Property PeakDecayDbPerSec As Single = 3.0F
        Public Shared Property RmsWindowMs As Integer = 50
        Public Shared Property AttackMs As Integer = 0
        Public Shared Property ReleaseMs As Integer = 300
        Public Shared Property ClipThresholdDb As Single = -0.1F
        
        ' NEW: Peak hold tracking
        Private Shared lastPeakTime As DateTime = DateTime.MinValue
        Private Shared lastPeakValueDb As Single = -96.0F
        
        ' NEW: Attack/Release smoothing
        Private Shared lastSmoothValueDb As Single = -96.0F
        
        Public Shared Function AnalyzeSamples(buffer() As Byte, bitsPerSample As Integer, channels As Integer) As LevelData
            ' ... existing peak/RMS calculation ...
            
            ' NEW: Apply peak hold and decay
            Dim currentPeakDb = CalculatePeakDb(samples, maxSample)
            currentPeakDb = ApplyPeakHoldAndDecay(currentPeakDb)
            
            ' NEW: Apply attack/release smoothing
            currentPeakDb = ApplyAttackRelease(currentPeakDb, lastSmoothValueDb)
            lastSmoothValueDb = currentPeakDb
            
            Return New LevelData With {
                .PeakDB = currentPeakDb,
                .RMSDB = rmsDb,
                .IsClipping = currentPeakDb >= ClipThresholdDb
            }
        End Function
        
        Private Shared Function ApplyPeakHoldAndDecay(currentDb As Single) As Single
            Dim now = DateTime.Now
            Dim timeSinceLastPeak = (now - lastPeakTime).TotalMilliseconds
            
            ' If new peak, update and hold
            If currentDb > lastPeakValueDb Then
                lastPeakValueDb = currentDb
                lastPeakTime = now
                Return currentDb
            End If
            
            ' During hold time, return held peak
            If timeSinceLastPeak < PeakHoldMs Then
                Return Math.Max(currentDb, lastPeakValueDb)
            End If
            
            ' After hold, apply decay
            Dim decayTimeSeconds = (timeSinceLastPeak - PeakHoldMs) / 1000.0F
            Dim decayAmount = PeakDecayDbPerSec * decayTimeSeconds
            Dim decayedPeak = lastPeakValueDb - decayAmount
            
            ' Return max of current or decayed peak
            Return Math.Max(currentDb, decayedPeak)
        End Function
        
        Private Shared Function ApplyAttackRelease(currentDb As Single, lastDb As Single) As Single
            If AttackMs = 0 AndAlso ReleaseMs = 0 Then
                Return currentDb ' Instant response
            End If
            
            ' Determine if rising (attack) or falling (release)
            Dim timeConstantMs = If(currentDb > lastDb, AttackMs, ReleaseMs)
            
            If timeConstantMs = 0 Then
                Return currentDb ' Instant for this direction
            End If
            
            ' Calculate smoothing coefficient (exponential)
            ' Assumes ~30fps update rate
            Dim alpha = CSng(1.0 - Math.Exp(-33.0 / timeConstantMs))
            
            ' Apply smoothing
            Return lastDb + alpha * (currentDb - lastDb)
        End Function
        
        ''' <summary>
        ''' Reset peak tracking (call when stopping recording)
        ''' </summary>
        Public Shared Sub ResetPeakTracking()
            lastPeakTime = DateTime.MinValue
            lastPeakValueDb = -96.0F
            lastSmoothValueDb = -96.0F
        End Sub
    End Class

End Namespace
```

---

### **Phase 3: Create InputTabPanel.vb** (1.5 hours)

#### **Step 3.1: Create UI Panel**

```vb
Namespace UI.TabPanels

    Public Class InputTabPanel
        Inherits UserControl
        
        ' Group 1: Input Volume
        Private grpInputVolume As GroupBox
        Private trackInputVolume As TrackBar
        Private lblInputVolumeValue As Label
        Private chkLinkToPlayback As CheckBox
        
        ' Group 2: Peak Behavior
        Private grpPeakBehavior As GroupBox
        Private lblPeakHold As Label
        Private cmbPeakHold As ComboBox
        Private lblPeakDecay As Label
        Private cmbPeakDecay As ComboBox
        
        ' Group 3: RMS
        Private grpRms As GroupBox
        Private lblRmsWindow As Label
        Private cmbRmsWindow As ComboBox
        
        ' Group 4: Ballistics
        Private grpBallistics As GroupBox
        Private lblAttack As Label
        Private cmbAttack As ComboBox
        Private lblRelease As Label
        Private cmbRelease As ComboBox
        
        ' Group 5: Clipping
        Private grpClipping As GroupBox
        Private lblClipThreshold As Label
        Private cmbClipThreshold As ComboBox
        Private lblClipHold As Label
        Private cmbClipHold As ComboBox
        Private lblClipColor As Label
        Private cmbClipColor As ComboBox
        
        ' Group 6: Appearance
        Private grpAppearance As GroupBox
        Private radioClassic As RadioButton
        Private radioBBC As RadioButton
        Private radioNordic As RadioButton
        Private radioBroadcast As RadioButton
        Private radioDbFS As RadioButton
        Private radioVU As RadioButton
        Private lblUpdateRate As Label
        Private cmbUpdateRate As ComboBox
        
        ' Group 7: Presets
        Private grpPresets As GroupBox
        Private btnFastResponse As Button
        Private btnSlowResponse As Button
        Private btnBroadcast As Button
        Private btnSaveCustom As Button
        Private btnLoad As Button
        Private btnReset As Button
        
        Public Event SettingsChanged As EventHandler(Of MeterSettings)
        
        Public Sub New()
            InitializeComponent()
            PopulateControls()
            ApplyDarkTheme()
        End Sub
        
        Private Sub InitializeComponent()
            ' Create all controls
            CreateInputVolumeGroup()
            CreatePeakBehaviorGroup()
            CreateRmsGroup()
            CreateBallisticsGroup()
            CreateClippingGroup()
            CreateAppearanceGroup()
            CreatePresetsGroup()
            
            ' Layout controls
            LayoutControls()
        End Sub
        
        Private Sub PopulateControls()
            ' Peak Hold Time
            cmbPeakHold.Items.AddRange({"100ms", "250ms", "500ms", "1000ms", "2000ms", "Infinite"})
            cmbPeakHold.SelectedItem = "500ms"
            
            ' Peak Decay Rate
            cmbPeakDecay.Items.AddRange({"1dB/s", "3dB/s", "6dB/s", "12dB/s", "24dB/s", "Instant"})
            cmbPeakDecay.SelectedItem = "3dB/s"
            
            ' RMS Window
            cmbRmsWindow.Items.AddRange({"10ms", "30ms", "50ms", "100ms", "300ms", "500ms"})
            cmbRmsWindow.SelectedItem = "50ms"
            
            ' Attack Time
            cmbAttack.Items.AddRange({"0ms (instant)", "10ms", "30ms", "50ms", "100ms"})
            cmbAttack.SelectedItem = "0ms (instant)"
            
            ' Release Time
            cmbRelease.Items.AddRange({"100ms", "300ms", "500ms", "1000ms", "2000ms"})
            cmbRelease.SelectedItem = "300ms"
            
            ' Clip Threshold
            cmbClipThreshold.Items.AddRange({"-0.5dB", "-0.3dB", "-0.1dB", "0dB"})
            cmbClipThreshold.SelectedItem = "-0.1dB"
            
            ' Clip Hold
            cmbClipHold.Items.AddRange({"500ms", "1000ms", "2000ms", "5000ms", "Infinite"})
            cmbClipHold.SelectedItem = "2000ms"
            
            ' Clip Color
            cmbClipColor.Items.AddRange({"Red", "Orange", "Magenta", "White"})
            cmbClipColor.SelectedItem = "Red"
            
            ' Update Rate
            cmbUpdateRate.Items.AddRange({"15fps", "30fps", "60fps"})
            cmbUpdateRate.SelectedItem = "30fps"
        End Sub
        
        Public Function GetSettings() As MeterSettings
            Dim settings = New MeterSettings()
            
            ' Input Volume
            settings.InputVolumePercent = trackInputVolume.Value
            settings.LinkToPlaybackVolume = chkLinkToPlayback.Checked
            
            ' Peak Behavior
            settings.PeakHoldMs = ParseMilliseconds(cmbPeakHold.SelectedItem.ToString())
            settings.PeakDecayDbPerSec = ParseDecayRate(cmbPeakDecay.SelectedItem.ToString())
            
            ' RMS
            settings.RmsWindowMs = ParseMilliseconds(cmbRmsWindow.SelectedItem.ToString())
            
            ' Ballistics
            settings.AttackMs = ParseMilliseconds(cmbAttack.SelectedItem.ToString())
            settings.ReleaseMs = ParseMilliseconds(cmbRelease.SelectedItem.ToString())
            
            ' Clipping
            settings.ClipThresholdDb = ParseDbValue(cmbClipThreshold.SelectedItem.ToString())
            settings.ClipHoldMs = ParseMilliseconds(cmbClipHold.SelectedItem.ToString())
            settings.ClipColor = ParseColor(cmbClipColor.SelectedItem.ToString())
            
            ' Appearance
            If radioClassic.Checked Then settings.MeterStyle = MeterStyleType.Classic
            If radioBBC.Checked Then settings.MeterStyle = MeterStyleType.BBC
            If radioNordic.Checked Then settings.MeterStyle = MeterStyleType.Nordic
            If radioBroadcast.Checked Then settings.MeterStyle = MeterStyleType.Broadcast
            
            settings.ScaleType = If(radioDbFS.Checked, MeterScaleType.dBFS, MeterScaleType.VU)
            settings.UpdateRateFps = ParseFps(cmbUpdateRate.SelectedItem.ToString())
            
            Return settings
        End Function
        
        Public Sub LoadSettings(settings As MeterSettings)
            ' Load settings into UI
            trackInputVolume.Value = settings.InputVolumePercent
            chkLinkToPlayback.Checked = settings.LinkToPlaybackVolume
            ' ... set other controls ...
        End Sub
        
        ' Event handlers
        Private Sub OnAnyControlChanged(sender As Object, e As EventArgs)
            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub
        
        ' Preset buttons
        Private Sub btnFastResponse_Click(sender As Object, e As EventArgs) Handles btnFastResponse.Click
            LoadSettings(MeterSettings.FastResponsePreset())
        End Sub
        
        Private Sub btnSlowResponse_Click(sender As Object, e As EventArgs) Handles btnSlowResponse.Click
            LoadSettings(MeterSettings.SlowResponsePreset())
        End Sub
        
        Private Sub btnBroadcast_Click(sender As Object, e As EventArgs) Handles btnBroadcast.Click
            LoadSettings(MeterSettings.BroadcastPreset())
        End Sub
        
    End Class

End Namespace
```

---

### **Phase 4: Integration** (1 hour)

#### **Step 4.1: Update MainForm.vb**

```vb
Private inputTabPanel As InputTabPanel
Private currentMeterSettings As MeterSettings

Private Sub MainForm_Load(...)
    ' ... existing code ...
    
    ' Create Input tab panel
    inputTabPanel = New InputTabPanel()
    tabInput.Controls.Add(inputTabPanel)
    inputTabPanel.Dock = DockStyle.Fill
    
    ' Wire up events
    AddHandler inputTabPanel.SettingsChanged, AddressOf OnMeterSettingsChanged
    
    ' Load saved settings
    currentMeterSettings = LoadMeterSettings()
    inputTabPanel.LoadSettings(currentMeterSettings)
    ApplyMeterSettings(currentMeterSettings)
End Sub

Private Sub OnMeterSettingsChanged(sender As Object, settings As MeterSettings)
    Services.LoggingServiceAdapter.Instance.LogInfo("Meter settings changed")
    
    ' Apply to audio system
    ApplyMeterSettings(settings)
    
    ' Save settings
    SaveMeterSettings(settings)
    currentMeterSettings = settings
End Sub

Private Sub ApplyMeterSettings(settings As MeterSettings)
    ' Apply to MicInputSource
    If mic IsNot Nothing Then
        mic.Volume = settings.InputVolumePercent / 100.0F
    End If
    
    ' Apply to AudioLevelMeter (static properties)
    AudioLevelMeter.PeakHoldMs = settings.PeakHoldMs
    AudioLevelMeter.PeakDecayDbPerSec = settings.PeakDecayDbPerSec
    AudioLevelMeter.RmsWindowMs = settings.RmsWindowMs
    AudioLevelMeter.AttackMs = settings.AttackMs
    AudioLevelMeter.ReleaseMs = settings.ReleaseMs
    AudioLevelMeter.ClipThresholdDb = settings.ClipThresholdDb
    
    ' Reset peak tracking when settings change
    AudioLevelMeter.ResetPeakTracking()
    
    ' Apply to VolumeMeterControl
    meterRecording.UpdateRate = settings.UpdateRateFps
    meterRecording.ClipColor = settings.ClipColor
    meterRecording.ClipHoldMs = settings.ClipHoldMs
    ' ... apply style, scale type, etc.
    
    Services.LoggingServiceAdapter.Instance.LogInfo($"Meter settings applied: Peak={settings.PeakHoldMs}ms, Decay={settings.PeakDecayDbPerSec}dB/s")
End Sub

Private Function LoadMeterSettings() As MeterSettings
    Dim settingsFile = Path.Combine(Application.StartupPath, "meter_settings.json")
    If File.Exists(settingsFile) Then
        Try
            Dim json = File.ReadAllText(settingsFile)
            Return MeterSettings.FromJson(json)
        Catch ex As Exception
            Logger.Instance.Warning("Failed to load meter settings, using defaults", "MainForm")
        End Try
    End If
    Return New MeterSettings() ' Defaults
End Function

Private Sub SaveMeterSettings(settings As MeterSettings)
    Dim settingsFile = Path.Combine(Application.StartupPath, "meter_settings.json")
    Try
        File.WriteAllText(settingsFile, settings.ToJson())
        Services.LoggingServiceAdapter.Instance.LogInfo("Meter settings saved")
    Catch ex As Exception
        Logger.Instance.Error("Failed to save meter settings", ex, "MainForm")
    End Try
End Sub
```

---

### **Phase 5: Update Designer** (30 min)

Add **Input** tab to `MainForm.Designer.vb` (move existing input volume controls here):

```vb
' Remove trackInputVolume and lblInputVolume from Program tab
' Add to new Input tab

mainTabs.TabPages.Add(tabInput)

' tabInput
tabInput.BackColor = Color.FromArgb(45, 45, 48)
tabInput.Location = New Point(4, 54)
tabInput.Name = "tabInput"
tabInput.Padding = New Padding(3)
tabInput.Size = New Size(342, 994)
tabInput.TabIndex = 5
tabInput.Text = "??? Input"
```

---

## ?? **Revised Time Estimate: 4.5 hours**

| Phase | Task | Time |
|-------|------|------|
| 1 | Data Model (MeterSettings.vb) | 30 min |
| 2 | Update AudioLevelMeter.vb | 30 min |
| 3 | Create InputTabPanel.vb | 1.5 hours |
| 4 | Integration (MainForm.vb) | 1 hour |
| 5 | Designer Update | 30 min |
| 6 | Testing | 30 min |
| **Total** | **Simplified** | **4.5 hours** |

---

## ?? **What You Get**

### **? Input Volume:**
- Slider (0-200%)
- Real-time adjustment
- Optional link to playback volume

### **? Meter Display Configuration:**
- **Peak Hold:** 100ms to Infinite
- **Peak Decay:** 1dB/s to Instant
- **RMS Window:** 10ms to 500ms
- **Attack/Release:** 0ms to 2000ms
- **Clip Threshold:** -0.5dB to 0dB
- **Clip Hold:** 500ms to Infinite
- **Clip Color:** Red/Orange/Magenta/White

### **? Meter Styles:**
- Classic (Green/Yellow/Red)
- BBC PPM (Green/Amber/Red)
- Nordic (Blue/White/Red)
- Broadcast (VU style)

### **? Presets:**
- Fast Response (transients)
- Slow Response (broadcast)
- Broadcast (BBC PPM)
- Custom save/load
- Reset to defaults

### **? Persistence:**
- Settings saved to `meter_settings.json`
- Auto-load on startup
- Logging of all changes

---

## ?? **Benefits**

1. ? **Much Simpler** - No DSP processing complexity
2. ? **Faster Implementation** - 4.5 hours vs 10 hours
3. ? **Focused Purpose** - Volume and metering only
4. ? **Modular Design** - DSP processing can be added to separate tabs later
5. ? **Professional Metering** - All the parameters pros need

---

## ?? **Ready to Implement?**

This simplified version focuses on what you need **now**:
- ? Input volume control
- ? Meter display configuration
- ? No AGC/Gate/HPF (those go in dedicated DSP tabs later)

**Approve to start implementation!** ??????

---

**Document Version:** 2.0 (Simplified)  
**Created:** 2024-01-11  
**For:** Rick (DSP Processor Project)  
**Feature:** Input Tab - Volume & Metering Only

**END OF SIMPLIFIED PLAN**
