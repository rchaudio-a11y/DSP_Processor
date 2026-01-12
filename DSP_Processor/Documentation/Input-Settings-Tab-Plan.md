# Input Settings Tab - Implementation Plan ???

## ?? **Overview**

Create a comprehensive **Input Settings** tab with advanced audio processing controls, including:
- Input volume with visual metering
- Peak detection parameters
- Level metering configuration
- Auto-gain control (AGC)
- Noise gate settings
- High-pass filter options

---

## ?? **Tab Layout Design**

### **Visual Structure:**

```
???????????????????????????????????????????????????????????
?  ??? Input Settings Tab                                  ?
???????????????????????????????????????????????????????????
?                                                          ?
?  ????????????????????????????????????????????????????  ?
?  ? Input Volume & Metering                          ?  ?
?  ????????????????????????????????????????????????????  ?
?  ? Input Volume: [?????????] 100%                  ?  ?
?  ?                                                   ?  ?
?  ? Peak Hold Time:  [500ms ?]                      ?  ?
?  ? Peak Decay Rate: [3dB/sec ?]                    ?  ?
?  ? RMS Window:      [50ms ?]                       ?  ?
?  ?                                                   ?  ?
?  ? Meter Ballistics:                                ?  ?
?  ?   Attack Time:  [0ms ?]  (instant)              ?  ?
?  ?   Release Time: [300ms ?]                       ?  ?
?  ?                                                   ?  ?
?  ? Clipping Indicator:                              ?  ?
?  ?   Threshold:    [-0.1dB ?]                      ?  ?
?  ?   Hold Time:    [2000ms ?]                      ?  ?
?  ????????????????????????????????????????????????????  ?
?                                                          ?
?  ????????????????????????????????????????????????????  ?
?  ? Auto-Gain Control (AGC)                          ?  ?
?  ????????????????????????????????????????????????????  ?
?  ? [?] Enable AGC                                   ?  ?
?  ?                                                   ?  ?
?  ? Target Level:   [-12dB ?]                       ?  ?
?  ? Max Gain:       [+20dB ?]                       ?  ?
?  ? Min Gain:       [-20dB ?]                       ?  ?
?  ? Attack Time:    [100ms ?]                       ?  ?
?  ? Release Time:   [1000ms ?]                      ?  ?
?  ?                                                   ?  ?
?  ? Current Gain: +3.5dB (read-only)                ?  ?
?  ????????????????????????????????????????????????????  ?
?                                                          ?
?  ????????????????????????????????????????????????????  ?
?  ? Noise Gate                                       ?  ?
?  ????????????????????????????????????????????????????  ?
?  ? [?] Enable Gate                                  ?  ?
?  ?                                                   ?  ?
?  ? Threshold:      [-40dB ?]                       ?  ?
?  ? Attack Time:    [1ms ?]                         ?  ?
?  ? Release Time:   [100ms ?]                       ?  ?
?  ? Hold Time:      [200ms ?]                       ?  ?
?  ?                                                   ?  ?
?  ? Gate Status: OPEN (read-only)                   ?  ?
?  ????????????????????????????????????????????????????  ?
?                                                          ?
?  ????????????????????????????????????????????????????  ?
?  ? High-Pass Filter                                 ?  ?
?  ????????????????????????????????????????????????????  ?
?  ? [?] Enable HPF (removes rumble/wind)            ?  ?
?  ?                                                   ?  ?
?  ? Cutoff Freq:    [80Hz ?]                        ?  ?
?  ? Filter Order:   [2nd (12dB/oct) ?]              ?  ?
?  ????????????????????????????????????????????????????  ?
?                                                          ?
?  ????????????????????????????????????????????????????  ?
?  ? Presets                                          ?  ?
?  ????????????????????????????????????????????????????  ?
?  ? [Voice] [Music] [Podcast] [Broadcast] [Custom]  ?  ?
?  ? [Save Current] [Load] [Reset to Defaults]       ?  ?
?  ????????????????????????????????????????????????????  ?
???????????????????????????????????????????????????????????
```

---

## ?? **Control Specifications**

### **Group 1: Input Volume & Metering**

