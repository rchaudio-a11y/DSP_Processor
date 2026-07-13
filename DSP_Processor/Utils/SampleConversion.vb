Imports System.Threading

Namespace Utils

    ''' <summary>
    ''' THE CANONICAL SAMPLE-FORMAT CONVERSION PAIR (feature 001-float32-pipeline).
    ''' The ONLY code in the engine that converts between integer PCM and the
    ''' float32 processing domain - invoked exclusively at I/O boundaries
    ''' (device/file in, recorded file out). Deterministic: NO dither (clarify Q2).
    ''' Scale pair: int16->float divides by 32768 (matches NAudio's reader);
    ''' float->int16 multiplies by 32767 (locked feature-003 contract).
    ''' Asymmetric round-trip error is at most 1 LSB - the null-test allowance.
    ''' </summary>
    Public Module SampleConversion

        ' FR-009 observable seam: count of non-finite samples (NaN/Infinity)
        ' encountered at the output boundary. Test-visible (InternalsVisibleTo);
        ' log fires once per session (rate-limit) - the count carries the tally.
        Private _nonFiniteCount As Integer = 0
        Private _nonFiniteLogged As Integer = 0

        ''' <summary>Total non-finite samples clamped at the output boundary (FR-009 seam)</summary>
        Friend ReadOnly Property NonFiniteCount As Integer
            Get
                Return Interlocked.CompareExchange(_nonFiniteCount, 0, 0)
            End Get
        End Property

        ''' <summary>
        ''' Convert float32 processing-domain samples to 16-bit PCM (boundary exit).
        ''' Symmetric scaling: +1.0 -> +32767, -1.0 -> -32767 (-32768 is unreachable);
        ''' over-range input clamps to +/-1.0 first, so it clips and never wraps.
        ''' Non-finite input (FR-009): NaN -> 0, +/-Infinity -> full-scale clamp,
        ''' NonFiniteCount incremented, logged once per session - never silent garbage.
        ''' </summary>
        ''' <param name="floatBuffer">Interleaved 32-bit IEEE float bytes</param>
        ''' <param name="byteCount">Valid byte count in floatBuffer</param>
        ''' <param name="channels">Channel count. Interleaving is preserved as-is -
        ''' conversion is per-sample, so this parameter does not affect the result;
        ''' kept for call-site clarity.</param>
        Public Function FloatToPcm16(floatBuffer As Byte(), byteCount As Integer, channels As Integer) As Byte()
            Dim sampleCount = byteCount \ 4 ' 4 bytes per float sample
            Dim pcm16Buffer(sampleCount * 2 - 1) As Byte ' 2 bytes per 16-bit sample
            FloatToPcm16(floatBuffer, byteCount, pcm16Buffer)
            Return pcm16Buffer
        End Function

        ''' <summary>
        ''' Scratch-buffer overload: converts into a caller-provided destination
        ''' (zero steady-state allocation for the record write exit).
        ''' </summary>
        ''' <returns>Bytes written to destination (byteCount \ 4 * 2)</returns>
        Public Function FloatToPcm16(floatBuffer As Byte(), byteCount As Integer, destination As Byte()) As Integer
            Dim sampleCount = byteCount \ 4

            For i = 0 To sampleCount - 1
                ' Read float sample (-1.0 to +1.0 nominal; excursions clamp below)
                Dim floatSample = BitConverter.ToSingle(floatBuffer, i * 4)

                ' FR-009: non-finite guard - defined outputs, observable, never silent
                If Single.IsNaN(floatSample) OrElse Single.IsInfinity(floatSample) Then
                    Interlocked.Increment(_nonFiniteCount)
                    If Interlocked.CompareExchange(_nonFiniteLogged, 1, 0) = 0 Then
                        Logger.Instance.Error($"Non-finite sample at output boundary (value: {floatSample}) - clamped; a processor is producing NaN/Infinity", Nothing, "SampleConversion")
                    End If
                    floatSample = If(Single.IsNaN(floatSample), 0.0F, If(floatSample > 0.0F, 1.0F, -1.0F))
                End If

                ' Clamp to valid range (range limiting happens ONLY here, FR-008)
                floatSample = Math.Max(-1.0F, Math.Min(1.0F, floatSample))

                ' Convert to 16-bit integer
                Dim int16Sample = CShort(floatSample * 32767.0F)

                ' Write as little-endian bytes
                destination(i * 2) = CByte(int16Sample And &HFF)
                destination(i * 2 + 1) = CByte((int16Sample >> 8) And &HFF)
            Next

            Return sampleCount * 2
        End Function

        ''' <summary>
        ''' Convert 16-bit PCM to float32 processing-domain samples (boundary entry).
        ''' Scale: divide by 32768 - agrees with NAudio's AudioFileReader so every
        ''' entry into the domain uses one scale (research R3).
        ''' </summary>
        Public Function Pcm16ToFloat(pcmBuffer As Byte(), byteCount As Integer) As Byte()
            Dim sampleCount = byteCount \ 2
            Dim floatBuffer(sampleCount * 4 - 1) As Byte
            Pcm16ToFloat(pcmBuffer, byteCount, floatBuffer)
            Return floatBuffer
        End Function

        ''' <summary>
        ''' Scratch-buffer overload: converts into a caller-provided destination
        ''' (zero steady-state allocation for int16 device capture entry).
        ''' </summary>
        ''' <returns>Bytes written to destination (byteCount \ 2 * 4)</returns>
        Public Function Pcm16ToFloat(pcmBuffer As Byte(), byteCount As Integer, destination As Byte()) As Integer
            Dim sampleCount = byteCount \ 2

            For i = 0 To sampleCount - 1
                Dim sample = BitConverter.ToInt16(pcmBuffer, i * 2)
                Dim floatSample As Single = sample / 32768.0F

                ' Write float as little-endian bytes without allocation
                Dim bits = BitConverter.SingleToInt32Bits(floatSample)
                Dim o = i * 4
                destination(o) = CByte(bits And &HFF)
                destination(o + 1) = CByte((bits >> 8) And &HFF)
                destination(o + 2) = CByte((bits >> 16) And &HFF)
                destination(o + 3) = CByte((bits >> 24) And &HFF)
            Next

            Return sampleCount * 4
        End Function

    End Module

End Namespace
