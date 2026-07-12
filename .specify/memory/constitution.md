<!--
Sync Impact Report
==================
Version change: 1.0.0 → 1.1.0
Modified principles:
  - V. Cross-Thread Safety by Construction — UI marshaling tightened: BeginInvoke
    (non-blocking) required; blocking Invoke prohibited from any path that can
    hold a state-machine lock
  - VI. Designer-Declared UI → Designer-First UI — added two named carve-outs:
    owner-paint custom controls and explicitly-declared dynamic control sections
Modified sections:
  - Audio & Performance Constraints — internal format changed from 16-bit PCM to
    float32 end-to-end target; integer PCM confined to I/O boundaries; current
    Int16 pipeline marked legacy (migration: feature 001-float32-pipeline)
Added sections: none
Removed sections: none
Templates:
  - .specify/templates/plan-template.md ✅ compatible (Constitution Check gate is
    dynamic — "[Gates determined based on constitution file]"; no edits required)
  - .specify/templates/spec-template.md ✅ compatible (no constitution-specific
    mandatory sections added)
  - .specify/templates/tasks-template.md ✅ compatible (task categories align with
    principle-driven work: state machines, audio path, UI, docs, versioning)
Follow-up TODOs:
  - ⚠ .github/instructions/audio.md still states "All audio is 16-bit PCM
    internally" — update to match float32 target (per Governance, constitution wins)
  - ⚠ .github/instructions/ui.md thread-safety example uses blocking-capable
    pattern — align with BeginInvoke-only rule
Source material synthesized from:
  - .github/copilot-instructions.md
  - .github/instructions/architecture.md, audio.md, ui.md, debugging.md, versioning.md
-->

# DSP_Processor Constitution

## Core Principles

### I. Single Ownership Per Subsystem

Every component MUST have exactly one owner for its state and lifecycle. Shared
mutable state and ambiguous responsibility are prohibited.

- GlobalStateMachine owns global state; it owns nothing else.
- Each Subsystem State Machine (SSM) owns its subsystem's state exclusively
  (AudioDeviceSSM, AudioInputSSM, AudioRoutingSSM, DSPModeSSM, RecordingManagerSSM).
- RecordingManager owns actions, not state. StateCoordinator owns coordination,
  not subsystems. DSPThread owns all audio processing. RecordingManager owns the
  microphone lifecycle.
- Every piece of state MUST have a single source of truth; duplicating state
  across components is a violation, not a convenience.

**Rationale**: Ambiguous ownership is the root cause of the hardest bug class in
this codebase (state drift, double-teardown, race conditions). One owner per
subsystem makes every state question answerable by reading one file.

### II. State Machine Architecture (GSM + SSMs)

All application and subsystem state MUST be modeled as explicit state machines.

- The GlobalStateMachine (GSM) governs application-level state; SSMs govern
  subsystem state. New subsystems with lifecycle state MUST get an SSM.
- State transitions MUST be explicit, named, and validated — no ad-hoc boolean
  flag combinations standing in for states.
- UI MUST be updated from state (state-driven rendering), never the reverse:
  controls reflect the machine, they do not define it.
- MainForm and other UI hosts MUST contain no business logic — they route events
  to state machines and render state changes.

**Rationale**: Explicit state machines make illegal states unrepresentable,
keep UI consistent, and give every transition a single auditable code path.

### III. No Circular Dependencies

Dependency edges between components MUST form a directed acyclic graph.

- If component A depends on B, B MUST NOT depend on A, directly or transitively.
- Upward communication MUST use events; downward communication MUST use direct
  calls. A lower layer never references a higher layer by type.
- Any change that would introduce a cycle MUST instead introduce an event,
  interface, or coordinator that breaks the cycle.

**Rationale**: Cycles destroy the ability to reason about initialization order,
teardown order, and ownership — all load-bearing in a real-time audio app.

### IV. Real-Time Audio Discipline (NON-NEGOTIABLE)

