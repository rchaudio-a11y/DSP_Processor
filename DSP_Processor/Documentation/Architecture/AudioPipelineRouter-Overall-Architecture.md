# Audio Pipeline Router - Overall Architecture Plan

**Date:** January 15, 2026  
**Purpose:** Centralized routing controller for flexible audio pipeline management  
**Status:** ?? **PLANNING PHASE**  
**Priority:** ?? **FOUNDATIONAL - DO THIS RIGHT**

---

## ?? Vision

### **Core Concept:**
Create a single, centralized `AudioPipelineRouter.vb` file that:
- **Defines** all audio paths and connections
- **Controls** which paths are active
- **Manages** buffer routing and processing options
- **Provides** simple API for UI to change routing
- **Offloads** complexity from other components

### **User's Goal:**
> "UI will be able to change the mapping with options like bypass dsp, enable fft's or meters from freewheeling buffers at any point in the dsp"

---

## ?? Architecture Overview

### **Current Problem:**
```
Routing logic scattered everywhere:
- MicInputSource decides what to enqueue
- RecordingManager decides when to drain
- MainForm decides what to do with buffers
- AudioRouter has its own routing
? NO CENTRAL CONTROL!
```

### **Proposed Solution:**
```
AudioPipelineRouter (Central Authority)
    ?
Defines ALL routing:
??? Source Selection (Mic, File, Line In)
??? Processing Path (Bypass, DSP, Effects)
??? Monitoring Points (FFT Pre, FFT Post, Meters)
??? Destinations (Recording, Playback, Both)
??? Buffer Management (Sizes, Counts, Strategies)
```

---

## ??? AudioPipelineRouter Design

### **Class Structure:**

```vb
Namespace Audio.Routing

    ''' <summary>
    ''' Central routing controller for audio pipeline.
    ''' All audio paths, processing options, and buffer routing defined here.
    ''' </summary>
    Public Class AudioPipelineRouter
        
        #Region "Configuration"
        
        ''' <summary>Audio source configuration</summary>
        Public Property SourceConfig As SourceConfiguration
        
        ''' <summary>DSP processing configuration</summary>
        Public Property ProcessingConfig As ProcessingConfiguration
        
        ''' <summary>Monitoring/metering configuration</summary>
        Public Property MonitoringConfig As MonitoringConfiguration
        
        ''' <summary>Recording/playback destinations</summary>
        Public Property DestinationConfig As DestinationConfiguration
        
        #End Region
        
        #Region "Routing Methods"
        
        ''' <summary>Route incoming audio buffer through pipeline</summary>
        Public Sub RouteAudioBuffer(buffer As Byte(), source As AudioSource)
        
        ''' <summary>Get active routing map (for visualization/debugging)</summary>
        Public Function GetActiveRoutingMap() As RoutingMap
        
        ''' <summary>Change routing configuration (called by UI)</summary>
        Public Sub UpdateRouting(newConfig As PipelineConfiguration)
        
        #End Region
        
        #Region "Events"
        
        ''' <summary>Fired when routing configuration changes</summary>
        Public Event RoutingChanged As EventHandler(Of RoutingChangedEventArgs)
        
        ''' <summary>Fired when buffer needs recording</summary>
        Public Event BufferForRecording As EventHandler(Of AudioBufferEventArgs)
        
        ''' <summary>Fired when buffer needs monitoring</summary>
        Public Event BufferForMonitoring As EventHandler(Of AudioBufferEventArgs)
        
        #End Region
        
    End Class
    
End Namespace
```

---

## ?? Configuration Classes

### **1. SourceConfiguration**
```vb
Public Class SourceConfiguration
    ''' <summary>Which audio source is active</summary>
    Public Property ActiveSource As AudioSourceType
    
    ''' <summary>Source-specific settings</summary>
    Public Property SourceSettings As AudioDeviceSettings
    
    Public Enum AudioSourceType
        Microphone      ' Live mic input
        LineIn          ' Line input (future)
        File            ' File playback
        NetworkStream   ' Network audio (future)
    End Enum
End Class
```

### **2. ProcessingConfiguration**
```vb
Public Class ProcessingConfiguration
    ''' <summary>Enable/disable DSP processing</summary>
    Public Property EnableDSP As Boolean = False
    
    ''' <summary>DSP processing order</summary>
    Public Property ProcessingChain As List(Of ProcessorType)
    
    ''' <summary>Gain stage settings</summary>
    Public Property InputGain As Single = 1.0F
    Public Property OutputGain As Single = 1.0F
    
    Public Enum ProcessorType
        None            ' Bypass all
        Gain            ' Just gain stage
        EQ              ' Equalizer (future)
        Compressor      ' Dynamics (future)
        Reverb          ' Effects (future)
    End Enum
End Class
```

