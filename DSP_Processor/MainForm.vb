Imports System.IO
Imports DSP_Processor.AudioIO
Imports DSP_Processor.Recording
Imports DSP_Processor.UI
Imports DSP_Processor.Utils
Imports DSP_Processor.Visualization
Imports DSP_Processor.Managers
Imports DSP_Processor.Audio.Routing
Imports NAudio.Wave

Partial Public Class MainForm
    ' MANAGERS (New architecture!)
    Private settingsManager As SettingsManager
    Private fileManager As FileManager
    Private recordingManager As RecordingManager
    Private playbackManager As PlaybackManager
    Private spectrumManager As SpectrumManager ' Task 2.0.4: FFT processing

    ' Audio Router (Phase 2.0)
    Private audioRouter As AudioIO.AudioRouter

    ' Audio Pipeline Router (Phase 2 Foundation - SHARED INSTANCE)
    Private pipelineRouter As AudioPipelineRouter

    ' Flag to prevent FFT queue buildup
    Private fftProcessingInProgress As Boolean = False
    Private lastMonitoringEventTime As DateTime = DateTime.MinValue
    Private monitoringThrottleMs As Integer = 50  ' Only fire monitoring events every 50ms

    ' Form load state
    Private isFormFullyLoaded As Boolean = False
    Private WithEvents deferredArmTimer As Timer

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' APPLY DARK THEME FIRST!
        DarkTheme.ApplyToForm(Me)
        DarkTheme.ApplyDangerButton(btnDelete)

        ' DEBUG: List all embedded resources
        Logger.Instance.Info("=== Embedded Resources ===", "MainForm")
        Dim resources = Utils.ResourceDeployer.ListEmbeddedResources()
        For Each resource In resources
            Logger.Instance.Info($"  - {resource}", "MainForm")
        Next
        Logger.Instance.Info($"Total: {resources.Length} embedded resources", "MainForm")

        ' Deploy embedded test audio files (white noise, etc.)
        Utils.ResourceDeployer.DeployTestAudioFiles()

        ' Create managers
        InitializeManagers()

        ' Create playback manager BEFORE wiring events
        playbackManager = New PlaybackManager()
        playbackManager.Initialize()

        ' Create spectrum manager (Task 2.0.4)
        spectrumManager = New SpectrumManager()
        spectrumManager.Initialize(4096, 44100)

        ' NOW wire up events (all objects exist!)
        WireManagerEvents()
        WirePlaybackEvents()
        WireTransportEvents()
        WireUIEvents()
        WireAudioSettingsPanel()
        WireInputTabPanel()
        WireAudioPipelinePanel()
        WireRoutingPanel()
        WireSpectrumSettingsPanel()
        WireDSPSignalFlowPanel()

        ' Load all settings (this will trigger OnSettingsLoaded event)
        settingsManager.LoadAll()

        ' Apply settings to AudioSettingsPanel and InputTabPanel
        AudioSettingsPanel1.LoadSettings(settingsManager.AudioSettings)
        InputTabPanel1.LoadSettings(settingsManager.MeterSettings)

        ' Initialize AudioPipelinePanel with injected router (prevents duplicate router instances)
        AudioPipelinePanel1.SetRouter(pipelineRouter)
        AudioPipelinePanel1.Initialize()

        ' Initialize RoutingPanel
        InitializeRoutingPanel()

        ' Initialize SpectrumSettingsPanel  
        SpectrumSettingsPanel1.LoadSettings(settingsManager.SpectrumSettings)

        ' Apply meter settings
        ApplyMeterSettings(settingsManager.MeterSettings)

        ' Apply spectrum settings
        ApplySpectrumSettings(settingsManager.SpectrumSettings)

        ' Apply dark theme to visualization tabs
        DarkTheme.ApplyToControl(visualizationTabs)

        ' NOTE: Volume control removed - use AudioPipelinePanel or DSPSignalFlowPanel for gain control

        ' Start timers for UI updates
        ' NOTE: TimerMeters removed - DSPSignalFlowPanel now event-driven via OnRecordingBufferAvailable!
        ' TimerPlayback will be started when playback begins (in OnPlaybackStarted)

        ' NOTE: Microphone will be armed after settings are loaded (in OnSettingsLoaded)

        ' Update UI state (will be updated again after mic arms)
        lblStatus.Text = "Status: Initializing..."
        btnStopPlayback.Enabled = False

        ' ===== TEST: AudioPipelineRouter (DORMANT - Tested Successfully!) =====
        ' Uncomment the following block to test the router:
        '
        ' Logger.Instance.Info("=== TESTING AudioPipelineRouter ===", "MainForm")
        ' testRouter = New AudioPipelineRouter()
        ' testRouter.Initialize()
        ' Logger.Instance.Info("Router initialized successfully", "MainForm")
        ' Logger.Instance.Info($"Available templates: {String.Join(", ", testRouter.AvailableTemplates)}", "MainForm")
        '
        ' ' Test applying a template
        ' If testRouter.ApplyTemplate("Simple Record") Then
        '     Logger.Instance.Info("Applied 'Simple Record' template", "MainForm")
        '     Logger.Instance.Info($"Current routing:{Environment.NewLine}{testRouter.GetActiveRoutingMap()}", "MainForm")
        ' End If
        '
        ' ' Test saving current as template
        ' If testRouter.SaveCurrentAsTemplate("My Test Config") Then
        '     Logger.Instance.Info("Saved current config as 'My Test Config'", "MainForm")
        ' End If
        '
        ' Logger.Instance.Info("=== AudioPipelineRouter Test Complete ===", "MainForm")

        Logger.Instance.Info("DSP Processor started", "MainForm")

        ' CRITICAL: Defer microphone arming until form is fully loaded and painted
        ' This prevents audio thread from overwhelming the UI thread during initialization
        deferredArmTimer = New Timer With {.Interval = 500}  ' 500ms delay
        AddHandler deferredArmTimer.Tick, AddressOf DeferredArmTimer_Tick
        deferredArmTimer.Start()

        Services.LoggingServiceAdapter.Instance.LogInfo("Form load complete - microphone will arm in 500ms")
    End Sub

