Imports System.Threading

''' <summary>
''' Satellite State Machine for Playback (AudioRouter)
''' Responds to GlobalStateMachine transitions and controls playback lifecycle
''' Ownership: Controls AudioRouter playback actions, does NOT own AudioRouter state
''' </summary>
Public Class PlaybackSSM
    Implements IStateMachine(Of PlaybackState)
    
    ' Thread-safe state storage
    Private _currentState As Integer = PlaybackState.Uninitialized
    
    ' Lock for state transitions
    Private ReadOnly _stateLock As New Object()
    
    ' Reference to AudioRouter (does NOT own - only controls)
    Private ReadOnly _audioRouter As AudioIO.AudioRouter
    
    ' Reference to GlobalStateMachine (subscribe to its transitions)
    Private ReadOnly _globalStateMachine As GlobalStateMachine
    
    ' StateChanged event
    Public Event StateChanged As EventHandler(Of StateChangedEventArgs(Of PlaybackState)) Implements IStateMachine(Of PlaybackState).StateChanged
    
#Region "Properties"
    
    ''' <summary>
    ''' Gets the current state (thread-safe read)
    ''' </summary>
    Public ReadOnly Property CurrentState As PlaybackState Implements IStateMachine(Of PlaybackState).CurrentState
        Get
            Return CType(Interlocked.CompareExchange(_currentState, 0, 0), PlaybackState)
        End Get
    End Property
    
#End Region
    
#Region "Constructor"
    
    ''' <summary>
    ''' Creates a new PlaybackSSM
    ''' </summary>
    ''' <param name="audioRouter">AudioRouter instance to control</param>
    ''' <param name="globalStateMachine">GlobalStateMachine to subscribe to</param>
    Public Sub New(audioRouter As AudioIO.AudioRouter, globalStateMachine As GlobalStateMachine)
        If audioRouter Is Nothing Then Throw New ArgumentNullException(NameOf(audioRouter))
        If globalStateMachine Is Nothing Then Throw New ArgumentNullException(NameOf(globalStateMachine))
        
        _audioRouter = audioRouter
        _globalStateMachine = globalStateMachine
        
        ' Initialize in Uninitialized state
        Interlocked.Exchange(_currentState, PlaybackState.Uninitialized)
        
        ' Subscribe to GlobalStateMachine transitions
        AddHandler _globalStateMachine.StateChanged, AddressOf OnGlobalStateChanged
        
        Utils.Logger.Instance.Info("PlaybackSSM created and subscribed to GlobalStateMachine", "PlaybackSSM")
    End Sub
    
#End Region
    
#Region "Public Methods"
    
    ''' <summary>
    ''' Attempts to transition to a new state
    ''' </summary>
    ''' <param name="newState">Target state</param>
    ''' <param name="reason">Reason for transition</param>
    ''' <returns>True if transition succeeded</returns>
    Public Function TransitionTo(newState As PlaybackState, reason As String) As Boolean Implements IStateMachine(Of PlaybackState).TransitionTo
        SyncLock _stateLock
            Dim oldState = CurrentState
            
            ' Check if transition is valid
            If Not IsValidTransition(oldState, newState) Then
                Utils.Logger.Instance.Warning($"PlaybackSSM: Invalid transition {oldState} ? {newState} (Reason: {reason})", "PlaybackSSM")
                Return False
            End If
            
            ' Perform state entry/exit actions
            OnStateExiting(oldState, newState)
            
            ' Update state (thread-safe)
            Interlocked.Exchange(_currentState, newState)
            
            OnStateEntering(oldState, newState)
            
            ' Create event args
            Dim args As New StateChangedEventArgs(Of PlaybackState)(oldState, newState, reason)
            
            ' Log transition
            Utils.Logger.Instance.Info($"PlaybackSSM: {args}", "PlaybackSSM")
            
            ' Fire event
            RaiseEvent StateChanged(Me, args)
            
            Return True
        End SyncLock
    End Function
    
    ''' <summary>
    ''' Checks if a state transition is valid
    ''' </summary>
    Public Function IsValidTransition(fromState As PlaybackState, toState As PlaybackState) As Boolean Implements IStateMachine(Of PlaybackState).IsValidTransition
        ' Same state is always valid
        If fromState = toState Then Return True
        
        Select Case fromState
            Case PlaybackState.Uninitialized
                ' Can only go to Idle (initialization)
                Return toState = PlaybackState.Idle
                
            Case PlaybackState.Idle
                ' Can start playing or go to error
                Return toState = PlaybackState.Playing OrElse
                       toState = PlaybackState.Error
                
            Case PlaybackState.Playing
                ' Can stop or encounter error
                Return toState = PlaybackState.Stopping OrElse
                       toState = PlaybackState.Error
                
            Case PlaybackState.Stopping
                ' Must return to Idle
                Return toState = PlaybackState.Idle OrElse
                       toState = PlaybackState.Error
                
            Case PlaybackState.Error
                ' Can recover to Idle
                Return toState = PlaybackState.Idle
                
            Case Else
                Return False
        End Select
    End Function
    
#End Region
    
