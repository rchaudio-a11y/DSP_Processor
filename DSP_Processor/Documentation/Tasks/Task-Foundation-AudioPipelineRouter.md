# Task: Implement AudioPipelineRouter Foundation

**Date:** January 15, 2026  
**Estimated Time:** 2 hours  
**Status:** ?? **READY TO START**  
**Priority:** ?? **FOUNDATIONAL**

---

## ?? Goal

Create the foundational `AudioPipelineRouter` system without breaking existing functionality.

**What We're Building:**
- Centralized routing controller
- Configuration classes
- Basic routing logic
- Event infrastructure

**What We're NOT Doing (Yet):**
- Integrating with existing code
- Removing old code
- Adding UI controls
- Full DSP implementation

---

## ?? Task Checklist

### **Step 1: Create Folder Structure (5 min)**

- [ ] Create `DSP_Processor\Audio\Routing\` folder
- [ ] Create `README.md` in that folder
- [ ] Add folder to project in Visual Studio

---

### **Step 2: Create Configuration Classes (30 min)**

**File:** `DSP_Processor\Audio\Routing\PipelineConfiguration.vb`

- [ ] Create `SourceConfiguration` class
  - [ ] `ActiveSource` property (enum)
  - [ ] `SourceSettings` property
  - [ ] `AudioSourceType` enum (Microphone, File, LineIn, NetworkStream)

- [ ] Create `ProcessingConfiguration` class
  - [ ] `EnableDSP` property (Boolean)
  - [ ] `ProcessingChain` property (List of ProcessorType)
  - [ ] `InputGain` property (Single)
  - [ ] `OutputGain` property (Single)
  - [ ] `ProcessorType` enum (None, Gain, EQ, Compressor, Reverb)

- [ ] Create `MonitoringConfiguration` class
  - [ ] `InputFFTTap` property (TapPoint enum)
  - [ ] `OutputFFTTap` property (TapPoint enum)
  - [ ] `LevelMeterTap` property (TapPoint enum)
  - [ ] `EnableInputFFT`, `EnableOutputFFT`, `EnableLevelMeter` properties
  - [ ] `FFTUpdateInterval`, `MeterUpdateInterval` properties
  - [ ] `TapPoint` enum (None, PreDSP, PostGain, PostDSP, PreOutput)

- [ ] Create `DestinationConfiguration` class
  - [ ] `EnableRecording` property
  - [ ] `EnablePlayback` property
  - [ ] `EnableMonitoring` property
  - [ ] `RecordingOptions` property

- [ ] Create `PipelineConfiguration` class (composite)
  - [ ] `Source` property (SourceConfiguration)
  - [ ] `Processing` property (ProcessingConfiguration)
  - [ ] `Monitoring` property (MonitoringConfiguration)
  - [ ] `Destination` property (DestinationConfiguration)

- [ ] Build and verify no errors

---

### **Step 3: Create Event Args Classes (15 min)**

**File:** `DSP_Processor\Audio\Routing\RoutingEventArgs.vb`

- [ ] Create `RoutingChangedEventArgs` class
  - [ ] `OldConfiguration` property
  - [ ] `NewConfiguration` property

- [ ] Create `AudioBufferEventArgs` class
  - [ ] `Buffer` property (Byte array)
  - [ ] `BitsPerSample` property
  - [ ] `Channels` property
  - [ ] `SampleRate` property
  - [ ] `TapPoint` property (optional)
  - [ ] `Purpose` property (optional - enum: InputFFT, OutputFFT, Recording, etc.)

- [ ] Create `RoutingMap` class (for visualization)
  - [ ] `ActivePaths` property (List of PathInfo)
  - [ ] `ToString()` override for debugging

- [ ] Build and verify no errors

---

### **Step 4: Create AudioPipelineRouter Core (45 min)**

**File:** `DSP_Processor\Audio\Routing\AudioPipelineRouter.vb`

- [ ] Create class structure
  ```vb
  Public Class AudioPipelineRouter
      ' Fields
      ' Properties
      ' Constructor
      ' Methods
      ' Events
  End Class
  ```

- [ ] Add properties
  - [ ] `CurrentConfiguration` property (read-only)
  - [ ] `IsInitialized` property (read-only)

- [ ] Add events
  - [ ] `RoutingChanged` event
  - [ ] `BufferForRecording` event
  - [ ] `BufferForMonitoring` event
  - [ ] `BufferForPlayback` event

- [ ] Implement `Initialize()` method
  - [ ] Set default configuration
  - [ ] Mark as initialized

- [ ] Implement `UpdateRouting()` method
  - [ ] Validate new configuration
  - [ ] Store old configuration
  - [ ] Apply new configuration
  - [ ] Raise RoutingChanged event

- [ ] Implement `RouteAudioBuffer()` method (stub for now)
  - [ ] Check if initialized
  - [ ] Log routing (for debugging)
  - [ ] Raise appropriate events based on configuration
  - [ ] No actual DSP yet - just event routing

- [ ] Implement `GetActiveRoutingMap()` method
  - [ ] Build RoutingMap from current configuration
  - [ ] Return for debugging/visualization

- [ ] Add XML documentation comments to all public members

- [ ] Build and verify no errors

---

### **Step 5: Create Simple Test (15 min)**

**File:** Add to `MainForm.vb` (temporary test code)

- [ ] Add router instance to MainForm
  ```vb
  Private testRouter As AudioPipelineRouter
  ```

- [ ] Initialize in Form_Load (commented out, not active)
  ```vb
  ' TEST: Initialize router (not active yet)
  ' testRouter = New AudioPipelineRouter()
  ' testRouter.Initialize()
  ```

- [ ] Add test button handler (temporary)
  ```vb
  Private Sub TestRouterButton_Click(...)
      ' Create test configuration
      Dim config = New PipelineConfiguration()
      ' ... set properties
      testRouter.UpdateRouting(config)
      MessageBox.Show(testRouter.GetActiveRoutingMap().ToString())
  End Sub
  ```

- [ ] Build and verify compiles
- [ ] Verify project still runs normally (router not active)

---

### **Step 6: Documentation (10 min)**

**File:** `DSP_Processor\Audio\Routing\README.md`

- [ ] Add overview
- [ ] Add class descriptions
- [ ] Add usage examples
- [ ] Add integration plan
- [ ] Add future enhancements

---

## ?? Verification Steps

After each step:
1. ? Build succeeds
2. ? No warnings
3. ? Project runs normally
4. ? Router exists but doesn't affect anything yet

Final verification:
1. ? All classes created
2. ? All properties implemented
3. ? All events defined
4. ? Basic routing logic works (in test)
5. ? Documentation complete
6. ? **Existing functionality unchanged**

---

## ?? File Structure After Completion

```
DSP_Processor\
?? Audio\
   ?? Routing\
      ?? README.md
      ?? AudioPipelineRouter.vb
      ?? PipelineConfiguration.vb
      ?? RoutingEventArgs.vb
      ?? (Future: RoutingPresets.vb, RoutingVisualizer.vb)
