Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.ComponentModel

Namespace UI

    ''' <summary>
    ''' Professional transport control for DAW-style playback and recording.
    ''' Provides play, stop, pause, and record buttons with LED indicators.
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 1.5
    ''' Matches the professional quality of VolumeMeterControl
    ''' Features:
    ''' - Large, touch-friendly buttons
    ''' - LED status indicators
    ''' - Color-coded states (green=play, red=record, yellow=pause)
    ''' - Dark theme optimized
    ''' - Smooth animations
    ''' </remarks>
    Public Class TransportControl
        Inherits UserControl

#Region "Enums"

        Public Enum TransportState
            Stopped
            Playing
            Recording
            Paused
        End Enum

        Private Enum ButtonType
            Play
            [Stop]
            Pause
            Record
        End Enum

#End Region

#Region "Private Fields"

        Private currentState As TransportState = TransportState.Stopped
        Private recordArmed As Boolean = False
        Private recordTime As TimeSpan = TimeSpan.Zero
        Private trackPos As TimeSpan = TimeSpan.Zero
        Private trackDur As TimeSpan = TimeSpan.MaxValue

        ' Button rectangles (calculated in OnResize)
        Private rectPlay As Rectangle
        Private rectStop As Rectangle
        Private rectPause As Rectangle
        Private rectRecord As Rectangle

        ' LED rectangles
        Private rectPlayLed As Rectangle
        Private rectRecordLed As Rectangle
        Private rectPauseLed As Rectangle

        ' Track position slider
        Private rectTrack As Rectangle
        Private rectTrackFill As Rectangle
        Private isDraggingTrack As Boolean = False

        ' Hover state
        Private hoveredButton As ButtonType? = Nothing

        ' Animation timer for LED pulse
        Private ledTimer As Timer
        Private ledPulsePhase As Single = 0

        ' Fonts
        Private timeFont As Font
        Private labelFont As Font

#End Region

#Region "Public Events"

        ''' <summary>Raised when play button is clicked</summary>
        Public Event PlayClicked As EventHandler

        ''' <summary>Raised when stop button is clicked</summary>
        Public Event StopClicked As EventHandler

        ''' <summary>Raised when pause button is clicked</summary>
        Public Event PauseClicked As EventHandler

        ''' <summary>Raised when record button is clicked</summary>
        Public Event RecordClicked As EventHandler

        ''' <summary>Raised when track position is changed by user</summary>
        Public Event PositionChanged As EventHandler(Of TimeSpan)

#End Region

