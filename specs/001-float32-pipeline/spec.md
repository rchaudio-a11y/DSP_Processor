# Feature Specification: Float32 Processing Pipeline

**Feature Branch**: `001-float32-pipeline`

**Created**: 2026-07-13

**Status**: Draft

**Input**: User description: "Migrate the audio engine's internal processing domain from 16-bit integer PCM to normalized floating-point samples end-to-end, with integer formats existing only at I/O boundaries. Constitution v1.1.0 already names the current Int16 pipeline as legacy scheduled for this migration. [...] (8 core requirements, explicit test contract boundaries, out-of-scope list — full text preserved in git history of this file's creation.)"

## Context

Constitution v1.1.0 (Audio & Performance Constraints): *"Internal DSP
processing targets float32 end-to-end; integer PCM formats (16/24-bit) exist
only at I/O boundaries. Processors MUST NOT assume sample bit depth. The
current Int16 pipeline is legacy, scheduled for migration (feature
001-float32-pipeline)."* This feature discharges that clause.

Today, sources already deliver floating-point (the file reader natively; WASAPI
capture natively), but the engine quantizes to 16-bit at entry and every
processor converts back to floating-point for its math and re-quantizes on
write. Costs: per-stage quantization noise, zero inter-stage headroom
(inter-stage clipping), and double conversion at every stage. This blocks the
planned multi-stage recording chain (Blumlein M/S processing), which cannot be
built on per-stage 16-bit truncation.

Both prerequisites are met: the audio core is behavior-locked by the feature
003 test foundation (SC-007), and the state machine is hardened (feature 002).

## Clarifications

### Session 2026-07-13

- Q: Pan law behavior at hard pan (compensation forces a choice: +3 dB
  extremes, unity extremes with a power dip, or −3 dB extremes)? → A:
  **Balance law: favored channel unity** — the favored channel stays at
  unity across the entire pan range; only the opposite channel attenuates
  (smooth monotonic taper to zero at hard pan). Center = both channels
  exactly ×1.0. This supersedes the "compensated constant-power" phrasing in
  the original feature description: the bypass discontinuity is eliminated
  *by construction* (center pan is mathematically transparent), and hard pan
  preserves the favored channel at input level.
- Q: Dither at the float→16-bit output boundary (now that genuinely
  continuous values reach it)? → A: **No dither in 001** — the boundary
  conversion stays deterministic (round-half-even), preserving FR-012's
  surviving round-trip suite and the deterministic null test exactly. Dither
  is deferred to the 24-bit export feature, where it lands as an additive
  option on the canonical conversion, not a rework.
- Q: Buffer sizing policy when samples widen 2× (preserve durations at 2×
  memory, preserve bytes at ½ durations, or mixed)? → A: **Preserve time
  durations** — all buffers keep their current seconds/samples semantics and
  byte capacities double (megabytes scale). The engine's behavior is
  specified in time (2 s DSP depth, 50% refill targets, <10 ms callbacks);
  the underrun safety margins tuned during Phase 6 debugging are preserved
  unchanged.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One Domain, One Conversion Per Boundary (Priority: P1)

As the audio engine, every sample between a source boundary and a sink
boundary is a normalized floating-point value. Conversion to or from integer
PCM happens exactly once per boundary — file/device in, playback device out,
recorded file out — and nowhere else. The WASAPI capture engine's private
duplicate conversion (deferred from feature 003) is consolidated into the
single canonical boundary conversion. Monitor tap points carry the
processing-domain format, and monitoring consumers (meters, FFT) read it
directly with no per-read conversion loops.

**Why this priority**: This is the migration itself — every other requirement
(headroom, pan law, the Blumlein chain) presupposes the domain change. It
also deletes the standing waste: double conversion per stage and per-read
integer→float loops in every meter and FFT consumer.

**Independent Test**: the bit-transparency null test — a unity-settings chain
(gain 1, pan center, width normal) from source to output nulls against the
source within 1 LSB of a single conversion round-trip; a code-level audit
finds exactly one int→float site per input boundary and one float→int site
per output boundary, and zero conversion loops in monitoring consumers.

**Acceptance Scenarios**:

1. **Given** a 16-bit source file played through a unity-settings chain,
   **When** the output is captured at the playback boundary and null-tested
   against the source, **Then** the residual is within 1 LSB of a single
   16-bit conversion round-trip (bit-transparency, SC-001).