| Control | Type | Range/Values | Default | Purpose |
|---------|------|--------------|---------|---------|
| Input Volume | TrackBar | 0-200% | 100% | Master input gain |
| Peak Hold Time | ComboBox | 100ms, 250ms, 500ms, 1000ms, 2000ms, Infinite | 500ms | How long to hold peak value |
| Peak Decay Rate | ComboBox | 1dB/s, 3dB/s, 6dB/s, 12dB/s, 24dB/s | 3dB/s | Peak meter decay speed |
| RMS Window | ComboBox | 10ms, 30ms, 50ms, 100ms, 300ms | 50ms | RMS averaging window |
| Attack Time | ComboBox | 0ms, 10ms, 30ms, 50ms, 100ms | 0ms | Meter rise speed |
| Release Time | ComboBox | 100ms, 300ms, 500ms, 1000ms, 2000ms | 300ms | Meter fall speed |
| Clip Threshold | ComboBox | -0.5dB, -0.3dB, -0.1dB, 0dB | -0.1dB | Clipping detection level |
| Clip Hold Time | ComboBox | 500ms, 1000ms, 2000ms, 5000ms, Infinite | 2000ms | Clip indicator hold |

---

### **Group 2: Auto-Gain Control (AGC)**

| Control | Type | Range/Values | Default | Purpose |
|---------|------|--------------|---------|---------|
| Enable AGC | CheckBox | On/Off | Off | Enable automatic gain |
| Target Level | ComboBox | -6dB, -12dB, -18dB, -20dB, -24dB | -12dB | Desired output level |
| Max Gain | ComboBox | +6dB, +12dB, +18dB, +20dB, +24dB | +20dB | Maximum boost |
| Min Gain | ComboBox | -6dB, -12dB, -18dB, -20dB, -? | -20dB | Maximum cut |
| Attack Time | ComboBox | 10ms, 50ms, 100ms, 200ms, 500ms | 100ms | Gain increase speed |
| Release Time | ComboBox | 100ms, 500ms, 1000ms, 2000ms, 5000ms | 1000ms | Gain decrease speed |
| Current Gain | Label | Read-only | - | Show current AGC gain |

---

### **Group 3: Noise Gate**

| Control | Type | Range/Values | Default | Purpose |
|---------|------|--------------|---------|---------|
| Enable Gate | CheckBox | On/Off | Off | Enable noise gate |
| Threshold | ComboBox | -60dB, -50dB, -40dB, -30dB, -20dB | -40dB | Gate open threshold |
| Attack Time | ComboBox | 0.1ms, 0.5ms, 1ms, 5ms, 10ms | 1ms | Gate open speed |
| Release Time | ComboBox | 10ms, 50ms, 100ms, 200ms, 500ms | 100ms | Gate close speed |
| Hold Time | ComboBox | 0ms, 50ms, 100ms, 200ms, 500ms | 200ms | Time before closing |
| Gate Status | Label | Read-only | - | OPEN / CLOSED |

---

### **Group 4: High-Pass Filter**

| Control | Type | Range/Values | Default | Purpose |
|---------|------|--------------|---------|---------|
| Enable HPF | CheckBox | On/Off | Off | Enable high-pass filter |
| Cutoff Freq | ComboBox | 40Hz, 60Hz, 80Hz, 100Hz, 120Hz, 150Hz | 80Hz | Rumble removal frequency |
| Filter Order | ComboBox | 1st (6dB/oct), 2nd (12dB/oct), 3rd (18dB/oct), 4th (24dB/oct) | 2nd | Filter steepness |

---

### **Group 5: Presets**

| Button | Action |
|--------|--------|
| Voice | Load voice recording preset |
| Music | Load music recording preset |
| Podcast | Load podcast preset |
| Broadcast | Load broadcast preset |
| Custom | User's saved settings |
| Save Current | Save current settings to Custom |
| Load | Load preset from file |
| Reset to Defaults | Reset all to factory defaults |

---

## ??? **Preset Configurations**

### **Voice Preset:**
```
Input Volume: 110%
AGC: Enabled (-18dB target, ±12dB range)
Noise Gate: Enabled (-45dB threshold)
HPF: Enabled (80Hz, 2nd order)
Peak Hold: 500ms
RMS Window: 100ms
```

