# DSP Processor - Project Folder Structure

## Current Structure (After Phase 0)

```
C:\Users\rchau\source\repos\DSP_Processor\
?
??? DSP_Processor\                          # Main project folder
?   ?
?   ??? AudioIO\                            # Audio Input/Output Layer
?   ?   ??? IInputSource.vb                 ? Existing - Input abstraction
?   ?   ??? IOutputSink.vb                  ? Existing - Output abstraction
?   ?   ??? IAudioEngine.vb                 ?? NEW - Multi-driver interface
?   ?   ??? MicInputSource.vb               ? Existing - WaveIn implementation
?   ?   ??? WaveFileOutput.vb               ? Existing - WAV file writer
?   ?   ??? PlaybackEngine.vb               ?? NEW - Playback management
?   ?
?   ??? Recording\                          # Recording Management
?   ?   ??? RecordingEngine.vb              ? Existing - Recording lifecycle
?   ?
?   ??? Visualization\                      # Rendering & Display
?   ?   ??? IRenderer.vb                    ?? NEW - Renderer interface
?   ?   ??? WaveformRenderer.vb             ?? NEW - Waveform rendering
?   ?
?   ??? DSP\                                # Digital Signal Processing
?   ?   ??? IProcessor.vb                   ?? NEW - DSP processor interface
?   ?
?   ??? Utils\                              # Utilities & Helpers
?   ?   ??? Logger.vb                       ?? NEW - Logging system
?   ?   ??? PerformanceMonitor.vb           ?? NEW - Performance tracking
?   ?
?   ??? Documentation\                      # Project Documentation
?   ?   ??? Changelog\
?   ?   ?   ??? Phase-0-Changelog.md        ?? NEW - 5W&H changelog
?   ?   ?   ??? Phase-0-TaskList.md         ?? NEW - Task breakdown
?   ?   ?   ??? Phase-0-README.md           ?? NEW - Integration guide
?   ?   ?   ??? Phase-0-Summary.md          ?? NEW - Status summary
?   ?   ?   ??? Folder-Structure.md         ?? NEW - This file
?   ?   ??? Implementation-Plan.md          ? Existing - Master roadmap
?   ?   ??? Project Outline.md              ? Existing - Architecture doc
?   ?   ??? Project outline 2.md            ? Existing - DSP specs
?   ?
?   ??? Logs\                               # Runtime Logs (created at runtime)
?   ?   ??? DSP_Processor_YYYYMMDD_HHMMSS.log
?   ?
?   ??? Recordings\                         # Audio Recordings (created at runtime)
?   ?   ??? Take_001.wav
?   ?
?   ??? MainForm.vb                         ? TO REFACTOR - UI orchestration
?   ??? MainForm.Designer.vb                ? Existing - Form designer
?   ??? MainForm.resx                       ? Existing - Form resources
?   ??? ApplicationEvents.vb                ? Existing - App events
?   ??? DSP_Processor.vbproj                ? Existing - Project file
?
??? .git\                                   # Git Repository
    ??? ...

Legend:
? Existing file (already in project)
?? NEW file (created in Phase 0)
? TO REFACTOR (needs modification)
```

---

## File Statistics

### Production Code

| Category | Files | Lines | Status |
|----------|-------|-------|--------|
| **AudioIO** | 6 | ~600 | 2 new, 4 existing |
| **Recording** | 1 | ~80 | Existing |
| **Visualization** | 2 | ~290 | 2 new |
| **DSP** | 1 | ~50 | 1 new |
| **Utils** | 2 | ~550 | 2 new |
| **UI** | 3 | ~400 | Existing |
| **Total** | **15** | **~1,970** | **7 new, 8 existing** |

### Documentation

