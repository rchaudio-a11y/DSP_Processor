Imports System.Threading

''' <summary>
''' Satellite State Machine for DSPThread
''' Responds to RecordingManagerSSM transitions and controls DSPThread lifecycle
''' Ownership: Controls DSPThread worker thread, does NOT own DSPThread state
''' </summary>
Public Class DSPThreadSSM
    Implements IStateMachine(Of DSPThreadState)

    ' Thread-safe state storage
    Private _currentState As Integer = DSPThreadState.Uninitialized

    ' Lock for state transitions
    Private ReadOnly _stateLock As New Object()

    ' Reference to DSPThread (does NOT own - only controls)
    Private ReadOnly _dspThread As DSP.DSPThread

    ' Reference to RecordingManagerSSM (subscribe to its transitions)
    Private ReadOnly _recordingManagerSSM As RecordingManagerSSM

    ' StateChanged event
    Public Event StateChanged As EventHandler(Of StateChangedEventArgs(Of DSPThreadState)) Implements IStateMachine(Of DSPThreadState).StateChanged

#Region "Properties"

    ''' <summary>
    ''' Gets the current state (thread-safe read)
    ''' </summary>
    Public ReadOnly Property CurrentState As DSPThreadState Implements IStateMachine(Of DSPThreadState).CurrentState
        Get
            Return CType(Interlocked.CompareExchange(_currentState, 0, 0), DSPThreadState)
        End Get
    End Property

#End Region

#Region "Constructor"

    ''' <summary>
    ''' Creates a new DSPThreadSSM
    ''' </summary>
    ''' <param name="dspThread">DSPThread instance to control</param>
    ''' <param name="recordingManagerSSM">RecordingManagerSSM to subscribe to</param>
    Public Sub New(dspThread As DSP.DSPThread, recordingManagerSSM As RecordingManagerSSM)
        If dspThread Is Nothing Then Throw New ArgumentNullException(NameOf(dspThread))
        If recordingManagerSSM Is Nothing Then Throw New ArgumentNullException(NameOf(recordingManagerSSM))

        _dspThread = dspThread
        _recordingManagerSSM = recordingManagerSSM

        ' Initialize in Uninitialized state
        Interlocked.Exchange(_currentState, DSPThreadState.Uninitialized)

        ' Subscribe to RecordingManagerSSM transitions
        AddHandler _recordingManagerSSM.StateChanged, AddressOf OnRecordingManagerStateChanged

        Utils.Logger.Instance.Info("DSPThreadSSM created and subscribed to RecordingManagerSSM", "DSPThreadSSM")
    End Sub

#End Region

#Region "Public Methods"

    ''' <summary>
    ''' Attempts to transition to a new state
    ''' </summary>
    ''' <param name="newState">Target state</param>
    ''' <param name="reason">Reason for transition</param>
    ''' <returns>True if transition succeeded</returns>
    Public Function TransitionTo(newState As DSPThreadState, reason As String) As Boolean Implements IStateMachine(Of DSPThreadState).TransitionTo
        SyncLock _stateLock
            Dim oldState = CurrentState

            ' Check if transition is valid
            If Not IsValidTransition(oldState, newState) Then
                Utils.Logger.Instance.Warning($"DSPThreadSSM: Invalid transition {oldState} → {newState} (Reason: {reason})", "DSPThreadSSM")
                Return False
            End If

            ' Perform state entry/exit actions
            OnStateExiting(oldState, newState)

            ' Update state (thread-safe)
            Interlocked.Exchange(_currentState, newState)

            OnStateEntering(oldState, newState)

            ' Create event args
            Dim args As New StateChangedEventArgs(Of DSPThreadState)(oldState, newState, reason)

            ' Log transition
            Utils.Logger.Instance.Info($"DSPThreadSSM: {args}", "DSPThreadSSM")

            ' Fire event
            RaiseEvent StateChanged(Me, args)

            Return True
        End SyncLock
    End Function

    ''' <summary>
    ''' Checks if a state transition is valid
    ''' </summary>
    Public Function IsValidTransition(fromState As DSPThreadState, toState As DSPThreadState) As Boolean Implements IStateMachine(Of DSPThreadState).IsValidTransition
        ' Same state is always valid
        If fromState = toState Then Return True

        Select Case fromState
            Case DSPThreadState.Uninitialized
                ' Can only go to Idle (initialization)
                Return toState = DSPThreadState.Idle

            Case DSPThreadState.Idle
                ' Can start running or go to error
                Return toState = DSPThreadState.Running OrElse
                       toState = DSPThreadState.Error

            Case DSPThreadState.Running
                ' Can stop or encounter error
                Return toState = DSPThreadState.Stopping OrElse
                       toState = DSPThreadState.Error

            Case DSPThreadState.Stopping
                ' Must return to Idle
                Return toState = DSPThreadState.Idle OrElse
                       toState = DSPThreadState.Error

            Case DSPThreadState.Error
                ' Can recover to Idle
                Return toState = DSPThreadState.Idle

            Case Else
                Return False
        End Select
    End Function

#End Region

