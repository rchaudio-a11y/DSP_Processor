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

#Region "US2 - Deadlock-free, ordered delivery (FR-006/007/008)"

    <TestMethod>
    Public Sub Delivery_LockNotHeld_CrossThreadProbe()
        ' Invariant 6: the machine's lock must be FREE while subscribers run.
        ' Probe: from inside a handler, a second thread acquires the lock
        ' (GetTransitionHistory takes _stateLock) - if delivery held the lock,
        ' the probe would block and the bounded join would fail.
        Dim gsm As New GlobalStateMachine()
        Dim probeCompleted = False
        Dim probeJoined = False

        AddHandler gsm.StateChanged,
            Sub(sender, args)
                Dim probe As New Thread(
                    Sub()
                        Dim history = gsm.GetTransitionHistory()
                        probeCompleted = history IsNot Nothing
                    End Sub)
                probe.IsBackground = True
                probe.Start()
                probeJoined = probe.Join(5000)
            End Sub

        gsm.TransitionTo(GlobalState.Idle, "probe trigger")

        Assert.IsTrue(probeJoined, "lock-acquiring probe must complete while delivery is in progress - the lock may not be held during delivery")
        Assert.IsTrue(probeCompleted)
    End Sub

    <TestMethod>
    Public Sub Deadlock_CuredClass_SubscriberOwnLockAndCascade_Completes()
        ' Spec US2-AS1 (the class this feature CURES): a subscriber that takes
        ' its own lock and requests a further transition inside its handler
        ' completes without deadlock. Plus a concurrent cross-thread caller
        ' holding NO subscriber locks. The residual AB-BA interleaving (caller
        ' HOLDING a subscriber lock) remains a documented discipline -
        ' research R1-RESIDUAL / GSM-Subscriber-Audit.md.
        Dim gsm As New GlobalStateMachine()
        Dim subscriberLock As New Object()
        Dim cascadeOutcome As TransitionOutcome = TransitionOutcome.Rejected
        Dim cascaded = False

        AddHandler gsm.StateChanged,
            Sub(sender, args)
                SyncLock subscriberLock ' own lock inside handler
                    If Not cascaded Then
                        cascaded = True
                        cascadeOutcome = gsm.RequestTransition(GlobalState.Playing, "in-handler under own lock")
                    End If
                End SyncLock
            End Sub

        Dim worker As New Thread(Sub() gsm.TransitionTo(GlobalState.Idle, "cross-thread opener"))
        worker.IsBackground = True
        worker.Start()

        Assert.IsTrue(worker.Join(5000), "must complete without deadlock (bounded wait)")
        Assert.AreEqual(TransitionOutcome.Deferred, cascadeOutcome)
        Assert.AreEqual(GlobalState.Playing, gsm.CurrentState, "the deferred cascade must have executed")
    End Sub

    <TestMethod>
    Public Sub Ordering_100TransitionsWithCascade_StrictCommitOrder()
        ' Invariant 7: payload order = commit order, transition IDs strictly
        ' ascending, event chain continuous - across cascaded transitions.
        ' Cascade fires ONLY on Playing->Stopping and returns the machine to
        ' Idle (a known state), keeping this single-threaded script
        ' deterministic (analysis C1).
        Dim gsm As New GlobalStateMachine()
        Dim events As New List(Of StateChangedEventArgs(Of GlobalState))

        AddHandler gsm.StateChanged,
            Sub(sender, args)
                events.Add(args)
                If args.OldState = GlobalState.Playing AndAlso args.NewState = GlobalState.Stopping Then
                    gsm.RequestTransition(GlobalState.Idle, "cascade back to Idle")
                End If
            End Sub

        Assert.IsTrue(gsm.TransitionTo(GlobalState.Idle, "setup"))
        For cycle = 1 To 34 ' 1 + 34*3 = 103 committed transitions
            Assert.IsTrue(gsm.TransitionTo(GlobalState.Playing, $"cycle {cycle}"))
            Assert.IsTrue(gsm.TransitionTo(GlobalState.Stopping, $"cycle {cycle}"))
            Assert.AreEqual(GlobalState.Idle, gsm.CurrentState, $"cascade must have returned to Idle in cycle {cycle}")
        Next

        Assert.IsTrue(events.Count >= 100, $"expected >= 100 events, got {events.Count}")

        ' Transition IDs strictly ascending, no gaps (format GSM_Tnn_...)
        Dim lastNum = 0
        For Each e In events
            Dim numPart = Integer.Parse(e.TransitionID.Substring(5, e.TransitionID.IndexOf("_"c, 5) - 5))
            Assert.AreEqual(lastNum + 1, numPart, $"transition IDs must be gapless ascending; got {e.TransitionID} after {lastNum}")
            lastNum = numPart
        Next

        ' Event chain continuity: each event's OldState = previous NewState
        For i = 1 To events.Count - 1
            Assert.AreEqual(events(i - 1).NewState, events(i).OldState,
                $"event {i} out of order: chain breaks at {events(i - 1).NewState} -> {events(i).OldState}")
        Next
    End Sub

    <TestMethod>
    Public Sub Coordinator_HasNoDisposalSurface()
        ' US3 / SC-006: process-lifetime ruling - the coordinator type must not
        ' implement IDisposable (removal is compile-enforced for callers;
        ' this reflection check locks the type surface itself)
        Assert.IsNull(GetType(StateCoordinator).GetInterface("IDisposable"),
            "StateCoordinator must not implement IDisposable (Architect ruling: process-lifetime)")
        Assert.IsNull(GetType(StateCoordinator).GetMethod("Dispose"),
            "StateCoordinator must expose no Dispose method")
    End Sub

    <TestMethod>
    Public Sub ErrorStateEntry_StillReachable_AfterScaffoldingDeletion()
        ' US3 / FR-012: the deleted OnStateEntering scaffolding contained one
        ' real behavior (error-state entry logging, now inlined in the commit
        ' path). This locks the observable half: the Error transition still
        ' performs and the machine reaches Error. (Log emission is suppressed
        ' under test; the inlined call is review-verified in CommitLocked.)
        Dim gsm As New GlobalStateMachine()
        Assert.IsTrue(gsm.TransitionTo(GlobalState.Idle, "setup"))
        Assert.AreEqual(TransitionOutcome.Performed, gsm.RequestTransition(GlobalState.[Error], "fault injection"))
        Assert.AreEqual(GlobalState.[Error], gsm.CurrentState)

        ' Recovery path intact
        Assert.IsTrue(gsm.TransitionTo(GlobalState.Idle, "recovery"))
        Assert.AreEqual(GlobalState.Idle, gsm.CurrentState)
    End Sub

    <TestMethod>
    Public Sub Containment_ThrowingSubscriber_OthersStillNotified()
        Dim gsm As New GlobalStateMachine()
        Dim secondSubscriberNotified = False

        AddHandler gsm.StateChanged,
            Sub(sender, args)
                Throw New InvalidOperationException("deliberately hostile subscriber")
            End Sub
        AddHandler gsm.StateChanged,
            Sub(sender, args)
                secondSubscriberNotified = True
            End Sub

        Dim performed = gsm.TransitionTo(GlobalState.Idle, "containment trigger")

        Assert.IsTrue(performed, "a subscriber exception must never look like a transition failure")
        Assert.IsTrue(secondSubscriberNotified, "remaining subscribers must still be notified (FR-008)")
        Assert.AreEqual(GlobalState.Idle, gsm.CurrentState, "the committed transition is unaffected")
    End Sub

#End Region

End Class
