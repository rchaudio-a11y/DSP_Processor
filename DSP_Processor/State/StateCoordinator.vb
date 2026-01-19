Imports System.Threading
Imports System.Windows.Forms

''' <summary>
''' State Coordinator - Central coordination point for all state machines
''' Singleton pattern - single global instance
''' Ownership: Creates and coordinates all state machines, but does NOT own subsystems (RecordingManager, DSPThread, etc.)
''' Thread-safe: All operations thread-safe
''' </summary>
Public Class StateCoordinator
    Implements IDisposable

    ' Singleton instance
    Private Shared ReadOnly _instance As New Lazy(Of StateCoordinator)(Function() New StateCoordinator(), LazyThreadSafetyMode.ExecutionAndPublication)

    ' Disposed flag (thread-safe using Interlocked)
    Private _disposed As Integer = 0

    ' State machines (owned by StateCoordinator)
    Private _globalStateMachine As GlobalStateMachine
    Private _recordingManagerSSM As RecordingManagerSSM
    Private _dspThreadSSM As DSPThreadSSM
    Private _uiStateMachine As UIStateMachine
    Private _playbackSSM As PlaybackSSM
    Private _audioDeviceSSM As State.AudioDeviceSSM
    Private _audioInputSSM As State.AudioInputSSM
    Private _dspModeSSM As State.DSPModeSSM
    Private _audioRoutingSSM As State.AudioRoutingSSM

    ' Subsystem references (NOT owned - just references for initialization)
    ' These will be set during Initialize() call
    Private _recordingManager As Managers.RecordingManager
    Private _dspThread As DSP.DSPThread
    Private _audioRouter As AudioIO.AudioRouter
    Private _mainForm As Form

#Region "Singleton"

    ''' <summary>
    ''' Gets the singleton StateCoordinator instance
    ''' </summary>
    Public Shared ReadOnly Property Instance As StateCoordinator
        Get
            Return _instance.Value
        End Get
    End Property

    ''' <summary>
    ''' Private constructor for singleton
    ''' </summary>
    Private Sub New()
        Utils.Logger.Instance.Info("StateCoordinator created (singleton)", "StateCoordinator")
    End Sub

#End Region

