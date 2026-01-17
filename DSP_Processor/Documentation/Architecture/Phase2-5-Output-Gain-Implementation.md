# Phase 2.5: Output Gain Stage Implementation Summary

## ? **COMPLETED: DSPThread Foundation**

### **What Was Added:**

1. **New Monitor Buffer:**
   ```vb
   Friend ReadOnly postOutputGainMonitorBuffer As Utils.RingBuffer
   ```

2. **New Methods:**
   ```vb
   Public Function PostOutputGainMonitorAvailable() As Integer
   Public Function ReadPostOutputGainMonitor(data As Byte(), offset As Integer, count As Integer) As Integer
   ```

### **Updated Architecture:**

```
Input ? INPUT GAIN ? [PostInputGain Tap] ? (Future: Filters/EQ) ? OUTPUT GAIN ? [PostOutputGain Tap] ? Output
            ?                                                             ?
      INPUT METERS                                                  OUTPUT METERS
```

---

## ?? **NEXT STEPS: Wire Output Gain Processors**

### **Step 1: Update AudioRouter**

Currently AudioRouter has ONE GainProcessor. We need to add a second one for output gain:

```vb
' AudioRouter.vb

' CURRENT:
Private _gainProcessor As DSP.GainProcessor ' Input gain

' ADD:
Private _outputGainProcessor As DSP.GainProcessor ' Output gain (NEW!)
```

Then in `StartDSPPlayback()`:

```vb
' Add INPUT gain processor
_gainProcessor = New DSP.GainProcessor(pcm16Format) With {
    .GainDB = 0.0F ' Unity gain
}
dspThread.Chain.AddProcessor(_gainProcessor)

' Wire INPUT gain tap point
_gainProcessor.SetMonitorOutputCallback(
    Sub(buffer As DSP.AudioBuffer)
        dspThread.postGainMonitorBuffer.Write(buffer.Buffer, 0, buffer.ByteCount)
    End Sub
)

' ADD OUTPUT gain processor (NEW!)
_outputGainProcessor = New DSP.GainProcessor(pcm16Format) With {
    .GainDB = 0.0F ' Unity gain
}
dspThread.Chain.AddProcessor(_outputGainProcessor)

' Wire OUTPUT gain tap point (NEW!)
_outputGainProcessor.SetMonitorOutputCallback(
    Sub(buffer As DSP.AudioBuffer)
        dspThread.postOutputGainMonitorBuffer.Write(buffer.Buffer, 0, buffer.ByteCount)
    End Sub
)
```

Add property:

```vb
''' <summary>Gets the OUTPUT GainProcessor instance (for UI control)</summary>
Public ReadOnly Property OutputGainProcessor As DSP.GainProcessor
    Get
        Return _outputGainProcessor
    End Get
End Property

''' <summary>Gets post-OUTPUT-gain samples for meter display</summary>
Public ReadOnly Property PostOutputGainSamples As Single()
    Get
        If dspThread IsNot Nothing Then
            Dim available = dspThread.PostOutputGainMonitorAvailable()
            If available > 0 Then
                Dim bufferSize = Math.Min(available, 4096)
                Dim buffer(bufferSize - 1) As Byte
                Dim bytesRead = dspThread.ReadPostOutputGainMonitor(buffer, 0, buffer.Length)
                
                If bytesRead > 0 Then
                    ' Convert Int16 PCM to Float32
                    Dim sampleCount = bytesRead \ 2
                    Dim samples(sampleCount - 1) As Single
                    For i = 0 To sampleCount - 1
                        Dim int16Sample = BitConverter.ToInt16(buffer, i * 2)
                        samples(i) = int16Sample / 32768.0F
                    Next
                    Return samples
                End If
            End If
        End If
        Return Nothing
    End Get
End Property
```

---

### **Step 2: Update RecordingManager**

Same pattern - add output gain processor:

```vb
' RecordingManager.vb

' CURRENT:
Private _gainProcessor As DSP.GainProcessor ' Input gain

' ADD:
Private _outputGainProcessor As DSP.GainProcessor ' Output gain (NEW!)
```

In `ArmMicrophone()`:

```vb
' Add INPUT gain processor
_gainProcessor = New DSP.GainProcessor(pcm16Format) With {
    .GainDB = 0.0F
}
dspThread.Chain.AddProcessor(_gainProcessor)

' Wire INPUT gain tap
_gainProcessor.SetMonitorOutputCallback(
    Sub(buffer As DSP.AudioBuffer)
        dspThread.postGainMonitorBuffer.Write(buffer.Buffer, 0, buffer.ByteCount)
    End Sub
)

' ADD OUTPUT gain processor (NEW!)
_outputGainProcessor = New DSP.GainProcessor(pcm16Format) With {
    .GainDB = 0.0F
}
dspThread.Chain.AddProcessor(_outputGainProcessor)

' Wire OUTPUT gain tap (NEW!)
_outputGainProcessor.SetMonitorOutputCallback(
    Sub(buffer As DSP.AudioBuffer)
        dspThread.postOutputGainMonitorBuffer.Write(buffer.Buffer, 0, buffer.ByteCount)
    End Sub
)
```

