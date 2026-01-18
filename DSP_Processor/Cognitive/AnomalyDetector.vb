Imports System.Collections.Generic
Imports System.Linq

Namespace Cognitive

    ''' <summary>
    ''' Anomaly Detector - v2.0 Feature
    ''' Detects unusual state transitions and behavior patterns
    ''' Flags deviations from learned habits
    ''' </summary>
    Public Class AnomalyDetector
        Implements ICognitiveSystem

        Private ReadOnly _coordinator As StateCoordinator
        Private ReadOnly _habitAnalyzer As HabitLoopAnalyzer
        Private ReadOnly _workingMemory As WorkingMemoryBuffer
        Private _enabled As Boolean = True
        Private ReadOnly _lock As New Object()

        ' Anomaly tracking
        Private _detectedAnomalies As New List(Of Anomaly)()
        Private _anomalyThreshold As Double = 0.2 ' 20% deviation threshold
        Private ReadOnly _maxAnomalies As Integer = 50

        ' Statistics
        Private _totalTransitionsAnalyzed As Integer = 0
        Private _totalAnomaliesDetected As Integer = 0

        ''' <summary>Fired when anomaly detected</summary>
        Public Event AnomalyDetected(sender As Object, anomaly As Anomaly)

        Public Sub New(coordinator As StateCoordinator, habitAnalyzer As HabitLoopAnalyzer, workingMemory As WorkingMemoryBuffer)
            _coordinator = coordinator
            _habitAnalyzer = habitAnalyzer
            _workingMemory = workingMemory
        End Sub

#Region "ICognitiveSystem Implementation"

        Public ReadOnly Property Name As String Implements ICognitiveSystem.Name
            Get
                Return "Anomaly Detector"
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
            AddHandler coordinator.GlobalStateMachine.StateChanged, AddressOf OnStateChanged
            Utils.Logger.Instance.Info("AnomalyDetector initialized", "AnomalyDetector")
        End Sub

        Public Sub OnStateChanged(transitionID As String, oldState As Object, newState As Object) Implements ICognitiveSystem.OnStateChanged
            If Not Enabled Then Return
            AnalyzeTransition(oldState, newState, transitionID)
        End Sub

        Public Function GetStatistics() As Object Implements ICognitiveSystem.GetStatistics
            SyncLock _lock
                Return New With {
                    .TotalAnalyzed = _totalTransitionsAnalyzed,
                    .TotalAnomalies = _totalAnomaliesDetected,
                    .AnomalyRate = If(_totalTransitionsAnalyzed > 0, CDbl(_totalAnomaliesDetected) / _totalTransitionsAnalyzed, 0.0),
                    .RecentAnomalies = _detectedAnomalies.Count,
                    .Threshold = _anomalyThreshold,
                    .Enabled = Enabled
                }
            End SyncLock
        End Function

        Public Sub Reset() Implements ICognitiveSystem.Reset
            SyncLock _lock
                _detectedAnomalies.Clear()
                _totalTransitionsAnalyzed = 0
                _totalAnomaliesDetected = 0
            End SyncLock
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If _coordinator IsNot Nothing Then
                RemoveHandler _coordinator.GlobalStateMachine.StateChanged, AddressOf OnStateChanged
            End If
        End Sub

#End Region

#Region "Anomaly Detection API"

        Public Function GetRecentAnomalies(count As Integer) As List(Of Anomaly)
            SyncLock _lock
                Return _detectedAnomalies.OrderByDescending(Function(a) a.Timestamp).Take(count).ToList()
            End SyncLock
        End Function

        Public Function GetAnomalyRate() As Double
            SyncLock _lock
                If _totalTransitionsAnalyzed = 0 Then Return 0.0
                Return CDbl(_totalAnomaliesDetected) / _totalTransitionsAnalyzed
            End SyncLock
        End Function

#End Region

