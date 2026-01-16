# Comprehensive Testing Plan - Pre-DSP Phase

**Date:** January 15, 2026  
**Purpose:** Verify all UI controls, displays, and audio functionality before DSP implementation  
**Status:** ?? **READY TO EXECUTE**

---

## ?? **Objectives**

Before beginning DSP implementation, we need to verify that:
1. ? All UI controls work correctly
2. ? All displays update properly
3. ? Audio recording is glitch-free
4. ? Settings save/load correctly
5. ? FFT/Spectrum displays work
6. ? No regressions from recent changes

---

## ?? **Test Categories**

### **Category 1: Audio Input/Output** ? CRITICAL
### **Category 2: Recording Functionality** ? CRITICAL
### **Category 3: UI Controls** ? HIGH PRIORITY
### **Category 4: Displays/Visualizations** ? HIGH PRIORITY
### **Category 5: Settings Management** ? MEDIUM PRIORITY
### **Category 6: Edge Cases/Stress Tests** ? MEDIUM PRIORITY

---

## ?? **Test Suite**

---

## **CATEGORY 1: Audio Input/Output** ? CRITICAL

### **Test 1.1: WASAPI Input**
**Purpose:** Verify WASAPI driver works correctly

**Steps:**
1. Select **WASAPI** from Audio Settings
2. Select **Internal Mic** device
3. Set buffer to **50ms**
4. Arm microphone
5. Record for **30 seconds**
6. Play back recording

**Expected Results:**
- ? Microphone arms without errors
- ? Recording starts/stops cleanly
- ? Audio quality is perfect (no clicks/pops)
- ? Log shows: "Subscribed to AudioDataAvailable callback"
- ? Log shows: "Async Write Stats: ... Overflows=0"

**Pass Criteria:**
- Zero clicks/pops in recording
- Zero buffer overflows in log
- Clean start/stop

---

### **Test 1.2: WaveIn Input**
**Purpose:** Verify WaveIn driver works correctly

**Steps:**
1. Select **WaveIn** from Audio Settings
2. Select **Internal Mic** device
3. Set buffer to **50ms**
4. Arm microphone
5. Record for **30 seconds**
6. Play back recording

**Expected Results:**
- ? Microphone arms without errors
- ? Recording starts/stops cleanly
- ? Audio quality is perfect (no clicks/pops)
- ? Log shows: "Subscribed to AudioDataAvailable callback"
- ? Log shows: "Async Write Stats: ... Overflows=0"

**Pass Criteria:**
- Zero clicks/pops in recording
- Zero buffer overflows in log
- Clean start/stop

---

### **Test 1.3: Buffer Size Variations**
**Purpose:** Verify different buffer sizes work

**Test Matrix:**

| Driver | Buffer Size | Expected Result |
|--------|-------------|-----------------|
| WASAPI | 10ms | ? Good (rare startup click acceptable) |
| WASAPI | 20ms | ? Perfect |
| WASAPI | 50ms | ? Perfect |
| WASAPI | 100ms | ? Perfect |
| WaveIn | 15ms | ?? Some clicks acceptable |
| WaveIn | 20ms | ? Good |
| WaveIn | 50ms | ? Perfect |
| WaveIn | 100ms | ? Perfect |

**Pass Criteria:**
- 20ms+ buffers have zero clicks
- Smaller buffers have minimal clicks

---

### **Test 1.4: Sample Rate Variations**
**Purpose:** Verify different sample rates work

**Test Matrix:**

| Sample Rate | Expected Result |
|-------------|-----------------|
| 44100 Hz | ? Perfect |
| 48000 Hz | ? Perfect |
| 96000 Hz | ? Perfect (if hardware supports) |

**Pass Criteria:**
- All supported sample rates work correctly
- Audio quality matches sample rate

---

## **CATEGORY 2: Recording Functionality** ? CRITICAL

### **Test 2.1: Manual Recording**
**Purpose:** Verify manual start/stop works

