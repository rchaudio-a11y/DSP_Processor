Namespace Models

    ''' <summary>
    ''' FFT Spectrum Analyzer settings
    ''' </summary>
    Public Class SpectrumSettings
        Public Property FFTSize As Integer = 4096
        Public Property WindowFunction As String = "Hann"
        Public Property Smoothing As Integer = 70
        Public Property PeakHoldEnabled As Boolean = False

        ''' <summary>
        ''' Serialize to JSON
        ''' </summary>
        Public Function ToJson() As String
            Return Newtonsoft.Json.JsonConvert.SerializeObject(Me, Newtonsoft.Json.Formatting.Indented)
        End Function

        ''' <summary>
        ''' Deserialize from JSON
        ''' </summary>
        Public Shared Function FromJson(json As String) As SpectrumSettings
            Return Newtonsoft.Json.JsonConvert.DeserializeObject(Of SpectrumSettings)(json)
        End Function
    End Class

End Namespace
