Namespace DSP

    ''' <summary>
    ''' Identifies available DSP tap points in the audio signal chain.
    ''' Each tap point represents a location where processed audio can be monitored.
    ''' </summary>
    Public Enum TapPoint
        ''' <summary>Raw audio before any DSP processing</summary>
        PreDSP = 0

        ''' <summary>After InputGainProcessor (first gain stage)</summary>
        PostInputGain = 1

        ''' <summary>After OutputGainProcessor (final gain stage)</summary>
        PostOutputGain = 2

        ''' <summary>Before final output (currently same as PostOutputGain)</summary>
        PreOutput = 3
    End Enum

End Namespace
