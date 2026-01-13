Imports System.Drawing
Imports DSP_Processor.Visualization
Imports DSP_Processor.Utils
Imports System.ComponentModel

Namespace UI

    ''' <summary>
    ''' Waveform display control - encapsulates WaveformRenderer and handles rendering lifecycle
    ''' Replaces picWaveform + manual renderer management in MainForm
    ''' </summary>
    Public Class WaveformDisplayControl
        Inherits UserControl

#Region "Fields"

        Private renderer As WaveformRenderer
        Private currentWaveform As Bitmap = Nothing
        Private currentFilePath As String = ""
        Private isLoading As Boolean = False

#End Region

#Region "Properties"

        ''' <summary>Background color for waveform display</summary>
        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
        Public Property WaveformBackgroundColor As Color
            Get
                Return If(renderer IsNot Nothing, renderer.BackgroundColor, Color.Black)
            End Get
            Set(value As Color)
                If renderer IsNot Nothing Then
                    renderer.BackgroundColor = value
                    RedrawIfLoaded()
                End If
            End Set
        End Property

        ''' <summary>Foreground color for left/mono channel</summary>
        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
        Public Property WaveformForegroundColor As Color
            Get
                Return If(renderer IsNot Nothing, renderer.ForegroundColor, Color.Lime)
            End Get
            Set(value As Color)
                If renderer IsNot Nothing Then
                    renderer.ForegroundColor = value
                    RedrawIfLoaded()
                End If
            End Set
        End Property

        ''' <summary>Color for right channel (stereo)</summary>
        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
        Public Property WaveformRightChannelColor As Color
            Get
                Return If(renderer IsNot Nothing, renderer.RightChannelColor, Color.Cyan)
            End Get
            Set(value As Color)
                If renderer IsNot Nothing Then
                    renderer.RightChannelColor = value
                    RedrawIfLoaded()
                End If
            End Set
        End Property

        ''' <summary>Currently loaded file path</summary>
        <Browsable(False)>
        Public ReadOnly Property CurrentFile As String
            Get
                Return currentFilePath
            End Get
        End Property

        ''' <summary>Is a waveform currently loaded?</summary>
        <Browsable(False)>
        Public ReadOnly Property IsLoaded As Boolean
            Get
                Return currentWaveform IsNot Nothing
            End Get
        End Property

#End Region

#Region "Constructor"

        Public Sub New()
            ' Initialize renderer
            renderer = New WaveformRenderer() With {
                .BackgroundColor = Color.Black,
                .ForegroundColor = Color.Lime,
                .RightChannelColor = Color.Cyan
            }

            ' Set control properties
            Me.SetStyle(ControlStyles.UserPaint Or
                       ControlStyles.AllPaintingInWmPaint Or
                       ControlStyles.OptimizedDoubleBuffer Or
                       ControlStyles.ResizeRedraw, True)

            Me.BackColor = Color.Black
            Me.DoubleBuffered = True

            Logger.Instance.Debug("WaveformDisplayControl initialized", "WaveformDisplayControl")
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>Load and display waveform from audio file</summary>
        Public Sub LoadFile(filepath As String)
            If String.IsNullOrEmpty(filepath) OrElse Not System.IO.File.Exists(filepath) Then
                Logger.Instance.Warning($"Invalid file path: {filepath}", "WaveformDisplayControl")
                Return
            End If

            Try
                isLoading = True
                Me.Invalidate() ' Show "Loading..." state

                Logger.Instance.Debug($"Loading waveform: {System.IO.Path.GetFileName(filepath)}", "WaveformDisplayControl")

                ' Dispose old waveform
                DisposeCurrentWaveform()

                ' Render new waveform
                Using timer = Logger.Instance.StartTimer("Waveform Rendering")
                    currentWaveform = renderer.Render(filepath, Me.Width, Me.Height)
                End Using

                currentFilePath = filepath
                isLoading = False

                ' Redraw with new waveform
                Me.Invalidate()

                Logger.Instance.Debug($"Waveform loaded: {System.IO.Path.GetFileName(filepath)}", "WaveformDisplayControl")

            Catch ex As Exception
                Logger.Instance.Error($"Failed to load waveform: {ex.Message}", ex, "WaveformDisplayControl")
                isLoading = False
                DisposeCurrentWaveform()
                Me.Invalidate() ' Show error state
            End Try
        End Sub

        ''' <summary>Clear current waveform display</summary>
        Public Sub Clear()
            DisposeCurrentWaveform()
            currentFilePath = ""
            Me.Invalidate()

            Logger.Instance.Debug("Waveform display cleared", "WaveformDisplayControl")
        End Sub

        ''' <summary>Clear renderer cache</summary>
        Public Sub ClearCache()
            If renderer IsNot Nothing Then
                renderer.ClearCache()
                Logger.Instance.Debug("Waveform cache cleared", "WaveformDisplayControl")
            End If
        End Sub

        ''' <summary>Redraw current waveform (call after size change)</summary>
        Public Sub Redraw()
            If Not String.IsNullOrEmpty(currentFilePath) Then
                LoadFile(currentFilePath)
            End If
        End Sub

#End Region

#Region "Private Methods"

        Private Sub DisposeCurrentWaveform()
            If currentWaveform IsNot Nothing Then
                currentWaveform.Dispose()
                currentWaveform = Nothing
            End If
        End Sub

        Private Sub RedrawIfLoaded()
            If Not String.IsNullOrEmpty(currentFilePath) AndAlso Not isLoading Then
                Redraw()
            End If
        End Sub

#End Region

#Region "Painting"

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics

            ' Fill background
            g.Clear(renderer.BackgroundColor)

            If isLoading Then
                ' Show "Loading..." message
                DrawCenteredText(g, "Loading waveform...", Brushes.White)

            ElseIf currentWaveform IsNot Nothing Then
                ' Draw waveform
                g.DrawImage(currentWaveform, 0, 0, Me.Width, Me.Height)

            Else
                ' Show "No waveform loaded" message
                DrawCenteredText(g, "No waveform loaded", Brushes.Gray)
            End If
        End Sub

        Private Sub DrawCenteredText(g As Graphics, text As String, brush As Brush)
            Using font = New Font("Segoe UI", 12)
                Dim textSize = g.MeasureString(text, font)
                Dim x = (Me.Width - textSize.Width) / 2
                Dim y = (Me.Height - textSize.Height) / 2
                g.DrawString(text, font, brush, x, y)
            End Using
        End Sub

        Protected Overrides Sub OnResize(e As EventArgs)
            MyBase.OnResize(e)

            ' Redraw at new size if waveform is loaded
            If Not isLoading AndAlso Not String.IsNullOrEmpty(currentFilePath) Then
                ' Delay redraw to avoid excessive redraws during resize
                System.Threading.Tasks.Task.Delay(100).ContinueWith(Sub()
                    If Me.InvokeRequired Then
                        Me.Invoke(Sub() Redraw())
                    Else
                        Redraw()
                    End If
                End Sub)
            End If
        End Sub

#End Region

#Region "Disposal"

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                DisposeCurrentWaveform()

                If renderer IsNot Nothing Then
                    renderer.ClearCache()
                    renderer = Nothing
                End If

                Logger.Instance.Debug("WaveformDisplayControl disposed", "WaveformDisplayControl")
            End If

            MyBase.Dispose(disposing)
        End Sub

#End Region

    End Class

End Namespace
