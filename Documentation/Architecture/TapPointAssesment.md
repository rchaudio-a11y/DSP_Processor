\- Recording SSM  

\- Playback SSM  

\- DSPThread SSM  

\- UI SSM  

\- \*\*Tap‑Point Architecture (cross‑cutting subsystem)\*\*  



And from your earlier panels + this tap‑point analysis, we can now produce a \*\*clean, authoritative report\*\* on the \*\*four new SSMs\*\* your system still needs, \*why\* they are required, and \*what they wire into\*.



Below is the full report, written in the same style as your architecture docs.



---



\# \*\*📘 SYSTEM ARCHITECTURE REPORT — The Four Required New SSMs\*\*  

\### \*AudioDevice SSM • AudioInput SSM • AudioRouting SSM • DSP Mode SSM\*



These SSMs complete your deterministic, introspective Step‑24 architecture.  

Each one governs a \*\*modeful subsystem\*\*—a subsystem with exclusive states, validation rules, and side‑effects that must be controlled by a state machine.



They are not UI state machines.  

They are not parameter managers.  

They are \*\*core subsystem controllers\*\*.



---



\# \*\*1. AUDIODEVICE SSM\*\*  

\### \*\*Purpose\*\*  

Controls the \*\*audio driver backend\*\* (WASAPI, ASIO, DirectSound).



\### \*\*Why It Must Exist\*\*  

Driver selection is a \*\*mode\*\*, not a parameter.



Switching drivers requires:



\- Tearing down the audio engine  

\- Reinitializing device lists  

\- Rebuilding buffer sizes  

\- Reapplying defaults  

\- Reconfiguring sample rates  

\- Reconfiguring latency  



And it must be \*\*validated\*\*:



\- Cannot switch drivers while recording  

\- Cannot switch drivers while playing  

\- Cannot switch drivers while DSPThread is running  



This is textbook SSM territory.



\### \*\*Wired To\*\*  

\- \*\*AudioSettingsPanel\*\* (driver dropdown → event emitter)  

\- \*\*AudioInputManager\*\* (device enumeration depends on driver)  

\- \*\*AudioDeviceSettings\*\* (default parameters)  

\- \*\*GlobalStateMachine\*\* (validation: no switching during active modes)  

\- \*\*UIStateMachine\*\* (reflect driver mode)



\### \*\*State Examples\*\*  

\- `AUDIO\_DEVICE\_UNINITIALIZED`  

\- `AUDIO\_DEVICE\_WASAPI`  

\- `AUDIO\_DEVICE\_ASIO`  

\- `AUDIO\_DEVICE\_DIRECTSOUND`  

\- `AUDIO\_DEVICE\_ERROR`



---



\# \*\*2. AUDIOINPUT SSM\*\*  

\### \*\*Purpose\*\*  

Controls the \*\*physical input device\*\* (Scarlett, Realtek, USB mic, etc.).



\### \*\*Why It Must Exist\*\*  

Input device selection is \*\*exclusive\*\* and has heavy side effects:



\- Device change requires stream teardown  

\- Device availability can change dynamically (USB unplugged)  

\- Device selection must be validated (cannot switch during recording)  

\- Device transitions must be logged and replayable  



This is a modeful subsystem.



\### \*\*Wired To\*\*  

\- \*\*AudioSettingsPanel\*\* (device dropdown → event emitter)  

\- \*\*AudioInputManager\*\* (device enumeration + activation)  

\- \*\*AudioDevice SSM\*\* (driver changes force device refresh)  

\- \*\*GlobalStateMachine\*\* (validation: cannot switch during recording)  

\- \*\*UIStateMachine\*\* (reflect device availability)



\### \*\*State Examples\*\*  

\- `AUDIO\_INPUT\_UNINITIALIZED`  

\- `AUDIO\_INPUT\_DEVICE\_SELECTED`  

\- `AUDIO\_INPUT\_DEVICE\_UNAVAILABLE`  

\- `AUDIO\_INPUT\_ERROR`



---



\# \*\*3. AUDIOROUTING SSM\*\*  

\### \*\*Purpose\*\*  

Controls the \*\*routing topology\*\* of the entire audio system:



\- Microphone vs FilePlayback  

