Imports System.Collections.Generic

Namespace Cognitive

    ''' <summary>
    ''' Adaptive Threshold Manager - v2.0 Feature
    ''' Dynamically adjusts habit detection thresholds based on user behavior
    ''' Allows system to evolve and adapt over time
    ''' </summary>
    Public Class AdaptiveThresholdManager
        Implements ICognitiveSystem

        Private ReadOnly _coordinator As StateCoordinator
        Private ReadOnly _habitAnalyzer As HabitLoopAnalyzer
        Private _enabled As Boolean = True
        Private ReadOnly _lock As New Object()

        ' Threshold tracking
        Private _currentHabitThreshold As Integer = 3
        Private _currentSequenceLength As Integer = 2
        Private _minThreshold As Integer = 2
        Private _maxThreshold As Integer = 10
        Private _minSequenceLength As Integer = 2
        Private _maxSequenceLength As Integer = 5

        ' Adaptation parameters
        Private _transitionsSinceLastAdaptation As Integer = 0
        Private _adaptationInterval As Integer = 50 ' Adapt every 50 transitions
        Private _adaptationHistory As New List(Of ThresholdSnapshot)()

        Public Sub New(coordinator As StateCoordinator, habitAnalyzer As HabitLoopAnalyzer)
            _coordinator = coordinator
            _habitAnalyzer = habitAnalyzer
        End Sub

#Region "ICognitiveSystem Implementation"

        Public ReadOnly Property Name As String Implements ICognitiveSystem.Name
            Get
                Return "Adaptive Threshold Manager"
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
            Utils.Logger.Instance.Info("AdaptiveThresholdManager initialized", "AdaptiveThreshold")
        End Sub

        Public Sub OnStateChanged(transitionID As String, oldState As Object, newState As Object) Implements ICognitiveSystem.OnStateChanged
            If Not Enabled Then Return

            SyncLock _lock
                _transitionsSinceLastAdaptation += 1

                If _transitionsSinceLastAdaptation >= _adaptationInterval Then
                    AdaptThresholds()
                    _transitionsSinceLastAdaptation = 0
                End If
            End SyncLock
        End Sub

        Public Function GetStatistics() As Object Implements ICognitiveSystem.GetStatistics
            SyncLock _lock
                Return New With {
                    .CurrentHabitThreshold = _currentHabitThreshold,
                    .CurrentSequenceLength = _currentSequenceLength,
                    .TransitionsUntilNextAdaptation = _adaptationInterval - _transitionsSinceLastAdaptation,
                    .TotalAdaptations = _adaptationHistory.Count,
                    .Enabled = Enabled
                }
            End SyncLock
        End Function

        Public Sub Reset() Implements ICognitiveSystem.Reset
            SyncLock _lock
                _currentHabitThreshold = 3
                _currentSequenceLength = 2
                _transitionsSinceLastAdaptation = 0
                _adaptationHistory.Clear()
            End SyncLock
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If _coordinator IsNot Nothing Then
                RemoveHandler _coordinator.GlobalStateMachine.StateChanged, AddressOf OnStateChanged
            End If
        End Sub

#End Region

#Region "Adaptation API"

        Public Function GetCurrentThresholds() As (habitThreshold As Integer, sequenceLength As Integer)
            SyncLock _lock
                Return (_currentHabitThreshold, _currentSequenceLength)
            End SyncLock
        End Function

        Public Function GetAdaptationHistory() As List(Of ThresholdSnapshot)
            SyncLock _lock
                Return _adaptationHistory.ToList()
            End SyncLock
        End Function

#End Region

#Region "Private Methods"

        Private Sub OnStateChanged(sender As Object, e As StateChangedEventArgs(Of GlobalState))
            OnStateChanged(e.TransitionID, e.OldState, e.NewState)
        End Sub

        Private Sub AdaptThresholds()
            If Not Enabled Then Return

            SyncLock _lock
                Dim stats = _habitAnalyzer.GetHabitStatistics()

                ' Save snapshot before adaptation
                Dim beforeSnapshot As New ThresholdSnapshot With {
                    .Timestamp = DateTime.Now,
                    .HabitThreshold = _currentHabitThreshold,
                    .SequenceLength = _currentSequenceLength,
                    .TotalPatterns = stats.TotalPatternsDetected,
                    .TotalHabits = stats.TotalHabits,
                    .Reason = ""
                }

                ' Adaptation Logic 1: Too many habits detected (system too sensitive)
                If stats.TotalHabits > 10 AndAlso stats.TotalPatternsDetected > 20 Then
                    ' Increase threshold to be more selective
                    If _currentHabitThreshold < _maxThreshold Then
                        _currentHabitThreshold += 1
                        beforeSnapshot.Reason = "Too many habits - increased threshold"
                        Utils.Logger.Instance.Info($"?? Adapted: Habit threshold increased to {_currentHabitThreshold}", "AdaptiveThreshold")
                    End If
                End If

                ' Adaptation Logic 2: Too few habits (system too strict)
                If stats.TotalHabits < 2 AndAlso stats.TotalPatternsDetected > 10 Then
                    ' Decrease threshold to detect more habits
                    If _currentHabitThreshold > _minThreshold Then
                        _currentHabitThreshold -= 1
                        beforeSnapshot.Reason = "Too few habits - decreased threshold"
                        Utils.Logger.Instance.Info($"?? Adapted: Habit threshold decreased to {_currentHabitThreshold}", "AdaptiveThreshold")
                    End If
                End If

                ' Adaptation Logic 3: High pattern diversity (increase sequence length)
                If stats.UniqueWorkflows > 5 AndAlso _currentSequenceLength < _maxSequenceLength Then
                    _currentSequenceLength += 1
                    beforeSnapshot.Reason &= " | Increased sequence length for better specificity"
                    Utils.Logger.Instance.Info($"?? Adapted: Sequence length increased to {_currentSequenceLength}", "AdaptiveThreshold")
                End If

                ' Adaptation Logic 4: Low pattern diversity (decrease sequence length)
                If stats.UniqueWorkflows < 2 AndAlso stats.TotalPatternsDetected > 5 AndAlso _currentSequenceLength > _minSequenceLength Then
                    _currentSequenceLength -= 1
                    beforeSnapshot.Reason &= " | Decreased sequence length for better detection"
                    Utils.Logger.Instance.Info($"?? Adapted: Sequence length decreased to {_currentSequenceLength}", "AdaptiveThreshold")
                End If

                ' Record adaptation if any changes occurred
                If beforeSnapshot.Reason <> "" Then
                    _adaptationHistory.Add(beforeSnapshot)

                    ' Trim history
                    While _adaptationHistory.Count > 50
                        _adaptationHistory.RemoveAt(0)
                    End While
                End If
            End SyncLock
        End Sub

#End Region

    End Class

#Region "Data Structures"

    Public Class ThresholdSnapshot
        Public Property Timestamp As DateTime
        Public Property HabitThreshold As Integer
        Public Property SequenceLength As Integer
        Public Property TotalPatterns As Integer
        Public Property TotalHabits As Integer
        Public Property Reason As String
    End Class

#End Region

End Namespace