### **Music Preset:**
```
Input Volume: 100%
AGC: Disabled
Noise Gate: Disabled
HPF: Disabled
Peak Hold: 1000ms
RMS Window: 50ms
```

### **Podcast Preset:**
```
Input Volume: 120%
AGC: Enabled (-12dB target, ±18dB range)
Noise Gate: Enabled (-40dB threshold)
HPF: Enabled (100Hz, 2nd order)
Peak Hold: 500ms
RMS Window: 100ms
```

### **Broadcast Preset:**
```
Input Volume: 100%
AGC: Enabled (-6dB target, ±6dB range)
Noise Gate: Enabled (-50dB threshold)
HPF: Enabled (60Hz, 2nd order)
Peak Hold: 250ms
RMS Window: 30ms
```

---

## ??? **Implementation Steps**

### **Phase 1: UI Creation** (2 hours)

#### **Step 1.1: Create InputSettingsPanel.vb**
```vb
Namespace UI.TabPanels

    Public Class InputSettingsPanel
        Inherits UserControl
        
        ' Group 1: Volume & Metering
        Private grpVolumeMeter As GroupBox
        Private trackInputVolume As TrackBar
        Private lblInputVolume As Label
        Private cmbPeakHold As ComboBox
        Private cmbPeakDecay As ComboBox
        Private cmbRmsWindow As ComboBox
        Private cmbAttackTime As ComboBox
        Private cmbReleaseTime As ComboBox
        Private cmbClipThreshold As ComboBox
        Private cmbClipHold As ComboBox
        
        ' Group 2: AGC
        Private grpAgc As GroupBox
        Private chkEnableAgc As CheckBox
        Private cmbAgcTarget As ComboBox
        Private cmbAgcMaxGain As ComboBox
        Private cmbAgcMinGain As ComboBox
        Private cmbAgcAttack As ComboBox
        Private cmbAgcRelease As ComboBox
        Private lblCurrentGain As Label
        
        ' Group 3: Noise Gate
        Private grpNoiseGate As GroupBox
        Private chkEnableGate As CheckBox
        Private cmbGateThreshold As ComboBox
        Private cmbGateAttack As ComboBox
        Private cmbGateRelease As ComboBox
        Private cmbGateHold As ComboBox
        Private lblGateStatus As Label
        
        ' Group 4: HPF
        Private grpHpf As GroupBox
        Private chkEnableHpf As CheckBox
        Private cmbHpfCutoff As ComboBox
        Private cmbHpfOrder As ComboBox
        
        ' Group 5: Presets
        Private grpPresets As GroupBox
        Private btnVoice As Button
        Private btnMusic As Button
        Private btnPodcast As Button
        Private btnBroadcast As Button
        Private btnCustom As Button
        Private btnSave As Button
        Private btnLoad As Button
        Private btnReset As Button
        
        ' Events
        Public Event SettingsChanged As EventHandler(Of InputSettings)
        
        Public Sub New()
            InitializeComponent()
        End Sub
        
        Private Sub InitializeComponent()
            ' Create all controls
            ' Apply DarkTheme
            ' Wire up events
        End Sub
        
        Public Function GetSettings() As InputSettings
            ' Return current UI state
        End Function
        
        Public Sub LoadSettings(settings As InputSettings)
            ' Update UI from settings object
        End Sub
        
    End Class
    
End Namespace
```

