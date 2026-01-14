Imports NAudio.Wave

Namespace AudioIO

    ''' <summary>
    ''' NAudio IWaveProvider implementation that reads from DSP output buffer.
    ''' PULL-BASED: Drives DSP processing on-demand when WaveOut requests data.
    ''' </summary>
    Public Class DSPOutputProvider
        Implements IWaveProvider

#Region "Private Fields"

        Private ReadOnly dspThread As DSP.DSPThread
        Private ReadOnly format As WaveFormat

#End Region

#Region "Properties"

        ''' <summary>Gets the wave format</summary>
        Public ReadOnly Property WaveFormat As WaveFormat Implements IWaveProvider.WaveFormat
            Get
                Return format
            End Get
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Creates a new DSP output provider
        ''' </summary>
        Public Sub New(dspThread As DSP.DSPThread)
            If dspThread Is Nothing Then
                Throw New ArgumentNullException(NameOf(dspThread))
            End If

            Me.dspThread = dspThread
            Me.format = dspThread.Format
        End Sub

#End Region

#Region "IWaveProvider Implementation"

        ''' <summary>
        ''' Reads audio data from DSP output buffer.
        ''' REAL-TIME SAFE: Only reads from buffer, NEVER does DSP work.
        ''' WaveOut audio callback must be fast and non-blocking.
        ''' </summary>
        Public Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer Implements IWaveProvider.Read
            If buffer Is Nothing Then
                Throw New ArgumentNullException(NameOf(buffer))
            End If
            If offset < 0 OrElse offset >= buffer.Length Then
                Throw New ArgumentOutOfRangeException(NameOf(offset))
            End If
            If count < 0 OrElse offset + count > buffer.Length Then
                Throw New ArgumentOutOfRangeException(NameOf(count))
            End If

            ' REAL-TIME SAFE: Just read from output buffer
            ' DSP worker thread keeps this filled ahead of time
            Dim bytesRead = dspThread.ReadOutput(buffer, offset, count)

            ' If not enough data, pad with silence (prevents glitches)
            If bytesRead < count Then
                Array.Clear(buffer, offset + bytesRead, count - bytesRead)
            End If

            ' Always return requested amount to keep WaveOut happy
            Return count
        End Function

#End Region

    End Class

End Namespace
