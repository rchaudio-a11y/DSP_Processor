# Audio/DSP Instructions

**Reference:** Main [copilot-instructions.md](../copilot-instructions.md)

---

## ?? Core Principles

1. **Event-Driven** - Never poll for audio data
2. **Zero-Copy** - Avoid buffer duplication
3. **Lock-Free** - Minimize locks in audio thread
4. **Latency-Critical** - Audio callbacks must complete fast (<10ms)

---

## ?? Event-Driven Audio Updates

### ? Correct Pattern

```vb
' Subscribe to audio callback:
AddHandler audioEngine.AudioDataAvailable, AddressOf OnAudioDataAvailable

' Event handler:
Private Sub OnAudioDataAvailable(sender As Object, e As AudioBufferEventArgs)
    ' Process audio buffer immediately
    ProcessAudio(e.Buffer, e.BitsPerSample, e.Channels, e.SampleRate)
    
    ' Update UI (thread-safe):
    If InvokeRequired Then
        BeginInvoke(Sub() UpdateMeters(e.Buffer))
    Else
        UpdateMeters(e.Buffer)
    End If
End Sub
```

### ? Wrong Pattern (Timer Polling)

```vb
' ? BAD - Don't poll for audio:
Private Sub Timer_Tick(sender As Object, e As EventArgs)
    Dim samples = audioEngine.GetLatestSamples()  ' Polling!
    UpdateMeters(samples)
End Sub
```

**Why wrong:** Wastes CPU, misses data, introduces latency

---

## ?? Zero-Copy Ring Buffers

### Multi-Reader Pattern

```vb
' Each reader gets own cursor:
Dim readerName = ringBuffer.CreateReader("FFT")

' Read without affecting other readers:
Dim bytesRead = ringBuffer.Read(readerName, buffer, 0, count)
```

**Benefits:**
- ? Multiple instruments share one buffer
- ? No data copying
- ? No contention between readers

**Use Cases:**
- FFT and meters both reading PostGain tap
- Multiple spectrum displays
- Simultaneous recording and monitoring

---

## ??? DSP Pipeline Architecture

### Signal Flow

```
Audio Input
    ?
[Input Tap - PreDSP]
    ?
Input GainProcessor
    ?
[Input Tap - PostGain]
    ?
[DSP Processing]
    ?
[Output Tap - PostDSP]
    ?
Output GainProcessor
    ?
[Output Tap - PreOutput]
    ?
Audio Output
```

### Tap Points

| Tap Location | Purpose | Use Cases |
|-------------|---------|-----------|
| **PreDSP** | Raw input | Input meters, raw FFT |
| **PostGain** | After input gain | Most common tap point |
| **PostDSP** | After processing | Effect verification |
| **PreOutput** | Before final output | Output meters, recording |

### Routing Examples

```vb
' Create reader at specific tap:
Dim fftReader = dspThread.CreateMonitorReader(TapPoint.PostGain, "InputFFT")

' Read from tap:
Dim available = dspThread.TapAvailable(TapPoint.PostGain, "InputFFT")
If available > 0 Then
    Dim bytesRead = dspThread.ReadFromTap(TapPoint.PostGain, "InputFFT", buffer, 0, count)
End If
```

---

## ?? Thread Safety

### Audio Thread Rules

**NEVER on audio thread:**
- ? Allocate memory
- ? Lock for long periods
- ? Call UI methods
- ? Perform I/O
- ? Log excessively

**OK on audio thread:**
- ? Process audio (fast algorithms)
- ? Write to ring buffer
- ? Update atomic variables
- ? Read processor settings (cached)

### UI Thread Rules

**NEVER on UI thread:**
- ? Block audio thread
- ? Access audio buffers directly (use ring buffers)

**OK on UI thread:**
- ? Update controls
- ? Read from ring buffers (via readers)
- ? Change processor settings (thread-safe properties)

---

## ?? Audio Metrics

### Latency Budgets

- **Audio callback:** < 10ms (hard limit)
- **Buffer processing:** < 5ms (typical)
- **Tap point writes:** < 1ms (should be instant)

### Monitoring

```vb
' Log slow callbacks:
Dim sw = Stopwatch.StartNew()
ProcessAudioBuffer(buffer)
sw.Stop()

If sw.ElapsedMilliseconds > 5 Then
    Logger.Instance.Warning($"Slow audio processing: {sw.ElapsedMilliseconds}ms")
End If
```

---

## ??? Gain Processing

### Input Gain Stage

**Location:** First processor in chain  
**Purpose:** Adjust input level before DSP  
**Control:** `RecordingManager.InputGainProcessor` or `AudioRouter.InputGainProcessor`

```vb
' Set gain (linear):
inputGainProcessor.Gain = 1.0F  ' Unity

' Set gain (dB):
inputGainProcessor.GainDB = 0.0F  ' 0 dB = unity
```

### Output Gain Stage

**Location:** Last processor in chain  
**Purpose:** Adjust final output level  
**Control:** `RecordingManager.OutputGainProcessor` or `AudioRouter.OutputGainProcessor`

```vb
' Set output gain:
outputGainProcessor.GainDB = -6.0F  ' -6 dB attenuation
```

### Gain UI Wiring

```vb
' Wire slider to processor:
Private Sub OnMicrophoneArmed(sender As Object, isArmed As Boolean)
    If isArmed AndAlso recordingManager.InputGainProcessor IsNot Nothing Then
        DspSignalFlowPanel1.SetGainProcessor(recordingManager.InputGainProcessor)
    End If
End Sub
```

---

## ?? Metering

### Real-Time Level Analysis

```vb
' Analyze audio buffer:
Dim levelData = AudioLevelMeter.AnalyzeSamples(buffer, bitsPerSample, channels)

' Update meter:
meterControl.SetLevel(levelData.PeakDB, levelData.RMSDB, levelData.IsClipping)
```

### Meter Update Frequency

- **Event-driven:** Update on every audio callback ?
- **Timer-driven:** Update at 60Hz ?

---

## ?? FFT Processing

### Adding Samples

```vb
' FFT expects interleaved stereo:
fftProcessor.AddSamples(buffer, offset, count, bitsPerSample, channels)
```

### Reading Results

```vb
' Get FFT output:
Dim magnitudes = fftProcessor.GetMagnitudes()  ' Float array
Dim binCount = fftProcessor.BinCount          ' Number of frequency bins
```

---

## ?? Best Practices

### DO
- ? Use event-driven updates for audio
- ? Implement zero-copy patterns (ring buffers)
- ? Keep audio callbacks fast (<10ms)
- ? Use multi-reader ring buffers for tap points
- ? Log performance metrics
- ? Document threading assumptions

### DON'T
- ? Poll for audio data with timers
- ? Copy buffers unnecessarily
- ? Block audio thread
- ? Access UI from audio thread
- ? Allocate memory in audio callbacks

---

**Last Updated:** 2026-01-16  
**Version:** 1.0.0
