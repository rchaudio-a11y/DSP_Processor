Imports System.Collections.Generic
Imports System.Linq
Imports System.Text

Namespace Cognitive

    ''' <summary>
    ''' Habit Loop Analyzer - Cognitive Pattern #2
    ''' Detects repeated state sequences (habits) in transition history
    ''' Identifies common workflows, predicts user behavior
    ''' Zero core impact - purely observational
    ''' </summary>
    Public Class HabitLoopAnalyzer
        Implements ICognitiveSystem

        Private ReadOnly _coordinator As StateCoordinator
        Private _enabled As Boolean = True
        Private ReadOnly _lock As New Object()

        ' Pattern tracking
        Private _sequenceTracker As New Dictionary(Of String, HabitPattern)()
        Private _recentTransitions As New Queue(Of String)()
        Private ReadOnly _sequenceLength As Integer
        Private ReadOnly _habitThreshold As Integer

        ' Known patterns (from StateRegistry.yaml)
        Private ReadOnly _knownPatterns As Dictionary(Of String, String)

        ''' <summary>
        ''' Creates habit loop analyzer
        ''' </summary>
        ''' <param name="coordinator">State coordinator to observe</param>
        ''' <param name="sequenceLength">Length of sequences to track (default 5)</param>
        ''' <param name="habitThreshold">Minimum frequency to be considered a habit (default 3)</param>
        Public Sub New(coordinator As StateCoordinator, Optional sequenceLength As Integer = 5, Optional habitThreshold As Integer = 3)
            _coordinator = coordinator
            _sequenceLength = sequenceLength
            _habitThreshold = habitThreshold
            _knownPatterns = InitializeKnownPatterns()
        End Sub

#Region "ICognitiveSystem Implementation"

        Public ReadOnly Property Name As String Implements ICognitiveSystem.Name
            Get
                Return "Habit Loop Analyzer"
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
            ' Subscribe to GlobalStateMachine transitions
            AddHandler coordinator.GlobalStateMachine.StateChanged, AddressOf OnTransition
        End Sub

        Public Sub OnStateChanged(transitionID As String, oldState As Object, newState As Object) Implements ICognitiveSystem.OnStateChanged
            If Not Enabled Then Return
            RecordTransition(transitionID)
        End Sub

        Public Function GetStatistics() As Object Implements ICognitiveSystem.GetStatistics
            SyncLock _lock
                Dim habits = _sequenceTracker.Values.Where(Function(p) p.Frequency >= _habitThreshold).ToList()
                Return New With {
                    .TotalPatterns = _sequenceTracker.Count,
                    .TotalHabits = habits.Count,
                    .SequenceLength = _sequenceLength,
                    .HabitThreshold = _habitThreshold,
                    .MostCommonHabit = GetMostCommonHabit(),
                    .Enabled = Enabled
                }
            End SyncLock
        End Function

        Public Sub Reset() Implements ICognitiveSystem.Reset
            SyncLock _lock
                _sequenceTracker.Clear()
                _recentTransitions.Clear()
            End SyncLock
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Unsubscribe from events
            If _coordinator IsNot Nothing Then
                RemoveHandler _coordinator.GlobalStateMachine.StateChanged, AddressOf OnTransition
            End If
        End Sub

#End Region

