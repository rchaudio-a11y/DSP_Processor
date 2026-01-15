Imports DSP_Processor.Models
Imports DSP_Processor.UI

Namespace UI.TabPanels

    ''' <summary>
    ''' Input settings tab panel for configuring input volume and meter display parameters
    ''' </summary>
    Public Class InputTabPanel
        Inherits UserControl
        
        ' Group 1: Input Volume
        Private grpInputVolume As GroupBox
        Private trackInputVolume As TrackBar
        Private lblInputVolumeValue As Label
        Private chkLinkToPlayback As CheckBox
        
        ' Group 2: Peak Behavior
        Private grpPeakBehavior As GroupBox
        Private lblPeakHold As Label
        Private cmbPeakHold As ComboBox
        Private lblPeakDecay As Label
        Private cmbPeakDecay As ComboBox
        
        ' Group 3: RMS & Ballistics
        Private grpMeterBehavior As GroupBox
        Private lblRmsWindow As Label
        Private cmbRmsWindow As ComboBox
        Private lblAttack As Label
        Private cmbAttack As ComboBox
        Private lblRelease As Label
        Private cmbRelease As ComboBox
        
        ' Group 4: Clipping
        Private grpClipping As GroupBox
        Private lblClipThreshold As Label
        Private cmbClipThreshold As ComboBox
        Private lblClipHold As Label
        Private cmbClipHold As ComboBox
        
        ' Group 5: Presets
        Private grpPresets As GroupBox
        Private btnFastResponse As Button
        Private btnSlowResponse As Button
        Private btnBroadcast As Button
        Private btnReset As Button
        
        Public Event SettingsChanged As EventHandler(Of MeterSettings)
        
        Private suppressEvents As Boolean = False
        
        Public Sub New()
            InitializeComponent()
        End Sub
        
        Private Sub InitializeComponent()
            ' Create groups
            CreateInputVolumeGroup()
            CreatePeakBehaviorGroup()
            CreateMeterBehaviorGroup()
            CreateClippingGroup()
            CreatePresetsGroup()
            
            ' Layout
            LayoutControls()
            
            ' Populate
            PopulateControls()
            
            ' Apply theme
            DarkTheme.ApplyToControl(Me)
        End Sub
        
        Private Sub CreateInputVolumeGroup()
            grpInputVolume = New GroupBox() With {
                .Text = "Input Volume Control",
                .Location = New Point(10, 10),
                .Size = New Size(320, 120)
            }
            
            Dim lblInputVolume = New Label() With {
                .Text = "Input Volume:",
                .Location = New Point(10, 25),
                .Size = New Size(100, 20)
            }
            
            trackInputVolume = New TrackBar() With {
                .Location = New Point(10, 50),
                .Size = New Size(250, 45),
                .Minimum = 0,
                .Maximum = 200,
                .Value = 100,
                .TickFrequency = 25
            }
            AddHandler trackInputVolume.Scroll, AddressOf OnControlChanged
            
            lblInputVolumeValue = New Label() With {
                .Text = "100%",
                .Location = New Point(270, 55),
                .Size = New Size(40, 20)
            }
            
            chkLinkToPlayback = New CheckBox() With {
                .Text = "Link to Playback Volume",
                .Location = New Point(10, 90),
                .Size = New Size(200, 20)
            }
            AddHandler chkLinkToPlayback.CheckedChanged, AddressOf OnControlChanged
            
            grpInputVolume.Controls.AddRange({lblInputVolume, trackInputVolume, lblInputVolumeValue, chkLinkToPlayback})
            Me.Controls.Add(grpInputVolume)
        End Sub
        
        Private Sub CreatePeakBehaviorGroup()
            grpPeakBehavior = New GroupBox() With {
                .Text = "Peak Behavior",
                .Location = New Point(10, 140),
                .Size = New Size(320, 100)
            }
            
            lblPeakHold = New Label() With {
                .Text = "Peak Hold Time:",
                .Location = New Point(10, 25),
                .Size = New Size(120, 20)
            }
            
            cmbPeakHold = New ComboBox() With {
                .Location = New Point(140, 23),
                .Size = New Size(160, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbPeakHold.SelectedIndexChanged, AddressOf OnControlChanged
            
            lblPeakDecay = New Label() With {
                .Text = "Peak Decay Rate:",
                .Location = New Point(10, 60),
                .Size = New Size(120, 20)
            }
            
            cmbPeakDecay = New ComboBox() With {
                .Location = New Point(140, 58),
                .Size = New Size(160, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbPeakDecay.SelectedIndexChanged, AddressOf OnControlChanged
            
            grpPeakBehavior.Controls.AddRange({lblPeakHold, cmbPeakHold, lblPeakDecay, cmbPeakDecay})
            Me.Controls.Add(grpPeakBehavior)
        End Sub
        
        Private Sub CreateMeterBehaviorGroup()
            grpMeterBehavior = New GroupBox() With {
                .Text = "Meter Behavior",
                .Location = New Point(10, 250),
                .Size = New Size(320, 150)
            }
            
            lblRmsWindow = New Label() With {
                .Text = "RMS Window:",
                .Location = New Point(10, 25),
                .Size = New Size(120, 20)
            }
            
            cmbRmsWindow = New ComboBox() With {
                .Location = New Point(140, 23),
                .Size = New Size(160, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbRmsWindow.SelectedIndexChanged, AddressOf OnControlChanged
            
            lblAttack = New Label() With {
                .Text = "Attack Time:",
                .Location = New Point(10, 60),
                .Size = New Size(120, 20)
            }
            
            cmbAttack = New ComboBox() With {
                .Location = New Point(140, 58),
                .Size = New Size(160, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbAttack.SelectedIndexChanged, AddressOf OnControlChanged
            
            lblRelease = New Label() With {
                .Text = "Release Time:",
                .Location = New Point(10, 95),
                .Size = New Size(120, 20)
            }
            
            cmbRelease = New ComboBox() With {
                .Location = New Point(140, 93),
                .Size = New Size(160, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbRelease.SelectedIndexChanged, AddressOf OnControlChanged
            
            grpMeterBehavior.Controls.AddRange({lblRmsWindow, cmbRmsWindow, lblAttack, cmbAttack, lblRelease, cmbRelease})
            Me.Controls.Add(grpMeterBehavior)
        End Sub
        
        Private Sub CreateClippingGroup()
            grpClipping = New GroupBox() With {
                .Text = "Clipping Detection",
                .Location = New Point(10, 410),
                .Size = New Size(320, 100)
            }
            
            lblClipThreshold = New Label() With {
                .Text = "Clip Threshold:",
                .Location = New Point(10, 25),
                .Size = New Size(120, 20)
            }
            
            cmbClipThreshold = New ComboBox() With {
                .Location = New Point(140, 23),
                .Size = New Size(160, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbClipThreshold.SelectedIndexChanged, AddressOf OnControlChanged
            
            lblClipHold = New Label() With {
                .Text = "Clip Hold Time:",
                .Location = New Point(10, 60),
                .Size = New Size(120, 20)
            }
            
            cmbClipHold = New ComboBox() With {
                .Location = New Point(140, 58),
                .Size = New Size(160, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbClipHold.SelectedIndexChanged, AddressOf OnControlChanged
            
            grpClipping.Controls.AddRange({lblClipThreshold, cmbClipThreshold, lblClipHold, cmbClipHold})
            Me.Controls.Add(grpClipping)
        End Sub
        
        Private Sub CreatePresetsGroup()
            grpPresets = New GroupBox() With {
                .Text = "Presets",
                .Location = New Point(10, 520),
                .Size = New Size(320, 100)
            }
            
            btnFastResponse = New Button() With {
                .Text = "Fast Response",
                .Location = New Point(10, 25),
                .Size = New Size(145, 30)
            }
            AddHandler btnFastResponse.Click, AddressOf btnFastResponse_Click
            
            btnSlowResponse = New Button() With {
                .Text = "Slow Response",
                .Location = New Point(165, 25),
                .Size = New Size(145, 30)
            }
            AddHandler btnSlowResponse.Click, AddressOf btnSlowResponse_Click
            
            btnBroadcast = New Button() With {
                .Text = "Broadcast (BBC)",
                .Location = New Point(10, 60),
                .Size = New Size(145, 30)
            }
            AddHandler btnBroadcast.Click, AddressOf btnBroadcast_Click
            
            btnReset = New Button() With {
                .Text = "Reset to Defaults",
                .Location = New Point(165, 60),
                .Size = New Size(145, 30)
            }
            AddHandler btnReset.Click, AddressOf btnReset_Click
            
            grpPresets.Controls.AddRange({btnFastResponse, btnSlowResponse, btnBroadcast, btnReset})
            Me.Controls.Add(grpPresets)
        End Sub
        
        Private Sub LayoutControls()
            Me.AutoScroll = True
            Me.BackColor = Color.FromArgb(45, 45, 48)
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
            
            ' Input Volume
            settings.InputVolumePercent = trackInputVolume.Value
            settings.LinkToPlaybackVolume = chkLinkToPlayback.Checked
            
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
                ' Input Volume
                trackInputVolume.Value = Math.Max(0, Math.Min(200, settings.InputVolumePercent))
                lblInputVolumeValue.Text = $"{trackInputVolume.Value}%"
                chkLinkToPlayback.Checked = settings.LinkToPlaybackVolume
                
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
        
        ' Helper methods
        Private Function ParseMilliseconds(value As String) As Integer
            If String.IsNullOrEmpty(value) Then Return 500
            
            If value.Contains("Infinite") Then Return Integer.MaxValue
            If value.Contains("instant") Then Return 0
            
            Dim numStr = value.Replace("ms", "").Trim()
            Dim result As Integer
            If Integer.TryParse(numStr, result) Then
                Return result
            End If
            Return 500
        End Function
        
        Private Function ParseDecayRate(value As String) As Single
            If String.IsNullOrEmpty(value) Then Return 3.0F
            
            If value.Contains("Instant") Then Return 999.0F
            
            Dim numStr = value.Replace("dB/s", "").Trim()
            Dim result As Single
            If Single.TryParse(numStr, result) Then
                Return result
            End If
            Return 3.0F
        End Function
        
        Private Function ParseDbValue(value As String) As Single
            If String.IsNullOrEmpty(value) Then Return -0.1F
            
            Dim numStr = value.Replace("dB", "").Trim()
            Dim result As Single
            If Single.TryParse(numStr, result) Then
                Return result
            End If
            Return -0.1F
        End Function
        
        Private Sub SelectComboValue(cmb As ComboBox, value As Integer, suffix As String)
            Dim target As String
            If value = Integer.MaxValue Then
                target = "Infinite"
            ElseIf value = 0 AndAlso suffix = "ms" Then
                target = "0ms (instant)"
            Else
                target = $"{value}{suffix}"
            End If
            
            For i = 0 To cmb.Items.Count - 1
                If cmb.Items(i).ToString().StartsWith(target) Then
                    cmb.SelectedIndex = i
                    Return
                End If
            Next
        End Sub
        
        Private Sub SelectComboValue(cmb As ComboBox, value As Single, suffix As String)
            Dim target As String
            If value >= 999.0F Then
                target = "Instant"
            Else
                target = $"{value:F0}{suffix}"
            End If
            
            For i = 0 To cmb.Items.Count - 1
                If cmb.Items(i).ToString().StartsWith(target) Then
                    cmb.SelectedIndex = i
                    Return
                End If
            Next
        End Sub
        
        ' Event handlers
        Private Sub OnControlChanged(sender As Object, e As EventArgs)
            If suppressEvents Then Return
            
            ' Update volume label
            If sender Is trackInputVolume Then
                lblInputVolumeValue.Text = $"{trackInputVolume.Value}%"
            End If
            
            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub
        
        Private Sub btnFastResponse_Click(sender As Object, e As EventArgs)
            LoadSettings(MeterSettings.FastResponsePreset())
            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub
        
        Private Sub btnSlowResponse_Click(sender As Object, e As EventArgs)
            LoadSettings(MeterSettings.SlowResponsePreset())
            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub
        
        Private Sub btnBroadcast_Click(sender As Object, e As EventArgs)
            LoadSettings(MeterSettings.BroadcastPreset())
            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub
        
        Private Sub btnReset_Click(sender As Object, e As EventArgs)
            LoadSettings(New MeterSettings())
            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub
        
    End Class

End Namespace
