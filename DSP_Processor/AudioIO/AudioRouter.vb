Imports NAudio.Wave

Namespace AudioIO

    ''' <summary>
    ''' Simplified audio routing coordinator for Phase 2.0.
    ''' Manages input source selection and output device selection.
    ''' DSP integration added for pass-through testing.
    ''' </summary>
    Public Class AudioRouter
        Implements IDisposable

#Region "Enums"

        Public Enum InputSourceType
            Microphone
            FilePlayback
        End Enum

        Public Enum OutputDestinationType
            None = 0
            Speakers = 1
            File = 2
            Both = 3
        End Enum

#End Region

#Region "Private Fields"

        Private _currentInputSource As InputSourceType = InputSourceType.Microphone
        Private _currentOutputDestination As OutputDestinationType = OutputDestinationType.Speakers
        Private outputDeviceManager As OutputDeviceManager
        Private disposed As Boolean = False

        ' DSP Components
        Private dspThread As DSP.DSPThread
        Private dspOutputProvider As DSPOutputProvider
        Private waveOut As WaveOutEvent
        Private fileReader As AudioFileReader
        Private _selectedInputFile As String
        Private feederThread As System.Threading.Thread
        Private feederCancellation As Boolean = False ' Cancellation flag for feeder thread
        Private _isPlaying As Boolean = False ' CRITICAL: Explicit playback state flag (Issue #1 Fix - Phase 6)
        Private _inputGainProcessor As DSP.GainProcessor ' Input gain stage (Phase 2.5)
        Private _outputGainProcessor As DSP.GainProcessor ' Output gain stage (Phase 2.5)

        ' Playback position tracking (for TransportControl integration)
        Private _playbackStartTime As DateTime = DateTime.MinValue
        Private _playbackDuration As TimeSpan = TimeSpan.Zero

#End Region

#Region "Properties"

        ''' <summary>Gets or sets the current input source</summary>
        Public Property CurrentInputSource As InputSourceType
            Get
                Return _currentInputSource
            End Get
            Set(value As InputSourceType)
                If value <> _currentInputSource Then
                    _currentInputSource = value
                    Utils.Logger.Instance.Info($"Input source changed to: {value}", "AudioRouter")
                    RaiseEvent InputSourceChanged(Me, value)
                End If
            End Set
        End Property

        ''' <summary>Gets or sets the current output destination</summary>
        Public Property CurrentOutputDestination As OutputDestinationType
            Get
                Return _currentOutputDestination
            End Get
            Set(value As OutputDestinationType)
                If value <> _currentOutputDestination Then
                    _currentOutputDestination = value
                    Utils.Logger.Instance.Info($"Output destination changed to: {value}", "AudioRouter")
                    RaiseEvent OutputDestinationChanged(Me, value)
                End If
            End Set
        End Property

        ''' <summary>Gets the output device manager</summary>
        Public ReadOnly Property OutputManager As OutputDeviceManager
            Get
                Return outputDeviceManager
            End Get
        End Property

        ''' <summary>Gets or sets the selected input file path</summary>
        Public Property SelectedInputFile As String
            Get
                Return _selectedInputFile
            End Get
            Set(value As String)
                _selectedInputFile = value
                Utils.Logger.Instance.Info($"Selected input file: {value}", "AudioRouter")
            End Set
        End Property

        ''' <summary>Is audio currently playing through DSP?</summary>
        Public ReadOnly Property IsPlaying As Boolean
            Get
                ' PHASE 6 FIX: Use explicit flag instead of waveOut.PlaybackState
                ' waveOut.PlaybackState is async/event-driven and may not reflect actual state immediately
                Return _isPlaying
            End Get
        End Property

        ''' <summary>Gets current playback position (for TransportControl)</summary>
        Public ReadOnly Property CurrentPosition As TimeSpan
            Get
                If IsPlaying AndAlso _playbackStartTime <> DateTime.MinValue Then
                    Dim elapsed = DateTime.Now - _playbackStartTime
                    ' Cap position at total duration (don't show time past file length during buffer drain)
                    Return If(elapsed > _playbackDuration, _playbackDuration, elapsed)
                End If
                Return TimeSpan.Zero
            End Get
        End Property

        ''' <summary>Gets total duration of loaded file (for TransportControl)</summary>
        Public ReadOnly Property TotalDuration As TimeSpan
            Get
                Return _playbackDuration
            End Get
        End Property

        ''' <summary>Gets the DSP thread for monitoring</summary>
        Public ReadOnly Property Thread As DSP.DSPThread
            Get
                Return dspThread
            End Get
        End Property

        ''' <summary>Gets the file reader wave format</summary>
        Public ReadOnly Property CurrentFormat As WaveFormat
            Get
                Return fileReader?.WaveFormat
            End Get
        End Property

        ''' <summary>Gets the INPUT GainProcessor instance (for UI control - Phase 2.5)</summary>
        Public ReadOnly Property InputGainProcessor As DSP.GainProcessor
            Get
                Return _inputGainProcessor
            End Get
        End Property

        ''' <summary>Gets the OUTPUT GainProcessor instance (for UI control - Phase 2.5)</summary>
        Public ReadOnly Property OutputGainProcessor As DSP.GainProcessor
            Get
                Return _outputGainProcessor
            End Get
        End Property

        ''' <summary>DEPRECATED: Use InputGainProcessor instead (kept for compatibility)</summary>
        Public ReadOnly Property GainProcessor As DSP.GainProcessor
            Get
                Return _inputGainProcessor
            End Get
        End Property

        ''' <summary>Gets input samples for meter display (reads from monitor buffer)</summary>
        Public ReadOnly Property InputSamples As Single()
            Get
                If dspThread IsNot Nothing Then
                    Dim available = dspThread.InputMonitorAvailable()
                    If available > 0 Then
                        ' Read up to 4KB for meters (0.1 seconds at 44.1kHz stereo)
                        Dim bufferSize = Math.Min(available, 4096)
                        Dim buffer(bufferSize - 1) As Byte
                        Dim bytesRead = dspThread.ReadInputMonitor(buffer, 0, buffer.Length)

                        If bytesRead > 0 Then
                            ' Convert Int16 PCM to Float32 samples
                            Dim sampleCount = bytesRead \ 2 ' 16-bit samples
                            Dim samples(sampleCount - 1) As Single
                            For i = 0 To sampleCount - 1
                                Dim int16Sample = BitConverter.ToInt16(buffer, i * 2)
                                samples(i) = int16Sample / 32768.0F ' Normalize to -1.0 to +1.0
                            Next
                            Return samples
                        End If
                    End If
                End If
                Return Nothing
            End Get
        End Property

        ''' <summary>Gets post-gain samples for meter display (reads from PostGain tap point - DSP TAP PATTERN)</summary>
        Public ReadOnly Property PostGainSamples As Single()
            Get
                If dspThread IsNot Nothing Then
                    Dim available = dspThread.PostGainMonitorAvailable()
                    If available > 0 Then
                        ' Read up to 4KB for meters
                        Dim bufferSize = Math.Min(available, 4096)
                        Dim buffer(bufferSize - 1) As Byte
                        Dim bytesRead = dspThread.ReadPostGainMonitor(buffer, 0, buffer.Length)

                        If bytesRead > 0 Then
                            ' Convert Int16 PCM to Float32 samples
                            Dim sampleCount = bytesRead \ 2 ' 16-bit samples
                            Dim samples(sampleCount - 1) As Single
                            For i = 0 To sampleCount - 1
                                Dim int16Sample = BitConverter.ToInt16(buffer, i * 2)
                                samples(i) = int16Sample / 32768.0F ' Normalize to -1.0 to +1.0
                            Next
                            Return samples
                        End If
                    End If
                End If
                Return Nothing
            End Get
        End Property

        ''' <summary>Gets post-OUTPUT-gain samples for meter display (Phase 2.5 - Output tap point)</summary>
        Public ReadOnly Property PostOutputGainSamples As Single()
            Get
                If dspThread IsNot Nothing Then
                    Dim available = dspThread.PostOutputGainMonitorAvailable()
                    If available > 0 Then
                        ' Read up to 4KB for meters
                        Dim bufferSize = Math.Min(available, 4096)
                        Dim buffer(bufferSize - 1) As Byte
                        Dim bytesRead = dspThread.ReadPostOutputGainMonitor(buffer, 0, buffer.Length)

                        If bytesRead > 0 Then
                            ' Convert Int16 PCM to Float32 samples
                            Dim sampleCount = bytesRead \ 2 ' 16-bit samples
                            Dim samples(sampleCount - 1) As Single
                            For i = 0 To sampleCount - 1
                                Dim int16Sample = BitConverter.ToInt16(buffer, i * 2)
                                samples(i) = int16Sample / 32768.0F ' Normalize to -1.0 to +1.0
                            Next
                            Return samples
                        End If
                    End If
                End If
                Return Nothing
            End Get
        End Property

        ''' <summary>Gets output samples for meter display (reads from monitor buffer)</summary>
        Public ReadOnly Property OutputSamples As Single()
            Get
                If dspThread IsNot Nothing Then
                    Dim available = dspThread.OutputMonitorAvailable()
                    If available > 0 Then
                        ' Read up to 4KB for meters
                        Dim bufferSize = Math.Min(available, 4096)
                        Dim buffer(bufferSize - 1) As Byte
                        Dim bytesRead = dspThread.ReadOutputMonitor(buffer, 0, buffer.Length)

                        If bytesRead > 0 Then
                            ' Convert Int16 PCM to Float32 samples
                            Dim sampleCount = bytesRead \ 2 ' 16-bit samples
                            Dim samples(sampleCount - 1) As Single
                            For i = 0 To sampleCount - 1
                                Dim int16Sample = BitConverter.ToInt16(buffer, i * 2)
                                samples(i) = int16Sample / 32768.0F ' Normalize to -1.0 to +1.0
                            Next
                            Return samples
                        End If
                    End If
                End If
                Return Nothing
            End Get
        End Property

#Region "Multi-Reader Tap Point API (Phase 2.5 - Architecture Rule #4)"

        ''' <summary>
        ''' Create a custom reader for a specific DSP tap point
        ''' Enables flexible routing of instruments (FFT, meters, analyzers) to any tap
        ''' </summary>
        ''' <param name="tapLocation">Which tap point to read from (PreDSP, PostGain, PostDSP, PreOutput)</param>
        ''' <param name="readerName">Unique name for reader (e.g., "CustomFFT", "PhaseAnalyzer")</param>
        ''' <returns>Reader handle for use with ReadFromTap()</returns>
        Public Function CreateTapReader(tapLocation As DSP.DSPThread.TapLocation, readerName As String) As String
            If dspThread Is Nothing Then
                Throw New InvalidOperationException("DSP thread not initialized! Call StartDSPPlayback() first.")
            End If

            Return dspThread.CreateTapReader(tapLocation, readerName)
        End Function

        ''' <summary>
        ''' Remove a custom tap reader
        ''' </summary>
        Public Sub RemoveTapReader(tapLocation As DSP.DSPThread.TapLocation, readerName As String)
            If dspThread IsNot Nothing Then
                dspThread.RemoveTapReader(tapLocation, readerName)
            End If
        End Sub

        ''' <summary>
        ''' Read audio from a custom tap reader
        ''' </summary>
        Public Function ReadFromTap(tapLocation As DSP.DSPThread.TapLocation, readerName As String, buffer As Byte(), offset As Integer, count As Integer) As Integer
            If dspThread Is Nothing Then Return 0
            Return dspThread.ReadFromTap(tapLocation, readerName, buffer, offset, count)
        End Function

        ''' <summary>
        ''' Check how many bytes are available for a specific reader
        ''' </summary>
        Public Function TapAvailable(tapLocation As DSP.DSPThread.TapLocation, readerName As String) As Integer
            If dspThread Is Nothing Then Return 0
            Return dspThread.TapAvailable(tapLocation, readerName)
        End Function

        ''' <summary>
        ''' Check if a custom reader exists
        ''' </summary>
        Public Function HasTapReader(tapLocation As DSP.DSPThread.TapLocation, readerName As String) As Boolean
            If dspThread Is Nothing Then Return False
            Return dspThread.HasTapReader(tapLocation, readerName)
        End Function

