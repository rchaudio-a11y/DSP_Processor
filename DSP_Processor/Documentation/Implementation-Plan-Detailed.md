# DSP Processor Redesign - Implementation Plan
## Complete Tabbed Interface + Recording Modes (No TemplateBuilder)

---

## ?? **Overview**

This plan redesigns DSP Processor with:
- ? Professional tabbed interface
- ? TheForge ILoggingService integration
- ? Three recording modes (Manual/Timed/Loop)
- ? File naming templates (custom parser, not TemplateBuilder)
- ? Settings persistence (JSON)
- ? Placeholder tabs for future features

**Estimated Time:** 12-16 hours total  
**Approach:** Incremental - test after each step

---

## ?? **Step-by-Step Breakdown**

### **Step 1: Add TheForge References** (30 min)

**Goal:** Link TheForge projects for ILoggingService

**Tasks:**
1. Add project reference to `TheForge.vbproj`
2. Add project reference to `RCH.TemplateStorage.vbproj` (for JSON serializer only)
3. Update `.vbproj` file
4. Test compilation

**Files Modified:**
- `DSP_Processor.vbproj`

**Verification:**
```vb
' Test compile with:
Imports TheForge.Services.Interfaces
Imports RCH.TemplateStorage.Services.Implementations
```

**Risks:**
- ?? Version conflicts (.NET Framework vs .NET 8)
- ?? Missing dependencies

**Fallback:**
- If TheForge reference fails, skip ILoggingService integration
- Continue with existing Logger

---

### **Step 2: Create ILoggingService Adapter** (45 min)

**Goal:** Wrap existing Logger to implement ILoggingService interface

**Why Adapter?**
- Keep existing Logger functionality
- Add ILoggingService compatibility
- Future: can swap implementations

**Implementation:**
```vb
' Services\LoggingServiceAdapter.vb
Namespace Services
    Public Class LoggingServiceAdapter
        Implements ILoggingService
        
        Private Shared _instance As LoggingServiceAdapter
        Public Shared ReadOnly Property Instance As LoggingServiceAdapter
        
        Public Event LogMessageReceived As EventHandler(Of LogMessageEventArgs) _
            Implements ILoggingService.LogMessageReceived
        
        Public Sub LogInfo(message As String) Implements ILoggingService.LogInfo
            Utils.Logger.Instance.Info(message, "")
            RaiseEvent LogMessageReceived(Me, New LogMessageEventArgs(...))
        End Sub
        
        ' ... implement other methods ...
    End Class
End Namespace
```

**Files Created:**
- `Services\LoggingServiceAdapter.vb` (new)

**Files Modified:**
- None (adapter wraps existing)

**Verification:**
```vb
' Test in MainForm_Load:
Dim logger = LoggingServiceAdapter.Instance
AddHandler logger.LogMessageReceived, Sub(s, e) Debug.WriteLine(e.Message)
logger.LogInfo("Test message")
```

---

### **Step 3: Create Recording Mode Models** (1 hour)

**Goal:** Define data structures for recording modes

**Models:**

**A) RecordingMode.vb**
```vb
Namespace Recording
    Public Enum RecordingMode
        Manual      ' User start/stop
        Timed       ' Auto-stop after duration
        LoopRecord  ' Multiple takes
    End Enum
    
    Public Class RecordingModeSettings
        Public Property Mode As RecordingMode = RecordingMode.Manual
        Public Property Duration As TimeSpan = TimeSpan.FromSeconds(30)
        Public Property LoopCount As Integer = 10
        Public Property IsInfiniteLoop As Boolean = False
        Public Property FileNamingTemplate As String = "Take_{take:000}.wav"
        
        ' Calculated properties
        Public ReadOnly Property TotalDuration As TimeSpan
            Get
                If Mode = RecordingMode.Timed Then
                    Return Duration
                ElseIf Mode = RecordingMode.LoopRecord AndAlso Not IsInfiniteLoop Then
                    Return TimeSpan.FromTicks(Duration.Ticks * LoopCount)
                Else
                    Return TimeSpan.Zero
                End If
            End Get
        End Property
        
        ' JSON serialization
        Public Function ToJson() As String
            Return JsonConvert.SerializeObject(Me, Formatting.Indented)
        End Function
        
        Public Shared Function FromJson(json As String) As RecordingModeSettings
            Return JsonConvert.DeserializeObject(Of RecordingModeSettings)(json)
        End Function
    End Class
End Namespace
```

