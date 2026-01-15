Imports Newtonsoft.Json
Imports DSP_Processor.Models
Imports DSP_Processor.Recording
Imports DSP_Processor.Managers

Namespace Audio.Routing

    ''' <summary>
    ''' Complete pipeline configuration - composite of all routing settings
    ''' Auto-saved to JSON on every change
    ''' </summary>
    Public Class PipelineConfiguration

        ''' <summary>Configuration file version for migration support</summary>
        <JsonProperty("version")>
        Public Property Version As Integer = 1

        ''' <summary>Last modification timestamp</summary>
        <JsonProperty("lastModified")>
        Public Property LastModified As DateTime = DateTime.Now

        ''' <summary>Audio source configuration</summary>
        <JsonProperty("source")>
        Public Property Source As SourceConfiguration

        ''' <summary>DSP processing configuration</summary>
        <JsonProperty("processing")>
        Public Property Processing As ProcessingConfiguration

        ''' <summary>Monitoring/metering configuration</summary>
        <JsonProperty("monitoring")>
        Public Property Monitoring As MonitoringConfiguration

        ''' <summary>Recording/playback destinations</summary>
        <JsonProperty("destination")>
        Public Property Destination As DestinationConfiguration

        Public Sub New()
            ' Initialize with defaults
            Source = New SourceConfiguration()
            Processing = New ProcessingConfiguration()
            Monitoring = New MonitoringConfiguration()
            Destination = New DestinationConfiguration()
        End Sub

    End Class

#Region "Source Configuration"

    ''' <summary>
    ''' Audio source configuration - where audio comes from
    ''' </summary>
    Public Class SourceConfiguration

        ''' <summary>Active audio source type</summary>
        <JsonProperty("activeSource")>
        Public Property ActiveSource As AudioSourceType = AudioSourceType.Microphone

        ''' <summary>Device settings for selected source</summary>
        <JsonProperty("sourceSettings")>
        Public Property SourceSettings As AudioDeviceSettings

        Public Sub New()
            ' Will be populated from AudioSettingsPanel
            SourceSettings = Nothing
        End Sub

    End Class

    ''' <summary>Audio source types</summary>
    Public Enum AudioSourceType
        ''' <summary>Live microphone input</summary>
        Microphone = 0

        ''' <summary>Line input (future)</summary>
        LineIn = 1

        ''' <summary>File playback</summary>
        File = 2

        ''' <summary>Network audio stream (future)</summary>
        NetworkStream = 3
    End Enum

#End Region

#Region "Processing Configuration"

    ''' <summary>
    ''' DSP processing configuration - how audio is processed
    ''' </summary>
    Public Class ProcessingConfiguration

        ''' <summary>Enable/disable DSP processing pipeline</summary>
        <JsonProperty("enableDSP")>
        Public Property EnableDSP As Boolean = False

        ''' <summary>Ordered list of processors in the chain</summary>
        <JsonProperty("processingChain")>
        Public Property ProcessingChain As List(Of ProcessorType)

        ''' <summary>Input gain (0.0 to 2.0, where 1.0 = 100%)</summary>
        <JsonProperty("inputGain")>
        Public Property InputGain As Single = 1.0F

        ''' <summary>Output gain (0.0 to 2.0, where 1.0 = 100%)</summary>
        <JsonProperty("outputGain")>
        Public Property OutputGain As Single = 1.0F

        Public Sub New()
            ProcessingChain = New List(Of ProcessorType)()
        End Sub

    End Class

    ''' <summary>DSP processor types</summary>
    Public Enum ProcessorType
        ''' <summary>No processing</summary>
        None = 0

        ''' <summary>Gain stage only</summary>
        Gain = 1

        ''' <summary>Equalizer (future)</summary>
        EQ = 2

        ''' <summary>Compressor/limiter (future)</summary>
        Compressor = 3

        ''' <summary>Reverb effect (future)</summary>
        Reverb = 4
    End Enum

#End Region

#Region "Monitoring Configuration"

    ''' <summary>
    ''' Monitoring configuration - FFT displays and level meters
    ''' </summary>
    Public Class MonitoringConfiguration

        ''' <summary>Where to tap audio for input FFT</summary>
        <JsonProperty("inputFFTTap")>
        Public Property InputFFTTap As TapPoint = TapPoint.PreDSP

        ''' <summary>Where to tap audio for output FFT</summary>
        <JsonProperty("outputFFTTap")>
        Public Property OutputFFTTap As TapPoint = TapPoint.PostDSP

        ''' <summary>Where to tap audio for level meters</summary>
        <JsonProperty("levelMeterTap")>
        Public Property LevelMeterTap As TapPoint = TapPoint.PreDSP

        ''' <summary>Enable input FFT display</summary>
        <JsonProperty("enableInputFFT")>
        Public Property EnableInputFFT As Boolean = True

        ''' <summary>Enable output FFT display</summary>
        <JsonProperty("enableOutputFFT")>
        Public Property EnableOutputFFT As Boolean = True

        ''' <summary>Enable level meter</summary>
        <JsonProperty("enableLevelMeter")>
        Public Property EnableLevelMeter As Boolean = True

        ''' <summary>FFT update interval in milliseconds (default: 50ms = 20 FPS)</summary>
        <JsonProperty("fftUpdateInterval")>
        Public Property FFTUpdateInterval As Integer = 50

        ''' <summary>Level meter update interval in milliseconds (default: 20ms = 50 FPS)</summary>
        <JsonProperty("meterUpdateInterval")>
        Public Property MeterUpdateInterval As Integer = 20

    End Class

    ''' <summary>Audio tap points in processing pipeline</summary>
    Public Enum TapPoint
        ''' <summary>Disabled - no tap</summary>
        None = 0

        ''' <summary>Before DSP processing (raw input)</summary>
        PreDSP = 1

        ''' <summary>After gain stage</summary>
        PostGain = 2

        ''' <summary>After all DSP processing</summary>
        PostDSP = 3

        ''' <summary>Before final output</summary>
        PreOutput = 4
    End Enum

#End Region

#Region "Destination Configuration"

    ''' <summary>
    ''' Destination configuration - where audio goes
    ''' </summary>
    Public Class DestinationConfiguration

        ''' <summary>Enable recording to file</summary>
        <JsonProperty("enableRecording")>
        Public Property EnableRecording As Boolean = False

        ''' <summary>Enable playback to speakers</summary>
        <JsonProperty("enablePlayback")>
        Public Property EnablePlayback As Boolean = False

        ''' <summary>Enable monitoring to headphones (future)</summary>
        <JsonProperty("enableMonitoring")>
        Public Property EnableMonitoring As Boolean = False

        ''' <summary>Recording options (mode, loop count, etc.)</summary>
        <JsonProperty("recordingOptions")>
        Public Property RecordingOptions As RecordingOptions

        Public Sub New()
            ' Will be populated from recording settings
            RecordingOptions = Nothing
        End Sub

    End Class

#End Region

End Namespace
