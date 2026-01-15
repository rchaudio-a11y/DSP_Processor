# Task 1.1: Input Abstraction Layer

**Priority:** ?? **HIGH**  
**Status:** ?? **Partially Complete** (Buffer architecture done, WASAPI integration pending)  
**Estimated Time:** 5 days ? ~2 days remaining  
**Dependencies:** Phase 0 (Complete)  
**Completion Date (Partial):** January 14, 2026

---

## ? Completed Work (2026-01-14)

### **Buffer Architecture Optimization**
- ? **Dual Freewheeling Buffer System** implemented in `MicInputSource.vb`
  - Separate `bufferQueue` (recording - critical path)
  - Separate `fftQueue` (visualization - freewheeling)
  - Lock-free concurrent queues
  - Max depth limiting on FFT queue (5 frames)
- ? **Async FFT Processing** in `MainForm.vb`
  - Background thread for FFT calculation
  - Fire-and-forget pattern with `Task.Run()`
  - UI updates via `BeginInvoke()`
  - Processing flag prevents queue buildup
- ? **4x Queue Drain Rate** in `RecordingManager.vb`
  - Reads 16KB per 20ms tick
  - Prevents buffer overflow
  - Separate read paths for recording vs FFT
- ? **Results:**
  - Zero audio clicks/pops
  - Smooth 60 FPS spectrum
  - Queue depth stays 0-5 buffers
  - No blocking on audio thread

**See:** `Documentation/Issues/Bug-Report-2026-01-14-Recording-Clicks-Pops.md`

---

## ?? Remaining Objectives

1. ~~Create buffer architecture~~ ? DONE
2. Integrate WASAPI driver (Task 1.2)
3. Define `DriverType` enum ? EXISTS
4. Implement `DeviceInfo` class ? EXISTS
5. Create `AudioInputManager` class ? EXISTS (needs WASAPI integration)
6. Support driver switching without application restart
7. Implement hot-plug device detection

---

## ?? Architecture

```
IAudioEngine (Interface)
    ??? WaveInEngine (Existing - wrap MicInputSource)
    ??? WasapiEngine (Task 1.2)
    ??? AsioEngine (Task 1.3)
    
AudioInputManager
    ??? DetectAvailableDrivers()
    ??? SelectDriver(DriverType)
    ??? GetDeviceList(DriverType)
    ??? CreateInputSource(device, driver)
    ??? Events: DeviceAdded, DeviceRemoved, DriverChanged
```

---

## ?? Implementation Checklist

### **Step 1: Create DriverType Enum**
- [ ] Create `AudioIO/DriverType.vb`
- [ ] Define enum values:
  ```vb
  Public Enum DriverType
      WaveIn = 0      ' Legacy WDM drivers (default)
      WASAPI = 1      ' Windows Audio Session API
      ASIO = 2        ' Audio Stream Input/Output
      DirectSound = 3 ' Legacy DirectSound (optional)
  End Enum
  ```

### **Step 2: Create DeviceInfo Class**
- [ ] Create `AudioIO/DeviceInfo.vb`
- [ ] Properties:
  - [ ] `Name As String` - Device friendly name
  - [ ] `Id As String` - Unique device identifier
  - [ ] `DriverType As DriverType` - Associated driver
  - [ ] `MaxChannels As Integer` - Maximum input channels
  - [ ] `SupportedSampleRates As Integer()` - Available sample rates
  - [ ] `SupportedBitDepths As Integer()` - Available bit depths
  - [ ] `IsDefault As Boolean` - Is default input device
  - [ ] `IsAvailable As Boolean` - Currently connected/available

### **Step 3: Enhance IAudioEngine Interface**
- [ ] Update `AudioIO/IAudioEngine.vb`
- [ ] Add properties:
  ```vb
  ReadOnly Property DriverType As DriverType
  ReadOnly Property LatencyMS As Integer
  ReadOnly Property SupportedDevices As IEnumerable(Of DeviceInfo)
  ```
- [ ] Add methods:
  ```vb
  Function SupportsExclusiveMode() As Boolean
  Function GetOptimalBufferSize() As Integer
  ```

### **Step 4: Create AudioInputManager Class**
- [ ] Create `AudioIO/AudioInputManager.vb`
- [ ] Implement singleton pattern (or use dependency injection)
- [ ] Properties:
  - [ ] `CurrentDriver As DriverType`
  - [ ] `AvailableDrivers As DriverType()`
  - [ ] `Devices As List(Of DeviceInfo)`
- [ ] Methods:
  - [ ] `DetectAvailableDrivers() As DriverType()`
  - [ ] `GetDevices(driver As DriverType) As List(Of DeviceInfo)`
  - [ ] `GetDefaultDevice(driver As DriverType) As DeviceInfo`
  - [ ] `CreateInputSource(device As DeviceInfo) As IInputSource`
  - [ ] `SetDriver(driver As DriverType)`
  - [ ] `RefreshDevices()`
