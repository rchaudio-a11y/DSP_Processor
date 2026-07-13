Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' FR-014: exhaustive validation of the GlobalStateMachine transition matrix.
''' Oracle: specs/003-tap-consolidation/data-model.md (26 valid / 38 invalid of 64).
''' Every from/to pair is exercised through the PUBLIC contract only - the
''' from-state is reached by driving valid transitions, never by reflection.
''' </summary>
<TestClass>
Public Class GlobalStateMachineTests

    ''' <summary>All 8 states in enum order (Uninitialized=0 .. Error=7).</summary>
    Private Shared ReadOnly AllStates As GlobalState() =
        CType([Enum].GetValues(GetType(GlobalState)), GlobalState())

    ''' <summary>
    ''' The expected transition matrix (data-model.md oracle).
    ''' Key: (fromState, toState) -> True if the transition must be accepted.
    ''' </summary>
    Private Shared Function ExpectedValid(fromState As GlobalState, toState As GlobalState) As Boolean
        ' Same-state is always a valid no-op
        If fromState = toState Then Return True

        Select Case fromState
            Case GlobalState.Uninitialized
                Return toState = GlobalState.Idle
            Case GlobalState.Idle
                Return toState = GlobalState.Arming OrElse
                       toState = GlobalState.Playing OrElse
                       toState = GlobalState.Error
            Case GlobalState.Arming
                Return toState = GlobalState.Armed OrElse
                       toState = GlobalState.Idle OrElse
                       toState = GlobalState.Error
            Case GlobalState.Armed
                Return toState = GlobalState.Recording OrElse
                       toState = GlobalState.Idle OrElse
                       toState = GlobalState.Error
            Case GlobalState.Recording
                Return toState = GlobalState.Stopping OrElse
                       toState = GlobalState.Error
            Case GlobalState.Stopping
                Return toState = GlobalState.Idle OrElse
                       toState = GlobalState.Error
            Case GlobalState.Playing
                Return toState = GlobalState.Stopping OrElse
                       toState = GlobalState.Idle OrElse
                       toState = GlobalState.Error
            Case GlobalState.Error
                Return toState = GlobalState.Idle
            Case Else
                Return False
        End Select
    End Function

    ''' <summary>
    ''' Shortest valid drive paths from a fresh machine (Uninitialized) to each
    ''' from-state, per data-model.md (max 4 hops).
    ''' </summary>
    Private Shared Function PathTo(target As GlobalState) As GlobalState()
        Select Case target
            Case GlobalState.Uninitialized : Return Array.Empty(Of GlobalState)()
            Case GlobalState.Idle : Return {GlobalState.Idle}
            Case GlobalState.Arming : Return {GlobalState.Idle, GlobalState.Arming}
            Case GlobalState.Armed : Return {GlobalState.Idle, GlobalState.Arming, GlobalState.Armed}
            Case GlobalState.Recording : Return {GlobalState.Idle, GlobalState.Arming, GlobalState.Armed, GlobalState.Recording}
            Case GlobalState.Stopping : Return {GlobalState.Idle, GlobalState.Playing, GlobalState.Stopping}
            Case GlobalState.Playing : Return {GlobalState.Idle, GlobalState.Playing}
            Case GlobalState.[Error] : Return {GlobalState.Idle, GlobalState.[Error]}
            Case Else
                Throw New ArgumentException($"No path defined for {target}")
        End Select
    End Function

    Private Shared Function DriveTo(target As GlobalState) As GlobalStateMachine
        Dim gsm As New GlobalStateMachine()
        For Each hop In PathTo(target)
            Assert.IsTrue(gsm.TransitionTo(hop, "test drive"), $"drive hop to {hop} unexpectedly rejected en route to {target}")
        Next
        Assert.AreEqual(target, gsm.CurrentState, $"drive failed to reach {target}")
        Return gsm
    End Function

    <TestMethod>
    Public Sub OracleTotals_TamperCheck_26Valid38Invalid()
        Dim valid = 0
        For Each fromState In AllStates
            For Each toState In AllStates
                If ExpectedValid(fromState, toState) Then valid += 1
            Next
        Next
        Assert.AreEqual(26, valid, "oracle drifted: expected exactly 26 valid pairs")
        Assert.AreEqual(38, 64 - valid, "oracle drifted: expected exactly 38 invalid pairs")
    End Sub

    <TestMethod>
    Public Sub TransitionMatrix_AllSixtyFourPairs_MatchOracle()
        Dim failures As New List(Of String)

        For Each fromState In AllStates
            For Each toState In AllStates
                Dim expected = ExpectedValid(fromState, toState)

                ' IsValidTransition: pure rule check
                Dim gsm = DriveTo(fromState)
                Dim ruleResult = gsm.IsValidTransition(fromState, toState)
                If ruleResult <> expected Then
                    failures.Add($"IsValidTransition({fromState} -> {toState}) = {ruleResult}, oracle says {expected}")
                End If

                ' TransitionTo: effect check on a FRESH machine per pair
                Dim gsm2 = DriveTo(fromState)
                Dim accepted = gsm2.TransitionTo(toState, "matrix test")
                If accepted <> expected Then
                    failures.Add($"TransitionTo({fromState} -> {toState}) returned {accepted}, oracle says {expected}")
                End If

                If expected Then
                    If gsm2.CurrentState <> toState Then
                        failures.Add($"accepted {fromState} -> {toState} but CurrentState = {gsm2.CurrentState}")
                    End If
                Else
                    If gsm2.CurrentState <> fromState Then
                        failures.Add($"rejected {fromState} -> {toState} must leave state unchanged, but CurrentState = {gsm2.CurrentState}")
                    End If
                End If
            Next
        Next

        Assert.AreEqual(0, failures.Count,
            $"{failures.Count} matrix deviations:{Environment.NewLine}{String.Join(Environment.NewLine, failures)}")
    End Sub

    <TestMethod>
    Public Sub ErrorRecovery_OnlyPathIsIdle()
        Dim gsm = DriveTo(GlobalState.[Error])
        ' Everything except Idle (and same-state) is rejected from Error
        Assert.IsFalse(gsm.TransitionTo(GlobalState.Recording, "must reject"))
        Assert.IsFalse(gsm.TransitionTo(GlobalState.Playing, "must reject"))
        Assert.AreEqual(GlobalState.[Error], gsm.CurrentState)

        ' Recovery works
        Assert.IsTrue(gsm.TransitionTo(GlobalState.Idle, "recovery"))
        Assert.AreEqual(GlobalState.Idle, gsm.CurrentState)
    End Sub

    <TestMethod>
    Public Sub StateChangedEvent_FiresWithCorrectArgs()
        Dim gsm As New GlobalStateMachine()
        Dim capturedOld As GlobalState = CType(-1, GlobalState)
        Dim capturedNew As GlobalState = CType(-1, GlobalState)
        Dim fireCount = 0

        AddHandler gsm.StateChanged,
            Sub(sender, args)
                fireCount += 1
                capturedOld = args.OldState
                capturedNew = args.NewState
            End Sub

        gsm.TransitionTo(GlobalState.Idle, "event test")

        Assert.AreEqual(1, fireCount)
        Assert.AreEqual(GlobalState.Uninitialized, capturedOld)
        Assert.AreEqual(GlobalState.Idle, capturedNew)
    End Sub

    <TestMethod>
    Public Sub RejectedTransition_DoesNotFireEvent()
        Dim gsm As New GlobalStateMachine() ' Uninitialized
        Dim fireCount = 0
        AddHandler gsm.StateChanged, Sub(sender, args) fireCount += 1

        gsm.TransitionTo(GlobalState.Recording, "invalid - must not fire")

        Assert.AreEqual(0, fireCount)
        Assert.AreEqual(GlobalState.Uninitialized, gsm.CurrentState)
    End Sub

End Class
