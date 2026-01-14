Imports NAudio.Wave
Imports System.Collections.Concurrent


Namespace AudioIO


    Public Interface IOutputSink
        ReadOnly Property SampleRate As Integer
        ReadOnly Property Channels As Integer
        ReadOnly Property BitsPerSample As Integer

        Sub Write(samples() As Byte, count As Integer)
        Sub CloseSink()
    End Interface
End Namespace