**B) FileNamingTemplate.vb**
```vb
Namespace Recording
    Public Class FileNamingTemplate
        Private Shared ReadOnly Tokens As New Dictionary(Of String, Func(Of Object, String)) From {
            {"{take}", Function(ctx) DirectCast(ctx, TemplateContext).TakeNumber.ToString("000")},
            {"{take:000}", Function(ctx) DirectCast(ctx, TemplateContext).TakeNumber.ToString("000")},
            {"{take:0000}", Function(ctx) DirectCast(ctx, TemplateContext).TakeNumber.ToString("0000")},
            {"{date}", Function(ctx) DateTime.Now.ToString("yyyyMMdd")},
            {"{date:yyyy-MM-dd}", Function(ctx) DateTime.Now.ToString("yyyy-MM-dd")},
            {"{time}", Function(ctx) DateTime.Now.ToString("HHmmss")},
            {"{time:HH-mm-ss}", Function(ctx) DateTime.Now.ToString("HH-mm-ss")},
            {"{mode}", Function(ctx) DirectCast(ctx, TemplateContext).Mode.ToString()},
            {"{duration}", Function(ctx) DirectCast(ctx, TemplateContext).Duration.TotalSeconds.ToString("0")},
            {"{samplerate}", Function(ctx) DirectCast(ctx, TemplateContext).SampleRate.ToString()},
            {"{channels}", Function(ctx) DirectCast(ctx, TemplateContext).Channels.ToString()},
            {"{bitdepth}", Function(ctx) DirectCast(ctx, TemplateContext).BitsPerSample.ToString()}
        }
        
        Public Shared Function Parse(template As String, context As TemplateContext) As String
            Dim result = template
            For Each kvp In Tokens
                If result.Contains(kvp.Key) Then
                    result = result.Replace(kvp.Key, kvp.Value(context))
                End If
            Next
            Return result
        End Function
        
        Public Shared Function Validate(template As String) As (IsValid As Boolean, Error As String)
            ' Check for invalid filename characters
            Dim invalidChars = Path.GetInvalidFileNameChars()
            
            ' Replace all known tokens with dummy values
            Dim testStr = template
            For Each token In Tokens.Keys
                testStr = testStr.Replace(token, "X")
            Next
            
            ' Check if result has invalid chars
            For Each c In invalidChars
                If testStr.Contains(c) Then
                    Return (False, $"Invalid character: '{c}'")
                End If
            Next
            
            Return (True, "")
        End Function
        
        Public Shared Function GetAvailableTokens() As String()
            Return Tokens.Keys.ToArray()
        End Function
    End Class
    
    Public Class TemplateContext
        Public Property TakeNumber As Integer
        Public Property Mode As RecordingMode
        Public Property Duration As TimeSpan
        Public Property SampleRate As Integer
        Public Property Channels As Integer
        Public Property BitsPerSample As Integer
    End Class
End Namespace
```

**Files Created:**
- `Recording\RecordingMode.vb`
- `Recording\FileNamingTemplate.vb`

**Verification:**
```vb
' Test template parsing:
Dim context = New TemplateContext With {
    .TakeNumber = 5,
    .Mode = RecordingMode.Loop,
    .Duration = TimeSpan.FromSeconds(30)
}
Dim result = FileNamingTemplate.Parse("Take_{date}_{take:000}.wav", context)
' Result: "Take_20240115_005.wav"

' Test validation:
Dim (isValid, err) = FileNamingTemplate.Validate("Take_{take}.wav")
' isValid = True

Dim (isValid2, err2) = FileNamingTemplate.Validate("Take_{take}/subfolder.wav")
' isValid2 = False, err2 = "Invalid character: '/'"
```

