Imports DSP_Processor.Managers

Namespace Audio.Routing

#Region "Event Arguments"

    ''' <summary>
    ''' Event arguments for routing configuration changes
    ''' </summary>
    Public Class RoutingChangedEventArgs
        Inherits EventArgs

        ''' <summary>Previous configuration (before change)</summary>
        Public Property OldConfiguration As PipelineConfiguration

        ''' <summary>New configuration (after change)</summary>
        Public Property NewConfiguration As PipelineConfiguration

        Public Sub New(oldConfig As PipelineConfiguration, newConfig As PipelineConfiguration)
            OldConfiguration = oldConfig
            NewConfiguration = newConfig
        End Sub

    End Class

    ''' <summary>
    ''' Extended audio buffer event args with routing information
    ''' </summary>
    Public Class AudioBufferRoutingEventArgs
        Inherits AudioBufferEventArgs

        ''' <summary>Where in the pipeline this buffer was tapped from (optional)</summary>
        Public Property TapPoint As TapPoint? = Nothing

        ''' <summary>Purpose/destination of this buffer</summary>
        Public Property Purpose As BufferPurpose = BufferPurpose.Unknown

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(buffer As Byte(), bitsPerSample As Integer, channels As Integer, sampleRate As Integer)
            Me.Buffer = buffer
            Me.BitsPerSample = bitsPerSample
            Me.Channels = channels
            Me.SampleRate = sampleRate
        End Sub

    End Class

    ''' <summary>Buffer purpose - what the buffer will be used for</summary>
    Public Enum BufferPurpose
        ''' <summary>Unknown/unspecified</summary>
        Unknown = 0

        ''' <summary>For input FFT display</summary>
        InputFFT = 1

        ''' <summary>For output FFT display</summary>
        OutputFFT = 2

        ''' <summary>For level meter</summary>
        LevelMeter = 3

        ''' <summary>For recording to file</summary>
        Recording = 4

        ''' <summary>For playback to speakers</summary>
        Playback = 5

        ''' <summary>For monitoring (headphones)</summary>
        Monitoring = 6
    End Enum

#End Region

#Region "Routing Visualization"

    ''' <summary>
    ''' Represents the current active routing map for visualization/debugging
    ''' </summary>
    Public Class RoutingMap

        ''' <summary>List of active paths in the pipeline</summary>
        Public Property ActivePaths As New List(Of PathInfo)

        ''' <summary>
        ''' Generate human-readable representation of routing
        ''' </summary>
        Public Overrides Function ToString() As String
            Dim sb As New System.Text.StringBuilder()
            sb.AppendLine("=== Audio Pipeline Routing Map ===")
            sb.AppendLine()

            If ActivePaths.Count = 0 Then
                sb.AppendLine("  [No active paths]")
            Else
                For Each path In ActivePaths
                    sb.AppendLine($"  {path}")
                Next
            End If

            sb.AppendLine()
            sb.AppendLine($"Total Active Paths: {ActivePaths.Count}")

            Return sb.ToString()
        End Function

    End Class

    ''' <summary>
    ''' Information about a single path in the routing
    ''' </summary>
    Public Class PathInfo

        ''' <summary>Source of this path</summary>
        Public Property Source As String

        ''' <summary>Destination of this path</summary>
        Public Property Destination As String

        ''' <summary>Processing applied in this path</summary>
        Public Property Processing As String

        ''' <summary>Is this path currently active?</summary>
        Public Property IsActive As Boolean

        Public Overrides Function ToString() As String
            Dim status = If(IsActive, "?", "?")
            Dim process = If(String.IsNullOrEmpty(Processing), "Direct", Processing)
            Return $"{status} {Source} ? [{process}] ? {Destination}"
        End Function

    End Class

#End Region

End Namespace
