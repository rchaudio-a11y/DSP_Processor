<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class DSPSignalFlowPanel
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
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
        TableLayoutPanel1 = New TableLayoutPanel()
        panelInputMeters = New Panel()
        lblInputTitle = New Label()
        groupGainPan = New GroupBox()
        lblGainValue = New Label()
        lblGain = New Label()
        trackGain = New TrackBar()
        lblPanValue = New Label()
        lblPan = New Label()
        trackPan = New TrackBar()
        groupHighPass = New GroupBox()
        chkHPFEnable = New CheckBox()
        lblHPFFreqValue = New Label()
        lblHPFFreq = New Label()
        trackHPFFreq = New TrackBar()
        lblHPFDesc = New Label()
        groupLowPass = New GroupBox()
        chkLPFEnable = New CheckBox()
        lblLPFFreqValue = New Label()
        lblLPFFreq = New Label()
        trackLPFFreq = New TrackBar()
        lblLPFDesc = New Label()
        groupOutputMixer = New GroupBox()
        lblMasterValue = New Label()
        lblMaster = New Label()
        trackMaster = New TrackBar()
        lblWidthValue = New Label()
        lblWidth = New Label()
        trackWidth = New TrackBar()
        panelOutputMeters = New Panel()
        lblOutputTitle = New Label()
        TableLayoutPanel1.SuspendLayout()
        panelInputMeters.SuspendLayout()
        groupGainPan.SuspendLayout()
        CType(trackGain, ComponentModel.ISupportInitialize).BeginInit()
        CType(trackPan, ComponentModel.ISupportInitialize).BeginInit()
        groupHighPass.SuspendLayout()
        CType(trackHPFFreq, ComponentModel.ISupportInitialize).BeginInit()
        groupLowPass.SuspendLayout()
        CType(trackLPFFreq, ComponentModel.ISupportInitialize).BeginInit()
        groupOutputMixer.SuspendLayout()
        CType(trackMaster, ComponentModel.ISupportInitialize).BeginInit()
        CType(trackWidth, ComponentModel.ISupportInitialize).BeginInit()
        panelOutputMeters.SuspendLayout()
        SuspendLayout()
        ' 
        ' TableLayoutPanel1
        ' 
        TableLayoutPanel1.BackColor = Color.FromArgb(CByte(64), CByte(64), CByte(64))
        TableLayoutPanel1.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        TableLayoutPanel1.ColumnCount = 3
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 15.0F))
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 70.0F))
        TableLayoutPanel1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 15.0F))
        TableLayoutPanel1.Controls.Add(panelInputMeters, 0, 0)
        TableLayoutPanel1.Controls.Add(groupGainPan, 1, 1)
        TableLayoutPanel1.Controls.Add(groupHighPass, 1, 2)
        TableLayoutPanel1.Controls.Add(groupLowPass, 1, 3)
        TableLayoutPanel1.Controls.Add(groupOutputMixer, 1, 4)
        TableLayoutPanel1.Controls.Add(panelOutputMeters, 2, 0)
        TableLayoutPanel1.Dock = DockStyle.Fill
        TableLayoutPanel1.Location = New Point(0, 0)
        TableLayoutPanel1.Name = "TableLayoutPanel1"
        TableLayoutPanel1.Padding = New Padding(10)
        TableLayoutPanel1.RowCount = 7
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 10.0F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 30.0F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 20.0F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 20.0F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Percent, 30.0F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 10.0F))
        TableLayoutPanel1.RowStyles.Add(New RowStyle(SizeType.Absolute, 10.0F))
        TableLayoutPanel1.Size = New Size(779, 800)
        TableLayoutPanel1.TabIndex = 0
        ' 
        ' panelInputMeters
        ' 
        panelInputMeters.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        panelInputMeters.Controls.Add(lblInputTitle)
        panelInputMeters.Dock = DockStyle.Fill
        panelInputMeters.Location = New Point(14, 14)
        panelInputMeters.Name = "panelInputMeters"
        TableLayoutPanel1.SetRowSpan(panelInputMeters, 7)
        panelInputMeters.Size = New Size(107, 772)
        panelInputMeters.TabIndex = 0
        ' 
        ' lblInputTitle
        ' 
        lblInputTitle.Dock = DockStyle.Top
        lblInputTitle.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        lblInputTitle.ForeColor = Color.White
        lblInputTitle.Location = New Point(0, 0)
        lblInputTitle.Name = "lblInputTitle"
        lblInputTitle.Size = New Size(107, 25)
        lblInputTitle.TabIndex = 0
        lblInputTitle.Text = "INPUT (L/R)"
        lblInputTitle.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' groupGainPan
        ' 
        groupGainPan.Controls.Add(lblGainValue)
        groupGainPan.Controls.Add(lblGain)
        groupGainPan.Controls.Add(trackGain)
        groupGainPan.Controls.Add(lblPanValue)
        groupGainPan.Controls.Add(lblPan)
        groupGainPan.Controls.Add(trackPan)
        groupGainPan.Dock = DockStyle.Fill
        groupGainPan.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        groupGainPan.ForeColor = Color.White
        groupGainPan.Location = New Point(128, 25)
        groupGainPan.Name = "groupGainPan"
        groupGainPan.Size = New Size(522, 216)
        groupGainPan.TabIndex = 1
        groupGainPan.TabStop = False
        groupGainPan.Text = "GAIN / PAN"
        ' 
        ' lblGainValue
        ' 
        lblGainValue.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        lblGainValue.Font = New Font("Segoe UI", 9.0F)
        lblGainValue.Location = New Point(377, 25)
        lblGainValue.Name = "lblGainValue"
        lblGainValue.Size = New Size(60, 20)
        lblGainValue.TabIndex = 5
        lblGainValue.Text = "0.0 dB"
        lblGainValue.TextAlign = ContentAlignment.TopRight
        ' 
        ' lblGain
        ' 
        lblGain.AutoSize = True
        lblGain.Font = New Font("Segoe UI", 9.0F)
        lblGain.Location = New Point(10, 25)
        lblGain.Name = "lblGain"
        lblGain.Size = New Size(42, 20)
        lblGain.TabIndex = 4
        lblGain.Text = "Gain:"
        ' 
        ' trackGain
        ' 
        trackGain.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        trackGain.Location = New Point(6, 48)
        trackGain.Maximum = 120
        trackGain.Minimum = -600
        trackGain.Name = "trackGain"
        trackGain.Size = New Size(502, 56)
        trackGain.TabIndex = 0
        trackGain.TickFrequency = 60
        ' 
        ' lblPanValue
        ' 
        lblPanValue.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        lblPanValue.Font = New Font("Segoe UI", 9.0F)
        lblPanValue.Location = New Point(230, 131)
        lblPanValue.Name = "lblPanValue"
        lblPanValue.Size = New Size(60, 20)
        lblPanValue.TabIndex = 3
        lblPanValue.Text = "Center"
        lblPanValue.TextAlign = ContentAlignment.TopRight
        ' 
        ' lblPan
        ' 
        lblPan.AutoSize = True
        lblPan.Font = New Font("Segoe UI", 9.0F)
        lblPan.Location = New Point(10, 131)
        lblPan.Name = "lblPan"
        lblPan.Size = New Size(35, 20)
        lblPan.TabIndex = 2
        lblPan.Text = "Pan:"
        ' 
        ' trackPan
        ' 
        trackPan.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        trackPan.Location = New Point(6, 154)
        trackPan.Maximum = 100
        trackPan.Minimum = -100
        trackPan.Name = "trackPan"
        trackPan.Size = New Size(502, 56)
        trackPan.TabIndex = 1
        trackPan.TickFrequency = 25
        ' 
        ' groupHighPass
        ' 
        groupHighPass.Controls.Add(chkHPFEnable)
        groupHighPass.Controls.Add(lblHPFFreqValue)
        groupHighPass.Controls.Add(lblHPFFreq)
        groupHighPass.Controls.Add(trackHPFFreq)
        groupHighPass.Controls.Add(lblHPFDesc)
        groupHighPass.Dock = DockStyle.Fill
        groupHighPass.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        groupHighPass.ForeColor = Color.White
        groupHighPass.Location = New Point(128, 248)
        groupHighPass.Name = "groupHighPass"
        groupHighPass.Size = New Size(522, 142)
        groupHighPass.TabIndex = 2
        groupHighPass.TabStop = False
        groupHighPass.Text = "HIGH-PASS FILTER"
        ' 
        ' chkHPFEnable
        ' 
        chkHPFEnable.AutoSize = True
        chkHPFEnable.Checked = True
        chkHPFEnable.CheckState = CheckState.Checked
        chkHPFEnable.Font = New Font("Segoe UI", 9.0F)
        chkHPFEnable.Location = New Point(10, 28)
        chkHPFEnable.Name = "chkHPFEnable"
        chkHPFEnable.Size = New Size(76, 24)
        chkHPFEnable.TabIndex = 0
        chkHPFEnable.Text = "Enable"
        chkHPFEnable.UseVisualStyleBackColor = True
        ' 
        ' lblHPFFreqValue
        ' 
        lblHPFFreqValue.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        lblHPFFreqValue.Font = New Font("Segoe UI", 9.0F)
        lblHPFFreqValue.Location = New Point(452, 55)
        lblHPFFreqValue.Name = "lblHPFFreqValue"
        lblHPFFreqValue.Size = New Size(60, 20)
        lblHPFFreqValue.TabIndex = 4
        lblHPFFreqValue.Text = "30 Hz"
        lblHPFFreqValue.TextAlign = ContentAlignment.TopRight
        ' 
        ' lblHPFFreq
        ' 
        lblHPFFreq.AutoSize = True
        lblHPFFreq.Font = New Font("Segoe UI", 9.0F)
        lblHPFFreq.Location = New Point(10, 55)
        lblHPFFreq.Name = "lblHPFFreq"
        lblHPFFreq.Size = New Size(79, 20)
        lblHPFFreq.TabIndex = 3
        lblHPFFreq.Text = "Frequency:"
        ' 
        ' trackHPFFreq
        ' 
        trackHPFFreq.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        trackHPFFreq.Location = New Point(10, 78)
        trackHPFFreq.Maximum = 180
        trackHPFFreq.Minimum = 30
        trackHPFFreq.Name = "trackHPFFreq"
        trackHPFFreq.Size = New Size(502, 56)
        trackHPFFreq.TabIndex = 1
        trackHPFFreq.TickFrequency = 30
        trackHPFFreq.Value = 30
        ' 
        ' lblHPFDesc
        ' 
        lblHPFDesc.AutoSize = True
        lblHPFDesc.Font = New Font("Segoe UI", 8.0F, FontStyle.Italic)
        lblHPFDesc.ForeColor = Color.FromArgb(CByte(180), CByte(180), CByte(180))
        lblHPFDesc.Location = New Point(100, 30)
        lblHPFDesc.Name = "lblHPFDesc"
        lblHPFDesc.Size = New Size(224, 19)
        lblHPFDesc.TabIndex = 2
        lblHPFDesc.Text = "(Removes DC, rumble, stage noise)"
        ' 
        ' groupLowPass
        ' 
        groupLowPass.Controls.Add(chkLPFEnable)
        groupLowPass.Controls.Add(lblLPFFreqValue)
        groupLowPass.Controls.Add(lblLPFFreq)
        groupLowPass.Controls.Add(trackLPFFreq)
        groupLowPass.Controls.Add(lblLPFDesc)
        groupLowPass.Dock = DockStyle.Fill
        groupLowPass.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        groupLowPass.ForeColor = Color.White
        groupLowPass.Location = New Point(128, 397)
        groupLowPass.Name = "groupLowPass"
        groupLowPass.Size = New Size(522, 142)
        groupLowPass.TabIndex = 3
        groupLowPass.TabStop = False
        groupLowPass.Text = "LOW-PASS FILTER"
        ' 
        ' chkLPFEnable
        ' 
        chkLPFEnable.AutoSize = True
        chkLPFEnable.Font = New Font("Segoe UI", 9.0F)
        chkLPFEnable.Location = New Point(10, 28)
        chkLPFEnable.Name = "chkLPFEnable"
        chkLPFEnable.Size = New Size(76, 24)
        chkLPFEnable.TabIndex = 0
        chkLPFEnable.Text = "Enable"
        chkLPFEnable.UseVisualStyleBackColor = True
        ' 
        ' lblLPFFreqValue
        ' 
        lblLPFFreqValue.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        lblLPFFreqValue.Font = New Font("Segoe UI", 9.0F)
        lblLPFFreqValue.Location = New Point(452, 55)
        lblLPFFreqValue.Name = "lblLPFFreqValue"
        lblLPFFreqValue.Size = New Size(60, 20)
        lblLPFFreqValue.TabIndex = 4
        lblLPFFreqValue.Text = "18 kHz"
        lblLPFFreqValue.TextAlign = ContentAlignment.TopRight
        ' 
        ' lblLPFFreq
        ' 
        lblLPFFreq.AutoSize = True
        lblLPFFreq.Font = New Font("Segoe UI", 9.0F)
        lblLPFFreq.Location = New Point(10, 55)
        lblLPFFreq.Name = "lblLPFFreq"
        lblLPFFreq.Size = New Size(79, 20)
        lblLPFFreq.TabIndex = 3
        lblLPFFreq.Text = "Frequency:"
        ' 
        ' trackLPFFreq
        ' 
        trackLPFFreq.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        trackLPFFreq.Location = New Point(10, 78)
        trackLPFFreq.Maximum = 12000  ' 12 kHz
        trackLPFFreq.Minimum = 4000   ' 4 kHz
        trackLPFFreq.Name = "trackLPFFreq"
        trackLPFFreq.Size = New Size(502, 56)
        trackLPFFreq.TabIndex = 1
        trackLPFFreq.TickFrequency = 1000  ' 1 kHz ticks
        trackLPFFreq.Value = 8000  ' Default 8 kHz
        ' 
        ' lblLPFDesc
        ' 
        lblLPFDesc.AutoSize = True
        lblLPFDesc.Font = New Font("Segoe UI", 8.0F, FontStyle.Italic)
        lblLPFDesc.ForeColor = Color.FromArgb(CByte(180), CByte(180), CByte(180))
        lblLPFDesc.Location = New Point(100, 30)
        lblLPFDesc.Name = "lblLPFDesc"
        lblLPFDesc.Size = New Size(149, 19)
        lblLPFDesc.TabIndex = 2
        lblLPFDesc.Text = "(Gentle hiss reduction)"
        ' 
        ' groupOutputMixer
        ' 
        groupOutputMixer.Controls.Add(lblMasterValue)
        groupOutputMixer.Controls.Add(lblMaster)
        groupOutputMixer.Controls.Add(trackMaster)
        groupOutputMixer.Controls.Add(lblWidthValue)
        groupOutputMixer.Controls.Add(lblWidth)
        groupOutputMixer.Controls.Add(trackWidth)
        groupOutputMixer.Dock = DockStyle.Fill
        groupOutputMixer.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        groupOutputMixer.ForeColor = Color.White
        groupOutputMixer.Location = New Point(128, 546)
        groupOutputMixer.Name = "groupOutputMixer"
        groupOutputMixer.Size = New Size(522, 216)
        groupOutputMixer.TabIndex = 4
        groupOutputMixer.TabStop = False
        groupOutputMixer.Text = "OUTPUT MIXER"
        ' 
        ' lblMasterValue
        ' 
        lblMasterValue.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        lblMasterValue.Font = New Font("Segoe UI", 9.0F)
        lblMasterValue.Location = New Point(389, 25)
        lblMasterValue.Name = "lblMasterValue"
        lblMasterValue.Size = New Size(60, 20)
        lblMasterValue.TabIndex = 5
        lblMasterValue.Text = "0.0 dB"
        lblMasterValue.TextAlign = ContentAlignment.TopRight
        ' 
        ' lblMaster
        ' 
        lblMaster.AutoSize = True
        lblMaster.Font = New Font("Segoe UI", 9.0F)
        lblMaster.Location = New Point(10, 25)
        lblMaster.Name = "lblMaster"
        lblMaster.Size = New Size(57, 20)
        lblMaster.TabIndex = 4
        lblMaster.Text = "Master:"
        ' 
        ' trackMaster
        ' 
        trackMaster.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        trackMaster.Location = New Point(10, 48)
        trackMaster.Maximum = 120
        trackMaster.Minimum = -600
        trackMaster.Name = "trackMaster"
        trackMaster.Size = New Size(502, 56)
        trackMaster.TabIndex = 0
        trackMaster.TickFrequency = 60
        ' 
        ' lblWidthValue
        ' 
        lblWidthValue.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        lblWidthValue.Font = New Font("Segoe UI", 9.0F)
        lblWidthValue.Location = New Point(452, 131)
        lblWidthValue.Name = "lblWidthValue"
        lblWidthValue.Size = New Size(60, 20)
        lblWidthValue.TabIndex = 3
        lblWidthValue.Text = "100%"
        lblWidthValue.TextAlign = ContentAlignment.TopRight
        ' 
        ' lblWidth
        ' 
        lblWidth.AutoSize = True
        lblWidth.Font = New Font("Segoe UI", 9.0F)
        lblWidth.Location = New Point(6, 131)
        lblWidth.Name = "lblWidth"
        lblWidth.Size = New Size(52, 20)
        lblWidth.TabIndex = 2
        lblWidth.Text = "Width:"
        ' 
        ' trackWidth
        ' 
        trackWidth.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        trackWidth.Location = New Point(10, 154)
        trackWidth.Maximum = 200
        trackWidth.Name = "trackWidth"
        trackWidth.Size = New Size(502, 56)
        trackWidth.TabIndex = 1
        trackWidth.TickFrequency = 25
        trackWidth.Value = 100
        ' 
        ' panelOutputMeters
        ' 
        panelOutputMeters.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        panelOutputMeters.Controls.Add(lblOutputTitle)
        panelOutputMeters.Dock = DockStyle.Fill
        panelOutputMeters.Location = New Point(657, 14)
        panelOutputMeters.Name = "panelOutputMeters"
        TableLayoutPanel1.SetRowSpan(panelOutputMeters, 7)
        panelOutputMeters.Size = New Size(108, 772)
        panelOutputMeters.TabIndex = 5
        ' 
        ' lblOutputTitle
        ' 
        lblOutputTitle.Dock = DockStyle.Top
        lblOutputTitle.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        lblOutputTitle.ForeColor = Color.White
        lblOutputTitle.Location = New Point(0, 0)
        lblOutputTitle.Name = "lblOutputTitle"
        lblOutputTitle.Size = New Size(108, 30)
        lblOutputTitle.TabIndex = 0
        lblOutputTitle.Text = "OUTPUT METERS (L/R)"
        lblOutputTitle.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' DSPSignalFlowPanel
        ' 
        AutoScaleDimensions = New SizeF(8.0F, 20.0F)
        AutoScaleMode = AutoScaleMode.Font
        AutoScroll = True
        BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        Controls.Add(TableLayoutPanel1)
        Name = "DSPSignalFlowPanel"
        Size = New Size(779, 800)
        TableLayoutPanel1.ResumeLayout(False)
        panelInputMeters.ResumeLayout(False)
        groupGainPan.ResumeLayout(False)
        groupGainPan.PerformLayout()
        CType(trackGain, ComponentModel.ISupportInitialize).EndInit()
        CType(trackPan, ComponentModel.ISupportInitialize).EndInit()
        groupHighPass.ResumeLayout(False)
        groupHighPass.PerformLayout()
        CType(trackHPFFreq, ComponentModel.ISupportInitialize).EndInit()
        groupLowPass.ResumeLayout(False)
        groupLowPass.PerformLayout()
        CType(trackLPFFreq, ComponentModel.ISupportInitialize).EndInit()
        groupOutputMixer.ResumeLayout(False)
        groupOutputMixer.PerformLayout()
        CType(trackMaster, ComponentModel.ISupportInitialize).EndInit()
        CType(trackWidth, ComponentModel.ISupportInitialize).EndInit()
        panelOutputMeters.ResumeLayout(False)
        ResumeLayout(False)
    End Sub

    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents panelInputMeters As Panel
    Friend WithEvents lblInputTitle As Label
    Friend WithEvents groupGainPan As GroupBox
    Friend WithEvents lblGainValue As Label
    Friend WithEvents lblGain As Label
    Friend WithEvents trackGain As TrackBar
    Friend WithEvents lblPanValue As Label
    Friend WithEvents lblPan As Label
    Friend WithEvents trackPan As TrackBar
    Friend WithEvents groupHighPass As GroupBox
    Friend WithEvents chkHPFEnable As CheckBox
    Friend WithEvents lblHPFFreqValue As Label
    Friend WithEvents lblHPFFreq As Label
    Friend WithEvents trackHPFFreq As TrackBar
    Friend WithEvents lblHPFDesc As Label
    Friend WithEvents groupLowPass As GroupBox
    Friend WithEvents chkLPFEnable As CheckBox
    Friend WithEvents lblLPFFreqValue As Label
    Friend WithEvents lblLPFFreq As Label
    Friend WithEvents trackLPFFreq As TrackBar
    Friend WithEvents lblLPFDesc As Label
    Friend WithEvents groupOutputMixer As GroupBox
    Friend WithEvents lblMasterValue As Label
    Friend WithEvents lblMaster As Label
    Friend WithEvents trackMaster As TrackBar
    Friend WithEvents lblWidthValue As Label
    Friend WithEvents lblWidth As Label
    Friend WithEvents trackWidth As TrackBar
    Friend WithEvents panelOutputMeters As Panel
    Friend WithEvents lblOutputTitle As Label

End Class
