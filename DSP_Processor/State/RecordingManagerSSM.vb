Imports System.Threading
Imports System.ComponentModel ' For Description attribute

''' <summary>
''' Satellite State Machine for RecordingManager
''' Responds to GlobalStateMachine transitions and controls RecordingManager lifecycle
''' Ownership: Controls RecordingManager actions, does NOT own RecordingManager state
''' </summary>
Public Class RecordingManagerSSM
    Implements IStateMachine(Of RecordingManagerState)

    ' Thread-safe state storage
    Private _currentState As Integer = RecordingManagerState.Uninitialized

    ' Lock for state transitions
    Private ReadOnly _stateLock As New Object()

    ' Transition counter for generating unique TransitionIDs (State Registry Pattern)
    Private _transitionCounter As Integer = 0

    ' Reference to RecordingManager (does NOT own - only controls)
    Private ReadOnly _recordingManager As Managers.RecordingManager

    ' Reference to GlobalStateMachine (subscribe to its transitions)
    Private ReadOnly _globalStateMachine As GlobalStateMachine

    ' StateChanged event
    Public Event StateChanged As EventHandler(Of StateChangedEventArgs(Of RecordingManagerState)) Implements IStateMachine(Of RecordingManagerState).StateChanged

#Region "Properties"

    ''' <summary>
    ''' Gets the current state (thread-safe read)
    ''' </summary>
    Public ReadOnly Property CurrentState As RecordingManagerState Implements IStateMachine(Of RecordingManagerState).CurrentState
        Get
            Return CType(Interlocked.CompareExchange(_currentState, 0, 0), RecordingManagerState)
        End Get
    End Property

    ''' <summary>
    ''' Is microphone currently armed?
    ''' Convenience property for common state check
    ''' </summary>
    Public ReadOnly Property IsArmed As Boolean
        Get
            Return CurrentState = RecordingManagerState.Armed
        End Get
    End Property

#End Region

#Region "Constructor"

    ''' <summary>
    ''' Creates a new RecordingManagerSSM
    ''' </summary>
    ''' <param name="recordingManager">RecordingManager instance to control</param>
    ''' <param name="globalStateMachine">GlobalStateMachine to subscribe to</param>
    Public Sub New(recordingManager As Managers.RecordingManager, globalStateMachine As GlobalStateMachine)
        If recordingManager Is Nothing Then Throw New ArgumentNullException(NameOf(recordingManager))
        If globalStateMachine Is Nothing Then Throw New ArgumentNullException(NameOf(globalStateMachine))

        _recordingManager = recordingManager
        _globalStateMachine = globalStateMachine

        ' Initialize in Uninitialized state
        Interlocked.Exchange(_currentState, RecordingManagerState.Uninitialized)

        ' Subscribe to GlobalStateMachine transitions
        AddHandler _globalStateMachine.StateChanged, AddressOf OnGlobalStateChanged

        Utils.Logger.Instance.Info("RecordingManagerSSM created and subscribed to GlobalStateMachine", "RecordingManagerSSM")
    End Sub

#End Region

#Region "Public Methods"

    ''' <summary>
    ''' Attempts to transition to a new state
    ''' </summary>
    ''' <param name="newState">Target state</param>
    ''' <param name="reason">Reason for transition</param>
    ''' <returns>True if transition succeeded</returns>
    Public Function TransitionTo(newState As RecordingManagerState, reason As String) As Boolean Implements IStateMachine(Of RecordingManagerState).TransitionTo
        SyncLock _stateLock
            Dim oldState = CurrentState

            ' Check if transition is valid
            If Not IsValidTransition(oldState, newState) Then
                Dim logMessage = $"Invalid transition rejected: {oldState} → {newState} (Reason: {reason})"
                Utils.Logger.Instance.Warning($"RecordingManagerSSM: {logMessage}", "RecordingManagerSSM")
                Return False
            End If

            ' Perform state entry/exit actions
            OnStateExiting(oldState, newState)

            ' Update state (thread-safe)
            Interlocked.Exchange(_currentState, newState)

            OnStateEntering(oldState, newState)

            ' Generate TransitionID for State Registry Pattern
            Dim transitionNum = System.Threading.Interlocked.Increment(_transitionCounter)
            Dim oldStateUID = GetStateUID(oldState)
            Dim newStateUID = GetStateUID(newState)
            Dim transitionID = $"REC_T{transitionNum:D2}_{oldStateUID}_TO_{newStateUID}"

            ' Create event args with State Registry Pattern support
            Dim args As New StateChangedEventArgs(Of RecordingManagerState)(
                oldState, newState, reason, transitionID, oldStateUID, newStateUID)

            ' ✅ LOG TRANSITION (State Registry Pattern - grep-friendly format)
            Utils.Logger.Instance.Info(args.ToString(), "RecordingManagerSSM")

            ' Fire event
            RaiseEvent StateChanged(Me, args)

            Return True
        End SyncLock
    End Function

    ''' <summary>
    ''' Checks if a state transition is valid
    ''' </summary>
    Public Function IsValidTransition(fromState As RecordingManagerState, toState As RecordingManagerState) As Boolean Implements IStateMachine(Of RecordingManagerState).IsValidTransition
        ' Same state is always valid
        If fromState = toState Then Return True

        Select Case fromState
            Case RecordingManagerState.Uninitialized
                ' Can only go to Idle (initialization)
                Return toState = RecordingManagerState.Idle

            Case RecordingManagerState.Idle
                ' Can start arming or go to error
                Return toState = RecordingManagerState.Arming OrElse
                       toState = RecordingManagerState.Error

            Case RecordingManagerState.Arming
                ' Can go to Armed or back to Idle (abort) or Error
                Return toState = RecordingManagerState.Armed OrElse
                       toState = RecordingManagerState.Idle OrElse
                       toState = RecordingManagerState.Error

            Case RecordingManagerState.Armed
                ' Can start recording or disarm
                Return toState = RecordingManagerState.Recording OrElse
                       toState = RecordingManagerState.Idle OrElse
                       toState = RecordingManagerState.Error

            Case RecordingManagerState.Recording
                ' Must stop recording
                Return toState = RecordingManagerState.Stopping OrElse
                       toState = RecordingManagerState.Error

            Case RecordingManagerState.Stopping
                ' Must return to Idle
                Return toState = RecordingManagerState.Idle OrElse
                       toState = RecordingManagerState.Error

            Case RecordingManagerState.Error
                ' Can recover to Idle
                Return toState = RecordingManagerState.Idle

            Case Else
                Return False
        End Select
    End Function