### **3. MonitoringConfiguration**
```vb
Public Class MonitoringConfiguration
    ''' <summary>Where to tap for input FFT</summary>
    Public Property InputFFTTap As TapPoint
    
    ''' <summary>Where to tap for output FFT</summary>
    Public Property OutputFFTTap As TapPoint
    
    ''' <summary>Where to tap for level meters</summary>
    Public Property LevelMeterTap As TapPoint
    
    ''' <summary>Enable/disable monitoring</summary>
    Public Property EnableInputFFT As Boolean = True
    Public Property EnableOutputFFT As Boolean = True
    Public Property EnableLevelMeter As Boolean = True
    
    ''' <summary>Monitoring update rates (ms)</summary>
    Public Property FFTUpdateInterval As Integer = 50    ' 20 FPS
    Public Property MeterUpdateInterval As Integer = 20  ' 50 FPS
    
    Public Enum TapPoint
        None            ' Disabled
        PreDSP          ' Before DSP processing (raw input)
        PostGain        ' After gain stage
        PostDSP         ' After all DSP processing
        PreOutput       ' Before output (final stage)
    End Enum
End Class
```

### **4. DestinationConfiguration**
```vb
Public Class DestinationConfiguration
    ''' <summary>Enable recording to file</summary>
    Public Property EnableRecording As Boolean = False
    
    ''' <summary>Enable playback to speakers</summary>
    Public Property EnablePlayback As Boolean = False
    
    ''' <summary>Enable monitoring (headphones)</summary>
    Public Property EnableMonitoring As Boolean = False
    
    ''' <summary>Recording options</summary>
    Public Property RecordingOptions As RecordingOptions
End Class
```

---

## ?? Routing Examples

### **Example 1: Simple Recording (No DSP)**
```vb
' User clicks "Record" with DSP disabled
Dim config = New PipelineConfiguration With {
    .Source = AudioSourceType.Microphone,
    .Processing = New ProcessingConfiguration With {
        .EnableDSP = False  ' BYPASS!
    },
    .Monitoring = New MonitoringConfiguration With {
        .InputFFTTap = TapPoint.PreDSP,      ' Show raw input
        .OutputFFTTap = TapPoint.None,       ' No output FFT
        .EnableLevelMeter = True
    },
    .Destination = New DestinationConfiguration With {
        .EnableRecording = True,
        .EnablePlayback = False
    }
}

router.UpdateRouting(config)

' Result: Mic ? Direct ? Recording + Input FFT + Meter
```

### **Example 2: Recording with DSP**
```vb
' User enables DSP pipeline
Dim config = New PipelineConfiguration With {
    .Source = AudioSourceType.Microphone,
    .Processing = New ProcessingConfiguration With {
        .EnableDSP = True,  ' DSP ENABLED!
        .ProcessingChain = {ProcessorType.Gain}  ' Just gain for now
    },
    .Monitoring = New MonitoringConfiguration With {
        .InputFFTTap = TapPoint.PreDSP,      ' Raw input
        .OutputFFTTap = TapPoint.PostDSP,    ' Processed output
        .EnableLevelMeter = True
    },
    .Destination = New DestinationConfiguration With {
        .EnableRecording = True
    }
}

router.UpdateRouting(config)

' Result: Mic ? DSP ? Gain ? Recording + Both FFTs + Meter
```

### **Example 3: File Playback with Monitoring**
```vb
' User plays back a file
Dim config = New PipelineConfiguration With {
    .Source = AudioSourceType.File,
    .Processing = New ProcessingConfiguration With {
        .EnableDSP = True,  ' Apply DSP to playback
        .InputGain = 0.8F,  ' Reduce volume
    },
    .Monitoring = New MonitoringConfiguration With {
        .InputFFTTap = TapPoint.PreDSP,      ' File content
        .OutputFFTTap = TapPoint.PostDSP,    ' After DSP
    },
    .Destination = New DestinationConfiguration With {
        .EnablePlayback = True,
        .EnableRecording = False
    }
}

router.UpdateRouting(config)

' Result: File ? DSP ? Gain ? Speakers + Both FFTs
```

---

## ?? UI Integration

