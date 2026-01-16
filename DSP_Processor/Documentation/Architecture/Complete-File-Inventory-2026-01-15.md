# DSP_Processor - Complete File Inventory

**Date:** January 15, 2026  
**Purpose:** Complete inventory of all files in the project, organized by directory  
**Status:** ?? **REFERENCE DOCUMENT**

---

## ?? **Project Overview**

**Total Files:** ~70 source files  
**Project Type:** Visual Basic .NET Windows Forms Application  
**Target Framework:** .NET 10.0  
**Primary Purpose:** Professional audio recording/playback with DSP capabilities

---

## ?? **Directory Structure**

```
DSP_Processor/
??? MainForm.vb                 (Main UI - **BLOATED**, needs refactoring)
??? MainForm.Designer.vb        (Designer code)
??? MainForm_NEW.vb             (?) 
??? ApplicationEvents.vb        (App-level events)
?
??? Audio/                      (Phase 2.0 Pipeline Routing)
?   ??? Routing/
?
??? AudioIO/                    (Core audio I/O)
?
??? DSP/                        (Digital Signal Processing)
?   ??? FFT/
?
??? Managers/                   (Business logic coordinators)
?
??? Models/                     (Data models)
?
??? Recording/                  (Recording engine)
?
??? Services/                   (Service layer)
?   ??? Interfaces/
?
??? UI/                         (User interface components)
?   ??? TabPanels/
?
??? Utils/                      (Utilities)
?
??? Visualization/              (Rendering & display)
```

---

## ?? **Root Directory**

### **MainForm.vb** ?? **[NEEDS REFACTORING]**
**Lines:** ~1500+  
**Purpose:** Main application form (god object)  
**Current Responsibilities:**
- UI event handling
- Audio system coordination
- Playback control
- Recording management
- Pipeline routing
- FFT processing
- Settings application
- Timer management
- Device management

**Should Be:** Thin UI layer (~500-700 lines)  
**Status:** Target for Phase 2 refactoring

---

### **MainForm.Designer.vb**
**Purpose:** Windows Forms designer-generated code  
**Contains:** Control declarations, layout, event wiring  
**Status:** Auto-generated, minimal manual edits

---

### **MainForm_NEW.vb** ?
**Purpose:** Unknown - possibly experimental/backup?  
**Action Required:** Investigate purpose, remove if obsolete

---

### **ApplicationEvents.vb**
**Purpose:** Application-level event handlers  
**Contains:** Startup, shutdown, unhandled exception handlers  
**Status:** Standard VB.NET application events

---

## ?? **Audio/Routing/** (Phase 2.0 Pipeline Architecture)

### **AudioPipelineRouter.vb** ? **[CORE]**
**Purpose:** Central pipeline routing coordinator  
**Responsibilities:**
- Routes audio through processing stages
- Manages tap points (PRE/POST DSP)
- Provides monitor buffers for FFT
- Coordinates DSP thread with file input

**Key Methods:**
- `Initialize()`
- `Route(buffer)` - Main routing logic
- `GetInputSamples()` - PRE-DSP tap
- `GetOutputSamples()` - POST-DSP tap

**Status:** Phase 2.0 foundation - working

---

### **PipelineConfiguration.vb**
**Purpose:** Pipeline configuration model  
**Contains:** Processor settings, routing maps  
**Status:** Model class

---

### **PipelineConfigurationManager.vb**
**Purpose:** Manages pipeline configurations  
**Responsibilities:**
- Load/save configurations
- Apply configurations to pipeline
- Configuration validation

**Status:** Configuration management

---

### **PipelineTemplateManager.vb**
**Purpose:** Manages pipeline templates/presets  
**Status:** Template system

---

### **DualBuffer.vb** ? **[CRITICAL]**
**Purpose:** Lock-free dual buffer for FFT  
**Design:** Double-buffering pattern  
**Used By:** AudioPipelineRouter (monitor buffers)  
**Status:** Production-ready (clicks eliminated!)

---

### **AudioStage.vb**
**Purpose:** Individual pipeline stage  
**Status:** Pipeline component

---

### **AudioPipeline.vb**
**Purpose:** Pipeline container/manager  
**Status:** Pipeline architecture

---

### **RoutingEventArgs.vb**
**Purpose:** Event args for routing events  
**Status:** Event infrastructure

---

## ?? **AudioIO/** (Audio Input/Output System)

### **AudioRouter.vb** ? **[REFACTORED - Phase 1]**
**Purpose:** Routes audio between input sources and DSP  
**Responsibilities:**
- File playback through DSP
- DSP thread management
- File feeder thread
- Monitor buffer management
- **NOW HAS EVENTS:** PlaybackStarted, PositionChanged, PlaybackStopped

**Status:** Event-driven, wired to TransportControl

---