#### **Step 1.2: Create InputSettings.vb (Model)**
```vb
Namespace Models

    Public Class InputSettings
        ' Volume & Metering
        Public Property InputVolume As Single = 1.0F
        Public Property PeakHoldMs As Integer = 500
        Public Property PeakDecayDbPerSec As Single = 3.0F
        Public Property RmsWindowMs As Integer = 50
        Public Property MeterAttackMs As Integer = 0
        Public Property MeterReleaseMs As Integer = 300
        Public Property ClipThresholdDb As Single = -0.1F
        Public Property ClipHoldMs As Integer = 2000
        
        ' AGC
        Public Property AgcEnabled As Boolean = False
        Public Property AgcTargetDb As Single = -12.0F
        Public Property AgcMaxGainDb As Single = 20.0F
        Public Property AgcMinGainDb As Single = -20.0F
        Public Property AgcAttackMs As Integer = 100
        Public Property AgcReleaseMs As Integer = 1000
        
        ' Noise Gate
        Public Property GateEnabled As Boolean = False
        Public Property GateThresholdDb As Single = -40.0F
        Public Property GateAttackMs As Integer = 1
        Public Property GateReleaseMs As Integer = 100
        Public Property GateHoldMs As Integer = 200
        
        ' HPF
        Public Property HpfEnabled As Boolean = False
        Public Property HpfCutoffHz As Integer = 80
        Public Property HpfOrder As Integer = 2
        
        ' Serialization
        Public Function ToJson() As String
            Return JsonConvert.SerializeObject(Me, Formatting.Indented)
        End Function
        
        Public Shared Function FromJson(json As String) As InputSettings
            Return JsonConvert.DeserializeObject(Of InputSettings)(json)
        End Function
        
        ' Presets
        Public Shared Function VoicePreset() As InputSettings
            Return New InputSettings() With {
                .InputVolume = 1.1F,
                .AgcEnabled = True,
                .AgcTargetDb = -18.0F,
                .GateEnabled = True,
                .GateThresholdDb = -45.0F,
                .HpfEnabled = True,
                .HpfCutoffHz = 80
            }
        End Function
        
        Public Shared Function MusicPreset() As InputSettings
            ' ... etc
        End Function
    End Class
    
End Namespace
```

---

### **Phase 2: Audio Processing** (3 hours)

#### **Step 2.1: Update AudioLevelMeter.vb**
Add support for configurable parameters:
```vb
Public Class AudioLevelMeter
    ' Existing code...
    
    ' NEW: Configurable parameters
    Public Shared Property PeakHoldMs As Integer = 500
    Public Shared Property PeakDecayDbPerSec As Single = 3.0F
    Public Shared Property RmsWindowMs As Integer = 50
    Public Shared Property ClipThresholdDb As Single = -0.1F
    
    ' NEW: Time-based peak decay
    Private Shared lastPeakTime As DateTime
    Private Shared lastPeakValue As Single
    
    Public Shared Function AnalyzeSamples(...) As LevelData
        ' Existing analysis
        
        ' NEW: Apply peak hold and decay
        Dim now = DateTime.Now
        Dim timeSinceLastPeak = (now - lastPeakTime).TotalMilliseconds
        
        If timeSinceLastPeak < PeakHoldMs Then
            ' Hold peak
            peakDb = Math.Max(peakDb, lastPeakValue)
        Else
            ' Decay peak
            Dim decayAmount = PeakDecayDbPerSec * (timeSinceLastPeak - PeakHoldMs) / 1000.0F
            Dim decayedPeak = lastPeakValue - decayAmount
            peakDb = Math.Max(peakDb, decayedPeak)
        End If
        
        ' Update last peak
        If peakDb > lastPeakValue Then
            lastPeakValue = peakDb
            lastPeakTime = now
        End If
        
        ' Rest of existing code...
    End Function
End Class
```