#Region "Properties - State Machine Access"

    ''' <summary>
    ''' Gets the GlobalStateMachine
    ''' </summary>
    Public ReadOnly Property GlobalStateMachine As GlobalStateMachine
        Get
            CheckDisposed()
            Return _globalStateMachine
        End Get
    End Property

    ''' <summary>
    ''' Gets the RecordingManagerSSM
    ''' </summary>
    Public ReadOnly Property RecordingManagerSSM As RecordingManagerSSM
        Get
            CheckDisposed()
            Return _recordingManagerSSM
        End Get
    End Property

    ''' <summary>
    ''' Gets the DSPThreadSSM
    ''' </summary>
    Public ReadOnly Property DSPThreadSSM As DSPThreadSSM
        Get
            CheckDisposed()
            Return _dspThreadSSM
        End Get
    End Property

    ''' <summary>
    ''' Gets the UIStateMachine
    ''' </summary>
    Public ReadOnly Property UIStateMachine As UIStateMachine
        Get
            CheckDisposed()
            Return _uiStateMachine
        End Get
    End Property

    ''' <summary>
    ''' Gets the PlaybackSSM
    ''' </summary>
    Public ReadOnly Property PlaybackSSM As PlaybackSSM
        Get
            CheckDisposed()
            Return _playbackSSM
        End Get
    End Property

    ''' <summary>
    ''' Gets the AudioDeviceSSM
    ''' </summary>
    Public ReadOnly Property AudioDeviceSSM As State.AudioDeviceSSM
        Get
            CheckDisposed()
            Return _audioDeviceSSM
        End Get
    End Property

    ''' <summary>
    ''' Gets the AudioInputSSM
    ''' </summary>
    Public ReadOnly Property AudioInputSSM As State.AudioInputSSM
        Get
            CheckDisposed()
            Return _audioInputSSM
        End Get
    End Property

    ''' <summary>
    ''' Gets the DSPModeSSM
    ''' </summary>
    Public ReadOnly Property DSPModeSSM As State.DSPModeSSM
        Get
            CheckDisposed()
            Return _dspModeSSM
        End Get
    End Property

    ''' <summary>
    ''' Gets the AudioRoutingSSM
    ''' </summary>
    Public ReadOnly Property AudioRoutingSSM As State.AudioRoutingSSM
        Get
            CheckDisposed()
            Return _audioRoutingSSM
        End Get
    End Property

    ''' <summary>
    ''' Gets the current global state (convenience property)
    ''' </summary>
    Public ReadOnly Property GlobalState As GlobalState
        Get
            CheckDisposed()
            Return _globalStateMachine?.CurrentState
        End Get
    End Property

    ''' <summary>
    ''' Gets whether system is initialized
    ''' </summary>
    Public ReadOnly Property IsInitialized As Boolean
        Get
            Return _globalStateMachine IsNot Nothing AndAlso
                   _globalStateMachine.CurrentState <> GlobalState.Uninitialized
        End Get
    End Property

#End Region

#Region "Initialization"

    ''' <summary>
    ''' Initializes the StateCoordinator and all state machines
    ''' Must be called once during application startup
    ''' Transitions from Uninitialized → Idle
    ''' </summary>
    ''' <param name="recordingManager">RecordingManager instance</param>
    ''' <param name="dspThread">DSPThread instance for recording (can be Nothing - will be wired after mic armed)</param>
    ''' <param name="audioRouter">AudioRouter instance for playback</param>
    ''' <param name="mainForm">MainForm for UI thread marshaling</param>
    Public Sub Initialize(recordingManager As Managers.RecordingManager,
                         dspThread As DSP.DSPThread,
                         audioRouter As AudioIO.AudioRouter,
                         mainForm As Form)

        CheckDisposed()

        ' Validate required parameters (dspThread can be Nothing)
        If recordingManager Is Nothing Then Throw New ArgumentNullException(NameOf(recordingManager))
        If audioRouter Is Nothing Then Throw New ArgumentNullException(NameOf(audioRouter))
        If mainForm Is Nothing Then Throw New ArgumentNullException(NameOf(mainForm))

        ' Prevent double initialization
        If _globalStateMachine IsNot Nothing Then
            Utils.Logger.Instance.Warning("StateCoordinator already initialized", "StateCoordinator")
            Return
        End If

        Utils.Logger.Instance.Info("Initializing StateCoordinator...", "StateCoordinator")

        ' Store subsystem references
        _recordingManager = recordingManager
        _dspThread = dspThread
        _audioRouter = audioRouter
        _mainForm = mainForm

        ' Create GlobalStateMachine first (others depend on it)
        _globalStateMachine = New GlobalStateMachine()
        Utils.Logger.Instance.Info("GlobalStateMachine created", "StateCoordinator")

        ' Create Satellite State Machines (subscribe to GSM)
        _recordingManagerSSM = New RecordingManagerSSM(recordingManager, _globalStateMachine)
        Utils.Logger.Instance.Info("RecordingManagerSSM created", "StateCoordinator")

        ' Create DSPThreadSSM only if dspThread is available
        If dspThread IsNot Nothing Then
            _dspThreadSSM = New DSPThreadSSM(dspThread, _recordingManagerSSM)
            Utils.Logger.Instance.Info("DSPThreadSSM created", "StateCoordinator")
        Else
            Utils.Logger.Instance.Info("DSPThreadSSM deferred - will be created after microphone arming", "StateCoordinator")
        End If

        _playbackSSM = New PlaybackSSM(audioRouter, _globalStateMachine)
        Utils.Logger.Instance.Info("PlaybackSSM created", "StateCoordinator")

        ' Create AudioDeviceSSM (manages driver backend)
        _audioDeviceSSM = New State.AudioDeviceSSM()
        Utils.Logger.Instance.Info("AudioDeviceSSM created", "StateCoordinator")

        ' Create AudioInputSSM (manages device selection)
        _audioInputSSM = New State.AudioInputSSM()
        Utils.Logger.Instance.Info("AudioInputSSM created", "StateCoordinator")

        ' Create DSPModeSSM (manages DSP enable/disable)
        _dspModeSSM = New State.DSPModeSSM()
        Utils.Logger.Instance.Info("DSPModeSSM created", "StateCoordinator")

        ' Create AudioRoutingSSM (manages routing topology + TAP POINTS!)
        _audioRoutingSSM = New State.AudioRoutingSSM(audioRouter, _recordingManagerSSM, _playbackSSM)
        Utils.Logger.Instance.Info("AudioRoutingSSM created", "StateCoordinator")

        ' Create UIStateMachine (subscribes to GSM)
        _uiStateMachine = New UIStateMachine(_globalStateMachine, mainForm)
        Utils.Logger.Instance.Info("UIStateMachine created", "StateCoordinator")

        ' Initialize AudioDeviceSSM to default driver (WASAPI)
        If Not _audioDeviceSSM.Initialize() Then
            Utils.Logger.Instance.Warning("AudioDeviceSSM initialization failed - continuing with default", "StateCoordinator")
        End If

        ' Initialize AudioInputSSM to default device
        If Not _audioInputSSM.Initialize() Then
            Utils.Logger.Instance.Warning("AudioInputSSM initialization failed - continuing with default", "StateCoordinator")
        End If

        ' Initialize DSPModeSSM to default mode (DISABLED)
        If Not _dspModeSSM.Initialize() Then
            Utils.Logger.Instance.Warning("DSPModeSSM initialization failed - continuing with default", "StateCoordinator")
        End If

        ' Initialize AudioRoutingSSM to default state (DISABLED)
        If Not _audioRoutingSSM.Initialize() Then
            Utils.Logger.Instance.Warning("AudioRoutingSSM initialization failed - continuing with default", "StateCoordinator")
        End If

        ' Transition from Uninitialized → Idle (system ready)
        Dim success = _globalStateMachine.TransitionTo(GlobalState.Idle, "StateCoordinator initialized")

        If success Then
            Utils.Logger.Instance.Info("StateCoordinator initialization complete - system IDLE", "StateCoordinator")
        Else
            Utils.Logger.Instance.Error("Failed to transition to Idle state", Nothing, "StateCoordinator")
            Throw New InvalidOperationException("StateCoordinator initialization failed")
        End If

    End Sub

