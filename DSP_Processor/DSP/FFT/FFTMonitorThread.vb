Imports System.Threading
Imports DSP_Processor.Audio.Routing
Imports DSP_Processor.Utils

Namespace DSP.FFT

    ''' <summary>
    ''' Freewheeling FFT thread that continuously monitors audio pipeline stages.
    ''' Runs independently from audio thread - NEVER blocks audio processing!
    ''' 
    ''' Architecture:
    ''' - Polls Input Stage and Output Stage for new data
    ''' - Calculates FFT when data available
    ''' - Throttles UI updates to 20 FPS (50ms)
    ''' - Runs at lower priority than audio thread
    ''' - Clean shutdown on Stop()
    ''' </summary>
    Public Class FFTMonitorThread
        Implements IDisposable

#Region "Private Fields"

        ' Thread control
        Private _running As Boolean = False
        Private _workerThread As Thread
        Private ReadOnly _startStopLock As New Object()

        ' Pipeline stages to monitor
        Private ReadOnly _inputStage As AudioStage
        Private ReadOnly _outputStage As AudioStage

        ' FFT processors (one for each tap point)
        Private ReadOnly _fftProcessorInput As FFTProcessor
        Private ReadOnly _fftProcessorOutput As FFTProcessor

        ' UI update throttling (20 FPS = 50ms per frame)
        Private _lastInputUpdateTime As DateTime = DateTime.MinValue
        Private _lastOutputUpdateTime As DateTime = DateTime.MinValue
        Private ReadOnly _uiUpdateIntervalMs As Integer = 50

        ' Performance tracking
        Private _totalBuffersProcessed As Long = 0
        Private _totalFFTsCalculated As Long = 0

#End Region

#Region "Public Events"

        ''' <summary>
        ''' Raised when a spectrum is ready for display (throttled to 20 FPS).
        ''' Already on background thread - UI must invoke to update controls.
        ''' </summary>
        Public Event SpectrumReady As EventHandler(Of SpectrumReadyEventArgs)

#End Region

#Region "Public Properties"

        ''' <summary>Check if FFT monitor thread is running</summary>
        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _running
            End Get
        End Property

        ''' <summary>Total buffers processed (for diagnostics)</summary>
        Public ReadOnly Property TotalBuffersProcessed As Long
            Get
                Return _totalBuffersProcessed
            End Get
        End Property

        ''' <summary>Total FFTs calculated (for diagnostics)</summary>
        Public ReadOnly Property TotalFFTsCalculated As Long
            Get
                Return _totalFFTsCalculated
            End Get
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Create a new FFT monitor thread.
        ''' </summary>
        ''' <param name="inputStage">Input stage to monitor (Pre-DSP tap)</param>
        ''' <param name="outputStage">Output stage to monitor (Post-DSP tap)</param>
        Public Sub New(inputStage As AudioStage, outputStage As AudioStage)
            If inputStage Is Nothing Then
                Throw New ArgumentNullException(NameOf(inputStage))
            End If
            If outputStage Is Nothing Then
                Throw New ArgumentNullException(NameOf(outputStage))
            End If

            _inputStage = inputStage
            _outputStage = outputStage

            ' Create FFT processors (4096 samples, Hann window)
            _fftProcessorInput = New FFTProcessor(4096) With {
                .SampleRate = 44100,
                .WindowFunction = FFTProcessor.WindowType.Hann
            }

            _fftProcessorOutput = New FFTProcessor(4096) With {
                .SampleRate = 44100,
                .WindowFunction = FFTProcessor.WindowType.Hann
            }

            Logger.Instance.Info("FFTMonitorThread created", "FFTMonitorThread")
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Start the freewheeling FFT monitor thread.
        ''' Thread-safe - can be called multiple times.
        ''' </summary>
        Public Sub Start()
            SyncLock _startStopLock
                If _running Then
                    Logger.Instance.Warning("FFT monitor thread already running", "FFTMonitorThread")
                    Return
                End If

                _running = True

                ' Create and start background thread
                _workerThread = New Thread(AddressOf WorkerLoop) With {
                    .IsBackground = True,
                    .Priority = ThreadPriority.BelowNormal,  ' Lower than audio thread!
                    .Name = "FFT Monitor Thread"
                }
                _workerThread.Start()

                Logger.Instance.Info("FFT monitor thread started", "FFTMonitorThread")
            End SyncLock
        End Sub

        ''' <summary>
        ''' Stop the FFT monitor thread.
        ''' Thread-safe - waits for clean shutdown (max 1 second).
        ''' </summary>
        Public Sub [Stop]()
            SyncLock _startStopLock
                If Not _running Then
                    Return
                End If

                Logger.Instance.Info("Stopping FFT monitor thread...", "FFTMonitorThread")

                ' Signal thread to stop
                _running = False

                ' Wait for thread to finish (max 1 second)
                If _workerThread IsNot Nothing AndAlso _workerThread.IsAlive Then
                    If Not _workerThread.Join(1000) Then
                        Logger.Instance.Warning("FFT monitor thread did not stop gracefully", "FFTMonitorThread")
                        ' Note: Don't abort - let it finish naturally
                    End If
                End If

                Logger.Instance.Info($"FFT monitor thread stopped. Total: {_totalBuffersProcessed} buffers, {_totalFFTsCalculated} FFTs", "FFTMonitorThread")
            End SyncLock
        End Sub

#End Region

#Region "Private Methods - Worker Thread"

        ''' <summary>
        ''' Main worker loop - runs continuously until Stop() is called.
        ''' Polls both input and output stages for new data.
        ''' </summary>
        Private Sub WorkerLoop()
            Logger.Instance.Debug("FFT monitor thread worker loop started", "FFTMonitorThread")

            While _running
                Try
                    ' === CHECK INPUT STAGE (Pre-DSP) ===
                    If _inputStage.HasMonitorData Then
                        Dim tapData = _inputStage.GetMonitorTapWithMetadata()
                        If tapData IsNot Nothing AndAlso tapData.Buffer IsNot Nothing Then
                            ProcessFFT(tapData, TapPoint.PreDSP, _fftProcessorInput, _lastInputUpdateTime)
                            _inputStage.ClearMonitorFlag()
                            Interlocked.Increment(_totalBuffersProcessed)
                        End If
                    End If

                    ' === CHECK OUTPUT STAGE (Post-DSP) ===
                    If _outputStage.HasMonitorData Then
                        Dim tapData = _outputStage.GetMonitorTapWithMetadata()
                        If tapData IsNot Nothing AndAlso tapData.Buffer IsNot Nothing Then
                            ProcessFFT(tapData, TapPoint.PostDSP, _fftProcessorOutput, _lastOutputUpdateTime)
                            _outputStage.ClearMonitorFlag()
                            Interlocked.Increment(_totalBuffersProcessed)
                        End If
                    End If

                    ' Small yield to prevent CPU spinning (10ms = 100 Hz polling rate)
                    Thread.Sleep(10)

                Catch ex As ThreadAbortException
                    ' Thread is being stopped - exit gracefully
                    Logger.Instance.Info("FFT monitor thread aborted", "FFTMonitorThread")
                    Exit While

                Catch ex As Exception
                    ' Log but don't crash the thread
                    Logger.Instance.Error("Error in FFT monitor thread", ex, "FFTMonitorThread")
                    ' Continue running despite error
                End Try
            End While

            Logger.Instance.Debug("FFT monitor thread worker loop exited", "FFTMonitorThread")
        End Sub

        ''' <summary>
        ''' Process FFT for a monitor tap and raise event if UI update is needed.
        ''' Throttles UI updates to 20 FPS (50ms interval).
        ''' </summary>
        Private Sub ProcessFFT(tapData As MonitorTapData,
                              tapPoint As TapPoint,
                              processor As FFTProcessor,
                              ByRef lastUpdateTime As DateTime)

            Try
                ' Update FFT processor metadata
                processor.SampleRate = tapData.SampleRate

                ' Add samples and calculate FFT (CPU-intensive operation)
                processor.AddSamples(tapData.Buffer, tapData.Buffer.Length, tapData.BitsPerSample, tapData.Channels)
                Dim spectrum = processor.CalculateSpectrum()

                Interlocked.Increment(_totalFFTsCalculated)

                ' Check if spectrum is valid
                If spectrum Is Nothing OrElse spectrum.Length = 0 Then
                    Return
                End If

                ' === THROTTLE UI UPDATES (20 FPS = 50ms) ===
                Dim now = DateTime.Now
                If (now - lastUpdateTime).TotalMilliseconds >= _uiUpdateIntervalMs Then

                    ' Raise event for UI update
                    RaiseEvent SpectrumReady(Me, New SpectrumReadyEventArgs With {
                        .Spectrum = spectrum,
                        .TapPoint = tapPoint,
                        .SampleRate = tapData.SampleRate,
                        .FFTSize = processor.FFTSize,
                        .StageName = tapData.StageName
                    })

                    ' Update last UI update time (track separately for input/output)
                    lastUpdateTime = now
                End If

            Catch ex As Exception
                Logger.Instance.Error($"Error processing FFT for {tapPoint}", ex, "FFTMonitorThread")
            End Try
        End Sub

#End Region

#Region "IDisposable Implementation"

        Private _disposed As Boolean = False

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _disposed Then
                If disposing Then
                    ' Stop thread
                    [Stop]()

                    ' Clear FFT processors
                    _fftProcessorInput?.Clear()
                    _fftProcessorOutput?.Clear()
                End If
                _disposed = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

#End Region

    End Class

    ''' <summary>
    ''' Event args for SpectrumReady event.
    ''' Contains spectrum data and metadata for UI display.
    ''' </summary>
    Public Class SpectrumReadyEventArgs
        Inherits EventArgs

        ''' <summary>FFT spectrum data (magnitude values)</summary>
        Public Property Spectrum As Single()

        ''' <summary>Tap point (Pre-DSP or Post-DSP)</summary>
        Public Property TapPoint As TapPoint

        ''' <summary>Sample rate of the audio</summary>
        Public Property SampleRate As Integer

        ''' <summary>FFT size used</summary>
        Public Property FFTSize As Integer

        ''' <summary>Name of the stage (for debugging)</summary>
        Public Property StageName As String

    End Class

End Namespace
