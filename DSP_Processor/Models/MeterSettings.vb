Imports Newtonsoft.Json

Namespace Models

    ''' <summary>
    ''' Configuration for volume meter display and input volume
    ''' </summary>
    Public Class MeterSettings
        ' Input Volume
        Public Property InputVolumePercent As Integer = 100
        Public Property LinkToPlaybackVolume As Boolean = False
        
        ' Peak Behavior
        Public Property PeakHoldMs As Integer = 500
        Public Property PeakDecayDbPerSec As Single = 3.0F
        
        ' RMS Calculation
        Public Property RmsWindowMs As Integer = 50
        
        ' Meter Ballistics
        Public Property AttackMs As Integer = 0
        Public Property ReleaseMs As Integer = 300
        
        ' Clipping Detection
        Public Property ClipThresholdDb As Single = -0.1F
        Public Property ClipHoldMs As Integer = 2000
        Public Property ClipColor As Drawing.Color = Drawing.Color.Red
        
        ' Meter Appearance
        Public Property MeterStyle As MeterStyleType = MeterStyleType.Classic
        Public Property ScaleType As MeterScaleType = MeterScaleType.dBFS
        Public Property UpdateRateFps As Integer = 30
        
        ''' <summary>
        ''' Serialize settings to JSON
        ''' </summary>
        Public Function ToJson() As String
            Return JsonConvert.SerializeObject(Me, Formatting.Indented)
        End Function
        
        ''' <summary>
        ''' Deserialize settings from JSON
        ''' </summary>
        Public Shared Function FromJson(json As String) As MeterSettings
            Return JsonConvert.DeserializeObject(Of MeterSettings)(json)
        End Function
        
        ''' <summary>
        ''' Fast response preset for transient-heavy material
        ''' </summary>
        Public Shared Function FastResponsePreset() As MeterSettings
            Return New MeterSettings() With {
                .PeakHoldMs = 250,
                .PeakDecayDbPerSec = 12.0F,
                .RmsWindowMs = 30,
                .AttackMs = 0,
                .ReleaseMs = 100,
                .ClipThresholdDb = -0.1F,
                .MeterStyle = MeterStyleType.Classic,
                .UpdateRateFps = 60
            }
        End Function
        
        ''' <summary>
        ''' Slow response preset for broadcast-style metering
        ''' </summary>
        Public Shared Function SlowResponsePreset() As MeterSettings
            Return New MeterSettings() With {
                .PeakHoldMs = 1000,
                .PeakDecayDbPerSec = 3.0F,
                .RmsWindowMs = 100,
                .AttackMs = 50,
                .ReleaseMs = 1000,
                .ClipThresholdDb = -0.3F,
                .MeterStyle = MeterStyleType.Classic,
                .UpdateRateFps = 30
            }
        End Function
        
        ''' <summary>
        ''' BBC PPM (Peak Programme Meter) style preset
        ''' </summary>
        Public Shared Function BroadcastPreset() As MeterSettings
            Return New MeterSettings() With {
                .PeakHoldMs = 500,
                .PeakDecayDbPerSec = 24.0F,
                .RmsWindowMs = 50,
                .AttackMs = 10,
                .ReleaseMs = 2000,
                .ClipThresholdDb = 0.0F,
                .MeterStyle = MeterStyleType.BBC,
                .UpdateRateFps = 30
            }
        End Function
    End Class
    
    ''' <summary>
    ''' Meter visual style types
    ''' </summary>
    Public Enum MeterStyleType
        Classic = 0   ' Green/Yellow/Red (standard)
        BBC = 1       ' Green/Amber/Red (PPM style)
        Nordic = 2    ' Blue/White/Red
        Broadcast = 3 ' VU meter style
    End Enum
    
    ''' <summary>
    ''' Meter scale types
    ''' </summary>
    Public Enum MeterScaleType
        dBFS = 0  ' Digital Full Scale
        VU = 1    ' Volume Unit (analog style)
    End Enum

End Namespace
