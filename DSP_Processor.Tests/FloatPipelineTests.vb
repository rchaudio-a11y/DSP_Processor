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

#Region "US2 - Inter-stage headroom, exit-only clamping (SC-003, FR-007/008)"

    <TestMethod>
    Public Sub Headroom_PlusSixDbThroughTwoStages_ArrivesClean()
        ' SC-003: full-scale signal x2.0 (stage one, +6 dB) then x0.5 (stage two)
        ' arrives identical - inter-stage clipping does not exist in the domain.
        Dim values = {0.9F, -0.9F, 0.999F, -0.999F, 0.5F, -0.25F}
        Dim inputBytes = FloatsToBytes(values)
        Dim buf As New DSP.AudioBuffer(FloatStereo, inputBytes.Length, True)
        buf.CopyFrom(inputBytes, 0, inputBytes.Length)

        Using chain = BuildChain(FloatStereo, 2.0F, 0.5F)
            chain.Process(buf)
        End Using

        For i = 0 To values.Length - 1
            Assert.AreEqual(values(i), buf.GetSample(i),
                $"sample {i}: x2.0 then x0.5 must be exact identity (powers of two) - no inter-stage clamp")
        Next
    End Sub

    <TestMethod>
    Public Sub Headroom_IntermediateBufferHoldsExcursions_Unclamped()
        ' FR-007 direct observation: after ONLY the boost stage, the buffer holds
        ' values beyond full scale - the processor did not clamp internally.
        Dim values = {0.9F, -0.9F}
        Dim inputBytes = FloatsToBytes(values)
        Dim buf As New DSP.AudioBuffer(FloatStereo, inputBytes.Length, True)
        buf.CopyFrom(inputBytes, 0, inputBytes.Length)

        Using boost As New DSP.GainProcessor(FloatStereo) With {.GainLinear = 2.0F}
            boost.Process(buf)
        End Using

        Assert.AreEqual(1.8F, buf.GetSample(0), 0.0000005F, "excursion must be held unclamped in the buffer")
        Assert.AreEqual(-1.8F, buf.GetSample(1), 0.0000005F)
    End Sub

    <TestMethod>
    Public Sub Headroom_HotSignalAtBoundary_ClampsNeverWraps()
        ' FR-008: the SAME hot signal sent to the exit boundary clips predictably.
        Dim hot = FloatsToBytes({1.8F, -1.8F, 0.5F})
        Dim pcm = Utils.SampleConversion.FloatToPcm16(hot, hot.Length, 1)

        Assert.AreEqual(CShort(32767), BitConverter.ToInt16(pcm, 0), "positive clamp, never wrap")
        Assert.AreEqual(CShort(-32767), BitConverter.ToInt16(pcm, 2), "negative clamp, never wrap")
        Assert.IsTrue(Math.Abs(BitConverter.ToInt16(pcm, 4) - 16384) <= 1, "in-range samples unaffected")
    End Sub

#End Region

#Region "US2 - Allocation audit + denormal sanity (SC-006, Constitution IV; analysis C3)"

    <TestMethod>
    Public Sub HotLoop_SteadyState_ZeroAllocationsPerBlock()
        ' SC-006: after warmup, ProcessorChain.Process allocates NOTHING -
        ' the migration's zero-alloc claim as a regression-tested property.
        Dim blockBytes = 256 * FloatStereo.BlockAlign ' one production block
        Dim inputBytes(blockBytes - 1) As Byte
        For i = 0 To (blockBytes \ 4) - 1
            System.Buffer.BlockCopy(BitConverter.GetBytes(CSng(Math.Sin(i * 0.05)) * 0.8F), 0, inputBytes, i * 4, 4)
        Next
        Dim buf As New DSP.AudioBuffer(FloatStereo, blockBytes, True)

        Using chain = BuildChain(FloatStereo, 1.3F, 0.7F) ' non-unity: full math path
            ' Warmup (JIT, any lazy init)
            For w = 1 To 50
                buf.CopyFrom(inputBytes, 0, inputBytes.Length)
                chain.Process(buf)
            Next

            Dim before = GC.GetAllocatedBytesForCurrentThread()
            For n = 1 To 1000
                chain.Process(buf) ' steady state: same buffer, no copies
            Next
            Dim delta = GC.GetAllocatedBytesForCurrentThread() - before

            Assert.AreEqual(0L, delta, $"hot loop allocated {delta} bytes across 1000 blocks - must be ZERO (Constitution IV)")
        End Using
    End Sub

    <TestMethod>
    Public Sub Denormals_DecayTail_NoPathologicalSlowdown()
        ' Analysis C3 (spec edge case): denormal-range input must not degrade
        ' processing pathologically. Generous 5x bound - catches pathology, not noise.
        Dim blockBytes = 256 * FloatStereo.BlockAlign
        Dim buf As New DSP.AudioBuffer(FloatStereo, blockBytes, True)

        Dim normalBytes(blockBytes - 1) As Byte
        Dim denormalBytes(blockBytes - 1) As Byte
        For i = 0 To (blockBytes \ 4) - 1
            System.Buffer.BlockCopy(BitConverter.GetBytes(0.5F), 0, normalBytes, i * 4, 4)
            System.Buffer.BlockCopy(BitConverter.GetBytes(Single.Epsilon * 100.0F), 0, denormalBytes, i * 4, 4)
        Next

        Using chain = BuildChain(FloatStereo, 0.9F, 0.9F) ' decay-ish gains keep values denormal
            ' Warmup both paths
            For w = 1 To 20
                buf.CopyFrom(normalBytes, 0, blockBytes) : chain.Process(buf)
                buf.CopyFrom(denormalBytes, 0, blockBytes) : chain.Process(buf)
            Next

            Const iterations As Integer = 2000
            Dim sw = Diagnostics.Stopwatch.StartNew()
            For n = 1 To iterations
                buf.CopyFrom(normalBytes, 0, blockBytes)
                chain.Process(buf)
            Next
            sw.Stop()
            Dim normalTicks = Math.Max(sw.ElapsedTicks, 1L)

            sw.Restart()
            For n = 1 To iterations
                buf.CopyFrom(denormalBytes, 0, blockBytes)
                chain.Process(buf)
            Next
            sw.Stop()
            Dim denormalTicks = sw.ElapsedTicks

            Assert.IsTrue(denormalTicks <= normalTicks * 5,
                $"denormal input took {denormalTicks} ticks vs {normalTicks} normal (> 5x bound) - denormal pathology")
        End Using
    End Sub