---

### **Step 4: Update RecordingEngine** (1.5 hours)

**Goal:** Add recording mode support to engine

**Changes:**

```vb
' Recording\RecordingEngine.vb

Public Class RecordingEngine
    ' ... existing fields ...
    
    ' NEW: Recording mode support
    Public Property Mode As RecordingMode = RecordingMode.Manual
    Public Property ModeSettings As RecordingModeSettings = New RecordingModeSettings()
    
    Private currentLoopIndex As Integer = 0
    Private loopStopwatch As Stopwatch
    Private templateContext As TemplateContext
    
    Public Sub StartRecording()
        ' ... existing validation ...
        
        ' Initialize template context
        templateContext = New TemplateContext With {
            .TakeNumber = currentIndex,
            .Mode = Mode,
            .Duration = ModeSettings.Duration,
            .SampleRate = InputSource.SampleRate,
            .Channels = InputSource.Channels,
            .BitsPerSample = InputSource.BitsPerSample
        }
        
        ' Generate filename using template
        Dim filename As String
        If String.IsNullOrEmpty(ModeSettings.FileNamingTemplate) Then
            filename = String.Format(AutoNamePattern, DateTime.Now, currentIndex)
        Else
            filename = FileNamingTemplate.Parse(ModeSettings.FileNamingTemplate, templateContext)
        End If
        
        Dim fullPath = IO.Path.Combine(OutputFolder, filename)
        
        ' ... rest of existing code ...
        
        ' Setup mode-specific logic
        Select Case Mode
            Case RecordingMode.Manual
                ' Existing behavior
                
            Case RecordingMode.Timed
                TimedRecordingEnabled = True
                RecordingDurationSeconds = ModeSettings.Duration.TotalSeconds
                
            Case RecordingMode.LoopRecord
                currentLoopIndex = 1
                loopStopwatch = Stopwatch.StartNew()
        End Select
        
        isRecording = True
        stopwatch.Start()
    End Sub
    
    Public Sub Process()
        ' ... existing buffer processing ...
        
        ' Check loop mode
        If Mode = RecordingMode.LoopRecord AndAlso 
           loopStopwatch IsNot Nothing AndAlso
           loopStopwatch.Elapsed >= ModeSettings.Duration Then
            
            ' Stop current take
            StopRecording()
            
            ' Check if should continue
            If ModeSettings.IsInfiniteLoop OrElse 
               currentLoopIndex < ModeSettings.LoopCount Then
                
                currentLoopIndex += 1
                currentIndex += 1
                
                ' Restart for next loop
                StartRecording()
            End If
        End If
        
        ' ... rest of existing code ...
    End Sub
End Class
```

**Files Modified:**
- `Recording\RecordingEngine.vb`

**Verification:**
```vb
' Test timed recording:
recorder.Mode = RecordingMode.Timed
recorder.ModeSettings.Duration = TimeSpan.FromSeconds(5)
recorder.StartRecording()
' Should auto-stop after 5 seconds

' Test loop recording:
recorder.Mode = RecordingMode.LoopRecord
recorder.ModeSettings.Duration = TimeSpan.FromSeconds(3)
recorder.ModeSettings.LoopCount = 3
recorder.StartRecording()
' Should create 3 files, 3 seconds each
```

---

### **Step 5: Create Tabbed Layout** (2 hours)

**Goal:** Rebuild MainForm with TabControl

