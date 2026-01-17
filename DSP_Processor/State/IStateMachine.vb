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
    ''' Creates state change event args
    ''' </summary>
    ''' <param name="oldState">Previous state</param>
    ''' <param name="newState">New current state</param>
    ''' <param name="reason">Reason for transition</param>
    Public Sub New(oldState As TState, newState As TState, reason As String)
        Me.OldState = oldState
        Me.NewState = newState
        Me.Reason = If(reason, "No reason provided")
        Me.Timestamp = DateTime.Now
    End Sub

    ''' <summary>
    ''' Returns formatted string for logging
    ''' </summary>
    Public Overrides Function ToString() As String
        Return $"[{Timestamp:HH:mm:ss.fff}] {OldState} → {NewState} (Reason: {Reason})"
    End Function

End Class
