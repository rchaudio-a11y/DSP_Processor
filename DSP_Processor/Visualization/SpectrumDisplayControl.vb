Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.ComponentModel

Namespace Visualization

    ''' <summary>
    ''' Control for displaying real-time FFT spectrum analyzer
    ''' </summary>
    Public Class SpectrumDisplayControl
        Inherits Control

        Private spectrum() As Single
        Private peakHold() As Single
        Private peakHoldTimer() As Integer
        Private smoothedSpectrum() As Single
        Private _minFrequency As Single = 20.0F
        Private _maxFrequency As Single = 20000.0F
        Private _minDB As Single = -60.0F
        Private _maxDB As Single = 0.0F
        Private _peakHoldEnabled As Boolean = False
        Private _peakHoldFrames As Integer = 30
        Private _smoothingFactor As Single = 0.7F
        Private _spectrumColor As Color = Color.Lime
        Private _peakHoldColor As Color = Color.Red
        Private _gridColor As Color = Color.FromArgb(50, 50, 50)
        Private _textColor As Color = Color.Gray
        Private _currentSampleRate As Integer = 44100 ' Store sample rate from UpdateSpectrum
        Private _currentFFTSize As Integer = 4096     ' Store FFT size from UpdateSpectrum
        
        ''' <summary>
        ''' Minimum frequency to display (Hz)
        ''' </summary>
        <Category("Appearance"), DefaultValue(20.0F)>
        Public Property MinFrequency As Single
            Get
                Return _minFrequency
            End Get
            Set(value As Single)
                _minFrequency = value
                Invalidate()
            End Set
        End Property
        
        ''' <summary>
        ''' Maximum frequency to display (Hz)
        ''' </summary>
        <Category("Appearance"), DefaultValue(20000.0F)>
        Public Property MaxFrequency As Single
            Get
                Return _maxFrequency
            End Get
            Set(value As Single)
                _maxFrequency = value
                Invalidate()
            End Set
        End Property
        
        ''' <summary>
        ''' Minimum dB level
        ''' </summary>
        <Category("Appearance"), DefaultValue(-60.0F)>
        Public Property MinDB As Single
            Get
                Return _minDB
            End Get
            Set(value As Single)
                _minDB = value
                Invalidate()
            End Set
        End Property
        
        ''' <summary>
        ''' Maximum dB level
        ''' </summary>
        <Category("Appearance"), DefaultValue(0.0F)>
        Public Property MaxDB As Single
            Get
                Return _maxDB
            End Get
            Set(value As Single)
                _maxDB = value
                Invalidate()
            End Set
        End Property
        
        ''' <summary>
        ''' Enable peak hold display
        ''' </summary>
        <Category("Behavior"), DefaultValue(False)>
        Public Property PeakHoldEnabled As Boolean
            Get
                Return _peakHoldEnabled
            End Get
            Set(value As Boolean)
                _peakHoldEnabled = value
                Invalidate()
            End Set
        End Property
        
        ''' <summary>
        ''' Peak hold time in frames
        ''' </summary>
        <Category("Behavior"), DefaultValue(30)>
        Public Property PeakHoldFrames As Integer
            Get
                Return _peakHoldFrames
            End Get
            Set(value As Integer)
                _peakHoldFrames = value
            End Set
        End Property
        
        ''' <summary>
        ''' Smoothing factor (0 = no smoothing, 1 = max smoothing)
        ''' </summary>
        <Category("Behavior"), DefaultValue(0.7F)>
        Public Property SmoothingFactor As Single
            Get
                Return _smoothingFactor
            End Get
            Set(value As Single)
                _smoothingFactor = Math.Max(0.0F, Math.Min(1.0F, value))
            End Set
        End Property
        
        ''' <summary>
        ''' Spectrum line color
        ''' </summary>
        <Category("Appearance"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
        Public Property SpectrumColor As Color
            Get
                Return _spectrumColor
            End Get
            Set(value As Color)
                _spectrumColor = value
                Invalidate()
            End Set
        End Property
        
        ''' <summary>
        ''' Peak hold color
        ''' </summary>
        <Category("Appearance"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
        Public Property PeakHoldColor As Color
            Get
                Return _peakHoldColor
            End Get
            Set(value As Color)
                _peakHoldColor = value
                Invalidate()
            End Set
        End Property
        
        ''' <summary>
        ''' Grid color
        ''' </summary>
        <Category("Appearance"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
        Public Property GridColor As Color
            Get
                Return _gridColor
            End Get
            Set(value As Color)
                _gridColor = value
                Invalidate()
            End Set
        End Property
        
        ''' <summary>
        ''' Text color
        ''' </summary>
        <Category("Appearance"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
        Public Property TextColor As Color
            Get
                Return _textColor
            End Get
            Set(value As Color)
                _textColor = value
                Invalidate()
            End Set
        End Property
        
        Public Sub New()
            Me.SetStyle(ControlStyles.UserPaint Or
                       ControlStyles.AllPaintingInWmPaint Or
                       ControlStyles.OptimizedDoubleBuffer, True)
            Me.BackColor = Color.Black
        End Sub
        
        ''' <summary>
        ''' Update spectrum data
        ''' </summary>
        Public Sub UpdateSpectrum(newSpectrum() As Single, sampleRate As Integer, fftSize As Integer)
            If newSpectrum Is Nothing OrElse newSpectrum.Length = 0 Then
                Return
            End If
            
            ' Store sample rate and FFT size for frequency mapping
            _currentSampleRate = sampleRate
            _currentFFTSize = fftSize
            
            ' DIAGNOSTIC: Log spectrum data every 30 calls (~500ms)
            Static callCount As Integer = 0
            callCount += 1
            If callCount Mod 30 = 0 Then
                ' Find peak value in spectrum
                Dim peakValue As Single = -96.0F
                Dim peakBin As Integer = 0
                For i = 0 To newSpectrum.Length - 1
                    If newSpectrum(i) > peakValue Then
                        peakValue = newSpectrum(i)
                        peakBin = i
                    End If
                Next
                
                Dim freq = (peakBin * (sampleRate / 2.0F) / newSpectrum.Length)
                Utils.Logger.Instance.Info($"UpdateSpectrum: {newSpectrum.Length} bins, Peak={peakValue:F1} dB at bin {peakBin} ({freq:F0} Hz), SampleRate={sampleRate}, MinFreq={_minFrequency}, MaxFreq={_maxFrequency}, MinDB={_minDB}, MaxDB={_maxDB}", "SpectrumDisplay")
            End If
            
            ' Initialize arrays if needed
            If spectrum Is Nothing OrElse spectrum.Length <> newSpectrum.Length Then
                ReDim spectrum(newSpectrum.Length - 1)
                ReDim smoothedSpectrum(newSpectrum.Length - 1)
                ReDim peakHold(newSpectrum.Length - 1)
                ReDim peakHoldTimer(newSpectrum.Length - 1)
                
                ' Initialize to the INCOMING spectrum values, not MinDB!
                For i = 0 To spectrum.Length - 1
                    spectrum(i) = newSpectrum(i)
                    smoothedSpectrum(i) = newSpectrum(i)
                    peakHold(i) = newSpectrum(i)
                    peakHoldTimer(i) = 0
                Next
                
                Utils.Logger.Instance.Info($"Spectrum arrays initialized: {spectrum.Length} bins", "SpectrumDisplay")
            End If
            
            ' Update spectrum with smoothing
            For i = 0 To newSpectrum.Length - 1
                ' Apply smoothing
                smoothedSpectrum(i) = smoothedSpectrum(i) * _smoothingFactor + newSpectrum(i) * (1.0F - _smoothingFactor)
                spectrum(i) = smoothedSpectrum(i)
                
                ' Update peak hold
                If _peakHoldEnabled Then
                    If spectrum(i) > peakHold(i) Then
                        peakHold(i) = spectrum(i)
                        peakHoldTimer(i) = _peakHoldFrames
                    Else
                        peakHoldTimer(i) -= 1
                        If peakHoldTimer(i) <= 0 Then
                            peakHold(i) = spectrum(i)
                        End If
                    End If
                End If
            Next
            
            ' Request redraw
            Me.Invalidate()
        End Sub
        
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            
            ' Draw background
            g.Clear(BackColor)
            
            If spectrum Is Nothing OrElse spectrum.Length = 0 Then
                DrawNoData(g)
                Return
            End If
            
            ' Draw grid
            DrawGrid(g)
            
            ' Draw spectrum
            DrawSpectrum(g)
            
            ' Draw peak hold
            If _peakHoldEnabled Then
                DrawPeakHold(g)
            End If
            
            ' Draw labels
            DrawLabels(g)
        End Sub
        
        Private Sub DrawNoData(g As Graphics)
            Using font As New Font("Segoe UI", 12)
                Dim text = "Waiting for audio data..."
                Dim size = g.MeasureString(text, font)
                g.DrawString(text, font, New SolidBrush(_textColor),
                            (Width - size.Width) / 2,
                            (Height - size.Height) / 2)
            End Using
        End Sub
        
        Private Sub DrawGrid(g As Graphics)
            Using pen As New Pen(_gridColor)
                ' Horizontal lines (dB)
                For db = _minDB To _maxDB Step 10
                    Dim y = MapDBToY(db)
                    g.DrawLine(pen, 40, y, Width - 40, y)
                Next
                
                ' Vertical lines (frequency)
                Dim frequencies = {50, 100, 200, 500, 1000, 2000, 5000, 10000}
                For Each freq In frequencies
                    If freq >= _minFrequency AndAlso freq <= _maxFrequency Then
                        Dim x = MapFrequencyToX(freq)
                        g.DrawLine(pen, x, 10, x, Height - 30)
                    End If
                Next
            End Using
        End Sub

        Private Sub DrawSpectrum(g As Graphics)
            If spectrum.Length < 2 Then Return

            ' Calculate Nyquist frequency from sample rate
            Dim nyquistFreq = _currentSampleRate / 2.0F

            ' DIAGNOSTIC: Log drawing info every 60 frames (~1 second at 60 FPS)
            Static drawCount As Integer = 0
            drawCount += 1
            
            ' Create path for spectrum
            Using path As New GraphicsPath()
                Dim points As New List(Of PointF)

                For i = 0 To spectrum.Length - 1
                    ' Calculate frequency for this bin using actual sample rate
                    Dim freq = (i * nyquistFreq / spectrum.Length)
                    
                    If freq >= _minFrequency AndAlso freq <= _maxFrequency Then
                        Dim x = MapFrequencyToX(freq)
                        Dim y = MapDBToY(spectrum(i))
                        points.Add(New PointF(x, y))
                    End If
                Next

                ' DIAGNOSTIC: Log point count
                If drawCount Mod 60 = 0 Then
                    Utils.Logger.Instance.Info($"DrawSpectrum: {points.Count} points, Nyquist={nyquistFreq:F0} Hz, Width={Width}, Height={Height}", "SpectrumDisplay")
                    
                    ' Log a few sample points
                    If points.Count > 0 Then
                        Utils.Logger.Instance.Info($"  Point 0: X={points(0).X:F1}, Y={points(0).Y:F1}", "SpectrumDisplay")
                        If points.Count > points.Count \ 2 Then
                            Dim mid = points.Count \ 2
                            Utils.Logger.Instance.Info($"  Point {mid}: X={points(mid).X:F1}, Y={points(mid).Y:F1}", "SpectrumDisplay")
                        End If
                        Utils.Logger.Instance.Info($"  Point {points.Count - 1}: X={points(points.Count - 1).X:F1}, Y={points(points.Count - 1).Y:F1}", "SpectrumDisplay")
                    End If
                End If

                If points.Count >= 2 Then
                    ' Draw filled area under curve
                    Dim fillPoints = points.ToArray()
                    Dim lastPoint = fillPoints(fillPoints.Length - 1)
                    Array.Resize(fillPoints, fillPoints.Length + 2)
                    fillPoints(fillPoints.Length - 2) = New PointF(lastPoint.X, Height - 30)
                    fillPoints(fillPoints.Length - 1) = New PointF(40, Height - 30)

                    Using brush As New SolidBrush(Color.FromArgb(50, _spectrumColor))
                        g.FillPolygon(brush, fillPoints)
                    End Using

                    ' Draw spectrum line
                    Using pen As New Pen(_spectrumColor, 2)
                        g.DrawLines(pen, points.ToArray())
                    End Using
                End If
            End Using
        End Sub

        Private Sub DrawPeakHold(g As Graphics)
            If peakHold.Length < 2 Then Return

            ' Calculate Nyquist frequency from sample rate
            Dim nyquistFreq = _currentSampleRate / 2.0F

            Dim points As New List(Of PointF)

            For i = 0 To peakHold.Length - 1
                ' Calculate frequency for this bin using actual sample rate
                Dim freq = (i * nyquistFreq / peakHold.Length)
                
                If freq >= _minFrequency AndAlso freq <= _maxFrequency Then
                    Dim x = MapFrequencyToX(freq)
                    Dim y = MapDBToY(peakHold(i))
                    points.Add(New PointF(x, y))
                End If
            Next

            If points.Count >= 2 Then
                Using pen As New Pen(_peakHoldColor, 1)
                    pen.DashStyle = DashStyle.Dot
                    g.DrawLines(pen, points.ToArray())
                End Using
            End If
        End Sub

        Private Sub DrawLabels(g As Graphics)
            Using font As New Font("Segoe UI", 8)
                Using brush As New SolidBrush(_textColor)
                    ' Frequency labels
                    Dim frequencies = {50, 100, 200, 500, 1000, 2000, 5000, 10000}
                    For Each freq In frequencies
                        If freq >= _minFrequency AndAlso freq <= _maxFrequency Then
                            Dim x = MapFrequencyToX(freq)
                            Dim label = If(freq >= 1000, $"{freq / 1000}k", freq.ToString())
                            Dim size = g.MeasureString(label, font)
                            g.DrawString(label, font, brush, x - size.Width / 2, Height - 25)
                        End If
                    Next

                    ' dB labels
                    For db = _minDB To _maxDB Step 10
                        Dim y = MapDBToY(db)
                        g.DrawString($"{db} dB", font, brush, 2, y - 8)
                    Next
                End Using
            End Using
        End Sub

        Private Function MapFrequencyToX(freq As Single) As Single
            ' Logarithmic frequency scale with equal 40px margins on both sides
            Dim logMin = Math.Log10(_minFrequency)
            Dim logMax = Math.Log10(_maxFrequency)
            Dim logFreq = Math.Log10(freq)
            
            Dim normalized = (logFreq - logMin) / (logMax - logMin)
            Return 40 + normalized * (Width - 80)
        End Function

        Private Function MapDBToY(db As Single) As Single
            ' Clamp dB to range first
            Dim clampedDB = Math.Max(_minDB, Math.Min(_maxDB, db))
            
            ' Linear dB scale
            Dim normalized = (clampedDB - _minDB) / (_maxDB - _minDB)
            Return Height - 30 - normalized * (Height - 40)
        End Function
        
        ''' <summary>
        ''' Clear spectrum data
        ''' </summary>
        Public Sub Clear()
            spectrum = Nothing
            smoothedSpectrum = Nothing
            peakHold = Nothing
            peakHoldTimer = Nothing
            Me.Invalidate()
        End Sub
        
    End Class

End Namespace