#Region "Private Methods - RecordingManagerSSM Integration"

    ''' <summary>
    ''' Responds to RecordingManagerSSM state changes
    ''' Maps RecordingManager transitions to DSPThread transitions
    ''' </summary>
    Private Sub OnRecordingManagerStateChanged(sender As Object, e As StateChangedEventArgs(Of RecordingManagerState))
        ' Map RecordingManager state to DSPThread state
        Select Case e.NewState
            Case RecordingManagerState.Uninitialized
                ' RecordingManager initializing
                TransitionTo(DSPThreadState.Uninitialized, "RecordingManager: Uninitialized")

            Case RecordingManagerState.Idle
                ' RecordingManager idle - stop DSP if running
                If CurrentState = DSPThreadState.Running Then
                    TransitionTo(DSPThreadState.Stopping, "RecordingManager: Idle")
                ElseIf CurrentState = DSPThreadState.Stopping Then
                    ' Already stopping - will transition to Idle when done
                Else
                    ' Already idle or uninitialized
                    If CurrentState <> DSPThreadState.Idle Then
                        TransitionTo(DSPThreadState.Idle, "RecordingManager: Idle")
                    End If
                End If

            Case RecordingManagerState.Arming
                ' RecordingManager arming - DSPThread stays idle
                ' (Don't start worker thread until recording actually starts)

            Case RecordingManagerState.Armed
                ' RecordingManager armed - DSPThread stays idle
                ' (Don't start worker thread until recording actually starts)

            Case RecordingManagerState.Recording
                ' RecordingManager recording - START DSPThread!
                If CurrentState = DSPThreadState.Idle Then
                    TransitionTo(DSPThreadState.Running, "RecordingManager: Recording")
                End If

            Case RecordingManagerState.Stopping
                ' RecordingManager stopping - STOP DSPThread!
                If CurrentState = DSPThreadState.Running Then
                    TransitionTo(DSPThreadState.Stopping, "RecordingManager: Stopping")
                End If

            Case RecordingManagerState.Error
                ' RecordingManager error - transition to error
                TransitionTo(DSPThreadState.Error, "RecordingManager: Error")
        End Select
    End Sub

    ''' <summary>
    ''' Called when exiting a state
    ''' </summary>
    Private Sub OnStateExiting(oldState As DSPThreadState, newState As DSPThreadState)
        Select Case oldState
            Case DSPThreadState.Running
                ' Exiting running state - stop will be handled in OnStateEntering(Stopping)
        End Select
    End Sub

    ''' <summary>
    ''' Called when entering a state
    ''' This is where we call DSPThread methods (OWNERSHIP: we control worker thread lifecycle)
    ''' </summary>
    Private Sub OnStateEntering(oldState As DSPThreadState, newState As DSPThreadState)
        Try
            Select Case newState
                Case DSPThreadState.Idle
                    ' Entered idle state - ensure worker thread stopped
                    If oldState = DSPThreadState.Stopping Then
                        Utils.Logger.Instance.Info("DSPThread worker stopped and idle", "DSPThreadSSM")
                    End If

                Case DSPThreadState.Running
                    ' Start worker thread - call DSPThread.Start()
                    Utils.Logger.Instance.Info("DSPThreadSSM: Starting DSP worker thread...", "DSPThreadSSM")
                    _dspThread.Start()
                    Utils.Logger.Instance.Info("DSP worker thread started", "DSPThreadSSM")

                Case DSPThreadState.Stopping
                    ' Stop worker thread - call DSPThread.Stop()
                    Utils.Logger.Instance.Info("DSPThreadSSM: Stopping DSP worker thread...", "DSPThreadSSM")
                    _dspThread.Stop()

                    ' After stop completes, transition to Idle
                    ' (Stop() is blocking until thread exits)
                    TransitionTo(DSPThreadState.Idle, "Worker thread stopped")

                Case DSPThreadState.Error
                    ' Error state - ensure worker thread is stopped
                    Utils.Logger.Instance.Error("DSPThreadSSM entered error state", Nothing, "DSPThreadSSM")

                    ' Try to stop worker thread safely
                    Try
                        If _dspThread.IsRunning Then
                            _dspThread.Stop()
                        End If
                    Catch ex As Exception
                        Utils.Logger.Instance.Error("Failed to stop DSPThread during error recovery", ex, "DSPThreadSSM")
                    End Try
            End Select

        Catch ex As Exception
            ' If any DSPThread call fails, transition to error
            Utils.Logger.Instance.Error($"DSPThreadSSM: Exception in OnStateEntering({newState})", ex, "DSPThreadSSM")
            If CurrentState <> DSPThreadState.Error Then
                TransitionTo(DSPThreadState.Error, $"Exception: {ex.Message}")
            End If
        End Try
    End Sub

#End Region

End Class

''' <summary>
''' DSPThread-specific states
''' These states track DSPThread worker thread lifecycle
''' </summary>
Public Enum DSPThreadState
    ''' <summary>Not yet initialized</summary>
    Uninitialized = 0

    ''' <summary>Idle (worker thread not running)</summary>
    Idle = 1

    ''' <summary>Worker thread running and processing audio</summary>
    Running = 2

    ''' <summary>Stopping worker thread</summary>
    Stopping = 3

    ''' <summary>Error state</summary>
    [Error] = 4
End Enum