- [ ] Events:
  - [ ] `DeviceAdded(device As DeviceInfo)`
  - [ ] `DeviceRemoved(device As DeviceInfo)`
  - [ ] `DriverChanged(oldDriver As DriverType, newDriver As DriverType)`

### **Step 5: Implement Driver Detection**
- [ ] Create `DetectAvailableDrivers()` method:
  ```vb
  Public Function DetectAvailableDrivers() As DriverType()
      Dim drivers As New List(Of DriverType)
      
      ' WaveIn always available on Windows
      drivers.Add(DriverType.WaveIn)
      
      ' Check for WASAPI (Windows Vista+)
      If Environment.OSVersion.Version.Major >= 6 Then
          drivers.Add(DriverType.WASAPI)
      End If
      
      ' Check for ASIO drivers
      If IsASIOAvailable() Then
          drivers.Add(DriverType.ASIO)
      End If
      
      Return drivers.ToArray()
  End Function
  ```

### **Step 6: Implement Device Enumeration**
- [ ] Create device enumeration for each driver type
- [ ] WaveIn devices: Use `WaveInEvent.DeviceCount`
- [ ] WASAPI devices: Use `MMDeviceEnumerator` (defer to Task 1.2)
- [ ] ASIO devices: Query ASIO registry keys (defer to Task 1.3)

### **Step 7: Implement Hot-Plug Detection**
- [ ] Use `SystemEvents.HardwareInserted` / `HardwareRemoved` events
- [ ] Create background monitor thread
- [ ] Refresh device list when hardware changes
- [ ] Raise `DeviceAdded` / `DeviceRemoved` events

### **Step 8: Create WaveInEngine Wrapper**
- [ ] Create `AudioIO/WaveInEngine.vb`
- [ ] Wrap existing `MicInputSource.vb`
- [ ] Implement `IAudioEngine` interface
- [ ] Provide consistent API with future WASAPI/ASIO engines

### **Step 9: Update UI for Driver Selection**
- [ ] Add `ComboBox` for driver selection in MainForm
- [ ] Populate with available drivers
- [ ] Reload device list when driver changes
- [ ] Persist driver selection in settings

---

## ?? Testing Checklist

### **Functionality Tests:**
- [ ] Detect all available drivers correctly
- [ ] Enumerate devices for each driver type
- [ ] Switch between drivers without restart
- [ ] Hot-plug detection works (plug/unplug USB mic)
- [ ] Default device selection correct

### **UI Tests:**
- [ ] Driver combo box populates correctly
- [ ] Device list updates when driver changes
- [ ] Selection persists across app restarts

### **Edge Case Tests:**
- [ ] No audio devices available
- [ ] Device disconnected during use
- [ ] Switch drivers during recording
- [ ] Invalid driver type handling

---

## ?? Expected Performance

| Metric | Target | Notes |
|--------|--------|-------|
| **Driver Detection** | <100ms | On startup |
| **Device Enumeration** | <200ms | Per driver |
| **Hot-Plug Response** | <500ms | From hardware event |
| **Driver Switch** | <1 second | Without recording |

---

## ?? Implementation Tips

### **Driver Detection Order:**
1. Always include WaveIn (most compatible)
2. Prefer WASAPI (lower latency, better quality)
3. ASIO only if drivers installed

### **Device ID Consistency:**
- Use stable device IDs that persist across reboots
- For WaveIn: Use device index (may change)
- For WASAPI: Use device endpoint ID (stable)
- For ASIO: Use driver name (stable)

### **Error Handling:**
- Gracefully degrade if driver not available
- Fall back to WaveIn if preferred driver fails
- Provide clear error messages to user

---

## ?? Reference Materials

### **NAudio Classes:**
- `WaveInEvent` - WaveIn wrapper
- `MMDeviceEnumerator` - WASAPI device enumeration
- `AsioOut` - ASIO wrapper

### **Windows APIs:**
- `CoreAudio` - WASAPI low-level API
- Registry - ASIO driver detection

---

## ? Definition of Done

- [ ] All driver types enum defined
- [ ] DeviceInfo class complete
- [ ] IAudioEngine interface updated
- [ ] AudioInputManager implemented
- [ ] Driver detection working
- [ ] Device enumeration working
- [ ] Hot-plug detection working
- [ ] WaveInEngine wrapper complete
- [ ] UI updated for driver selection
- [ ] All tests passing
- [ ] Documentation complete

---

## ?? Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **Driver Detection Success** | 100% | Auto-detect all installed drivers |
| **Device Enumeration Accuracy** | 100% | Match Windows Device Manager |
| **Hot-Plug Detection** | <500ms | Measure event latency |
| **Driver Switch** | No app restart | Functional test |

---

**Task Created:** January 14, 2026  
**Target Start:** After Phase 2.2 completion  
**Target Completion:** 5 days  
**Dependencies:** Phase 0 complete  
**Blocks:** Task 1.2 (WASAPI), Task 1.3 (ASIO)
