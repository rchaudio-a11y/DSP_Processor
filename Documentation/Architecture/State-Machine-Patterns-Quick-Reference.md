# State Machine Patterns - Quick Reference
## Patterns Applied in Step 22.5

**Created:** 2026-01-17  
**Purpose:** Quick reference for state machine architectural patterns  

---

## ??? **Pattern 1: Logger Recursion Guard**

**Problem:** Logging triggers state changes ? infinite recursion ? StackOverflowException

**Solution:**
```visualbasic
' Logger.vb
Private Shared ReadOnly isLogging As New ThreadLocal(Of Boolean)(Function() False)

Private Sub Log(level As LogLevel, message As String, ex As Exception, context As String)
    If isLogging.Value Then Return  ' Guard
    isLogging.Value = True
    Try
        ' ... logging logic ...
    Finally
        isLogging.Value = False
    End Try
End Sub
```

**When to Use:**
- Any component that can be called recursively
- Operations that might trigger callbacks/events
- Thread-specific state tracking

**Benefits:**
- ? Thread-safe (ThreadLocal)
- ? Zero overhead when not recursing
- ? Prevents infinite loops
- ? Maintains functionality

---

## ?? **Pattern 2: Pending Transition Queue**

**Problem:** Re-entry guard blocks legitimate multi-step flows ? deadlock

**Solution:**
```visualbasic
' GlobalStateMachine.vb
Private _isTransitioning As Boolean = False
Private _pendingTransition As GlobalState? = Nothing
Private _pendingReason As String = Nothing

Public Function TransitionTo(newState As GlobalState, reason As String) As Boolean
    SyncLock _stateLock
        ' If already transitioning, QUEUE it
        If _isTransitioning Then
            _pendingTransition = newState
            _pendingReason = reason
            Return True  ' Accept
        End If
        
        _isTransitioning = True
        Try
            ' ... perform transition ...
        Finally
            _isTransitioning = False
            
            ' Execute queued transition
            If _pendingTransition.HasValue Then
                Dim queued = _pendingTransition.Value
                _pendingTransition = Nothing
                TransitionTo(queued, _pendingReason)  ' Recursive but safe
            End If
        End Try
    End SyncLock
End Function
```

**When to Use:**
- State machines with multi-step flows
- Event-driven transitions
- Cascading state changes
- When re-entry guard blocks legitimate calls

**Benefits:**
- ? Prevents infinite recursion
- ? Allows multi-step flows
- ? Maintains deterministic order
- ? Automatic execution after guard releases

**Tradeoffs:**
- ?? Only queues ONE transition (can extend to queue)
- ?? Recursive calls (controlled)
- ?? More complex than simple guard

---

## ?? **Pattern 3: Completion Callback**

**Problem:** Need to ensure cleanup completes BEFORE state transition

**Solution:**
```visualbasic
' Component with async cleanup:
Public Sub StopRecording(Optional onComplete As Action = Nothing)
    Try
        ' Stop engine (BLOCKS until finalized)
        recorder.StopRecording()
        
        ' Cleanup
        Logger.Instance.Info("Recording stopped")
        
        ' Execute callback AFTER finalization
        onComplete?.Invoke()
    Catch ex As Exception
        Logger.Instance.Error("Failed to stop", ex)
        Throw
    End Try
End Sub

' Caller (state machine):
Case GlobalState.Stopping
    _recordingManager.StopRecording(
        Sub()
            ' Transition AFTER finalization
            _globalStateMachine.TransitionTo(GlobalState.Idle, "Stopped")
        End Sub)
```

**When to Use:**
- File finalization required before transition
- Cleanup must complete before next state
- Async operations that need confirmation
- Preventing premature transitions

**Benefits:**
- ? Guaranteed execution order
- ? No re-entry (callback runs AFTER method returns)
- ? Clean separation of concerns
- ? Prevents corrupted files

**Tradeoffs:**
- ?? Callback adds complexity
- ?? Must handle callback failures
- ?? Can't return value from callback

