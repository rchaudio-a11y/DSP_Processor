# Task 1.2: WASAPI Implementation

**Priority:** ?? **HIGH**  
**Status:** ? Not Started  
**Estimated Time:** 7 days  
**Dependencies:** Task 1.1 (Input Abstraction Layer)

---

## ?? Task Overview

Implement Windows Audio Session API (WASAPI) support for professional-grade, low-latency audio input. WASAPI provides better quality and lower latency than legacy WaveIn drivers.

---

## ?? Objectives

1. Implement `WasapiEngine` class with IAudioEngine interface
2. Support Exclusive Mode (lowest latency, direct hardware access)
3. Support Shared Mode (compatible, system mixer)
4. Automatic mode selection and graceful fallback
5. Sample rate conversion when needed
6. Device capability detection

---

## ?? WASAPI Architecture

```
WasapiEngine : IAudioEngine
    ??? Mode: Exclusive | Shared
    ??? Device: MMDevice
    ??? AudioClient: IAudioClient
    ??? CaptureClient: IAudioCaptureClient
    ??? Properties:
        ??? Latency (Exclusive: 3-10ms, Shared: 10-30ms)
        ??? BufferSize (negotiated with device)
        ??? SampleRate (native device rate)
```

---

## ?? Implementation Checklist

### **Step 1: Create WasapiCapabilities Class**
- [ ] Create `AudioIO/WasapiCapabilities.vb`
- [ ] Properties:
  - [ ] `SupportsExclusiveMode As Boolean`
  - [ ] `SupportsSharedMode As Boolean`
  - [ ] `NativeSampleRate As Integer`
  - [ ] `NativeBitDepth As Integer`
  - [ ] `NativeChannels As Integer`
  - [ ] `MinBufferDuration As TimeSpan`
  - [ ] `MaxBufferDuration As TimeSpan`
  - [ ] `DefaultBufferDuration As TimeSpan`
- [ ] Methods:
  - [ ] `QueryCapabilities(device As MMDevice) As WasapiCapabilities`
  - [ ] `SupportsFormat(format As WaveFormat) As Boolean`

### **Step 2: Create WasapiInputSource Class**
- [ ] Create `AudioIO/WasapiInputSource.vb`
- [ ] Implement `IInputSource` interface
- [ ] Constructor parameters:
  - [ ] `device As MMDevice`
  - [ ] `shareMode As AudioClientShareMode`
  - [ ] `desiredLatency As Integer` (milliseconds)
  - [ ] `waveFormat As WaveFormat`
- [ ] Properties:
  - [ ] `CurrentMode As AudioClientShareMode`
  - [ ] `ActualLatency As Integer`
  - [ ] `BufferSize As Integer`
  - [ ] `IsExclusiveModeActive As Boolean`
- [ ] Methods:
  - [ ] `Start()`
  - [ ] `Stop()`
  - [ ] `GetCaptureBuffer() As Byte()`
  - [ ] `ReleaseBuffer()`

### **Step 3: Implement Exclusive Mode**
- [ ] Initialize WASAPI in exclusive mode:
  ```vb
  Public Sub InitializeExclusive(device As MMDevice, format As WaveFormat)
      ' Get audio client
      audioClient = device.AudioClient
      
      ' Check if format supported
      If Not audioClient.IsFormatSupported(AudioClientShareMode.Exclusive, format) Then
          ' Try native format
          format = GetNativeFormat(device)
      End If
      
      ' Initialize exclusive mode with minimum latency
      Dim bufferDuration = GetMinimumBufferDuration(device)
      audioClient.Initialize(
          AudioClientShareMode.Exclusive,
          AudioClientStreamFlags.None,
          bufferDuration,
          bufferDuration,
          format,
          Guid.Empty
      )
      
      ' Create capture client
      captureClient = audioClient.AudioCaptureClient
  End Sub
  ```

### **Step 4: Implement Shared Mode**
- [ ] Initialize WASAPI in shared mode (fallback):
  ```vb
  Public Sub InitializeShared(device As MMDevice, format As WaveFormat)
      audioClient = device.AudioClient
      
      ' Shared mode uses system mixer, more flexible
      Dim bufferDuration = New TimeSpan(0, 0, 0, 0, 30) ' 30ms
      audioClient.Initialize(
          AudioClientShareMode.Shared,
          AudioClientStreamFlags.None,
          bufferDuration,
          TimeSpan.Zero,
          format,
          Guid.Empty
      )
      
      captureClient = audioClient.AudioCaptureClient
  End Sub
  ```

### **Step 5: Implement Capture Loop**
- [ ] Create background capture thread
- [ ] Implement zero-copy buffer access:
  ```vb
  Private Sub CaptureLoop()
      While Not stopFlag
          ' Wait for buffer to fill
          Dim packetsAvailable = captureClient.GetNextPacketSize()
          
          While packetsAvailable > 0
              ' Get pointer to buffer (zero-copy)
              Dim buffer As IntPtr
              Dim framesAvailable As Integer
              Dim flags As AudioClientBufferFlags
              
              captureClient.GetBuffer(buffer, framesAvailable, flags)
              
              ' Copy to managed buffer if needed
              If framesAvailable > 0 Then
                  Dim bytes = framesAvailable * waveFormat.BlockAlign
                  Dim managedBuffer(bytes - 1) As Byte
                  Marshal.Copy(buffer, managedBuffer, 0, bytes)
                  
                  ' Raise DataAvailable event
                  RaiseEvent DataAvailable(Me, New AudioDataEventArgs(managedBuffer))
              End If
              
              ' Release buffer back to system
              captureClient.ReleaseBuffer(framesAvailable)
              
              packetsAvailable = captureClient.GetNextPacketSize()
          End While
          
          ' Sleep briefly to avoid busy-wait
          Thread.Sleep(1)
      End While
  End Sub
  ```

