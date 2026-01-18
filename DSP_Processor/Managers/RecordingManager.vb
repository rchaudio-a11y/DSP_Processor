Imports System.Threading
Imports DSP_Processor.AudioIO
Imports DSP_Processor.Recording
Imports DSP_Processor.Models
Imports DSP_Processor.Utils

Namespace Managers

    ''' <summary>
    ''' Manages recording lifecycle - microphone arming, recording start/stop, buffer processing
    ''' </summary>
    ''' <remarks>
    ''' Phase 0: MainForm Refactoring
    ''' Extracts all recording logic from MainForm into dedicated manager
    ''' </remarks>
    Public Class RecordingManager
        Implements IDisposable

#Region "Events"

        ''' <summary>Raised when recording starts</summary>
        Public Event RecordingStarted As EventHandler

        ''' <summary>Raised when recording stops</summary>
        Public Event RecordingStopped As EventHandler(Of RecordingStoppedEventArgs)

        ''' <summary>Raised when recording time updates</summary>
        Public Event RecordingTimeUpdated As EventHandler(Of TimeSpan)

        ''' <summary>Raised when audio buffer is available for metering/FFT</summary>
        Public Event BufferAvailable As EventHandler(Of AudioBufferEventArgs)

        ''' <summary>Raised when microphone is armed/disarmed</summary>
        Public Event MicrophoneArmed As EventHandler(Of Boolean) ' True = armed, False = disarmed

#End Region

#Region "Fields"

        Private mic As IInputSource ' Can be MicInputSource or WasapiEngine
        Private recorder As RecordingEngine
        ' REMOVED: processingTimer - replaced with callback-driven recording
        ' PHASE 5 STEP 21: Removed _isArmed and _isRecording (stateless manager pattern)
        ' State is now queried from subsystems (mic IsNot Nothing = armed, recorder.IsRecording = recording)

        ' PHASE 2: DSP Pipeline Integration
        Private dspThread As DSP.DSPThread ' Routes mic audio through DSP for meters/effects
        Private _inputGainProcessor As DSP.GainProcessor ' Input gain stage (Phase 2.5)
        Private _outputGainProcessor As DSP.GainProcessor ' Output gain stage (Phase 2.5)
        Private tapPointManager As DSP.TapPointManager ' Phase 2.7: Centralized tap point access
        Private useDSP As Boolean = True ' Enable DSP processing (can disable for testing)

        ' Settings
        Private audioSettings As AudioDeviceSettings
        Private recordingOptions As RecordingOptions

        ' DIAGNOSTIC: Performance tracking for recorder.Process() timing (kept for callback monitoring)
        ' Thread-safe counters using Interlocked operations
        Private ReadOnly _processingStopwatch As New Diagnostics.Stopwatch()
        Private _totalProcessingTimeMs As Long = 0 ' Changed to Long for Interlocked.Add
        Private _processCallCount As Long = 0
        Private _slowCallCount As Long = 0  ' Calls > 1ms
        Private _verySlowCallCount As Long = 0  ' Calls > 5ms
        Private _lastStatsLogTime As DateTime = DateTime.MinValue

#End Region

