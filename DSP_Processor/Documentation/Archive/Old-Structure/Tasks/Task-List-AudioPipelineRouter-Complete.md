# Task List: AudioPipelineRouter Foundation & Integration

**Date:** January 15, 2026  
**Status:** ? **PHASE 1 COMPLETE**  
**Session Goal:** Complete Phase 1 (Foundation) - 2-3 hours  
**Actual Time:** ~2 hours  
**Priority:** ?? **FOUNDATIONAL**

---

## ?? Core Architecture Principles

### **1. Configuration Storage** ? COMPLETE
- ? **JSON format** - Using Newtonsoft.Json (like SettingsManager)
- ? **Auto-save on change** - With 500ms throttling/debounce
- ? **Load last state** - On application startup
- ? **Template/Preset support** - Built-in + user-defined
- ? **Atomic file writes** - Never corrupt settings
- ?? **Undo/Redo** - Configuration history (Phase 4 - Future)

### **2. UI Control Architecture**
> **User Requirement:** "all controls should be not hard coded so I can manipulate them in a user control"

**Pattern:** Follow `AudioSettingsPanel.vb` approach
```
DSP_Processor\
?? UI\
   ?? TabPanels\
      ?? AudioSettingsPanel.vb          (Existing - device settings)
      ?? AudioPipelinePanel.vb          (NEW - routing config)
      ?? MonitoringPanel.vb              (NEW - FFT/meters config)
      ?? AdvancedRoutingPanel.vb        (FUTURE - presets/templates)
```

**Placeable Display Controls:**
```
DSP_Processor\
?? UI\
   ?? Controls\
      ?? WaveformDisplay.vb             (Existing - make placeable)
      ?? SpectrumDisplay.vb             (Existing - make placeable)
      ?? AudioLevelMeter.vb             (Existing - already user control?)
      ?? TransportControl.vb            (Existing - already user control)
```

**Hard-Code Exceptions (Performance/Unrealistic):**
- ? **Painting logic** - Waveform/spectrum rendering algorithms
- ? **Audio buffer processing** - DSP calculations
- ? **Control placement** - User controls, sizable, movable
- ? **Configuration** - All settings in JSON

---

## ?? Phase 1: Foundation ? **COMPLETE** (2 hours actual)

### **Task 1: Create Router Foundation (1 hour)** ?

