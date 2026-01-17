Imports DSP_Processor.Models
Imports DSP_Processor.UI

Namespace UI.TabPanels

    ''' <summary>
    ''' Input settings tab panel for configuring input volume and meter display parameters
    ''' NOW USES DESIGNER! All controls are in InputTabPanel.Designer.vb
    ''' </summary>
    Partial Public Class InputTabPanel
        Inherits UserControl

        Public Event SettingsChanged As EventHandler(Of MeterSettings)

        Private suppressEvents As Boolean = False

        Public Sub New()
            InitializeComponent()

            ' Wire up events (controls already exist in Designer!)
            AddHandler trackInputVolume.Scroll, AddressOf OnControlChanged
            AddHandler chkLinkToPlayback.CheckedChanged, AddressOf OnControlChanged
            AddHandler cmbPeakHold.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler cmbPeakDecay.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler cmbRmsWindow.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler cmbAttack.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler cmbRelease.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler cmbClipThreshold.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler cmbClipHold.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler btnFastResponse.Click, AddressOf btnFastResponse_Click
            AddHandler btnSlowResponse.Click, AddressOf btnSlowResponse_Click
            AddHandler btnBroadcast.Click, AddressOf btnBroadcast_Click
            AddHandler btnReset.Click, AddressOf btnReset_Click

            ' Populate combo boxes
            PopulateControls()
        End Sub

        Private Sub PopulateControls()
            ' Peak Hold Time
            cmbPeakHold.Items.AddRange({"100ms", "250ms", "500ms", "1000ms", "2000ms", "Infinite"})
            cmbPeakHold.SelectedIndex = 2 ' 500ms

            ' Peak Decay Rate
            cmbPeakDecay.Items.AddRange({"1dB/s", "3dB/s", "6dB/s", "12dB/s", "24dB/s", "Instant"})
            cmbPeakDecay.SelectedIndex = 1 ' 3dB/s

            ' RMS Window
            cmbRmsWindow.Items.AddRange({"10ms", "30ms", "50ms", "100ms", "300ms", "500ms"})
            cmbRmsWindow.SelectedIndex = 2 ' 50ms

            ' Attack Time
            cmbAttack.Items.AddRange({"0ms (instant)", "10ms", "30ms", "50ms", "100ms"})
            cmbAttack.SelectedIndex = 0 ' 0ms

            ' Release Time
            cmbRelease.Items.AddRange({"100ms", "300ms", "500ms", "1000ms", "2000ms"})
            cmbRelease.SelectedIndex = 1 ' 300ms

            ' Clip Threshold
            cmbClipThreshold.Items.AddRange({"-0.5dB", "-0.3dB", "-0.1dB", "0dB"})
            cmbClipThreshold.SelectedIndex = 2 ' -0.1dB

            ' Clip Hold
            cmbClipHold.Items.AddRange({"500ms", "1000ms", "2000ms", "5000ms", "Infinite"})
            cmbClipHold.SelectedIndex = 2 ' 2000ms
        End Sub

        Public Function GetSettings() As MeterSettings
            Dim settings = New MeterSettings()

            ' NOTE: Input volume removed - use DSPSignalFlowPanel or AudioPipelinePanel for gain control

            ' Peak Behavior
            settings.PeakHoldMs = ParseMilliseconds(cmbPeakHold.SelectedItem?.ToString())
            settings.PeakDecayDbPerSec = ParseDecayRate(cmbPeakDecay.SelectedItem?.ToString())

            ' RMS & Ballistics
            settings.RmsWindowMs = ParseMilliseconds(cmbRmsWindow.SelectedItem?.ToString())
            settings.AttackMs = ParseMilliseconds(cmbAttack.SelectedItem?.ToString())
            settings.ReleaseMs = ParseMilliseconds(cmbRelease.SelectedItem?.ToString())

            ' Clipping
            settings.ClipThresholdDb = ParseDbValue(cmbClipThreshold.SelectedItem?.ToString())
            settings.ClipHoldMs = ParseMilliseconds(cmbClipHold.SelectedItem?.ToString())

            Return settings
        End Function

        Public Sub LoadSettings(settings As MeterSettings)
            suppressEvents = True

            Try
                ' NOTE: Input volume removed - use DSPSignalFlowPanel or AudioPipelinePanel for gain control

                ' Peak Behavior
                SelectComboValue(cmbPeakHold, settings.PeakHoldMs, "ms")
                SelectComboValue(cmbPeakDecay, settings.PeakDecayDbPerSec, "dB/s")

                ' RMS & Ballistics
                SelectComboValue(cmbRmsWindow, settings.RmsWindowMs, "ms")
                SelectComboValue(cmbAttack, settings.AttackMs, "ms")
                SelectComboValue(cmbRelease, settings.ReleaseMs, "ms")

                ' Clipping
                SelectComboValue(cmbClipThreshold, settings.ClipThresholdDb, "dB")
                SelectComboValue(cmbClipHold, settings.ClipHoldMs, "ms")

            Finally
                suppressEvents = False
            End Try
        End Sub