#Region "Properties"

        ''' <summary>Is microphone armed and ready?</summary>
        ''' <remarks>PHASE 5 STEP 21: Stateless query - checks if mic exists and DSP is initialized</remarks>
        Public ReadOnly Property IsArmed As Boolean
            Get
                ' Mic is armed if mic exists and DSP thread exists
                Return mic IsNot Nothing AndAlso dspThread IsNot Nothing
            End Get
        End Property

        ''' <summary>Is currently recording?</summary>
        ''' <remarks>PHASE 5 STEP 21: Stateless query - delegates to RecordingEngine (single source of truth)</remarks>
        Public ReadOnly Property IsRecording As Boolean
            Get
                ' Recording state is queried from RecordingEngine (single source of truth)
                Return recorder IsNot Nothing AndAlso recorder.IsRecording
            End Get
        End Property

        ''' <summary>Current recording duration</summary>
        Public ReadOnly Property RecordingDuration As TimeSpan
            Get
                If recorder IsNot Nothing Then
                    Return recorder.RecordingDuration
                End If
                Return TimeSpan.Zero
            End Get
        End Property

        ''' <summary>Recording options (mode, loop count, etc.)</summary>
        Public Property Options As RecordingOptions
            Get
                Return recordingOptions
            End Get
            Set(value As RecordingOptions)
                recordingOptions = value
                If recorder IsNot Nothing Then
                    recorder.Options = value
                End If
            End Set
        End Property

        ''' <summary>Input volume (0.0 to 2.0, where 1.0 is 100%)</summary>
        Public Property InputVolume As Single
            Get
                If mic IsNot Nothing Then
                    ' Both MicInputSource and WasapiEngine have Volume property
                    If TypeOf mic Is MicInputSource Then
                        Return DirectCast(mic, MicInputSource).Volume
                    ElseIf TypeOf mic Is WasapiEngine Then
                        Return DirectCast(mic, WasapiEngine).Volume
                    End If
                End If
                Return 1.0F
            End Get
            Set(value As Single)
                If mic IsNot Nothing Then
                    If TypeOf mic Is MicInputSource Then
                        DirectCast(mic, MicInputSource).Volume = value
                    ElseIf TypeOf mic Is WasapiEngine Then
                        DirectCast(mic, WasapiEngine).Volume = value
                    End If
                End If
            End Set
        End Property

        ''' <summary>Gets the INPUT GainProcessor instance for UI control (Phase 2.5)</summary>
        Public ReadOnly Property InputGainProcessor As DSP.GainProcessor
            Get
                Return _inputGainProcessor
            End Get
        End Property

        ''' <summary>Gets the OUTPUT GainProcessor instance for UI control (Phase 2.5)</summary>
        Public ReadOnly Property OutputGainProcessor As DSP.GainProcessor
            Get
                Return _outputGainProcessor
            End Get
        End Property

        ''' <summary>Gets the TapPointManager for reading processed audio from tap points (Phase 2.7)</summary>
        Public ReadOnly Property TapManager As DSP.TapPointManager
            Get
                Return tapPointManager
            End Get
        End Property

        ''' <summary>DEPRECATED: Use InputGainProcessor instead (kept for compatibility)</summary>
        Public ReadOnly Property GainProcessor As DSP.GainProcessor
            Get
                Return _inputGainProcessor
            End Get
        End Property

        ''' <summary>Gets post-gain samples for meter display (Phase 2: DSP Tap Point Pattern)</summary>
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
                Throw New InvalidOperationException("DSP thread not initialized! Arm microphone first.")
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

