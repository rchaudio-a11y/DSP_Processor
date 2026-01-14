Namespace AudioIO

    ''' <summary>
    ''' Base interface for audio input engines (WaveIn, WASAPI, ASIO)
    ''' Provides abstraction for multiple audio driver types
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 0, Task 0.2.2
    ''' Purpose: Abstract audio input layer for Phase 1 multi-driver support
    ''' </remarks>
    Public Interface IAudioEngine
        Inherits IDisposable

        ''' <summary>
        ''' Start capturing audio from the input device
        ''' </summary>
        ''' <exception cref="InvalidOperationException">Already recording</exception>
        ''' <exception cref="InvalidOperationException">Device not available</exception>
        Sub Start()

        ''' <summary>
        ''' Stop capturing audio
        ''' </summary>
        Sub [Stop]()

        ''' <summary>
        ''' Gets whether audio is currently being captured
        ''' </summary>
        ReadOnly Property IsRecording As Boolean

        ''' <summary>
        ''' Gets the audio sample rate in Hz
        ''' </summary>
        ReadOnly Property SampleRate As Integer

        ''' <summary>
        ''' Gets the number of audio channels
        ''' </summary>
        ReadOnly Property Channels As Integer

        ''' <summary>
        ''' Gets the bit depth per sample
        ''' </summary>
        ReadOnly Property BitsPerSample As Integer

        ''' <summary>
        ''' Gets the actual measured latency in milliseconds
        ''' </summary>
        ''' <remarks>
        ''' May differ from buffer size due to driver/hardware behavior
        ''' </remarks>
        ReadOnly Property Latency As Integer

        ''' <summary>
        ''' Gets the audio engine/driver name (e.g., "WaveIn", "WASAPI", "ASIO")
        ''' </summary>
        ReadOnly Property EngineName As String

        ''' <summary>
        ''' Raised when audio data is available for processing
        ''' </summary>
        Event DataAvailable As EventHandler(Of AudioDataEventArgs)

        ''' <summary>
        ''' Raised when recording stops (either by error or user action)
        ''' </summary>
        Event RecordingStopped As EventHandler(Of StoppedEventArgs)

    End Interface

    ''' <summary>
    ''' Event arguments for audio data availability
    ''' </summary>
    Public Class AudioDataEventArgs
        Inherits EventArgs

        ''' <summary>
        ''' Audio data buffer (PCM samples)
        ''' </summary>
        Public Property Buffer As Byte()

        ''' <summary>
        ''' Number of valid bytes in the buffer
        ''' </summary>
        Public Property BytesRecorded As Integer

        Public Sub New(buffer As Byte(), bytesRecorded As Integer)
            Me.Buffer = buffer
            Me.BytesRecorded = bytesRecorded
        End Sub

    End Class

    ''' <summary>
    ''' Event arguments for recording stopped event
    ''' </summary>
    Public Class StoppedEventArgs
        Inherits EventArgs

        ''' <summary>
        ''' Exception that caused the stop (Nothing if user-initiated)
        ''' </summary>
        Public Property Exception As Exception

        Public Sub New(Optional exception As Exception = Nothing)
            Me.Exception = exception
        End Sub

    End Class

End Namespace
