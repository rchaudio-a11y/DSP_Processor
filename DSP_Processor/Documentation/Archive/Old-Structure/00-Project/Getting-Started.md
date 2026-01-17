# Getting Started with DSP_Processor

**Date:** January 15, 2026  
**Audience:** New developers  
**Time to Setup:** ~15 minutes

---

## ?? **Prerequisites**

### **Required:**
- **Visual Studio 2022** (Community Edition or higher)
- **.NET 8.0 SDK**
- **Windows 10/11** (WASAPI support)

### **Optional:**
- **Audio interface** (for recording)
- **Stereo test files** (for testing)
- **Git** (for version control)

---

## ?? **Quick Start**

### **1. Clone the Repository**
```bash
git clone https://github.com/rchaudio-a11y/DSP_Processor.git
cd DSP_Processor
```

### **2. Open in Visual Studio**
1. Open `DSP_Processor.sln`
2. Wait for NuGet packages to restore
3. Build the solution (Ctrl+Shift+B)

### **3. Run the Application**
1. Press **F5** (Start Debugging)
2. Application should launch with dark-themed UI

---

## ??? **First Time Setup**

### **Configure Audio Devices:**

1. **Click "?? Program" tab** (left side)
2. **Audio Settings section:**
   - Select **Input Device** (microphone/line-in)
   - Select **Output Device** (speakers/headphones)
   - Choose **WASAPI Exclusive Mode** for lowest latency
3. **Recording Settings:**
   - Set **Sample Rate** (44.1 kHz or 48 kHz recommended)
   - Choose **Bit Depth** (16-bit or 24-bit)
   - Select **Recording Format** (WAV recommended for testing)

### **Test Audio:**

1. **Load a Test File:**
   - Click "?? Files" tab
   - Click "Add Files" or drag & drop audio file
   - Select file from list
2. **Play Audio:**
   - Click ? Play button in transport controls
   - Adjust volume slider if needed
3. **View Visualizations:**
   - Click "?? Waveform" tab to see waveform
   - Click "?? Spectrum" tab to see FFT analyzer
   - Click "?? Meters" tab to see VU meters

---

## ??? **Project Structure**

```
DSP_Processor\
??? Audio\              # Audio routing and management
?   ??? Routing\        # AudioPipelineRouter
??? AudioIO\            # Audio I/O (inputs, outputs)
?   ??? AudioInputManager.vb
?   ??? OutputDeviceManager.vb
?   ??? PlaybackEngine.vb
??? DSP\                # DSP processors and chain
?   ??? GainProcessor.vb
?   ??? ProcessorChain.vb
?   ??? DSPThread.vb
??? Recording\          # Recording engine
?   ??? RecordingEngine.vb
??? UI\                 # User interface controls
?   ??? TabPanels\      # Tab page user controls
?   ??? Visualization\  # Spectrum, waveform controls
??? Utils\              # Utilities (logging, etc.)
??? Documentation\      # Project documentation
```

---

## ?? **Development Workflow**

### **Making Changes:**

1. **Find the relevant code:**
   - Use [Architecture docs](../01-Architecture/) to understand structure
   - Use [Active Tasks](../04-Tasks/Active-Tasks.md) for current work
2. **Make your changes:**
   - Follow [Coding Standards](../02-Implementation/Coding-Standards.md)
   - Add comments for complex logic
3. **Test your changes:**
   - Build (Ctrl+Shift+B)
   - Run (F5)
   - Test with audio files
4. **Commit:**
   - Commit with descriptive message
   - Reference task/issue number

### **Building:**
```bash
# Debug build
dotnet build --configuration Debug

# Release build
dotnet build --configuration Release
```

### **Testing:**
- **Manual Testing:** Run app and test features
- **Stereo Test:** Use stereo test file to verify L/R channels
- **Performance Test:** Monitor CPU usage during playback

---

## ?? **Key Concepts**

### **Audio Signal Flow:**
```
Input Device
    ?
AudioInputManager
    ?
AudioPipelineRouter
    ?
DSPThread ? ProcessorChain
    ?
PlaybackEngine
    ?
Output Device
```

### **Threading Model:**
- **UI Thread:** User interface (MainForm)
- **Audio Thread:** Real-time audio processing (DSPThread)
- **FFT Thread:** Spectrum analysis (FFTMonitorThread)
- **Logging Thread:** Async logging (LoggingManager)

### **DSP Processor Chain:**
```
Input
  ?
GainProcessor (gain + pan)
  ?
HighPassFilter (30-180 Hz) [In Progress]
  ?
LowPassFilter (8-20 kHz) [In Progress]
  ?
OutputMixer (master gain + width) [In Progress]
  ?
Output
```

---

## ?? **Troubleshooting**

### **No Audio Output:**
1. Check output device selection in Settings
2. Verify volume slider is not at 0%
3. Try different WASAPI mode (Shared vs Exclusive)
4. Check Windows audio settings

### **Build Errors:**
1. Restore NuGet packages (right-click solution ? Restore)
2. Clean solution (Build ? Clean Solution)
3. Rebuild (Build ? Rebuild Solution)
4. Check .NET 8.0 SDK is installed

### **Application Crashes:**
1. Check Logs tab for error messages
2. Review stack trace
3. Check [Known Issues](../06-Issues/Known-Issues.md)
4. Try Debug mode (F5) to get more info

### **Recording Issues:**
1. Verify input device is selected
2. Check input volume level
3. Ensure file path is writable
4. Try different recording format

---

## ?? **Next Steps**

### **Learn the Architecture:**
- Read [Audio Pipeline](../01-Architecture/Audio-Pipeline.md)
- Understand [DSP Chain](../01-Architecture/DSP-Chain.md)
- Review [Threading Model](../01-Architecture/Threading-Model.md)

### **Start Contributing:**
- Check [Active Tasks](../04-Tasks/Active-Tasks.md)
- Read [Implementation Guides](../02-Implementation/)
- Follow [Coding Standards](../02-Implementation/Coding-Standards.md)

### **Explore Features:**
- Try all visualization tabs
- Test recording functionality
- Experiment with DSP controls (once complete)

---

## ?? **Getting Help**

- **Documentation:** See [INDEX.md](../INDEX.md)
- **Known Issues:** Check [Issues](../06-Issues/Known-Issues.md)
- **Architecture Questions:** Read [Architecture docs](../01-Architecture/)
- **Task Questions:** Check [Active Tasks](../04-Tasks/Active-Tasks.md)

---

## ? **Verification Checklist**

After setup, you should be able to:
- [x] Build solution without errors
- [x] Launch application
- [x] Select audio devices
- [x] Play an audio file
- [x] See waveform visualization
- [x] See spectrum analyzer
- [x] See VU meters
- [x] View logs tab

If all checked, you're ready to develop! ??

---

**Last Updated:** January 15, 2026  
**For detailed documentation, see:** [Documentation Index](../INDEX.md)
