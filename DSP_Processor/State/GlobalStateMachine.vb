Imports System.Threading
Imports System.ComponentModel ' For Description attribute

''' <summary>
''' Global State Machine - Controls application-wide state transitions
''' Implements the master state diagram for DSP_Processor
''' Thread-safe using SyncLock for state transitions
''' </summary>
Public Class GlobalStateMachine
    Implements IStateMachine(Of GlobalState)

    ' Thread-safe state storage using Interlocked
    Private _currentState As Integer = GlobalState.Uninitialized

    ' Lock for state transitions
    Private ReadOnly _stateLock As New Object()
    
    ' RE-ENTRY GUARD: Prevent recursive transitions (StackOverflowException fix)
    Private _isTransitioning As Boolean = False
    
    ' PENDING TRANSITION QUEUE: Queue transitions blocked by re-entry (Option C fix)
    ' When a transition is attempted during an active transition, queue it for later
    Private _pendingTransition As GlobalState? = Nothing
    Private _pendingReason As String = Nothing

    ' Transition history (for debugging/logging)
    Private ReadOnly _transitionHistory As New List(Of StateChangedEventArgs(Of GlobalState))
    Private Const MaxHistorySize As Integer = 100

    ' Transition counter for generating unique TransitionIDs (State Registry Pattern)
    Private _transitionCounter As Integer = 0

    ' StateChanged event
    Public Event StateChanged As EventHandler(Of StateChangedEventArgs(Of GlobalState)) Implements IStateMachine(Of GlobalState).StateChanged

#Region "Properties"

    ''' <summary>
    ''' Gets the current state (thread-safe read)
    ''' </summary>
    Public ReadOnly Property CurrentState As GlobalState Implements IStateMachine(Of GlobalState).CurrentState
        Get
            Return CType(Interlocked.CompareExchange(_currentState, 0, 0), GlobalState)
        End Get
    End Property

#End Region

