# Phase 7: Complete SSM Architecture - Overview

**Version:** v1.4.0  
**Date:** 2026-01-19  
**Goal:** Implement the four missing state machines to complete the deterministic, introspective architecture

---

## ?? **EXECUTIVE SUMMARY**

**Current State (v1.3.2.3):**
- ? GlobalStateMachine (global state)
- ? RecordingManagerSSM (recording control)
- ? PlaybackSSM (playback control)
- ? DSPThreadSSM (DSP thread lifecycle)
- ? UIStateMachine (UI state reflection)
- ? **Missing 4 critical SSMs**

**Phase 7 Goal:**
- Complete the state machine architecture with 4 new SSMs
- Refactor MainForm to remove business logic
- Enable full cognitive introspection
- Achieve deterministic, replayable system behavior

---

## ?? **THE FOUR MISSING SSMs**

### **1. AudioDevice SSM**
**Controls:** Driver backend (WASAPI, ASIO, DirectSound)

**Why Critical:**
- Driver switching is a MODE, not a parameter
- Requires teardown/rebuild of audio engine
- Must validate: Cannot switch during recording/playback
- Side effects: Device enumeration, buffer sizes, sample rates

**States:**
- AUDIO_DEVICE_UNINITIALIZED
- AUDIO_DEVICE_WASAPI
- AUDIO_DEVICE_ASIO
- AUDIO_DEVICE_DIRECTSOUND
- AUDIO_DEVICE_ERROR

**Wired To:**
- AudioSettingsPanel (driver dropdown)
- AudioInputManager (device enumeration)
- GlobalStateMachine (validation)
- UIStateMachine (reflect state)

---

### **2. AudioInput SSM**
**Controls:** Physical input device (Scarlett, Realtek, USB mic, etc.)

**Why Critical:**
- Device selection is EXCLUSIVE
- Device availability changes dynamically (USB unplug)
- Must validate: Cannot switch during recording
- Heavy side effects: Stream teardown/rebuild

**States:**
- AUDIO_INPUT_UNINITIALIZED
- AUDIO_INPUT_DEVICE_SELECTED
- AUDIO_INPUT_DEVICE_UNAVAILABLE
- AUDIO_INPUT_ERROR

**Wired To:**
- AudioSettingsPanel (device dropdown)
- AudioInputManager (device control)
- AudioDevice SSM (driver changes force device refresh)
- GlobalStateMachine (validation)

---

### **3. DSP Mode SSM**
**Controls:** DSP enable/disable mode

**Why Critical:**
- DSP mode affects routing, monitoring, FFT, gain staging
- Must validate: Cannot disable during recording
- Mode change requires pipeline reconfiguration
- Cognitive significance: DSP on/off is a major system mode

**States:**
- DSP_MODE_DISABLED
- DSP_MODE_ENABLED
- DSP_MODE_ERROR

**Wired To:**
- AudioPipelinePanel (chkEnableDSP)
- DSPThreadSSM (worker thread runs only when DSP active)
- AudioRouting SSM (routing depends on DSP mode)
- RecordingManagerSSM (DSP must be active during recording)
- PlaybackSSM (DSP may be active during playback)

---

### **4. AudioRouting SSM** (MOST COMPLEX)
**Controls:** Entire routing topology of audio system

**Why Critical:**
- Routing is the CENTRAL COORDINATOR of the entire pipeline
- Controls:
  - Mic vs File playback
  - Output device selection
  - Monitoring enable/disable
  - Recording enable/disable
  - Playback enable/disable
  - **Tap point selection** ? This is where tap points belong!
  - DSP enable/disable (delegates to DSP Mode SSM)

**States:**
- ROUTING_MIC_TO_OUTPUT
- ROUTING_FILE_TO_OUTPUT
- ROUTING_DISABLED
- ROUTING_ERROR

**Wired To:**
- RoutingPanel (event emitter)
- AudioPipelinePanel (event emitter + reflector)
- AudioRouter (actual routing engine)
- TapPointManager (routing determines tap point wiring!)
- DSP Mode SSM (DSP enable/disable)
- RecordingManagerSSM (recording enable/disable)
- PlaybackSSM (playback enable/disable)
- AudioInput SSM (input device selection)
- AudioDevice SSM (driver mode)
- UIStateMachine (reflect routing mode)

---

