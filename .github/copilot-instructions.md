# Copilot Instructions

## General Guidelines
- Always implement a State Machine to centralize control of applications. State machines should be the single source of truth for application state and control flow.
- Prefer event-driven architecture over timer-driven logic, especially for audio and state updates. Use events and callbacks instead of polling timers whenever possible.
- Call `detect_memories` whenever a preference is stated or when asked to remember something, to utilize the memory popup feature for saving to Copilot instructions. Always trigger the memory detection UI rather than manually noting preferences.

## Documentation Preferences
- Use the `Documentation\Templates\` folder for all documentation templates
- Template naming convention: `[Type]-v[Major]_[Minor]_[Patch].md` (e.g., `Task-v1_0_0.md`)
- Current template version: v1.0.0
- Follow the RDF-aligned folder structure:
  - **Templates/**: All document templates (versioned files)
  - **Architecture/**: System architecture and design decisions (timeless, all maturity levels)
  - **Active/**: Current work - Tasks.md (single file), Issues.md (single file), Sessions/ (dated files)
  - **Completed/**: Finished work - Features/, Tasks/, Issues/ (archived documentation)
  - **Reference/**: Timeless reference material (RDF.md, coding standards, etc.)
  - **Changelog/**: Version history - CURRENT.md (active), Archive/ (historical)
- When creating new documentation:
  1. Start with appropriate versioned template from `Templates/`
  2. Fill out all sections completely
  3. Save to proper folder (Active/ for work-in-progress, Architecture/ for design, etc.)
  4. Update relevant index files (Active/Tasks.md, Active/Issues.md, Changelog/CURRENT.md)
- When completing work:
  1. Move from `Active/` to `Completed/`
  2. Create completion document in `Completed/Features/`
  3. Update `Changelog/CURRENT.md`
- After each plan phase:
  1. Create session notes using `Session-Notes-v1_0_0.md` template
  2. Update Changelog with phase completion
  3. Maintain version tracking for all documentation changes

## User Preferences
- Implement DoubleClick event handlers for all TrackBar controls. Users can double-click on slider controls to reset them to default values: Volume/Gain sliders default to 100%, Pan sliders default to 50% (center), and other controls should have sensible defaults.
- Prefer the Rick Development Framework (RDF) methodology over Agile, Scrum, or Kanban. RDF is a recursive, architecture-driven approach that treats debugging as creative exploration, values deep understanding over velocity, and focuses on building systems that evolve rather than degrade. Documentation can be found at Documentation\Reference\RDF.md.