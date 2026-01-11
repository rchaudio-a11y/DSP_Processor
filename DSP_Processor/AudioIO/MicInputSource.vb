Imports NAudio.Wave
Imports System.Collections.Concurrent


Namespace AudioIO

    Public Class MicInputSource
        Implements IInputSource

        Private waveIn As WaveInEvent
        Private bufferQueue As New ConcurrentQueue(Of Byte())
        Private sampleRateValue As Integer
        Private channelsValue As Integer
        Private bitsValue As Integer

        Public Sub New(sampleRate As Integer, channels As String, bits As Integer, Optional deviceIndex As Integer = 0, Optional BufferMill As Integer = 20)
            sampleRateValue = sampleRate
            bitsValue = bits
            
            Dim BM As Integer = If(BufferMill > 0, BufferMill, 20)
            Debug.WriteLine("MicInputSource reports channels: " & channels)
            Select Case channels
                Case "Mono (1)"
                    channelsValue = 1
                Case "Stereo (2)"
                    channelsValue = 2
                Case Else
                    channelsValue = 1
                    MsgBox("MicInputSource reports channels: " & channels)
            End Select
            ' Create WaveFormat with correct parameters (sampleRate, bits, channels)
            waveIn = New WaveInEvent() With {
                .DeviceNumber = deviceIndex,
                .WaveFormat = New WaveFormat(sampleRateValue, bitsValue, channelsValue),
                .BufferMilliseconds = BM
            }


            AddHandler waveIn.DataAvailable, AddressOf OnDataAvailable
            waveIn.StartRecording()
        End Sub

        Private Sub OnDataAvailable(sender As Object, e As WaveInEventArgs)
            ' Copy the buffer so NAudio can reuse its internal one
            Dim copy(e.BytesRecorded - 1) As Byte
            Buffer.BlockCopy(e.Buffer, 0, copy, 0, e.BytesRecorded)
            bufferQueue.Enqueue(copy)
        End Sub

        Public ReadOnly Property SampleRate As Integer Implements IInputSource.SampleRate
            Get
                Return sampleRateValue
            End Get
        End Property

        Public ReadOnly Property Channels As Integer Implements IInputSource.Channels
            Get
                Return channelsValue
            End Get
        End Property

        Public ReadOnly Property BitsPerSample As Integer Implements IInputSource.BitsPerSample
            Get
                Return bitsValue
            End Get
        End Property

        Public Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer Implements IInputSource.Read
            Dim totalRead As Integer = 0
            Dim outBuffer() As Byte = Nothing

            While totalRead < count AndAlso bufferQueue.TryDequeue(outBuffer)
                Dim toCopy As Integer = Math.Min(outBuffer.Length, count - totalRead)
                System.Buffer.BlockCopy(outBuffer, 0, buffer, offset + totalRead, toCopy)
                totalRead += toCopy
            End While

            Return totalRead
        End Function

        Public Sub Dispose()
            If waveIn IsNot Nothing Then
                waveIn.StopRecording()
                waveIn.Dispose()
                waveIn = Nothing
            End If
        End Sub

    End Class
End Namespace

