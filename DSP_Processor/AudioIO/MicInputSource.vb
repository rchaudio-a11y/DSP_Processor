Imports NAudio.Wave
Imports System.Collections.Concurrent
Imports System.Threading
Imports DSP_Processor.Utils

Namespace AudioIO

    Public Class MicInputSource
        Implements IInputSource

        Private waveIn As WaveInEvent
        Private bufferQueue As New ConcurrentQueue(Of Byte()) ' CRITICAL PATH: Recording (never drops)
        Private fftQueue As New ConcurrentQueue(Of Byte()) ' FREEWHEELING: FFT/Metering (can drop frames)
        Private sampleRateValue As Integer
        Private channelsValue As Integer
        Private bitsValue As Integer
        Private volumeValue As Single = 1.0F ' 0.0 to 1.0 (0% to 100%)
        Private bufferOverflowCount As Integer = 0 ' Track buffer overflows
        Private lastOverflowWarning As DateTime = DateTime.MinValue
        Private Const MAX_FFT_QUEUE_DEPTH As Integer = 5 ' Max 5 frames (~100ms) in FFT queue
        Private _disposed As Boolean = False ' Track disposal to prevent race conditions

        ''' <summary>
        ''' REAL-TIME CALLBACK: Fires when audio data arrives from driver
        ''' Use this for glitch-free recording instead of polling
        ''' </summary>
        Public Event AudioDataAvailable As EventHandler(Of AudioCallbackEventArgs) Implements IInputSource.AudioDataAvailable

        Public Sub New(sampleRate As Integer, channels As String, bits As Integer, Optional deviceIndex As Integer = 0, Optional BufferMill As Integer = 20)
            sampleRateValue = sampleRate
            bitsValue = bits

            Dim BM As Integer = If(BufferMill > 0, BufferMill, 20)

            Logger.Instance.Info($"MicInputSource creating: Device={deviceIndex}, {channels}, {sampleRate}Hz, {bits}-bit", "MicInputSource")

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
            ' Check if disposed - ignore callbacks after disposal starts
            If _disposed Then Return
            
            ' Capture ALL audio data immediately - no delays, no skipping!
            If e.BytesRecorded > 0 Then
                ' Copy the buffer so NAudio can reuse its internal one
                Dim copy(e.BytesRecorded - 1) As Byte
                Buffer.BlockCopy(e.Buffer, 0, copy, 0, e.BytesRecorded)
                
                ' Apply volume adjustment if not 100%
                If Math.Abs(volumeValue - 1.0F) > 0.001F Then
                    ApplyVolume(copy, bitsValue)
                End If
                
                ' REAL-TIME: Raise event for callback-driven recording (GLITCH-FREE!)
                RaiseEvent AudioDataAvailable(Me, New AudioCallbackEventArgs With {
                    .Buffer = copy,
                    .BytesRecorded = e.BytesRecorded
                })
                
                ' LEGACY: Enqueue to recording buffer (for timer-driven polling - DEPRECATED)
                bufferQueue.Enqueue(copy)
                
                ' FREEWHEELING PATH: Enqueue to FFT buffer (drop old frames if too deep)
                If fftQueue.Count >= MAX_FFT_QUEUE_DEPTH Then
                    ' Queue full - drop oldest frame to prevent blocking
                    Dim discarded As Byte() = Nothing
                    fftQueue.TryDequeue(discarded)
                    Logger.Instance.Debug($"FFT queue full ({MAX_FFT_QUEUE_DEPTH} frames), dropped oldest frame", "MicInputSource")
                End If
                
                ' Make a separate copy for FFT (don't share references!)
                Dim fftCopy(e.BytesRecorded - 1) As Byte
                Buffer.BlockCopy(copy, 0, fftCopy, 0, e.BytesRecorded)
                fftQueue.Enqueue(fftCopy)
                
                ' Detect buffer overflow (recording queue too large = consumer not keeping up)
                If bufferQueue.Count > 10 Then ' More than 10 buffers queued = potential issue
                    bufferOverflowCount += 1
                    
                    ' Warn every 5 seconds to avoid log spam
                    If (DateTime.Now - lastOverflowWarning).TotalSeconds > 5 Then
                        Logger.Instance.Warning($"RECORDING buffer queue overflow! Queue size: {bufferQueue.Count}, Overflows: {bufferOverflowCount}. Timer-driven polling cannot keep up! Use AudioDataAvailable event instead.", "MicInputSource")
                        lastOverflowWarning = DateTime.Now
                    End If
                End If
            End If
        End Sub

        Private Sub ApplyVolume(buffer() As Byte, bitDepth As Integer)
            Select Case bitDepth
                Case 16
                    ' 16-bit signed samples
                    For i As Integer = 0 To buffer.Length - 1 Step 2
                        Dim sample As Short = BitConverter.ToInt16(buffer, i)
                        Dim adjusted As Integer = CInt(sample * volumeValue)
                        ' Clamp to prevent overflow
                        adjusted = Math.Max(Short.MinValue, Math.Min(Short.MaxValue, adjusted))
                        Dim bytes = BitConverter.GetBytes(CShort(adjusted))
                        buffer(i) = bytes(0)
                        buffer(i + 1) = bytes(1)
                    Next
                    
                Case 24
                    ' 24-bit samples (3 bytes per sample)
                    For i As Integer = 0 To buffer.Length - 1 Step 3
                        ' Read 24-bit sample (little-endian)
                        Dim sample As Integer = buffer(i) Or (buffer(i + 1) << 8) Or (buffer(i + 2) << 16)
                        ' Sign extend
                        If (sample And &H800000) <> 0 Then
                            sample = sample Or &HFF000000
                        End If
                        ' Apply volume
                        Dim adjusted As Integer = CInt(sample * volumeValue)
                        ' Clamp
                        adjusted = Math.Max(&HFF800000, Math.Min(&H7FFFFF, adjusted))
                        ' Write back
                        buffer(i) = CByte(adjusted And &HFF)
                        buffer(i + 1) = CByte((adjusted >> 8) And &HFF)
                        buffer(i + 2) = CByte((adjusted >> 16) And &HFF)
                    Next
                    
                Case 32
                    ' 32-bit float samples
                    For i As Integer = 0 To buffer.Length - 1 Step 4
                        Dim sample As Single = BitConverter.ToSingle(buffer, i)
                        Dim adjusted As Single = sample * volumeValue
                        ' Clamp to prevent distortion
                        adjusted = Math.Max(-1.0F, Math.Min(1.0F, adjusted))
                        Dim bytes = BitConverter.GetBytes(adjusted)
                        System.Buffer.BlockCopy(bytes, 0, buffer, i, 4)
                    Next
            End Select
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

        Public Property Volume As Single
            Get
                Return volumeValue
            End Get
            Set(value As Single)
                ' Clamp between 0.0 and 2.0 (allow up to 200% boost)
                volumeValue = Math.Max(0.0F, Math.Min(2.0F, value))
            End Set
        End Property
        
        ''' <summary>Gets the current recording buffer queue depth</summary>
        Public ReadOnly Property BufferQueueCount As Integer
            Get
                Return bufferQueue.Count
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
        ''' Read from the FREEWHEELING FFT queue (separate from recording path)
        ''' This queue can drop frames if FFT processing is too slow
        ''' </summary>
        Public Function ReadForFFT(buffer() As Byte, offset As Integer, count As Integer) As Integer
            Dim totalRead As Integer = 0
            Dim outBuffer() As Byte = Nothing

            ' Read all available buffers up to requested count
            While totalRead < count AndAlso fftQueue.TryDequeue(outBuffer)
                Dim toCopy As Integer = Math.Min(outBuffer.Length, count - totalRead)
                System.Buffer.BlockCopy(outBuffer, 0, buffer, offset + totalRead, toCopy)
                totalRead += toCopy
            End While

            Return totalRead
        End Function

        ''' <summary>
        ''' Clears all buffered audio data. Call this before starting a new recording.
        ''' </summary>
        Public Sub ClearBuffers()
            Dim dummy As Byte() = Nothing
            Dim cleared As Integer = 0
            Dim fftCleared As Integer = 0
            
            ' Clear recording buffer
            While bufferQueue.TryDequeue(dummy)
                cleared += 1
            End While
            
            ' Clear FFT buffer
            While fftQueue.TryDequeue(dummy)
                fftCleared += 1
            End While
            
            If cleared > 0 Or fftCleared > 0 Then
                Logger.Instance.Debug($"Cleared {cleared} recording buffers and {fftCleared} FFT buffers", "MicInputSource")
            End If
        End Sub

        Public Sub Dispose()
            _disposed = True ' Set flag FIRST to stop new callbacks
            
            If waveIn IsNot Nothing Then
                waveIn.StopRecording()
                ' Give time for pending callbacks to see the disposed flag
                System.Threading.Thread.Sleep(20)
                RemoveHandler waveIn.DataAvailable, AddressOf OnDataAvailable
                waveIn.Dispose()
                waveIn = Nothing
            End If

            ' Clear both buffer queues
            While bufferQueue.TryDequeue(Nothing)
            End While
            While fftQueue.TryDequeue(Nothing)
            End While

            Logger.Instance.Debug("MicInputSource disposed", "MicInputSource")
        End Sub

    End Class
End Namespace

