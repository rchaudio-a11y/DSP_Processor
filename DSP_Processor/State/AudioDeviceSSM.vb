Imports DSP_Processor.AudioIO

Namespace State

    ''' <summary>
    ''' AudioDevice State Machine (SSM) - Controls audio driver backend selection
    ''' Manages: WASAPI, ASIO, DirectSound driver modes
    ''' Owner: Driver backend state (NOT device selection - that's AudioInput SSM)
    ''' </summary>
    Public Class AudioDeviceSSM
        Implements IStateMachine(Of AudioDeviceState)

#Region "IStateMachine Implementation"

        Public ReadOnly Property CurrentState As AudioDeviceState Implements IStateMachine(Of AudioDeviceState).CurrentState
            Get
                Return _currentState
            End Get
        End Property

        Public Event StateChanged As EventHandler(Of StateChangedEventArgs(Of AudioDeviceState)) Implements IStateMachine(Of AudioDeviceState).StateChanged

        Public Function IsValidTransition(fromState As AudioDeviceState, toState As AudioDeviceState) As Boolean Implements IStateMachine(Of AudioDeviceState).IsValidTransition
            ' From Uninitialized, can go to any driver or Error
            If fromState = AudioDeviceState.Uninitialized Then
                Return toState <> AudioDeviceState.Uninitialized
            End If

            ' From any driver, can switch to any other driver or Error
            If IsDriverState(fromState) Then
                Return IsDriverState(toState) OrElse toState = AudioDeviceState.ErrorState
            End If

            ' From Error, can recover to any driver
            If fromState = AudioDeviceState.ErrorState Then
                Return IsDriverState(toState)
            End If

            Return False
        End Function

#End Region

#Region "Private Fields"

        Private _currentState As AudioDeviceState
        Private _initialized As Boolean = False

#End Region

#Region "Initialization"

        Public Sub New()
            _currentState = AudioDeviceState.Uninitialized
        End Sub

        ''' <summary>
        ''' Initialize AudioDevice SSM with default driver (WASAPI)
        ''' </summary>
        Public Function Initialize() As Boolean
            If _initialized Then
                Utils.Logger.Instance.Warning("AudioDevice SSM already initialized", "AudioDeviceSSM")
                Return False
            End If

            Try
                ' Transition to default driver (WASAPI)
                Dim success = TransitionTo(AudioDeviceState.WASAPI, "System initialization - default driver")

                If success Then
                    _initialized = True
                    Utils.Logger.Instance.Info("✅ AudioDevice SSM initialized (WASAPI)", "AudioDeviceSSM")
                Else
                    Utils.Logger.Instance.Error("❌ AudioDevice SSM initialization failed", Nothing, "AudioDeviceSSM")
                End If

                Return success

            Catch ex As Exception
                Utils.Logger.Instance.Error("AudioDevice SSM initialization exception", ex, "AudioDeviceSSM")
                Return False
            End Try
        End Function

#End Region

#Region "State Transitions"

        ''' <summary>
        ''' Attempt to transition to a new driver state
        ''' </summary>
        Public Function TransitionTo(newState As AudioDeviceState, reason As String) As Boolean Implements IStateMachine(Of AudioDeviceState).TransitionTo
            ' Validate transition
            If Not IsValidTransition(_currentState, newState) Then
                Utils.Logger.Instance.Warning($"Invalid transition: {_currentState} → {newState} (Reason: {reason})", "AudioDeviceSSM")
                Return False
            End If

            ' Validate preconditions (global state, armed mic, etc.)
            Dim validationMessage As String = Nothing
            If Not ValidateTransition(newState, validationMessage) Then
                Utils.Logger.Instance.Warning($"Transition validation failed: {validationMessage}", "AudioDeviceSSM")
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
                Dim args As New StateChangedEventArgs(Of AudioDeviceState)(oldState, newState, reason)
                RaiseEvent StateChanged(Me, args)

                Utils.Logger.Instance.Info($"🎧 AudioDevice: {oldState} → {newState} | {transitionID} | {reason}", "AudioDeviceSSM")

                Return True

            Catch ex As Exception
                Utils.Logger.Instance.Error($"Transition failed: {oldState} → {newState}", ex, "AudioDeviceSSM")

                ' Try to recover to old state
                _currentState = oldState
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Validate preconditions for transition (GlobalState, armed mic, etc.)
        ''' </summary>
        Private Function ValidateTransition(newState As AudioDeviceState, ByRef message As String) As Boolean
            ' Switching drivers requires system to be idle
            If IsDriverState(_currentState) AndAlso IsDriverState(newState) Then
                ' Check GlobalStateMachine state
                Dim coordinator = StateCoordinator.Instance
                If coordinator IsNot Nothing Then
                    Dim globalState = coordinator.GlobalState

                    ' Cannot switch during Recording or Playing
                    If globalState = GlobalState.Recording Then
                        message = "Cannot switch audio driver while recording"
                        Return False
                    End If

                    If globalState = GlobalState.Playing Then
                        message = "Cannot switch audio driver during playback"
                        Return False
                    End If

                    ' Check if microphone is armed
                    Dim recSSM = coordinator.RecordingManagerSSM
                    If recSSM IsNot Nothing AndAlso recSSM.IsArmed Then
                        message = "Cannot switch audio driver while microphone is armed"
                        Return False
                    End If
                End If
            End If

            Return True
        End Function

        ''' <summary>
        ''' Execute exit actions when leaving a state
        ''' </summary>
        Private Sub ExecuteExitActions(state As AudioDeviceState)
            Select Case state
                Case AudioDeviceState.WASAPI, AudioDeviceState.ASIO, AudioDeviceState.DirectSound
                    ' Teardown current driver
                    TeardownDriver(state)
            End Select
        End Sub

        ''' <summary>
        ''' Execute entry actions when entering a state
        ''' </summary>
        Private Sub ExecuteEntryActions(state As AudioDeviceState)
            Select Case state
                Case AudioDeviceState.WASAPI
                    InitializeWASAPI()

                Case AudioDeviceState.ASIO
                    InitializeASIO()

                Case AudioDeviceState.DirectSound
                    InitializeDirectSound()

                Case AudioDeviceState.ErrorState
                    ' Error state - log and notify
                    Utils.Logger.Instance.Error("AudioDevice entered ERROR state", Nothing, "AudioDeviceSSM")
            End Select
        End Sub

#End Region

#Region "Driver Management"

        Private Sub InitializeWASAPI()
            Try
                ' Set driver in AudioInputManager
                ' TODO Phase 7: Replace with SwitchDriver() when AudioInputManager is refactored
                AudioInputManager.Instance.CurrentDriver = DriverType.WASAPI
                Utils.Logger.Instance.Info("WASAPI driver initialized", "AudioDeviceSSM")
            Catch ex As Exception
                Utils.Logger.Instance.Error("WASAPI initialization failed", ex, "AudioDeviceSSM")
                Throw
            End Try
        End Sub

        Private Sub InitializeASIO()
            Try
                AudioInputManager.Instance.CurrentDriver = DriverType.ASIO
                Utils.Logger.Instance.Info("ASIO driver initialized", "AudioDeviceSSM")
            Catch ex As Exception
                Utils.Logger.Instance.Error("ASIO initialization failed", ex, "AudioDeviceSSM")
                Throw
            End Try
        End Sub

        Private Sub InitializeDirectSound()
            Try
                AudioInputManager.Instance.CurrentDriver = DriverType.DirectSound
                Utils.Logger.Instance.Info("DirectSound driver initialized", "AudioDeviceSSM")
            Catch ex As Exception
                Utils.Logger.Instance.Error("DirectSound initialization failed", ex, "AudioDeviceSSM")
                Throw
            End Try
        End Sub

        Private Sub TeardownDriver(driver As AudioDeviceState)
            Try
                ' Teardown logic here (currently handled by AudioInputManager.SwitchDriver)
                Utils.Logger.Instance.Debug($"Teardown {driver} driver", "AudioDeviceSSM")
            Catch ex As Exception
                Utils.Logger.Instance.Warning($"Driver teardown warning: {ex.Message}", "AudioDeviceSSM")
            End Try
        End Sub

#End Region

#Region "Helper Methods"

        Private Function IsDriverState(state As AudioDeviceState) As Boolean
            Return state = AudioDeviceState.WASAPI OrElse
                   state = AudioDeviceState.ASIO OrElse
                   state = AudioDeviceState.DirectSound
        End Function

        Private Function GenerateTransitionID(oldState As AudioDeviceState, newState As AudioDeviceState) As String
            Return $"AUDIODEV_{oldState}_{newState}"
        End Function

#End Region

    End Class

    ''' <summary>
    ''' AudioDevice SSM States
    ''' </summary>
    Public Enum AudioDeviceState
        Uninitialized
        WASAPI
        ASIO
        DirectSound
        ErrorState
    End Enum

End Namespace