#End Region

#End Region

#Region "Events"

        Public Event InputSourceChanged As EventHandler(Of InputSourceType)
        Public Event OutputDestinationChanged As EventHandler(Of OutputDestinationType)
        Public Event PlaybackStopped As EventHandler
        Public Event PlaybackCompleted As EventHandler

        ''' <summary>Raised when playback starts (for TransportControl integration)</summary>
        Public Event PlaybackStarted As EventHandler(Of String)

        ''' <summary>Raised periodically during playback to update position (for TransportControl)</summary>
        Public Event PositionChanged As EventHandler(Of TimeSpan)

        ''' <summary>Raised when audio samples are available for FFT analysis (input)</summary>
        Public Event InputSamplesAvailable As EventHandler(Of AudioSamplesEventArgs)

        ''' <summary>Raised when audio samples are available for FFT analysis (output)</summary>
        Public Event OutputSamplesAvailable As EventHandler(Of AudioSamplesEventArgs)

#End Region

#Region "Initialization"

        ''' <summary>
        ''' Initializes the audio router
        ''' </summary>
        Public Sub Initialize()
            Try
                ' Create output device manager
                outputDeviceManager = New OutputDeviceManager()

                ' Select default output device
                Dim defaultDevice = outputDeviceManager.GetDefaultDeviceIndex()
                If defaultDevice >= 0 Then
                    outputDeviceManager.SelectedDeviceIndex = defaultDevice
                    Utils.Logger.Instance.Info($"Default output device: {defaultDevice}", "AudioRouter")
                End If

                Utils.Logger.Instance.Info("AudioRouter initialized (Phase 2.0 with DSP routing)", "AudioRouter")
            Catch ex As Exception
                Utils.Logger.Instance.Error("Failed to initialize AudioRouter", ex, "AudioRouter")
                Throw
            End Try
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Gets available output device names
        ''' </summary>
        Public Function GetOutputDeviceNames() As String()
            If outputDeviceManager IsNot Nothing Then
                Return outputDeviceManager.GetDeviceNames()
            End If
            Return Array.Empty(Of String)()
        End Function

        ''' <summary>
        ''' Selects an output device by index
        ''' </summary>
        ''' <param name="deviceIndex">Device index</param>
        Public Sub SelectOutputDevice(deviceIndex As Integer)
            Try
                If outputDeviceManager IsNot Nothing Then
                    outputDeviceManager.SelectedDeviceIndex = deviceIndex
                    Dim deviceNames = outputDeviceManager.GetDeviceNames()
                    If deviceIndex >= 0 AndAlso deviceIndex < deviceNames.Length Then
                        Utils.Logger.Instance.Info($"Output device selected: {deviceNames(deviceIndex)}", "AudioRouter")
                    End If
                End If
            Catch ex As Exception
                Utils.Logger.Instance.Error("Failed to select output device", ex, "AudioRouter")
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Set output device (Task 2.0.2 - alias for SelectOutputDevice for consistency)
        ''' </summary>
        Public Sub SetOutputDevice(deviceIndex As Integer)
            SelectOutputDevice(deviceIndex)
        End Sub

        ''' <summary>
        ''' Gets the currently selected output device index
        ''' </summary>
        Public Function GetSelectedOutputDevice() As Integer
            If outputDeviceManager IsNot Nothing Then
                Return outputDeviceManager.SelectedDeviceIndex
            End If
            Return -1
        End Function

        ''' <summary>
        ''' Play a file through DSP pipeline (Task 2.0.2 - moved from MainForm)
        ''' Encapsulates file loading and playback initiation
        ''' </summary>
        ''' <param name="filepath">Full path to audio file</param>
        Public Sub PlayFile(filepath As String)
            Try
                Utils.Logger.Instance.Info($"PlayFile called: {IO.Path.GetFileName(filepath)}", "AudioRouter")

                ' Validate file exists
                If String.IsNullOrEmpty(filepath) OrElse Not IO.File.Exists(filepath) Then
                    Throw New IO.FileNotFoundException($"Audio file not found: {filepath}")
                End If

                ' Set selected file
                SelectedInputFile = filepath

                ' Start DSP playback
                StartDSPPlayback()

                Utils.Logger.Instance.Info($"PlayFile completed: {IO.Path.GetFileName(filepath)}", "AudioRouter")

            Catch ex As Exception
                Utils.Logger.Instance.Error($"PlayFile failed: {ex.Message}", ex, "AudioRouter")
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Start playing audio through DSP chain (file input only for now)
        ''' </summary>
        Public Sub StartDSPPlayback()
            Try
                ' Stop any existing playback
                StopDSPPlayback()

                ' Validate we have a file selected
                If String.IsNullOrEmpty(_selectedInputFile) OrElse Not IO.File.Exists(_selectedInputFile) Then
                    Throw New InvalidOperationException("No valid input file selected")
                End If

                Utils.Logger.Instance.Info($"Starting DSP playback: {_selectedInputFile}", "AudioRouter")

                ' Create file reader (AudioFileReader provides IEEE Float)
                fileReader = New AudioFileReader(_selectedInputFile)
                Utils.Logger.Instance.Info($"File format: {fileReader.WaveFormat.SampleRate}Hz, {fileReader.WaveFormat.Channels}ch, IEEE Float (will convert to PCM16)", "AudioRouter")

                ' Create PCM16 wave format for DSP processing
                Dim pcm16Format As New WaveFormat(fileReader.WaveFormat.SampleRate, 16, fileReader.WaveFormat.Channels)

                Utils.Logger.Instance.Info("=== FORMAT CHAIN ===", "AudioRouter")
                Utils.Logger.Instance.Info($"FILE format: {fileReader.WaveFormat.SampleRate}Hz, {fileReader.WaveFormat.Channels}ch, {fileReader.WaveFormat.BitsPerSample}bit, Encoding={fileReader.WaveFormat.Encoding}, BlockAlign={fileReader.WaveFormat.BlockAlign}, AvgBytes/sec={fileReader.WaveFormat.AverageBytesPerSecond}", "AudioRouter")
                Utils.Logger.Instance.Info($"PCM16 format: {pcm16Format.SampleRate}Hz, {pcm16Format.Channels}ch, {pcm16Format.BitsPerSample}bit, Encoding={pcm16Format.Encoding}, BlockAlign={pcm16Format.BlockAlign}, AvgBytes/sec={pcm16Format.AverageBytesPerSecond}", "AudioRouter")

                ' Create DSP thread with 2-second ring buffers (proven stable size)
                ' 2 seconds provides ample buffering for 100ms WaveOut latency + processing overhead
                ' At 44.1kHz: 2 seconds = 88,200 samples per channel
                Dim inputBufferSize = pcm16Format.AverageBytesPerSecond * 2 ' 2 seconds of audio
                Dim outputBufferSize = pcm16Format.AverageBytesPerSecond * 2 ' 2 seconds of audio
                dspThread = New DSP.DSPThread(pcm16Format, inputBufferSize, outputBufferSize)

                ' AUTO-CREATE DEFAULT READERS for file playback FFT/meters
                ' (RecordingManager uses TapPointManager which creates its own readers)
                If Not dspThread.inputMonitorBuffer.HasReader("_default_input") Then
                    dspThread.inputMonitorBuffer.CreateReader("_default_input")
                    Utils.Logger.Instance.Info("✅ Created default INPUT monitor reader for file playback", "AudioRouter")
                End If

                If Not dspThread.outputMonitorBuffer.HasReader("_default_output") Then
                    dspThread.outputMonitorBuffer.CreateReader("_default_output")
                    Utils.Logger.Instance.Info("✅ Created default OUTPUT monitor reader for file playback", "AudioRouter")
                End If

                Utils.Logger.Instance.Info($"DSP format: {dspThread.Format.SampleRate}Hz, {dspThread.Format.Channels}ch, {dspThread.Format.BitsPerSample}bit, Encoding={dspThread.Format.Encoding}, BlockAlign={dspThread.Format.BlockAlign}, AvgBytes/sec={dspThread.Format.AverageBytesPerSecond}", "AudioRouter")
                Utils.Logger.Instance.Info($"DSP buffers: {inputBufferSize} bytes input, {outputBufferSize} bytes output (2 seconds each)", "AudioRouter")


                ' PHASE 2.5: Add INPUT gain processor (first in chain)
                _inputGainProcessor = New DSP.GainProcessor(pcm16Format) With {
                    .GainDB = 0.0F ' 0 dB = unity gain (no change)
                }
                dspThread.Chain.AddProcessor(_inputGainProcessor)
                Utils.Logger.Instance.Info("Added INPUT Gain processor to chain (0 dB)", "AudioRouter")

                ' DSP TAP POINT PATTERN: Wire INPUT GainProcessor output to PostGainMonitor buffer
                Dim inputTapCallCount As Integer = 0 ' Diagnostic counter
                _inputGainProcessor.SetMonitorOutputCallback(
                    Sub(buffer As DSP.AudioBuffer)
                        ' Write processed audio (after INPUT gain/pan) to monitor buffer (non-blocking)
                        If buffer IsNot Nothing AndAlso buffer.ByteCount > 0 Then
                            dspThread.postGainMonitorBuffer.Write(buffer.Buffer, 0, buffer.ByteCount)

                            ' DIAGNOSTIC: Log first few calls
                            inputTapCallCount += 1
                            If inputTapCallCount <= 5 OrElse inputTapCallCount Mod 100 = 0 Then
                                Utils.Logger.Instance.Debug($"PostInputGain tap callback #{inputTapCallCount}: {buffer.ByteCount} bytes written", "AudioRouter")
                            End If
                        End If
                    End Sub
                )
                Utils.Logger.Instance.Info("✅ INPUT GainProcessor tap point wired to PostGainMonitor buffer", "AudioRouter")

                ' PHASE 2.5: Add OUTPUT gain processor (last in chain)
                _outputGainProcessor = New DSP.GainProcessor(pcm16Format) With {
                    .GainDB = 0.0F ' 0 dB = unity gain (no change)
                }
                dspThread.Chain.AddProcessor(_outputGainProcessor)
                Utils.Logger.Instance.Info("Added OUTPUT Gain processor to chain (0 dB)", "AudioRouter")

                ' DSP TAP POINT PATTERN: Wire OUTPUT GainProcessor output to PostOutputGainMonitor buffer
                Dim outputTapCallCount As Integer = 0 ' Diagnostic counter
                _outputGainProcessor.SetMonitorOutputCallback(
                    Sub(buffer As DSP.AudioBuffer)
                        ' Write processed audio (after OUTPUT gain/pan) to monitor buffer (non-blocking)
                        If buffer IsNot Nothing AndAlso buffer.ByteCount > 0 Then
                            dspThread.postOutputGainMonitorBuffer.Write(buffer.Buffer, 0, buffer.ByteCount)

                            ' DIAGNOSTIC: Log first few calls
                            outputTapCallCount += 1
                            If outputTapCallCount <= 5 OrElse outputTapCallCount Mod 100 = 0 Then
                                Utils.Logger.Instance.Debug($"PostOutputGain tap callback #{outputTapCallCount}: {buffer.ByteCount} bytes written", "AudioRouter")
                            End If
                        End If
                    End Sub
                )
                Utils.Logger.Instance.Info("✅ OUTPUT GainProcessor tap point wired to PostOutputGainMonitor buffer", "AudioRouter")

                ' Create DSP output provider (bridges DSPThread to NAudio)
                dspOutputProvider = New DSPOutputProvider(dspThread)

                Utils.Logger.Instance.Info($"PROVIDER format: {dspOutputProvider.WaveFormat.SampleRate}Hz, {dspOutputProvider.WaveFormat.Channels}ch, {dspOutputProvider.WaveFormat.BitsPerSample}bit, Encoding={dspOutputProvider.WaveFormat.Encoding}, BlockAlign={dspOutputProvider.WaveFormat.BlockAlign}, AvgBytes/sec={dspOutputProvider.WaveFormat.AverageBytesPerSecond}", "AudioRouter")

                ' Create wave output with lower latency
                waveOut = New WaveOutEvent() With {
                    .DeviceNumber = outputDeviceManager.SelectedDeviceIndex,
                    .DesiredLatency = 100
                }

                Utils.Logger.Instance.Info($"WAVEOUT device: {outputDeviceManager.SelectedDeviceIndex}, DesiredLatency: 100ms", "AudioRouter")

                ' Wire up playback stopped event
                AddHandler waveOut.PlaybackStopped, AddressOf OnWaveOutStopped

                ' Initialize wave output with DSP provider
                waveOut.Init(dspOutputProvider)

                ' START DSP WORKER THREAD
                dspThread.Start()

                ' Pre-fill 1 second of audio data for smooth startup
                Dim prebufferSize = fileReader.WaveFormat.AverageBytesPerSecond * 1 ' 1 second in IEEE Float format
                Dim prebufferFloat(prebufferSize - 1) As Byte
                Dim bytesRead = fileReader.Read(prebufferFloat, 0, prebufferSize)

                If bytesRead > 0 Then
                    ' Convert pre-fill data to PCM16
                    Dim prebufferPCM16 = ConvertFloatToPCM16(prebufferFloat, bytesRead, fileReader.WaveFormat.Channels)

                    ' Write pre-fill to DSP input buffer
                    dspThread.WriteInput(prebufferPCM16, 0, prebufferPCM16.Length)

                    Utils.Logger.Instance.Info($"Pre-filled {prebufferPCM16.Length} bytes PCM16 (1 second)", "AudioRouter")
                End If

                ' Start feeding audio from file to DSP input
                StartFileFeeder()

                ' Brief delay to let DSP worker fill output buffer
                System.Threading.Thread.Sleep(100)

                ' Calculate duration for position tracking (TransportControl)
                _playbackDuration = fileReader.TotalTime
                _playbackStartTime = DateTime.Now

                ' PHASE 6 FIX: Set IsPlaying flag BEFORE starting playback
                _isPlaying = True

                ' Start playback
                waveOut.Play()

                Utils.Logger.Instance.Info("DSP playback started successfully", "AudioRouter")
                Utils.Logger.Instance.Info($"IsPlaying flag set: {_isPlaying}", "AudioRouter")

                ' Raise PlaybackStarted event (for TransportControl integration)
                RaiseEvent PlaybackStarted(Me, IO.Path.GetFileName(_selectedInputFile))
                Utils.Logger.Instance.Info($"PlaybackStarted event raised: {IO.Path.GetFileName(_selectedInputFile)}, Duration={_playbackDuration}", "AudioRouter")

            Catch ex As Exception
                Utils.Logger.Instance.Error("Failed to start DSP playback", ex, "AudioRouter")
                StopDSPPlayback() ' Cleanup on error
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Background thread that feeds audio from file to DSP input buffer (EVENT-DRIVEN)
        ''' Waits for signal from DSP when input buffer is low (<25%)
        ''' </summary>
        Private Sub StartFileFeeder()
            feederCancellation = False ' Reset cancellation flag

            ' Capture format and signal event for use in thread
            Dim inputFormat = fileReader.WaveFormat
            Dim pcm16BlockAlign = inputFormat.Channels * 2 ' 2 bytes per PCM16 sample
            Dim inputLowSignal = dspThread.InputLowSignal

            ' CRITICAL: Capture local reference to dspThread to prevent race condition
            ' If StopDSPPlayback() sets dspThread=Nothing while feeder is running, we'd crash!
            Dim localDspThread = dspThread
            Dim localWaveOut = waveOut

            feederThread = New System.Threading.Thread(
                Sub()
                    Try
                        ' Industry standard block size: 256 samples
                        Const BLOCK_SIZE_SAMPLES As Integer = 256

                        Dim floatBlockSize = BLOCK_SIZE_SAMPLES * inputFormat.Channels * 4 ' 4 bytes per float
                        Dim pcm16BlockSize = BLOCK_SIZE_SAMPLES * pcm16BlockAlign
                        Dim floatBuffer(floatBlockSize - 1) As Byte
                        Dim bytesRead As Integer
                        Dim blockCount As Integer = 0

                        ' Log startup ONCE
                        Utils.Logger.Instance.Info($"File feeder started (EVENT-DRIVEN): {BLOCK_SIZE_SAMPLES} samples per block ({floatBlockSize} bytes IEEE Float → {pcm16BlockSize} bytes PCM16)", "AudioRouter")

                        Dim reachedEOF As Boolean = False ' Track if we reached end naturally

                        While Not feederCancellation
                            ' WAIT for signal from DSP (input buffer <25%)
                            ' Use 1ms timeout for VERY fast response
                            If Not inputLowSignal.WaitOne(1) Then
                                Continue While ' Timeout - check cancellation and try again
                            End If

                            ' Check cancellation after waking
                            If feederCancellation Then Exit While

                            ' Feed multiple blocks to fill input buffer back to healthy level
                            ' Target: refill to at least 50% capacity
                            Dim inputCapacity = localDspThread.InputAvailable()
                            Dim targetFill = 176400 \ 2 ' 50% of 2-second buffer

                            While inputCapacity < targetFill AndAlso Not feederCancellation
                                ' Read from file
                                bytesRead = fileReader.Read(floatBuffer, 0, floatBlockSize)
                                If bytesRead = 0 Then
                                    Utils.Logger.Instance.Info("File feeder reached end of file", "AudioRouter")
                                    feederCancellation = True ' Stop the outer loop too
                                    reachedEOF = True ' Mark that we reached EOF naturally
                                    Exit While
                                End If

                                ' Convert to PCM16
                                Dim pcm16Buffer = ConvertFloatToPCM16(floatBuffer, bytesRead, inputFormat.Channels)

                                ' Write to input buffer
                                Dim bytesWritten = localDspThread.WriteInput(pcm16Buffer, 0, pcm16Buffer.Length)

                                If bytesWritten < pcm16Buffer.Length Then
                                    ' Buffer full - exit burst
                                    Exit While
                                End If

                                blockCount += 1
                                inputCapacity = localDspThread.InputAvailable()
                            End While
                        End While

                        ' Log final stats ONCE after stopping
                        Utils.Logger.Instance.Info($"File feeder stopped: {blockCount} blocks written", "AudioRouter")

                        ' Raise PlaybackStopped if we finished the file naturally (triggers OnWaveOutStopped)
                        If reachedEOF Then
                            Utils.Logger.Instance.Info("🎬 File feeder reached EOF - draining DSP buffers...", "AudioRouter")

                            ' PHASE 2.5 FIX: Wait for DSP buffers to drain!
                            ' DSP has ~200ms of audio still in buffers (input + output)
                            ' Don't stop immediately or we'll lose the end of the file
                            
                            ' Wait for DSP input buffer to empty
                            Dim maxWait As Integer = 50 ' Max 50 iterations (500ms at 10ms each)
                            Dim waitCount As Integer = 0
                            While localDspThread.InputAvailable() > 1024 AndAlso waitCount < maxWait
                                System.Threading.Thread.Sleep(10) ' 10ms
                                waitCount += 1
                            End While
                            Utils.Logger.Instance.Info($"DSP input drained after {waitCount * 10}ms", "AudioRouter")
                            
                            ' Brief delay for DSP worker to process remaining input
                            System.Threading.Thread.Sleep(50)
                            
                            Utils.Logger.Instance.Info("DSP buffers drained, stopping WaveOut...", "AudioRouter")

                            ' Stop waveOut which will trigger OnWaveOutStopped event
                            ' WaveOut will drain its own internal buffers naturally
                            If localWaveOut IsNot Nothing Then
                                localWaveOut.Stop()
                            End If
                        End If


                    Catch ex As System.Threading.ThreadAbortException
                        Utils.Logger.Instance.Info("File feeder thread aborted", "AudioRouter")
                    Catch ex As Exception
                        Utils.Logger.Instance.Error("File feeder error", ex, "AudioRouter")
                    End Try
                End Sub
            )

            feederThread.Name = "File Feeder Thread (Event-Driven)"
            feederThread.Priority = System.Threading.ThreadPriority.Highest ' HIGHEST priority for responsiveness
            feederThread.IsBackground = True
            feederThread.Start()
        End Sub

        ''' <summary>
        ''' Convert IEEE Float samples (AudioFileReader format) to 16-bit PCM
        ''' </summary>
        Private Function ConvertFloatToPCM16(floatBuffer As Byte(), byteCount As Integer, channels As Integer) As Byte()
            ' AudioFileReader provides IEEE Float (32-bit per sample)
            Dim sampleCount = byteCount \ 4 ' 4 bytes per float sample
            Dim pcm16Buffer(sampleCount * 2 - 1) As Byte ' 2 bytes per 16-bit sample

            ' Log first conversion for verification
            Static firstLog As Boolean = True
            If firstLog Then
                Utils.Logger.Instance.Info($"Float→PCM16: {byteCount} bytes float → {sampleCount} samples → {pcm16Buffer.Length} bytes PCM16", "AudioRouter")
                firstLog = False
            End If

            For i = 0 To sampleCount - 1
                ' Read float sample (-1.0 to +1.0)
                Dim floatSample = BitConverter.ToSingle(floatBuffer, i * 4)

                ' Clamp to valid range
                floatSample = Math.Max(-1.0F, Math.Min(1.0F, floatSample))

                ' Convert to 16-bit integer (-32768 to 32767)
                Dim int16Sample = CShort(floatSample * 32767.0F)

                ' Write as little-endian bytes
                pcm16Buffer(i * 2) = CByte(int16Sample And &HFF)
                pcm16Buffer(i * 2 + 1) = CByte((int16Sample >> 8) And &HFF)
            Next

            Return pcm16Buffer
        End Function

        ''' <summary>
        ''' Stop DSP playback
        ''' </summary>
        Public Sub StopDSPPlayback()
            Try
                Utils.Logger.Instance.Info("Stopping DSP playback", "AudioRouter")

                ' PHASE 6 FIX: Clear IsPlaying flag FIRST
                _isPlaying = False

                ' Signal feeder thread to stop FIRST
                feederCancellation = True

                ' Stop wave output (this stops pulling from DSP output)
                If waveOut IsNot Nothing Then
                    RemoveHandler waveOut.PlaybackStopped, AddressOf OnWaveOutStopped
                    waveOut.Stop()
                    waveOut.Dispose()
                    waveOut = Nothing
                End If

                ' Wait for feeder thread to exit gracefully (short timeout)
                If feederThread IsNot Nothing AndAlso feederThread.IsAlive Then
                    If Not feederThread.Join(200) Then ' Wait max 200ms
                        Utils.Logger.Instance.Warning("File feeder thread did not exit gracefully", "AudioRouter")
                    End If
                    feederThread = Nothing
                End If

                ' Stop DSP worker thread
                If dspThread IsNot Nothing Then
                    dspThread.Stop()
                    dspThread.Dispose()
                    dspThread = Nothing
                End If

                ' Dispose output provider
                dspOutputProvider = Nothing

                ' Dispose file reader (this will unblock any pending Read() calls)
                If fileReader IsNot Nothing Then
                    fileReader.Dispose()
                    fileReader = Nothing
                End If

                ' Reset position tracking (TransportControl)
                _playbackStartTime = DateTime.MinValue
                _playbackDuration = TimeSpan.Zero

                Utils.Logger.Instance.Info("DSP playback stopped successfully", "AudioRouter")

            Catch ex As Exception
                Utils.Logger.Instance.Error("Error stopping DSP playback", ex, "AudioRouter")
            End Try
        End Sub

        ''' <summary>
        ''' Update position tracking and raise PositionChanged event (call from timer)
        ''' Matches RecordingManager.UpdatePosition() pattern
        ''' </summary>
        Public Sub UpdatePosition()
            If IsPlaying Then
                RaiseEvent PositionChanged(Me, CurrentPosition)
            End If
        End Sub