**Steps:**
1. Arm microphone
2. Click **Record** button
3. Wait 10 seconds
4. Click **Stop** button
5. Check recording file exists
6. Play back recording

**Expected Results:**
- ? Recording starts immediately
- ? Transport shows recording time
- ? VU meter shows levels
- ? Recording stops cleanly
- ? File saved with correct name
- ? Audio quality is perfect

**Pass Criteria:**
- Clean start/stop
- File exists and is valid
- Zero clicks/pops

---

### **Test 2.2: Timed Recording**
**Purpose:** Verify timed recording auto-stops

**Steps:**
1. Select **Timed Recording** mode
2. Set duration to **30 seconds**
3. Arm microphone
4. Click **Record**
5. Wait for auto-stop

**Expected Results:**
- ? Recording starts
- ? Timer counts down
- ? Recording stops at 30 seconds
- ? File saved
- ? Status returns to idle

**Pass Criteria:**
- Auto-stops at correct time
- File is exactly 30 seconds
- Zero clicks/pops

---

### **Test 2.3: Loop Recording**
**Purpose:** Verify loop mode works

**Steps:**
1. Select **Loop Recording** mode
2. Set loop count to **3 takes**
3. Set take duration to **10 seconds**
4. Set delay to **2 seconds**
5. Arm microphone
6. Click **Record**
7. Wait for all takes to complete

**Expected Results:**
- ? Take 1 records for 10 seconds
- ? 2 second delay
- ? Take 2 records for 10 seconds
- ? 2 second delay
- ? Take 3 records for 10 seconds
- ? Recording stops
- ? 3 files created

**Pass Criteria:**
- All 3 takes recorded
- Correct timing
- All files valid
- Zero clicks/pops

---

### **Test 2.4: Long Recording**
**Purpose:** Verify stability over long recordings

**Steps:**
1. Set timed recording to **5 minutes**
2. Arm microphone
3. Click **Record**
4. Wait for completion
5. Check log for issues

**Expected Results:**
- ? Records full 5 minutes
- ? No crashes/hangs
- ? No memory leaks
- ? File size correct (~50MB at 44.1kHz/16-bit)
- ? Zero overflows in log

**Pass Criteria:**
- Completes without issues
- No performance degradation
- Zero clicks/pops throughout

---

## **CATEGORY 3: UI Controls** ? HIGH PRIORITY

### **Test 3.1: Transport Control**
**Purpose:** Verify transport buttons work

**Controls to Test:**

| Control | Action | Expected Result |
|---------|--------|-----------------|
| **Record** | Click | Starts recording |
| **Stop** | Click while recording | Stops recording |
| **Play** | Click on file | Plays back file |
| **Pause** | Click while playing | Pauses playback |
| **Resume** | Click while paused | Resumes playback |
| **Time Display** | During recording | Shows elapsed time |
| **Time Display** | During playback | Shows playback position |

**Pass Criteria:**
- All buttons respond correctly
- Time display updates smoothly
- No UI freezes

---

### **Test 3.2: Volume Controls**
**Purpose:** Verify volume controls work

**Controls to Test:**

| Control | Action | Expected Result |
|---------|--------|-----------------|
| **Input Volume** | Slide 0% ? 100% | Adjusts input level |
| **Input Meter** | Speak into mic | Shows level changing |
| **Playback Volume** | Slide 0% ? 100% | Adjusts playback level |
| **Playback Meter** | During playback | Shows level changing |

**Pass Criteria:**
- Volume changes take effect immediately
- Meters respond accurately
- No audio distortion at high levels

---

### **Test 3.3: Audio Settings Panel**
**Purpose:** Verify audio settings controls

**Controls to Test:**

