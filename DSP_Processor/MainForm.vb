Imports System.IO
Imports DSP_Processor.AudioIO
Imports DSP_Processor.Recording
Imports DSP_Processor.UI
Imports DSP_Processor.Utils
Imports DSP_Processor.Visualization
Imports NAudio.Wave

Partial Public Class MainForm
    Private mic As MicInputSource
    Private recorder As RecordingEngine
    Private playbackEngine As PlaybackEngine
    Private waveformRenderer As WaveformRenderer
    Private micIsArmed As Boolean = False ' Track if mic is ready

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' APPLY DARK THEME FIRST!
        DarkTheme.ApplyToForm(Me)

        ' Style specific buttons
        DarkTheme.ApplyDangerButton(btnDelete)

        ' Create recording engine
        recorder = New RecordingEngine() With {
            .OutputFolder = "Recordings",
            .AutoNamePattern = "Take_{0:yyyyMMdd}-{1:000}.wav",
            .TimedRecordingEnabled = False,
            .AutoRestartEnabled = False,
            .MaxRecordings = 1
        }

        ' Ensure recordings folder exists
        Dim folder = Path.Combine(Application.StartupPath, "Recordings")
        If Not Directory.Exists(folder) Then Directory.CreateDirectory(folder)

        RefreshRecordingList()
        lblStatus.Text = "Status: Idle"

        ' Populate UI controls
        PopulateInputDevices()
        PopulateSampleRates()
        PopulateBitDepths()
        PopulateChannelModes()
        PopulateBufferSizes()

        ' Initialize new modules
        playbackEngine = New PlaybackEngine()
        waveformRenderer = New WaveformRenderer() With {
            .BackgroundColor = Color.Black,
            .ForegroundColor = Color.Lime,
            .RightChannelColor = Color.Cyan
        }

        ' Wire up events
        AddHandler playbackEngine.PlaybackStopped, AddressOf OnPlaybackStopped
        AddHandler playbackEngine.PositionChanged, AddressOf OnPositionChanged

        ' Wire up TransportControl events
        AddHandler transportControl.RecordClicked, AddressOf OnTransportRecord
        AddHandler transportControl.StopClicked, AddressOf OnTransportStop
        AddHandler transportControl.PlayClicked, AddressOf OnTransportPlay
        AddHandler transportControl.PauseClicked, AddressOf OnTransportPause
        AddHandler transportControl.PositionChanged, AddressOf OnTransportPositionChanged

        ' Start meter timer
        TimerMeters.Start()

        ' Initialize playback controls
        btnStopPlayback.Enabled = False
        trackVolume.Value = 100
        lblVolume.Text = "100%"

        ' Initialize input volume
        trackInputVolume.Value = 100
        lblInputVolume.Text = "Input Volume: 100%"

        ' Pre-warm audio drivers
        PreWarmAudioDrivers()

        ' ARM THE MIC - Start it now so it's ready when user clicks Record
        ArmMicrophone()

        ' Subscribe to logging events
        AddHandler Services.LoggingServiceAdapter.Instance.LogMessageReceived, AddressOf OnLogMessage

        ' Log application startup
        Services.LoggingServiceAdapter.Instance.LogInfo("DSP Processor started successfully")
        Services.LoggingServiceAdapter.Instance.LogInfo($"Audio devices: {WaveIn.DeviceCount} input device(s) found")

        Logger.Instance.Info("DSP Processor started", "MainForm")
    End Sub

    ''' <summary>
    ''' Pre-warms NAudio drivers by briefly initializing and releasing audio device.
    ''' This eliminates the "cold start" delay on first recording.
    ''' </summary>
    Private Sub PreWarmAudioDrivers()
        Try
            Services.LoggingServiceAdapter.Instance.LogInfo("Pre-warming audio drivers...")

            ' Get current settings
            Dim deviceIndex = cmbInputDevices.SelectedIndex
            If deviceIndex < 0 Then
                Services.LoggingServiceAdapter.Instance.LogWarning("No audio device selected for pre-warming")
                Return ' No device selected
            End If

            Dim sampleRate = 44100 ' Use default
            Dim bits = 16
            Dim channels = 2

            Logger.Instance.Debug("Pre-warming audio drivers...", "MainForm")

            ' Create a temporary WaveInEvent to initialize drivers
            Using tempWaveIn As New WaveInEvent() With {
                .DeviceNumber = deviceIndex,
                .WaveFormat = New WaveFormat(sampleRate, bits, channels),
                .BufferMilliseconds = 20
            }
                ' Start and immediately stop to initialize drivers
                tempWaveIn.StartRecording()
                System.Threading.Thread.Sleep(50) ' Let driver initialize
                tempWaveIn.StopRecording()
            End Using

            Services.LoggingServiceAdapter.Instance.LogInfo("Audio drivers pre-warmed successfully")
            Logger.Instance.Debug("Audio drivers pre-warmed successfully", "MainForm")

        Catch ex As Exception
            ' Don't fail if pre-warming fails - just log it
            Services.LoggingServiceAdapter.Instance.LogWarning($"Failed to pre-warm audio drivers: {ex.Message}")
            Logger.Instance.Warning($"Failed to pre-warm audio drivers: {ex.Message}", "MainForm")
        End Try
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

    Private Sub lstRecordings_DoubleClick(sender As Object, e As EventArgs) Handles lstRecordings.DoubleClick
        If lstRecordings.SelectedItem Is Nothing Then Return

        Dim fileName = lstRecordings.SelectedItem.ToString()
        Dim fullPath = Path.Combine(Application.StartupPath, "Recordings", fileName)

        Try
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
        ' Update input volume (0-200% range)
        If mic IsNot Nothing Then
            Dim volumePercent = trackInputVolume.Value
            mic.Volume = volumePercent / 100.0F
            lblInputVolume.Text = $"Input Volume: {volumePercent}%"

            ' Warn if boosting too much
            If volumePercent > 150 Then
                lblInputVolume.ForeColor = Color.Orange
            ElseIf volumePercent > 100 Then
                lblInputVolume.ForeColor = Color.Yellow
            Else
                lblInputVolume.ForeColor = DarkTheme.TextColor
            End If

            Services.LoggingServiceAdapter.Instance.LogInfo($"Input volume changed: {volumePercent}%")
        End If
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        ' Check if a recording is selected
        If lstRecordings.SelectedItem Is Nothing Then
            Services.LoggingServiceAdapter.Instance.LogWarning("Delete attempted with no file selected")
            MessageBox.Show("Please select a recording to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim fileName = lstRecordings.SelectedItem.ToString()
        Dim fullPath = Path.Combine(Application.StartupPath, "Recordings", fileName)

        Services.LoggingServiceAdapter.Instance.LogInfo($"Delete requested for: {fileName}")

        ' Confirm deletion
        Dim result = MessageBox.Show(
            $"Are you sure you want to delete '{fileName}'?{Environment.NewLine}{Environment.NewLine}This cannot be undone.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2)

        If result = DialogResult.Yes Then
            Try
                ' Get the selected filename before clearing selection
                Dim selectedFile = fileName

                ' Clear selection FIRST to release any references
                lstRecordings.ClearSelected()

                ' Clear waveform image if this file is displayed
                If Not String.IsNullOrEmpty(selectedFile) Then
                    ' Dispose of the current image
                    If picWaveform.Image IsNot Nothing Then
                        Services.LoggingServiceAdapter.Instance.LogDebug($"Disposing waveform image for deletion: {selectedFile}")
                        Dim oldImage = picWaveform.Image
                        picWaveform.Image = Nothing
                        oldImage.Dispose()
                    End If
                    
                    ' Clear renderer cache
                    Services.LoggingServiceAdapter.Instance.LogDebug("Clearing waveform renderer cache")
                    waveformRenderer.ClearCache()
                End If

                ' Stop playback if this file is playing
                If playbackEngine IsNot Nothing AndAlso playbackEngine.IsPlaying Then
                    Services.LoggingServiceAdapter.Instance.LogInfo("Stopping playback before deletion")
                    playbackEngine.Stop()

                    ' Give playback engine time to close the file
                    System.Threading.Thread.Sleep(100)
                End If

                ' Force release of any file handles - TWICE for good measure
                GC.Collect()
                GC.WaitForPendingFinalizers()
                GC.Collect()

                ' Small delay to ensure all handles are released
                System.Threading.Thread.Sleep(50)

                ' Delete the file
                If File.Exists(fullPath) Then
                    File.Delete(fullPath)
                    Services.LoggingServiceAdapter.Instance.LogInfo($"File deleted successfully: {fileName}")
                    Logger.Instance.Info($"Deleted recording: {fileName}", "MainForm")

                    ' Refresh the list
                    RefreshRecordingList()

                    MessageBox.Show($"'{fileName}' has been deleted.", "Delete Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    Services.LoggingServiceAdapter.Instance.LogError($"File not found during deletion: {fileName}")
                    MessageBox.Show($"File not found: {fileName}", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If

            Catch ex As Exception
                Services.LoggingServiceAdapter.Instance.LogError($"Failed to delete file '{fileName}': {ex.Message}", ex)
                MessageBox.Show($"Failed to delete file: {ex.Message}", "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Logger.Instance.Error($"Failed to delete recording: {fileName}", ex, "MainForm")
            End Try
        Else
            Services.LoggingServiceAdapter.Instance.LogInfo($"Delete cancelled by user: {fileName}")
        End If
    End Sub

    Private Sub OnPlaybackStopped(sender As Object, e As NAudio.Wave.StoppedEventArgs)
        panelLED.BackColor = DarkTheme.SuccessGreen
        lblStatus.Text = "Status: Ready (Mic Armed)"

        TimerPlayback.Stop()
        progressPlayback.Value = 0
        progressPlayback.Style = ProgressBarStyle.Continuous
        btnStopPlayback.Enabled = False
    End Sub

    Private Sub TimerPlayback_Tick(sender As Object, e As EventArgs) Handles TimerPlayback.Tick
        playbackEngine.UpdatePosition() ' Fires PositionChanged event

        ' Update transport control
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

    Private Sub lstRecordings_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstRecordings.SelectedIndexChanged
        If lstRecordings.SelectedItem Is Nothing Then Return

        Dim fileName = lstRecordings.SelectedItem.ToString()
        Dim fullPath = Path.Combine(Application.StartupPath, "Recordings", fileName)

        Try
            Services.LoggingServiceAdapter.Instance.LogInfo($"Rendering waveform for: {fileName}")
            
            ' Dispose of old image before rendering new one
            If picWaveform.Image IsNot Nothing Then
                Services.LoggingServiceAdapter.Instance.LogDebug("Disposing old waveform image before rendering new one")
                Dim oldImage = picWaveform.Image
                picWaveform.Image = Nothing
                oldImage.Dispose()
            End If
            
            Using timer = Logger.Instance.StartTimer("Waveform Rendering")
                Dim waveform = waveformRenderer.Render(fullPath, picWaveform.Width, picWaveform.Height)
                picWaveform.Image = waveform
            End Using
            
            Services.LoggingServiceAdapter.Instance.LogInfo($"Waveform rendered successfully: {fileName}")
        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to render waveform for '{fileName}': {ex.Message}", ex)
            Logger.Instance.Error("Failed to render waveform", ex, "MainForm")
        End Try
    End Sub

    ''' <summary>
    ''' Arms the microphone for recording - starts capture but doesn't write to file.
    ''' This eliminates the cold-start delay and lets meters work immediately.
    ''' </summary>
    Private Sub ArmMicrophone()
        Try
            Services.LoggingServiceAdapter.Instance.LogInfo("Arming microphone...")

            ' Get default settings
            Dim deviceIndex = If(cmbInputDevices.SelectedIndex >= 0, cmbInputDevices.SelectedIndex, 0)
            Dim sampleRate = 44100
            Dim bits = 16
            Dim channelMode = "Stereo (2)"
            Dim bufferMs = 20

            ' Try to use actual selected settings if valid
            Try
                If cmbSampleRates.SelectedItem IsNot Nothing Then
                    sampleRate = Integer.Parse(cmbSampleRates.SelectedItem.ToString())
                End If
                If cmbBitDepths.SelectedItem IsNot Nothing Then
                    bits = CInt(cmbBitDepths.SelectedItem)
                End If
                If cmbChannelMode.SelectedItem IsNot Nothing Then
                    channelMode = cmbChannelMode.SelectedItem.ToString()
                End If
                If cmbBufferSize.SelectedItem IsNot Nothing Then
                    bufferMs = CInt(cmbBufferSize.SelectedItem)
                End If
            Catch
                ' Use defaults if settings aren't valid
            End Try

            Services.LoggingServiceAdapter.Instance.LogInfo($"Mic settings: {channelMode}, {sampleRate}Hz, {bits}-bit, {bufferMs}ms buffer")
            Logger.Instance.Debug($"Arming microphone: {channelMode}, {sampleRate}Hz, {bits}-bit", "MainForm")

            ' Create and start mic (audio capture begins, but we're not recording yet)
            mic = New MicInputSource(sampleRate, channelMode, bits, deviceIndex, bufferMs)

            ' Apply current input volume setting
            mic.Volume = trackInputVolume.Value / 100.0F

            ' Start timer to consume buffers (prevents queue buildup)
            TimerAudio.Start()

            micIsArmed = True
            lblStatus.Text = "Status: Ready (Mic Armed)"
            panelLED.BackColor = Color.Yellow ' Yellow = Armed but not recording
            transportControl.IsRecordArmed = True

            Services.LoggingServiceAdapter.Instance.LogInfo("Microphone armed successfully")
            Logger.Instance.Info("Microphone armed and ready", "MainForm")

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to arm microphone: {ex.Message}", ex)
            Logger.Instance.Error("Failed to arm microphone", ex, "MainForm")
            MessageBox.Show($"Failed to arm microphone: {ex.Message}", "Microphone Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

#Region "TransportControl Event Handlers"

    Private Sub OnTransportRecord(sender As Object, e As EventArgs)
        ' Start recording through TransportControl
        Try
            Services.LoggingServiceAdapter.Instance.LogInfo("Starting recording...")

            ' Mic is already armed and running, just need to start writing to file!

            ' If mic isn't armed for some reason, arm it now
            If Not micIsArmed OrElse mic Is Nothing Then
                Services.LoggingServiceAdapter.Instance.LogWarning("Microphone not armed, arming now...")
                Dim deviceIndex = cmbInputDevices.SelectedIndex
                Dim sampleRate = Integer.Parse(cmbSampleRates.SelectedItem.ToString())
                Dim bits = CInt(cmbBitDepths.SelectedItem)
                Dim channelMode As String = cmbChannelMode.SelectedItem.ToString()
                Dim bufferMs = CInt(cmbBufferSize.SelectedItem)

                mic = New MicInputSource(sampleRate, channelMode, bits, deviceIndex, bufferMs)
                mic.Volume = trackInputVolume.Value / 100.0F
                TimerAudio.Start()
                System.Threading.Thread.Sleep(1000) ' Give time to warm up
            End If

            ' Ensure timer is running
            If Not TimerAudio.Enabled Then
                TimerAudio.Start()
                Services.LoggingServiceAdapter.Instance.LogInfo("Audio timer started")
            End If

            ' CRITICAL: Clear stale buffers before starting new recording
            If mic IsNot Nothing Then
                mic.ClearBuffers()
                Services.LoggingServiceAdapter.Instance.LogInfo("Audio buffers cleared")
            End If

            ' Clear any selected item to release file handles
            lstRecordings.ClearSelected()
            waveformRenderer.ClearCache()

            ' Force release of any file handles
            GC.Collect()
            GC.WaitForPendingFinalizers()

            ' Start recording - mic is already capturing, now we write to file!
            recorder.InputSource = mic
            recorder.StartRecording()

            ' Verify recording actually started
            If Not recorder.IsRecording Then
                Throw New InvalidOperationException("Recording failed to start - recorder.IsRecording is False")
            End If

            ' Update transport control
            transportControl.State = UI.TransportControl.TransportState.Recording

            Services.LoggingServiceAdapter.Instance.LogInfo($"Recording started successfully (IsRecording={recorder.IsRecording})")
            Logger.Instance.Info($"Recording started (mic was already armed), IsRecording={recorder.IsRecording}", "MainForm")

        Catch ex As Exception
            Services.LoggingServiceAdapter.Instance.LogError($"Failed to start recording: {ex.Message}", ex)
            MessageBox.Show($"Failed to start recording: {ex.Message}", "Recording Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Logger.Instance.Error("Failed to start recording", ex, "MainForm")
        End Try
    End Sub

    Private Sub OnTransportStop(sender As Object, e As EventArgs)
        ' Check if playing or recording
        If playbackEngine IsNot Nothing AndAlso playbackEngine.IsPlaying Then
            Services.LoggingServiceAdapter.Instance.LogInfo("Stopping playback...")
            ' Stop playback
            If playbackEngine IsNot Nothing Then
                playbackEngine.Stop()
            End If

            ' Update UI
            TimerPlayback.Stop()
            progressPlayback.Value = 0
            btnStopPlayback.Enabled = False
            Services.LoggingServiceAdapter.Instance.LogInfo("Playback stopped")
        ElseIf recorder.IsRecording Then
            Services.LoggingServiceAdapter.Instance.LogInfo("Stopping recording...")
            ' Stop recording
            recorder.StopRecording()
            RefreshRecordingList()

            ' Reset recording meter
            meterRecording.Reset()

            ' Update transport control
            transportControl.State = UI.TransportControl.TransportState.Stopped
            transportControl.RecordingTime = TimeSpan.Zero

            Services.LoggingServiceAdapter.Instance.LogInfo("Recording stopped (mic still armed)")
            Logger.Instance.Info("Recording stopped (mic still armed)", "MainForm")
        End If
    End Sub

    Private Sub OnTransportPlay(sender As Object, e As EventArgs)
        ' Play selected recording
        Services.LoggingServiceAdapter.Instance.LogInfo("Play button clicked")

        If lstRecordings.SelectedItem IsNot Nothing Then
            lstRecordings_DoubleClick(sender, e)
        Else
            Services.LoggingServiceAdapter.Instance.LogWarning("Play attempted with no file selected")
        End If
    End Sub

    Private Sub OnTransportPause(sender As Object, e As EventArgs)
        Services.LoggingServiceAdapter.Instance.LogInfo("Pause button clicked (not yet implemented)")
        ' TODO: Implement pause functionality in Phase 1.5
        MessageBox.Show("Pause functionality coming soon!", "Not Implemented", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub OnTransportPositionChanged(sender As Object, position As TimeSpan)
        ' Seek to position during playback
        If playbackEngine IsNot Nothing AndAlso playbackEngine.IsPlaying Then
            ' TODO: Add Seek method to PlaybackEngine
            Logger.Instance.Debug($"Seek requested to: {position}", "MainForm")
        End If
    End Sub

    Private Sub UpdateTransportState()
        ' Update transport control state based on current activity
        If recorder IsNot Nothing AndAlso recorder.IsRecording Then
            transportControl.State = UI.TransportControl.TransportState.Recording
            transportControl.RecordingTime = recorder.RecordingDuration
        ElseIf playbackEngine IsNot Nothing AndAlso playbackEngine.IsPlaying Then
            transportControl.State = UI.TransportControl.TransportState.Playing
            transportControl.TrackPosition = playbackEngine.CurrentPosition
            transportControl.TrackDuration = playbackEngine.TotalDuration
        Else
            transportControl.State = UI.TransportControl.TransportState.Stopped
        End If
    End Sub

#End Region

    Private Sub TimerAudio_Tick(sender As Object, e As EventArgs) Handles TimerAudio.Tick
        Try
            ' If we're recording, let the recorder handle everything
            If recorder IsNot Nothing AndAlso recorder.InputSource IsNot Nothing Then
                recorder.Process()

                ' Update recording timer
                Dim duration = recorder.RecordingDuration
                lblRecordingTime.Text = $"{duration.Minutes:00}:{duration.Seconds:00}"

                ' Update transport control
                transportControl.RecordingTime = duration

                ' Update meter from recorder's last buffer
                If recorder.LastBuffer IsNot Nothing Then
                    Try
                        Dim levelData = AudioLevelMeter.AnalyzeSamples(
                            recorder.LastBuffer,
                            recorder.InputSource.BitsPerSample,
                            recorder.InputSource.Channels)

                        meterRecording.SetLevel(levelData.PeakDB, levelData.RMSDB, levelData.IsClipping)
                    Catch ex As Exception
                        ' Ignore metering errors
                    End Try
                End If
            ElseIf mic IsNot Nothing Then
                ' Not recording, just consume buffers to keep meter working
                Dim buffer(4095) As Byte
                Dim read = mic.Read(buffer, 0, buffer.Length)

                If read > 0 Then
                    Try
                        Dim levelData = AudioLevelMeter.AnalyzeSamples(
                        buffer,
                        mic.BitsPerSample,
                        mic.Channels)

                        meterRecording.SetLevel(levelData.PeakDB, levelData.RMSDB, levelData.IsClipping)
                    Catch ex As Exception
                        ' Ignore metering errors
                    End Try
                End If
            End If

            ' Update transport state
            UpdateTransportState()
        Catch ex As Exception
            ' Catch any timer errors to prevent clicks from exceptions
            Logger.Instance.Error("Error in TimerAudio_Tick", ex, "MainForm")
        End Try
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
            Services.LoggingServiceAdapter.Instance.LogInfo($"Input device changed: {cmbInputDevices.SelectedItem}")
        End If
    End Sub

    Private Sub cmbSampleRates_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSampleRates.SelectedIndexChanged
        If cmbSampleRates.SelectedItem IsNot Nothing Then
            Services.LoggingServiceAdapter.Instance.LogInfo($"Sample rate changed: {cmbSampleRates.SelectedItem} Hz")
        End If
    End Sub

    Private Sub cmbBitDepths_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbBitDepths.SelectedIndexChanged
        If cmbBitDepths.SelectedItem IsNot Nothing Then
            Services.LoggingServiceAdapter.Instance.LogInfo($"Bit depth changed: {cmbBitDepths.SelectedItem}-bit")
        End If
    End Sub

    Private Sub cmbChannelMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbChannelMode.SelectedIndexChanged
        If cmbChannelMode.SelectedItem IsNot Nothing Then
            Services.LoggingServiceAdapter.Instance.LogInfo($"Channel mode changed: {cmbChannelMode.SelectedItem}")
        End If
    End Sub

    Private Sub cmbBufferSize_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbBufferSize.SelectedIndexChanged
        If cmbBufferSize.SelectedItem IsNot Nothing Then
            Services.LoggingServiceAdapter.Instance.LogInfo($"Buffer size changed: {cmbBufferSize.SelectedItem} ms")
        End If
    End Sub

    Private Sub mainTabs_SelectedIndexChanged(sender As Object, e As EventArgs)
        If mainTabs.SelectedTab IsNot Nothing Then
            Services.LoggingServiceAdapter.Instance.LogInfo($"Switched to tab: {mainTabs.SelectedTab.Text}")
        End If
    End Sub

#End Region

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        ' Unsubscribe from events
        RemoveHandler Services.LoggingServiceAdapter.Instance.LogMessageReceived, AddressOf OnLogMessage

        ' Log shutdown
        Services.LoggingServiceAdapter.Instance.LogInfo("DSP Processor shutting down")

        ' Existing cleanup code
        TimerAudio.Stop()

        If recorder IsNot Nothing Then
            recorder.StopRecording()
        End If

        If mic IsNot Nothing Then
            mic.Dispose()
        End If

        ' NEW: Cleanup playback and waveform renderer
        If playbackEngine IsNot Nothing Then
            playbackEngine.Dispose()
        End If

        If waveformRenderer IsNot Nothing Then
            waveformRenderer.ClearCache()
        End If

        ' NEW: Close logger
        Logger.Instance.Close()

        MyBase.OnFormClosing(e)
    End Sub

End Class
