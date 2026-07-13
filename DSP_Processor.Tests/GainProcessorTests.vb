Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports NAudio.Wave

''' <summary>
''' FR-013: GainProcessor math tests, RE-DERIVED in the float32 processing domain.
'''
''' ============================ SUPERSESSION RECORD =============================
''' Feature 001-float32-pipeline, 2026-07-13, per Architect ruling (feature
''' description item 5) and spec FR-010/FR-011.
'''
''' This file previously locked (feature 003, v1.3.3.1) the UNCOMPENSATED
''' constant-power pan law, including its defect: any non-unity setting applied
''' ~-3 dB to center-panned audio, a step discontinuity at the bypass boundary.
''' The following locked assertions are hereby EXPLICITLY SUPERSEDED:
'''   - Stereo_CenterPan_AppliesConstantPowerAttenuation  (locked the -3 dB defect)
'''   - HardPan_FullyAttenuatesOppositeChannel            (locked cos/sin extremes)
'''   - ConstantPowerPan_PowerPreservedAcrossPanRange     (locked L2+R2 preservation)
''' Replacement law: BALANCE (clarify Q1) - favored channel unity at every pan
''' position, opposite channel on a cosine taper cos(|pan|*pi/2), center pan
''' mathematically transparent. Full sweep assertions: FloatPipelineTests (SC-004).
''' Recorded here and in Documentation/Changelog/CURRENT.md (v1.3.5.x) - never a
''' silent edit. The int16-domain tests were re-derived with float tolerances.
''' ==============================================================================
''' </summary>
<TestClass>
Public Class GainProcessorTests

    Private Shared ReadOnly StereoFormat As WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)
    Private Shared ReadOnly MonoFormat As WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1)

    ''' <summary>Builds an AudioBuffer from interleaved float32 samples.</summary>
    Private Shared Function MakeBuffer(format As WaveFormat, ParamArray samples As Single()) As DSP.AudioBuffer
        Dim bytes(samples.Length * 4 - 1) As Byte
        System.Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length)
        Dim buf As New DSP.AudioBuffer(format, bytes.Length, True)
        buf.CopyFrom(bytes, 0, bytes.Length)
        Return buf
    End Function

#Region "Unity bypass & enable/bypass plumbing"

    <TestMethod>
    Public Sub UnitySettings_OutputBitIdentical()
        Dim gp As New DSP.GainProcessor(StereoFormat) ' defaults: gain 1, pan 0, width 1
        Dim buf = MakeBuffer(StereoFormat, 0.1F, -0.2F, 0.999F, -1.0F, 1.5F, -1.5F) ' incl. excursions
        Dim before = CType(buf.Buffer.Clone(), Byte())

        gp.Process(buf)

        CollectionAssert.AreEqual(before, buf.Buffer, "unity gain/pan/width must not alter a single byte")
    End Sub

    <TestMethod>
    Public Sub Bypassed_OutputUnchangedEvenWithGain()
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.GainLinear = 2.0F, .Bypassed = True}
        Dim buf = MakeBuffer(StereoFormat, 0.25F, -0.25F)
        Dim before = CType(buf.Buffer.Clone(), Byte())

        gp.Process(buf)

        CollectionAssert.AreEqual(before, buf.Buffer)
    End Sub

    <TestMethod>
    Public Sub Disabled_OutputUnchangedEvenWithGain()
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.GainLinear = 2.0F, .Enabled = False}
        Dim buf = MakeBuffer(StereoFormat, 0.25F, -0.25F)
        Dim before = CType(buf.Buffer.Clone(), Byte())

        gp.Process(buf)

        CollectionAssert.AreEqual(before, buf.Buffer)
    End Sub

    <TestMethod>
    Public Sub Process_NullBuffer_Throws()
        Dim gp As New DSP.GainProcessor(StereoFormat)
        Assert.ThrowsException(Of ArgumentNullException)(Sub() gp.Process(Nothing))
    End Sub

    <TestMethod>
    Public Sub Process_IncompatibleFormat_Throws()
        Dim gp As New DSP.GainProcessor(StereoFormat)
        Dim buf = MakeBuffer(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2), 0.1F, 0.1F)
        Assert.ThrowsException(Of InvalidOperationException)(Sub() gp.Process(buf))
    End Sub

#End Region