### **MicInputSource.vb** ? **[EVENT-DRIVEN]**
**Purpose:** Microphone input (WaveIn)  
**Implements:** IInputSource  
**Key Features:**
- WaveIn callback handling
- Volume control
- **AudioDataAvailable event** (real-time callback)
- Lock-free buffer queue

**Status:** Production-ready (clicks eliminated!)

---

### **WasapiEngine.vb** ? **[EVENT-DRIVEN]**
**Purpose:** WASAPI input engine  
**Implements:** IAudioEngine  
**Key Features:**
- WASAPI exclusive/shared mode
- Low-latency capture
- **AudioDataAvailable event** (real-time callback)
- Lock-free buffer queue

**Status:** Production-ready (clicks eliminated!)

---

### **IInputSource.vb**
**Purpose:** Interface for audio input sources  
**Methods:**
- `Start()`, `Stop()`, `Read()`
- **Event:** `AudioDataAvailable` (new!)

**Status:** Interface definition

---

### **PlaybackEngine.vb**
**Purpose:** Direct WAV playback (non-DSP)  
**Features:**
- NAudio WaveOutEvent wrapper
- Position tracking
- Events: PlaybackStarted, PlaybackStopped, PositionChanged

**Status:** Functional but **unused** (all playback goes through AudioRouter)

---

### **PlaybackManager.vb**
**Purpose:** Manages PlaybackEngine  
**Status:** Wrapper around PlaybackEngine, **unused**

---

### **WaveInEngine.vb**
**Purpose:** WaveIn implementation of IAudioEngine  
**Status:** Legacy/alternative to MicInputSource

---

### **WaveFileOutput.vb**
**Purpose:** WAV file writing  
**Used By:** RecordingEngine  
**Status:** File output

---

### **DSPOutputProvider.vb** ?
**Purpose:** Bridges DSPThread to NAudio for playback  
**Implements:** IWaveProvider  
**Used By:** AudioRouter (DSP playback)  
**Status:** Critical bridge component

---

### **AudioInputManager.vb**
**Purpose:** Manages audio input devices  
**Status:** Device management

---

### **OutputDeviceManager.vb**
**Purpose:** Manages audio output devices  
**Responsibilities:**
- Enumerate output devices
- Device selection
- Default device detection

**Status:** Device management

---

### **DeviceInfo.vb**
**Purpose:** Audio device information model  
**Status:** Data model

---

### **IAudioEngine.vb**
**Purpose:** Interface for audio engines  
**Status:** Interface definition

---

### **IOutputSink.vb**
**Purpose:** Interface for audio output sinks  
**Status:** Interface definition

---

### **DriverType.vb**
**Purpose:** Enum for driver types (WASAPI, WaveIn, ASIO)  
**Status:** Enum definition

---

## ?? **DSP/** (Digital Signal Processing)

### **DSPThread.vb** ? **[CORE]**
**Purpose:** Real-time DSP processing thread  
**Architecture:** Pull-based with worker thread  
**Features:**
- Ring buffer input/output
- ProcessorChain execution
- Monitor buffers (0.5s each)
- Event-driven file feeder coordination

**Status:** Production-ready

---

### **ProcessorChain.vb**
**Purpose:** Chain of DSP processors  
**Features:**
- Add/remove processors
- Process buffers through chain
- Bypass capability

**Status:** Processor management

---

### **ProcessorBase.vb**
**Purpose:** Base class for DSP processors  
**Implements:** IProcessor  
**Status:** Abstract base class

---

### **IProcessor.vb**
**Purpose:** Interface for DSP processors  
**Methods:** `Process(buffer, offset, count)`  
**Status:** Interface definition

---

### **GainProcessor.vb**
**Purpose:** Volume/gain adjustment processor  
**Features:**
- dB-based gain control
- Sample-by-sample processing

**Status:** Working processor

---

### **PassThroughProcessor.vb**
**Purpose:** No-op processor (for testing/bypass)  
**Status:** Utility processor

---

### **AudioBuffer.vb**
**Purpose:** Audio buffer helper class  
**Status:** Utility class

---

### **FFT/FFTProcessor.vb** ?
**Purpose:** Fast Fourier Transform processing  
**Features:**
- Windowing functions
- Spectrum calculation
- Peak detection

**Status:** FFT implementation

---

### **FFT/FFTMonitorThread.vb**
**Purpose:** Dedicated FFT processing thread  
**Status:** FFT threading

---

## ?? **Managers/** (Business Logic Coordinators)

### **RecordingManager.vb** ? **[EVENT-DRIVEN]**
**Purpose:** Recording coordinator  
**Responsibilities:**
- Manage recording lifecycle
- Microphone arming/disarming
- Buffer routing to RecordingEngine
- Time tracking