#End Region

#Region "Private Methods - GlobalStateMachine Integration"

    ''' <summary>
    ''' Responds to GlobalStateMachine state changes
    ''' Maps global transitions to RecordingManager transitions
    ''' </summary>
    Private Sub OnGlobalStateChanged(sender As Object, e As StateChangedEventArgs(Of GlobalState))
        ' Map global state to RecordingManager state
        Select Case e.NewState
            Case GlobalState.Uninitialized
                ' System initializing
                TransitionTo(RecordingManagerState.Uninitialized, "Global: Uninitialized")

            Case GlobalState.Idle
                ' System idle - RecordingManager should be idle
                If CurrentState <> RecordingManagerState.Idle Then
                    TransitionTo(RecordingManagerState.Idle, "Global: Idle")
                End If

            Case GlobalState.Arming
                ' Global is arming - start arming process
                TransitionTo(RecordingManagerState.Arming, "Global: Arming")

            Case GlobalState.Armed
                ' Global is armed - complete arming
                TransitionTo(RecordingManagerState.Armed, "Global: Armed")

            Case GlobalState.Recording
                ' Global is recording - start recording
                TransitionTo(RecordingManagerState.Recording, "Global: Recording")

            Case GlobalState.Stopping
                ' Global is stopping - stop recording (if we're recording)
                If CurrentState = RecordingManagerState.Recording Then
                    TransitionTo(RecordingManagerState.Stopping, "Global: Stopping")
                End If

            Case GlobalState.Playing
                ' Global is playing - RecordingManager stays idle
                ' (Playback doesn't involve RecordingManager)

            Case GlobalState.Error
                ' Global error - transition to error
                TransitionTo(RecordingManagerState.Error, "Global: Error")
        End Select
    End Sub

    ''' <summary>
    ''' Called when exiting a state
    ''' </summary>
    Private Sub OnStateExiting(oldState As RecordingManagerState, newState As RecordingManagerState)
        Select Case oldState
            Case RecordingManagerState.Recording
                ' Exiting recording - stop will be handled in OnStateEntering(Stopping)

            Case RecordingManagerState.Armed
                ' Exiting armed state - disarm if going to Idle
                If newState = RecordingManagerState.Idle Then
                    ' Cleanup arming (if needed)
                End If
        End Select
    End Sub

    ''' <summary>
    ''' Called when entering a state
    ''' This is where we call RecordingManager methods (OWNERSHIP: we control actions)
    ''' </summary>
    Private Sub OnStateEntering(oldState As RecordingManagerState, newState As RecordingManagerState)
        Try
            Select Case newState
                Case RecordingManagerState.Idle
                    ' Entered idle state - ensure recording stopped
                    If oldState = RecordingManagerState.Stopping Then
                        ' Cleanup after stop (if needed)
                        Utils.Logger.Instance.Info("RecordingManager returned to Idle", "RecordingManagerSSM")
                    End If

                Case RecordingManagerState.Arming
                    ' Start arming process - call RecordingManager.ArmMicrophone()
                    Utils.Logger.Instance.Info("RecordingManagerSSM: Arming microphone...", "RecordingManagerSSM")
                    _recordingManager.ArmMicrophone()
                    
                    ' STEP 22.5 FIX: Transition to Armed AFTER arming completes
                    ' This prevents re-entry: ArmMicrophone() → (blocks) → TransitionTo(Armed)
                    _globalStateMachine.TransitionTo(GlobalState.Armed, "Microphone armed successfully")

                Case RecordingManagerState.Armed
                    ' Armed and ready - automatically transition to Recording
                    ' This completes the 3-step flow: Arming → Armed → Recording
                    Utils.Logger.Instance.Info("RecordingManager armed and ready", "RecordingManagerSSM")
                    _globalStateMachine.TransitionTo(GlobalState.Recording, "Armed and ready, starting recording")

                Case RecordingManagerState.Recording
                    ' Start recording - call RecordingManager.StartRecording()
                    Utils.Logger.Instance.Info("RecordingManagerSSM: Starting recording...", "RecordingManagerSSM")
                    _recordingManager.StartRecording()

                Case RecordingManagerState.Stopping
                    ' Stop recording with callback - STEP 22.5 FIX (Callback Pattern)
                    ' Callback executes AFTER WAV finalization, preventing corrupted files
                    Utils.Logger.Instance.Info("RecordingManagerSSM: Stopping recording...", "RecordingManagerSSM")
                    _recordingManager.StopRecording(
                        Sub()
                            ' Transition to Idle AFTER finalization complete
                            ' This prevents re-entry deadlock and ensures file integrity
                            _globalStateMachine.TransitionTo(GlobalState.Idle, "Recording stopped and finalized")
                        End Sub)

                Case RecordingManagerState.Error
                    ' Error state - log and prepare for recovery
                    Utils.Logger.Instance.Error("RecordingManagerSSM entered error state", Nothing, "RecordingManagerSSM")
            End Select

        Catch ex As Exception
            ' If any RecordingManager call fails, transition to error
            Utils.Logger.Instance.Error($"RecordingManagerSSM: Exception in OnStateEntering({newState})", ex, "RecordingManagerSSM")
            If CurrentState <> RecordingManagerState.Error Then
                TransitionTo(RecordingManagerState.Error, $"Exception: {ex.Message}")
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Gets the UID for a state from its Description attribute (State Registry Pattern)
    ''' </summary>
    Private Shared Function GetStateUID(state As RecordingManagerState) As String
        Dim field = GetType(RecordingManagerState).GetField(state.ToString())
        If field Is Nothing Then Return state.ToString()
        
        Dim attr = CType(Attribute.GetCustomAttribute(field, GetType(ComponentModel.DescriptionAttribute)), 
                        ComponentModel.DescriptionAttribute)
        Return If(attr?.Description, state.ToString())
    End Function

#End Region

End Class

''' <summary>
''' RecordingManager-specific states
''' These states track RecordingManager's lifecycle
''' UIDs follow format: REC_{STATE} for State Registry Pattern
''' </summary>
Public Enum RecordingManagerState
    ''' <summary>Not yet initialized</summary>
    <Description("REC_UNINITIALIZED")>
    Uninitialized = 0

    ''' <summary>Idle and ready</summary>
    <Description("REC_IDLE")>
    Idle = 1

    ''' <summary>Arming microphone</summary>
    <Description("REC_ARMING")>
    Arming = 2

    ''' <summary>Armed and ready to record</summary>
    <Description("REC_ARMED")>
    Armed = 3

    ''' <summary>Currently recording</summary>
    <Description("REC_RECORDING")>
    Recording = 4

    ''' <summary>Stopping recording</summary>
    <Description("REC_STOPPING")>
    Stopping = 5

    ''' <summary>Error state</summary>
    <Description("REC_ERROR")>
    [Error] = 6
End Enum
