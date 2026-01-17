Imports NAudio.Wave

Namespace DSP

    ''' <summary>
    ''' Simple gain (volume) processor for DSP chain
    ''' </summary>
    Public Class GainProcessor
        Inherits ProcessorBase

        Private _gainLinear As Single = 1.0F ' Linear gain (1.0 = unity gain)
        Private _panPosition As Single = 0.0F ' Pan position: -1.0 (left) to +1.0 (right), 0 = center

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
        ''' Gets or sets the pan position (-1.0 = full left, 0.0 = center, +1.0 = full right)
        ''' Uses constant-power pan law to maintain perceived loudness
        ''' </summary>
        Public Property PanPosition As Single
            Get
                Return _panPosition
            End Get
            Set(value As Single)
                ' Clamp to valid range
                _panPosition = Math.Max(-1.0F, Math.Min(1.0F, value))
            End Set
        End Property

        ''' <summary>
        ''' Process audio buffer (apply gain)
        ''' </summary>
        Protected Overrides Sub ProcessInternal(buffer As AudioBuffer)
            If buffer Is Nothing OrElse buffer.ByteCount = 0 Then
                Return
            End If

            ' Unity gain and center pan - no processing needed, but STILL send to monitor
            If Math.Abs(_gainLinear - 1.0F) < 0.001F AndAlso Math.Abs(_panPosition) < 0.001F Then
                ' DSP TAP POINT PATTERN: Send to monitor even when bypassing processing
                SendToMonitor(buffer)
                Return
            End If

            ' Process 16-bit PCM samples
            Dim sampleCount = buffer.ByteCount \ Format.BlockAlign
            
            ' Calculate pan gains once (constant-power law)
            Dim panAngle = (_panPosition + 1.0F) * CSng(Math.PI) / 4.0F ' 0 to ?/2
            Dim leftPanGain = CSng(Math.Cos(panAngle))
            Dim rightPanGain = CSng(Math.Sin(panAngle))

            For i = 0 To sampleCount - 1
                Dim offset = i * Format.BlockAlign

                ' Process each channel with pan
                If Format.Channels = 1 Then
                    ' Mono - just apply gain (no panning)
                    Dim sampleOffset = offset
                    Dim sample = BitConverter.ToInt16(buffer.Buffer, sampleOffset)
                    Dim gained = CInt(sample * _gainLinear)
                    gained = Math.Max(-32768, Math.Min(32767, gained))
                    Dim bytes = BitConverter.GetBytes(CShort(gained))
                    buffer.Buffer(sampleOffset) = bytes(0)
                    buffer.Buffer(sampleOffset + 1) = bytes(1)
                    
                ElseIf Format.Channels = 2 Then
                    ' Stereo - apply gain AND pan
                    ' Left channel
                    Dim leftOffset = offset
                    Dim leftSample = BitConverter.ToInt16(buffer.Buffer, leftOffset)
                    Dim leftGained = CInt(leftSample * _gainLinear * leftPanGain)
                    leftGained = Math.Max(-32768, Math.Min(32767, leftGained))
                    Dim leftBytes = BitConverter.GetBytes(CShort(leftGained))
                    buffer.Buffer(leftOffset) = leftBytes(0)
                    buffer.Buffer(leftOffset + 1) = leftBytes(1)
                    
                    ' Right channel
                    Dim rightOffset = offset + 2
                    Dim rightSample = BitConverter.ToInt16(buffer.Buffer, rightOffset)
                    Dim rightGained = CInt(rightSample * _gainLinear * rightPanGain)
                    rightGained = Math.Max(-32768, Math.Min(32767, rightGained))
                    Dim rightBytes = BitConverter.GetBytes(CShort(rightGained))
                    buffer.Buffer(rightOffset) = rightBytes(0)
                    buffer.Buffer(rightOffset + 1) = rightBytes(1)
                Else
                    ' Multi-channel - fall back to old per-channel gain only
                    For ch = 0 To Format.Channels - 1
                        Dim sampleOffset = offset + (ch * 2)
                        Dim sample = BitConverter.ToInt16(buffer.Buffer, sampleOffset)
                        Dim gained = CInt(sample * _gainLinear)
                        gained = Math.Max(-32768, Math.Min(32767, gained))
                        Dim bytes = BitConverter.GetBytes(CShort(gained))
                        buffer.Buffer(sampleOffset) = bytes(0)
                        buffer.Buffer(sampleOffset + 1) = bytes(1)
                    Next
                End If
            Next
            
            ' DSP TAP POINT PATTERN: Send processed output to monitor (after gain/pan applied)
            SendToMonitor(buffer)
        End Sub

    End Class

End Namespace
