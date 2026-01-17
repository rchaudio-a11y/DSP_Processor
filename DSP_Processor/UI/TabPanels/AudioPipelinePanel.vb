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
        ' REMOVED: chkEnableLevelMeter, lblLevelMeterTap, cmbLevelMeterTap
        ' Level meter is always on for real-time accuracy - no need for enable/disable

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
        Private isDirty As Boolean = False  ' Track unsaved changes
        Private router As AudioPipelineRouter

#End Region

#Region "Constructor"

        Public Sub New()
            InitializeComponent()
            ' Router will be injected via SetRouter() from MainForm
        End Sub

#End Region

#Region "Initialization"

        ''' <summary>Inject router instance from MainForm (prevents duplicate router instances)</summary>
        Public Sub SetRouter(routerInstance As AudioPipelineRouter)
    If routerInstance Is Nothing Then
        Throw New ArgumentNullException(NameOf(routerInstance))
    End If

    ' Unsubscribe from old router if any
    If router IsNot Nothing Then
        RemoveHandler router.RoutingChanged, AddressOf OnRouterConfigurationChanged
    End If

    ' Set new router
    router = routerInstance

    ' Subscribe to events
    AddHandler router.RoutingChanged, AddressOf OnRouterConfigurationChanged

    Utils.Logger.Instance.Info("Router injected into AudioPipelinePanel", "AudioPipelinePanel")
End Sub

Private Sub InitializeRouter()
    Try
        ' DEPRECATED: Router is now injected via SetRouter()
        ' This method kept for backward compatibility but should not be used
        Utils.Logger.Instance.Warning("InitializeRouter() called - router should be injected via SetRouter()", "AudioPipelinePanel")

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
            AddHandler trkInputGain.DoubleClick, AddressOf trkInputGain_DoubleClick

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
            AddHandler trkOutputGain.DoubleClick, AddressOf trkOutputGain_DoubleClick

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
                .Size = New Size(380, 145),
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

            ' REMOVED: Level Meter controls (checkbox + combo)
            ' Meter is always on - doesn't need UI controls

            grpMonitoring.Controls.AddRange(New Control() {
                chkEnableInputFFT, lblInputFFTTap, cmbInputFFTTap,
                chkEnableOutputFFT, lblOutputFFTTap, cmbOutputFFTTap
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

''' <summary>Set configuration with automatic event suppression</summary>
Private Sub SetConfiguration(config As PipelineConfiguration, Optional suppress As Boolean = True)
    If config Is Nothing Then Return

    Dim wasSuppress = suppressEvents
    suppressEvents = suppress
    Try
        LoadConfiguration(config)
    Finally
        suppressEvents = wasSuppress
    End Try
