# Architecture Instructions

**Reference:** Main [copilot-instructions.md](../copilot-instructions.md)  
**Methodology:** [RDF.md](../../DSP_Processor/Documentation/Reference/RDF.md)

---

## ??? RDF Phase Guidance

### Phase 1: Curiosity Ignition
> *"Generate momentum, not answers"*

**Activities:**
- Identify problem, pattern, or "what if" moment
- Create rough notes, sketches
- Explore possibilities

**Output:** Problem statement in `Active/Sessions/`

---

### Phase 2: Insight Bloom
> *"Turn curiosity into clarity"*

**Activities:**
- Design architecture
- Define system boundaries
- Establish invariants
- Analyze options

**Output:** Use `Architecture-v1_0_0.md` template

**Key Sections:**
- Problem statement
- Proposed architecture
- **Boundaries & Invariants** (sacred!)
- Design decisions with rationale
- Trade-offs

---

### Phase 3: Build Momentum
> *"Build cleanly, intentionally, with respect for boundaries"*

**Activities:**
- Implement core modules
- Respect boundaries defined in Phase 2
- Build with architecture in mind

**Output:** Code + `Task-v1_0_0.md` documentation

**Principles:**
- Clean implementation
- No hacks or shortcuts
- Follow established patterns

---

### Phase 4: Recursive Debugging
> *"Bugs become teachers"*

**Activities:**
- Deep bug investigation
- Root cause analysis
- **Discover architecture truths**
- Refine invariants

**Output:** Use `Issue-v1_0_0.md` template

**Key Focus:**
- What does this bug reveal?
- What architectural assumption was wrong?
- What boundary was violated?
- How does this deepen understanding?

**NOT:**
- Quick fixes without understanding
- Treating bugs as failures
- Minimizing debug time

---

### Phase 5: Validation Loop
> *"Ensure architecture holds under pressure"*

**Activities:**
- Test edge cases
- Stress test
- Verify invariants
- Performance testing

**Output:** Test results in task/issue docs

---

### Phase 6: Synthesis
> *"Turn working system into polished, expressive tool"*

**Activities:**
- Complete documentation
- Polish UI
- Write lessons learned
- Create completion docs

**Output:** Move to `Completed/Features/`

---

### Meta-Phase: Recursion
> *"Begin next loop with more clarity and stronger foundations"*

**Activities:**
- Reflect on what was learned
- Update architecture understanding
- Plan next loop

**Output:** Meta-reflection in session notes

---

## ?? System Boundaries & Invariants

### What Are They?

**Boundaries:** Where components meet, what they can/cannot do  
**Invariants:** Rules that must ALWAYS be true

### Examples from DSP_Processor

**Boundaries:**
- DSPThread owns all audio processing
- UI thread never touches audio buffers directly
- RecordingManager owns microphone lifecycle

**Invariants:**
- Audio callbacks must complete in < 10ms
- Ring buffers never overwrite unread data
- All audio is 16-bit PCM internally
- Zero-copy for tap points (no buffer duplication)

### Documenting Them

```markdown
## System Boundaries

### Invariants (Sacred Rules)
1. **Audio Thread Isolation:** UI never blocks audio thread
2. **Zero-Copy Taps:** Tap points use multi-reader ring buffers
3. **Event-Driven Updates:** No polling for audio data

### Constraints
- Maximum latency: 10ms
- Sample rate: 44.1kHz or 48kHz
- Bit depth: 16-bit PCM
```

---

## ?? Design Decision Documentation

### Architecture Decision Record (ADR) Format

Use when making significant architectural choices.

**Template Sections:**
1. **Context:** What problem are we solving?
2. **Options:** What alternatives exist?
3. **Decision:** What did we choose?
4. **Rationale:** Why this option?
5. **Consequences:** What are the trade-offs?

**Example:**
- [Multi-Tap-Routing-Architecture-Decision.md](../../DSP_Processor/Documentation/Architecture/Multi-Tap-Routing-Architecture-Decision.md)

---

## ?? Architecture Evolution

### When to Update Architecture Docs

- ? New component added
- ? Boundary changed
- ? Invariant discovered/refined
- ? Phase 4 debugging revealed flaw
- ? Major refactoring

### When NOT to Update

- ? Small bug fixes
- ? UI tweaks
- ? Performance optimizations (unless architectural)

### Update Process

1. Copy `Architecture-v1_0_0.md` template
2. Document changes with Phase 2 mindset
3. Link from `Active/Tasks.md`
4. Save to `Architecture/`
5. Reference in Changelog

---

## ?? Recursion in Practice

### Triggering a New Loop

**Scenarios:**
- Phase 4 reveals fundamental flaw ? Back to Phase 2
- New understanding emerges ? Refine architecture
- Edge case questions assumptions ? Explore (Phase 1)

**Process:**
1. Document the trigger in session notes
2. Create new architecture doc if needed
3. Update existing boundaries/invariants
4. Note what changed in understanding

### Compounding Understanding

Each loop should:
- ? Deepen system knowledge
- ? Strengthen architectural confidence
- ? Improve engineer intuition
- ? Refine boundaries/invariants

**NOT:**
- ? Just ship features faster
- ? Accumulate technical debt
- ? Compromise understanding for velocity

---

## ?? Architecture Quality Metrics

### Good Architecture
- Clear boundaries
- Explicit invariants
- Composable components
- Easy to reason about
- Survives edge cases

### Bad Architecture
- Unclear ownership
- Hidden assumptions
- Spaghetti dependencies
- Hard to test
- Brittle under stress

---

**Last Updated:** 2026-01-16  
**Version:** 1.0.0
