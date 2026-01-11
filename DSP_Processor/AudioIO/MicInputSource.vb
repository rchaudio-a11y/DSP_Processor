Imports NAudio.Wave
Imports System.Collections.Concurrent
Imports System.Threading
Imports DSP_Processor.Utils

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

            Logger.Instance.Debug($"MicInputSource creating: {channels}, {sampleRate}Hz, {bits}-bit", "MicInputSource")

            Select Case channels
                Case "Mono (1)"
                    channelsValue = 1
                Case "Stereo (2)"
                    channelsValue = 2
                Case Else
                    channelsValue = 1
                    Logger.Instance.Warning($"Unknown channel mode: {channels}, defaulting to Mono", "MicInputSource")
            End Select

            ' Create WaveFormat with correct parameters
            waveIn = New WaveInEvent() With {
                .DeviceNumber = deviceIndex,
                .WaveFormat = New WaveFormat(sampleRateValue, bitsValue, channelsValue),
                .BufferMilliseconds = BM,
                .NumberOfBuffers = 3
            }

            AddHandler waveIn.DataAvailable, AddressOf OnDataAvailable
            waveIn.StartRecording()

            Logger.Instance.Info($"MicInputSource initialized: {channelsValue}ch, {sampleRate}Hz, {bits}-bit, {BM}ms buffer", "MicInputSource")
        End Sub

        Private Sub OnDataAvailable(sender As Object, e As WaveInEventArgs)
            ' Capture ALL audio data immediately - no delays, no skipping!
            If e.BytesRecorded > 0 Then
                ' Copy the buffer so NAudio can reuse its internal one
                Dim copy(e.BytesRecorded - 1) As Byte
                Buffer.BlockCopy(e.Buffer, 0, copy, 0, e.BytesRecorded)
                bufferQueue.Enqueue(copy)
            End If
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

            ' Read all available buffers up to requested count
            While totalRead < count AndAlso bufferQueue.TryDequeue(outBuffer)
                Dim toCopy As Integer = Math.Min(outBuffer.Length, count - totalRead)
                System.Buffer.BlockCopy(outBuffer, 0, buffer, offset + totalRead, toCopy)
                totalRead += toCopy
            End While

            ' Return actual bytes read - DON'T fill with silence!
            ' The RecordingEngine will handle the actual data length correctly
            Return totalRead
        End Function

        ''' <summary>
        ''' Clears all buffered audio data. Call this before starting a new recording.
        ''' </summary>
        Public Sub ClearBuffers()
            Dim dummy As Byte() = Nothing
            Dim cleared As Integer = 0
            While bufferQueue.TryDequeue(dummy)
                cleared += 1
            End While
            
            If cleared > 0 Then
                Logger.Instance.Debug($"Cleared {cleared} stale buffers from queue", "MicInputSource")
            End If
        End Sub

        Public Sub Dispose()
            If waveIn IsNot Nothing Then
                waveIn.StopRecording()
                RemoveHandler waveIn.DataAvailable, AddressOf OnDataAvailable
                waveIn.Dispose()
                waveIn = Nothing
            End If

            ' Clear buffer queue
            While bufferQueue.TryDequeue(Nothing)
            End While

            Logger.Instance.Debug("MicInputSource disposed", "MicInputSource")
        End Sub

    End Class
End Namespace

