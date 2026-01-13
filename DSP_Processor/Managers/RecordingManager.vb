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

        Private mic As MicInputSource
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
                    Return mic.Volume
                End If
                Return 1.0F
            End Get
            Set(value As Single)
                If mic IsNot Nothing Then
                    mic.Volume = value
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

                Logger.Instance.Info("Arming microphone...", "RecordingManager")

                ' Create mic input source
                mic = New MicInputSource(
                    audioSettings.SampleRate,
                    If(audioSettings.Channels = 1, "Mono (1)", "Stereo (2)"),
                    audioSettings.BitDepth,
                    audioSettings.InputDeviceIndex,
                    audioSettings.BufferMilliseconds)

                ' Apply volume
                mic.Volume = 1.0F ' Default, can be adjusted via property

                ' Start processing timer (20ms intervals)
                processingTimer = New Timer(AddressOf ProcessingTimer_Tick, Nothing, 0, 20)

                _isArmed = True
                RaiseEvent MicrophoneArmed(Me, True)

                Logger.Instance.Info("Microphone armed successfully", "RecordingManager")

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

                ' Stop timer
                processingTimer?.Dispose()
                processingTimer = Nothing

                ' Dispose mic
                mic?.Dispose()
                mic = Nothing

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

                ' Clear stale buffers
                mic?.ClearBuffers()

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
                    recorder.Process()

                    ' Check if recording just stopped (loop take completed)
                    Dim isRecording = recorder.IsRecording
                    If wasRecording AndAlso Not isRecording AndAlso recordingOptions.Mode = RecordingMode.LoopMode Then
                        ' Loop take completed
                        Logger.Instance.Debug("Loop take completed", "RecordingManager")
                        ' Don't set _isRecording = False here, loop mode continues
                    End If

                    ' Raise time update
                    RaiseEvent RecordingTimeUpdated(Me, recorder.RecordingDuration)

                    ' Raise buffer available event (for metering/FFT)
                    If recorder.LastBuffer IsNot Nothing Then
                        Dim args As New AudioBufferEventArgs With {
                            .Buffer = recorder.LastBuffer,
                            .BitsPerSample = recorder.InputSource.BitsPerSample,
                            .Channels = recorder.InputSource.Channels,
                            .SampleRate = recorder.InputSource.SampleRate
                        }
                        RaiseEvent BufferAvailable(Me, args)
                    End If

                ElseIf mic IsNot Nothing Then
                    ' Not recording, just consume buffers for metering
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
                    End If
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
