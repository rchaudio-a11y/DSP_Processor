# Spectrum Display Troubleshooting Guide

**Issue:** No spectrum controls visible in Spectrum tab  
**Date:** 2026-01-12  
**Status:** FIXED

---

## ?? **Root Causes Identified**

### **Issue #1: Splitter Distance Calculation** ? FIXED
- **Problem:** `SplitterDistance = (tabSpectrum.Width - 450) \ 2` 
- **Issue:** If `tabSpectrum.Width` is 0 at initialization time, splitter distance becomes negative/zero
- **Result:** One panel has zero width, controls are not visible
- **Fix:** Changed to fixed distance: `SplitterDistance = 400`

### **Issue #2: Timer Not Enabled**
- **Status:** Already handled correctly
- Timer is created with `Enabled = False`
- Timer is started in `OnTransportRecord` when recording begins
- This is correct behavior (no FFT data until recording starts)

### **Issue #3: Tab Reference**
- **Status:** Already fixed in previous session
- Was adding to `tabSpectrum` (mainTabs - left side)
- Now correctly adds to `tabSpectrum1` (visualizationTabs - right side)

---

## ? **Current Status**

### **Working Code:**
```vb
Private Sub InitializeSpectrumAnalyzer()
    ' Create FFT processors
    fftInput = New DSP.FFT.FFTProcessor(4096)...
    fftOutput = New DSP.FFT.FFTProcessor(4096)...
    
    ' Create spectrum displays
    spectrumInputDisplay = New SpectrumDisplayControl()...
    spectrumOutputDisplay = New SpectrumDisplayControl()...
    
    ' Create split container with FIXED distance
    Dim splitSpectrum As New SplitContainer() With {
        .Dock = DockStyle.Fill,
        .Orientation = Orientation.Vertical,
        .SplitterDistance = 400  ' ? FIXED!
    }
    
    ' Add labels
    Dim lblPreDSP As New Label()...
    Dim lblPostDSP As New Label()...
    
    ' Add to panels
    splitSpectrum.Panel1.Controls.Add(spectrumInputDisplay)
    splitSpectrum.Panel1.Controls.Add(lblPreDSP)
    splitSpectrum.Panel2.Controls.Add(spectrumOutputDisplay)
    splitSpectrum.Panel2.Controls.Add(lblPostDSP)
    
    ' Add to CORRECT tab (visualizationTabs, right side)
    tabSpectrum1.Controls.Add(splitSpectrum)  ' ? CORRECT TAB!
    
    ' Create timer (will be started when recording begins)
    TimerSpectrum = New Timer() With {
        .Interval = 33,  ' 30 FPS
        .Enabled = False  ' Start on record
    }
End Sub

Private Sub OnTransportRecord(...)
    ' ... recording setup ...
    
    ' Start spectrum analyzer timer
    If TimerSpectrum IsNot Nothing Then
        TimerSpectrum.Start()  ' ? Starts FFT updates
    End If
End Sub
```

---

## ?? **How to Test**

### **Step 1: Run the Application**
```
Press F5 in Visual Studio
```

### **Step 2: Navigate to Spectrum Tab**
- Click **?? Spectrum** tab on **RIGHT SIDE** (visualizationTabs)
- You should see:
  - Split panel with two halves
  - Labels: "PRE-DSP (Input)" and "POST-DSP (Output)"
  - Black background in both panels

### **Step 3: Start Recording**
- Click the **Record** button ?? (on transport control or left tabs)
- Make some noise (talk, play music, etc.)

### **Step 4: Watch the Spectrum**
- Switch to ?? Spectrum tab (right side)
- You should now see:
  - **Left panel (PRE-DSP):** Cyan spectrum display showing input
  - **Right panel (POST-DSP):** Lime spectrum display showing output
  - Real-time updates (30 FPS)
  - Frequency scale from 20Hz to 20kHz
  - dB scale from -60dB to 0dB

### **Step 5: Adjust Settings**
- Click **?? Spectrum** tab on **LEFT SIDE** (mainTabs - settings)
- Change FFT size, window function, smoothing, peak hold
- Observe changes in real-time on right side display

---

## ?? **Expected Behavior**

### **When NOT Recording:**
- Spectrum tab shows black panels with labels
- No spectrum lines visible
- Timer is not running

### **When Recording:**
- Spectrum displays update 30 times per second
- Cyan line (input) shows frequency content
- Lime line (output) shows same as input (no DSP yet)
- Grid shows frequency divisions (20Hz, 100Hz, 1kHz, 10kHz, 20kHz)
- dB scale shows -60, -40, -20, 0 dB

