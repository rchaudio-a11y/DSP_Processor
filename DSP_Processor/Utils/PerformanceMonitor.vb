Imports System.Diagnostics
Imports System.Threading

Namespace Utils

    ''' <summary>
    ''' Performance monitoring for CPU, memory, and audio metrics
    ''' Thread-safe singleton pattern
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 0, Task 0.3.2
    ''' Purpose: Track performance metrics for DSP development and debugging
    ''' </remarks>
    Public NotInheritable Class PerformanceMonitor

        Private Shared ReadOnly _instance As New Lazy(Of PerformanceMonitor)(Function() New PerformanceMonitor())
        Private ReadOnly lockObj As New Object()

        Private cpuCounter As PerformanceCounter
        Private memoryCounter As PerformanceCounter
        Private isMonitoring As Boolean = False
        Private monitoringThread As Thread

        ' Counters
        Private bufferUnderrunCountValue As Integer = 0
        Private bufferOverrunCountValue As Integer = 0
        Private audioLatencyMsValue As Double = 0

#Region "Properties"

        ''' <summary>
        ''' Gets the singleton instance
        ''' </summary>
        Public Shared ReadOnly Property Instance As PerformanceMonitor
            Get
                Return _instance.Value
            End Get
        End Property

        ''' <summary>
        ''' Gets current CPU usage percentage (0-100)
        ''' </summary>
        Public ReadOnly Property CpuUsagePercent As Double
            Get
                Try
                    Return If(cpuCounter?.NextValue(), 0)
                Catch
                    Return 0
                End Try
            End Get
        End Property

        ''' <summary>
        ''' Gets current memory usage in MB
        ''' </summary>
        Public ReadOnly Property MemoryUsageMB As Double
            Get
                Return Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0)
            End Get
        End Property

        ''' <summary>
        ''' Gets private bytes memory usage in MB
        ''' </summary>
        Public ReadOnly Property PrivateBytesMB As Double
            Get
                Return Process.GetCurrentProcess().PrivateMemorySize64 / (1024.0 * 1024.0)
            End Get
        End Property

        ''' <summary>
        ''' Gets managed heap memory in MB
        ''' </summary>
        Public ReadOnly Property GCTotalMemoryMB As Double
            Get
                Return GC.GetTotalMemory(False) / (1024.0 * 1024.0)
            End Get
        End Property

        ''' <summary>
        ''' Gets or sets the reported audio latency in milliseconds
        ''' </summary>
        Public Property AudioLatencyMs As Double
            Get
                SyncLock lockObj
                    Return audioLatencyMsValue
                End SyncLock
            End Get
            Set(value As Double)
                SyncLock lockObj
                    audioLatencyMsValue = value
                End SyncLock
            End Set
        End Property

        ''' <summary>
        ''' Gets the number of buffer underruns detected
        ''' </summary>
        Public ReadOnly Property BufferUnderrunCount As Integer
            Get
                SyncLock lockObj
                    Return bufferUnderrunCountValue
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the number of buffer overruns detected
        ''' </summary>
        Public ReadOnly Property BufferOverrunCount As Integer
            Get
                SyncLock lockObj
                    Return bufferOverrunCountValue
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets whether monitoring is active
        ''' </summary>
        Public ReadOnly Property IsMonitoringActive As Boolean
            Get
                Return isMonitoring
            End Get
        End Property

#End Region