#End Region

#Region "US3 - Balance pan law (SC-004; supersedes the 003-locked constant-power law)"

    Private Shared Function ProcessStereo(gain As Single, pan As Single, left As Single, right As Single) As (L As Single, R As Single)
        Dim gp As New DSP.GainProcessor(FloatStereo) With {.GainLinear = gain, .PanPosition = pan}
        Dim bytes = FloatsToBytes({left, right})
        Dim buf As New DSP.AudioBuffer(FloatStereo, bytes.Length, True)
        buf.CopyFrom(bytes, 0, bytes.Length)
        gp.Process(buf)
        Return (buf.GetSample(0), buf.GetSample(1))
    End Function

    <TestMethod>
    Public Sub CenterPan_Gain2_ExactlyDoubles()
        ' SC-004: the hidden ~-3 dB center attenuation is GONE - gain 2.0 at
        ' center yields exactly x2.0 (the defect feature 003 locked, now fixed).
        Dim result = ProcessStereo(2.0F, 0.0F, 0.25F, -0.25F)
        Assert.AreEqual(0.5F, result.L, "center pan: L = input x gain, exactly")
        Assert.AreEqual(-0.5F, result.R, "center pan: R = input x gain, exactly")
    End Sub

    <TestMethod>
    Public Sub BypassBoundary_NoStep()
        ' SC-004: sweeping across the unity-bypass threshold produces no level
        ' step > 0.01 dB. Gain 1.0 hits the bypass fast path; 1.001 runs the
        ' full math path - under the balance law both are (near-)transparent.
        Dim bypassed = ProcessStereo(1.0F, 0.0F, 0.5F, 0.5F)
        Dim fullPath = ProcessStereo(1.001F, 0.0F, 0.5F, 0.5F)

        Dim stepDb = Math.Abs(20.0 * Math.Log10(fullPath.L / bypassed.L))
        Dim expectedDb = 20.0 * Math.Log10(1.001) ' ~0.0087 dB from the gain itself
        Assert.IsTrue(Math.Abs(stepDb - expectedDb) < 0.001,
            $"bypass-boundary step: {stepDb:F5} dB vs expected {expectedDb:F5} dB from gain alone - the law itself must add NOTHING")
        Assert.IsTrue(stepDb < 0.01, $"total step {stepDb:F5} dB must be < 0.01 dB")
    End Sub

    <TestMethod>
    Public Sub PanSweep_FavoredChannelUnity()
        ' SC-004: the favored channel equals input x gain at EVERY pan position.
        Const gain As Single = 1.5F
        Const input As Single = 0.4F
        For Each pan In New Single() {-1.0F, -0.5F, -0.1F, 0.0F, 0.1F, 0.5F, 1.0F}
            Dim r = ProcessStereo(gain, pan, input, input)
            Dim favored = If(pan <= 0.0F, r.L, r.R)
            Assert.AreEqual(input * gain, favored, 0.0000005F,
                $"pan {pan}: favored channel must be input x gain (unity pan contribution)")
        Next
    End Sub

    <TestMethod>
    Public Sub PanSweep_OppositeTaper_CosineMonotonic()
        ' SC-004: opposite channel = cos(|pan| * pi/2) x input x gain, strictly
        ' decreasing in |pan|, and never a factor > 1.0 before user gain.
        Const input As Single = 0.4F
        Dim lastOpposite As Single = Single.MaxValue
        For Each pan In New Single() {0.1F, 0.25F, 0.5F, 0.75F, 0.9F, 1.0F}
            Dim r = ProcessStereo(1.0F, pan, input, input) ' pan right: L is opposite
            Dim expected = CSng(Math.Cos(pan * Math.PI / 2.0)) * input
            Assert.AreEqual(expected, r.L, 0.0000005F, $"pan {pan}: opposite must follow the cosine taper")
            Assert.IsTrue(r.L < lastOpposite, $"pan {pan}: taper must be strictly decreasing")
            Assert.IsTrue(r.L <= input + 0.0000005F AndAlso r.R <= input + 0.0000005F,
                $"pan {pan}: no boost anywhere before user gain")
            lastOpposite = r.L
        Next
    End Sub

#End Region

End Class