#### **Step 2.2: Create AutoGainControl.vb**
```vb
Namespace DSP

    Public Class AutoGainControl
        Implements IProcessor
        
        Public Property Enabled As Boolean = False
        Public Property TargetLevelDb As Single = -12.0F
        Public Property MaxGainDb As Single = 20.0F
        Public Property MinGainDb As Single = -20.0F
        Public Property AttackMs As Integer = 100
        Public Property ReleaseMs As Integer = 1000
        
        Private currentGainDb As Single = 0.0F
        Private attackCoeff As Single
        Private releaseCoeff As Single
        
        Public Sub New()
            UpdateCoefficients()
        End Sub
        
        Private Sub UpdateCoefficients()
            ' Calculate attack/release coefficients
            attackCoeff = CSng(Math.Exp(-1.0 / (AttackMs / 1000.0 * 44100)))
            releaseCoeff = CSng(Math.Exp(-1.0 / (ReleaseMs / 1000.0 * 44100)))
        End Sub
        
        Public Function Process(buffer() As Byte, bitsPerSample As Integer) As Byte() _
            Implements IProcessor.Process
            
            If Not Enabled Then Return buffer
            
            ' Measure current level
            Dim levelData = AudioLevelMeter.AnalyzeSamples(buffer, bitsPerSample, 2)
            Dim currentDb = levelData.RMSDB
            
            ' Calculate desired gain
            Dim desiredGainDb = TargetLevelDb - currentDb
            desiredGainDb = Math.Max(MinGainDb, Math.Min(MaxGainDb, desiredGainDb))
            
            ' Smooth gain changes
            Dim coeff = If(desiredGainDb > currentGainDb, attackCoeff, releaseCoeff)
            currentGainDb = currentGainDb * coeff + desiredGainDb * (1.0F - coeff)
            
            ' Apply gain
            Dim linearGain = CSng(Math.Pow(10, currentGainDb / 20.0))
            Return ApplyGain(buffer, linearGain, bitsPerSample)
        End Function
        
        Public ReadOnly Property CurrentGainDb As Single
            Get
                Return currentGainDb
            End Get
        End Property
    End Class
    
End Namespace
```

#### **Step 2.3: Create NoiseGate.vb**
```vb
Namespace DSP

    Public Class NoiseGate
        Implements IProcessor
        
        Public Property Enabled As Boolean = False
        Public Property ThresholdDb As Single = -40.0F
        Public Property AttackMs As Integer = 1
        Public Property ReleaseMs As Integer = 100
        Public Property HoldMs As Integer = 200
        
        Private isOpen As Boolean = False
        Private holdStartTime As DateTime
        Private currentGain As Single = 0.0F
        
        Public Function Process(buffer() As Byte, bitsPerSample As Integer) As Byte() _
            Implements IProcessor.Process
            
            If Not Enabled Then Return buffer
            
            ' Measure level
            Dim levelData = AudioLevelMeter.AnalyzeSamples(buffer, bitsPerSample, 2)
            Dim currentDb = levelData.RMSDB
            
            ' Determine gate state
            If currentDb > ThresholdDb Then
                ' Above threshold - open gate
                isOpen = True
                holdStartTime = DateTime.Now
            ElseIf isOpen Then
                ' Check hold time
                Dim holdElapsed = (DateTime.Now - holdStartTime).TotalMilliseconds
                If holdElapsed > HoldMs Then
                    isOpen = False
                End If
            End If
            
            ' Calculate target gain
            Dim targetGain = If(isOpen, 1.0F, 0.0F)
            
            ' Smooth gain (attack/release)
            Dim rampRate = If(targetGain > currentGain, AttackMs, ReleaseMs)
            ' ... apply smoothing ...
            
            ' Apply gain
            Return ApplyGain(buffer, currentGain, bitsPerSample)
        End Function
        
        Public ReadOnly Property GateStatus As String
            Get
                Return If(isOpen, "OPEN", "CLOSED")
            End Get
        End Property
    End Class
    
End Namespace
```

#### **Step 2.4: Create HighPassFilter.vb**
```vb
Namespace DSP

    Public Class HighPassFilter
        Implements IProcessor
        
        Public Property Enabled As Boolean = False
        Public Property CutoffHz As Integer = 80
        Public Property Order As Integer = 2
        
        Private coefficients() As Single
        Private state() As Single
        
        Public Sub New()
            UpdateFilter()
        End Sub
        
        Private Sub UpdateFilter()
            ' Calculate Butterworth HPF coefficients
            ' For each order, we need more coefficients
            ' ... implement or use DSP library ...
        End Sub
        
        Public Function Process(buffer() As Byte, bitsPerSample As Integer) As Byte() _
            Implements IProcessor.Process
            
            If Not Enabled Then Return buffer
            
            ' Apply IIR filter to buffer
            ' ... filter implementation ...
            
            Return buffer
        End Function
    End Class
    
End Namespace
```

---

### **Phase 3: Integration** (2 hours)

