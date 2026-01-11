Imports NAudio.Wave
Imports System.Collections.Concurrent
Imports System.IO

Namespace AudioIO


    Public Class WavFileOutput
        Implements IOutputSink

        Private stream As FileStream
        Private writer As BinaryWriter
        Private dataLength As Integer = 0



        Public Sub New(path As String, sampleRate As Integer, channels As Integer, bits As Integer)
            Me.SampleRate = sampleRate
            Me.Channels = channels
            Me.BitsPerSample = bits

            stream = New FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)
            writer = New BinaryWriter(stream)

            WriteHeaderPlaceholder()
        End Sub

        Public ReadOnly Property SampleRate As Integer Implements IOutputSink.SampleRate
        Public ReadOnly Property Channels As Integer Implements IOutputSink.Channels
        Public ReadOnly Property BitsPerSample As Integer Implements IOutputSink.BitsPerSample

        Private Sub WriteHeaderPlaceholder()
            ' 44-byte WAV header placeholder
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"))
            writer.Write(0) ' placeholder for file size
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"))
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "))
            writer.Write(16) ' PCM header size
            writer.Write(CShort(1)) ' PCM format
            writer.Write(CShort(Channels))
            writer.Write(SampleRate)
            writer.Write(SampleRate * Channels * BitsPerSample \ 8)
            writer.Write(CShort(Channels * BitsPerSample \ 8))
            writer.Write(CShort(BitsPerSample))
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"))
            writer.Write(0) ' placeholder for data size
        End Sub

        Public Sub Write(samples() As Byte, count As Integer) Implements IOutputSink.Write
            writer.Write(samples, 0, count)
            dataLength += count
        End Sub

        Public Sub CloseSink() Implements IOutputSink.CloseSink
            writer.Seek(4, SeekOrigin.Begin)
            writer.Write(36 + dataLength)

            writer.Seek(40, SeekOrigin.Begin)
            writer.Write(dataLength)

            writer.Close()
            stream.Close()
        End Sub
    End Class
End Namespace