# Phase 2: Microphone DSP Pipeline Integration - Design Document

## ?? **GOAL**

Route microphone audio through DSPThread so that:
- ? Meters show live mic audio
- ? DSP effects can be applied to mic (future)
- ? Unified architecture (all audio through one pipeline)
- ? Consistent tap points everywhere

---

## **?? CURRENT ARCHITECTURE (Broken)**

```
????????????????????????????????????????????????????????????
? MICROPHONE PATH (No Meters!)                             ?
????????????????????????????????????????????????????????????

Microphone ? WaveIn ? RecordingManager.OnAudioDataAvailable()
                              ?
                         recorder.ProcessBuffer()
                              ?
                         Direct to WAV File
                         
                         ? NO DSP!
                         ? NO METERS!
                         ? NO TAP POINTS!


????????????????????????????????????????????????????????????
? FILE PLAYBACK PATH (Has Meters!)                         ?
????????????????????????????????????????????????????????????

File ? AudioFileReader ? AudioRouter.PlayFile()
                              ?
                         DSPThread
                              ?
                    [PostGainMonitor ? Meters!]
                              ?
                         WaveOut ? Speakers
```

---

## **?? PROPOSED ARCHITECTURE (Unified)**

```
????????????????????????????????????????????????????????????
? UNIFIED AUDIO PIPELINE                                    ?
????????????????????????????????????????????????????????????

                    ???????????????
                    ?  MainForm   ?
                    ???????????????
                           ?
              ???????????????????????????
              ?                         ?
        ????????????            ????????????????
        ?  Audio   ?            ?  Recording   ?
        ?  Router  ?            ?   Manager    ?
        ? (Files)  ?            ? (Microphone) ?
        ????????????            ????????????????
             ?                         ?
             ? ?????????????????????????
             ? ?
        ???????????????????
        ?   DSPThread     ? ? UNIFIED!
        ?  (ALL AUDIO)    ?
        ???????????????????
             ?
        ????????????
        ?   Tap    ?
        ?  Points  ? ???? Meters (Real-time) ?
        ????????????
             ?
        ???????????
        ?  Output ?
        ? Router  ?
        ???????????
             ?
      ???????????????
      ?             ?
  Speakers      WAV File
```

---

## **?? IMPLEMENTATION STRATEGY**

### **Option A: Minimal Changes (Recommended)** ?

**Keep RecordingManager as-is**, just route its audio through DSPThread:

```
Microphone ? WaveIn ? RecordingManager
                              ?
                      OnAudioDataAvailable()
                              ?
                    ?????????????????????
                    ?                   ?
            DSPThread.WriteInput()   (Direct WAV if bypassing DSP)
                    ?
            [PostGainMonitor] ? Meters!
                    ?
            DSPThread.ReadOutput()
                    ?
            recorder.ProcessBuffer()
                    ?
                WAV File
```

**Pros:**
- ? Minimal changes to existing code
- ? RecordingManager stays mostly unchanged
- ? Easy to add "bypass DSP" option
- ? Fast to implement (2-3 hours)

**Cons:**
- ?? Extra buffer copy (input ? DSP ? output)
- ?? Adds 10-20ms latency to recording

---

### **Option B: Full Integration (Overkill)**

Rewrite RecordingManager to use DSPThread internally.

**Pros:**
- ? Cleanest architecture
- ? No redundant code

**Cons:**
- ? 6-8 hours of refactoring
- ? High risk of breaking recording
- ? Overkill for just getting meters

---

## **?? CHOSEN: Option A (Minimal Changes)**

---

## **?? DETAILED IMPLEMENTATION**

### **Step 1: Add DSPThread Mode to RecordingManager**

