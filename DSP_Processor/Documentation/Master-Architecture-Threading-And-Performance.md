# DSP Processor - Master Architecture: Threading & Real-Time Performance

## ?? Document Control
- **Project:** DSP Processor - Professional Audio Recording & Processing
- **Version:** 2.0 (Master Architecture)
- **Date:** 2024
- **Status:** Architecture Planning Phase
- **Author:** Rick + GitHub Copilot

---

## ?? Executive Summary

This document defines the complete threading and performance architecture for the DSP Processor application, addressing:

1. **Audio Thread** - Real-time audio capture/playback (high priority, lock-free)
2. **DSP Thread** - Signal processing pipeline (normal priority, buffered)
3. **UI Thread** - Visualization and user interaction (normal priority, GPU-accelerated)
4. **No thread blocks another** - Lock-free communication via ring buffers
5. **Professional quality** - No glitches, no stutters, no dropouts

---

## ??? High-Level Architecture Overview

```
??????????????????????????????????????????????????????????????????
?                       WINDOWS OS / .NET CLR                    ?
?  ????????????????  ????????????????  ????????????????         ?
?  ?  Audio Thread?  ?   DSP Thread ?  ?  UI Thread   ?         ?
?  ?  (High Prio) ?  ? (Normal Prio)?  ?(Normal Prio) ?         ?
?  ????????????????  ????????????????  ????????????????         ?
??????????????????????????????????????????????????????????????????
          ?                  ?                  ?
          ? Lock-Free Ring   ? Lock-Free Ring   ? Triple Buffer
          ? Buffer (PCM)     ? Buffer (Spectrum)? (Waveform)
          ?                  ?                  ?
???????????????????????????????????????????????????????????????????
?                         DATA FLOW                               ?
?                                                                 ?
?  Audio In ? [Ring] ? DSP Pipeline ? [Ring] ? Audio Out         ?
?                ?                       ?                        ?
?            Meters (Peak/RMS)      Visualization                 ?
?                ?                       ?                        ?
?            [Triple Buffer]     [Triple Buffer]                  ?
?                ?                       ?                        ?
?          UI Thread (Draw)      UI Thread (Draw)                 ?
???????????????????????????????????????????????????????????????????
```

---

## ?? Thread Architecture Breakdown

### Thread 1: Audio Thread (High Priority) ??
**Purpose:** Real-time audio capture and playback  
**Priority:** `ThreadPriority.Highest` or WASAPI Real-Time thread  
**Latency Target:** <10ms (WASAPI) | <20ms (WaveIn)  
**Frequency:** Event-driven (audio driver callback)

#### Responsibilities
- Capture PCM audio from microphone
- Write PCM audio to speakers
- **Write** to PCM ring buffer (for DSP)
- **Write** to meter ring buffer (for UI)
- **NO DSP PROCESSING** - Just capture/playback
- **NO FILE I/O** - Ring buffer only
- **NO MEMORY ALLOCATION** - Pre-allocated buffers

#### Thread Safety
- ? Lock-free ring buffer writes
- ? No mutexes or locks
- ? Atomic operations only
- ? Pre-allocated buffer pool

#### Code Location
- `AudioIO/MicInputSource.vb` - Audio capture
- `AudioIO/PlaybackEngine.vb` - Audio playback (simplified)
- **Key Method:** `OnDataAvailable()` in MicInputSource

#### Current Implementation Status
- ? Using `ConcurrentQueue<Byte()>` (good for now)
- ?? **Future:** Switch to lock-free ring buffer for Phase 2

---

### Thread 2: DSP Thread (Normal Priority) ??
**Purpose:** Signal processing (FFT, EQ, Dynamics, Multiband)  
**Priority:** `ThreadPriority.Normal`  
**Latency Target:** <50ms end-to-end  
**Frequency:** Continuous loop (~100Hz polling)

#### Responsibilities
- **Read** from PCM ring buffer
- Run DSP pipeline:
  - Pre-filters (HP/LP)
  - Crossover (5-band split)
  - Per-band processing (EQ, Dynamics)
  - Band mixer (sum + limiter)
  - Post-filters (HP/LP)
- **Write** processed audio to output ring buffer
- **Write** spectrum data to visualization ring buffer
- Calculate FFT for spectrum analyzer

#### Thread Safety
- ? Lock-free ring buffer reads/writes
- ? Atomic parameter updates (double-buffered)
- ? No blocking on UI updates
- ? Graceful degradation on CPU overload