```

---

## ?? Success Criteria

- ? Foundation classes exist
- ? Compiles without errors
- ? Basic routing logic implemented
- ? Events defined and can be subscribed to
- ? Configuration can be changed
- ? **Existing code still works (router not integrated)**
- ? Clear path for integration

---

## ?? Next Steps (After This Session)

1. **Test router independently** (create unit tests)
2. **Plan integration strategy** (which component first?)
3. **Create integration tasks** (one component at a time)
4. **Add UI controls** (AudioPipelinePanel)
5. **Remove old routing code** (careful migration)

---

## ?? Time Breakdown

- Step 1 (Folder): 5 minutes
- Step 2 (Config): 30 minutes
- Step 3 (Events): 15 minutes
- Step 4 (Router): 45 minutes
- Step 5 (Test): 15 minutes
- Step 6 (Docs): 10 minutes
- **Total: 2 hours**

---

## ?? Implementation Notes

### **Keep It Simple:**
- Don't overcomplicate
- Focus on structure, not full implementation
- Router should be usable but minimal

### **Don't Break Anything:**
- Router is parallel to existing code
- Not integrated yet
- Can be tested independently
- Safe to develop

### **Think Ahead:**
- Design for extensibility
- But don't build what we don't need yet
- Clean interfaces
- Good documentation

---

**Ready to implement?** Let's start with Step 1! ??
