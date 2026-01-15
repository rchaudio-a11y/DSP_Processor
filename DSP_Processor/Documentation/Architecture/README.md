# Architecture Documentation

This folder contains system architecture documentation, audio pipeline analysis, threading models, and performance considerations for the DSP_Processor project.

---

## ?? Architecture Documents

### **System Architecture**
| File | Status | Description |
|------|--------|-------------|
| [`Master-Architecture-Threading-And-Performance.md`](Master-Architecture-Threading-And-Performance.md) | ? Reference | Core architecture, threading model, performance design |

### **Audio Pipeline**
| File | Status | Description |
|------|--------|-------------|
| [`Audio-Pipeline-Analysis.md`](Audio-Pipeline-Analysis.md) | ? Reference | Audio data flow analysis |
| [`Audio-Flow-Complete-Trace.md`](Audio-Flow-Complete-Trace.md) | ? Reference | Complete audio flow tracing |
| [`Audio-Pipeline-Diagnostic-Plan.md`](Audio-Pipeline-Diagnostic-Plan.md) | ? Reference | Pipeline diagnostic approach |

---

## ?? Key Concepts

### **Threading Model**
- **UI Thread** - Windows Forms event loop, user interactions
- **Audio Capture Thread** - NAudio callback thread (real-time)
- **Processing Timer** - 20ms tick for buffer processing
- **DSP Thread** - Background processing for DSP chain
- **FFT Thread** - Async background thread for spectrum analysis (2026-01-14)

### **Buffer Architecture (2026-01-14 Update)**
- **Dual Queue System:**
  - `bufferQueue` - Critical recording path (never drops)
  - `fftQueue` - Freewheeling FFT path (can drop frames)
- **Lock-Free Concurrent Queues** - Thread-safe, no blocking
- **Async FFT Processing** - CPU-intensive work off audio thread

### **Audio Data Flow**
```
Mic ? WaveInEvent.DataAvailable (callback)
    ? MicInputSource.OnDataAvailable() (split)
        ??? bufferQueue ? RecordingEngine ? File I/O
        ??? fftQueue ? Async FFT ? Spectrum Display
```

### **Performance Targets**
- Audio thread latency: < 1ms per tick ?
- FFT processing: 5-10ms (background thread) ?
- Queue depth: < 10 buffers ?
- UI frame rate: 60 FPS ?

---

## ?? Architecture Diagrams

### **Before Buffer Optimization (Pre-2026-01-14)**
```
Mic ? Single Queue ? [Recording + FFT + Metering]
                     ? All competing, blocking
```

### **After Buffer Optimization (2026-01-14)**
```
Mic ? [Dual Queue Split]
      ?? Recording Queue (critical) ? File I/O
      ?? FFT Queue (freewheel) ? Async FFT ? UI
      ?? Metering (sync, fast) ? Level Meters
```

---

## ?? Design Patterns Used

### **Manager Pattern**
Central coordinators for subsystems:
- `RecordingManager` - Recording lifecycle
- `PlaybackManager` - Playback control
- `FileManager` - File operations
- `SettingsManager` - Configuration persistence

### **Event-Driven Architecture**
- Loosely coupled components
- Publisher-subscriber pattern
- Events for state changes and data availability

### **Producer-Consumer Pattern**
- Audio capture (producer) ? Queue ? File writer (consumer)
- Separate queues for separate consumers (recording vs FFT)

### **Async Task Pattern**
- Fire-and-forget for FFT processing
- `Task.Run()` for background work
- `BeginInvoke()` for UI marshaling

---

## ?? Related Documentation

- **Plans:** `../Plans/` - Implementation plans and guides
- **Issues:** `../Issues/Bug-Report-2026-01-14-Recording-Clicks-Pops.md` - Buffer architecture fix
- **Tasks:** `../Tasks/Task-1.1-Input-Abstraction-Layer.md` - Buffer implementation details

---

## ?? For New Contributors

### **Understanding the Architecture:**
1. Read `Master-Architecture-Threading-And-Performance.md` first
2. Review audio pipeline documents
3. Study the buffer architecture changes (2026-01-14)
4. Understand threading model and performance targets

### **Key Principles:**
- **Never block the audio thread** - Audio is real-time
- **Separate critical from non-critical** - Recording vs visualization
- **Use async for CPU-intensive work** - FFT, file I/O when possible
- **Lock-free where possible** - Concurrent queues, atomic operations

---

**Last Updated:** January 14, 2026  
**Total Documents:** 4  
**Status:** Active reference material