#### Code Location (Future - Phase 2)
- `DSP/MultibandEngine.vb` - Main DSP coordinator
- `DSP/ProcessorChain.vb` - Sequential processing
- `DSP/FFT/FFTProcessor.vb` - Spectrum analysis

#### Processing Budget
- **Target:** <20ms per audio buffer (44.1kHz, 2048 samples = 46ms buffer)
- **Buffer:** 2048 samples = 46ms @ 44.1kHz
- **DSP Overhead:** <20ms leaves 26ms safety margin

#### Graceful Degradation Strategy
```vb
If processingTime > targetTime Then
    ' Option 1: Skip this buffer (pass-through)
    ' Option 2: Reduce FFT size
    ' Option 3: Disable some processors
    ' Option 4: Log warning
End If
```

---

### Thread 3: UI Thread (Normal Priority) ??
**Purpose:** User interaction and visualization  
**Priority:** `ThreadPriority.Normal`  
**Target FPS:** 30-60 FPS  
**Frequency:** Timer-driven (~30-60Hz)

#### Responsibilities
- Display volume meters (Peak/RMS)
- Display waveform visualization
- Display spectrum analyzer
- Handle user input (buttons, sliders)
- Update status labels, timers, progress bars
- **NO AUDIO PROCESSING** - Reads from buffers only
- **NO BLOCKING** - Always responsive

#### Thread Safety
- ? Triple-buffered visualization data
- ? Atomic reads from meter buffer
- ? No locks on audio/DSP threads
- ? GPU-accelerated rendering (Phase 3)

#### Code Location
- `MainForm.vb` - UI orchestration
- `Visualization/SpectrumDisplayControl.vb` - Spectrum rendering
- `UI/VolumeMeterControl.vb` - Volume meters
- `Visualization/WaveformRenderer.vb` - Waveform display

#### Current Implementation Status
- ? Timer-based updates (30 FPS)
- ?? **Future:** GPU-accelerated rendering (Phase 3)

---

## ?? Data Flow Architecture

### Audio Capture ? DSP ? Playback

```
???????????????
? Microphone  ? Hardware
???????????????
       ? NAudio WaveInEvent
       ?
???????????????????????????????????????????
? AUDIO THREAD (High Priority)            ?
? OnDataAvailable()                        ?
?  - Copy PCM buffer                       ?
?  - Enqueue to pcmRingBuffer              ?
?  - Enqueue to meterRingBuffer            ?
?  - Return immediately (no blocking)      ?
???????????????????????????????????????????
       ? ConcurrentQueue / Lock-Free Ring
       ?
???????????????????????????????????????????
? DSP THREAD (Normal Priority)            ?
? ProcessLoop()                            ?
?  While running:                          ?
?    - Read from pcmRingBuffer             ?
?    - Run DSP pipeline                    ?
?    - Write to outputRingBuffer           ?
?    - Calculate FFT ? spectrumBuffer      ?
???????????????????????????????????????????
       ? Lock-Free Ring Buffer
       ?
???????????????????????????????????????????
? AUDIO THREAD (High Priority)            ?
? PlaybackCallback()                       ?
?  - Read from outputRingBuffer            ?
?  - Send to WaveOutEvent                  ?
?  - Return immediately                    ?
???????????????????????????????????????????
       ? NAudio WaveOutEvent
       ?
???????????????
?  Speakers   ? Hardware
???????????????
```

### Metering Data Flow

```
AUDIO THREAD
    ? Enqueue raw PCM
[meterRingBuffer] (Lock-free)
    ? Dequeue periodically
METER ANALYZER (DSP Thread or dedicated)
    ? Calculate Peak/RMS/dB
[meterDataBuffer] (Triple-buffered)
    ? Read latest
UI THREAD (30 FPS timer)
    ? Draw meters
SCREEN
```

### Spectrum Data Flow

```
DSP THREAD
    ? FFT calculation
[spectrumRingBuffer] (Lock-free)
    ? Dequeue latest
[spectrumDataBuffer] (Triple-buffered)
    ? Read for rendering
UI THREAD (30 FPS timer)
    ? Draw spectrum
SCREEN
```

---

## ?? Lock-Free Ring Buffer Implementation

### Why Lock-Free?
- ? No thread blocking
- ? No priority inversion
- ? Deterministic performance
- ? No GC pressure
- ? Cache-friendly

### Implementation Options

#### Option 1: Use System.Threading.Channels (Recommended)
**Pros:**
- ? Built into .NET Core 3.0+
- ? Lock-free and thread-safe
- ? Well-tested and optimized
- ? Supports bounded/unbounded

