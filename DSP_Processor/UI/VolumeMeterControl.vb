Imports System.Drawing
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D

Namespace UI

    ''' <summary>
    ''' Professional volume meter control with peak, RMS, and clip indication
    ''' Displays audio levels in dB scale from -60dB to 0dB
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 0, Volume Meter Feature
    ''' Features:
    ''' - Color-coded levels (Green/Yellow/Red)
    ''' - Peak hold with decay
    ''' - Clip indicator LED
    ''' - dB scale markings
    ''' </remarks>
    Public Class VolumeMeterControl
        Inherits UserControl

        Private peakLevelDB As Single = -60
        Private rmsLevelDB As Single = -60
        Private peakHoldDB As Single = -60
        Private peakHoldTime As DateTime = DateTime.MinValue
        Private isClipping As Boolean = False
        Private clipTime As DateTime = DateTime.MinValue

        Private Const PeakDecayRate As Single = 20.0F ' dB per second
        Private Const ClipHoldTime As Single = 1.0F ' seconds

#Region "Properties"

        ''' <summary>
        ''' Current peak level in dB
        ''' </summary>
        Public ReadOnly Property CurrentPeakDB As Single
            Get
                Return peakLevelDB
            End Get
        End Property

        ''' <summary>
        ''' Current RMS level in dB
        ''' </summary>
        Public ReadOnly Property CurrentRMSDB As Single
            Get
                Return rmsLevelDB
            End Get
        End Property

        ''' <summary>
        ''' Whether meter is currently showing clipping
        ''' </summary>
        Public ReadOnly Property IsShowingClip As Boolean
            Get
                Return isClipping
            End Get
        End Property

#End Region