#Region "Habit Detection API"

        ''' <summary>
        ''' Record a transition for pattern analysis
        ''' </summary>
        Public Sub RecordTransition(transitionID As String)
            If Not Enabled Then Return

            SyncLock _lock
                ' Add to recent transitions queue
                _recentTransitions.Enqueue(transitionID)

                ' Keep only last N transitions
                While _recentTransitions.Count > _sequenceLength
                    _recentTransitions.Dequeue()
                End While

                ' If we have enough transitions, record the sequence
                If _recentTransitions.Count = _sequenceLength Then
                    Dim sequence = String.Join(" ? ", _recentTransitions)
                    RecordSequence(sequence)
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets all detected habits (frequency >= threshold)
        ''' </summary>
        Public Function GetCommonHabits() As List(Of HabitPattern)
            SyncLock _lock
                Return _sequenceTracker.Values.
                    Where(Function(p) p.Frequency >= _habitThreshold).
                    OrderByDescending(Function(p) p.Frequency).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets all patterns (including low-frequency ones)
        ''' </summary>
        Public Function GetAllPatterns() As List(Of HabitPattern)
            SyncLock _lock
                Return _sequenceTracker.Values.
                    OrderByDescending(Function(p) p.Frequency).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets habit statistics summary
        ''' </summary>
        Public Function GetHabitStatistics() As HabitStats
            SyncLock _lock
                Dim habits = GetCommonHabits()
                Dim allPatterns = _sequenceTracker.Values.ToList()

                Return New HabitStats With {
                    .TotalPatternsDetected = allPatterns.Count,
                    .TotalHabits = habits.Count,
                    .TotalOccurrences = allPatterns.Sum(Function(p) p.Frequency),
                    .MostCommonHabit = habits.FirstOrDefault(),
                    .AverageFrequency = If(allPatterns.Count > 0, allPatterns.Average(Function(p) CDbl(p.Frequency)), 0.0),
                    .UniqueWorkflows = CountUniqueWorkflows()
                }
            End SyncLock
        End Function

        ''' <summary>
        ''' Generates human-readable habit report
        ''' </summary>
        Public Function GenerateHabitReport() As String
            SyncLock _lock
                Dim report As New StringBuilder()
                report.AppendLine("Habit Loop Analysis Report")
                report.AppendLine(New String("="c, 60))
                report.AppendLine()

                Dim stats = GetHabitStatistics()
                report.AppendLine($"Total Patterns: {stats.TotalPatternsDetected}")
                report.AppendLine($"Habits (freq >= {_habitThreshold}): {stats.TotalHabits}")
                report.AppendLine($"Total Occurrences: {stats.TotalOccurrences}")
                report.AppendLine($"Unique Workflows: {stats.UniqueWorkflows}")
                report.AppendLine()

                Dim habits = GetCommonHabits()
                If habits.Count = 0 Then
                    report.AppendLine("No habits detected yet (need more data)")
                Else
                    report.AppendLine("Common Habits:")
                    report.AppendLine(New String("-"c, 60))

                    For i = 0 To Math.Min(habits.Count - 1, 9) ' Top 10
                        Dim habit = habits(i)
                        report.AppendLine()
                        report.AppendLine($"{i + 1}. {habit.Label}")
                        report.AppendLine($"   Sequence: {habit.Sequence}")
                        report.AppendLine($"   Frequency: {habit.Frequency} times")
                        report.AppendLine($"   Last Occurred: {habit.LastOccurrence:yyyy-MM-dd HH:mm:ss}")
                        report.AppendLine($"   Duration: {habit.AverageDuration.TotalSeconds:F1}s average")
                    Next
                End If

                Return report.ToString()
            End SyncLock
        End Function

        ''' <summary>
        ''' Exports habit analysis to log
        ''' </summary>
        Public Sub ExportToLog()
            If Not Enabled Then
                Utils.Logger.Instance.Warning("Habit Loop Analyzer disabled - cannot export", "HabitLoopAnalyzer")
                Return
            End If

            Utils.Logger.Instance.Info("=== Habit Loop Analysis ===", "HabitLoopAnalyzer")
            Utils.Logger.Instance.Info(GenerateHabitReport(), "HabitLoopAnalyzer")
            Utils.Logger.Instance.Info("=== End Habit Analysis ===", "HabitLoopAnalyzer")
        End Sub

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' Event handler for state transitions
        ''' </summary>
        Private Sub OnTransition(sender As Object, e As StateChangedEventArgs(Of GlobalState))
            RecordTransition(e.TransitionID)
        End Sub

        ''' <summary>
        ''' Record a sequence pattern
        ''' </summary>
        Private Sub RecordSequence(sequence As String)
            If _sequenceTracker.ContainsKey(sequence) Then
                ' Update existing pattern
                Dim pattern = _sequenceTracker(sequence)
                pattern.Frequency += 1
                pattern.LastOccurrence = DateTime.Now
                _sequenceTracker(sequence) = pattern
            Else
                ' New pattern
                Dim label = LabelSequence(sequence)
                _sequenceTracker(sequence) = New HabitPattern With {
                    .Sequence = sequence,
                    .Frequency = 1,
                    .LastOccurrence = DateTime.Now,
                    .Label = label,
                    .AverageDuration = TimeSpan.Zero
                }
            End If
        End Sub

        ''' <summary>
        ''' Label a sequence based on known patterns
        ''' </summary>
        Private Function LabelSequence(sequence As String) As String
            ' Check against known patterns
            For Each kvp In _knownPatterns
                If sequence.Contains(kvp.Key) Then
                    Return kvp.Value
                End If
            Next

            ' Generate generic label based on transitions
            If sequence.Contains("RECORDING") Then
                Return "Recording Workflow"
            ElseIf sequence.Contains("PLAYING") Then
                Return "Playback Workflow"
            ElseIf sequence.Contains("ERROR") Then
                Return "Error Recovery"
            Else
                Return "Unknown Pattern"
            End If
        End Function

        ''' <summary>
        ''' Initialize known patterns from StateRegistry.yaml
        ''' </summary>
        Private Function InitializeKnownPatterns() As Dictionary(Of String, String)
            Return New Dictionary(Of String, String) From {
                {"GSM_IDLE.*GSM_ARMING.*GSM_ARMED.*GSM_RECORDING.*GSM_STOPPING.*GSM_IDLE", "Full Record Cycle"},
                {"GSM_IDLE.*GSM_PLAYING.*GSM_IDLE", "Quick Playback"},
                {"GSM_IDLE.*GSM_PLAYING.*GSM_STOPPING.*GSM_IDLE", "Playback with Stop"},
                {"GSM_ERROR.*GSM_IDLE", "Error Recovery"},
                {"GSM_ARMING.*GSM_ARMED.*GSM_IDLE", "Aborted Recording"}
            }
        End Function

        ''' <summary>
        ''' Count unique workflow types
        ''' </summary>
        Private Function CountUniqueWorkflows() As Integer
            Return _sequenceTracker.Values.
                Select(Function(p) p.Label).
                Distinct().
                Count()
        End Function

        ''' <summary>
        ''' Get most common habit name
        ''' </summary>
        Private Function GetMostCommonHabit() As String
            Dim most = _sequenceTracker.Values.
                Where(Function(p) p.Frequency >= _habitThreshold).
                OrderByDescending(Function(p) p.Frequency).
                FirstOrDefault()

            Return If(most.Sequence IsNot Nothing, most.Label, "None")
        End Function

