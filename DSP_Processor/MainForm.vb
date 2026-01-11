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
        DarkTheme.ApplyPrimaryButton(btnRecord)
        DarkTheme.ApplyDangerButton(btnStop)
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

        ' Start meter timer
        TimerMeters.Start()

        ' Initialize playback controls
        btnStopPlayback.Enabled = False
        trackVolume.Value = 100
        lblVolume.Text = "100%"

        ' Pre-warm audio drivers
        PreWarmAudioDrivers()

        ' ARM THE MIC - Start it now so it's ready when user clicks Record
        ArmMicrophone()

        Logger.Instance.Info("DSP Processor started", "MainForm")
    End Sub

    ''' <summary>
    ''' Pre-warms NAudio drivers by briefly initializing and releasing audio device.
    ''' This eliminates the "cold start" delay on first recording.
    ''' </summary>
    Private Sub PreWarmAudioDrivers()
        Try
            ' Get current settings
            Dim deviceIndex = cmbInputDevices.SelectedIndex
            If deviceIndex < 0 Then Return ' No device selected

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

            Logger.Instance.Debug("Audio drivers pre-warmed successfully", "MainForm")

        Catch ex As Exception
            ' Don't fail if pre-warming fails - just log it
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
            For Each file In Directory.GetFiles(folder, "*.wav")
                lstRecordings.Items.Add(Path.GetFileName(file))
            Next
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

    Private Sub btnRecord_Click(sender As Object, e As EventArgs) Handles btnRecord.Click
        Try
            ' Mic is already armed and running, just need to start writing to file!

            ' If mic isn't armed for some reason, arm it now
            If Not micIsArmed OrElse mic Is Nothing Then
                Dim deviceIndex = cmbInputDevices.SelectedIndex
                Dim sampleRate = Integer.Parse(cmbSampleRates.SelectedItem.ToString())
                Dim bits = CInt(cmbBitDepths.SelectedItem)
                Dim channelMode As String = cmbChannelMode.SelectedItem.ToString()
                Dim bufferMs = CInt(cmbBufferSize.SelectedItem)

                mic = New MicInputSource(sampleRate, channelMode, bits, deviceIndex, bufferMs)
                TimerAudio.Start()
                System.Threading.Thread.Sleep(1000) ' Give time to warm up
            End If

            ' CRITICAL: Clear stale buffers before starting new recording
            If mic IsNot Nothing Then
                mic.ClearBuffers()
                Logger.Instance.Info("Cleared stale buffers before recording", "MainForm")
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

            ' Update UI - instant!
            lblStatus.Text = "Status: Recording..."
            panelLED.BackColor = Color.Red
            lblRecordingTime.Visible = True
            lblRecordingTime.Text = "00:00"

            Logger.Instance.Info("Recording started (mic was already armed)", "MainForm")

        Catch ex As Exception
            MessageBox.Show($"Failed to start recording: {ex.Message}", "Recording Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Logger.Instance.Error("Failed to start recording", ex, "MainForm")
        End Try
    End Sub

    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        recorder.StopRecording()
        RefreshRecordingList()

        ' Keep mic armed (don't stop timer) - just stop writing to file
        lblStatus.Text = "Status: Ready (Mic Armed)"
        panelLED.BackColor = Color.Yellow ' Back to armed state
        lblRecordingTime.Visible = False ' Hide timer

        ' Reset recording meter
        meterRecording.Reset()

        Logger.Instance.Info("Recording stopped (mic still armed)", "MainForm")
    End Sub

    Private Sub TimerAudio_Tick(sender As Object, e As EventArgs) Handles TimerAudio.Tick
        Try
            ' If we're recording, let the recorder handle everything
            If recorder IsNot Nothing AndAlso recorder.InputSource IsNot Nothing Then
                recorder.Process()

                ' Update recording timer
                Dim duration = recorder.RecordingDuration
                lblRecordingTime.Text = $"{duration.Minutes:00}:{duration.Seconds:00}"

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
        Catch ex As Exception
            ' Catch any timer errors to prevent clicks from exceptions
            Logger.Instance.Error("Error in TimerAudio_Tick", ex, "MainForm")
        End Try
    End Sub

    Private Sub TimerMeters_Tick(sender As Object, e As EventArgs) Handles TimerMeters.Tick
        ' Update playback meter - DISABLED for now as sample monitoring causes audio glitches
        ' TODO: Find better way to meter playback without intercepting audio stream
        If playbackEngine IsNot Nothing AndAlso playbackEngine.IsPlaying Then
            ' Just keep meter at mid-level during playback as visual indicator
            meterPlayback.SetLevel(-12, -18, False)
        Else
            ' Reset meter when not playing
            meterPlayback.SetLevel(-60, -60, False)
        End If

        ' Reset recording meter when not recording
        If Not TimerAudio.Enabled Then
            meterRecording.SetLevel(-60, -60, False)
        End If
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
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
    Private Sub lstRecordings_DoubleClick(sender As Object, e As EventArgs) Handles lstRecordings.DoubleClick
        If lstRecordings.SelectedItem Is Nothing Then Return

        Dim fileName = lstRecordings.SelectedItem.ToString()
        Dim fullPath = Path.Combine(Application.StartupPath, "Recordings", fileName)

        Try
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

        Catch ex As Exception
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
        End If
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        ' Check if a recording is selected
        If lstRecordings.SelectedItem Is Nothing Then
            MessageBox.Show("Please select a recording to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim fileName = lstRecordings.SelectedItem.ToString()
        Dim fullPath = Path.Combine(Application.StartupPath, "Recordings", fileName)

        ' Confirm deletion
        Dim result = MessageBox.Show(
            $"Are you sure you want to delete '{fileName}'?{Environment.NewLine}{Environment.NewLine}This cannot be undone.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2)

        If result = DialogResult.Yes Then
            Try
                ' Clear waveform if this file is displayed
                If Not String.IsNullOrEmpty(fileName) Then
                    picWaveform.Image = Nothing
                    waveformRenderer.ClearCache()
                End If

                ' Stop playback if this file is playing
                If playbackEngine IsNot Nothing AndAlso playbackEngine.IsPlaying Then
                    playbackEngine.Stop()
                End If

                ' Force release of any file handles
                GC.Collect()
                GC.WaitForPendingFinalizers()

                ' Delete the file
                If File.Exists(fullPath) Then
                    File.Delete(fullPath)
                    Logger.Instance.Info($"Deleted recording: {fileName}", "MainForm")

                    ' Refresh the list
                    RefreshRecordingList()

                    MessageBox.Show($"'{fileName}' has been deleted.", "Delete Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    MessageBox.Show($"File not found: {fileName}", "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If

            Catch ex As Exception
                MessageBox.Show($"Failed to delete file: {ex.Message}", "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Logger.Instance.Error($"Failed to delete recording: {fileName}", ex, "MainForm")
            End Try
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
            Using timer = Logger.Instance.StartTimer("Waveform Rendering")
                Dim waveform = waveformRenderer.Render(fullPath, picWaveform.Width, picWaveform.Height)
                picWaveform.Image = waveform
            End Using
        Catch ex As Exception
            Logger.Instance.Error("Failed to render waveform", ex, "MainForm")
        End Try
    End Sub

    ''' <summary>
    ''' Arms the microphone for recording - starts capture but doesn't write to file.
    ''' This eliminates the cold-start delay and lets meters work immediately.
    ''' </summary>
    Private Sub ArmMicrophone()
        Try
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

            Logger.Instance.Debug($"Arming microphone: {channelMode}, {sampleRate}Hz, {bits}-bit", "MainForm")

            ' Create and start mic (audio capture begins, but we're not recording yet)
            mic = New MicInputSource(sampleRate, channelMode, bits, deviceIndex, bufferMs)

            ' Start timer to consume buffers (prevents queue buildup)
            TimerAudio.Start()

            micIsArmed = True
            lblStatus.Text = "Status: Ready (Mic Armed)"
            panelLED.BackColor = Color.Yellow ' Yellow = Armed but not recording

            Logger.Instance.Info("Microphone armed and ready", "MainForm")

        Catch ex As Exception
            Logger.Instance.Error("Failed to arm microphone", ex, "MainForm")
            MessageBox.Show($"Failed to arm microphone: {ex.Message}", "Microphone Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub cmbBitDepths_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbBitDepths.SelectedIndexChanged

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

    End Sub
End Class
