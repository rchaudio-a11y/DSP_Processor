Imports DSP_Processor.Audio.Routing
Imports DSP_Processor.Managers

Namespace UI.TabPanels

    ''' <summary>
    ''' Audio pipeline routing configuration panel.
    ''' Allows user to configure all routing options via centralized UI.
    ''' </summary>
    Public Class AudioPipelinePanel
        Inherits UserControl

#Region "Controls"

        Private grpPipelineSettings As GroupBox
        Private lblPresets As Label
        Private cmbPresets As ComboBox
        Private btnSavePreset As Button
        Private btnDeletePreset As Button

        Private grpProcessing As GroupBox
        Private chkEnableDSP As CheckBox
        Private lblInputGain As Label
        Private trkInputGain As TrackBar
        Private lblInputGainValue As Label
        Private lblOutputGain As Label
        Private trkOutputGain As TrackBar
        Private lblOutputGainValue As Label

        Private grpMonitoring As GroupBox
        Private chkEnableInputFFT As CheckBox
        Private lblInputFFTTap As Label
        Private cmbInputFFTTap As ComboBox
        Private chkEnableOutputFFT As CheckBox
        Private lblOutputFFTTap As Label
        Private cmbOutputFFTTap As ComboBox
        Private chkEnableLevelMeter As CheckBox
        Private lblLevelMeterTap As Label
        Private cmbLevelMeterTap As ComboBox

        Private grpDestination As GroupBox
        Private chkEnableRecording As CheckBox
        Private chkEnablePlayback As CheckBox

#End Region

