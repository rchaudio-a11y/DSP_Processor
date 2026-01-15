Imports System.IO
Imports DSP_Processor.DSP.FFT
Imports DSP_Processor.Models
Imports DSP_Processor.Services
Imports DSP_Processor.Utils
Imports DSP_Processor.Visualization

Namespace Managers

    ''' <summary>
    ''' Manages the spectrum analyzer subsystem - FFT processing and visualization
    ''' </summary>
    Public Class SpectrumManager
        Implements IDisposable

        ' FFT Processors
        Private fftInput As FFTProcessor
        Private fftOutput As FFTProcessor

        ' Display Controls
        Private spectrumInputDisplay As SpectrumDisplayControl
        Private spectrumOutputDisplay As SpectrumDisplayControl

        ' Update Timer
        Private WithEvents updateTimer As Timer

        ' Settings
        Private currentSettings As SpectrumSettings

        ' UI Controls (for settings binding)
        Private cmbFFTSize As ComboBox
        Private cmbWindowFunction As ComboBox
        Private numSmoothing As NumericUpDown
        Private chkPeakHold As CheckBox

        ''' <summary>
        ''' Initialize the spectrum analyzer with UI controls and display container
        ''' </summary>
        Public Sub Initialize(displayTab As TabPage,
                              fftSizeCombo As ComboBox,
                              windowFunctionCombo As ComboBox,
                              smoothingNumeric As NumericUpDown,
                              peakHoldCheck As CheckBox)

            Try
                LoggingServiceAdapter.Instance.LogInfo("Initializing spectrum analyzer...")

                ' Store UI control references
                Me.cmbFFTSize = fftSizeCombo
                Me.cmbWindowFunction = windowFunctionCombo
                Me.numSmoothing = smoothingNumeric
                Me.chkPeakHold = peakHoldCheck

                ' Load saved settings
                currentSettings = LoadSettings()

                ' Create FFT processors
                fftInput = New FFTProcessor(4096) With {
                    .WindowFunction = FFTProcessor.WindowType.Hann,
                    .SampleRate = 44100
                }

                fftOutput = New FFTProcessor(4096) With {
                    .WindowFunction = FFTProcessor.WindowType.Hann,
                    .SampleRate = 44100
                }

                ' Create spectrum display controls
                spectrumInputDisplay = New SpectrumDisplayControl() With {
                    .Dock = DockStyle.Fill,
                    .BackColor = Color.Black,
                    .SpectrumColor = Color.Cyan,
                    .PeakHoldColor = Color.Red,
                    .MinFrequency = 20,
                    .MaxFrequency = 20000,
                    .MinDB = -60,
                    .MaxDB = 0,
                    .PeakHoldEnabled = False,
                    .SmoothingFactor = 0.7F
                }

                spectrumOutputDisplay = New SpectrumDisplayControl() With {
                    .Dock = DockStyle.Fill,
                    .BackColor = Color.Black,
                    .SpectrumColor = Color.Lime,
                    .PeakHoldColor = Color.Orange,
                    .MinFrequency = 20,
                    .MaxFrequency = 20000,
                    .MinDB = -60,
                    .MaxDB = 0,
                    .PeakHoldEnabled = False,
                    .SmoothingFactor = 0.7F
                }

                ' Create split container for side-by-side displays
                Dim splitSpectrum As New SplitContainer() With {
                    .Dock = DockStyle.Fill,
                    .Orientation = Orientation.Vertical,
                    .SplitterDistance = displayTab.Width \ 2
                }

                ' Add labels
                Dim lblPreDSP As New Label() With {
                    .Text = "PRE-DSP (Input)",
                    .Dock = DockStyle.Top,
                    .TextAlign = ContentAlignment.MiddleCenter,
                    .ForeColor = Color.Cyan,
                    .BackColor = Color.FromArgb(30, 30, 30),
                    .Height = 30,
                    .Font = New Font("Segoe UI", 10, FontStyle.Bold)
                }

                Dim lblPostDSP As New Label() With {
                    .Text = "POST-DSP (Output)",
                    .Dock = DockStyle.Top,
                    .TextAlign = ContentAlignment.MiddleCenter,
                    .ForeColor = Color.Lime,
                    .BackColor = Color.FromArgb(30, 30, 30),
                    .Height = 30,
                    .Font = New Font("Segoe UI", 10, FontStyle.Bold)
                }

                ' Add displays to split panels
                splitSpectrum.Panel1.Controls.Add(spectrumInputDisplay)
                splitSpectrum.Panel1.Controls.Add(lblPreDSP)

                splitSpectrum.Panel2.Controls.Add(spectrumOutputDisplay)
                splitSpectrum.Panel2.Controls.Add(lblPostDSP)

                ' Add split container to display tab
                displayTab.Controls.Add(splitSpectrum)

                ' Adjust splitter when tab resizes
                AddHandler displayTab.Resize, Sub() splitSpectrum.SplitterDistance = displayTab.Width \ 2

                ' Apply dark theme
                UI.DarkTheme.ApplyToControl(splitSpectrum)

                ' Initialize UI controls
                cmbFFTSize.SelectedIndex = 2 ' 4096
                cmbWindowFunction.SelectedIndex = 1 ' Hann
                numSmoothing.Value = 70
                chkPeakHold.Checked = False

                ' Wire up event handlers
                AddHandler cmbFFTSize.SelectedIndexChanged, AddressOf OnFFTSizeChanged
                AddHandler cmbWindowFunction.SelectedIndexChanged, AddressOf OnWindowFunctionChanged
                AddHandler numSmoothing.ValueChanged, AddressOf OnSmoothingChanged
                AddHandler chkPeakHold.CheckedChanged, AddressOf OnPeakHoldChanged

                ' Apply loaded settings (will override defaults if settings exist)
                ApplySettings(currentSettings)

                ' Create update timer (30 FPS)
                updateTimer = New Timer() With {
                    .Interval = 33,
                    .Enabled = False
                }

                LoggingServiceAdapter.Instance.LogInfo("Spectrum analyzer initialized successfully")
                Logger.Instance.Info("Spectrum analyzer ready", "SpectrumManager")

            Catch ex As Exception
                LoggingServiceAdapter.Instance.LogError($"Failed to initialize spectrum analyzer: {ex.Message}", ex)
                Logger.Instance.Error("Failed to initialize spectrum analyzer", ex, "SpectrumManager")
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Start the spectrum analyzer (start timer and enable updates)
        ''' </summary>
        Public Sub Start()
            If updateTimer IsNot Nothing Then
                updateTimer.Start()
                LoggingServiceAdapter.Instance.LogInfo("Spectrum analyzer started")
            End If
        End Sub

        ''' <summary>
        ''' Stop the spectrum analyzer (stop timer)
        ''' </summary>
        Public Sub [Stop]()
            If updateTimer IsNot Nothing Then
                updateTimer.Stop()
                LoggingServiceAdapter.Instance.LogInfo("Spectrum analyzer stopped")
            End If
        End Sub

        ''' <summary>
        ''' Add audio samples to the input spectrum (pre-DSP)
        ''' </summary>
        Public Sub AddInputSamples(buffer As Byte(), length As Integer, bitsPerSample As Integer)
            If fftInput IsNot Nothing Then
                fftInput.AddSamples(buffer, length, bitsPerSample, 1) ' Assume mono for now
            End If
        End Sub

        ''' <summary>
        ''' Add audio samples to the output spectrum (post-DSP)
        ''' </summary>
        Public Sub AddOutputSamples(buffer As Byte(), length As Integer, bitsPerSample As Integer)
            If fftOutput IsNot Nothing Then
                fftOutput.AddSamples(buffer, length, bitsPerSample, 1) ' Assume mono for now
            End If
        End Sub

        ''' <summary>
        ''' Clear all spectrum data
        ''' </summary>
        Public Sub Clear()
            Try
                fftInput?.Clear()
                fftOutput?.Clear()
                spectrumInputDisplay?.Clear()
                spectrumOutputDisplay?.Clear()
                LoggingServiceAdapter.Instance.LogInfo("Spectrum analyzer reset")
            Catch ex As Exception
                Logger.Instance.Error("Failed to reset spectrum", ex, "SpectrumManager")
            End Try
        End Sub

        ''' <summary>
        ''' Timer tick - update spectrum displays
        ''' </summary>
        Private Sub UpdateTimer_Tick(sender As Object, e As EventArgs) Handles updateTimer.Tick
            Try
                ' Calculate spectra
                Dim spectrumIn = fftInput.CalculateSpectrum()
                Dim spectrumOut = fftOutput.CalculateSpectrum()

                ' Update displays
                If spectrumIn IsNot Nothing AndAlso spectrumIn.Length > 0 Then
                    spectrumInputDisplay.UpdateSpectrum(spectrumIn, fftInput.SampleRate, fftInput.FFTSize)
                End If

                If spectrumOut IsNot Nothing AndAlso spectrumOut.Length > 0 Then
                    spectrumOutputDisplay.UpdateSpectrum(spectrumOut, fftOutput.SampleRate, fftOutput.FFTSize)
                End If
            Catch ex As Exception
                ' Log but don't crash
                Logger.Instance.Error($"Spectrum update error: {ex.Message}", ex, "SpectrumManager")
            End Try
        End Sub

