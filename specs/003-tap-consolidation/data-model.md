# Phase 1 Data Model: Tap Monitoring Consolidation & Audio Core Test Foundation

**Date**: 2026-07-12 | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)

## MultiReaderRingBuffer (refactored)

**Owner**: `Utils` layer; instances owned exclusively by `DSPThread` (four tap
buffers). Constitution I: all reader/overrun state lives here, nowhere else.

### Writer-side state

| Field | Type | Semantics |
|-------|------|-----------|
| `buffer` | `Byte()` | Fixed circular storage, length = `capacity` (unchanged) |
| `totalBytesWritten` | `Long` | Monotonic; grows by `count` on every `Write`; never wraps |

### Per-reader state (internal `Reader` record, keyed by name)

| Field | Type | Semantics |
|-------|------|-----------|
| `Name` | `String` | Unique per buffer; creation returns the name as handle (unchanged) |
| `TotalBytesRead` | `Long` | Monotonic; reader's logical position; `= totalBytesWritten` at creation (start at live edge, no phantom backlog) |
| `OverrunEvents` | `Integer` | Count of detected overruns since creation |
| `TotalBytesLost` | `Long` | Accumulated bytes skipped by resyncs |

### Public result type

```
Structure ReaderStats
    OverrunEvents As Integer          ' every detected overrun occurrence
    OverrunEpisodeCount As Integer    ' transitions into overrun after a clean read (rate-limit unit)
    TotalBytesLost As Long
    Pending As Long                   ' bytes available right now (post-clamp)
    LastOverrunLogEmitted As DateTime ' when the rate-limited log last fired (MinValue = never)
End Structure
```

*(OverrunEpisodeCount and LastOverrunLogEmitted added per analysis E1 — they
make FR-008 rate-limiting test-verifiable without observing the logger.)*

### Invariants (sacred — RDF Phase 2)

1. `0 <= totalBytesWritten - reader.TotalBytesRead` at all times after any
   public call returns (a reader is never ahead of the writer).
2. After overrun handling: `totalBytesWritten - reader.TotalBytesRead <=
   capacity` (a reader is never more than one buffer behind).
3. **Overrun predicate**: `pending > capacity` where `pending =
   totalBytesWritten - reader.TotalBytesRead`. Loss amount `= pending -
   capacity`. Resync: `reader.TotalBytesRead = totalBytesWritten - capacity`.
4. A single `Read` never crosses a resync point → returned audio is always
   contiguous (FR-007).
5. Physical index for monotonic position `p` is `p Mod capacity`; Mod is used
   *only* for physical addressing, never for distance math (kills the aliasing
   class: a lap of exactly N×capacity is arithmetically visible in `pending`).
6. All fields above are touched only under the single existing lock (retained
   by scope decision; documented, see plan Complexity Tracking). No Interlocked
   needed for the Longs because the lock provides the barrier. Concurrency
   edge cases (e.g., remove-reader during write) are serialized by this lock
   **by design** — review-verified, not suite-verified; tests are
   single-threaded per research.md R8 *(analysis C1)*.
7. `Write` performs no allocation and no logging (Constitution IV). Overrun
   detection, resync, stats, and rate-limited logging occur only inside
   reader-side calls (`Read`/`Available`/`GetReaderStats`).

### State transitions (reader lifecycle)

```
(create) --> Live: TotalBytesRead = totalBytesWritten
Live --Write laps reader--> Overrun-pending (latent; detected on next reader call)
Overrun-pending --Read/Available--> Live: stats updated, position resynced,
                                     loss reported (ByRef overload) / queryable
Live --RemoveReader--> (gone; other readers unaffected)
```

## TapLocation mapping (FR-003 documentation source)

| TapLocation | Signal-flow position | Backing buffer | Content today |
|-------------|---------------------|----------------|---------------|
| `PreDSP` | Raw input, before any processing | `inputMonitorBuffer` | Raw input audio |
| `PostGain` | After INPUT gain stage | `postGainMonitorBuffer` | Gain-staged input |
| `PostDSP` | After OUTPUT gain stage (processor callback) | `postOutputGainMonitorBuffer` | **Near-identical to PreOutput** |
| `PreOutput` | After full chain (worker write) | `outputMonitorBuffer` | **Near-identical to PostDSP** |