#### **Step 3.1: Update MicInputSource.vb**
Add processing chain:
```vb
Public Class MicInputSource
    Implements IInputSource
    
    ' Existing code...
    
    ' NEW: Processing chain
    Private agc As AutoGainControl
    Private gate As NoiseGate
    Private hpf As HighPassFilter
    
    Private Sub OnDataAvailable(sender As Object, e As WaveInEventArgs)
        ' Existing code...
        
        ' NEW: Apply processing chain
        Dim processed = copy
        
        If hpf IsNot Nothing AndAlso hpf.Enabled Then
            processed = hpf.Process(processed, bitsValue)
        End If
        
        If gate IsNot Nothing AndAlso gate.Enabled Then
            processed = gate.Process(processed, bitsValue)
        End If
        
        If Math.Abs(volumeValue - 1.0F) > 0.001F Then
            processed = ApplyVolume(processed, bitsValue)
        End If
        
        If agc IsNot Nothing AndAlso agc.Enabled Then
            processed = agc.Process(processed, bitsValue)
        End If
        
        bufferQueue.Enqueue(processed)
    End Sub
    
    Public Sub UpdateInputSettings(settings As InputSettings)
        ' Update all processors
        volumeValue = settings.InputVolume
        
        If agc Is Nothing Then agc = New AutoGainControl()
        agc.Enabled = settings.AgcEnabled
        agc.TargetLevelDb = settings.AgcTargetDb
        ' ... set other AGC properties
        
        If gate Is Nothing Then gate = New NoiseGate()
        gate.Enabled = settings.GateEnabled
        ' ... set gate properties
        
        If hpf Is Nothing Then hpf = New HighPassFilter()
        hpf.Enabled = settings.HpfEnabled
        ' ... set HPF properties
        
        ' Update AudioLevelMeter settings
        AudioLevelMeter.PeakHoldMs = settings.PeakHoldMs
        AudioLevelMeter.PeakDecayDbPerSec = settings.PeakDecayDbPerSec
        AudioLevelMeter.RmsWindowMs = settings.RmsWindowMs
        AudioLevelMeter.ClipThresholdDb = settings.ClipThresholdDb
    End Sub
End Class
```

#### **Step 3.2: Update MainForm.vb**
Add Input Settings tab:
```vb
Private inputSettingsPanel As InputSettingsPanel

Private Sub MainForm_Load(...)
    ' Existing code...
    
    ' Create Input Settings panel
    inputSettingsPanel = New InputSettingsPanel()
    tabInput.Controls.Add(inputSettingsPanel)
    inputSettingsPanel.Dock = DockStyle.Fill
    
    ' Wire up events
    AddHandler inputSettingsPanel.SettingsChanged, AddressOf OnInputSettingsChanged
    
    ' Load saved settings
    Dim settings = LoadInputSettings()
    inputSettingsPanel.LoadSettings(settings)
    ApplyInputSettings(settings)
End Sub

Private Sub OnInputSettingsChanged(sender As Object, settings As InputSettings)
    Services.LoggingServiceAdapter.Instance.LogInfo("Input settings changed")
    
    ' Apply to mic
    If mic IsNot Nothing Then
        mic.UpdateInputSettings(settings)
    End If
    
    ' Save settings
    SaveInputSettings(settings)
End Sub

Private Function LoadInputSettings() As InputSettings
    Dim settingsFile = Path.Combine(Application.StartupPath, "input_settings.json")
    If File.Exists(settingsFile) Then
        Try
            Dim json = File.ReadAllText(settingsFile)
            Return InputSettings.FromJson(json)
        Catch ex As Exception
            Logger.Instance.Warning("Failed to load input settings, using defaults", "MainForm")
        End Try
    End If
    Return New InputSettings()
End Function

Private Sub SaveInputSettings(settings As InputSettings)
    Dim settingsFile = Path.Combine(Application.StartupPath, "input_settings.json")
    Try
        File.WriteAllText(settingsFile, settings.ToJson())
        Services.LoggingServiceAdapter.Instance.LogInfo("Input settings saved")
    Catch ex As Exception
        Logger.Instance.Error("Failed to save input settings", ex, "MainForm")
    End Try
End Sub
```

---

### **Phase 4: Update Designer** (1 hour)

