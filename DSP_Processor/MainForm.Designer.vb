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
        btnRecord = New Button()
        btnStop = New Button()
        Panel1 = New Panel()
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
        panelLED = New Panel()
        lblStatus = New Label()
        lstRecordings = New ListBox()
        progressPlayback = New ProgressBar()
        TimerPlayback = New Timer(components)
        picWaveform = New PictureBox()
        Panel1.SuspendLayout()
        CType(picWaveform, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' TimerAudio
        ' 
        TimerAudio.Interval = 10
        ' 
        ' btnRecord
        ' 
        btnRecord.Location = New Point(3, 3)
        btnRecord.Name = "btnRecord"
        btnRecord.Size = New Size(151, 42)
        btnRecord.TabIndex = 0
        btnRecord.Text = "Record"
        btnRecord.UseVisualStyleBackColor = True
        ' 
        ' btnStop
        ' 
        btnStop.Location = New Point(3, 51)
        btnStop.Name = "btnStop"
        btnStop.Size = New Size(147, 41)
        btnStop.TabIndex = 1
        btnStop.Text = "Stop"
        btnStop.UseVisualStyleBackColor = True
        ' 
        ' Panel1
        ' 
        Panel1.Controls.Add(cmbBufferSize)
        Panel1.Controls.Add(Label2)
        Panel1.Controls.Add(cmbBitDepths)
        Panel1.Controls.Add(lblBitDepth)
        Panel1.Controls.Add(cmbSampleRates)
        Panel1.Controls.Add(lblSampleRate)
        Panel1.Controls.Add(cmbChannelMode)
        Panel1.Controls.Add(Label1)
        Panel1.Controls.Add(lblInputDevices)
        Panel1.Controls.Add(cmbInputDevices)
        Panel1.Controls.Add(panelLED)
        Panel1.Controls.Add(lblStatus)
        Panel1.Controls.Add(btnRecord)
        Panel1.Controls.Add(btnStop)
        Panel1.Location = New Point(12, 12)
        Panel1.Name = "Panel1"
        Panel1.Size = New Size(378, 601)
        Panel1.TabIndex = 2
        ' 
        ' cmbBufferSize
        ' 
        cmbBufferSize.FormattingEnabled = True
        cmbBufferSize.Items.AddRange(New Object() {"5", "10", "20", "50"})
        cmbBufferSize.Location = New Point(6, 501)
        cmbBufferSize.Name = "cmbBufferSize"
        cmbBufferSize.Size = New Size(369, 28)
        cmbBufferSize.TabIndex = 13
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(7, 473)
        Label2.Name = "Label2"
        Label2.Size = New Size(80, 20)
        Label2.TabIndex = 12
        Label2.Text = "Buffer Size"
        ' 
        ' cmbBitDepths
        ' 
        cmbBitDepths.FormattingEnabled = True
        cmbBitDepths.Items.AddRange(New Object() {"16", "24", "32"})
        cmbBitDepths.Location = New Point(6, 422)
        cmbBitDepths.Name = "cmbBitDepths"
        cmbBitDepths.Size = New Size(369, 28)
        cmbBitDepths.TabIndex = 11
        ' 
        ' lblBitDepth
        ' 
        lblBitDepth.AutoSize = True
        lblBitDepth.Location = New Point(7, 397)
        lblBitDepth.Name = "lblBitDepth"
        lblBitDepth.Size = New Size(72, 20)
        lblBitDepth.TabIndex = 10
        lblBitDepth.Text = "Bit Depth"
        ' 
        ' cmbSampleRates
        ' 
        cmbSampleRates.FormattingEnabled = True
        cmbSampleRates.Items.AddRange(New Object() {"44100", "48000", "96000"})
        cmbSampleRates.Location = New Point(6, 349)
        cmbSampleRates.Name = "cmbSampleRates"
        cmbSampleRates.Size = New Size(369, 28)
        cmbSampleRates.TabIndex = 9
        ' 
        ' lblSampleRate
        ' 
        lblSampleRate.AutoSize = True
        lblSampleRate.Location = New Point(13, 312)
        lblSampleRate.Name = "lblSampleRate"
        lblSampleRate.Size = New Size(93, 20)
        lblSampleRate.TabIndex = 8
        lblSampleRate.Text = "Sample Rate"
        ' 
        ' cmbChannelMode
        ' 
        cmbChannelMode.FormattingEnabled = True
        cmbChannelMode.Location = New Point(3, 267)
        cmbChannelMode.Name = "cmbChannelMode"
        cmbChannelMode.Size = New Size(372, 28)
        cmbChannelMode.TabIndex = 7
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(8, 240)
        Label1.Name = "Label1"
        Label1.Size = New Size(68, 20)
        Label1.TabIndex = 6
        Label1.Text = "Channels"
        ' 
        ' lblInputDevices
        ' 
        lblInputDevices.AutoSize = True
        lblInputDevices.Location = New Point(3, 176)
        lblInputDevices.Name = "lblInputDevices"
        lblInputDevices.Size = New Size(98, 20)
        lblInputDevices.TabIndex = 5
        lblInputDevices.Text = "Input Devices"
        ' 
        ' cmbInputDevices
        ' 
        cmbInputDevices.FormattingEnabled = True
        cmbInputDevices.Location = New Point(3, 199)
        cmbInputDevices.Name = "cmbInputDevices"
        cmbInputDevices.Size = New Size(372, 28)
        cmbInputDevices.TabIndex = 4
        ' 
        ' panelLED
        ' 
        panelLED.BackColor = Color.Lime
        panelLED.BorderStyle = BorderStyle.Fixed3D
        panelLED.Location = New Point(24, 147)
        panelLED.Name = "panelLED"
        panelLED.Size = New Size(20, 20)
        panelLED.TabIndex = 3
        ' 
        ' lblStatus
        ' 
        lblStatus.AutoSize = True
        lblStatus.Location = New Point(13, 113)
        lblStatus.Name = "lblStatus"
        lblStatus.Size = New Size(49, 20)
        lblStatus.TabIndex = 2
        lblStatus.Text = "Status"
        ' 
        ' lstRecordings
        ' 
        lstRecordings.FormattingEnabled = True
        lstRecordings.Location = New Point(396, 29)
        lstRecordings.Name = "lstRecordings"
        lstRecordings.ScrollAlwaysVisible = True
        lstRecordings.Size = New Size(346, 584)
        lstRecordings.TabIndex = 3
        ' 
        ' progressPlayback
        ' 
        progressPlayback.Location = New Point(796, 39)
        progressPlayback.Maximum = 1000
        progressPlayback.Name = "progressPlayback"
        progressPlayback.Size = New Size(849, 43)
        progressPlayback.Style = ProgressBarStyle.Marquee
        progressPlayback.TabIndex = 4
        progressPlayback.Value = 1
        ' 
        ' TimerPlayback
        ' 
        TimerPlayback.Interval = 50
        ' 
        ' picWaveform
        ' 
        picWaveform.BackColor = Color.Black
        picWaveform.Location = New Point(798, 97)
        picWaveform.Name = "picWaveform"
        picWaveform.Size = New Size(847, 367)
        picWaveform.TabIndex = 5
        picWaveform.TabStop = False
        ' 
        ' MainForm
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1723, 651)
        Controls.Add(picWaveform)
        Controls.Add(progressPlayback)
        Controls.Add(lstRecordings)
        Controls.Add(Panel1)
        Name = "MainForm"
        Text = "Form1"
        Panel1.ResumeLayout(False)
        Panel1.PerformLayout()
        CType(picWaveform, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents TimerAudio As Timer
    Friend WithEvents btnRecord As Button
    Friend WithEvents btnStop As Button
    Friend WithEvents Panel1 As Panel
    Friend WithEvents lblStatus As Label
    Friend WithEvents lstRecordings As ListBox
    Friend WithEvents panelLED As Panel
    Friend WithEvents progressPlayback As ProgressBar
    Friend WithEvents TimerPlayback As Timer
    Friend WithEvents picWaveform As PictureBox
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

End Class