#End Region

#Region "Public API - State Queries"

    ''' <summary>
    ''' Gets a snapshot of all state machine states (for State Debugger Panel)
    ''' Thread-safe snapshot
    ''' </summary>
    Public Function GetSystemState() As SystemStateSnapshot
        CheckDisposed()

        If Not IsInitialized Then
            Return New SystemStateSnapshot() ' Return empty snapshot if not initialized
        End If

        ' Create snapshot of all state machines
        Return New SystemStateSnapshot With {
            .GlobalState = _globalStateMachine.CurrentState,
            .RecordingState = _recordingManagerSSM.CurrentState,
            .DSPState = _dspThreadSSM.CurrentState,
            .UIState = _uiStateMachine.CurrentState,
            .PlaybackState = _playbackSSM.CurrentState,
            .AudioDeviceState = _audioDeviceSSM.CurrentState,
            .AudioInputState = _audioInputSSM.CurrentState,
            .DSPModeState = _dspModeSSM.CurrentState,
            .RoutingState = _audioRoutingSSM.CurrentState,
            .Timestamp = DateTime.Now
        }
    End Function

    ''' <summary>
    ''' Gets the transition history from GlobalStateMachine (for debugging)
    ''' </summary>
    Public Function GetTransitionHistory() As IReadOnlyList(Of StateChangedEventArgs(Of GlobalState))
        CheckDisposed()

        If _globalStateMachine Is Nothing Then
            Dim emptyList As New List(Of StateChangedEventArgs(Of GlobalState))
            Return emptyList.AsReadOnly()
        End If

        Return _globalStateMachine.GetTransitionHistory()
    End Function

    ''' <summary>
    ''' Dumps all state machine states to a formatted string (for logging/debugging)
    ''' </summary>
    Public Function DumpAllStates() As String
        CheckDisposed()

        If Not IsInitialized Then
            Return "StateCoordinator not initialized"
        End If

        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine("=== State Machine Status ===")
        sb.AppendLine($"Global State:          {_globalStateMachine.CurrentState}")
        sb.AppendLine($"RecordingManager:      {_recordingManagerSSM.CurrentState}")
        sb.AppendLine($"DSPThread:             {_dspThreadSSM.CurrentState}")
        sb.AppendLine($"UI State:              {_uiStateMachine.CurrentState}")
        sb.AppendLine($"Playback:              {_playbackSSM.CurrentState}")
        sb.AppendLine($"Timestamp:             {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}")

        ' Add transition history summary
        Dim history = _globalStateMachine.GetTransitionHistory()
        sb.AppendLine($"Transition History:    {history.Count} transitions")

        If history.Count > 0 Then
            sb.AppendLine("Last 5 transitions:")
            For i = Math.Max(0, history.Count - 5) To history.Count - 1
                sb.AppendLine($"  {history(i)}")
            Next
        End If

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Attempts to recover from Error state (transitions Error → Idle)
    ''' For use by State Debugger Panel "Recover" button
    ''' </summary>
    Public Function RecoverFromError() As Boolean
        CheckDisposed()

        If Not IsInitialized Then
            Utils.Logger.Instance.Warning("Cannot recover - StateCoordinator not initialized", "StateCoordinator")
            Return False
        End If

        If _globalStateMachine.CurrentState <> GlobalState.Error Then
            Utils.Logger.Instance.Warning($"Cannot recover - not in Error state (current: {_globalStateMachine.CurrentState})", "StateCoordinator")
            Return False
        End If

        Utils.Logger.Instance.Info("Attempting error recovery...", "StateCoordinator")

        ' Transition back to Idle
        Dim success = _globalStateMachine.TransitionTo(GlobalState.Idle, "Manual error recovery")

        If success Then
            Utils.Logger.Instance.Info("Error recovery successful - returned to Idle", "StateCoordinator")
        Else
            Utils.Logger.Instance.Error("Error recovery failed", Nothing, "StateCoordinator")
        End If

        Return success
    End Function