**Layout Structure:**
```
MainForm
?? toolbarPanel (Top, 60px height)
?  ?? btnRecord, btnStop, btnPlay, btnStopPlayback
?  ?? lblRecordingTime
?  ?? trackVolume, lblVolume
?  ?? lblCurrentFile
?  ?? lblStatus, panelLED
?
?? mainTabs (Fill)
?  ?? tabFiles
?  ?? tabProgram
?  ?? tabRecording
?  ?? tabDSP
?  ?? tabAnalysis
?
?? splitBottom (Bottom, 400px height)
   ?? visualizationTabs (Left, 120px width)
   ?  ?? tabWaveform
   ?  ?? tabPreFFT
   ?  ?? tabPostFFT
   ?  ?? tabFilter
   ?
   ?? displayArea (Fill)
      ?? picWaveform
      ?? (future: spectrum, filter graph)
```

**Implementation:**
```vb
' MainForm.vb

Public Class MainForm
    ' ... existing fields ...
    
    ' NEW: Tab controls
    Private toolbarPanel As Panel
    Private mainTabs As TabControl
    Private splitBottom As SplitContainer
    Private visualizationTabs As TabControl
    Private displayArea As Panel
    
    ' Tab panels
    Private filesPanel As FilesTabPanel
    Private programPanel As ProgramOptionsPanel
    Private recordingPanel As RecordingOptionsPanel
    Private dspPanel As DspFiltersPanel
    Private analysisPanel As AnalysisPanel
    
    Private Sub MainForm_Load(...)
        ' Apply dark theme first
        DarkTheme.ApplyToForm(Me)
        
        ' Create toolbar
        CreateToolbar()
        
        ' Create main tabs
        CreateMainTabs()
        
        ' Create visualization area
        CreateVisualizationArea()
        
        ' ... rest of initialization ...
    End Sub
    
    Private Sub CreateToolbar()
        toolbarPanel = New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 70,
            .BackColor = DarkTheme.ControlBackground
        }
        
        ' Move existing controls to toolbar
        toolbarPanel.Controls.Add(btnRecord)
        toolbarPanel.Controls.Add(btnStop)
        ' ... position controls ...
        
        Me.Controls.Add(toolbarPanel)
    End Sub
    
    Private Sub CreateMainTabs()
        mainTabs = New TabControl() With {
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 10)
        }
        
        ' Create tabs
        Dim tabFiles = New TabPage("?? Files")
        Dim tabProgram = New TabPage("?? Program")
        Dim tabRecording = New TabPage("??? Recording")
        Dim tabDSP = New TabPage("?? DSP")
        Dim tabAnalysis = New TabPage("?? Analysis")
        
        ' Create tab content panels
        filesPanel = New FilesTabPanel() With {.Dock = DockStyle.Fill}
        programPanel = New ProgramOptionsPanel() With {.Dock = DockStyle.Fill}
        recordingPanel = New RecordingOptionsPanel() With {.Dock = DockStyle.Fill}
        dspPanel = New DspFiltersPanel() With {.Dock = DockStyle.Fill}
        analysisPanel = New AnalysisPanel() With {.Dock = DockStyle.Fill}
        
        ' Add panels to tabs
        tabFiles.Controls.Add(filesPanel)
        tabProgram.Controls.Add(programPanel)
        tabRecording.Controls.Add(recordingPanel)
        tabDSP.Controls.Add(dspPanel)
        tabAnalysis.Controls.Add(analysisPanel)
        
        ' Add tabs
        mainTabs.TabPages.Add(tabFiles)
        mainTabs.TabPages.Add(tabProgram)
        mainTabs.TabPages.Add(tabRecording)
        mainTabs.TabPages.Add(tabDSP)
        mainTabs.TabPages.Add(tabAnalysis)
        
        ' Apply dark theme
        DarkTheme.ApplyToControl(mainTabs)
        
        Me.Controls.Add(mainTabs)
    End Sub
    
    Private Sub CreateVisualizationArea()
        splitBottom = New SplitContainer() With {
            .Dock = DockStyle.Bottom,
            .Height = 400,
            .SplitterDistance = 120,
            .FixedPanel = FixedPanel.Panel1
        }
        
        ' Left: Visualization tabs
        visualizationTabs = New TabControl() With {
            .Dock = DockStyle.Fill,
            .Alignment = TabAlignment.Left,
            .SizeMode = TabSizeMode.Fixed,
            .ItemSize = New Size(40, 100)
        }
        
        visualizationTabs.TabPages.Add("?? Wave")
        visualizationTabs.TabPages.Add("?? Pre")
        visualizationTabs.TabPages.Add("?? Post")
        visualizationTabs.TabPages.Add("??? Filter")
        
        splitBottom.Panel1.Controls.Add(visualizationTabs)
        
        ' Right: Display area
        displayArea = New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.Black
        }
        
        ' Move waveform picture to display
        picWaveform.Dock = DockStyle.Fill
        displayArea.Controls.Add(picWaveform)
        
        splitBottom.Panel2.Controls.Add(displayArea)
        
        DarkTheme.ApplyToControl(splitBottom)
        Me.Controls.Add(splitBottom)
    End Sub
End Class
```

