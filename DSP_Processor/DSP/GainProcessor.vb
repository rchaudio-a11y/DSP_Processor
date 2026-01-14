Imports NAudio.Wave

Namespace DSP

    ''' <summary>
    ''' Simple gain (volume) processor for DSP chain
    ''' </summary>
    Public Class GainProcessor
        Inherits ProcessorBase

        Private _gainLinear As Single = 1.0F ' Linear gain (1.0 = unity gain)

        ''' <summary>
        ''' Creates a new gain processor
        ''' </summary>
        ''' <param name="format">Wave format</param>
        Public Sub New(format As WaveFormat)
            MyBase.New(format)
        End Sub

        ''' <summary>
        ''' Gets the name of this processor
        ''' </summary>
        Public Overrides ReadOnly Property Name As String
            Get
                Return "Gain"
            End Get
        End Property

        ''' <summary>
        ''' Gets the latency introduced by this processor (gain has no latency)
        ''' </summary>
        Public Overrides ReadOnly Property LatencySamples As Integer
            Get
                Return 0 ' Gain is instantaneous, no latency
            End Get
        End Property

        ''' <summary>
        ''' Gets or sets the gain in dB (-60 to +20 dB)
        ''' </summary>
        Public Property GainDB As Single
            Get
                Return 20.0F * Math.Log10(_gainLinear)
            End Get
            Set(value As Single)
                ' Clamp to safe range
                value = Math.Max(-60.0F, Math.Min(20.0F, value))
                ' Convert dB to linear (10^(dB/20))
                _gainLinear = CSng(Math.Pow(10.0, value / 20.0))
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets the gain as linear multiplier (0.0 to 10.0)
        ''' </summary>
        Public Property GainLinear As Single
            Get
                Return _gainLinear
            End Get
            Set(value As Single)
                _gainLinear = Math.Max(0.0F, Math.Min(10.0F, value))
            End Set
        End Property

        ''' <summary>
        ''' Process audio buffer (apply gain)
        ''' </summary>
        Protected Overrides Sub ProcessInternal(buffer As AudioBuffer)
            If buffer Is Nothing OrElse buffer.ByteCount = 0 Then
                Return
            End If

            ' Unity gain - no processing needed
            If Math.Abs(_gainLinear - 1.0F) < 0.001F Then
                Return
            End If

            ' Process 16-bit PCM samples
            Dim sampleCount = buffer.ByteCount \ Format.BlockAlign

            For i = 0 To sampleCount - 1
                Dim offset = i * Format.BlockAlign

                ' Process each channel
                For ch = 0 To Format.Channels - 1
                    Dim sampleOffset = offset + (ch * 2) ' 2 bytes per sample (16-bit)

                    ' Read 16-bit sample
                    Dim sample = BitConverter.ToInt16(buffer.Buffer, sampleOffset)

                    ' Apply gain
                    Dim gained = CInt(sample * _gainLinear)

                    ' Clamp to prevent clipping
                    gained = Math.Max(-32768, Math.Min(32767, gained))

                    ' Write back
                    Dim bytes = BitConverter.GetBytes(CShort(gained))
                    buffer.Buffer(sampleOffset) = bytes(0)
                    buffer.Buffer(sampleOffset + 1) = bytes(1)
                Next
            Next
        End Sub

    End Class

End Namespace