#Region "Event Handlers"

        Private Sub OnControlChanged(sender As Object, e As EventArgs)
            If suppressEvents Then Return

            ' Update volume label
            If TypeOf sender Is TrackBar AndAlso sender Is trackInputVolume Then
                lblInputVolumeValue.Text = $"{trackInputVolume.Value}%"
            End If

            ' Raise settings changed event
            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub

        Private Sub btnFastResponse_Click(sender As Object, e As EventArgs)
            suppressEvents = True
            Try
                ' Fast response preset
                SelectComboByText(cmbPeakHold, "250ms")
                SelectComboByText(cmbPeakDecay, "24dB/s")
                SelectComboByText(cmbRmsWindow, "10ms")
                SelectComboByText(cmbAttack, "0ms (instant)")
                SelectComboByText(cmbRelease, "100ms")
            Finally
                suppressEvents = False
            End Try

            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub

        Private Sub btnSlowResponse_Click(sender As Object, e As EventArgs)
            suppressEvents = True
            Try
                ' Slow response preset
                SelectComboByText(cmbPeakHold, "2000ms")
                SelectComboByText(cmbPeakDecay, "3dB/s")
                SelectComboByText(cmbRmsWindow, "500ms")
                SelectComboByText(cmbAttack, "100ms")
                SelectComboByText(cmbRelease, "2000ms")
            Finally
                suppressEvents = False
            End Try

            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub

        Private Sub btnBroadcast_Click(sender As Object, e As EventArgs)
            suppressEvents = True
            Try
                ' BBC/EBU broadcast standard
                SelectComboByText(cmbPeakHold, "1000ms")
                SelectComboByText(cmbPeakDecay, "12dB/s")
                SelectComboByText(cmbRmsWindow, "300ms")
                SelectComboByText(cmbAttack, "10ms")
                SelectComboByText(cmbRelease, "1000ms")
            Finally
                suppressEvents = False
            End Try

            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub

        Private Sub btnReset_Click(sender As Object, e As EventArgs)
            Dim defaults = New MeterSettings()
            LoadSettings(defaults)
            RaiseEvent SettingsChanged(Me, defaults)
        End Sub

#End Region

#Region "Helper Methods"

        Private Function ParseMilliseconds(text As String) As Integer
            If String.IsNullOrEmpty(text) Then Return 500
            If text.Contains("Infinite") Then Return -1
            If text.Contains("instant") Then Return 0

            Dim numStr = text.Replace("ms", "").Trim()
            Dim result As Integer
            If Integer.TryParse(numStr, result) Then
                Return result
            End If
            Return 500
        End Function

        Private Function ParseDecayRate(text As String) As Single
            If String.IsNullOrEmpty(text) Then Return 20.0F
            If text.Contains("Instant") Then Return 1000.0F

            Dim numStr = text.Replace("dB/s", "").Trim()
            Dim result As Single
            If Single.TryParse(numStr, result) Then
                Return result
            End If
            Return 20.0F
        End Function

        Private Function ParseDbValue(text As String) As Single
            If String.IsNullOrEmpty(text) Then Return -0.1F

            Dim numStr = text.Replace("dB", "").Trim()
            Dim result As Single
            If Single.TryParse(numStr, result) Then
                Return result
            End If
            Return -0.1F
        End Function

        Private Sub SelectComboValue(combo As ComboBox, value As Object, suffix As String)
            If combo Is Nothing OrElse value Is Nothing Then Return

            For i = 0 To combo.Items.Count - 1
                Dim item = combo.Items(i).ToString()
                If item.Contains(value.ToString()) Then
                    combo.SelectedIndex = i
                    Return
                End If
            Next

            ' Default to middle item
            If combo.Items.Count > 0 Then
                combo.SelectedIndex = combo.Items.Count \ 2
            End If
        End Sub

        Private Sub SelectComboByText(combo As ComboBox, text As String)
            For i = 0 To combo.Items.Count - 1
                If combo.Items(i).ToString() = text Then
                    combo.SelectedIndex = i
                    Return
                End If
            Next
        End Sub

#End Region

    End Class

End Namespace
