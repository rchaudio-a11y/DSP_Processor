# Testing Issues Found - Pre-DSP Phase

**Date:** January 15, 2026  
**Testing Phase:** Comprehensive UI/Functionality Testing  
**Tester:** User  
**Status:** ?? **IN PROGRESS**

---

## ?? **Summary**

**Issues Found:** 5  
**Critical:** 2 (Blocking)  
**High Priority:** 1  
**Medium/Low:** 2 (Feature gaps)

---

## ?? **CRITICAL ISSUES (BLOCKING DSP)**

### **Issue #1: Playback Time Not Counting**

**Severity:** ?? **CRITICAL**  
**Category:** Transport Control  
**Test:** Test 3.1 - Transport Control  
**Status:** ? **BLOCKING**

**Description:**
When playing back a recorded file, the time display on the transport control does not update. The time stays at 00:00:00 instead of showing the current playback position.

**Steps to Reproduce:**
1. Record a file (any length)
2. Select the file in the file list
3. Click the Play button
4. Observe the time display

**Expected:**
- Time display shows: 00:00:01, 00:00:02, etc.
- Updates smoothly during playback

**Actual:**
- Time display stays at: 00:00:00
- Does not update during playback

**Impact:**
- User cannot see playback position
- Cannot scrub to specific time
- Poor user experience

**Root Cause (Suspected):**
- `TimerPlayback` may not be firing
- Time calculation may be incorrect
- Event handler may not be wired up

**Files Likely Involved:**
- `MainForm.vb` - `TimerPlayback_Tick()` handler
- `PlaybackEngine.vb` - Position tracking
- `TransportControl.vb` - Time display update

**Priority:** **FIX IMMEDIATELY BEFORE DSP**

---

### **Issue #2: Play Indicator Not Staying Lit**

**Severity:** ?? **CRITICAL**  
**Category:** Transport Control  
**Test:** Test 3.1 - Transport Control  
**Status:** ? **BLOCKING**

**Description:**
The play indicator (LED/button state) above the play button does not stay lit during playback. It should remain illuminated while a file is playing to show the current transport state.

**Steps to Reproduce:**
1. Record a file
2. Select the file
3. Click Play button
4. Observe the play indicator

**Expected:**
- Indicator lights up when Play is clicked
- Remains lit during entire playback
- Turns off when playback stops/ends

**Actual:**
- Indicator may flash briefly or not light at all
- Does not stay lit during playback
- User can't tell if playback is active

**Impact:**
- User cannot see transport state
- Confusing UX (is it playing or not?)
- Critical for DAW-style interface

**Root Cause (Suspected):**
- `TransportControl.SetPlaybackState()` not called
- State not maintained during playback
- Timer not updating indicator

**Files Likely Involved:**
- `TransportControl.vb` - Indicator state management
- `MainForm.vb` - Playback state events
- `PlaybackEngine.vb` - Playback state

**Priority:** **FIX IMMEDIATELY BEFORE DSP**

---

## ?? **HIGH PRIORITY ISSUES**

### **Issue #3: Buffer Size Not Persisting When Switching Drivers**

**Severity:** ?? **HIGH**  
**Category:** Settings Management  
**Test:** Test 5.1 - Save Settings  
**Status:** ?? **Should fix before DSP**

**Description:**
When switching between audio drivers (WASAPI ? WaveIn), custom buffer size settings are not preserved. The buffer resets to the default value for the new driver instead of keeping the user's custom setting.

**Steps to Reproduce:**
1. Select WASAPI driver
2. Set buffer size to 40ms (custom value)
3. Switch to WaveIn driver
4. Switch back to WASAPI
5. Observe buffer size

**Expected:**
- WASAPI buffer stays at 40ms when returning to WASAPI
- Each driver remembers its last custom setting
- Or: Single buffer setting applies to both drivers

**Actual:**
- WASAPI buffer resets to default (50ms or 20ms)
- Custom 40ms setting is lost
- User must re-enter custom value

**Impact:**
- Frustrating for users who use custom buffers
- Settings don't persist as expected
- Violates least-surprise principle

**Root Cause (Suspected):**
- Settings are per-driver but not saved properly
- Driver change loads driver defaults instead of saved settings
- `AudioSettingsPanel.LoadDefaultsForDriver()` overwrites saved values

**Files Likely Involved:**
- `AudioSettingsPanel.vb` - Driver switching logic
- `SettingsManager.vb` - Settings persistence
- `AudioDeviceSettings.cs` - Settings model

**Possible Solutions:**
1. **Option A:** Save buffer size per driver (WASAPI buffer, WaveIn buffer)
2. **Option B:** Single buffer size applies to all drivers
3. **Option C:** Prompt user: "Apply current buffer to new driver?"

**Priority:** **FIX BEFORE DSP** (Settings UX is important)

---

## ?? **FEATURE GAPS (Expected)**

### **Issue #4: No Monitor Output Selection**

**Severity:** ?? **MEDIUM** (Feature gap)  
**Category:** Audio Output  
**Test:** N/A (Feature not in test plan)  
**Status:** ?? **Feature not implemented**

**Description:**
There is no UI control to select a monitor output device (speakers/headphones). The app likely uses the default Windows audio device.

**Expected (Future Feature):**
- Dropdown to select output device
- Separate from input device selection
- Volume control for monitor output

**Current Workaround:**
- Use Windows system default audio device
- Change via Windows Sound settings

**Impact:**
- Low - Users can use Windows defaults
- Medium - Professional users want per-app routing

**Status:** **NOT BLOCKING DSP**  
**Recommendation:** Add to DSP phase or post-DSP backlog

---

