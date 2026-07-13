Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports NAudio.Wave

''' <summary>
''' FR-013: behavior-locking tests for GainProcessor math.
''' Locks CURRENT behavior, including the constant-power center attenuation:
''' the stereo path applies cos(pi/4) ~ 0.7071 per side whenever the processor
''' is not in its unity bypass (gain=1, pan=0, width=1).
''' </summary>
<TestClass>
Public Class GainProcessorTests

    Private Shared ReadOnly StereoFormat As New WaveFormat(44100, 16, 2)
    Private Shared ReadOnly MonoFormat As New WaveFormat(44100, 16, 1)

    ''' <summary>Builds an AudioBuffer from interleaved Int16 samples.</summary>
    Private Shared Function MakeBuffer(format As WaveFormat, ParamArray samples As Short()) As DSP.AudioBuffer
        Dim bytes(samples.Length * 2 - 1) As Byte
        System.Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length)
        Dim buf As New DSP.AudioBuffer(format, bytes.Length, True)
        buf.CopyFrom(bytes, 0, bytes.Length)
        Return buf
    End Function

    Private Shared Function SampleAt(buf As DSP.AudioBuffer, index As Integer) As Short
        Return BitConverter.ToInt16(buf.Buffer, index * 2)
    End Function

#Region "Unity bypass & enable/bypass plumbing"

    <TestMethod>
    Public Sub UnitySettings_OutputBitIdentical()
        Dim gp As New DSP.GainProcessor(StereoFormat) ' defaults: gain 1, pan 0, width 1
        Dim buf = MakeBuffer(StereoFormat, 1000S, -2000S, 12345S, -12345S, 32767S, Short.MinValue)
        Dim before = CType(buf.Buffer.Clone(), Byte())

        gp.Process(buf)

        CollectionAssert.AreEqual(before, buf.Buffer, "unity gain/pan/width must not alter a single byte")
    End Sub

    <TestMethod>
    Public Sub Bypassed_OutputUnchangedEvenWithGain()
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.GainLinear = 2.0F, .Bypassed = True}
        Dim buf = MakeBuffer(StereoFormat, 1000S, -1000S)
        Dim before = CType(buf.Buffer.Clone(), Byte())

        gp.Process(buf)

        CollectionAssert.AreEqual(before, buf.Buffer)
    End Sub

    <TestMethod>
    Public Sub Disabled_OutputUnchangedEvenWithGain()
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.GainLinear = 2.0F, .Enabled = False}
        Dim buf = MakeBuffer(StereoFormat, 1000S, -1000S)
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
        Dim buf = MakeBuffer(New WaveFormat(48000, 16, 2), 100S, 100S)
        Assert.ThrowsException(Of InvalidOperationException)(Sub() gp.Process(buf))
    End Sub

#End Region

#Region "Gain math"

    <TestMethod>
    Public Sub Mono_GainTimesTwo_ExactDoubling()
        ' Mono path has no pan factor: pure multiply
        Dim gp As New DSP.GainProcessor(MonoFormat) With {.GainLinear = 2.0F}
        Dim buf = MakeBuffer(MonoFormat, 1000S, -750S, 0S)

        gp.Process(buf)

        Assert.AreEqual(CShort(2000), SampleAt(buf, 0))
        Assert.AreEqual(CShort(-1500), SampleAt(buf, 1))
        Assert.AreEqual(CShort(0), SampleAt(buf, 2))
    End Sub

    <TestMethod>
    Public Sub Mono_Clamping_NeverWraps()
        Dim gp As New DSP.GainProcessor(MonoFormat) With {.GainLinear = 10.0F}
        Dim buf = MakeBuffer(MonoFormat, 32000S, -32000S)

        gp.Process(buf)

        Assert.AreEqual(CShort(32767), SampleAt(buf, 0), "positive clamp")
        Assert.AreEqual(CShort(-32768), SampleAt(buf, 1), "negative clamp")
    End Sub

    <TestMethod>
    Public Sub Stereo_CenterPan_AppliesConstantPowerAttenuation()
        ' LOCKED BEHAVIOR: gain 2 at center pan yields x(2 * cos(pi/4)) ~ x1.41421,
        ' NOT x2 - the constant-power law attenuates center-panned stereo by 3 dB
        ' on the processing path (unity bypass skips this entirely).
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.GainLinear = 2.0F}
        Dim buf = MakeBuffer(StereoFormat, 10000S, 10000S)

        gp.Process(buf)

        Dim expected = CInt(Math.Round(10000.0 * 2.0 * Math.Cos(Math.PI / 4)))
        Assert.IsTrue(Math.Abs(CInt(SampleAt(buf, 0)) - expected) <= 1, $"L: got {SampleAt(buf, 0)}, expected ~{expected}")
        Assert.IsTrue(Math.Abs(CInt(SampleAt(buf, 1)) - expected) <= 1, $"R: got {SampleAt(buf, 1)}, expected ~{expected}")
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

#Region "Constant-power pan (analysis B2: rel tolerance 1e-3 on linear power)"

    <TestMethod>
    Public Sub ConstantPowerPan_PowerPreservedAcrossPanRange()
        ' Equal L=R input, width=1: mid=S, side=0 -> L'=S*g*cos(a), R'=S*g*sin(a)
        ' so L'^2 + R'^2 must equal (S*g)^2 at every pan position.
        Const s As Double = 10000.0
        For Each pan In New Single() {-1.0F, -0.5F, 0.0F, 0.5F, 1.0F}
            Dim gp As New DSP.GainProcessor(StereoFormat) With {.GainLinear = 1.5F, .PanPosition = pan}
            Dim buf = MakeBuffer(StereoFormat, CShort(s), CShort(s))

            gp.Process(buf)

            Dim l = CDbl(SampleAt(buf, 0))
            Dim r = CDbl(SampleAt(buf, 1))
            Dim expectedPower = (s * 1.5) * (s * 1.5)
            Dim actualPower = l * l + r * r
            Dim relError = Math.Abs(actualPower - expectedPower) / expectedPower
            Assert.IsTrue(relError <= 0.001, $"pan {pan}: relative power error {relError} > 1e-3 (L={l}, R={r})")
        Next
    End Sub

    <TestMethod>
    Public Sub HardPan_FullyAttenuatesOppositeChannel()
        ' pan -1: angle 0 -> cos=1, sin=0 -> all left, right silent
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.PanPosition = -1.0F}
        Dim buf = MakeBuffer(StereoFormat, 10000S, 10000S)
        gp.Process(buf)
        Assert.AreEqual(CShort(10000), SampleAt(buf, 0))
        Assert.AreEqual(CShort(0), SampleAt(buf, 1))
    End Sub

#End Region

#Region "Stereo width"

    <TestMethod>
    Public Sub WidthZero_CollapsesToMono()
        ' width 0: side=0 -> both channels become mid, then center-pan factor applies
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.StereoWidth = 0.0F}
        Dim buf = MakeBuffer(StereoFormat, 20000S, 10000S)

        gp.Process(buf)

        Dim mid = 15000.0 * Math.Cos(Math.PI / 4)
        Assert.IsTrue(Math.Abs(CInt(SampleAt(buf, 0)) - mid) <= 1, $"L: got {SampleAt(buf, 0)}, expected ~{mid:F0}")
        Assert.AreEqual(SampleAt(buf, 0), SampleAt(buf, 1), "mono collapse: L must equal R")
    End Sub

    <TestMethod>
    Public Sub WidthTwo_DoublesSideSignal()
        ' L=20000, R=10000 -> mid 15000, side 5000; width 2 -> side 10000
        ' L' = 25000 * cos(pi/4), R' = 5000 * sin(pi/4) at center pan
        Dim gp As New DSP.GainProcessor(StereoFormat) With {.StereoWidth = 2.0F}
        Dim buf = MakeBuffer(StereoFormat, 20000S, 10000S)

        gp.Process(buf)

        Dim expectedL = 25000.0 * Math.Cos(Math.PI / 4)
        Dim expectedR = 5000.0 * Math.Sin(Math.PI / 4)
        Assert.IsTrue(Math.Abs(CInt(SampleAt(buf, 0)) - expectedL) <= 1, $"L: got {SampleAt(buf, 0)}, expected ~{expectedL:F0}")
        Assert.IsTrue(Math.Abs(CInt(SampleAt(buf, 1)) - expectedR) <= 1, $"R: got {SampleAt(buf, 1)}, expected ~{expectedR:F0}")
    End Sub

#End Region

End Class
