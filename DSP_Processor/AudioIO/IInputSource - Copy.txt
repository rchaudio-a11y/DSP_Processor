Imports NAudio.Wave
Imports System.Collections.Concurrent


Namespace AudioIO

    Public Interface IInputSource
        ReadOnly Property SampleRate As Integer
        ReadOnly Property Channels As Integer
        ReadOnly Property BitsPerSample As Integer

        Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
    End Interface
End Namespace