---

## ?? **Pattern 4: State-Driven Data Refresh**

**Problem:** UI doesn't reflect data changes after state transitions

**Solution:**
```visualbasic
' MainForm.OnUIStateChanged():
Private Sub OnUIStateChanged(sender As Object, e As StateChangedEventArgs(Of UIState))
    Select Case e.NewState
        Case UIState.IdleUI
            ' ... UI updates ...
            
            ' Refresh data when returning from recording
            If e.OldState = UIState.RecordingUI Then
                fileManager.RefreshFileList()
                Logger.Instance.Info("File list refreshed")
            End If
    End Select
End Sub
```

**When to Use:**
- UI needs to reflect new data
- File lists, search results, etc.
- After background operations complete
- When transitioning back to idle state

**Benefits:**
- ? Automatic refresh on state change
- ? No manual refresh needed
- ? Consistent UX
- ? Decoupled from business logic

---

## ?? **Pattern Selection Guide**

### **Use Logger Recursion Guard When:**
- ? Component can be called recursively
- ? Logging/callbacks might trigger re-entry
- ? Need thread-specific state

### **Use Pending Transition Queue When:**
- ? Multi-step state flows required
- ? Re-entry guard blocks legitimate calls
- ? Event-driven state changes
- ? Need deterministic execution order

### **Use Completion Callback When:**
- ? Cleanup must complete before transition
- ? File finalization required
- ? Async operations need confirmation
- ? Preventing premature state changes

### **Use State-Driven Refresh When:**
- ? UI needs to reflect new data
- ? Data changes during state transition
- ? Want automatic refresh
- ? Decoupling data from UI logic

---

## ?? **Anti-Patterns to Avoid**

### **? Logging in State Transitions:**
```visualbasic
' BAD:
Public Function TransitionTo(...) As Boolean
    Logger.Instance.Info(...)  ' Might trigger more transitions!
    ' ... transition logic ...
End Function

' GOOD:
Public Function TransitionTo(...) As Boolean
    ' ... transition logic ...
    Console.WriteLine(...)  ' Console only, no side effects
End Function
```

### **? Calling TransitionTo() in StateChanged Handlers:**
```visualbasic
' BAD:
Private Sub OnStateChanged(sender As Object, e As StateChangedEventArgs)
    _stateMachine.TransitionTo(...)  ' Re-entrant!
End Sub

' GOOD:
Private Sub OnStateChanged(sender As Object, e As StateChangedEventArgs)
    _component.DoWork()  ' No callbacks to state machine
End Sub
```

### **? Transitioning Before Finalization:**
```visualbasic
' BAD:
Public Sub StopRecording()
    recorder.StopRecording()  ' Async, might not finish
    TransitionTo(Idle)  ' TOO EARLY!
End Sub

' GOOD:
Public Sub StopRecording(onComplete As Action)
    recorder.StopRecording()  ' Blocks until done
    onComplete?.Invoke()  ' AFTER finalization
End Sub
```

---

## ?? **Related Patterns**

**Command Pattern:** Use for user actions that trigger state transitions

**Observer Pattern:** Already used for StateChanged events

**Strategy Pattern:** Could be used for different state transition strategies

**State Pattern:** The foundation of our state machine architecture

---

## ?? **Success Metrics**

**Pattern Working When:**
- ? No StackOverflowException
- ? No deadlocks (system not stuck)
- ? Files not corrupted
- ? UI reflects data changes
- ? Logs show clean transitions
- ? No re-entry warnings

**Pattern Failing When:**
- ? Crashes or hangs
- ? Stuck in intermediate states
- ? Corrupted output files
- ? UI out of sync with data
- ? Excessive log spam
- ? Re-entry warnings flooding console

---

**Created:** 2026-01-17  
**Author:** Rick + GitHub Copilot  
**Status:** Production-ready patterns  
**Version:** v1.3.2.1