2. **Given** the completed migration, **When** the codebase is audited for
   integer↔float sample conversion sites, **Then** exactly one canonical
   conversion utility is used, invoked only at the named boundaries — the
   WASAPI capture engine's private variant no longer exists.
3. **Given** meters and spectrum displays running, **When** they read from
   tap points, **Then** they receive processing-domain samples directly and
   contain no per-read integer→float conversion loops.
4. **Given** the capture/record path, **When** audio enters from the device
   and exits to a file, **Then** conversion to the processing domain happens
   at device entry and conversion to the target file format happens at write
   exit — only.

---

### User Story 2 - Inter-Stage Headroom, Exit-Only Clamping (Priority: P2)

As a processing chain, my inter-stage signal levels may exceed full scale
without artifacts: a hot stage output flows into the next stage undamaged,
and range limiting happens exactly once — at the final output conversion.
This is the property that makes the planned Blumlein M/S recording chain
buildable (M/S matrixing transiently exceeds full scale by design).

**Why this priority**: headroom is the engineering payoff of the domain
change and the direct enabler of the next planned feature; it is only
meaningful once US1's domain exists.

**Independent Test**: the inter-stage headroom test — drive a signal above
full scale through two chained stages (boost then attenuate); the final
output is clean (no wrap, no truncation artifacts) and matches the
mathematically expected result within floating-point tolerance.

**Acceptance Scenarios**:

1. **Given** a full-scale source boosted +6 dB by stage one, **When** stage
   two attenuates −6 dB before output, **Then** the output equals the source
   within floating-point tolerance — no inter-stage clipping occurred.
2. **Given** a signal still above full scale at the output boundary, **When**
   it is converted for the playback device or recorded file, **Then** it
   clips predictably at full scale (clamp, never wrap) — the boundary
   conversion's existing accuracy contract, unchanged.
3. **Given** any processor in the chain, **When** its input exceeds ±1.0,
   **Then** the processor performs its math without internal clamping
   (clamping exists only in the boundary conversion).

---

### User Story 3 - Balance Pan Law (Priority: P3) ⚠️ DELIBERATE BEHAVIOR CHANGE

As a user adjusting gain, pan, or width, engaging any non-unity setting no
longer causes an audible ~−3 dB level step at center pan. The pan law becomes
the **balance law** (clarified 2026-07-13): the favored channel stays at
unity across the whole pan range, only the opposite channel attenuates with
a smooth monotonic taper to zero at hard pan, and center pan is both
channels at exactly ×1.0 — mathematically transparent, so no discontinuity
at the bypass boundary can exist. **Architect-ruled behavior change**:
feature 003 discovered and behavior-locked the existing defect (any
non-unity setting applies ~−3 dB at center because the constant-power law is
uncompensated); that lock is now explicitly released and superseded.

**Why this priority**: it is a real, audible defect fix — but it is
independent of the domain migration mechanics and must land after the
processor math is re-derived in the new domain (US1), so its assertions are
written once, not twice.

**Independent Test**: with gain 2.0 at center pan, output level equals input
×2.0 exactly (no hidden −3 dB); sweeping a setting across the bypass
threshold produces no level step; power preservation holds across the pan
range relative to the center-unity reference.

**Acceptance Scenarios**:

1. **Given** gain 2.0, center pan, normal width, **When** audio passes
   through, **Then** output = input ×2.0 within floating-point tolerance —
   the ~−3 dB center attenuation is gone.
2. **Given** settings swept continuously across the unity-bypass boundary
   (e.g., gain 1.0 → 1.001), **When** output levels are compared on both
   sides, **Then** the level difference is inaudible (< 0.01 dB) — no step
   discontinuity.
3. **Given** stereo input at any pan position, **When** processed, **Then**
   the favored channel equals input × gain exactly (unity pan contribution)
   and the opposite channel follows a smooth monotonic taper from ×1.0 at
   center to ×0.0 at hard pan, within floating-point tolerance.
4. **Given** the feature-003 test that locked the −3 dB behavior, **When**
   this feature completes, **Then** that assertion has been explicitly
   superseded (removed/replaced with the center-unity assertion), with the
   supersession recorded — never silently edited.

---