**Files Modified:**
- `MainForm.vb` (major refactor)
- `MainForm.Designer.vb` (updated)

**Verification:**
- App launches with tabbed interface
- All tabs visible
- Toolbar at top
- Visualization area at bottom

---

### **Step 6: Create Files Tab** (1 hour)

**Goal:** Move file list and add management features

**Implementation:**
```vb
' UI\TabPanels\FilesTabPanel.vb

Public Class FilesTabPanel
    Inherits UserControl
    
    Private lstFiles As ListBox
    Private txtSearch As TextBox
    Private cmbFilter As ComboBox
    Private grpFileInfo As GroupBox
    Private lblFileName As Label
    Private lblDuration As Label
    Private lblSize As Label
    Private lblFormat As Label
    Private lblCreated As Label
    Private btnDelete As Button
    Private btnRename As Button
    Private btnExport As Button
    
    Public Event FileSelected As EventHandler(Of String)
    Public Event FileDeleted As EventHandler(Of String)
    Public Event FileRenamed As EventHandler(Of (OldName As String, NewName As String))
    
    Public Sub New()
        InitializeComponent()
    End Sub
    
    Public Sub LoadFiles(folder As String)
        lstFiles.Items.Clear()
        If Directory.Exists(folder) Then
            For Each file In Directory.GetFiles(folder, "*.wav")
                lstFiles.Items.Add(Path.GetFileName(file))
            Next
        End If
    End Sub
    
    Private Sub lstFiles_SelectedIndexChanged(sender As Object, e As EventArgs)
        If lstFiles.SelectedItem IsNot Nothing Then
            Dim fileName = lstFiles.SelectedItem.ToString()
            ShowFileInfo(fileName)
            RaiseEvent FileSelected(Me, fileName)
        End If
    End Sub
    
    Private Sub ShowFileInfo(fileName As String)
        ' Get file info and display
        Dim fullPath = Path.Combine(Application.StartupPath, "Recordings", fileName)
        If File.Exists(fullPath) Then
            Dim fileInfo = New FileInfo(fullPath)
            lblFileName.Text = fileName
            lblSize.Text = $"{fileInfo.Length / 1024.0:F2} KB"
            lblCreated.Text = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss")
            
            ' TODO: Read WAV header for duration/format
        End If
    End Sub
End Class
```

**Files Created:**
- `UI\TabPanels\FilesTabPanel.vb`

**Verification:**
- Files tab shows recording list
- File info displays when selected
- Delete/rename buttons work

---

### **Step 7: Create Program Options Tab** (1 hour)

**Goal:** Move device settings and add program options

**Implementation:**
```vb
' UI\TabPanels\ProgramOptionsPanel.vb

Public Class ProgramOptionsPanel
    Inherits UserControl
    
    Private grpAudio As GroupBox
    Private cmbInputDevice As ComboBox
    Private cmbSampleRate As ComboBox
    Private cmbBitDepth As ComboBox
    Private cmbChannels As ComboBox
    Private cmbBufferSize As ComboBox
    
    Private grpAppearance As GroupBox
    Private radioThemeDark As RadioButton
    Private radioThemeLight As RadioButton
    
    Private grpPerformance As GroupBox
    Private chkGpuAccel As CheckBox
    Private cmbCpuPriority As ComboBox
    
    Public Event SettingsChanged As EventHandler
    
    Public Sub LoadSettings(settings As ProgramSettings)
        ' Populate from settings
    End Sub
    
    Public Function GetSettings() As ProgramSettings
        ' Return current UI state as settings object
    End Function
End Class
```

