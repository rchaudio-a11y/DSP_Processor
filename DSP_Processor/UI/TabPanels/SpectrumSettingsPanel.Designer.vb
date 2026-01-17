Imports DSP_Processor.Models

Namespace UI.TabPanels

    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class SpectrumSettingsPanel
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
            Me.lblFFTSize = New System.Windows.Forms.Label()
            Me.cmbFFTSize = New System.Windows.Forms.ComboBox()
            Me.lblWindowFunction = New System.Windows.Forms.Label()
            Me.cmbWindowFunction = New System.Windows.Forms.ComboBox()
            Me.lblSmoothing = New System.Windows.Forms.Label()
            Me.numSmoothing = New System.Windows.Forms.NumericUpDown()
            Me.chkPeakHold = New System.Windows.Forms.CheckBox()
            Me.lblMinFreq = New System.Windows.Forms.Label()
            Me.lblMinFreqValue = New System.Windows.Forms.Label()
            Me.trackMinFreq = New System.Windows.Forms.TrackBar()
            Me.lblMaxFreq = New System.Windows.Forms.Label()
            Me.lblMaxFreqValue = New System.Windows.Forms.Label()
            Me.trackMaxFreq = New System.Windows.Forms.TrackBar()
            Me.lblDBRange = New System.Windows.Forms.Label()
            Me.lblDBRangeValue = New System.Windows.Forms.Label()
            Me.trackDBRange = New System.Windows.Forms.TrackBar()
            Me.btnResetSpectrum = New System.Windows.Forms.Button()
            Me.tableLayoutPanel.SuspendLayout()
            CType(Me.numSmoothing, System.ComponentModel.ISupportInitialize).BeginInit()
            CType(Me.trackMinFreq, System.ComponentModel.ISupportInitialize).BeginInit()
            CType(Me.trackMaxFreq, System.ComponentModel.ISupportInitialize).BeginInit()
            CType(Me.trackDBRange, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            'tableLayoutPanel
            '
            Me.tableLayoutPanel.ColumnCount = 2
            Me.tableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150.0!))
            Me.tableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.tableLayoutPanel.Controls.Add(Me.lblFFTSize, 0, 0)
            Me.tableLayoutPanel.Controls.Add(Me.cmbFFTSize, 1, 0)
            Me.tableLayoutPanel.Controls.Add(Me.lblWindowFunction, 0, 1)
            Me.tableLayoutPanel.Controls.Add(Me.cmbWindowFunction, 1, 1)
            Me.tableLayoutPanel.Controls.Add(Me.lblSmoothing, 0, 2)
            Me.tableLayoutPanel.Controls.Add(Me.numSmoothing, 1, 2)
            Me.tableLayoutPanel.Controls.Add(Me.chkPeakHold, 0, 3)
            Me.tableLayoutPanel.Controls.Add(Me.lblMinFreq, 0, 4)
            Me.tableLayoutPanel.Controls.Add(Me.lblMinFreqValue, 1, 4)
            Me.tableLayoutPanel.Controls.Add(Me.trackMinFreq, 0, 5)
            Me.tableLayoutPanel.Controls.Add(Me.lblMaxFreq, 0, 6)
            Me.tableLayoutPanel.Controls.Add(Me.lblMaxFreqValue, 1, 6)
            Me.tableLayoutPanel.Controls.Add(Me.trackMaxFreq, 0, 7)
            Me.tableLayoutPanel.Controls.Add(Me.lblDBRange, 0, 8)
            Me.tableLayoutPanel.Controls.Add(Me.lblDBRangeValue, 1, 8)
            Me.tableLayoutPanel.Controls.Add(Me.trackDBRange, 0, 9)
            Me.tableLayoutPanel.Controls.Add(Me.btnResetSpectrum, 0, 10)
            Me.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill
            Me.tableLayoutPanel.Location = New System.Drawing.Point(0, 0)
            Me.tableLayoutPanel.Name = "tableLayoutPanel"
            Me.tableLayoutPanel.Padding = New System.Windows.Forms.Padding(10)
            Me.tableLayoutPanel.RowCount = 11
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45.0!))
            Me.tableLayoutPanel.Size = New System.Drawing.Size(440, 550)
            Me.tableLayoutPanel.TabIndex = 0
            '
            'lblFFTSize
            '
            Me.lblFFTSize.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblFFTSize.AutoSize = True
            Me.lblFFTSize.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblFFTSize.ForeColor = System.Drawing.Color.White
            Me.lblFFTSize.Location = New System.Drawing.Point(13, 17)
            Me.lblFFTSize.Name = "lblFFTSize"
            Me.lblFFTSize.Size = New System.Drawing.Size(66, 20)
            Me.lblFFTSize.TabIndex = 0
            Me.lblFFTSize.Text = "FFT Size:"
            '
            'cmbFFTSize
            '
            Me.cmbFFTSize.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.cmbFFTSize.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbFFTSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbFFTSize.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbFFTSize.ForeColor = System.Drawing.Color.White
            Me.cmbFFTSize.FormattingEnabled = True
            Me.cmbFFTSize.Items.AddRange(New Object() {"1024", "2048", "4096", "8192", "16384"})
            Me.cmbFFTSize.Location = New System.Drawing.Point(163, 13)
            Me.cmbFFTSize.Name = "cmbFFTSize"
            Me.cmbFFTSize.Size = New System.Drawing.Size(180, 28)
            Me.cmbFFTSize.TabIndex = 1
            '
            'lblWindowFunction
            '
            Me.lblWindowFunction.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblWindowFunction.AutoSize = True
            Me.lblWindowFunction.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblWindowFunction.ForeColor = System.Drawing.Color.White
            Me.lblWindowFunction.Location = New System.Drawing.Point(13, 52)
            Me.lblWindowFunction.Name = "lblWindowFunction"
            Me.lblWindowFunction.Size = New System.Drawing.Size(129, 20)
            Me.lblWindowFunction.TabIndex = 2
            Me.lblWindowFunction.Text = "Window Function:"
            '
            'cmbWindowFunction
            '
            Me.cmbWindowFunction.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.cmbWindowFunction.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.cmbWindowFunction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbWindowFunction.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbWindowFunction.ForeColor = System.Drawing.Color.White
            Me.cmbWindowFunction.FormattingEnabled = True
            Me.cmbWindowFunction.Items.AddRange(New Object() {"None", "Hann", "Hamming", "Blackman"})
            Me.cmbWindowFunction.Location = New System.Drawing.Point(163, 48)
            Me.cmbWindowFunction.Name = "cmbWindowFunction"
            Me.cmbWindowFunction.Size = New System.Drawing.Size(180, 28)
            Me.cmbWindowFunction.TabIndex = 3
            '
            'lblSmoothing
            '
            Me.lblSmoothing.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblSmoothing.AutoSize = True
            Me.lblSmoothing.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblSmoothing.ForeColor = System.Drawing.Color.White
            Me.lblSmoothing.Location = New System.Drawing.Point(13, 87)
            Me.lblSmoothing.Name = "lblSmoothing"
            Me.lblSmoothing.Size = New System.Drawing.Size(120, 20)
            Me.lblSmoothing.TabIndex = 4
            Me.lblSmoothing.Text = "Smoothing (0-10):"
            '
            'numSmoothing
            '
            Me.numSmoothing.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.numSmoothing.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.numSmoothing.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.numSmoothing.ForeColor = System.Drawing.Color.White
            Me.numSmoothing.Location = New System.Drawing.Point(163, 84)
            Me.numSmoothing.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
            Me.numSmoothing.Name = "numSmoothing"
            Me.numSmoothing.Size = New System.Drawing.Size(120, 27)
            Me.numSmoothing.TabIndex = 5
            Me.numSmoothing.Value = New Decimal(New Integer() {5, 0, 0, 0})
            '
            'chkPeakHold
            '
            Me.chkPeakHold.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.chkPeakHold.AutoSize = True
            Me.tableLayoutPanel.SetColumnSpan(Me.chkPeakHold, 2)
            Me.chkPeakHold.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.chkPeakHold.ForeColor = System.Drawing.Color.White
            Me.chkPeakHold.Location = New System.Drawing.Point(13, 120)
            Me.chkPeakHold.Name = "chkPeakHold"
            Me.chkPeakHold.Size = New System.Drawing.Size(170, 24)
            Me.chkPeakHold.TabIndex = 6
            Me.chkPeakHold.Text = "Enable Peak Hold"
            Me.chkPeakHold.UseVisualStyleBackColor = True
            '
            'lblMinFreq
            '
            Me.lblMinFreq.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblMinFreq.AutoSize = True
            Me.lblMinFreq.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblMinFreq.ForeColor = System.Drawing.Color.White
            Me.lblMinFreq.Location = New System.Drawing.Point(13, 155)
            Me.lblMinFreq.Name = "lblMinFreq"
            Me.lblMinFreq.Size = New System.Drawing.Size(124, 20)
            Me.lblMinFreq.TabIndex = 7
            Me.lblMinFreq.Text = "Min Frequency:"
            '
            'lblMinFreqValue
            '
            Me.lblMinFreqValue.Anchor = System.Windows.Forms.AnchorStyles.Right
            Me.lblMinFreqValue.AutoSize = True
            Me.lblMinFreqValue.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
            Me.lblMinFreqValue.ForeColor = System.Drawing.Color.Cyan
            Me.lblMinFreqValue.Location = New System.Drawing.Point(376, 155)
            Me.lblMinFreqValue.Name = "lblMinFreqValue"
            Me.lblMinFreqValue.Size = New System.Drawing.Size(51, 20)
            Me.lblMinFreqValue.TabIndex = 8
            Me.lblMinFreqValue.Text = "20 Hz"
            '
            'trackMinFreq
            '
            Me.trackMinFreq.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.tableLayoutPanel.SetColumnSpan(Me.trackMinFreq, 2)
            Me.trackMinFreq.Location = New System.Drawing.Point(13, 187)
            Me.trackMinFreq.Maximum = 1000
            Me.trackMinFreq.Minimum = 10
            Me.trackMinFreq.Name = "trackMinFreq"
            Me.trackMinFreq.Size = New System.Drawing.Size(414, 56)
            Me.trackMinFreq.TabIndex = 9
            Me.trackMinFreq.TickFrequency = 100
            Me.trackMinFreq.Value = 20
            '
            'lblMaxFreq
            '
            Me.lblMaxFreq.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblMaxFreq.AutoSize = True
            Me.lblMaxFreq.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblMaxFreq.ForeColor = System.Drawing.Color.White
            Me.lblMaxFreq.Location = New System.Drawing.Point(13, 255)
            Me.lblMaxFreq.Name = "lblMaxFreq"
            Me.lblMaxFreq.Size = New System.Drawing.Size(127, 20)
            Me.lblMaxFreq.TabIndex = 10
            Me.lblMaxFreq.Text = "Max Frequency:"
            '
            'lblMaxFreqValue
            '
            Me.lblMaxFreqValue.Anchor = System.Windows.Forms.AnchorStyles.Right
            Me.lblMaxFreqValue.AutoSize = True
            Me.lblMaxFreqValue.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
            Me.lblMaxFreqValue.ForeColor = System.Drawing.Color.Cyan
            Me.lblMaxFreqValue.Location = New System.Drawing.Point(344, 255)
            Me.lblMaxFreqValue.Name = "lblMaxFreqValue"
            Me.lblMaxFreqValue.Size = New System.Drawing.Size(83, 20)
            Me.lblMaxFreqValue.TabIndex = 11
            Me.lblMaxFreqValue.Text = "20000 Hz"
            '
            'trackMaxFreq
            '
            Me.trackMaxFreq.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.tableLayoutPanel.SetColumnSpan(Me.trackMaxFreq, 2)
            Me.trackMaxFreq.Location = New System.Drawing.Point(13, 287)
            Me.trackMaxFreq.Maximum = 22000
            Me.trackMaxFreq.Minimum = 1000
            Me.trackMaxFreq.Name = "trackMaxFreq"
            Me.trackMaxFreq.Size = New System.Drawing.Size(414, 56)
            Me.trackMaxFreq.TabIndex = 12
            Me.trackMaxFreq.TickFrequency = 2000
            Me.trackMaxFreq.Value = 20000
            '
            'lblDBRange
            '
            Me.lblDBRange.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblDBRange.AutoSize = True
            Me.lblDBRange.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblDBRange.ForeColor = System.Drawing.Color.White
            Me.lblDBRange.Location = New System.Drawing.Point(13, 355)
            Me.lblDBRange.Name = "lblDBRange"
            Me.lblDBRange.Size = New System.Drawing.Size(79, 20)
            Me.lblDBRange.TabIndex = 13
            Me.lblDBRange.Text = "dB Range:"
            '
            'lblDBRangeValue
            '
            Me.lblDBRangeValue.Anchor = System.Windows.Forms.AnchorStyles.Right
            Me.lblDBRangeValue.AutoSize = True
            Me.lblDBRangeValue.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
            Me.lblDBRangeValue.ForeColor = System.Drawing.Color.Cyan
            Me.lblDBRangeValue.Location = New System.Drawing.Point(379, 355)
            Me.lblDBRangeValue.Name = "lblDBRangeValue"
            Me.lblDBRangeValue.Size = New System.Drawing.Size(48, 20)
            Me.lblDBRangeValue.TabIndex = 14
            Me.lblDBRangeValue.Text = "90 dB"
            '
            'trackDBRange
            '
            Me.trackDBRange.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.tableLayoutPanel.SetColumnSpan(Me.trackDBRange, 2)
            Me.trackDBRange.Location = New System.Drawing.Point(13, 387)
            Me.trackDBRange.Maximum = 120
            Me.trackDBRange.Minimum = 30
            Me.trackDBRange.Name = "trackDBRange"
            Me.trackDBRange.Size = New System.Drawing.Size(414, 56)
            Me.trackDBRange.TabIndex = 15
            Me.trackDBRange.TickFrequency = 10
            Me.trackDBRange.Value = 90
            '
            'btnResetSpectrum
            '
            Me.btnResetSpectrum.Anchor = System.Windows.Forms.AnchorStyles.None
            Me.tableLayoutPanel.SetColumnSpan(Me.btnResetSpectrum, 2)
            Me.btnResetSpectrum.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.btnResetSpectrum.ForeColor = System.Drawing.Color.Black
            Me.btnResetSpectrum.Location = New System.Drawing.Point(145, 460)
            Me.btnResetSpectrum.Name = "btnResetSpectrum"
            Me.btnResetSpectrum.Size = New System.Drawing.Size(150, 35)
            Me.btnResetSpectrum.TabIndex = 16
            Me.btnResetSpectrum.Text = "Reset to Defaults"
            Me.btnResetSpectrum.UseVisualStyleBackColor = True
            '
            'SpectrumSettingsPanel
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 20.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(45, Byte), Integer), CType(CType(48, Byte), Integer))
            Me.Controls.Add(Me.tableLayoutPanel)
            Me.Name = "SpectrumSettingsPanel"
            Me.Size = New System.Drawing.Size(440, 550)
            Me.tableLayoutPanel.ResumeLayout(False)
            Me.tableLayoutPanel.PerformLayout()
            CType(Me.numSmoothing, System.ComponentModel.ISupportInitialize).EndInit()
            CType(Me.trackMinFreq, System.ComponentModel.ISupportInitialize).EndInit()
            CType(Me.trackMaxFreq, System.ComponentModel.ISupportInitialize).EndInit()
            CType(Me.trackDBRange, System.ComponentModel.ISupportInitialize).EndInit()
            Me.ResumeLayout(False)

        End Sub

        Friend WithEvents tableLayoutPanel As TableLayoutPanel
        Friend WithEvents lblFFTSize As Label
        Friend WithEvents cmbFFTSize As ComboBox
        Friend WithEvents lblWindowFunction As Label
        Friend WithEvents cmbWindowFunction As ComboBox
        Friend WithEvents lblSmoothing As Label
        Friend WithEvents numSmoothing As NumericUpDown
        Friend WithEvents chkPeakHold As CheckBox
        Friend WithEvents lblMinFreq As Label
        Friend WithEvents lblMinFreqValue As Label
        Friend WithEvents trackMinFreq As TrackBar
        Friend WithEvents lblMaxFreq As Label
        Friend WithEvents lblMaxFreqValue As Label
        Friend WithEvents trackMaxFreq As TrackBar
        Friend WithEvents lblDBRange As Label
        Friend WithEvents lblDBRangeValue As Label
        Friend WithEvents trackDBRange As TrackBar
        Friend WithEvents btnResetSpectrum As Button

    End Class

End Namespace