**Divergence note (must land in tap documentation)**: PostDSP and PreOutput
will diverge when any processor is inserted after the output-gain stage or
when the worker applies post-chain steps (e.g., dither in feature 001). They
are retained as distinct taps for that reason.

## SampleConversion (new `Utils` module)

- `FloatToPcm16(floatBuffer As Byte(), byteCount As Integer, channels As
  Integer) As Byte()` — extracted verbatim from `AudioRouter` (little-endian
  byte-pair writes, clamped at ±full-scale).
- Validation rules (test oracle, FR-012): symmetric scaling — +1.0 maps to
  +32767 and -1.0 maps to **-32767** (-32768 is unreachable in the current
  implementation; verified during extraction and locked as-is); 0.0 maps to
  exactly 0; round-trip float→PCM16→float error ≤ 1 LSB (1/32767) across the
  amplitude sweep; over-range input clamps to ±1.0 first, so it clips and
  never wraps.
- Known duplicate left in place: `WasapiEngine.ConvertFloatToPCM16`
  (capture path, channel-agnostic) — consolidation deferred to feature 001.

## Logger test-suppress mode

- `Utils.Logger.SuppressForTesting As Boolean` (Shared, default False).
- Contract: when True *before first `Instance` access* — no directory
  creation, no file open, no writer thread, `Log` is a no-op (optionally
  `Debug.WriteLine`). When False: behavior is byte-for-byte today's.
- Set once in `DSP_Processor.Tests\TestSetup.vb` `<AssemblyInitialize>`.

## GSM transition matrix oracle

Derived from `GlobalStateMachine.IsValidTransition`
(`State/GlobalStateMachine.vb:154-208`), constitution-locked as the FR-014
test oracle. ✓ = valid, ✗ = rejected (state must remain unchanged).

| from \ to | Uninit | Idle | Arming | Armed | Recording | Stopping | Playing | Error |
|-----------|--------|------|--------|-------|-----------|----------|---------|-------|
| **Uninitialized** | ✓* | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| **Idle** | ✗ | ✓* | ✓ | ✗ | ✗ | ✗ | ✓ | ✓ |
| **Arming** | ✗ | ✓ | ✓* | ✓ | ✗ | ✗ | ✗ | ✓ |
| **Armed** | ✗ | ✓ | ✗ | ✓* | ✓ | ✗ | ✗ | ✓ |
| **Recording** | ✗ | ✗ | ✗ | ✗ | ✓* | ✓ | ✗ | ✓ |
| **Stopping** | ✗ | ✓ | ✗ | ✗ | ✗ | ✓* | ✗ | ✓ |
| **Playing** | ✗ | ✓ | ✗ | ✗ | ✗ | ✓ | ✓* | ✓ |
| **Error** | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓* |

\* diagonal = same-state transition, always valid (no-op rule).

**Totals**: 26 valid / 38 invalid of 64 pairs — the test must assert exactly
these counts as a tamper check on the oracle itself.

**From-state setup paths** (shortest valid drive from a fresh instance;
max 4 hops):

| Target from-state | Path |
|-------------------|------|
| Uninitialized | (fresh instance) |
| Idle | Uninitialized→Idle |
| Arming | →Idle→Arming |
| Armed | →Idle→Arming→Armed |
| Recording | →Idle→Arming→Armed→Recording |
| Stopping | →Idle→Playing→Stopping |
| Playing | →Idle→Playing |
| Error | →Idle→Error |

## Test suite structure (FR → class map)

| Test class | FR | Oracle source |
|------------|----|---------------|
| `RingBufferTests` | FR-010 | Write/read round-trip, wraparound, Available/FreeSpace accounting, full/empty boundaries |
| `MultiReaderRingBufferTests` | FR-011 | Invariants 1–5 above; independence; exact-lap and multi-lap overrun; resync; reader create-at-live-edge; remove-during-activity |
| `SampleConversionTests` | FR-012 | SampleConversion validation rules above |
| `GainProcessorTests` | FR-013 | Unity bypass bit-identity; clamp at extremes; mute floor (-60 dB); constant-power pan (L²+R² preserved within tolerance); mono + stereo paths; width extremes |
| `GlobalStateMachineTests` | FR-014 | The 8×8 oracle table + totals check + Error-recovery path + state-unchanged-on-reject |
