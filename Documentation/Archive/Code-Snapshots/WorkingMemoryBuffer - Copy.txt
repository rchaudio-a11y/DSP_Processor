Imports System.Collections.Generic
Imports System.Linq

Namespace Cognitive

    ''' <summary>
    ''' Working Memory Buffer - Cognitive Pattern #1
    ''' Provides a rolling window of recent transitions (short-term memory)
    ''' Built on State Registry Pattern - uses existing GlobalStateMachine history
    ''' Zero core impact - purely observational
    ''' </summary>
    Public Class WorkingMemoryBuffer
        Implements ICognitiveSystem

        Private ReadOnly _coordinator As StateCoordinator
        Private _enabled As Boolean = True
        Private ReadOnly _maxSize As Integer
        Private ReadOnly _lock As New Object()

        ''' <summary>
        ''' Creates working memory buffer
        ''' </summary>
        ''' <param name="coordinator">State coordinator to observe</param>
        ''' <param name="maxSize">Maximum number of transitions to remember (default 50)</param>
        Public Sub New(coordinator As StateCoordinator, Optional maxSize As Integer = 50)
            _coordinator = coordinator
            _maxSize = maxSize
        End Sub

#Region "ICognitiveSystem Implementation"

        ''' <summary>Name of cognitive system</summary>
        Public ReadOnly Property Name As String Implements ICognitiveSystem.Name
            Get
                Return "Working Memory Buffer"
            End Get
        End Property

        ''' <summary>Is system currently enabled</summary>
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

        ''' <summary>Initialize cognitive system</summary>
        Public Sub Initialize(coordinator As StateCoordinator) Implements ICognitiveSystem.Initialize
            ' Already initialized in constructor
        End Sub

        ''' <summary>Process state change event (not used - reads history directly)</summary>
        Public Sub OnStateChanged(transitionID As String, oldState As Object, newState As Object) Implements ICognitiveSystem.OnStateChanged
            ' Not needed - we read from GlobalStateMachine.GetTransitionHistory()
        End Sub

        ''' <summary>Get system statistics</summary>
        Public Function GetStatistics() As Object Implements ICognitiveSystem.GetStatistics
            Return New With {
                .TotalTransitions = Count,
                .MaxSize = _maxSize,
                .MemoryUsage = $"{Count}/{_maxSize}",
                .OldestTransitionAge = GetOldestTransitionAge(),
                .Enabled = Enabled
            }
        End Function

        ''' <summary>Reset system state</summary>
        Public Sub Reset() Implements ICognitiveSystem.Reset
            ' Clear history in GlobalStateMachine
            _coordinator.GlobalStateMachine.ClearHistory()
        End Sub

        ''' <summary>Dispose resources</summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Nothing to dispose - we don't own the history
        End Sub

#End Region

