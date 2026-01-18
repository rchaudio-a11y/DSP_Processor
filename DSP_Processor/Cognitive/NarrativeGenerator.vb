Namespace Cognitive

    ''' <summary>
    ''' Narrative Generator - Creates human-readable session summaries
    ''' Part of v1.x Cognitive Layer - Foundation for v2.0 Narrative Engine
    ''' </summary>
    Public Class NarrativeGenerator
        Implements ICognitiveSystem
        Implements IDisposable

        Private ReadOnly _coordinator As StateCoordinator
        Private ReadOnly _workingMemory As WorkingMemoryBuffer
        Private ReadOnly _habitAnalyzer As HabitLoopAnalyzer
        Private ReadOnly _attentionSpotlight As AttentionSpotlight
        Private ReadOnly _conflictDetector As ConflictDetector
        Private ReadOnly _lock As New Object()
        Private _disposed As Boolean = False
        Private _enabled As Boolean = True

        ' Session tracking
        Private _sessionStart As DateTime
        Private _totalRecordings As Integer = 0
        Private _totalPlaybacks As Integer = 0
        Private _totalStops As Integer = 0

        Public Sub New(coordinator As StateCoordinator,
                      workingMemory As WorkingMemoryBuffer,
                      habitAnalyzer As HabitLoopAnalyzer,
                      attentionSpotlight As AttentionSpotlight,
                      conflictDetector As ConflictDetector)

            If coordinator Is Nothing Then Throw New ArgumentNullException(NameOf(coordinator))
            If workingMemory Is Nothing Then Throw New ArgumentNullException(NameOf(workingMemory))
            If habitAnalyzer Is Nothing Then Throw New ArgumentNullException(NameOf(habitAnalyzer))
            If attentionSpotlight Is Nothing Then Throw New ArgumentNullException(NameOf(attentionSpotlight))
            If conflictDetector Is Nothing Then Throw New ArgumentNullException(NameOf(conflictDetector))

            _coordinator = coordinator
            _workingMemory = workingMemory
            _habitAnalyzer = habitAnalyzer
            _attentionSpotlight = attentionSpotlight
            _conflictDetector = conflictDetector
            _sessionStart = DateTime.Now
        End Sub

#Region "ICognitiveSystem Implementation"

        Public ReadOnly Property Name As String Implements ICognitiveSystem.Name
            Get
                Return "NarrativeGenerator"
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
            ' Subscribe to global state machine for counting actions
            AddHandler coordinator.GlobalStateMachine.StateChanged, AddressOf OnGlobalStateChanged

            Utils.Logger.Instance.Info("NarrativeGenerator initialized", "NarrativeGenerator")
        End Sub

        Public Sub Reset() Implements ICognitiveSystem.Reset
            SyncLock _lock
                _sessionStart = DateTime.Now
                _totalRecordings = 0
                _totalPlaybacks = 0
                _totalStops = 0
            End SyncLock

            Utils.Logger.Instance.Info("NarrativeGenerator reset", "NarrativeGenerator")
        End Sub

        Public Function GetStatistics() As Object Implements ICognitiveSystem.GetStatistics
            SyncLock _lock
                Return New NarrativeStatistics With {
                    .SessionDuration = DateTime.Now - _sessionStart,
                    .TotalRecordings = _totalRecordings,
                    .TotalPlaybacks = _totalPlaybacks,
                    .TotalStops = _totalStops
                }
            End SyncLock
        End Function

        Public Sub OnStateChanged(transitionID As String, oldState As Object, newState As Object) Implements ICognitiveSystem.OnStateChanged
            ' Count user actions
            If Not _enabled Then Return

            ' Check if these are GlobalState values
            If TypeOf oldState Is GlobalState AndAlso TypeOf newState Is GlobalState Then
                CountAction(CType(oldState, GlobalState), CType(newState, GlobalState))
            End If
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Generates a human-readable session summary
        ''' </summary>
        Public Function GenerateSessionSummary() As String
            If Not _enabled Then
                Return "Narrative Generator is disabled."
            End If

            Dim summary As New System.Text.StringBuilder()
            summary.AppendLine($"Session Summary: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            summary.AppendLine("═══════════════════════════════════════════════════════")
            summary.AppendLine($"Duration: {FormatDuration(DateTime.Now - _sessionStart)}")
            summary.AppendLine()

            ' User activity summary
            summary.AppendLine(GenerateActivityNarrative())
            summary.AppendLine()

            ' Attention focus summary
            If _attentionSpotlight.Enabled Then
                summary.AppendLine(GenerateAttentionNarrative())
                summary.AppendLine()
            End If

            ' Habit detection summary
            If _habitAnalyzer.Enabled Then
                summary.AppendLine(GenerateHabitNarrative())
                summary.AppendLine()
            End If

            ' System health summary
            If _conflictDetector.Enabled Then
                summary.AppendLine(GenerateHealthNarrative())
                summary.AppendLine()
            End If

            Return summary.ToString()
        End Function

        ''' <summary>
        ''' Generates a brief one-line summary
        ''' </summary>
        Public Function GenerateBriefSummary() As String
            SyncLock _lock
                Dim duration = FormatDuration(DateTime.Now - _sessionStart)
                Dim healthScore = If(_conflictDetector.Enabled, _conflictDetector.GetHealthScore(), 1.0)
                Dim healthRating = GetHealthRating(healthScore)

                Return $"{duration} session: {_totalRecordings} recordings, {_totalPlaybacks} playbacks, {healthRating} health"
            End SyncLock
        End Function

