Imports System.Threading
Imports NAudio.Wave

Namespace DSP

    ''' <summary>
    ''' Pull-based DSP processing with dedicated worker thread.
    ''' Worker thread keeps output buffer filled ahead of WaveOut.
    ''' WaveOut thread NEVER does DSP work - only reads from output buffer.
    ''' </summary>
    Public Class DSPThread
        Implements IDisposable

#Region "Private Fields"

        Private ReadOnly inputBuffer As Utils.RingBuffer
        Private ReadOnly outputBuffer As Utils.RingBuffer

        ' PHASE 2.5: Multi-Reader Monitor Buffers (Architecture Rule #4)
        Friend ReadOnly inputMonitorBuffer As Utils.MultiReaderRingBuffer ' PreDSP tap (raw audio) - Friend for TapPointManager
        Friend ReadOnly outputMonitorBuffer As Utils.MultiReaderRingBuffer ' PreOutput tap (final processed) - Friend for TapPointManager
        Friend ReadOnly postGainMonitorBuffer As Utils.MultiReaderRingBuffer ' PostGain tap (after INPUT gain) - Friend for TapPointManager
        Friend ReadOnly postOutputGainMonitorBuffer As Utils.MultiReaderRingBuffer ' PostDSP tap (after OUTPUT gain) - Friend for TapPointManager

        Private ReadOnly processorChain As ProcessorChain
        Private ReadOnly workBuffer As AudioBuffer
        ' Thread-safe flags using Interlocked (0=False, 1=True)
        Private _disposed As Integer = 0 ' Thread-safe: Use Interlocked.CompareExchange
        Private ReadOnly inputLowEvent As AutoResetEvent ' Signal feeder when input low

        ' Worker thread for DSP processing
        Private workerThread As Thread
        Private _shouldStop As Integer = 0 ' Thread-safe: Use Interlocked.Exchange
        Private _isRunningFlag As Integer = 0 ' Thread-safe: Use Interlocked.Exchange

        ' Processing stats
        Private _processedSamples As Long = 0
        Private _droppedSamples As Long = 0

#End Region

#Region "Properties"

        ''' <summary>Gets the wave format</summary>
        Public ReadOnly Property Format As WaveFormat

        ''' <summary>Gets the processor chain</summary>
        Public ReadOnly Property Chain As ProcessorChain
            Get
                Return processorChain
            End Get
        End Property

        ''' <summary>Gets the total latency of the processor chain in samples</summary>
        Public ReadOnly Property TotalLatencySamples As Integer
            Get
                Return processorChain?.GetTotalLatency()
            End Get
        End Property

        ''' <summary>Gets the total latency of the processor chain in milliseconds</summary>
        Public ReadOnly Property TotalLatencyMs As Double
            Get
                Dim samples = TotalLatencySamples
                If samples = 0 OrElse Format Is Nothing Then Return 0.0
                Return (samples / CDbl(Format.SampleRate)) * 1000.0
            End Get
        End Property

        ''' <summary>Gets whether worker thread is running</summary>
        Public ReadOnly Property IsRunning As Boolean
            Get
                Return Interlocked.CompareExchange(_isRunningFlag, 0, 0) = 1
            End Get
        End Property

        ''' <summary>Gets the total number of processed samples</summary>
        Public ReadOnly Property ProcessedSamples As Long
            Get
                Return Interlocked.Read(_processedSamples)
            End Get
        End Property

        ''' <summary>Gets the total number of dropped samples (underruns)</summary>
        Public ReadOnly Property DroppedSamples As Long
            Get
                Return Interlocked.Read(_droppedSamples)
            End Get
        End Property

        ''' <summary>Gets the event for signaling file feeder</summary>
        Public ReadOnly Property InputLowSignal As AutoResetEvent
            Get
                Return inputLowEvent
            End Get
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Creates a new DSP thread
        ''' </summary>
        ''' <param name="format">Wave format to process</param>
        ''' <param name="inputBufferSize">Input buffer size in bytes</param>
        ''' <param name="outputBufferSize">Output buffer size in bytes</param>
        Public Sub New(format As WaveFormat, inputBufferSize As Integer, outputBufferSize As Integer)
            If format Is Nothing Then
                Throw New ArgumentNullException(NameOf(format))
            End If

            Me.Format = format
            inputLowEvent = New AutoResetEvent(False)

            ' Create buffers
            inputBuffer = New Utils.RingBuffer(inputBufferSize)
            outputBuffer = New Utils.RingBuffer(outputBufferSize)

            ' PHASE 2.5: Create MULTI-READER monitor buffers (0.5 seconds each for FFT and meters)
            ' Architecture Rule #4: Multiple instruments can read same audio independently
            Dim monitorSize = outputBufferSize \ 4
            inputMonitorBuffer = New Utils.MultiReaderRingBuffer(monitorSize)         ' PreDSP tap
            outputMonitorBuffer = New Utils.MultiReaderRingBuffer(monitorSize)        ' PreOutput tap
            postGainMonitorBuffer = New Utils.MultiReaderRingBuffer(monitorSize)      ' PostGain tap (after INPUT gain)
            postOutputGainMonitorBuffer = New Utils.MultiReaderRingBuffer(monitorSize) ' PostDSP tap (after OUTPUT gain)

            ' Create processor chain
            processorChain = New ProcessorChain(format)

            ' Create work buffer (256 samples)
            Const BLOCK_SIZE_SAMPLES As Integer = 256
            Dim blockSizeBytes = BLOCK_SIZE_SAMPLES * format.BlockAlign
            workBuffer = New AudioBuffer(format, blockSizeBytes, True)

            Utils.Logger.Instance.Info($"DSP initialized (pull-based with worker thread): {BLOCK_SIZE_SAMPLES} samples ({blockSizeBytes} bytes, {format.Channels}ch)", "DSPThread")
            Utils.Logger.Instance.Info($"Monitor buffers: {monitorSize} bytes each (0.5 seconds) - MULTI-READER taps: PreDSP + PostGain + PostDSP + PreOutput", "DSPThread")
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Start the DSP worker thread
        ''' </summary>
        Public Sub Start()
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 Then
                Throw New ObjectDisposedException(NameOf(DSPThread))
            End If
            If IsRunning Then Return

            Interlocked.Exchange(_shouldStop, 0) ' Set to False
            workerThread = New Thread(AddressOf WorkerLoop)
            workerThread.Name = "DSP Worker Thread"
            workerThread.Priority = ThreadPriority.AboveNormal ' High priority for real-time
            workerThread.IsBackground = True
            workerThread.Start()

            Utils.Logger.Instance.Info("DSP worker thread started", "DSPThread")
        End Sub

        ''' <summary>
        ''' Stop the DSP worker thread
        ''' Implements shutdown barrier pattern (50ms grace period)
        ''' Thread-safe: Can be called multiple times safely
        ''' </summary>
        Public Sub [Stop]()
            ' Check if already stopped (thread-safe read)
            If Interlocked.CompareExchange(_isRunningFlag, 0, 0) = 0 Then
                Return ' Already stopped
            End If

            ' Signal worker thread to stop
            Interlocked.Exchange(_shouldStop, 1) ' Set to True

            ' Grace period (50ms) - let worker finish current audio block
            ' This prevents buffer corruption and ensures clean shutdown
            Thread.Sleep(50)

            ' Wait for worker thread to exit (longer timeout after grace period)
            Dim joined As Boolean = workerThread?.Join(5000) ' 5 seconds max

            If joined Then
                Utils.Logger.Instance.Info($"DSP worker stopped cleanly. Processed: {ProcessedSamples}, Dropped: {DroppedSamples}", "DSPThread")
            Else
                ' Worker thread did not stop within timeout
                Utils.Logger.Instance.Warning($"DSP worker did not stop within timeout. Processed: {ProcessedSamples}, Dropped: {DroppedSamples}", "DSPThread")
            End If
        End Sub

        ''' <summary>
        ''' Writes audio data to the input buffer
        ''' </summary>
        Public Function WriteInput(data As Byte(), offset As Integer, count As Integer) As Integer
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 Then
                Throw New ObjectDisposedException(NameOf(DSPThread))
            End If
            Return inputBuffer.Write(data, offset, count)
        End Function

        ''' <summary>
        ''' Reads processed audio data from the output buffer (called by WaveOut)
        ''' REAL-TIME SAFE: Never blocks, never does DSP work
        ''' </summary>
        Public Function ReadOutput(data As Byte(), offset As Integer, count As Integer) As Integer
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 Then
                Throw New ObjectDisposedException(NameOf(DSPThread))
            End If
            Return outputBuffer.Read(data, offset, count)
        End Function

        ''' <summary>
        ''' Reads RAW audio from INPUT monitor buffer (before DSP processing)
        ''' NON-BLOCKING: For FFT comparison - shows what went INTO the DSP
        ''' DEPRECATED: Use CreateTapReader() and ReadFromTap() for multi-reader support
        ''' </summary>
        Public Function ReadInputMonitor(data As Byte(), offset As Integer, count As Integer) As Integer
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 Then
                Throw New ObjectDisposedException(NameOf(DSPThread))
            End If
            ' Auto-create default reader if not exists
            If Not inputMonitorBuffer.HasReader("_default_input") Then
                inputMonitorBuffer.CreateReader("_default_input")
            End If
            Return inputMonitorBuffer.Read("_default_input", data, offset, count)
        End Function

        ''' <summary>
        ''' Reads PROCESSED audio from OUTPUT monitor buffer (after DSP processing)
        ''' NON-BLOCKING: For FFT comparison - shows what came OUT of the DSP
        ''' DEPRECATED: Use CreateTapReader() and ReadFromTap() for multi-reader support
        ''' </summary>
        Public Function ReadOutputMonitor(data As Byte(), offset As Integer, count As Integer) As Integer
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 Then
                Throw New ObjectDisposedException(NameOf(DSPThread))
            End If
            ' Auto-create default reader if not exists
            If Not outputMonitorBuffer.HasReader("_default_output") Then
                outputMonitorBuffer.CreateReader("_default_output")
            End If
            Return outputMonitorBuffer.Read("_default_output", data, offset, count)
        End Function

        ''' <summary>
        ''' Reads audio from POST-GAIN monitor buffer (after Gain/Pan, DSP tap point pattern)
        ''' NON-BLOCKING: For meters - shows audio after INPUT gain/pan adjustments
        ''' DEPRECATED: Use CreateTapReader() and ReadFromTap() for multi-reader support
        ''' </summary>
        Public Function ReadPostGainMonitor(data As Byte(), offset As Integer, count As Integer) As Integer
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 Then
                Throw New ObjectDisposedException(NameOf(DSPThread))
            End If
            ' Auto-create default reader if not exists
            If Not postGainMonitorBuffer.HasReader("_default_postgain") Then
                postGainMonitorBuffer.CreateReader("_default_postgain")
            End If
            Return postGainMonitorBuffer.Read("_default_postgain", data, offset, count)
        End Function

        ''' <summary>
        ''' Reads audio from POST-OUTPUT-GAIN monitor buffer (Phase 2.5)
        ''' NON-BLOCKING: For output meters - shows audio after OUTPUT gain/pan adjustments
        ''' DEPRECATED: Use CreateTapReader() and ReadFromTap() for multi-reader support
        ''' </summary>
        Public Function ReadPostOutputGainMonitor(data As Byte(), offset As Integer, count As Integer) As Integer
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 Then
                Throw New ObjectDisposedException(NameOf(DSPThread))
            End If
            ' Auto-create default reader if not exists
            If Not postOutputGainMonitorBuffer.HasReader("_default_postoutputgain") Then
                postOutputGainMonitorBuffer.CreateReader("_default_postoutputgain")
            End If
            Return postOutputGainMonitorBuffer.Read("_default_postoutputgain", data, offset, count)
        End Function


        ''' <summary>
        ''' Gets the number of bytes available in the output buffer
        ''' </summary>
        Public Function OutputAvailable() As Integer
            Return outputBuffer.Available
        End Function

        ''' <summary>
        ''' Gets the number of bytes available in the INPUT monitor buffer
        ''' </summary>
        Public Function InputMonitorAvailable() As Integer
            ' Use default reader or return total available
            If inputMonitorBuffer.HasReader("_default_input") Then
                Return inputMonitorBuffer.Available("_default_input")
            End If
            Return 0 ' No reader yet
        End Function

        ''' <summary>
        ''' Gets the number of bytes available in the OUTPUT monitor buffer
        ''' </summary>
        Public Function OutputMonitorAvailable() As Integer
            ' Use default reader or return total available
            If outputMonitorBuffer.HasReader("_default_output") Then
                Return outputMonitorBuffer.Available("_default_output")
            End If
            Return 0 ' No reader yet
        End Function

        ''' <summary>
        ''' Gets the number of bytes available in the POST-GAIN monitor buffer (DSP tap point)
        ''' </summary>
        Public Function PostGainMonitorAvailable() As Integer
            ' Use default reader or return total available
            If postGainMonitorBuffer.HasReader("_default_postgain") Then
                Return postGainMonitorBuffer.Available("_default_postgain")
            End If
            Return 0 ' No reader yet
        End Function

        ''' <summary>
        ''' Gets the number of bytes available in the POST-OUTPUT-GAIN monitor buffer (Phase 2.5)
        ''' </summary>
        Public Function PostOutputGainMonitorAvailable() As Integer
            ' Use default reader or return total available
            If postOutputGainMonitorBuffer.HasReader("_default_postoutputgain") Then
                Return postOutputGainMonitorBuffer.Available("_default_postoutputgain")
            End If
            Return 0 ' No reader yet
        End Function

        ''' <summary>
        ''' Gets the number of bytes available in the input buffer
        ''' </summary>
        Public Function InputAvailable() As Integer
            Return inputBuffer.Available
        End Function

        ''' <summary>
        ''' Clears all buffers
        ''' </summary>
        Public Sub ClearBuffers()
            inputBuffer?.Clear()
            outputBuffer?.Clear()
            workBuffer?.Clear()
        End Sub

        ''' <summary>
        ''' Resets the processor chain
        ''' </summary>
        Public Sub ResetProcessors()
            processorChain?.Reset()
        End Sub