**Files Created:**
- `UI\TabPanels\ProgramOptionsPanel.vb`
- `Models\ProgramSettings.vb` (settings class)

---

### **Step 8: Create Recording Options Tab** (2 hours)

**Goal:** Implement recording modes UI

**Implementation:**
```vb
' UI\TabPanels\RecordingOptionsPanel.vb

Public Class RecordingOptionsPanel
    Inherits UserControl
    
    ' Mode selection
    Private radioManual As RadioButton
    Private radioTimed As RadioButton
    Private radioLoop As RadioButton
    
    ' Timed options
    Private panelTimed As Panel
    Private numTimedDuration As NumericUpDown
    Private lblTimedTotal As Label
    
    ' Loop options
    Private panelLoop As Panel
    Private numLoopDuration As NumericUpDown
    Private numLoopCount As NumericUpDown
    Private chkInfiniteLoop As CheckBox
    Private lblLoopTotal As Label
    
    ' File naming
    Private grpFileNaming As GroupBox
    Private txtTemplate As TextBox
    Private lblPreview As Label
    Private lstTokens As ListBox
    
    Public Event SettingsChanged As EventHandler(Of RecordingModeSettings)
    
    Private Sub radioManual_CheckedChanged(sender As Object, e As EventArgs)
        panelTimed.Visible = False
        panelLoop.Visible = False
        RaiseSettingsChanged()
    End Sub
    
    Private Sub radioTimed_CheckedChanged(sender As Object, e As EventArgs)
        panelTimed.Visible = True
        panelLoop.Visible = False
        RaiseSettingsChanged()
    End Sub
    
    Private Sub radioLoop_CheckedChanged(sender As Object, e As EventArgs)
        panelTimed.Visible = False
        panelLoop.Visible = True
        RaiseSettingsChanged()
    End Sub
    
    Private Sub numTimedDuration_ValueChanged(sender As Object, e As EventArgs)
        UpdateTimedTotal()
        RaiseSettingsChanged()
    End Sub
    
    Private Sub UpdateTimedTotal()
        Dim duration = TimeSpan.FromSeconds(numTimedDuration.Value)
        lblTimedTotal.Text = $"Total: {duration:mm\:ss}"
    End Sub
    
    Private Sub UpdateLoopTotal()
        Dim duration = TimeSpan.FromSeconds(numLoopDuration.Value)
        If chkInfiniteLoop.Checked Then
            lblLoopTotal.Text = "Total: ? (until stopped)"
        Else
            Dim total = TimeSpan.FromTicks(duration.Ticks * numLoopCount.Value)
            lblLoopTotal.Text = $"Total: {total:hh\:mm\:ss} ({numLoopCount.Value} takes)"
        End If
    End Sub
    
    Private Sub txtTemplate_TextChanged(sender As Object, e As EventArgs)
        UpdatePreview()
        RaiseSettingsChanged()
    End Sub
    
    Private Sub UpdatePreview()
        Dim context = New TemplateContext With {
            .TakeNumber = 1,
            .Mode = GetSelectedMode(),
            .Duration = TimeSpan.FromSeconds(numLoopDuration.Value),
            .SampleRate = 44100,
            .Channels = 2,
            .BitsPerSample = 16
        }
        
        Try
            Dim result = FileNamingTemplate.Parse(txtTemplate.Text, context)
            Dim (isValid, err) = FileNamingTemplate.Validate(txtTemplate.Text)
            
            If isValid Then
                lblPreview.Text = $"Preview: {result} ?"
                lblPreview.ForeColor = DarkTheme.SuccessGreen
            Else
                lblPreview.Text = $"Error: {err}"
                lblPreview.ForeColor = DarkTheme.ErrorRed
            End If
        Catch ex As Exception
            lblPreview.Text = $"Error: {ex.Message}"
            lblPreview.ForeColor = DarkTheme.ErrorRed
        End Try
    End Sub
    
    Public Function GetSettings() As RecordingModeSettings
        Dim settings = New RecordingModeSettings() With {
            .Mode = GetSelectedMode(),
            .FileNamingTemplate = txtTemplate.Text
        }
        
        Select Case settings.Mode
            Case RecordingMode.Timed
                settings.Duration = TimeSpan.FromSeconds(numTimedDuration.Value)
                
            Case RecordingMode.LoopRecord
                settings.Duration = TimeSpan.FromSeconds(numLoopDuration.Value)
                settings.LoopCount = CInt(numLoopCount.Value)
                settings.IsInfiniteLoop = chkInfiniteLoop.Checked
        End Select
        
        Return settings
    End Function
    
    Private Function GetSelectedMode() As RecordingMode
        If radioTimed.Checked Then Return RecordingMode.Timed
        If radioLoop.Checked Then Return RecordingMode.LoopRecord
        Return RecordingMode.Manual
    End Function
End Class
```

