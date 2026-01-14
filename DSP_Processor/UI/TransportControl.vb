Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.ComponentModel

Namespace UI

    ''' <summary>
    ''' Professional transport control for DAW-style playback and recording.
    ''' Provides play, stop, pause, and record buttons with LED indicators.
    ''' </summary>
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

        Private rectPlay As Rectangle
        Private rectStop As Rectangle
        Private rectPause As Rectangle
        Private rectRecord As Rectangle
        Private rectPlayLed As Rectangle
        Private rectRecordLed As Rectangle
        Private rectPauseLed As Rectangle
        Private rectTrack As Rectangle
        Private rectTrackFill As Rectangle
        Private isDraggingTrack As Boolean = False
        Private hoveredButton As ButtonType? = Nothing

        Private ledPulsePhase As Single = 0

        Private timeFont As Font
        Private labelFont As Font

        Private _buttonSize As Integer = 50
        Private _buttonSpacing As Integer = 10
        Private _buttonMargin As Integer = 10
        Private _ledSize As Integer = 8
        Private _trackHeight As Integer = 20
        Private _trackMarginBottom As Integer = 30
        Private _timeDisplayY As Integer = 5
        Private _timeFontSize As Single = 16.0F
        Private _labelFontSize As Single = 8.0F
        Private _enableLedPulse As Boolean = True

#End Region

#Region "Designer Properties"

        <Category("Layout")>
        <DefaultValue(50)>
        Public Property ButtonSize As Integer
            Get
                Return _buttonSize
            End Get
            Set(value As Integer)
                If value <> _buttonSize AndAlso value >= 20 AndAlso value <= 100 Then
                    _buttonSize = value
                    CalculateLayout()
                    Me.Invalidate()
                End If
            End Set
        End Property

        <Category("Layout")>
        <DefaultValue(10)>
        Public Property ButtonSpacing As Integer
            Get
                Return _buttonSpacing
            End Get
            Set(value As Integer)
                If value <> _buttonSpacing AndAlso value >= 0 AndAlso value <= 50 Then
                    _buttonSpacing = value
                    CalculateLayout()
                    Me.Invalidate()
                End If
            End Set
        End Property

        <Category("Layout")>
        <DefaultValue(10)>
        Public Property ButtonMargin As Integer
            Get
                Return _buttonMargin
            End Get
            Set(value As Integer)
                If value <> _buttonMargin AndAlso value >= 0 AndAlso value <= 50 Then
                    _buttonMargin = value
                    CalculateLayout()
                    Me.Invalidate()
                End If
            End Set
        End Property

        <Category("Appearance")>
        <DefaultValue(True)>
        Public Property EnableLedPulse As Boolean
            Get
                Return _enableLedPulse
            End Get
            Set(value As Boolean)
                _enableLedPulse = value
                Me.Invalidate()
            End Set
        End Property

#End Region

#Region "Runtime Properties"

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

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property RecordingTime As TimeSpan
            Get
                Return recordTime
            End Get
            Set(value As TimeSpan)
                recordTime = value
                Me.Invalidate()
            End Set
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property TrackPosition As TimeSpan
            Get
                Return trackPos
            End Get
            Set(value As TimeSpan)
                trackPos = value
                UpdateTrackFillRect()
                Me.Invalidate()
            End Set
        End Property

        <Browsable(False)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        Public Property TrackDuration As TimeSpan
            Get
                Return trackDur
            End Get
            Set(value As TimeSpan)
                trackDur = value
                UpdateTrackFillRect()
                Me.Invalidate()
            End Set
        End Property

#End Region

#Region "Events"

        Public Event PlayClicked As EventHandler
        Public Event StopClicked As EventHandler
        Public Event PauseClicked As EventHandler
        Public Event RecordClicked As EventHandler
        Public Event PositionChanged As EventHandler(Of TimeSpan)

#End Region