#Region "Gain math (float domain - no clamping, FR-007)"

    <TestMethod>
    Public Sub Mono_GainTimesTwo_ExactDoubling()
        ' 2.0 is a power of two: float multiplication is exact
        Dim gp As New DSP.GainProcessor(MonoFormat) With {.GainLinear = 2.0F}
        Dim buf = MakeBuffer(MonoFormat, 0.25F, -0.1875F, 0.0F)

        gp.Process(buf)

        Assert.AreEqual(0.5F, buf.GetSample(0))
        Assert.AreEqual(-0.375F, buf.GetSample(1))
        Assert.AreEqual(0.0F, buf.GetSample(2))
    End Sub

    <TestMethod>
    Public Sub Gain_ExcursionsAboveFullScale_FlowUnclamped()
        ' FR-007: the processor never clamps - values beyond +/-1.0 flow undamaged
        Dim gp As New DSP.GainProcessor(MonoFormat) With {.GainLinear = 4.0F}
        Dim buf = MakeBuffer(MonoFormat, 0.9F, -0.9F)

        gp.Process(buf)

        Assert.AreEqual(3.6F, buf.GetSample(0), 0.0000005F, "3.6 must flow through, not clamp")
        Assert.AreEqual(-3.6F, buf.GetSample(1), 0.0000005F)
    End Sub

    <TestMethod>
    Public Sub GainDB_MuteFloor_MinusSixty()
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.GainLinear = 0.0F}
        Assert.AreEqual(-60.0F, gp.GainDB, "muted gain must floor at -60 dB, not -Infinity")
    End Sub

    <TestMethod>
    Public Sub GainDB_SetGet_RoundTrips()
        Dim gp As New DSP.GainProcessor(StereoFormat)
        gp.GainDB = -6.0F
        Assert.AreEqual(-6.0F, gp.GainDB, 0.01F)
        gp.GainDB = 20.0F
        Assert.AreEqual(20.0F, gp.GainDB, 0.01F)
        gp.GainDB = -100.0F ' clamps to -60
        Assert.AreEqual(-60.0F, gp.GainDB, 0.01F)
    End Sub

#End Region

#Region "Balance pan law basics (supersedes constant-power law - see header)"

    <TestMethod>
    Public Sub Stereo_CenterPan_Transparent_GainExact()
        ' THE DEFECT FIX: gain 2.0 at center pan yields exactly x2.0 on both
        ' channels. Under the superseded law this was x(2*cos(pi/4)) ~ x1.414.
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.GainLinear = 2.0F}
        Dim buf = MakeBuffer(StereoFormat, 0.25F, -0.25F)

        gp.Process(buf)

        Assert.AreEqual(0.5F, buf.GetSample(0), "center pan must be transparent: L = input x gain, exactly")
        Assert.AreEqual(-0.5F, buf.GetSample(1), "center pan must be transparent: R = input x gain, exactly")
    End Sub

    <TestMethod>
    Public Sub HardPan_FavoredChannelUnity_OppositeSilent()
        ' Balance law at hard left: L = input x gain (favored, unity pan factor), R = 0
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.PanPosition = -1.0F}
        Dim buf = MakeBuffer(StereoFormat, 0.5F, 0.5F)

        gp.Process(buf)

        Assert.AreEqual(0.5F, buf.GetSample(0), 0.0000005F, "favored channel stays at unity")
        Assert.AreEqual(0.0F, buf.GetSample(1), 0.0000005F, "opposite channel fully tapered at hard pan")
    End Sub

#End Region

#Region "Stereo width (float domain)"

    <TestMethod>
    Public Sub WidthZero_CollapsesToMono_NoCenterAttenuation()
        ' width 0: side = 0, both channels = mid. Balance law: NO pan attenuation
        ' at center (the superseded law multiplied by cos(pi/4) here).
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.StereoWidth = 0.0F}
        Dim buf = MakeBuffer(StereoFormat, 0.5F, 0.25F)

        gp.Process(buf)

        Assert.AreEqual(0.375F, buf.GetSample(0), "L = mid, exactly - no hidden attenuation")
        Assert.AreEqual(0.375F, buf.GetSample(1), "R = mid, exactly")
    End Sub

    <TestMethod>
    Public Sub WidthTwo_DoublesSideSignal()
        ' L=0.5, R=0.25 -> mid=0.375, side=0.125; width 2 -> side=0.25
        ' L' = 0.625, R' = 0.125 (center pan: no further factors)
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.StereoWidth = 2.0F}
        Dim buf = MakeBuffer(StereoFormat, 0.5F, 0.25F)

        gp.Process(buf)

        Assert.AreEqual(0.625F, buf.GetSample(0))
        Assert.AreEqual(0.125F, buf.GetSample(1))
    End Sub

#End Region

End Class
