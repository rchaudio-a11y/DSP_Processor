# Debugging Instructions

**Reference:** Main [copilot-instructions.md](../copilot-instructions.md)  
**RDF Phase:** Phase 4 - Recursive Debugging

---

## ?? RDF Phase 4: Bugs as Teachers

> *"Bugs become teachers. Edge cases reveal architecture flaws and opportunities."* — [RDF.md](../../DSP_Processor/Documentation/Reference/RDF.md)

**Mindset:** Debug deeply to understand, not just to fix.

---

## ?? Debugging Philosophy

### What Bugs Reveal

- **Architecture truths** - Boundary violations, wrong assumptions
- **Invariant violations** - What we thought was always true... isn't
- **Missing abstractions** - Patterns begging to emerge
- **Edge cases** - Where the system meets reality

### NOT a Bug

- ? Feature request
- ? Missing functionality
- ? Performance optimization (unless regression)

---

## ?? Bug Investigation Process

### 1. Reproduction

**Create `Issue-v1_0_0.md` with:**

```markdown
## Reproduction Steps
1. Arm microphone
2. Speak into mic
3. Move volume slider
4. **Expected:** Volume changes
5. **Actual:** Nothing happens

**Reproducibility:** Always | Sometimes | Rare
```

### 2. Evidence Gathering

**Collect:**
- Error messages / stack traces
- Log output
- State at time of bug
- Environment details

**Log strategically:**
```vb
Logger.Instance.Debug($"Event handler called: {sender.GetType().Name}")
Logger.Instance.Warning($"Expected non-null, got null: {variableName}")
```

### 3. Root Cause Analysis

**Ask:**
1. What assumption was wrong?
2. What boundary was violated?
3. What invariant broke?
4. What does this reveal about architecture?

**Document in Issue-v1_0_0.md:**
```markdown
## Root Cause Analysis

**Hypothesis:** Slider events not wired

**Investigation:**
1. Checked event handlers - None found ?
2. Checked AddHandler calls - Missing! ?
3. Root cause: Handles keyword without WithEvents

**Architectural Insight:**
- Mixing declarative (Handles) and programmatic (AddHandler) fails silently
- Need consistent pattern: WithEvents for Designer, AddHandler for programmatic
```

---

## ?? Testing Strategy

### Edge Cases First

**Think:**
- What if buffer is null?
- What if sample rate changes mid-stream?
- What if user clicks 100 times per second?
- What if audio device disconnects?

**Test:**
```vb
' Test null buffer:
Try
    ProcessAudio(Nothing, 16, 2, 44100)
    Assert.Fail("Should have thrown ArgumentNullException")
Catch ex As ArgumentNullException
    ' Expected
End Try
```

### Stress Testing

```vb
' Rapid state changes:
For i = 1 To 1000
    recordingManager.ArmMicrophone()
    recordingManager.DisarmMicrophone()
Next

' Verify: No crashes, no memory leaks
```

---

## ?? Common Bug Patterns

### Pattern 1: Event Wiring Mismatch

**Symptom:** Event handler never fires  
**Root Cause:** `Handles` keyword without `WithEvents`

**Fix:**
```vb
' ? Wrong:
Private Sub OnClick(sender As Object, e As EventArgs) Handles btn.Click
    ' btn isn't WithEvents!
End Sub

' ? Right:
AddHandler btn.Click, AddressOf OnClick
Private Sub OnClick(sender As Object, e As EventArgs)
    ' Works!
End Sub
```

### Pattern 2: Thread Affinity Violation

**Symptom:** InvalidOperationException: "Cross-thread operation"  
**Root Cause:** Audio thread trying to update UI

**Fix:**
```vb
' ? Wrong:
Private Sub OnAudioData(buffer As Byte())
    meterControl.Level = CalculateLevel(buffer)  ' Cross-thread!
End Sub

' ? Right:
Private Sub OnAudioData(buffer As Byte())
    Dim level = CalculateLevel(buffer)
    If InvokeRequired Then
        BeginInvoke(Sub() meterControl.Level = level)
    Else
        meterControl.Level = level
    End If
End Sub
```

### Pattern 3: Ring Buffer Read Contention

**Symptom:** FFT and meter get different data  
**Root Cause:** Single read pointer shared

**Fix:**
```vb
' ? Wrong (single-reader RingBuffer):
Dim fftData = ringBuffer.Read(fftBuffer)     ' Advances pointer
Dim meterData = ringBuffer.Read(meterBuffer) ' Gets NEXT data!

' ? Right (multi-reader RingBuffer):
Dim fftData = ringBuffer.Read("FFT", fftBuffer)       ' Independent cursor
Dim meterData = ringBuffer.Read("Meter", meterBuffer) ' Independent cursor
```

---

## ?? Lessons Learned Documentation

### After Every Bug

**In Issue-v1_0_0.md:**
```markdown
## Lessons Learned (RDF Phase 4)

### Technical Learning
- VB.NET Handles keyword requires WithEvents declaration
- Mixing patterns causes silent failures

### Process Learning
- Check event wiring first for non-firing events
- Use consistent pattern within a component

### Architecture Changes
- Document WithEvents vs AddHandler pattern in UI standards
- Add EventWiringValidator utility (future)
```

---

## ?? When Debugging Triggers Recursion

### Back to Phase 2 (Architecture)

**Scenario:** Bug reveals fundamental design flaw

**Process:**
1. Document discovery in Issue doc
2. Create new Architecture doc
3. Refine boundaries/invariants
4. Update existing architecture
5. Plan refactoring in Task doc

**Example:**
- Bug: Timer polling causes missed data
- Discovery: Event-driven is architecturally required
- Recursion: Return to Phase 2, redesign as event-driven

---

## ?? Bug Metrics

### Track
- Time to reproduce
- Time to root cause
- Time to fix
- Number of related bugs found

### Goal
- **NOT** minimize bugs
- **YES** maximize learning per bug
- **YES** prevent bug classes (architectural fixes)

---

## ?? When to Stop Debugging

### Stop When
- ? Root cause understood
- ? Architectural insight gained
- ? Lesson documented
- ? Fix implemented and tested
- ? Regression test added

### DON'T Stop When
- ? Bug "seems" fixed but root cause unknown
- ? Workaround applied without understanding
- ? Similar bugs keep appearing

---

**Last Updated:** 2026-01-16  
**Version:** 1.0.0