### **Settings Panel Changes:**

**Tab: "Audio Pipeline"** (new tab)
```
?? Audio Pipeline Configuration ??????????????????
?                                                 ?
? ?? Input Source:                                ?
?    ? Microphone  ? Line In  ? File             ?
?                                                 ?
? ?? DSP Processing:                              ?
?    ? Enable DSP Pipeline                       ?
?    ? Gain Stage (Input: [====|----] 80%)       ?
?    ? Equalizer (coming soon)                   ?
?                                                 ?
? ?? Monitoring:                                  ?
?    Input FFT:  [PreDSP ?]  ? Enable            ?
?    Output FFT: [PostDSP?]  ? Enable            ?
?    Level Meter: ? Enable   Update: [20ms ?]    ?
?                                                 ?
? ?? Destinations:                                ?
?    ? Record to File                            ?
?    ? Monitor (Headphones)                      ?
?                                                 ?
? [Preview Routing] [Apply] [Revert]             ?
???????????????????????????????????????????????????
```

**Quick Access (Main Toolbar):**
```
?? Quick Routing ??????????????????????????????????
? Preset: [Simple Record ?]                       ?
?         - Simple Record (No DSP)                ?
?         - Pro Record (With DSP)                 ?
?         - Playback Only                         ?
?         - Custom...                             ?
?                                                 ?
? Quick Toggles:                                  ?
? [DSP] [FFT In] [FFT Out] [Meter]               ?
???????????????????????????????????????????????????
```

---

## ?? Implementation Plan

### **Phase 1: Create Router Foundation (2 hours)**

**Step 1.1: Create File Structure**
```
DSP_Processor\
?? Audio\
   ?? Routing\
      ?? AudioPipelineRouter.vb        (Main router)
      ?? PipelineConfiguration.vb      (Config classes)
      ?? RoutingMap.vb                 (Routing visualization)
      ?? README.md                     (Architecture docs)
```

**Step 1.2: Implement Configuration Classes**
- SourceConfiguration
- ProcessingConfiguration
- MonitoringConfiguration
- DestinationConfiguration
- PipelineConfiguration (composite)

**Step 1.3: Implement Router Core**
- Basic routing logic
- Configuration management
- Event raising
- Routing map generation

**Step 1.4: Add Unit Tests (Optional)**
- Test configuration changes
- Test routing logic
- Test event firing

---

### **Phase 2: Integrate with Existing Code (3-4 hours)**

**Step 2.1: Modify MicInputSource**
```vb
' OLD: Decides what to enqueue itself
Private Sub OnDataAvailable(...)
    bufferQueue.Enqueue(...)  ' Recording
    fftQueue.Enqueue(...)     ' FFT
End Sub

' NEW: Ask router what to do
Private Sub OnDataAvailable(...)
    router.RouteAudioBuffer(e.Buffer, AudioSource.Microphone)
End Sub
```

**Step 2.2: Modify RecordingManager**
```vb
' OLD: Complex timer with branches
Private Sub ProcessingTimer_Tick(...)
    If recorder IsNot Nothing Then
        ' Drain recording
    ElseIf mic IsNot Nothing Then
        ' Drain armed
    End If
End Sub

' NEW: Subscribe to router events
Private Sub OnBufferForRecording(sender As Object, e As AudioBufferEventArgs)
    recorder.Process(e.Buffer)
End Sub

Private Sub OnBufferForMonitoring(sender As Object, e As AudioBufferEventArgs)
    ' Update meters, FFT, etc.
End Sub
```

**Step 2.3: Modify MainForm**
```vb
' OLD: Handles BufferAvailable event
Private Sub OnRecordingBufferAvailable(...)
    ' Do metering and FFT
End Sub

' NEW: Subscribe to router events
Private Sub OnMonitoringBufferAvailable(sender As Object, e As AudioBufferEventArgs)
    ' Router handles throttling, we just display
    UpdateMeters(e.Buffer)
    UpdateFFT(e.Buffer)
End Sub
```

**Step 2.4: Add UI Controls**
- New AudioPipelinePanel.vb
- Quick toggle buttons
- Preset dropdown
- Apply/Revert buttons

---

### **Phase 3: Test & Validate (1-2 hours)**

**Step 3.1: Test Configurations**
- Simple record (no DSP)
- DSP record
- File playback
- All combinations

**Step 3.2: Verify No Regressions**
- WaveIn Mic
- WASAPI Mic
- Stereo Mix
- Driver switching

