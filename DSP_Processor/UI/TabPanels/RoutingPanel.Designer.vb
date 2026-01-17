Imports DSP_Processor.AudioIO
Imports DSP_Processor.Managers

Namespace UI.TabPanels

    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class RoutingPanel
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
            Me.lblInputSource = New System.Windows.Forms.Label()
            Me.radioMicrophone = New System.Windows.Forms.RadioButton()
            Me.radioFilePlayback = New System.Windows.Forms.RadioButton()
            Me.btnBrowseInputFile = New System.Windows.Forms.Button()
            Me.lblSelectedFile = New System.Windows.Forms.Label()
            Me.lblOutputDevice = New System.Windows.Forms.Label()
            Me.cmbOutputDevice = New System.Windows.Forms.ComboBox()
            Me.tableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'tableLayoutPanel
            '
            Me.tableLayoutPanel.ColumnCount = 2
            Me.tableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150.0!))
            Me.tableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.tableLayoutPanel.Controls.Add(Me.lblInputSource, 0, 0)
            Me.tableLayoutPanel.Controls.Add(Me.radioMicrophone, 0, 1)
            Me.tableLayoutPanel.Controls.Add(Me.radioFilePlayback, 0, 2)
            Me.tableLayoutPanel.Controls.Add(Me.btnBrowseInputFile, 0, 3)
            Me.tableLayoutPanel.Controls.Add(Me.lblSelectedFile, 0, 4)
            Me.tableLayoutPanel.Controls.Add(Me.lblOutputDevice, 0, 5)
            Me.tableLayoutPanel.Controls.Add(Me.cmbOutputDevice, 0, 6)
            Me.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill
            Me.tableLayoutPanel.Location = New System.Drawing.Point(0, 0)
            Me.tableLayoutPanel.Name = "tableLayoutPanel"
            Me.tableLayoutPanel.Padding = New System.Windows.Forms.Padding(10)
            Me.tableLayoutPanel.RowCount = 7
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
            Me.tableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
            Me.tableLayoutPanel.Size = New System.Drawing.Size(400, 270)
            Me.tableLayoutPanel.TabIndex = 0
            '
            'lblInputSource
            '
            Me.lblInputSource.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblInputSource.AutoSize = True
            Me.tableLayoutPanel.SetColumnSpan(Me.lblInputSource, 2)
            Me.lblInputSource.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
            Me.lblInputSource.ForeColor = System.Drawing.Color.White
            Me.lblInputSource.Location = New System.Drawing.Point(13, 15)
            Me.lblInputSource.Name = "lblInputSource"
            Me.lblInputSource.Size = New System.Drawing.Size(99, 20)
            Me.lblInputSource.TabIndex = 0
            Me.lblInputSource.Text = "Input Source:"
            '
            'radioMicrophone
            '
            Me.radioMicrophone.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.radioMicrophone.AutoSize = True
            Me.radioMicrophone.Checked = True
            Me.tableLayoutPanel.SetColumnSpan(Me.radioMicrophone, 2)
            Me.radioMicrophone.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.radioMicrophone.ForeColor = System.Drawing.Color.White
            Me.radioMicrophone.Location = New System.Drawing.Point(13, 45)
            Me.radioMicrophone.Name = "radioMicrophone"
            Me.radioMicrophone.Size = New System.Drawing.Size(143, 24)
            Me.radioMicrophone.TabIndex = 1
            Me.radioMicrophone.TabStop = True
            Me.radioMicrophone.Text = "🎤 Microphone"
            Me.radioMicrophone.UseVisualStyleBackColor = True
            '
            'radioFilePlayback
            '
            Me.radioFilePlayback.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.radioFilePlayback.AutoSize = True
            Me.tableLayoutPanel.SetColumnSpan(Me.radioFilePlayback, 2)
            Me.radioFilePlayback.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.radioFilePlayback.ForeColor = System.Drawing.Color.White
            Me.radioFilePlayback.Location = New System.Drawing.Point(13, 80)
            Me.radioFilePlayback.Name = "radioFilePlayback"
            Me.radioFilePlayback.Size = New System.Drawing.Size(151, 24)
            Me.radioFilePlayback.TabIndex = 2
            Me.radioFilePlayback.Text = "📁 File Playback"
            Me.radioFilePlayback.UseVisualStyleBackColor = True
            '
            'btnBrowseInputFile
            '
            Me.btnBrowseInputFile.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.btnBrowseInputFile.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.tableLayoutPanel.SetColumnSpan(Me.btnBrowseInputFile, 2)
            Me.btnBrowseInputFile.Enabled = False
            Me.btnBrowseInputFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat
            Me.btnBrowseInputFile.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.btnBrowseInputFile.ForeColor = System.Drawing.Color.White
            Me.btnBrowseInputFile.Location = New System.Drawing.Point(33, 115)
            Me.btnBrowseInputFile.Margin = New System.Windows.Forms.Padding(23, 3, 3, 3)
            Me.btnBrowseInputFile.Name = "btnBrowseInputFile"
            Me.btnBrowseInputFile.Size = New System.Drawing.Size(354, 34)
            Me.btnBrowseInputFile.TabIndex = 3
            Me.btnBrowseInputFile.Text = "Browse for audio file..."
            Me.btnBrowseInputFile.UseVisualStyleBackColor = False
            '
            'lblSelectedFile
            '
            Me.lblSelectedFile.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.tableLayoutPanel.SetColumnSpan(Me.lblSelectedFile, 2)
            Me.lblSelectedFile.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.lblSelectedFile.ForeColor = System.Drawing.Color.Cyan
            Me.lblSelectedFile.Location = New System.Drawing.Point(33, 165)
            Me.lblSelectedFile.Margin = New System.Windows.Forms.Padding(23, 0, 3, 0)
            Me.lblSelectedFile.Name = "lblSelectedFile"
            Me.lblSelectedFile.Size = New System.Drawing.Size(354, 40)
            Me.lblSelectedFile.TabIndex = 4
            Me.lblSelectedFile.Text = "No file selected"
            '
            'lblOutputDevice
            '
            Me.lblOutputDevice.Anchor = System.Windows.Forms.AnchorStyles.Left
            Me.lblOutputDevice.AutoSize = True
            Me.tableLayoutPanel.SetColumnSpan(Me.lblOutputDevice, 2)
            Me.lblOutputDevice.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
            Me.lblOutputDevice.ForeColor = System.Drawing.Color.White
            Me.lblOutputDevice.Location = New System.Drawing.Point(13, 225)
            Me.lblOutputDevice.Name = "lblOutputDevice"
            Me.lblOutputDevice.Size = New System.Drawing.Size(118, 20)
            Me.lblOutputDevice.TabIndex = 5
            Me.lblOutputDevice.Text = "Output Device:"
            '
            'cmbOutputDevice
            '
            Me.cmbOutputDevice.Anchor = CType((System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.cmbOutputDevice.BackColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer), CType(CType(60, Byte), Integer))
            Me.tableLayoutPanel.SetColumnSpan(Me.cmbOutputDevice, 2)
            Me.cmbOutputDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbOutputDevice.Font = New System.Drawing.Font("Segoe UI", 9.0!)
            Me.cmbOutputDevice.ForeColor = System.Drawing.Color.White
            Me.cmbOutputDevice.FormattingEnabled = True
            Me.cmbOutputDevice.Location = New System.Drawing.Point(13, 223)
            Me.cmbOutputDevice.Name = "cmbOutputDevice"
            Me.cmbOutputDevice.Size = New System.Drawing.Size(374, 28)
            Me.cmbOutputDevice.TabIndex = 6
            '
            'RoutingPanel
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 20.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(45, Byte), Integer), CType(CType(48, Byte), Integer))
            Me.Controls.Add(Me.tableLayoutPanel)
            Me.Name = "RoutingPanel"
            Me.Size = New System.Drawing.Size(400, 270)
            Me.tableLayoutPanel.ResumeLayout(False)
            Me.tableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

        Friend WithEvents tableLayoutPanel As TableLayoutPanel
        Friend WithEvents lblInputSource As Label
        Friend WithEvents radioMicrophone As RadioButton
        Friend WithEvents radioFilePlayback As RadioButton
        Friend WithEvents btnBrowseInputFile As Button
        Friend WithEvents lblSelectedFile As Label
        Friend WithEvents lblOutputDevice As Label
        Friend WithEvents cmbOutputDevice As ComboBox

    End Class

End Namespace
