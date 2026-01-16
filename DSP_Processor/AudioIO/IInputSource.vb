Imports NAudio.Wave
Imports System.Collections.Concurrent


Namespace AudioIO

    ''' <summary>
    ''' Event args for real-time audio callback
    ''' </summary>
    Public Class AudioCallbackEventArgs
        Inherits EventArgs

        Public Property Buffer As Byte()
        Public Property BytesRecorded As Integer
    End Class

    Public Interface IInputSource
        ReadOnly Property SampleRate As Integer
        ReadOnly Property Channels As Integer
        ReadOnly Property BitsPerSample As Integer

        Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer

        ''' <summary>
        ''' Fires when audio data arrives from the driver (real-time callback)
        ''' Subscribe to this for glitch-free recording instead of polling with a timer
        ''' </summary>
        Event AudioDataAvailable As EventHandler(Of AudioCallbackEventArgs)
    End Interface
End Namespace