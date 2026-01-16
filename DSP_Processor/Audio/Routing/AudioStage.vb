Imports DSP_Processor.Utils

Namespace Audio.Routing

    ''' <summary>
    ''' Represents one stage in the audio pipeline with dual buffers.
    ''' Each stage has:
    ''' - Processing buffer (Buffer 1) - modified by audio thread
    ''' - Monitor tap (Buffer 2) - read by FFT thread
    ''' Examples: Input Stage, Gain Stage, Output Stage
    ''' </summary>
    Public Class AudioStage

#Region "Private Fields"

        Private ReadOnly _dualBuffer As DualBuffer
        Private ReadOnly _stageName As String

#End Region

#Region "Public Properties"

        ''' <summary>Name of this stage (for logging/debugging)</summary>
        Public ReadOnly Property Name As String
            Get
                Return _stageName
            End Get
        End Property

        ''' <summary>Check if monitor buffer has new data (lock-free)</summary>
        Public ReadOnly Property HasMonitorData As Boolean
            Get
                Return _dualBuffer.HasMonitorData
            End Get
        End Property

        ''' <summary>Get the dual buffer (for direct access if needed)</summary>
        Friend ReadOnly Property DualBuffer As DualBuffer
            Get
                Return _dualBuffer
            End Get
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Create a new audio pipeline stage.
        ''' </summary>
        ''' <param name="name">Name of the stage (for logging)</param>
        ''' <param name="initialBufferSize">Initial buffer size in bytes (optional)</param>
        Public Sub New(name As String, Optional initialBufferSize As Integer = 8192)
            _stageName = name
            _dualBuffer = New DualBuffer()

            Logger.Instance.Debug($"AudioStage '{name}' created", "AudioStage")
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Input new audio data to this stage.
        ''' Writes to both buffers:
        ''' - Buffer 1 (processing buffer)
        ''' - Buffer 2 (monitor tap for FFT)
        ''' Fast path - just array copies.
        ''' </summary>
        ''' <param name="data">Audio data</param>
        ''' <param name="sampleRate">Sample rate</param>
        ''' <param name="bitsPerSample">Bits per sample</param>
        ''' <param name="channels">Number of channels</param>
        Public Sub Input(data As Byte(), sampleRate As Integer, bitsPerSample As Integer, channels As Integer)
            If data Is Nothing OrElse data.Length = 0 Then
                Return
            End If

            ' Update metadata
            _dualBuffer.SampleRate = sampleRate
            _dualBuffer.BitsPerSample = bitsPerSample
            _dualBuffer.Channels = channels

            ' Write to dual buffers
            _dualBuffer.Write(data)
        End Sub

        ''' <summary>
        ''' Process audio in-place using the provided processor function.
        ''' Modifies Buffer 1 directly (fast path).
        ''' Buffer 2 remains unchanged for FFT monitoring.
        ''' </summary>
        ''' <param name="processor">Processing function that modifies the buffer</param>
        ''' <returns>The processed buffer (same reference as Buffer 1)</returns>
        Public Function Process(processor As Func(Of Byte(), Byte())) As Byte()
            If processor Is Nothing Then
                Return _dualBuffer.ProcessingBuffer
            End If

            Try
                ' Get processing buffer (Buffer 1)
                Dim buffer = _dualBuffer.ProcessingBuffer

                If buffer IsNot Nothing Then
                    ' Process in-place
                    Return processor(buffer)
                End If

                Return buffer

            Catch ex As Exception
                Logger.Instance.Error($"Error processing stage '{_stageName}'", ex, "AudioStage")
                Return _dualBuffer.ProcessingBuffer
            End Try
        End Function

        ''' <summary>
        ''' Get monitor tap (Buffer 2) for FFT analysis.
        ''' Thread-safe - called by FFT thread.
        ''' Returns a COPY of the buffer.
        ''' </summary>
        ''' <returns>Copy of monitor buffer, or Nothing if no data</returns>
        Public Function GetMonitorTap() As Byte()
            Return _dualBuffer.ReadMonitorBuffer()
        End Function

        ''' <summary>
        ''' Get monitor tap with metadata.
        ''' </summary>
        ''' <returns>Monitor data with metadata, or Nothing</returns>
        Public Function GetMonitorTapWithMetadata() As MonitorTapData
            Dim buffer = _dualBuffer.ReadMonitorBuffer()
            If buffer Is Nothing Then
                Return Nothing
            End If

            Return New MonitorTapData With {
                .Buffer = buffer,
                .SampleRate = _dualBuffer.SampleRate,
                .BitsPerSample = _dualBuffer.BitsPerSample,
                .Channels = _dualBuffer.Channels,
                .StageName = _stageName
            }
        End Function

        ''' <summary>
        ''' Clear the "has data" flag after FFT thread reads the buffer.
        ''' Called by FFT thread after processing.
        ''' </summary>
        Public Sub ClearMonitorFlag()
            _dualBuffer.ClearMonitorFlag()
        End Sub

        ''' <summary>
        ''' Clear all buffers in this stage.
        ''' </summary>
        Public Sub Clear()
            _dualBuffer.Clear()
        End Sub

#End Region

    End Class

    ''' <summary>
    ''' Monitor tap data with metadata.
    ''' Returned by GetMonitorTapWithMetadata().
    ''' </summary>
    Public Class MonitorTapData
        Public Property Buffer As Byte()
        Public Property SampleRate As Integer
        Public Property BitsPerSample As Integer
        Public Property Channels As Integer
        Public Property StageName As String
    End Class

End Namespace