The audio path is latency-critical and MUST obey hard real-time rules.

- Zero allocations in audio-path hot loops: no `New`, no LINQ, no string
  formatting, no boxing inside audio callbacks or DSP processing loops. Buffers
  are pre-allocated and reused.
- Audio callbacks MUST complete in under 10 ms (hard limit); buffer processing
  SHOULD complete in under 5 ms; tap-point writes MUST be effectively instant
  (< 1 ms).
- The audio thread MUST NOT: lock for extended periods, call UI methods,
  perform I/O, or log excessively. It MAY: process audio, write ring buffers,
  update atomic variables, and read cached settings.
- Zero-copy is the required pattern for tap points: multi-reader ring buffers
  with independent per-reader cursors, never buffer duplication. Ring buffers
  MUST never overwrite unread data.
- Audio data flow MUST be event-driven. Polling audio data with timers is
  prohibited (timers are acceptable only for UI animation, timeout detection,
  and non-critical periodic cleanup).

**Rationale**: A single allocation or lock in the hot loop can trigger GC pauses
and audible dropouts; these rules are physics, not preference.

### V. Cross-Thread Safety by Construction

All cross-thread communication MUST use explicit, correct synchronization.

- Every flag or counter shared across threads MUST use `Interlocked` operations
  or `Volatile` reads/writes — plain field access across threads is prohibited.
- The UI thread MUST never touch audio buffers directly; it reads via ring-buffer
  readers only. The audio thread MUST never touch UI controls.
- UI updates originating off the UI thread MUST marshal via `BeginInvoke`
  (non-blocking); blocking `Invoke` is prohibited from any path that can hold a
  state-machine lock.
- Threading assumptions MUST be documented at the component boundary where they
  apply.

**Rationale**: "Cross-thread operation" exceptions and silent memory-visibility
bugs are the two recurring failure modes at the UI/audio boundary; both are
eliminated by construction, not by testing.

### VI. Designer-First UI

UI controls MUST be declared in the WinForms Designer; code only initializes.
Exactly two carve-outs are permitted:

- **(a) Owner-paint custom controls**: VolumeMeterControl,
  SpectrumDisplayControl, and WaveformDisplayControl may be constructed and
  managed in code where owner-drawing requires it.
- **(b) Explicitly-declared dynamic control sections**: regions documented in
  the owning form/panel as dynamic (e.g., controls generated from runtime data)
  may construct controls in code; the section boundary MUST be declared, not
  implicit.
- Everything else lives in `*.Designer.vb` as `Friend WithEvents` declarations;
  runtime code configures values and wires behavior but MUST NOT construct
  controls.
- Event handlers for Designer controls use the `Handles` keyword; `AddHandler`
  is reserved for non-control event sources (state machines, audio engine,
  processors) and for carve-out controls constructed in code. Never use
  `Handles` on a non-`WithEvents` reference — it fails silently.
- Control naming MUST follow the established prefixes (`btn`, `lbl`, `txt`,
  `cmb`, `chk`, `track`, `panel`, `grp`, `lst`, `meter`) with descriptive names.
- All TrackBar sliders MUST support double-click reset to their documented
  default (gain/volume: unity; pan: center).

**Rationale**: A single declaration surface keeps the Designer usable, makes
event wiring auditable, and eliminates the silent-failure class caused by
mixing declarative and programmatic UI construction.

### VII. RDF Methodology

All work MUST follow the Recursive Development Framework
(`Documentation/Reference/RDF.md`): Curiosity Ignition → Insight Bloom → Build
Momentum → Recursive Debugging → Validation Loop → Synthesis → Recursion.

- Architecture work (Phase 2) MUST document system boundaries and invariants
  before implementation begins; invariants are sacred and violations are
  architecture bugs.
- Bugs are teachers (Phase 4): every non-trivial bug gets root-cause analysis
  and a documented architectural insight — quick fixes without understanding
  are prohibited. Debugging stops only when the root cause is understood, the
  lesson is documented, and a regression test exists.
