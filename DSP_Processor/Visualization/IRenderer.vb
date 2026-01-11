Imports System.Drawing

Namespace Visualization

    ''' <summary>
    ''' Base interface for all visualization renderers (waveform, spectrum, etc.)
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 0, Task 0.2.3
    ''' Purpose: Standardize visualization components for extensibility
    ''' </remarks>
    Public Interface IRenderer

        ''' <summary>
        ''' Renders visualization data to a bitmap
        ''' </summary>
        ''' <param name="data">Audio data or file path to render</param>
        ''' <param name="width">Target bitmap width in pixels</param>
        ''' <param name="height">Target bitmap height in pixels</param>
        ''' <returns>Rendered bitmap (caller is responsible for disposal)</returns>
        Function Render(data As Object, width As Integer, height As Integer) As Bitmap

        ''' <summary>
        ''' Gets or sets the background color
        ''' </summary>
        Property BackgroundColor As Color

        ''' <summary>
        ''' Gets or sets the foreground/waveform color
        ''' </summary>
        Property ForegroundColor As Color

        ''' <summary>
        ''' Clears any cached rendering data to free memory
        ''' </summary>
        Sub ClearCache()

        ''' <summary>
        ''' Gets the renderer name for display purposes
        ''' </summary>
        ReadOnly Property Name As String

    End Interface

End Namespace
