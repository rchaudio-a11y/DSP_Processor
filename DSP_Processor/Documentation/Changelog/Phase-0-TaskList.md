# Phase 0: Foundation & Refactoring - Task List

## Quick Reference

**Phase:** 0 - Foundation & Refactoring  
**Status:** ?? Not Started  
**Duration:** 2-3 weeks  
**Start Date:** TBD  
**Target Completion:** TBD  
**Lead:** Rick  

---

## Phase Overview

### Goal
Stabilize the current codebase and establish architectural patterns that will support Phases 1-4 development.

### Why Phase 0 is Critical
Before building new features (WASAPI, DSP, etc.), we need:
1. **Clean Architecture** - Separate concerns so modules can evolve independently
2. **Testing Safety Net** - Automated tests to catch regressions during refactoring
3. **Diagnostic Tools** - Logging and performance monitoring for Phase 2 DSP work
4. **Quality Baseline** - Consistent standards for future code

### Success Criteria
- ? All existing functionality works (no regressions)
- ? Build time <10 seconds
- ? Test suite with >10 tests passing
- ? Code coverage >50% for core modules
- ? Zero scattered Debug.WriteLine statements
- ? Clear module boundaries

---

## Task Breakdown

### Task 0.1: Code Reorganization
**Priority:** ?? High  
**Effort:** 3-4 days  
**Status:** ? Not Started  
**Dependencies:** None  
**Blockers:** None  

#### Objective
Extract monolithic MainForm into clean, reusable modules.

#### Current Problem
MainForm.vb has ~400+ lines and handles:
- UI events
- Playback logic
- Waveform rendering (300+ lines!)
- Recording coordination
- Device management

This violates Single Responsibility Principle and makes testing impossible.

#### Solution
Create two new modules:
1. **PlaybackEngine** - All playback logic
2. **WaveformRenderer** - All visualization logic

#### Subtasks

##### 0.1.1: Create PlaybackEngine.vb ?
**Effort:** 1 day  
**File:** `AudioIO/PlaybackEngine.vb` (new)

**Spec:**
```vb
Public Class PlaybackEngine
    Implements IDisposable
    
    ' Properties
    Public ReadOnly Property IsPlaying As Boolean
    Public ReadOnly Property CurrentPosition As TimeSpan
    Public ReadOnly Property TotalDuration As TimeSpan
    
    ' Methods
    Public Sub Load(filepath As String)
    Public Sub Play()
    Public Sub Pause()
    Public Sub Stop()
    Public Sub Seek(position As TimeSpan)
    
    ' Events
    Public Event PlaybackStopped As EventHandler(Of StoppedEventArgs)
    Public Event PositionChanged As EventHandler(Of TimeSpan)
End Class
```

**What to Extract from MainForm:**
- Lines 150-180: `lstRecordings_DoubleClick` playback code
- Lines 195-210: `OnPlaybackStopped` handler
- Lines 215-230: `TimerPlayback_Tick` position tracking
- `playbackOutput` and `playbackReader` fields

**Acceptance:**
- [ ] PlaybackEngine.vb created
- [ ] All playback logic moved
- [ ] WaveOutEvent lifetime managed correctly
- [ ] Events fire properly
- [ ] IDisposable implemented

---

##### 0.1.2: Create WaveformRenderer.vb ?
**Effort:** 1.5 days  
**File:** `Visualization/WaveformRenderer.vb` (new)

**Spec:**
```vb
Public Class WaveformRenderer
    Implements IRenderer
    
    ' Properties
    Public Property BackgroundColor As Color
    Public Property WaveformColorLeft As Color
    Public Property WaveformColorRight As Color
    Public Property Width As Integer
    Public Property Height As Integer
    
    ' Methods
    Public Function Render(audioFile As String) As Bitmap
    Public Function RenderMono(audioFile As String) As Bitmap
    Public Function RenderStereo(audioFile As String) As Bitmap
    Public Sub ClearCache()
End Class
```