#Region "Working Memory API"

        ''' <summary>
        ''' Gets current number of transitions in working memory
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                If Not Enabled Then Return 0
                Return _coordinator.GlobalStateMachine.GetTransitionHistory().Count
            End Get
        End Property

        ''' <summary>
        ''' Gets recent transitions (most recent first)
        ''' </summary>
        ''' <param name="count">Number of transitions to retrieve</param>
        Public Function GetRecentTransitions(Optional count As Integer = 10) As List(Of StateChangedEventArgs(Of GlobalState))
            If Not Enabled Then Return New List(Of StateChangedEventArgs(Of GlobalState))()

            Dim history = _coordinator.GlobalStateMachine.GetTransitionHistory()
            Return history.OrderByDescending(Function(t) t.Timestamp).
                          Take(count).
                          ToList()
        End Function

        ''' <summary>
        ''' Gets transitions within a time window
        ''' </summary>
        Public Function GetWindowedView(startTime As DateTime, endTime As DateTime) As List(Of StateChangedEventArgs(Of GlobalState))
            If Not Enabled Then Return New List(Of StateChangedEventArgs(Of GlobalState))()

            Dim history = _coordinator.GlobalStateMachine.GetTransitionHistory()
            Return history.Where(Function(t) t.Timestamp >= startTime AndAlso t.Timestamp <= endTime).
                          OrderBy(Function(t) t.Timestamp).
                          ToList()
        End Function

        ''' <summary>
        ''' Gets transitions matching a specific state UID
        ''' Example: Find all transitions from/to GSM_IDLE
        ''' </summary>
        Public Function GetTransitionsByState(stateUID As String) As List(Of StateChangedEventArgs(Of GlobalState))
            If Not Enabled Then Return New List(Of StateChangedEventArgs(Of GlobalState))()

            Dim history = _coordinator.GlobalStateMachine.GetTransitionHistory()
            Return history.Where(Function(t) t.OldStateUID = stateUID OrElse t.NewStateUID = stateUID).
                          OrderBy(Function(t) t.Timestamp).
                          ToList()
        End Function

        ''' <summary>
        ''' Gets transitions matching a specific TransitionID pattern
        ''' Example: Find all "T01" transitions (first transition in each session)
        ''' </summary>
        Public Function GetTransitionsByID(transitionIDPattern As String) As List(Of StateChangedEventArgs(Of GlobalState))
            If Not Enabled Then Return New List(Of StateChangedEventArgs(Of GlobalState))()

            Dim history = _coordinator.GlobalStateMachine.GetTransitionHistory()
            Return history.Where(Function(t) t.TransitionID.Contains(transitionIDPattern)).
                          OrderBy(Function(t) t.Timestamp).
                          ToList()
        End Function

        ''' <summary>
        ''' Gets all transitions (most recent first)
        ''' </summary>
        Public Function GetAllTransitions() As List(Of StateChangedEventArgs(Of GlobalState))
            If Not Enabled Then Return New List(Of StateChangedEventArgs(Of GlobalState))()

            Dim history = _coordinator.GlobalStateMachine.GetTransitionHistory()
            Return history.OrderByDescending(Function(t) t.Timestamp).ToList()
        End Function

        ''' <summary>
        ''' Generates human-readable summary of recent activity
        ''' </summary>
        Public Function GenerateSummary(Optional count As Integer = 10) As String
            If Not Enabled Then Return "Working Memory Buffer disabled"

            Dim recent = GetRecentTransitions(count)
            If recent.Count = 0 Then
                Return "No transitions in working memory"
            End If

            Dim summary As New System.Text.StringBuilder()
            summary.AppendLine($"Working Memory: Last {recent.Count} transitions")
            summary.AppendLine(New String("-"c, 60))

            For i = 0 To recent.Count - 1
                Dim t = recent(i)
                summary.AppendLine($"{i + 1}. {t}")
            Next

            Return summary.ToString()
        End Function

        ''' <summary>
        ''' Exports working memory to log file
        ''' </summary>
        Public Sub ExportToLog(Optional count As Integer = 50)
            If Not Enabled Then
                Utils.Logger.Instance.Warning("Working Memory Buffer disabled - cannot export", "WorkingMemoryBuffer")
                Return
            End If

            Dim recent = GetRecentTransitions(count)
            Utils.Logger.Instance.Info($"=== Working Memory Dump ({recent.Count} transitions) ===", "WorkingMemoryBuffer")

            For Each transition In recent
                Utils.Logger.Instance.Info(transition.ToString(), "WorkingMemoryBuffer")
            Next

            Utils.Logger.Instance.Info("=== End Working Memory Dump ===", "WorkingMemoryBuffer")
        End Sub

#End Region

#Region "Private Helpers"

        ''' <summary>
        ''' Gets age of oldest transition in memory
        ''' </summary>
        Private Function GetOldestTransitionAge() As TimeSpan
            Dim history = _coordinator.GlobalStateMachine.GetTransitionHistory()
            If history.Count = 0 Then Return TimeSpan.Zero

            Dim oldest = history.OrderBy(Function(t) t.Timestamp).First()
            Return DateTime.Now - oldest.Timestamp
        End Function

#End Region

    End Class

End Namespace
