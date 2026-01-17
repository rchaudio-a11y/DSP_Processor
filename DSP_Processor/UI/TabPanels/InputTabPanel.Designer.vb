Imports DSP_Processor.Models
Imports DSP_Processor.UI

Namespace UI.TabPanels

    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class InputTabPanel
        Inherits System.Windows.Forms.UserControl

        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub

        Private components As System.ComponentModel.IContainer

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me.tableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.Label1 = New System.Windows.Forms.Label()
            Me.trackInputVolume = New System.Windows.Forms.TrackBar()
            Me.lblInputVolumeValue = New System.Windows.Forms.Label()
            Me.chkLinkToPlayback = New System.Windows.Forms.CheckBox()
            Me.Label2 = New System.Windows.Forms.Label()
            Me.lblPeakHold = New System.Windows.Forms.Label()
            Me.cmbPeakHold = New System.Windows.Forms.ComboBox()
            Me.lblPeakDecay = New System.Windows.Forms.Label()
            Me.cmbPeakDecay = New System.Windows.Forms.ComboBox()
            Me.Label3 = New System.Windows.Forms.Label()
            Me.lblRmsWindow = New System.Windows.Forms.Label()
            Me.cmbRmsWindow = New System.Windows.Forms.ComboBox()
            Me.lblAttack = New System.Windows.Forms.Label()
            Me.cmbAttack = New System.Windows.Forms.ComboBox()
            Me.lblRelease = New System.Windows.Forms.Label()
            Me.cmbRelease = New System.Windows.Forms.ComboBox()
            Me.Label4 = New System.Windows.Forms.Label()
            Me.lblClipThreshold = New System.Windows.Forms.Label()
            Me.cmbClipThreshold = New System.Windows.Forms.ComboBox()
            Me.lblClipHold = New System.Windows.Forms.Label()
            Me.cmbClipHold = New System.Windows.Forms.ComboBox()
            Me.Label5 = New System.Windows.Forms.Label()
            Me.btnFastResponse = New System.Windows.Forms.Button()
            Me.btnSlowResponse = New System.Windows.Forms.Button()
            Me.btnBroadcast = New System.Windows.Forms.Button()
            Me.btnReset = New System.Windows.Forms.Button()
            Me.tableLayoutPanel.SuspendLayout()
            CType(Me.trackInputVolume, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            'tableLayoutPanel
            '
            Me.tableLayoutPanel.AutoScroll = True
            Me.tableLayoutPanel.ColumnCount = 2
            Me.tableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130.0!))
            Me.tableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.tableLayoutPanel.Controls.Add(Me.Label1, 0, 0)
            Me.tableLayoutPanel.Controls.Add(Me.trackInputVolume, 0, 1)
            Me.tableLayoutPanel.Controls.Add(Me.lblInputVolumeValue, 1, 1)
            Me.tableLayoutPanel.Controls.Add(Me.chkLinkToPlayback, 0, 2)
            Me.tableLayoutPanel.Controls.Add(Me.Label2, 0, 3)
            Me.tableLayoutPanel.Controls.Add(Me.lblPeakHold, 0, 4)
            Me.tableLayoutPanel.Controls.Add(Me.cmbPeakHold, 1, 4)
            Me.tableLayoutPanel.Controls.Add(Me.lblPeakDecay, 0, 5)
            Me.tableLayoutPanel.Controls.Add(Me.cmbPeakDecay, 1, 5)
            Me.tableLayoutPanel.Controls.Add(Me.Label3, 0, 6)
            Me.tableLayoutPanel.Controls.Add(Me.lblRmsWindow, 0, 7)
            Me.tableLayoutPanel.Controls.Add(Me.cmbRmsWindow, 1, 7)
            Me.tableLayoutPanel.Controls.Add(Me.lblAttack, 0, 8)
            Me.tableLayoutPanel.Controls.Add(Me.cmbAttack, 1, 8)
            Me.tableLayoutPanel.Controls.Add(Me.lblRelease, 0, 9)
            Me.tableLayoutPanel.Controls.Add(Me.cmbRelease, 1, 9)
            Me.tableLayoutPanel.Controls.Add(Me.Label4, 0, 10)
            Me.tableLayoutPanel.Controls.Add(Me.lblClipThreshold, 0, 11)
            Me.tableLayoutPanel.Controls.Add(Me.cmbClipThreshold, 1, 11)
            Me.tableLayoutPanel.Controls.Add(Me.lblClipHold, 0, 12)
            Me.tableLayoutPanel.Controls.Add(Me.cmbClipHold, 1, 12)
            Me.tableLayoutPanel.Controls.Add(Me.Label5, 0, 13)
            Me.tableLayoutPanel.Controls.Add(Me.btnFastResponse, 0, 14)
            Me.tableLayoutPanel.Controls.Add(Me.btnSlowResponse, 1, 14)
            Me.tableLayoutPanel.Controls.Add(Me.btnBroadcast, 0, 15)
            Me.tableLayoutPanel.Controls.Add(Me.btnReset, 1, 15)
            Me.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill
            Me.tableLayoutPanel.Location = New System.Drawing.Point(0, 0)
            Me.tableLayoutPanel.Name = "tableLayoutPanel"
            Me.tableLayoutPanel.Padding = New System.Windows.Forms.Padding(10)
            Me.tableLayoutPanel.RowCount = 16
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
            Me.tableLayoutPanel.Size = New System.Drawing.Size(360, 670)
            Me.tableLayoutPanel.TabIndex = 0
            '
            'Label1
            '
            Me.Label1.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.Label1.AutoSize = True
            Me.tableLayoutPanel.SetColumnSpan(Me.Label1, 2)
            Me.Label1.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Bold)
            Me.Label1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(200, Byte), Integer), CType(CType(100, Byte), Integer))
            Me.Label1.Location = New System.Drawing.Point(13, 15)
            Me.Label1.Name = "Label1"
            Me.Label1.Size = New System.Drawing.Size(111, 23)
            Me.Label1.TabIndex = 0
            Me.Label1.Text = "Input Volume"
            '
            'trackInputVolume
            '
            Me.trackInputVolume.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.trackInputVolume.Location = New System.Drawing.Point(13, 42)
            Me.trackInputVolume.Maximum = 200
            Me.trackInputVolume.Name = "trackInputVolume"
            Me.trackInputVolume.Size = New System.Drawing.Size(124, 56)
            Me.trackInputVolume.TabIndex = 1
            Me.trackInputVolume.TickFrequency = 25
            Me.trackInputVolume.Value = 100
            '
            'lblInputVolumeValue
            '
            Me.lblInputVolumeValue.Anchor = System.Windows.Forms.AnchorStyles.Right
            Me.lblInputVolumeValue.AutoSize = True
            Me.lblInputVolumeValue.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
            Me.lblInputVolumeValue.ForeColor = System.Drawing.Color.Cyan
            Me.lblInputVolumeValue.Location = New System.Drawing.Point(277, 58)
            Me.lblInputVolumeValue.Name = "lblInputVolumeValue"
            Me.lblInputVolumeValue.Size = New System.Drawing.Size(70, 28)
            Me.lblInputVolumeValue.TabIndex = 2
            Me.lblInputVolumeValue.Text = "100%"
            '
            'chkLinkToPlayback
            '
            Me.chkLinkToPlayback.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.chkLinkToPlayback.AutoSize = True
            Me.tableLayoutPanel.SetColumnSpan(Me.chkLinkToPlayback, 2)
            Me.chkLinkToPlayback.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.chkLinkToPlayback.ForeColor = System.Drawing.Color.White
            Me.chkLinkToPlayback.Location = New System.Drawing.Point(13, 95)
            Me.chkLinkToPlayback.Name = "chkLinkToPlayback"
            Me.chkLinkToPlayback.Size = New System.Drawing.Size(179, 24)
            Me.chkLinkToPlayback.TabIndex = 3
            Me.chkLinkToPlayback.Text = "Link to Playback Volume"
            Me.chkLinkToPlayback.UseVisualStyleBackColor = True
            '
            'Label2
            '
            Me.Label2.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.Label2.AutoSize = True
            Me.tableLayoutPanel.SetColumnSpan(Me.Label2, 2)
            Me.Label2.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Bold)
            Me.Label2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(200, Byte), Integer), CType(CType(100, Byte), Integer))
            Me.Label2.Location = New System.Drawing.Point(13, 138)
            Me.Label2.Name = "Label2"
            Me.Label2.Size = New System.Drawing.Size(123, 23)
            Me.Label2.TabIndex = 4
            Me.Label2.Text = "Peak Behavior"
            '
            'lblPeakHold
            '
            Me.lblPeakHold.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblPeakHold.AutoSize = True
            Me.lblPeakHold.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblPeakHold.ForeColor = System.Drawing.Color.White
            Me.lblPeakHold.Location = New System.Drawing.Point(13, 172)
            Me.lblPeakHold.Name = "lblPeakHold"
            Me.lblPeakHold.Size = New System.Drawing.Size(118, 20)
            Me.lblPeakHold.TabIndex = 5
            Me.lblPeakHold.Text = "Peak Hold Time:"
            '
            'cmbPeakHold
            '
            Me.cmbPeakHold.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbPeakHold.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbPeakHold.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbPeakHold.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbPeakHold.ForeColor = System.Drawing.Color.White
            Me.cmbPeakHold.FormattingEnabled = True
            Me.cmbPeakHold.Location = New System.Drawing.Point(143, 168)
            Me.cmbPeakHold.Name = "cmbPeakHold"
            Me.cmbPeakHold.Size = New System.Drawing.Size(204, 28)
            Me.cmbPeakHold.TabIndex = 6
            '
            'lblPeakDecay
            '
            Me.lblPeakDecay.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblPeakDecay.AutoSize = True
            Me.lblPeakDecay.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblPeakDecay.ForeColor = System.Drawing.Color.White
            Me.lblPeakDecay.Location = New System.Drawing.Point(13, 207)
            Me.lblPeakDecay.Name = "lblPeakDecay"
            Me.lblPeakDecay.Size = New System.Drawing.Size(123, 20)
            Me.lblPeakDecay.TabIndex = 7
            Me.lblPeakDecay.Text = "Peak Decay Rate:"
            '
            'cmbPeakDecay
            '
            Me.cmbPeakDecay.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbPeakDecay.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbPeakDecay.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbPeakDecay.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbPeakDecay.ForeColor = System.Drawing.Color.White
            Me.cmbPeakDecay.FormattingEnabled = True
            Me.cmbPeakDecay.Location = New System.Drawing.Point(143, 203)
            Me.cmbPeakDecay.Name = "cmbPeakDecay"
            Me.cmbPeakDecay.Size = New System.Drawing.Size(204, 28)
            Me.cmbPeakDecay.TabIndex = 8
            '
            'Label3
            '
            Me.Label3.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.Label3.AutoSize = True
            Me.tableLayoutPanel.SetColumnSpan(Me.Label3, 2)
            Me.Label3.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Bold)
            Me.Label3.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(200, Byte), Integer), CType(CType(100, Byte), Integer))
            Me.Label3.Location = New System.Drawing.Point(13, 258)
            Me.Label3.Name = "Label3"
            Me.Label3.Size = New System.Drawing.Size(144, 23)
            Me.Label3.TabIndex = 9
            Me.Label3.Text = "RMS && Ballistics"
            '
            'lblRmsWindow
            '
            Me.lblRmsWindow.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblRmsWindow.AutoSize = True
            Me.lblRmsWindow.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblRmsWindow.ForeColor = System.Drawing.Color.White
            Me.lblRmsWindow.Location = New System.Drawing.Point(13, 292)
            Me.lblRmsWindow.Name = "lblRmsWindow"
            Me.lblRmsWindow.Size = New System.Drawing.Size(98, 20)
            Me.lblRmsWindow.TabIndex = 10
            Me.lblRmsWindow.Text = "RMS Window:"
            '
            'cmbRmsWindow
            '
            Me.cmbRmsWindow.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbRmsWindow.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbRmsWindow.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbRmsWindow.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbRmsWindow.ForeColor = System.Drawing.Color.White
            Me.cmbRmsWindow.FormattingEnabled = True
            Me.cmbRmsWindow.Location = New System.Drawing.Point(143, 288)
            Me.cmbRmsWindow.Name = "cmbRmsWindow"
            Me.cmbRmsWindow.Size = New System.Drawing.Size(204, 28)
            Me.cmbRmsWindow.TabIndex = 11
            '
            'lblAttack
            '
            Me.lblAttack.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblAttack.AutoSize = True
            Me.lblAttack.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblAttack.ForeColor = System.Drawing.Color.White
            Me.lblAttack.Location = New System.Drawing.Point(13, 327)
            Me.lblAttack.Name = "lblAttack"
            Me.lblAttack.Size = New System.Drawing.Size(87, 20)
            Me.lblAttack.TabIndex = 12
            Me.lblAttack.Text = "Attack Time:"
            '
            'cmbAttack
            '
            Me.cmbAttack.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbAttack.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbAttack.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbAttack.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbAttack.ForeColor = System.Drawing.Color.White
            Me.cmbAttack.FormattingEnabled = True
            Me.cmbAttack.Location = New System.Drawing.Point(143, 323)
            Me.cmbAttack.Name = "cmbAttack"
            Me.cmbAttack.Size = New System.Drawing.Size(204, 28)
            Me.cmbAttack.TabIndex = 13
            '
            'lblRelease
            '
            Me.lblRelease.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblRelease.AutoSize = True
            Me.lblRelease.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblRelease.ForeColor = System.Drawing.Color.White
            Me.lblRelease.Location = New System.Drawing.Point(13, 362)
            Me.lblRelease.Name = "lblRelease"
            Me.lblRelease.Size = New System.Drawing.Size(93, 20)
            Me.lblRelease.TabIndex = 14
            Me.lblRelease.Text = "Release Time:"
            '
            'cmbRelease
            '
            Me.cmbRelease.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbRelease.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbRelease.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbRelease.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbRelease.ForeColor = System.Drawing.Color.White
            Me.cmbRelease.FormattingEnabled = True
            Me.cmbRelease.Location = New System.Drawing.Point(143, 358)
            Me.cmbRelease.Name = "cmbRelease"
            Me.cmbRelease.Size = New System.Drawing.Size(204, 28)
            Me.cmbRelease.TabIndex = 15
            '
            'Label4
            '
            Me.Label4.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.Label4.AutoSize = True
            Me.tableLayoutPanel.SetColumnSpan(Me.Label4, 2)
            Me.Label4.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Bold)
            Me.Label4.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(200, Byte), Integer), CType(CType(100, Byte), Integer))
            Me.Label4.Location = New System.Drawing.Point(13, 398)
            Me.Label4.Name = "Label4"
            Me.Label4.Size = New System.Drawing.Size(163, 23)
            Me.Label4.TabIndex = 16
            Me.Label4.Text = "Clipping Detection"
            '
            'lblClipThreshold
            '
            Me.lblClipThreshold.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblClipThreshold.AutoSize = True
            Me.lblClipThreshold.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblClipThreshold.ForeColor = System.Drawing.Color.White
            Me.lblClipThreshold.Location = New System.Drawing.Point(13, 432)
            Me.lblClipThreshold.Name = "lblClipThreshold"
            Me.lblClipThreshold.Size = New System.Drawing.Size(108, 20)
            Me.lblClipThreshold.TabIndex = 17
            Me.lblClipThreshold.Text = "Clip Threshold:"
            '
            'cmbClipThreshold
            '
            Me.cmbClipThreshold.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbClipThreshold.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbClipThreshold.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbClipThreshold.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbClipThreshold.ForeColor = System.Drawing.Color.White
            Me.cmbClipThreshold.FormattingEnabled = True
            Me.cmbClipThreshold.Location = New System.Drawing.Point(143, 428)
            Me.cmbClipThreshold.Name = "cmbClipThreshold"
            Me.cmbClipThreshold.Size = New System.Drawing.Size(204, 28)
            Me.cmbClipThreshold.TabIndex = 18
            '
            'lblClipHold
            '
            Me.lblClipHold.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblClipHold.AutoSize = True
            Me.lblClipHold.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblClipHold.ForeColor = System.Drawing.Color.White
            Me.lblClipHold.Location = New System.Drawing.Point(13, 467)
            Me.lblClipHold.Name = "lblClipHold"
            Me.lblClipHold.Size = New System.Drawing.Size(113, 20)
            Me.lblClipHold.TabIndex = 19
            Me.lblClipHold.Text = "Clip Hold Time:"
            '
            'cmbClipHold
            '
            Me.cmbClipHold.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbClipHold.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbClipHold.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbClipHold.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbClipHold.ForeColor = System.Drawing.Color.White
            Me.cmbClipHold.FormattingEnabled = True
            Me.cmbClipHold.Location = New System.Drawing.Point(143, 463)
            Me.cmbClipHold.Name = "cmbClipHold"
            Me.cmbClipHold.Size = New System.Drawing.Size(204, 28)
            Me.cmbClipHold.TabIndex = 20
            '
            'Label5
            '
            Me.Label5.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.Label5.AutoSize = True
            Me.tableLayoutPanel.SetColumnSpan(Me.Label5, 2)
            Me.Label5.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Bold)
            Me.Label5.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(200, Byte), Integer), CType(CType(100, Byte), Integer))
            Me.Label5.Location = New System.Drawing.Point(13, 503)
            Me.Label5.Name = "Label5"
            Me.Label5.Size = New System.Drawing.Size(70, 23)
            Me.Label5.TabIndex = 21
            Me.Label5.Text = "Presets"
            '
            'btnFastResponse
            '
            Me.btnFastResponse.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.btnFastResponse.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.btnFastResponse.ForeColor = System.Drawing.Color.Black
            Me.btnFastResponse.Location = New System.Drawing.Point(13, 538)
            Me.btnFastResponse.Name = "btnFastResponse"
            Me.btnFastResponse.Size = New System.Drawing.Size(124, 34)
            Me.btnFastResponse.TabIndex = 22
            Me.btnFastResponse.Text = "Fast Response"
            Me.btnFastResponse.UseVisualStyleBackColor = True
            '
            'btnSlowResponse
            '
            Me.btnSlowResponse.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.btnSlowResponse.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.btnSlowResponse.ForeColor = System.Drawing.Color.Black
            Me.btnSlowResponse.Location = New System.Drawing.Point(143, 538)
            Me.btnSlowResponse.Name = "btnSlowResponse"
            Me.btnSlowResponse.Size = New System.Drawing.Size(204, 34)
            Me.btnSlowResponse.TabIndex = 23
            Me.btnSlowResponse.Text = "Slow Response"
            Me.btnSlowResponse.UseVisualStyleBackColor = True
            '
            'btnBroadcast
            '
            Me.btnBroadcast.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.btnBroadcast.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.btnBroadcast.ForeColor = System.Drawing.Color.Black
            Me.btnBroadcast.Location = New System.Drawing.Point(13, 578)
            Me.btnBroadcast.Name = "btnBroadcast"
            Me.btnBroadcast.Size = New System.Drawing.Size(124, 34)
            Me.btnBroadcast.TabIndex = 24
            Me.btnBroadcast.Text = "Broadcast"
            Me.btnBroadcast.UseVisualStyleBackColor = True
            '
            'btnReset
            '
            Me.btnReset.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.btnReset.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.btnReset.ForeColor = System.Drawing.Color.Black
            Me.btnReset.Location = New System.Drawing.Point(143, 578)
            Me.btnReset.Name = "btnReset"
            Me.btnReset.Size = New System.Drawing.Size(204, 34)
            Me.btnReset.TabIndex = 25
            Me.btnReset.Text = "Reset to Defaults"
            Me.btnReset.UseVisualStyleBackColor = True
            '
            'InputTabPanel
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 20.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.AutoScroll = True
            Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(45, Byte), Integer), CType(CType(48, Byte), Integer))
            Me.Controls.Add(Me.tableLayoutPanel)
            Me.ForeColor = System.Drawing.Color.FromArgb(CType(CType(241, Byte), Integer), CType(CType(241, Byte), Integer), CType(CType(241, Byte), Integer))
            Me.Name = "InputTabPanel"
            Me.Size = New System.Drawing.Size(360, 670)
            Me.tableLayoutPanel.ResumeLayout(False)
            Me.tableLayoutPanel.PerformLayout()
            CType(Me.trackInputVolume, System.ComponentModel.ISupportInitialize).EndInit()
            Me.ResumeLayout(False)

        End Sub

        Friend WithEvents tableLayoutPanel As TableLayoutPanel
        Friend WithEvents Label1 As Label
        Friend WithEvents trackInputVolume As TrackBar
        Friend WithEvents lblInputVolumeValue As Label
        Friend WithEvents chkLinkToPlayback As CheckBox
        Friend WithEvents Label2 As Label
        Friend WithEvents lblPeakHold As Label
        Friend WithEvents cmbPeakHold As ComboBox
        Friend WithEvents lblPeakDecay As Label
        Friend WithEvents cmbPeakDecay As ComboBox
        Friend WithEvents Label3 As Label
        Friend WithEvents lblRmsWindow As Label
        Friend WithEvents cmbRmsWindow As ComboBox
        Friend WithEvents lblAttack As Label
        Friend WithEvents cmbAttack As ComboBox
        Friend WithEvents lblRelease As Label
        Friend WithEvents cmbRelease As ComboBox
        Friend WithEvents Label4 As Label
        Friend WithEvents lblClipThreshold As Label
        Friend WithEvents cmbClipThreshold As ComboBox
        Friend WithEvents lblClipHold As Label
        Friend WithEvents cmbClipHold As ComboBox
        Friend WithEvents Label5 As Label
        Friend WithEvents btnFastResponse As Button
        Friend WithEvents btnSlowResponse As Button
        Friend WithEvents btnBroadcast As Button
        Friend WithEvents btnReset As Button

    End Class

End Namespace