**Step 3.3: Performance Check**
- Buffer overflow still fixed?
- Clean recordings?
- Responsive UI?

---

### **Phase 4: Future Enhancements (Future)**

**Step 4.1: Add More Processing**
- EQ filters
- Compressor
- Effects chain

**Step 4.2: Add More Sources**
- Line input
- Network streaming
- Multiple inputs

**Step 4.3: Add Routing Visualization**
- Show active paths on UI
- Debug routing issues
- Visual feedback

---

## ?? Router Internal Architecture

### **How Routing Works:**

```vb
Public Sub RouteAudioBuffer(buffer As Byte(), source As AudioSource)
    ' 1. Determine processing path
    Dim processedBuffer As Byte()
    If ProcessingConfig.EnableDSP Then
        processedBuffer = ApplyDSP(buffer)
    Else
        processedBuffer = buffer  ' Bypass
    End If
    
    ' 2. Tap for monitoring (if enabled)
    If MonitoringConfig.EnableInputFFT Then
        Dim tapBuffer = GetTapBuffer(buffer, MonitoringConfig.InputFFTTap)
        RaiseEvent BufferForMonitoring(Me, New AudioBufferEventArgs With {
            .Buffer = tapBuffer,
            .TapPoint = MonitoringConfig.InputFFTTap,
            .Purpose = MonitoringPurpose.InputFFT
        })
    End If
    
    If MonitoringConfig.EnableOutputFFT Then
        Dim tapBuffer = GetTapBuffer(processedBuffer, MonitoringConfig.OutputFFTTap)
        RaiseEvent BufferForMonitoring(Me, New AudioBufferEventArgs With {
            .Buffer = tapBuffer,
            .TapPoint = MonitoringConfig.OutputFFTTap,
            .Purpose = MonitoringPurpose.OutputFFT
        })
    End If
    
    ' 3. Route to destinations
    If DestinationConfig.EnableRecording Then
        RaiseEvent BufferForRecording(Me, New AudioBufferEventArgs With {
            .Buffer = processedBuffer
        })
    End If
    
    If DestinationConfig.EnablePlayback Then
        RaiseEvent BufferForPlayback(Me, New AudioBufferEventArgs With {
            .Buffer = processedBuffer
        })
    End If
End Sub

Private Function ApplyDSP(buffer As Byte()) As Byte()
    Dim result = buffer
    
    ' Apply processing chain in order
    For Each processor In ProcessingConfig.ProcessingChain
        Select Case processor
            Case ProcessorType.Gain
                result = ApplyGain(result, ProcessingConfig.InputGain)
            ' Future: Add more processors
        End Select
    Next
    
    Return result
End Function
```

---

## ?? Benefits of This Approach

### **1. Centralized Control**
? All routing logic in ONE place  
? Easy to understand flow  
? Single source of truth  

### **2. Flexibility**
? UI can change routing easily  
? Add new paths without touching existing code  
? Future-proof architecture  

### **3. Testability**
? Can unit test routing logic  
? Can simulate configurations  
? Easy to debug  

### **4. Performance**
? Router controls throttling (no event spam)  
? Can optimize buffer copies  
? Clear performance bottlenecks  

### **5. Maintainability**
? New developers understand flow quickly  
? Configuration changes don't break code  
? Clear separation of concerns  

---

## ?? Configuration File Support

### **Save/Load Routing Presets:**

```xml
<!-- UserRoutingPresets.xml -->
<RoutingPresets>
  <Preset Name="Simple Record">
    <Source>Microphone</Source>
    <DSP Enabled="false" />
    <Monitoring>
      <InputFFT Enabled="true" Tap="PreDSP" />
      <OutputFFT Enabled="false" />
      <LevelMeter Enabled="true" UpdateMs="20" />
    </Monitoring>
    <Destinations>
      <Recording Enabled="true" />
      <Playback Enabled="false" />
    </Destinations>
  </Preset>
  
  <Preset Name="Pro Record">
    <Source>Microphone</Source>
    <DSP Enabled="true">
      <Gain Input="1.0" Output="1.0" />
    </DSP>
    <Monitoring>
      <InputFFT Enabled="true" Tap="PreDSP" />
      <OutputFFT Enabled="true" Tap="PostDSP" />
      <LevelMeter Enabled="true" UpdateMs="20" />
    </Monitoring>
    <Destinations>
      <Recording Enabled="true" />
    </Destinations>
  </Preset>
</RoutingPresets>
```

