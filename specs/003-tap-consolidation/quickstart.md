# Quickstart: Validating 003-tap-consolidation

**Date**: 2026-07-12 | **Plan**: [plan.md](plan.md)

## Prerequisites

- Windows with .NET 10 SDK (`dotnet --version` ≥ 10.0)
- No audio hardware required for the test suite
- Repository at feature branch/lineage containing this spec

## Build & test (primary validation — SC-003)

```powershell
# From repo root
dotnet build DSP_Processor\DSP_Processor.vbproj
dotnet test DSP_Processor.Tests\DSP_Processor.Tests.vbproj
```

**Expected**: build succeeds with 0 errors; all tests pass; total wall time
under 30 seconds; no `Logs/` directory or `.log` files created anywhere by the
test run (Logger suppress mode working).

Useful filters during development:

```powershell
dotnet test DSP_Processor.Tests --filter ClassName~GlobalStateMachineTests
dotnet test DSP_Processor.Tests --filter ClassName~MultiReaderRingBufferTests
```

## API-surface audit (SC-001)

```powershell
# Must return ZERO matches in DSP_Processor\ (production code):
Get-ChildItem DSP_Processor -Recurse -Filter *.vb |
  Select-String -Pattern 'ReadInputMonitor|ReadOutputMonitor|ReadPostGainMonitor|ReadPostOutputGainMonitor|InputMonitorAvailable|OutputMonitorAvailable|PostGainMonitorAvailable|PostOutputGainMonitorAvailable'
```

**Expected**: no output (the 8 legacy members and all their call sites are gone).

## Overrun detection spot-check (SC-002)

Covered by `MultiReaderRingBufferTests` (exact-lap, multi-lap, resync,
independence). The stress variant: stall one reader while writing > capacity,
assert `GetReaderStats().OverrunEvents > 0`, `TotalBytesLost` equals the
arithmetic expectation, and post-resync reads are contiguous.

## GSM matrix completeness (SC-004)

`GlobalStateMachineTests` iterates all 64 from/to pairs against the oracle in
[data-model.md](data-model.md#gsm-transition-matrix-oracle) and asserts the
26-valid/38-invalid totals as a tamper check.

## Manual smoke test (SC-006 — behavior unchanged after migration)

1. `dotnet run` the app (or launch from VS), arm the microphone.
2. Watch input meters and spectrum display — behavior identical to v1.3.2.5.
3. Load a WAV file and play through DSP; watch output meters/FFT.
4. Open the State Debugger Panel before arming (regression check on the
   v1.3.2.5 null-guard — must not throw).
5. Check the log file: tap reader creations appear with the new named readers
   (`Router.*`, `RecMgr.*`); no overrun warnings during normal operation.

## Validation results

| Date | Milestone | Result |
|------|-----------|--------|
| 2026-07-12 | US1 gate (T011): 39 tests vs unmodified production code | ✅ 39/39 passed, 81–233 ms wall (target < 30 s), zero log files created |
| 2026-07-12 | US1 mutation check (T011): off-by-one injected into RingBuffer write-position advance | ✅ 6 named RingBufferTests failures, then reverted → 39/39 green, `git diff` clean |
| 2026-07-12 | US2 (T015): monotonic-counter refactor + 16 overrun tests | ✅ 55/55 passed, 88 ms; zero US1 test modifications (behavior-lock held) |

## Sequencing guard (FR-015)

The behavior-locking test classes (`RingBufferTests`, `SampleConversionTests`,
`GainProcessorTests`, `GlobalStateMachineTests`) MUST be green against the
unmodified production code BEFORE the MultiReaderRingBuffer refactor or any
consumer migration is merged. `MultiReaderRingBufferTests`' overrun cases are
the only tests written against the NEW contract.
