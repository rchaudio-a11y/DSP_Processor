Namespace State

    ''' <summary>
    ''' DSP Mode State Machine (SSM) - Controls DSP enable/disable mode
    ''' Manages: DSP on/off decision-making (NOT thread execution)
    ''' Owner: DSP mode state (NOT DSP thread - that's DSPThreadSSM)
    ''' </summary>
    Public Class DSPModeSSM
        Implements IStateMachine(Of DSPModeState)

#Region "IStateMachine Implementation"

        Public ReadOnly Property CurrentState As DSPModeState Implements IStateMachine(Of DSPModeState).CurrentState
            Get
                Return _currentState
            End Get
        End Property

        Public Event StateChanged As EventHandler(Of StateChangedEventArgs(Of DSPModeState)) Implements IStateMachine(Of DSPModeState).StateChanged

        Public Function IsValidTransition(fromState As DSPModeState, toState As DSPModeState) As Boolean Implements IStateMachine(Of DSPModeState).IsValidTransition
            ' Same state is always valid
            If fromState = toState Then Return True

            Select Case fromState
                Case DSPModeState.Uninitialized
                    ' Can transition to enabled or disabled
                    Return toState = DSPModeState.Disabled OrElse
                           toState = DSPModeState.Enabled OrElse
                           toState = DSPModeState.ErrorState

                Case DSPModeState.Disabled
                    ' Can enable or error
                    Return toState = DSPModeState.Enabled OrElse
                           toState = DSPModeState.ErrorState

                Case DSPModeState.Enabled
                    ' Can disable or error
                    Return toState = DSPModeState.Disabled OrElse
                           toState = DSPModeState.ErrorState

                Case DSPModeState.ErrorState
                    ' Can recover to either state
                    Return toState = DSPModeState.Disabled OrElse
                           toState = DSPModeState.Enabled

                Case Else
                    Return False
            End Select
        End Function

#End Region

#Region "Private Fields"

        Private _currentState As DSPModeState
        Private _initialized As Boolean = False

#End Region

#Region "Initialization"

        Public Sub New()
            _currentState = DSPModeState.Uninitialized
        End Sub

        ''' <summary>
        ''' Initialize DSP Mode SSM with default state (DISABLED for safety)
        ''' </summary>
        Public Function Initialize() As Boolean
            If _initialized Then
                Utils.Logger.Instance.Warning("DSP Mode SSM already initialized", "DSPModeSSM")
                Return False
            End If

            Try
                ' Default to DISABLED for safety (user must explicitly enable)
                Dim success = TransitionTo(DSPModeState.Disabled, "System initialization - DSP disabled by default")

                If success Then
                    _initialized = True
                    Utils.Logger.Instance.Info("✅ DSP Mode SSM initialized (DISABLED)", "DSPModeSSM")
                Else
                    Utils.Logger.Instance.Error("❌ DSP Mode SSM initialization failed", Nothing, "DSPModeSSM")
                End If

                Return success

            Catch ex As Exception
                Utils.Logger.Instance.Error("DSP Mode SSM initialization exception", ex, "DSPModeSSM")
                Return False
            End Try
        End Function

#End Region

#Region "State Transitions"

        ''' <summary>
        ''' Attempt to transition to a new DSP mode state
        ''' </summary>
        Public Function TransitionTo(newState As DSPModeState, reason As String) As Boolean Implements IStateMachine(Of DSPModeState).TransitionTo
            ' Validate transition
            If Not IsValidTransition(_currentState, newState) Then
                Utils.Logger.Instance.Warning($"Invalid transition: {_currentState} → {newState} (Reason: {reason})", "DSPModeSSM")
                Return False
            End If

            ' Validate preconditions (global state, recording, etc.)
            Dim validationMessage As String = Nothing
            If Not ValidateTransition(newState, validationMessage) Then
                Utils.Logger.Instance.Warning($"Transition validation failed: {validationMessage}", "DSPModeSSM")
                Return False
            End If

            ' Execute transition
            Dim oldState = _currentState
            Dim transitionID = GenerateTransitionID(oldState, newState)

            Try
                ' Exit actions for old state
                ExecuteExitActions(oldState)

                ' Change state
                _currentState = newState

                ' Entry actions for new state
                ExecuteEntryActions(newState)

                ' Raise state change event
                Dim args As New StateChangedEventArgs(Of DSPModeState)(oldState, newState, reason)
                RaiseEvent StateChanged(Me, args)

                Utils.Logger.Instance.Info($"🎛️ DSP Mode: {oldState} → {newState} | {transitionID} | {reason}", "DSPModeSSM")

                Return True

            Catch ex As Exception
                Utils.Logger.Instance.Error($"Transition failed: {oldState} → {newState}", ex, "DSPModeSSM")

                ' Try to recover to old state
                _currentState = oldState
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Validate preconditions for transition
        ''' </summary>
        Private Function ValidateTransition(newState As DSPModeState, ByRef message As String) As Boolean
            ' Enabling DSP has validation rules
            If newState = DSPModeState.Enabled Then
                Dim coordinator = StateCoordinator.Instance
                If coordinator IsNot Nothing Then
                    ' Check if recording is active
                    ' Per design: "DSP can be enabled during playback, but not during recording"
                    Dim globalState = coordinator.GlobalState

                    If globalState = GlobalState.Recording Then
                        message = "Cannot enable DSP while recording"
                        Return False
                    End If
                End If
            End If

            ' Disabling DSP is always allowed (no restrictions)
            Return True
        End Function

        ''' <summary>
        ''' Execute exit actions when leaving a state
        ''' </summary>
        Private Sub ExecuteExitActions(state As DSPModeState)
            ' No specific exit actions needed
        End Sub

        ''' <summary>
        ''' Execute entry actions when entering a state
        ''' </summary>
        Private Sub ExecuteEntryActions(state As DSPModeState)
            Select Case state
                Case DSPModeState.Disabled
                    ' Signal DSPThreadSSM to stop (if coordinator available)
                    Dim coordinator = StateCoordinator.Instance
                    If coordinator IsNot Nothing AndAlso coordinator.DSPThreadSSM IsNot Nothing Then
                        ' TODO Phase 7.3: Signal DSPThreadSSM to stop thread
                        ' For now, just log
                        Utils.Logger.Instance.Info("DSP Mode DISABLED - DSP processing bypassed", "DSPModeSSM")
                    End If

                Case DSPModeState.Enabled
                    ' Signal DSPThreadSSM to start (if coordinator available)
                    Dim coordinator = StateCoordinator.Instance
                    If coordinator IsNot Nothing AndAlso coordinator.DSPThreadSSM IsNot Nothing Then
                        ' TODO Phase 7.3: Signal DSPThreadSSM to start thread
                        ' For now, just log
                        Utils.Logger.Instance.Info("DSP Mode ENABLED - DSP processing active", "DSPModeSSM")
                    End If

                Case DSPModeState.ErrorState
                    Utils.Logger.Instance.Error("DSP Mode entered ERROR state", Nothing, "DSPModeSSM")
            End Select
        End Sub

#End Region

#Region "Public API"

        ''' <summary>
        ''' Request to enable DSP (called from UI)
        ''' </summary>
        Public Function RequestEnable(reason As String) As Boolean
            Return TransitionTo(DSPModeState.Enabled, reason)
        End Function

        ''' <summary>
        ''' Request to disable DSP (called from UI)
        ''' </summary>
        Public Function RequestDisable(reason As String) As Boolean
            Return TransitionTo(DSPModeState.Disabled, reason)
        End Function

        ''' <summary>
        ''' Is DSP currently enabled?
        ''' </summary>
        Public ReadOnly Property IsEnabled As Boolean
            Get
                Return _currentState = DSPModeState.Enabled
            End Get
        End Property

        ''' <summary>
        ''' Is DSP currently disabled?
        ''' </summary>
        Public ReadOnly Property IsDisabled As Boolean
            Get
                Return _currentState = DSPModeState.Disabled
            End Get
        End Property

#End Region

#Region "Helper Methods"

        Private Function GenerateTransitionID(oldState As DSPModeState, newState As DSPModeState) As String
            Return $"DSPMODE_{oldState}_{newState}"
        End Function

#End Region

    End Class

    ''' <summary>
    ''' DSP Mode SSM States
    ''' </summary>
    Public Enum DSPModeState
        Uninitialized
        Disabled
        Enabled
        ErrorState
    End Enum

End Namespace