### Edge Cases

- Signal at exactly ±full scale after boundary conversion: clamps predictably,
  never wraps (existing boundary contract, unchanged).
- NaN or Infinity produced by a defective processor: the output boundary
  conversion must not emit garbage — non-finite samples convert to a defined
  clamped value, and the condition is observable (diagnostic), not silent.
- Denormal-range samples (silence decay tails): must not degrade processing
  performance below the audio deadline.
- Mono and stereo interleaving preserved through the domain (per-sample
  conversion is channel-agnostic, as today).
- Byte-oriented infrastructure (ring buffers, tap points, overrun detection)
  carries the new domain transparently — sample width changes from 2 to 4
  bytes. Policy (clarified 2026-07-13): time durations are preserved — every
  buffer keeps its seconds/samples semantics and its byte capacity doubles;
  no latency or safety margin changes.
- Recorded WAV output remains 16-bit PCM in this feature (24-bit export is
  out of scope but must not be made harder).
- A source that is already floating-point (file reader, WASAPI) enters the
  domain without any intermediate integer quantization — the current
  float→int16→float double hop at entry is eliminated, not just relocated.
- Playback of existing 16-bit recordings: unchanged audible behavior at unity
  settings (bit-transparency covers this).

## Requirements *(mandatory)*

### Functional Requirements

**Domain migration (US1)**

- **FR-001**: Audio samples MUST flow as normalized floating-point values
  from source boundary entry to sink boundary exit; no intermediate stage may
  quantize to an integer format.
- **FR-002**: Integer↔float conversion MUST occur exactly once per boundary:
  file/device input, playback device output, recorded file output. One
  canonical conversion utility serves all boundaries.
- **FR-003**: The WASAPI capture engine's private float→PCM16 conversion
  (deferred from feature 003, research R5) MUST be eliminated in favor of the
  canonical boundary conversion.
- **FR-004**: Processors MUST operate natively on floating-point samples;
  every per-stage integer→float→integer conversion MUST be removed.
- **FR-005**: Monitor tap points MUST carry the processing-domain format;
  monitoring consumers (meters, FFT) MUST read it directly, and their
  per-read integer→float conversion loops MUST be removed.
- **FR-006**: The capture/record path MUST convert to the processing domain
  at device entry and to the target file format at write exit — nowhere in
  between. Recorded file format remains 16-bit PCM WAV in this feature.

**Signal semantics (US2)**

- **FR-007**: Inter-stage signal levels MUST be allowed to exceed full scale
  without artifacts; no processor may clamp internally.
- **FR-008**: Range limiting MUST happen exactly once, at the final output
  boundary conversion, which clips predictably (clamp, never wrap) per its
  existing accuracy contract.
- **FR-009**: Non-finite samples (NaN/Infinity) reaching the output boundary
  MUST convert to a defined clamped value with an observable diagnostic —
  never silent garbage.

**Pan law (US3 — deliberate behavior change, Architect-ruled)**

- **FR-010**: The pan law MUST become the balance law (clarified
  2026-07-13, superseding "compensated constant-power" from the original
  description): the favored channel at unity for every pan position, the
  opposite channel on a smooth monotonic taper from unity at center to zero
  at hard pan, and center pan mathematically transparent (both channels
  ×1.0) — eliminating the bypass-boundary discontinuity by construction.
- **FR-011**: The feature-003 assertions that locked the uncompensated law
  (~−3 dB at center under non-unity settings) MUST be explicitly superseded
  and replaced — the supersession recorded in the test file and changelog,
  never silently edited.

**Test contract (cross-cutting — explicit boundaries)**

- **FR-012**: SURVIVING UNCHANGED (empty diff at feature completion): the GSM
  transition matrix suite, the tap-point/multi-reader behavioral suite
  (reader independence, overrun detection — byte semantics are
  format-agnostic), and the boundary conversion round-trip suite (the
  boundary still exists with the same accuracy contract).
- **FR-013**: SUPERSEDED AND RE-DERIVED in the floating-point domain:
  processor math tests (gain accuracy, pan power preservation, width) with
  floating-point tolerances, including new center-unity pan assertions
  replacing the locked −3 dB behavior (FR-011).