| File | Lines | Purpose |
|------|-------|---------|
| Phase-0-Changelog.md | ~600 | 5W&H changelog template |
| Phase-0-TaskList.md | ~800 | Detailed task specs |
| Phase-0-README.md | ~600 | Integration guide |
| Phase-0-Summary.md | ~400 | Status summary |
| Folder-Structure.md | ~200 | This file |
| Implementation-Plan.md | ~2,000 | Master roadmap |
| Project Outline.md | ~500 | Architecture |
| Project outline 2.md | ~400 | DSP specs |
| **Total** | **~5,500** | **8 documents** |

---

## Phase 1 Structure (Preview)

After Phase 1, the structure will expand to:

```
DSP_Processor\
?
??? AudioIO\
?   ??? ...existing files...
?   ??? AudioInputManager.vb           # Phase 1 - Driver management
?   ??? DeviceInfo.vb                  # Phase 1 - Device metadata
?   ??? DriverType.vb                  # Phase 1 - Enum
?   ??? WasapiInputSource.vb           # Phase 1 - WASAPI implementation
?   ??? WasapiCapabilities.vb          # Phase 1 - WASAPI features
?   ??? AsioInputSource.vb             # Phase 1 - ASIO implementation (optional)
?   ??? DeviceCapabilities.vb          # Phase 1 - Capability detection
?   ??? DeviceProber.vb                # Phase 1 - Device probing
?   ??? ChannelRouter.vb               # Phase 1 - Advanced routing
?   ??? ...
?
??? UI\                                # Phase 1 - Separate UI folder
?   ??? MainForm.vb
?   ??? MainForm.Designer.vb
?   ??? ChannelMatrixControl.vb        # Phase 1 - Routing UI
?   ??? ...
?
??? Tests\                             # Phase 1 - Unit tests
    ??? DSP_Processor.Tests\
        ??? AudioIO\
        ?   ??? MicInputSourceTests.vb
        ?   ??? WavFileOutputTests.vb
        ?   ??? PlaybackEngineTests.vb
        ??? Recording\
        ?   ??? RecordingEngineTests.vb
        ??? ...
```

---

## Phase 2 Structure (Preview)

After Phase 2, DSP modules will be added:

```
DSP_Processor\
?
??? DSP\
?   ??? IProcessor.vb                  ? Already exists (Phase 0)
?   ??? ProcessorBase.vb               # Phase 2
?   ??? ProcessorChain.vb              # Phase 2
?   ??? AudioBuffer.vb                 # Phase 2
?   ??? MultibandEngine.vb             # Phase 2
?   ?
?   ??? Filters\                       # Phase 2 - Filter modules
?   ?   ??? BiquadFilter.vb
?   ?   ??? FilterType.vb
?   ?   ??? FilterCalculator.vb
?   ?   ??? PreFilterBank.vb
?   ?   ??? PostFilterBank.vb
?   ?
?   ??? Crossover\                     # Phase 2 - Crossover system
?   ?   ??? CrossoverFilterBank.vb
?   ?   ??? CrossoverPoint.vb
?   ?   ??? BandDefinition.vb
?   ?
?   ??? EQ\                            # Phase 2 - Parametric EQ
?   ?   ??? ParametricEQ.vb
?   ?   ??? EQBand.vb
?   ?
?   ??? Dynamics\                      # Phase 2 - Dynamics processing
?   ?   ??? DynamicsProcessor.vb
?   ?   ??? Compressor.vb
?   ?   ??? Gate.vb
?   ?   ??? Limiter.vb
?   ?   ??? EnvelopeFollower.vb
?   ?
?   ??? Multiband\                     # Phase 2 - Multiband engine
?   ?   ??? BandProcessor.vb
?   ?   ??? BandMixer.vb
?   ?   ??? BandState.vb
?   ?
?   ??? FFT\                           # Phase 2 - FFT processing
?       ??? FFTProcessor.vb
?
??? ...
```

---

## Naming Conventions

### Files
- **PascalCase** for all file names
- **Interfaces:** Start with `I` (e.g., `IProcessor.vb`)
- **Base classes:** End with `Base` (e.g., `ProcessorBase.vb`)
- **Implementations:** Descriptive names (e.g., `WasapiInputSource.vb`)

