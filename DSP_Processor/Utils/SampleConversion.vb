Namespace Utils

    ''' <summary>
    ''' Sample-format conversion utilities (feature 003-tap-consolidation, research R5).
    ''' Extracted from AudioRouter so the conversion is a first-class, testable unit.
    ''' NOTE: WasapiEngine keeps a private channel-agnostic variant on the capture
    ''' path; consolidating it is deferred to feature 001-float32-pipeline.
    ''' </summary>
    Public Module SampleConversion

        ''' <summary>
        ''' Convert IEEE Float samples (AudioFileReader format) to 16-bit PCM.
        ''' Symmetric scaling: +1.0 -> +32767, -1.0 -> -32767 (-32768 is unreachable);
        ''' over-range input clamps to +/-1.0 first, so it clips and never wraps.
        ''' </summary>
        ''' <param name="floatBuffer">Interleaved 32-bit IEEE float bytes</param>
        ''' <param name="byteCount">Valid byte count in floatBuffer</param>
        ''' <param name="channels">Channel count. Interleaving is preserved as-is -
        ''' conversion is per-sample, so this parameter does not affect the result;
        ''' kept for call-site clarity and the float32-pipeline migration.</param>
        Public Function FloatToPcm16(floatBuffer As Byte(), byteCount As Integer, channels As Integer) As Byte()
            Dim sampleCount = byteCount \ 4 ' 4 bytes per float sample
            Dim pcm16Buffer(sampleCount * 2 - 1) As Byte ' 2 bytes per 16-bit sample

            For i = 0 To sampleCount - 1
                ' Read float sample (-1.0 to +1.0)
                Dim floatSample = BitConverter.ToSingle(floatBuffer, i * 4)

                ' Clamp to valid range
                floatSample = Math.Max(-1.0F, Math.Min(1.0F, floatSample))

                ' Convert to 16-bit integer
                Dim int16Sample = CShort(floatSample * 32767.0F)

                ' Write as little-endian bytes
                pcm16Buffer(i * 2) = CByte(int16Sample And &HFF)
                pcm16Buffer(i * 2 + 1) = CByte((int16Sample >> 8) And &HFF)
            Next

            Return pcm16Buffer
        End Function

    End Module

End Namespace
