# **📘 SYSTEM ARCHITECTURE REPORT — The Four New Required SSMs**  
### *AudioDevice SSM • AudioInput SSM • AudioRouting SSM • DSP Mode SSM*

These four SSMs complete your Step‑24‑aligned, deterministic, introspective architecture.  
Each one governs a **modeful subsystem** — meaning a subsystem with **exclusive states**, **validation rules**, and **side‑effects** that must be controlled deterministically.

They are not UI state machines.  
They are not parameter managers.  
They are **core subsystem controllers**.

---

# **1. AUDIODEVICE SSM**  
### **Purpose**  
Controls the **audio driver backend** (WASAPI, ASIO, DirectSound).  
This is a *modeful* subsystem with exclusive states and heavy side effects.

### **Why It Must Exist**  
- Driver selection is **exclusive** (you can only use one backend at a time).  
- Switching drivers requires **teardown + reinitialization** of the entire audio engine.  
- Driver changes affect **device enumeration**, **buffer sizes**, **sample rates**, and **latency**.  
- Driver changes must be **validated** (cannot switch during recording or playback).  
- Driver transitions must be **logged**, **replayable**, and **cognitively visible**.

### **Wired To**  
- **AudioSettingsPanel** (event emitter)  
- **AudioInputManager** (device enumeration)  
- **AudioDeviceSettings** (default parameters)  
- **GlobalStateMachine** (validation: cannot switch during recording/playback)  
- **UIStateMachine** (reflect driver mode)

### **Example States**  
- `AUDIO_DEVICE_IDLE`  
- `AUDIO_DEVICE_WASAPI`  
- `AUDIO_DEVICE_ASIO`  
- `AUDIO_DEVICE_DIRECTSOUND`  
- `AUDIO_DEVICE_ERROR`

---

# **2. AUDIOINPUT SSM**  
### **Purpose**  
Controls the **physical input device** (Scarlett, Realtek, USB mic, etc.).  
This is a modeful subsystem because only **one device** can be active at a time.

### **Why It Must Exist**  
- Device selection is **exclusive**.  
- Device changes require **stream teardown + reinit**.  
- Device availability can change dynamically (USB unplugged).  
- Device selection must be **validated** (cannot switch during recording).  
- Device transitions must be **logged**, **replayable**, and **cognitively visible**.

### **Wired To**  
- **AudioSettingsPanel** (device dropdown)  
- **AudioInputManager** (device enumeration + activation)  
- **AudioDevice SSM** (driver changes trigger device refresh)  
- **GlobalStateMachine** (validation: cannot switch during recording)  
- **UIStateMachine** (reflect device availability)

### **Example States**  
- `AUDIO_INPUT_UNINITIALIZED`  
- `AUDIO_INPUT_DEVICE_SELECTED`  
- `AUDIO_INPUT_DEVICE_UNAVAILABLE`  
- `AUDIO_INPUT_ERROR`

---

# **3. AUDIOROUTING SSM**  
### **Purpose**  
Controls the **routing topology** of the entire audio system:

- Input source (Microphone vs FilePlayback)  
- Output device selection  
- Monitoring enable/disable  
- Recording enable/disable  
- Playback enable/disable  
- Tap point selection  
- DSP enable/disable (delegated to DSP SSM)

### **Why It Must Exist**  
Routing is a **modeful subsystem** with:

- Exclusive states  
- Validation rules  
- Side effects  
- Cross‑subsystem dependencies  
- Cognitive significance  

Routing changes affect:

- DSP pipeline  
- RecordingManager  
- Playback engine  
- Meters  
- FFT  
- File readers  
- Device activation  

This is too important to leave as UI logic.

### **Wired To**  
- **RoutingPanel** (event emitter)  
- **AudioPipelinePanel** (event emitter + reflector)  
- **AudioRouter** (actual routing engine)  
- **DSP SSM** (DSP enable/disable)  
- **REC SSM** (recording enable/disable)  
- **PLAY SSM** (playback enable/disable)  
- **AudioInput SSM** (input device selection)  
- **AudioDevice SSM** (driver mode)  
- **UIStateMachine** (reflect routing mode)

### **Example States**  
- `ROUTING_MIC_TO_OUTPUT`  
- `ROUTING_FILE_TO_OUTPUT`  
- `ROUTING_ERROR`  

(Actual state list will be more detailed.)

---

# **4. DSP MODE SSM**  
### **Purpose**  
Controls the **DSP enable/disable mode** — not the DSP thread (you already have DSPThreadSSM), but the **DSP processing pipeline mode**.

### **Why It Must Exist**  
- DSP enable/disable is an **exclusive mode**.  
- DSP mode affects:  
  - Routing  
  - Monitoring  
  - FFT  
  - Gain staging  
  - Recording  
  - Playback  
- DSP mode must be **validated** (cannot disable DSP mid‑recording).  
- DSP mode transitions must be **logged**, **replayable**, and **cognitively visible**.

### **Wired To**  
- **AudioPipelinePanel** (`chkEnableDSP`)  
- **DSPThreadSSM** (worker thread runs only when DSP is active)  
- **AudioRouting SSM** (routing depends on DSP mode)  
- **RecordingManagerSSM** (DSP must be active during recording)  
- **PlaybackSSM** (DSP may be active during playback)  
- **UIStateMachine** (reflect DSP mode)

### **Example States**  
- `DSP_MODE_DISABLED`  
- `DSP_MODE_ENABLED`  
- `DSP_MODE_ERROR`

---

# **📘 Summary Table — The Four New SSMs**

| SSM | Purpose | Why Needed | Wired To |
|-----|---------|------------|----------|
| **AudioDevice SSM** | Controls driver backend | Exclusive modes, heavy side effects | AudioSettingsPanel, AudioInputManager, GlobalStateMachine |
| **AudioInput SSM** | Controls physical input device | Exclusive device selection, validation | AudioSettingsPanel, AudioInputManager, AudioDevice SSM |
| **AudioRouting SSM** | Controls routing topology | Complex modeful subsystem | RoutingPanel, AudioPipelinePanel, DSP/REC/PLAY SSMs |
| **DSP Mode SSM** | Controls DSP enable/disable | DSP is a mode, not a parameter | AudioPipelinePanel, DSPThreadSSM, REC/PLAY SSMs |