#End Region

#Region "Private Methods"

        Private Sub OnWaveOutStopped(sender As Object, e As NAudio.Wave.StoppedEventArgs)
            Try
                Utils.Logger.Instance.Info("🎬 WaveOut playback stopped (file ended naturally)", "AudioRouter")
                Utils.Logger.Instance.Info($"   Exception in event: {If(e.Exception IsNot Nothing, e.Exception.Message, "None")}", "AudioRouter")
                
                ' PHASE 6 FIX: Clear IsPlaying flag on EOF
                _isPlaying = False
                
                ' IMPORTANT: Raise PlaybackStopped event BEFORE calling StopDSPPlayback()
                ' This allows MainForm to stop the timer before we clean up
                Utils.Logger.Instance.Info("About to raise PlaybackStopped event...", "AudioRouter")
                RaiseEvent PlaybackStopped(Me, EventArgs.Empty)
                Utils.Logger.Instance.Info("✅ PlaybackStopped event raised successfully!", "AudioRouter")
                
                ' Now clean up resources
                Utils.Logger.Instance.Info("Calling StopDSPPlayback() for cleanup...", "AudioRouter")
                StopDSPPlayback()
                Utils.Logger.Instance.Info("✅ Cleanup complete!", "AudioRouter")
            Catch ex As Exception
                Utils.Logger.Instance.Error("❌ ERROR in OnWaveOutStopped!", ex, "AudioRouter")
            End Try
        End Sub

        ''' <summary>
        ''' Raise input samples event for FFT analysis (non-blocking)
        ''' </summary>
        Private Sub RaiseInputSamplesEvent(buffer As Byte(), count As Integer, bitsPerSample As Integer)
            ' NOT USED - We pull FFT from DSP output buffer instead
            ' This method kept for future reference if we need pre-DSP monitoring
        End Sub

        ''' <summary>
        ''' Get output samples from DSP for FFT analysis (non-blocking)
        ''' Reads from MONITOR buffer (parallel to output) - NO COMPETITION with WaveOut!
        ''' </summary>
        Public Sub UpdateOutputSamples()
            Try
                If dspThread IsNot Nothing AndAlso fileReader IsNot Nothing Then
                    ' Read from OUTPUT MONITOR buffer (after DSP processing)
                    Dim available = dspThread.OutputMonitorAvailable()

                    ' DIAGNOSTIC: Log every 30 calls (~500ms at 60Hz)
                    Static callCount As Integer = 0
                    callCount += 1
                    If callCount Mod 30 = 0 AndAlso available > 0 Then
                        Utils.Logger.Instance.Info($"UpdateOutputSamples: {available} bytes available in output monitor", "AudioRouter")
                    End If

                    If available > 0 Then
                        ' Read up to 8KB for FFT
                        Dim bufferSize = Math.Min(available, 8192)
                        Dim buffer(bufferSize - 1) As Byte
                        Dim bytesRead = dspThread.ReadOutputMonitor(buffer, 0, buffer.Length)

                        If bytesRead > 0 Then
                            ' DIAGNOSTIC: Check sample amplitude RIGHT AFTER reading from monitor buffer
                            Static lastDiagTime As DateTime = DateTime.MinValue
                            If (DateTime.Now - lastDiagTime).TotalSeconds >= 1.0 Then
                                Dim maxSample As Single = 0.0F
                                For i = 0 To bytesRead - 1 Step 2
                                    If i + 1 < bytesRead Then
                                        Dim sample = Math.Abs(BitConverter.ToInt16(buffer, i))
                                        If sample > maxSample Then maxSample = sample
                                    End If
                                Next
                                Dim maxSampleDB = 20.0F * Math.Log10(Math.Max(maxSample / 32768.0F, 0.00001F))
                                Utils.Logger.Instance.Info($"MONITOR BUFFER CHECK: Peak={maxSampleDB:F1} dBFS from {bytesRead} bytes BEFORE FFT", "AudioRouter")
                                lastDiagTime = DateTime.Now
                            End If

                            ' Clone buffer for async processing
                            Dim clonedBuffer(bytesRead - 1) As Byte
                            Array.Copy(buffer, clonedBuffer, bytesRead)

                            Dim args As New AudioSamplesEventArgs With {
                                .Samples = clonedBuffer,
                                .Count = bytesRead,
                                .SampleRate = fileReader.WaveFormat.SampleRate,
                                .Channels = fileReader.WaveFormat.Channels,
                                .BitsPerSample = 16
                            }

                            ' Raise event asynchronously
                            System.Threading.ThreadPool.QueueUserWorkItem(
                                Sub(state)
                                    Try
                                        RaiseEvent OutputSamplesAvailable(Me, args)
                                    Catch ex As Exception
                                        ' Ignore FFT errors
                                    End Try
                                End Sub
                            )
                        End If
                    End If
                End If
            Catch ex As Exception
                ' Don't let FFT errors crash
                Utils.Logger.Instance.Error($"UpdateOutputSamples error: {ex.Message}", ex, "AudioRouter")
            End Try
        End Sub

        ''' <summary>
        ''' Get INPUT samples from DSP for FFT analysis (non-blocking)
        ''' Reads from INPUT MONITOR buffer - shows RAW audio BEFORE processing
        ''' </summary>
        Public Sub UpdateInputSamples()
            Try
                If dspThread IsNot Nothing AndAlso fileReader IsNot Nothing Then
                    ' Read from INPUT MONITOR buffer (before DSP processing)
                    Dim available = dspThread.InputMonitorAvailable()

                    ' DIAGNOSTIC: Log every 30 calls (~500ms at 60Hz)
                    Static callCount As Integer = 0
                    callCount += 1
                    If callCount Mod 30 = 0 AndAlso available > 0 Then
                        Utils.Logger.Instance.Info($"UpdateInputSamples: {available} bytes available in input monitor", "AudioRouter")
                    End If

                    If available > 0 Then
                        ' Read up to 8KB for FFT
                        Dim bufferSize = Math.Min(available, 8192)
                        Dim buffer(bufferSize - 1) As Byte
                        Dim bytesRead = dspThread.ReadInputMonitor(buffer, 0, buffer.Length)

                        If bytesRead > 0 Then
                            ' Clone buffer for async processing
                            Dim clonedBuffer(bytesRead - 1) As Byte
                            Array.Copy(buffer, clonedBuffer, bytesRead)

                            Dim args As New AudioSamplesEventArgs With {
                                .Samples = clonedBuffer,
                                .Count = bytesRead,
                                .SampleRate = fileReader.WaveFormat.SampleRate,
                                .Channels = fileReader.WaveFormat.Channels,
                                .BitsPerSample = 16
                            }

                            ' Raise event asynchronously
                            System.Threading.ThreadPool.QueueUserWorkItem(
                                Sub(state)
                                    Try
                                        RaiseEvent InputSamplesAvailable(Me, args)
                                    Catch ex As Exception
                                        ' Ignore FFT errors
                                    End Try
                                End Sub
                            )
                        End If
                    End If
                End If
            Catch ex As Exception
                ' Don't let FFT errors crash
                Utils.Logger.Instance.Error($"UpdateInputSamples error: {ex.Message}", ex, "AudioRouter")
            End Try
        End Sub

#End Region

#Region "IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            If Not disposed Then
                Utils.Logger.Instance.Info("Disposing AudioRouter", "AudioRouter")
                StopDSPPlayback()
                disposed = True
            End If
        End Sub

#End Region

    End Class

    ''' <summary>Event args for audio samples (FFT analysis)</summary>
    Public Class AudioSamplesEventArgs
        Inherits EventArgs

        Public Property Samples As Byte()
        Public Property Count As Integer
        Public Property SampleRate As Integer
        Public Property Channels As Integer
        Public Property BitsPerSample As Integer
    End Class

End Namespace
