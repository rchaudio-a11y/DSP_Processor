Namespace Managers

    ''' <summary>
    ''' Immutable snapshot of monitoring state at a point in time
    ''' Thread-safe: All data is captured once and immutable
    ''' Used for State Debugger Panel display
    ''' </summary>
    Public Class MonitoringSnapshot

        ''' <summary>When this snapshot was taken</summary>
        Public ReadOnly Property Timestamp As DateTime

        ''' <summary>All registered readers at snapshot time (immutable list)</summary>
        Public ReadOnly Property Readers As IReadOnlyList(Of ReaderInfo)

        ''' <summary>Count of healthy readers</summary>
        Public ReadOnly Property HealthyCount As Integer
            Get
                Return Readers.Where(Function(r) r.GetHealth() = ReaderHealth.Healthy).Count()
            End Get
        End Property

        ''' <summary>Count of unhealthy readers (Stale + Dead)</summary>
        Public ReadOnly Property UnhealthyCount As Integer
            Get
                Return Readers.Where(Function(r) r.GetHealth() = ReaderHealth.Stale OrElse r.GetHealth() = ReaderHealth.Dead).Count()
            End Get
        End Property

        ''' <summary>Count of readers with unknown health</summary>
        Public ReadOnly Property UnknownCount As Integer
            Get
                Return Readers.Where(Function(r) r.GetHealth() = ReaderHealth.Unknown).Count()
            End Get
        End Property

        ''' <summary>Total readers</summary>
        Public ReadOnly Property TotalCount As Integer
            Get
                Return Readers.Count
            End Get
        End Property

        ''' <summary>
        ''' Create snapshot from reader list
        ''' </summary>
        Public Sub New(readers As IEnumerable(Of ReaderInfo))
            Timestamp = DateTime.Now
            Me.Readers = readers.ToList().AsReadOnly()
        End Sub

        ''' <summary>
        ''' Get all readers with Stale or Dead health status
        ''' </summary>
        Public Function GetUnhealthyReaders() As List(Of ReaderInfo)
            Return Readers.Where(Function(r)
                                     Dim health = r.GetHealth()
                                     Return health = ReaderHealth.Stale OrElse health = ReaderHealth.Dead
                                 End Function).ToList()
        End Function

        ''' <summary>
        ''' Get all readers for a specific tap point
        ''' </summary>
        Public Function GetReadersByTapPoint(tapPoint As DSP.TapPoint) As List(Of ReaderInfo)
            Return Readers.Where(Function(r) r.TapPoint = tapPoint).ToList()
        End Function

        ''' <summary>
        ''' Get all readers owned by a specific component
        ''' </summary>
        Public Function GetReadersByOwner(owner As String) As List(Of ReaderInfo)
            Return Readers.Where(Function(r) String.Equals(r.Owner, owner, StringComparison.OrdinalIgnoreCase)).ToList()
        End Function

        ''' <summary>
        ''' Returns formatted string for logging
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"[{Timestamp:HH:mm:ss.fff}] Total={TotalCount}, Healthy={HealthyCount}, Unhealthy={UnhealthyCount}, Unknown={UnknownCount}"
        End Function

        ''' <summary>
        ''' Get detailed report string for debugging
        ''' </summary>
        Public Function GetDetailedReport() As String
            Dim sb As New System.Text.StringBuilder()
            sb.AppendLine($"=== Monitoring Snapshot @ {Timestamp:yyyy-MM-dd HH:mm:ss.fff} ===")
            sb.AppendLine($"Total Readers: {TotalCount}")
            sb.AppendLine($"  Healthy:     {HealthyCount}")
            sb.AppendLine($"  Stale:       {Readers.Where(Function(r) r.GetHealth() = ReaderHealth.Stale).Count()}")
            sb.AppendLine($"  Dead:        {Readers.Where(Function(r) r.GetHealth() = ReaderHealth.Dead).Count()}")
            sb.AppendLine($"  Unknown:     {UnknownCount}")
            sb.AppendLine()

            If Readers.Count > 0 Then
                sb.AppendLine("Readers:")
                For Each reader In Readers
                    sb.AppendLine($"  {reader}")
                Next
            Else
                sb.AppendLine("No readers registered")
            End If

            Return sb.ToString()
        End Function

    End Class

End Namespace
