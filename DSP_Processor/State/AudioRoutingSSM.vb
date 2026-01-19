Namespace State

    ''' <summary>
    ''' AudioRouting State Machine (SSM) - Controls entire audio routing topology
    ''' Manages: Routing topology, tap point lifecycle, monitoring, recording/playback paths
    ''' Owner: Routing decisions and tap point management
    ''' THE TAP POINT FIX: This is where tap points properly belong!
    ''' </summary>
    Public Class AudioRoutingSSM
        Implements IStateMachine(Of AudioRoutingState)

#Region "IStateMachine Implementation"

        Public ReadOnly Property CurrentState As AudioRoutingState Implements IStateMachine(Of AudioRoutingState).CurrentState
            Get
                Return _currentState
            End Get
        End Property

        Public Event StateChanged As EventHandler(Of StateChangedEventArgs(Of AudioRoutingState)) Implements IStateMachine(Of AudioRoutingState).StateChanged

        Public Function IsValidTransition(fromState As AudioRoutingState, toState As AudioRoutingState) As Boolean Implements IStateMachine(Of AudioRoutingState).IsValidTransition
            ' Same state is always valid
            If fromState = toState Then Return True

            Select Case fromState
                Case AudioRoutingState.Uninitialized
                    ' Can only go to Disabled or Error
                    Return toState = AudioRoutingState.Disabled OrElse
                           toState = AudioRoutingState.ErrorState

                Case AudioRoutingState.Disabled
                    ' Can transition to any operational state
                    Return toState = AudioRoutingState.MicToMonitoring OrElse
                           toState = AudioRoutingState.FileToOutput OrElse
                           toState = AudioRoutingState.ErrorState

                Case AudioRoutingState.MicToMonitoring
                    ' Can start recording or go idle
                    Return toState = AudioRoutingState.MicToRecording OrElse
                           toState = AudioRoutingState.Disabled OrElse
                           toState = AudioRoutingState.ErrorState

                Case AudioRoutingState.MicToRecording
                    ' Recording can stop (back to monitoring) or mic disarmed (idle)
                    Return toState = AudioRoutingState.MicToMonitoring OrElse
                           toState = AudioRoutingState.Disabled OrElse
                           toState = AudioRoutingState.ErrorState

                Case AudioRoutingState.FileToOutput
                    ' Playback can stop (idle) or mic can be armed
                    Return toState = AudioRoutingState.Disabled OrElse
                           toState = AudioRoutingState.MicToMonitoring OrElse
                           toState = AudioRoutingState.ErrorState

                Case AudioRoutingState.ErrorState
                    ' Can recover to any state
                    Return True

                Case Else
                    Return False
            End Select
        End Function

#End Region

#Region "Private Fields"

        Private _currentState As AudioRoutingState
        Private _initialized As Boolean = False
        
        ' Reference to AudioRouter (executor)
        Private ReadOnly _audioRouter As AudioIO.AudioRouter
        
        ' Reference to RecordingManagerSSM (for event subscription)
        Private WithEvents _recordingManagerSSM As RecordingManagerSSM
        
        ' Reference to PlaybackSSM (for event subscription)
        Private WithEvents _playbackSSM As PlaybackSSM

#End Region

