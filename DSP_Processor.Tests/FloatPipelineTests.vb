Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports NAudio.Wave

''' <summary>
''' Feature 001 (float32-pipeline) tests - FR-014 NEW partition.
''' Foundational: canonical pair accuracy, non-finite handling, sample accessors.
''' US1: null-test group (3 variants). US2: headroom, allocation, denormals.
''' US3: balance-pan law group.
''' Surviving suites (GSM matrix, MRRB, SampleConversionTests) are NOT touched.
''' </summary>
<TestClass>
Public Class FloatPipelineTests

    Private Shared ReadOnly FloatStereo As WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)

    Private Shared Function FloatsToBytes(values As Single()) As Byte()
        Dim bytes(values.Length * 4 - 1) As Byte
        System.Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length)
        Return bytes
    End Function

    Private Shared Function BytesToFloats(bytes As Byte(), byteCount As Integer) As Single()
        Dim floats(byteCount \ 4 - 1) As Single
        System.Buffer.BlockCopy(bytes, 0, floats, 0, byteCount)
        Return floats
    End Function

    Private Shared Function Int16sToBytes(values As Short()) As Byte()
        Dim bytes(values.Length * 2 - 1) As Byte
        System.Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length)
        Return bytes
    End Function

#Region "Foundational - Pcm16ToFloat accuracy (canonical pair)"

    <TestMethod>
    Public Sub Pcm16ToFloat_ScaleIsDivideBy32768()
        ' Matches NAudio's AudioFileReader scale (research R3)
        Dim pcm = Int16sToBytes({16384S, -16384S, 32767S, Short.MinValue, 0S})
        Dim floats = BytesToFloats(Utils.SampleConversion.Pcm16ToFloat(pcm, pcm.Length), 20)

        Assert.AreEqual(0.5F, floats(0))
        Assert.AreEqual(-0.5F, floats(1))
        Assert.AreEqual(32767.0F / 32768.0F, floats(2))
        Assert.AreEqual(-1.0F, floats(3), "Short.MinValue must map to exactly -1.0")
        Assert.AreEqual(0.0F, floats(4))
    End Sub

    <TestMethod>
    Public Sub CanonicalPair_RoundTrip_WithinOneLsb()
        ' int16 -> float (/32768) -> int16 (x32767): asymmetric pair, max 1 LSB error
        Dim source(999) As Short
        For i = 0 To 999
            source(i) = CShort((i * 65) Mod 65536 - 32768)
        Next
        Dim pcmIn = Int16sToBytes(source)

        Dim floatBytes = Utils.SampleConversion.Pcm16ToFloat(pcmIn, pcmIn.Length)
        Dim pcmOut = Utils.SampleConversion.FloatToPcm16(floatBytes, floatBytes.Length, 1)

        For i = 0 To 999
            Dim recovered = BitConverter.ToInt16(pcmOut, i * 2)
            Assert.IsTrue(Math.Abs(CInt(recovered) - CInt(source(i))) <= 1,
                $"sample {i}: {source(i)} -> {recovered}, error > 1 LSB")
        Next
    End Sub

    <TestMethod>
    Public Sub Pcm16ToFloat_ScratchOverload_MatchesAllocating()
        Dim pcm = Int16sToBytes({100S, -200S, 3000S})
        Dim allocated = Utils.SampleConversion.Pcm16ToFloat(pcm, pcm.Length)
        Dim scratch(11) As Byte
        Dim written = Utils.SampleConversion.Pcm16ToFloat(pcm, pcm.Length, scratch)

        Assert.AreEqual(12, written)
        CollectionAssert.AreEqual(allocated, scratch)
    End Sub

#End Region

#Region "Foundational - Non-finite handling (FR-009)"

    <TestMethod>
    Public Sub NonFinite_DefinedOutputs_AndSeamIncrements()
        Dim before = Utils.SampleConversion.NonFiniteCount

        Dim hostile = FloatsToBytes({Single.NaN, Single.PositiveInfinity, Single.NegativeInfinity, 0.25F})
        Dim pcm = Utils.SampleConversion.FloatToPcm16(hostile, hostile.Length, 1)

        Assert.AreEqual(CShort(0), BitConverter.ToInt16(pcm, 0), "NaN must convert to 0 (silence)")
        Assert.AreEqual(CShort(32767), BitConverter.ToInt16(pcm, 2), "+Inf must clamp to full scale")
        Assert.AreEqual(CShort(-32767), BitConverter.ToInt16(pcm, 4), "-Inf must clamp to -full scale")
        Assert.IsTrue(Math.Abs(BitConverter.ToInt16(pcm, 6) - 8192) <= 1, "finite samples unaffected")
        Assert.AreEqual(before + 3, Utils.SampleConversion.NonFiniteCount, "seam must count each non-finite sample")
    End Sub

#End Region

#Region "Foundational - AudioBuffer float accessors (AR-3)"

    <TestMethod>
    Public Sub AudioBuffer_SampleAccessors_RoundTrip()
        Dim buf As New DSP.AudioBuffer(FloatStereo, 64, True)
        Dim values = {0.5F, -0.5F, 1.5F, -2.0F, 0.0F, 32767.0F / 32768.0F, Single.Epsilon, -1.0F}
        Dim bytes = FloatsToBytes(values)
        buf.CopyFrom(bytes, 0, bytes.Length)

        Assert.AreEqual(values.Length, buf.FloatSampleCount)
        For i = 0 To values.Length - 1
            Assert.AreEqual(values(i), buf.GetSample(i), $"GetSample({i})")
        Next

        ' SetSample writes bit-exactly, including excursions beyond full scale
        buf.SetSample(2, 3.25F)
        buf.SetSample(7, -0.125F)
        Assert.AreEqual(3.25F, buf.GetSample(2))
        Assert.AreEqual(-0.125F, buf.GetSample(7))
        Assert.AreEqual(3.25F, BitConverter.ToSingle(buf.Buffer, 8), "accessor writes must be byte-compatible")
    End Sub

#End Region

#Region "US1 - Null-test group (SC-001, three variants per analysis C2)"

    ''' <summary>Chain with the standard two gain stages (input + output), like production.</summary>
    Private Shared Function BuildChain(format As WaveFormat, gain1 As Single, gain2 As Single) As DSP.ProcessorChain
        Dim chain As New DSP.ProcessorChain(format)
        chain.AddProcessor(New DSP.GainProcessor(format) With {.GainLinear = gain1})
        chain.AddProcessor(New DSP.GainProcessor(format) With {.GainLinear = gain2})
        Return chain
    End Function

    Private Shared Function MakeInt16TestPattern() As Short()
        ' Deterministic pattern sweeping the full range incl. near-full-scale values
        Dim source(2047) As Short
        For i = 0 To 2047
            source(i) = CShort(((i * 523) Mod 65536) - 32768)
        Next
        source(0) = 32767 : source(1) = -32768 : source(2) = 0
        source(3) = 32000 : source(4) = -32000 ' near-full-scale: clamp detectors
        Return source
    End Function

    <TestMethod>
    Public Sub NullTest_UnityChain_Int16Source_WithinOneLsb()
        ' SC-001 variant 1: source -> entry conversion -> unity chain -> exit
        ' conversion nulls within 1 LSB of the single canonical round trip.
        Dim source = MakeInt16TestPattern()
        Dim pcmIn = Int16sToBytes(source)

        Dim floatBytes = Utils.SampleConversion.Pcm16ToFloat(pcmIn, pcmIn.Length)
        Dim buf As New DSP.AudioBuffer(FloatStereo, floatBytes.Length, True)
        buf.CopyFrom(floatBytes, 0, floatBytes.Length)

        Using chain = BuildChain(FloatStereo, 1.0F, 1.0F) ' unity settings
            chain.Process(buf)
        End Using

        Dim pcmOut = Utils.SampleConversion.FloatToPcm16(buf.Buffer, buf.ByteCount, 2)
        For i = 0 To source.Length - 1
            Dim recovered = BitConverter.ToInt16(pcmOut, i * 2)
            Assert.IsTrue(Math.Abs(CInt(recovered) - CInt(source(i))) <= 1,
                $"unity null: sample {i}: {source(i)} -> {recovered}")
        Next
    End Sub

    <TestMethod>
    Public Sub NullTest_FullMathPath_TimesTwoThenHalf_WithinOneLsb()
        ' SC-001 variant 2 (analysis C2): x2.0 then x0.5 - both exact powers of
        ' two, so float multiplication is exact. Mathematically identity through
        ' the COMPLETE math path with a 2x inter-stage excursion; near-full-scale
        ' samples make any hidden clamp (or bypass shortcut) detectable. This
        ' variant CANNOT pass via the unity-bypass fast path.
        Dim source = MakeInt16TestPattern()
        Dim pcmIn = Int16sToBytes(source)

        Dim floatBytes = Utils.SampleConversion.Pcm16ToFloat(pcmIn, pcmIn.Length)
        Dim buf As New DSP.AudioBuffer(FloatStereo, floatBytes.Length, True)
        buf.CopyFrom(floatBytes, 0, floatBytes.Length)

        Using chain = BuildChain(FloatStereo, 2.0F, 0.5F)
            chain.Process(buf)
        End Using

        Dim pcmOut = Utils.SampleConversion.FloatToPcm16(buf.Buffer, buf.ByteCount, 2)
        For i = 0 To source.Length - 1
            Dim recovered = BitConverter.ToInt16(pcmOut, i * 2)
            Assert.IsTrue(Math.Abs(CInt(recovered) - CInt(source(i))) <= 1,
                $"full-math null: sample {i}: {source(i)} -> {recovered} (a hidden clamp or broken math path would show here)")
        Next
    End Sub

    <TestMethod>
    Public Sub NullTest_FloatSource_UnityChain_ByteIdentity()
        ' SC-001 variant 3: the zero-conversion playback path - a float source
        ' through the unity chain must be BYTE-IDENTICAL (no conversion anywhere).
        Dim values(511) As Single
        For i = 0 To 511
            values(i) = CSng(Math.Sin(i * 0.1)) * 0.9F
        Next
        values(10) = 1.5F : values(11) = -1.5F ' excursions must survive untouched

        Dim inputBytes = FloatsToBytes(values)
        Dim buf As New DSP.AudioBuffer(FloatStereo, inputBytes.Length, True)
        buf.CopyFrom(inputBytes, 0, inputBytes.Length)

        Using chain = BuildChain(FloatStereo, 1.0F, 1.0F)
            chain.Process(buf)
        End Using

        Dim outputBytes(buf.ByteCount - 1) As Byte
        buf.CopyTo(outputBytes, 0, buf.ByteCount)
        CollectionAssert.AreEqual(inputBytes, outputBytes,
            "float source through unity chain must be byte-identical (zero conversions)")
    End Sub

#End Region

End Class
