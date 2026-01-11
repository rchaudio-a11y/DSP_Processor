Imports System.IO
Imports System.Text
Imports System.Threading

Namespace Utils

    ''' <summary>
    ''' Log severity levels
    ''' </summary>
    Public Enum LogLevel
        ''' <summary>Detailed debugging information</summary>
        Debug = 0
        ''' <summary>General informational messages</summary>
        Info = 1
        ''' <summary>Warning messages (non-critical issues)</summary>
        Warning = 2
        ''' <summary>Error messages (recoverable failures)</summary>
        [Error] = 3
        ''' <summary>Critical errors (application may fail)</summary>
        Critical = 4
    End Enum

    ''' <summary>
    ''' Centralized logging system with file rotation and performance tracking
    ''' Thread-safe singleton pattern
    ''' </summary>
    Public NotInheritable Class Logger

        Private Shared ReadOnly _instance As New Lazy(Of Logger)(Function() New Logger())
        Private ReadOnly lockObj As New Object()
        Private currentLogFile As String
        Private logWriter As StreamWriter

#Region "Properties"

        ''' <summary>
        ''' Gets the singleton Logger instance
        ''' </summary>
        Public Shared ReadOnly Property Instance As Logger
            Get
                Return _instance.Value
            End Get
        End Property

        ''' <summary>
        ''' Minimum log level to output (messages below this level are ignored)
        ''' </summary>
        Public Property MinimumLevel As LogLevel = LogLevel.Info

        ''' <summary>
        ''' Enable/disable logging to file
        ''' </summary>
        Public Property LogToFile As Boolean = True

        ''' <summary>
        ''' Enable/disable logging to console
        ''' </summary>
        Public Property LogToConsole As Boolean = True

        ''' <summary>
        ''' Directory for log files (relative to application path)
        ''' </summary>
        Public Property LogDirectory As String = "Logs"

        ''' <summary>
        ''' Maximum number of log files to keep (oldest deleted first)
        ''' </summary>
        Public Property MaxLogFiles As Integer = 10

        ''' <summary>
        ''' Maximum log file size in MB before rotation
        ''' </summary>
        Public Property MaxLogSizeMB As Integer = 10

#End Region

#Region "Constructor"

        Private Sub New()
            ' Private constructor for singleton
            EnsureLogDirectoryExists()
            OpenLogFile()
        End Sub

#End Region

#Region "Public Logging Methods"

        ''' <summary>
        ''' Log debug message (detailed development information)
        ''' </summary>
        Public Sub Debug(message As String, Optional context As String = Nothing)
            Log(LogLevel.Debug, message, Nothing, context)
        End Sub

        ''' <summary>
        ''' Log informational message
        ''' </summary>
        Public Sub Info(message As String, Optional context As String = Nothing)
            Log(LogLevel.Info, message, Nothing, context)
        End Sub

        ''' <summary>
        ''' Log warning message
        ''' </summary>
        Public Sub Warning(message As String, Optional context As String = Nothing)
            Log(LogLevel.Warning, message, Nothing, context)
        End Sub

        ''' <summary>
        ''' Log error message with optional exception
        ''' </summary>
        Public Sub [Error](message As String, Optional ex As Exception = Nothing, Optional context As String = Nothing)
            Log(LogLevel.Error, message, ex, context)
        End Sub

        ''' <summary>
        ''' Log critical error with optional exception
        ''' </summary>
        Public Sub Critical(message As String, Optional ex As Exception = Nothing, Optional context As String = Nothing)
            Log(LogLevel.Critical, message, ex, context)
        End Sub

        ''' <summary>
        ''' Start a performance timer (use with Using block for automatic logging)
        ''' </summary>
        ''' <param name="name">Operation name</param>
        ''' <returns>LogTimer that logs elapsed time when disposed</returns>
        ''' <example>
        ''' Using timer = Logger.Instance.StartTimer("Waveform Rendering")
        '''     ' ... code to time ...
        ''' End Using
        ''' ' Automatically logs: "[DEBUG] Waveform Rendering took 45ms"
        ''' </example>
        Public Function StartTimer(name As String) As LogTimer
            Return New LogTimer(name, Me)
        End Function

#End Region

#Region "Private Methods"

        Private Sub Log(level As LogLevel, message As String, ex As Exception, context As String)
            ' Check minimum level
            If level < MinimumLevel Then Return

            ' Format log entry
            Dim logEntry = FormatLogEntry(level, message, ex, context)

            ' Output to console
            If LogToConsole Then
                Console.WriteLine(logEntry)
            End If

            ' Output to file
            If LogToFile Then
                WriteToFile(logEntry)
            End If
        End Sub

        Private Function FormatLogEntry(level As LogLevel, message As String, ex As Exception, context As String) As String
            Dim sb As New StringBuilder()

            ' Timestamp
            sb.Append("[")
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            sb.Append("] ")

            ' Level
            sb.Append("[")
            sb.Append(level.ToString().ToUpper())
            sb.Append("] ")

            ' Context (optional)
            If Not String.IsNullOrEmpty(context) Then
                sb.Append("[")
                sb.Append(context)
                sb.Append("] ")
            End If

            ' Message
            sb.Append(message)

            ' Exception (if provided)
            If ex IsNot Nothing Then
                sb.AppendLine()
                sb.Append("  Exception: ")
                sb.Append(ex.GetType().Name)
                sb.Append(" - ")
                sb.Append(ex.Message)
                If Not String.IsNullOrEmpty(ex.StackTrace) Then
                    sb.AppendLine()
                    sb.Append("  Stack Trace: ")
                    sb.Append(ex.StackTrace.Replace(vbCrLf, vbCrLf & "    "))
                End If
            End If

            Return sb.ToString()
        End Function

        Private Sub WriteToFile(logEntry As String)
            SyncLock lockObj
                Try
                    ' Check if rotation needed
                    If NeedsRotation() Then
                        RotateLogFile()
                    End If

                    ' Write to file
                    If logWriter IsNot Nothing Then
                        logWriter.WriteLine(logEntry)
                    End If

                Catch ex As Exception
                    ' Failed to write log - output to console as fallback
                    Console.WriteLine($"[ERROR] Failed to write log: {ex.Message}")
                End Try
            End SyncLock
        End Sub

        Private Sub EnsureLogDirectoryExists()
            Dim fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogDirectory)
            If Not Directory.Exists(fullPath) Then
                Directory.CreateDirectory(fullPath)
            End If
        End Sub

        Private Sub OpenLogFile()
            Dim timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss")
            Dim fileName = $"DSP_Processor_{timestamp}.log"
            currentLogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogDirectory, fileName)

            Try
                logWriter = New StreamWriter(currentLogFile, append:=True)
                logWriter.AutoFlush = True
            Catch ex As Exception
                Console.WriteLine($"[ERROR] Failed to open log file: {ex.Message}")
            End Try
        End Sub

        Private Function NeedsRotation() As Boolean
            If String.IsNullOrEmpty(currentLogFile) Then Return False
            If Not File.Exists(currentLogFile) Then Return False

            Dim info As New FileInfo(currentLogFile)
            Return info.Length > (MaxLogSizeMB * 1024 * 1024)
        End Function

        Private Sub RotateLogFile()
            ' Close current file
            If logWriter IsNot Nothing Then
                logWriter.Close()
                logWriter.Dispose()
                logWriter = Nothing
            End If

            ' Open new file
            OpenLogFile()

            ' Clean up old files
            CleanupOldLogFiles()
        End Sub

        Private Sub CleanupOldLogFiles()
            Try
                Dim logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogDirectory)
                Dim files = Directory.GetFiles(logDir, "DSP_Processor_*.log") _
                    .Select(Function(f) New FileInfo(f)) _
                    .OrderByDescending(Function(fi) fi.CreationTime) _
                    .ToList()

                ' Delete oldest files if we exceed MaxLogFiles
                If files.Count > MaxLogFiles Then
                    For i = MaxLogFiles To files.Count - 1
                        Try
                            files(i).Delete()
                        Catch
                            ' Ignore errors deleting old logs
                        End Try
                    Next
                End If

            Catch ex As Exception
                Console.WriteLine($"[ERROR] Failed to cleanup old logs: {ex.Message}")
            End Try
        End Sub

#End Region

#Region "Public Utility Methods"

        ''' <summary>
        ''' Flush buffered log entries to disk
        ''' </summary>
        Public Sub Flush()
            SyncLock lockObj
                logWriter?.Flush()
            End SyncLock
        End Sub

        ''' <summary>
        ''' Close the log file (call on application shutdown)
        ''' </summary>
        Public Sub Close()
            SyncLock lockObj
                If logWriter IsNot Nothing Then
                    logWriter.Close()
                    logWriter.Dispose()
                    logWriter = Nothing
                End If
            End SyncLock
        End Sub

#End Region

    End Class

    ''' <summary>
    ''' Disposable timer for automatic performance logging
    ''' Use with Using block for RAII pattern
    ''' </summary>
    Public Class LogTimer
        Implements IDisposable

        Private ReadOnly name As String
        Private ReadOnly logger As Logger
        Private ReadOnly startTime As DateTime

        Friend Sub New(name As String, logger As Logger)
            Me.name = name
            Me.logger = logger
            Me.startTime = DateTime.Now
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dim elapsed = DateTime.Now.Subtract(startTime)
            logger.Debug($"{name} took {elapsed.TotalMilliseconds:F2}ms")
        End Sub

    End Class

End Namespace