#### **Task 1.1: Folder Structure (5 min)** ?
- [x] Create `DSP_Processor\Audio\Routing\` folder
- [x] Add folder to project in Visual Studio
- [x] Create `README.md` in Routing folder

#### **Task 1.2: Configuration Classes (30 min)** ?

**File:** `Audio\Routing\PipelineConfiguration.vb`

- [x] Create `SourceConfiguration` class
- [x] Create `ProcessingConfiguration` class
- [x] Create `MonitoringConfiguration` class
- [x] Create `DestinationConfiguration` class
- [x] Create `PipelineConfiguration` class (composite)
- [x] Add enums: `AudioSourceType`, `ProcessorType`, `TapPoint`
- [x] Build and verify no errors

#### **Task 1.3: Event Args Classes (15 min)** ?

**File:** `Audio\Routing\RoutingEventArgs.vb`

- [x] Create `RoutingChangedEventArgs`
- [x] Create `AudioBufferRoutingEventArgs`
- [x] Create `RoutingMap` class
- [x] Build and verify no errors

#### **Task 1.4: Router Core (15 min)** ?

**File:** `Audio\Routing\AudioPipelineRouter.vb`

- [x] Create class skeleton
- [x] Implement `Initialize()` - Loads from JSON
- [x] Implement `UpdateRouting()` - Auto-saves to JSON
- [x] Implement `GetActiveRoutingMap()` - Visualization
- [x] Stub `RouteAudioBuffer()` - Logging only
- [x] Add XML documentation comments
- [x] Build and verify no errors

---

### **Task 2: JSON Configuration Manager (45 min)** ?

#### **Task 2.1: Configuration File Manager (30 min)** ?

**File:** `Audio\Routing\PipelineConfigurationManager.vb`

- [x] Create class structure (Singleton pattern)
- [x] Implement `LoadConfiguration()` method
- [x] Implement `SaveConfiguration()` method with 500ms throttling
- [x] Implement atomic write (temp file ? rename)
- [x] Implement `GetDefaultConfiguration()` method
- [x] Build and verify no errors

#### **Task 2.2: Template Management (15 min)** ?

**File:** `Audio\Routing\PipelineTemplateManager.vb`

- [x] Create class structure (Singleton pattern)
- [x] Implement `LoadTemplates()` method
- [x] Implement `SaveTemplate()` method
- [x] Implement `DeleteTemplate()` method
- [x] Implement `GetBuiltInPresets()` method (Simple Record, Pro Record, Playback Only)
- [x] Build and verify no errors

---

### **Task 3: Wire Up to AudioPipelineRouter (15 min)** ?

- [x] Add `PipelineConfigurationManager` to `AudioPipelineRouter`
- [x] `Initialize()` calls `ConfigManager.LoadConfiguration()`
- [x] `UpdateRouting()` calls `ConfigManager.SaveConfiguration()` (throttled)
- [x] Add property: `AvailableTemplates` (returns templates)
- [x] Add methods: `ApplyTemplate()`, `SaveCurrentAsTemplate()`, `DeleteTemplate()`
- [x] Build and verify no errors

---

### **Task 4: Create Test in MainForm (10 min)** ?

**TESTED SUCCESSFULLY!**

- [x] Add field: `Private testRouter As AudioPipelineRouter` (commented out)
- [x] Test code added to `Form_Load()` (commented out)
- [x] Build and run - Compiled successfully
- [x] Test executed - All features working!
  - ? Router initialized
  - ? Templates loaded (3 built-in)
  - ? Applied "Simple Record" template
  - ? Routing map displayed
  - ? Saved "My Test Config" template
  - ? JSON files created
  - ? Auto-save triggered
- [x] Test code re-commented to keep router dormant

**Test Log:** `DSP_Processor_20260115_151347.log`

---

## ?? Phase 2: UI User Controls (NEXT SESSION - 2-3 hours)

### **Task 5: Create AudioPipelinePanel (1.5 hours)**

**File:** `UI\TabPanels\AudioPipelinePanel.vb`

**Pattern:** Copy structure from `AudioSettingsPanel.vb`

- [ ] Create UserControl in Designer
- [ ] Add UI elements (follow design from architecture doc):
  - [ ] ComboBox for presets/templates
  - [ ] CheckBox for "Enable DSP"
  - [ ] Sliders for Input/Output Gain
  - [ ] ComboBoxes for FFT tap points
  - [ ] CheckBoxes for enable FFT/meters
  - [ ] Buttons: Save Template, Delete Template

- [ ] Implement code-behind:
  - [ ] `LoadConfiguration(config As PipelineConfiguration)` method
  - [ ] `GetConfiguration() As PipelineConfiguration` method
  - [ ] Control event handlers ? Update router
  - [ ] Router event handlers ? Update controls

- [ ] Wire to AudioPipelineRouter:
  - [ ] Subscribe to `RoutingChanged` event
  - [ ] Call `UpdateRouting()` on control changes

- [ ] Add to MainForm TabControl (new tab: "Pipeline")

- [ ] Test: Change settings, verify JSON saved, restart app, verify loaded

---

### **Task 6: Make Display Controls Placeable (1 hour)**

**Goal:** Make WaveformDisplay and SpectrumDisplay true UserControls

#### **Task 6.1: WaveformDisplay (30 min)**

- [ ] Check if already UserControl (might be!)
- [ ] If not, convert:
  - [ ] Create `UI\Controls\WaveformDisplay.vb` (UserControl)
  - [ ] Move painting logic from MainForm
  - [ ] Add properties: `AudioData`, `DisplayRange`, `Colors`, etc.
  - [ ] Keep painting code (performance critical)

- [ ] Make configurable placement:
  - [ ] Dock property
  - [ ] Anchor property
  - [ ] Size property

- [ ] Test: Place on MainForm, verify resizes correctly

#### **Task 6.2: SpectrumDisplay (30 min)**

- [ ] Same as WaveformDisplay
- [ ] Ensure FFT data source is configurable (tap point)

---

### **Task 7: Migrate Existing Controls (30 min)**

**Goal:** Move hard-coded controls from MainForm to panels

- [ ] Identify controls currently on MainForm:
  - [ ] Device selection dropdowns ? AudioSettingsPanel? (might already be there)
  - [ ] Level meters ? MonitoringPanel? (new panel)
  - [ ] Transport controls ? Already UserControl?

- [ ] Create `MonitoringPanel.vb` if needed:
  - [ ] FFT display container
  - [ ] Level meter container
  - [ ] Update rate controls

- [ ] Test: Verify all controls work in panels

---

## ?? Phase 3: Router Integration (FUTURE SESSION - 3-4 hours)

### **Task 8: Integrate MicInputSource**
- [ ] Modify `OnDataAvailable()` to call `router.RouteAudioBuffer()`
- [ ] Remove direct enqueuing to bufferQueue/fftQueue
- [ ] Test: Verify router receives buffers

### **Task 9: Integrate RecordingManager**
- [ ] Subscribe to router events: `BufferForRecording`, `BufferForMonitoring`
- [ ] Remove old `ProcessingTimer_Tick()` logic
- [ ] Test: Verify recording still works

### **Task 10: Integrate MainForm**
- [ ] Subscribe to router `BufferForMonitoring` event
- [ ] Remove old `OnRecordingBufferAvailable()` handler
- [ ] Test: Verify FFT/meters still work

### **Task 11: Remove Old Code**
- [ ] Remove dual queue system from MicInputSource (if safe)
- [ ] Remove complex timer logic from RecordingManager
- [ ] Clean up MainForm event handlers
- [ ] Full regression testing

---

## ?? Phase 4: Advanced Features (FUTURE - 2-3 hours)

### **Task 12: Undo/Redo**
- [ ] Implement configuration history in ConfigurationManager
- [ ] Add UI buttons: Undo/Redo
- [ ] Test: Change settings, undo, verify restored

### **Task 13: Routing Visualization**
- [ ] Create `RoutingMapDisplay.vb` UserControl
- [ ] Show active paths graphically
- [ ] Place in AudioPipelinePanel or separate tab

### **Task 14: Quick Toggles**
- [ ] Add toolbar buttons: DSP, FFT In, FFT Out, Meter
- [ ] One-click toggle without opening settings
- [ ] Test: Toggle, verify instant update

---

## ? Completion Checklist

### **Phase 1 Complete When:**
- [ ] All router classes compile
- [ ] JSON save/load works
- [ ] Test in MainForm successful
- [ ] No errors, no crashes
- [ ] Configuration persists across restarts

### **Phase 2 Complete When:**
- [ ] AudioPipelinePanel exists and works
- [ ] Display controls are placeable UserControls
- [ ] All controls in panels (not hard-coded on MainForm)
- [ ] Configuration changes update JSON
- [ ] UI reflects loaded configuration

### **Phase 3 Complete When:**
- [ ] Router integrated with existing code
- [ ] Old routing code removed
- [ ] All tests pass
- [ ] No regressions (WaveIn/WASAPI both work)
- [ ] Buffer overflow still fixed

### **Phase 4 Complete When:**
- [ ] Undo/Redo works
- [ ] Visualization works (optional)
- [ ] Quick toggles work
- [ ] User is happy! ??

---

## ?? File Structure After Phase 2

```
DSP_Processor\
?? Audio\
?  ?? Routing\
?     ?? AudioPipelineRouter.vb
?     ?? PipelineConfiguration.vb
?     ?? PipelineConfigurationManager.vb
?     ?? PipelineTemplateManager.vb
?     ?? RoutingEventArgs.vb
?     ?? README.md
?
?? UI\
?  ?? TabPanels\
?  ?  ?? AudioSettingsPanel.vb        (Existing)
?  ?  ?? AudioPipelinePanel.vb        (NEW - Phase 2)
?  ?  ?? MonitoringPanel.vb           (NEW - Phase 2, if needed)
?  ?
?  ?? Controls\
?     ?? WaveformDisplay.vb           (Existing, make placeable)
?     ?? SpectrumDisplay.vb           (Existing, make placeable)
?     ?? AudioLevelMeter.vb           (Existing)
?     ?? TransportControl.vb          (Existing)
?
?? Settings\
   ?? AudioPipeline.json               (Auto-saved active config)
   ?? AudioPipelineTemplates.json      (User templates)
   ?? AudioPipelineDefaults.json       (Factory defaults)