#Region "Multi-Reader Tap Point API (Phase 2.5 - Architecture Rule #4)"

        ''' <summary>
        ''' Tap point locations for flexible instrument routing
        ''' Maps to TapPoint enum in PipelineConfiguration
        ''' </summary>
        Public Enum TapLocation
            PreDSP = 1        ' Raw input (before any processing)
            PostGain = 2      ' After INPUT gain stage
            PostDSP = 3       ' After OUTPUT gain stage (final processed)
            PreOutput = 4     ' Before final output (same as PostDSP currently)
        End Enum

        ''' <summary>
        ''' Create an independent reader cursor for a specific tap point
        ''' Enables multiple instruments to read same audio without contention
        ''' </summary>
        ''' <param name="tapLocation">Which tap point to read from</param>
        ''' <param name="readerName">Unique name for this reader (e.g., "InputFFT", "OutputMeter")</param>
        ''' <returns>Reader handle for use in ReadFromTap()</returns>
        Public Function CreateTapReader(tapLocation As TapLocation, readerName As String) As String
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 Then
                Throw New ObjectDisposedException(NameOf(DSPThread))
            End If

            Select Case tapLocation
                Case TapLocation.PreDSP
                    Return inputMonitorBuffer.CreateReader(readerName)
                Case TapLocation.PostGain
                    Return postGainMonitorBuffer.CreateReader(readerName)
                Case TapLocation.PostDSP
                    Return postOutputGainMonitorBuffer.CreateReader(readerName)
                Case TapLocation.PreOutput
                    Return outputMonitorBuffer.CreateReader(readerName)
                Case Else
                    Throw New ArgumentException($"Invalid tap location: {tapLocation}")
            End Select
        End Function

        ''' <summary>
        ''' Remove a reader cursor from a tap point
        ''' </summary>
        Public Sub RemoveTapReader(tapLocation As TapLocation, readerName As String)
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 OrElse String.IsNullOrWhiteSpace(readerName) Then Return

            Select Case tapLocation
                Case TapLocation.PreDSP
                    inputMonitorBuffer.RemoveReader(readerName)
                Case TapLocation.PostGain
                    postGainMonitorBuffer.RemoveReader(readerName)
                Case TapLocation.PostDSP
                    postOutputGainMonitorBuffer.RemoveReader(readerName)
                Case TapLocation.PreOutput
                    outputMonitorBuffer.RemoveReader(readerName)
            End Select
        End Sub

        ''' <summary>
        ''' Read audio from a specific tap point using a reader cursor
        ''' Each reader maintains independent position
        ''' </summary>
        Public Function ReadFromTap(tapLocation As TapLocation, readerName As String, buffer As Byte(), offset As Integer, count As Integer) As Integer
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 Then
                Throw New ObjectDisposedException(NameOf(DSPThread))
            End If

            Select Case tapLocation
                Case TapLocation.PreDSP
                    Return inputMonitorBuffer.Read(readerName, buffer, offset, count)
                Case TapLocation.PostGain
                    Return postGainMonitorBuffer.Read(readerName, buffer, offset, count)
                Case TapLocation.PostDSP
                    Return postOutputGainMonitorBuffer.Read(readerName, buffer, offset, count)
                Case TapLocation.PreOutput
                    Return outputMonitorBuffer.Read(readerName, buffer, offset, count)
                Case Else
                    Throw New ArgumentException($"Invalid tap location: {tapLocation}")
            End Select
        End Function

        ''' <summary>
        ''' Get bytes available for a specific reader at a tap point
        ''' </summary>
        Public Function TapAvailable(tapLocation As TapLocation, readerName As String) As Integer
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 OrElse String.IsNullOrWhiteSpace(readerName) Then Return 0

            Select Case tapLocation
                Case TapLocation.PreDSP
                    Return inputMonitorBuffer.Available(readerName)
                Case TapLocation.PostGain
                    Return postGainMonitorBuffer.Available(readerName)
                Case TapLocation.PostDSP
                    Return postOutputGainMonitorBuffer.Available(readerName)
                Case TapLocation.PreOutput
                    Return outputMonitorBuffer.Available(readerName)
                Case Else
                    Return 0
            End Select
        End Function

        ''' <summary>
        ''' Check if a reader exists at a tap point
        ''' </summary>
        Public Function HasTapReader(tapLocation As TapLocation, readerName As String) As Boolean
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 OrElse String.IsNullOrWhiteSpace(readerName) Then Return False

            Select Case tapLocation
                Case TapLocation.PreDSP
                    Return inputMonitorBuffer.HasReader(readerName)
                Case TapLocation.PostGain
                    Return postGainMonitorBuffer.HasReader(readerName)
                Case TapLocation.PostDSP
                    Return postOutputGainMonitorBuffer.HasReader(readerName)
                Case TapLocation.PreOutput
                    Return outputMonitorBuffer.HasReader(readerName)
                Case Else
                    Return False
            End Select
        End Function

