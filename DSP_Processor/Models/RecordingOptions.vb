Imports Newtonsoft.Json

Namespace Models

    ''' <summary>
    ''' Recording mode types
    ''' </summary>
    Public Enum RecordingMode
        ''' <summary>Record until manually stopped</summary>
        Manual = 0
        
        ''' <summary>Record for a specific duration then auto-stop</summary>
        Timed = 1
        
        ''' <summary>Record multiple takes automatically</summary>
        LoopMode = 2
    End Enum

    ''' <summary>
    ''' Configuration for recording behavior and modes
    ''' </summary>
    Public Class RecordingOptions
        ''' <summary>
        ''' Current recording mode
        ''' </summary>
        Public Property Mode As RecordingMode = RecordingMode.Manual
        
        ''' <summary>
        ''' Duration for timed recordings (in seconds)
        ''' </summary>
        Public Property TimedDurationSeconds As Integer = 60
        
        ''' <summary>
        ''' Number of recordings in loop mode
        ''' </summary>
        Public Property LoopCount As Integer = 3
        
        ''' <summary>
        ''' Duration per recording in loop mode (in seconds)
        ''' </summary>
        Public Property LoopDurationSeconds As Integer = 30
        
        ''' <summary>
        ''' Delay between loop recordings (in seconds)
        ''' </summary>
        Public Property LoopDelaySeconds As Integer = 2
        
        ''' <summary>
        ''' Auto-increment take numbers in loop mode
        ''' </summary>
        Public Property AutoIncrementTakeNumbers As Boolean = True
        
        ''' <summary>
        ''' Serialize to JSON
        ''' </summary>
        Public Function ToJson() As String
            Return JsonConvert.SerializeObject(Me, Formatting.Indented)
        End Function
        
        ''' <summary>
        ''' Deserialize from JSON
        ''' </summary>
        Public Shared Function FromJson(json As String) As RecordingOptions
            Return JsonConvert.DeserializeObject(Of RecordingOptions)(json)
        End Function
        
        ''' <summary>
        ''' Get human-readable description of current mode
        ''' </summary>
        Public Function GetDescription() As String
            Select Case Mode
                Case RecordingMode.Manual
                    Return "Record until manually stopped"
                    
                Case RecordingMode.Timed
                    Dim ts = TimeSpan.FromSeconds(TimedDurationSeconds)
                    Return $"Record for {ts:mm\:ss}"
                    
                Case RecordingMode.LoopMode
                    Dim ts = TimeSpan.FromSeconds(LoopDurationSeconds)
                    Return $"Record {LoopCount} takes × {ts:mm\:ss}"
                    
                Case Else
                    Return "Unknown mode"
            End Select
        End Function
        
        ''' <summary>
        ''' Quick presets
        ''' </summary>
        Public Shared Function QuickTake30Sec() As RecordingOptions
            Return New RecordingOptions() With {
                .Mode = RecordingMode.Timed,
                .TimedDurationSeconds = 30
            }
        End Function
        
        Public Shared Function QuickTake60Sec() As RecordingOptions
            Return New RecordingOptions() With {
                .Mode = RecordingMode.Timed,
                .TimedDurationSeconds = 60
            }
        End Function
        
        Public Shared Function QuickLoop5Takes() As RecordingOptions
            Return New RecordingOptions() With {
                .Mode = RecordingMode.LoopMode,
                .LoopCount = 5,
                .LoopDurationSeconds = 30,
                .LoopDelaySeconds = 2
            }
        End Function
    End Class

End Namespace
