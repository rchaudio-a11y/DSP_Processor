''' <summary>
''' TheForge-compatible logging service interface
''' Copied from TheForge project for compatibility
''' </summary>
Namespace Services.Interfaces

    ''' <summary>
    ''' Interface for logging service operations.
    ''' </summary>
    Public Interface ILoggingService
        ''' <summary>
        ''' Event raised when a log message is recorded.
        ''' </summary>
        Event LogMessageReceived As EventHandler(Of LogMessageEventArgs)

        ''' <summary>
        ''' Gets or sets the minimum log level filter. Messages below this level are not raised.
        ''' </summary>
        Property FilterLevel As LogLevel?

        ''' <summary>
        ''' Gets or sets the search text filter. Only messages containing this text are raised.
        ''' </summary>
        Property SearchText As String

        ''' <summary>
        ''' Logs an informational message.
        ''' </summary>
        Sub LogInfo(message As String)

        ''' <summary>
        ''' Logs a warning message.
        ''' </summary>
        Sub LogWarning(message As String)

        ''' <summary>
        ''' Logs an error message.
        ''' </summary>
        Sub LogError(message As String)

        ''' <summary>
        ''' Clears all log entries.
        ''' </summary>
        Sub ClearLog()

        ''' <summary>
        ''' Gets all log entries.
        ''' </summary>
        Function GetLogEntries() As List(Of String)
    End Interface

    ''' <summary>
    ''' Event arguments for log message events.
    ''' </summary>
    Public Class LogMessageEventArgs
        Inherits EventArgs

        Public Property Message As String
        Public Property Level As LogLevel

        Public Sub New(message As String, level As LogLevel)
            Me.Message = message
            Me.Level = level
        End Sub
    End Class

    ''' <summary>
    ''' Log message severity levels.
    ''' </summary>
    Public Enum LogLevel
        Info = 0
        Warning = 1
        [Error] = 2
    End Enum

End Namespace