| Control | Action | Expected Result |
|---------|--------|-----------------|
| **Driver** | Change WASAPI ? WaveIn | Driver changes, mic restarts |
| **Device** | Change device | Device changes, mic restarts |
| **Sample Rate** | Change rate | Rate changes, mic restarts |
| **Buffer Size** | Change buffer | Buffer changes, mic restarts |
| **Channels** | Change Mono ? Stereo | Channel config changes |
| **Bits Per Sample** | Change 16 ? 24 | Bit depth changes |

**Pass Criteria:**
- All changes apply immediately
- Mic restarts cleanly
- Settings persist after restart

---

### **Test 3.4: Recording Options Panel**
**Purpose:** Verify recording options controls

**Controls to Test:**

| Control | Action | Expected Result |
|---------|--------|-----------------|
| **Mode** | Manual | Manual recording |
| **Mode** | Timed | Shows duration control |
| **Mode** | Loop | Shows loop controls |
| **Duration** | Set value | Timed recording uses value |
| **Loop Count** | Set value | Loop records N takes |
| **Loop Delay** | Set value | Delay between takes |

**Pass Criteria:**
- All modes work correctly
- Controls enable/disable appropriately
- Values persist after restart

---

### **Test 3.5: Pipeline Panel**
**Purpose:** Verify DSP pipeline controls (even if DSP not implemented)

**Controls to Test:**

| Control | Action | Expected Result |
|---------|--------|-----------------|
| **Enable DSP** | Toggle | Enables/disables pipeline |
| **Input FFT** | Toggle | Shows input spectrum |
| **Output FFT** | Toggle | Shows output spectrum |
| **Tap Point** | Change | FFT tap changes |

**Pass Criteria:**
- Controls respond correctly
- TRUE BYPASS works (DSP off)
- FFT displays update

---

## **CATEGORY 4: Displays/Visualizations** ? HIGH PRIORITY

### **Test 4.1: VU Meters**
**Purpose:** Verify VU meters work correctly

**Tests:**

| Meter | Test | Expected Result |
|-------|------|-----------------|
| **Recording Meter** | Speak into mic | Green bar moves, peaks hold |
| **Recording Meter** | Silence | Bar drops, shows noise floor |
| **Recording Meter** | Loud input | Red zone shows clipping |
| **Playback Meter** | Play file | Green bar moves, matches audio |
| **Decay** | After peak | Peak indicator fades slowly |

**Pass Criteria:**
- Meters respond smoothly (no stuttering)
- Peak indicators work
- Clipping indicator works
- 50 FPS update rate maintained

---

### **Test 4.2: Waveform Display**
**Purpose:** Verify waveform visualization works

**Tests:**

| Test | Action | Expected Result |
|------|--------|-----------------|
| **Load File** | Select recording | Waveform displays |
| **Zoom** | Scroll wheel | Zooms in/out |
| **Pan** | Drag | Pans left/right |
| **Playback** | Click play | Playhead moves |
| **Scroll** | During playback | Waveform scrolls smoothly |

**Pass Criteria:**
- Waveform renders correctly
- Zoom/pan smooth
- Playhead tracks accurately
- 60 FPS maintained

---

### **Test 4.3: Spectrum Analyzer**
**Purpose:** Verify spectrum analyzer works

**Tests:**

| Test | Action | Expected Result |
|------|--------|-----------------|
| **Enable** | Toggle Input FFT | Spectrum displays |
| **Frequency Response** | Speak "sssss" | High frequencies peak |
| **Frequency Response** | Speak "oooo" | Low frequencies peak |
| **Update Rate** | Continuous audio | Smooth 20 FPS updates |
| **FFT Size** | Change 1024 ? 4096 | Resolution increases |
| **Window** | Change window type | Spectrum changes |

**Pass Criteria:**
- Spectrum renders correctly
- Frequencies accurate
- Smooth updates (no stuttering)
- No crashes with different settings

---

### **Test 4.4: Phase Scope**
**Purpose:** Verify phase scope works (if implemented)

**Tests:**