#Region "Initialization"

        Public Sub New(audioRouter As AudioIO.AudioRouter, recordingManagerSSM As RecordingManagerSSM, playbackSSM As PlaybackSSM)
            If audioRouter Is Nothing Then Throw New ArgumentNullException(NameOf(audioRouter))
            If recordingManagerSSM Is Nothing Then Throw New ArgumentNullException(NameOf(recordingManagerSSM))
            If playbackSSM Is Nothing Then Throw New ArgumentNullException(NameOf(playbackSSM))

            _audioRouter = audioRouter
            _recordingManagerSSM = recordingManagerSSM
            _playbackSSM = playbackSSM
            _currentState = AudioRoutingState.Uninitialized

            Utils.Logger.Instance.Info("AudioRoutingSSM created", "AudioRoutingSSM")
        End Sub

        ''' <summary>
        ''' Initialize AudioRouting SSM with default state (DISABLED)
        ''' </summary>
        Public Function Initialize() As Boolean
            If _initialized Then
                Utils.Logger.Instance.Warning("AudioRouting SSM already initialized", "AudioRoutingSSM")
                Return False
            End If

            Try
                ' Default to DISABLED (no routing active)
                Dim success = TransitionTo(AudioRoutingState.Disabled, "System initialization - no routing active")
                
                If success Then
                    _initialized = True
                    Utils.Logger.Instance.Info("? AudioRouting SSM initialized (DISABLED)", "AudioRoutingSSM")
                Else
                    Utils.Logger.Instance.Error("? AudioRouting SSM initialization failed", Nothing, "AudioRoutingSSM")
                End If

                Return success

            Catch ex As Exception
                Utils.Logger.Instance.Error("AudioRouting SSM initialization exception", ex, "AudioRoutingSSM")
                Return False
            End Try
        End Function

#End Region

#Region "State Transitions"

        ''' <summary>
        ''' Attempt to transition to a new routing state
        ''' </summary>
        Public Function TransitionTo(newState As AudioRoutingState, reason As String) As Boolean Implements IStateMachine(Of AudioRoutingState).TransitionTo
            ' Validate transition
            If Not IsValidTransition(_currentState, newState) Then
                Utils.Logger.Instance.Warning($"Invalid transition: {_currentState} ? {newState} (Reason: {reason})", "AudioRoutingSSM")
                Return False
            End If

            ' Validate preconditions
            Dim validationMessage As String = Nothing
            If Not ValidateTransition(newState, validationMessage) Then
                Utils.Logger.Instance.Warning($"Transition validation failed: {validationMessage}", "AudioRoutingSSM")
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
                Dim args As New StateChangedEventArgs(Of AudioRoutingState)(oldState, newState, reason)
                RaiseEvent StateChanged(Me, args)

                Utils.Logger.Instance.Info($"?? Routing: {oldState} ? {newState} | {transitionID} | {reason}", "AudioRoutingSSM")

                Return True

            Catch ex As Exception
                Utils.Logger.Instance.Error($"Transition failed: {oldState} ? {newState}", ex, "AudioRoutingSSM")
                
                ' Try to recover to old state
                _currentState = oldState
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Validate preconditions for transition
        ''' </summary>
        Private Function ValidateTransition(newState As AudioRoutingState, ByRef message As String) As Boolean
            ' All transitions currently valid (complex validation can be added here)
            Return True
        End Function

        ''' <summary>
        ''' Execute exit actions when leaving a state
        ''' </summary>
        Private Sub ExecuteExitActions(state As AudioRoutingState)
            Select Case state
                Case AudioRoutingState.MicToMonitoring
                    ' Cleanup monitoring tap points (if transitioning away completely)
                    Utils.Logger.Instance.Debug("Exiting MicToMonitoring state", "AudioRoutingSSM")

                Case AudioRoutingState.MicToRecording
                    ' Cleanup recording tap points
                    Utils.Logger.Instance.Debug("Exiting MicToRecording state", "AudioRoutingSSM")

                Case AudioRoutingState.FileToOutput
                    ' Cleanup playback tap points
                    Utils.Logger.Instance.Debug("Exiting FileToOutput state", "AudioRoutingSSM")
            End Select
        End Sub

        ''' <summary>
        ''' Execute entry actions when entering a state
        ''' THE TAP POINT FIX HAPPENS HERE!
        ''' </summary>
        Private Sub ExecuteEntryActions(state As AudioRoutingState)
            Select Case state
                Case AudioRoutingState.Disabled
                    ' Disable all routing
                    Utils.Logger.Instance.Info("Audio Routing: DISABLED - No routing active", "AudioRoutingSSM")
                    
                    ' TODO Phase 7.3: Clear tap point readers from MainForm
                    ' For now, just log
                    
                Case AudioRoutingState.MicToMonitoring
                    ' Configure routing: Mic ? (DSP if enabled) ? Monitoring
                    Utils.Logger.Instance.Info("Audio Routing: MIC_TO_MONITORING - Monitoring active", "AudioRoutingSSM")
                    
                    ' THE TAP POINT FIX: This is where tap points get wired!
                    ' TODO Phase 7.3: Initialize tap points for mic monitoring
                    ' - "MicInputMonitor" (PreDSP tap)
                    ' - "MicOutputMonitor" (PostDSP tap)
                    ' Wire tap points to MainForm via events (NO FALLBACK!)
                    
                Case AudioRoutingState.MicToRecording
                    ' Configure routing: Mic ? (DSP if enabled) ? Recording + Monitoring
                    Utils.Logger.Instance.Info("Audio Routing: MIC_TO_RECORDING - Recording active", "AudioRoutingSSM")
                    
                    ' Recording tap points already set up in MicToMonitoring
                    ' Just add recording path
                    
                Case AudioRoutingState.FileToOutput
                    ' Configure routing: File ? (DSP if enabled) ? Output + Monitoring
                    Utils.Logger.Instance.Info("Audio Routing: FILE_TO_OUTPUT - Playback active", "AudioRoutingSSM")
                    
                    ' THE TAP POINT FIX: Setup playback tap points
                    ' TODO Phase 7.3: Initialize tap points for file playback
                    ' - "FileInputMonitor" (PreDSP tap)
                    ' - "FileOutputMonitor" (PostDSP tap)
                    
                Case AudioRoutingState.ErrorState
                    Utils.Logger.Instance.Error("AudioRouting entered ERROR state", Nothing, "AudioRoutingSSM")
            End Select
        End Sub