#Region "Constructor"

        Private Sub New()
            Try
                ' Initialize performance counters
                Dim processName = Process.GetCurrentProcess().ProcessName
                cpuCounter = New PerformanceCounter("Processor", "% Processor Time", "_Total")
                ' Note: Memory counter not needed as we use Process API directly
            Catch ex As Exception
                ' Performance counters may not be available - continue without them
                Logger.Instance.Warning($"Failed to initialize performance counters: {ex.Message}", "PerformanceMonitor")
            End Try
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Start continuous monitoring (updates metrics periodically)
        ''' </summary>
        Public Sub StartMonitoring()
            If isMonitoring Then Return

            isMonitoring = True
            monitoringThread = New Thread(AddressOf MonitoringLoop)
            monitoringThread.IsBackground = True
            monitoringThread.Name = "PerformanceMonitor"
            monitoringThread.Start()

            Logger.Instance.Info("Performance monitoring started", "PerformanceMonitor")
        End Sub

        ''' <summary>
        ''' Stop continuous monitoring
        ''' </summary>
        Public Sub StopMonitoring()
            If Not isMonitoring Then Return

            isMonitoring = False
            monitoringThread?.Join(1000) ' Wait up to 1 second for thread to exit

            Logger.Instance.Info("Performance monitoring stopped", "PerformanceMonitor")
        End Sub

        ''' <summary>
        ''' Reset all counters to zero
        ''' </summary>
        Public Sub ResetCounters()
            SyncLock lockObj
                bufferUnderrunCountValue = 0
                bufferOverrunCountValue = 0
                audioLatencyMsValue = 0
            End SyncLock

            Logger.Instance.Debug("Performance counters reset", "PerformanceMonitor")
        End Sub

        ''' <summary>
        ''' Record a buffer underrun event
        ''' </summary>
        Public Sub RecordBufferUnderrun()
            SyncLock lockObj
                bufferUnderrunCountValue += 1
            End SyncLock

            Logger.Instance.Warning($"Buffer underrun detected (total: {BufferUnderrunCount})", "PerformanceMonitor")
            RaisePerformanceWarning("Buffer Underrun", "Audio buffer starved - CPU may be overloaded")
        End Sub

        ''' <summary>
        ''' Record a buffer overrun event
        ''' </summary>
        Public Sub RecordBufferOverrun()
            SyncLock lockObj
                bufferOverrunCountValue += 1
            End SyncLock

            Logger.Instance.Warning($"Buffer overrun detected (total: {BufferOverrunCount})", "PerformanceMonitor")
            RaisePerformanceWarning("Buffer Overrun", "Audio buffer overflow - processing too slow")
        End Sub

        ''' <summary>
        ''' Get current performance snapshot as formatted string
        ''' </summary>
        Public Function GetSnapshot() As String
            Return $"CPU: {CpuUsagePercent:F1}%, Memory: {MemoryUsageMB:F1}MB, Latency: {AudioLatencyMs:F1}ms, Underruns: {BufferUnderrunCount}, Overruns: {BufferOverrunCount}"
        End Function

#End Region

#Region "Events"

        ''' <summary>
        ''' Raised when performance metrics exceed warning thresholds
        ''' </summary>
        Public Event PerformanceWarning As EventHandler(Of PerformanceEventArgs)

        Private Sub RaisePerformanceWarning(warningType As String, message As String)
            RaiseEvent PerformanceWarning(Me, New PerformanceEventArgs(warningType, message))
        End Sub

#End Region

#Region "Private Methods"

        Private Sub MonitoringLoop()
            While isMonitoring
                Try
                    ' Check for warning conditions
                    Dim cpu = CpuUsagePercent
                    Dim memory = MemoryUsageMB

                    ' Warn if CPU >80%
                    If cpu > 80 Then
                        RaisePerformanceWarning("High CPU Usage", $"CPU usage at {cpu:F1}%")
                    End If

                    ' Warn if memory >500MB
                    If memory > 500 Then
                        RaisePerformanceWarning("High Memory Usage", $"Memory usage at {memory:F1}MB")
                    End If

                    ' Log snapshot every 10 seconds
                    Static lastLogTime As DateTime = DateTime.MinValue
                    If DateTime.Now.Subtract(lastLogTime).TotalSeconds >= 10 Then
                        Logger.Instance.Debug(GetSnapshot(), "PerformanceMonitor")
                        lastLogTime = DateTime.Now
                    End If

                Catch ex As Exception
                    Logger.Instance.Error("Error in monitoring loop", ex, "PerformanceMonitor")
                End Try

                ' Sleep for 1 second
                Thread.Sleep(1000)
            End While
        End Sub

#End Region

    End Class

    ''' <summary>
    ''' Event arguments for performance warnings
    ''' </summary>
    Public Class PerformanceEventArgs
        Inherits EventArgs

        Public Property WarningType As String
        Public Property Message As String

        Public Sub New(warningType As String, message As String)
            Me.WarningType = warningType
            Me.Message = message
        End Sub

    End Class

End Namespace
