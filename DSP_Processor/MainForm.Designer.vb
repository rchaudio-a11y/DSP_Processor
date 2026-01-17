<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        TimerAudio = New Timer(components)
        btnStopPlayback = New Button()
        lblStatus = New Label()
        panelLED = New Panel()
        meterRecording = New UI.VolumeMeterControl()
        meterPlayback = New UI.VolumeMeterControl()
        lblRecordingTime = New Label()
        lstRecordings = New ListBox()
        progressPlayback = New ProgressBar()
        TimerPlayback = New Timer(components)
        TimerMeters = New Timer(components)
        btnDelete = New Button()
        transportControl = New UI.TransportControl()
        splitWaveformArea = New SplitContainer()
        WaveformDisplayControl1 = New UI.WaveformDisplayControl()
        mainTabs = New TabControl()
        tabFiles = New TabPage()
        tabProgram = New TabPage()
        AudioSettingsPanel1 = New UI.TabPanels.AudioSettingsPanel()
        RoutingPanel1 = New UI.TabPanels.RoutingPanel()
        tabInput = New TabPage()
        InputTabPanel1 = New UI.TabPanels.InputTabPanel()
        tabRecording = New TabPage()
        RecordingOptionsPanel1 = New UI.TabPanels.RecordingOptionsPanel()
        tabPipeline = New TabPage()
        AudioPipelinePanel1 = New UI.TabPanels.AudioPipelinePanel()
        tabSpectrum = New TabPage()
        SpectrumSettingsPanel1 = New UI.TabPanels.SpectrumSettingsPanel()
        lblSpectrumInfo = New Label()
        tabAnalysis = New TabPage()
        tabLogs = New TabPage()
        txtLogViewer = New RichTextBox()
        btnClearLogs = New Button()
        btnSaveLogs = New Button()
        chkAutoScroll = New CheckBox()
        cmbLogLevel = New ComboBox()
        lblLogLevel = New Label()
        visualizationTabs = New TabControl()
        tabWaveform = New TabPage()
        tabSpectrum1 = New TabPage()
        SpectrumAnalyzerControl1 = New UI.SpectrumAnalyzerControl()
        tabPhase = New TabPage()
        tabMeters = New TabPage()
        TabPage1 = New TabPage()
        DspSignalFlowPanel1 = New DSPSignalFlowPanel()
        CType(splitWaveformArea, ComponentModel.ISupportInitialize).BeginInit()
        splitWaveformArea.Panel1.SuspendLayout()
        splitWaveformArea.Panel2.SuspendLayout()
        splitWaveformArea.SuspendLayout()
        mainTabs.SuspendLayout()
        tabFiles.SuspendLayout()
        tabProgram.SuspendLayout()
        tabInput.SuspendLayout()
        tabRecording.SuspendLayout()
        tabPipeline.SuspendLayout()
        tabSpectrum.SuspendLayout()
        tabLogs.SuspendLayout()
        visualizationTabs.SuspendLayout()
        tabWaveform.SuspendLayout()
        tabSpectrum1.SuspendLayout()
        tabMeters.SuspendLayout()
        TabPage1.SuspendLayout()
        SuspendLayout()
        ' 
        ' TimerAudio
        ' 
        TimerAudio.Interval = 10
        ' 
        ' btnStopPlayback
        ' 
        btnStopPlayback.Location = New Point(-1000, -1000)
        btnStopPlayback.Name = "btnStopPlayback"
        btnStopPlayback.Size = New Size(1, 1)
        btnStopPlayback.TabIndex = 100
        btnStopPlayback.Visible = False
        ' 
        ' lblStatus
        ' 
        lblStatus.AutoSize = True
        lblStatus.Location = New Point(-1000, -1000)
        lblStatus.Name = "lblStatus"
        lblStatus.Size = New Size(49, 20)
        lblStatus.TabIndex = 101
        lblStatus.Text = "Status"
        lblStatus.Visible = False
        ' 
        ' panelLED
        ' 
        panelLED.BackColor = Color.Lime
        panelLED.BorderStyle = BorderStyle.Fixed3D
        panelLED.Location = New Point(-1000, -1000)
        panelLED.Name = "panelLED"
        panelLED.Size = New Size(20, 20)
        panelLED.TabIndex = 102
        panelLED.Visible = False
        ' 
        ' meterRecording
        ' 
        meterRecording.BackColor = Color.Black
        meterRecording.Dock = DockStyle.Fill
        meterRecording.Location = New Point(0, 0)
        meterRecording.Name = "meterRecording"
        meterRecording.Size = New Size(60, 812)
        meterRecording.TabIndex = 0
        ' 
        ' meterPlayback
        ' 
        meterPlayback.BackColor = Color.Black
        meterPlayback.Location = New Point(-1000, -1000)
        meterPlayback.Name = "meterPlayback"
        meterPlayback.Size = New Size(40, 220)
        meterPlayback.TabIndex = 15
        meterPlayback.Visible = False
        ' 
        ' lblRecordingTime
        ' 
        lblRecordingTime.AutoSize = True
        lblRecordingTime.Font = New Font("Segoe UI", 16.0F, FontStyle.Bold)
        lblRecordingTime.ForeColor = Color.Red
        lblRecordingTime.Location = New Point(172, 50)
        lblRecordingTime.Name = "lblRecordingTime"
        lblRecordingTime.Size = New Size(88, 37)
        lblRecordingTime.TabIndex = 9
        lblRecordingTime.Text = "00:00"
        lblRecordingTime.Visible = False
        ' 
        ' lstRecordings
        ' 
        lstRecordings.FormattingEnabled = True
        lstRecordings.Location = New Point(6, 114)
        lstRecordings.Name = "lstRecordings"
        lstRecordings.ScrollAlwaysVisible = True
        lstRecordings.Size = New Size(324, 324)
        lstRecordings.TabIndex = 3
        ' 
        ' progressPlayback
        ' 
        progressPlayback.Dock = DockStyle.Top
        progressPlayback.Location = New Point(0, 0)
        progressPlayback.Maximum = 1000
        progressPlayback.Name = "progressPlayback"
        progressPlayback.Size = New Size(1248, 30)
        progressPlayback.Style = ProgressBarStyle.Marquee
        progressPlayback.TabIndex = 0
        progressPlayback.Value = 1
        ' 
        ' TimerPlayback
        ' 
        TimerPlayback.Interval = 17
        ' 
        ' TimerMeters
        ' 
        TimerMeters.Interval = 33
        ' 
        ' btnDelete
        ' 
        btnDelete.Location = New Point(3, 6)
        btnDelete.Name = "btnDelete"
        btnDelete.Size = New Size(150, 40)
        btnDelete.TabIndex = 10
        btnDelete.Text = "Delete Recording"
        btnDelete.UseVisualStyleBackColor = True
        ' 
        ' transportControl
        ' 
        transportControl.BackColor = Color.FromArgb(CByte(30), CByte(30), CByte(30))
        transportControl.Dock = DockStyle.Top
        transportControl.Location = New Point(0, 0)
        transportControl.Name = "transportControl"
        transportControl.Size = New Size(1782, 194)
        transportControl.TabIndex = 6
        ' 
        ' splitWaveformArea
        ' 
        splitWaveformArea.Dock = DockStyle.Fill
        splitWaveformArea.FixedPanel = FixedPanel.Panel1
        splitWaveformArea.IsSplitterFixed = True
        splitWaveformArea.Location = New Point(3, 3)
        splitWaveformArea.Name = "splitWaveformArea"
        ' 
        ' splitWaveformArea.Panel1
        ' 
        splitWaveformArea.Panel1.Controls.Add(meterRecording)
        splitWaveformArea.Panel1MinSize = 60
        ' 
        ' splitWaveformArea.Panel2
        ' 
        splitWaveformArea.Panel2.Controls.Add(WaveformDisplayControl1)
        splitWaveformArea.Panel2.Controls.Add(progressPlayback)
        splitWaveformArea.Size = New Size(1312, 812)
        splitWaveformArea.SplitterDistance = 60
        splitWaveformArea.TabIndex = 1
        ' 
        ' WaveformDisplayControl1
        ' 
        WaveformDisplayControl1.BackColor = Color.Black
        WaveformDisplayControl1.Dock = DockStyle.Fill
        WaveformDisplayControl1.Location = New Point(0, 30)
        WaveformDisplayControl1.Name = "WaveformDisplayControl1"
        WaveformDisplayControl1.Size = New Size(1248, 782)
        WaveformDisplayControl1.TabIndex = 1
        WaveformDisplayControl1.WaveformBackgroundColor = Color.Black
        WaveformDisplayControl1.WaveformForegroundColor = Color.Lime
        WaveformDisplayControl1.WaveformRightChannelColor = Color.Cyan
        ' 
        ' mainTabs
        ' 
        mainTabs.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
        mainTabs.Controls.Add(tabFiles)
        mainTabs.Controls.Add(tabProgram)
        mainTabs.Controls.Add(tabInput)
        mainTabs.Controls.Add(tabRecording)
        mainTabs.Controls.Add(tabPipeline)
        mainTabs.Controls.Add(tabSpectrum)
        mainTabs.Controls.Add(tabAnalysis)
        mainTabs.Controls.Add(tabLogs)
        mainTabs.Location = New Point(0, 200)
        mainTabs.Multiline = True
        mainTabs.Name = "mainTabs"
        mainTabs.SelectedIndex = 0
        mainTabs.Size = New Size(454, 851)
        mainTabs.TabIndex = 8
        ' 
        ' tabFiles
        ' 
        tabFiles.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabFiles.Controls.Add(lstRecordings)
        tabFiles.Controls.Add(btnDelete)
        tabFiles.Location = New Point(4, 54)
        tabFiles.Name = "tabFiles"
        tabFiles.Padding = New Padding(3)
        tabFiles.Size = New Size(446, 793)
        tabFiles.TabIndex = 0
        tabFiles.Text = "📁 Files"
        ' 
        ' tabProgram
        ' 
        tabProgram.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabProgram.Controls.Add(AudioSettingsPanel1)
        tabProgram.Controls.Add(RoutingPanel1)
        tabProgram.Location = New Point(4, 29)
        tabProgram.Name = "tabProgram"
        tabProgram.Padding = New Padding(3)
        tabProgram.Size = New Size(192, 67)
        tabProgram.TabIndex = 1
        tabProgram.Text = "⚙️ Program"
        ' 
        ' AudioSettingsPanel1
        ' 
        AudioSettingsPanel1.AutoScroll = True
        AudioSettingsPanel1.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        AudioSettingsPanel1.Dock = DockStyle.Top
        AudioSettingsPanel1.Location = New Point(3, 3)
        AudioSettingsPanel1.Name = "AudioSettingsPanel1"
        AudioSettingsPanel1.Size = New Size(186, 338)
        AudioSettingsPanel1.TabIndex = 1
        ' 
        ' RoutingPanel1
        ' 
        RoutingPanel1.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        RoutingPanel1.Location = New Point(3, 347)
        RoutingPanel1.Name = "RoutingPanel1"
        RoutingPanel1.Size = New Size(440, 270)
        RoutingPanel1.TabIndex = 2
        ' 
        ' tabInput
        ' 
        tabInput.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabInput.Controls.Add(InputTabPanel1)
        tabInput.Location = New Point(4, 54)
        tabInput.Name = "tabInput"
        tabInput.Padding = New Padding(3)
        tabInput.Size = New Size(192, 42)
        tabInput.TabIndex = 5
        tabInput.Text = "🎚️ Input"
        ' 
        ' InputTabPanel1
        ' 
        InputTabPanel1.AutoScroll = True
        InputTabPanel1.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        InputTabPanel1.Dock = DockStyle.Fill
        InputTabPanel1.ForeColor = Color.FromArgb(CByte(241), CByte(241), CByte(241))
        InputTabPanel1.Location = New Point(3, 3)
        InputTabPanel1.Name = "InputTabPanel1"
        InputTabPanel1.Size = New Size(186, 36)
        InputTabPanel1.TabIndex = 0
        ' 
        ' tabRecording
        ' 
        tabRecording.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabRecording.Controls.Add(RecordingOptionsPanel1)
        tabRecording.Location = New Point(4, 54)
        tabRecording.Name = "tabRecording"
        tabRecording.Padding = New Padding(3)
        tabRecording.Size = New Size(192, 42)
        tabRecording.TabIndex = 2
        tabRecording.Text = "🎛️ Recording"
        ' 
        ' RecordingOptionsPanel1
        ' 
        RecordingOptionsPanel1.AutoScroll = True
        RecordingOptionsPanel1.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        RecordingOptionsPanel1.Dock = DockStyle.Fill
        RecordingOptionsPanel1.ForeColor = Color.FromArgb(CByte(241), CByte(241), CByte(241))
        RecordingOptionsPanel1.Location = New Point(3, 3)
        RecordingOptionsPanel1.Name = "RecordingOptionsPanel1"
        RecordingOptionsPanel1.Size = New Size(186, 36)
        RecordingOptionsPanel1.TabIndex = 0
        ' 
        ' tabPipeline
        ' 
        tabPipeline.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabPipeline.Controls.Add(AudioPipelinePanel1)
        tabPipeline.Location = New Point(4, 54)
        tabPipeline.Name = "tabPipeline"
        tabPipeline.Padding = New Padding(3)
        tabPipeline.Size = New Size(446, 793)
        tabPipeline.TabIndex = 8
        tabPipeline.Text = "🔀 Pipeline"
        ' 
        ' AudioPipelinePanel1
        ' 
        AudioPipelinePanel1.AutoScroll = True
        AudioPipelinePanel1.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        AudioPipelinePanel1.Dock = DockStyle.Fill
        AudioPipelinePanel1.Location = New Point(3, 3)
        AudioPipelinePanel1.Name = "AudioPipelinePanel1"
        AudioPipelinePanel1.Size = New Size(440, 787)
        AudioPipelinePanel1.TabIndex = 0
        ' 
        ' tabSpectrum
        ' 
        tabSpectrum.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabSpectrum.Controls.Add(SpectrumSettingsPanel1)
        tabSpectrum.Controls.Add(lblSpectrumInfo)
        tabSpectrum.Location = New Point(4, 79)
        tabSpectrum.Name = "tabSpectrum"
        tabSpectrum.Padding = New Padding(3)
        tabSpectrum.Size = New Size(192, 17)
        tabSpectrum.TabIndex = 6
        tabSpectrum.Text = "🌈 Spectrum"
        ' 
        ' SpectrumSettingsPanel1
        ' 
        SpectrumSettingsPanel1.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        SpectrumSettingsPanel1.Location = New Point(3, 3)
        SpectrumSettingsPanel1.Name = "SpectrumSettingsPanel1"
        SpectrumSettingsPanel1.Size = New Size(440, 550)
        SpectrumSettingsPanel1.TabIndex = 0
        ' 
        ' lblSpectrumInfo
        ' 
        lblSpectrumInfo.Location = New Point(0, 0)
        lblSpectrumInfo.Name = "lblSpectrumInfo"
        lblSpectrumInfo.Size = New Size(100, 23)
        lblSpectrumInfo.TabIndex = 1
        ' 
        ' tabAnalysis
        ' 
        tabAnalysis.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabAnalysis.Location = New Point(4, 104)
        tabAnalysis.Name = "tabAnalysis"
        tabAnalysis.Padding = New Padding(3)
        tabAnalysis.Size = New Size(192, 0)
        tabAnalysis.TabIndex = 3
        tabAnalysis.Text = "📊 Analysis"
        ' 
        ' tabLogs
        ' 
        tabLogs.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabLogs.Controls.Add(txtLogViewer)
        tabLogs.Controls.Add(btnClearLogs)
        tabLogs.Controls.Add(btnSaveLogs)
        tabLogs.Controls.Add(chkAutoScroll)
        tabLogs.Controls.Add(cmbLogLevel)
        tabLogs.Controls.Add(lblLogLevel)
        tabLogs.Location = New Point(4, 104)
        tabLogs.Name = "tabLogs"
        tabLogs.Padding = New Padding(3)
        tabLogs.Size = New Size(192, 0)
        tabLogs.TabIndex = 5
        tabLogs.Text = "📜 Logs"
        ' 
        ' txtLogViewer
        ' 
        txtLogViewer.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtLogViewer.BackColor = Color.FromArgb(CByte(30), CByte(30), CByte(30))
        txtLogViewer.ForeColor = Color.White
        txtLogViewer.Location = New Point(6, 42)
        txtLogViewer.Name = "txtLogViewer"
        txtLogViewer.ReadOnly = True
        txtLogViewer.Size = New Size(180, 0)
        txtLogViewer.TabIndex = 0
        txtLogViewer.Text = ""
        ' 
        ' btnClearLogs
        ' 
        btnClearLogs.BackColor = Color.FromArgb(CByte(75), CByte(75), CByte(78))
        btnClearLogs.FlatStyle = FlatStyle.Flat
        btnClearLogs.ForeColor = Color.White
        btnClearLogs.Location = New Point(6, 6)
        btnClearLogs.Name = "btnClearLogs"
        btnClearLogs.Size = New Size(75, 30)
        btnClearLogs.TabIndex = 1
        btnClearLogs.Text = "Clear"
        btnClearLogs.UseVisualStyleBackColor = False
        ' 
        ' btnSaveLogs
        ' 
        btnSaveLogs.BackColor = Color.FromArgb(CByte(75), CByte(75), CByte(78))
        btnSaveLogs.FlatStyle = FlatStyle.Flat
        btnSaveLogs.ForeColor = Color.White
        btnSaveLogs.Location = New Point(87, 6)
        btnSaveLogs.Name = "btnSaveLogs"
        btnSaveLogs.Size = New Size(75, 30)
        btnSaveLogs.TabIndex = 2
        btnSaveLogs.Text = "Save"
        btnSaveLogs.UseVisualStyleBackColor = False
        ' 
        ' chkAutoScroll
        ' 
        chkAutoScroll.AutoSize = True
        chkAutoScroll.Checked = True
        chkAutoScroll.CheckState = CheckState.Checked
        chkAutoScroll.ForeColor = Color.White
        chkAutoScroll.Location = New Point(168, 10)
        chkAutoScroll.Name = "chkAutoScroll"
        chkAutoScroll.Size = New Size(104, 24)
        chkAutoScroll.TabIndex = 3
        chkAutoScroll.Text = "Auto Scroll"
        chkAutoScroll.UseVisualStyleBackColor = True
        ' 
        ' cmbLogLevel
        ' 
        cmbLogLevel.BackColor = Color.FromArgb(CByte(30), CByte(30), CByte(30))
        cmbLogLevel.ForeColor = Color.White
        cmbLogLevel.FormattingEnabled = True
        cmbLogLevel.Items.AddRange(New Object() {"All", "Debug", "Info", "Warn", "Error", "Fatal"})
        cmbLogLevel.Location = New Point(365, 6)
        cmbLogLevel.Name = "cmbLogLevel"
        cmbLogLevel.Size = New Size(75, 28)
        cmbLogLevel.TabIndex = 4
        cmbLogLevel.Text = "All"
        ' 
        ' lblLogLevel
        ' 
        lblLogLevel.AutoSize = True
        lblLogLevel.ForeColor = Color.White
        lblLogLevel.Location = New Point(286, 9)
        lblLogLevel.Name = "lblLogLevel"
        lblLogLevel.Size = New Size(75, 20)
        lblLogLevel.TabIndex = 5
        lblLogLevel.Text = "Log Level:"
        ' 
        ' visualizationTabs
        ' 
        visualizationTabs.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        visualizationTabs.Controls.Add(tabWaveform)
        visualizationTabs.Controls.Add(tabSpectrum1)
        visualizationTabs.Controls.Add(tabPhase)
        visualizationTabs.Controls.Add(tabMeters)
        visualizationTabs.Controls.Add(TabPage1)
        visualizationTabs.Location = New Point(456, 200)
        visualizationTabs.Multiline = True
        visualizationTabs.Name = "visualizationTabs"
        visualizationTabs.SelectedIndex = 0
        visualizationTabs.Size = New Size(1326, 851)
        visualizationTabs.TabIndex = 9
        ' 
        ' tabWaveform
        ' 
        tabWaveform.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabWaveform.Controls.Add(splitWaveformArea)
        tabWaveform.Location = New Point(4, 29)
        tabWaveform.Name = "tabWaveform"
        tabWaveform.Padding = New Padding(3)
        tabWaveform.Size = New Size(1318, 818)
        tabWaveform.TabIndex = 0
        tabWaveform.Text = "📈 Waveform"
        ' 
        ' tabSpectrum1
        ' 
        tabSpectrum1.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabSpectrum1.Controls.Add(SpectrumAnalyzerControl1)
        tabSpectrum1.Location = New Point(4, 29)
        tabSpectrum1.Name = "tabSpectrum1"
        tabSpectrum1.Padding = New Padding(3)
        tabSpectrum1.Size = New Size(1318, 818)
        tabSpectrum1.TabIndex = 1
        tabSpectrum1.Text = "🌈 Spectrum"
        ' 
        ' SpectrumAnalyzerControl1
        ' 
        SpectrumAnalyzerControl1.BackColor = Color.Black
        SpectrumAnalyzerControl1.Dock = DockStyle.Fill
        SpectrumAnalyzerControl1.Location = New Point(3, 3)
        SpectrumAnalyzerControl1.Name = "SpectrumAnalyzerControl1"
        SpectrumAnalyzerControl1.Size = New Size(1312, 812)
        SpectrumAnalyzerControl1.TabIndex = 0
        ' 
        ' tabPhase
        ' 
        tabPhase.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabPhase.Location = New Point(4, 29)
        tabPhase.Name = "tabPhase"
        tabPhase.Padding = New Padding(3)
        tabPhase.Size = New Size(1318, 818)
        tabPhase.TabIndex = 2
        tabPhase.Text = "🔄 Phase"
        ' 
        ' tabMeters
        ' 
        tabMeters.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabMeters.Controls.Add(meterPlayback)
        tabMeters.Location = New Point(4, 29)
        tabMeters.Name = "tabMeters"
        tabMeters.Padding = New Padding(3)
        tabMeters.Size = New Size(1318, 818)
        tabMeters.TabIndex = 3
        tabMeters.Text = "📊 Meters"
        ' 
        ' TabPage1
        ' 
        TabPage1.Controls.Add(DspSignalFlowPanel1)
        TabPage1.Location = New Point(4, 29)
        TabPage1.Name = "TabPage1"
        TabPage1.Size = New Size(1318, 818)
        TabPage1.TabIndex = 4
        TabPage1.Text = "DSP Sigal Flow"
        TabPage1.UseVisualStyleBackColor = True
        ' 
        ' DspSignalFlowPanel1
        ' 
        DspSignalFlowPanel1.AutoScroll = True
        DspSignalFlowPanel1.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        DspSignalFlowPanel1.Dock = DockStyle.Fill
        DspSignalFlowPanel1.Location = New Point(0, 0)
        DspSignalFlowPanel1.Name = "DspSignalFlowPanel1"
        DspSignalFlowPanel1.Size = New Size(1318, 818)
        DspSignalFlowPanel1.TabIndex = 0
        ' 
        ' MainForm
        ' 
        AutoScaleDimensions = New SizeF(8.0F, 20.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1782, 1053)
        Controls.Add(transportControl)
        Controls.Add(visualizationTabs)
        Controls.Add(mainTabs)
        Controls.Add(lblRecordingTime)
        Name = "MainForm"
        Text = "DSP Processor - Dark Mode"
        splitWaveformArea.Panel1.ResumeLayout(False)
        splitWaveformArea.Panel2.ResumeLayout(False)
        CType(splitWaveformArea, ComponentModel.ISupportInitialize).EndInit()
        splitWaveformArea.ResumeLayout(False)
        mainTabs.ResumeLayout(False)
        tabFiles.ResumeLayout(False)
        tabProgram.ResumeLayout(False)
        tabInput.ResumeLayout(False)
        tabRecording.ResumeLayout(False)
        tabPipeline.ResumeLayout(False)
        tabSpectrum.ResumeLayout(False)
        tabLogs.ResumeLayout(False)
        tabLogs.PerformLayout()
        visualizationTabs.ResumeLayout(False)
        tabWaveform.ResumeLayout(False)
        tabSpectrum1.ResumeLayout(False)
        tabMeters.ResumeLayout(False)
        TabPage1.ResumeLayout(False)
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents TimerAudio As Timer
    Friend WithEvents btnStopPlayback As Button
    Friend WithEvents lblStatus As Label
    Friend WithEvents panelLED As Panel
    Friend WithEvents meterRecording As DSP_Processor.UI.VolumeMeterControl
    Friend WithEvents meterPlayback As DSP_Processor.UI.VolumeMeterControl
    Friend WithEvents TimerMeters As Timer
    Friend WithEvents lblRecordingTime As Label
    Friend WithEvents lstRecordings As ListBox
    Friend WithEvents progressPlayback As ProgressBar
    Friend WithEvents TimerPlayback As Timer
    Friend WithEvents btnDelete As Button
    Friend WithEvents transportControl As DSP_Processor.UI.TransportControl
    Friend WithEvents splitWaveformArea As SplitContainer
    Friend WithEvents mainTabs As TabControl
    Friend WithEvents tabFiles As TabPage
    Friend WithEvents tabProgram As TabPage
    Friend WithEvents tabRecording As TabPage
    Friend WithEvents tabSpectrum As TabPage
    Friend WithEvents tabAnalysis As TabPage
    Friend WithEvents tabInput As TabPage
    Friend WithEvents tabLogs As TabPage
    Friend WithEvents txtLogViewer As RichTextBox
    Friend WithEvents btnClearLogs As Button
    Friend WithEvents btnSaveLogs As Button
    Friend WithEvents chkAutoScroll As CheckBox
    Friend WithEvents cmbLogLevel As ComboBox
    Friend WithEvents lblLogLevel As Label
    Friend WithEvents visualizationTabs As TabControl
    Friend WithEvents tabWaveform As TabPage
    Friend WithEvents tabSpectrum1 As TabPage
    Friend WithEvents tabPhase As TabPage
    Friend WithEvents tabMeters As TabPage
    Friend WithEvents RecordingOptionsPanel1 As UI.TabPanels.RecordingOptionsPanel
    Friend WithEvents SpectrumAnalyzerControl1 As UI.SpectrumAnalyzerControl
    Friend WithEvents AudioSettingsPanel1 As UI.TabPanels.AudioSettingsPanel
    Friend WithEvents InputTabPanel1 As UI.TabPanels.InputTabPanel
    Friend WithEvents AudioPipelinePanel1 As UI.TabPanels.AudioPipelinePanel
    Friend WithEvents RoutingPanel1 As UI.TabPanels.RoutingPanel
    Friend WithEvents SpectrumSettingsPanel1 As UI.TabPanels.SpectrumSettingsPanel
    Friend WithEvents tabPipeline As TabPage
    Friend WithEvents WaveformDisplayControl1 As UI.WaveformDisplayControl
    Friend WithEvents lblSpectrumInfo As Label
    Friend WithEvents TabPage1 As TabPage
    Friend WithEvents DspSignalFlowPanel1 As DSPSignalFlowPanel
End Class