### **Step 6: Implement Sample Rate Conversion**
- [ ] Use NAudio's `WaveFormatConversionStream` when needed
- [ ] Prefer native device sample rate in exclusive mode
- [ ] Allow resampling in shared mode

### **Step 7: Implement Automatic Mode Selection**
- [ ] Create `AutoSelectMode()` method:
  ```vb
  Public Function AutoSelectMode(device As MMDevice, desiredLatency As Integer) As AudioClientShareMode
      Dim caps = WasapiCapabilities.QueryCapabilities(device)
      
      ' Try exclusive mode first for low latency
      If desiredLatency < 10 AndAlso caps.SupportsExclusiveMode Then
          Return AudioClientShareMode.Exclusive
      End If
      
      ' Fall back to shared mode
      Return AudioClientShareMode.Shared
  End Function
  ```

### **Step 8: Implement Error Handling & Recovery**
- [ ] Handle device disconnection
- [ ] Handle format not supported errors
- [ ] Automatic fallback to shared mode
- [ ] Graceful degradation to WaveIn if WASAPI fails

### **Step 9: Create WasapiEngine Class**
- [ ] Create `AudioIO/WasapiEngine.vb`
- [ ] Implement `IAudioEngine` interface
- [ ] Wrap `WasapiInputSource`
- [ ] Provide device enumeration using `MMDeviceEnumerator`
- [ ] Implement capability detection

### **Step 10: Integrate with AudioInputManager**
- [ ] Update `AudioInputManager.CreateInputSource()` to support WASAPI
- [ ] Add WASAPI device enumeration
- [ ] Add mode selection UI (Exclusive/Shared)

---

## ?? Testing Checklist

### **Functionality Tests:**
- [ ] Exclusive mode works (if supported)
- [ ] Shared mode works (always supported)
- [ ] Sample rates: 44.1kHz, 48kHz, 96kHz, 192kHz
- [ ] Bit depths: 16-bit, 24-bit, 32-bit float
- [ ] Multi-channel support (mono, stereo, 5.1, 7.1)
- [ ] Automatic mode selection

### **Latency Tests:**
- [ ] Exclusive mode: <10ms
- [ ] Shared mode: <30ms
- [ ] Measure input-to-output latency
- [ ] Compare with WaveIn baseline

### **Stability Tests:**
- [ ] 1-hour continuous recording (no dropouts)
- [ ] Device disconnect/reconnect
- [ ] Format change handling
- [ ] Buffer overrun recovery

### **Quality Tests:**
- [ ] No audio artifacts (clicks, pops)
- [ ] Frequency response flat (sweep test)
- [ ] THD+N measurement (<-100dB)

---

## ?? Expected Performance

| Metric | Exclusive Mode | Shared Mode | WaveIn (Baseline) |
|--------|----------------|-------------|-------------------|
| **Latency** | 3-10ms | 10-30ms | 20-50ms |
| **CPU Usage** | 2-5% | 3-7% | 5-10% |
| **Dropout Rate** | 0% | 0% | Occasional |
| **Max Sample Rate** | 192kHz | 192kHz | 96kHz |
| **Bit Depth** | 32-bit float | 32-bit float | 16/24-bit |

---

## ?? Implementation Tips

### **Exclusive Mode Considerations:**
- Requires application to have exclusive control of device
- Other apps cannot use device simultaneously
- Best for professional recording
- May fail if device in use

### **Shared Mode Considerations:**
- Always available (system mixer)
- Multiple apps can share device
- Higher latency than exclusive
- Good for general use

### **Buffer Size Selection:**
- Smaller buffer = lower latency, more CPU
- Larger buffer = higher latency, less CPU
- Exclusive mode: 3-10ms typical
- Shared mode: 10-30ms typical

### **Sample Rate:**
- Use native device rate when possible
- Avoid resampling (quality loss)
- Common rates: 44100, 48000, 96000, 192000

---

## ?? Reference Materials

### **NAudio WASAPI Classes:**
- `MMDeviceEnumerator` - Device discovery
- `MMDevice` - Represents audio device
- `AudioClient` - Main WASAPI interface
- `AudioCaptureClient` - Capture buffer access
- `WasapiCapture` - High-level wrapper

### **Microsoft Documentation:**
- [WASAPI Overview](https://docs.microsoft.com/en-us/windows/win32/coreaudio/wasapi)
- [Exclusive Mode Streams](https://docs.microsoft.com/en-us/windows/win32/coreaudio/exclusive-mode-streams)
- [AudioClient Interface](https://docs.microsoft.com/en-us/windows/win32/api/audioclient/nn-audioclient-iaudioclient)

---

## ? Definition of Done

- [ ] WasapiCapabilities class complete
- [ ] WasapiInputSource implemented
- [ ] Exclusive mode working
- [ ] Shared mode working
- [ ] Automatic mode selection
- [ ] Sample rate conversion
- [ ] Error handling & recovery
- [ ] WasapiEngine class complete
- [ ] Integration with AudioInputManager
- [ ] UI for mode selection
- [ ] Latency targets met
- [ ] All tests passing
- [ ] Documentation complete

---

## ?? Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **Latency (Exclusive)** | <10ms | RTL (Round-Trip Latency) test |
| **Latency (Shared)** | <30ms | RTL test |
| **CPU Usage** | <7% | Performance Monitor |
| **Dropout Rate** | 0% | 1-hour stress test |
| **Format Support** | 100% | Test all common formats |

---

**Task Created:** January 14, 2026  
**Target Start:** After Task 1.1  
**Target Completion:** 7 days  
**Dependencies:** Task 1.1 (Input Abstraction Layer)  
**Blocks:** None (ASIO is optional)