#Region "Settings Management"

        Private Function LoadSettings() As SpectrumSettings
            Dim settingsFile = Path.Combine(Application.StartupPath, "spectrum_settings.json")
            If File.Exists(settingsFile) Then
                Try
                    Dim json = File.ReadAllText(settingsFile)
                    Dim settings = SpectrumSettings.FromJson(json)
                    LoggingServiceAdapter.Instance.LogInfo("Spectrum settings loaded from file")
                    Return settings
                Catch ex As Exception
                    LoggingServiceAdapter.Instance.LogWarning($"Failed to load spectrum settings: {ex.Message}")
                    Logger.Instance.Warning("Failed to load spectrum settings, using defaults", "SpectrumManager")
                End Try
            End If
            Return New SpectrumSettings() ' Defaults
        End Function

        Private Sub SaveSettings(settings As SpectrumSettings)
            Dim settingsFile = Path.Combine(Application.StartupPath, "spectrum_settings.json")
            Try
                File.WriteAllText(settingsFile, settings.ToJson())
                LoggingServiceAdapter.Instance.LogInfo("Spectrum settings saved to file")
            Catch ex As Exception
                LoggingServiceAdapter.Instance.LogError($"Failed to save spectrum settings: {ex.Message}", ex)
                Logger.Instance.Error("Failed to save spectrum settings", ex, "SpectrumManager")
            End Try
        End Sub

        Private Sub ApplySettings(settings As SpectrumSettings)
            Try
                ' Apply to FFT processors
                If fftInput IsNot Nothing Then
                    fftInput.FFTSize = settings.FFTSize
                    fftOutput.FFTSize = settings.FFTSize

                    Dim windowType As FFTProcessor.WindowType
                    Select Case settings.WindowFunction
                        Case "None"
                            windowType = FFTProcessor.WindowType.None
                        Case "Hann"
                            windowType = FFTProcessor.WindowType.Hann
                        Case "Hamming"
                            windowType = FFTProcessor.WindowType.Hamming
                        Case "Blackman"
                            windowType = FFTProcessor.WindowType.Blackman
                        Case Else
                            windowType = FFTProcessor.WindowType.Hann
                    End Select

                    fftInput.WindowFunction = windowType
                    fftOutput.WindowFunction = windowType
                End If

                ' Apply to displays
                If spectrumInputDisplay IsNot Nothing Then
                    Dim smoothing = CSng(settings.Smoothing / 100.0)
                    spectrumInputDisplay.SmoothingFactor = smoothing
                    spectrumOutputDisplay.SmoothingFactor = smoothing
                    spectrumInputDisplay.PeakHoldEnabled = settings.PeakHoldEnabled
                    spectrumOutputDisplay.PeakHoldEnabled = settings.PeakHoldEnabled
                End If

                ' Update UI controls to match
                For i = 0 To cmbFFTSize.Items.Count - 1
                    If cmbFFTSize.Items(i).ToString() = settings.FFTSize.ToString() Then
                        cmbFFTSize.SelectedIndex = i
                        Exit For
                    End If
                Next

                For i = 0 To cmbWindowFunction.Items.Count - 1
                    If cmbWindowFunction.Items(i).ToString() = settings.WindowFunction Then
                        cmbWindowFunction.SelectedIndex = i
                        Exit For
                    End If
                Next

                numSmoothing.Value = settings.Smoothing
                chkPeakHold.Checked = settings.PeakHoldEnabled

                LoggingServiceAdapter.Instance.LogInfo($"Spectrum settings applied: FFT={settings.FFTSize}, Window={settings.WindowFunction}")

            Catch ex As Exception
                LoggingServiceAdapter.Instance.LogError($"Failed to apply spectrum settings: {ex.Message}", ex)
                Logger.Instance.Error("Failed to apply spectrum settings", ex, "SpectrumManager")
            End Try
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub OnFFTSizeChanged(sender As Object, e As EventArgs)
            If fftInput IsNot Nothing AndAlso cmbFFTSize.SelectedItem IsNot Nothing Then
                Try
                    Dim newSize = Integer.Parse(cmbFFTSize.SelectedItem.ToString())
                    fftInput.FFTSize = newSize
                    fftOutput.FFTSize = newSize

                    ' Save settings
                    currentSettings.FFTSize = newSize
                    SaveSettings(currentSettings)

                    LoggingServiceAdapter.Instance.LogInfo($"FFT size changed to: {newSize}")
                Catch ex As Exception
                    Logger.Instance.Error("Failed to change FFT size", ex, "SpectrumManager")
                End Try
            End If
        End Sub

        Private Sub OnWindowFunctionChanged(sender As Object, e As EventArgs)
            If fftInput IsNot Nothing AndAlso cmbWindowFunction.SelectedItem IsNot Nothing Then
                Try
                    Dim windowType As FFTProcessor.WindowType
                    Select Case cmbWindowFunction.SelectedItem.ToString()
                        Case "None"
                            windowType = FFTProcessor.WindowType.None
                        Case "Hann"
                            windowType = FFTProcessor.WindowType.Hann
                        Case "Hamming"
                            windowType = FFTProcessor.WindowType.Hamming
                        Case "Blackman"
                            windowType = FFTProcessor.WindowType.Blackman
                        Case Else
                            windowType = FFTProcessor.WindowType.Hann
                    End Select

                    fftInput.WindowFunction = windowType
                    fftOutput.WindowFunction = windowType

                    ' Save settings
                    currentSettings.WindowFunction = cmbWindowFunction.SelectedItem.ToString()
                    SaveSettings(currentSettings)

                    LoggingServiceAdapter.Instance.LogInfo($"Window function changed to: {cmbWindowFunction.SelectedItem}")
                Catch ex As Exception
                    Logger.Instance.Error("Failed to change window function", ex, "SpectrumManager")
                End Try
            End If
        End Sub

        Private Sub OnSmoothingChanged(sender As Object, e As EventArgs)
            If spectrumInputDisplay IsNot Nothing Then
                Try
                    Dim smoothing = CSng(numSmoothing.Value / 100.0)
                    spectrumInputDisplay.SmoothingFactor = smoothing
                    spectrumOutputDisplay.SmoothingFactor = smoothing

                    ' Save settings
                    currentSettings.Smoothing = CInt(numSmoothing.Value)
                    SaveSettings(currentSettings)

                    LoggingServiceAdapter.Instance.LogInfo($"Smoothing changed to: {numSmoothing.Value}%")
                Catch ex As Exception
                    Logger.Instance.Error("Failed to change smoothing", ex, "SpectrumManager")
                End Try
            End If
        End Sub

        Private Sub OnPeakHoldChanged(sender As Object, e As EventArgs)
            If spectrumInputDisplay IsNot Nothing Then
                Try
                    spectrumInputDisplay.PeakHoldEnabled = chkPeakHold.Checked
                    spectrumOutputDisplay.PeakHoldEnabled = chkPeakHold.Checked

                    ' Save settings
                    currentSettings.PeakHoldEnabled = chkPeakHold.Checked
                    SaveSettings(currentSettings)

                    LoggingServiceAdapter.Instance.LogInfo($"Peak hold: {If(chkPeakHold.Checked, "Enabled", "Disabled")}")
                Catch ex As Exception
                    Logger.Instance.Error("Failed to change peak hold", ex, "SpectrumManager")
                End Try
            End If
        End Sub

#End Region

#Region "IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            [Stop]()

            ' Unhook event handlers
            If cmbFFTSize IsNot Nothing Then
                RemoveHandler cmbFFTSize.SelectedIndexChanged, AddressOf OnFFTSizeChanged
            End If
            If cmbWindowFunction IsNot Nothing Then
                RemoveHandler cmbWindowFunction.SelectedIndexChanged, AddressOf OnWindowFunctionChanged
            End If
            If numSmoothing IsNot Nothing Then
                RemoveHandler numSmoothing.ValueChanged, AddressOf OnSmoothingChanged
            End If
            If chkPeakHold IsNot Nothing Then
                RemoveHandler chkPeakHold.CheckedChanged, AddressOf OnPeakHoldChanged
            End If

            updateTimer?.Dispose()
            spectrumInputDisplay?.Clear()
            spectrumOutputDisplay?.Clear()
        End Sub

#End Region

    End Class

End Namespace
