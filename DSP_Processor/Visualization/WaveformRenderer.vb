Imports NAudio.Wave
Imports System.Drawing
Imports System.IO

Namespace Visualization

    ''' <summary>
    ''' Renders audio waveforms to bitmaps with automatic normalization and caching.
    ''' Supports both mono and stereo waveforms.
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 0, Task 0.1.2
    ''' Purpose: Extract waveform rendering from MainForm
    ''' Features:
    ''' - Mono and stereo rendering
    ''' - Auto-zoom normalization
    ''' - Bitmap caching for performance
    ''' - Dual-mono fallback for mono files
    ''' </remarks>
    Public Class WaveformRenderer
        Implements IRenderer

        Private cachedBitmap As Bitmap
        Private cachedFilePath As String
        Private cachedWidth As Integer
        Private cachedHeight As Integer

#Region "Properties"

        ''' <summary>
        ''' Background color for the waveform display
        ''' </summary>
        Public Property BackgroundColor As Color = Color.Black Implements IRenderer.BackgroundColor

        ''' <summary>
        ''' Foreground color for mono or left channel waveform
        ''' </summary>
        Public Property ForegroundColor As Color = Color.Lime Implements IRenderer.ForegroundColor

        ''' <summary>
        ''' Color for right channel in stereo display
        ''' </summary>
        Public Property RightChannelColor As Color = Color.Cyan

        ''' <summary>
        ''' Renderer name
        ''' </summary>
        Public ReadOnly Property Name As String = "Waveform Renderer" Implements IRenderer.Name

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Renders an audio file to a bitmap
        ''' </summary>
        ''' <param name="data">File path (String) to render</param>
        ''' <param name="width">Bitmap width</param>
        ''' <param name="height">Bitmap height</param>
        ''' <returns>Rendered bitmap</returns>
        Public Function Render(data As Object, width As Integer, height As Integer) As Bitmap Implements IRenderer.Render
            If TypeOf data Is String Then
                Return RenderFromFile(CStr(data), width, height)
            Else
                Throw New ArgumentException("WaveformRenderer expects file path (String) as data parameter")
            End If
        End Function

        ''' <summary>
        ''' Clears the cached bitmap
        ''' </summary>
        Public Sub ClearCache() Implements IRenderer.ClearCache
            If cachedBitmap IsNot Nothing Then
                Utils.Logger.Instance.Debug($"Disposing cached waveform bitmap: {cachedFilePath}", "WaveformRenderer")
                cachedBitmap.Dispose()
                cachedBitmap = Nothing
            End If
            cachedFilePath = Nothing
        End Sub

#End Region

