Imports System.Threading

Namespace Audio.Routing

    ''' <summary>
    ''' LOCK-FREE dual-buffer for audio processing + monitoring.
    ''' Buffer 1 = Processing buffer (modified in-place by audio thread)
    ''' Buffer 2 = Monitor tap (read-only copy for FFT thread)
    ''' 
    ''' DESIGN PHILOSOPHY:
    ''' Audio thread = NEVER BLOCKS (highest priority, real-time)
    ''' FFT thread = Freewheeling (disposable, just visualization)
    ''' 
    ''' No locks! If FFT reads during audio write = one corrupted frame.
    ''' User never notices (20 FPS). But blocking audio = AUDIBLE CLICKS!
    ''' 
    ''' Trade-off: Invisible FFT glitch vs guaranteed audio quality.
    ''' </summary>
    Public Class DualBuffer

#Region "Private Fields"

        ''' <summary>Buffer 1 - Processing buffer (audio thread modifies this)</summary>
        Private _processingBuffer As Byte()

        ''' <summary>Buffer 2 - Monitor tap (FFT thread reads this LOCK-FREE)</summary>
        Private _monitorBuffer As Byte()

        ''' <summary>Flag indicating if monitor buffer has new data (atomic)</summary>
        Private _hasMonitorData As Integer = 0  ' 0 = false, 1 = true (for Interlocked)

#End Region

#Region "Public Properties"

        ''' <summary>Sample rate of the audio data</summary>
        Public Property SampleRate As Integer

        ''' <summary>Bits per sample (typically 16 or 24)</summary>
        Public Property BitsPerSample As Integer

        ''' <summary>Number of audio channels (1 = mono, 2 = stereo)</summary>
        Public Property Channels As Integer

        ''' <summary>Size of each buffer in bytes</summary>
        Public ReadOnly Property BufferSize As Integer
            Get
                Return If(_processingBuffer IsNot Nothing, _processingBuffer.Length, 0)
            End Get
        End Property

        ''' <summary>
        ''' Get processing buffer (Buffer 1) for in-place DSP modifications.
        ''' This buffer is NOT copied - audio thread modifies it directly.
        ''' </summary>
        Public ReadOnly Property ProcessingBuffer As Byte()
            Get
                Return _processingBuffer
            End Get
        End Property

        ''' <summary>
        ''' Check if monitor buffer has new data (lock-free, atomic read).
        ''' FFT thread checks this before reading.
        ''' </summary>
        Public ReadOnly Property HasMonitorData As Boolean
            Get
                Return Interlocked.CompareExchange(_hasMonitorData, 0, 0) = 1
            End Get
        End Property

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Write new audio data to both buffers.
        ''' Buffer 1 = direct reference (for in-place processing)
        ''' Buffer 2 = copy (for monitoring)
        ''' Fast path - only copies to monitor buffer if needed.
        ''' </summary>
        ''' <param name="data">Audio data to write</param>
        Public Sub Write(data As Byte())
            If data Is Nothing OrElse data.Length = 0 Then
                Return
            End If

            ' Ensure buffers are allocated
            If _processingBuffer Is Nothing OrElse _processingBuffer.Length <> data.Length Then
                _processingBuffer = New Byte(data.Length - 1) {}
                _monitorBuffer = New Byte(data.Length - 1) {}
            End If

            ' Buffer 1: Copy for processing (audio thread will modify this in-place)
            Array.Copy(data, _processingBuffer, data.Length)

            ' Buffer 2: Copy for monitoring (LOCK-FREE - FFT thread reads this)
            ' No lock! Audio thread NEVER WAITS.
            ' If FFT reads during write = one corrupted frame (acceptable)
            Array.Copy(data, _monitorBuffer, data.Length)

            ' Set flag atomically (lock-free for readers)
            Interlocked.Exchange(_hasMonitorData, 1)
        End Sub

        ''' <summary>
        ''' Read monitoring buffer (Buffer 2) - LOCK-FREE!
        ''' FFT thread calls this to get a snapshot of the audio.
        ''' 
        ''' DESIGN PHILOSOPHY:
        ''' - Audio thread NEVER waits (highest priority)
        ''' - FFT thread is disposable (just visualization)
        ''' - If we read partial data during a write = ONE bad FFT frame
        ''' - User never notices (20 FPS = 50ms per frame)
        ''' - But audio blocking even 1?s = AUDIBLE CLICK!
        ''' 
        ''' Trade-off: Risk invisible FFT glitch vs guaranteed audio quality.
        ''' </summary>
        ''' <returns>Copy of monitor buffer, or Nothing if no data</returns>
        Public Function ReadMonitorBuffer() As Byte()
            If Not HasMonitorData Then
                Return Nothing
            End If

            If _monitorBuffer Is Nothing Then
                Return Nothing
            End If

            ' NO LOCK: Read directly (fast, non-blocking)
            ' If audio thread is writing during read = one corrupted frame
            ' Better than blocking audio thread!
            Dim bufferCopy(_monitorBuffer.Length - 1) As Byte
            Array.Copy(_monitorBuffer, bufferCopy, _monitorBuffer.Length)
            Return bufferCopy
        End Function

        ''' <summary>
        ''' Clear the "has data" flag after FFT thread reads the buffer.
        ''' Called by FFT thread after processing the monitor tap.
        ''' </summary>
        Public Sub ClearMonitorFlag()
            Interlocked.Exchange(_hasMonitorData, 0)
        End Sub

        ''' <summary>
        ''' Clear all buffers and reset state.
        ''' LOCK-FREE: Not critical path, races are acceptable.
        ''' </summary>
        Public Sub Clear()
            If _processingBuffer IsNot Nothing Then
                Array.Clear(_processingBuffer, 0, _processingBuffer.Length)
            End If
            If _monitorBuffer IsNot Nothing Then
                Array.Clear(_monitorBuffer, 0, _monitorBuffer.Length)
            End If
            Interlocked.Exchange(_hasMonitorData, 0)
        End Sub

#End Region

    End Class

End Namespace
