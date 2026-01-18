''' <summary>
''' Common interface for all cognitive systems
''' Cognitive systems observe state machines without affecting core logic
''' Zero core impact guarantee - purely observational
''' </summary>
Public Interface ICognitiveSystem
    Inherits IDisposable

    ''' <summary>Name of cognitive system</summary>
    ReadOnly Property Name As String

    ''' <summary>Is system currently enabled</summary>
    Property Enabled As Boolean

    ''' <summary>Initialize cognitive system</summary>
    ''' <param name="coordinator">State coordinator to observe</param>
    Sub Initialize(coordinator As StateCoordinator)

    ''' <summary>Process state change event</summary>
    ''' <param name="transitionID">Unique transition ID</param>
    ''' <param name="oldState">Previous state</param>
    ''' <param name="newState">New state</param>
    Sub OnStateChanged(transitionID As String, oldState As Object, newState As Object)

    ''' <summary>Get system statistics</summary>
    ''' <returns>Anonymous object with system metrics</returns>
    Function GetStatistics() As Object

    ''' <summary>Reset system state</summary>
    Sub Reset()

End Interface