```vb
' RecordingManager.vb

Private dspThread As DSP.DSPThread ' NEW: Optional DSP processing
Private useDSP As Boolean = True    ' NEW: Enable/disable DSP

Public Sub ArmMicrophone()
    ' ... existing WaveIn setup ...
    
    ' NEW: Create DSPThread for mic audio
    If useDSP Then
        Dim format = New WaveFormat(mic.SampleRate, 16, mic.Channels)
        Dim bufferSize = format.AverageBytesPerSecond * 2 ' 2 seconds
        dspThread = New DSP.DSPThread(format, bufferSize, bufferSize)
        
        ' Add GainProcessor (for volume/pan control)
        Dim gainProc = New DSP.GainProcessor(format)
        dspThread.Chain.AddProcessor(gainProc)
        
        ' Wire tap point for meters
        gainProc.SetMonitorOutputCallback(
            Sub(buffer As DSP.AudioBuffer)
                ' Raise event for meters
                RaiseEvent MicAudioProcessed(Me, buffer)
            End Sub
        )
        
        dspThread.Start()
        Logger.Instance.Info("DSPThread created for microphone (tap points active)", "RecordingManager")
    End If
End Sub
```

### **Step 2: Route Audio Through DSP**

```vb
' RecordingManager.vb

Private Sub OnAudioDataAvailable(sender As Object, e As AudioCallbackEventArgs)
    Try
        Dim processedBuffer As Byte() = e.Buffer
        
        ' NEW: Route through DSP if enabled
        If useDSP AndAlso dspThread IsNot Nothing Then
            ' Write to DSP input
            dspThread.WriteInput(e.Buffer, 0, e.BytesRecorded)
            
            ' Read from DSP output (processed)
            Dim outputBuffer(e.BytesRecorded - 1) As Byte
            Dim bytesRead = dspThread.ReadOutput(outputBuffer, 0, outputBuffer.Length)
            
            If bytesRead > 0 Then
                processedBuffer = outputBuffer
            End If
        End If
        
        ' Write to WAV file (existing code)
        If recorder IsNot Nothing AndAlso recorder.IsRecording Then
            recorder.ProcessBuffer(processedBuffer, processedBuffer.Length)
        End If
        
        ' Raise buffer available (existing code)
        RaiseEvent BufferAvailable(Me, New AudioBufferEventArgs With {
            .Buffer = processedBuffer,
            .BitsPerSample = mic.BitsPerSample,
            .Channels = mic.Channels,
            .SampleRate = mic.SampleRate
        })
        
    Catch ex As Exception
        Logger.Instance.Error("Error in audio callback", ex, "RecordingManager")
    End Try
End Sub
```

### **Step 3: Update MainForm Meters**

```vb
' MainForm.vb

Private Sub UpdateDSPMeters()
    Try
        ' Get levels from appropriate source
        Dim inputLeftDb As Single = -60.0F
        Dim inputRightDb As Single = -60.0F
        Dim outputLeftDb As Single = -60.0F
        Dim outputRightDb As Single = -60.0F
        
        ' Check which audio source is active
        If audioRouter IsNot Nothing AndAlso audioRouter.IsPlaying Then
            ' FILE PLAYBACK - use AudioRouter tap points
            Dim postGainSamples = audioRouter.PostGainSamples
            If postGainSamples IsNot Nothing AndAlso postGainSamples.Length > 0 Then
                inputLeftDb = CalculatePeakDb(postGainSamples, 0)
                inputRightDb = CalculatePeakDb(postGainSamples, 1)
            End If
            
            Dim outputSamples = audioRouter.OutputSamples
            If outputSamples IsNot Nothing AndAlso outputSamples.Length > 0 Then
                outputLeftDb = CalculatePeakDb(outputSamples, 0)
                outputRightDb = CalculatePeakDb(outputSamples, 1)
            End If
            
        ElseIf recordingManager IsNot Nothing AndAlso recordingManager.IsMicArmed Then
            ' MICROPHONE - use RecordingManager tap points ? NEW!
            Dim micSamples = recordingManager.PostGainSamples ' NEW property
            If micSamples IsNot Nothing AndAlso micSamples.Length > 0 Then
                inputLeftDb = CalculatePeakDb(micSamples, 0)
                inputRightDb = CalculatePeakDb(micSamples, 1)
                outputLeftDb = inputLeftDb  ' Mic doesn't have separate output yet
                outputRightDb = inputRightDb
            End If
        End If
        
        ' Update meters
        DSPSignalFlowPanel1.UpdateMeters(inputLeftDb, inputRightDb, outputLeftDb, outputRightDb)
        
    Catch ex As Exception
        Logger.Instance.Error("Failed to update DSP meters", ex, "MainForm")
    End Try
End Sub
```

