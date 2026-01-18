Imports System.Collections.Generic
Imports System.Linq

Namespace Cognitive

    ''' <summary>
    ''' Prediction Engine - v2.0 Feature
    ''' Uses habit patterns to predict next user actions
    ''' Calculates confidence scores and fires prediction events
    ''' </summary>
    Public Class PredictionEngine
        Implements ICognitiveSystem

        Private ReadOnly _coordinator As StateCoordinator
        Private ReadOnly _habitAnalyzer As HabitLoopAnalyzer
        Private _enabled As Boolean = True
        Private ReadOnly _lock As New Object()

        ' Prediction tracking
        Private _currentPrediction As Prediction
        Private _predictionHistory As New List(Of Prediction)()
        Private _predictionAccuracy As Double = 0.0
        Private _totalPredictions As Integer = 0
        Private _correctPredictions As Integer = 0

        ' Configuration
        Private ReadOnly _minConfidence As Double = 0.3 ' 30% minimum
        Private ReadOnly _maxHistorySize As Integer = 100

        ''' <summary>Fired when a new prediction is made</summary>
        Public Event PredictionMade(sender As Object, prediction As Prediction)

        ''' <summary>Fired when a prediction is confirmed or denied</summary>
        Public Event PredictionResult(sender As Object, wasCorrect As Boolean, prediction As Prediction)

        Public Sub New(coordinator As StateCoordinator, habitAnalyzer As HabitLoopAnalyzer)
            _coordinator = coordinator
            _habitAnalyzer = habitAnalyzer
        End Sub

#Region "ICognitiveSystem Implementation"

        Public ReadOnly Property Name As String Implements ICognitiveSystem.Name
            Get
                Return "Prediction Engine"
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
            ' Subscribe to state changes for prediction validation
            AddHandler coordinator.GlobalStateMachine.StateChanged, AddressOf OnStateChanged
            Utils.Logger.Instance.Info("PredictionEngine initialized", "PredictionEngine")
        End Sub

        Public Sub OnStateChanged(transitionID As String, oldState As Object, newState As Object) Implements ICognitiveSystem.OnStateChanged
            If Not Enabled Then Return

            ' Validate current prediction
            If _currentPrediction IsNot Nothing Then
                ValidatePrediction(newState)
            End If

            ' Generate new prediction
            GeneratePrediction(newState)
        End Sub

        Public Function GetStatistics() As Object Implements ICognitiveSystem.GetStatistics
            SyncLock _lock
                Return New With {
                    .TotalPredictions = _totalPredictions,
                    .CorrectPredictions = _correctPredictions,
                    .Accuracy = _predictionAccuracy,
                    .CurrentPrediction = If(_currentPrediction IsNot Nothing, _currentPrediction.NextState, "None"),
                    .CurrentConfidence = If(_currentPrediction IsNot Nothing, _currentPrediction.Confidence, 0.0),
                    .Enabled = Enabled
                }
            End SyncLock
        End Function

        Public Sub Reset() Implements ICognitiveSystem.Reset
            SyncLock _lock
                _currentPrediction = Nothing
                _predictionHistory.Clear()
                _totalPredictions = 0
                _correctPredictions = 0
                _predictionAccuracy = 0.0
            End SyncLock
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If _coordinator IsNot Nothing Then
                RemoveHandler _coordinator.GlobalStateMachine.StateChanged, AddressOf OnStateChanged
            End If
        End Sub

#End Region

#Region "Prediction API"

        ''' <summary>Gets current prediction</summary>
        Public Function GetCurrentPrediction() As Prediction
            SyncLock _lock
                Return _currentPrediction
            End SyncLock
        End Function

        ''' <summary>Gets prediction accuracy</summary>
        Public Function GetAccuracy() As Double
            SyncLock _lock
                Return _predictionAccuracy
            End SyncLock
        End Function

#End Region

#Region "Private Methods"

        Private Sub OnStateChanged(sender As Object, e As StateChangedEventArgs(Of GlobalState))
            OnStateChanged(e.TransitionID, e.OldState, e.NewState)
        End Sub

        Private Sub GeneratePrediction(currentState As Object)
            If Not Enabled Then Return

            SyncLock _lock
                ' Get habits that end with current state
                Dim habits = _habitAnalyzer.GetCommonHabits()
                Dim relevantHabits = habits.Where(Function(h) h.Sequence.EndsWith(currentState.ToString())).ToList()

                If relevantHabits.Count = 0 Then
                    _currentPrediction = Nothing
                    Return
                End If

                ' Find most likely next state based on habit frequency
                Dim predictions As New Dictionary(Of String, Double)()

                For Each habit In relevantHabits
                    ' Extract next state from pattern
                    Dim states = habit.Sequence.Split(New String() {" ? "}, StringSplitOptions.None)
                    Dim currentIndex = Array.IndexOf(states, currentState.ToString())

                    If currentIndex >= 0 AndAlso currentIndex < states.Length - 1 Then
                        Dim nextState = states(currentIndex + 1)
                        Dim confidence = CDbl(habit.Frequency) / _habitAnalyzer.GetHabitStatistics().TotalOccurrences

                        If predictions.ContainsKey(nextState) Then
                            predictions(nextState) += confidence
                        Else
                            predictions(nextState) = confidence
                        End If
                    End If
                Next

                If predictions.Count = 0 Then
                    _currentPrediction = Nothing
                    Return
                End If

                ' Get highest confidence prediction
                Dim bestPrediction = predictions.OrderByDescending(Function(p) p.Value).First()

                If bestPrediction.Value >= _minConfidence Then
                    _currentPrediction = New Prediction With {
                        .NextState = bestPrediction.Key,
                        .Confidence = bestPrediction.Value,
                        .Timestamp = DateTime.Now,
                        .BasedOnHabits = relevantHabits.Count
                    }

                    _totalPredictions += 1

                    Utils.Logger.Instance.Info($"?? PREDICTION: Next state likely '{_currentPrediction.NextState}' (confidence: {_currentPrediction.Confidence:P0})", "PredictionEngine")
                    RaiseEvent PredictionMade(Me, _currentPrediction)
                Else
                    _currentPrediction = Nothing
                End If
            End SyncLock
        End Sub

        Private Sub ValidatePrediction(actualState As Object)
            If _currentPrediction Is Nothing Then Return

            SyncLock _lock
                Dim wasCorrect = (_currentPrediction.NextState = actualState.ToString())

                If wasCorrect Then
                    _correctPredictions += 1
                    Utils.Logger.Instance.Info($"? Prediction CORRECT! Predicted: {_currentPrediction.NextState}", "PredictionEngine")
                Else
                    Utils.Logger.Instance.Debug($"? Prediction wrong. Predicted: {_currentPrediction.NextState}, Actual: {actualState}", "PredictionEngine")
                End If

                ' Update accuracy
                _predictionAccuracy = CDbl(_correctPredictions) / _totalPredictions

                ' Fire result event
                RaiseEvent PredictionResult(Me, wasCorrect, _currentPrediction)

                ' Add to history
                _currentPrediction.WasCorrect = wasCorrect
                _predictionHistory.Add(_currentPrediction)

                ' Trim history
                While _predictionHistory.Count > _maxHistorySize
                    _predictionHistory.RemoveAt(0)
                End While
            End SyncLock
        End Sub

#End Region

    End Class

#Region "Data Structures"

    ''' <summary>Represents a state prediction</summary>
    Public Class Prediction
        Public Property NextState As String
        Public Property Confidence As Double
        Public Property Timestamp As DateTime
        Public Property BasedOnHabits As Integer
        Public Property WasCorrect As Boolean?
    End Class

#End Region

End Namespace
