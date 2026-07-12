Imports DSP_Processor.Models

Namespace UI.TabPanels

    ''' <summary>
    ''' Spectrum settings panel - FFT configuration and display options.
    ''' NOW USES DESIGNER! All controls are in SpectrumSettingsPanel.Designer.vb
    ''' </summary>
    Partial Public Class SpectrumSettingsPanel
        Inherits UserControl

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

            ' Wire up events (controls already exist in Designer!)
            AddHandler cmbFFTSize.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler cmbWindowFunction.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler numSmoothing.ValueChanged, AddressOf OnControlChanged
            AddHandler chkPeakHold.CheckedChanged, AddressOf OnControlChanged
            AddHandler trackMinFreq.ValueChanged, AddressOf OnTrackBarChanged
            AddHandler trackMaxFreq.ValueChanged, AddressOf OnTrackBarChanged
            AddHandler trackDBRange.ValueChanged, AddressOf OnTrackBarChanged
            AddHandler btnResetSpectrum.Click, AddressOf OnResetClick

            ' Set defaults
            cmbFFTSize.SelectedIndex = 2 ' 4096
            cmbWindowFunction.SelectedIndex = 1 ' Hann
            numSmoothing.Value = 5
            chkPeakHold.Checked = True

            ' Update trackbar labels
            UpdateTrackBarLabels()
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
                UpdateTrackBarLabels()

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
            Dim dbRange = trackDBRange.Value
            settings.MinDB = -dbRange
            settings.MaxDB = 0

            Return settings
        End Function

#End Region

#Region "Event Handlers"

        Private Sub OnControlChanged(sender As Object, e As EventArgs)
            If suppressEvents Then Return
            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub

        Private Sub OnTrackBarChanged(sender As Object, e As EventArgs)
            UpdateTrackBarLabels()

            If Not suppressEvents Then
                RaiseEvent SettingsChanged(Me, GetSettings())
            End If
        End Sub

        Private Sub OnResetClick(sender As Object, e As EventArgs)
            RaiseEvent ResetRequested(Me, EventArgs.Empty)
        End Sub

        Private Sub UpdateTrackBarLabels()
            lblMinFreqValue.Text = $"{trackMinFreq.Value} Hz"
            lblMaxFreqValue.Text = $"{trackMaxFreq.Value} Hz"
            lblDBRangeValue.Text = $"{trackDBRange.Value} dB"
        End Sub

#End Region

    End Class

End Namespace
