Imports DSP_Processor.Models
Imports DSP_Processor.UI
Imports DSP_Processor.Managers
Imports NAudio.Wave

Namespace UI.TabPanels

    ''' <summary>
    ''' Audio settings panel - device, sample rate, bit depth, channels, buffer size
    ''' NOW USES DESIGNER! All controls are in AudioSettingsPanel.Designer.vb
    ''' </summary>
    Partial Public Class AudioSettingsPanel
        Inherits UserControl

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

            ' Wire up events (controls already exist in Designer!)
            AddHandler cmbAudioDriver.SelectedIndexChanged, AddressOf OnDriverChanged
            AddHandler cmbInputDevices.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler cmbSampleRates.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler cmbBitDepths.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler cmbChannelMode.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler cmbBufferSize.SelectedIndexChanged, AddressOf OnControlChanged

            ' Populate dropdowns
            PopulateControls()
        End Sub

#End Region

#Region "Initialization"

        Private Sub PopulateControls()
            suppressEvents = True

            Try
                ' Audio Drivers
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

                ' Buffer Sizes (milliseconds)
                cmbBufferSize.Items.Clear()
                Dim bufferSizes = New Integer() {10, 15, 20, 25, 30, 40, 50, 60, 75, 100, 150, 200}
                For Each bufferSize As Integer In bufferSizes
                    cmbBufferSize.Items.Add(bufferSize.ToString())
                Next
                cmbBufferSize.SelectedIndex = 2 ' Default to 20ms

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
                    settings.InputDeviceIndex = devices(cmbInputDevices.SelectedIndex).DeviceIndex
                    Utils.Logger.Instance.Debug($"Selected device: ComboIndex={cmbInputDevices.SelectedIndex}, ActualDeviceIndex={settings.InputDeviceIndex}, Name={devices(cmbInputDevices.SelectedIndex).Name}", "AudioSettingsPanel")
                Else
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
                End If

                ' Input Device - find device by DeviceIndex
                If settings.InputDeviceIndex >= 0 Then
                    Dim devices = AudioIO.AudioInputManager.Instance.GetDevices(settings.DriverType)
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

                ' Buffer Size
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
                PopulateInputDevices()

                If currentIndex >= 0 AndAlso currentIndex < cmbInputDevices.Items.Count Then
                    cmbInputDevices.SelectedIndex = currentIndex
                ElseIf cmbInputDevices.Items.Count > 0 Then
                    cmbInputDevices.SelectedIndex = 0
                End If
            Finally
                suppressEvents = False
            End Try
        End Sub

        ''' <summary>Populates the audio driver dropdown</summary>
        Private Sub PopulateDrivers()
            cmbAudioDriver.Items.Clear()

            Dim availableDrivers = AudioIO.AudioInputManager.Instance.AvailableDrivers
            For Each driver In availableDrivers
                cmbAudioDriver.Items.Add(driver.ToString())
            Next

            If cmbAudioDriver.Items.Count > 0 Then
                cmbAudioDriver.SelectedIndex = 0
            End If

            Utils.Logger.Instance.Debug($"Populated {cmbAudioDriver.Items.Count} audio drivers", "AudioSettingsPanel")
        End Sub

        ''' <summary>Populates the input device dropdown based on selected driver</summary>
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
            RaiseEvent SettingsChanged(Me, GetSettings())
        End Sub

        ''' <summary>Handles driver selection change</summary>
        Private Sub OnDriverChanged(sender As Object, e As EventArgs)
            If suppressEvents Then Return

            If cmbAudioDriver.SelectedIndex >= 0 Then
                Dim selectedDriver As AudioIO.DriverType
                If [Enum].TryParse(cmbAudioDriver.SelectedItem.ToString(), selectedDriver) Then
                    ' Phase 7: Request driver change through AudioDeviceSSM (state machine validation)
                    Dim targetState = ConvertDriverToState(selectedDriver)
                    Dim coordinator = StateCoordinator.Instance

                    If coordinator IsNot Nothing AndAlso coordinator.AudioDeviceSSM IsNot Nothing Then
                        Dim success = coordinator.AudioDeviceSSM.TransitionTo(targetState, $"User selected {selectedDriver} driver")

                        If Not success Then
                            ' Transition failed (validation rejected) - revert dropdown
                            Utils.Logger.Instance.Warning($"Driver change to {selectedDriver} rejected by state machine", "AudioSettingsPanel")
                            MessageBox.Show($"Cannot switch audio driver at this time.{vbCrLf}Please stop recording/playback first.",
                                          "Driver Change Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning)

                            ' Revert dropdown to current driver
                            suppressEvents = True
                            Try
                                Dim currentDriver = AudioIO.AudioInputManager.Instance.CurrentDriver
                                cmbAudioDriver.SelectedItem = currentDriver.ToString()
                            Finally
                                suppressEvents = False
                            End Try
                            Return
                        End If

                        Utils.Logger.Instance.Info($"Audio driver changed to: {selectedDriver} (via SSM)", "AudioSettingsPanel")
                    Else
                        ' Fallback: StateCoordinator not available (shouldn't happen in normal operation)
                        Utils.Logger.Instance.Warning("StateCoordinator not available - setting driver directly", "AudioSettingsPanel")
                        AudioIO.AudioInputManager.Instance.CurrentDriver = selectedDriver
                    End If

                    suppressEvents = True
                    Try
                        Dim defaults = Managers.AudioDeviceSettings.GetDefaultsForDriver(selectedDriver)

                        ' Apply defaults
                        Dim rateIndex = Array.IndexOf({8000, 11025, 16000, 22050, 32000, 44100, 48000, 96000}, defaults.SampleRate)
                        If rateIndex >= 0 Then cmbSampleRates.SelectedIndex = rateIndex

                        Dim depthIndex = Array.IndexOf({8, 16, 24, 32}, defaults.BitDepth)
                        If depthIndex >= 0 Then cmbBitDepths.SelectedIndex = depthIndex

                        cmbChannelMode.SelectedIndex = If(defaults.Channels = 1, 0, 1)

                        Dim bufferIndex = Array.IndexOf({10, 20, 30, 50, 100, 200}, defaults.BufferMilliseconds)
                        If bufferIndex >= 0 Then cmbBufferSize.SelectedIndex = bufferIndex

                        Utils.Logger.Instance.Info($"Loaded defaults for {selectedDriver}: {defaults.SampleRate}Hz/{defaults.BitDepth}bit/{defaults.BufferMilliseconds}ms", "AudioSettingsPanel")

                        PopulateInputDevices()
                    Finally
                        suppressEvents = False
                    End Try

                    RaiseEvent SettingsChanged(Me, GetSettings())
                End If
            End If
        End Sub

#End Region

#Region "Helper Methods"

        ''' <summary>
        ''' Converts DriverType to AudioDeviceState for SSM transitions
        ''' </summary>
        Private Function ConvertDriverToState(driver As AudioIO.DriverType) As State.AudioDeviceState
            Select Case driver
                Case AudioIO.DriverType.WASAPI
                    Return State.AudioDeviceState.WASAPI
                Case AudioIO.DriverType.ASIO
                    Return State.AudioDeviceState.ASIO
                Case AudioIO.DriverType.DirectSound
                    Return State.AudioDeviceState.DirectSound
                Case Else
                    Return State.AudioDeviceState.WASAPI ' Default fallback
            End Select
        End Function

#End Region

    End Class

End Namespace