- **FR-014**: NEW tests: the bit-transparency null test (US1), the
  inter-stage headroom test (US2), the non-finite handling test (FR-009),
  and an allocation audit of the steady-state hot loop (Constitution IV — the
  migration must not introduce per-block allocations).

### Key Entities

- **Processing Domain**: normalized floating-point samples (±1.0 nominal,
  excursions above permitted) flowing between boundaries. Interleaving and
  channel semantics unchanged from today.
- **Boundary**: a point where audio enters or leaves the engine — file/device
  in, playback device out, recorded file out. The only places integer formats
  exist and the only places conversion or clamping occur.
- **Canonical Conversion**: the single utility performing integer↔float
  sample conversion at every boundary, preserving the feature-003 accuracy
  contract (round-trip ≤ 1 LSB, symmetric scaling, clamp-never-wrap).
  Deterministic — no dither in this feature (clarified 2026-07-13; dither is
  a future additive option for the 24-bit export feature).
- **Pan Law (balance)**: favored channel unity everywhere; opposite channel
  tapers smoothly to zero at hard pan; center transparent (both ×1.0). The
  uncompensated constant-power law it replaces is a recorded, superseded
  behavior.
- **Test Contract**: the explicit three-way partition of the existing suite —
  surviving / superseded / new — defined in FR-012..014.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Unity-chain null test: source-to-output residual ≤ 1 LSB of a
  single 16-bit conversion round-trip, for both a 16-bit file source and a
  float source.
- **SC-002**: Conversion audit: exactly one canonical conversion utility;
  one int→float invocation per input boundary, one float→int per output
  boundary; zero conversion loops in monitoring consumers; the WASAPI private
  variant deleted.
- **SC-003**: Headroom: +6 dB inter-stage excursion through two stages
  arrives bit-equal (within float tolerance) after compensating attenuation;
  the same excursion sent to the boundary clips cleanly, never wraps.
- **SC-004**: Pan law: gain 2.0 at center pan yields exactly ×2.0 (the
  hidden −3 dB is gone); no step > 0.01 dB across the bypass boundary; the
  favored channel equals input × gain at every pan position and the opposite
  channel tapers monotonically to zero, within float tolerance.
- **SC-005**: Test contract honored: FR-012's surviving suites pass with
  empty diffs; FR-013's superseded tests are replaced with recorded
  supersession; FR-014's new tests exist and pass.
- **SC-006**: Steady-state hot loop performs zero allocations per processed
  block (audit + measurement), and the audio callback budget (< 10 ms) is
  unchanged or improved (the double conversions are gone).
- **SC-007**: Full suite remains headless and completes in under 30 seconds.

## Assumptions

- **Processing domain representation**: 32-bit IEEE float, normalized ±1.0
  nominal, interleaved — matching what the file reader and WASAPI already
  deliver. (64-bit doubles rejected: no audible benefit for this chain,
  double the memory bandwidth.)
- **Byte-oriented infrastructure is reused, not rewritten**: ring buffers and
  tap points carry bytes and are format-agnostic (their suites survive
  unchanged, FR-012). Buffer sizing follows the preserve-durations policy
  (clarified 2026-07-13): byte capacities double so every buffer's time
  semantics — and Phase 6's tuned feeder margins — are unchanged.
- **The null-test reference** is one 16→float→16 round trip through the
  canonical conversion — the unavoidable cost of a 16-bit source and sink.
- **Pan-law taper curve** (the exact attenuation shape of the opposite
  channel — e.g., cosine vs linear) is a design decision for the plan,
  constrained by FR-010: smooth, monotonic, unity at center, zero at hard
  pan. The favored channel's unity contribution is fixed by clarification,
  not a plan choice.
- **Recorded output stays 16-bit WAV**; 24-bit export is a separate feature
  enabled by this work (out of scope, must not be precluded). Dither at
  bit-depth reduction is likewise deferred to that feature (clarified
  2026-07-13) — all 001 boundary conversions are deterministic.
- **Out of scope** (per feature description): ASIO/multichannel device
  support and system-audio loopback capture (feature 004); new processor
  types (EQ, filters — post-001); 24-bit file export.
- **Prerequisites consumed**: feature 003's behavior-locked audio core
  (SC-007 there) and feature 002's hardened state machine. The 003 lock on
  the uncompensated pan law is explicitly released by the Architect ruling in
  this feature's description (FR-011 governs how).