#Region "Initialization"

    Private Sub InitializeManagers()
        ' Create settings manager
        settingsManager = New SettingsManager()
        Logger.Instance.Info("SettingsManager created", "MainForm")

        ' Create file manager
        fileManager = New FileManager()
        Logger.Instance.Info("FileManager created", "MainForm")

        ' Create recording manager
        recordingManager = New RecordingManager()
        Logger.Instance.Info("RecordingManager created", "MainForm")

        ' Create audio router (Phase 2.0)
        audioRouter = New AudioIO.AudioRouter()
        audioRouter.Initialize()
        Logger.Instance.Info("AudioRouter created", "MainForm")

        ' Create pipeline router (Phase 2 Foundation - shared instance)
        pipelineRouter = New AudioPipelineRouter()
        pipelineRouter.Initialize()
        Logger.Instance.Info("AudioPipelineRouter created (shared instance)", "MainForm")
    End Sub

    Private Sub WireManagerEvents()
        ' SettingsManager events
        AddHandler settingsManager.SettingsLoaded, AddressOf OnSettingsLoaded
        AddHandler settingsManager.SettingsSaved, AddressOf OnSettingsSaved

        ' FileManager events
        AddHandler fileManager.FileListChanged, AddressOf OnFileListChanged
        AddHandler fileManager.FileDeleted, AddressOf OnFileDeleted
        AddHandler fileManager.FileValidationFailed, AddressOf OnFileValidationFailed

        ' RecordingManager events
        AddHandler recordingManager.RecordingStarted, AddressOf OnRecordingStarted
        AddHandler recordingManager.RecordingStopped, AddressOf OnRecordingStopped
        AddHandler recordingManager.RecordingTimeUpdated, AddressOf OnRecordingTimeUpdated
        AddHandler recordingManager.BufferAvailable, AddressOf OnRecordingBufferAvailable
        AddHandler recordingManager.MicrophoneArmed, AddressOf OnMicrophoneArmed
    End Sub

    Private Sub WirePlaybackEvents()
        ' PlaybackManager events (currently unused - direct WAV playback)
        AddHandler playbackManager.PlaybackStarted, AddressOf OnPlaybackStarted
        AddHandler playbackManager.PlaybackStopped, AddressOf OnPlaybackStopped
        AddHandler playbackManager.PositionChanged, AddressOf OnPositionChanged

        ' AudioRouter events (ACTUALLY USED - DSP playback)
        AddHandler audioRouter.PlaybackStarted, AddressOf OnAudioRouterPlaybackStarted
        AddHandler audioRouter.PlaybackStopped, AddressOf OnAudioRouterPlaybackStopped
        AddHandler audioRouter.PositionChanged, AddressOf OnAudioRouterPositionChanged

        Logger.Instance.Info("Playback events wired (PlaybackManager + AudioRouter)", "MainForm")
    End Sub

    Private Sub WireTransportEvents()
        AddHandler transportControl.RecordClicked, AddressOf OnTransportRecord
        AddHandler transportControl.StopClicked, AddressOf OnTransportStop
        AddHandler transportControl.PlayClicked, AddressOf OnTransportPlay
        AddHandler transportControl.PauseClicked, AddressOf OnTransportPause
        AddHandler transportControl.PositionChanged, AddressOf OnTransportPositionChanged
    End Sub

    Private Sub WireUIEvents()
        ' Subscribe to logging events
        AddHandler Services.LoggingServiceAdapter.Instance.LogMessageReceived, AddressOf OnLogMessage

        ' Wire up RoutingPanel events
        AddHandler RoutingPanel1.InputSourceChanged, AddressOf OnRoutingPanelInputSourceChanged
        AddHandler RoutingPanel1.OutputDeviceChanged, AddressOf OnRoutingPanelOutputDeviceChanged
        AddHandler RoutingPanel1.BrowseFileClicked, AddressOf OnRoutingPanelBrowseFileClicked

        ' Wire up AudioRouter FFT events
        AddHandler audioRouter.InputSamplesAvailable, AddressOf OnDSPInputSamples
        AddHandler audioRouter.OutputSamplesAvailable, AddressOf OnDSPOutputSamples
        AddHandler audioRouter.PlaybackCompleted, AddressOf OnPlaybackCompleted

        ' Wire up FFT Monitor event (Phase 3 - NEW!)
        ' Single throttled event replaces old BufferForMonitoring
        AddHandler pipelineRouter.FFTMonitor.SpectrumReady, AddressOf OnSpectrumReady
    End Sub

    Private Sub WireAudioSettingsPanel()
        ' Wire up AudioSettingsPanel event
        AddHandler AudioSettingsPanel1.SettingsChanged, AddressOf OnAudioSettingsChanged
        Logger.Instance.Info("AudioSettingsPanel wired", "MainForm")
    End Sub

    Private Sub WireInputTabPanel()
        ' Wire up InputTabPanel event
        AddHandler InputTabPanel1.SettingsChanged, AddressOf OnMeterSettingsChanged
        Logger.Instance.Info("InputTabPanel wired", "MainForm")
    End Sub

    Private Sub WireAudioPipelinePanel()
        ' Wire up AudioPipelinePanel event
        AddHandler AudioPipelinePanel1.ConfigurationChanged, AddressOf OnPipelineConfigurationChanged

        ' Inject RecordingManager for real-time gain control
        If recordingManager IsNot Nothing Then
            AudioPipelinePanel1.SetRecordingManager(recordingManager)
            Logger.Instance.Info("RecordingManager injected into AudioPipelinePanel", "MainForm")
        End If

        Logger.Instance.Info("AudioPipelinePanel wired", "MainForm")
    End Sub

    Private Sub WireRoutingPanel()
        ' RoutingPanel events already wired in WireUIEvents
        Logger.Instance.Info("RoutingPanel wired", "MainForm")
    End Sub

    Private Sub WireSpectrumSettingsPanel()
        ' Wire up SpectrumSettingsPanel events
        AddHandler SpectrumSettingsPanel1.SettingsChanged, AddressOf OnSpectrumSettingsChanged
        AddHandler SpectrumSettingsPanel1.ResetRequested, AddressOf OnSpectrumResetRequested
        Logger.Instance.Info("SpectrumSettingsPanel wired", "MainForm")
    End Sub

    Private Sub WireDSPSignalFlowPanel()
        ' Wire DSP Signal Flow Panel to AudioRouter's GainProcessor
        ' The AudioRouter now exposes its GainProcessor for UI control
        If audioRouter IsNot Nothing Then
            ' Note: GainProcessor won't exist until PlayFile() is called
            ' We'll wire it when playback starts
            Logger.Instance.Info("DSPSignalFlowPanel wiring deferred until playback starts", "MainForm")
        Else
            Logger.Instance.Warning("Cannot wire DSPSignalFlowPanel - audioRouter is Nothing", "MainForm")
        End If
    End Sub





    ''' <summary>Deferred microphone arming - called 500ms after form load completes</summary>
    Private Sub DeferredArmTimer_Tick(sender As Object, e As EventArgs) Handles deferredArmTimer.Tick
        ' Stop the timer
        deferredArmTimer.Stop()
        deferredArmTimer.Dispose()

        ' Mark form as fully loaded
        isFormFullyLoaded = True

        ' Now arm the microphone
        Try
            Services.LoggingServiceAdapter.Instance.LogInfo("Arming microphone (deferred)")
            recordingManager.ArmMicrophone()
            Services.LoggingServiceAdapter.Instance.LogInfo("Microphone armed successfully")
        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to arm microphone: {ex.Message}", ex)
            MessageBox.Show($"Warning: Failed to initialize audio input.{Environment.NewLine}{ex.Message}", "Audio Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try

        ' Update UI
        lblStatus.Text = "Status: Ready (Mic Armed)"
    End Sub

#End Region

#Region "Manager Event Handlers"

    Private Sub OnSettingsLoaded(sender As Object, e As EventArgs)
        Services.LoggingServiceAdapter.Instance.LogInfo("All settings loaded")

        ' Initialize recording manager with settings
        recordingManager.Initialize(settingsManager.AudioSettings, settingsManager.RecordingOptions)

        ' Load options into RecordingOptionsPanel
        RecordingOptionsPanel1.LoadOptions(settingsManager.RecordingOptions)
        recordingManager.Options = settingsManager.RecordingOptions

        ' Wire up RecordingOptionsPanel event
        AddHandler RecordingOptionsPanel1.OptionsChanged, AddressOf OnRecordingOptionsChanged

        ' RoutingPanel already initialized in MainForm_Load
        ' Output devices populated via InitializeRoutingPanel()

        ' DEFER: Don't arm microphone here - it will be armed after form finishes loading
        ' This prevents audio thread from firing events while form is still painting

        ' Refresh file list
        fileManager.RefreshFileList()

        ' Update UI state
        lblStatus.Text = "Status: Initializing (Please wait...)"
        btnStopPlayback.Enabled = False
        TimerMeters.Start()
    End Sub

    Private Sub OnSettingsSaved(sender As Object, e As EventArgs)
        Services.LoggingServiceAdapter.Instance.LogInfo("All settings saved")
    End Sub

    Private Sub OnFileListChanged(sender As Object, e As EventArgs)
        ' Update list box
        lstRecordings.Items.Clear()
        For Each fileInfo In fileManager.Files
            ' Add indicator for test files
            Dim displayName = fileInfo.Name
            If fileInfo.DirectoryName.EndsWith("Test Audio", StringComparison.OrdinalIgnoreCase) Then
                displayName = "🔊 " & fileInfo.Name ' Speaker icon for test files
            End If
            lstRecordings.Items.Add(displayName)
        Next
        Services.LoggingServiceAdapter.Instance.LogInfo($"File list updated: {fileManager.FileCount} file(s)")
    End Sub

    Private Sub OnFileDeleted(sender As Object, filepath As String)
        Services.LoggingServiceAdapter.Instance.LogInfo($"File deleted: {Path.GetFileName(filepath)}")
        MessageBox.Show($"'{Path.GetFileName(filepath)}' has been deleted.", "Delete Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub OnFileValidationFailed(sender As Object, message As String)
        MessageBox.Show(message, "File Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

    Private Sub OnRecordingStarted(sender As Object, e As EventArgs)
        ' Update UI
        transportControl.State = UI.TransportControl.TransportState.Recording
        panelLED.BackColor = Color.Red
        lblStatus.Text = "Status: Recording"

        ' Clear FFT buffers for fresh spectrum (Task 2.0.4)
        spectrumManager.Clear()

        ' Clear both PRE and POST spectrum displays
        SpectrumAnalyzerControl1.InputDisplay.Clear()
        SpectrumAnalyzerControl1.OutputDisplay.Clear()

        Services.LoggingServiceAdapter.Instance.LogInfo("Recording started")
    End Sub

    Private Sub OnRecordingStopped(sender As Object, e As RecordingStoppedEventArgs)
        ' Update UI
        transportControl.State = UI.TransportControl.TransportState.Stopped
        transportControl.RecordingTime = TimeSpan.Zero
        panelLED.BackColor = Color.Yellow ' Yellow = Armed
        lblStatus.Text = "Status: Ready (Mic Armed)"
        lblRecordingTime.Text = "00:00"

        ' Reset meter
        meterRecording.Reset()

        ' Refresh file list
        fileManager.RefreshFileList()

        Services.LoggingServiceAdapter.Instance.LogInfo($"Recording stopped: {e.Duration.TotalSeconds:F1}s")
    End Sub

    Private Sub OnRecordingTimeUpdated(sender As Object, duration As TimeSpan)
        ' Update UI (on UI thread if needed)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() OnRecordingTimeUpdated(sender, duration))
            Return
        End If

        lblRecordingTime.Text = $"{duration.Minutes:00}:{duration.Seconds:00}"
        transportControl.RecordingTime = duration
    End Sub

    Private Sub OnRecordingBufferAvailable(sender As Object, e As AudioBufferEventArgs)
        ' Route through AudioPipelineRouter (Task 2.0.3 - safety checks moved to router)
        ' Router handles: DSP processing, FFT taps, monitoring, recording destination
        pipelineRouter?.RouteAudioBuffer(
            e.Buffer,
            Audio.Routing.AudioSourceType.Microphone,
            e.BitsPerSample,
            e.Channels,
            e.SampleRate)

        ' FAST PATH: Update meters immediately (EVENT-DRIVEN, not timer-based!)
        ' NOTE: This is direct - not routed through pipeline for lowest latency
        Try
            Dim levelData = AudioLevelMeter.AnalyzeSamples(e.Buffer, e.BitsPerSample, e.Channels)

            ' Waveform tab meter (existing)
            meterRecording.SetLevel(levelData.PeakDB, levelData.RMSDB, levelData.IsClipping)

            ' DSP Signal Flow Panel meters (NEW - event-driven!)
            ' For now, use same levels for input and output (future: read from DSP tap points)
            DspSignalFlowPanel1.UpdateMeters(
                levelData.PeakDB,  ' Input Left
                levelData.PeakDB,  ' Input Right
                levelData.PeakDB,  ' Output Left
                levelData.PeakDB)  ' Output Right
        Catch
            ' Ignore metering errors
        End Try
    End Sub

    Private Sub OnMicrophoneArmed(sender As Object, isArmed As Boolean)
        If isArmed Then
            panelLED.BackColor = Color.Yellow
            lblStatus.Text = "Status: Ready (Mic Armed)"
            transportControl.IsRecordArmed = True

            ' Wire DSPSignalFlowPanel to RecordingManager's INPUT gain processor
            If recordingManager IsNot Nothing AndAlso recordingManager.InputGainProcessor IsNot Nothing Then
                DspSignalFlowPanel1.SetGainProcessor(recordingManager.InputGainProcessor)
                Logger.Instance.Info("DSPSignalFlowPanel wired to RecordingManager INPUT GainProcessor", "MainForm")
            End If
        Else
            panelLED.BackColor = Color.Gray
            lblStatus.Text = "Status: Mic Disarmed"
            transportControl.IsRecordArmed = False
        End If
    End Sub

#End Region

#Region "UserControl Event Handlers"

    ''' <summary>Handles audio settings changes from AudioSettingsPanel</summary>
    Private Sub OnAudioSettingsChanged(sender As Object, settings As Managers.AudioDeviceSettings)
        Services.LoggingServiceAdapter.Instance.LogInfo("Audio settings changed via AudioSettingsPanel")

        ' Update settings manager
        settingsManager.AudioSettings = settings

        ' Apply to recording manager (will re-arm mic with new settings)
        If recordingManager IsNot Nothing Then
            ' CRITICAL: Disarm old device FIRST before switching
            Try
                If recordingManager.IsArmed Then
                    recordingManager.DisarmMicrophone()
                    Services.LoggingServiceAdapter.Instance.LogInfo("Disarmed old audio input")
                End If
            Catch ex As Exception
                Services.LoggingServiceAdapter.Instance.LogWarning($"Failed to disarm old device: {ex.Message}")
            End Try

            ' Now initialize with new settings and arm new device
            recordingManager.Initialize(settings, settingsManager.RecordingOptions)
            Try
                recordingManager.ArmMicrophone()
                Services.LoggingServiceAdapter.Instance.LogInfo("Armed new audio input")
            Catch ex As Exception
                Services.LoggingServiceAdapter.Instance.LogError($"Failed to arm new device: {ex.Message}", ex)
            End Try
        End If

        ' Save settings
        settingsManager.SaveAll()
    End Sub

    Private Sub OnMeterSettingsChanged(sender As Object, settings As Models.MeterSettings)
        Services.LoggingServiceAdapter.Instance.LogInfo("Meter settings changed")

        ' Update settings manager
        settingsManager.MeterSettings = settings

        ' Apply to audio system
        ApplyMeterSettings(settings)

        ' Save settings
        settingsManager.SaveAll()
    End Sub

    Private Sub OnPipelineConfigurationChanged(sender As Object, config As PipelineConfiguration)
        Services.LoggingServiceAdapter.Instance.LogInfo("Pipeline configuration changed")

        ' Configuration is automatically saved by AudioPipelineRouter
        ' Here we would apply the configuration to the actual audio flow (Phase 3)
        ' For now, just log it
        Logger.Instance.Info($"Pipeline config updated: DSP={config.Processing.EnableDSP}, Recording={config.Destination.EnableRecording}", "MainForm")
    End Sub

    Private Sub InitializeRoutingPanel()
        ' Populate output devices
        Try
            Dim deviceNames = audioRouter.GetOutputDeviceNames().ToList()
            Dim selectedDevice = audioRouter.GetSelectedOutputDevice()
            RoutingPanel1.LoadOutputDevices(deviceNames, selectedDevice)

            ' Set initial input source
            RoutingPanel1.SetMicrophoneInput()

            Logger.Instance.Info("RoutingPanel initialized", "MainForm")
        Catch ex As Exception
            Logger.Instance.Error("Failed to initialize RoutingPanel", ex, "MainForm")
        End Try
    End Sub

    Private Sub OnRoutingPanelInputSourceChanged(sender As Object, inputSource As String)
        Logger.Instance.Info($"Input source changed: {inputSource}", "MainForm")
        ' Handle input source change (same logic as before)
        ' This will be fully implemented in Phase 3
    End Sub

    Private Sub OnRoutingPanelOutputDeviceChanged(sender As Object, deviceIndex As Integer)
        Try
            audioRouter.SelectOutputDevice(deviceIndex)
            Services.LoggingServiceAdapter.Instance.LogInfo($"Output device changed: index {deviceIndex}")
        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to change output device: {ex.Message}", ex)
        End Try
    End Sub

    Private Sub OnRoutingPanelBrowseFileClicked(sender As Object, e As EventArgs)
        ' Same logic as old OnBrowseInputFileClick
        OnBrowseInputFileClick(sender, e)
    End Sub

    Private Sub OnSpectrumSettingsChanged(sender As Object, settings As Models.SpectrumSettings)
        Services.LoggingServiceAdapter.Instance.LogInfo("Spectrum settings changed")

        ' Update settings manager
        settingsManager.SpectrumSettings = settings

        ' Apply settings (implementation from existing handlers)
        ApplySpectrumSettings(settings)

        ' Save settings
        settingsManager.SaveAll()
    End Sub

    Private Sub OnSpectrumResetRequested(sender As Object, e As EventArgs)
        Services.LoggingServiceAdapter.Instance.LogInfo("Spectrum reset requested")

        ' Create default settings
        Dim defaults = New Models.SpectrumSettings()

        ' Load into panel
        SpectrumSettingsPanel1.LoadSettings(defaults)

        ' Apply
        OnSpectrumSettingsChanged(sender, defaults)
    End Sub

    Private Sub ApplyMeterSettings(settings As Models.MeterSettings)
        Try
            ' Apply to RecordingManager
            recordingManager.InputVolume = settings.InputVolumePercent / 100.0F

            ' Apply to AudioLevelMeter (static properties)
            AudioLevelMeter.PeakHoldMs = settings.PeakHoldMs
            AudioLevelMeter.PeakDecayDbPerSec = settings.PeakDecayDbPerSec
            AudioLevelMeter.RmsWindowMs = settings.RmsWindowMs
            AudioLevelMeter.AttackMs = settings.AttackMs
            AudioLevelMeter.ReleaseMs = settings.ReleaseMs
            AudioLevelMeter.ClipThresholdDb = settings.ClipThresholdDb

            Services.LoggingServiceAdapter.Instance.LogInfo($"Meter settings applied: Peak={settings.PeakHoldMs}ms, Decay={settings.PeakDecayDbPerSec}dB/s")

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to apply meter settings: {ex.Message}", ex)
            Logger.Instance.Error("Failed to apply meter settings", ex, "MainForm")
        End Try
    End Sub

    Private Sub ApplySpectrumSettings(settings As Models.SpectrumSettings)
        Try
            ' Apply FFT settings to SpectrumManager (Task 2.0.4)
            spectrumManager.ApplySettings(settings)

            ' Apply to spectrum displays (UI only)
            Dim smoothingFactor = CSng(settings.Smoothing / 100)
            SpectrumAnalyzerControl1.InputDisplay.SmoothingFactor = smoothingFactor
            SpectrumAnalyzerControl1.OutputDisplay.SmoothingFactor = smoothingFactor
            SpectrumAnalyzerControl1.InputDisplay.PeakHoldEnabled = settings.PeakHoldEnabled
            SpectrumAnalyzerControl1.OutputDisplay.PeakHoldEnabled = settings.PeakHoldEnabled
            SpectrumAnalyzerControl1.InputDisplay.MinFrequency = settings.MinFrequency
            SpectrumAnalyzerControl1.OutputDisplay.MinFrequency = settings.MinFrequency
            SpectrumAnalyzerControl1.InputDisplay.MaxFrequency = settings.MaxFrequency
            SpectrumAnalyzerControl1.OutputDisplay.MaxFrequency = settings.MaxFrequency
            SpectrumAnalyzerControl1.InputDisplay.MinDB = settings.MinDB
            SpectrumAnalyzerControl1.OutputDisplay.MinDB = settings.MinDB
            SpectrumAnalyzerControl1.InputDisplay.MaxDB = settings.MaxDB
            SpectrumAnalyzerControl1.OutputDisplay.MaxDB = settings.MaxDB

            Services.LoggingServiceAdapter.Instance.LogInfo($"Spectrum settings applied: FFT={settings.FFTSize}, Window={settings.WindowFunction}")

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to apply spectrum settings: {ex.Message}", ex)
            Logger.Instance.Error("Failed to apply spectrum settings", ex, "MainForm")
        End Try
    End Sub

    Private Sub OnRecordingOptionsChanged(sender As Object, options As Models.RecordingOptions)
        Services.LoggingServiceAdapter.Instance.LogInfo($"Recording options changed: {options.Mode} mode")

        ' Update settings manager
        settingsManager.RecordingOptions = options

        ' Apply to RecordingManager (uses new method from Task 2.0.1)
        recordingManager.ApplyRecordingOptions(options)

        ' Save settings
        settingsManager.SaveAll()
    End Sub