#Region "Private Methods"

        Private Sub OnStateChanged(sender As Object, e As StateChangedEventArgs(Of GlobalState))
            OnStateChanged(e.TransitionID, e.OldState, e.NewState)
        End Sub

        Private Sub AnalyzeTransition(oldState As Object, newState As Object, transitionID As String)
            If Not Enabled Then Return

            SyncLock _lock
                _totalTransitionsAnalyzed += 1

                ' Check 1: Unknown transition (never seen before)
                Dim transitionString = $"{oldState} ? {newState}"
                Dim allPatterns = _habitAnalyzer.GetAllPatterns()
                Dim isKnownTransition = allPatterns.Any(Function(p) p.Sequence.Contains(transitionString))

                If Not isKnownTransition AndAlso allPatterns.Count > 5 Then
                    ' System has learned patterns, but this transition is new
                    RecordAnomaly(AnomalyType.UnknownTransition, transitionString, 0.8, "Never seen this transition before")
                    Return
                End If

                ' Check 2: Rapid state changes (< 100ms between transitions)
                Dim recent = _workingMemory.GetRecentTransitions(2)
                If recent.Count = 2 Then
                    Dim timeDiff = (recent(0).Timestamp - recent(1).Timestamp).TotalMilliseconds
                    If timeDiff < 100 Then
                        RecordAnomaly(AnomalyType.RapidStateChange, transitionString, 0.6, $"Transition in {timeDiff:F0}ms (too fast)")
                    End If
                End If

                ' Check 3: Unusual timing (deviation from habit average)
                Dim matchingHabits = allPatterns.Where(Function(p) p.Sequence.Contains(transitionString)).ToList()
                If matchingHabits.Count > 0 Then
                    Dim avgDuration = matchingHabits.Average(Function(h) h.AverageDuration.TotalSeconds)
                    Dim recentTransitions = _workingMemory.GetRecentTransitions(5)
                    
                    If recentTransitions.Count >= 2 Then
                        Dim actualDuration = (recentTransitions(0).Timestamp - recentTransitions(1).Timestamp).TotalSeconds
                        Dim deviation = Math.Abs(actualDuration - avgDuration) / avgDuration

                        If deviation > 2.0 Then ' 200% deviation
                            RecordAnomaly(AnomalyType.TimingAnomaly, transitionString, deviation * 0.3, $"Duration {actualDuration:F1}s vs expected {avgDuration:F1}s")
                        End If
                    End If
                End If

                ' Check 4: Breaking habit patterns
                Dim episodes = _workingMemory.GetEpisodes()
                If episodes.Count > 0 Then
                    Dim lastEpisode = episodes.Last()
                    If lastEpisode.EpisodeType.Contains("Error") OrElse lastEpisode.EpisodeType.Contains("Aborted") Then
                        RecordAnomaly(AnomalyType.BrokenPattern, transitionString, 0.5, $"Episode ended abnormally: {lastEpisode.EpisodeType}")
                    End If
                End If
            End SyncLock
        End Sub

        Private Sub RecordAnomaly(type As AnomalyType, transition As String, severity As Double, description As String)
            Dim anomaly As New Anomaly With {
                .Type = type,
                .Transition = transition,
                .Severity = Math.Min(1.0, severity),
                .Description = description,
                .Timestamp = DateTime.Now
            }

            _detectedAnomalies.Add(anomaly)
            _totalAnomaliesDetected += 1

            ' Trim old anomalies
            While _detectedAnomalies.Count > _maxAnomalies
                _detectedAnomalies.RemoveAt(0)
            End While

            Utils.Logger.Instance.Warning($"?? ANOMALY: {type} - {description} (severity: {severity:P0})", "AnomalyDetector")
            RaiseEvent AnomalyDetected(Me, anomaly)
        End Sub

#End Region

    End Class

#Region "Data Structures"

    Public Enum AnomalyType
        UnknownTransition
        RapidStateChange
        TimingAnomaly
        BrokenPattern
        UnexpectedSequence
    End Enum

    Public Class Anomaly
        Public Property Type As AnomalyType
        Public Property Transition As String
        Public Property Severity As Double
        Public Property Description As String
        Public Property Timestamp As DateTime
    End Class

#End Region

End Namespace
