# Quickstart: Validating 001-float32-pipeline

**Date**: 2026-07-13 | **Plan**: [plan.md](plan.md)

## Prerequisites

- Windows with .NET 10 SDK; no audio hardware for the suite (hardware needed
  only for the manual listening checks)
- On branch `001-float32-pipeline` (story checkpoints v1.3.5.x commit here;
  merge to master at feature completion)

## Build & test (primary validation)

```powershell
dotnet build DSP_Processor.slnx
dotnet test DSP_Processor.Tests\DSP_Processor.Tests.vbproj
```

**Expected**: 0 errors; all tests green (< 30 s, headless, zero file I/O).

## Test-contract gates (FR-012/013 — run at EVERY checkpoint)

```powershell
# SURVIVING suites: all three must print NOTHING:
git diff --stat DSP_Processor.Tests/GlobalStateMachineTests.vb `
                DSP_Processor.Tests/MultiReaderRingBufferTests.vb `
                DSP_Processor.Tests/SampleConversionTests.vb

# SUPERSEDED suite: header must record the supersession:
Get-Content DSP_Processor.Tests\GainProcessorTests.vb -TotalCount 20 |
  Select-String 'SUPERSEDED|Architect'
```

## Conversion-site audit (SC-002)

```powershell
# Only Utils/SampleConversion.vb may convert sample formats.
# Expect ZERO hits outside it (production code):
Get-ChildItem DSP_Processor -Recurse -Filter *.vb |
  Where-Object { $_.Name -ne 'SampleConversion.vb' } |
  Select-String -Pattern '32767|32768' |
  Where-Object { $_.Line -notmatch 'comment-safe patterns reviewed at task time' }
# Review any hits: display math in UI is allowed only if it consumes
# normalized floats (no int16 parsing).
```

## New-test groups (FloatPipelineTests.vb, FR-014)

```powershell
dotnet test DSP_Processor.Tests --filter ClassName~FloatPipelineTests
```

Covers: SC-001 null test (≤ 1 LSB), SC-003 headroom (+6 dB inter-stage,
clean exit clamp), SC-004 balance-pan group, FR-009 non-finite, SC-006
allocation audit (0 bytes/block steady-state), Pcm16ToFloat accuracy.

## Manual listening checks (hardware required)

1. Play a 16-bit WAV at unity settings — must sound identical to v1.3.4.x
   (bit-transparency, audible confirmation of SC-001).
2. Sweep pan slowly across center with gain at +6 dB — NO level jump at
   center (the old −3 dB step is gone); hard pan leaves the favored channel
   at full level.
3. Record a mic take (WASAPI and WaveIn if available); play it back — clean
   audio, correct levels (capture entry + write exit boundaries).
4. Watch meters + FFT during playback and recording — behavior identical to
   before (they now read float directly).
5. Playback device compatibility: if the output device rejects float (rare),
   note it — the deferred fallback in plan Complexity gets activated.

## Versioning & branch discipline

- v1.3.5.1 (US1 domain), v1.3.5.2 (US2 headroom), v1.3.5.3 (US3 pan law) —
  committed + tagged on `001-float32-pipeline`; merge to master when the
  feature completes (Architect branch ruling for this feature).

## Validation results

| Date | Milestone | Result |
|------|-----------|--------|
| 2026-07-13 | T001 baseline (feature branch) | ✅ 67/67, 332 ms, surviving suites committed-clean |
| 2026-07-13 | T004 foundational (canonical pair + accessors) | ✅ 72/72; surviving diffs EMPTY |
| 2026-07-13 | US2 gate (T014): headroom (3 tests) + zero-alloc audit (delta = 0 bytes / 1000 blocks) + denormal sanity (< 5×) | ✅ 79/79, 317 ms; surviving diffs EMPTY |
| 2026-07-13 | US1 gate (T011): domain migrated, 3 null variants | ✅ 74/74, 210 ms; surviving diffs EMPTY; **SC-002 audit: only `SampleConversion.vb` converts** — 5 residual `32767/32768` hits reviewed and classified as inert 16-bit display dispatcher branches (`AudioLevelMeter.Analyze16Bit` :196/:204, `FFTProcessor` Case 16 :88/:97, `MainForm.CalculateTruePeakDB` 16-branch :1650); no int16 data reaches them (all sources emit 32) and none is a conversion loop on the active path. R4 deviation recorded (accessors, not Span). |
