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

    ' Audio Router (Phase 2.0)
    Private audioRouter As AudioIO.AudioRouter

    ' ===== TEST: AudioPipelineRouter (Phase 1 Foundation - DORMANT) =====
    ' Uncomment to test the router independently:
    ' Private testRouter As AudioPipelineRouter

    ' FFT processing for spectrum display (separate processors for INPUT and OUTPUT)
    Private fftProcessorInput As DSP.FFT.FFTProcessor
    Private fftProcessorOutput As DSP.FFT.FFTProcessor
    
    ' Flag to prevent FFT queue buildup
    Private fftProcessingInProgress As Boolean = False

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

        ' Create FFT processors for spectrum analysis (separate for INPUT and OUTPUT)
        fftProcessorInput = New DSP.FFT.FFTProcessor(4096) With {
            .SampleRate = 44100,
            .WindowFunction = DSP.FFT.FFTProcessor.WindowType.Hann
        }

        fftProcessorOutput = New DSP.FFT.FFTProcessor(4096) With {
            .SampleRate = 44100,
            .WindowFunction = DSP.FFT.FFTProcessor.WindowType.Hann
        }

        ' NOW wire up events (all objects exist!)
        WireManagerEvents()
        WirePlaybackEvents()
        WireTransportEvents()
        WireUIEvents()
        WireAudioSettingsPanel()
        WireInputTabPanel()

        ' Load all settings (this will trigger OnSettingsLoaded event)
        settingsManager.LoadAll()

        ' Apply settings to AudioSettingsPanel and InputTabPanel
        AudioSettingsPanel1.LoadSettings(settingsManager.AudioSettings)
        InputTabPanel1.LoadSettings(settingsManager.MeterSettings)

        ' Apply meter settings
        ApplyMeterSettings(settingsManager.MeterSettings)

        ' Apply spectrum settings
        ApplySpectrumSettings(settingsManager.SpectrumSettings)

        ' Apply dark theme to visualization tabs
        DarkTheme.ApplyToControl(visualizationTabs)

        ' Initialize volume controls
        trackVolume.Value = 100
        lblVolume.Text = "100%"

        ' Start timers for UI updates
        TimerMeters.Start() ' For volume meters
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
        AddHandler playbackManager.PlaybackStarted, AddressOf OnPlaybackStarted
        AddHandler playbackManager.PlaybackStopped, AddressOf OnPlaybackStopped
        AddHandler playbackManager.PositionChanged, AddressOf OnPositionChanged
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

        ' Wire up routing UI events (Phase 2.0)
        AddHandler radioMicrophone.CheckedChanged, AddressOf OnInputSourceChanged
        AddHandler radioFilePlayback.CheckedChanged, AddressOf OnInputSourceChanged
        AddHandler cmbOutputDevice.SelectedIndexChanged, AddressOf OnOutputDeviceChanged

        ' Wire up file browser button
        AddHandler btnBrowseInputFile.Click, AddressOf OnBrowseInputFileClick

        ' Wire up AudioRouter FFT events
        AddHandler audioRouter.InputSamplesAvailable, AddressOf OnDSPInputSamples
        AddHandler audioRouter.OutputSamplesAvailable, AddressOf OnDSPOutputSamples
        AddHandler audioRouter.PlaybackCompleted, AddressOf OnPlaybackCompleted
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

        ' Populate output devices in routing UI (Phase 2.0)
        PopulateOutputDevices()

        ' Now arm the microphone
        Try
            recordingManager.ArmMicrophone()
        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to arm microphone: {ex.Message}", ex)
            MessageBox.Show($"Warning: Failed to initialize audio input.{Environment.NewLine}{ex.Message}", "Audio Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try

        ' Refresh file list
        fileManager.RefreshFileList()

        ' Update UI state
        lblStatus.Text = "Status: Ready (Mic Armed)"
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

        ' Clear FFT buffers for fresh spectrum
        fftProcessorInput.Clear()
        fftProcessorOutput.Clear()

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
        ' FAST PATH: Update meter immediately (must be real-time)
        Try
            Dim levelData = AudioLevelMeter.AnalyzeSamples(e.Buffer, e.BitsPerSample, e.Channels)
            meterRecording.SetLevel(levelData.PeakDB, levelData.RMSDB, levelData.IsClipping)
        Catch
            ' Ignore metering errors
        End Try

        ' ASYNC PATH: FFT processing off the audio thread (can drop frames if too slow)
        ' Skip if already processing to prevent queue buildup
        If Not fftProcessingInProgress Then
            fftProcessingInProgress = True
            
            ' Make a copy of the buffer for async processing
            Dim bufferCopy(e.Buffer.Length - 1) As Byte
            Array.Copy(e.Buffer, bufferCopy, e.Buffer.Length)
            Dim sampleRate = e.SampleRate
            Dim bitsPerSample = e.BitsPerSample
            Dim channels = e.Channels
            
            ' Fire and forget - process FFT on background thread
            Task.Run(Sub()
                Try
                    ' Add samples and calculate spectrum (CPU-intensive)
                    fftProcessorInput.SampleRate = sampleRate
                    fftProcessorInput.AddSamples(bufferCopy, bufferCopy.Length, bitsPerSample, channels)
                    Dim spectrum = fftProcessorInput.CalculateSpectrum()

                    If spectrum IsNot Nothing AndAlso spectrum.Length > 0 Then
                        ' Update UI on UI thread
                        Me.BeginInvoke(New Action(Sub()
                            Try
                                SpectrumAnalyzerControl1.InputDisplay.UpdateSpectrum(spectrum, sampleRate, fftProcessorInput.FFTSize)
                            Catch
                                ' Ignore UI update errors
                            End Try
                        End Sub))
                    End If
                Catch
                    ' Ignore FFT errors (freewheeling - can drop frames)
                Finally
                    fftProcessingInProgress = False
                End Try
            End Sub)
        End If
    End Sub

    Private Sub OnMicrophoneArmed(sender As Object, isArmed As Boolean)
        If isArmed Then
            panelLED.BackColor = Color.Yellow
            lblStatus.Text = "Status: Ready (Mic Armed)"
            transportControl.IsRecordArmed = True
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
            ' Apply FFT settings
            fftProcessorInput.FFTSize = settings.FFTSize
            fftProcessorOutput.FFTSize = settings.FFTSize

            ' Apply window function
            Dim windowType As DSP.FFT.FFTProcessor.WindowType
            Select Case settings.WindowFunction
                Case "None"
                    windowType = DSP.FFT.FFTProcessor.WindowType.None
                Case "Hann"
                    windowType = DSP.FFT.FFTProcessor.WindowType.Hann
                Case "Hamming"
                    windowType = DSP.FFT.FFTProcessor.WindowType.Hamming
                Case "Blackman"
                    windowType = DSP.FFT.FFTProcessor.WindowType.Blackman
                Case Else
                    windowType = DSP.FFT.FFTProcessor.WindowType.Hann
            End Select
            fftProcessorInput.WindowFunction = windowType
            fftProcessorOutput.WindowFunction = windowType

            ' Apply to UI controls
            cmbFFTSize.SelectedItem = settings.FFTSize.ToString()
            cmbWindowFunction.SelectedItem = settings.WindowFunction
            numSmoothing.Value = settings.Smoothing
            chkPeakHold.Checked = settings.PeakHoldEnabled

            ' Apply frequency range
            trackMinFreq.Value = settings.MinFrequency
            trackMaxFreq.Value = settings.MaxFrequency
            lblMinFreqValue.Text = $"{settings.MinFrequency} Hz"
            lblMaxFreqValue.Text = $"{settings.MaxFrequency} Hz"

            ' Apply dB range
            trackDBRange.Value = settings.MinDB
            lblDBRangeValue.Text = $"{settings.MinDB} dB"

            ' Apply to spectrum displays
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

        ' Apply to recorder
        recordingManager.Options = options

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

            ' Route through DSP pipeline (AudioRouter)
            audioRouter.SelectedInputFile = fullPath
            audioRouter.StartDSPPlayback()

            panelLED.BackColor = Color.Magenta ' Magenta = DSP Processing
            lblStatus.Text = $"Status: DSP Playback - {fileName}"

            ' Start timer for FFT updates
            TimerPlayback.Start()

            ' Enable stop button
            btnStopPlayback.Enabled = True

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
            Services.LoggingServiceAdapter.Instance.LogInfo("Stopping DSP playback...")
            audioRouter.StopDSPPlayback()
            panelLED.BackColor = Color.Yellow
            lblStatus.Text = "Status: Ready (Mic Armed)"
            Services.LoggingServiceAdapter.Instance.LogInfo("DSP playback stopped")
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

    Private Sub trackVolume_Scroll(sender As Object, e As EventArgs) Handles trackVolume.Scroll
        ' Update playback volume via PlaybackManager
        Dim volumePercent = trackVolume.Value
        playbackManager.Volume = volumePercent / 100.0F
        lblVolume.Text = $"{volumePercent}%"

        Services.LoggingServiceAdapter.Instance.LogInfo($"Playback volume changed: {volumePercent}%")
    End Sub

    Private Sub OnPlaybackStarted(sender As Object, filepath As String)
        ' Start the timer to update playback position
        TimerPlayback.Start()

        ' Update UI
        panelLED.BackColor = Color.RoyalBlue
        lblStatus.Text = $"Status: Playing {Path.GetFileName(filepath)}"
        btnStopPlayback.Enabled = True
    End Sub

    Private Sub OnPlaybackStopped(sender As Object, e As NAudio.Wave.StoppedEventArgs)
        panelLED.BackColor = DarkTheme.SuccessGreen
        lblStatus.Text = "Status: Ready (Mic Armed)"

        TimerPlayback.Stop()
        progressPlayback.Value = 0
        btnStopPlayback.Enabled = False
    End Sub

    Private Sub TimerPlayback_Tick(sender As Object, e As EventArgs) Handles TimerPlayback.Tick
        ' Update regular playback position
        If playbackManager IsNot Nothing AndAlso playbackManager.IsPlaying Then
            playbackManager.UpdatePosition()
            transportControl.TrackPosition = playbackManager.CurrentPosition
            transportControl.TrackDuration = playbackManager.TotalDuration

            ' DIAGNOSTIC: Log playback position every 30 ticks (~0.5 seconds)
            Static tickCount As Integer = 0
            tickCount += 1
            If tickCount Mod 30 = 0 Then
                Logger.Instance.Debug($"Playback position: {playbackManager.CurrentPosition} / {playbackManager.TotalDuration}", "MainForm")
            End If
        End If

        ' Update DSP BOTH input (PRE) and output (POST) samples for FFT comparison at 60 Hz
        If audioRouter IsNot Nothing AndAlso audioRouter.IsPlaying Then
            audioRouter.UpdateInputSamples()  ' PRE-DSP (raw audio)
            audioRouter.UpdateOutputSamples() ' POST-DSP (processed audio)
        End If

        UpdateTransportState()
    End Sub

    Private Sub OnPositionChanged(sender As Object, position As TimeSpan)
        Dim total = playbackManager.TotalDuration
        If total.TotalMilliseconds > 0 Then
            Dim pct = CInt((position.TotalMilliseconds / total.TotalMilliseconds) * 1000)
            progressPlayback.Value = Math.Min(1000, Math.Max(0, pct))
        End If
    End Sub

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

            ' Feed RAW audio to INPUT display (BEFORE any processing)
            fftProcessorInput.SampleRate = e.SampleRate
            fftProcessorInput.AddSamples(e.Samples, e.Count, e.BitsPerSample, e.Channels)
            Dim spectrumInput = fftProcessorInput.CalculateSpectrum()

            If spectrumInput IsNot Nothing AndAlso spectrumInput.Length > 0 Then
                SpectrumAnalyzerControl1.InputDisplay.UpdateSpectrum(spectrumInput, e.SampleRate, fftProcessorInput.FFTSize)
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

            ' Feed PROCESSED audio to OUTPUT display (AFTER DSP processing)
            fftProcessorOutput.SampleRate = e.SampleRate
            fftProcessorOutput.AddSamples(e.Samples, e.Count, e.BitsPerSample, e.Channels)
            Dim spectrumOutput = fftProcessorOutput.CalculateSpectrum()

            If spectrumOutput IsNot Nothing AndAlso spectrumOutput.Length > 0 Then
                SpectrumAnalyzerControl1.OutputDisplay.UpdateSpectrum(spectrumOutput, e.SampleRate, fftProcessorOutput.FFTSize)
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

#Region "Spectrum Analyzer Event Handlers"

    Private Sub cmbFFTSize_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFFTSize.SelectedIndexChanged
        If cmbFFTSize.SelectedItem IsNot Nothing AndAlso settingsManager IsNot Nothing Then
            Dim fftSize = Integer.Parse(cmbFFTSize.SelectedItem.ToString())
            fftProcessorInput.FFTSize = fftSize
            fftProcessorOutput.FFTSize = fftSize
            settingsManager.SpectrumSettings.FFTSize = fftSize
            settingsManager.SaveAll()
            Services.LoggingServiceAdapter.Instance.LogInfo($"FFT size changed to: {fftSize}")
        End If
    End Sub

    Private Sub cmbWindowFunction_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbWindowFunction.SelectedIndexChanged
        If cmbWindowFunction.SelectedItem IsNot Nothing AndAlso settingsManager IsNot Nothing Then
            Dim windowType As DSP.FFT.FFTProcessor.WindowType
            Select Case cmbWindowFunction.SelectedItem.ToString()
                Case "None"
                    windowType = DSP.FFT.FFTProcessor.WindowType.None
                Case "Hann"
                    windowType = DSP.FFT.FFTProcessor.WindowType.Hann
                Case "Hamming"
                    windowType = DSP.FFT.FFTProcessor.WindowType.Hamming
                Case "Blackman"
                    windowType = DSP.FFT.FFTProcessor.WindowType.Blackman
                Case Else
                    windowType = DSP.FFT.FFTProcessor.WindowType.Hann
            End Select
            fftProcessorInput.WindowFunction = windowType
            fftProcessorOutput.WindowFunction = windowType
            settingsManager.SpectrumSettings.WindowFunction = cmbWindowFunction.SelectedItem.ToString()
            settingsManager.SaveAll()
            Services.LoggingServiceAdapter.Instance.LogInfo($"Window function changed to: {cmbWindowFunction.SelectedItem}")
        End If
    End Sub

    Private Sub numSmoothing_ValueChanged(sender As Object, e As EventArgs) Handles numSmoothing.ValueChanged
        ' Guard against firing during initialization
        If settingsManager Is Nothing OrElse SpectrumAnalyzerControl1 Is Nothing Then Return

        ' Convert percentage (0-100) to factor (0.0-1.0) and apply to both displays
        Dim smoothingFactor = CSng(numSmoothing.Value / 100)
        SpectrumAnalyzerControl1.InputDisplay.SmoothingFactor = smoothingFactor
        SpectrumAnalyzerControl1.OutputDisplay.SmoothingFactor = smoothingFactor
        settingsManager.SpectrumSettings.Smoothing = CInt(numSmoothing.Value)
        settingsManager.SaveAll()
        Services.LoggingServiceAdapter.Instance.LogInfo($"Smoothing changed to: {numSmoothing.Value}%")
    End Sub

    Private Sub chkPeakHold_CheckedChanged(sender As Object, e As EventArgs) Handles chkPeakHold.CheckedChanged
        ' Guard against firing during initialization
        If settingsManager Is Nothing OrElse SpectrumAnalyzerControl1 Is Nothing Then Return

        ' Apply to both displays
        SpectrumAnalyzerControl1.InputDisplay.PeakHoldEnabled = chkPeakHold.Checked
        SpectrumAnalyzerControl1.OutputDisplay.PeakHoldEnabled = chkPeakHold.Checked
        settingsManager.SpectrumSettings.PeakHoldEnabled = chkPeakHold.Checked
        settingsManager.SaveAll()
        Services.LoggingServiceAdapter.Instance.LogInfo($"Peak hold: {If(chkPeakHold.Checked, "enabled", "disabled")}")
    End Sub

    Private Sub btnResetSpectrum_Click(sender As Object, e As EventArgs) Handles btnResetSpectrum.Click
        ' Clear both PRE and POST displays
        SpectrumAnalyzerControl1.InputDisplay.Clear()
        SpectrumAnalyzerControl1.OutputDisplay.Clear()
        fftProcessorInput.Clear()
        fftProcessorOutput.Clear()
        Services.LoggingServiceAdapter.Instance.LogInfo("Spectrum analyzer reset")
    End Sub

    Private Sub trackMinFreq_Scroll(sender As Object, e As EventArgs) Handles trackMinFreq.Scroll
        ' Guard against firing during initialization
        If settingsManager Is Nothing OrElse SpectrumAnalyzerControl1 Is Nothing Then Return

        ' Update both displays
        SpectrumAnalyzerControl1.InputDisplay.MinFrequency = trackMinFreq.Value
        SpectrumAnalyzerControl1.OutputDisplay.MinFrequency = trackMinFreq.Value
        lblMinFreqValue.Text = $"{trackMinFreq.Value} Hz"
        settingsManager.SpectrumSettings.MinFrequency = trackMinFreq.Value
        settingsManager.SaveAll()
        Services.LoggingServiceAdapter.Instance.LogInfo($"Min frequency: {trackMinFreq.Value} Hz")
    End Sub

    Private Sub trackMaxFreq_Scroll(sender As Object, e As EventArgs) Handles trackMaxFreq.Scroll
        ' Guard against firing during initialization
        If settingsManager Is Nothing OrElse SpectrumAnalyzerControl1 Is Nothing Then Return

        ' Update both displays
        SpectrumAnalyzerControl1.InputDisplay.MaxFrequency = trackMaxFreq.Value
        SpectrumAnalyzerControl1.OutputDisplay.MaxFrequency = trackMaxFreq.Value
        lblMaxFreqValue.Text = $"{trackMaxFreq.Value} Hz"
        settingsManager.SpectrumSettings.MaxFrequency = trackMaxFreq.Value
        settingsManager.SaveAll()
        Services.LoggingServiceAdapter.Instance.LogInfo($"Max frequency: {trackMaxFreq.Value} Hz")
    End Sub

    Private Sub trackDBRange_Scroll(sender As Object, e As EventArgs) Handles trackDBRange.Scroll
        ' Guard against firing during initialization
        If settingsManager Is Nothing OrElse SpectrumAnalyzerControl1 Is Nothing Then Return

        ' Update both displays - trackbar value is negative (-100 to -20)
        Dim minDB = trackDBRange.Value
        SpectrumAnalyzerControl1.InputDisplay.MinDB = minDB
        SpectrumAnalyzerControl1.OutputDisplay.MinDB = minDB
        lblDBRangeValue.Text = $"{minDB} dB"
        settingsManager.SpectrumSettings.MinDB = minDB
        settingsManager.SaveAll()
        Services.LoggingServiceAdapter.Instance.LogInfo($"dB range (min): {minDB} dB")
    End Sub

#End Region

#Region "Audio Routing (Phase 2.0)"

    ''' <summary>Populate output device dropdown</summary>
    Private Sub PopulateOutputDevices()
        Try
            cmbOutputDevice.Items.Clear()

            Dim deviceNames = audioRouter.GetOutputDeviceNames()
            cmbOutputDevice.Items.AddRange(deviceNames)

            ' Select current device
            Dim selectedDevice = audioRouter.GetSelectedOutputDevice()
            If selectedDevice >= 0 AndAlso selectedDevice < cmbOutputDevice.Items.Count Then
                cmbOutputDevice.SelectedIndex = selectedDevice
            ElseIf cmbOutputDevice.Items.Count > 0 Then
                cmbOutputDevice.SelectedIndex = 0
            End If

            Services.LoggingServiceAdapter.Instance.LogInfo($"Output devices populated: {deviceNames.Length} device(s)")
        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to populate output devices: {ex.Message}", ex)
        End Try
    End Sub

    ''' <summary>Handle input source selection change</summary>
    Private Sub OnInputSourceChanged(sender As Object, e As EventArgs)
        Try
            If radioMicrophone.Checked Then
                audioRouter.CurrentInputSource = AudioIO.AudioRouter.InputSourceType.Microphone
                btnBrowseInputFile.Enabled = False
                lblSelectedFile.Text = "No file selected"
                Services.LoggingServiceAdapter.Instance.LogInfo("Input source: Microphone")
            ElseIf radioFilePlayback.Checked Then
                audioRouter.CurrentInputSource = AudioIO.AudioRouter.InputSourceType.FilePlayback
                btnBrowseInputFile.Enabled = True
                Services.LoggingServiceAdapter.Instance.LogInfo("Input source: File Playback")
            End If
        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to change input source: {ex.Message}", ex)
        End Try
    End Sub

    ''' <summary>Handle browse for input file button</summary>
    Private Sub OnBrowseInputFileClick(sender As Object, e As EventArgs)
        Try
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Title = "Select Audio File for DSP Playback"
                openFileDialog.Filter = "WAV Files (*.wav)|*.wav|All Audio Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*"
                openFileDialog.InitialDirectory = Path.Combine(Application.StartupPath, "Recordings")

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    Dim selectedFile = openFileDialog.FileName
                    lblSelectedFile.Text = Path.GetFileName(selectedFile)

                    ' Store in AudioRouter
                    audioRouter.SelectedInputFile = selectedFile

                    ' Start DSP playback immediately
                    Try
                        audioRouter.StartDSPPlayback()

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

    ''' <summary>Handle output device selection change</summary>
    Private Sub OnOutputDeviceChanged(sender As Object, e As EventArgs)
        Try
            If cmbOutputDevice.SelectedIndex >= 0 Then
                audioRouter.SelectOutputDevice(cmbOutputDevice.SelectedIndex)
                Services.LoggingServiceAdapter.Instance.LogInfo($"Output device changed: {cmbOutputDevice.SelectedItem}")
            End If
        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to change output device: {ex.Message}", ex)
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

        ' Cleanup modules
        playbackManager?.Dispose()
        WaveformDisplayControl1?.Dispose()
        fftProcessorInput?.Clear()
        fftProcessorOutput?.Clear()

        ' Close logger
        Logger.Instance.Close()

        MyBase.OnFormClosing(e)
    End Sub

    Private Sub tabProgram_Click(sender As Object, e As EventArgs) Handles tabProgram.Click

    End Sub
End Class
