# Quickstart: Validating 002-state-machine-hardening

**Date**: 2026-07-12 | **Plan**: [plan.md](plan.md)

## Prerequisites

- Windows with .NET 10 SDK; no audio hardware needed for the test suite
- Feature 003 complete (v1.3.3.3) — its harness and matrix tests are the base

## Build & test (primary validation)

```powershell
dotnet build DSP_Processor\DSP_Processor.vbproj
dotnet test DSP_Processor.Tests\DSP_Processor.Tests.vbproj
```

**Expected**: 0 errors; all tests green (55 existing + the data-model test
map's 11 named tests plus audit-driven additions in
`StateMachineHardeningTests`); wall time < 30 s; zero log files created.

**Behavior-lock check (SC-002)**: `git diff` on
`DSP_Processor.Tests/GlobalStateMachineTests.vb` must be EMPTY at feature
completion — the 64-pair matrix suite passes with zero assertion changes.

```powershell
dotnet test DSP_Processor.Tests --filter ClassName~StateMachineHardeningTests
```

## Disposal-surface removal check (SC-006)

```powershell
# Zero matches expected in production code:
Get-ChildItem DSP_Processor -Recurse -Filter *.vb |
  Select-String -Pattern 'StateCoordinator.*IDisposable|Sub Dispose.*StateCoordinator'
```

Plus the reflection test `Coordinator_HasNoDisposalSurface` (asserts the type
no longer implements IDisposable).

## Subscriber audit check (SC-005)

`Documentation/Architecture/GSM-Subscriber-Audit.md` exists with all 11 rows
from research R7 filled (finding + action + verified column), and the
second-order/SSM-boundary note present.

## Manual smoke test (app-level)

1. Launch the app; open the State Debugger panel.
2. Run a record cycle (arm → record → stop) and a playback cycle — the SSM
   cascade paths exercise deferral windows; watch for: no hangs (deadlock),
   state flow identical to v1.3.3.3, transition IDs sequential in the log.
3. Check the log for any `Invalid transition rejected` lines during normal
   cycles (there should be none) and confirm no `deferred` markers appear
   outside cascade moments.
4. Close the app: shutdown completes cleanly (no coordinator-disposal path
   exists anymore; subsystems still torn down by their owners).

## Validation results

| Date | Milestone | Result |
|------|-----------|--------|
| _(filled at implementation checkpoints)_ | | |