### **Issue #5: No Arm/Mute/Solo Controls for Input**

**Severity:** ?? **LOW** (Feature gap)  
**Category:** Input Controls  
**Test:** N/A (Feature not in test plan)  
**Status:** ?? **Feature not implemented**

**Description:**
There are no Arm/Mute/Solo buttons for input devices. In multi-input DAWs, these are used to enable/disable/isolate inputs.

**Expected (Future Feature):**
- Arm button: Enable/disable input monitoring
- Mute button: Silence input without disarming
- Solo button: Hear only this input (mute others)

**Current Workaround:**
- Arm/disarm microphone to enable/disable
- No mute/solo available

**Impact:**
- Very Low for single-input setup
- Would be useful for multi-input in future

**Status:** **NOT BLOCKING DSP**  
**Recommendation:** Consider for future multi-input support

---

## ?? **Issue Priority Matrix**

| Issue | Severity | Blocks DSP? | Fix Timeframe | Category |
|-------|----------|-------------|---------------|----------|
| #1: Playback time not counting | ?? Critical | ? YES | Immediate | Transport |
| #2: Play indicator not lit | ?? Critical | ? YES | Immediate | Transport |
| #3: Buffer not persisting | ?? High | ?? Should fix | Before DSP | Settings |
| #4: No monitor output | ?? Medium | ? No | Post-DSP | Feature |
| #5: No arm/mute/solo | ?? Low | ? No | Backlog | Feature |

---

## ?? **Recommended Fix Order**

### **Phase 1: Fix Critical Issues (BEFORE DSP)**
1. ? **Issue #1** - Fix playback time display
2. ? **Issue #2** - Fix play indicator state
3. ?? **Issue #3** - Fix buffer persistence (or document as known issue)

**Estimated Time:** 1-2 hours

### **Phase 2: Verify Fixes**
1. Re-test Transport Control (Test 3.1)
2. Re-test Settings Management (Test 5.1)
3. Continue comprehensive testing

**Estimated Time:** 30 minutes

### **Phase 3: Begin DSP**
- Once Issues #1 and #2 are fixed, DSP can begin
- Issue #3 can be fixed in parallel or documented

---

## ?? **Fix Suggestions**

### **Issue #1: Playback Time Fix**

**Likely Problem:**
```visualbasic
' MainForm.vb - TimerPlayback may not be starting
Private Sub OnPlaybackStarted()
    ' MISSING: TimerPlayback.Start()
End Sub
```

**Likely Fix:**
```visualbasic
Private Sub OnPlaybackStarted()
    TimerPlayback.Start()  ' Ensure timer starts!
End Sub

Private Sub TimerPlayback_Tick()
    If playbackEngine IsNot Nothing Then
        Dim position = playbackEngine.CurrentPosition
        transportControl.UpdateTime(position)  ' Update display
    End If
End Sub
```

---

### **Issue #2: Play Indicator Fix**

**Likely Problem:**
```visualbasic
' TransportControl state not updated during playback
```

**Likely Fix:**
```visualbasic
Private Sub OnPlaybackStarted()
    transportControl.SetPlayState(True)  ' Light up indicator
    TimerPlayback.Start()
End Sub

Private Sub OnPlaybackStopped()
    transportControl.SetPlayState(False)  ' Turn off indicator
    TimerPlayback.Stop()
End Sub
```

---

### **Issue #3: Buffer Persistence Fix**

**Option A: Per-Driver Buffer Settings**
```visualbasic
' AudioDeviceSettings model
Public Property WasapiBufferMs As Integer = 50
Public Property WaveInBufferMs As Integer = 50

' When switching drivers
Private Sub OnDriverChanged(newDriver As DriverType)
    If newDriver = DriverType.WASAPI Then
        bufferSlider.Value = settings.WasapiBufferMs
    Else
        bufferSlider.Value = settings.WaveInBufferMs
    End If
End Sub
```

**Option B: Single Buffer Applies to All**
```visualbasic
' Just save one buffer size, apply to both drivers
' Simpler, but may not be optimal for both drivers
```

---

## ?? **Testing Status**

### **Tests Executed:**
- ? Started Category 1: Audio Input/Output
- ?? Found issues in Category 3: UI Controls (Transport)
- ?? Found issues in Category 5: Settings Management

### **Tests Blocked:**
- ? Test 3.1: Transport Control (FAILED - Issues #1, #2)
- ?? Test 5.1: Save Settings (PARTIAL FAIL - Issue #3)

### **Recommendation:**
**PAUSE TESTING ? FIX CRITICAL ISSUES ? RESUME TESTING**

---

## ?? **Next Steps**

### **Immediate Actions:**
1. ? **Fix Issue #1** (Playback time display)
2. ? **Fix Issue #2** (Play indicator state)
3. ? **Test fixes** (verify Issues #1 and #2 resolved)
4. ?? **Fix or document Issue #3** (Buffer persistence)

### **After Fixes:**
1. **Resume comprehensive testing** (complete all categories)
2. **Document any additional issues**
3. **Generate final test report**
4. **Proceed to DSP phase** (if all critical tests pass)

---

## ?? **Related Documents**

- [Comprehensive-Testing-Plan-Pre-DSP-2026-01-15.md](Comprehensive-Testing-Plan-Pre-DSP-2026-01-15.md)
- [Recording-Architecture-Final-2026-01-15.md](../Architecture/Recording-Architecture-Final-2026-01-15.md)

---

**Status:** ?? **TESTING IN PROGRESS - CRITICAL ISSUES FOUND**  
**Next Action:** Fix Issues #1 and #2 before continuing testing  
**Updated:** January 15, 2026