| Test | Action | Expected Result |
|------|--------|-----------------|
| **Mono Signal** | Play mono file | Vertical line (phase=0°) |
| **Stereo Signal** | Play stereo file | Lissajous pattern |
| **Out of Phase** | Play inverted stereo | Horizontal line (phase=180°) |

**Pass Criteria:**
- Display renders correctly
- Phase relationships visible
- Smooth updates

---

## **CATEGORY 5: Settings Management** ? MEDIUM PRIORITY

### **Test 5.1: Save Settings**
**Purpose:** Verify settings save correctly

**Steps:**
1. Change audio settings (driver, device, buffer)
2. Change recording options (mode, duration)
3. Change pipeline settings (DSP, FFT)
4. Close application
5. Reopen application
6. Verify settings restored

**Expected Results:**
- ? All settings restored correctly
- ? Driver/device restored
- ? Recording options restored
- ? Pipeline settings restored

**Pass Criteria:**
- 100% of settings persist
- No errors on load
- App starts with last settings

---

### **Test 5.2: Default Settings**
**Purpose:** Verify default settings load

**Steps:**
1. Delete settings file
2. Start application
3. Verify defaults loaded

**Expected Defaults:**
- WASAPI driver
- Default device (first available)
- 44100 Hz sample rate
- 50ms buffer
- Stereo, 16-bit
- Manual recording mode

**Pass Criteria:**
- Defaults load correctly
- No errors
- App functional immediately

---

### **Test 5.3: Settings Validation**
**Purpose:** Verify invalid settings rejected

**Tests:**

| Invalid Setting | Expected Result |
|-----------------|-----------------|
| Sample rate = 0 | Resets to default (44100) |
| Buffer = 0ms | Resets to default (50ms) |
| Buffer = 500ms | Capped to max (200ms) |
| Loop count = 0 | Resets to 1 |
| Loop duration = 0 | Resets to minimum (1s) |

**Pass Criteria:**
- Invalid values rejected
- Defaults substituted
- No crashes

---

## **CATEGORY 6: Edge Cases/Stress Tests** ? MEDIUM PRIORITY

### **Test 6.1: Rapid Start/Stop**
**Purpose:** Verify rapid record/stop cycles

**Steps:**
1. Arm microphone
2. Click **Record**
3. Immediately click **Stop**
4. Repeat 10 times

**Expected Results:**
- ? No crashes
- ? All files valid
- ? No memory leaks
- ? Background writer handles rapid cycles

**Pass Criteria:**
- No errors in log
- All files playable
- No resource leaks

---

### **Test 6.2: Arm/Disarm Cycling**
**Purpose:** Verify rapid arm/disarm cycles

**Steps:**
1. Arm microphone
2. Immediately disarm
3. Repeat 10 times

**Expected Results:**
- ? No crashes
- ? Resources cleaned up
- ? No driver errors

**Pass Criteria:**
- No errors in log
- No resource leaks
- Driver stable

---

### **Test 6.3: Change Settings During Recording**
**Purpose:** Verify app handles setting changes during recording

**Steps:**
1. Start recording
2. Try to change driver (should be disabled/warned)
3. Try to change device (should be disabled/warned)
4. Try to change buffer (should be disabled/warned)

**Expected Results:**
- ? Critical settings locked during recording
- ? User warned if attempted
- ? Recording continues unaffected

**Pass Criteria:**
- Settings cannot be changed during recording
- Warning shown to user
- No crashes

---

### **Test 6.4: Disk Space Full**
**Purpose:** Verify app handles disk full gracefully

**Steps:**
1. Fill disk to < 100MB free
2. Start recording
3. Let recording run until disk full

**Expected Results:**
- ? Recording stops gracefully
- ? Error message shown
- ? Partial file saved
- ? No crashes

**Pass Criteria:**
- App doesn't crash
- User informed
- Partial file valid

---

### **Test 6.5: File System Errors**
**Purpose:** Verify app handles file errors

**Tests:**

