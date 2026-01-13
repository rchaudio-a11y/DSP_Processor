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
    Private playbackManager As PlaybackManager

    ' FFT processing for spectrum display
    Private fftProcessor As DSP.FFT.FFTProcessor

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' APPLY DARK THEME FIRST!
        DarkTheme.ApplyToForm(Me)
        DarkTheme.ApplyDangerButton(btnDelete)

        ' Create managers
        InitializeManagers()

        ' Create playback manager BEFORE wiring events
        playbackManager = New PlaybackManager()
        playbackManager.Initialize()

        ' Create FFT processor for spectrum analysis
        fftProcessor = New DSP.FFT.FFTProcessor(4096) With {
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

        ' Initialize volume controls
        trackVolume.Value = 100
        lblVolume.Text = "100%"

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

#Region "UserControl Event Handlers"

    ''' <summary>Handles audio settings changes from AudioSettingsPanel</summary>
    Private Sub OnAudioSettingsChanged(sender As Object, settings As Managers.AudioDeviceSettings)
        Services.LoggingServiceAdapter.Instance.LogInfo("Audio settings changed via AudioSettingsPanel")

        ' Update settings manager
        settingsManager.AudioSettings = settings

        ' Apply to recording manager (will re-arm mic with new settings)
        If recordingManager IsNot Nothing Then
            recordingManager.Initialize(settings, settingsManager.RecordingOptions)
            Try
                recordingManager.ArmMicrophone()
            Catch ex As Exception
                Services.LoggingServiceAdapter.Instance.LogError($"Failed to re-arm microphone with new settings: {ex.Message}", ex)
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

        Dim fileName = lstRecordings.SelectedItem.ToString()
        Dim fullPath = Path.Combine(Application.StartupPath, "Recordings", fileName)

        Try
            Services.LoggingServiceAdapter.Instance.LogInfo($"Loading file for playback: {fileName}")

            ' Use PlaybackManager to handle everything
            playbackManager.Volume = trackVolume.Value / 100.0F
            playbackManager.LoadAndPlay(fullPath)

            panelLED.BackColor = Color.RoyalBlue
            lblStatus.Text = $"Status: Playing {fileName}"
            progressPlayback.Style = ProgressBarStyle.Continuous
            TimerPlayback.Start()

            ' Enable stop button
            btnStopPlayback.Enabled = True

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to play file '{fileName}': {ex.Message}", ex)
            MessageBox.Show($"Failed to play file: {ex.Message}", "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

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
        ' Stop playback via PlaybackManager
        playbackManager?.Stop()

        ' Update UI
        panelLED.BackColor = DarkTheme.SuccessGreen
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
        ' Update UI
        panelLED.BackColor = Color.RoyalBlue
        lblStatus.Text = $"Status: Playing {Path.GetFileName(filepath)}"
    End Sub

    Private Sub OnPlaybackStopped(sender As Object, e As NAudio.Wave.StoppedEventArgs)
        panelLED.BackColor = DarkTheme.SuccessGreen
        lblStatus.Text = "Status: Ready (Mic Armed)"

        TimerPlayback.Stop()
        progressPlayback.Value = 0
        btnStopPlayback.Enabled = False
    End Sub

    Private Sub TimerPlayback_Tick(sender As Object, e As EventArgs) Handles TimerPlayback.Tick
        playbackManager.UpdatePosition()
        transportControl.TrackPosition = playbackManager.CurrentPosition
        transportControl.TrackDuration = playbackManager.TotalDuration
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

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        ' Unsubscribe from events
        RemoveHandler Services.LoggingServiceAdapter.Instance.LogMessageReceived, AddressOf OnLogMessage

        ' Log shutdown
        Services.LoggingServiceAdapter.Instance.LogInfo("DSP Processor shutting down")

        ' Dispose managers
        recordingManager?.Dispose()

        ' Cleanup modules
        playbackManager?.Dispose()
        WaveformDisplayControl1?.Dispose()
        fftProcessor?.Clear()

        ' Close logger
        Logger.Instance.Close()

        MyBase.OnFormClosing(e)
    End Sub

End Class
