Imports DSP_Processor.Models
Imports DSP_Processor.UI
Imports DSP_Processor.Managers
Imports NAudio.Wave

Namespace UI.TabPanels

    ''' <summary>
    ''' Audio settings panel - device, sample rate, bit depth, channels, buffer size
    ''' Replaces scattered combo boxes in MainForm with single UserControl
    ''' </summary>
    Public Class AudioSettingsPanel
        Inherits UserControl

#Region "Controls"

        Private grpAudioSettings As GroupBox
        Private lblInputDevice As Label
        Private cmbInputDevices As ComboBox
        Private lblSampleRate As Label
        Private cmbSampleRates As ComboBox
        Private lblBitDepth As Label
        Private cmbBitDepths As ComboBox
        Private lblChannelMode As Label
        Private cmbChannelMode As ComboBox
        Private lblBufferSize As Label
        Private cmbBufferSize As ComboBox

#End Region

#Region "Events"

        ''' <summary>Raised when any audio setting changes</summary>
        Public Event SettingsChanged As EventHandler(Of Managers.AudioDeviceSettings)

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
            ' Create group box
            grpAudioSettings = New GroupBox() With {
                .Text = "Audio Device Settings",
                .Location = New Point(10, 10),
                .Size = New Size(320, 260),
                .BackColor = Color.FromArgb(45, 45, 48),
                .ForeColor = Color.White
            }

            ' Input Device
            lblInputDevice = New Label() With {
                .Text = "Input Device:",
                .Location = New Point(10, 25),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbInputDevices = New ComboBox() With {
                .Location = New Point(10, 48),
                .Size = New Size(290, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbInputDevices.SelectedIndexChanged, AddressOf OnControlChanged

            ' Sample Rate
            lblSampleRate = New Label() With {
                .Text = "Sample Rate:",
                .Location = New Point(10, 83),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbSampleRates = New ComboBox() With {
                .Location = New Point(10, 106),
                .Size = New Size(140, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbSampleRates.SelectedIndexChanged, AddressOf OnControlChanged

            ' Bit Depth
            lblBitDepth = New Label() With {
                .Text = "Bit Depth:",
                .Location = New Point(160, 83),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbBitDepths = New ComboBox() With {
                .Location = New Point(160, 106),
                .Size = New Size(140, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbBitDepths.SelectedIndexChanged, AddressOf OnControlChanged

            ' Channel Mode
            lblChannelMode = New Label() With {
                .Text = "Channel Mode:",
                .Location = New Point(10, 141),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbChannelMode = New ComboBox() With {
                .Location = New Point(10, 164),
                .Size = New Size(140, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbChannelMode.SelectedIndexChanged, AddressOf OnControlChanged

            ' Buffer Size
            lblBufferSize = New Label() With {
                .Text = "Buffer Size:",
                .Location = New Point(160, 141),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbBufferSize = New ComboBox() With {
                .Location = New Point(160, 164),
                .Size = New Size(140, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbBufferSize.SelectedIndexChanged, AddressOf OnControlChanged

            ' Add controls to group
            grpAudioSettings.Controls.AddRange({
                lblInputDevice, cmbInputDevices,
                lblSampleRate, cmbSampleRates,
                lblBitDepth, cmbBitDepths,
                lblChannelMode, cmbChannelMode,
                lblBufferSize, cmbBufferSize
            })

            ' Add group to panel
            Me.Controls.Add(grpAudioSettings)

            ' Set panel properties
            Me.BackColor = Color.FromArgb(45, 45, 48)
            Me.AutoScroll = True

            ' Populate dropdowns
            PopulateControls()
        End Sub

        Private Sub PopulateControls()
            suppressEvents = True

            Try
                ' Input Devices
                cmbInputDevices.Items.Clear()
                For i = 0 To WaveIn.DeviceCount - 1
                    Dim caps = WaveIn.GetCapabilities(i)
                    cmbInputDevices.Items.Add($"{i}: {caps.ProductName}")
                Next
                If cmbInputDevices.Items.Count > 0 Then
                    cmbInputDevices.SelectedIndex = 0
                End If

                ' Sample Rates
                cmbSampleRates.Items.Clear()
                Dim rates = New Integer() {8000, 11025, 16000, 22050, 32000, 44100, 48000, 96000}
                For Each sampleRate As Integer In rates
                    cmbSampleRates.Items.Add(sampleRate.ToString())
                Next
                cmbSampleRates.SelectedIndex = 5 ' Default to 44100 Hz

                ' Bit Depths
                cmbBitDepths.Items.Clear()
                Dim depths = New Integer() {8, 16, 24, 32}
                For Each depth As Integer In depths
                    cmbBitDepths.Items.Add(depth.ToString())
                Next
                cmbBitDepths.SelectedIndex = 1 ' Default to 16-bit

                ' Channel Modes
                cmbChannelMode.Items.Clear()
                cmbChannelMode.Items.AddRange({"Mono (1)", "Stereo (2)"})
                cmbChannelMode.SelectedIndex = 1 ' Default to Stereo

                ' Buffer Sizes (milliseconds)
                cmbBufferSize.Items.Clear()
                Dim bufferSizes = New Integer() {10, 20, 30, 50, 100, 200}
                For Each bufferSize As Integer In bufferSizes
                    cmbBufferSize.Items.Add(bufferSize.ToString())
                Next
                cmbBufferSize.SelectedIndex = 1 ' Default to 20ms

            Finally
                suppressEvents = False
            End Try
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>Get current settings from UI</summary>
        Public Function GetSettings() As Managers.AudioDeviceSettings
            Dim settings = New Managers.AudioDeviceSettings()

            settings.InputDeviceIndex = cmbInputDevices.SelectedIndex

            If cmbSampleRates.SelectedItem IsNot Nothing Then
                settings.SampleRate = Integer.Parse(cmbSampleRates.SelectedItem.ToString())
            End If

            If cmbBitDepths.SelectedItem IsNot Nothing Then
                settings.BitDepth = Integer.Parse(cmbBitDepths.SelectedItem.ToString())
            End If

            settings.Channels = If(cmbChannelMode.SelectedIndex = 0, 1, 2)

            If cmbBufferSize.SelectedItem IsNot Nothing Then
                settings.BufferMilliseconds = Integer.Parse(cmbBufferSize.SelectedItem.ToString())
            End If

            Return settings
        End Function

        ''' <summary>Load settings into UI</summary>
        Public Sub LoadSettings(settings As Managers.AudioDeviceSettings)
            suppressEvents = True

            Try
                ' Input Device
                If settings.InputDeviceIndex >= 0 AndAlso settings.InputDeviceIndex < cmbInputDevices.Items.Count Then
                    cmbInputDevices.SelectedIndex = settings.InputDeviceIndex
                End If

                ' Sample Rate
                Dim rateIndex = Array.IndexOf({8000, 11025, 16000, 22050, 32000, 44100, 48000, 96000}, settings.SampleRate)
                If rateIndex >= 0 Then cmbSampleRates.SelectedIndex = rateIndex

                ' Bit Depth
                Dim depthIndex = Array.IndexOf({8, 16, 24, 32}, settings.BitDepth)
                If depthIndex >= 0 Then cmbBitDepths.SelectedIndex = depthIndex

                ' Channels
                cmbChannelMode.SelectedIndex = If(settings.Channels = 1, 0, 1)

                ' Buffer Size
                Dim bufferIndex = Array.IndexOf({10, 20, 30, 50, 100, 200}, settings.BufferMilliseconds)
                If bufferIndex >= 0 Then cmbBufferSize.SelectedIndex = bufferIndex

            Finally
                suppressEvents = False
            End Try
        End Sub

        ''' <summary>Refresh device list (call when devices change)</summary>
        Public Sub RefreshDevices()
            Dim currentIndex = cmbInputDevices.SelectedIndex

            suppressEvents = True
            Try
                cmbInputDevices.Items.Clear()
                For i = 0 To WaveIn.DeviceCount - 1
                    Dim caps = WaveIn.GetCapabilities(i)
                    cmbInputDevices.Items.Add($"{i}: {caps.ProductName}")
                Next

                ' Restore selection if valid
                If currentIndex >= 0 AndAlso currentIndex < cmbInputDevices.Items.Count Then
                    cmbInputDevices.SelectedIndex = currentIndex
                ElseIf cmbInputDevices.Items.Count > 0 Then
                    cmbInputDevices.SelectedIndex = 0
                End If
            Finally
                suppressEvents = False
            End Try
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub OnControlChanged(sender As Object, e As EventArgs)
            If suppressEvents Then Return

            ' Raise single event with all settings
            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub

#End Region

    End Class

End Namespace
