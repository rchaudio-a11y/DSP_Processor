Imports DSP_Processor.Utils

Namespace Audio.Routing

    ''' <summary>
    ''' Complete audio pipeline with multiple processing stages.
    ''' Architecture:
    '''   Input Stage  ? DSP Processing ? Gain Stage ? Output Stage ? Destinations
    '''      ? (tap)                         ? (tap)      ? (tap)
    '''    FFT Thread                     FFT Thread    FFT Thread
    ''' 
    ''' Fast path design:
    ''' - No events, no locks in audio thread
    ''' - Simple buffer copies to monitor taps
    ''' - In-place processing where possible
    ''' - FFT thread reads taps independently
    ''' </summary>
    Public Class AudioPipeline

#Region "Private Fields"

        ' Pipeline stages (each has dual buffers)
        Private ReadOnly _inputStage As AudioStage
        Private ReadOnly _gainStage As AudioStage
        Private ReadOnly _outputStage As AudioStage

        ' Pipeline configuration
        Private _config As PipelineConfiguration
        Private ReadOnly _configLock As New Object()

        ' Performance counters (for diagnostics)
        Private _buffersProcessed As Long = 0
        Private _totalProcessingTimeMs As Double = 0
        Private _slowBufferCount As Long = 0  ' Buffers that took > 5ms to process
        Private _lastStatsLogTime As DateTime = DateTime.MinValue
        Private ReadOnly _perfStopwatch As New Diagnostics.Stopwatch()

#End Region

#Region "Public Properties"

        ''' <summary>
        ''' Get a specific pipeline stage (for FFT thread access).
        ''' </summary>
        Public Function GetStage(stage As PipelineStage) As AudioStage
            Select Case stage
                Case PipelineStage.Input
                    Return _inputStage
                Case PipelineStage.Gain
                    Return _gainStage
                Case PipelineStage.Output
                    Return _outputStage
                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>Total buffers processed through the pipeline</summary>
        Public ReadOnly Property BuffersProcessed As Long
            Get
                Return _buffersProcessed
            End Get
        End Property

        ''' <summary>Average processing time per buffer in milliseconds</summary>
        Public ReadOnly Property AverageProcessingTimeMs As Double
            Get
                If _buffersProcessed = 0 Then Return 0
                Return _totalProcessingTimeMs / _buffersProcessed
            End Get
        End Property

        ''' <summary>Count of buffers that took longer than 5ms to process (may cause clicks)</summary>
        Public ReadOnly Property SlowBufferCount As Long
            Get
                Return _slowBufferCount
            End Get
        End Property

        ''' <summary>Estimated pipeline latency in milliseconds (based on buffer size and sample rate)</summary>
        Public Function GetEstimatedLatencyMs(bufferSize As Integer, sampleRate As Integer) As Double
            If sampleRate = 0 Then Return 0
            ' Latency = (bufferSize / bytesPerSample / channels) / sampleRate * 1000
            ' Assume 16-bit stereo
            Dim samplesPerBuffer = bufferSize / 4  ' 2 bytes per sample * 2 channels
            Return (samplesPerBuffer / sampleRate) * 1000
        End Function

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Create a new audio pipeline with the given configuration.
        ''' </summary>
        ''' <param name="configuration">Pipeline configuration</param>
        Public Sub New(configuration As PipelineConfiguration)
            If configuration Is Nothing Then
                Throw New ArgumentNullException(NameOf(configuration))
            End If

            _config = configuration

            ' Create pipeline stages
            _inputStage = New AudioStage("Input", 8192)
            _gainStage = New AudioStage("Gain", 8192)
            _outputStage = New AudioStage("Output", 8192)

            Logger.Instance.Info("AudioPipeline created with 3 stages", "AudioPipeline")
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Process one audio buffer through the complete pipeline.
        ''' FAST PATH - Called from audio thread, must be non-blocking!
        ''' 
        ''' Flow:
        ''' 1. Input Stage (optional tap for FFT)
        ''' 2. DSP Processing (gain, future: EQ, compressor, etc.)
        ''' 3. Gain Stage (optional tap for FFT)
        ''' 4. Output Stage (optional tap for FFT)
        ''' 5. Return processed buffer
        ''' </summary>
        ''' <param name="inputBuffer">Input audio buffer</param>
        ''' <param name="sampleRate">Sample rate (e.g., 44100)</param>
        ''' <param name="bitsPerSample">Bits per sample (e.g., 16)</param>
        ''' <param name="channels">Number of channels (e.g., 2 for stereo)</param>
        ''' <returns>Processed audio buffer</returns>
        Public Function ProcessBuffer(inputBuffer As Byte(),
                                     sampleRate As Integer,
                                     bitsPerSample As Integer,
                                     channels As Integer) As Byte()

            If inputBuffer Is Nothing OrElse inputBuffer.Length = 0 Then
                Return inputBuffer
            End If

            ' === PERFORMANCE TRACKING: Start timing ===
            _perfStopwatch.Restart()

            ' Get configuration snapshot (thread-safe read)
            Dim config As PipelineConfiguration
            SyncLock _configLock
                config = _config
            End SyncLock

            ' === CRITICAL: Save raw input buffer if needed later ===
            ' If Output FFT is set to Pre-DSP, we need a copy of the raw input
            ' BEFORE DSP processing modifies it in-place!
            Dim rawInputBuffer As Byte() = Nothing
            If config.Monitoring.EnableOutputFFT AndAlso config.Monitoring.OutputFFTTap = TapPoint.PreDSP Then
                rawInputBuffer = New Byte(inputBuffer.Length - 1) {}
                Array.Copy(inputBuffer, rawInputBuffer, inputBuffer.Length)
            End If

            ' === STAGE 1: INPUT TAP (based on selected tap point) ===
            ' Route to inputStage based on WHERE the user wants to tap from
            If config.Monitoring.EnableInputFFT Then
                ' Decide which data to send to inputStage based on InputFFTTap setting
                Select Case config.Monitoring.InputFFTTap
                    Case TapPoint.PreDSP
                        ' Tap BEFORE DSP processing (raw audio)
                        _inputStage.Input(inputBuffer, sampleRate, bitsPerSample, channels)
                End Select
            End If

            ' === STAGE 2: DSP PROCESSING ===
            ' Process in-place (modifies inputBuffer directly)
            Dim processedBuffer = inputBuffer
            If config.Processing.EnableDSP Then
                processedBuffer = ApplyDSP(inputBuffer, config.Processing, bitsPerSample, channels, sampleRate)
            Else
                ' IMPORTANT: When DSP is disabled, processedBuffer = inputBuffer (same reference)
                ' This means Pre-DSP and Post-DSP taps will show IDENTICAL data!
                ' To see a difference, enable DSP and adjust gain!
            End If

            ' === INPUT TAP: Post-DSP tap points (if selected) ===
            If config.Monitoring.EnableInputFFT Then
                Select Case config.Monitoring.InputFFTTap
                    Case TapPoint.PostDSP, TapPoint.PostGain
                        ' Tap AFTER DSP processing (processed audio)
                        _inputStage.Input(processedBuffer, sampleRate, bitsPerSample, channels)
                End Select
            End If

            ' === STAGE 3: OUTPUT TAP (based on selected tap point) ===
            ' Route to gainStage based on WHERE the user wants to tap from
            If config.Monitoring.EnableOutputFFT Then
                Select Case config.Monitoring.OutputFFTTap
                    Case TapPoint.PreDSP
                        ' Tap BEFORE DSP processing (raw audio)
                        ' Use saved raw copy if available, otherwise fallback to inputBuffer
                        If rawInputBuffer IsNot Nothing Then
                            _gainStage.Input(rawInputBuffer, sampleRate, bitsPerSample, channels)
                        Else
                            _gainStage.Input(inputBuffer, sampleRate, bitsPerSample, channels)
                        End If
                    Case TapPoint.PostDSP, TapPoint.PostGain
                        ' Tap AFTER DSP processing (processed audio)
                        _gainStage.Input(processedBuffer, sampleRate, bitsPerSample, channels)
                End Select
            End If

            ' === STAGE 4: OUTPUT STAGE ===
            ' Copy to output stage if destinations are enabled
            If config.Destination.EnableRecording OrElse config.Destination.EnablePlayback Then
                _outputStage.Input(processedBuffer, sampleRate, bitsPerSample, channels)
            End If

            ' === PERFORMANCE TRACKING: Stop timing and log stats ===
            _perfStopwatch.Stop()
            Dim elapsedMs = _perfStopwatch.Elapsed.TotalMilliseconds

            ' Update counters (thread-safe)
            Threading.Interlocked.Increment(_buffersProcessed)
            _totalProcessingTimeMs += elapsedMs

            ' Track slow buffers (> 5ms may cause clicks/pops)
            If elapsedMs > 5.0 Then
                Threading.Interlocked.Increment(_slowBufferCount)
                Logger.Instance.Warning($"SLOW BUFFER: {elapsedMs:F2}ms (threshold: 5ms) - May cause clicks!", "AudioPipeline")
            End If

            ' Log stats every 10 seconds
            Dim now = DateTime.Now
            If (now - _lastStatsLogTime).TotalSeconds >= 10 Then
                _lastStatsLogTime = now
                Dim avgLatency = GetEstimatedLatencyMs(inputBuffer.Length, sampleRate)
                Logger.Instance.Info($"Pipeline Stats: {_buffersProcessed} buffers, Avg={AverageProcessingTimeMs:F3}ms, Slow={_slowBufferCount}, Latency={avgLatency:F2}ms", "AudioPipeline")
            End If

            ' Return processed buffer (same reference if no DSP, or modified buffer if DSP applied)
            Return processedBuffer
        End Function

        ''' <summary>
        ''' Update pipeline configuration.
        ''' Thread-safe.
        ''' Clears stage buffers to prevent stale data when tap points change.
        ''' </summary>
        ''' <param name="newConfig">New configuration</param>
        Public Sub UpdateConfiguration(newConfig As PipelineConfiguration)
            If newConfig Is Nothing Then
                Return
            End If

            SyncLock _configLock
                _config = newConfig
            End SyncLock

            ' CRITICAL: Clear all stage buffers when config changes!
            ' This prevents FFT thread from reading stale data from old tap points.
            ' Example: If you switch Output FFT from "Post-DSP" to "Pre-DSP",
            ' the gainStage still has old processed audio data - clearing fixes this!
            _inputStage.Clear()
            _gainStage.Clear()
            _outputStage.Clear()

            Logger.Instance.Info("Pipeline configuration updated (buffers cleared)", "AudioPipeline")
        End Sub

        ''' <summary>
        ''' Clear all pipeline stages.
        ''' </summary>
        Public Sub Clear()
            _inputStage.Clear()
            _gainStage.Clear()
            _outputStage.Clear()
        End Sub

