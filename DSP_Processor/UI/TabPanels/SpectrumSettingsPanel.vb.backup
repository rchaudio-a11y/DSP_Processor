Imports DSP_Processor.Models

Namespace UI.TabPanels

    ''' <summary>
    ''' Spectrum settings panel - FFT configuration and display options.
    ''' Replaces hard-coded grpFFTSettings on Spectrum tab.
    ''' </summary>
    Public Class SpectrumSettingsPanel
        Inherits UserControl

#Region "Controls"

        Private grpFFTSettings As GroupBox
        Private lblFFTSize As Label
        Private cmbFFTSize As ComboBox
        Private lblWindowFunction As Label
        Private cmbWindowFunction As ComboBox
        Private lblSmoothing As Label
        Private numSmoothing As NumericUpDown
        Private chkPeakHold As CheckBox
        Private btnResetSpectrum As Button
        Private lblMinFreq As Label
        Private trackMinFreq As TrackBar
        Private lblMinFreqValue As Label
        Private lblMaxFreq As Label
        Private trackMaxFreq As TrackBar
        Private lblMaxFreqValue As Label
        Private lblDBRange As Label
        Private trackDBRange As TrackBar
        Private lblDBRangeValue As Label

#End Region

#Region "Events"

        ''' <summary>Raised when spectrum settings change</summary>
        Public Event SettingsChanged As EventHandler(Of SpectrumSettings)

        ''' <summary>Raised when reset button clicked</summary>
        Public Event ResetRequested As EventHandler

#End Region

#Region "Fields"

        Private suppressEvents As Boolean = False

#End Region

#Region "Constructor"

        Public Sub New()
            InitializeComponent()
        End Sub

#End Region