- Documentation is synthesis, not overhead: task, issue, and architecture docs
  use the established templates and live in `Documentation/` (Active →
  Completed as work finishes).

**Rationale**: The project's velocity comes from compounding understanding, not
from shipping fast; RDF is how each loop starts stronger than the last.

### VIII. Task-Aligned Versioning

Versions MUST use the format `v[Major].[Phase].[SubPhase].[Task]` and stay
synchronized with `Documentation/Active/Tasks.md`.

- Major = project iteration; Phase = major feature phase; SubPhase = feature
  sub-section; Task = individual completed task.
- Every completed task MUST: mark the task complete in `Active/Tasks.md`, add a
  `Changelog/CURRENT.md` entry, commit with the version in the message
  (including the RDF phase), and tag the release.
- Version numbers MUST NOT be skipped; sub-phase completion resets the task
  digit, phase completion resets sub-phase and task digits.

**Rationale**: The version number is self-documenting progress — anyone can map
a build to its exact task, phase, and documentation without archaeology.

## Audio & Performance Constraints

- **Platform**: VB.NET / .NET WinForms desktop application (Windows).
- **Internal format**: internal DSP processing targets float32 end-to-end;
  integer PCM formats (16/24-bit) exist only at I/O boundaries (WAV read/write,
  device capture). Processors MUST NOT assume sample bit depth. The current
  Int16 pipeline is legacy, scheduled for migration (feature
  001-float32-pipeline). Sample rates are 44.1 kHz or 48 kHz.
- **Latency budgets**: audio callback < 10 ms (hard), buffer processing < 5 ms
  (typical), tap writes < 1 ms. Slow callbacks MUST be logged with timings.
- **Signal chain**: Input → [PreDSP tap] → Input Gain → [PostGain tap] → DSP →
  [PostDSP tap] → Output Gain → [PreOutput tap] → Output. New instrumentation
  MUST attach at an existing tap point via a named multi-reader cursor.
- **Metering and FFT** are event-driven consumers of tap-point ring buffers;
  they never receive copies of audio-thread buffers.

## Development Workflow & Quality Gates

- **Plan gate**: every implementation plan MUST pass a Constitution Check
  against Principles I–VIII before research/design proceeds; violations require
  an explicit Complexity Tracking justification or a redesign.
- **Boundaries first**: new features document boundaries and invariants
  (RDF Phase 2) before code (Phase 3).
- **Testing discipline**: edge cases first (null buffers, mid-stream format
  changes, rapid state cycling, device disconnect); stress tests verify no
  crashes or leaks; every fixed bug gets a regression test.
- **Logging**: state transitions and user-facing resets are logged via
  `Logger.Instance`; audio-thread logging is minimal and never per-sample.
- **Documentation cadence**: task completion updates `Active/Tasks.md` and the
  changelog; sub-phase completion adds session notes; phase completion produces
  comprehensive docs moved to `Completed/Features/` and updates architecture
  docs when boundaries changed.

## Governance

This constitution supersedes all other practices, including the source
instruction files it was synthesized from (`.github/copilot-instructions.md`
and `.github/instructions/`). Where those files conflict with this document,
this document wins and the source files SHOULD be updated to match.

- **Amendments**: any change to principles or constraints requires a documented
  rationale, an updated Sync Impact Report, and propagation to dependent
  templates (`.specify/templates/`) and instruction files in the same change.
- **Versioning policy**: this document follows semantic versioning — MAJOR for
  principle removals or redefinitions, MINOR for new or materially expanded
  principles/sections, PATCH for clarifications and wording.
- **Compliance review**: every spec, plan, and PR MUST be checked against the
  Core Principles; the plan-level Constitution Check is the enforcement gate,
  and unjustified violations block implementation. Complexity MUST always be
  justified against a simpler rejected alternative.

**Version**: 1.1.0 | **Ratified**: 2026-07-12 | **Last Amended**: 2026-07-12