### **Step 4: Add PostGainSamples to RecordingManager**

```vb
' RecordingManager.vb

''' <summary>Gets post-gain samples for meter display (DSP tap point)</summary>
Public ReadOnly Property PostGainSamples As Single()
    Get
        If dspThread IsNot Nothing Then
            Dim available = dspThread.PostGainMonitorAvailable()
            If available > 0 Then
                Dim bufferSize = Math.Min(available, 4096)
                Dim buffer(bufferSize - 1) As Byte
                Dim bytesRead = dspThread.ReadPostGainMonitor(buffer, 0, buffer.Length)
                
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

## **? PERFORMANCE IMPACT**

### **Latency Added:**
- DSP processing: ~5ms (256-sample blocks at 44.1kHz)
- Buffer copy overhead: ~1ms
- **Total added latency: ~6ms** ? Acceptable!

### **CPU Impact:**
- DSP thread already running for file playback
- Gain processor: <1% CPU
- Monitor buffer writes: <0.5% CPU
- **Total: ~1.5% CPU overhead** ? Minimal!

### **Memory Impact:**
- DSPThread: 2 seconds * 176KB = 352KB
- Monitor buffers: 3 * 44KB = 132KB
- **Total: ~500KB additional RAM** ? Negligible!

---

## **?? TESTING PLAN**

### **Test 1: Microphone Meters**
1. Arm microphone
2. Speak into mic
3. **Expected:** Meters show live audio immediately
4. **Success:** Meters respond < 20ms latency

### **Test 2: Recording with DSP**
1. Start recording
2. Speak into mic
3. Stop recording
4. Play back WAV
5. **Expected:** Audio sounds identical (no quality loss)
6. **Success:** No audible artifacts

### **Test 3: File Playback Meters**
1. Play audio file
2. **Expected:** Meters still work (no regression)
3. **Success:** File meters work as before

### **Test 4: Switching Sources**
1. Arm mic ? meters show mic
2. Play file ? meters show file
3. Stop file ? meters show mic again
4. **Expected:** Smooth transitions, no crashes
5. **Success:** Clean source switching

---

## **?? SUCCESS CRITERIA**

- ? Microphone audio shows on meters in real-time
- ? Recording still works (no quality loss)
- ? File playback still works (no regression)
- ? Meter latency < 20ms (visual threshold)
- ? No audio dropouts or clicks
- ? CPU usage < 5% increase

---

## **?? RISKS & MITIGATION**

### **Risk 1: Recording latency**
**Mitigation:** Make DSP optional (`useDSP` flag)

### **Risk 2: Buffer underruns**
**Mitigation:** Use 2-second buffers (same as file playback)

### **Risk 3: Breaking existing recording**
**Mitigation:** Minimal changes, keep fallback path

### **Risk 4: Thread safety**
**Mitigation:** DSPThread already thread-safe (ring buffers)

---

## **?? IMPLEMENTATION ESTIMATE**

| Task | Time | Status |
|------|------|--------|
| Add DSPThread to RecordingManager | 1 hour | Pending |
| Wire audio through DSP | 1 hour | Pending |
| Add PostGainSamples property | 30 min | Pending |
| Update MainForm meters | 30 min | Pending |
| Testing & debugging | 1 hour | Pending |
| **TOTAL** | **4 hours** | - |

---

## **?? NEXT STEPS**

1. ? Design complete (this document)
2. **Start implementation** (Step 6: Create MicrophoneInputSource)
3. Wire RecordingManager
4. Update meters
5. Test thoroughly

---

**Created:** January 15, 2026  
**Status:** Ready for implementation  
**Priority:** HIGH (Phase 2 - Critical for usability)
