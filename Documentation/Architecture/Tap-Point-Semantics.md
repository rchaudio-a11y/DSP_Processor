# Tap Point Semantics

**Created:** 2026-07-12 (feature 003-tap-consolidation, FR-003)
**Version:** v1.3.3.3
**API contract:** [monitor-reader-api.md](../../specs/003-tap-consolidation/contracts/monitor-reader-api.md)

## The single monitoring API

As of v1.3.3.3 there is exactly ONE way to read monitor audio: the
`TapLocation` reader API on `DSPThread` (with thin pass-throughs on
`AudioRouter` and `RecordingManager`):

```vb
Dim reader = dspThread.CreateTapReader(TapLocation.PostGain, "MyInstrument")
Dim available = dspThread.TapAvailable(TapLocation.PostGain, "MyInstrument")
Dim bytesRead = dspThread.ReadFromTap(TapLocation.PostGain, "MyInstrument", buffer, 0, count)
Dim stats = dspThread.GetTapReaderStats(TapLocation.PostGain, "MyInstrument")
dspThread.RemoveTapReader(TapLocation.PostGain, "MyInstrument")
```

The legacy per-tap `Read*Monitor` / `*MonitorAvailable` method pairs were
removed in v1.3.3.3. Every reader is named, independent, and overrun-aware
(a lapped reader is told how much it lost and is resynced to the oldest
valid audio; reads can never return spliced old/new data).

## Signal-flow positions

```
Audio Input
    |
    v
[PreDSP tap] ................ raw input, before any processing
    |
Input GainProcessor (gain/pan/width)
    |
    v
[PostGain tap] .............. after INPUT gain stage
    |
DSP Processing (chain)
    |
Output GainProcessor
    |
    v
[PostDSP tap] ............... after OUTPUT gain stage (processor callback)
    |
    v
[PreOutput tap] ............. post-chain worker write, before WaveOut
    |
Audio Output
```

| TapLocation | Position | Backing buffer | Typical consumers |
|-------------|----------|----------------|-------------------|
| `PreDSP` | Raw input before any processing | `inputMonitorBuffer` | Input meters, raw FFT (`Router.InputSamples`, `Router.InputEvents`) |
| `PostGain` | After input gain/pan/width | `postGainMonitorBuffer` | Input-chain meters (`Router.PostGainSamples`, `RecMgr.PostGain`) |
| `PostDSP` | After output gain (fed by the output GainProcessor's monitor callback) | `postOutputGainMonitorBuffer` | Effect verification (`Router.PostOutputGainSamples`, `RecMgr.PostOutputGain`) |
| `PreOutput` | After the full chain (written by the DSP worker post-chain) | `outputMonitorBuffer` | Output meters, FFT (`Router.OutputSamples`, `Router.OutputEvents`) |

## PostDSP vs PreOutput — equivalent TODAY, distinct on purpose

`PostDSP` and `PreOutput` currently carry **near-identical content**: the
output GainProcessor is the last processor in the chain, so its monitor
callback (PostDSP) and the worker's post-chain write (PreOutput) see the
same audio, modulo write timing.

They are kept as separate taps because they **diverge as soon as**:

- any processor is inserted into the chain *after* the output gain stage, or
- the worker applies post-chain steps before output (e.g., dither or final
  limiting in feature 001-float32-pipeline).

Choose `PostDSP` to observe "what the output gain produced"; choose
`PreOutput` to observe "what is about to reach the output device." Today the
distinction is conventional; after feature 001 it will be real.

## Registered reader names (v1.3.3.3)

| Reader | Tap | Owner |
|--------|-----|-------|
| `Router.InputSamples` | PreDSP | AudioRouter meter property |
| `Router.InputEvents` | PreDSP | AudioRouter FFT event pump |
| `Router.PostGainSamples` | PostGain | AudioRouter meter property |
| `Router.PostOutputGainSamples` | PostDSP | AudioRouter meter property |
| `Router.OutputSamples` | PreOutput | AudioRouter meter property |
| `Router.OutputEvents` | PreOutput | AudioRouter FFT event pump |
| `RecMgr.PostGain` | PostGain | RecordingManager meter property |
| `RecMgr.PostOutputGain` | PostDSP | RecordingManager meter property |

Readers are created lazily on first use and start at the live edge (no
backlog). Overrun statistics per reader are available via
`GetTapReaderStats` (events, episodes, bytes lost, pending).
