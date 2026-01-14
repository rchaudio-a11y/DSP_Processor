Imports NAudio.Wave
Imports System.Collections.Concurrent
Imports System.IO

Namespace AudioIO


    Public Class WavFileOutput
        Implements IOutputSink
        Implements IDisposable

        Private stream As FileStream
        Private writer As BinaryWriter
        Private dataLength As Integer = 0
        Private disposed As Boolean = False



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
            If disposed Then Throw New ObjectDisposedException("WavFileOutput")
            writer.Write(samples, 0, count)
            dataLength += count
        End Sub

        Public Sub CloseSink() Implements IOutputSink.CloseSink
            If disposed Then Return

            Try
                ' Update header with actual sizes
                writer.Seek(4, SeekOrigin.Begin)
                writer.Write(36 + dataLength)

                writer.Seek(40, SeekOrigin.Begin)
                writer.Write(dataLength)

                writer.Flush()
            Finally
                ' Always close streams
                writer?.Close()
                stream?.Close()
                disposed = True
            End Try
        End Sub

        ' IDisposable implementation
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposed Then
                If disposing Then
                    ' Dispose managed resources
                    CloseSink()
                End If
                disposed = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

    End Class
End Namespace