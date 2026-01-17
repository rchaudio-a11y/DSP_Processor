Imports DSP_Processor.Models
Imports DSP_Processor.Managers
Imports NAudio.Wave

Namespace UI.TabPanels

    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class AudioSettingsPanel
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
            Me.lblAudioDriver = New System.Windows.Forms.Label()
            Me.cmbAudioDriver = New System.Windows.Forms.ComboBox()
            Me.lblInputDevice = New System.Windows.Forms.Label()
            Me.cmbInputDevices = New System.Windows.Forms.ComboBox()
            Me.lblSampleRate = New System.Windows.Forms.Label()
            Me.cmbSampleRates = New System.Windows.Forms.ComboBox()
            Me.lblBitDepth = New System.Windows.Forms.Label()
            Me.cmbBitDepths = New System.Windows.Forms.ComboBox()
            Me.lblChannelMode = New System.Windows.Forms.Label()
            Me.cmbChannelMode = New System.Windows.Forms.ComboBox()
            Me.lblBufferSize = New System.Windows.Forms.Label()
            Me.cmbBufferSize = New System.Windows.Forms.ComboBox()
            Me.tableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'tableLayoutPanel
            '
            Me.tableLayoutPanel.ColumnCount = 2
            Me.tableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120.0!))
            Me.tableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.tableLayoutPanel.Controls.Add(Me.lblAudioDriver, 0, 0)
            Me.tableLayoutPanel.Controls.Add(Me.cmbAudioDriver, 1, 0)
            Me.tableLayoutPanel.Controls.Add(Me.lblInputDevice, 0, 1)
            Me.tableLayoutPanel.Controls.Add(Me.cmbInputDevices, 1, 1)
            Me.tableLayoutPanel.Controls.Add(Me.lblSampleRate, 0, 2)
            Me.tableLayoutPanel.Controls.Add(Me.cmbSampleRates, 1, 2)
            Me.tableLayoutPanel.Controls.Add(Me.lblBitDepth, 0, 3)
            Me.tableLayoutPanel.Controls.Add(Me.cmbBitDepths, 1, 3)
            Me.tableLayoutPanel.Controls.Add(Me.lblChannelMode, 0, 4)
            Me.tableLayoutPanel.Controls.Add(Me.cmbChannelMode, 1, 4)
            Me.tableLayoutPanel.Controls.Add(Me.lblBufferSize, 0, 5)
            Me.tableLayoutPanel.Controls.Add(Me.cmbBufferSize, 1, 5)
            Me.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill
            Me.tableLayoutPanel.Location = New System.Drawing.Point(0, 0)
            Me.tableLayoutPanel.Name = "tableLayoutPanel"
            Me.tableLayoutPanel.Padding = New System.Windows.Forms.Padding(10)
            Me.tableLayoutPanel.RowCount = 6
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
            Me.tableLayoutPanel.Size = New System.Drawing.Size(350, 350)
            Me.tableLayoutPanel.TabIndex = 0
            '
            'lblAudioDriver
            '
            Me.lblAudioDriver.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblAudioDriver.AutoSize = True
            Me.lblAudioDriver.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblAudioDriver.ForeColor = System.Drawing.Color.White
            Me.lblAudioDriver.Location = New System.Drawing.Point(13, 20)
            Me.lblAudioDriver.Name = "lblAudioDriver"
            Me.lblAudioDriver.Size = New System.Drawing.Size(97, 20)
            Me.lblAudioDriver.TabIndex = 0
            Me.lblAudioDriver.Text = "Audio Driver:"
            '
            'cmbAudioDriver
            '
            Me.cmbAudioDriver.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbAudioDriver.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbAudioDriver.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbAudioDriver.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbAudioDriver.ForeColor = System.Drawing.Color.White
            Me.cmbAudioDriver.FormattingEnabled = True
            Me.cmbAudioDriver.Location = New System.Drawing.Point(133, 16)
            Me.cmbAudioDriver.Name = "cmbAudioDriver"
            Me.cmbAudioDriver.Size = New System.Drawing.Size(204, 28)
            Me.cmbAudioDriver.TabIndex = 1
            '
            'lblInputDevice
            '
            Me.lblInputDevice.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblInputDevice.AutoSize = True
            Me.lblInputDevice.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblInputDevice.ForeColor = System.Drawing.Color.White
            Me.lblInputDevice.Location = New System.Drawing.Point(13, 60)
            Me.lblInputDevice.Name = "lblInputDevice"
            Me.lblInputDevice.Size = New System.Drawing.Size(97, 20)
            Me.lblInputDevice.TabIndex = 2
            Me.lblInputDevice.Text = "Input Device:"
            '
            'cmbInputDevices
            '
            Me.cmbInputDevices.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbInputDevices.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbInputDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbInputDevices.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbInputDevices.ForeColor = System.Drawing.Color.White
            Me.cmbInputDevices.FormattingEnabled = True
            Me.cmbInputDevices.Location = New System.Drawing.Point(133, 56)
            Me.cmbInputDevices.Name = "cmbInputDevices"
            Me.cmbInputDevices.Size = New System.Drawing.Size(204, 28)
            Me.cmbInputDevices.TabIndex = 3
            '
            'lblSampleRate
            '
            Me.lblSampleRate.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblSampleRate.AutoSize = True
            Me.lblSampleRate.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblSampleRate.ForeColor = System.Drawing.Color.White
            Me.lblSampleRate.Location = New System.Drawing.Point(13, 100)
            Me.lblSampleRate.Name = "lblSampleRate"
            Me.lblSampleRate.Size = New System.Drawing.Size(96, 20)
            Me.lblSampleRate.TabIndex = 4
            Me.lblSampleRate.Text = "Sample Rate:"
            '
            'cmbSampleRates
            '
            Me.cmbSampleRates.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbSampleRates.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbSampleRates.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbSampleRates.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbSampleRates.ForeColor = System.Drawing.Color.White
            Me.cmbSampleRates.FormattingEnabled = True
            Me.cmbSampleRates.Location = New System.Drawing.Point(133, 96)
            Me.cmbSampleRates.Name = "cmbSampleRates"
            Me.cmbSampleRates.Size = New System.Drawing.Size(204, 28)
            Me.cmbSampleRates.TabIndex = 5
            '
            'lblBitDepth
            '
            Me.lblBitDepth.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblBitDepth.AutoSize = True
            Me.lblBitDepth.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblBitDepth.ForeColor = System.Drawing.Color.White
            Me.lblBitDepth.Location = New System.Drawing.Point(13, 140)
            Me.lblBitDepth.Name = "lblBitDepth"
            Me.lblBitDepth.Size = New System.Drawing.Size(76, 20)
            Me.lblBitDepth.TabIndex = 6
            Me.lblBitDepth.Text = "Bit Depth:"
            '
            'cmbBitDepths
            '
            Me.cmbBitDepths.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbBitDepths.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbBitDepths.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbBitDepths.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbBitDepths.ForeColor = System.Drawing.Color.White
            Me.cmbBitDepths.FormattingEnabled = True
            Me.cmbBitDepths.Location = New System.Drawing.Point(133, 136)
            Me.cmbBitDepths.Name = "cmbBitDepths"
            Me.cmbBitDepths.Size = New System.Drawing.Size(204, 28)
            Me.cmbBitDepths.TabIndex = 7
            '
            'lblChannelMode
            '
            Me.lblChannelMode.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblChannelMode.AutoSize = True
            Me.lblChannelMode.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblChannelMode.ForeColor = System.Drawing.Color.White
            Me.lblChannelMode.Location = New System.Drawing.Point(13, 180)
            Me.lblChannelMode.Name = "lblChannelMode"
            Me.lblChannelMode.Size = New System.Drawing.Size(109, 20)
            Me.lblChannelMode.TabIndex = 8
            Me.lblChannelMode.Text = "Channel Mode:"
            '
            'cmbChannelMode
            '
            Me.cmbChannelMode.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbChannelMode.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbChannelMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbChannelMode.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbChannelMode.ForeColor = System.Drawing.Color.White
            Me.cmbChannelMode.FormattingEnabled = True
            Me.cmbChannelMode.Location = New System.Drawing.Point(133, 176)
            Me.cmbChannelMode.Name = "cmbChannelMode"
            Me.cmbChannelMode.Size = New System.Drawing.Size(204, 28)
            Me.cmbChannelMode.TabIndex = 9
            '
            'lblBufferSize
            '
            Me.lblBufferSize.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblBufferSize.AutoSize = True
            Me.lblBufferSize.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblBufferSize.ForeColor = System.Drawing.Color.White
            Me.lblBufferSize.Location = New System.Drawing.Point(13, 220)
            Me.lblBufferSize.Name = "lblBufferSize"
            Me.lblBufferSize.Size = New System.Drawing.Size(84, 20)
            Me.lblBufferSize.TabIndex = 10
            Me.lblBufferSize.Text = "Buffer Size:"
            '
            'cmbBufferSize
            '
            Me.cmbBufferSize.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbBufferSize.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbBufferSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbBufferSize.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbBufferSize.ForeColor = System.Drawing.Color.White
            Me.cmbBufferSize.FormattingEnabled = True
            Me.cmbBufferSize.Location = New System.Drawing.Point(133, 216)
            Me.cmbBufferSize.Name = "cmbBufferSize"
            Me.cmbBufferSize.Size = New System.Drawing.Size(204, 28)
            Me.cmbBufferSize.TabIndex = 11
            '
            'AudioSettingsPanel
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 20.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(45, Byte), Integer), CType(CType(48, Byte), Integer))
            Me.Controls.Add(Me.tableLayoutPanel)
            Me.Name = "AudioSettingsPanel"
            Me.Size = New System.Drawing.Size(350, 350)
            Me.tableLayoutPanel.ResumeLayout(False)
            Me.tableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

        Friend WithEvents tableLayoutPanel As TableLayoutPanel
        Friend WithEvents lblAudioDriver As Label
        Friend WithEvents cmbAudioDriver As ComboBox
        Friend WithEvents lblInputDevice As Label
        Friend WithEvents cmbInputDevices As ComboBox
        Friend WithEvents lblSampleRate As Label
        Friend WithEvents cmbSampleRates As ComboBox
        Friend WithEvents lblBitDepth As Label
        Friend WithEvents cmbBitDepths As ComboBox
        Friend WithEvents lblChannelMode As Label
        Friend WithEvents cmbChannelMode As ComboBox
        Friend WithEvents lblBufferSize As Label
        Friend WithEvents cmbBufferSize As ComboBox

    End Class

End Namespace
