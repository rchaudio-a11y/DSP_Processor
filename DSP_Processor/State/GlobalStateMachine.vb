Imports System.Threading
Imports System.ComponentModel ' For Description attribute

''' <summary>
''' Global State Machine - Controls application-wide state transitions
''' Implements the master state diagram for DSP_Processor
'''
''' FEATURE 002 (state-machine-hardening) - single-drainer design:
''' - Transitions commit under _stateLock, but state-change events are
'''   delivered OUTSIDE it, in strict commit order, one subscriber at a time
'''   with per-subscriber exception containment.
''' - "Transition in progress" = the window from commit through completed
'''   delivery. Requests from INSIDE that window on the drainer's own thread
'''   (handler cascades) are deferred FIFO and executed by the drainer -
'''   the ONLY producer of TransitionOutcome.Deferred. Cross-thread callers
'''   arriving mid-window block until the window closes, then proceed as the
'''   next drainer (preserves pre-feature SyncLock semantics; analysis F1).
''' - Every caller receives a truthful outcome: Performed / Deferred /
'''   Rejected. The Boolean TransitionTo surface returns True ONLY for
'''   Performed.
''' See specs/002-state-machine-hardening/contracts/transition-api.md.
''' </summary>
Public Class GlobalStateMachine
    Implements IStateMachine(Of GlobalState)

    ' Thread-safe state storage using Interlocked
    Private _currentState As Integer = GlobalState.Uninitialized

    ' Lock for state transitions - guards ONLY bookkeeping (state word, queues,
    ' history, counters). NEVER held while subscriber code runs (invariant 6).
    Private ReadOnly _stateLock As New Object()

    ' SINGLE-DRAINER WINDOW (feature 002) - all guarded by _stateLock
    Private _windowOpen As Boolean = False
    Private _drainerThreadId As Integer = -1
    Private _currentWindowTransitionID As String = Nothing
    Private ReadOnly _deferralQueue As New Queue(Of TransitionRequest)
    Private ReadOnly _deliveryQueue As New Queue(Of StateChangedEventArgs(Of GlobalState))
    Private _depthWarningCount As Integer = 0

    ''' <summary>Deferral depth beyond which a runaway-cascade diagnostic fires (spec assumption)</summary>
    Private Const DeferralDepthWarningThreshold As Integer = 16

    ' Transition history (for debugging/logging)
    Private ReadOnly _transitionHistory As New List(Of StateChangedEventArgs(Of GlobalState))
    Private Const MaxHistorySize As Integer = 100

    ' Transition counter for generating unique TransitionIDs (State Registry Pattern)
    Private _transitionCounter As Integer = 0

    ' StateChanged event
    Public Event StateChanged As EventHandler(Of StateChangedEventArgs(Of GlobalState)) Implements IStateMachine(Of GlobalState).StateChanged

    ''' <summary>A transition request held in the deferral queue</summary>
    Private Class TransitionRequest
        Public Property TargetState As GlobalState
        Public Property Reason As String
        Public Property OriginTransitionID As String
    End Class

#Region "Properties"

    ''' <summary>
    ''' Gets the current state (thread-safe read)
    ''' </summary>
    Public ReadOnly Property CurrentState As GlobalState Implements IStateMachine(Of GlobalState).CurrentState
        Get
            Return CType(Interlocked.CompareExchange(_currentState, 0, 0), GlobalState)
        End Get
    End Property

    ''' <summary>
    ''' Observable seam for the deferral-depth diagnostic (analysis E1):
    ''' number of times the depth>threshold warning has fired. Test-visible
    ''' because the log itself is suppressed under Logger.SuppressForTesting.
    ''' </summary>
    Friend ReadOnly Property DepthWarningCount As Integer
        Get
            SyncLock _stateLock
                Return _depthWarningCount
            End SyncLock
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
    ''' Attempts to transition to a new state (truthful three-valued surface).
    ''' Performed = validated and committed during this call.
    ''' Deferred  = requested from inside the current window's own delivery
    '''             (handler cascade); queued FIFO, executed by the drainer,
    '''             final fate observable in the log.
    ''' Rejected  = invalid from the state current at execution; state unchanged.
    ''' </summary>
    Public Function RequestTransition(newState As GlobalState, reason As String) As TransitionOutcome
        Dim myThreadId = Environment.CurrentManagedThreadId
        Dim originalOutcome As TransitionOutcome

        SyncLock _stateLock
            If _windowOpen Then
                If _drainerThreadId = myThreadId Then
                    ' Same-thread re-entrancy (handler cascade): defer FIFO.
                    ' The ONLY path that produces Deferred (analysis F1).
                    _deferralQueue.Enqueue(New TransitionRequest With {
                        .TargetState = newState,
                        .Reason = reason,
                        .OriginTransitionID = _currentWindowTransitionID})

                    If _deferralQueue.Count > DeferralDepthWarningThreshold Then
                        _depthWarningCount += 1
                        Utils.Logger.Instance.Warning($"Deferral queue depth {_deferralQueue.Count} exceeds {DeferralDepthWarningThreshold} - runaway cascade? (latest: {CurrentState} -> {newState}, Reason: {reason})", "GlobalStateMachine")
                    End If

                    Utils.Logger.Instance.Info($"Transition deferred: {CurrentState} -> {newState} (Reason: {reason}) during {_currentWindowTransitionID}", "GlobalStateMachine")
                    Return TransitionOutcome.Deferred
                End If

                ' Cross-thread caller mid-window: block until the window closes,
                ' then proceed as the next drainer (analysis F1 - preserves the
                ' pre-feature SyncLock block-then-perform semantics exactly).
                ' KNOWN RESIDUAL (documented in GSM-Subscriber-Audit.md): a
                ' caller must not hold a lock that a subscriber handler
                ' acquires - identical discipline to the pre-feature design.
                While _windowOpen
                    Monitor.Wait(_stateLock)
                End While
            End If

            ' Window closed and we hold the lock: open it, become the drainer
            _windowOpen = True
            _drainerThreadId = myThreadId

            ' Commit (or reject) the original request under the lock
            originalOutcome = CommitLocked(newState, reason, deferredOrigin:=Nothing)
        End SyncLock

        ' Deliver events + execute deferred requests OUTSIDE the lock, in
        ' commit order, until both queues are empty; guarantee window closure
        ' even on unexpected failure so waiters can never hang forever.
        Try
            DrainWindow()
        Finally
            EnsureWindowClosed(myThreadId)
        End Try

        Return originalOutcome
    End Function

    ''' <summary>
    ''' Attempts to transition to a new state (legacy two-valued surface).
    ''' </summary>
    ''' <returns>True ONLY if the transition was performed by this call.
    ''' False = rejected or deferred. This surface can never claim success
    ''' for a transition that did not occur (feature 002, FR-002/FR-003).</returns>
    Public Function TransitionTo(newState As GlobalState, reason As String) As Boolean Implements IStateMachine(Of GlobalState).TransitionTo
        Return RequestTransition(newState, reason) = TransitionOutcome.Performed
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
    ''' Validates and commits one transition. MUST be called under _stateLock.
    ''' On success: state updated, history recorded, event queued for ordered
    ''' out-of-lock delivery. On rejection: logged (with deferred-origin
    ''' marker when applicable), state untouched (FR-004).
    ''' </summary>
    Private Function CommitLocked(newState As GlobalState, reason As String, deferredOrigin As String) As TransitionOutcome
        Dim oldState = CurrentState
        Dim deferredMarker = If(deferredOrigin IsNot Nothing, $" (deferred; originally requested during {deferredOrigin})", "")

        If Not IsValidTransition(oldState, newState) Then
            ' State Registry Pattern - rejection tracking
            Utils.Logger.Instance.Warning($"Invalid transition rejected: {oldState} -> {newState} (Reason: {reason}){deferredMarker}", "GlobalStateMachine")
            Return TransitionOutcome.Rejected
        End If

        ' Update state (thread-safe)
        Interlocked.Exchange(_currentState, newState)

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

        ' LOG TRANSITION (State Registry Pattern - grep-friendly format)
        Utils.Logger.Instance.Info(args.ToString() & deferredMarker, "GlobalStateMachine")

        ' Error-state entry logging - inlined from the deleted OnStateEntering
        ' scaffolding (feature 002, FR-012)
        If newState = GlobalState.Error Then
            Utils.Logger.Instance.Error($"System entered error state from {oldState}", Nothing, "GlobalStateMachine")
        End If

        ' Queue the event for ordered delivery outside the lock
        _currentWindowTransitionID = transitionID
        _deliveryQueue.Enqueue(args)

        Return TransitionOutcome.Performed
    End Function

    ''' <summary>
    ''' Drainer loop: alternates between delivering queued events (outside the
    ''' lock) and executing deferred requests (under the lock) until both
    ''' queues are empty, then closes the window and wakes blocked callers.
    ''' Only ever runs on the thread that opened the window (invariant 8).
    ''' </summary>
    Private Sub DrainWindow()
        Do
            ' 1) Deliver the next pending event, if any (OUTSIDE the lock)
            Dim args As StateChangedEventArgs(Of GlobalState) = Nothing
            SyncLock _stateLock
                If _deliveryQueue.Count > 0 Then
                    args = _deliveryQueue.Dequeue()
                End If
            End SyncLock

            If args IsNot Nothing Then
                DeliverEvent(args)
                Continue Do
            End If

            ' 2) No events pending: execute the next deferred request, or close
            SyncLock _stateLock
                If _deferralQueue.Count > 0 Then
                    Dim request = _deferralQueue.Dequeue()
                    ' Late validation against the NOW-current state (invariant 3);
                    ' outcome is recorded via log - the deferring caller already
                    ' received Deferred (fire-and-observe, spec assumption)
                    CommitLocked(request.TargetState, request.Reason, request.OriginTransitionID)
                Else
                    ' Both queues empty: close the window, wake blocked callers
                    _windowOpen = False
                    _drainerThreadId = -1
                    _currentWindowTransitionID = Nothing
                    Monitor.PulseAll(_stateLock)
                    Exit Do
                End If
            End SyncLock
        Loop
    End Sub

    ''' <summary>
    ''' Failsafe: guarantees the window closes even if the drain fails
    ''' unexpectedly, so blocked cross-thread callers can never hang forever.
    ''' No-op on the normal path (DrainWindow already closed it).
    ''' </summary>
    Private Sub EnsureWindowClosed(ownerThreadId As Integer)
        SyncLock _stateLock
            If _windowOpen AndAlso _drainerThreadId = ownerThreadId Then
                _windowOpen = False
                _drainerThreadId = -1
                _currentWindowTransitionID = Nothing
                Monitor.PulseAll(_stateLock)
                Utils.Logger.Instance.Error("Transition window closed by failsafe - drain did not complete normally", Nothing, "GlobalStateMachine")
            End If
        End SyncLock
    End Sub

    ''' <summary>
    ''' Delivers one event to all subscribers, each in its own Try/Catch:
    ''' a throwing subscriber is logged and skipped, never aborting delivery
    ''' to the rest and never affecting the committed transition (FR-008).
    ''' Runs OUTSIDE _stateLock (invariant 6). Payload is authoritative -
    ''' live state may already have advanced (subscribers audited, FR-009).
    ''' </summary>
    Private Sub DeliverEvent(args As StateChangedEventArgs(Of GlobalState))
        Dim handlers = StateChangedEvent
        If handlers Is Nothing Then Return

        For Each handler As EventHandler(Of StateChangedEventArgs(Of GlobalState)) In handlers.GetInvocationList()
            Try
                handler.Invoke(Me, args)
            Catch ex As Exception
                Utils.Logger.Instance.Error($"StateChanged subscriber threw during delivery of {args.TransitionID} - skipped, remaining subscribers still notified", ex, "GlobalStateMachine")
            End Try
        Next
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
''' Truthful outcome of a transition request (feature 002, FR-002).
''' The legacy Boolean surface maps Performed to True and everything else
''' to False - it can never claim success for a transition that did not occur.
''' </summary>
Public Enum TransitionOutcome
    ''' <summary>Validated and committed during this call</summary>
    Performed = 0
    ''' <summary>Queued FIFO from inside the current window's own delivery (handler cascade); final fate observable in the log</summary>
    Deferred = 1
    ''' <summary>Invalid from the state current at execution; state unchanged</summary>
    Rejected = 2
End Enum

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