**Events:**
- RecordingStarted
- RecordingStopped
- RecordingTimeUpdated
- BufferAvailable
- MicrophoneArmed

**Status:** Production-ready (event-driven)

---

### **PlaybackManager.vb**
**Purpose:** Playback coordinator (wraps PlaybackEngine)  
**Status:** **Unused** - all playback goes through AudioRouter

**Action Required:** Integrate into PlaybackCoordinator (Phase 2)

---

### **FileManager.vb**
**Purpose:** Recording file management  
**Responsibilities:**
- List recorded files
- File validation
- File deletion
- Storage management

**Status:** File management

---

### **SettingsManager.vb**
**Purpose:** Application settings management  
**Responsibilities:**
- Load/save settings
- Device preferences
- Pipeline configurations
- Spectrum settings

**Status:** Settings persistence

---

### **SpectrumManager.vb**
**Purpose:** Spectrum analysis manager  
**Status:** Spectrum coordination

---

## ?? **Models/** (Data Models)

### **RecordingOptions.vb**
**Purpose:** Recording configuration model  
**Properties:**
- Recording mode (timed/loop/continuous)
- Duration
- Loop count
- Format settings

**Status:** Data model

---

### **SpectrumSettings.vb**
**Purpose:** Spectrum analyzer settings model  
**Properties:**
- FFT size
- Window function
- Frequency range
- dB range

**Status:** Data model

---

### **MeterSettings.vb**
**Purpose:** Audio meter settings model  
**Status:** Data model

---

## ?? **Recording/** (Recording Engine)

### **RecordingEngine.vb** ? **[REFACTORED - Async Writer]**
**Purpose:** Core recording engine  
**Architecture:**
- Callback-driven (no timer!)
- Async file writer thread
- Lock-free write queue
- Direct buffer processing

**Key Methods:**
- `ProcessBuffer(buffer)` - Direct callback
- `WriterThreadLoop()` - Async disk I/O

**Status:** Production-ready (clicks eliminated!)

---

## ?? **Services/** (Service Layer)

### **Services/LoggingServiceAdapter.vb**
**Purpose:** Logging service adapter  
**Status:** Service wrapper

---

### **Services/Interfaces/ILoggingService.vb**
**Purpose:** Logging service interface  
**Status:** Interface definition

---

## ?? **UI/** (User Interface Components)

### **TransportControl.vb** ? **[CUSTOM CONTROL]**
**Purpose:** DAW-style transport control  
**Features:**
- Play/Stop/Pause/Record buttons
- LED indicators (Playing/Paused/Recording)
- Time displays (PLAY/REC)
- Progress slider
- State management

**Properties:**
- State (Playing/Recording/Stopped/Paused)
- TrackPosition, TrackDuration
- RecordingTime
- IsRecordArmed

**Events:**
- PlayClicked, StopClicked, PauseClicked, RecordClicked
- PositionChanged

**Status:** Working, **could be more self-contained** (Phase 2.2)

---

### **TransportControl.Designer.vb**
**Purpose:** TransportControl designer code  
**Status:** Auto-generated

---

### **SpectrumAnalyzerControl.vb**
**Purpose:** Spectrum analyzer display  
**Features:**
- Dual displays (INPUT/OUTPUT)
- PRE/POST DSP comparison
- Peak hold
- Frequency labels

**Status:** Working visualization

---

### **VolumeMeterControl.vb**
**Purpose:** Audio level meter  
**Features:**
- Peak/RMS display
- Clip indicator
- dB scale

**Status:** Working meter

---

### **WaveformDisplayControl.vb**
**Purpose:** Waveform display  
**Features:**
- File waveform rendering
- Zoom/pan
- Selection

**Status:** Waveform visualization

---

### **StatusIndicatorControl.vb**
**Purpose:** Status LED indicator  
**Status:** UI component

---

### **DarkTheme.vb**
**Purpose:** Dark theme colors and styles  
**Contains:** Color constants, theme settings  
**Status:** Theme definition

---

### **TabPanels/AudioSettingsPanel.vb**
**Purpose:** Audio device settings UI  
**Status:** Settings panel

---

### **TabPanels/InputTabPanel.vb**
**Purpose:** Input settings UI  
**Status:** Settings panel

---

### **TabPanels/RecordingOptionsPanel.vb**
**Purpose:** Recording options UI  
**Status:** Settings panel

---

### **TabPanels/AudioPipelinePanel.vb**
**Purpose:** Pipeline configuration UI  
**Status:** Settings panel

---

### **TabPanels/RoutingPanel.vb**
**Purpose:** Audio routing UI  
**Status:** Settings panel

---

### **TabPanels/SpectrumSettingsPanel.vb**
**Purpose:** Spectrum analyzer settings UI  
**Status:** Settings panel

---

## ?? **Utils/** (Utilities)