#End Region

#Region "Private Methods - Narrative Generation"

        Private Function GenerateActivityNarrative() As String
            Dim narrative As New System.Text.StringBuilder()

            SyncLock _lock
                If _totalRecordings = 0 AndAlso _totalPlaybacks = 0 Then
                    narrative.AppendLine("No user activity recorded yet.")
                Else
                    Dim actions As New List(Of String)

                    If _totalRecordings > 0 Then
                        actions.Add($"recorded {_totalRecordings} {Pluralize("time", _totalRecordings)}")
                    End If

                    If _totalPlaybacks > 0 Then
                        actions.Add($"played {_totalPlaybacks} {Pluralize("file", _totalPlaybacks)}")
                    End If

                    narrative.AppendLine($"The user {String.Join(" and ", actions)}.")
                End If
            End SyncLock

            Return narrative.ToString()
        End Function

        Private Function GenerateAttentionNarrative() As String
            Dim narrative As New System.Text.StringBuilder()

            Try
                Dim stats = _attentionSpotlight.GetStatistics()

                ' Access the dynamic object properties (CORRECTED!)
                Dim activeSubsystem = stats.ActiveSubsystem
                Dim timeSinceLast = stats.TimeSinceLastActivity
                Dim totalEvents = stats.TotalEvents

                ' Show current focus if any
                If activeSubsystem IsNot Nothing AndAlso activeSubsystem.ToString() <> "None" Then
                    narrative.AppendLine($"Currently focused on {activeSubsystem} ({timeSinceLast:F1}s ago).")
                End If

                ' Show activity count
                If totalEvents > 0 Then
                    narrative.AppendLine($"System recorded {totalEvents} attention {Pluralize("shift", totalEvents)}.")
                ElseIf totalEvents = 0 Then
                    narrative.AppendLine("No subsystem activity detected yet.")
                End If

            Catch ex As Exception
                narrative.AppendLine($"Attention tracking error: {ex.Message}")
                Utils.Logger.Instance.Error("Narrative attention error", ex, "NarrativeGenerator")
            End Try

            Return narrative.ToString()
        End Function

        Private Function GenerateHabitNarrative() As String
            Dim narrative As New System.Text.StringBuilder()

            Try
                ' Use GetHabitStatistics() instead of GetStatistics() (proper API!)
                Dim stats = _habitAnalyzer.GetHabitStatistics()
                Dim totalHabits = stats.TotalHabits
                Dim mostCommonHabit = stats.MostCommonHabit ' This is a HabitPattern structure!

                If totalHabits > 0 Then
                    narrative.Append($"System detected {totalHabits} recurring {Pluralize("habit", totalHabits)}")

                    ' Check if MostCommonHabit has a valid sequence (not default/empty)
                    If mostCommonHabit.Sequence IsNot Nothing AndAlso mostCommonHabit.Label <> "" AndAlso mostCommonHabit.Label <> "None" Then
                        ' Access the Frequency property of the HabitPattern structure
                        narrative.Append($": ""{mostCommonHabit.Label}"" ({mostCommonHabit.Frequency}x)")

                        ' Show second most common if exists
                        If totalHabits > 1 Then
                            narrative.Append(" and others")
                        End If
                    End If

                    narrative.AppendLine(".")
                Else
                    narrative.AppendLine("No recurring patterns detected yet.")
                End If

            Catch ex As Exception
                narrative.AppendLine($"Habit analysis error: {ex.Message}")
                Utils.Logger.Instance.Error("Narrative habit error", ex, "NarrativeGenerator")
            End Try

            Return narrative.ToString()
        End Function

        Private Function GenerateHealthNarrative() As String
            Dim narrative As New System.Text.StringBuilder()

            Try
                Dim healthScore = _conflictDetector.GetHealthScore()
                Dim stats = CType(_conflictDetector.GetStatistics(), Object)
                Dim totalConflicts = stats.TotalConflicts
                Dim healthRating = GetHealthRating(healthScore)

                narrative.Append($"System health is {healthRating} ({healthScore:P0})")

                If totalConflicts = 0 Then
                    narrative.AppendLine(" - no conflicts detected.")
                Else
                    narrative.AppendLine($" - {totalConflicts} {Pluralize("conflict", totalConflicts)} detected.")
                End If

            Catch ex As Exception
                narrative.AppendLine("Health monitoring unavailable.")
            End Try

            Return narrative.ToString()
        End Function

        Private Function GetHealthRating(score As Double) As String
            If score >= 0.95 Then Return "Excellent"
            If score >= 0.85 Then Return "Good"
            If score >= 0.7 Then Return "Fair"
            If score >= 0.5 Then Return "Poor"
            Return "Critical"
        End Function

        Private Function FormatDuration(duration As TimeSpan) As String
            If duration.TotalMinutes < 1 Then
                Return $"{duration.TotalSeconds:F0} seconds"
            ElseIf duration.TotalHours < 1 Then
                Return $"{duration.TotalMinutes:F0} minutes"
            Else
                Return $"{duration.TotalHours:F1} hours"
            End If
        End Function

        Private Function Pluralize(word As String, count As Integer) As String
            If count = 1 Then
                Return word
            Else
                ' Simple pluralization (add 's')
                Return word & "s"
            End If
        End Function

        Private Sub CountAction(oldState As GlobalState, newState As GlobalState)
            SyncLock _lock
                ' Count transitions that indicate user actions
                If oldState = GlobalState.Idle AndAlso newState = GlobalState.Arming Then
                    _totalRecordings += 1
                End If

                If oldState = GlobalState.Idle AndAlso newState = GlobalState.Playing Then
                    _totalPlaybacks += 1
                End If

                If (oldState = GlobalState.Recording OrElse oldState = GlobalState.Playing) AndAlso
                   newState = GlobalState.Idle Then
                    _totalStops += 1
                End If
            End SyncLock
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub OnGlobalStateChanged(sender As Object, e As StateChangedEventArgs(Of GlobalState))
            If Not _enabled Then Return
            OnStateChanged(e.TransitionID, e.OldState, e.NewState)
        End Sub

#End Region

#Region "IDisposable Implementation"

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _disposed Then
                If disposing Then
                    ' Unsubscribe from events
                    If _coordinator IsNot Nothing Then
                        RemoveHandler _coordinator.GlobalStateMachine.StateChanged, AddressOf OnGlobalStateChanged
                    End If

                    Utils.Logger.Instance.Info("NarrativeGenerator disposed", "NarrativeGenerator")
                End If

                _disposed = True
            End If
        End Sub

#End Region

    End Class

#Region "Data Structures"

    ''' <summary>Narrative statistics</summary>
    Public Structure NarrativeStatistics
        Public SessionDuration As TimeSpan
        Public TotalRecordings As Integer
        Public TotalPlaybacks As Integer
        Public TotalStops As Integer
    End Structure

#End Region

End Namespace