#Region "Private Methods - GlobalStateMachine Integration"
    
    ''' <summary>
    ''' Responds to GlobalStateMachine state changes
    ''' Maps global transitions to Playback transitions
    ''' </summary>
    Private Sub OnGlobalStateChanged(sender As Object, e As StateChangedEventArgs(Of GlobalState))
        ' Map global state to Playback state
        Select Case e.NewState
            Case GlobalState.Uninitialized
                ' System initializing
                TransitionTo(PlaybackState.Uninitialized, "Global: Uninitialized")
                
            Case GlobalState.Idle
                ' System idle - playback should be idle
                If CurrentState <> PlaybackState.Idle Then
                    ' If we're playing, stop first
                    If CurrentState = PlaybackState.Playing Then
                        TransitionTo(PlaybackState.Stopping, "Global: Idle")
                    Else
                        TransitionTo(PlaybackState.Idle, "Global: Idle")
                    End If
                End If
                
            Case GlobalState.Arming, GlobalState.Armed, GlobalState.Recording
                ' Global is recording - playback stays idle
                ' (Recording and playback are mutually exclusive)
                If CurrentState = PlaybackState.Playing Then
                    ' Stop playback if recording starts
                    TransitionTo(PlaybackState.Stopping, "Global: Recording started")
                End If
                
            Case GlobalState.Stopping
                ' Global is stopping - stop playback if playing
                If CurrentState = PlaybackState.Playing Then
                    TransitionTo(PlaybackState.Stopping, "Global: Stopping")
                End If
                
            Case GlobalState.Playing
                ' Global is playing - START playback!
                If CurrentState = PlaybackState.Idle Then
                    TransitionTo(PlaybackState.Playing, "Global: Playing")
                End If
                
            Case GlobalState.Error
                ' Global error - transition to error
                TransitionTo(PlaybackState.Error, "Global: Error")
        End Select
    End Sub
    
    ''' <summary>
    ''' Called when exiting a state
    ''' </summary>
    Private Sub OnStateExiting(oldState As PlaybackState, newState As PlaybackState)
        Select Case oldState
            Case PlaybackState.Playing
                ' Exiting playing state - stop will be handled in OnStateEntering(Stopping)
        End Select
    End Sub
    
    ''' <summary>
    ''' Called when entering a state
    ''' This is where we call AudioRouter methods (OWNERSHIP: we control playback actions)
    ''' </summary>
    Private Sub OnStateEntering(oldState As PlaybackState, newState As PlaybackState)
        Try
            Select Case newState
                Case PlaybackState.Idle
                    ' Entered idle state - ensure playback stopped
                    If oldState = PlaybackState.Stopping Then
                        Utils.Logger.Instance.Info("Playback stopped and idle", "PlaybackSSM")
                    End If
                    
                Case PlaybackState.Playing
                    ' Start playback - call AudioRouter.StartPlayback()
                    Utils.Logger.Instance.Info("PlaybackSSM: Starting playback...", "PlaybackSSM")
                    
                    ' Note: AudioRouter.StartPlayback() signature may vary
                    ' This is a placeholder - actual method may need parameters
                    ' Phase 5 (Step 24) will wire this correctly
                    ' For now, just log the action
                    Utils.Logger.Instance.Info("Playback started (AudioRouter integration pending Phase 5)", "PlaybackSSM")
                    
                Case PlaybackState.Stopping
                    ' Stop playback - call AudioRouter.StopPlayback()
                    Utils.Logger.Instance.Info("PlaybackSSM: Stopping playback...", "PlaybackSSM")
                    
                    ' Note: AudioRouter.StopPlayback() signature may vary
                    ' This is a placeholder - actual method may need parameters
                    ' Phase 5 (Step 24) will wire this correctly
                    ' For now, just log the action
                    Utils.Logger.Instance.Info("Playback stopped (AudioRouter integration pending Phase 5)", "PlaybackSSM")
                    
                    ' After stop completes, transition to Idle
                    TransitionTo(PlaybackState.Idle, "Playback stopped")
                    
                Case PlaybackState.Error
                    ' Error state - ensure playback is stopped
                    Utils.Logger.Instance.Error("PlaybackSSM entered error state", Nothing, "PlaybackSSM")
                    
                    ' Try to stop playback safely
                    Try
                        ' Placeholder for AudioRouter.StopPlayback()
                        Utils.Logger.Instance.Info("Attempting to stop playback during error recovery", "PlaybackSSM")
                    Catch ex As Exception
                        Utils.Logger.Instance.Error("Failed to stop playback during error recovery", ex, "PlaybackSSM")
                    End Try
            End Select
            
        Catch ex As Exception
            ' If any AudioRouter call fails, transition to error
            Utils.Logger.Instance.Error($"PlaybackSSM: Exception in OnStateEntering({newState})", ex, "PlaybackSSM")
            If CurrentState <> PlaybackState.Error Then
                TransitionTo(PlaybackState.Error, $"Exception: {ex.Message}")
            End If
        End Try
    End Sub
    
#End Region
    
End Class

''' <summary>
''' Playback-specific states
''' These states track playback lifecycle
''' </summary>
Public Enum PlaybackState
    ''' <summary>Not yet initialized</summary>
    Uninitialized = 0
    
    ''' <summary>Idle (not playing)</summary>
    Idle = 1
    
    ''' <summary>Currently playing back audio</summary>
    Playing = 2
    
    ''' <summary>Stopping playback</summary>
    Stopping = 3
    
    ''' <summary>Error state</summary>
    [Error] = 4
End Enum