**What to Extract from MainForm:**
- Lines 240-350: `DrawWaveform()` method
- Lines 355-420: `DrawWaveformMono()` method
- Peak detection logic
- Auto-normalization code

**Optimizations to Add:**
- Bitmap caching (don't redraw on every selection change)
- Progressive rendering for large files
- LOD (Level of Detail) system

**Acceptance:**
- [ ] WaveformRenderer.vb created
- [ ] Mono rendering works
- [ ] Stereo rendering works
- [ ] Auto-zoom normalization works
- [ ] Performance >30 FPS

---

##### 0.1.3: Refactor MainForm.vb ?
**Effort:** 0.5 days  
**File:** `MainForm.vb` (modify)

**Spec:**
```vb
' MainForm should now be thin UI orchestration
Private playbackEngine As PlaybackEngine
Private waveformRenderer As WaveformRenderer

Private Sub lstRecordings_DoubleClick(...) Handles ...
    playbackEngine.Load(fullPath)
    playbackEngine.Play()
End Sub

Private Sub lstRecordings_SelectedIndexChanged(...) Handles ...
    Dim waveform = waveformRenderer.Render(fullPath)
    picWaveform.Image = waveform
End Sub
```

**Changes:**
- Replace playback code with `playbackEngine` calls
- Replace rendering code with `waveformRenderer` calls
- Wire up events from new modules to UI updates
- Remove ~300 lines of code

**Acceptance:**
- [ ] MainForm.vb <150 lines
- [ ] All playback works
- [ ] All waveform display works
- [ ] No code duplication
- [ ] Build succeeds

---

#### Task 0.1 Deliverables
- ? `AudioIO/PlaybackEngine.vb` (new, ~150 lines)
- ? `Visualization/WaveformRenderer.vb` (new, ~300 lines)
- ? `MainForm.vb` (refactored, ~150 lines, -300 lines deleted)

#### Verification Steps
1. ? Run application
2. ? Select audio file in list
3. ? Verify waveform displays correctly
4. ? Double-click to play
5. ? Verify playback works
6. ? Verify progress bar updates
7. ? Click stop
8. ? Verify playback stops cleanly

---

### Task 0.2: Interface Standardization
**Priority:** ?? High  
**Effort:** 2-3 days  
**Status:** ? Not Started  
**Dependencies:** Task 0.1  
**Blockers:** None  

#### Objective
Create standard interfaces for future architectural components.

#### Why This Matters
- Phase 1: Need `IAudioEngine` for WASAPI/ASIO
- Phase 2: Need `IProcessor` for DSP modules
- Already done: `IInputSource`, `IOutputSink`
- Task 0.1 created: `IRenderer`

#### Subtasks

##### 0.2.1: Create IProcessor.vb ?
**Effort:** 0.5 days  
**File:** `DSP/IProcessor.vb` (new)

**Spec:**
```vb
''' <summary>
''' Base interface for all DSP processors (EQ, Dynamics, Filters, etc.)
''' </summary>
Public Interface IProcessor
    ''' <summary>
    ''' Process audio buffer in-place
    ''' </summary>
    Sub Process(buffer As AudioBuffer)
    
    ''' <summary>
    ''' Additional latency added by this processor (in samples)
    ''' </summary>
    ReadOnly Property Latency As Integer
    
    ''' <summary>
    ''' Bypass this processor (pass-through)
    ''' </summary>
    Property Bypassed As Boolean
    
    ''' <summary>
    ''' Reset internal state (clear delays, filters, etc.)
    ''' </summary>
    Sub Reset()
    
    ''' <summary>
    ''' Processor name for UI display
    ''' </summary>
    ReadOnly Property Name As String
End Interface
```

**XML Documentation:**
- [ ] Complete summary tags
- [ ] Parameter descriptions
- [ ] Usage examples
- [ ] See Also links

**Acceptance:**
- [ ] Interface compiles
- [ ] XML docs complete
- [ ] Design reviewed

---

##### 0.2.2: Create IAudioEngine.vb ?
**Effort:** 1 day  
**File:** `AudioIO/IAudioEngine.vb` (new)

**Spec:**
```vb
''' <summary>
''' Base interface for audio input engines (WaveIn, WASAPI, ASIO)
''' </summary>
Public Interface IAudioEngine
    Inherits IDisposable
    
    ''' <summary>
    ''' Start capturing audio
    ''' </summary>
    Sub Start()
    
    ''' <summary>
    ''' Stop capturing audio
    ''' </summary>
    Sub [Stop]()
    
    ''' <summary>
    ''' Is currently recording
    ''' </summary>
    ReadOnly Property IsRecording As Boolean
    
    ''' <summary>
    ''' Audio format properties
    ''' </summary>
    ReadOnly Property SampleRate As Integer
    ReadOnly Property Channels As Integer
    ReadOnly Property BitsPerSample As Integer
    
    ''' <summary>
    ''' Actual measured latency (milliseconds)
    ''' </summary>
    ReadOnly Property Latency As Integer
    
    ''' <summary>
    ''' Driver/engine name
    ''' </summary>
    ReadOnly Property EngineName As String
    
    ''' <summary>
    ''' Fired when audio data is available
    ''' </summary>
    Event DataAvailable As EventHandler(Of AudioDataEventArgs)
    
    ''' <summary>
    ''' Fired when recording stops (error or user)
    ''' </summary>
    Event RecordingStopped As EventHandler(Of StoppedEventArgs)
End Interface

''' <summary>
''' Event args for audio data
''' </summary>
Public Class AudioDataEventArgs
    Inherits EventArgs
    
    Public Property Buffer As Byte()
    Public Property BytesRecorded As Integer
End Class
```

**Design Notes:**
- Matches NAudio patterns
- Extensible for WASAPI/ASIO
- Thread-safe by design

**Acceptance:**
- [ ] Interface compiles
- [ ] XML docs complete
- [ ] MicInputSource can implement it (Phase 1)

---

##### 0.2.3: Create IRenderer.vb ?
**Effort:** 0.5 days  
**File:** `Visualization/IRenderer.vb` (new)

**Spec:**
```vb
''' <summary>
''' Base interface for all visualization renderers
''' </summary>
Public Interface IRenderer
    ''' <summary>
    ''' Render visualization to bitmap
    ''' </summary>
    Function Render(data As Object, width As Integer, height As Integer) As Bitmap
    
    ''' <summary>
    ''' Visual properties
    ''' </summary>
    Property BackgroundColor As Color
    Property ForegroundColor As Color
    
    ''' <summary>
    ''' Clear any cached rendering data
    ''' </summary>
    Sub ClearCache()
    
    ''' <summary>
    ''' Renderer name
    ''' </summary>
    ReadOnly Property Name As String
End Interface
```

**Acceptance:**
- [ ] Interface compiles
- [ ] WaveformRenderer implements it
- [ ] XML docs complete

---

##### 0.2.4: Update Existing Code to Use Interfaces ?
**Effort:** 1 day  
**Files:** `MicInputSource.vb`, `WaveformRenderer.vb` (from 0.1)

**Changes:**
1. MicInputSource should implement IAudioEngine (or refactor to adapter pattern)
2. WaveformRenderer should implement IRenderer
3. Update constructors/methods to match interface contracts

**Acceptance:**
- [ ] All implementations compile
- [ ] No breaking changes to existing code
- [ ] Interfaces provably useful

---

#### Task 0.2 Deliverables
- ? `DSP/IProcessor.vb` (new, ~30 lines + XML)
- ? `AudioIO/IAudioEngine.vb` (new, ~40 lines + XML)
- ? `Visualization/IRenderer.vb` (new, ~25 lines + XML)
- ? Updated implementations

#### Verification Steps
1. ? Build succeeds
2. ? IntelliSense shows XML docs
3. ? Interfaces follow SOLID principles
4. ? Code review passes

---

### Task 0.3: Logging & Diagnostics
**Priority:** ?? Medium  
**Effort:** 2 days  
**Status:** ? Not Started  
**Dependencies:** None  
**Blockers:** None  

#### Objective
Replace scattered `Debug.WriteLine` with centralized, production-ready logging.

#### Current Problem
```vb
' Scattered throughout codebase:
Debug.WriteLine("MicInputSource reports channels: " & channels)
Debug.WriteLine("Recording started")
' etc...
```

Problems:
- Not visible in release builds
- No file output
- No severity levels
- No performance tracking
- Can't be disabled

#### Solution
Create `Logger` and `PerformanceMonitor` classes.

#### Subtasks

##### 0.3.1: Create Logger.vb ?
**Effort:** 1 day  
**File:** `Utils/Logger.vb` (new)

**Spec:**
```vb
Public Enum LogLevel
    Debug = 0
    Info = 1
    Warning = 2
    [Error] = 3
    Critical = 4
End Enum

Public Class Logger
    Private Shared instance As Logger
    
    ' Singleton
    Public Shared ReadOnly Property Instance As Logger
        Get
            If instance Is Nothing Then
                instance = New Logger()
            End If
            Return instance
        End Get
    End Property
    
    ' Configuration
    Public Property MinimumLevel As LogLevel = LogLevel.Info
    Public Property LogToFile As Boolean = True
    Public Property LogToConsole As Boolean = True
    Public Property LogDirectory As String = "Logs"
    Public Property MaxLogFiles As Integer = 10
    Public Property MaxLogSizeMB As Integer = 10
    
    ' Logging methods
    Public Sub Debug(message As String, Optional context As String = Nothing)
    Public Sub Info(message As String, Optional context As String = Nothing)
    Public Sub Warning(message As String, Optional context As String = Nothing)
    Public Sub [Error](message As String, Optional ex As Exception = Nothing, Optional context As String = Nothing)
    Public Sub Critical(message As String, Optional ex As Exception = Nothing, Optional context As String = Nothing)
    
    ' Performance tracking
    Public Function StartTimer(name As String) As LogTimer
    
    ' Cleanup
    Public Sub Flush()
    Public Sub Close()
End Class

Public Class LogTimer
    Implements IDisposable
    
    Private startTime As DateTime
    Private name As String
    
    ' Automatically logs elapsed time on disposal
    Public Sub Dispose() Implements IDisposable.Dispose
        Logger.Instance.Debug($"{name} took {elapsed}ms")
    End Sub
End Class
```

**Features:**
- Thread-safe file writing
- Automatic log rotation
- Structured log format: `[YYYY-MM-DD HH:MM:SS.fff] [LEVEL] [Context] Message`
- Performance timer using `Using` pattern

**Example Usage:**
```vb
' Simple logging
Logger.Instance.Info("Application started")
Logger.Instance.Error("Failed to load file", ex, "FileIO")

' Performance tracking
Using timer = Logger.Instance.StartTimer("Waveform Rendering")
    ' ... rendering code ...
End Using
' Automatically logs: "[DEBUG] Waveform Rendering took 45ms"
```

**Acceptance:**
- [ ] Logger singleton works
- [ ] All log levels work
- [ ] File rotation works (tested with 11MB log)
- [ ] Thread-safe (tested with concurrent writes)
- [ ] Performance <1ms overhead
- [ ] LogTimer RAII pattern works

---

##### 0.3.2: Create PerformanceMonitor.vb ?
**Effort:** 0.5 days  
**File:** `Utils/PerformanceMonitor.vb` (new)

**Spec:**
```vb
Public Class PerformanceMonitor
    Private Shared instance As PerformanceMonitor
    
    Public Shared ReadOnly Property Instance As PerformanceMonitor
    
    ' CPU Monitoring
    Public ReadOnly Property CpuUsagePercent As Double
    Public ReadOnly Property ThreadCpuUsagePercent As Double
    
    ' Memory Monitoring
    Public ReadOnly Property MemoryUsageMB As Double
    Public ReadOnly Property PrivateBytesMB As Double
    Public ReadOnly Property GCTotalMemoryMB As Double
    
    ' Audio Monitoring
    Public Property AudioLatencyMs As Double
    Public ReadOnly Property BufferUnderrunCount As Integer
    Public ReadOnly Property BufferOverrunCount As Integer
    
    ' Methods
    Public Sub StartMonitoring()
    Public Sub StopMonitoring()
    Public Sub ResetCounters()
    Public Sub RecordBufferUnderrun()
    Public Sub RecordBufferOverrun()
    
    ' Event when metrics exceed thresholds
    Public Event PerformanceWarning As EventHandler(Of PerformanceEventArgs)
End Class
```

**Use Cases:**
- Phase 2 DSP: Track CPU usage during multiband processing
- Phase 1 WASAPI: Monitor actual audio latency
- Debugging: Detect buffer underruns causing audio glitches

**Acceptance:**
- [ ] CPU tracking works
- [ ] Memory tracking works
- [ ] Audio metrics work
- [ ] Warning events fire correctly

---

##### 0.3.3: Replace Debug.WriteLine Calls ?
**Effort:** 0.5 days  
**Files:** All .vb files

**Process:**
1. Search for `Debug.WriteLine`
2. Replace with appropriate Logger call:
   - `Debug.WriteLine("X")` ? `Logger.Instance.Debug("X")`
   - Informational ? `Logger.Instance.Info("X")`
   - Errors ? `Logger.Instance.Error("X", ex)`
3. Add context where helpful:
   - `Logger.Instance.Debug("Buffer size: 20ms", "MicInputSource")`

**Files to Update:**
- `MicInputSource.vb` (line 22)
- `RecordingEngine.vb` (if any)
- `MainForm.vb` (if any)

**Acceptance:**
- [ ] Zero `Debug.WriteLine` in codebase
- [ ] All log messages use Logger
- [ ] Build succeeds
- [ ] Logs appear in file

---

#### Task 0.3 Deliverables
- ? `Utils/Logger.vb` (new, ~200 lines)
- ? `Utils/PerformanceMonitor.vb` (new, ~150 lines)
- ? Updated all files to use Logger

#### Verification Steps
1. ? Run application
2. ? Check `Logs/` folder created
3. ? Check log file contains timestamped entries
4. ? Perform long operation (10-minute recording)
5. ? Check log file rotates if >10MB
6. ? Check PerformanceMonitor reports CPU <25%

---

### Task 0.4: Unit Testing Framework
**Priority:** ?? High  
**Effort:** 3 days  
**Status:** ? Not Started  
**Dependencies:** Tasks 0.1, 0.2, 0.3  
**Blockers:** None  

#### Objective
Establish automated testing to prevent regressions during Phase 1-4 development.

#### Why This Matters
Without tests:
- Refactoring is scary (might break something)
- DSP algorithm bugs are hard to catch
- Performance regressions go unnoticed
- New features break old features

With tests:
- Refactor with confidence
- Catch bugs immediately
- Document expected behavior
- Enable continuous integration

#### Subtasks

##### 0.4.1: Create Test Project ?
**Effort:** 0.5 days  
**File:** `DSP_Processor.Tests/` (new project)

**Steps:**
1. Visual Studio ? Add New Project ? xUnit Test Project (VB.NET)
2. Add reference to `DSP_Processor` main project
3. Install NuGet packages:
   - `xunit` (core framework)
   - `xunit.runner.visualstudio` (VS integration)
   - `coverlet.collector` (code coverage)
4. Configure test settings

**Project Structure:**
```
DSP_Processor.Tests/
??? AudioIO/
?   ??? MicInputSourceTests.vb
?   ??? WavFileOutputTests.vb
?   ??? PlaybackEngineTests.vb
??? Recording/
?   ??? RecordingEngineTests.vb
??? Visualization/
?   ??? WaveformRendererTests.vb
??? Utils/
?   ??? LoggerTests.vb
??? TestHelpers/
    ??? AudioTestData.vb
```

**Acceptance:**
- [ ] Test project builds
- [ ] Test runner discovers tests
- [ ] Can run tests from VS Test Explorer
- [ ] Can run tests from command line

---

##### 0.4.2: Create AudioIO Tests ?
**Effort:** 1 day  
**Files:** `AudioIO/MicInputSourceTests.vb`, `AudioIO/WavFileOutputTests.vb`, `AudioIO/PlaybackEngineTests.vb`

**MicInputSourceTests.vb:**
```vb
Imports Xunit
Imports DSP_Processor.AudioIO

Public Class MicInputSourceTests
    
    <Fact>
    Public Sub Test_Constructor_ValidParameters_Succeeds()
        ' Arrange
        Dim sampleRate = 44100
        Dim channels = "Stereo (2)"
        Dim bits = 16
        
        ' Act
        Dim mic = New MicInputSource(sampleRate, channels, bits)
        
        ' Assert
        Assert.Equal(44100, mic.SampleRate)
        Assert.Equal(2, mic.Channels)
        Assert.Equal(16, mic.BitsPerSample)
    End Sub
    
    <Theory>
    <InlineData("Mono (1)", 1)>
    <InlineData("Stereo (2)", 2)>
    Public Sub Test_ChannelParsing_ValidStrings_ParsesCorrectly(channelStr As String, expected As Integer)
        Dim mic = New MicInputSource(44100, channelStr, 16)
        Assert.Equal(expected, mic.Channels)
    End Sub
    
    <Theory>
    <InlineData(10)>
    <InlineData(20)>
    <InlineData(50)>
    Public Sub Test_BufferSize_ValidMilliseconds_SetsCorrectly(bufferMs As Integer)
        ' Test that buffer size is actually set correctly
        ' (would need internal access or property to verify)
    End Sub
    
    <Fact>
    Public Sub Test_Dispose_ProperCleanup_NoExceptions()
        Dim mic = New MicInputSource(44100, "Mono (1)", 16)
        mic.Dispose()
        ' Should not throw
    End Sub
End Class
```

**WavFileOutputTests.vb:**
```vb
Public Class WavFileOutputTests
    
    <Fact>
    Public Sub Test_Constructor_CreatesFile_WithValidHeader()
        Dim path = Path.GetTempFileName()
        Dim output = New WavFileOutput(path, 44100, 2, 16)
        output.CloseSink()
        
        Assert.True(File.Exists(path))
        
        ' Verify WAV header
        Dim bytes = File.ReadAllBytes(path)
        Assert.Equal("RIFF"c, System.Text.Encoding.ASCII.GetString(bytes, 0, 4))
        Assert.Equal("WAVE"c, System.Text.Encoding.ASCII.GetString(bytes, 8, 4))
    End Sub
    
    <Fact>
    Public Sub Test_Write_UpdatesDataLength_Correctly()
        Dim path = Path.GetTempFileName()
        Dim output = New WavFileOutput(path, 44100, 1, 16)
        
        Dim buffer(1023) As Byte
        output.Write(buffer, buffer.Length)
        output.CloseSink()
        
        Dim info = New FileInfo(path)
        Assert.Equal(44 + 1024, info.Length) ' Header + data
    End Sub
End Class
```

**Acceptance:**
- [ ] 8+ tests for MicInputSource
- [ ] 4+ tests for WavFileOutput
- [ ] 5+ tests for PlaybackEngine
- [ ] All tests pass
- [ ] Coverage >60% for AudioIO

---

##### 0.4.3: Create Recording Tests ?
**Effort:** 0.5 days  
**File:** `Recording/RecordingEngineTests.vb`

**RecordingEngineTests.vb:**
```vb
Public Class RecordingEngineTests
    
    <Fact>
    Public Sub Test_StartRecording_CreatesOutputFolder()
        Dim recorder = New RecordingEngine() With {
            .OutputFolder = "TestRecordings"
        }
        recorder.StartRecording()
        Assert.True(Directory.Exists("TestRecordings"))
        recorder.StopRecording()
    End Sub
    
    <Fact>
    Public Sub Test_AutoNaming_Increments_Correctly()
        ' Test that Take_001.wav, Take_002.wav, etc. work
    End Sub
    
    <Fact>
    Public Sub Test_StopRecording_ClosesFile_Cleanly()
        ' Verify file is closed and can be read immediately
    End Sub
End Class
```

**Acceptance:**
- [ ] 5+ tests for RecordingEngine
- [ ] All tests pass
- [ ] Coverage >50% for Recording

---

##### 0.4.4: Create Visualization Tests ?
**Effort:** 0.5 days  
**File:** `Visualization/WaveformRendererTests.vb`

**WaveformRendererTests.vb:**
```vb
Public Class WaveformRendererTests
    
    <Fact>
    Public Sub Test_RenderMono_ReturnsValidBitmap()
        Dim renderer = New WaveformRenderer()
        ' Use test audio file
        Dim bmp = renderer.RenderMono("test_mono.wav")
        Assert.NotNull(bmp)
        Assert.Equal(800, bmp.Width)
        Assert.Equal(400, bmp.Height)
    End Sub
    
    <Fact>
    Public Sub Test_RenderStereo_ReturnsValidBitmap()
        Dim renderer = New WaveformRenderer()
        Dim bmp = renderer.RenderStereo("test_stereo.wav")
        Assert.NotNull(bmp)
    End Sub
End Class
```

**Test Data:**
- Create small test audio files (1-2 seconds)
- Include in project as embedded resources

**Acceptance:**
- [ ] 3+ tests for WaveformRenderer
- [ ] All tests pass
- [ ] Coverage >40% for Visualization

---

##### 0.4.5: Create Utils Tests ?
**Effort:** 0.25 days  
**File:** `Utils/LoggerTests.vb`

**LoggerTests.vb:**
```vb
Public Class LoggerTests
    
    <Fact>
    Public Sub Test_Logger_Singleton_ReturnsSameInstance()
        Dim log1 = Logger.Instance
        Dim log2 = Logger.Instance
        Assert.Same(log1, log2)
    End Sub
    
    <Fact>
    Public Sub Test_LogToFile_CreatesLogFile()
        Logger.Instance.LogToFile = True
        Logger.Instance.Info("Test message")
        Logger.Instance.Flush()
        
        Dim logFiles = Directory.GetFiles(Logger.Instance.LogDirectory, "*.log")
        Assert.NotEmpty(logFiles)
    End Sub
    
    <Fact>
    Public Sub Test_LogTimer_AutomaticallyLogsTime()
        Using timer = Logger.Instance.StartTimer("Test")
            Thread.Sleep(100)
        End Using
        ' Verify log contains "Test took ~100ms"
    End Sub
End Class
```

**Acceptance:**
- [ ] 5+ tests for Logger
- [ ] All tests pass
- [ ] Coverage >70% for Utils

---

##### 0.4.6: Set Up Code Coverage ?
**Effort:** 0.25 days  
**Tools:** Coverlet + ReportGenerator

**Steps:**
1. Run tests with coverage:
   ```bash
   dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
   ```

2. Generate HTML report:
   ```bash
   reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport
   ```

3. Open `coveragereport/index.html`

**Coverage Targets:**
- AudioIO: >60%
- Recording: >50%
- Visualization: >40%
- Utils: >70%
- **Overall: >50%**

**Acceptance:**
- [ ] Coverage report generates
- [ ] Overall coverage >50%
- [ ] Can identify uncovered code
- [ ] Integrated with VS Code Coverage tool

---

#### Task 0.4 Deliverables
- ? `DSP_Processor.Tests/` project (functional)
- ? 25+ unit tests (all passing)
- ? Code coverage >50%
- ? Coverage report HTML

#### Verification Steps
1. ? Open Test Explorer in VS
2. ? Run All Tests
3. ? All tests pass (green checkmarks)
4. ? Check coverage report
5. ? Verify >50% coverage

---

## Phase 0 Daily Checklist

### Daily Start Routine
- [ ] Pull latest from Git
- [ ] Run all tests (verify passing)
- [ ] Review today's task
- [ ] Open relevant files

### Daily End Routine
- [ ] Run all tests (verify still passing)
- [ ] Update changelog with what was done
- [ ] Commit changes with meaningful message
- [ ] Push to Git
- [ ] Update task status

---

## Risk Mitigation

### Risk: Breaking Existing Functionality
**Mitigation:**
- Run application after every subtask
- Test all features manually
- Automated tests catch regressions

### Risk: Refactoring Takes Longer Than Expected
**Mitigation:**
- Break tasks into small chunks
- Can skip Task 0.3 (logging) if time-constrained
- Tasks 0.1, 0.2, 0.4 are mandatory

### Risk: Tests Are Hard to Write
**Mitigation:**
- Focus on "happy path" tests first
- Skip edge cases initially
- 50% coverage is acceptable (not 100%)

---

## Success Metrics Dashboard

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| **Tasks Complete** | 4/4 | 0/4 | ?? |
| **Build Time** | <10s | TBD | ? |
| **Test Count** | >10 | 0 | ?? |
| **Tests Passing** | 100% | N/A | ? |
| **Code Coverage** | >50% | 0% | ?? |
| **MainForm.vb Lines** | <200 | 400+ | ?? |
| **Debug.WriteLine Count** | 0 | ~5 | ?? |

**Legend:** ?? Not Started | ?? In Progress | ? Complete

---

## Completion Criteria

### Must Have (Blocking)
- ? Task 0.1 complete (code reorganization)
- ? Task 0.2 complete (interfaces)
- ? Task 0.4 complete (testing)
- ? All tests passing
- ? Build succeeds
- ? No regressions (existing features work)

### Should Have (High Priority)
- ? Task 0.3 complete (logging)
- ? Code coverage >50%
- ? Changelog complete
- ? Documentation updated

### Nice to Have (Optional)
- ? Code coverage >60%
- ? Performance benchmarks
- ? Architecture diagrams

---

## Next Phase Preview

### Phase 1: Advanced Input Engine
After Phase 0, we'll be ready to:
- Implement `IAudioEngine` interface (created in Task 0.2)
- Add WASAPI support
- Tested by framework (from Task 0.4)
- Logged properly (from Task 0.3)
- Clean architecture (from Task 0.1)

**Estimated Start:** 1 week after Phase 0 completion

---

## Questions & Answers

### Q: Can I skip Task 0.3 (logging)?
**A:** Yes, if time-constrained. Logging is helpful but not critical for Phases 1-2. You can add it later.

### Q: What if tests fail?
**A:** That's the point! Tests catch bugs. Fix the bug, verify tests pass, then continue.

### Q: How do I know code coverage is good enough?
**A:** 50% is acceptable for Phase 0. Focus on core logic (AudioIO, Recording). UI code can have lower coverage.

### Q: What if refactoring breaks something?
**A:** That's why we test after every subtask. Revert the commit and try a different approach.

---

**Document Version:** 1.0  
**Last Updated:** [Date]  
**Author:** Rick  

**Status:** ?? Phase 0 Not Started - Ready to Begin

---

**END OF TASK LIST**