\- Output device selection  

\- Monitoring enable/disable  

\- Recording enable/disable  

\- Playback enable/disable  

\- Tap point selection  

\- DSP enable/disable (delegated to DSP Mode SSM)



\### \*\*Why It Must Exist\*\*  

Routing is a \*\*modeful subsystem\*\* with:



\- Exclusive states  

\- Validation rules  

\- Cross‑subsystem dependencies  

\- Side effects across DSP, recording, playback, meters, FFT  

\- Cognitive significance  



Your Tap‑Point Architecture Analysis confirms this:  

\*\*Routing is the central coordinator of the entire audio pipeline.\*\*



\### \*\*Wired To\*\*  

\- \*\*RoutingPanel\*\* (event emitter)  

\- \*\*AudioPipelinePanel\*\* (event emitter + reflector)  

\- \*\*AudioRouter\*\* (actual routing engine)  

\- \*\*TapPointManager\*\* (routing determines tap point wiring)  

\- \*\*DSP Mode SSM\*\* (DSP enable/disable)  

\- \*\*REC SSM\*\* (recording enable/disable)  

\- \*\*PLAY SSM\*\* (playback enable/disable)  

\- \*\*AudioInput SSM\*\* (input device selection)  

\- \*\*AudioDevice SSM\*\* (driver mode)  

\- \*\*UIStateMachine\*\* (reflect routing mode)



\### \*\*State Examples\*\*  

\- `ROUTING\_MIC\_TO\_OUTPUT`  

\- `ROUTING\_FILE\_TO\_OUTPUT`  

\- `ROUTING\_DISABLED`  

\- `ROUTING\_ERROR`



---



\# \*\*4. DSP MODE SSM\*\*  

\### \*\*Purpose\*\*  

Controls the \*\*DSP enable/disable mode\*\* (not the DSP thread — that’s DSPThreadSSM, which you already have).



\### \*\*Why It Must Exist\*\*  

DSP enable/disable is a \*\*mode\*\*, not a parameter.



DSP mode affects:



\- Routing  

\- Monitoring  

\- FFT  

\- Gain staging  

\- Recording  

\- Playback  

\- Tap point availability  



And must be validated:



\- Cannot disable DSP while recording  

\- Cannot disable DSP while DSPThread is running  

\- Cannot enable DSP if routing is invalid  



\### \*\*Wired To\*\*  

\- \*\*AudioPipelinePanel\*\* (`chkEnableDSP`)  

\- \*\*DSPThreadSSM\*\* (worker thread runs only when DSP is active)  

\- \*\*AudioRouting SSM\*\* (routing depends on DSP mode)  

\- \*\*RecordingManagerSSM\*\* (DSP must be active during recording)  

\- \*\*PlaybackSSM\*\* (DSP may be active during playback)  

\- \*\*UIStateMachine\*\* (reflect DSP mode)



\### \*\*State Examples\*\*  

\- `DSP\_MODE\_DISABLED`  

\- `DSP\_MODE\_ENABLED`  

\- `DSP\_MODE\_ERROR`



---



\# \*\*📘 Summary Table — The Four New SSMs\*\*



| SSM | Purpose | Why Needed | Wired To |

|-----|---------|------------|----------|

| \*\*AudioDevice SSM\*\* | Controls driver backend | Exclusive modes, heavy side effects | AudioSettingsPanel, AudioInputManager, GlobalStateMachine |

| \*\*AudioInput SSM\*\* | Controls physical input device | Exclusive device selection, validation | AudioSettingsPanel, AudioInputManager, AudioDevice SSM |

| \*\*AudioRouting SSM\*\* | Controls routing topology | Complex modeful subsystem | RoutingPanel, AudioPipelinePanel, DSP/REC/PLAY SSMs |

| \*\*DSP Mode SSM\*\* | Controls DSP enable/disable | DSP is a mode, not a parameter | AudioPipelinePanel, DSPThreadSSM, REC/PLAY SSMs |



---

&nbsp; 

With these four SSMs added, your architecture becomes:



\- Fully deterministic  

\- Fully introspective  

\- Fully replayable  

\- Fully modular  

\- Fully hierarchical  

\- Fully cognitive‑layer compatible  