End Sub

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
                ' REMOVED: Level meter controls (always on)

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

            ' Monitoring - use TypeOf for safety with value types
            config.Monitoring.EnableInputFFT = chkEnableInputFFT.Checked
            If cmbInputFFTTap.SelectedItem IsNot Nothing AndAlso TypeOf cmbInputFFTTap.SelectedItem Is TapPoint Then
                config.Monitoring.InputFFTTap = DirectCast(cmbInputFFTTap.SelectedItem, TapPoint)
            End If

            config.Monitoring.EnableOutputFFT = chkEnableOutputFFT.Checked
            If cmbOutputFFTTap.SelectedItem IsNot Nothing AndAlso TypeOf cmbOutputFFTTap.SelectedItem Is TapPoint Then
                config.Monitoring.OutputFFTTap = DirectCast(cmbOutputFFTTap.SelectedItem, TapPoint)
            End If

            ' REMOVED: Level meter controls (always enabled = True, always taps Pre-DSP)
            ' Set defaults programmatically
            config.Monitoring.EnableLevelMeter = True  ' Always on
            config.Monitoring.LevelMeterTap = TapPoint.PreDSP  ' Always Pre-DSP for accuracy

            ' Destination
            config.Destination.EnableRecording = chkEnableRecording.Checked
            config.Destination.EnablePlayback = chkEnablePlayback.Checked

            Return config
        End Function

        ''' <summary>Populate combo boxes and load initial configuration</summary>
        Public Sub Initialize()
            Utils.Logger.Instance.Info("Initializing AudioPipelinePanel", "AudioPipelinePanel")

            suppressEvents = True
            Try
                ' Populate presets
                cmbPresets.Items.Clear()
                If router IsNot Nothing Then
                    Utils.Logger.Instance.Info($"Loading {router.AvailableTemplates.Count()} available templates", "AudioPipelinePanel")
                    For Each templateName In router.AvailableTemplates
                        cmbPresets.Items.Add(templateName)
                    Next
                    If cmbPresets.Items.Count > 0 Then
                        cmbPresets.SelectedIndex = 0
                        Utils.Logger.Instance.Info($"Default template selected: {cmbPresets.SelectedItem}", "AudioPipelinePanel")
                    End If
                Else
                    Utils.Logger.Instance.Warning("Router is null during Initialize", "AudioPipelinePanel")
                End If

                ' Populate tap point combos
                PopulateTapCombos()

                ' Load current configuration
                If router IsNot Nothing AndAlso router.CurrentConfiguration IsNot Nothing Then
                    Utils.Logger.Instance.Info("Loading current router configuration into UI", "AudioPipelinePanel")
                    LoadConfiguration(router.CurrentConfiguration)
                Else
                    Utils.Logger.Instance.Warning("No current configuration to load", "AudioPipelinePanel")
                End If

                Utils.Logger.Instance.Info("AudioPipelinePanel initialization complete", "AudioPipelinePanel")

            Finally
                suppressEvents = False
            End Try
        End Sub

        ''' <summary>Populate all tap point combo boxes</summary>
        Private Sub PopulateTapCombos()
            Utils.Logger.Instance.Debug("Populating tap point combo boxes", "AudioPipelinePanel")

            Dim tapPoints = [Enum].GetValues(GetType(TapPoint))

            cmbInputFFTTap.Items.Clear()
            cmbOutputFFTTap.Items.Clear()
            ' REMOVED: cmbLevelMeterTap (meter always on, no UI control)

            For Each tapPoint As TapPoint In tapPoints
                cmbInputFFTTap.Items.Add(tapPoint)
                cmbOutputFFTTap.Items.Add(tapPoint)
                ' REMOVED: cmbLevelMeterTap
            Next

            ' Set default selections
            cmbInputFFTTap.SelectedIndex = 1  ' PreDSP
            cmbOutputFFTTap.SelectedIndex = 3 ' PostDSP
            ' REMOVED: cmbLevelMeterTap default (always PreDSP in code)

            Utils.Logger.Instance.Debug("Tap points initialized: InputFFT=PreDSP, OutputFFT=PostDSP, Meter=PreDSP(fixed)", "AudioPipelinePanel")
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub OnSettingChanged(sender As Object, e As EventArgs)
            If suppressEvents Then
                Utils.Logger.Instance.Debug("OnSettingChanged suppressed", "AudioPipelinePanel")
                Return
            End If
            Utils.Logger.Instance.Info($"Setting changed: {sender?.GetType().Name}", "AudioPipelinePanel")
            isDirty = True
            ApplyConfiguration()
        End Sub

        Private Sub trkInputGain_ValueChanged(sender As Object, e As EventArgs)
            lblInputGainValue.Text = $"{trkInputGain.Value}%"
            Utils.Logger.Instance.Debug($"Input gain changed: {trkInputGain.Value}%", "AudioPipelinePanel")
            OnSettingChanged(sender, e)
        End Sub

        Private Sub trkInputGain_DoubleClick(sender As Object, e As EventArgs)
            ' Reset to default (100% = unity gain)
            trkInputGain.Value = 100
            Utils.Logger.Instance.Info("Input gain reset to default (100%)", "AudioPipelinePanel")
        End Sub

        Private Sub trkOutputGain_ValueChanged(sender As Object, e As EventArgs)
            lblOutputGainValue.Text = $"{trkOutputGain.Value}%"
            Utils.Logger.Instance.Debug($"Output gain changed: {trkOutputGain.Value}%", "AudioPipelinePanel")
            OnSettingChanged(sender, e)
        End Sub

        Private Sub trkOutputGain_DoubleClick(sender As Object, e As EventArgs)
            ' Reset to default (100% = unity gain)
            trkOutputGain.Value = 100
            Utils.Logger.Instance.Info("Output gain reset to default (100%)", "AudioPipelinePanel")
        End Sub

        Private Sub cmbPresets_SelectedIndexChanged(sender As Object, e As EventArgs)
            If suppressEvents OrElse cmbPresets.SelectedItem Is Nothing Then
                Utils.Logger.Instance.Debug("Preset selection change suppressed or null", "AudioPipelinePanel")
                Return
            End If

            Dim templateName = cmbPresets.SelectedItem.ToString()
            Utils.Logger.Instance.Info($"Preset selected: {templateName}", "AudioPipelinePanel")

            If router IsNot Nothing AndAlso router.ApplyTemplate(templateName) Then
                ' Template applied, reload UI
                Utils.Logger.Instance.Info($"Template '{templateName}' applied successfully", "AudioPipelinePanel")
                LoadConfiguration(router.CurrentConfiguration)
            Else
                Utils.Logger.Instance.Warning($"Failed to apply template '{templateName}'", "AudioPipelinePanel")
            End If
        End Sub

        Private Sub btnSavePreset_Click(sender As Object, e As EventArgs)
            Utils.Logger.Instance.Info("Save preset button clicked", "AudioPipelinePanel")

            ' Prompt for template name
            Dim templateName = InputBox("Enter template name:", "Save Template")
            If String.IsNullOrWhiteSpace(templateName) Then
                Utils.Logger.Instance.Info("Save preset cancelled by user", "AudioPipelinePanel")
                Return
            End If

            Utils.Logger.Instance.Info($"Attempting to save template: {templateName}", "AudioPipelinePanel")

            If router IsNot Nothing Then
                If router.SaveCurrentAsTemplate(templateName) Then
                    Utils.Logger.Instance.Info($"Template '{templateName}' saved successfully", "AudioPipelinePanel")
                    MessageBox.Show($"Template '{templateName}' saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    ' Refresh presets list
                    Initialize()
                Else
                    Utils.Logger.Instance.Warning($"Failed to save template '{templateName}'", "AudioPipelinePanel")
                    MessageBox.Show("Failed to save template.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            Else
                Utils.Logger.Instance.Warning("Router is null, cannot save template", "AudioPipelinePanel")
            End If
        End Sub

        Private Sub btnDeletePreset_Click(sender As Object, e As EventArgs)
            Utils.Logger.Instance.Info("Delete preset button clicked", "AudioPipelinePanel")

            If cmbPresets.SelectedItem Is Nothing Then
                Utils.Logger.Instance.Warning("No preset selected for deletion", "AudioPipelinePanel")
                Return
            End If

            Dim templateName = cmbPresets.SelectedItem.ToString()
            Utils.Logger.Instance.Info($"Attempting to delete template: {templateName}", "AudioPipelinePanel")

            ' Confirm deletion
            Dim result = MessageBox.Show($"Delete template '{templateName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                Utils.Logger.Instance.Info($"User confirmed deletion of '{templateName}'", "AudioPipelinePanel")

                If router IsNot Nothing AndAlso router.DeleteTemplate(templateName) Then
                    Utils.Logger.Instance.Info($"Template '{templateName}' deleted successfully", "AudioPipelinePanel")
                    MessageBox.Show($"Template '{templateName}' deleted.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    ' Refresh presets list
                    Initialize()
                Else
                    Utils.Logger.Instance.Warning($"Failed to delete template '{templateName}' (may be built-in)", "AudioPipelinePanel")
                    MessageBox.Show("Failed to delete template (built-in presets cannot be deleted).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            Else
                Utils.Logger.Instance.Info($"User cancelled deletion of '{templateName}'", "AudioPipelinePanel")
            End If
        End Sub

        Private Sub OnRouterConfigurationChanged(sender As Object, e As RoutingChangedEventArgs)
            Utils.Logger.Instance.Info("Router configuration changed externally", "AudioPipelinePanel")

            ' Router configuration changed externally, reload UI
            If Me.InvokeRequired Then
                Utils.Logger.Instance.Debug("Invoking LoadConfiguration on UI thread", "AudioPipelinePanel")
                Me.Invoke(Sub() LoadConfiguration(e.NewConfiguration))
            Else
                Utils.Logger.Instance.Debug("Loading configuration on current thread", "AudioPipelinePanel")
                LoadConfiguration(e.NewConfiguration)
            End If
        End Sub

        Private Sub ApplyConfiguration()
            If Not isDirty Then
                Utils.Logger.Instance.Debug("ApplyConfiguration skipped - no changes", "AudioPipelinePanel")
                Return
            End If

            If router Is Nothing Then
                Utils.Logger.Instance.Warning("ApplyConfiguration called but router is null", "AudioPipelinePanel")
                Return
            End If

            Utils.Logger.Instance.Info("Applying configuration changes", "AudioPipelinePanel")

            Dim config = GetConfiguration()

            Utils.Logger.Instance.Debug($"Config: DSP={config.Processing.EnableDSP}, InputGain={config.Processing.InputGain:F2}, OutputGain={config.Processing.OutputGain:F2}", "AudioPipelinePanel")
            Utils.Logger.Instance.Debug($"Config: InputFFT={config.Monitoring.EnableInputFFT}, OutputFFT={config.Monitoring.EnableOutputFFT}, Recording={config.Destination.EnableRecording}", "AudioPipelinePanel")

            router.UpdateRouting(config)

            ' Clear dirty flag after successful update
            isDirty = False

            ' Raise event for MainForm
            Utils.Logger.Instance.Debug("Raising ConfigurationChanged event", "AudioPipelinePanel")
            RaiseEvent ConfigurationChanged(Me, config)
        End Sub

#End Region

    End Class

End Namespace