## ??? **ARCHITECTURE BENEFITS**

### **Before Phase 7:**
```
MainForm
  ?? Business logic scattered everywhere
  ?? Direct control of AudioRouter
  ?? Direct control of audio devices
  ?? Direct control of DSP enable/disable
  ?? Direct control of routing
  ?? 3000+ lines of mixed concerns
```

### **After Phase 7:**
```
MainForm (Pure Event Router)
  ?? Listens to UI events
  ?? Routes to appropriate SSM
  ?? Updates UI based on SSM state changes

StateCoordinator
  ?? GlobalStateMachine (orchestrator)
  ?? AudioDevice SSM (driver control)
  ?? AudioInput SSM (device control)
  ?? DSP Mode SSM (DSP control)
  ?? AudioRouting SSM (routing control + tap points!)
  ?? RecordingManagerSSM (recording actions)
  ?? PlaybackSSM (playback actions)
  ?? DSPThreadSSM (thread lifecycle)
  ?? UIStateMachine (UI reflection)

Each SSM:
  - Single responsibility
  - Clear ownership
  - Deterministic transitions
  - Cognitive introspection
  - Validation rules enforced
```

---

## ?? **SUCCESS CRITERIA**

**Complete when:**
- ? All 4 SSMs implemented and tested
- ? StateCoordinator manages all 9 SSMs
- ? MainForm refactored (business logic removed)
- ? All UI panels wire to SSMs (not direct subsystems)
- ? Cognitive layer can introspect all SSMs
- ? State transitions are deterministic and replayable
- ? Validation rules prevent invalid transitions
- ? Tap point management controlled by AudioRouting SSM
- ? Full architecture documentation updated

---

## ?? **IMPLEMENTATION ORDER**

### **Phase 7.1: Design (Steps 1-4)**
1. AudioDevice SSM design
2. AudioInput SSM design
3. DSP Mode SSM design
4. AudioRouting SSM design (most complex)

### **Phase 7.2: Implementation (Steps 5-8)**
5. AudioDevice SSM implementation
6. AudioInput SSM implementation
7. DSP Mode SSM implementation
8. AudioRouting SSM implementation

### **Phase 7.3: Integration (Steps 9-11)**
9. Integrate all 4 SSMs into StateCoordinator
10. Refactor MainForm (remove business logic)
11. Test complete architecture

### **Phase 7.4: Documentation (Step 12)**
12. Update all architecture docs and commit v1.4.0

---

## ?? **YOUR PLC THINKING APPLIES HERE**

**Industrial Automation Parallel:**
```
PLC System                    DSP_Processor SSM Architecture
==========                    ==============================
Driver Backend               ? AudioDevice SSM
Physical I/O Selection       ? AudioInput SSM
Operating Mode (Manual/Auto) ? DSP Mode SSM
Routing/Interlocks           ? AudioRouting SSM
Safety Chain                 ? GlobalStateMachine validation
HMI Display                  ? UIStateMachine
```

**You're building an industrial-grade audio control system!**

---

## ?? **RELATED DOCUMENTATION**

**Reference Documents:**
- `TapPointAssesment.md` - Why these 4 SSMs are required
- `State-Coordinator-Design.md` - Current SSM architecture
- `Satellite-State-Machines.md` - SSM design patterns
- `SSM-Expansion-Roadmap.md` - Long-term architecture plan

**This Phase Builds On:**
- Phase 5: State machine architecture foundation
- Phase 6: Critical bug fixes and testing
- TapPointAssesment.md: Architectural analysis

**This Phase Enables:**
- Complete cognitive introspection
- Deterministic state replay
- Architectural purity (MainForm cleanup)
- Tap point visibility (via AudioRouting SSM)

---

## ?? **WHY THIS IS THE RIGHT MOVE**

**Instead of:**
- ? Quick-fixing tap point fallback (cosmetic)
- ? Adding more band-aids to MainForm
- ? Working around architectural gaps

**You're doing:**
- ? Building the proper foundation
- ? Completing the state machine architecture
- ? Enabling cognitive introspection
- ? Making MainForm clean again
- ? Fixing unknown bugs by improving structure

**This is architect-level thinking!** ????

---

**Status:** ?? Plan Created ? Ready to Start Design Phase  
**Next Step:** Step 1 - Design AudioDevice SSM