#End Region

#Region "Private DSP Methods"

        ''' <summary>
        ''' Apply DSP processing to audio buffer (in-place modification).
        ''' Phase 3: Separate input gain and output gain stages.
        ''' Phase 4 will expand this to full DSP chain (EQ, Compressor, Reverb, etc.).
        ''' 
        ''' Signal flow:
        ''' Input ? InputGain ? [DSP Chain] ? OutputGain ? Output
        ''' </summary>
        Private Function ApplyDSP(buffer As Byte(),
                                 processing As ProcessingConfiguration,
                                 bitsPerSample As Integer,
                                 channels As Integer,
                                 sampleRate As Integer) As Byte()

            Try
                ' === STAGE 1: INPUT GAIN (Pre-DSP trim) ===
                Dim inputGain = processing.InputGain
                If Math.Abs(inputGain - 1.0F) > 0.001F Then
                    ApplyGain(buffer, inputGain, bitsPerSample)
                End If

                ' === STAGE 2: DSP CHAIN ===
                ' Phase 4 will add: EQ, Compressor, Reverb, etc. here
                ' For now, this is where they would go between input and output gain

                ' === STAGE 3: OUTPUT GAIN (Post-DSP master fader) ===
                Dim outputGain = processing.OutputGain
                If Math.Abs(outputGain - 1.0F) > 0.001F Then
                    ApplyGain(buffer, outputGain, bitsPerSample)
                End If

                Return buffer

            Catch ex As Exception
                Logger.Instance.Error("Error in DSP processing", ex, "AudioPipeline")
                ' Return unmodified buffer on error (fail-safe)
                Return buffer
            End Try
        End Function

        ''' <summary>
        ''' Apply gain to buffer in-place (helper function).
        ''' Extracted for reuse by both InputGain and OutputGain stages.
        ''' </summary>
        Private Sub ApplyGain(buffer As Byte(), gain As Single, bitsPerSample As Integer)
            If bitsPerSample = 16 Then
                ' Process 16-bit PCM in-place
                For i = 0 To buffer.Length - 1 Step 2
                    If i + 1 < buffer.Length Then
                        ' Read sample
                        Dim sample = BitConverter.ToInt16(buffer, i)

                        ' Apply gain
                        Dim processed = CInt(sample * gain)

                        ' Clamp to 16-bit range (prevent clipping)
                        If processed > Short.MaxValue Then processed = Short.MaxValue
                        If processed < Short.MinValue Then processed = Short.MinValue

                        ' Write back (in-place modification)
                        Dim bytes = BitConverter.GetBytes(CShort(processed))
                        buffer(i) = bytes(0)
                        buffer(i + 1) = bytes(1)
                    End If
                Next
            End If

            ' TODO: Phase 4 - Add support for 24-bit, 32-bit formats
        End Sub

#End Region

    End Class

    ''' <summary>
    ''' Enum for identifying pipeline stages.
    ''' Used by FFT thread to access specific stage taps.
    ''' </summary>
    Public Enum PipelineStage
        ''' <summary>Input stage (Pre-DSP tap)</summary>
        Input
        ''' <summary>Gain stage (Post-DSP tap)</summary>
        Gain
        ''' <summary>Output stage (Final output tap)</summary>
        Output
    End Enum

End Namespace