### **Logger.vb** ?
**Purpose:** Logging system  
**Features:**
- File logging
- Log levels (Debug, Info, Warning, Error)
- Thread-safe
- Log rotation

**Status:** Core utility

---

### **RingBuffer.vb**
**Purpose:** Circular buffer implementation  
**Used By:** DSPThread, audio engines  
**Status:** Data structure

---

### **AudioLevelMeter.vb**
**Purpose:** Audio level calculation  
**Features:**
- Peak detection
- RMS calculation
- dB conversion

**Status:** Audio utility

---

### **PerformanceMonitor.vb**
**Purpose:** Performance metrics tracking  
**Status:** Diagnostics

---

### **ResourceDeployer.vb**
**Purpose:** Embedded resource deployment  
**Status:** Resource management

---

## ?? **Visualization/** (Rendering)

### **SpectrumDisplayControl.vb**
**Purpose:** Spectrum display rendering  
**Status:** Visualization component

---

### **WaveformRenderer.vb**
**Purpose:** Waveform rendering engine  
**Status:** Rendering component

---

### **IRenderer.vb**
**Purpose:** Renderer interface  
**Status:** Interface definition

---

## ?? **File Count by Category**

| Category | Count | Purpose |
|----------|-------|---------|
| **AudioIO** | 15 | Audio input/output, drivers, devices |
| **DSP** | 8 | Signal processing, FFT |
| **Managers** | 5 | Business logic coordinators |
| **Recording** | 1 | Recording engine |
| **UI** | 13 | User interface components |
| **Audio/Routing** | 8 | Phase 2.0 pipeline architecture |
| **Models** | 3 | Data models |
| **Utils** | 5 | Utilities and helpers |
| **Visualization** | 3 | Rendering and display |
| **Services** | 2 | Service layer |
| **Root** | 4 | MainForm, app events |
| **Total** | **~67** | Source files |

---

## ?? **Files Requiring Attention (Phase 2)**

### **Critical:**
1. ? **MainForm.vb** - Needs refactoring (god object)
2. ? **MainForm_NEW.vb** - Purpose unclear, investigate
3. ?? **PlaybackManager.vb** - Unused, integrate into PlaybackCoordinator
4. ?? **PlaybackEngine.vb** - Unused, integrate into PlaybackCoordinator

### **Medium Priority:**
1. **TransportControl.vb** - Could be more self-contained
2. **SpectrumManager.vb** - Might merge into FFTCoordinator
3. **AudioInputManager.vb** - Might merge into AudioSystemController

---

## ?? **Files by Refactoring Phase**

### **Phase 2.1: Coordinators (New Files)**
- `Managers/PlaybackCoordinator.vb` ? NEW
- `Managers/RecordingCoordinator.vb` ? NEW
- `Managers/AudioSystemController.vb` ? NEW

### **Phase 2.2: Specialized (New Files)**
- `Managers/FFTCoordinator.vb` ? NEW
- `Managers/TimerCoordinator.vb` ? NEW

### **Phase 2.3: Refactor (Modified Files)**
- `MainForm.vb` ? SLIM DOWN
- `MainForm.Designer.vb` ? REMOVE TIMERS
- `TransportControl.vb` ? MAKE SELF-CONTAINED

### **Phase 2.4: Cleanup (Deprecated Files)**
- `MainForm_NEW.vb` ? INVESTIGATE/REMOVE
- `PlaybackManager.vb` ? INTEGRATE/DEPRECATE
- `PlaybackEngine.vb` ? INTEGRATE/DEPRECATE

---

## ?? **Architecture Health**

### **Strengths:**
? Clear separation of AudioIO, DSP, UI  
? Event-driven recording architecture (production-ready)  
? Lock-free buffers (no clicks!)  
? Phase 2.0 pipeline routing (foundation laid)  
? Comprehensive logging  

### **Weaknesses:**
? MainForm is a god object (1500+ lines)  
? Playback not properly coordinated  
? Some managers unused/underutilized  
? Timer management scattered  
? FFT logic in MainForm  

### **Opportunities:**
? Extract coordinators (PlaybackCoordinator, etc.)  
? Make TransportControl self-contained  
? Unified audio system controller  
? Testable coordinators  
? Cleaner MainForm  

---

## ?? **Related Documents**

- [Refactoring Plan](MainForm-Refactoring-Plan-Phase2.md)
- [Recording Architecture](../Architecture/Recording-Architecture-Final-2026-01-15.md)
- [TransportControl Analysis](../Architecture/TransportControl-Complete-Analysis-2026-01-15.md)
- [Pipeline Router Architecture](../Architecture/AudioPipelineRouter-Overall-Architecture.md)

---

**Status:** ?? **COMPLETE**  
**Purpose:** Reference for Phase 2 refactoring  
**Date:** January 15, 2026
