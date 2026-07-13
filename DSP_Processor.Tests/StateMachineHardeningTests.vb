Imports System.Threading
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' Feature 002 (state-machine-hardening) tests - FR-013.
''' US1: truthful lossless deferral. US2: delivery guarantees. US3: coordinator surface.
''' Oracle: specs/002-state-machine-hardening/data-model.md invariants 1-10.
''' </summary>
<TestClass>
Public Class StateMachineHardeningTests

    ''' <summary>Drives a fresh machine to Idle (the hub state for scenarios).</summary>
    Private Shared Function NewMachineAtIdle() As GlobalStateMachine
        Dim gsm As New GlobalStateMachine()
        Assert.IsTrue(gsm.TransitionTo(GlobalState.Idle, "test setup"))
        Return gsm
    End Function

#Region "US1 - Truthful, lossless deferral (FR-001/002/003/004/005)"

    <TestMethod>
    Public Sub Deferral_ThreeRequestsInWindow_AllExecuteInOrder()
        Dim gsm As New GlobalStateMachine()
        Dim events As New List(Of String)
        Dim inHandlerOutcomes As New List(Of TransitionOutcome)
        Dim cascaded = False

        AddHandler gsm.StateChanged,
            Sub(sender, args)
                events.Add($"{args.OldState}->{args.NewState}")
                If Not cascaded Then
                    cascaded = True
                    ' Three deferrals from inside the first window - a chain
                    ' where each is valid only from the state its predecessor
                    ' produces (proves FIFO + late validation together)
                    inHandlerOutcomes.Add(gsm.RequestTransition(GlobalState.Playing, "cascade 1"))
                    inHandlerOutcomes.Add(gsm.RequestTransition(GlobalState.Stopping, "cascade 2"))
                    inHandlerOutcomes.Add(gsm.RequestTransition(GlobalState.Idle, "cascade 3"))
                End If
            End Sub

        Dim performed = gsm.TransitionTo(GlobalState.Idle, "opens window")

        Assert.IsTrue(performed, "the original transition was performed by this call")
        CollectionAssert.AreEqual(
            {TransitionOutcome.Deferred, TransitionOutcome.Deferred, TransitionOutcome.Deferred}.ToList(),
            inHandlerOutcomes, "all in-window requests must be truthfully reported as Deferred")
        CollectionAssert.AreEqual(
            {"Uninitialized->Idle", "Idle->Playing", "Playing->Stopping", "Stopping->Idle"}.ToList(),
            events, "all deferred requests must execute, in arrival order, none lost")
        Assert.AreEqual(GlobalState.Idle, gsm.CurrentState)
    End Sub

    <TestMethod>
    Public Sub Outcomes_Direct_Performed_Rejected_TruthfulOnBothSurfaces()
        Dim gsm = NewMachineAtIdle()

        ' Direct valid: Performed / True
        Assert.AreEqual(TransitionOutcome.Performed, gsm.RequestTransition(GlobalState.Playing, "valid"))
        Assert.AreEqual(GlobalState.Playing, gsm.CurrentState)
        Assert.IsTrue(gsm.TransitionTo(GlobalState.Stopping, "valid boolean"))

        ' Direct invalid: Rejected / False, state unchanged
        Assert.AreEqual(TransitionOutcome.Rejected, gsm.RequestTransition(GlobalState.Recording, "invalid from Stopping"))
        Assert.AreEqual(GlobalState.Stopping, gsm.CurrentState, "rejected transition must leave state unchanged")
        Assert.IsFalse(gsm.TransitionTo(GlobalState.Playing, "invalid boolean from Stopping"))
        Assert.AreEqual(GlobalState.Stopping, gsm.CurrentState)
    End Sub

    <TestMethod>
    Public Sub Outcome_InWindow_DeferredThenExecuted()
        Dim gsm As New GlobalStateMachine()
        Dim inHandlerOutcome As TransitionOutcome = TransitionOutcome.Rejected
        Dim inHandlerBooleanResult As Boolean = True
        Dim fired = False

        AddHandler gsm.StateChanged,
            Sub(sender, args)
                If Not fired Then
                    fired = True
                    inHandlerOutcome = gsm.RequestTransition(GlobalState.Playing, "deferred cascade")
                    ' The Boolean surface must also be truthful in-window:
                    inHandlerBooleanResult = gsm.TransitionTo(GlobalState.Stopping, "boolean cascade")
                End If
            End Sub

        gsm.TransitionTo(GlobalState.Idle, "opens window")

        Assert.AreEqual(TransitionOutcome.Deferred, inHandlerOutcome)
        Assert.IsFalse(inHandlerBooleanResult, "Boolean surface must return False for a deferred (not-yet-performed) request - the old lie is dead")
        Assert.AreEqual(GlobalState.Stopping, gsm.CurrentState, "both deferred requests must have executed after the window drained")
    End Sub

    <TestMethod>
    Public Sub DeferredRequest_InvalidAtExecution_RejectedVisibly()
        Dim gsm As New GlobalStateMachine()
        Dim events As New List(Of String)
        Dim cascaded = False

        AddHandler gsm.StateChanged,
            Sub(sender, args)
                events.Add($"{args.OldState}->{args.NewState}")
                If Not cascaded Then
                    cascaded = True
                    gsm.RequestTransition(GlobalState.Playing, "valid from Idle")     ' will perform
                    gsm.RequestTransition(GlobalState.Recording, "INVALID from Playing") ' will be rejected at execution
                End If
            End Sub

        gsm.TransitionTo(GlobalState.Idle, "opens window")

        CollectionAssert.AreEqual(
            {"Uninitialized->Idle", "Idle->Playing"}.ToList(), events,
            "the invalid deferred request must produce NO event")
        Assert.AreEqual(GlobalState.Playing, gsm.CurrentState, "rejected deferred request must leave state unchanged")
    End Sub

    <TestMethod>
    Public Sub QueueDepthDiagnostic_LoudWarningPast16()
        Dim gsm As New GlobalStateMachine()
        Dim eventCount = 0
        Dim cascaded = False

        AddHandler gsm.StateChanged,
            Sub(sender, args)
                eventCount += 1
                If Not cascaded Then
                    cascaded = True
                    ' 17 same-state no-op deferrals: builds queue depth 17 (> 16)
                    For i = 1 To 17
                        gsm.RequestTransition(GlobalState.Idle, $"no-op cascade {i}")
                    Next
                End If
            End Sub

        gsm.TransitionTo(GlobalState.Idle, "opens window")

        Assert.AreEqual(1 + 17, eventCount, "nothing dropped: all 17 deferred no-ops must execute")
        Assert.IsTrue(gsm.DepthWarningCount >= 1, "the depth diagnostic must fire past 16 (observable seam, analysis E1)")
        Assert.AreEqual(GlobalState.Idle, gsm.CurrentState)
    End Sub

    <TestMethod>
    Public Sub CrossThread_CallerBlocksThenPerforms_NeverDeferred()
        ' Analysis F1: a cross-thread caller arriving mid-window blocks until
        ' the window closes, then performs - it must never observe Deferred.
        Dim gsm As New GlobalStateMachine()
        Dim aboutToCall As New ManualResetEventSlim(False)
        Dim t2Outcome As TransitionOutcome = TransitionOutcome.Deferred
        Dim fired = False

        Dim t2 As New Thread(
            Sub()
                aboutToCall.Wait(5000)
                Thread.Sleep(25) ' widen the mid-window overlap (correctness does not depend on it)
                t2Outcome = gsm.RequestTransition(GlobalState.Playing, "cross-thread mid-window")
            End Sub)
        t2.IsBackground = True

        AddHandler gsm.StateChanged,
            Sub(sender, args)
                If Not fired Then
                    fired = True
                    t2.Start()
                    aboutToCall.Set()
                    Thread.Sleep(100) ' hold the window open while T2 arrives and blocks
                End If
            End Sub

        gsm.TransitionTo(GlobalState.Idle, "opens window")

        Assert.IsTrue(t2.Join(5000), "cross-thread caller must complete (bounded wait)")
        Assert.AreEqual(TransitionOutcome.Performed, t2Outcome,
            "cross-thread caller must block-then-perform, never Deferred (analysis F1 / MainForm modal-dialog evidence)")
        Assert.AreEqual(GlobalState.Playing, gsm.CurrentState)
    End Sub

#End Region

End Class