#Region "Constructor"

        Public Sub New()
            Logger.Instance.Info("RecordingManager initialized", "RecordingManager")
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>Initialize with settings</summary>
        Public Sub Initialize(settings As AudioDeviceSettings, options As RecordingOptions)
            audioSettings = settings
            recordingOptions = options

            ' Create recording engine
            recorder = New RecordingEngine() With {
                .OutputFolder = "Recordings",
                .AutoNamePattern = "Take_{0:yyyyMMdd}-{1:000}.wav",
                .Options = recordingOptions
            }

            Logger.Instance.Info("RecordingManager initialized with settings", "RecordingManager")
        End Sub

        ''' <summary>Arm microphone for recording (starts capture but doesn't write to file)</summary>
        Public Sub ArmMicrophone()
            Try
                ' PHASE 5 STEP 21: Check mic existence instead of _isArmed flag
                If mic IsNot Nothing AndAlso dspThread IsNot Nothing Then
                    Logger.Instance.Warning("Microphone already armed", "RecordingManager")
                    Return
                End If

                ' Ensure initialized
                If audioSettings Is Nothing Then
                    Dim ex = New InvalidOperationException("RecordingManager must be initialized before arming microphone. Call Initialize() first.")
                    Logger.Instance.Error("Cannot arm microphone - RecordingManager not initialized", ex, "RecordingManager")
                    Throw ex
                End If

                Logger.Instance.Info("Arming audio input...", "RecordingManager")

                ' DIAGNOSTIC: Log settings
                Logger.Instance.Info($"Driver: {audioSettings.DriverType}", "RecordingManager")
                Logger.Instance.Info($"Buffer size: {audioSettings.BufferMilliseconds}ms", "RecordingManager")
                Logger.Instance.Info($"Sample rate: {audioSettings.SampleRate}Hz, Channels: {audioSettings.Channels}, Bits: {audioSettings.BitDepth}", "RecordingManager")
                Logger.Instance.Info($"Device index: {audioSettings.InputDeviceIndex}", "RecordingManager")

                ' Get device info for selected driver and device index
                Dim devices = AudioIO.AudioInputManager.Instance.GetDevices(audioSettings.DriverType)

                If devices.Count = 0 Then
                    Throw New InvalidOperationException($"No {audioSettings.DriverType} devices available")
                End If

                If audioSettings.InputDeviceIndex < 0 Or audioSettings.InputDeviceIndex >= devices.Count Then
                    Throw New ArgumentOutOfRangeException($"Device index {audioSettings.InputDeviceIndex} is out of range. Only {devices.Count} devices available.")
                End If

                Dim selectedDevice = devices(audioSettings.InputDeviceIndex)
                Logger.Instance.Info($"Selected device: {selectedDevice.Name}", "RecordingManager")

                ' Check driver type and create appropriate engine
                Select Case audioSettings.DriverType
                    Case AudioIO.DriverType.WaveIn
                        ' WaveIn: Use MicInputSource (legacy, will be replaced with WaveInEngine)
                        Logger.Instance.Info("Using WaveIn (MicInputSource) for audio capture", "RecordingManager")
                        mic = New MicInputSource(
                            audioSettings.SampleRate,
                            If(audioSettings.Channels = 1, "Mono (1)", "Stereo (2)"),
                            audioSettings.BitDepth,
                            audioSettings.InputDeviceIndex,
                            audioSettings.BufferMilliseconds)

                    Case AudioIO.DriverType.WASAPI
                        ' WASAPI: Use WasapiEngine (low-latency, professional audio)
                        Logger.Instance.Info("Using WASAPI for audio capture", "RecordingManager")

                        Dim wasapiEngine = New WasapiEngine(
                            selectedDevice,
                            audioSettings.SampleRate,
                            audioSettings.Channels,
                            audioSettings.BitDepth,
                            audioSettings.BufferMilliseconds)

                        ' Start WASAPI capture
                        wasapiEngine.Start()

                        mic = wasapiEngine

                        ' Log the actual format WASAPI is using (may differ from requested)
                        Logger.Instance.Info($"WASAPI engine created: {selectedDevice.Name}, requested: {audioSettings.SampleRate}Hz, actual: {wasapiEngine.SampleRate}Hz/{wasapiEngine.BitsPerSample}bit", "RecordingManager")

                    Case Else
                        Throw New NotSupportedException($"Driver type {audioSettings.DriverType} is not supported")
                End Select

                ' Apply volume
                If TypeOf mic Is MicInputSource Then
                    DirectCast(mic, MicInputSource).Volume = 1.0F
                ElseIf TypeOf mic Is WasapiEngine Then
                    DirectCast(mic, WasapiEngine).Volume = 1.0F
                End If

                ' CALLBACK-DRIVEN RECORDING: Subscribe to real-time audio callback
                ' This eliminates timer jitter and buffer queue overflows
                AddHandler mic.AudioDataAvailable, AddressOf OnAudioDataAvailable
                Logger.Instance.Info("Subscribed to AudioDataAvailable callback (glitch-free recording mode)", "RecordingManager")

                ' PHASE 2: Create DSPThread for mic audio (unified pipeline!)
                If useDSP Then
                    Logger.Instance.Info("🎵 Phase 2.5: Initializing DSP pipeline for microphone...", "RecordingManager")

                    ' Create PCM16 format for DSP
                    Dim pcm16Format As New NAudio.Wave.WaveFormat(mic.SampleRate, 16, mic.Channels)

                    ' Create DSP thread with 2-second buffers (same as file playback)
                    Dim bufferSize = pcm16Format.AverageBytesPerSecond * 2 ' 2 seconds
                    dspThread = New DSP.DSPThread(pcm16Format, bufferSize, bufferSize)

                    ' PHASE 2.5: Add INPUT gain processor (first in chain)
                    _inputGainProcessor = New DSP.GainProcessor(pcm16Format) With {
                        .GainDB = 0.0F ' Unity gain
                    }
                    dspThread.Chain.AddProcessor(_inputGainProcessor)
                    Logger.Instance.Info("Added INPUT Gain processor to chain (0 dB)", "RecordingManager")

                    ' Wire INPUT gain tap point for meters (DSP TAP POINT PATTERN)
                    _inputGainProcessor.SetMonitorOutputCallback(
                        Sub(buffer As DSP.AudioBuffer)
                            If buffer IsNot Nothing AndAlso buffer.ByteCount > 0 Then
                                dspThread.postGainMonitorBuffer.Write(buffer.Buffer, 0, buffer.ByteCount)
                            End If
                        End Sub
                    )
                    Logger.Instance.Info("✅ INPUT GainProcessor tap point wired to PostGainMonitor buffer", "RecordingManager")

                    ' PHASE 2.5: Add OUTPUT gain processor (last in chain)
                    _outputGainProcessor = New DSP.GainProcessor(pcm16Format) With {
                        .GainDB = 0.0F ' Unity gain
                    }
                    dspThread.Chain.AddProcessor(_outputGainProcessor)
                    Logger.Instance.Info("Added OUTPUT Gain processor to chain (0 dB)", "RecordingManager")

                    ' Wire OUTPUT gain tap point for meters
                    _outputGainProcessor.SetMonitorOutputCallback(
                        Sub(buffer As DSP.AudioBuffer)
                            If buffer IsNot Nothing AndAlso buffer.ByteCount > 0 Then
                                dspThread.postOutputGainMonitorBuffer.Write(buffer.Buffer, 0, buffer.ByteCount)
                            End If
                        End Sub
                    )
                    Logger.Instance.Info("✅ OUTPUT GainProcessor tap point wired to PostOutputGainMonitor buffer", "RecordingManager")

                    ' Start DSP worker thread
                    dspThread.Start()

                    ' PHASE 2.7: Create TapPointManager for centralized tap access
                    tapPointManager = New DSP.TapPointManager(dspThread)
                    Logger.Instance.Info("✅ TapPointManager created (centralized tap point access)", "RecordingManager")

                    Logger.Instance.Info($"✅ DSP pipeline active: {pcm16Format.SampleRate}Hz, {pcm16Format.Channels}ch, Input+Output gain stages!", "RecordingManager")
                End If

                ' PHASE 5 STEP 21: No longer setting _isArmed flag (stateless)
                RaiseEvent MicrophoneArmed(Me, True)

                Logger.Instance.Info("Audio input armed successfully", "RecordingManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to arm microphone", ex, "RecordingManager")
                Throw
            End Try
        End Sub

        ''' <summary>Disarm microphone (stop capture)</summary>
        Public Sub DisarmMicrophone()
            Try
                ' PHASE 5 STEP 21: Check mic existence instead of _isArmed flag
                If mic Is Nothing Then Return

                Logger.Instance.Info("Disarming microphone...", "RecordingManager")

                ' Unsubscribe from callback FIRST to prevent race conditions
                If mic IsNot Nothing Then
                    RemoveHandler mic.AudioDataAvailable, AddressOf OnAudioDataAvailable
                    Logger.Instance.Info("Unsubscribed from AudioDataAvailable callback", "RecordingManager")
                End If

                ' Give time for any pending callbacks to complete
                System.Threading.Thread.Sleep(50)

                ' Dispose mic and wait for cleanup
                If mic IsNot Nothing Then
                    If TypeOf mic Is IDisposable Then
                        DirectCast(mic, IDisposable).Dispose()
                    End If
                    mic = Nothing
                End If

                ' PHASE 2.5: Clean up DSP thread and processors
                If dspThread IsNot Nothing Then
                    Logger.Instance.Info("Stopping DSP thread...", "RecordingManager")

                    ' PHASE 2.7: Dispose TapPointManager first
                    If tapPointManager IsNot Nothing Then
                        tapPointManager.Dispose()
                        tapPointManager = Nothing
                        Logger.Instance.Info("TapPointManager disposed", "RecordingManager")
                    End If

                    dspThread.Stop()
                    dspThread.Dispose()
                    dspThread = Nothing
                    _inputGainProcessor = Nothing
                    _outputGainProcessor = Nothing
                    Logger.Instance.Info("DSP thread stopped", "RecordingManager")
                End If

                ' Another small delay to ensure disposal is complete
                System.Threading.Thread.Sleep(50)

                ' PHASE 5 STEP 21: No longer setting _isArmed flag (stateless)
                RaiseEvent MicrophoneArmed(Me, False)

                Logger.Instance.Info("Microphone disarmed", "RecordingManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to disarm microphone", ex, "RecordingManager")
            End Try
        End Sub

        ''' <summary>Start recording to file (stateless - does not trigger state transitions)</summary>
        ''' <remarks>
        ''' STEP 22.5 FIX: This method NO LONGER calls GlobalStateMachine.TransitionTo().
        ''' State transitions should be triggered by RecordingManagerSSM, not RecordingManager.
        ''' This prevents circular dependencies and re-entry deadlocks.
        ''' </remarks>
        Public Sub StartRecording()
            Try
                ' PHASE 5 STEP 21: Check recorder.IsRecording instead of _isRecording flag
                If recorder IsNot Nothing AndAlso recorder.IsRecording Then
                    Logger.Instance.Warning("Already recording", "RecordingManager")
                    Return
                End If

                ' Ensure mic is armed (stateless check)
                If mic Is Nothing Then
                    Logger.Instance.Info("Microphone not armed, arming now...", "RecordingManager")
                    ArmMicrophone()
                    System.Threading.Thread.Sleep(500) ' Give time to warm up
                End If

                Logger.Instance.Info("🔴 Starting recording engine...", "RecordingManager")

                ' DON'T clear buffers - we WANT the pre-filled audio from mic arming!
                ' This is the whole point of arming the mic early - to avoid losing the first second
                ' mic?.ClearBuffers() ' REMOVED - was causing first second loss!

                ' DIAGNOSTIC: Log buffer queue size
                If mic IsNot Nothing Then
                    Logger.Instance.Info($"Starting recording with pre-filled mic buffer", "RecordingManager")
                End If

                ' Set recorder input
                recorder.InputSource = mic

                ' Start recording based on mode
                Select Case recordingOptions.Mode
                    Case RecordingMode.LoopMode
                        recorder.StartLoopRecording()
                        Logger.Instance.Info($"Loop recording started: {recordingOptions.LoopCount} takes", "RecordingManager")

                    Case Else
                        recorder.StartRecording()
                        Logger.Instance.Info("Recording started", "RecordingManager")
                End Select

                ' PHASE 5 STEP 21: No longer setting _isRecording flag (stateless)
                ' RecordingEngine.IsRecording is now the single source of truth
                RaiseEvent RecordingStarted(Me, EventArgs.Empty)

            Catch ex As Exception
                Logger.Instance.Error("Failed to start recording", ex, "RecordingManager")
                ' PHASE 5 STEP 21: No longer setting _isRecording flag (stateless)
                Throw
            End Try
        End Sub

        ''' <summary>Stop recording with optional completion callback (for state machine finalization)</summary>
        ''' <param name="onComplete">Optional callback executed AFTER recording stops and file is finalized</param>
        ''' <remarks>
        ''' STEP 22.5 FIX (Callback Pattern): This method NO LONGER calls GlobalStateMachine.TransitionTo().
        ''' Instead, caller provides a callback that executes AFTER engine stops and WAV file finalizes.
        ''' This prevents:
        ''' - Re-entry deadlocks (callback runs AFTER method returns)
        ''' - Corrupted WAV files (finalization completes BEFORE transition)
        ''' - Circular dependencies (no calls back to state machine)
        ''' </remarks>
        Public Sub StopRecording(Optional onComplete As Action = Nothing)
            Try
                ' PHASE 5 STEP 21: Check recorder.IsRecording instead of _isRecording flag
                If recorder Is Nothing OrElse Not recorder.IsRecording Then
                    ' Not recording - still execute callback if provided
                    onComplete?.Invoke()
                    Return
                End If

                Logger.Instance.Info("⏹️ Stopping recording engine...", "RecordingManager")

                ' Get duration before stopping
                Dim duration = If(recorder IsNot Nothing, recorder.RecordingDuration, TimeSpan.Zero)
                Dim filePath = "" ' RecordingEngine doesn't expose last file path

                ' Check if in loop mode
                If recordingOptions.Mode = RecordingMode.LoopMode Then
                    recorder.CancelLoopRecording()
                Else
                    recorder.StopRecording()  ' BLOCKS until finalization complete
                End If

                Logger.Instance.Info("⏹️ Recording engine stopped successfully", "RecordingManager")

                ' PHASE 5 STEP 21: No longer setting _isRecording flag (stateless)

                Dim args As New RecordingStoppedEventArgs With {
                    .Duration = duration,
                    .FilePath = filePath
                }
                
                ' Execute callback AFTER finalization (Step 22.5 Callback Pattern)
                ' This ensures WAV file is complete BEFORE state transitions
                onComplete?.Invoke()

                RaiseEvent RecordingStopped(Me, args)

                Logger.Instance.Info($"Recording stopped: {args.Duration.TotalSeconds:F1}s", "RecordingManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to stop recording", ex, "RecordingManager")
                ' PHASE 5 STEP 21: No longer setting _isRecording flag (stateless)
            End Try
        End Sub

        ''' <summary>
        ''' Apply recording options (moved from MainForm)
        ''' </summary>
        Public Sub ApplyRecordingOptions(options As RecordingOptions)
            Try
                Logger.Instance.Info($"Applying recording options: {options.Mode} mode", "RecordingManager")

                ' Update internal options
                recordingOptions = options

                ' Apply to recorder if it exists
                If recorder IsNot Nothing Then
                    recorder.Options = options
                End If

                Logger.Instance.Info("Recording options applied successfully", "RecordingManager")
            Catch ex As Exception
                Logger.Instance.Error("Failed to apply recording options", ex, "RecordingManager")
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Apply audio settings (moved from MainForm)
        ''' Re-arms microphone with new settings if currently armed
        ''' </summary>
        Public Sub ApplyAudioSettings(settings As AudioDeviceSettings)
            Try
                Logger.Instance.Info($"Applying audio settings: {settings.DriverType}, {settings.SampleRate}Hz", "RecordingManager")

                ' Update internal settings
                audioSettings = settings

                ' If mic is armed, re-arm with new settings
                If IsArmed Then
                    Logger.Instance.Info("Microphone is armed, re-arming with new settings...", "RecordingManager")
                    DisarmMicrophone()
                    System.Threading.Thread.Sleep(100) ' Wait for cleanup
                    ArmMicrophone()
                Else
                    Logger.Instance.Info("Microphone not armed, settings will be applied on next arm", "RecordingManager")
                End If

                Logger.Instance.Info("Audio settings applied successfully", "RecordingManager")
            Catch ex As Exception
                Logger.Instance.Error("Failed to apply audio settings", ex, "RecordingManager")
                Throw
            End Try
        End Sub

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' REAL-TIME AUDIO CALLBACK: Called by driver when audio data arrives
        ''' This is synchronous with audio delivery - NO TIMER JITTER!
        ''' </summary>
        Private Sub OnAudioDataAvailable(sender As Object, e As AudioIO.AudioCallbackEventArgs)
            Try
                ' Static variable for time updates (shared across both paths)
                Static lastTimeUpdate As DateTime = DateTime.MinValue
                
                ' PHASE 2.5: Route through DSP for PROCESSING + METERS (ASYNC!)
                If useDSP AndAlso dspThread IsNot Nothing Then
                    ' Write raw audio to DSP input (non-blocking)
                    dspThread.WriteInput(e.Buffer, 0, e.BytesRecorded)
                    
                    ' CRITICAL: Must drain output buffer to trigger DSP worker processing!
                    ' DSPThread is PULL-based - worker only processes when output is drained.
                    ' This is what triggers tap callbacks for meters (freewheeling monitoring).
                    ' We don't use this output for anything - just triggering processing.
                    Dim drainBuffer(e.BytesRecorded - 1) As Byte
                    Dim bytesRead = dspThread.ReadOutput(drainBuffer, 0, drainBuffer.Length)
                    
                    ' For recording, use the drained processed audio
                    If recorder IsNot Nothing AndAlso recorder.IsRecording AndAlso bytesRead > 0 Then
                        recorder.ProcessBuffer(drainBuffer, bytesRead)
                        
                        ' Raise time update periodically
                        If (DateTime.Now - lastTimeUpdate).TotalMilliseconds >= 100 Then
                            RaiseEvent RecordingTimeUpdated(Me, recorder.RecordingDuration)
                            lastTimeUpdate = DateTime.Now
                        End If
                    End If
                Else
                    ' DSP disabled - use raw buffer for recording
                    If recorder IsNot Nothing AndAlso recorder.IsRecording Then
                        recorder.ProcessBuffer(e.Buffer, e.BytesRecorded)
                        
                        If (DateTime.Now - lastTimeUpdate).TotalMilliseconds >= 100 Then
                            RaiseEvent RecordingTimeUpdated(Me, recorder.RecordingDuration)
                            lastTimeUpdate = DateTime.Now
                        End If
                    End If
                End If
                
                ' Always raise BufferAvailable for FFT/metering (use raw buffer)
                Dim args As New AudioBufferEventArgs With {
                    .Buffer = e.Buffer,
                    .BitsPerSample = mic.BitsPerSample,
                    .Channels = mic.Channels,
                    .SampleRate = mic.SampleRate
                }
                RaiseEvent BufferAvailable(Me, args)
                
                ' PHASE 6 FIX: Check if loop recording has completed all takes
                ' CRITICAL: Only trigger transition if we're STILL in Recording state!
                ' This prevents infinite loop after state has already transitioned.
                If recorder IsNot Nothing AndAlso recorder.IsLoopRecordingComplete Then
                    If StateCoordinator.Instance.GlobalStateMachine.CurrentState = GlobalState.Recording Then
                        Logger.Instance.Info("LOOP RECORDING COMPLETE - Triggering state transition", "RecordingManager")
                        Services.LoggingServiceAdapter.Instance.LogInfo("Loop recording complete - transitioning to Idle")
                        
                        ' Trigger state machine transition to Stopping → Idle
                        ' This will update UI, refresh file list, etc.
                        StateCoordinator.Instance.GlobalStateMachine.TransitionTo(
                            GlobalState.Stopping,
                            "Loop recording completed all takes")
                    End If
                End If
                
            Catch ex As Exception
                Logger.Instance.Error("Error in audio callback", ex, "RecordingManager")
                ' Don't crash the audio thread!
            End Try
        End Sub

''' <summary>
''' DEPRECATED: Timer-driven processing (replaced by OnAudioDataAvailable callback)
''' Kept for reference but no longer used
''' </summary>
Private Sub ProcessingTimer_Tick(state As Object)
            Try
                ' Track if we were recording before Process() call
                Dim wasRecording = recorder IsNot Nothing AndAlso recorder.IsRecording

                ' If recording, let recorder handle everything
                If recorder IsNot Nothing AndAlso recorder.InputSource IsNot Nothing Then
                    ' AGGRESSIVE DRAIN: Check queue depth and drain aggressively if needed
                    Dim currentQueueDepth As Integer = 0
                    
                    If TypeOf mic Is MicInputSource Then
                        currentQueueDepth = DirectCast(mic, MicInputSource).BufferQueueCount
                    ElseIf TypeOf mic Is WasapiEngine Then
                        currentQueueDepth = DirectCast(mic, WasapiEngine).BufferQueueCount
                    End If
                    
                    ' If queue is building, drain DIRECTLY from mic before Process()
                    If currentQueueDepth > 10 Then
                        ' Calculate how much excess to drain
                        Dim excessBuffers = currentQueueDepth - 5  ' Target: keep at ~5 buffers
                        Dim drainCalls = Math.Min(excessBuffers, 20)  ' Max 20 drains per tick
                        
                        For i = 1 To drainCalls
                            Dim throwaway(4095) As Byte
                            Dim read = mic.Read(throwaway, 0, throwaway.Length)
                            If read = 0 Then Exit For  ' No more data
                        Next
                    End If
                    
                    ' Now call Process() for recording (adaptive rate)
                    ' REDUCED: Max processCount from 16 ? 8 to reduce burst load
                    Dim processCount As Integer = 2  ' Reduced from 4
                    
                    If currentQueueDepth > 100 Then
                        processCount = 8  ' Reduced from 16
                    ElseIf currentQueueDepth > 50 Then
                        processCount = 6  ' Reduced from 12
                    ElseIf currentQueueDepth > 20 Then
                        processCount = 4  ' Reduced from 8
                    End If
                    
                    ' DIAGNOSTIC: Time the processing loop
                    Dim loopStartTime = _processingStopwatch.Elapsed.TotalMilliseconds
                    
                    For i = 1 To processCount
                        ' Time each individual Process() call
                        _processingStopwatch.Restart()
                        recorder.Process()
                        _processingStopwatch.Stop()
                        
                        ' Thread-safe counter updates using Interlocked
                        Dim elapsed = _processingStopwatch.Elapsed.TotalMilliseconds
                        Dim elapsedMs = CLng(elapsed) ' Convert to Long for Interlocked.Add
                        
                        Interlocked.Add(_totalProcessingTimeMs, elapsedMs)
                        Interlocked.Increment(_processCallCount)
                        
                        ' Track slow calls (thread-safe)
                        If elapsed > 5.0 Then
                            Interlocked.Increment(_verySlowCallCount)
                            Logger.Instance.Warning($"VERY SLOW Process() call: {elapsed:F2}ms (threshold: 5ms)", "RecordingManager")
                        ElseIf elapsed > 1.0 Then
                            Interlocked.Increment(_slowCallCount)
                            Logger.Instance.Warning($"SLOW Process() call: {elapsed:F2}ms (threshold: 1ms)", "RecordingManager")
                        End If
                    Next
                    
                    Dim loopEndTime = _processingStopwatch.Elapsed.TotalMilliseconds
                    Dim totalLoopTime = loopEndTime - loopStartTime
                    
                    ' Log if loop took longer than timer interval (10ms)
                    If totalLoopTime > 10.0 Then
                        Logger.Instance.Warning($"Process() loop EXCEEDED timer interval: {totalLoopTime:F2}ms (count={processCount}, depth={currentQueueDepth})", "RecordingManager")
                    End If

                    ' Check if recording just stopped (loop take completed)
                    Dim isRecording = recorder.IsRecording
                    If wasRecording AndAlso Not isRecording AndAlso recordingOptions.Mode = RecordingMode.LoopMode Then
                        ' Loop take completed
                        Logger.Instance.Debug("Loop take completed", "RecordingManager")
                        ' Don't set _isRecording = False here, loop mode continues
                    End If

                    ' Raise time update
                    RaiseEvent RecordingTimeUpdated(Me, recorder.RecordingDuration)

                    ' DIAGNOSTIC: Log performance stats every 10 seconds
                    Dim now = DateTime.Now
                    If (now - _lastStatsLogTime).TotalSeconds >= 10 Then
                        _lastStatsLogTime = now
                        
                        ' Thread-safe reads using Interlocked
                        Dim callCount = Interlocked.Read(_processCallCount)
                        If callCount > 0 Then
                            Dim totalTime = Interlocked.Read(_totalProcessingTimeMs)
                            Dim slowCount = Interlocked.Read(_slowCallCount)
                            Dim verySlowCount = Interlocked.Read(_verySlowCallCount)
                            
                            Dim avgTime = CDbl(totalTime) / callCount
                            Logger.Instance.Info($"Process() Performance: Calls={callCount}, Avg={avgTime:F3}ms, Slow(>1ms)={slowCount}, VerySlow(>5ms)={verySlowCount}", "RecordingManager")
                        End If
                    End If


                    ' FREEWHEELING FFT PATH: Read from separate FFT queue
                    ' This doesn't block recording and can drop frames if UI is slow
                    Dim fftBuffer(4095) As Byte ' Small 4KB buffer for responsive FFT
                    Dim fftRead As Integer = 0
                    
                    If TypeOf mic Is MicInputSource Then
                        Dim micSource = DirectCast(mic, MicInputSource)
                        fftRead = micSource.ReadForFFT(fftBuffer, 0, fftBuffer.Length)
                    ElseIf TypeOf mic Is WasapiEngine Then
                        Dim wasapiEngine = DirectCast(mic, WasapiEngine)
                        fftRead = wasapiEngine.ReadForFFT(fftBuffer, 0, fftBuffer.Length)
                    End If
                        
                    If fftRead > 0 Then
                        ' Copy only valid data
                        Dim validBuffer(fftRead - 1) As Byte
                        Array.Copy(fftBuffer, validBuffer, fftRead)
                            
                        Dim args As New AudioBufferEventArgs With {
                            .Buffer = validBuffer,
                            .BitsPerSample = mic.BitsPerSample,
                            .Channels = mic.Channels,
                            .SampleRate = mic.SampleRate
                        }
                        RaiseEvent BufferAvailable(Me, args)
                    End If

                ElseIf mic IsNot Nothing Then
                    ' Not recording, just consume buffers for metering
                    ' AGGRESSIVE DRAIN: Same strategy as recording path
                    Dim currentQueueDepth As Integer = 0
                    
                    If TypeOf mic Is MicInputSource Then
                        currentQueueDepth = DirectCast(mic, MicInputSource).BufferQueueCount
                    ElseIf TypeOf mic Is WasapiEngine Then
                        currentQueueDepth = DirectCast(mic, WasapiEngine).BufferQueueCount
                    End If
                    
                    ' If queue building, drain AGGRESSIVELY (but reduced max)
                    Dim drainCount As Integer = 2  ' Reduced from 4
                    
                    If currentQueueDepth > 100 Then
                        drainCount = 10  ' Reduced from 20
                    ElseIf currentQueueDepth > 50 Then
                        drainCount = 8   ' Reduced from 16
                    ElseIf currentQueueDepth > 20 Then
                        drainCount = 6   ' Reduced from 12
                    ElseIf currentQueueDepth > 10 Then
                        drainCount = 4   ' Reduced from 8
                    End If
                    
                    ' Drain at adaptive rate
                    For i = 1 To drainCount
                        Dim buffer(4095) As Byte
                        Dim read = mic.Read(buffer, 0, buffer.Length)

                        If read > 0 Then
                            Dim args As New AudioBufferEventArgs With {
                                .Buffer = buffer,
                                .BitsPerSample = mic.BitsPerSample,
                                .Channels = mic.Channels,
                                .SampleRate = mic.SampleRate
                            }
                            RaiseEvent BufferAvailable(Me, args)
                        Else
                            ' No more data, stop draining
                            Exit For
                        End If
                    Next
                End If

            Catch ex As Exception
                Logger.Instance.Error("Error in processing timer", ex, "RecordingManager")
            End Try
        End Sub

#End Region

#Region "IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            Logger.Instance.Info("Disposing RecordingManager", "RecordingManager")

            StopRecording()
            DisarmMicrophone()

            ' REMOVED: processingTimer?.Dispose() - no longer using timer
            ' Note: RecordingEngine doesn't implement IDisposable, no disposal needed

            Logger.Instance.Info("RecordingManager disposed", "RecordingManager")
        End Sub

#End Region

    End Class

#Region "Event Args"

    ''' <summary>Recording stopped event arguments</summary>
    Public Class RecordingStoppedEventArgs
        Inherits EventArgs

        Public Property Duration As TimeSpan
        Public Property FilePath As String
    End Class

    ''' <summary>Audio buffer available event arguments</summary>
    Public Class AudioBufferEventArgs
        Inherits EventArgs

        Public Property Buffer As Byte()
        Public Property BitsPerSample As Integer
        Public Property Channels As Integer
        Public Property SampleRate As Integer
    End Class

#End Region

End Namespace
