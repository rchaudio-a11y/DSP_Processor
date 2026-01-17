Imports DSP_Processor.Models
Imports DSP_Processor.UI

Namespace UI.TabPanels

    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class RecordingOptionsPanel
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
            Label1 = New Label()
            radioManual = New RadioButton()
            radioTimed = New RadioButton()
            radioLoop = New RadioButton()
            lblModeDescription = New Label()
            Label2 = New Label()
            lblTimedDuration = New Label()
            numTimedHours = New NumericUpDown()
            numTimedMinutes = New NumericUpDown()
            numTimedSeconds = New NumericUpDown()
            lblTimedFormat = New Label()
            Label3 = New Label()
            lblLoopCount = New Label()
            numLoopCount = New NumericUpDown()
            lblLoopDuration = New Label()
            numLoopHours = New NumericUpDown()
            numLoopMinutes = New NumericUpDown()
            numLoopSeconds = New NumericUpDown()
            lblLoopDelay = New Label()
            numLoopDelay = New NumericUpDown()
            chkAutoIncrement = New CheckBox()
            Label4 = New Label()
            btn30Sec = New Button()
            btn60Sec = New Button()
            btn5Takes = New Button()
            tableLayoutPanel.SuspendLayout()
            CType(numTimedHours, ComponentModel.ISupportInitialize).BeginInit()
            CType(numTimedMinutes, ComponentModel.ISupportInitialize).BeginInit()
            CType(numTimedSeconds, ComponentModel.ISupportInitialize).BeginInit()
            CType(numLoopCount, ComponentModel.ISupportInitialize).BeginInit()
            CType(numLoopHours, ComponentModel.ISupportInitialize).BeginInit()
            CType(numLoopMinutes, ComponentModel.ISupportInitialize).BeginInit()
            CType(numLoopSeconds, ComponentModel.ISupportInitialize).BeginInit()
            CType(numLoopDelay, ComponentModel.ISupportInitialize).BeginInit()
            SuspendLayout()
            ' 
            ' tableLayoutPanel
            ' 
            tableLayoutPanel.AutoScroll = True
            tableLayoutPanel.ColumnCount = 4
            tableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 130.0F))
            tableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33F))
            tableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33F))
            tableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.34F))
            tableLayoutPanel.Controls.Add(Label1, 0, 0)
            tableLayoutPanel.Controls.Add(radioManual, 0, 1)
            tableLayoutPanel.Controls.Add(radioTimed, 0, 2)
            tableLayoutPanel.Controls.Add(radioLoop, 0, 3)
            tableLayoutPanel.Controls.Add(lblModeDescription, 0, 4)
            tableLayoutPanel.Controls.Add(Label2, 0, 5)
            tableLayoutPanel.Controls.Add(lblTimedDuration, 0, 6)
            tableLayoutPanel.Controls.Add(numTimedHours, 1, 6)
            tableLayoutPanel.Controls.Add(numTimedMinutes, 2, 6)
            tableLayoutPanel.Controls.Add(numTimedSeconds, 3, 6)
            tableLayoutPanel.Controls.Add(lblTimedFormat, 0, 7)
            tableLayoutPanel.Controls.Add(Label3, 0, 8)
            tableLayoutPanel.Controls.Add(lblLoopCount, 0, 9)
            tableLayoutPanel.Controls.Add(numLoopCount, 1, 9)
            tableLayoutPanel.Controls.Add(lblLoopDuration, 0, 10)
            tableLayoutPanel.Controls.Add(numLoopHours, 1, 10)
            tableLayoutPanel.Controls.Add(numLoopMinutes, 2, 10)
            tableLayoutPanel.Controls.Add(numLoopSeconds, 3, 10)
            tableLayoutPanel.Controls.Add(lblLoopDelay, 0, 11)
            tableLayoutPanel.Controls.Add(numLoopDelay, 1, 11)
            tableLayoutPanel.Controls.Add(chkAutoIncrement, 0, 12)
            tableLayoutPanel.Controls.Add(Label4, 0, 13)
            tableLayoutPanel.Controls.Add(btn30Sec, 0, 14)
            tableLayoutPanel.Controls.Add(btn60Sec, 0, 15)
            tableLayoutPanel.Controls.Add(btn5Takes, 0, 16)
            tableLayoutPanel.Dock = DockStyle.Fill
            tableLayoutPanel.Location = New Point(0, 0)
            tableLayoutPanel.Name = "tableLayoutPanel"
            tableLayoutPanel.Padding = New Padding(10)
            tableLayoutPanel.RowCount = 17
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 25.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 35.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40.0F))
            tableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40.0F))
            tableLayoutPanel.Size = New Size(377, 640)
            tableLayoutPanel.TabIndex = 0
            ' 
            ' Label1
            ' 
            Label1.Anchor = AnchorStyles.Left
            Label1.AutoSize = True
            tableLayoutPanel.SetColumnSpan(Label1, 4)
            Label1.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold)
            Label1.ForeColor = Color.FromArgb(CByte(255), CByte(200), CByte(100))
            Label1.Location = New Point(13, 13)
            Label1.Name = "Label1"
            Label1.Size = New Size(143, 23)
            Label1.TabIndex = 0
            Label1.Text = "Recording Mode"
            ' 
            ' radioManual
            ' 
            radioManual.Anchor = AnchorStyles.Left
            radioManual.AutoSize = True
            radioManual.Checked = True
            tableLayoutPanel.SetColumnSpan(radioManual, 4)
            radioManual.Font = New Font("Segoe UI", 9.0F)
            radioManual.ForeColor = Color.White
            radioManual.Location = New Point(13, 45)
            radioManual.Name = "radioManual"
            radioManual.Size = New Size(232, 24)
            radioManual.TabIndex = 1
            radioManual.TabStop = True
            radioManual.Text = "Manual - Record until stopped"
            radioManual.UseVisualStyleBackColor = True
            ' 
            ' radioTimed
            ' 
            radioTimed.Anchor = AnchorStyles.Left
            radioTimed.AutoSize = True
            tableLayoutPanel.SetColumnSpan(radioTimed, 4)
            radioTimed.Font = New Font("Segoe UI", 9.0F)
            radioTimed.ForeColor = Color.White
            radioTimed.Location = New Point(13, 80)
            radioTimed.Name = "radioTimed"
            radioTimed.Size = New Size(270, 24)
            radioTimed.TabIndex = 2
            radioTimed.Text = "Timed - Record for specific duration"
            radioTimed.UseVisualStyleBackColor = True
            ' 
            ' radioLoop
            ' 
            radioLoop.Anchor = AnchorStyles.Left
            radioLoop.AutoSize = True
            tableLayoutPanel.SetColumnSpan(radioLoop, 4)
            radioLoop.Font = New Font("Segoe UI", 9.0F)
            radioLoop.ForeColor = Color.White
            radioLoop.Location = New Point(13, 115)
            radioLoop.Name = "radioLoop"
            radioLoop.Size = New Size(242, 24)
            radioLoop.TabIndex = 3
            radioLoop.Text = "Loop - Multiple automatic takes"
            radioLoop.UseVisualStyleBackColor = True
            ' 
            ' lblModeDescription
            ' 
            lblModeDescription.Anchor = AnchorStyles.Left
            tableLayoutPanel.SetColumnSpan(lblModeDescription, 4)
            lblModeDescription.Font = New Font("Segoe UI", 9.0F, FontStyle.Italic)
            lblModeDescription.ForeColor = Color.LightGray
            lblModeDescription.Location = New Point(13, 148)
            lblModeDescription.Name = "lblModeDescription"
            lblModeDescription.Size = New Size(324, 28)
            lblModeDescription.TabIndex = 4
            lblModeDescription.Text = "Record until manually stopped"
            ' 
            ' Label2
            ' 
            Label2.Anchor = AnchorStyles.Left
            Label2.AutoSize = True
            tableLayoutPanel.SetColumnSpan(Label2, 4)
            Label2.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold)
            Label2.ForeColor = Color.FromArgb(CByte(255), CByte(200), CByte(100))
            Label2.Location = New Point(13, 183)
            Label2.Name = "Label2"
            Label2.Size = New Size(216, 23)
            Label2.TabIndex = 5
            Label2.Text = "Timed Recording Options"
            ' 
            ' lblTimedDuration
            ' 
            lblTimedDuration.Anchor = AnchorStyles.Left
            lblTimedDuration.AutoSize = True
            lblTimedDuration.Font = New Font("Segoe UI", 9.0F)
            lblTimedDuration.ForeColor = Color.White
            lblTimedDuration.Location = New Point(13, 220)
            lblTimedDuration.Name = "lblTimedDuration"
            lblTimedDuration.Size = New Size(70, 20)
            lblTimedDuration.TabIndex = 6
            lblTimedDuration.Text = "Duration:"
            ' 
            ' numTimedHours
            ' 
            numTimedHours.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            numTimedHours.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            numTimedHours.Font = New Font("Segoe UI", 9.0F)
            numTimedHours.ForeColor = Color.White
            numTimedHours.Location = New Point(143, 216)
            numTimedHours.Maximum = New Decimal(New Integer() {23, 0, 0, 0})
            numTimedHours.Name = "numTimedHours"
            numTimedHours.Size = New Size(69, 27)
            numTimedHours.TabIndex = 7
            ' 
            ' numTimedMinutes
            ' 
            numTimedMinutes.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            numTimedMinutes.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            numTimedMinutes.Font = New Font("Segoe UI", 9.0F)
            numTimedMinutes.ForeColor = Color.White
            numTimedMinutes.Location = New Point(218, 216)
            numTimedMinutes.Maximum = New Decimal(New Integer() {59, 0, 0, 0})
            numTimedMinutes.Name = "numTimedMinutes"
            numTimedMinutes.Size = New Size(69, 27)
            numTimedMinutes.TabIndex = 8
            numTimedMinutes.Value = New Decimal(New Integer() {1, 0, 0, 0})
            ' 
            ' numTimedSeconds
            ' 
            numTimedSeconds.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            numTimedSeconds.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            numTimedSeconds.Font = New Font("Segoe UI", 9.0F)
            numTimedSeconds.ForeColor = Color.White
            numTimedSeconds.Location = New Point(293, 216)
            numTimedSeconds.Maximum = New Decimal(New Integer() {59, 0, 0, 0})
            numTimedSeconds.Name = "numTimedSeconds"
            numTimedSeconds.Size = New Size(71, 27)
            numTimedSeconds.TabIndex = 9
            ' 
            ' lblTimedFormat
            ' 
            lblTimedFormat.Anchor = AnchorStyles.None
            tableLayoutPanel.SetColumnSpan(lblTimedFormat, 3)
            lblTimedFormat.Font = New Font("Segoe UI", 8.0F)
            lblTimedFormat.ForeColor = Color.Gray
            lblTimedFormat.Location = New Point(62, 253)
            lblTimedFormat.Name = "lblTimedFormat"
            lblTimedFormat.Size = New Size(175, 18)
            lblTimedFormat.TabIndex = 10
            lblTimedFormat.Text = "HH : MM : SS"
            lblTimedFormat.TextAlign = ContentAlignment.MiddleCenter
            ' 
            ' Label3
            ' 
            Label3.Anchor = AnchorStyles.Left
            Label3.AutoSize = True
            tableLayoutPanel.SetColumnSpan(Label3, 4)
            Label3.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold)
            Label3.ForeColor = Color.FromArgb(CByte(255), CByte(200), CByte(100))
            Label3.Location = New Point(13, 278)
            Label3.Name = "Label3"
            Label3.Size = New Size(205, 23)
            Label3.TabIndex = 11
            Label3.Text = "Loop Recording Options"
            ' 
            ' lblLoopCount
            ' 
            lblLoopCount.Anchor = AnchorStyles.Left
            lblLoopCount.AutoSize = True
            lblLoopCount.Font = New Font("Segoe UI", 9.0F)
            lblLoopCount.ForeColor = Color.White
            lblLoopCount.Location = New Point(13, 315)
            lblLoopCount.Name = "lblLoopCount"
            lblLoopCount.Size = New Size(122, 20)
            lblLoopCount.TabIndex = 12
            lblLoopCount.Text = "Number of takes:"
            ' 
            ' numLoopCount
            ' 
            numLoopCount.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            numLoopCount.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            tableLayoutPanel.SetColumnSpan(numLoopCount, 2)
            numLoopCount.Font = New Font("Segoe UI", 9.0F)
            numLoopCount.ForeColor = Color.White
            numLoopCount.Location = New Point(143, 311)
            numLoopCount.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
            numLoopCount.Name = "numLoopCount"
            numLoopCount.Size = New Size(144, 27)
            numLoopCount.TabIndex = 13
            numLoopCount.Value = New Decimal(New Integer() {3, 0, 0, 0})
            ' 
            ' lblLoopDuration
            ' 
            lblLoopDuration.Anchor = AnchorStyles.Left
            lblLoopDuration.AutoSize = True
            lblLoopDuration.Font = New Font("Segoe UI", 9.0F)
            lblLoopDuration.ForeColor = Color.White
            lblLoopDuration.Location = New Point(13, 345)
            lblLoopDuration.Name = "lblLoopDuration"
            lblLoopDuration.Size = New Size(97, 40)
            lblLoopDuration.TabIndex = 14
            lblLoopDuration.Text = "Duration per take:"
            ' 
            ' numLoopHours
            ' 
            numLoopHours.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            numLoopHours.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            numLoopHours.Font = New Font("Segoe UI", 9.0F)
            numLoopHours.ForeColor = Color.White
            numLoopHours.Location = New Point(143, 351)
            numLoopHours.Maximum = New Decimal(New Integer() {23, 0, 0, 0})
            numLoopHours.Name = "numLoopHours"
            numLoopHours.Size = New Size(69, 27)
            numLoopHours.TabIndex = 15
            ' 
            ' numLoopMinutes
            ' 
            numLoopMinutes.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            numLoopMinutes.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            numLoopMinutes.Font = New Font("Segoe UI", 9.0F)
            numLoopMinutes.ForeColor = Color.White
            numLoopMinutes.Location = New Point(218, 351)
            numLoopMinutes.Maximum = New Decimal(New Integer() {59, 0, 0, 0})
            numLoopMinutes.Name = "numLoopMinutes"
            numLoopMinutes.Size = New Size(69, 27)
            numLoopMinutes.TabIndex = 16
            ' 
            ' numLoopSeconds
            ' 
            numLoopSeconds.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            numLoopSeconds.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            numLoopSeconds.Font = New Font("Segoe UI", 9.0F)
            numLoopSeconds.ForeColor = Color.White
            numLoopSeconds.Location = New Point(293, 351)
            numLoopSeconds.Maximum = New Decimal(New Integer() {59, 0, 0, 0})
            numLoopSeconds.Name = "numLoopSeconds"
            numLoopSeconds.Size = New Size(71, 27)
            numLoopSeconds.TabIndex = 17
            numLoopSeconds.Value = New Decimal(New Integer() {30, 0, 0, 0})
            ' 
            ' lblLoopDelay
            ' 
            lblLoopDelay.Anchor = AnchorStyles.Left
            lblLoopDelay.AutoSize = True
            lblLoopDelay.Font = New Font("Segoe UI", 9.0F)
            lblLoopDelay.ForeColor = Color.White
            lblLoopDelay.Location = New Point(13, 385)
            lblLoopDelay.Name = "lblLoopDelay"
            lblLoopDelay.Size = New Size(112, 40)
            lblLoopDelay.TabIndex = 18
            lblLoopDelay.Text = "Delay between (sec):"
            ' 
            ' numLoopDelay
            ' 
            numLoopDelay.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            numLoopDelay.BackColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
            tableLayoutPanel.SetColumnSpan(numLoopDelay, 2)
            numLoopDelay.Font = New Font("Segoe UI", 9.0F)
            numLoopDelay.ForeColor = Color.White
            numLoopDelay.Location = New Point(143, 391)
            numLoopDelay.Maximum = New Decimal(New Integer() {60, 0, 0, 0})
            numLoopDelay.Name = "numLoopDelay"
            numLoopDelay.Size = New Size(144, 27)
            numLoopDelay.TabIndex = 19
            numLoopDelay.Value = New Decimal(New Integer() {2, 0, 0, 0})
            ' 
            ' chkAutoIncrement
            ' 
            chkAutoIncrement.Anchor = AnchorStyles.Left
            chkAutoIncrement.AutoSize = True
            chkAutoIncrement.Checked = True
            chkAutoIncrement.CheckState = CheckState.Checked
            tableLayoutPanel.SetColumnSpan(chkAutoIncrement, 4)
            chkAutoIncrement.Font = New Font("Segoe UI", 9.0F)
            chkAutoIncrement.ForeColor = Color.White
            chkAutoIncrement.Location = New Point(13, 430)
            chkAutoIncrement.Name = "chkAutoIncrement"
            chkAutoIncrement.Size = New Size(228, 24)
            chkAutoIncrement.TabIndex = 20
            chkAutoIncrement.Text = "Auto-increment take numbers"
            chkAutoIncrement.UseVisualStyleBackColor = True
            ' 
            ' Label4
            ' 
            Label4.Anchor = AnchorStyles.Left
            Label4.AutoSize = True
            tableLayoutPanel.SetColumnSpan(Label4, 4)
            Label4.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold)
            Label4.ForeColor = Color.FromArgb(CByte(255), CByte(200), CByte(100))
            Label4.Location = New Point(13, 463)
            Label4.Name = "Label4"
            Label4.Size = New Size(117, 23)
            Label4.TabIndex = 21
            Label4.Text = "Quick Presets"
            ' 
            ' btn30Sec
            ' 
            btn30Sec.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            tableLayoutPanel.SetColumnSpan(btn30Sec, 4)
            btn30Sec.Font = New Font("Segoe UI", 9.0F)
            btn30Sec.ForeColor = Color.Black
            btn30Sec.Location = New Point(13, 493)
            btn30Sec.Name = "btn30Sec"
            btn30Sec.Size = New Size(351, 34)
            btn30Sec.TabIndex = 22
            btn30Sec.Text = "30 Second Take"
            btn30Sec.UseVisualStyleBackColor = True
            ' 
            ' btn60Sec
            ' 
            btn60Sec.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            tableLayoutPanel.SetColumnSpan(btn60Sec, 4)
            btn60Sec.Font = New Font("Segoe UI", 9.0F)
            btn60Sec.ForeColor = Color.Black
            btn60Sec.Location = New Point(13, 533)
            btn60Sec.Name = "btn60Sec"
            btn60Sec.Size = New Size(351, 34)
            btn60Sec.TabIndex = 23
            btn60Sec.Text = "60 Second Take"
            btn60Sec.UseVisualStyleBackColor = True
            ' 
            ' btn5Takes
            ' 
            btn5Takes.Anchor = AnchorStyles.Left Or AnchorStyles.Right
            tableLayoutPanel.SetColumnSpan(btn5Takes, 4)
            btn5Takes.Font = New Font("Segoe UI", 9.0F)
            btn5Takes.ForeColor = Color.Black
            btn5Takes.Location = New Point(13, 583)
            btn5Takes.Name = "btn5Takes"
            btn5Takes.Size = New Size(351, 34)
            btn5Takes.TabIndex = 24
            btn5Takes.Text = "5 × 30sec Loop"
            btn5Takes.UseVisualStyleBackColor = True
            ' 
            ' RecordingOptionsPanel
            ' 
            AutoScaleDimensions = New SizeF(8.0F, 20.0F)
            AutoScaleMode = AutoScaleMode.Font
            AutoScroll = True
            BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
            Controls.Add(tableLayoutPanel)
            Name = "RecordingOptionsPanel"
            Size = New Size(377, 640)
            tableLayoutPanel.ResumeLayout(False)
            tableLayoutPanel.PerformLayout()
            CType(numTimedHours, ComponentModel.ISupportInitialize).EndInit()
            CType(numTimedMinutes, ComponentModel.ISupportInitialize).EndInit()
            CType(numTimedSeconds, ComponentModel.ISupportInitialize).EndInit()
            CType(numLoopCount, ComponentModel.ISupportInitialize).EndInit()
            CType(numLoopHours, ComponentModel.ISupportInitialize).EndInit()
            CType(numLoopMinutes, ComponentModel.ISupportInitialize).EndInit()
            CType(numLoopSeconds, ComponentModel.ISupportInitialize).EndInit()
            CType(numLoopDelay, ComponentModel.ISupportInitialize).EndInit()
            ResumeLayout(False)

        End Sub

        Friend WithEvents tableLayoutPanel As TableLayoutPanel
        Friend WithEvents Label1 As Label
        Friend WithEvents radioManual As RadioButton
        Friend WithEvents radioTimed As RadioButton
        Friend WithEvents radioLoop As RadioButton
        Friend WithEvents lblModeDescription As Label
        Friend WithEvents Label2 As Label
        Friend WithEvents lblTimedDuration As Label
        Friend WithEvents numTimedHours As NumericUpDown
        Friend WithEvents numTimedMinutes As NumericUpDown
        Friend WithEvents numTimedSeconds As NumericUpDown
        Friend WithEvents lblTimedFormat As Label
        Friend WithEvents Label3 As Label
        Friend WithEvents lblLoopCount As Label
        Friend WithEvents numLoopCount As NumericUpDown
        Friend WithEvents lblLoopDuration As Label
        Friend WithEvents numLoopHours As NumericUpDown
        Friend WithEvents numLoopMinutes As NumericUpDown
        Friend WithEvents numLoopSeconds As NumericUpDown
        Friend WithEvents lblLoopDelay As Label
        Friend WithEvents numLoopDelay As NumericUpDown
        Friend WithEvents chkAutoIncrement As CheckBox
        Friend WithEvents Label4 As Label
        Friend WithEvents btn30Sec As Button
        Friend WithEvents btn60Sec As Button
        Friend WithEvents btn5Takes As Button

    End Class

End Namespace
