# Audio Pipeline Routing

**Purpose:** Centralized audio routing and pipeline control  
**Created:** January 15, 2026  
**Status:** ?? Under Development

---

## Overview

This namespace contains the core routing infrastructure for DSP_Processor's audio pipeline.

**Key Components:**
- `AudioPipelineRouter` - Central routing controller
- `PipelineConfiguration` - Configuration classes for all routing options
- `PipelineConfigurationManager` - JSON save/load with auto-save
- `PipelineTemplateManager` - User templates and presets

---

## Architecture

```
AudioPipelineRouter (Central Authority)
    ?
Defines ALL routing:
??? Source Selection (Mic, File, Line In)
??? Processing Path (Bypass, DSP, Effects)
??? Monitoring Points (FFT Pre, FFT Post, Meters)
??? Destinations (Recording, Playback, Both)
```

---

## Configuration

**JSON Auto-Save:** All configuration changes automatically persist  
**Load Last State:** Restores previous configuration on startup  
**Templates:** Built-in presets + user-defined templates

**Configuration Files:**
- `Settings\AudioPipeline.json` - Active configuration (auto-saved)
- `Settings\AudioPipelineTemplates.json` - User templates
- `Settings\AudioPipelineDefaults.json` - Factory defaults

---

## Usage Example

```vb
' Initialize router
Dim router = New AudioPipelineRouter()
router.Initialize()  ' Loads last saved configuration

' Change configuration
Dim config = New PipelineConfiguration With {
    .Processing = New ProcessingConfiguration With {
        .EnableDSP = True
    },
    .Monitoring = New MonitoringConfiguration With {
        .EnableInputFFT = True,
        .InputFFTTap = TapPoint.PreDSP
    }
}

router.UpdateRouting(config)  ' Auto-saves to JSON

' Subscribe to events
AddHandler router.BufferForRecording, AddressOf OnRecordingBuffer
AddHandler router.BufferForMonitoring, AddressOf OnMonitoringBuffer

' Route audio
router.RouteAudioBuffer(audioBuffer, AudioSourceType.Microphone)
```

---

## Integration Status

- [x] Phase 1: Foundation (Router classes, JSON manager)
- [ ] Phase 2: UI Controls (AudioPipelinePanel)
- [ ] Phase 3: Integration (Wire to existing code)
- [ ] Phase 4: Advanced Features (Undo/Redo, visualization)

---

## Files

| File | Purpose | Status |
|------|---------|--------|
| `AudioPipelineRouter.vb` | Central routing controller | ?? In Progress |
| `PipelineConfiguration.vb` | Configuration classes | ?? In Progress |
| `PipelineConfigurationManager.vb` | JSON persistence | ?? In Progress |
| `PipelineTemplateManager.vb` | Template management | ?? In Progress |
| `RoutingEventArgs.vb` | Event arguments | ?? In Progress |

---

## Design Principles

1. **Centralized Control** - Single source of truth for routing
2. **Configuration-Driven** - All routing controlled by JSON config
3. **Event-Based** - Components subscribe to router events
4. **Auto-Save** - Changes persist immediately (500ms throttle)
5. **Flexible** - Easy to add new sources/processors/destinations

---

## Related Documentation

- [Architecture Overview](../../Documentation/Architecture/AudioPipelineRouter-Overall-Architecture.md)
- [Complete Task List](../../Documentation/Tasks/Task-List-AudioPipelineRouter-Complete.md)
- [Current vs Intended Flow](../../Documentation/Architecture/Audio-Pipeline-Current-vs-Intended.md)

---

**Last Updated:** January 15, 2026  
**Maintainer:** DSP_Processor Development Team
