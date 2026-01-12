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
        cmbBufferSize = New ComboBox()
        Label2 = New Label()
        cmbBitDepths = New ComboBox()
        lblBitDepth = New Label()
        cmbSampleRates = New ComboBox()
        lblSampleRate = New Label()
        cmbChannelMode = New ComboBox()
        Label1 = New Label()
        lblInputDevices = New Label()
        cmbInputDevices = New ComboBox()
        lblRecordingTime = New Label()
        lstRecordings = New ListBox()
        progressPlayback = New ProgressBar()
        TimerPlayback = New Timer(components)
        picWaveform = New PictureBox()
        TimerMeters = New Timer(components)
        trackVolume = New TrackBar()
        lblVolume = New Label()
        btnDelete = New Button()
        transportControl = New UI.TransportControl()
        splitWaveformArea = New SplitContainer()
        mainTabs = New TabControl()
        tabFiles = New TabPage()
        tabProgram = New TabPage()
        trackInputVolume = New TrackBar()
        lblInputVolume = New Label()
        tabRecording = New TabPage()
        tabAnalysis = New TabPage()
        tabLogs = New TabPage()
        txtLogViewer = New RichTextBox()
        btnClearLogs = New Button()
        btnSaveLogs = New Button()
        chkAutoScroll = New CheckBox()
        cmbLogLevel = New ComboBox()
        lblLogLevel = New Label()
        CType(picWaveform, ComponentModel.ISupportInitialize).BeginInit()
        CType(trackVolume, ComponentModel.ISupportInitialize).BeginInit()
        CType(splitWaveformArea, ComponentModel.ISupportInitialize).BeginInit()
        splitWaveformArea.Panel1.SuspendLayout()
        splitWaveformArea.Panel2.SuspendLayout()
        splitWaveformArea.SuspendLayout()
        mainTabs.SuspendLayout()
        tabFiles.SuspendLayout()
        tabProgram.SuspendLayout()
        CType(trackInputVolume, ComponentModel.ISupportInitialize).BeginInit()
        tabLogs.SuspendLayout()
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
        meterRecording.Size = New Size(60, 400)
        meterRecording.TabIndex = 0
        ' 
        ' meterPlayback
        ' 
        meterPlayback.BackColor = Color.Black
        meterPlayback.Location = New Point(709, 730)
        meterPlayback.Name = "meterPlayback"
        meterPlayback.Size = New Size(40, 220)
        meterPlayback.TabIndex = 15
        ' 
        ' cmbBufferSize
        ' 
        cmbBufferSize.FormattingEnabled = True
        cmbBufferSize.Items.AddRange(New Object() {"5", "10", "20", "50"})
        cmbBufferSize.Location = New Point(7, 250)
        cmbBufferSize.Name = "cmbBufferSize"
        cmbBufferSize.Size = New Size(150, 28)
        cmbBufferSize.TabIndex = 13
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.BackColor = Color.FromArgb(CByte(192), CByte(255), CByte(255))
        Label2.Location = New Point(7, 227)
        Label2.Name = "Label2"
        Label2.Size = New Size(80, 20)
        Label2.TabIndex = 12
        Label2.Text = "Buffer Size"
        ' 
        ' cmbBitDepths
        ' 
        cmbBitDepths.FormattingEnabled = True
        cmbBitDepths.Items.AddRange(New Object() {"16", "24", "32"})
        cmbBitDepths.Location = New Point(7, 196)
        cmbBitDepths.Name = "cmbBitDepths"
        cmbBitDepths.Size = New Size(150, 28)
        cmbBitDepths.TabIndex = 11
        ' 
        ' lblBitDepth
        ' 
        lblBitDepth.AutoSize = True
        lblBitDepth.BackColor = Color.FromArgb(CByte(192), CByte(255), CByte(255))
        lblBitDepth.Location = New Point(7, 173)
        lblBitDepth.Name = "lblBitDepth"
        lblBitDepth.Size = New Size(72, 20)
        lblBitDepth.TabIndex = 10
        lblBitDepth.Text = "Bit Depth"
        ' 
        ' cmbSampleRates
        ' 
        cmbSampleRates.FormattingEnabled = True
        cmbSampleRates.Items.AddRange(New Object() {"44100", "48000", "96000"})
        cmbSampleRates.Location = New Point(7, 142)
        cmbSampleRates.Name = "cmbSampleRates"
        cmbSampleRates.Size = New Size(150, 28)
        cmbSampleRates.TabIndex = 9
        ' 
        ' lblSampleRate
        ' 
        lblSampleRate.AutoSize = True
        lblSampleRate.BackColor = Color.FromArgb(CByte(192), CByte(255), CByte(255))
        lblSampleRate.Location = New Point(6, 119)
        lblSampleRate.Name = "lblSampleRate"
        lblSampleRate.Size = New Size(93, 20)
        lblSampleRate.TabIndex = 8
        lblSampleRate.Text = "Sample Rate"
        ' 
        ' cmbChannelMode
        ' 
        cmbChannelMode.FormattingEnabled = True
        cmbChannelMode.Location = New Point(6, 88)
        cmbChannelMode.Name = "cmbChannelMode"
        cmbChannelMode.Size = New Size(150, 28)
        cmbChannelMode.TabIndex = 7
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.BackColor = Color.FromArgb(CByte(192), CByte(255), CByte(255))
        Label1.Location = New Point(6, 65)
        Label1.Name = "Label1"
        Label1.Size = New Size(68, 20)
        Label1.TabIndex = 6
        Label1.Text = "Channels"
        ' 
        ' lblInputDevices
        ' 
        lblInputDevices.AutoSize = True
        lblInputDevices.BackColor = Color.FromArgb(CByte(192), CByte(255), CByte(255))
        lblInputDevices.Location = New Point(6, 11)
        lblInputDevices.Name = "lblInputDevices"
        lblInputDevices.Size = New Size(98, 20)
        lblInputDevices.TabIndex = 5
        lblInputDevices.Text = "Input Devices"
        ' 
        ' cmbInputDevices
        ' 
        cmbInputDevices.FormattingEnabled = True
        cmbInputDevices.Location = New Point(6, 34)
        cmbInputDevices.Name = "cmbInputDevices"
        cmbInputDevices.Size = New Size(164, 28)
        cmbInputDevices.TabIndex = 4
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
        progressPlayback.Size = New Size(1718, 40)
        progressPlayback.Style = ProgressBarStyle.Marquee
        progressPlayback.TabIndex = 0
        progressPlayback.Value = 1
        ' 
        ' TimerPlayback
        ' 
        TimerPlayback.Interval = 50
        ' 
        ' picWaveform
        ' 
        picWaveform.BackColor = Color.Black
        picWaveform.Dock = DockStyle.Fill
        picWaveform.Location = New Point(0, 40)
        picWaveform.Name = "picWaveform"
        picWaveform.Size = New Size(1718, 360)
        picWaveform.TabIndex = 1
        picWaveform.TabStop = False
        ' 
        ' TimerMeters
        ' 
        TimerMeters.Interval = 33
        ' 
        ' trackVolume
        ' 
        trackVolume.Location = New Point(6, 52)
        trackVolume.Maximum = 100
        trackVolume.Name = "trackVolume"
        trackVolume.Size = New Size(324, 56)
        trackVolume.TabIndex = 7
        trackVolume.TickFrequency = 10
        trackVolume.Value = 100
        ' 
        ' lblVolume
        ' 
        lblVolume.AutoSize = True
        lblVolume.Location = New Point(274, 29)
        lblVolume.Name = "lblVolume"
        lblVolume.Size = New Size(45, 20)
        lblVolume.TabIndex = 8
        lblVolume.Text = "100%"
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
        splitWaveformArea.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        splitWaveformArea.FixedPanel = FixedPanel.Panel1
        splitWaveformArea.IsSplitterFixed = True
        splitWaveformArea.Location = New Point(0, 130)
        splitWaveformArea.Name = "splitWaveformArea"
        ' 
        ' splitWaveformArea.Panel1
        ' 
        splitWaveformArea.Panel1.Controls.Add(meterRecording)
        ' 
        ' splitWaveformArea.Panel2
        ' 
        splitWaveformArea.Panel2.Controls.Add(picWaveform)
        splitWaveformArea.Panel2.Controls.Add(progressPlayback)
        splitWaveformArea.Size = New Size(1782, 400)
        splitWaveformArea.SplitterDistance = 60
        splitWaveformArea.TabIndex = 7
        ' 
        ' mainTabs
        ' 
        mainTabs.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
        mainTabs.Controls.Add(tabFiles)
        mainTabs.Controls.Add(tabProgram)
        mainTabs.Controls.Add(tabRecording)
        mainTabs.Controls.Add(tabAnalysis)
        mainTabs.Controls.Add(tabLogs)
        mainTabs.Location = New Point(0, 536)
        mainTabs.Multiline = True
        mainTabs.Name = "mainTabs"
        mainTabs.SelectedIndex = 0
        mainTabs.Size = New Size(350, 505)
        mainTabs.TabIndex = 8
        ' 
        ' tabFiles
        ' 
        tabFiles.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabFiles.Controls.Add(lstRecordings)
        tabFiles.Controls.Add(btnDelete)
        tabFiles.Controls.Add(trackVolume)
        tabFiles.Controls.Add(lblVolume)
        tabFiles.Location = New Point(4, 54)
        tabFiles.Name = "tabFiles"
        tabFiles.Padding = New Padding(3)
        tabFiles.Size = New Size(342, 447)
        tabFiles.TabIndex = 0
        tabFiles.Text = "📁 Files"
        ' 
        ' tabProgram
        ' 
        tabProgram.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabProgram.Controls.Add(cmbBufferSize)
        tabProgram.Controls.Add(cmbInputDevices)
        tabProgram.Controls.Add(Label2)
        tabProgram.Controls.Add(lblInputDevices)
        tabProgram.Controls.Add(cmbBitDepths)
        tabProgram.Controls.Add(Label1)
        tabProgram.Controls.Add(lblBitDepth)
        tabProgram.Controls.Add(cmbChannelMode)
        tabProgram.Controls.Add(cmbSampleRates)
        tabProgram.Controls.Add(lblSampleRate)
        tabProgram.Controls.Add(trackInputVolume)
        tabProgram.Controls.Add(lblInputVolume)
        tabProgram.Location = New Point(4, 54)
        tabProgram.Name = "tabProgram"
        tabProgram.Padding = New Padding(3)
        tabProgram.Size = New Size(342, 508)
        tabProgram.TabIndex = 1
        tabProgram.Text = "⚙️ Program"
        ' 
        ' trackInputVolume
        ' 
        trackInputVolume.Location = New Point(10, 300)
        trackInputVolume.Maximum = 200
        trackInputVolume.Name = "trackInputVolume"
        trackInputVolume.Size = New Size(250, 56)
        trackInputVolume.TabIndex = 20
        trackInputVolume.TickFrequency = 10
        trackInputVolume.Value = 100
        ' 
        ' lblInputVolume
        ' 
        lblInputVolume.AutoSize = True
        lblInputVolume.Location = New Point(10, 280)
        lblInputVolume.Name = "lblInputVolume"
        lblInputVolume.Size = New Size(140, 20)
        lblInputVolume.TabIndex = 21
        lblInputVolume.Text = "Input Volume: 100%"
        ' 
        ' tabRecording
        ' 
        tabRecording.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabRecording.Location = New Point(4, 54)
        tabRecording.Name = "tabRecording"
        tabRecording.Padding = New Padding(3)
        tabRecording.Size = New Size(342, 359)
        tabRecording.TabIndex = 2
        tabRecording.Text = "🎛️ Recording"
        ' 
        ' tabAnalysis
        ' 
        tabAnalysis.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        tabAnalysis.Location = New Point(4, 54)
        tabAnalysis.Name = "tabAnalysis"
        tabAnalysis.Padding = New Padding(3)
        tabAnalysis.Size = New Size(342, 508)
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
        tabLogs.Location = New Point(4, 54)
        tabLogs.Name = "tabLogs"
        tabLogs.Padding = New Padding(3)
        tabLogs.Size = New Size(342, 447)
        tabLogs.TabIndex = 4
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
        txtLogViewer.Size = New Size(330, 399)
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
        cmbLogLevel.Location = New Point(262, 6)
        cmbLogLevel.Name = "cmbLogLevel"
        cmbLogLevel.Size = New Size(75, 28)
        cmbLogLevel.TabIndex = 4
        cmbLogLevel.Text = "All"
        ' 
        ' lblLogLevel
        ' 
        lblLogLevel.AutoSize = True
        lblLogLevel.ForeColor = Color.White
        lblLogLevel.Location = New Point(183, 9)
        lblLogLevel.Name = "lblLogLevel"
        lblLogLevel.Size = New Size(75, 20)
        lblLogLevel.TabIndex = 5
        lblLogLevel.Text = "Log Level:"
        ' 
        ' MainForm
        ' 
        AutoScaleDimensions = New SizeF(8.0F, 20.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1782, 1053)
        Controls.Add(mainTabs)
        Controls.Add(splitWaveformArea)
        Controls.Add(transportControl)
        Controls.Add(lblRecordingTime)
        Name = "MainForm"
        Text = "DSP Processor - Dark Mode"
        CType(picWaveform, ComponentModel.ISupportInitialize).EndInit()
        CType(trackVolume, ComponentModel.ISupportInitialize).EndInit()
        splitWaveformArea.Panel1.ResumeLayout(False)
        splitWaveformArea.Panel2.ResumeLayout(False)
        CType(splitWaveformArea, ComponentModel.ISupportInitialize).EndInit()
        splitWaveformArea.ResumeLayout(False)
        mainTabs.ResumeLayout(False)
        tabFiles.ResumeLayout(False)
        tabFiles.PerformLayout()
        tabProgram.ResumeLayout(False)
        tabProgram.PerformLayout()
        CType(trackInputVolume, ComponentModel.ISupportInitialize).EndInit()
        tabLogs.ResumeLayout(False)
        tabLogs.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents TimerAudio As Timer
    Friend WithEvents btnStopPlayback As Button
    Friend WithEvents lblStatus As Label
    Friend WithEvents panelLED As Panel
    Friend WithEvents meterRecording As DSP_Processor.UI.VolumeMeterControl
    Friend WithEvents meterPlayback As DSP_Processor.UI.VolumeMeterControl
    Friend WithEvents cmbInputDevices As ComboBox
    Friend WithEvents lblInputDevices As Label
    Friend WithEvents cmbBitDepths As ComboBox
    Friend WithEvents lblBitDepth As Label
    Friend WithEvents cmbSampleRates As ComboBox
    Friend WithEvents lblSampleRate As Label
    Friend WithEvents cmbChannelMode As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents cmbBufferSize As ComboBox
    Friend WithEvents Label2 As Label
    Friend WithEvents TimerMeters As Timer
    Friend WithEvents lblRecordingTime As Label
    Friend WithEvents lstRecordings As ListBox
    Friend WithEvents progressPlayback As ProgressBar
    Friend WithEvents TimerPlayback As Timer
    Friend WithEvents picWaveform As PictureBox
    Friend WithEvents trackVolume As TrackBar
    Friend WithEvents lblVolume As Label
    Friend WithEvents btnDelete As Button
    Friend WithEvents transportControl As DSP_Processor.UI.TransportControl
    Friend WithEvents splitWaveformArea As SplitContainer
    Friend WithEvents mainTabs As TabControl
    Friend WithEvents tabFiles As TabPage
    Friend WithEvents tabProgram As TabPage
    Friend WithEvents tabRecording As TabPage
    Friend WithEvents tabAnalysis As TabPage
    Friend WithEvents tabLogs As TabPage
    Friend WithEvents txtLogViewer As RichTextBox
    Friend WithEvents btnClearLogs As Button
    Friend WithEvents btnSaveLogs As Button
    Friend WithEvents chkAutoScroll As CheckBox
    Friend WithEvents cmbLogLevel As ComboBox
    Friend WithEvents lblLogLevel As Label
    Friend WithEvents trackInputVolume As TrackBar
    Friend WithEvents lblInputVolume As Label

End Class