### **When Stopped:**
- Last spectrum frame remains visible
- Timer stops
- No more updates

---

## ?? **If Still Not Visible**

### **Debug Checklist:**

1. **Check Tab Location:**
   ```vb
   ' In MainForm_Load or InitializeSpectrumAnalyzer
   Services.LoggingServiceAdapter.Instance.LogInfo($"tabSpectrum1 size: {tabSpectrum1.Width}x{tabSpectrum1.Height}")
   Services.LoggingServiceAdapter.Instance.LogInfo($"splitSpectrum size: {splitSpectrum.Width}x{splitSpectrum.Height}")
   ```

2. **Check Controls Are Added:**
   ```vb
   Services.LoggingServiceAdapter.Instance.LogInfo($"tabSpectrum1.Controls.Count: {tabSpectrum1.Controls.Count}")
   Services.LoggingServiceAdapter.Instance.LogInfo($"splitSpectrum.Panel1.Controls.Count: {splitSpectrum.Panel1.Controls.Count}")
   ```

3. **Check Timer is Starting:**
   ```vb
   ' In OnTransportRecord after TimerSpectrum.Start()
   Services.LoggingServiceAdapter.Instance.LogInfo($"TimerSpectrum enabled: {TimerSpectrum.Enabled}")
   ```

4. **Check FFT Data:**
   ```vb
   ' In TimerSpectrum_Tick
   If spectrumIn IsNot Nothing Then
       Services.LoggingServiceAdapter.Instance.LogInfo($"Spectrum bins: {spectrumIn.Length}, Max value: {spectrumIn.Max():F2} dB")
   End If
   ```

5. **Check Visual Studio Designer:**
   - Open `MainForm.Designer.vb`
   - Search for `tabSpectrum1`
   - Verify it has `Location`, `Size`, and `Visible = True`

---

## ?? **Common Issues**

### **Issue: Spectrum Tab is Empty**
**Cause:** Controls added to wrong tab  
**Fix:** Verify `tabSpectrum1.Controls.Add(splitSpectrum)` not `tabSpectrum.Controls.Add(...)`

### **Issue: Timer Never Starts**
**Cause:** `TimerSpectrum` is Nothing  
**Fix:** Ensure `InitializeSpectrumAnalyzer()` is called before recording

### **Issue: Spectrum is All Black**
**Cause:** No audio input or FFT not receiving data  
**Fix:** Check microphone is working, verify FFT.AddSamples() is being called

### **Issue: Spectrum is Frozen**
**Cause:** Timer stopped or exception in TimerSpectrum_Tick  
**Fix:** Check logs for exceptions, verify timer.Enabled = True

### **Issue: Only One Panel Visible**
**Cause:** SplitterDistance = 0 or negative  
**Fix:** Use fixed distance (400) instead of calculated

---

## ?? **Success Indicators**

? **Spectrum tab on RIGHT SIDE shows two panels with labels**  
? **Recording starts = spectrum displays update in real-time**  
? **Cyan line (input) and Lime line (output) visible**  
? **Grid and scale labels visible**  
? **Settings on LEFT SIDE affect display on RIGHT SIDE**  
? **Settings persist between sessions (saved to JSON)**

---

## ?? **Logs to Check**

Look for these messages in ?? Logs tab:

```
[INFO] Spectrum analyzer initialized successfully
[INFO] Recording started successfully
[INFO] TimerSpectrum enabled: True
[DEBUG] Spectrum update: 2048 bins, first value: -45.32 dB
[INFO] FFT size changed to: 4096
[INFO] Spectrum settings saved to file
```

---

## ?? **Next Steps After Verification**

Once spectrum is displaying correctly:

1. **Test All Settings:**
   - FFT size: 1024, 2048, 4096, 8192, 16384
   - Window: None, Hann, Hamming, Blackman
   - Smoothing: 0%, 50%, 100%
   - Peak Hold: On/Off

2. **Test Settings Persistence:**
   - Change settings
   - Close app
   - Reopen app
   - Verify settings restored

3. **Test Performance:**
   - Record for 5+ minutes
   - Watch for any freezing or slowdown
   - Check CPU usage

4. **Prepare for DSP Integration:**
   - Pre-DSP (input) is working
   - Post-DSP (output) currently shows same as input
   - Ready for actual DSP processing to be added!

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-12  
**Status:** Issue Fixed, Ready for Testing

**END OF TROUBLESHOOTING GUIDE**
