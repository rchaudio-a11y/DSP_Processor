# Contract: Float Processing Domain & Canonical Conversion

**Date**: 2026-07-13 | **Feature**: 001-float32-pipeline

## Canonical conversion pair (`Utils.SampleConversion`)

```vb
' EXISTING - locked 003 contract preserved (surviving suite), plus FR-009 guard:
' clamp to +/-1.0 then scale x32767, round-half-even, little-endian.
' NaN -> 0, +/-Infinity -> +/-full-scale; increments NonFiniteCount (Friend seam),
' logs on first occurrence. NO dither (clarify Q2) - deterministic.
Public Function FloatToPcm16(floatBuffer As Byte(), byteCount As Integer, channels As Integer) As Byte()

' NEW - the inverse: int16 bytes -> IEEE float bytes, scale /32768
' (agrees with NAudio's AudioFileReader so all domain entries share one scale).
Public Function Pcm16ToFloat(pcmBuffer As Byte(), byteCount As Integer) As Byte()
' + scratch-buffer overloads for steady-state zero-allocation call sites
```

| # | Guarantee | Spec ref |
|---|-----------|----------|
| 1 | These are the ONLY integer↔float sample conversion sites in the codebase | FR-002, SC-002 |
| 2 | Asymmetric scale pair (÷32768 in, ×32767 out): round-trip error ≤ 1 LSB — the null-test reference | SC-001 |
| 3 | Out-conversion clamps at full scale, never wraps; deterministic (no dither) | FR-008, clarify Q2 |
| 4 | Non-finite input produces defined output + observable `NonFiniteCount` + log; never silent garbage | FR-009 |
| 5 | Existing `SampleConversionTests` pass with an EMPTY file diff | FR-012 |

## Processing domain

| # | Guarantee | Spec ref |
|---|-----------|----------|
| 6 | Every sample between boundaries is normalized float32; interleaving unchanged | FR-001 |
| 7 | Processors read/write ONLY via `AudioBuffer.SampleSpan() As Span(Of Single)`; no byte access, no internal clamping (excursions > ±1.0 flow undamaged) | FR-004, FR-007, AR-3 |
| 8 | Ring buffers, taps, overrun detection: byte-semantics untouched — `MultiReaderRingBufferTests` empty diff | FR-012, AR-1 |
| 9 | Buffer time semantics preserved: capacities derive from the float format's `AverageBytesPerSecond` (2× bytes, same seconds) | clarify Q3, AR-5 |
| 10 | Taps carry domain-format bytes; monitoring consumers reinterpret (`BlockCopy` to `Single()`), zero per-read conversion loops | FR-005 |
| 11 | Playback path end-to-end conversion count: ZERO (float file/device in, float device out) | FR-002 (≤1/boundary) |

## Balance pan law (behavior change, Architect-ruled)

| # | Guarantee | Spec ref |
|---|-----------|----------|
| 12 | Favored channel contribution = 1.0 at every pan position; opposite = cos(|pan|·π/2); no boost anywhere | FR-010, AR-4 |
| 13 | Center pan is mathematically transparent (both ×1.0) — bypass optimization has no behavior cliff | FR-010, SC-004 |
| 14 | The 003 lock on the old law is released by RECORDED supersession (test-file header + changelog), never a silent edit | FR-011 |

## Removed surface / behavior

- `WasapiEngine.ConvertFloatToPCM16` (private duplicate) — DELETED (FR-003).
- Per-stage int16↔float conversion in `GainProcessor` (and its clamps) — DELETED.
- Feeder-thread float→PCM16 conversion on file playback — DELETED (not relocated).
- Per-read int16→float loops in `AudioRouter`/`RecordingManager` tap helpers — DELETED.
- Orphan `_default_input`/`_default_output` reader creation (`AudioRouter.vb:471-479`, 003 leftover) — DELETED.
- Uncompensated constant-power pan law — SUPERSEDED (guarantee 14).