**Files Created:**
- `UI\TabPanels\RecordingOptionsPanel.vb`

**Verification:**
- Mode selection toggles panels
- Calculations update live
- Template preview works
- Token list displays

---

### **Step 9: Create DSP Tab Placeholder** (30 min)

**Goal:** Basic UI for future DSP features

```vb
' UI\TabPanels\DspFiltersPanel.vb

Public Class DspFiltersPanel
    Inherits UserControl
    
    Private lblPlaceholder As Label
    
    Public Sub New()
        lblPlaceholder = New Label() With {
            .Text = "DSP/Filters - Coming in Phase 2" & vbCrLf & vbCrLf &
                    "Planned features:" & vbCrLf &
                    "• High-pass/Low-pass filters" & vbCrLf &
                    "• Parametric EQ" & vbCrLf &
                    "• Compressor/Limiter" & vbCrLf &
                    "• Filter cascade" & vbCrLf &
                    "• Real-time preview",
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 12)
        }
        Me.Controls.Add(lblPlaceholder)
    End Sub
End Class
```

**Files Created:**
- `UI\TabPanels\DspFiltersPanel.vb`

---

### **Step 10: Create Analysis Tab Placeholder** (30 min)

**Goal:** Basic UI for future analysis features

```vb
' UI\TabPanels\AnalysisPanel.vb

Public Class AnalysisPanel
    Inherits UserControl
    
    Private lblPlaceholder As Label
    
    Public Sub New()
        lblPlaceholder = New Label() With {
            .Text = "Analysis Tools - Coming in Phase 2" & vbCrLf & vbCrLf &
                    "Planned features:" & vbCrLf &
                    "• FFT Spectrum Analyzer" & vbCrLf &
                    "• Spectrogram view" & vbCrLf &
                    "• THD measurement" & vbCrLf &
                    "• Frequency response" & vbCrLf &
                    "• Phase analysis",
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 12)
        }
        Me.Controls.Add(lblPlaceholder)
    End Sub
End Class
```

**Files Created:**
- `UI\TabPanels\AnalysisPanel.vb`

---

### **Step 11: Wire Everything Together** (2 hours)

**Goal:** Connect tabs to MainForm and RecordingEngine

