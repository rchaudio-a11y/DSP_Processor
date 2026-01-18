''' <summary>
''' Generic state machine interface
''' Provides thread-safe state transitions with validation and event notifications
''' </summary>
''' <typeparam name="TState">Enum type representing states (must be Structure/Enum)</typeparam>
Public Interface IStateMachine(Of TState As Structure)

    ''' <summary>Gets the current state (thread-safe)</summary>
    ReadOnly Property CurrentState As TState

    ''' <summary>
    ''' Attempts to transition to a new state
    ''' </summary>
    ''' <param name="newState">Target state</param>
    ''' <param name="reason">Reason for transition (for logging/debugging)</param>
    ''' <returns>True if transition succeeded, False if invalid</returns>
    Function TransitionTo(newState As TState, reason As String) As Boolean

    ''' <summary>
    ''' Checks if a state transition is valid
    ''' </summary>
    ''' <param name="fromState">Starting state</param>
    ''' <param name="toState">Target state</param>
    ''' <returns>True if transition is allowed by state diagram</returns>
    Function IsValidTransition(fromState As TState, toState As TState) As Boolean

    ''' <summary>
    ''' Fired when state changes successfully
    ''' Event fires AFTER state has changed and is thread-safe
    ''' </summary>
    Event StateChanged As EventHandler(Of StateChangedEventArgs(Of TState))

End Interface

''' <summary>
''' Event arguments for state change notifications
''' Immutable snapshot of state transition
''' Enhanced with State Registry Pattern UIDs and TransitionIDs
''' </summary>
''' <typeparam name="TState">Enum type representing states</typeparam>
Public Class StateChangedEventArgs(Of TState As Structure)
    Inherits EventArgs

    ''' <summary>State before transition</summary>
    Public ReadOnly Property OldState As TState

    ''' <summary>State after transition</summary>
    Public ReadOnly Property NewState As TState

    ''' <summary>Reason for transition (e.g., "User clicked Record", "Error occurred")</summary>
    Public ReadOnly Property Reason As String

    ''' <summary>Timestamp when transition occurred</summary>
    Public ReadOnly Property Timestamp As DateTime

    ''' <summary>
    ''' Unique transition ID for this specific transition instance
    ''' Format: {PREFIX}_T{Number}_{OldState}_TO_{NewState}
    ''' Example: GSM_T01_IDLE_TO_ARMING
    ''' </summary>
    Public ReadOnly Property TransitionID As String

    ''' <summary>
    ''' UID of the old state from Description attribute
    ''' Example: GSM_IDLE, REC_RECORDING
    ''' </summary>
    Public ReadOnly Property OldStateUID As String

    ''' <summary>
    ''' UID of the new state from Description attribute
    ''' Example: GSM_ARMING, REC_RECORDING
    ''' </summary>
    Public ReadOnly Property NewStateUID As String

    ''' <summary>
    ''' Creates state change event args (legacy constructor for backward compatibility)
    ''' </summary>
    ''' <param name="oldState">Previous state</param>
    ''' <param name="newState">New current state</param>
    ''' <param name="reason">Reason for transition</param>
    Public Sub New(oldState As TState, newState As TState, reason As String)
        Me.OldState = oldState
        Me.NewState = newState
        Me.Reason = If(reason, "No reason provided")
        Me.Timestamp = DateTime.Now
        Me.TransitionID = ""
        Me.OldStateUID = oldState.ToString()
        Me.NewStateUID = newState.ToString()
    End Sub

    ''' <summary>
    ''' Creates state change event args with full State Registry Pattern support
    ''' </summary>
    ''' <param name="oldState">Previous state</param>
    ''' <param name="newState">New current state</param>
    ''' <param name="reason">Reason for transition</param>
    ''' <param name="transitionID">Unique transition ID</param>
    ''' <param name="oldStateUID">UID of old state from Description attribute</param>
    ''' <param name="newStateUID">UID of new state from Description attribute</param>
    Public Sub New(oldState As TState, newState As TState, reason As String, 
                   transitionID As String, oldStateUID As String, newStateUID As String)
        Me.OldState = oldState
        Me.NewState = newState
        Me.Reason = If(reason, "No reason provided")
        Me.Timestamp = DateTime.Now
        Me.TransitionID = transitionID
        Me.OldStateUID = oldStateUID
        Me.NewStateUID = newStateUID
    End Sub

    ''' <summary>
    ''' Returns formatted string for logging
    ''' Optimized for grep-friendly State Registry Pattern format
    ''' Example: [22:38:57.805] [GSM] T01: GSM_IDLE → GSM_ARMING (User clicked Record)
    ''' </summary>
    Public Overrides Function ToString() As String
        If String.IsNullOrEmpty(TransitionID) Then
            ' Legacy format (no TransitionID)
            Return $"[{Timestamp:HH:mm:ss.fff}] {OldState} → {NewState} ({Reason})"
        Else
            ' Enhanced State Registry Pattern format
            ' Extract machine prefix and transition number from full TransitionID
            ' TransitionID format: "GSM_T01_GSM_IDLE_TO_GSM_ARMING"
            ' Extract: [GSM] T01
            Dim parts = TransitionID.Split("_"c)
            Dim machinePrefix = If(parts.Length > 0, parts(0), "???")
            Dim transNum = If(parts.Length > 1, parts(1), "???")
            
            ' Format: [timestamp] [MACHINE] TXX: OLD_STATE → NEW_STATE (reason)
            Return $"[{Timestamp:HH:mm:ss.fff}] [{machinePrefix}] {transNum}: {OldStateUID} → {NewStateUID} ({Reason})"
        End If
    End Function

End Class