| Error Scenario | Expected Result |
|----------------|-----------------|
| Output folder deleted during recording | Error shown, recording stops |
| File locked by another process | Error shown, new filename generated |
| Output folder read-only | Error shown, user prompted for location |
| Network drive disconnected | Error shown, recording stops |

**Pass Criteria:**
- All errors handled gracefully
- User informed
- No crashes

---

## ?? **Test Execution Checklist**

### **Pre-Test Setup:**
- [ ] Latest build compiled successfully
- [ ] Log file cleared
- [ ] Test recordings folder created
- [ ] Internal mic functional
- [ ] Speakers/headphones connected

### **Test Execution:**
- [ ] **Category 1: Audio Input/Output** (Tests 1.1 - 1.4)
- [ ] **Category 2: Recording Functionality** (Tests 2.1 - 2.4)
- [ ] **Category 3: UI Controls** (Tests 3.1 - 3.5)
- [ ] **Category 4: Displays/Visualizations** (Tests 4.1 - 4.4)
- [ ] **Category 5: Settings Management** (Tests 5.1 - 5.3)
- [ ] **Category 6: Edge Cases/Stress Tests** (Tests 6.1 - 6.5)

### **Post-Test:**
- [ ] All logs reviewed
- [ ] All test recordings validated
- [ ] Performance metrics recorded
- [ ] Issues logged (if any)
- [ ] Test report generated

---

## ?? **Test Report Template**

```markdown
# Test Execution Report
**Date:** [Date]
**Tester:** [Name]
**Build:** [Version]

## Summary
- **Tests Executed:** X / Y
- **Tests Passed:** X
- **Tests Failed:** X
- **Issues Found:** X

## Results by Category
### Category 1: Audio Input/Output
- Test 1.1: ? PASS / ? FAIL - [Notes]
- Test 1.2: ? PASS / ? FAIL - [Notes]
...

### Issues Found
1. **[Issue Title]**
   - Severity: Critical / High / Medium / Low
   - Description: [Details]
   - Steps to Reproduce: [Steps]
   - Expected: [Expected]
   - Actual: [Actual]

## Recommendation
- [ ] ? READY FOR DSP PHASE
- [ ] ?? MINOR ISSUES (can proceed with caution)
- [ ] ? BLOCKING ISSUES (must fix before DSP)
```

---

## ?? **Success Criteria**

### **READY FOR DSP PHASE if:**
- ? All **Category 1** tests pass (Audio Input/Output)
- ? All **Category 2** tests pass (Recording Functionality)
- ? 90%+ of **Category 3** tests pass (UI Controls)
- ? 90%+ of **Category 4** tests pass (Displays)
- ? All **Category 5** tests pass (Settings)
- ? No critical issues in **Category 6** (Edge Cases)

### **MINOR ISSUES ACCEPTABLE:**
- ?? Cosmetic UI issues
- ?? Non-critical edge cases
- ?? Performance optimizations

### **BLOCKING ISSUES:**
- ? Recording clicks/pops
- ? Crashes
- ? Data loss
- ? Settings not saving
- ? Driver failures

---

## ?? **Next Steps After Testing**

### **If All Tests Pass:**
1. Generate test report
2. Document any minor issues
3. Create "known issues" list
4. **BEGIN DSP IMPLEMENTATION! ??**

### **If Issues Found:**
1. Prioritize issues (Critical ? High ? Medium ? Low)
2. Fix critical/high issues first
3. Re-test affected areas
4. Repeat until success criteria met

---

## ?? **Related Documents**

- [Recording-Architecture-Final-2026-01-15.md](../Architecture/Recording-Architecture-Final-2026-01-15.md)
- [Timer-Inventory-And-Architecture.md](../Architecture/Timer-Inventory-And-Architecture.md)
- [Task-Phase3-Audio-Integration.md](../Tasks/Task-Phase3-Audio-Integration.md)

---

**Status:** ?? **READY TO EXECUTE**  
**Estimated Time:** 2-3 hours  
**Priority:** ? HIGH - Complete before DSP phase