**Implementation:**
```vb
' MainForm.vb - Event wiring

Private Sub MainForm_Load(...)
    ' ... create tabs ...
    
    ' Wire up Files tab
    AddHandler filesPanel.FileSelected, AddressOf OnFileSelected
    AddHandler filesPanel.FileDeleted, AddressOf OnFileDeleted
    filesPanel.LoadFiles(Path.Combine(Application.StartupPath, "Recordings"))
    
    ' Wire up Program tab
    AddHandler programPanel.SettingsChanged, AddressOf OnProgramSettingsChanged
    programPanel.LoadSettings(LoadProgramSettings())
    
    ' Wire up Recording tab
    AddHandler recordingPanel.SettingsChanged, AddressOf OnRecordingSettingsChanged
    recordingPanel.LoadSettings(recorder.ModeSettings)
    
    ' ... rest ...
End Sub

Private Sub OnRecordingSettingsChanged(sender As Object, settings As RecordingModeSettings)
    recorder.Mode = settings.Mode
    recorder.ModeSettings = settings
    Logger.Instance.Info($"Recording mode changed to: {settings.Mode}", "MainForm")
End Sub

Private Sub btnRecord_Click(sender As Object, e As EventArgs) Handles btnRecord.Click
    ' ... existing code ...
    
    ' Recording mode settings are already applied from tab
    recorder.StartRecording()
    
    ' ... update UI ...
End Sub
```

**Files Modified:**
- `MainForm.vb`

---

### **Step 12: Test Everything** (2 hours)

**Goal:** Verify all functionality works

**Test Checklist:**

**? Tab Navigation**
- [ ] All tabs load without errors
- [ ] Switching tabs preserves state
- [ ] Tab content displays correctly

**? Recording Modes**
- [ ] Manual mode works (existing behavior)
- [ ] Timed mode auto-stops after duration
- [ ] Loop mode creates multiple files
- [ ] Infinite loop mode continues until stopped

**? File Naming**
- [ ] Template preview updates correctly
- [ ] All tokens work (`{take}`, `{date}`, `{time}`, etc.)
- [ ] Validation catches invalid templates
- [ ] Files created with correct names

**? UI/UX**
- [ ] Dark theme applied to all tabs
- [ ] Controls respond correctly
- [ ] Calculations update in real-time
- [ ] No visual glitches

**? Performance**
- [ ] No lag when switching tabs
- [ ] Recording still smooth
- [ ] Memory usage acceptable

**? Settings Persistence**
- [ ] Settings save to JSON
- [ ] Settings load on startup
- [ ] Settings survive app restart

---

## ?? **Time Estimates**

| Step | Description | Time |
|------|-------------|------|
| 1 | TheForge references | 30 min |
| 2 | Logging adapter | 45 min |
| 3 | Recording models | 1 hour |
| 4 | RecordingEngine update | 1.5 hours |
| 5 | Tabbed layout | 2 hours |
| 6 | Files tab | 1 hour |
| 7 | Program tab | 1 hour |
| 8 | Recording tab | 2 hours |
| 9 | DSP tab | 30 min |
| 10 | Analysis tab | 30 min |
| 11 | Wire everything | 2 hours |
| 12 | Testing | 2 hours |
| **Total** | **Complete** | **14.5 hours** |

---

## ?? **Success Criteria**

### **Must Have (MVP)**
- [x] Professional tabbed interface
- [x] Three recording modes working
- [x] File naming templates functional
- [x] Settings persistence
- [x] No broken existing features

### **Should Have**
- [x] Dark theme on all tabs
- [x] Real-time calculations
- [x] Template validation
- [x] Smooth navigation

### **Nice to Have**
- [ ] Settings import/export
- [ ] Preset templates
- [ ] Keyboard shortcuts
- [ ] Tab tooltips

---

## ?? **Review Checklist**

Before starting, confirm:
- [ ] Plan structure makes sense
- [ ] Step order is logical
- [ ] Time estimates are reasonable
- [ ] No critical features missing
- [ ] Fallback plans for risks
- [ ] Testing is adequate

---

## ?? **Ready to Start?**

Once you approve this plan, I'll:
1. ? Start with Step 1 (TheForge references)
2. ? Complete each step in order
3. ? Test after each step
4. ? Report progress as I go
5. ? Ask for guidance if blocked

**Approve this plan and I'll begin implementation!** ??
