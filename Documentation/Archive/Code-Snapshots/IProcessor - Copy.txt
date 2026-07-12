Namespace DSP

    ''' <summary>
    ''' Base interface for all DSP processors (EQ, Dynamics, Filters, etc.)
    ''' Used for creating processor chains in Phase 2
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 0, Task 0.2.1
    ''' Purpose: Standardize DSP modules for processor chaining
    ''' </remarks>
    Public Interface IProcessor

        ''' <summary>
        ''' Process audio buffer in-place
        ''' </summary>
        ''' <param name="buffer">Audio buffer to process</param>
        ''' <remarks>
        ''' Implementations should modify the buffer in-place for performance.
        ''' Buffer format is defined by AudioBuffer class (Phase 2).
        ''' </remarks>
        Sub Process(buffer As Object) ' AudioBuffer type will be defined in Phase 2

        ''' <summary>
        ''' Additional latency added by this processor (in samples)
        ''' </summary>
        ''' <returns>Latency in samples</returns>
        ''' <remarks>
        ''' Return 0 if processor adds no latency.
        ''' Used for latency compensation in multiband processing.
        ''' </remarks>
        ReadOnly Property Latency As Integer

        ''' <summary>
        ''' Bypass this processor (pass-through without processing)
        ''' </summary>
        ''' <value>True to bypass, False to process</value>
        Property Bypassed As Boolean

        ''' <summary>
        ''' Reset internal state (clear delays, filter states, etc.)
        ''' </summary>
        ''' <remarks>
        ''' Call when audio stream is discontinuous (seek, file change).
        ''' Should clear all internal buffers and filter states.
        ''' </remarks>
        Sub Reset()

        ''' <summary>
        ''' Processor name for UI display and logging
        ''' </summary>
        ReadOnly Property Name As String

    End Interface

End Namespace