#End Region

#Region "File Operations"

    Private Sub lstRecordings_DoubleClick(sender As Object, e As EventArgs) Handles lstRecordings.DoubleClick
        If lstRecordings.SelectedItem Is Nothing Then Return

        Dim displayName = lstRecordings.SelectedItem.ToString()

        ' Remove test file indicator if present
        Dim fileName = displayName.Replace("🔊 ", "")

        ' Find actual file path from FileManager
        Dim fileInfo = fileManager.Files.FirstOrDefault(Function(f) f.Name = fileName)
        If fileInfo Is Nothing Then
            Services.LoggingServiceAdapter.Instance.LogWarning($"File not found: {fileName}")
            Return
        End If

        Dim fullPath = fileInfo.FullName

        Try
            Services.LoggingServiceAdapter.Instance.LogInfo($"Loading file for DSP playback: {fileName}")

            ' DISARM MICROPHONE during playback to prevent feedback/conflicts
            If recordingManager IsNot Nothing Then
                recordingManager.DisarmMicrophone()
                Services.LoggingServiceAdapter.Instance.LogInfo("Microphone disarmed for DSP playback")
            End If

            ' Play file through AudioRouter (Task 2.0.2 - encapsulated method)
            audioRouter.PlayFile(fullPath)

            ' Start timer for FFT updates AND transport position
            TimerPlayback.Start()

            Services.LoggingServiceAdapter.Instance.LogInfo($"DSP playback started: {fileName}")

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to play file '{fileName}': {ex.Message}", ex)
            MessageBox.Show($"Failed to play file: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

            ' Re-arm mic on error
            If recordingManager IsNot Nothing Then
                Try
                    recordingManager.ArmMicrophone()
                Catch
                    ' Ignore re-arm errors
                End Try
            End If
        End Try
    End Sub

    Private Sub lstRecordings_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstRecordings.SelectedIndexChanged
        If lstRecordings.SelectedItem Is Nothing Then Return

        Dim displayName = lstRecordings.SelectedItem.ToString()

        ' Remove test file indicator if present
        Dim fileName = displayName.Replace("🔊 ", "")

        ' Find actual file path from FileManager
        Dim fileInfo = fileManager.Files.FirstOrDefault(Function(f) f.Name = fileName)
        If fileInfo Is Nothing Then
            Services.LoggingServiceAdapter.Instance.LogWarning($"File not found: {fileName}")
            Return
        End If

        Dim fullPath = fileInfo.FullName

        ' Validate file through FileManager
        If Not fileManager.ValidateFile(fullPath) Then
            Services.LoggingServiceAdapter.Instance.LogWarning($"File validation failed: {fileName}")
            Return
        End If

        Try
            Services.LoggingServiceAdapter.Instance.LogInfo($"Rendering waveform for: {fileName}")

            ' Use WaveformDisplayControl instead of manual rendering
            WaveformDisplayControl1.LoadFile(fullPath)

            Services.LoggingServiceAdapter.Instance.LogInfo($"Waveform rendered successfully: {fileName}")

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to render waveform for '{fileName}': {ex.Message}", ex)
            Logger.Instance.Error("Failed to render waveform", ex, "MainForm")
        End Try
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If lstRecordings.SelectedItem Is Nothing Then
            Services.LoggingServiceAdapter.Instance.LogWarning("Delete attempted with no file selected")
            MessageBox.Show("Please select a recording to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim displayName = lstRecordings.SelectedItem.ToString()

        ' Remove test file indicator if present
        Dim fileName = displayName.Replace("🔊 ", "")

        ' Find actual file path from FileManager
        Dim fileInfo = fileManager.Files.FirstOrDefault(Function(f) f.Name = fileName)
        If fileInfo Is Nothing Then
            Services.LoggingServiceAdapter.Instance.LogWarning($"File not found: {fileName}")
            Return
        End If

        Dim fullPath = fileInfo.FullName

        ' Prevent deletion of test files
        If fileInfo.DirectoryName.EndsWith("Test Audio", StringComparison.OrdinalIgnoreCase) Then
            MessageBox.Show("Cannot delete test audio files. These are read-only reference files.", "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Services.LoggingServiceAdapter.Instance.LogWarning($"Attempted to delete test file: {fileName}")
            Return
        End If

        ' Confirm deletion
        Dim result = MessageBox.Show(
            $"Are you sure you want to delete '{fileName}'?{Environment.NewLine}{Environment.NewLine}This cannot be undone.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2)

        If result = DialogResult.Yes Then
            ' Clear selection and waveform
            lstRecordings.ClearSelected()
            WaveformDisplayControl1.Clear()
            WaveformDisplayControl1.ClearCache()

            ' Stop playback if playing this file
            If playbackManager IsNot Nothing AndAlso playbackManager.IsPlaying Then
                playbackManager.Stop()
                System.Threading.Thread.Sleep(100)
            End If

            ' Force GC
            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()
            System.Threading.Thread.Sleep(50)

            ' Delete through FileManager
            fileManager.DeleteFile(fullPath)
        Else
            Services.LoggingServiceAdapter.Instance.LogInfo($"Delete cancelled by user: {fileName}")
        End If
    End Sub

#End Region

#Region "TransportControl Event Handlers"

    Private Sub OnTransportRecord(sender As Object, e As EventArgs)
        Try
            Services.LoggingServiceAdapter.Instance.LogInfo("Starting recording...")
            lstRecordings.ClearSelected()
            WaveformDisplayControl1.ClearCache()
            GC.Collect()
            GC.WaitForPendingFinalizers()
            recordingManager.StartRecording()
        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to start recording: {ex.Message}", ex)
            MessageBox.Show($"Failed to start recording: {ex.Message}", "Recording Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Logger.Instance.Error("Failed to start recording", ex, "MainForm")
        End Try
    End Sub

    Private Sub OnTransportStop(sender As Object, e As EventArgs)
        If playbackManager IsNot Nothing AndAlso playbackManager.IsPlaying Then
            Services.LoggingServiceAdapter.Instance.LogInfo("Stopping playback...")
            playbackManager.Stop()
            TimerPlayback.Stop()
            progressPlayback.Value = 0
            btnStopPlayback.Enabled = False
            Services.LoggingServiceAdapter.Instance.LogInfo("Playback stopped")

        ElseIf audioRouter IsNot Nothing AndAlso audioRouter.IsPlaying Then
            Logger.Instance.Info("⏹️ STOP CLICKED - Synchronous stop starting...", "MainForm")

            ' PHASE 1 FIX: Synchronous stop with immediate UI update
            Try
                ' Stop DSP playback (will complete synchronously now)
                audioRouter.StopDSPPlayback()

                ' Update transport state IMMEDIATELY (don't wait for events!)
                transportControl.State = UI.TransportControl.TransportState.Stopped
                transportControl.TrackPosition = TimeSpan.Zero
                transportControl.TrackDuration = TimeSpan.Zero

                ' Stop timer
                TimerPlayback.Stop()
                progressPlayback.Value = 0
                btnStopPlayback.Enabled = False

                ' Update UI immediately
                panelLED.BackColor = Color.Orange ' Orange = Stopping (will turn yellow when mic armed)
                lblStatus.Text = "Status: Stopping..."

                Logger.Instance.Info("✅ Stop: UI updated immediately (<100ms)", "MainForm")

                ' Re-arm microphone in background (don't block UI)
                Task.Run(Sub()
                             Try
                                 Logger.Instance.Info("Background: Re-arming microphone...", "MainForm")
                                 recordingManager.ArmMicrophone()

                                 ' Update UI when mic is ready (invoke to UI thread)
                                 BeginInvoke(Sub()
                                                 panelLED.BackColor = Color.Yellow
                                                 lblStatus.Text = "Status: Ready (Mic Armed)"
                                                 Logger.Instance.Info("✅ Microphone re-armed successfully", "MainForm")
                                             End Sub)

                             Catch ex As Exception
                                 Logger.Instance.Error("Failed to re-arm microphone", ex, "MainForm")
                                 BeginInvoke(Sub()
                                                 panelLED.BackColor = Color.Gray
                                                 lblStatus.Text = "Status: Idle (Mic Error)"
                                             End Sub)
                             End Try
                         End Sub)

            Catch ex As Exception
                Logger.Instance.Error("❌ Stop failed!", ex, "MainForm")
                MessageBox.Show($"Failed to stop: {ex.Message}", "Stop Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try

        ElseIf recordingManager.IsRecording Then
            Services.LoggingServiceAdapter.Instance.LogInfo("Stopping recording...")
            recordingManager.StopRecording()
        End If
    End Sub

    Private Sub OnTransportPlay(sender As Object, e As EventArgs)
        Services.LoggingServiceAdapter.Instance.LogInfo("Play button clicked")
        If lstRecordings.SelectedItem IsNot Nothing Then
            lstRecordings_DoubleClick(sender, e)
        Else
            Services.LoggingServiceAdapter.Instance.LogWarning("Play attempted with no file selected")
        End If
    End Sub

    Private Sub OnTransportPause(sender As Object, e As EventArgs)
        Services.LoggingServiceAdapter.Instance.LogInfo("Pause button clicked (not yet implemented)")
        MessageBox.Show("Pause functionality coming soon!", "Not Implemented", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub OnTransportPositionChanged(sender As Object, position As TimeSpan)
        If playbackManager IsNot Nothing AndAlso playbackManager.IsPlaying Then
            Logger.Instance.Debug($"Seek requested to: {position}", "MainForm")
        End If
    End Sub

#End Region

#Region "Playback Event Handlers"

    Private Sub btnStopPlayback_Click(sender As Object, e As EventArgs) Handles btnStopPlayback.Click
        ' Stop DSP playback via AudioRouter
        If audioRouter IsNot Nothing AndAlso audioRouter.IsPlaying Then
            audioRouter.StopDSPPlayback()

            ' RE-ARM MICROPHONE after playback stops
            If recordingManager IsNot Nothing Then
                Try
                    recordingManager.ArmMicrophone()
                    Services.LoggingServiceAdapter.Instance.LogInfo("Microphone re-armed after DSP playback")
                Catch ex As Exception
                    Services.LoggingServiceAdapter.Instance.LogError($"Failed to re-arm microphone: {ex.Message}", ex)
                End Try
            End If
        End If

        ' Stop regular playback via PlaybackManager (if any)
        playbackManager?.Stop()

        ' Update UI
        panelLED.BackColor = Color.Yellow ' Yellow = Armed
        lblStatus.Text = "Status: Ready (Mic Armed)"
        TimerPlayback.Stop()
        progressPlayback.Value = 0
        btnStopPlayback.Enabled = False
    End Sub

    Private Sub OnPlaybackStarted(sender As Object, filepath As String)
        ' Start the timer to update playback position
        TimerPlayback.Start()

        ' FIX Issue #2: Set transport state to Playing (lights up indicator!)
        transportControl.State = UI.TransportControl.TransportState.Playing

        ' FIX Issue #1: Set track duration so time display works
        If playbackManager IsNot Nothing Then
            transportControl.TrackDuration = playbackManager.TotalDuration
            transportControl.TrackPosition = TimeSpan.Zero
        End If

        ' DIAGNOSTIC: Log playback start with all details
        Logger.Instance.Info($"🎵 Playback started: {Path.GetFileName(filepath)}", "MainForm")
        Logger.Instance.Info($"   Duration={playbackManager?.TotalDuration}, IsPlaying={playbackManager?.IsPlaying}", "MainForm")
        Logger.Instance.Info($"   Timer started, State set to Playing", "MainForm")

        ' Update UI
        panelLED.BackColor = Color.RoyalBlue
        lblStatus.Text = $"Status: Playing {Path.GetFileName(filepath)}"
        btnStopPlayback.Enabled = True
    End Sub

    Private Sub OnPlaybackStopped(sender As Object, e As NAudio.Wave.StoppedEventArgs)
        ' FIX Issue #2: Reset transport state (turns off indicator!)
        transportControl.State = UI.TransportControl.TransportState.Stopped

        panelLED.BackColor = DarkTheme.SuccessGreen
        lblStatus.Text = "Status: Ready (Mic Armed)"

        TimerPlayback.Stop()
        progressPlayback.Value = 0
        btnStopPlayback.Enabled = False

        ' FIX Issue #1: Reset track position/duration
        transportControl.TrackPosition = TimeSpan.Zero
        transportControl.TrackDuration = TimeSpan.Zero
    End Sub

    Private Sub TimerPlayback_Tick(sender As Object, e As EventArgs) Handles TimerPlayback.Tick
        ' DIAGNOSTIC: Log timer ticks (every 30 ticks = ~0.5s)
        Static tickCount As Integer = 0
        tickCount += 1
        If tickCount Mod 30 = 0 Then
            Logger.Instance.Debug($"⏱️ TimerPlayback_Tick #{tickCount}, IsPlaying={playbackManager?.IsPlaying}, DSP={audioRouter?.IsPlaying}", "MainForm")
        End If

        ' Update playback position (PlaybackManager OR AudioRouter) - EVENT-DRIVEN!
        If playbackManager IsNot Nothing AndAlso playbackManager.IsPlaying Then
            ' PlaybackManager playback (direct WAV)
            playbackManager.UpdatePosition()
        ElseIf audioRouter IsNot Nothing AndAlso audioRouter.IsPlaying Then
            ' AudioRouter/DSP playback - call UpdatePosition() which raises PositionChanged event!
            audioRouter.UpdatePosition()
        End If

        ' Update DSP BOTH input (PRE) and output (POST) samples for FFT comparison at 60 Hz
        If audioRouter IsNot Nothing AndAlso audioRouter.IsPlaying Then
            audioRouter.UpdateInputSamples()  ' PRE-DSP (raw audio)
            audioRouter.UpdateOutputSamples() ' POST-DSP (processed audio)

            ' NOTE: DSP Signal Flow meters removed from timer - now event-driven!
            ' Meters updated via OnRecordingBufferAvailable (for mic) and future file playback events
        End If

        ' REMOVED UpdateTransportState() - events handle state now!
        ' UpdateTransportState was overriding event-driven state changes causing race condition
    End Sub

    Private Sub OnPositionChanged(sender As Object, position As TimeSpan)
        Dim total = playbackManager.TotalDuration
        If total.TotalMilliseconds > 0 Then
            Dim pct = CInt((position.TotalMilliseconds / total.TotalMilliseconds) * 1000)
            progressPlayback.Value = Math.Min(1000, Math.Max(0, pct))
        End If
    End Sub

    ''' <summary>
    ''' AudioRouter playback started (DSP playback) - matches RecordingManager pattern
    ''' </summary>
    Private Sub OnAudioRouterPlaybackStarted(sender As Object, filename As String)
        Logger.Instance.Info($"🎵 AudioRouter playback started: {filename}", "MainForm")

        ' Wire DSP panel to the GainProcessor (now that it exists)
        If audioRouter.GainProcessor IsNot Nothing Then
            DspSignalFlowPanel1.SetGainProcessor(audioRouter.GainProcessor)
            Logger.Instance.Info("✅ DSPSignalFlowPanel wired to AudioRouter.GainProcessor", "MainForm")
        End If

        ' Update TransportControl (event-driven, like RecordingManager!)
        transportControl.State = UI.TransportControl.TransportState.Playing
        transportControl.TrackDuration = audioRouter.TotalDuration
        transportControl.TrackPosition = TimeSpan.Zero

        ' Update UI
        panelLED.BackColor = Color.Magenta ' Magenta = DSP Processing
        lblStatus.Text = $"Status: DSP Playback - {filename}"
        btnStopPlayback.Enabled = True
    End Sub

    ''' <summary>
    ''' AudioRouter playback stopped (DSP playback) - matches RecordingManager pattern
    ''' </summary>
    Private Sub OnAudioRouterPlaybackStopped(sender As Object, e As EventArgs)
        Try
            Logger.Instance.Info("🛑 AudioRouter playback stopped - NATURAL EOF DETECTED!", "MainForm")

            ' PHASE 1 FIX: Fast-path EOF handling with immediate UI update

            ' Update TransportControl immediately
            transportControl.State = UI.TransportControl.TransportState.Stopped
            transportControl.TrackPosition = TimeSpan.Zero
            transportControl.TrackDuration = TimeSpan.Zero

            ' Stop timer
            TimerPlayback.Stop()
            progressPlayback.Value = 0
            btnStopPlayback.Enabled = False

            ' Update UI immediately
            panelLED.BackColor = Color.Orange ' Orange = Stopping (will turn yellow when mic armed)
            lblStatus.Text = "Status: Playback Complete, Re-arming..."

            Logger.Instance.Info("✅ EOF: UI updated immediately (<50ms)", "MainForm")

            ' Re-arm microphone in background (don't block)
            Task.Run(Sub()
                         Try
                             Logger.Instance.Info("Background: Re-arming microphone after EOF...", "MainForm")
                             recordingManager.ArmMicrophone()

                             ' Update UI when mic is ready
                             BeginInvoke(Sub()
                                             panelLED.BackColor = Color.Yellow
                                             lblStatus.Text = "Status: Ready (Mic Armed)"
                                             Logger.Instance.Info("✅ Microphone re-armed after EOF", "MainForm")
                                         End Sub)

                         Catch ex As Exception
                             Logger.Instance.Error("Failed to re-arm microphone after EOF", ex, "MainForm")
                             BeginInvoke(Sub()
                                             panelLED.BackColor = Color.Gray
                                             lblStatus.Text = "Status: Idle (Mic Error)"
                                         End Sub)
                         End Try
                     End Sub)

        Catch ex As Exception
            Logger.Instance.Error("❌ ERROR in OnAudioRouterPlaybackStopped!", ex, "MainForm")
        End Try
    End Sub

    ''' <summary>
    ''' AudioRouter position updated (DSP playback) - matches RecordingManager pattern
    ''' </summary>
    Private Sub OnAudioRouterPositionChanged(sender As Object, position As TimeSpan)
        ' Update TransportControl (event-driven, like RecordingManager!)
        transportControl.TrackPosition = position

        ' Update progress bar
        Dim total = audioRouter.TotalDuration
        If total.TotalMilliseconds > 0 Then
            Dim pct = CInt((position.TotalMilliseconds / total.TotalMilliseconds) * 1000)
            progressPlayback.Value = Math.Min(1000, Math.Max(0, pct))
        End If
    End Sub

    ''' <summary>
    ''' Calculate peak level in dB for a specific channel
    ''' </summary>
    Private Function CalculatePeakDb(samples As Single(), channel As Integer) As Single
        If samples Is Nothing OrElse samples.Length = 0 Then
            Return -60.0F
        End If

        Dim peak As Single = 0.0F
        Dim channels As Integer = 2 ' Stereo

        ' Find peak sample for the specified channel (interleaved stereo)
        For i As Integer = channel To samples.Length - 1 Step channels
            Dim absSample = Math.Abs(samples(i))
            If absSample > peak Then
                peak = absSample
            End If
        Next

        ' Convert to dB (with floor at -60dB)
        If peak < 0.00001F Then ' -100dB
            Return -60.0F
        End If

        Dim db = 20.0F * CSng(Math.Log10(peak))
        Return Math.Max(-60.0F, Math.Min(0.0F, db))
    End Function




    Private Sub UpdateTransportState()
        If recordingManager IsNot Nothing AndAlso recordingManager.IsRecording Then
            transportControl.State = UI.TransportControl.TransportState.Recording
            transportControl.RecordingTime = recordingManager.RecordingDuration
        ElseIf playbackManager IsNot Nothing AndAlso playbackManager.IsPlaying Then
            transportControl.State = UI.TransportControl.TransportState.Playing
            transportControl.TrackPosition = playbackManager.CurrentPosition
            transportControl.TrackDuration = playbackManager.TotalDuration
        Else
            transportControl.State = UI.TransportControl.TransportState.Stopped
        End If
    End Sub

    ''' <summary>Handle DSP input samples for FFT (PRE-DSP - raw audio)</summary>
    Private Sub OnDSPInputSamples(sender As Object, e As AudioIO.AudioSamplesEventArgs)
        Try
            ' DIAGNOSTIC: Log that we received the event
            Static callCount As Integer = 0
            callCount += 1
            If callCount Mod 10 = 0 Then
                Services.LoggingServiceAdapter.Instance.LogInfo($"OnDSPInputSamples: Event received! Count={callCount}, Samples={e.Count} bytes")
            End If

            ' Process through SpectrumManager (Task 2.0.4)
            Dim spectrumInput = spectrumManager.ProcessInputSamples(e.Samples, e.Count, e.BitsPerSample, e.Channels, e.SampleRate)

            If spectrumInput IsNot Nothing AndAlso spectrumInput.Length > 0 Then
                SpectrumAnalyzerControl1.InputDisplay.UpdateSpectrum(spectrumInput, e.SampleRate, spectrumManager.FFTSize)
            Else
                ' Log if spectrum is null/empty
                If callCount Mod 10 = 0 Then
                    Services.LoggingServiceAdapter.Instance.LogWarning($"OnDSPInputSamples: Spectrum is NULL or empty!")
                End If
            End If
        Catch ex As Exception
            ' Log FFT errors instead of ignoring
            Services.LoggingServiceAdapter.Instance.LogError($"OnDSPInputSamples error: {ex.Message}", ex)
        End Try
    End Sub

    ''' <summary>Handle DSP output samples for FFT (POST-DSP - processed audio)</summary>
    Private Sub OnDSPOutputSamples(sender As Object, e As AudioIO.AudioSamplesEventArgs)
        Try
            ' DIAGNOSTIC: Log that we received the event
            Static callCount As Integer = 0
            callCount += 1
            If callCount Mod 10 = 0 Then
                Services.LoggingServiceAdapter.Instance.LogInfo($"OnDSPOutputSamples: Event received! Count={callCount}, Samples={e.Count} bytes")
            End If

            ' Process through SpectrumManager (Task 2.0.4)
            Dim spectrumOutput = spectrumManager.ProcessOutputSamples(e.Samples, e.Count, e.BitsPerSample, e.Channels, e.SampleRate)

            If spectrumOutput IsNot Nothing AndAlso spectrumOutput.Length > 0 Then
                SpectrumAnalyzerControl1.OutputDisplay.UpdateSpectrum(spectrumOutput, e.SampleRate, spectrumManager.FFTSize)
            Else
                ' Log if spectrum is null/empty
                If callCount Mod 10 = 0 Then
                    Services.LoggingServiceAdapter.Instance.LogWarning($"OnDSPOutputSamples: Spectrum is NULL or empty!")
                End If
            End If

            ' DIAGNOSTIC: Calculate TRUE peak level from audio samples (every second)
            Static lastLogTime As DateTime = DateTime.MinValue
            If (DateTime.Now - lastLogTime).TotalSeconds >= 1.0 Then
                Dim peakLevel = CalculateTruePeakDB(e.Samples, e.Count, e.BitsPerSample)
                Services.LoggingServiceAdapter.Instance.LogInfo($"TRUE Audio Peak: {peakLevel:F1} dBFS (not FFT!)")
                lastLogTime = DateTime.Now
            End If

        Catch ex As Exception
            ' Log FFT errors instead of ignoring
            Services.LoggingServiceAdapter.Instance.LogError($"OnDSPOutputSamples error: {ex.Message}", ex)
        End Try
    End Sub

    ''' <summary>Handle playback completion (file reached EOF naturally)</summary>
    Private Sub OnPlaybackCompleted(sender As Object, e As EventArgs)
        Try
            Services.LoggingServiceAdapter.Instance.LogInfo("Playback completed naturally (EOF reached)")
            Logger.Instance.Info("Playback completed naturally", "MainForm")

            ' Update transport control to stopped state
            If InvokeRequired Then
                Invoke(New Action(Sub()
                                      transportControl.State = UI.TransportControl.TransportState.Stopped
                                      panelLED.BackColor = Color.Yellow
                                      lblStatus.Text = "Status: Ready (Mic Armed)"
                                  End Sub))
            Else
                transportControl.State = UI.TransportControl.TransportState.Stopped
                panelLED.BackColor = Color.Yellow
                lblStatus.Text = "Status: Ready (Mic Armed)"
            End If
        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"OnPlaybackCompleted error: {ex.Message}", ex)
        End Try
    End Sub

    ''' <summary>Calculate TRUE peak level in dBFS from audio samples</summary>
    Private Function CalculateTruePeakDB(samples As Byte(), count As Integer, bitsPerSample As Integer) As Single
        If samples Is Nothing OrElse count = 0 Then Return -96.0F

        Dim maxSample As Single = 0.0F

        If bitsPerSample = 16 Then
            ' 16-bit PCM
            For i = 0 To count - 1 Step 2
                If i + 1 < count Then
                    Dim sample = Math.Abs(BitConverter.ToInt16(samples, i))
                    If sample > maxSample Then maxSample = sample
                End If
            Next
            maxSample /= 32768.0F ' Normalize to 0.0-1.0
        End If

        ' Convert to dB
        If maxSample < 0.00001F Then Return -96.0F ' Silence threshold
        Return 20.0F * Math.Log10(maxSample)
    End Function

    ''' <summary>
    ''' Handle spectrum ready event from FFT monitor thread (Phase 3 - NEW!)
    ''' This replaces the old OnPipelineMonitoring event handler.
    ''' Already throttled to 20 FPS by FFT thread - no throttling needed here!
    ''' </summary>
    Private Sub OnSpectrumReady(sender As Object, e As DSP.FFT.SpectrumReadyEventArgs)
        ' Simple UI update - already throttled by FFT thread
        If Me.InvokeRequired Then
            Me.BeginInvoke(New Action(Sub() UpdateSpectrum(e)))
        Else
            UpdateSpectrum(e)
        End If
    End Sub

    ''' <summary>
    ''' Update spectrum display on UI thread (Phase 3 - NEW!)
    ''' </summary>
    Private Sub UpdateSpectrum(e As DSP.FFT.SpectrumReadyEventArgs)
        Try
            If e.TapPoint = Audio.Routing.TapPoint.PreDSP Then
                ' Pre-DSP = Input display (raw audio)
                SpectrumAnalyzerControl1.InputDisplay.UpdateSpectrum(e.Spectrum, e.SampleRate, e.FFTSize)
            Else
                ' Post-DSP = Output display (processed audio)
                SpectrumAnalyzerControl1.OutputDisplay.UpdateSpectrum(e.Spectrum, e.SampleRate, e.FFTSize)
            End If
        Catch ex As Exception
            ' Ignore UI errors (don't crash on spectrum update failure)
            Logger.Instance.Debug($"UpdateSpectrum error: {ex.Message}", "MainForm")
        End Try
    End Sub

#End Region

#Region "Logging"

    Private Sub OnLogMessage(sender As Object, e As Services.Interfaces.LogMessageEventArgs)
        ' Thread-safe UI update
        If txtLogViewer.InvokeRequired Then
            txtLogViewer.Invoke(Sub() AppendLogMessage(e.Message, e.Level))
        Else
            AppendLogMessage(e.Message, e.Level)
        End If
    End Sub

    Private Sub AppendLogMessage(message As String, level As Services.Interfaces.LogLevel)
        ' Color-code by level
        Dim color As Color
        Select Case level
            Case Services.Interfaces.LogLevel.Error
                color = Color.FromArgb(255, 100, 100) ' Light red
            Case Services.Interfaces.LogLevel.Warning
                color = Color.FromArgb(255, 200, 100) ' Orange
            Case Else
                color = Color.White
        End Select

        ' Append with color
        txtLogViewer.SelectionStart = txtLogViewer.TextLength
        txtLogViewer.SelectionLength = 0
        txtLogViewer.SelectionColor = color
        txtLogViewer.AppendText(message & vbCrLf)
        txtLogViewer.SelectionColor = txtLogViewer.ForeColor

        ' Auto-scroll if enabled
        If chkAutoScroll.Checked Then
            txtLogViewer.SelectionStart = txtLogViewer.TextLength
            txtLogViewer.ScrollToCaret()
        End If
    End Sub

    Private Sub btnClearLogs_Click(sender As Object, e As EventArgs) Handles btnClearLogs.Click
        txtLogViewer.Clear()
        Services.LoggingServiceAdapter.Instance.ClearLog()
        Services.LoggingServiceAdapter.Instance.LogInfo("Log viewer cleared")
    End Sub

    Private Sub btnSaveLogs_Click(sender As Object, e As EventArgs) Handles btnSaveLogs.Click
        Try
            ' Get all log entries
            Dim logs = Services.LoggingServiceAdapter.Instance.GetLogEntries()

            ' Create save dialog
            Using sfd As New SaveFileDialog()
                sfd.Filter = "Text Files (*.txt)|*.txt|Log Files (*.log)|*.log|All Files (*.*)|*.*"
                sfd.DefaultExt = "txt"
                sfd.FileName = $"DSP_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                sfd.InitialDirectory = Path.Combine(Application.StartupPath, "Logs")

                If sfd.ShowDialog() = DialogResult.OK Then
                    File.WriteAllLines(sfd.FileName, logs)
                    Services.LoggingServiceAdapter.Instance.LogInfo($"Logs exported to: {Path.GetFileName(sfd.FileName)}")
                    MessageBox.Show($"Logs saved to:{vbCrLf}{sfd.FileName}", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to save logs: {ex.Message}", ex)
            MessageBox.Show($"Failed to save logs: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub cmbLogLevel_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbLogLevel.SelectedIndexChanged
        ' Update filter level
        Select Case cmbLogLevel.SelectedItem?.ToString()
            Case "All", "Debug"
                Services.LoggingServiceAdapter.Instance.FilterLevel = Nothing
            Case "Info"
                Services.LoggingServiceAdapter.Instance.FilterLevel = Services.Interfaces.LogLevel.Info
            Case "Warn", "Warning"
                Services.LoggingServiceAdapter.Instance.FilterLevel = Services.Interfaces.LogLevel.Warning
            Case "Error", "Fatal"
                Services.LoggingServiceAdapter.Instance.FilterLevel = Services.Interfaces.LogLevel.Error
        End Select

        Services.LoggingServiceAdapter.Instance.LogInfo($"Log filter changed to: {cmbLogLevel.SelectedItem}")
    End Sub

#End Region

#Region "Audio Routing (Phase 2.0)"

    ''' <summary>Handle browse for input file button - called by RoutingPanel</summary>
    Private Sub OnBrowseInputFileClick(sender As Object, e As EventArgs)
        Try
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Title = "Select Audio File for DSP Playback"
                openFileDialog.Filter = "WAV Files (*.wav)|*.wav|All Audio Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*"
                openFileDialog.InitialDirectory = Path.Combine(Application.StartupPath, "Recordings")

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    Dim selectedFile = openFileDialog.FileName

                    ' Update RoutingPanel display
                    RoutingPanel1.SelectedFilePath = Path.GetFileName(selectedFile)

                    ' Play file through AudioRouter (Task 2.0.2 - encapsulated method)
                    Try
                        audioRouter.PlayFile(selectedFile)

                        ' Update UI
                        panelLED.BackColor = Color.Magenta ' Magenta = DSP Processing
                        lblStatus.Text = $"Status: DSP Playback - {Path.GetFileName(selectedFile)}"

                        Services.LoggingServiceAdapter.Instance.LogInfo($"DSP playback started: {selectedFile}")
                        Logger.Instance.Info($"DSP playback started: {selectedFile}", "MainForm")

                        MessageBox.Show($"DSP Playback Started!{Environment.NewLine}{Environment.NewLine}File: {Path.GetFileName(selectedFile)}{Environment.NewLine}Routing: File → DSP Chain → Speakers{Environment.NewLine}{Environment.NewLine}(Pass-through test - no processing yet)", "DSP Routing Active", MessageBoxButtons.OK, MessageBoxIcon.Information)

                    Catch ex As Exception
                        Services.LoggingServiceAdapter.Instance.LogError($"Failed to start DSP playback: {ex.Message}", ex)
                        MessageBox.Show($"Failed to start DSP playback:{Environment.NewLine}{ex.Message}", "DSP Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

                        ' Reset UI
                        panelLED.BackColor = Color.Yellow
                        lblStatus.Text = "Status: Ready (Mic Armed)"
                    End Try
                End If
            End Using
        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to browse for input file: {ex.Message}", ex)
        End Try
    End Sub

#End Region

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        ' Unsubscribe from events
        RemoveHandler Services.LoggingServiceAdapter.Instance.LogMessageReceived, AddressOf OnLogMessage

        ' Log shutdown
        Services.LoggingServiceAdapter.Instance.LogInfo("DSP Processor shutting down")

        ' Dispose managers
        recordingManager?.Dispose()
        audioRouter?.Dispose()
        spectrumManager?.Dispose() ' Task 2.0.4

        ' Cleanup modules
        playbackManager?.Dispose()
        WaveformDisplayControl1?.Dispose()

        ' Close logger
        Logger.Instance.Close()

        MyBase.OnFormClosing(e)
    End Sub

    Private Sub tabProgram_Click(sender As Object, e As EventArgs) Handles tabProgram.Click

    End Sub
End Class
