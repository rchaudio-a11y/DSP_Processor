Imports NAudio.Wave

Namespace DSP

    ''' <summary>
    ''' Simple pass-through processor for testing audio routing infrastructure.
    ''' Does not modify audio - just validates the DSP chain works.
    ''' </summary>
    Public Class PassThroughProcessor
        Inherits ProcessorBase

        Public Sub New(format As WaveFormat)
            MyBase.New(format)
        End Sub

        ''' <summary>
        ''' Gets the name of this processor
        ''' </summary>
        Public Overrides ReadOnly Property Name As String
            Get
                Return "Pass-Through"
            End Get
        End Property

        ''' <summary>
        ''' Gets the latency introduced by this processor (pass-through has no latency)
        ''' </summary>
        Public Overrides ReadOnly Property LatencySamples As Integer
            Get
                Return 0 ' Pass-through is instantaneous
            End Get
        End Property

        ''' <summary>
        ''' Process audio buffer (does nothing - pass through)
        ''' </summary>
        Protected Overrides Sub ProcessInternal(buffer As AudioBuffer)
            ' Do nothing - just pass audio through
            ' This validates the routing infrastructure works
            
            ' Optional: Log occasionally for debugging
            Static callCount As Integer = 0
            callCount += 1
            
            If callCount Mod 1000 = 0 Then ' Log every 1000 calls
                Utils.Logger.Instance.Debug($"PassThrough: Processed {callCount} buffers ({buffer.SampleCount} samples)", "PassThroughProcessor")
            End If
        End Sub

    End Class

End Namespace