Add property:

```vb
''' <summary>Gets the OUTPUT GainProcessor instance (Phase 2.5)</summary>
Public ReadOnly Property OutputGainProcessor As DSP.GainProcessor
    Get
        Return _outputGainProcessor
    End Get
End Property

''' <summary>Gets post-OUTPUT-gain samples for output meters (Phase 2.5)</summary>
Public ReadOnly Property PostOutputGainSamples As Single()
    Get
        If dspThread IsNot Nothing Then
            Dim available = dspThread.PostOutputGainMonitorAvailable()
            If available > 0 Then
                Dim bufferSize = Math.Min(available, 4096)
                Dim buffer(bufferSize - 1) As Byte
                Dim bytesRead = dspThread.ReadPostOutputGainMonitor(buffer, 0, buffer.Length)
                
                If bytesRead > 0 Then
                    ' Convert Int16 PCM to Float32
                    Dim sampleCount = bytesRead \ 2
                    Dim samples(sampleCount - 1) As Single
                    For i = 0 To sampleCount - 1
                        Dim int16Sample = BitConverter.ToInt16(buffer, i * 2)
                        samples(i) = int16Sample / 32768.0F
                    Next
                    Return samples
                End If
            End If
        End If
        Return Nothing
    End Get
End Property
```

---

### **Step 3: Update MainForm Meters**

Now meters can use DIFFERENT tap points:

```vb
' MainForm.vb - UpdateDSPMeters()

If audioRouter IsNot Nothing AndAlso audioRouter.IsPlaying Then
    ' FILE PLAYBACK
    ' INPUT meters: PostGainSamples (after INPUT gain)
    Dim inputSamples = audioRouter.PostGainSamples
    If inputSamples IsNot Nothing AndAlso inputSamples.Length > 0 Then
        inputLeftDb = CalculatePeakDb(inputSamples, 0)
        inputRightDb = CalculatePeakDb(inputSamples, 1)
    End If
    
    ' OUTPUT meters: PostOutputGainSamples (after OUTPUT gain) ? NEW!
    Dim outputSamples = audioRouter.PostOutputGainSamples
    If outputSamples IsNot Nothing AndAlso outputSamples.Length > 0 Then
        outputLeftDb = CalculatePeakDb(outputSamples, 0)
        outputRightDb = CalculatePeakDb(outputSamples, 1)
    End If
    
ElseIf recordingManager IsNot Nothing AndAlso (recordingManager.IsArmed OrElse recordingManager.IsRecording) Then
    ' MICROPHONE
    ' INPUT meters: PostGainSamples (after INPUT gain)
    Dim inputSamples = recordingManager.PostGainSamples
    If inputSamples IsNot Nothing AndAlso inputSamples.Length > 0 Then
        inputLeftDb = CalculatePeakDb(inputSamples, 0)
        inputRightDb = CalculatePeakDb(inputSamples, 1)
    End If
    
    ' OUTPUT meters: PostOutputGainSamples (after OUTPUT gain) ? NEW!
    Dim outputSamples = recordingManager.PostOutputGainSamples
    If outputSamples IsNot Nothing AndAlso outputSamples.Length > 0 Then
        outputLeftDb = CalculatePeakDb(outputSamples, 0)
        outputRightDb = CalculatePeakDb(outputSamples, 1)
    End If
End If

DSPSignalFlowPanel1.UpdateMeters(inputLeftDb, inputRightDb, outputLeftDb, outputRightDb)
```

---

## ?? **BENEFITS:**

1. **Separate Control:**
   - Input gain: Adjust mic/file level going INTO DSP
   - Output gain: Adjust final volume going TO speakers/recorder

2. **Accurate Meters:**
   - Input meters: Show level after input gain (pre-DSP)
   - Output meters: Show level after output gain (post-DSP)

3. **Future-Proof:**
   - Filters/EQ will go BETWEEN input and output gain
   - Meters show actual signal at each stage

4. **Standard Architecture:**
   - Matches mixing console workflow
   - Input ? Process ? Output pattern

---

## ?? **SIGNAL FLOW:**

### **Current (Phase 2):**
```
Input ? GainProcessor ? [Tap] ? Output
         (serving double duty)
```

### **After Phase 2.5:**
```
Input ? INPUT GainProcessor ? [Input Tap] ? OUTPUT GainProcessor ? [Output Tap] ? Output
             ?                                        ?
        INPUT METERS                           OUTPUT METERS
```

### **Future (Phase 3+):**
```
Input ? INPUT Gain ? [Tap] ? Filters ? EQ ? OUTPUT Gain ? [Tap] ? Output
             ?                                      ?
        INPUT METERS                          OUTPUT METERS
```

---

## ? **STATUS:**

- ? DSPThread updated (buffer + methods)
- ? AudioRouter needs wiring
- ? RecordingManager needs wiring
- ? MainForm meters need updating

**Estimated Time:** 30-45 minutes to wire everything up!

---

**Created:** January 15, 2026  
**Phase:** 2.5 - Separate Input/Output Gain Stages
