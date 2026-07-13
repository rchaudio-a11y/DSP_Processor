# Contract: Single Monitor Reader API

**Date**: 2026-07-12 | **Feature**: 003-tap-consolidation

After this feature, the members below are the ONLY way to read monitor audio.
Primary surface lives on `DSPThread`; `AudioRouter` and `RecordingManager`
keep their existing thin pass-throughs (same signatures, delegation only).

## Surface (post-feature)

### Reader lifecycle

```vb
' Create an independent, named cursor at a tap point.
' Position starts at the live edge (no backlog). Returns readerName as handle.
' Throws ObjectDisposedException after dispose; ArgumentException on bad tap.
Public Function CreateTapReader(tapLocation As TapLocation, readerName As String) As String

' Remove a reader; other readers on the same tap are unaffected. Idempotent.
Public Sub RemoveTapReader(tapLocation As TapLocation, readerName As String)
```

### Reading (overrun-aware)

```vb
' Existing signature, upgraded semantics:
' - Never returns spliced audio (single read never crosses a resync point).
' - If the reader was lapped: records overrun stats, resyncs to oldest valid
'   data, then returns contiguous data from there.
' - Default-signature consumers are FR-005-conformant: loss is recorded at
'   the reader and queryable via GetTapReaderStats (analysis F1).
Public Function ReadFromTap(tapLocation As TapLocation, readerName As String,
                            buffer As Byte(), offset As Integer, count As Integer) As Integer

' NEW overload: identical, plus loss reporting at the call site.
' bytesLost = bytes skipped by any resync performed during THIS call (0 if none).
Public Function ReadFromTap(tapLocation As TapLocation, readerName As String,
                            buffer As Byte(), offset As Integer, count As Integer,
                            ByRef bytesLost As Long) As Integer

' Existing signature, upgraded semantics: detect-and-resync like Read, then
' return pending bytes (always in 0..capacity).
Public Function TapAvailable(tapLocation As TapLocation, readerName As String) As Integer
```

### Diagnostics (new)

```vb
' Per-reader statistics snapshot. Does NOT clear counters (monotonic).
Public Function GetTapReaderStats(tapLocation As TapLocation, readerName As String) As Utils.ReaderStats
' ReaderStats: OverrunEvents As Integer, OverrunEpisodeCount As Integer,
'              TotalBytesLost As Long, Pending As Long,
'              LastOverrunLogEmitted As DateTime (MinValue = never)
```

## Behavioral contract

| # | Guarantee | Spec ref |
|---|-----------|----------|
| 1 | Readers are independent: no call on reader A changes reader B's position or stats | FR-004 |
| 2 | The first reader-side call after a lap reports/records the loss with exact byte count | FR-005 |
| 3 | After that call, the reader is resynced to the oldest valid byte and subsequent reads are contiguous | FR-006 |
| 4 | No call ever returns audio spanning lost data without the loss having been recorded — including laps of exactly 1× or N× capacity | FR-007 |
| 5 | Overrun logging: emitted on transition into an overrun episode (first overrun after a clean read), rate-limited to 1 s minimum interval per reader, from the reader-side call, never from `Write` | FR-008 |
| 6 | `Write` cost profile unchanged: no allocation, no logging, single existing lock | Constitution IV |
| 7 | Threading: all members safe to call from UI thread; `Write` is called only by the DSP worker (unchanged ownership) | Constitution I/V |

## Removed members (breaking, internal-only — verified no other consumers)

`DSPThread`: `ReadInputMonitor`, `ReadOutputMonitor`, `ReadPostGainMonitor`,
`ReadPostOutputGainMonitor`, `InputMonitorAvailable`, `OutputMonitorAvailable`,
`PostGainMonitorAvailable`, `PostOutputGainMonitorAvailable` — plus the hidden
`"_default_*"` readers they lazily created.

Migration map (consumer → named reader): see [research.md §R6](../research.md).

## Also part of the public-surface change set

- `Utils.SampleConversion.FloatToPcm16(...)` — NEW (extracted from
  `AudioRouter`; AudioRouter delegates).
- `Utils.MultiReaderRingBuffer` — same lifecycle members; `Read`/`Available`
  gain the semantics above; NEW `Read` ByRef-loss overload and
  `GetReaderStats`; class comment corrected re: single-lock contention.
- `Utils.Logger.SuppressForTesting` — NEW Shared property (test-only escape
  hatch; no effect unless set before first Instance access).
- `Utils.ReaderStats` — NEW public structure.