#Region "Constructor"

    ''' <summary>
    ''' Creates a new GlobalStateMachine in Uninitialized state
    ''' </summary>
    Public Sub New()
        ' Initialize in Uninitialized state
        Interlocked.Exchange(_currentState, GlobalState.Uninitialized)
    End Sub

#End Region

#Region "Public Methods"

    ''' <summary>
    ''' Attempts to transition to a new state
    ''' </summary>
    ''' <param name="newState">Target state</param>
    ''' <param name="reason">Reason for transition (for logging)</param>
    ''' <returns>True if transition succeeded, False if invalid</returns>
    Public Function TransitionTo(newState As GlobalState, reason As String) As Boolean Implements IStateMachine(Of GlobalState).TransitionTo
        SyncLock _stateLock
            ' RE-ENTRY GUARD with QUEUEING (Option C: Pending Transition Queue)
            ' If already transitioning, QUEUE this request instead of rejecting it
            ' This prevents deadlocks when state changes trigger cascading transitions
            If _isTransitioning Then
                ' Already transitioning - queue this transition for execution after completion
                Console.WriteLine($"[INFO] Re-entrant transition queued: {CurrentState} ? {newState} (Reason: {reason})")
                _pendingTransition = newState
                _pendingReason = reason
                Return True  ' Accept request, will execute after current transition completes
            End If
            
            _isTransitioning = True
            Try
                Dim oldState = CurrentState

                ' Check if transition is valid
                If Not IsValidTransition(oldState, newState) Then
                    ' Log invalid transition (State Registry Pattern - rejection tracking)
                    Dim logMessage = $"Invalid transition rejected: {oldState} ? {newState} (Reason: {reason})"
                    Console.WriteLine($"[WARNING] [GlobalStateMachine] {logMessage}")
                    Utils.Logger.Instance.Warning(logMessage, "GlobalStateMachine")
                    Return False
                End If

                ' Perform state entry/exit actions
                OnStateExiting(oldState, newState)

                ' Update state (thread-safe)
                Interlocked.Exchange(_currentState, newState)

                OnStateEntering(oldState, newState)

                ' Generate TransitionID for State Registry Pattern
                Dim transitionNum = Interlocked.Increment(_transitionCounter)
                Dim oldStateUID = GetStateUID(oldState)
                Dim newStateUID = GetStateUID(newState)
                Dim transitionID = $"GSM_T{transitionNum:D2}_{oldStateUID}_TO_{newStateUID}"

                ' Create event args with State Registry Pattern support
                Dim args As New StateChangedEventArgs(Of GlobalState)(
                    oldState, newState, reason, transitionID, oldStateUID, newStateUID)

                ' Record in history
                RecordTransition(args)

                ' ? LOG TRANSITION (State Registry Pattern - grep-friendly format)
                Utils.Logger.Instance.Info(args.ToString(), "GlobalStateMachine")

                ' Fire event FIRST (while still in lock)
                ' Event subscribers get notified immediately
                RaiseEvent StateChanged(Me, args)
                
                ' IMPORTANT: Return success BEFORE executing queued transition
                ' The queued transition will execute in the Finally block
                Return True
                
            Finally
                _isTransitioning = False
                
                ' EXECUTE QUEUED TRANSITION (Option C: Pending Transition Queue)
                ' If a transition was queued while we were transitioning, execute it now
                If _pendingTransition.HasValue Then
                    Dim queuedState = _pendingTransition.Value
                    Dim queuedReason = _pendingReason
                    _pendingTransition = Nothing
                    _pendingReason = Nothing
                    
                    Console.WriteLine($"[INFO] Executing queued transition: {CurrentState} ? {queuedState}")
                    
                    ' Recursive call - but NOT re-entrant because guard has been released
                    ' This happens AFTER the current transition completes
                    TransitionTo(queuedState, queuedReason)
                End If
            End Try
        End SyncLock
    End Function

    ''' <summary>
    ''' Checks if a state transition is valid according to state diagram
    ''' </summary>
    ''' <param name="fromState">Starting state</param>
    ''' <param name="toState">Target state</param>
    ''' <returns>True if transition is allowed</returns>
    Public Function IsValidTransition(fromState As GlobalState, toState As GlobalState) As Boolean Implements IStateMachine(Of GlobalState).IsValidTransition
        ' Same state is always valid (no-op)
        If fromState = toState Then Return True

        ' Error state can transition to Idle (recovery)
        If fromState = GlobalState.Error AndAlso toState = GlobalState.Idle Then Return True

        ' Define valid transitions based on state diagram
        Select Case fromState
            Case GlobalState.Uninitialized
                ' Can only transition to Idle (initialization complete)
                Return toState = GlobalState.Idle

            Case GlobalState.Idle
                ' Can start recording or playback
                Return toState = GlobalState.Arming OrElse
                       toState = GlobalState.Playing OrElse
                       toState = GlobalState.Error

            Case GlobalState.Arming
                ' Must go to Armed or Error
                Return toState = GlobalState.Armed OrElse
                       toState = GlobalState.Error OrElse
                       toState = GlobalState.Idle ' Allow abort during arming

            Case GlobalState.Armed
                ' Can start recording or go back to Idle
                Return toState = GlobalState.Recording OrElse
                       toState = GlobalState.Idle OrElse
                       toState = GlobalState.Error

            Case GlobalState.Recording
                ' Must go to Stopping
                Return toState = GlobalState.Stopping OrElse
                       toState = GlobalState.Error

            Case GlobalState.Stopping
                ' Must go to Idle
                Return toState = GlobalState.Idle OrElse
                       toState = GlobalState.Error

            Case GlobalState.Playing
                ' Can stop playback
                Return toState = GlobalState.Stopping OrElse
                       toState = GlobalState.Idle OrElse
                       toState = GlobalState.Error

            Case GlobalState.Error
                ' Can only recover to Idle
                Return toState = GlobalState.Idle

            Case Else
                Return False
        End Select
    End Function

    ''' <summary>
    ''' Gets the transition history (for debugging)
    ''' Thread-safe snapshot
    ''' </summary>
    Public Function GetTransitionHistory() As IReadOnlyList(Of StateChangedEventArgs(Of GlobalState))
        SyncLock _stateLock
            Return _transitionHistory.ToList().AsReadOnly()
        End SyncLock
    End Function

    ''' <summary>
    ''' Clears transition history
    ''' </summary>
    Public Sub ClearHistory()
        SyncLock _stateLock
            _transitionHistory.Clear()
        End SyncLock
    End Sub