### Folders
- **PascalCase** for folder names
- **Plural nouns** for component folders (e.g., `Filters\`, `Dynamics\`)
- **Singular nouns** for functional folders (e.g., `AudioIO\`, `Recording\`)

### Namespaces
- **Root:** `DSP_Processor`
- **Structure:** Mirrors folder structure
  - `DSP_Processor.AudioIO`
  - `DSP_Processor.DSP.Filters`
  - `DSP_Processor.Utils`

---

## Git Ignore Recommendations

### Add to .gitignore
```gitignore
# Logs folder (generated at runtime)
Logs/

# Recordings folder (user data)
Recordings/

# Visual Studio
.vs/
bin/
obj/
*.user
*.suo

# Build artifacts
*.exe
*.dll
*.pdb

# Test results
TestResults/
```

### Keep in Git
```
# Documentation
Documentation/

# Source code
*.vb
*.vbproj
*.sln

# Project files
*.resx
*.Designer.vb
```

---

## Folder Size Estimates

### Current (Phase 0)
- **Production Code:** ~1,970 lines ? ~60 KB
- **Documentation:** ~5,500 lines ? ~200 KB
- **Total:** ~260 KB

### After Phase 1
- **Production Code:** ~3,500 lines ? ~110 KB
- **Tests:** ~1,500 lines ? ~50 KB
- **Documentation:** ~8,000 lines ? ~280 KB
- **Total:** ~440 KB

### After Phase 2
- **Production Code:** ~8,000 lines ? ~250 KB
- **Tests:** ~3,000 lines ? ~100 KB
- **Documentation:** ~10,000 lines ? ~350 KB
- **Total:** ~700 KB

### After Phase 4 (Complete)
- **Production Code:** ~15,000 lines ? ~500 KB
- **Tests:** ~5,000 lines ? ~170 KB
- **Documentation:** ~12,000 lines ? ~400 KB
- **Total:** ~1.1 MB

---

## Backup Strategy

### Recommended Approach
1. **Git commits** - After each task completion
2. **Git tags** - After each phase completion
3. **Remote backup** - GitHub repository (already set up)

### Tag Naming
```bash
# Phase tags
phase-0-complete
phase-1-complete
phase-2-complete
phase-3-complete
phase-4-complete

# Version tags (once stable)
v1.0.0
v1.1.0
v2.0.0
```

### Branch Strategy
```
master (main)        # Stable releases
  ??? develop        # Integration branch
      ??? feature/phase-1-wasapi
      ??? feature/phase-2-dsp
      ??? feature/phase-3-ui
      ??? feature/phase-4-project
```

---

## Navigation Tips

### Quick File Access (VS)
- **Ctrl+,** - Go to file
- **Ctrl+T** - Go to type
- **Ctrl+Shift+T** - Go to symbol
- **F12** - Go to definition

### Recommended VS Folders Order
```
Solution 'DSP_Processor'
?? DSP_Processor
?  ?? AudioIO
?  ?? Recording
?  ?? Visualization
?  ?? DSP
?  ?? Utils
?  ?? UI (MainForm)
?  ?? Documentation
?? DSP_Processor.Tests (Phase 1)
```

---

## Summary

### ? Phase 0 Complete
- 7 new production files
- 5 documentation files
- Clean folder structure
- Ready for Phase 1

### ?? Code Organization
- Clear separation of concerns
- Logical folder hierarchy
- Consistent naming conventions
- Scalable architecture

### ?? Next Steps
1. Complete Task 0.1.3 (MainForm refactoring)
2. Commit and tag Phase 0
3. Review Phase 1 structure
4. Begin WASAPI implementation

---

**Document Version:** 1.0  
**Created:** 2024  
**Purpose:** Project structure reference

**END OF DOCUMENT**