#Region "Public Properties"

        ''' <summary>
        ''' Gets or sets the current transport state
        ''' </summary>
        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property State As TransportState
            Get
                Return currentState
            End Get
            Set(value As TransportState)
                If currentState <> value Then
                    currentState = value
                    Me.Invalidate()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets whether record is armed (ready to record)
        ''' </summary>
        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property IsRecordArmed As Boolean
            Get
                Return recordArmed
            End Get
            Set(value As Boolean)
                If recordArmed <> value Then
                    recordArmed = value
                    Me.Invalidate()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets the recording time display
        ''' </summary>
        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property RecordingTime As TimeSpan
            Get
                Return recordTime
            End Get
            Set(value As TimeSpan)
                If recordTime <> value Then
                    recordTime = value
                    Me.Invalidate()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets the track playback position
        ''' </summary>
        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property TrackPosition As TimeSpan
            Get
                Return trackPos
            End Get
            Set(value As TimeSpan)
                If trackPos <> value Then
                    trackPos = value
                    UpdateTrackFillRect()
                    Me.Invalidate()
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets the track total duration
        ''' </summary>
        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property TrackDuration As TimeSpan
            Get
                Return trackDur
            End Get
            Set(value As TimeSpan)
                If trackDur <> value Then
                    trackDur = value
                    UpdateTrackFillRect()
                    Me.Invalidate()
                End If
            End Set
        End Property

#End Region

#Region "Constructor"

        Public Sub New()
            ' Enable double buffering
            Me.SetStyle(ControlStyles.UserPaint Or
                       ControlStyles.AllPaintingInWmPaint Or
                       ControlStyles.OptimizedDoubleBuffer Or
                       ControlStyles.ResizeRedraw, True)

            Me.Size = New Size(800, 120)
            Me.BackColor = DarkTheme.BackgroundDark

            ' Initialize fonts
            timeFont = New Font("Consolas", 16, FontStyle.Bold)
            labelFont = New Font("Segoe UI", 8)

            ' LED pulse animation timer
            ledTimer = New Timer() With {.Interval = 50}
            AddHandler ledTimer.Tick, AddressOf OnLedTimerTick
            ledTimer.Start()
        End Sub

#End Region

#Region "Layout Calculation"

        Protected Overrides Sub OnResize(e As EventArgs)
            MyBase.OnResize(e)
            CalculateLayout()
        End Sub

        Private Sub CalculateLayout()
            Dim margin = 10
            Dim buttonSize = 50
            Dim buttonSpacing = 10
            Dim ledSize = 8

            ' Buttons row (centered vertically)
            Dim buttonY = 25 ' Buttons position
            Dim buttonX = margin

            rectStop = New Rectangle(buttonX, buttonY, buttonSize, buttonSize)
            buttonX += buttonSize + buttonSpacing

            rectPlay = New Rectangle(buttonX, buttonY, buttonSize, buttonSize)
            buttonX += buttonSize + buttonSpacing

            rectPause = New Rectangle(buttonX, buttonY, buttonSize, buttonSize)
            buttonX += buttonSize + buttonSpacing

            rectRecord = New Rectangle(buttonX, buttonY, buttonSize, buttonSize)
            buttonX += buttonSize + buttonSpacing + 30 ' Extra space before time displays

            ' LEDs (above buttons)
            rectPlayLed = New Rectangle(rectPlay.X + (buttonSize - ledSize) \ 2,
                                        buttonY - ledSize - 5,
                                        ledSize, ledSize)
            rectPauseLed = New Rectangle(rectPause.X + (buttonSize - ledSize) \ 2,
                                         buttonY - ledSize - 5,
                                         ledSize, ledSize)
            rectRecordLed = New Rectangle(rectRecord.X + (buttonSize - ledSize) \ 2,
                                          buttonY - ledSize - 5,
                                          ledSize, ledSize)

            ' Track slider (at the very bottom of the control)
            Dim trackY = Me.Height - 30 ' Position at bottom with 30px height
            Dim trackHeight = 20
            rectTrack = New Rectangle(margin, trackY, Me.Width - margin * 2, trackHeight)

            UpdateTrackFillRect()
        End Sub

        Private Sub UpdateTrackFillRect()
            If trackDur.TotalMilliseconds > 0 Then
                Dim progress = trackPos.TotalMilliseconds / trackDur.TotalMilliseconds
                Dim fillWidth = CInt(rectTrack.Width * progress)
                rectTrackFill = New Rectangle(rectTrack.X, rectTrack.Y, fillWidth, rectTrack.Height)
            Else
                rectTrackFill = New Rectangle(rectTrack.X, rectTrack.Y, 0, rectTrack.Height)
            End If
        End Sub

#End Region

#Region "Painting"

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit

            ' Draw background
            g.Clear(DarkTheme.BackgroundDark)

            ' Draw LEDs
            DrawLed(g, rectPlayLed, currentState = TransportState.Playing, DarkTheme.SuccessGreen)
            DrawLed(g, rectPauseLed, currentState = TransportState.Paused, DarkTheme.WarningYellow)
            DrawLed(g, rectRecordLed, currentState = TransportState.Recording Or recordArmed, DarkTheme.ErrorRed)

            ' Draw time displays (top center of control)
            Dim timeY = 5 ' Near the top
            Dim centerX = Me.Width \ 2
            DrawTimeDisplay(g, centerX - 150, timeY, "PLAY", trackPos)
            DrawTimeDisplay(g, centerX + 30, timeY, "REC", recordTime)

            ' Draw buttons
            DrawButton(g, rectStop, ButtonType.Stop, "", currentState = TransportState.Stopped)
            DrawButton(g, rectPlay, ButtonType.Play, "", currentState = TransportState.Playing)
            DrawButton(g, rectPause, ButtonType.Pause, "", currentState = TransportState.Paused)
            DrawButton(g, rectRecord, ButtonType.Record, "", currentState = TransportState.Recording)

            ' Draw track slider
            DrawTrackSlider(g)
        End Sub

        Private Sub DrawLed(g As Graphics, rect As Rectangle, isOn As Boolean, color As Color)
            ' LED glow effect
            If isOn Then
                ' Pulsing glow
                Dim pulse = 0.7F + (Math.Sin(ledPulsePhase) * 0.3F)
                Dim glowColor = Color.FromArgb(CInt(100 * pulse), color)

                Using glowBrush = New SolidBrush(glowColor)
                    Dim glowRect = New Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8)
                    g.FillEllipse(glowBrush, glowRect)
                End Using

                ' Bright center
                Using brush = New SolidBrush(color)
                    g.FillEllipse(brush, rect)
                End Using

                ' Highlight
                Using highlight = New SolidBrush(Color.FromArgb(150, Color.White))
                    Dim highlightRect = New Rectangle(rect.X + 2, rect.Y + 1, 3, 3)
                    g.FillEllipse(highlight, highlightRect)
                End Using
            Else
                ' Off LED - dark
                Using brush = New SolidBrush(Color.FromArgb(40, color))
                    g.FillEllipse(brush, rect)
                End Using
            End If

            ' LED border
            Using pen = New Pen(DarkTheme.BorderColor, 1)
                g.DrawEllipse(pen, rect)
            End Using
        End Sub

        Private Sub DrawButton(g As Graphics, rect As Rectangle, btnType As ButtonType, symbol As String, isActive As Boolean)
            Dim isHovered = (hoveredButton = btnType)

            ' Button background
            Dim bgColor As Color
            If isActive Then
                bgColor = DarkTheme.ControlBackgroundActive
            ElseIf isHovered Then
                bgColor = DarkTheme.ControlBackgroundHover
            Else
                bgColor = DarkTheme.ControlBackground
            End If

            Using brush = New SolidBrush(bgColor)
                g.FillRectangle(brush, rect)
            End Using

            ' Button border
            Using pen = New Pen(If(isActive, DarkTheme.AccentBlue, DarkTheme.BorderColor), 2)
                g.DrawRectangle(pen, rect)
            End Using

            ' Draw symbol as filled shapes instead of text
            Dim symbolColor = If(isActive, Color.White, DarkTheme.TextColor)
            Using symbolBrush = New SolidBrush(symbolColor)
                Dim centerX = rect.X + rect.Width \ 2
                Dim centerY = rect.Y + rect.Height \ 2
                Dim size = 20 ' Symbol size

                Select Case btnType
                    Case ButtonType.Stop
                        ' Draw square for Stop
                        Dim stopRect = New Rectangle(centerX - size \ 2, centerY - size \ 2, size, size)
                        g.FillRectangle(symbolBrush, stopRect)

                    Case ButtonType.Play
                        ' Draw triangle for Play
                        Dim playPoints = {
                            New Point(centerX - size \ 3, centerY - size \ 2),
                            New Point(centerX - size \ 3, centerY + size \ 2),
                            New Point(centerX + size \ 2, centerY)
                        }
                        g.FillPolygon(symbolBrush, playPoints)

                    Case ButtonType.Pause
                        ' Draw two vertical bars for Pause
                        Dim barWidth = size \ 3
                        Dim bar1 = New Rectangle(centerX - size \ 2, centerY - size \ 2, barWidth, size)
                        Dim bar2 = New Rectangle(centerX + size \ 6, centerY - size \ 2, barWidth, size)
                        g.FillRectangle(symbolBrush, bar1)
                        g.FillRectangle(symbolBrush, bar2)

                    Case ButtonType.Record
                        ' Draw circle for Record
                        Dim recordRect = New Rectangle(centerX - size \ 2, centerY - size \ 2, size, size)
                        g.FillEllipse(symbolBrush, recordRect)
                End Select
            End Using

            ' Label below button
            Dim labelText As String = btnType.ToString().ToUpper()
            Using labelBrush = New SolidBrush(DarkTheme.TextColorDim)
                Dim labelRect = New Rectangle(rect.X, rect.Bottom + 2, rect.Width, 15)
                Dim sf = New StringFormat() With {
                    .Alignment = StringAlignment.Center,
                    .LineAlignment = StringAlignment.Near
                }
                g.DrawString(labelText, labelFont, labelBrush, labelRect, sf)
            End Using
        End Sub

        Private Sub DrawTimeDisplay(g As Graphics, x As Integer, y As Integer, label As String, time As TimeSpan)
            ' Label
            Using labelBrush = New SolidBrush(DarkTheme.TextColorDim)
                g.DrawString(label, labelFont, labelBrush, x, y)
            End Using

            ' Time
            Dim timeStr = $"{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds \ 100:0}"
            Using timeBrush = New SolidBrush(DarkTheme.TextColor)
                g.DrawString(timeStr, timeFont, timeBrush, x, y + 15)
            End Using
        End Sub

        Private Sub DrawTrackSlider(g As Graphics)
            ' Background track
            Using bgBrush = New SolidBrush(DarkTheme.ControlBackground)
                g.FillRectangle(bgBrush, rectTrack)
            End Using

            ' Progress fill
            If rectTrackFill.Width > 0 Then
                Using fillBrush = New SolidBrush(DarkTheme.AccentBlue)
                    g.FillRectangle(fillBrush, rectTrackFill)
                End Using
            End If

            ' Border
            Using pen = New Pen(DarkTheme.BorderColor, 1)
                g.DrawRectangle(pen, rectTrack)
            End Using

            ' Position marker
            If rectTrackFill.Width > 0 Then
                Dim markerX = rectTrackFill.Right
                Using markerPen = New Pen(Color.White, 2)
                    g.DrawLine(markerPen, markerX, rectTrack.Top, markerX, rectTrack.Bottom)
                End Using
            End If

            ' Time labels
            Using timeBrush = New SolidBrush(DarkTheme.TextColorDim)
                Using smallFont = New Font("Segoe UI", 8)
                    ' Current time
                    Dim currentTime = $"{trackPos.Minutes:00}:{trackPos.Seconds:00}"
                    g.DrawString(currentTime, smallFont, timeBrush, rectTrack.X, rectTrack.Bottom + 2)

                    ' Total time
                    If trackDur <> TimeSpan.MaxValue Then
                        Dim totalTime = $"{trackDur.Minutes:00}:{trackDur.Seconds:00}"
                        Dim totalSize = g.MeasureString(totalTime, smallFont)
                        g.DrawString(totalTime, smallFont, timeBrush,
                                   rectTrack.Right - totalSize.Width, rectTrack.Bottom + 2)
                    End If
                End Using
            End Using
        End Sub

#End Region

#Region "Mouse Handling"

        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            MyBase.OnMouseMove(e)

            ' Update hover state
            Dim newHover As ButtonType? = Nothing

            If rectPlay.Contains(e.Location) Then
                newHover = ButtonType.Play
            ElseIf rectStop.Contains(e.Location) Then
                newHover = ButtonType.Stop
            ElseIf rectPause.Contains(e.Location) Then
                newHover = ButtonType.Pause
            ElseIf rectRecord.Contains(e.Location) Then
                newHover = ButtonType.Record
            End If

            If newHover <> hoveredButton Then
                hoveredButton = newHover
                Me.Cursor = If(hoveredButton.HasValue, Cursors.Hand, Cursors.Default)
                Me.Invalidate()
            End If

            ' Track dragging
            If isDraggingTrack AndAlso e.Button = MouseButtons.Left Then
                UpdateTrackPositionFromMouse(e.X)
            End If
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)

            If e.Button = MouseButtons.Left Then
                If rectPlay.Contains(e.Location) Then
                    RaiseEvent PlayClicked(Me, EventArgs.Empty)
                ElseIf rectStop.Contains(e.Location) Then
                    RaiseEvent StopClicked(Me, EventArgs.Empty)
                ElseIf rectPause.Contains(e.Location) Then
                    RaiseEvent PauseClicked(Me, EventArgs.Empty)
                ElseIf rectRecord.Contains(e.Location) Then
                    RaiseEvent RecordClicked(Me, EventArgs.Empty)
                ElseIf rectTrack.Contains(e.Location) Then
                    isDraggingTrack = True
                    UpdateTrackPositionFromMouse(e.X)
                End If
            End If
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)
            isDraggingTrack = False
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)
            hoveredButton = Nothing
            Me.Cursor = Cursors.Default
            Me.Invalidate()
        End Sub

        Private Sub UpdateTrackPositionFromMouse(mouseX As Integer)
            Dim relativeX = mouseX - rectTrack.X
            Dim progress = relativeX / CSng(rectTrack.Width)
            progress = Math.Max(0, Math.Min(1, progress))

            Dim newPosition = TimeSpan.FromTicks(CLng(trackDur.Ticks * progress))
            If newPosition <> trackPos Then
                trackPos = newPosition
                UpdateTrackFillRect()
                Me.Invalidate()
                RaiseEvent PositionChanged(Me, newPosition)
            End If
        End Sub

#End Region

#Region "Animation"

        Private Sub OnLedTimerTick(sender As Object, e As EventArgs)
            ledPulsePhase += 0.2F
            If ledPulsePhase > Math.PI * 2 Then
                ledPulsePhase = 0
            End If
            Me.Invalidate()
        End Sub

#End Region

#Region "Cleanup"

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                ledTimer?.Dispose()
                timeFont?.Dispose()
                labelFont?.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub

#End Region

    End Class

End Namespace
