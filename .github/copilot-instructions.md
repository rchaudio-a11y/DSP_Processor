# Copilot Instructions - Main Index

**Project:** DSP_Processor  
**Current Version:** v1.3.1.3 - In Progress Testing

## Core Principles
1. State Machine Architecture
2. Event-Driven Design  
3. RDF Methodology
4. Documentation is Synthesis
5. Clear ownership boundaries for every subsystem. Each component should have exactly ONE owner for state and lifecycle. Prevent shared mutable state and ambiguous responsibility. Example: GlobalStateMachine owns global state, RecordingManager owns actions (not state), StateCoordinator owns coordination (not subsystems). No circular dependencies. Single source of truth pattern.

## Specialized Instructions

- [Documentation](instructions/documentation.md) - Templates, workflow
- [UI/Forms](instructions/ui.md) - Event wiring, controls
- [Architecture](instructions/architecture.md) - RDF phases, boundaries
- [Audio/DSP](instructions/audio.md) - Zero-copy, DSP pipeline
- [Debugging](instructions/debugging.md) - Phase 4 mindset
- [Versioning](instructions/versioning.md) - Task-aligned versions

## Quick Reference
**Methodology:** RDF - Documentation/Reference/RDF.md  
**Version Format:** v[Major].[Phase].[SubPhase].[Task]  
**Current Task:** 3.1.3 - Wire DSP UI

**Last Updated:** 2026-01-16