#End Region

#Region "SSM Event Handlers - Reactive Coordination"

        ''' <summary>
        ''' Respond to RecordingManagerSSM state changes
        ''' </summary>
        Private Sub OnRecordingManagerStateChanged(sender As Object, e As StateChangedEventArgs(Of RecordingManagerState)) Handles _recordingManagerSSM.StateChanged
            Select Case e.NewState
                Case RecordingManagerState.Armed
                    ' Microphone armed ? transition to monitoring
                    TransitionTo(AudioRoutingState.MicToMonitoring, "Microphone armed")

                Case RecordingManagerState.Recording
                    ' Recording started ? transition to recording path
                    TransitionTo(AudioRoutingState.MicToRecording, "Recording started")

                Case RecordingManagerState.Stopping
                    ' Recording stopping ? back to monitoring
                    If _currentState = AudioRoutingState.MicToRecording Then
                        TransitionTo(AudioRoutingState.MicToMonitoring, "Recording stopped")
                    End If

                Case RecordingManagerState.Idle
                    ' Microphone disarmed ? disable routing
                    If _currentState = AudioRoutingState.MicToMonitoring OrElse
                       _currentState = AudioRoutingState.MicToRecording Then
                        TransitionTo(AudioRoutingState.Disabled, "Microphone disarmed")
                    End If
            End Select
        End Sub

        ''' <summary>
        ''' Respond to PlaybackSSM state changes
        ''' </summary>
        Private Sub OnPlaybackStateChanged(sender As Object, e As StateChangedEventArgs(Of PlaybackState)) Handles _playbackSSM.StateChanged
            Select Case e.NewState
                Case PlaybackState.Playing
                    ' Playback started ? transition to file output
                    TransitionTo(AudioRoutingState.FileToOutput, "Playback started")

                Case PlaybackState.Idle
                    ' Playback stopped ? disable routing (unless mic is armed)
                    If _currentState = AudioRoutingState.FileToOutput Then
                        ' Check if mic is armed
                        Dim coordinator = StateCoordinator.Instance
                        If coordinator IsNot Nothing AndAlso coordinator.RecordingManagerSSM IsNot Nothing Then
                            If coordinator.RecordingManagerSSM.IsArmed Then
                                TransitionTo(AudioRoutingState.MicToMonitoring, "Playback stopped, mic still armed")
                            Else
                                TransitionTo(AudioRoutingState.Disabled, "Playback stopped")
                            End If
                        Else
                            TransitionTo(AudioRoutingState.Disabled, "Playback stopped")
                        End If
                    End If
            End Select
        End Sub

#End Region

#Region "Public API"

        ''' <summary>
        ''' Is routing currently active?
        ''' </summary>
        Public ReadOnly Property IsRoutingActive As Boolean
            Get
                Return _currentState <> AudioRoutingState.Disabled AndAlso
                       _currentState <> AudioRoutingState.Uninitialized AndAlso
                       _currentState <> AudioRoutingState.ErrorState
            End Get
        End Property

        ''' <summary>
        ''' Is microphone routing active?
        ''' </summary>
        Public ReadOnly Property IsMicRouting As Boolean
            Get
                Return _currentState = AudioRoutingState.MicToMonitoring OrElse
                       _currentState = AudioRoutingState.MicToRecording
            End Get
        End Property

        ''' <summary>
        ''' Is file playback routing active?
        ''' </summary>
        Public ReadOnly Property IsFileRouting As Boolean
            Get
                Return _currentState = AudioRoutingState.FileToOutput
            End Get
        End Property

#End Region

#Region "Helper Methods"

        Private Function GenerateTransitionID(oldState As AudioRoutingState, newState As AudioRoutingState) As String
            Return $"ROUTING_{oldState}_{newState}"
        End Function

#End Region

    End Class

    ''' <summary>
    ''' AudioRouting SSM States
    ''' </summary>
    Public Enum AudioRoutingState
        Uninitialized
        Disabled
        MicToMonitoring
        MicToRecording
        FileToOutput
        ErrorState
    End Enum

End Namespace
