Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' FR-012: behavior-locking tests for Utils.SampleConversion.FloatToPcm16.
''' Locks the CURRENT symmetric-scaling behavior: +/-1.0 -> +/-32767
''' (-32768 unreachable), over-range clamps, 0.0 -> exactly 0.
''' </summary>
<TestClass>
Public Class SampleConversionTests

    Private Shared Function FloatsToBytes(ParamArray values As Single()) As Byte()
        Dim bytes(values.Length * 4 - 1) As Byte
        System.Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length)
        Return bytes
    End Function

    Private Shared Function Pcm16At(pcm As Byte(), sampleIndex As Integer) As Short
        Return BitConverter.ToInt16(pcm, sampleIndex * 2)
    End Function

    <TestMethod>
    Public Sub FullScale_MapsSymmetrically_32768Unreachable()
        Dim pcm = Utils.SampleConversion.FloatToPcm16(FloatsToBytes(1.0F, -1.0F), 8, 1)
        Assert.AreEqual(CShort(32767), Pcm16At(pcm, 0), "+1.0 must map to +32767")
        Assert.AreEqual(CShort(-32767), Pcm16At(pcm, 1), "-1.0 must map to -32767 (symmetric scaling; -32768 unreachable)")
    End Sub

    <TestMethod>
    Public Sub Zero_MapsToExactlyZero()
        Dim pcm = Utils.SampleConversion.FloatToPcm16(FloatsToBytes(0.0F), 4, 1)
        Assert.AreEqual(CShort(0), Pcm16At(pcm, 0))
    End Sub

    <TestMethod>
    Public Sub OverRange_ClipsToFullScale_NeverWraps()
        Dim pcm = Utils.SampleConversion.FloatToPcm16(FloatsToBytes(2.0F, -2.0F, 100.0F, -100.0F), 16, 1)
        Assert.AreEqual(CShort(32767), Pcm16At(pcm, 0))
        Assert.AreEqual(CShort(-32767), Pcm16At(pcm, 1))
        Assert.AreEqual(CShort(32767), Pcm16At(pcm, 2))
        Assert.AreEqual(CShort(-32767), Pcm16At(pcm, 3))
    End Sub

    <TestMethod>
    Public Sub RoundTrip_AmplitudeSweep_ErrorWithinOneLsb()
        ' Sweep -1.0 .. +1.0 in 401 steps; float -> pcm16 -> float error <= 1 LSB (1/32767)
        Const steps As Integer = 401
        Dim values(steps - 1) As Single
        For i = 0 To steps - 1
            values(i) = -1.0F + (2.0F * i / (steps - 1))
        Next

        Dim floatBytes(steps * 4 - 1) As Byte
        System.Buffer.BlockCopy(values, 0, floatBytes, 0, floatBytes.Length)
        Dim pcm = Utils.SampleConversion.FloatToPcm16(floatBytes, floatBytes.Length, 1)

        Dim lsb As Single = 1.0F / 32767.0F
        For i = 0 To steps - 1
            Dim recovered As Single = Pcm16At(pcm, i) / 32767.0F
            Dim err = Math.Abs(recovered - values(i))
            Assert.IsTrue(err <= lsb, $"sample {i}: input {values(i)}, recovered {recovered}, error {err} > 1 LSB")
        Next
    End Sub

    <TestMethod>
    Public Sub OutputSize_IsHalfInputSize_TruncatesPartialSample()
        ' 10 bytes = 2 complete floats + 2 stray bytes -> 2 samples -> 4 bytes out
        Dim input(9) As Byte
        System.Buffer.BlockCopy(New Single() {0.25F, -0.25F}, 0, input, 0, 8)
        Dim pcm = Utils.SampleConversion.FloatToPcm16(input, 10, 1)
        Assert.AreEqual(4, pcm.Length)
    End Sub

    <TestMethod>
    Public Sub StereoInterleaving_OrderPreserved()
        ' L, R, L, R with distinct values must come out in the same order
        Dim pcm = Utils.SampleConversion.FloatToPcm16(FloatsToBytes(0.5F, -0.5F, 0.25F, -0.25F), 16, 2)
        Assert.IsTrue(Pcm16At(pcm, 0) > 0)
        Assert.IsTrue(Pcm16At(pcm, 1) < 0)
        Assert.AreEqual(Pcm16At(pcm, 0), CShort(-Pcm16At(pcm, 1)), "L/R symmetry must hold")
        Assert.IsTrue(Math.Abs(CInt(Pcm16At(pcm, 2)) * 2 - CInt(Pcm16At(pcm, 0))) <= 1, "0.25 must be half of 0.5 within rounding")
    End Sub

    <TestMethod>
    Public Sub LittleEndian_ByteOrderLocked()
        ' 0.5 * 32767 = 16383.5 -> CShort banker's rounding -> 16384 = &H4000
        Dim pcm = Utils.SampleConversion.FloatToPcm16(FloatsToBytes(0.5F), 4, 1)
        Assert.AreEqual(CByte(&H0), pcm(0), "low byte first (little-endian)")
        Assert.AreEqual(CByte(&H40), pcm(1), "high byte second")
    End Sub

End Class
