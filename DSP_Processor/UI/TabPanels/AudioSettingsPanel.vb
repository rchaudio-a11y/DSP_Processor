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
        Private lblAudioDriver As Label
        Private cmbAudioDriver As ComboBox
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
                .Size = New Size(320, 320),
                .BackColor = Color.FromArgb(45, 45, 48),
                .ForeColor = Color.White
            }

            ' Audio Driver (NEW - Task 1.1)
            lblAudioDriver = New Label() With {
                .Text = "Audio Driver:",
                .Location = New Point(10, 25),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbAudioDriver = New ComboBox() With {
                .Location = New Point(10, 48),
                .Size = New Size(290, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbAudioDriver.SelectedIndexChanged, AddressOf OnDriverChanged

            ' Input Device (moved down)
            lblInputDevice = New Label() With {
                .Text = "Input Device:",
                .Location = New Point(10, 83),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbInputDevices = New ComboBox() With {
                .Location = New Point(10, 106),
                .Size = New Size(290, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbInputDevices.SelectedIndexChanged, AddressOf OnControlChanged

            ' Sample Rate (moved down)
            lblSampleRate = New Label() With {
                .Text = "Sample Rate:",
                .Location = New Point(10, 141),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbSampleRates = New ComboBox() With {
                .Location = New Point(10, 164),
                .Size = New Size(140, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbSampleRates.SelectedIndexChanged, AddressOf OnControlChanged

            ' Bit Depth (moved down)
            lblBitDepth = New Label() With {
                .Text = "Bit Depth:",
                .Location = New Point(160, 141),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbBitDepths = New ComboBox() With {
                .Location = New Point(160, 164),
                .Size = New Size(140, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbBitDepths.SelectedIndexChanged, AddressOf OnControlChanged

            ' Channel Mode (moved down)
            lblChannelMode = New Label() With {
                .Text = "Channel Mode:",
                .Location = New Point(10, 199),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbChannelMode = New ComboBox() With {
                .Location = New Point(10, 222),
                .Size = New Size(140, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbChannelMode.SelectedIndexChanged, AddressOf OnControlChanged

            ' Buffer Size (moved down)
            lblBufferSize = New Label() With {
                .Text = "Buffer Size:",
                .Location = New Point(160, 199),
                .Size = New Size(100, 20),
                .ForeColor = Color.White
            }

            cmbBufferSize = New ComboBox() With {
                .Location = New Point(160, 222),
                .Size = New Size(140, 25),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbBufferSize.SelectedIndexChanged, AddressOf OnControlChanged

            ' Add controls to group
            grpAudioSettings.Controls.AddRange({
                lblAudioDriver, cmbAudioDriver,
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
                ' Audio Drivers (NEW - Task 1.1)
                PopulateDrivers()

                ' Input Devices (now uses AudioInputManager)
                PopulateInputDevices()

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

                ' Buffer Sizes (milliseconds) - EXPANDED for fine-tuning
                cmbBufferSize.Items.Clear()
                Dim bufferSizes = New Integer() {10, 15, 20, 25, 30, 40, 50, 60, 75, 100, 150, 200}
                For Each bufferSize As Integer In bufferSizes
                    cmbBufferSize.Items.Add(bufferSize.ToString())
                Next
                cmbBufferSize.SelectedIndex = 2 ' Default to 20ms (was index 1, now index 2)

            Finally
                suppressEvents = False
            End Try
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>Get current settings from UI</summary>
        Public Function GetSettings() As Managers.AudioDeviceSettings
            Dim settings = New Managers.AudioDeviceSettings()

            ' Get selected driver type
            If cmbAudioDriver.SelectedIndex >= 0 Then
                Dim driverName = cmbAudioDriver.SelectedItem.ToString()
                If [Enum].TryParse(driverName, settings.DriverType) Then
                    ' Driver type set successfully
                End If
            End If

            ' Get ACTUAL device index from DeviceInfo, not combo box index
            If cmbInputDevices.SelectedIndex >= 0 Then
                Dim devices = AudioIO.AudioInputManager.Instance.GetDevices(settings.DriverType)
                If cmbInputDevices.SelectedIndex < devices.Count Then
                    ' Use the DeviceIndex property from DeviceInfo (the real hardware index)
                    settings.InputDeviceIndex = devices(cmbInputDevices.SelectedIndex).DeviceIndex
                    Utils.Logger.Instance.Debug($"Selected device: ComboIndex={cmbInputDevices.SelectedIndex}, ActualDeviceIndex={settings.InputDeviceIndex}, Name={devices(cmbInputDevices.SelectedIndex).Name}", "AudioSettingsPanel")
                Else
                    ' Fallback to combo index if out of range
                    settings.InputDeviceIndex = cmbInputDevices.SelectedIndex
                End If
            End If

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
                ' Driver Type
                Dim driverIndex = cmbAudioDriver.Items.IndexOf(settings.DriverType.ToString())
                If driverIndex >= 0 Then
                    cmbAudioDriver.SelectedIndex = driverIndex
                    ' This will trigger driver change and populate devices
                End If
                
                ' Input Device - find device by DeviceIndex, not combo index
                If settings.InputDeviceIndex >= 0 Then
                    Dim devices = AudioIO.AudioInputManager.Instance.GetDevices(settings.DriverType)
                    ' Find which combo box position has the device with matching DeviceIndex
                    Dim comboIndex = -1
                    For i = 0 To devices.Count - 1
                        If devices(i).DeviceIndex = settings.InputDeviceIndex Then
                            comboIndex = i
                            Exit For
                        End If
                    Next
                    
                    If comboIndex >= 0 AndAlso comboIndex < cmbInputDevices.Items.Count Then
                        cmbInputDevices.SelectedIndex = comboIndex
                        Utils.Logger.Instance.Debug($"Loaded device: SavedDeviceIndex={settings.InputDeviceIndex}, ComboIndex={comboIndex}, Name={devices(comboIndex).Name}", "AudioSettingsPanel")
                    Else
                        ' Device not found, default to first device
                        If cmbInputDevices.Items.Count > 0 Then
                            cmbInputDevices.SelectedIndex = 0
                            Utils.Logger.Instance.Warning($"Saved device index {settings.InputDeviceIndex} not found, defaulting to device 0", "AudioSettingsPanel")
                        End If
                    End If
                End If

                ' Sample Rate
                Dim rateIndex = Array.IndexOf({8000, 11025, 16000, 22050, 32000, 44100, 48000, 96000}, settings.SampleRate)
                If rateIndex >= 0 Then cmbSampleRates.SelectedIndex = rateIndex

                ' Bit Depth
                Dim depthIndex = Array.IndexOf({8, 16, 24, 32}, settings.BitDepth)
                If depthIndex >= 0 Then cmbBitDepths.SelectedIndex = depthIndex

                ' Channels
                cmbChannelMode.SelectedIndex = If(settings.Channels = 1, 0, 1)

                ' Buffer Size - EXPANDED options for fine-tuning
                Dim bufferIndex = Array.IndexOf({10, 15, 20, 25, 30, 40, 50, 60, 75, 100, 150, 200}, settings.BufferMilliseconds)
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
                ' Use AudioInputManager for driver-aware device refresh
                PopulateInputDevices()

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

        ''' <summary>Populates the audio driver dropdown (Task 1.1)</summary>
        Private Sub PopulateDrivers()
            cmbAudioDriver.Items.Clear()

            Dim availableDrivers = AudioIO.AudioInputManager.Instance.AvailableDrivers
            For Each driver In availableDrivers
                cmbAudioDriver.Items.Add(driver.ToString())
            Next

            ' Select first available driver (typically WaveIn)
            If cmbAudioDriver.Items.Count > 0 Then
                cmbAudioDriver.SelectedIndex = 0
            End If

            Utils.Logger.Instance.Debug($"Populated {cmbAudioDriver.Items.Count} audio drivers", "AudioSettingsPanel")
        End Sub

        ''' <summary>Populates the input device dropdown based on selected driver (Task 1.1)</summary>
        Private Sub PopulateInputDevices()
            cmbInputDevices.Items.Clear()

            Dim currentDriver = AudioIO.AudioInputManager.Instance.CurrentDriver
            Dim devices = AudioIO.AudioInputManager.Instance.GetDevices(currentDriver)

            For Each device In devices
                cmbInputDevices.Items.Add(device.Name)
            Next

            ' Select default device if available
            Dim defaultDevice = devices.FirstOrDefault(Function(d) d.IsDefault)
            If defaultDevice IsNot Nothing Then
                Dim index = devices.IndexOf(defaultDevice)
                If index >= 0 Then
                    cmbInputDevices.SelectedIndex = index
                End If
            ElseIf cmbInputDevices.Items.Count > 0 Then
                cmbInputDevices.SelectedIndex = 0
            End If

            Utils.Logger.Instance.Debug($"Populated {cmbInputDevices.Items.Count} input devices for {currentDriver}", "AudioSettingsPanel")
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub OnControlChanged(sender As Object, e As EventArgs)
            If suppressEvents Then Return

            ' Raise single event with all settings
            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub

        ''' <summary>Handles driver selection change (Task 1.1)</summary>
        Private Sub OnDriverChanged(sender As Object, e As EventArgs)
            If suppressEvents Then Return

            ' Get selected driver
            If cmbAudioDriver.SelectedIndex >= 0 Then
                Dim selectedDriver As AudioIO.DriverType
                If [Enum].TryParse(cmbAudioDriver.SelectedItem.ToString(), selectedDriver) Then
                    ' Update AudioInputManager
                    AudioIO.AudioInputManager.Instance.CurrentDriver = selectedDriver
                    Utils.Logger.Instance.Info($"Audio driver changed to: {selectedDriver}", "AudioSettingsPanel")

                    ' Load driver-specific default settings
                    suppressEvents = True
                    Try
                        Dim defaults = Managers.AudioDeviceSettings.GetDefaultsForDriver(selectedDriver)
                        
                        ' Apply defaults to UI controls
                        ' Sample Rate
                        Dim rateIndex = Array.IndexOf({8000, 11025, 16000, 22050, 32000, 44100, 48000, 96000}, defaults.SampleRate)
                        If rateIndex >= 0 Then cmbSampleRates.SelectedIndex = rateIndex
                        
                        ' Bit Depth
                        Dim depthIndex = Array.IndexOf({8, 16, 24, 32}, defaults.BitDepth)
                        If depthIndex >= 0 Then cmbBitDepths.SelectedIndex = depthIndex
                        
                        ' Channels
                        cmbChannelMode.SelectedIndex = If(defaults.Channels = 1, 0, 1)
                        
                        ' Buffer Size
                        Dim bufferIndex = Array.IndexOf({10, 20, 30, 50, 100, 200}, defaults.BufferMilliseconds)
                        If bufferIndex >= 0 Then cmbBufferSize.SelectedIndex = bufferIndex
                        
                        Utils.Logger.Instance.Info($"Loaded defaults for {selectedDriver}: {defaults.SampleRate}Hz/{defaults.BitDepth}bit/{defaults.BufferMilliseconds}ms", "AudioSettingsPanel")
                        
                        ' Refresh device list for new driver
                        PopulateInputDevices()
                    Finally
                        suppressEvents = False
                    End Try

                    ' Raise settings changed event
                    RaiseEvent SettingsChanged(Me, GetSettings())
                End If
            End If
        End Sub

#End Region

    End Class

End Namespace