#End Region

#Region "Disposal"

    ''' <summary>
    ''' Disposes StateCoordinator and all state machines
    ''' Implements shutdown barrier pattern (50ms grace period)
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Atomic test-and-set (prevents double disposal)
        If Interlocked.CompareExchange(_disposed, 1, 0) = 1 Then
            Return ' Already disposed
        End If

        Utils.Logger.Instance.Info("StateCoordinator disposing...", "StateCoordinator")

        ' Grace period (50ms) - let any in-flight transitions complete
        ' This is the shutdown barrier pattern from Thread-Safety-Patterns.md Part 13
        Thread.Sleep(50)

        ' Dispose state machines in reverse order of creation
        Try
            ' UIStateMachine (unsubscribe from GSM)
            _uiStateMachine = Nothing

            ' PlaybackSSM (unsubscribe from GSM)
            _playbackSSM = Nothing

            ' DSPThreadSSM (unsubscribe from RecordingManagerSSM)
            _dspThreadSSM = Nothing

            ' RecordingManagerSSM (unsubscribe from GSM)
            _recordingManagerSSM = Nothing

            ' GlobalStateMachine (last)
            _globalStateMachine = Nothing

            Utils.Logger.Instance.Info("StateCoordinator disposed successfully", "StateCoordinator")

        Catch ex As Exception
            Utils.Logger.Instance.Error("Error during StateCoordinator disposal", ex, "StateCoordinator")
        End Try

        ' Clear subsystem references (we don't own them, so don't dispose)
        _recordingManager = Nothing
        _dspThread = Nothing
        _audioRouter = Nothing
        _mainForm = Nothing
    End Sub

    ''' <summary>
    ''' Checks if disposed and throws if so (disposal guard pattern)
    ''' </summary>
    Private Sub CheckDisposed()
        If Interlocked.CompareExchange(_disposed, 0, 0) = 1 Then
            Throw New ObjectDisposedException(NameOf(StateCoordinator))
        End If
    End Sub

#End Region

End Class

''' <summary>
''' Snapshot of all state machine states at a point in time
''' Immutable snapshot for State Debugger Panel
''' </summary>
Public Class SystemStateSnapshot
    ''' <summary>Global state machine state</summary>
    Public Property GlobalState As GlobalState

    ''' <summary>RecordingManager satellite state machine state</summary>
    Public Property RecordingState As RecordingManagerState

    ''' <summary>DSPThread satellite state machine state</summary>
    Public Property DSPState As DSPThreadState

    ''' <summary>UI state machine state</summary>
    Public Property UIState As UIState

    ''' <summary>Playback satellite state machine state</summary>
    Public Property PlaybackState As PlaybackState

    ''' <summary>AudioDevice satellite state machine state</summary>
    Public Property AudioDeviceState As State.AudioDeviceState

    ''' <summary>AudioInput satellite state machine state</summary>
    Public Property AudioInputState As State.AudioInputState

    ''' <summary>DSP Mode satellite state machine state</summary>
    Public Property DSPModeState As State.DSPModeState

    ''' <summary>AudioRouting satellite state machine state</summary>
    Public Property RoutingState As State.AudioRoutingState

    ''' <summary>Timestamp when snapshot was taken</summary>
    Public Property Timestamp As DateTime

    ''' <summary>
    ''' Returns formatted string for display
    ''' </summary>
    Public Overrides Function ToString() As String
        Return $"[{Timestamp:HH:mm:ss.fff}] Global:{GlobalState} Rec:{RecordingState} DSP:{DSPState} UI:{UIState} Play:{PlaybackState} Audio:{AudioDeviceState} Input:{AudioInputState} Mode:{DSPModeState} Route:{RoutingState}"
    End Function
End Class