Add **Input** tab to `MainForm.Designer.vb`:
```vb
mainTabs.TabPages.Add(tabInput)

' tabInput
tabInput.BackColor = Color.FromArgb(45, 45, 48)
tabInput.Controls.Add(inputSettingsPanel)
tabInput.Location = New Point(4, 54)
tabInput.Name = "tabInput"
tabInput.Padding = New Padding(3)
tabInput.Size = New Size(342, 994)
tabInput.TabIndex = 5
tabInput.Text = "??? Input"
```

---

## ?? **Testing Plan**

### **Test 1: Volume Control**
- ? Slider changes input volume
- ? 0% = muted
- ? 100% = unity
- ? 200% = 2x boost
- ? Meter reflects volume change

### **Test 2: Peak Metering**
- ? Peak hold time works (500ms default)
- ? Peak decays at correct rate (3dB/s default)
- ? RMS averages correctly (50ms window)
- ? Clip indicator triggers at threshold (-0.1dB)

### **Test 3: AGC**
- ? AGC boosts quiet signals to target level
- ? AGC reduces loud signals to target level
- ? Gain changes smoothly (attack/release)
- ? Current gain display updates
- ? Respects min/max gain limits

### **Test 4: Noise Gate**
- ? Gate opens above threshold
- ? Gate closes below threshold
- ? Hold time prevents chatter
- ? Attack/release smooth
- ? Status display updates (OPEN/CLOSED)

### **Test 5: HPF**
- ? Filter removes rumble below cutoff
- ? Different orders tested (6dB to 24dB/oct)
- ? No audible distortion

### **Test 6: Presets**
- ? Voice preset loads correctly
- ? Music preset loads correctly
- ? Podcast preset loads correctly
- ? Broadcast preset loads correctly
- ? Custom save/load works
- ? Reset to defaults works

---

## ?? **Success Criteria**

### **Must Have:**
- ? Input volume control (0-200%)
- ? Peak/RMS metering with configurable parameters
- ? AGC with adjustable target level
- ? Noise gate with threshold control
- ? High-pass filter
- ? Preset system
- ? Settings persistence (JSON)

### **Should Have:**
- ? Real-time status displays (AGC gain, gate status)
- ? Visual feedback for all changes
- ? Comprehensive logging
- ? Dark theme applied

### **Nice to Have:**
- [ ] Spectrum analyzer for HPF visualization
- [ ] AGC gain reduction meter
- [ ] Gate visualization
- [ ] Per-preset custom settings

---

## ?? **Time Estimate**

| Phase | Task | Time |
|-------|------|------|
| 1 | UI Creation | 2 hours |
| 2 | Audio Processing | 3 hours |
| 3 | Integration | 2 hours |
| 4 | Designer Update | 1 hour |
| 5 | Testing | 2 hours |
| **Total** | **Complete** | **10 hours** |

---

## ?? **Benefits**

### **1. Professional Control**
- ? Full control over audio processing
- ? Fine-tune metering behavior
- ? Optimize for different use cases

### **2. Problem Solving**
- ? AGC solves variable input levels
- ? Gate removes background noise
- ? HPF removes rumble/wind

### **3. Workflow Efficiency**
- ? Presets for quick switching
- ? Save custom configurations
- ? Recall settings instantly

### **4. Audio Quality**
- ? Professional-grade processing
- ? Broadcast-ready output
- ? Clean, polished recordings

---

## ?? **Next Steps**

**After Review:**
1. Create `InputSettingsPanel.vb` (UI)
2. Create `InputSettings.vb` (model)
3. Create `AutoGainControl.vb` (DSP)
4. Create `NoiseGate.vb` (DSP)
5. Create `HighPassFilter.vb` (DSP)
6. Update `MicInputSource.vb` (integration)
7. Update `MainForm.vb` (wire up)
8. Update `MainForm.Designer.vb` (add tab)
9. Test all features
10. Document and commit

---

## ?? **Status**

**Plan:** ? Complete  
**Approval:** ? Awaiting review  
**Implementation:** ? Ready to start  
**Est. Time:** 10 hours  

**Ready to build a professional input processing system!** ????

---

**Document Version:** 1.0  
**Created:** 2024-01-11  
**For:** Rick (DSP Processor Project)  
**Feature:** Input Settings Tab - Advanced Audio Controls

**END OF PLAN**
