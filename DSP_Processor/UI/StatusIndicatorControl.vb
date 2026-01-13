Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.ComponentModel

Namespace UI

    ''' <summary>
    ''' Professional status indicator control with LED-style display
    ''' Replaces simple panelLED with animated, color-coded status display
    ''' </summary>
    Public Class StatusIndicatorControl
        Inherits UserControl

#Region "Enums"

        Public Enum Status
            Idle = 0        ' Gray - No activity
            Armed = 1       ' Yellow - Ready to record
            Recording = 2   ' Red - Recording in progress (pulsing)
            Playing = 3     ' Blue - Playback in progress
            [Error] = 4     ' Orange - Error state
        End Enum

#End Region

#Region "Fields"

        Private _currentStatus As Status = Status.Idle
        Private pulseTimer As Timer
        Private pulsePhase As Single = 0.0F
        Private labelFont As Font

#End Region

#Region "Properties"

        ''' <summary>Current status</summary>
        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
        <DefaultValue(Status.Idle)>
        Public Property CurrentStatus As Status
            Get
                Return _currentStatus
            End Get
            Set(value As Status)
                If _currentStatus <> value Then
                    _currentStatus = value
                    pulsePhase = 0.0F ' Reset pulse

                    ' Start/stop pulse timer
                    If value = Status.Recording Then
                        pulseTimer.Start()
                    Else
                        pulseTimer.Stop()
                    End If

                    Me.Invalidate()
                End If
            End Set
        End Property

        ''' <summary>Show status text label?</summary>
        <Browsable(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
        <DefaultValue(True)>
        Public Property ShowLabel As Boolean = True

#End Region

#Region "Constructor"

        Public Sub New()
            ' Set control properties
            Me.SetStyle(ControlStyles.UserPaint Or
                       ControlStyles.AllPaintingInWmPaint Or
                       ControlStyles.OptimizedDoubleBuffer Or
                       ControlStyles.ResizeRedraw, True)

            Me.Size = New Size(120, 40)
            Me.BackColor = Color.FromArgb(45, 45, 48)

            ' Create font
            labelFont = New Font("Segoe UI", 9, FontStyle.Bold)

            ' Create pulse timer (60fps for smooth animation)
            pulseTimer = New Timer() With {.Interval = 16}
            AddHandler pulseTimer.Tick, AddressOf OnPulseTimerTick
        End Sub

#End Region

#Region "Painting"

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias

            ' Get status color and label
            Dim ledColor = GetStatusColor(_currentStatus)
            Dim statusText = GetStatusText(_currentStatus)

            ' Apply pulse effect for recording
            If _currentStatus = Status.Recording Then
                Dim pulse = 0.7F + (CSng(Math.Sin(pulsePhase)) * 0.3F) ' 0.7 to 1.0
                ledColor = Color.FromArgb(CInt(ledColor.R * pulse), CInt(ledColor.G * pulse), CInt(ledColor.B * pulse))
            End If

            ' Calculate layout
            Dim ledSize = Math.Min(Me.Height - 10, 30) ' Max 30px
            Dim ledRect = New Rectangle(5, (Me.Height - ledSize) \ 2, ledSize, ledSize)
            Dim textX = ledRect.Right + 10
            Dim textY = (Me.Height - g.MeasureString(statusText, labelFont).Height) / 2

            ' Draw LED glow
            Using glowBrush = New SolidBrush(Color.FromArgb(80, ledColor))
                Dim glowRect = New Rectangle(ledRect.X - 5, ledRect.Y - 5, ledRect.Width + 10, ledRect.Height + 10)
                g.FillEllipse(glowBrush, glowRect)
            End Using

            ' Draw LED
            Using ledBrush = New SolidBrush(ledColor)
                g.FillEllipse(ledBrush, ledRect)
            End Using

            ' Draw LED highlight
            Using highlightBrush = New SolidBrush(Color.FromArgb(100, Color.White))
                Dim highlightRect = New Rectangle(ledRect.X + 5, ledRect.Y + 3, ledRect.Width \ 3, ledRect.Height \ 3)
                g.FillEllipse(highlightBrush, highlightRect)
            End Using

            ' Draw LED border
            Using borderPen = New Pen(Color.FromArgb(60, 60, 60), 2)
                g.DrawEllipse(borderPen, ledRect)
            End Using

            ' Draw label
            If ShowLabel Then
                Using textBrush = New SolidBrush(Color.White)
                    g.DrawString(statusText, labelFont, textBrush, textX, textY)
                End Using
            End If
        End Sub

#End Region

#Region "Animation"

        Private Sub OnPulseTimerTick(sender As Object, e As EventArgs)
            pulsePhase += 0.15F ' Pulse speed
            If pulsePhase > Math.PI * 2 Then
                pulsePhase = 0.0F
            End If

            Me.Invalidate()
        End Sub

#End Region

#Region "Helper Methods"

        Private Function GetStatusColor(status As Status) As Color
            Select Case status
                Case Status.Idle
                    Return Color.FromArgb(80, 80, 80) ' Gray

                Case Status.Armed
                    Return Color.FromArgb(255, 215, 0) ' Gold/Yellow

                Case Status.Recording
                    Return Color.FromArgb(255, 50, 50) ' Red

                Case Status.Playing
                    Return Color.FromArgb(70, 130, 255) ' Royal Blue

                Case Status.Error
                    Return Color.FromArgb(255, 140, 0) ' Orange

                Case Else
                    Return Color.Gray
            End Select
        End Function

        Private Function GetStatusText(status As Status) As String
            Select Case status
                Case Status.Idle
                    Return "Idle"

                Case Status.Armed
                    Return "Armed"

                Case Status.Recording
                    Return "Recording"

                Case Status.Playing
                    Return "Playing"

                Case Status.Error
                    Return "Error"

                Case Else
                    Return "Unknown"
            End Select
        End Function

#End Region

#Region "Disposal"

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                pulseTimer?.Stop()
                pulseTimer?.Dispose()
                labelFont?.Dispose()
            End If

            MyBase.Dispose(disposing)
        End Sub

#End Region

    End Class

End Namespace