```

---

## ?? Session Plan

### **This Session (2-3 hours):**
1. ? Complete Task 1 (Router Foundation)
2. ? Complete Task 2 (JSON Manager)
3. ? Complete Task 3 (Wire Up)
4. ? Complete Task 4 (Test)
5. ? Build, test, commit

**Deliverable:** Router foundation exists, JSON save/load works, doesn't break anything

---

### **Next Session (2-3 hours):**
1. ? Complete Task 5 (AudioPipelinePanel)
2. ? Complete Task 6 (Placeable Controls)
3. ? Complete Task 7 (Migrate Controls)
4. ? Build, test, commit

**Deliverable:** Complete UI for configuring routing, all controls in panels

---

### **Future Session (3-4 hours):**
1. ? Complete Task 8-11 (Integration)
2. ? Test everything
3. ? Remove old code
4. ? Build, test, commit

**Deliverable:** Router fully integrated, old code removed, everything works

---

## ?? Implementation Notes

### **UI Control Design Principles:**

**DO:**
- ? Make all controls UserControls (sizable, placeable)
- ? Follow AudioSettingsPanel.vb pattern
- ? Use properties for configuration (not hard-coded values)
- ? Raise events for changes (don't directly call other code)
- ? Support designer (can drag/drop onto form)

**DON'T:**
- ? Hard-code control layout on MainForm
- ? Hard-code settings/values in controls
- ? Put painting algorithms in UserControl properties (keep in painting methods)
- ? Tight coupling between controls

**EXCEPTIONS (Can Hard-Code):**
- ?? Painting/rendering algorithms (performance critical)
- ?? DSP buffer processing (performance critical)
- ?? Audio callback handlers (performance critical)

---

### **JSON Configuration Pattern:**

**Follow existing SettingsManager pattern:**
1. Check how `SettingsManager.vb` saves settings
2. Use same JSON library
3. Use same file path pattern (`Settings\` folder)
4. Use same error handling

**Auto-Save Pattern:**
```vb
Private autoSaveTimer As System.Timers.Timer

Public Sub UpdateConfiguration(config)
    _currentConfiguration = config
    
    ' Trigger auto-save (debounced)
    If autoSaveTimer Is Nothing Then
        autoSaveTimer = New System.Timers.Timer(500)
        AddHandler autoSaveTimer.Elapsed, AddressOf OnAutoSave
        autoSaveTimer.AutoReset = False
    End If
    autoSaveTimer.Stop()
    autoSaveTimer.Start()
End Sub
```

---

## ?? Ready to Start?

**First task:** Create `Audio\Routing\` folder and README

**Estimated time for Phase 1:** 2-3 hours

**Let's begin!** ??