#End Region

    End Class

#Region "Data Structures"

    ''' <summary>
    ''' Represents a detected habit pattern
    ''' </summary>
    Public Structure HabitPattern
        ''' <summary>Transition sequence</summary>
        Public Sequence As String

        ''' <summary>Number of times this pattern occurred</summary>
        Public Frequency As Integer

        ''' <summary>Last time this pattern occurred</summary>
        Public LastOccurrence As DateTime

        ''' <summary>Human-readable label</summary>
        Public Label As String

        ''' <summary>Average duration of this pattern</summary>
        Public AverageDuration As TimeSpan
    End Structure

    ''' <summary>
    ''' Statistical summary of habit analysis
    ''' </summary>
    Public Structure HabitStats
        ''' <summary>Total patterns detected</summary>
        Public TotalPatternsDetected As Integer

        ''' <summary>Patterns with frequency >= threshold</summary>
        Public TotalHabits As Integer

        ''' <summary>Total pattern occurrences</summary>
        Public TotalOccurrences As Integer

        ''' <summary>Most frequently occurring habit</summary>
        Public MostCommonHabit As HabitPattern

        ''' <summary>Average frequency across all patterns</summary>
        Public AverageFrequency As Double

        ''' <summary>Number of unique workflow types</summary>
        Public UniqueWorkflows As Integer
    End Structure

#End Region

End Namespace