#Region "Private Rendering Methods"

        Private Function RenderFromFile(path As String, width As Integer, height As Integer) As Bitmap
            ' Check cache
            If cachedBitmap IsNot Nothing AndAlso
               cachedFilePath = path AndAlso
               cachedWidth = width AndAlso
               cachedHeight = height Then
                Utils.Logger.Instance.Debug($"Using cached waveform bitmap: {path}", "WaveformRenderer")
                Return cachedBitmap
            End If

            Utils.Logger.Instance.Debug($"Rendering waveform bitmap: {path} ({width}x{height})", "WaveformRenderer")

            ' Clear old cache
            ClearCache()

            ' Render new waveform
            Dim bmp As Bitmap
            
            ' Use Using block to ensure file is closed properly
            Using reader As New AudioFileReader(path)
                Dim channels = reader.WaveFormat.Channels
                Utils.Logger.Instance.Debug($"Audio format: {channels}ch, {reader.WaveFormat.SampleRate}Hz, {reader.WaveFormat.BitsPerSample}-bit", "WaveformRenderer")

                If channels >= 2 Then
                    bmp = RenderStereo(reader, width, height)
                Else
                    bmp = RenderMono(reader, width, height)
                End If
            End Using ' File is closed here
            
            Utils.Logger.Instance.Debug($"AudioFileReader disposed, file handle released: {path}", "WaveformRenderer")
            
            ' Force garbage collection to release file handle
            GC.Collect()
            GC.WaitForPendingFinalizers()

            ' Cache result
            cachedBitmap = bmp
            cachedFilePath = path
            cachedWidth = width
            cachedHeight = height

            Utils.Logger.Instance.Debug($"Waveform bitmap created and cached: {path}", "WaveformRenderer")

            Return bmp
        End Function

        Private Function RenderMono(reader As AudioFileReader, width As Integer, height As Integer) As Bitmap
            Utils.Logger.Instance.Debug($"Rendering mono waveform: {width}x{height}", "WaveformRenderer")
            Dim bmp As New Bitmap(width, height)
            Dim pixelsDrawn As Integer = 0

            Using g = Graphics.FromImage(bmp)
                g.Clear(BackgroundColor)

                ' PASS 1: Find peak amplitude for normalization
                Dim maxPeak As Single = 0.0F
                Dim buffer(4096 - 1) As Single
                Dim read As Integer

                Do
                    read = reader.Read(buffer, 0, buffer.Length)
                    For i = 0 To read - 1
                        Dim v = Math.Abs(buffer(i))
                        If v > maxPeak Then maxPeak = v
                    Next
                Loop While read > 0

                If maxPeak < 0.000001F Then maxPeak = 0.000001F

                ' Reset reader for drawing
                reader.Position = 0

                ' PASS 2: Draw waveform
                Dim samplesPerPixel = CInt(reader.Length / reader.WaveFormat.BlockAlign / width)
                If samplesPerPixel < 1 Then samplesPerPixel = 1

                Dim drawBuffer(samplesPerPixel - 1) As Single
                Dim x As Integer = 0
                Dim midY = height \ 2

                While reader.Read(drawBuffer, 0, drawBuffer.Length) > 0 AndAlso x < width
                    Dim localMax = drawBuffer.Max()
                    Dim localMin = drawBuffer.Min()

                    ' Normalize to max peak
                    Dim scaledMax = localMax / maxPeak
                    Dim scaledMin = localMin / maxPeak

                    ' Calculate Y positions
                    Dim yMax = midY - CInt(scaledMax * (height \ 2))
                    Dim yMin = midY - CInt(scaledMin * (height \ 2))

                    ' Draw line
                    g.DrawLine(New Pen(ForegroundColor), x, yMax, x, yMin)
                    x += 1
                End While
                
                pixelsDrawn = x
            End Using

            Utils.Logger.Instance.Debug($"Mono waveform rendering complete: {pixelsDrawn} pixels drawn", "WaveformRenderer")

            Return bmp
        End Function

        Private Function RenderStereo(reader As AudioFileReader, width As Integer, height As Integer) As Bitmap
            Utils.Logger.Instance.Debug($"Rendering stereo waveform: {width}x{height}", "WaveformRenderer")
            Dim bmp As New Bitmap(width, height)
            Dim pixelsDrawn As Integer = 0
            Dim peakL As Single = 0
            Dim peakR As Single = 0

            Using g = Graphics.FromImage(bmp)
                g.Clear(BackgroundColor)

                ' PASS 1: Find peak per channel for normalization
                Dim maxL As Single = 0.0F
                Dim maxR As Single = 0.0F
                Dim buffer(4096 - 1) As Single
                Dim read As Integer

                Do
                    read = reader.Read(buffer, 0, reader.WaveFormat.BlockAlign)
                    For i = 0 To read - 1 Step 2
                        If i + 1 < read Then
                            Dim l = Math.Abs(buffer(i))
                            Dim r = Math.Abs(buffer(i + 1))
                            If l > maxL Then maxL = l
                            If r > maxR Then maxR = r
                        End If
                    Next
                Loop While read > 0

                If maxL < 0.000001F Then maxL = 0.000001F
                If maxR < 0.000001F Then maxR = 0.000001F

                ' Reset reader
                reader.Position = 0

                ' PASS 2: Draw stereo lanes
                Dim samplesPerPixel = CInt(reader.Length / reader.WaveFormat.BlockAlign / width)
                If samplesPerPixel < 1 Then samplesPerPixel = 1

                Dim drawBuffer(samplesPerPixel * 2 - 1) As Single
                Dim x As Integer = 0

                ' Left channel is top quarter, right channel is bottom quarter
                Dim midL = height \ 4
                Dim midR = (height * 3) \ 4

                While reader.Read(drawBuffer, 0, drawBuffer.Length) > 0 AndAlso x < width
                    Dim localMaxL As Single = -1
                    Dim localMinL As Single = 1
                    Dim localMaxR As Single = -1
                    Dim localMinR As Single = 1

                    ' Find local min/max per channel
                    For i = 0 To drawBuffer.Length - 2 Step 2
                        Dim l = drawBuffer(i)
                        Dim r = drawBuffer(i + 1)

                        If l > localMaxL Then localMaxL = l
                        If l < localMinL Then localMinL = l
                        If r > localMaxR Then localMaxR = r
                        If r < localMinR Then localMinR = r
                    Next

                    ' Scale each channel independently
                    Dim yMaxL = midL - CInt((localMaxL / maxL) * (height \ 4))
                    Dim yMinL = midL - CInt((localMinL / maxL) * (height \ 4))
                    Dim yMaxR = midR - CInt((localMaxR / maxR) * (height \ 4))
                    Dim yMinR = midR - CInt((localMinR / maxR) * (height \ 4))

                    ' Draw lines
                    g.DrawLine(New Pen(ForegroundColor), x, yMaxL, x, yMinL)
                    g.DrawLine(New Pen(RightChannelColor), x, yMaxR, x, yMinR)

                    x += 1
                End While
                
                pixelsDrawn = x
                peakL = maxL
                peakR = maxR
            End Using

            Utils.Logger.Instance.Debug($"Stereo waveform rendering complete: {pixelsDrawn} pixels drawn (L:{peakL:F6}, R:{peakR:F6})", "WaveformRenderer")

            Return bmp
        End Function

#End Region

    End Class

End Namespace