#Region "Events"

        ''' <summary>Raised when any routing configuration changes</summary>
        Public Event ConfigurationChanged As EventHandler(Of PipelineConfiguration)

#End Region

#Region "Fields"

        Private suppressEvents As Boolean = False
        Private router As AudioPipelineRouter

#End Region

#Region "Constructor"

        Public Sub New()
            InitializeComponent()
            InitializeRouter()
        End Sub

#End Region

#Region "Initialization"

        Private Sub InitializeRouter()
            Try
                ' Get or create router instance
                router = New AudioPipelineRouter()
                router.Initialize()

                ' Subscribe to router events
                AddHandler router.RoutingChanged, AddressOf OnRouterConfigurationChanged

            Catch ex As Exception
                ' Log error but don't crash
                Utils.Logger.Instance.Error("Failed to initialize router in panel", ex, "AudioPipelinePanel")
            End Try
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            ' Main container
            Me.AutoScaleMode = AutoScaleMode.Font
            Me.Size = New Size(400, 600)
            Me.BackColor = Color.FromArgb(45, 45, 48)

            ' Create controls
            CreatePresetControls()
            CreateProcessingControls()
            CreateMonitoringControls()
            CreateDestinationControls()

            ' Position groups
            grpPipelineSettings.Location = New Point(10, 10)
            grpProcessing.Location = New Point(10, 130)
            grpMonitoring.Location = New Point(10, 300)
            grpDestination.Location = New Point(10, 500)

            ' Add to panel
            Me.Controls.AddRange(New Control() {
                grpPipelineSettings,
                grpProcessing,
                grpMonitoring,
                grpDestination
            })

            Me.ResumeLayout(False)
        End Sub

        Private Sub CreatePresetControls()
            grpPipelineSettings = New GroupBox With {
                .Text = "Pipeline Presets",
                .Size = New Size(380, 110),
                .ForeColor = Color.White
            }

            lblPresets = New Label With {
                .Text = "Template:",
                .Location = New Point(10, 25),
                .Size = New Size(70, 20),
                .ForeColor = Color.White
            }

            cmbPresets = New ComboBox With {
                .Location = New Point(90, 23),
                .Size = New Size(270, 21),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbPresets.SelectedIndexChanged, AddressOf cmbPresets_SelectedIndexChanged

            btnSavePreset = New Button With {
                .Text = "Save As...",
                .Location = New Point(90, 55),
                .Size = New Size(130, 30)
            }
            AddHandler btnSavePreset.Click, AddressOf btnSavePreset_Click

            btnDeletePreset = New Button With {
                .Text = "Delete",
                .Location = New Point(230, 55),
                .Size = New Size(130, 30)
            }
            AddHandler btnDeletePreset.Click, AddressOf btnDeletePreset_Click

            grpPipelineSettings.Controls.AddRange(New Control() {
                lblPresets, cmbPresets,
                btnSavePreset, btnDeletePreset
            })
        End Sub

        Private Sub CreateProcessingControls()
            grpProcessing = New GroupBox With {
                .Text = "DSP Processing",
                .Size = New Size(380, 160),
                .ForeColor = Color.White
            }

            chkEnableDSP = New CheckBox With {
                .Text = "Enable DSP Processing",
                .Location = New Point(10, 25),
                .Size = New Size(200, 20),
                .ForeColor = Color.White
            }
            AddHandler chkEnableDSP.CheckedChanged, AddressOf OnSettingChanged

            lblInputGain = New Label With {
                .Text = "Input Gain:",
                .Location = New Point(10, 55),
                .Size = New Size(80, 20),
                .ForeColor = Color.White
            }

            trkInputGain = New TrackBar With {
                .Location = New Point(100, 50),
                .Size = New Size(200, 45),
                .Minimum = 0,
                .Maximum = 200,
                .Value = 100,
                .TickFrequency = 25
            }
            AddHandler trkInputGain.ValueChanged, AddressOf trkInputGain_ValueChanged

            lblInputGainValue = New Label With {
                .Text = "100%",
                .Location = New Point(310, 55),
                .Size = New Size(50, 20),
                .ForeColor = Color.White
            }

            lblOutputGain = New Label With {
                .Text = "Output Gain:",
                .Location = New Point(10, 105),
                .Size = New Size(80, 20),
                .ForeColor = Color.White
            }

            trkOutputGain = New TrackBar With {
                .Location = New Point(100, 100),
                .Size = New Size(200, 45),
                .Minimum = 0,
                .Maximum = 200,
                .Value = 100,
                .TickFrequency = 25
            }
            AddHandler trkOutputGain.ValueChanged, AddressOf trkOutputGain_ValueChanged

            lblOutputGainValue = New Label With {
                .Text = "100%",
                .Location = New Point(310, 105),
                .Size = New Size(50, 20),
                .ForeColor = Color.White
            }

            grpProcessing.Controls.AddRange(New Control() {
                chkEnableDSP,
                lblInputGain, trkInputGain, lblInputGainValue,
                lblOutputGain, trkOutputGain, lblOutputGainValue
            })
        End Sub

        Private Sub CreateMonitoringControls()
            grpMonitoring = New GroupBox With {
                .Text = "Monitoring",
                .Size = New Size(380, 190),
                .ForeColor = Color.White
            }

            chkEnableInputFFT = New CheckBox With {
                .Text = "Enable Input FFT",
                .Location = New Point(10, 25),
                .Size = New Size(150, 20),
                .ForeColor = Color.White
            }
            AddHandler chkEnableInputFFT.CheckedChanged, AddressOf OnSettingChanged

            lblInputFFTTap = New Label With {
                .Text = "Input FFT Tap:",
                .Location = New Point(20, 50),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbInputFFTTap = New ComboBox With {
                .Location = New Point(130, 48),
                .Size = New Size(230, 21),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbInputFFTTap.SelectedIndexChanged, AddressOf OnSettingChanged

            chkEnableOutputFFT = New CheckBox With {
                .Text = "Enable Output FFT",
                .Location = New Point(10, 85),
                .Size = New Size(150, 20),
                .ForeColor = Color.White
            }
            AddHandler chkEnableOutputFFT.CheckedChanged, AddressOf OnSettingChanged

            lblOutputFFTTap = New Label With {
                .Text = "Output FFT Tap:",
                .Location = New Point(20, 110),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbOutputFFTTap = New ComboBox With {
                .Location = New Point(130, 108),
                .Size = New Size(230, 21),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbOutputFFTTap.SelectedIndexChanged, AddressOf OnSettingChanged

            chkEnableLevelMeter = New CheckBox With {
                .Text = "Enable Level Meter",
                .Location = New Point(10, 145),
                .Size = New Size(150, 20),
                .ForeColor = Color.White
            }
            AddHandler chkEnableLevelMeter.CheckedChanged, AddressOf OnSettingChanged

            lblLevelMeterTap = New Label With {
                .Text = "Level Meter Tap:",
                .Location = New Point(20, 170),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbLevelMeterTap = New ComboBox With {
                .Location = New Point(130, 168),
                .Size = New Size(230, 21),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbLevelMeterTap.SelectedIndexChanged, AddressOf OnSettingChanged

            grpMonitoring.Controls.AddRange(New Control() {
                chkEnableInputFFT, lblInputFFTTap, cmbInputFFTTap,
                chkEnableOutputFFT, lblOutputFFTTap, cmbOutputFFTTap,
                chkEnableLevelMeter, lblLevelMeterTap, cmbLevelMeterTap
            })
        End Sub

        Private Sub CreateDestinationControls()
            grpDestination = New GroupBox With {
                .Text = "Destination",
                .Size = New Size(380, 80),
                .ForeColor = Color.White
            }

            chkEnableRecording = New CheckBox With {
                .Text = "Enable Recording",
                .Location = New Point(10, 25),
                .Size = New Size(150, 20),
                .ForeColor = Color.White
            }
            AddHandler chkEnableRecording.CheckedChanged, AddressOf OnSettingChanged

            chkEnablePlayback = New CheckBox With {
                .Text = "Enable Playback",
                .Location = New Point(10, 50),
                .Size = New Size(150, 20),
                .ForeColor = Color.White
            }
            AddHandler chkEnablePlayback.CheckedChanged, AddressOf OnSettingChanged

            grpDestination.Controls.AddRange(New Control() {
                chkEnableRecording,
                chkEnablePlayback
            })
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>Load configuration into UI controls</summary>
        Public Sub LoadConfiguration(config As PipelineConfiguration)
            If config Is Nothing Then Return

            suppressEvents = True
            Try
                ' Processing
                chkEnableDSP.Checked = config.Processing.EnableDSP
                trkInputGain.Value = CInt(config.Processing.InputGain * 100)
                trkOutputGain.Value = CInt(config.Processing.OutputGain * 100)

                ' Monitoring
                chkEnableInputFFT.Checked = config.Monitoring.EnableInputFFT
                cmbInputFFTTap.SelectedItem = config.Monitoring.InputFFTTap
                chkEnableOutputFFT.Checked = config.Monitoring.EnableOutputFFT
                cmbOutputFFTTap.SelectedItem = config.Monitoring.OutputFFTTap
                chkEnableLevelMeter.Checked = config.Monitoring.EnableLevelMeter
                cmbLevelMeterTap.SelectedItem = config.Monitoring.LevelMeterTap

                ' Destination
                chkEnableRecording.Checked = config.Destination.EnableRecording
                chkEnablePlayback.Checked = config.Destination.EnablePlayback

            Finally
                suppressEvents = False
            End Try
        End Sub

        ''' <summary>Get current configuration from UI controls</summary>
        Public Function GetConfiguration() As PipelineConfiguration
            Dim config = New PipelineConfiguration()

            ' Processing
            config.Processing.EnableDSP = chkEnableDSP.Checked
            config.Processing.InputGain = trkInputGain.Value / 100.0F
            config.Processing.OutputGain = trkOutputGain.Value / 100.0F

            ' Monitoring
            config.Monitoring.EnableInputFFT = chkEnableInputFFT.Checked
            If cmbInputFFTTap.SelectedItem IsNot Nothing Then
                config.Monitoring.InputFFTTap = DirectCast(cmbInputFFTTap.SelectedItem, TapPoint)
            End If
            config.Monitoring.EnableOutputFFT = chkEnableOutputFFT.Checked
            If cmbOutputFFTTap.SelectedItem IsNot Nothing Then
                config.Monitoring.OutputFFTTap = DirectCast(cmbOutputFFTTap.SelectedItem, TapPoint)
            End If
            config.Monitoring.EnableLevelMeter = chkEnableLevelMeter.Checked
            If cmbLevelMeterTap.SelectedItem IsNot Nothing Then
                config.Monitoring.LevelMeterTap = DirectCast(cmbLevelMeterTap.SelectedItem, TapPoint)
            End If

            ' Destination
            config.Destination.EnableRecording = chkEnableRecording.Checked
            config.Destination.EnablePlayback = chkEnablePlayback.Checked

            Return config
        End Function

        ''' <summary>Populate combo boxes and load initial configuration</summary>
        Public Sub Initialize()
            suppressEvents = True
            Try
                ' Populate presets
                cmbPresets.Items.Clear()
                If router IsNot Nothing Then
                    For Each templateName In router.AvailableTemplates
                        cmbPresets.Items.Add(templateName)
                    Next
                    If cmbPresets.Items.Count > 0 Then
                        cmbPresets.SelectedIndex = 0
                    End If
                End If

                ' Populate tap point combos
                For Each tapPoint As TapPoint In [Enum].GetValues(GetType(TapPoint))
                    cmbInputFFTTap.Items.Add(tapPoint)
                    cmbOutputFFTTap.Items.Add(tapPoint)
                    cmbLevelMeterTap.Items.Add(tapPoint)
                Next
                cmbInputFFTTap.SelectedIndex = 1 ' PreDSP
                cmbOutputFFTTap.SelectedIndex = 3 ' PostDSP
                cmbLevelMeterTap.SelectedIndex = 1 ' PreDSP

                ' Load current configuration
                If router IsNot Nothing AndAlso router.CurrentConfiguration IsNot Nothing Then
                    LoadConfiguration(router.CurrentConfiguration)
                End If

            Finally
                suppressEvents = False
            End Try
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub OnSettingChanged(sender As Object, e As EventArgs)
            If suppressEvents Then Return
            ApplyConfiguration()
        End Sub

        Private Sub trkInputGain_ValueChanged(sender As Object, e As EventArgs)
            lblInputGainValue.Text = $"{trkInputGain.Value}%"
            OnSettingChanged(sender, e)
        End Sub

        Private Sub trkOutputGain_ValueChanged(sender As Object, e As EventArgs)
            lblOutputGainValue.Text = $"{trkOutputGain.Value}%"
            OnSettingChanged(sender, e)
        End Sub

        Private Sub cmbPresets_SelectedIndexChanged(sender As Object, e As EventArgs)
            If suppressEvents OrElse cmbPresets.SelectedItem Is Nothing Then Return

            Dim templateName = cmbPresets.SelectedItem.ToString()
            If router IsNot Nothing AndAlso router.ApplyTemplate(templateName) Then
                ' Template applied, reload UI
                LoadConfiguration(router.CurrentConfiguration)
            End If
        End Sub

        Private Sub btnSavePreset_Click(sender As Object, e As EventArgs)
            ' Prompt for template name
            Dim templateName = InputBox("Enter template name:", "Save Template")
            If String.IsNullOrWhiteSpace(templateName) Then Return

            If router IsNot Nothing Then
                If router.SaveCurrentAsTemplate(templateName) Then
                    MessageBox.Show($"Template '{templateName}' saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    ' Refresh presets list
                    Initialize()
                Else
                    MessageBox.Show("Failed to save template.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End If
        End Sub

        Private Sub btnDeletePreset_Click(sender As Object, e As EventArgs)
            If cmbPresets.SelectedItem Is Nothing Then Return

            Dim templateName = cmbPresets.SelectedItem.ToString()

            ' Confirm deletion
            Dim result = MessageBox.Show($"Delete template '{templateName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                If router IsNot Nothing AndAlso router.DeleteTemplate(templateName) Then
                    MessageBox.Show($"Template '{templateName}' deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    ' Refresh presets list
                    Initialize()
                Else
                    MessageBox.Show("Failed to delete template (built-in presets cannot be deleted).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End If
        End Sub

        Private Sub OnRouterConfigurationChanged(sender As Object, e As RoutingChangedEventArgs)
            ' Router configuration changed externally, reload UI
            If Me.InvokeRequired Then
                Me.Invoke(Sub() LoadConfiguration(e.NewConfiguration))
            Else
                LoadConfiguration(e.NewConfiguration)
            End If
        End Sub

        Private Sub ApplyConfiguration()
            If router Is Nothing Then Return

            Dim config = GetConfiguration()
            router.UpdateRouting(config)

            ' Raise event for MainForm
            RaiseEvent ConfigurationChanged(Me, config)
        End Sub

#End Region

    End Class

End Namespace
