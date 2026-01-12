Imports DSP_Processor.Services.Interfaces

Namespace Services

    ''' <summary>
    ''' Adapter that bridges DSP_Processor.Utils.Logger to ILoggingService
    ''' Provides unified logging interface with event-driven architecture
    ''' </summary>
    Public Class LoggingServiceAdapter
        Implements ILoggingService

        Private Shared ReadOnly _instance As New Lazy(Of LoggingServiceAdapter)(Function() New LoggingServiceAdapter())
        Private logEntries As New List(Of String)
        Private ReadOnly lockObj As New Object()

#Region "Singleton"

        ''' <summary>
        ''' Gets the singleton instance
        ''' </summary>
        Public Shared ReadOnly Property Instance As LoggingServiceAdapter
            Get
                Return _instance.Value
            End Get
        End Property

        Private Sub New()
            ' Private constructor for singleton
        End Sub

#End Region

#Region "ILoggingService Implementation"

        ''' <summary>
        ''' Event raised when a log message is recorded
        ''' </summary>
        Public Event LogMessageReceived As EventHandler(Of LogMessageEventArgs) Implements ILoggingService.LogMessageReceived

        ''' <summary>
        ''' Minimum log level filter (null = show all)
        ''' </summary>
        Public Property FilterLevel As LogLevel? Implements ILoggingService.FilterLevel

        ''' <summary>
        ''' Search text filter (null/empty = show all)
        ''' </summary>
        Public Property SearchText As String Implements ILoggingService.SearchText

        ''' <summary>
        ''' Log informational message
        ''' </summary>
        Public Sub LogInfo(message As String) Implements ILoggingService.LogInfo
            Log(message, LogLevel.Info, Utils.LogLevel.Info)
        End Sub

        ''' <summary>
        ''' Log warning message
        ''' </summary>
        Public Sub LogWarning(message As String) Implements ILoggingService.LogWarning
            Log(message, LogLevel.Warning, Utils.LogLevel.Warning)
        End Sub

        ''' <summary>
        ''' Log error message
        ''' </summary>
        Public Sub LogError(message As String) Implements ILoggingService.LogError
            Log(message, LogLevel.Error, Utils.LogLevel.Error)
        End Sub

        ''' <summary>
        ''' Clear all log entries
        ''' </summary>
        Public Sub ClearLog() Implements ILoggingService.ClearLog
            SyncLock lockObj
                logEntries.Clear()
            End SyncLock
        End Sub

        ''' <summary>
        ''' Get all log entries
        ''' </summary>
        Public Function GetLogEntries() As List(Of String) Implements ILoggingService.GetLogEntries
            SyncLock lockObj
                Return New List(Of String)(logEntries)
            End SyncLock
        End Function

#End Region

#Region "Extended Logging Methods"

        ''' <summary>
        ''' Log debug message (not in ILoggingService but useful)
        ''' </summary>
        Public Sub LogDebug(message As String)
            ' Debug level doesn't exist in TheForge.LogLevel, so map to Info
            Log(message, LogLevel.Info, Utils.LogLevel.Debug)
        End Sub

        ''' <summary>
        ''' Log critical error (not in ILoggingService but useful)
        ''' </summary>
        Public Sub LogCritical(message As String)
            ' Critical level doesn't exist in TheForge.LogLevel, so map to Error
            Log(message, LogLevel.Error, Utils.LogLevel.Critical)
        End Sub

        ''' <summary>
        ''' Log error with exception details
        ''' </summary>
        Public Sub LogError(message As String, ex As Exception)
            Dim fullMessage = $"{message}{vbCrLf}Exception: {ex.GetType().Name} - {ex.Message}"
            If Not String.IsNullOrEmpty(ex.StackTrace) Then
                fullMessage &= vbCrLf & "Stack Trace: " & ex.StackTrace
            End If
            Log(fullMessage, LogLevel.Error, Utils.LogLevel.Error)
        End Sub

#End Region

#Region "Private Methods"

        Private Sub Log(message As String, forgeLevel As LogLevel, utilsLevel As Utils.LogLevel)
            ' Format with timestamp
            Dim timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            Dim formattedMessage = $"[{timestamp}] [{forgeLevel.ToString().ToUpper()}] {message}"

            ' Add to in-memory log
            SyncLock lockObj
                logEntries.Add(formattedMessage)
                ' Keep last 1000 entries to prevent memory bloat
                If logEntries.Count > 1000 Then
                    logEntries.RemoveAt(0)
                End If
            End SyncLock

            ' Log to file via existing Logger
            Select Case utilsLevel
                Case Utils.LogLevel.Debug
                    Utils.Logger.Instance.Debug(message, "DSP")
                Case Utils.LogLevel.Info
                    Utils.Logger.Instance.Info(message, "DSP")
                Case Utils.LogLevel.Warning
                    Utils.Logger.Instance.Warning(message, "DSP")
                Case Utils.LogLevel.Error
                    Utils.Logger.Instance.Error(message, Nothing, "DSP")
                Case Utils.LogLevel.Critical
                    Utils.Logger.Instance.Critical(message, Nothing, "DSP")
            End Select

            ' Raise event if filters allow
            If ShouldRaiseEvent(message, forgeLevel) Then
                RaiseEvent LogMessageReceived(Me, New LogMessageEventArgs(formattedMessage, forgeLevel))
            End If
        End Sub

        Private Function ShouldRaiseEvent(message As String, level As LogLevel) As Boolean
            ' Check level filter
            If FilterLevel.HasValue AndAlso level < FilterLevel.Value Then
                Return False
            End If

            ' Check search text filter
            If Not String.IsNullOrEmpty(SearchText) AndAlso
               Not message.Contains(SearchText, StringComparison.OrdinalIgnoreCase) Then
                Return False
            End If

            Return True
        End Function

#End Region

    End Class

End Namespace
