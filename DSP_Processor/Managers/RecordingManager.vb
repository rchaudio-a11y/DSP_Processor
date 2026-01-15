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
        Private processingTimer As Timer
        Private _isArmed As Boolean = False
        Private _isRecording As Boolean = False

        ' Settings
        Private audioSettings As AudioDeviceSettings
        Private recordingOptions As RecordingOptions

#End Region

#Region "Properties"

        ''' <summary>Is microphone armed and ready?</summary>
        Public ReadOnly Property IsArmed As Boolean
            Get
                Return _isArmed
            End Get
        End Property

        ''' <summary>Is currently recording?</summary>
        Public ReadOnly Property IsRecording As Boolean
            Get
                Return _isRecording AndAlso recorder IsNot Nothing AndAlso recorder.IsRecording
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
                If _isArmed Then
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

                ' Start processing timer (20ms intervals - must be <= buffer size)
                ' Keep at 20ms for responsiveness even if buffer is larger
                processingTimer = New Timer(AddressOf ProcessingTimer_Tick, Nothing, 0, 20)
                Logger.Instance.Debug($"Processing timer: 20ms intervals, Buffer: {audioSettings.BufferMilliseconds}ms", "RecordingManager")

                _isArmed = True
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
                If Not _isArmed Then Return

                Logger.Instance.Info("Disarming microphone...", "RecordingManager")

                ' Stop timer FIRST to prevent race conditions
                processingTimer?.Dispose()
                processingTimer = Nothing

                ' Give time for any pending timer events to complete
                System.Threading.Thread.Sleep(50)

                ' Dispose mic and wait for cleanup
                If mic IsNot Nothing Then
                    If TypeOf mic Is IDisposable Then
                        DirectCast(mic, IDisposable).Dispose()
                    End If
                    mic = Nothing
                End If
                
                ' Another small delay to ensure disposal is complete
                System.Threading.Thread.Sleep(50)

                _isArmed = False
                RaiseEvent MicrophoneArmed(Me, False)

                Logger.Instance.Info("Microphone disarmed", "RecordingManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to disarm microphone", ex, "RecordingManager")
            End Try
        End Sub

        ''' <summary>Start recording to file</summary>
        Public Sub StartRecording()
            Try
                If _isRecording Then
                    Logger.Instance.Warning("Already recording", "RecordingManager")
                    Return
                End If

                ' Ensure mic is armed
                If Not _isArmed OrElse mic Is Nothing Then
                    Logger.Instance.Info("Microphone not armed, arming now...", "RecordingManager")
                    ArmMicrophone()
                    System.Threading.Thread.Sleep(500) ' Give time to warm up
                End If

                Logger.Instance.Info("Starting recording...", "RecordingManager")

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

                _isRecording = True
                RaiseEvent RecordingStarted(Me, EventArgs.Empty)

            Catch ex As Exception
                Logger.Instance.Error("Failed to start recording", ex, "RecordingManager")
                _isRecording = False
                Throw
            End Try
        End Sub

        ''' <summary>Stop recording</summary>
        Public Sub StopRecording()
            Try
                If Not _isRecording Then Return

                Logger.Instance.Info("Stopping recording...", "RecordingManager")

                ' Get duration before stopping
                Dim duration = If(recorder IsNot Nothing, recorder.RecordingDuration, TimeSpan.Zero)
                Dim filePath = "" ' RecordingEngine doesn't expose last file path

                ' Check if in loop mode
                If recordingOptions.Mode = RecordingMode.LoopMode Then
                    recorder.CancelLoopRecording()
                Else
                    recorder.StopRecording()
                End If

                _isRecording = False

                Dim args As New RecordingStoppedEventArgs With {
                    .Duration = duration,
                    .FilePath = filePath
                }

                RaiseEvent RecordingStopped(Me, args)

                Logger.Instance.Info($"Recording stopped: {args.Duration.TotalSeconds:F1}s", "RecordingManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to stop recording", ex, "RecordingManager")
                _isRecording = False
            End Try
        End Sub

#End Region

#Region "Private Methods"

        Private Sub ProcessingTimer_Tick(state As Object)
            Try
                ' Track if we were recording before Process() call
                Dim wasRecording = _isRecording AndAlso recorder IsNot Nothing AndAlso recorder.IsRecording

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
                    Dim processCount As Integer = 4  ' Default
                    
                    If currentQueueDepth > 100 Then
                        processCount = 16
                    ElseIf currentQueueDepth > 50 Then
                        processCount = 12
                    ElseIf currentQueueDepth > 20 Then
                        processCount = 8
                    End If
                    
                    For i = 1 To processCount
                        recorder.Process()
                    Next

                    ' Check if recording just stopped (loop take completed)
                    Dim isRecording = recorder.IsRecording
                    If wasRecording AndAlso Not isRecording AndAlso recordingOptions.Mode = RecordingMode.LoopMode Then
                        ' Loop take completed
                        Logger.Instance.Debug("Loop take completed", "RecordingManager")
                        ' Don't set _isRecording = False here, loop mode continues
                    End If

                    ' Raise time update
                    RaiseEvent RecordingTimeUpdated(Me, recorder.RecordingDuration)

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
                    
                    ' If queue building, drain AGGRESSIVELY
                    Dim drainCount As Integer = 4  ' Default
                    
                    If currentQueueDepth > 100 Then
                        drainCount = 20  ' Maximum aggressive
                    ElseIf currentQueueDepth > 50 Then
                        drainCount = 16
                    ElseIf currentQueueDepth > 20 Then
                        drainCount = 12
                    ElseIf currentQueueDepth > 10 Then
                        drainCount = 8
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

            processingTimer?.Dispose()
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
