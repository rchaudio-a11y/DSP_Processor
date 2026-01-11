Imports System.IO
Imports DSP_Processor.AudioIO
Imports DSP_Processor.Recording
Imports NAudio.Wave

Partial Public Class MainForm
    Private mic As MicInputSource
    Private recorder As RecordingEngine
    Private playbackOutput As WaveOutEvent
    Private playbackReader As AudioFileReader



    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' Create recording engine (NO InputSource yet)
        recorder = New RecordingEngine() With {
        .OutputFolder = "Recordings",
        .AutoNamePattern = "Take_{0:000}.wav",
        .TimedRecordingEnabled = False,
        .AutoRestartEnabled = False,
        .MaxRecordings = 1
    }

        ' Ensure recordings folder exists
        Dim folder = Path.Combine(Application.StartupPath, "Recordings")
        If Not Directory.Exists(folder) Then Directory.CreateDirectory(folder)

        ' Populate input device list
        cmbInputDevices.Items.Clear()
        For i = 0 To WaveIn.DeviceCount - 1
            Dim caps = WaveIn.GetCapabilities(i)
            cmbInputDevices.Items.Add($"{i}: {caps.ProductName}")
        Next

        If cmbInputDevices.Items.Count > 0 Then
            cmbInputDevices.SelectedIndex = 0
        End If

        RefreshRecordingList()
        lblStatus.Text = "Status: Idle"
        PopulateInputDevices()
        PopulateSampleRates()
        PopulateBitDepths()
        PopulateChannelModes()
        PopulateBufferSizes()

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
        Dim deviceIndex = cmbInputDevices.SelectedIndex
        Dim sampleRate = Integer.Parse(cmbSampleRates.SelectedItem.ToString())
        Dim bits = CInt(cmbBitDepths.SelectedItem)
        Dim channelMode As String = cmbChannelMode.SelectedItem.ToString()
        Dim bufferMs = CInt(cmbBufferSize.SelectedItem)

        ' Create the input source with correct parameter order: sampleRate, channels (string), bits, deviceIndex, bufferMs
        mic = New MicInputSource(sampleRate, channelMode, bits, deviceIndex, bufferMs)

        recorder.InputSource = mic
        recorder.StartRecording()

        TimerAudio.Start()
        lblStatus.Text = "Status: Recording..."
        panelLED.BackColor = Color.Red
    End Sub



    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        TimerAudio.Stop()
        recorder.StopRecording()
        RefreshRecordingList()
        panelLED.BackColor = Color.Green
        lblStatus.Text = "Status: Idle"




    End Sub


    Private Sub TimerAudio_Tick(sender As Object, e As EventArgs) Handles TimerAudio.Tick
        recorder.Process()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        TimerAudio.Stop()

        If recorder IsNot Nothing Then
            recorder.StopRecording()
        End If

        If mic IsNot Nothing Then
            mic.Dispose()
        End If

        MyBase.OnFormClosing(e)
    End Sub
    Private Sub lstRecordings_DoubleClick(sender As Object, e As EventArgs) Handles lstRecordings.DoubleClick
        If lstRecordings.SelectedItem Is Nothing Then Return

        Dim fileName = lstRecordings.SelectedItem.ToString()
        Dim fullPath = Path.Combine(Application.StartupPath, "Recordings", fileName)

        ' Stop previous playback if any
        If playbackOutput IsNot Nothing Then
            playbackOutput.Stop()
            playbackOutput.Dispose()
            playbackOutput = Nothing
        End If
        progressPlayback.Style = ProgressBarStyle.Marquee
        If playbackReader IsNot Nothing Then
            playbackReader.Dispose()
            playbackReader = Nothing
        End If

        playbackReader = New AudioFileReader(fullPath)
        playbackOutput = New WaveOutEvent()

        playbackOutput.Init(playbackReader)
        playbackOutput.Play()
        panelLED.BackColor = Color.RoyalBlue
        lblStatus.Text = $"Status: Playing {fileName}"



        TimerPlayback.Start()
        ' Detect when playback ends
        AddHandler playbackOutput.PlaybackStopped, AddressOf OnPlaybackStopped



    End Sub
    Private Sub OnPlaybackStopped(sender As Object, e As StoppedEventArgs)
        panelLED.BackColor = Color.Green
        lblStatus.Text = "Status: Idle"



        ' Clean up
        playbackOutput?.Dispose()
        playbackReader?.Dispose()
        playbackOutput = Nothing
        playbackReader = Nothing
        TimerPlayback.Stop()
        progressPlayback.Value = 0
    End Sub

    Private Sub TimerPlayback_Tick(sender As Object, e As EventArgs) Handles TimerPlayback.Tick
        If playbackReader Is Nothing Then
            progressPlayback.Value = 0
            Return
        End If

        Dim pos = playbackReader.CurrentTime.TotalMilliseconds
        Dim len = playbackReader.TotalTime.TotalMilliseconds

        If len > 0 Then
            Dim pct = CInt((pos / len) * 1000)
            progressPlayback.Value = Math.Min(1000, Math.Max(0, pct))
        End If
    End Sub
    Private Sub lstRecordings_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstRecordings.SelectedIndexChanged
        If lstRecordings.SelectedItem Is Nothing Then Return

        Dim fileName = lstRecordings.SelectedItem.ToString()
        Dim fullPath = Path.Combine(Application.StartupPath, "Recordings", fileName)

        DrawWaveform(fullPath)
    End Sub
    Private Sub DrawWaveform(path As String)
        Dim width = picWaveform.Width
        Dim height = picWaveform.Height

        Dim bmp As New Bitmap(width, height)
        Using g = Graphics.FromImage(bmp)
            g.Clear(Color.Black)

            Using reader As New AudioFileReader(path)

                Dim channels = reader.WaveFormat.Channels
                If channels < 2 Then
                    ' fallback to mono renderer if needed
                    DrawWaveformMono(path)
                    Return
                End If

                ' -------------------------------
                ' PASS 1: Find peak per channel
                ' -------------------------------
                Dim maxL As Single = 0.0F
                Dim maxR As Single = 0.0F

                Dim buffer(4096 - 1) As Single
                Dim read As Integer

                Do
                    read = reader.Read(buffer, 0, buffer.Length)
                    For i = 0 To read - 1 Step 2
                        Dim l = Math.Abs(buffer(i))
                        Dim r = Math.Abs(buffer(i + 1))

                        If l > maxL Then maxL = l
                        If r > maxR Then maxR = r
                    Next
                Loop While read > 0

                If maxL < 0.000001F Then maxL = 0.000001F
                If maxR < 0.000001F Then maxR = 0.000001F

                ' Reset reader for drawing
                reader.Position = 0

                ' -------------------------------
                ' PASS 2: Draw stereo lanes
                ' -------------------------------
                Dim samplesPerPixel = CInt(reader.WaveFormat.SampleRate / width)
                If samplesPerPixel < 1 Then samplesPerPixel = 1

                Dim drawBuffer(samplesPerPixel * 2 - 1) As Single
                Dim x As Integer = 0

                Dim midL = height \ 4
                Dim midR = (height * 3) \ 4

                While reader.Read(drawBuffer, 0, drawBuffer.Length) > 0 AndAlso x < width

                    Dim localMaxL As Single = -1
                    Dim localMinL As Single = 1
                    Dim localMaxR As Single = -1
                    Dim localMinR As Single = 1

                    For i = 0 To drawBuffer.Length - 2 Step 2
                        Dim l = drawBuffer(i)
                        Dim r = drawBuffer(i + 1)

                        If l > localMaxL Then localMaxL = l
                        If l < localMinL Then localMinL = l

                        If r > localMaxR Then localMaxR = r
                        If r < localMinR Then localMinR = r
                    Next

                    ' Scale each channel independently
                    Dim yMaxL = midL - CInt((localMaxL / maxL) * (height \ 4))
                    Dim yMinL = midL - CInt((localMinL / maxL) * (height \ 4))

                    Dim yMaxR = midR - CInt((localMaxR / maxR) * (height \ 4))
                    Dim yMinR = midR - CInt((localMinR / maxR) * (height \ 4))

                    g.DrawLine(Pens.Lime, x, yMaxL, x, yMinL)
                    g.DrawLine(Pens.Cyan, x, yMaxR, x, yMinR)

                    x += 1
                End While

            End Using
        End Using

        picWaveform.Image = bmp
    End Sub
    Private Sub DrawWaveformMono(path As String)
        Dim width = picWaveform.Width
        Dim height = picWaveform.Height

        Dim bmp As New Bitmap(width, height)
        Using g = Graphics.FromImage(bmp)
            g.Clear(Color.Black)

            Using reader As New AudioFileReader(path)

                ' First pass: find peak
                Dim maxPeak As Single = 0.0F
                Dim buffer(4096 - 1) As Single
                Dim read As Integer

                Do
                    read = reader.Read(buffer, 0, buffer.Length)
                    For i = 0 To read - 1
                        Dim v = Math.Abs(buffer(i))
                        If v > maxPeak Then maxPeak = v
                    Next
                Loop While read > 0

                If maxPeak < 0.000001F Then maxPeak = 0.000001F

                ' Reset for drawing
                reader.Position = 0

                Dim samplesPerPixel = CInt(reader.WaveFormat.SampleRate / width)
                If samplesPerPixel < 1 Then samplesPerPixel = 1

                Dim drawBuffer(samplesPerPixel - 1) As Single
                Dim x As Integer = 0

                While reader.Read(drawBuffer, 0, drawBuffer.Length) > 0 AndAlso x < width
                    Dim localMax = drawBuffer.Max()
                    Dim localMin = drawBuffer.Min()

                    Dim scaledMax = localMax / maxPeak
                    Dim scaledMin = localMin / maxPeak

                    Dim yMax = CInt((1 - scaledMax) * height / 2)
                    Dim yMin = CInt((1 - scaledMin) * height / 2)

                    g.DrawLine(Pens.Lime, x, yMax, x, yMin)
                    x += 1
                End While

            End Using
        End Using

        picWaveform.Image = bmp
    End Sub







End Class