#Region "Initialization"

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            ' Main container
            Me.AutoScaleMode = AutoScaleMode.Font
            Me.Size = New Size(440, 550)
            Me.BackColor = Color.FromArgb(45, 45, 48)

            ' Create group box
            grpFFTSettings = New GroupBox With {
                .Text = "FFT Settings",
                .Location = New Point(10, 10),
                .Size = New Size(420, 530),
                .ForeColor = Color.White
            }

            ' FFT Size
            lblFFTSize = New Label With {
                .Text = "FFT Size:",
                .Location = New Point(10, 30),
                .Size = New Size(65, 20),
                .ForeColor = Color.White
            }

            cmbFFTSize = New ComboBox With {
                .Location = New Point(10, 53),
                .Size = New Size(150, 28),
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            cmbFFTSize.Items.AddRange(New Object() {"1024", "2048", "4096", "8192", "16384"})
            cmbFFTSize.SelectedIndex = 2 ' 4096
            AddHandler cmbFFTSize.SelectedIndexChanged, AddressOf OnSettingChanged

            ' Window Function
            lblWindowFunction = New Label With {
                .Text = "Window Function:",
                .Location = New Point(10, 90),
                .Size = New Size(127, 20),
                .ForeColor = Color.White
            }

            cmbWindowFunction = New ComboBox With {
                .Location = New Point(10, 113),
                .Size = New Size(150, 28),
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            cmbWindowFunction.Items.AddRange(New Object() {"None", "Hann", "Hamming", "Blackman"})
            cmbWindowFunction.SelectedIndex = 1 ' Hann
            AddHandler cmbWindowFunction.SelectedIndexChanged, AddressOf OnSettingChanged

            ' Smoothing
            lblSmoothing = New Label With {
                .Text = "Smoothing:",
                .Location = New Point(10, 150),
                .Size = New Size(80, 20),
                .ForeColor = Color.White
            }

            numSmoothing = New NumericUpDown With {
                .Location = New Point(10, 173),
                .Size = New Size(80, 27),
                .Minimum = 0,
                .Maximum = 10,
                .Value = 3,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler numSmoothing.ValueChanged, AddressOf OnSettingChanged

            ' Peak Hold
            chkPeakHold = New CheckBox With {
                .Text = "Peak Hold",
                .Location = New Point(100, 173),
                .Size = New Size(100, 24),
                .ForeColor = Color.White
            }
            AddHandler chkPeakHold.CheckedChanged, AddressOf OnSettingChanged

            ' Min Frequency
            lblMinFreq = New Label With {
                .Text = "Min Frequency:",
                .Location = New Point(10, 210),
                .Size = New Size(110, 20),
                .ForeColor = Color.White
            }

            trackMinFreq = New TrackBar With {
                .Location = New Point(10, 233),
                .Size = New Size(300, 56),
                .Minimum = 20,
                .Maximum = 1000,
                .Value = 20,
                .TickFrequency = 100
            }
            AddHandler trackMinFreq.ValueChanged, AddressOf OnMinFreqChanged

            lblMinFreqValue = New Label With {
                .Text = "20 Hz",
                .Location = New Point(320, 240),
                .Size = New Size(80, 20),
                .ForeColor = Color.White
            }

            ' Max Frequency
            lblMaxFreq = New Label With {
                .Text = "Max Frequency:",
                .Location = New Point(10, 290),
                .Size = New Size(115, 20),
                .ForeColor = Color.White
            }

            trackMaxFreq = New TrackBar With {
                .Location = New Point(10, 313),
                .Size = New Size(300, 56),
                .Minimum = 1000,
                .Maximum = 22000,
                .Value = 12000,
                .TickFrequency = 2000
            }
            AddHandler trackMaxFreq.ValueChanged, AddressOf OnMaxFreqChanged

            lblMaxFreqValue = New Label With {
                .Text = "12000 Hz",
                .Location = New Point(320, 320),
                .Size = New Size(80, 20),
                .ForeColor = Color.White
            }

            ' DB Range
            lblDBRange = New Label With {
                .Text = "dB Range:",
                .Location = New Point(10, 370),
                .Size = New Size(80, 20),
                .ForeColor = Color.White
            }

            trackDBRange = New TrackBar With {
                .Location = New Point(10, 393),
                .Size = New Size(300, 56),
                .Minimum = 20,
                .Maximum = 120,
                .Value = 60,
                .TickFrequency = 10
            }
            AddHandler trackDBRange.ValueChanged, AddressOf OnDBRangeChanged

            lblDBRangeValue = New Label With {
                .Text = "60 dB",
                .Location = New Point(320, 400),
                .Size = New Size(80, 20),
                .ForeColor = Color.White
            }

            ' Reset Button
            btnResetSpectrum = New Button With {
                .Text = "Reset to Defaults",
                .Location = New Point(10, 460),
                .Size = New Size(150, 40),
                .BackColor = Color.FromArgb(60, 60, 60),
                .FlatStyle = FlatStyle.Flat,
                .ForeColor = Color.White
            }
            AddHandler btnResetSpectrum.Click, AddressOf OnResetClick

            ' Add all controls to group box
            grpFFTSettings.Controls.AddRange(New Control() {
                lblFFTSize, cmbFFTSize,
                lblWindowFunction, cmbWindowFunction,
                lblSmoothing, numSmoothing,
                chkPeakHold,
                lblMinFreq, trackMinFreq, lblMinFreqValue,
                lblMaxFreq, trackMaxFreq, lblMaxFreqValue,
                lblDBRange, trackDBRange, lblDBRangeValue,
                btnResetSpectrum
            })

            ' Add group box to panel
            Me.Controls.Add(grpFFTSettings)

            Me.ResumeLayout(False)
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>Load spectrum settings into UI</summary>
        Public Sub LoadSettings(settings As SpectrumSettings)
            If settings Is Nothing Then Return

            suppressEvents = True
            Try
                ' FFT Size
                Dim fftSizeStr = settings.FFTSize.ToString()
                Dim index = cmbFFTSize.Items.IndexOf(fftSizeStr)
                If index >= 0 Then
                    cmbFFTSize.SelectedIndex = index
                End If

                ' Window Function
                Dim windowIndex = 0
                Select Case settings.WindowFunction.ToLowerInvariant()
                    Case "none" : windowIndex = 0
                    Case "hann" : windowIndex = 1
                    Case "hamming" : windowIndex = 2
                    Case "blackman" : windowIndex = 3
                End Select
                cmbWindowFunction.SelectedIndex = windowIndex

                ' Smoothing
                numSmoothing.Value = Math.Min(Math.Max(settings.Smoothing \ 10, numSmoothing.Minimum), numSmoothing.Maximum)

                ' Peak Hold
                chkPeakHold.Checked = settings.PeakHoldEnabled

                ' Frequency Range
                trackMinFreq.Value = Math.Min(Math.Max(settings.MinFrequency, trackMinFreq.Minimum), trackMinFreq.Maximum)
                trackMaxFreq.Value = Math.Min(Math.Max(settings.MaxFrequency, trackMaxFreq.Minimum), trackMaxFreq.Maximum)

                ' DB Range
                trackDBRange.Value = Math.Min(Math.Max(settings.MaxDB - settings.MinDB, trackDBRange.Minimum), trackDBRange.Maximum)

                ' Update labels
                lblMinFreqValue.Text = $"{trackMinFreq.Value} Hz"
                lblMaxFreqValue.Text = $"{trackMaxFreq.Value} Hz"
                lblDBRangeValue.Text = $"{trackDBRange.Value} dB"

            Finally
                suppressEvents = False
            End Try
        End Sub

        ''' <summary>Get current settings from UI</summary>
        Public Function GetSettings() As SpectrumSettings
            Dim settings = New SpectrumSettings()

            ' FFT Size
            If Integer.TryParse(cmbFFTSize.SelectedItem?.ToString(), settings.FFTSize) Then
                ' Success
            End If

            ' Window Function
            Dim windowNames = New String() {"None", "Hann", "Hamming", "Blackman"}
            If cmbWindowFunction.SelectedIndex >= 0 AndAlso cmbWindowFunction.SelectedIndex < windowNames.Length Then
                settings.WindowFunction = windowNames(cmbWindowFunction.SelectedIndex)
            End If

            ' Smoothing
            settings.Smoothing = CInt(numSmoothing.Value) * 10

            ' Peak Hold
            settings.PeakHoldEnabled = chkPeakHold.Checked

            ' Frequency Range
            settings.MinFrequency = trackMinFreq.Value
            settings.MaxFrequency = trackMaxFreq.Value

            ' DB Range
            settings.MinDB = -trackDBRange.Value
            settings.MaxDB = 0

            Return settings
        End Function

#End Region

#Region "Event Handlers"

        Private Sub OnSettingChanged(sender As Object, e As EventArgs)
            If Not suppressEvents Then
                RaiseEvent SettingsChanged(Me, GetSettings())
            End If
        End Sub

        Private Sub OnMinFreqChanged(sender As Object, e As EventArgs)
            lblMinFreqValue.Text = $"{trackMinFreq.Value} Hz"
            OnSettingChanged(sender, e)
        End Sub

        Private Sub OnMaxFreqChanged(sender As Object, e As EventArgs)
            lblMaxFreqValue.Text = $"{trackMaxFreq.Value} Hz"
            OnSettingChanged(sender, e)
        End Sub

        Private Sub OnDBRangeChanged(sender As Object, e As EventArgs)
            lblDBRangeValue.Text = $"{trackDBRange.Value} dB"
            OnSettingChanged(sender, e)
        End Sub

        Private Sub OnResetClick(sender As Object, e As EventArgs)
            RaiseEvent ResetRequested(Me, EventArgs.Empty)
        End Sub

#End Region

    End Class

End Namespace