**Cons:**
- ?? Requires .NET Core 3.0+ (you're on .NET 10 ?)

```vb
Imports System.Threading.Channels

Public Class AudioRingBuffer
    Private channel As Channel(Of Byte())
    
    Public Sub New(capacity As Integer)
        ' Bounded channel with drop-oldest policy
        Dim options = New BoundedChannelOptions(capacity) With {
            .FullMode = BoundedChannelFullMode.DropOldest
        }
        channel = Channel.CreateBounded(Of Byte())(options)
    End Sub
    
    Public Sub Write(buffer As Byte())
        ' Non-blocking write
        channel.Writer.TryWrite(buffer)
    End Sub
    
    Public Function Read() As Byte()
        Dim buffer As Byte() = Nothing
        channel.Reader.TryRead(buffer)
        Return buffer
    End Function
End Class
```

#### Option 2: Keep ConcurrentQueue (Acceptable)
**Pros:**
- ? Already implemented
- ? Thread-safe
- ? Works well for low-latency

**Cons:**
- ?? Not truly lock-free (uses Interlocked operations)
- ?? Unbounded (can grow indefinitely)

**Verdict:** Good enough for Phase 1, upgrade to Channels in Phase 2

---

## ?? Triple Buffering for Visualization

### Why Triple Buffering?
- ? UI reads from stable buffer
- ? DSP writes to back buffer
- ? Swap buffers atomically
- ? No tearing, no locks

### Implementation

```vb
Public Class TripleBuffer(Of T)
    Private buffers(2) As T
    Private currentWriteIndex As Integer = 0
    Private currentReadIndex As Integer = 1
    Private lockObj As New Object()
    
    Public Sub New(defaultValue As T)
        For i = 0 To 2
            buffers(i) = defaultValue
        Next
    End Sub
    
    Public Sub Write(data As T)
        ' DSP thread writes here
        buffers(currentWriteIndex) = data
    End Sub
    
    Public Sub Swap()
        ' Atomically swap buffers
        SyncLock lockObj
            Dim temp = currentReadIndex
            currentReadIndex = currentWriteIndex
            currentWriteIndex = temp
        End SyncLock
    End Sub
    
    Public Function Read() As T
        ' UI thread reads here (no lock needed)
        Return buffers(currentReadIndex)
    End Function
End Class
```

### Usage

```vb
' In DSP Thread
tripleBuffer.Write(latestSpectrumData)
tripleBuffer.Swap() ' Atomic swap

' In UI Thread (timer)
Dim spectrumData = tripleBuffer.Read() ' Always stable
DrawSpectrum(spectrumData)
```

---

## ? Performance Optimization Strategies

### Memory Management

#### Pre-Allocated Buffer Pool
**Problem:** `new Byte(4096)` every audio callback = GC pressure  
**Solution:** Recycle buffers

```vb
Public Class BufferPool
    Private pool As ConcurrentBag(Of Byte())
    Private bufferSize As Integer
    
    Public Sub New(bufferSize As Integer, initialCount As Integer)
        Me.bufferSize = bufferSize
        pool = New ConcurrentBag(Of Byte())
        
        ' Pre-allocate buffers
        For i = 1 To initialCount
            pool.Add(New Byte(bufferSize - 1) {})
        Next
    End Sub
    
    Public Function Rent() As Byte()
        Dim buffer As Byte() = Nothing
        If Not pool.TryTake(buffer) Then
            ' Pool exhausted, allocate new
            buffer = New Byte(bufferSize - 1) {}
        End If
        Return buffer
    End Function
    
    Public Sub Return(buffer As Byte())
        ' Return buffer to pool
        pool.Add(buffer)
    End Sub
End Class
```

### CPU Optimization

#### SIMD Vectorization (Phase 2+)
**Use:** `System.Numerics.Vector<T>` for bulk processing

```vb
Imports System.Numerics

' Process 8 samples at once (AVX2)
For i = 0 To samples.Length - 1 Step Vector(Of Single).Count
    Dim vec = New Vector(Of Single)(samples, i)
    vec = vec * gainVector
    vec.CopyTo(samples, i)
Next
```

### GPU Acceleration (Phase 3)

#### SkiaSharp for Visualization
**Benefits:**
- ? Hardware-accelerated rendering
- ? Cross-platform
- ? 60+ FPS easily

```vb
Imports SkiaSharp

Public Class SpectrumRenderer
    Public Sub RenderToGpu(canvas As SKCanvas, spectrumData As Single())
        Using paint As New SKPaint() With {.Color = SKColors.Cyan}
            For i = 0 To spectrumData.Length - 1
                Dim x = i * barWidth
                Dim y = height - (spectrumData(i) * height)
                canvas.DrawRect(x, y, barWidth, height - y, paint)
            Next
        End Using
    End Sub
End Class
```

---

## ?? Recommended Implementation Plan

### Phase 1: Current State (Functional)
- ? Audio capture via `ConcurrentQueue`
- ? Basic metering
- ? Timer-based UI updates
- ? Waveform rendering (CPU)
- **Result:** Works, but could be optimized

### Phase 2: Lock-Free & DSP (Next)
- ?? Replace `ConcurrentQueue` with `System.Threading.Channels`
- ?? Create dedicated DSP thread
- ?? Implement lock-free ring buffers
- ?? Add FFT for spectrum analyzer
- ?? Implement multiband processor
- **Result:** Professional-grade real-time DSP

### Phase 3: GPU Acceleration (Future)
- ?? SkiaSharp for waveform rendering
- ?? SkiaSharp for spectrum analyzer
- ?? 60+ FPS visualization
- **Result:** Smooth, buttery graphics

### Phase 4: Advanced Optimization (Optional)
- ?? SIMD vectorization for DSP
- ?? Custom lock-free data structures
- ?? Profile-guided optimization
- **Result:** Studio-grade performance

---

## ?? Code Structure (Phase 2 Target)

### New Files to Create

```
DSP_Processor/
??? Audio/
?   ??? AudioRingBuffer.vb           (Lock-free ring buffer)
?   ??? BufferPool.vb                 (Memory pool)
?   ??? AudioThreadManager.vb         (Manages audio callbacks)
??? DSP/
?   ??? DSPThread.vb                  (Main DSP processing loop)
?   ??? ProcessorChain.vb             (Sequential DSP pipeline)
?   ??? FFT/
?       ??? FFTProcessor.vb           (Already exists, enhance)
??? Visualization/
?   ??? TripleBuffer.vb               (Generic triple buffer)
?   ??? SpectrumBuffer.vb             (Spectrum-specific buffer)
?   ??? MeterBuffer.vb                (Meter-specific buffer)
??? Utils/
    ??? ThreadSafeQueue.vb            (Wrapper for Channels)
```

---

## ?? Success Metrics

### Performance Targets

| Metric | Phase 1 (Current) | Phase 2 (DSP) | Phase 3 (GPU) |
|--------|-------------------|---------------|---------------|
| Audio Latency | 20ms (WaveIn) | <10ms (WASAPI) | <10ms |
| CPU Usage (Idle) | <5% | <5% | <3% |
| CPU Usage (Recording) | <10% | <25% | <20% |
| CPU Usage (DSP) | N/A | <40% | <30% |
| UI FPS | 30 FPS | 30 FPS | 60 FPS |
| Buffer Underruns | 0 | 0 | 0 |
| Dropouts | 0 | 0 | 0 |
| Memory (Idle) | 50 MB | 80 MB | 100 MB |
| Memory (Running) | 60 MB | 120 MB | 150 MB |

### Quality Targets

| Metric | Target | Critical |
|--------|--------|----------|
| Audio Glitches | 0 per hour | 0 per 10 min |
| UI Stutters | 0 per hour | 0 per 5 min |
| Thread Deadlocks | 0 | 0 |
| File Locking Issues | 0 | 0 |
| Crash Rate | <1 per 100 hours | <1 per 10 hours |

---

## ?? Critical Design Decisions

### Decision #1: Use System.Threading.Channels
**Rationale:** Proven, optimized, lock-free  
**Alternative:** Custom ring buffer (more work, more bugs)  
**Verdict:** ? Use Channels in Phase 2

### Decision #2: Dedicated DSP Thread
**Rationale:** Separate audio capture from processing  
**Alternative:** Process in audio callback (causes glitches)  
**Verdict:** ? Dedicated thread is mandatory

### Decision #3: Triple Buffering for Visualization
**Rationale:** No tearing, no locks  
**Alternative:** Direct reads (causes tearing/stuttering)  
**Verdict:** ? Triple buffer is essential

### Decision #4: Pre-Allocated Buffer Pool
**Rationale:** Reduce GC pressure  
**Alternative:** Allocate on demand (GC pauses)  
**Verdict:** ? Pool is mandatory for Phase 2

### Decision #5: GPU Rendering (Phase 3)
**Rationale:** 60 FPS without CPU load  
**Alternative:** CPU rendering (maxes out at 30 FPS)  
**Verdict:** ? GPU for Phase 3, CPU acceptable for Phase 1-2

---

## ?? Anti-Patterns to Avoid

### ? DON'T: Lock in Audio Callback
```vb
' BAD:
Private Sub OnDataAvailable(...)
    SyncLock lockObj
        ProcessAudio(buffer) ' BLOCKS AUDIO THREAD!
    End SyncLock
End Sub
```

### ? DO: Lock-Free Write
```vb
' GOOD:
Private Sub OnDataAvailable(...)
    audioRingBuffer.Write(buffer) ' Lock-free
End Sub
```

### ? DON'T: Allocate in Audio Callback
```vb
' BAD:
Private Sub OnDataAvailable(...)
    Dim copy = New Byte(buffer.Length) {} ' GC pressure
    buffer.CopyTo(copy, 0)
End Sub
```

### ? DO: Use Pre-Allocated Buffers
```vb
' GOOD:
Private Sub OnDataAvailable(...)
    Dim pooledBuffer = bufferPool.Rent()
    buffer.CopyTo(pooledBuffer, 0)
    audioRingBuffer.Write(pooledBuffer)
End Sub
```

### ? DON'T: Block UI Thread
```vb
' BAD:
Private Sub TimerUpdate_Tick(...)
    Dim spectrum = CalculateFFT(buffer) ' Blocks UI!
    DrawSpectrum(spectrum)
End Sub
```

### ? DO: Read Pre-Computed Data
```vb
' GOOD:
Private Sub TimerUpdate_Tick(...)
    Dim spectrum = spectrumBuffer.Read() ' Already computed
    DrawSpectrum(spectrum)
End Sub
```

---

## ?? References & Resources

### Threading & Concurrency
- **System.Threading.Channels:** https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/
- **Lock-Free Programming:** https://preshing.com/20120612/an-introduction-to-lock-free-programming/
- **.NET Threading Best Practices:** https://docs.microsoft.com/en-us/dotnet/standard/threading/

### Real-Time Audio
- **WASAPI Low-Latency:** https://docs.microsoft.com/en-us/windows/win32/coreaudio/wasapi
- **NAudio Documentation:** https://github.com/naudio/NAudio
- **Real-Time Audio Programming 101:** http://www.rossbencina.com/code/real-time-audio-programming-101-time-waits-for-nothing

### DSP
- **DSP Guide:** http://www.dspguide.com/
- **Multiband Processing:** https://www.soundonsound.com/techniques/multiband-compression
- **FFT Algorithms:** https://en.wikipedia.org/wiki/Cooley%E2%80%93Tukey_FFT_algorithm

### GPU Rendering
- **SkiaSharp:** https://github.com/mono/SkiaSharp
- **Hardware Acceleration in WinForms:** https://docs.microsoft.com/en-us/dotnet/desktop/winforms/advanced/

---

## ? Next Steps

### Immediate (Phase 1 Completion)
1. ? Complete MainForm refactoring (Task 0.1.3)
2. ? Test current threading model
3. ? Document any performance issues

### Short-Term (Phase 2 Planning)
1. ?? Create `AudioRingBuffer.vb` using Channels
2. ?? Create `DSPThread.vb` skeleton
3. ?? Implement `TripleBuffer<T>` generic class
4. ?? Create performance benchmarks

### Medium-Term (Phase 2 Implementation)
1. ?? Implement lock-free architecture
2. ?? Add FFT spectrum analyzer
3. ?? Implement multiband processor
4. ?? Profile and optimize

### Long-Term (Phase 3+)
1. ?? GPU-accelerated visualization
2. ?? SIMD vectorization for DSP
3. ?? Advanced optimization

---

## ?? Summary

### What You Have Now (Phase 1)
- ? Functional audio capture/playback
- ? Basic threading with `ConcurrentQueue`
- ? Timer-based UI updates
- ? CPU-rendered waveforms
- **Status:** ? Works well, professional enough

### What You'll Build (Phase 2)
- ?? Lock-free ring buffers (Channels)
- ?? Dedicated DSP processing thread
- ?? Real-time spectrum analyzer
- ?? Multiband signal processing
- ?? Triple-buffered visualization
- **Status:** ?? Next major milestone

### What's Possible (Phase 3+)
- ?? GPU-accelerated rendering (60+ FPS)
- ?? SIMD vectorization (2-4x faster DSP)
- ?? Studio-grade performance
- **Status:** ?? Future enhancement

---

**This architecture will scale from Phase 1 (functional) to Phase 4 (studio-grade) without major rewrites.** ??

---

**Document Version:** 2.0 (Master Architecture)  
**Created:** 2024  
**Author:** Rick + GitHub Copilot  
**Purpose:** Complete threading and performance architecture

**END OF MASTER ARCHITECTURE DOCUMENT** ??
