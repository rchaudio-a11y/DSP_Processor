Imports System.IO
Imports DSP_Processor.AudioIO
Imports DSP_Processor.Recording
Imports DSP_Processor.UI
Imports DSP_Processor.Utils
Imports DSP_Processor.Visualization
Imports DSP_Processor.Managers
Imports NAudio.Wave

Partial Public Class MainForm
    ' MANAGERS (New architecture!)
    Private settingsManager As SettingsManager
    Private fileManager As FileManager
    Private recordingManager As RecordingManager
    
    ' Existing components (not yet refactored)
    Private playbackEngine As PlaybackEngine
    Private waveformRenderer As WaveformRenderer

    ' FFT processing for spectrum display
    Private fftProcessor As DSP.FFT.FFTProcessor

    ' UI Panels
    Private inputTabPanel As UI.TabPanels.InputTabPanel
    Private recordingOptionsPanel As UI.TabPanels.RecordingOptionsPanel

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' APPLY DARK THEME FIRST!
        DarkTheme.ApplyToForm(Me)
        DarkTheme.ApplyDangerButton(btnDelete)

        ' Create managers
        InitializeManagers()

        ' Create playback/rendering components BEFORE wiring events
        playbackEngine = New PlaybackEngine()
        waveformRenderer = New WaveformRenderer() With {
            .BackgroundColor = Color.Black,
            .ForegroundColor = Color.Lime,
            .RightChannelColor = Color.Cyan
        }

        ' Create FFT processor for spectrum analysis
        fftProcessor = New DSP.FFT.FFTProcessor(4096) With {
            .SampleRate = 44100,
            .WindowFunction = DSP.FFT.FFTProcessor.WindowType.Hann
        }

        ' NOW wire up events (all objects exist!)
        WireManagerEvents()
        WirePlaybackEvents()      ' ✅ Safe now - playbackEngine exists
        WireTransportEvents()     ' ✅ Safe - transportControl from designer
        WireUIEvents()

        ' Load all settings (this will trigger OnSettingsLoaded event)
        settingsManager.LoadAll()

        ' Populate UI controls with current settings
        PopulateUIControls()

        ' Initialize UI tabs
        InitializeInputSettingsTab()
        InitializeRecordingOptionsTab()

        ' Apply dark theme to visualization tabs
        DarkTheme.ApplyToControl(visualizationTabs)

        ' Initialize SpectrumDisplayControl1 defaults
        cmbFFTSize.SelectedItem = "4096"
        cmbWindowFunction.SelectedItem = "Hann"
        numSmoothing.Value = 70 ' 0.7 factor
        chkPeakHold.Checked = False
        trackMinFreq.Value = 20
        trackMaxFreq.Value = 12000
        lblMinFreqValue.Text = "20 Hz"
        lblMaxFreqValue.Text = "12000 Hz"

        ' NOTE: Microphone will be armed after settings are loaded (in OnSettingsLoaded)

        ' Update UI state (will be updated again after mic arms)
        lblStatus.Text = "Status: Initializing..."
        btnStopPlayback.Enabled = False

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
    End Sub

    Private Sub PopulateUIControls()
        ' Populate dropdowns
        PopulateInputDevices()
        PopulateSampleRates()
        PopulateBitDepths()
        PopulateChannelModes()
        PopulateBufferSizes()

        ' Set defaults from settings
        SetUIFromSettings(settingsManager.AudioSettings)

        ' Initialize volume controls
        trackVolume.Value = 100
        lblVolume.Text = "100%"
        trackInputVolume.Value = settingsManager.MeterSettings.InputVolumePercent
        lblInputVolume.Text = $"Input Volume: {trackInputVolume.Value}%"
    End Sub

    Private Sub SetUIFromSettings(settings As AudioDeviceSettings)
        ' Set combo box selections from saved settings
        If settings.InputDeviceIndex < cmbInputDevices.Items.Count Then
            cmbInputDevices.SelectedIndex = settings.InputDeviceIndex
        End If

        ' Set sample rate
        Dim rateIndex = Array.IndexOf({8000, 11025, 16000, 22050, 32000, 44100, 48000, 96000}, settings.SampleRate)
        If rateIndex >= 0 Then cmbSampleRates.SelectedIndex = rateIndex

        ' Set bit depth
        Dim depthIndex = Array.IndexOf({8, 16, 24, 32}, settings.BitDepth)
        If depthIndex >= 0 Then cmbBitDepths.SelectedIndex = depthIndex

        ' Set channels
        cmbChannelMode.SelectedIndex = If(settings.Channels = 1, 0, 1)

        ' Set buffer size
        Dim bufferIndex = Array.IndexOf({10, 20, 30, 50, 100, 200}, settings.BufferMilliseconds)
        If bufferIndex >= 0 Then cmbBufferSize.SelectedIndex = bufferIndex
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
        AddHandler playbackEngine.PlaybackStopped, AddressOf OnPlaybackStopped
        AddHandler playbackEngine.PositionChanged, AddressOf OnPositionChanged
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
    End Sub

