Imports System.Collections.Generic
Imports System.Linq
Imports System.Text

Namespace Cognitive

    ''' <summary>
    ''' Attention Spotlight - Cognitive Pattern #3
    ''' Tracks which subsystem is currently "active" (most recent transition)
    ''' Models cognitive attention - spotlight of focus on one subsystem at a time
    ''' Zero core impact - purely observational
    ''' </summary>
    Public Class AttentionSpotlight
        Implements ICognitiveSystem

        Private ReadOnly _coordinator As StateCoordinator
        Private _enabled As Boolean = True
        Private ReadOnly _lock As New Object()

        ' Activity tracking
        Private _lastActivity As New Dictionary(Of String, DateTime)()
        Private _activityHistory As New List(Of AttentionEvent)()
        Private ReadOnly _maxHistorySize As Integer

        ' Subsystem names
        Private ReadOnly _subsystems As String() = {
            "GlobalStateMachine",
            "RecordingManagerSSM",
            "DSPThreadSSM",
            "PlaybackSSM",
            "UIStateMachine"
        }

        ''' <summary>
        ''' Creates attention spotlight
        ''' </summary>
        ''' <param name="coordinator">State coordinator to observe</param>
        ''' <param name="maxHistorySize">Maximum attention events to remember (default 1000)</param>
        Public Sub New(coordinator As StateCoordinator, Optional maxHistorySize As Integer = 1000)
            _coordinator = coordinator
            _maxHistorySize = maxHistorySize

            ' Initialize activity tracking
            For Each subsystem In _subsystems
                _lastActivity(subsystem) = DateTime.MinValue
            Next
        End Sub

#Region "ICognitiveSystem Implementation"

        Public ReadOnly Property Name As String Implements ICognitiveSystem.Name
            Get
                Return "Attention Spotlight"
            End Get
        End Property

        Public Property Enabled As Boolean Implements ICognitiveSystem.Enabled
            Get
                SyncLock _lock
                    Return _enabled
                End SyncLock
            End Get
            Set(value As Boolean)
                SyncLock _lock
                    _enabled = value
                End SyncLock
            End Set
        End Property

        Public Sub Initialize(coordinator As StateCoordinator) Implements ICognitiveSystem.Initialize
            ' Subscribe to all state machines
            AddHandler coordinator.GlobalStateMachine.StateChanged, Sub(s, e) RecordActivity("GlobalStateMachine", e.Timestamp)
            AddHandler coordinator.RecordingManagerSSM.StateChanged, Sub(s, e) RecordActivity("RecordingManagerSSM", e.Timestamp)
            AddHandler coordinator.PlaybackSSM.StateChanged, Sub(s, e) RecordActivity("PlaybackSSM", e.Timestamp)
            AddHandler coordinator.UIStateMachine.StateChanged, Sub(s, e) RecordActivity("UIStateMachine", e.Timestamp)

            ' DSPThreadSSM is optional (created after microphone arming)
            If coordinator.DSPThreadSSM IsNot Nothing Then
                AddHandler coordinator.DSPThreadSSM.StateChanged, Sub(s, e) RecordActivity("DSPThreadSSM", e.Timestamp)
            End If
        End Sub

        Public Sub OnStateChanged(transitionID As String, oldState As Object, newState As Object) Implements ICognitiveSystem.OnStateChanged
            ' Not used - we subscribe to events directly
        End Sub

        Public Function GetStatistics() As Object Implements ICognitiveSystem.GetStatistics
            SyncLock _lock
                Dim activeSubsystem = GetActiveSubsystem()
                Dim timeSinceLastActivity = If(activeSubsystem IsNot Nothing,
                    DateTime.Now - _lastActivity(activeSubsystem),
                    TimeSpan.Zero)

                Return New With {
                    .ActiveSubsystem = If(activeSubsystem, "None"),
                    .TimeSinceLastActivity = timeSinceLastActivity.TotalSeconds,
                    .TotalEvents = _activityHistory.Count,
                    .TrackedSubsystems = _subsystems.Length,
                    .Enabled = Enabled
                }
            End SyncLock
        End Function

        Public Sub Reset() Implements ICognitiveSystem.Reset
            SyncLock _lock
                _activityHistory.Clear()
                For Each subsystem In _subsystems
                    _lastActivity(subsystem) = DateTime.MinValue
                Next
            End SyncLock
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Unsubscribe from events
            If _coordinator IsNot Nothing Then
                RemoveHandler _coordinator.GlobalStateMachine.StateChanged, Sub(s, e) RecordActivity("GlobalStateMachine", e.Timestamp)
                RemoveHandler _coordinator.RecordingManagerSSM.StateChanged, Sub(s, e) RecordActivity("RecordingManagerSSM", e.Timestamp)
                RemoveHandler _coordinator.PlaybackSSM.StateChanged, Sub(s, e) RecordActivity("PlaybackSSM", e.Timestamp)
                RemoveHandler _coordinator.UIStateMachine.StateChanged, Sub(s, e) RecordActivity("UIStateMachine", e.Timestamp)

                If _coordinator.DSPThreadSSM IsNot Nothing Then
                    RemoveHandler _coordinator.DSPThreadSSM.StateChanged, Sub(s, e) RecordActivity("DSPThreadSSM", e.Timestamp)
                End If
            End If
        End Sub

#End Region

#Region "Attention Tracking API"

        ''' <summary>
        ''' Record activity for a subsystem
        ''' </summary>
        Public Sub RecordActivity(subsystemName As String, timestamp As DateTime)
            If Not Enabled Then Return

            SyncLock _lock
                ' Update last activity time
                _lastActivity(subsystemName) = timestamp

                ' Record attention event
                Dim evt As New AttentionEvent With {
                    .Subsystem = subsystemName,
                    .Timestamp = timestamp,
                    .DurationSinceLast = If(_activityHistory.Count > 0,
                        timestamp - _activityHistory.Last().Timestamp,
                        TimeSpan.Zero)
                }

                _activityHistory.Add(evt)

                ' Trim history if needed
                While _activityHistory.Count > _maxHistorySize
                    _activityHistory.RemoveAt(0)
                End While
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets currently active subsystem (most recent transition)
        ''' </summary>
        Public Function GetActiveSubsystem() As String
            SyncLock _lock
                If _lastActivity.Count = 0 Then Return Nothing

                ' Find subsystem with most recent activity
                Dim mostRecent = _lastActivity.
                    Where(Function(kvp) kvp.Value <> DateTime.MinValue).
                    OrderByDescending(Function(kvp) kvp.Value).
                    FirstOrDefault()

                Return If(mostRecent.Key IsNot Nothing, mostRecent.Key, Nothing)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets attention history (all events)
        ''' </summary>
        Public Function GetAttentionHistory() As List(Of AttentionEvent)
            SyncLock _lock
                Return _activityHistory.ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets recent attention events
        ''' </summary>
        Public Function GetRecentActivity(count As Integer) As List(Of AttentionEvent)
            SyncLock _lock
                Return _activityHistory.
                    OrderByDescending(Function(e) e.Timestamp).
                    Take(count).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets attention events within time window
        ''' </summary>
        Public Function GetActivityInWindow(startTime As DateTime, endTime As DateTime) As List(Of AttentionEvent)
            SyncLock _lock
                Return _activityHistory.
                    Where(Function(e) e.Timestamp >= startTime AndAlso e.Timestamp <= endTime).
                    OrderBy(Function(e) e.Timestamp).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Generates attention heatmap (activity distribution)
        ''' </summary>
        Public Function GenerateHeatmap(duration As TimeSpan) As AttentionHeatmap
            SyncLock _lock
                Dim cutoffTime = DateTime.Now - duration
                Dim recentEvents = _activityHistory.
                    Where(Function(e) e.Timestamp >= cutoffTime).
                    ToList()

                If recentEvents.Count = 0 Then
                    Return New AttentionHeatmap With {
                        .Duration = duration,
                        .TotalEvents = 0,
                        .SubsystemActivity = New Dictionary(Of String, Integer)()
                    }
                End If

                ' Count events per subsystem
                Dim activityCounts = recentEvents.
                    GroupBy(Function(e) e.Subsystem).
                    ToDictionary(Function(g) g.Key, Function(g) g.Count())

                Return New AttentionHeatmap With {
                    .Duration = duration,
                    .TotalEvents = recentEvents.Count,
                    .SubsystemActivity = activityCounts,
                    .MostActiveSubsystem = activityCounts.OrderByDescending(Function(kvp) kvp.Value).First().Key
                }
            End SyncLock
        End Function

        ''' <summary>
        ''' Generates human-readable attention report
        ''' </summary>
        Public Function GenerateAttentionReport(Optional duration As TimeSpan = Nothing) As String
            If duration = TimeSpan.Zero Then
                duration = TimeSpan.FromSeconds(10) ' Default 10 seconds
            End If

            SyncLock _lock
                Dim report As New StringBuilder()
                report.AppendLine("Attention Spotlight Report")
                report.AppendLine(New String("="c, 60))
                report.AppendLine()

                ' Current focus
                Dim active = GetActiveSubsystem()
                If active IsNot Nothing Then
                    Dim timeSince = DateTime.Now - _lastActivity(active)
                    report.AppendLine($"Current Focus: {active}")
                    report.AppendLine($"Last Activity: {timeSince.TotalSeconds:F1}s ago")
                    report.AppendLine()
                Else
                    report.AppendLine("Current Focus: None")
                    report.AppendLine()
                End If

                ' Heatmap
                Dim heatmap = GenerateHeatmap(duration)
                report.AppendLine($"Activity Heatmap (last {duration.TotalSeconds:F0}s):")
                report.AppendLine(New String("-"c, 60))

                If heatmap.TotalEvents = 0 Then
                    report.AppendLine("No activity in this time window")
                Else
                    For Each kvp In heatmap.SubsystemActivity.OrderByDescending(Function(kv) kv.Value)
                        Dim percentage = (kvp.Value / CDbl(heatmap.TotalEvents)) * 100
                        Dim barLength = CInt(Math.Round(percentage / 2)) ' Scale to 50 chars max
                        Dim bar = New String("?"c, barLength) & New String("?"c, 50 - barLength)

                        report.AppendLine($"[{kvp.Key,-20}] {bar}  {percentage:F1}%")
                    Next

                    report.AppendLine()
                    report.AppendLine($"Most Active: {heatmap.MostActiveSubsystem} ({heatmap.SubsystemActivity(heatmap.MostActiveSubsystem)} events)")
                End If

                Return report.ToString()
            End SyncLock
        End Function

        ''' <summary>
        ''' Exports attention analysis to log
        ''' </summary>
        Public Sub ExportToLog(Optional duration As TimeSpan = Nothing)
            If Not Enabled Then
                Utils.Logger.Instance.Warning("Attention Spotlight disabled - cannot export", "AttentionSpotlight")
                Return
            End If

            Utils.Logger.Instance.Info("=== Attention Spotlight Analysis ===", "AttentionSpotlight")
            Utils.Logger.Instance.Info(GenerateAttentionReport(duration), "AttentionSpotlight")
            Utils.Logger.Instance.Info("=== End Attention Analysis ===", "AttentionSpotlight")
        End Sub

#End Region

    End Class

#Region "Data Structures"

    ''' <summary>
    ''' Represents a single attention event (focus shift)
    ''' </summary>
    Public Structure AttentionEvent
        ''' <summary>Subsystem that received attention</summary>
        Public Subsystem As String

        ''' <summary>When attention shifted</summary>
        Public Timestamp As DateTime

        ''' <summary>Time since last attention shift</summary>
        Public DurationSinceLast As TimeSpan
    End Structure

    ''' <summary>
    ''' Statistical summary of attention distribution
    ''' </summary>
    Public Structure AttentionHeatmap
        ''' <summary>Time window analyzed</summary>
        Public Duration As TimeSpan

        ''' <summary>Total attention events</summary>
        Public TotalEvents As Integer

        ''' <summary>Activity count per subsystem</summary>
        Public SubsystemActivity As Dictionary(Of String, Integer)

        ''' <summary>Subsystem with most activity</summary>
        Public MostActiveSubsystem As String
    End Structure

#End Region

End Namespace