#End Region

#Region "Private Methods"

    ''' <summary>
    ''' Called when exiting a state (before state change)
    ''' </summary>
    Private Sub OnStateExiting(oldState As GlobalState, newState As GlobalState)
        ' State-specific exit actions
        Select Case oldState
            Case GlobalState.Recording
                ' Recording is stopping - cleanup will be handled by RecordingManagerSSM

            Case GlobalState.Playing
                ' Playback is stopping - cleanup will be handled by PlaybackSSM

            Case GlobalState.Armed
                ' Disarming - cleanup will be handled by RecordingManagerSSM

        End Select
    End Sub

    ''' <summary>
    ''' Called when entering a state (after state change)
    ''' </summary>
    Private Sub OnStateEntering(oldState As GlobalState, newState As GlobalState)
        ' State-specific entry actions
        Select Case newState
            Case GlobalState.Idle
                ' Entered idle state - system ready

            Case GlobalState.Arming
                ' Starting arming process - will be handled by RecordingManagerSSM

            Case GlobalState.Armed
                ' System armed and ready to record

            Case GlobalState.Recording
                ' Recording started - handled by RecordingManagerSSM

            Case GlobalState.Playing
                ' Playback started - handled by PlaybackSSM

            Case GlobalState.Stopping
                ' Stopping in progress

            Case GlobalState.Error
                ' Error state entered - log error
                Utils.Logger.Instance.Error($"System entered error state from {oldState}", Nothing, "GlobalStateMachine")

        End Select
    End Sub

    ''' <summary>
    ''' Gets the UID for a state from its Description attribute (State Registry Pattern)
    ''' </summary>
    ''' <param name="state">State to get UID for</param>
    ''' <returns>UID string (e.g., "GSM_IDLE") or state name if no Description attribute</returns>
    Private Shared Function GetStateUID(state As GlobalState) As String
        Dim field = GetType(GlobalState).GetField(state.ToString())
        If field Is Nothing Then Return state.ToString()
        
        Dim attr = CType(Attribute.GetCustomAttribute(field, GetType(ComponentModel.DescriptionAttribute)), 
                        ComponentModel.DescriptionAttribute)
        Return If(attr?.Description, state.ToString())
    End Function

    ''' <summary>
    ''' Records transition in history
    ''' </summary>
    Private Sub RecordTransition(args As StateChangedEventArgs(Of GlobalState))
        _transitionHistory.Add(args)

        ' Trim history if too large
        If _transitionHistory.Count > MaxHistorySize Then
            _transitionHistory.RemoveAt(0)
        End If
    End Sub

#End Region

End Class

''' <summary>
''' Global application states
''' These states represent the overall system state
''' UIDs follow format: GSM_{STATE} for State Registry Pattern
''' </summary>
Public Enum GlobalState
    ''' <summary>System not yet initialized</summary>
    <Description("GSM_UNINITIALIZED")>
    Uninitialized = 0

    ''' <summary>System idle, ready for user input</summary>
    <Description("GSM_IDLE")>
    Idle = 1

    ''' <summary>Arming microphone for recording</summary>
    <Description("GSM_ARMING")>
    Arming = 2

    ''' <summary>Armed and ready to record</summary>
    <Description("GSM_ARMED")>
    Armed = 3

    ''' <summary>Currently recording</summary>
    <Description("GSM_RECORDING")>
    Recording = 4

    ''' <summary>Stopping recording or playback</summary>
    <Description("GSM_STOPPING")>
    Stopping = 5

    ''' <summary>Playing back audio</summary>
    <Description("GSM_PLAYING")>
    Playing = 6

    ''' <summary>Error state (recovery needed)</summary>
    <Description("GSM_ERROR")>
    [Error] = 7
End Enum