#End Region

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' DSP worker thread loop - keeps output buffer filled ahead of WaveOut
        ''' Rate-matched to audio playback speed (not unlimited processing)
        ''' </summary>
        Private Sub WorkerLoop()
            Interlocked.Exchange(_isRunningFlag, 1) ' Set to True
            Dim cycleCount As Long = 0

            ' Calculate target delay to match sample rate
            ' 256 samples at 44.1kHz = 5.8ms per block
            Dim samplesPerBlock As Integer = 256
            Dim targetMsPerBlock As Double = (samplesPerBlock / Format.SampleRate) * 1000.0
            Dim targetTicksPerBlock As Long = CLng(targetMsPerBlock * TimeSpan.TicksPerMillisecond)

            Dim stopwatch = Diagnostics.Stopwatch.StartNew()
            Dim nextProcessTime As Long = targetTicksPerBlock

            Try
                While Interlocked.CompareExchange(_shouldStop, 0, 0) = 0 ' While False
                    ' Check if we have enough input data to process
                    If inputBuffer.Available >= workBuffer.Capacity Then
                        ' Check if it's time to process next block (rate limiting)
                        Dim currentTime = stopwatch.ElapsedTicks
                        If currentTime >= nextProcessTime Then
                            ' Process one block
                            Dim bytesRead = inputBuffer.Read(workBuffer.Buffer, 0, workBuffer.Capacity)
                            If bytesRead > 0 Then
                                workBuffer.ByteCount = bytesRead

                                ' COPY to INPUT monitor buffer (BEFORE processing - raw audio)
                                inputMonitorBuffer.Write(workBuffer.Buffer, 0, bytesRead)

                                ' Process through chain
                                Try
                                    processorChain.Process(workBuffer)
                                Catch
                                    ' Silently continue
                                End Try

                                ' Write to output buffer
                                Dim bytesWritten = outputBuffer.Write(workBuffer.Buffer, 0, workBuffer.ByteCount)

                                ' COPY to OUTPUT monitor buffer (AFTER processing - processed audio)
                                outputMonitorBuffer.Write(workBuffer.Buffer, 0, workBuffer.ByteCount)

                                ' Track stats
                                Interlocked.Add(_processedSamples, bytesRead \ Format.BlockAlign)

                                If bytesWritten < workBuffer.ByteCount Then
                                    Dim dropped = (workBuffer.ByteCount - bytesWritten) \ Format.BlockAlign
                                    Interlocked.Add(_droppedSamples, dropped)
                                End If

                                cycleCount += 1

                                ' Schedule next process time
                                nextProcessTime += targetTicksPerBlock
                            End If
                        Else
                            ' Not time yet - brief sleep
                            Thread.Sleep(1)
                        End If
                    Else
                        ' Not enough input - signal feeder
                        Dim inputFillPercent = (inputBuffer.Available * 100) \ inputBuffer.Capacity
                        If inputFillPercent < 50 Then
                            inputLowEvent.Set()
                        End If

                        Thread.Sleep(1) ' Wait for more data
                    End If
                End While

            Catch ex As ThreadAbortException
                ' Exit cleanly
            Catch ex As Exception
                Utils.Logger.Instance.Error("DSP worker thread crashed", ex, "DSPThread")
            Finally
                Interlocked.Exchange(_isRunningFlag, 0) ' Set to False
                Utils.Logger.Instance.Info($"DSP worker stopped. Cycles={cycleCount}, Processed={ProcessedSamples}, Dropped={DroppedSamples}", "DSPThread")
            End Try
        End Sub

#End Region

#Region "IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Thread-safe double-dispose protection using Interlocked.CompareExchange
            ' If _disposed was 0 (False) and we set it to 1 (True), we proceed with disposal
            ' If _disposed was already 1 (True), CompareExchange returns 1 and we skip disposal
            If Interlocked.CompareExchange(_disposed, 1, 0) = 0 Then
                [Stop]()

                inputBuffer?.Dispose()
                outputBuffer?.Dispose()
                inputMonitorBuffer?.Dispose()
                outputMonitorBuffer?.Dispose()
                workBuffer?.Dispose()
                processorChain?.Dispose()
                inputLowEvent?.Dispose()

                Utils.Logger.Instance.Info($"DSP disposed. Processed: {ProcessedSamples}, Dropped: {DroppedSamples}", "DSPThread")
            End If
        End Sub

#End Region

    End Class

End Namespace
