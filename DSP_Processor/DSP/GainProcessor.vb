Imports NAudio.Wave

Namespace DSP

    ''' <summary>
    ''' Gain / balance-pan / stereo-width processor for the DSP chain.
    '''
    ''' FEATURE 001 (float32-pipeline) - operates natively on the float32
    ''' processing domain via AudioBuffer.GetSample/SetSample (never bytes).
    ''' NO internal clamping: inter-stage excursions beyond +/-1.0 flow
    ''' undamaged (FR-007); range limiting happens only at the boundary
    ''' conversion (FR-008).
    '''
    ''' PAN = BALANCE LAW (Architect-ruled behavior change, clarify Q1):
    ''' the favored channel stays at unity for every pan position; only the
    ''' opposite channel attenuates, on a cosine taper cos(|pan| * pi/2).
    ''' Center pan is both channels x1.0 - mathematically transparent - so
    ''' the unity bypass below is a pure optimization with no behavior cliff.
    ''' (Supersedes the uncompensated constant-power law locked by feature
    ''' 003; released per FR-011 with the supersession recorded in
    ''' GainProcessorTests.vb and the changelog.)
    ''' </summary>
    Public Class GainProcessor
        Inherits ProcessorBase

        Private _gainLinear As Single = 1.0F ' Linear gain (1.0 = unity gain)
        Private _panPosition As Single = 0.0F ' Pan position: -1.0 (left) to +1.0 (right), 0 = center
        Private _stereoWidth As Single = 1.0F ' Stereo width: 0.0 (mono) to 2.0 (wide), 1.0 = normal (100%)


        ''' <summary>
        ''' Creates a new gain processor
        ''' </summary>
        ''' <param name="format">Wave format (float32 processing domain)</param>
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
                ' Floor at -60 dB: GainLinear allows 0.0 (mute), which would otherwise
                ' produce -Infinity and poison downstream UI math
                If _gainLinear <= 0.001F Then Return -60.0F
                Return Math.Max(-60.0F, 20.0F * CSng(Math.Log10(_gainLinear)))
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
        ''' Balance law: favored channel unity, opposite channel cosine taper (feature 001)
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
        ''' Gets or sets the stereo width (0.0 = mono, 1.0 = normal, 2.0 = wide)
        ''' Uses mid-side processing to adjust stereo image width
        ''' PHASE 2.7: Added for Master/Width control on Output stage
        ''' </summary>
        Public Property StereoWidth As Single
            Get
                Return _stereoWidth
            End Get
            Set(value As Single)
                ' Clamp to valid range (0.0 to 2.0)
                _stereoWidth = Math.Max(0.0F, Math.Min(2.0F, value))
            End Set
        End Property


        ''' <summary>
        ''' Process audio buffer (apply gain, balance pan, width) in float32
        ''' </summary>
        Protected Overrides Sub ProcessInternal(buffer As AudioBuffer)
            If buffer Is Nothing OrElse buffer.ByteCount = 0 Then
                Return
            End If

            ' Unity gain, center pan, and normal width - no processing needed, but STILL send to monitor.
            ' Under the balance law this fast path is a pure optimization: center pan is
            ' mathematically transparent, so skipping the math changes nothing (no behavior cliff).
            If Math.Abs(_gainLinear - 1.0F) < 0.001F AndAlso Math.Abs(_panPosition) < 0.001F AndAlso Math.Abs(_stereoWidth - 1.0F) < 0.001F Then
                ' DSP TAP POINT PATTERN: Send to monitor even when bypassing processing
                SendToMonitor(buffer)
                Return
            End If

            Dim totalSamples = buffer.FloatSampleCount

            ' Balance pan factors (clarify Q1 / AR-4): favored channel unity,
            ' opposite channel cosine taper - no boost anywhere, no clamping anywhere
            Dim leftPanGain As Single = 1.0F
            Dim rightPanGain As Single = 1.0F
            If _panPosition > 0.001F Then
                leftPanGain = CSng(Math.Cos(_panPosition * Math.PI / 2.0)) ' pan right: left tapers
            ElseIf _panPosition < -0.001F Then
                rightPanGain = CSng(Math.Cos(-_panPosition * Math.PI / 2.0)) ' pan left: right tapers
            End If

            If Format.Channels = 1 Then
                ' Mono - gain only (no panning)
                For i = 0 To totalSamples - 1
                    buffer.SetSample(i, buffer.GetSample(i) * _gainLinear)
                Next

            ElseIf Format.Channels = 2 Then
                ' Stereo - width (M/S), then gain and balance pan; no intermediate rounding
                For i = 0 To totalSamples - 2 Step 2
                    Dim leftSample = buffer.GetSample(i)
                    Dim rightSample = buffer.GetSample(i + 1)

                    ' Stereo width via mid-side: Mid = (L+R)/2, Side = (L-R)/2
                    Dim mid As Single = (leftSample + rightSample) * 0.5F
                    Dim side As Single = (leftSample - rightSample) * 0.5F * _stereoWidth

                    ' Reconstruct, apply gain and balance pan (excursions flow undamaged)
                    buffer.SetSample(i, (mid + side) * _gainLinear * leftPanGain)
                    buffer.SetSample(i + 1, (mid - side) * _gainLinear * rightPanGain)
                Next

            Else
                ' Multi-channel - per-channel gain only
                For i = 0 To totalSamples - 1
                    buffer.SetSample(i, buffer.GetSample(i) * _gainLinear)
                Next
            End If

            ' DSP TAP POINT PATTERN: Send processed output to monitor (after gain/pan applied)
            SendToMonitor(buffer)
        End Sub

    End Class

End Namespace