---

## ?? Migration Strategy

### **How to Integrate Without Breaking:**

**Week 1: Foundation**
1. Create AudioPipelineRouter files
2. Implement configuration classes
3. Add basic routing logic
4. NO integration yet - parallel development

**Week 2: Integration (Careful!)**
1. Add router instance to MainForm
2. Wire up events (but keep old code)
3. Test both paths work
4. Switch one component at a time

**Week 3: Migration**
1. Remove old routing from MicInputSource
2. Remove old routing from RecordingManager
3. Clean up MainForm event handlers
4. Full testing

**Week 4: Polish**
1. Add UI controls
2. Add presets
3. Add visualization
4. Documentation

---

## ? Success Criteria

**Phase 1 Complete When:**
- ? Router classes compile
- ? Configuration changes work
- ? Events fire correctly
- ? Unit tests pass

**Phase 2 Complete When:**
- ? Router integrated with existing code
- ? Old paths removed
- ? All tests pass
- ? No regressions

**Phase 3 Complete When:**
- ? UI controls work
- ? Presets load/save
- ? Performance acceptable
- ? Buffer overflow still fixed

**Phase 4 Complete When:**
- ? User can easily change routing from UI
- ? System is flexible for future additions
- ? Documentation complete
- ? User is happy! ??

---

## ?? UI Architecture Requirements

### **User Control Design Principle:**
> **User Requirement:** "all controls should be not hard coded so I can manipulate them in a user control"

**Pattern:** Follow `UI\TabPanels\AudioSettingsPanel.vb` approach

### **Control Structure:**
```
UI\
?? TabPanels\                 (Settings panels)
?  ?? AudioSettingsPanel.vb   (Existing - device settings)
?  ?? AudioPipelinePanel.vb   (NEW - routing configuration)
?  ?? MonitoringPanel.vb      (NEW - FFT/meter settings)
?
?? Controls\                   (Placeable display controls)
   ?? WaveformDisplay.vb      (Make placeable/sizable)
   ?? SpectrumDisplay.vb      (Make placeable/sizable)
   ?? AudioLevelMeter.vb      (Already UserControl?)
   ?? TransportControl.vb     (Already UserControl)
```

### **Hard-Code Exceptions:**
**Only hard-code when:**
- ?? **Performance critical** - Painting algorithms, DSP processing, audio callbacks
- ?? **Unrealistic to externalize** - Core rendering logic

**Everything else:**
- ? **UserControls** - All UI elements
- ? **Properties** - All configuration
- ? **Events** - All communication
- ? **JSON** - All settings

---

## ?? Configuration Storage Architecture

### **JSON Auto-Save Requirements:**
> **User Requirement:** "every time a setting is changed the json files should be updated with that change"

**Implementation:**
```vb
Public Sub UpdateRouting(newConfig As PipelineConfiguration)
    ' Validate
    ValidateConfiguration(newConfig)
    
    ' Apply
    _currentConfiguration = newConfig
    
    ' AUTO-SAVE (throttled to 500ms)
    TriggerAutoSave()
    
    ' Notify
    RaiseEvent RoutingChanged(...)
End Sub
```

### **File Structure:**
```
Settings\
?? AudioPipeline.json          (Active config - auto-saved)
?? AudioPipelineTemplates.json (User templates)
?? AudioPipelineDefaults.json  (Factory defaults)
```

### **Features:**
- ? **Load last state** on startup
- ? **Auto-save** on every change (throttled)
- ? **Templates** for quick switching
- ? **Atomic writes** (never corrupt)
- ? **Undo/Redo** (optional history)

---

## ?? First Step (RIGHT NOW)

### **Create Foundation (2 hours, THIS SESSION):**

1. **Create folder structure**
2. **Create AudioPipelineRouter.vb with basic structure**
3. **Create PipelineConfiguration.vb with all config classes**
4. **Create PipelineConfigurationManager.vb for JSON handling**
5. **Add simple test in MainForm to verify it works**
6. **Build and test - should compile, do nothing yet**

**After this session:**
- Foundation exists
- JSON save/load works
- Can build on it incrementally
- Won't break existing functionality
- Clear path forward

---

**Ready to start?** Let's create the foundation! ??

**Estimated Time:**
- Foundation + JSON: 2-3 hours
- UI Controls: 2-3 hours
- Full integration: 3-4 hours
- **Total: 8-10 hours** for complete system
- Worth every minute for clean, maintainable architecture!