#End Region

#Region "Manager Event Handlers"

    Private Sub OnSettingsLoaded(sender As Object, e As EventArgs)
        Services.LoggingServiceAdapter.Instance.LogInfo("All settings loaded")

        ' Initialize recording manager with settings
        recordingManager.Initialize(settingsManager.AudioSettings, settingsManager.RecordingOptions)

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
            lstRecordings.Items.Add(fileInfo.Name)
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

        ' Clear FFT buffer for fresh spectrum
        fftProcessor.Clear()

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

        ' Clear spectrum displays (optional - comment out to keep last frame visible)
        ' SpectrumAnalyzerControl1.InputDisplay.Clear()
        ' SpectrumAnalyzerControl1.OutputDisplay.Clear()

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
        ' Update meter
        Try
            Dim levelData = AudioLevelMeter.AnalyzeSamples(e.Buffer, e.BitsPerSample, e.Channels)
            meterRecording.SetLevel(levelData.PeakDB, levelData.RMSDB, levelData.IsClipping)

            ' Update spectrum display with FFT (feed to INPUT display for PRE monitoring)
            fftProcessor.SampleRate = e.SampleRate
            fftProcessor.AddSamples(e.Buffer, e.Buffer.Length, e.BitsPerSample)
            Dim spectrum = fftProcessor.CalculateSpectrum()

            If spectrum IsNot Nothing AndAlso spectrum.Length > 0 Then
                ' Feed live audio to INPUT display (PRE - before processing)
                SpectrumAnalyzerControl1.InputDisplay.UpdateSpectrum(spectrum, e.SampleRate, fftProcessor.FFTSize)
                ' OUTPUT display (POST) would show processed audio if DSP was applied
            End If
        Catch
            ' Ignore metering/FFT errors
        End Try
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

    Private Sub InitializeInputSettingsTab()
        Try
            Services.LoggingServiceAdapter.Instance.LogInfo("Initializing Input Settings tab...")

            ' Create Input tab panel
            inputTabPanel = New UI.TabPanels.InputTabPanel()
            tabInput.Controls.Add(inputTabPanel)
            inputTabPanel.Dock = DockStyle.Fill

            ' Wire up events
            AddHandler inputTabPanel.SettingsChanged, AddressOf OnMeterSettingsChanged

            ' Load settings and apply
            inputTabPanel.LoadSettings(settingsManager.MeterSettings)
            ApplyMeterSettings(settingsManager.MeterSettings)

            Services.LoggingServiceAdapter.Instance.LogInfo("Input Settings tab initialized successfully")

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to initialize Input Settings tab: {ex.Message}", ex)
            Logger.Instance.Error("Failed to initialize Input Settings tab", ex, "MainForm")
        End Try
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

            ' Update UI
            trackInputVolume.Value = settings.InputVolumePercent
            lblInputVolume.Text = $"Input Volume: {settings.InputVolumePercent}%"

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

    Private Sub InitializeRecordingOptionsTab()
        Try
            Services.LoggingServiceAdapter.Instance.LogInfo("Initializing Recording Options tab...")

            ' Create Recording Options panel
            recordingOptionsPanel = New UI.TabPanels.RecordingOptionsPanel()
            tabRecording.Controls.Add(recordingOptionsPanel)
            recordingOptionsPanel.Dock = DockStyle.Fill

            ' Wire up events
            AddHandler recordingOptionsPanel.OptionsChanged, AddressOf OnRecordingOptionsChanged

            ' Load options and apply
            recordingOptionsPanel.LoadOptions(settingsManager.RecordingOptions)
            recordingManager.Options = settingsManager.RecordingOptions

            Services.LoggingServiceAdapter.Instance.LogInfo("Recording Options tab initialized successfully")

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to initialize Recording Options tab: {ex.Message}", ex)
            Logger.Instance.Error("Failed to initialize Recording Options tab", ex, "MainForm")
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

    Private Sub PopulateInputDevices()
        cmbInputDevices.Items.Clear()
        For i = 0 To WaveIn.DeviceCount - 1
            Dim caps = WaveIn.GetCapabilities(i)
            cmbInputDevices.Items.Add($"{i}: {caps.ProductName}")
        Next
        If cmbInputDevices.Items.Count > 0 Then
            cmbInputDevices.SelectedIndex = 0
        End If
    End Sub

    Private Sub PopulateSampleRates()
        cmbSampleRates.Items.Clear()
        Dim rates = New Integer() {8000, 11025, 16000, 22050, 32000, 44100, 48000, 96000}
        Dim Rate As Integer
        For Each Rate In rates
            cmbSampleRates.Items.Add(Rate.ToString())
        Next
        cmbSampleRates.SelectedIndex = 5 ' Default to 44100 Hz
    End Sub

    Private Sub PopulateBitDepths()
        cmbBitDepths.Items.Clear()
        Dim depths = New Integer() {8, 16, 24, 32}
        Dim Depth As Integer
        For Each Depth In depths
            cmbBitDepths.Items.Add(Depth.ToString())
        Next
        cmbBitDepths.SelectedIndex = 1 ' Default to 16-bit
    End Sub

    Private Sub PopulateChannelModes()
        cmbChannelMode.Items.Clear()
        cmbChannelMode.Items.Add("Mono (1)")
        cmbChannelMode.Items.Add("Stereo (2)")
        cmbChannelMode.SelectedIndex = 1 ' Default to Stereo
    End Sub

    Private Sub RefreshRecordingList()
        Dim folder = Path.Combine(Application.StartupPath, "Recordings")
        lstRecordings.Items.Clear()

        If Directory.Exists(folder) Then
            Dim fileCount = 0
            For Each file In Directory.GetFiles(folder, "*.wav")
                lstRecordings.Items.Add(Path.GetFileName(file))
                fileCount += 1
            Next

            Services.LoggingServiceAdapter.Instance.LogInfo($"Recording list refreshed: {fileCount} file(s) found")
        Else
            Services.LoggingServiceAdapter.Instance.LogWarning($"Recordings folder not found: {folder}")
        End If
    End Sub

    Private Sub PopulateBufferSizes()
        cmbBufferSize.Items.Clear()
        ' These are buffer sizes in MILLISECONDS for NAudio
        Dim sizes = New Integer() {10, 20, 30, 50, 100, 200}
        Dim Size As Integer
        For Each Size In sizes
            cmbBufferSize.Items.Add(Size.ToString())
        Next
        cmbBufferSize.SelectedIndex = 1 ' Default to 20ms
    End Sub

    ''' <summary>
    ''' Pre-warms NAudio drivers by briefly initializing and releasing audio device.
    ''' This eliminates the "cold start" delay on first recording.
    ''' </summary>
    Private Sub PreWarmAudioDrivers()
        Try
            Services.LoggingServiceAdapter.Instance.LogInfo("Pre-warming audio drivers...")

            Dim deviceIndex = cmbInputDevices.SelectedIndex
            If deviceIndex < 0 Then
                Services.LoggingServiceAdapter.Instance.LogWarning("No audio device selected for pre-warming")
                Return
            End If

            Dim sampleRate = 44100
            Dim bits = 16
            Dim channels = 2

            Logger.Instance.Debug("Pre-warming audio drivers...", "MainForm")

            Using tempWaveIn As New WaveInEvent() With {
                .DeviceNumber = deviceIndex,
                .WaveFormat = New WaveFormat(sampleRate, bits, channels),
                .BufferMilliseconds = 20
            }
                tempWaveIn.StartRecording()
                System.Threading.Thread.Sleep(50)
                tempWaveIn.StopRecording()
            End Using

            Services.LoggingServiceAdapter.Instance.LogInfo("Audio drivers pre-warmed successfully")
            Logger.Instance.Debug("Audio drivers pre-warmed successfully", "MainForm")

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogWarning($"Failed to pre-warm audio drivers: {ex.Message}")
            Logger.Instance.Warning($"Failed to pre-warm audio drivers: {ex.Message}", "MainForm")
        End Try
    End Sub

    Private Sub lstRecordings_DoubleClick(sender As Object, e As EventArgs) Handles lstRecordings.DoubleClick
        If lstRecordings.SelectedItem Is Nothing Then Return

        Dim fileName = lstRecordings.SelectedItem.ToString()
        Dim fullPath = Path.Combine(Application.StartupPath, "Recordings", fileName)

        Try
            ' Extra safety: ensure file exists and is not locked
            If Not File.Exists(fullPath) Then
                Services.LoggingServiceAdapter.Instance.LogError($"File not found: {fileName}")
                MessageBox.Show($"File not found: {fileName}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                RefreshRecordingList() ' Refresh to remove stale entries
                Return
            End If

            ' Try to open file to check if it's locked (with retry for recently stopped recordings)
            Dim maxRetries As Integer = 3
            Dim retryDelay As Integer = 100 ' ms
            Dim fileAccessible As Boolean = False

            For attempt = 1 To maxRetries
                Try
                    Using fs As New FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                        ' File is accessible
                        fileAccessible = True
                        Exit For
                    End Using
                Catch ex As IOException
                    ' File might still be closing from recording
                    If attempt < maxRetries Then
                        Services.LoggingServiceAdapter.Instance.LogDebug($"File temporarily locked, retrying... (attempt {attempt}/{maxRetries})")
                        System.Threading.Thread.Sleep(retryDelay)
                    End If
                End Try
            Next

            If Not fileAccessible Then
                Services.LoggingServiceAdapter.Instance.LogWarning($"File is locked or in use: {fileName}")
                MessageBox.Show($"File is currently in use. Please wait a moment and try again.", "File Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Services.LoggingServiceAdapter.Instance.LogInfo($"Loading file for playback: {fileName}")

            playbackEngine.Load(fullPath)

            ' Apply current volume from slider BEFORE starting playback
            Dim volumePercent = trackVolume.Value
            playbackEngine.Volume = volumePercent / 100.0F

            playbackEngine.Play()

            panelLED.BackColor = Color.RoyalBlue
            lblStatus.Text = $"Status: Playing {fileName}"
            progressPlayback.Style = ProgressBarStyle.Continuous
            TimerPlayback.Start()

            ' Enable stop button
            btnStopPlayback.Enabled = True

            Services.LoggingServiceAdapter.Instance.LogInfo($"Playback started: {fileName} (Volume: {volumePercent}%)")

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to play file '{fileName}': {ex.Message}", ex)
            MessageBox.Show($"Failed to play file: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnStopPlayback_Click(sender As Object, e As EventArgs) Handles btnStopPlayback.Click
        ' Stop playback
        If playbackEngine IsNot Nothing Then
            playbackEngine.Stop()
        End If

        ' Update UI
        panelLED.BackColor = DarkTheme.SuccessGreen
        lblStatus.Text = "Status: Ready (Mic Armed)"
        TimerPlayback.Stop()
        progressPlayback.Value = 0
        btnStopPlayback.Enabled = False
    End Sub

    Private Sub trackVolume_Scroll(sender As Object, e As EventArgs) Handles trackVolume.Scroll
        ' Update playback volume
        If playbackEngine IsNot Nothing Then
            Dim volumePercent = trackVolume.Value
            playbackEngine.Volume = volumePercent / 100.0F
            lblVolume.Text = $"{volumePercent}%"

            Services.LoggingServiceAdapter.Instance.LogInfo($"Playback volume changed: {volumePercent}%")
        End If
    End Sub

    Private Sub trackInputVolume_Scroll(sender As Object, e As EventArgs) Handles trackInputVolume.Scroll
        Dim volumePercent = trackInputVolume.Value

        ' Update RecordingManager
        recordingManager.InputVolume = volumePercent / 100.0F

        ' Update UI
        lblInputVolume.Text = $"Input Volume: {volumePercent}%"

        ' Color warning
        If volumePercent > 150 Then
            lblInputVolume.ForeColor = Color.Orange
        ElseIf volumePercent > 100 Then
            lblInputVolume.ForeColor = Color.Yellow
        Else
            lblInputVolume.ForeColor = DarkTheme.TextColor
        End If

        ' Update and save settings
        settingsManager.MeterSettings.InputVolumePercent = volumePercent
        settingsManager.SaveAll()

        Services.LoggingServiceAdapter.Instance.LogInfo($"Input volume changed: {volumePercent}%")
    End Sub

#Region "File Operations"

    Private Sub lstRecordings_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstRecordings.SelectedIndexChanged
        If lstRecordings.SelectedItem Is Nothing Then Return

        Dim fileName = lstRecordings.SelectedItem.ToString()
        Dim fullPath = Path.Combine(fileManager.RecordingsFolder, fileName)

        ' Validate file through FileManager
        If Not fileManager.ValidateFile(fullPath) Then
            Services.LoggingServiceAdapter.Instance.LogWarning($"File validation failed: {fileName}")
            Return
        End If

        Try
            Services.LoggingServiceAdapter.Instance.LogInfo($"Rendering waveform for: {fileName}")

            ' Dispose of old image
            If picWaveform.Image IsNot Nothing Then
                Dim oldImage = picWaveform.Image
                picWaveform.Image = Nothing
                oldImage.Dispose()
            End If

            ' Render waveform
            Using timer = Logger.Instance.StartTimer("Waveform Rendering")
                Dim waveform = waveformRenderer.Render(fullPath, picWaveform.Width, picWaveform.Height)

                If waveform IsNot Nothing AndAlso waveform.Width > 0 AndAlso waveform.Height > 0 Then
                    picWaveform.Image = waveform
                Else
                    waveform?.Dispose()
                End If
            End Using

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

        Dim fileName = lstRecordings.SelectedItem.ToString()
        Dim fullPath = Path.Combine(fileManager.RecordingsFolder, fileName)

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
            If picWaveform.Image IsNot Nothing Then
                Dim oldImage = picWaveform.Image
                picWaveform.Image = Nothing
                oldImage.Dispose()
            End If
            waveformRenderer.ClearCache()

            ' Stop playback if playing this file
            If playbackEngine IsNot Nothing AndAlso playbackEngine.IsPlaying Then
                playbackEngine.Stop()
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
            waveformRenderer.ClearCache()
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
        If playbackEngine IsNot Nothing AndAlso playbackEngine.IsPlaying Then
            Services.LoggingServiceAdapter.Instance.LogInfo("Stopping playback...")
            playbackEngine.Stop()
            TimerPlayback.Stop()
            progressPlayback.Value = 0
            btnStopPlayback.Enabled = False
            Services.LoggingServiceAdapter.Instance.LogInfo("Playback stopped")
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
        If playbackEngine IsNot Nothing AndAlso playbackEngine.IsPlaying Then
            Logger.Instance.Debug($"Seek requested to: {position}", "MainForm")
        End If
    End Sub

#End Region

    Private Sub OnPlaybackStopped(sender As Object, e As NAudio.Wave.StoppedEventArgs)
        panelLED.BackColor = DarkTheme.SuccessGreen
        lblStatus.Text = "Status: Ready (Mic Armed)"

        TimerPlayback.Stop()
        progressPlayback.Value = 0
        btnStopPlayback.Enabled = False
    End Sub

    Private Sub TimerPlayback_Tick(sender As Object, e As EventArgs) Handles TimerPlayback.Tick
        playbackEngine.UpdatePosition()
        transportControl.TrackPosition = playbackEngine.CurrentPosition
        transportControl.TrackDuration = playbackEngine.TotalDuration
        UpdateTransportState()
    End Sub

    Private Sub OnPositionChanged(sender As Object, position As TimeSpan)
        Dim total = playbackEngine.TotalDuration
        If total.TotalMilliseconds > 0 Then
            Dim pct = CInt((position.TotalMilliseconds / total.TotalMilliseconds) * 1000)
            progressPlayback.Value = Math.Min(1000, Math.Max(0, pct))
        End If
    End Sub

    Private Sub UpdateTransportState()
        If recordingManager IsNot Nothing AndAlso recordingManager.IsRecording Then
            transportControl.State = UI.TransportControl.TransportState.Recording
            transportControl.RecordingTime = recordingManager.RecordingDuration
        ElseIf playbackEngine IsNot Nothing AndAlso playbackEngine.IsPlaying Then
            transportControl.State = UI.TransportControl.TransportState.Playing
            transportControl.TrackPosition = playbackEngine.CurrentPosition
            transportControl.TrackDuration = playbackEngine.TotalDuration
        Else
            transportControl.State = UI.TransportControl.TransportState.Stopped
        End If
    End Sub

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

#Region "Settings Change Handlers"

    Private Sub cmbInputDevices_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbInputDevices.SelectedIndexChanged
        If cmbInputDevices.SelectedIndex >= 0 Then
            settingsManager.AudioSettings.InputDeviceIndex = cmbInputDevices.SelectedIndex
            settingsManager.SaveAll()
            Services.LoggingServiceAdapter.Instance.LogInfo($"Input device changed: {cmbInputDevices.SelectedItem}")
        End If
    End Sub

    Private Sub cmbSampleRates_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSampleRates.SelectedIndexChanged
        If cmbSampleRates.SelectedItem IsNot Nothing Then
            settingsManager.AudioSettings.SampleRate = Integer.Parse(cmbSampleRates.SelectedItem.ToString())
            settingsManager.SaveAll()
            Services.LoggingServiceAdapter.Instance.LogInfo($"Sample rate changed: {cmbSampleRates.SelectedItem} Hz")
        End If
    End Sub

    Private Sub cmbBitDepths_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbBitDepths.SelectedIndexChanged
        If cmbBitDepths.SelectedItem IsNot Nothing Then
            settingsManager.AudioSettings.BitDepth = CInt(cmbBitDepths.SelectedItem)
            settingsManager.SaveAll()
            Services.LoggingServiceAdapter.Instance.LogInfo($"Bit depth changed: {cmbBitDepths.SelectedItem}-bit")
        End If
    End Sub

    Private Sub cmbChannelMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbChannelMode.SelectedIndexChanged
        If cmbChannelMode.SelectedItem IsNot Nothing Then
            settingsManager.AudioSettings.Channels = If(cmbChannelMode.SelectedIndex = 0, 1, 2)
            settingsManager.SaveAll()
            Services.LoggingServiceAdapter.Instance.LogInfo($"Channel mode changed: {cmbChannelMode.SelectedItem}")
        End If
    End Sub

    Private Sub cmbBufferSize_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbBufferSize.SelectedIndexChanged
        If cmbBufferSize.SelectedItem IsNot Nothing Then
            settingsManager.AudioSettings.BufferMilliseconds = CInt(cmbBufferSize.SelectedItem)
            settingsManager.SaveAll()
            Services.LoggingServiceAdapter.Instance.LogInfo($"Buffer size changed: {cmbBufferSize.SelectedItem} ms")
        End If
    End Sub

#End Region

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        ' Unsubscribe from events
        RemoveHandler Services.LoggingServiceAdapter.Instance.LogMessageReceived, AddressOf OnLogMessage

        ' Log shutdown
        Services.LoggingServiceAdapter.Instance.LogInfo("DSP Processor shutting down")

        ' Dispose managers
        recordingManager?.Dispose()
        ' Note: SettingsManager and FileManager don't need disposal

        ' Cleanup existing modules
        playbackEngine?.Dispose()
        waveformRenderer?.ClearCache()
        fftProcessor?.Clear()
        ' NOTE: SpectrumDisplayControl1 from designer handles its own disposal

        ' Close logger
        Logger.Instance.Close()

        MyBase.OnFormClosing(e)
    End Sub

#Region "Spectrum Analyzer Event Handlers - Wire to SpectrumDisplayControl1 properties"

    Private Sub cmbFFTSize_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFFTSize.SelectedIndexChanged
        If cmbFFTSize.SelectedItem IsNot Nothing Then
            Dim fftSize = Integer.Parse(cmbFFTSize.SelectedItem.ToString())
            fftProcessor.FFTSize = fftSize
            Services.LoggingServiceAdapter.Instance.LogInfo($"FFT size changed to: {fftSize}")
        End If
    End Sub

    Private Sub cmbWindowFunction_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbWindowFunction.SelectedIndexChanged
        If cmbWindowFunction.SelectedItem IsNot Nothing Then
            Select Case cmbWindowFunction.SelectedItem.ToString()
                Case "None"
                    fftProcessor.WindowFunction = DSP.FFT.FFTProcessor.WindowType.None
                Case "Hann"
                    fftProcessor.WindowFunction = DSP.FFT.FFTProcessor.WindowType.Hann
                Case "Hamming"
                    fftProcessor.WindowFunction = DSP.FFT.FFTProcessor.WindowType.Hamming
                Case "Blackman"
                    fftProcessor.WindowFunction = DSP.FFT.FFTProcessor.WindowType.Blackman
            End Select
            Services.LoggingServiceAdapter.Instance.LogInfo($"Window function changed to: {cmbWindowFunction.SelectedItem}")
        End If
    End Sub

    Private Sub numSmoothing_ValueChanged(sender As Object, e As EventArgs) Handles numSmoothing.ValueChanged
        ' Convert percentage (0-100) to factor (0.0-1.0) and apply to both displays
        Dim smoothingFactor = CSng(numSmoothing.Value / 100)
        SpectrumAnalyzerControl1.InputDisplay.SmoothingFactor = smoothingFactor
        SpectrumAnalyzerControl1.OutputDisplay.SmoothingFactor = smoothingFactor
        Services.LoggingServiceAdapter.Instance.LogInfo($"Smoothing changed to: {numSmoothing.Value}%")
    End Sub

    Private Sub chkPeakHold_CheckedChanged(sender As Object, e As EventArgs) Handles chkPeakHold.CheckedChanged
        ' Apply to both displays
        SpectrumAnalyzerControl1.InputDisplay.PeakHoldEnabled = chkPeakHold.Checked
        SpectrumAnalyzerControl1.OutputDisplay.PeakHoldEnabled = chkPeakHold.Checked
        Services.LoggingServiceAdapter.Instance.LogInfo($"Peak hold: {If(chkPeakHold.Checked, "enabled", "disabled")}")
    End Sub

    Private Sub btnResetSpectrum_Click(sender As Object, e As EventArgs) Handles btnResetSpectrum.Click
        ' Clear both PRE and POST displays
        SpectrumAnalyzerControl1.InputDisplay.Clear()
        SpectrumAnalyzerControl1.OutputDisplay.Clear()
        fftProcessor.Clear()
        Services.LoggingServiceAdapter.Instance.LogInfo("Spectrum analyzer reset")
    End Sub

    Private Sub trackMinFreq_Scroll(sender As Object, e As EventArgs) Handles trackMinFreq.Scroll
        ' Update both displays
        SpectrumAnalyzerControl1.InputDisplay.MinFrequency = trackMinFreq.Value
        SpectrumAnalyzerControl1.OutputDisplay.MinFrequency = trackMinFreq.Value
        lblMinFreqValue.Text = $"{trackMinFreq.Value} Hz"
        Services.LoggingServiceAdapter.Instance.LogInfo($"Min frequency: {trackMinFreq.Value} Hz")
    End Sub

    Private Sub trackMaxFreq_Scroll(sender As Object, e As EventArgs) Handles trackMaxFreq.Scroll
        ' Update both displays
        SpectrumAnalyzerControl1.InputDisplay.MaxFrequency = trackMaxFreq.Value
        SpectrumAnalyzerControl1.OutputDisplay.MaxFrequency = trackMaxFreq.Value
        lblMaxFreqValue.Text = $"{trackMaxFreq.Value} Hz"
        Services.LoggingServiceAdapter.Instance.LogInfo($"Max frequency: {trackMaxFreq.Value} Hz")
    End Sub

#End Region

End Class