#Region "Constructor"

        Public Sub New()
            ' Enable double buffering for smooth rendering
            Me.SetStyle(ControlStyles.UserPaint Or
                       ControlStyles.AllPaintingInWmPaint Or
                       ControlStyles.OptimizedDoubleBuffer, True)

            ' Default size
            Me.Size = New Size(40, 250)
            Me.BackColor = Color.Black
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Updates the meter with new level data
        ''' </summary>
        ''' <param name="peakDB">Peak level in dB</param>
        ''' <param name="rmsDB">RMS level in dB</param>
        ''' <param name="clipping">True if signal is clipping</param>
        Public Sub SetLevel(peakDB As Single, rmsDB As Single, clipping As Boolean)
            ' Clamp values
            peakDB = Math.Max(-60, Math.Min(0, peakDB))
            rmsDB = Math.Max(-60, Math.Min(0, rmsDB))

            Me.peakLevelDB = peakDB
            Me.rmsLevelDB = rmsDB

            ' Peak hold logic
            If peakDB > peakHoldDB Then
                peakHoldDB = peakDB
                peakHoldTime = DateTime.Now
            Else
                ' Decay peak hold
                If peakHoldTime <> DateTime.MinValue Then
                    Dim elapsed = DateTime.Now.Subtract(peakHoldTime).TotalSeconds
                    Dim decay = CSng(elapsed * PeakDecayRate)
                    peakHoldDB = Math.Max(peakDB, peakLevelDB - decay)
                End If
            End If

            ' Clip indicator logic
            If clipping Then
                isClipping = True
                clipTime = DateTime.Now
            Else
                ' Hold clip for ClipHoldTime seconds
                If clipTime <> DateTime.MinValue Then
                    If DateTime.Now.Subtract(clipTime).TotalSeconds > ClipHoldTime Then
                        isClipping = False
                    End If
                End If
            End If

            ' Redraw
            Me.Invalidate()
        End Sub

        ''' <summary>
        ''' Resets meter to zero/idle state
        ''' </summary>
        Public Sub Reset()
            peakLevelDB = -60
            rmsLevelDB = -60
            peakHoldDB = -60
            peakHoldTime = DateTime.MinValue
            isClipping = False
            clipTime = DateTime.MinValue
            Me.Invalidate()
        End Sub

#End Region

#Region "Rendering"

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)

            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias

            ' Calculate meter area (leave space for clip indicator and scale)
            Dim clipHeight As Integer = 20
            Dim meterLeft As Integer = 25
            Dim meterWidth As Integer = Math.Max(15, Me.Width - meterLeft - 5)
            Dim meterTop As Integer = clipHeight + 5
            Dim meterHeight As Integer = Me.Height - meterTop - 5

            Dim meterRect As New Rectangle(meterLeft, meterTop, meterWidth, meterHeight)

            ' Draw background
            g.FillRectangle(Brushes.Black, Me.ClientRectangle)

            ' Draw meter background (dark gray)
            g.FillRectangle(Brushes.DarkGray, meterRect)
            g.DrawRectangle(Pens.Gray, meterRect)

            ' Draw RMS bar with gradient
            If rmsLevelDB > -60 Then
                DrawLevelBar(g, meterRect, rmsLevelDB, fillSolid:=True)
            End If

            ' Draw peak hold line
            If peakHoldDB > -60 Then
                Dim peakY = DBToPixelY(peakHoldDB, meterRect)
                Using pen As New Pen(Color.White, 2)
                    g.DrawLine(pen, meterRect.Left, peakY, meterRect.Right, peakY)
                End Using
            End If

            ' Draw scale markings
            DrawScale(g, meterRect)

            ' Draw clip indicator
            DrawClipIndicator(g, meterLeft, 2, meterWidth, clipHeight - 4)
        End Sub

        Private Sub DrawLevelBar(g As Graphics, meterRect As Rectangle, levelDB As Single, fillSolid As Boolean)
            Dim levelHeight = DBToPixelHeight(levelDB, meterRect.Height)
            If levelHeight <= 0 Then Return

            Dim levelY = meterRect.Bottom - levelHeight
            Dim levelRect As New Rectangle(meterRect.Left, levelY, meterRect.Width, levelHeight)

            If fillSolid Then
                ' Draw solid bar with color based on level
                Dim barColor = GetColorForLevel(levelDB)
                Using brush As New SolidBrush(barColor)
                    g.FillRectangle(brush, levelRect)
                End Using
            Else
                ' Draw gradient (for peak bar if needed)
                Using brush As New LinearGradientBrush(levelRect, Color.LimeGreen, Color.Red, LinearGradientMode.Vertical)
                    g.FillRectangle(brush, levelRect)
                End Using
            End If
        End Sub

        Private Sub DrawScale(g As Graphics, meterRect As Rectangle)
            ' Draw dB scale: 0, -6, -12, -18, -30, -60
            Dim marks() As Integer = {0, -6, -12, -18, -30, -60}
            Dim font As New Font("Arial", 7, FontStyle.Regular)

            For Each db In marks
                Dim y = DBToPixelY(db, meterRect)

                ' Tick mark
                g.DrawLine(Pens.Gray, meterRect.Left - 4, y, meterRect.Left, y)

                ' Label
                Dim label = db.ToString()
                Dim labelSize = g.MeasureString(label, font)
                g.DrawString(label, font, Brushes.LightGray, meterRect.Left - labelSize.Width - 5, y - labelSize.Height / 2)
            Next

            font.Dispose()
        End Sub

        Private Sub DrawClipIndicator(g As Graphics, x As Integer, y As Integer, width As Integer, height As Integer)
            Dim clipRect As New Rectangle(x, y, width, height)

            If isClipping Then
                ' Red when clipping
                g.FillRectangle(Brushes.Red, clipRect)
                g.DrawRectangle(Pens.DarkRed, clipRect)

                ' "CLIP" text
                Dim font As New Font("Arial", 8, FontStyle.Bold)
                Dim textSize = g.MeasureString("CLIP", font)
                g.DrawString("CLIP", font, Brushes.White,
                            x + (width - textSize.Width) / 2,
                            y + (height - textSize.Height) / 2)
                font.Dispose()
            Else
                ' Dark green when not clipping
                g.FillRectangle(Brushes.DarkGreen, clipRect)
                g.DrawRectangle(Pens.Gray, clipRect)
            End If
        End Sub

        Private Function DBToPixelHeight(db As Single, maxHeight As Integer) As Integer
            ' Map -60dB to 0, 0dB to maxHeight
            Dim normalized As Single = (db + 60.0F) / 60.0F ' 0.0 to 1.0
            Return CInt(normalized * maxHeight)
        End Function

        Private Function DBToPixelY(db As Single, meterRect As Rectangle) As Integer
            Dim height = DBToPixelHeight(db, meterRect.Height)
            Return meterRect.Bottom - height
        End Function

        Private Function GetColorForLevel(db As Single) As Color
            ' Dark theme color coding:
            ' Green: -60 to -18 dB
            ' Yellow: -18 to -6 dB
            ' Red: -6 to 0 dB

            If db > -6 Then
                ' Red zone - use dark theme red
                Return DarkTheme.MeterRed
            ElseIf db > -18 Then
                ' Yellow zone - blend from yellow to orange
                Dim t As Single = (db + 18) / 12 ' 0 to 1
                Return BlendColors(DarkTheme.MeterYellow, Color.OrangeRed, t)
            Else
                ' Green zone - use dark theme green
                Return DarkTheme.MeterGreen
            End If
        End Function

        Private Function BlendColors(c1 As Color, c2 As Color, t As Single) As Color
            ' Linear blend between two colors
            t = Math.Max(0, Math.Min(1, t))
            Dim r = CInt(c1.R * (1 - t) + c2.R * t)
            Dim g = CInt(c1.G * (1 - t) + c2.G * t)
            Dim b = CInt(c1.B * (1 - t) + c2.B * t)
            Return Color.FromArgb(r, g, b)
        End Function

#End Region

    End Class

End Namespace
