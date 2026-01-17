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
            tableLayoutPanel = New TableLayoutPanel()
            chkLinkToPlayback = New CheckBox()
            Label2 = New Label()
            lblPeakHold = New Label()
            cmbPeakHold = New ComboBox()
            lblPeakDecay = New Label()
            cmbPeakDecay = New ComboBox()
            Label3 = New Label()
            lblRmsWindow = New Label()
            cmbRmsWindow = New ComboBox()
            lblAttack = New Label()
            cmbAttack = New ComboBox()
            lblRelease = New Label()
            cmbRelease = New ComboBox()
            Label4 = New Label()
            lblClipThreshold = New Label()
            cmbClipThreshold = New ComboBox()
            lblClipHold = New Label()
            cmbClipHold = New ComboBox()
            Label5 = New Label()
            btnFastResponse = New Button()
            btnSlowResponse = New Button()
            btnBroadcast = New Button()
            btnReset = New Button()
            tableLayoutPanel.SuspendLayout()
            SuspendLayout()
            ' 
            ' tableLayoutPanel
            ' 
            tableLayoutPanel.AutoScroll = True
            tableLayoutPanel.ColumnCount = 2
            tableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 130F))
            tableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100F))
            tableLayoutPanel.Controls.Add(chkLinkToPlayback, 0, 0)
            tableLayoutPanel.Controls.Add(Label2, 0, 1)
            tableLayoutPanel.Controls.Add(lblPeakHold, 0, 2)
            tableLayoutPanel.Controls.Add(cmbPeakHold, 1, 2)
            tableLayoutPanel.Controls.Add(lblPeakDecay, 0, 3)
            tableLayoutPanel.Controls.Add(cmbPeakDecay, 1, 3)
            tableLayoutPanel.Controls.Add(Label3, 0, 4)
            tableLayoutPanel.Controls.Add(lblRmsWindow, 0, 5)
            tableLayoutPanel.Controls.Add(cmbRmsWindow, 1, 5)
            tableLayoutPanel.Controls.Add(lblAttack, 0, 6)
            tableLayoutPanel.Controls.Add(cmbAttack, 1, 6)
            tableLayoutPanel.Controls.Add(lblRelease, 0, 7)
            tableLayoutPanel.Controls.Add(cmbRelease, 1, 7)
            tableLayoutPanel.Controls.Add(Label4, 0, 8)
            tableLayoutPanel.Controls.Add(lblClipThreshold, 0, 9)
            tableLayoutPanel.Controls.Add(cmbClipThreshold, 1, 9)
            tableLayoutPanel.Controls.Add(lblClipHold, 0, 10)
            tableLayoutPanel.Controls.Add(cmbClipHold, 1, 10)
            tableLayoutPanel.Controls.Add(Label5, 0, 11)
            tableLayoutPanel.Controls.Add(btnFastResponse, 0, 12)
            tableLayoutPanel.Controls.Add(btnSlowResponse, 1, 12)
            tableLayoutPanel.Controls.Add(btnBroadcast, 0, 13)
            tableLayoutPanel.Controls.Add(btnReset, 1, 13)
            tableLayoutPanel.Dock = DockStyle.Fill
            tableLayoutPanel.Location = New Point(0, 0)
            tableLayoutPanel.Name = "tableLayoutPanel"
            tableLayoutPanel.Padding = New Padding(10)
            tableLayoutPanel.RowCount = 14
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
            tableLayoutPanel.Size = New Size(360, 670)
            tableLayoutPanel.TabIndex = 0
            ' 
            ' chkLinkToPlayback
            ' 
            chkLinkToPlayback.Anchor = AnchorStyles.Left
            chkLinkToPlayback.AutoSize = True
            tableLayoutPanel.SetColumnSpan(chkLinkToPlayback, 2)
            chkLinkToPlayback.Font = New Font("Segoe UI", 9F)
            chkLinkToPlayback.ForeColor = Color.White
            chkLinkToPlayback.Location = New Point(13, 15)
            chkLinkToPlayback.Name = "chkLinkToPlayback"
            chkLinkToPlayback.Size = New Size(191, 24)
            chkLinkToPlayback.TabIndex = 3
            chkLinkToPlayback.Text = "Link to Playback Volume"
            chkLinkToPlayback.UseVisualStyleBackColor = True
            ' 
            ' Label2
            ' 
            Label2.Anchor = AnchorStyles.Left
            Label2.AutoSize = True
            tableLayoutPanel.SetColumnSpan(Label2, 2)
            Label2.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold)
            Label2.ForeColor = Color.FromArgb(CByte(255), CByte(200), CByte(100))
            Label2.Location = New Point(13, 48)
            Label2.Name = "Label2"
            Label2.Size = New Size(122, 23)
            Label2.TabIndex = 4
            Label2.Text = "Peak Behavior"
            ' 
            ' lblPeakHold
            ' 
            lblPeakHold.Anchor = AnchorStyles.Left
            lblPeakHold.AutoSize = True
            lblPeakHold.Font = New Font("Segoe UI", 9F)
            lblPeakHold.ForeColor = Color.White
            lblPeakHold.Location = New Point(13, 82)
            lblPeakHold.Name = "lblPeakHold"
            lblPeakHold.Size = New Size(116, 20)
            lblPeakHold.TabIndex = 5
            lblPeakHold.Text = "Peak Hold Time:"
            ' 
            ' cmbPeakHold
            ' 
            cmbPeakHold.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            cmbPeakHold.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            cmbPeakHold.DropDownStyle = ComboBoxStyle.DropDownList
            cmbPeakHold.Font = New Font("Segoe UI", 9F)
            cmbPeakHold.ForeColor = Color.White
            cmbPeakHold.FormattingEnabled = True
            cmbPeakHold.Location = New Point(143, 78)
            cmbPeakHold.Name = "cmbPeakHold"
            cmbPeakHold.Size = New Size(204, 28)
            cmbPeakHold.TabIndex = 6
            ' 
            ' lblPeakDecay
            ' 
            lblPeakDecay.Anchor = AnchorStyles.Left
            lblPeakDecay.AutoSize = True
            lblPeakDecay.Font = New Font("Segoe UI", 9F)
            lblPeakDecay.ForeColor = Color.White
            lblPeakDecay.Location = New Point(13, 117)
            lblPeakDecay.Name = "lblPeakDecay"
            lblPeakDecay.Size = New Size(121, 20)
            lblPeakDecay.TabIndex = 7
            lblPeakDecay.Text = "Peak Decay Rate:"
            ' 
            ' cmbPeakDecay
            ' 
            cmbPeakDecay.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            cmbPeakDecay.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            cmbPeakDecay.DropDownStyle = ComboBoxStyle.DropDownList
            cmbPeakDecay.Font = New Font("Segoe UI", 9F)
            cmbPeakDecay.ForeColor = Color.White
            cmbPeakDecay.FormattingEnabled = True
            cmbPeakDecay.Location = New Point(143, 113)
            cmbPeakDecay.Name = "cmbPeakDecay"
            cmbPeakDecay.Size = New Size(204, 28)
            cmbPeakDecay.TabIndex = 8
            ' 
            ' Label3
            ' 
            Label3.Anchor = AnchorStyles.Left
            Label3.AutoSize = True
            tableLayoutPanel.SetColumnSpan(Label3, 2)
            Label3.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold)
            Label3.ForeColor = Color.FromArgb(CByte(255), CByte(200), CByte(100))
            Label3.Location = New Point(13, 148)
            Label3.Name = "Label3"
            Label3.Size = New Size(140, 23)
            Label3.TabIndex = 9
            Label3.Text = "RMS && Ballistics"
            ' 
            ' lblRmsWindow
            ' 
            lblRmsWindow.Anchor = AnchorStyles.Left
            lblRmsWindow.AutoSize = True
            lblRmsWindow.Font = New Font("Segoe UI", 9F)
            lblRmsWindow.ForeColor = Color.White
            lblRmsWindow.Location = New Point(13, 182)
            lblRmsWindow.Name = "lblRmsWindow"
            lblRmsWindow.Size = New Size(101, 20)
            lblRmsWindow.TabIndex = 10
            lblRmsWindow.Text = "RMS Window:"
            ' 
            ' cmbRmsWindow
            ' 
            cmbRmsWindow.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            cmbRmsWindow.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            cmbRmsWindow.DropDownStyle = ComboBoxStyle.DropDownList
            cmbRmsWindow.Font = New Font("Segoe UI", 9F)
            cmbRmsWindow.ForeColor = Color.White
            cmbRmsWindow.FormattingEnabled = True
            cmbRmsWindow.Location = New Point(143, 178)
            cmbRmsWindow.Name = "cmbRmsWindow"
            cmbRmsWindow.Size = New Size(204, 28)
            cmbRmsWindow.TabIndex = 11
            ' 
            ' lblAttack
            ' 
            lblAttack.Anchor = AnchorStyles.Left
            lblAttack.AutoSize = True
            lblAttack.Font = New Font("Segoe UI", 9F)
            lblAttack.ForeColor = Color.White
            lblAttack.Location = New Point(13, 217)
            lblAttack.Name = "lblAttack"
            lblAttack.Size = New Size(91, 20)
            lblAttack.TabIndex = 12
            lblAttack.Text = "Attack Time:"
            ' 
            ' cmbAttack
            ' 
            cmbAttack.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            cmbAttack.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            cmbAttack.DropDownStyle = ComboBoxStyle.DropDownList
            cmbAttack.Font = New Font("Segoe UI", 9F)
            cmbAttack.ForeColor = Color.White
            cmbAttack.FormattingEnabled = True
            cmbAttack.Location = New Point(143, 213)
            cmbAttack.Name = "cmbAttack"
            cmbAttack.Size = New Size(204, 28)
            cmbAttack.TabIndex = 13
            ' 
            ' lblRelease
            ' 
            lblRelease.Anchor = AnchorStyles.Left
            lblRelease.AutoSize = True
            lblRelease.Font = New Font("Segoe UI", 9F)
            lblRelease.ForeColor = Color.White
            lblRelease.Location = New Point(13, 252)
            lblRelease.Name = "lblRelease"
            lblRelease.Size = New Size(100, 20)
            lblRelease.TabIndex = 14
            lblRelease.Text = "Release Time:"
            ' 
            ' cmbRelease
            ' 
            cmbRelease.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            cmbRelease.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            cmbRelease.DropDownStyle = ComboBoxStyle.DropDownList
            cmbRelease.Font = New Font("Segoe UI", 9F)
            cmbRelease.ForeColor = Color.White
            cmbRelease.FormattingEnabled = True
            cmbRelease.Location = New Point(143, 248)
            cmbRelease.Name = "cmbRelease"
            cmbRelease.Size = New Size(204, 28)
            cmbRelease.TabIndex = 15
            ' 
            ' Label4
            ' 
            Label4.Anchor = AnchorStyles.Left
            Label4.AutoSize = True
            tableLayoutPanel.SetColumnSpan(Label4, 2)
            Label4.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold)
            Label4.ForeColor = Color.FromArgb(CByte(255), CByte(200), CByte(100))
            Label4.Location = New Point(13, 283)
            Label4.Name = "Label4"
            Label4.Size = New Size(162, 23)
            Label4.TabIndex = 16
            Label4.Text = "Clipping Detection"
            ' 
            ' lblClipThreshold
            ' 
            lblClipThreshold.Anchor = AnchorStyles.Left
            lblClipThreshold.AutoSize = True
            lblClipThreshold.Font = New Font("Segoe UI", 9F)
            lblClipThreshold.ForeColor = Color.White
            lblClipThreshold.Location = New Point(13, 317)
            lblClipThreshold.Name = "lblClipThreshold"
            lblClipThreshold.Size = New Size(107, 20)
            lblClipThreshold.TabIndex = 17
            lblClipThreshold.Text = "Clip Threshold:"
            ' 
            ' cmbClipThreshold
            ' 
            cmbClipThreshold.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            cmbClipThreshold.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            cmbClipThreshold.DropDownStyle = ComboBoxStyle.DropDownList
            cmbClipThreshold.Font = New Font("Segoe UI", 9F)
            cmbClipThreshold.ForeColor = Color.White
            cmbClipThreshold.FormattingEnabled = True
            cmbClipThreshold.Location = New Point(143, 313)
            cmbClipThreshold.Name = "cmbClipThreshold"
            cmbClipThreshold.Size = New Size(204, 28)
            cmbClipThreshold.TabIndex = 18
            ' 
            ' lblClipHold
            ' 
            lblClipHold.Anchor = AnchorStyles.Left
            lblClipHold.AutoSize = True
            lblClipHold.Font = New Font("Segoe UI", 9F)
            lblClipHold.ForeColor = Color.White
            lblClipHold.Location = New Point(13, 352)
            lblClipHold.Name = "lblClipHold"
            lblClipHold.Size = New Size(112, 20)
            lblClipHold.TabIndex = 19
            lblClipHold.Text = "Clip Hold Time:"
            ' 
            ' cmbClipHold
            ' 
            cmbClipHold.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            cmbClipHold.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            cmbClipHold.DropDownStyle = ComboBoxStyle.DropDownList
            cmbClipHold.Font = New Font("Segoe UI", 9F)
            cmbClipHold.ForeColor = Color.White
            cmbClipHold.FormattingEnabled = True
            cmbClipHold.Location = New Point(143, 348)
            cmbClipHold.Name = "cmbClipHold"
            cmbClipHold.Size = New Size(204, 28)
            cmbClipHold.TabIndex = 20
            ' 
            ' Label5
            ' 
            Label5.Anchor = AnchorStyles.Left
            Label5.AutoSize = True
            tableLayoutPanel.SetColumnSpan(Label5, 2)
            Label5.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold)
            Label5.ForeColor = Color.FromArgb(CByte(255), CByte(200), CByte(100))
            Label5.Location = New Point(13, 383)
            Label5.Name = "Label5"
            Label5.Size = New Size(66, 23)
            Label5.TabIndex = 21
            Label5.Text = "Presets"
            ' 
            ' btnFastResponse
            ' 
            btnFastResponse.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            btnFastResponse.Font = New Font("Segoe UI", 9F)
            btnFastResponse.ForeColor = Color.Black
            btnFastResponse.Location = New Point(13, 413)
            btnFastResponse.Name = "btnFastResponse"
            btnFastResponse.Size = New Size(124, 34)
            btnFastResponse.TabIndex = 22
            btnFastResponse.Text = "Fast Response"
            btnFastResponse.UseVisualStyleBackColor = True
            ' 
            ' btnSlowResponse
            ' 
            btnSlowResponse.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            btnSlowResponse.Font = New Font("Segoe UI", 9F)
            btnSlowResponse.ForeColor = Color.Black
            btnSlowResponse.Location = New Point(143, 413)
            btnSlowResponse.Name = "btnSlowResponse"
            btnSlowResponse.Size = New Size(204, 34)
            btnSlowResponse.TabIndex = 23
            btnSlowResponse.Text = "Slow Response"
            btnSlowResponse.UseVisualStyleBackColor = True
            ' 
            ' btnBroadcast
            ' 
            btnBroadcast.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            btnBroadcast.Font = New Font("Segoe UI", 9F)
            btnBroadcast.ForeColor = Color.Black
            btnBroadcast.Location = New Point(13, 538)
            btnBroadcast.Name = "btnBroadcast"
            btnBroadcast.Size = New Size(124, 34)
            btnBroadcast.TabIndex = 24
            btnBroadcast.Text = "Broadcast"
            btnBroadcast.UseVisualStyleBackColor = True
            ' 
            ' btnReset
            ' 
            btnReset.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            btnReset.Font = New Font("Segoe UI", 9F)
            btnReset.ForeColor = Color.Black
            btnReset.Location = New Point(143, 538)
            btnReset.Name = "btnReset"
            btnReset.Size = New Size(204, 34)
            btnReset.TabIndex = 25
            btnReset.Text = "Reset to Defaults"
            btnReset.UseVisualStyleBackColor = True
            ' 
            ' InputTabPanel
            ' 
            AutoScaleDimensions = New SizeF(8F, 20F)
            AutoScaleMode = AutoScaleMode.Font
            AutoScroll = True
            BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
            Controls.Add(tableLayoutPanel)
            ForeColor = Color.FromArgb(CByte(241), CByte(241), CByte(241))
            Name = "InputTabPanel"
            Size = New Size(360, 670)
            tableLayoutPanel.ResumeLayout(False)
            tableLayoutPanel.PerformLayout()
            ResumeLayout(False)

        End Sub

        Friend WithEvents tableLayoutPanel As TableLayoutPanel
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
