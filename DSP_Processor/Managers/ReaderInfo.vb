Imports System.Threading

Namespace Managers

    ''' <summary>
    ''' Information about a registered tap point reader
    ''' Tracks creation, usage, and health metrics
    ''' Thread-safe: All properties use Interlocked for thread safety
    ''' </summary>
    Public Class ReaderInfo

        ' Immutable identity
        Private ReadOnly _name As String
        Private ReadOnly _tapPoint As DSP.TapPoint
        Private ReadOnly _owner As String
        Private ReadOnly _createdAt As DateTime

        ' Mutable stats (thread-safe using Interlocked)
        Private _lastReadAt As Long ' DateTime.Ticks
        Private _bytesRead As Long
        Private _readCount As Long

#Region "Properties"

        ''' <summary>Unique reader name (validated at creation)</summary>
        Public ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        ''' <summary>Which tap point this reader reads from</summary>
        Public ReadOnly Property TapPoint As DSP.TapPoint
            Get
                Return _tapPoint
            End Get
        End Property

        ''' <summary>Owner component (e.g., "MainForm", "FFT", "Analyzer")</summary>
        Public ReadOnly Property Owner As String
            Get
                Return _owner
            End Get
        End Property

        ''' <summary>When this reader was created</summary>
        Public ReadOnly Property CreatedAt As DateTime
            Get
                Return _createdAt
            End Get
        End Property

        ''' <summary>
        ''' Last time this reader successfully read data (thread-safe)
        ''' </summary>
        Public Property LastReadAt As DateTime
            Get
                Dim ticks = Interlocked.Read(_lastReadAt)
                If ticks = 0 Then Return DateTime.MinValue
                Return New DateTime(ticks)
            End Get
            Set(value As DateTime)
                Interlocked.Exchange(_lastReadAt, value.Ticks)
            End Set
        End Property

        ''' <summary>
        ''' Total bytes read by this reader (thread-safe)
        ''' </summary>
        Public Property BytesRead As Long
            Get
                Return Interlocked.Read(_bytesRead)
            End Get
            Set(value As Long)
                Interlocked.Exchange(_bytesRead, value)
            End Set
        End Property

        ''' <summary>
        ''' Total number of successful reads (thread-safe)
        ''' </summary>
        Public Property ReadCount As Long
            Get
                Return Interlocked.Read(_readCount)
            End Get
            Set(value As Long)
                Interlocked.Exchange(_readCount, value)
            End Set
        End Property

        ''' <summary>
        ''' Time since last read (thread-safe)
        ''' </summary>
        Public ReadOnly Property TimeSinceLastRead As TimeSpan
            Get
                Dim lastRead = LastReadAt
                If lastRead = DateTime.MinValue Then
                    Return TimeSpan.MaxValue ' Never read
                End If
                Return DateTime.Now - lastRead
            End Get
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Create new reader info
        ''' </summary>
        Public Sub New(name As String, tapPoint As DSP.TapPoint, owner As String)
            _name = name
            _tapPoint = tapPoint
            _owner = owner
            _createdAt = DateTime.Now
            _lastReadAt = 0 ' Not read yet
            _bytesRead = 0
            _readCount = 0
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Record a successful read (thread-safe)
        ''' </summary>
        ''' <param name="bytesReadNow">Bytes read in this operation</param>
        Public Sub RecordRead(bytesReadNow As Integer)
            ' Update last read time
            Interlocked.Exchange(_lastReadAt, DateTime.Now.Ticks)

            ' Increment counters
            Interlocked.Add(_bytesRead, bytesReadNow)
            Interlocked.Increment(_readCount)
        End Sub

        ''' <summary>
        ''' Get current health status based on last read time
        ''' </summary>
        Public Function GetHealth() As ReaderHealth
            Dim timeSince = TimeSinceLastRead

            ' Never read yet
            If timeSince = TimeSpan.MaxValue Then
                Return ReaderHealth.Unknown
            End If

            ' Health thresholds
            If timeSince.TotalSeconds > 30 Then
                Return ReaderHealth.Dead ' No activity in 30+ seconds
            ElseIf timeSince.TotalSeconds > 5 Then
                Return ReaderHealth.Stale ' No activity in 5+ seconds
            Else
                Return ReaderHealth.Healthy ' Active
            End If
        End Function

        ''' <summary>
        ''' Returns formatted string for logging/debugging
        ''' </summary>
        Public Overrides Function ToString() As String
            Dim health = GetHealth()
            Dim timeSince = TimeSinceLastRead
            Dim timeSinceStr = If(timeSince = TimeSpan.MaxValue, "never", $"{timeSince.TotalSeconds:F1}s ago")

            Return $"[{Name}] Tap={TapPoint}, Owner={Owner}, Health={health}, LastRead={timeSinceStr}, Reads={ReadCount}, Bytes={BytesRead}"
        End Function

#End Region

    End Class

End Namespace