#Region "Initialization"

        Public Sub New()
            InitializeComponent()

            ' Set up custom initialization
            Me.SetStyle(ControlStyles.UserPaint Or
                       ControlStyles.AllPaintingInWmPaint Or
                       ControlStyles.OptimizedDoubleBuffer Or
                       ControlStyles.ResizeRedraw, True)

            timeFont = New Font("Consolas", _timeFontSize, FontStyle.Bold)
            labelFont = New Font("Segoe UI", _labelFontSize)
        End Sub

#End Region

#Region "Layout"

        Protected Overrides Sub OnResize(e As EventArgs)
            MyBase.OnResize(e)
            CalculateLayout()
        End Sub

        Private Sub CalculateLayout()
            Dim buttonY = 25
            Dim buttonX = _buttonMargin

            rectStop = New Rectangle(buttonX, buttonY, _buttonSize, _buttonSize)
            buttonX += _buttonSize + _buttonSpacing

            rectPlay = New Rectangle(buttonX, buttonY, _buttonSize, _buttonSize)
            buttonX += _buttonSize + _buttonSpacing

            rectPause = New Rectangle(buttonX, buttonY, _buttonSize, _buttonSize)
            buttonX += _buttonSize + _buttonSpacing

            rectRecord = New Rectangle(buttonX, buttonY, _buttonSize, _buttonSize)

            rectPlayLed = New Rectangle(rectPlay.X + (_buttonSize - _ledSize) \ 2, buttonY - _ledSize - 5, _ledSize, _ledSize)
            rectPauseLed = New Rectangle(rectPause.X + (_buttonSize - _ledSize) \ 2, buttonY - _ledSize - 5, _ledSize, _ledSize)
            rectRecordLed = New Rectangle(rectRecord.X + (_buttonSize - _ledSize) \ 2, buttonY - _ledSize - 5, _ledSize, _ledSize)

            Dim trackY = Me.Height - _trackMarginBottom
            rectTrack = New Rectangle(_buttonMargin, trackY, Me.Width - _buttonMargin * 2, _trackHeight)

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
            g.Clear(Me.BackColor)

            DrawLed(g, rectPlayLed, currentState = TransportState.Playing, DarkTheme.SuccessGreen)
            DrawLed(g, rectPauseLed, currentState = TransportState.Paused, DarkTheme.WarningYellow)
            DrawLed(g, rectRecordLed, currentState = TransportState.Recording Or recordArmed, DarkTheme.ErrorRed)

            Dim centerX = Me.Width \ 2
            DrawTimeDisplay(g, centerX - 150, _timeDisplayY, "PLAY", trackPos)
            DrawTimeDisplay(g, centerX + 30, _timeDisplayY, "REC", recordTime)

            DrawButton(g, rectStop, ButtonType.Stop, currentState = TransportState.Stopped)
            DrawButton(g, rectPlay, ButtonType.Play, currentState = TransportState.Playing)
            DrawButton(g, rectPause, ButtonType.Pause, currentState = TransportState.Paused)
            DrawButton(g, rectRecord, ButtonType.Record, currentState = TransportState.Recording)

            DrawTrackSlider(g)
        End Sub

        Private Sub DrawLed(g As Graphics, rect As Rectangle, isOn As Boolean, color As Color)
            If isOn Then
                Dim pulse As Single = If(_enableLedPulse, 0.7F + (Math.Sin(ledPulsePhase) * 0.3F), 1.0F)
                Using glowBrush = New SolidBrush(Color.FromArgb(CInt(100 * pulse), color))
                    g.FillEllipse(glowBrush, New Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8))
                End Using
                Using brush = New SolidBrush(color)
                    g.FillEllipse(brush, rect)
                End Using
            Else
                Using brush = New SolidBrush(Color.FromArgb(40, color))
                    g.FillEllipse(brush, rect)
                End Using
            End If
            Using pen = New Pen(DarkTheme.BorderColor, 1)
                g.DrawEllipse(pen, rect)
            End Using
        End Sub

        Private Sub DrawButton(g As Graphics, rect As Rectangle, btnType As ButtonType, isActive As Boolean)
            Dim isHovered = (hoveredButton = btnType)
            Dim bgColor = If(isActive, DarkTheme.ControlBackgroundActive, If(isHovered, DarkTheme.ControlBackgroundHover, DarkTheme.ControlBackground))

            Using brush = New SolidBrush(bgColor)
                g.FillRectangle(brush, rect)
            End Using
            Using pen = New Pen(If(isActive, DarkTheme.AccentBlue, DarkTheme.BorderColor), 2)
                g.DrawRectangle(pen, rect)
            End Using

            Dim symbolColor = If(isActive, Color.White, DarkTheme.TextColor)
            Using symbolBrush = New SolidBrush(symbolColor)
                Dim cx = rect.X + rect.Width \ 2
                Dim cy = rect.Y + rect.Height \ 2
                Dim sz = CInt(_buttonSize * 0.4)

                Select Case btnType
                    Case ButtonType.Stop
                        g.FillRectangle(symbolBrush, New Rectangle(cx - sz \ 2, cy - sz \ 2, sz, sz))
                    Case ButtonType.Play
                        g.FillPolygon(symbolBrush, {New Point(cx - sz \ 3, cy - sz \ 2), New Point(cx - sz \ 3, cy + sz \ 2), New Point(cx + sz \ 2, cy)})
                    Case ButtonType.Pause
                        Dim bw = sz \ 3
                        g.FillRectangle(symbolBrush, New Rectangle(cx - sz \ 2, cy - sz \ 2, bw, sz))
                        g.FillRectangle(symbolBrush, New Rectangle(cx + sz \ 6, cy - sz \ 2, bw, sz))
                    Case ButtonType.Record
                        g.FillEllipse(symbolBrush, New Rectangle(cx - sz \ 2, cy - sz \ 2, sz, sz))
                End Select
            End Using

            Using labelBrush = New SolidBrush(DarkTheme.TextColorDim)
                g.DrawString(btnType.ToString().ToUpper(), labelFont, labelBrush, New Rectangle(rect.X, rect.Bottom + 2, rect.Width, 15),
                           New StringFormat() With {.Alignment = StringAlignment.Center})
            End Using
        End Sub

        Private Sub DrawTimeDisplay(g As Graphics, x As Integer, y As Integer, label As String, time As TimeSpan)
            Using labelBrush = New SolidBrush(DarkTheme.TextColorDim)
                g.DrawString(label, labelFont, labelBrush, x, y)
            End Using
            Using timeBrush = New SolidBrush(DarkTheme.TextColor)
                g.DrawString($"{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds \ 100:0}", timeFont, timeBrush, x, y + 15)
            End Using
        End Sub

        Private Sub DrawTrackSlider(g As Graphics)
            Using bgBrush = New SolidBrush(DarkTheme.ControlBackground)
                g.FillRectangle(bgBrush, rectTrack)
            End Using
            If rectTrackFill.Width > 0 Then
                Using fillBrush = New SolidBrush(DarkTheme.AccentBlue)
                    g.FillRectangle(fillBrush, rectTrackFill)
                End Using
                Using markerPen = New Pen(Color.White, 2)
                    g.DrawLine(markerPen, rectTrackFill.Right, rectTrack.Top, rectTrackFill.Right, rectTrack.Bottom)
                End Using
            End If
            Using pen = New Pen(DarkTheme.BorderColor, 1)
                g.DrawRectangle(pen, rectTrack)
            End Using
        End Sub

#End Region

#Region "Mouse"

        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            MyBase.OnMouseMove(e)
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
            Dim progress = Math.Max(0, Math.Min(1, relativeX / CSng(rectTrack.Width)))
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

        Private Sub ledTimer_Tick(sender As Object, e As EventArgs) Handles ledTimer.Tick
            If _enableLedPulse Then
                ledPulsePhase += 0.2F
                If ledPulsePhase > Math.PI * 2 Then ledPulsePhase = 0
                Me.Invalidate()
            End If
        End Sub

#End Region

    End Class

End Namespace
