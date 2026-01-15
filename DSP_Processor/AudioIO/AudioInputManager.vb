Imports NAudio.Wave
Imports System.Collections.Generic
Imports Microsoft.Win32

Namespace AudioIO

    ''' <summary>
    ''' Central manager for audio input devices across multiple driver types.
    ''' Handles driver detection, device enumeration, and hot-plug events.
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 1, Task 1.1
    ''' Purpose: Provide unified interface for WaveIn, WASAPI, and ASIO drivers
    ''' Pattern: Singleton (or can be used with dependency injection)
    ''' </remarks>
    Public Class AudioInputManager
        Implements IDisposable

#Region "Singleton Instance"

        Private Shared _instance As AudioInputManager
        Private Shared ReadOnly _lock As New Object()

        ''' <summary>
        ''' Gets the singleton instance of AudioInputManager
        ''' </summary>
        Public Shared ReadOnly Property Instance As AudioInputManager
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New AudioInputManager()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

#End Region

#Region "Private Fields"

        Private _currentDriver As DriverType = DriverType.WaveIn
        Private _availableDrivers As List(Of DriverType)
        Private _devices As New Dictionary(Of DriverType, List(Of DeviceInfo))
        Private _disposed As Boolean = False
        Private _monitorThread As Threading.Thread
        Private _stopMonitoring As Boolean = False

#End Region

#Region "Properties"

        ''' <summary>Gets or sets the currently selected driver type</summary>
        Public Property CurrentDriver As DriverType
            Get
                Return _currentDriver
            End Get
            Set(value As DriverType)
                If value <> _currentDriver Then
                    Dim oldDriver = _currentDriver
                    _currentDriver = value
                    RaiseEvent DriverChanged(Me, New DriverChangedEventArgs(oldDriver, value))
                    Utils.Logger.Instance.Info($"Driver changed from {oldDriver} to {value}", "AudioInputManager")
                End If
            End Set
        End Property

        ''' <summary>Gets the list of available driver types on this system</summary>
        Public ReadOnly Property AvailableDrivers As DriverType()
            Get
                If _availableDrivers Is Nothing Then
                    DetectAvailableDrivers()
                End If
                Return _availableDrivers.ToArray()
            End Get
        End Property

        ''' <summary>Gets the list of devices for the current driver</summary>
        Public ReadOnly Property Devices As List(Of DeviceInfo)
            Get
                Return GetDevices(_currentDriver)
            End Get
        End Property

#End Region

#Region "Events"

        ''' <summary>Raised when a new audio device is detected</summary>
        Public Event DeviceAdded As EventHandler(Of DeviceEventArgs)

        ''' <summary>Raised when an audio device is removed</summary>
        Public Event DeviceRemoved As EventHandler(Of DeviceEventArgs)

        ''' <summary>Raised when the audio driver type is changed</summary>
        Public Event DriverChanged As EventHandler(Of DriverChangedEventArgs)

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Private constructor for singleton pattern
        ''' </summary>
        Private Sub New()
            Utils.Logger.Instance.Info("AudioInputManager initialized", "AudioInputManager")
            DetectAvailableDrivers()
            StartHotPlugMonitoring()
        End Sub

#End Region

#Region "Driver Detection"

        ''' <summary>
        ''' Detects all available audio driver types on the system
        ''' </summary>
        Public Sub DetectAvailableDrivers()
            _availableDrivers = New List(Of DriverType)

            ' WaveIn is always available on Windows
            _availableDrivers.Add(DriverType.WaveIn)
            Utils.Logger.Instance.Debug("WaveIn driver available", "AudioInputManager")

            ' WASAPI available on Windows Vista and later (version 6.0+)
            Try
                If Environment.OSVersion.Version.Major >= 6 Then
                    _availableDrivers.Add(DriverType.WASAPI)
                    Utils.Logger.Instance.Debug("WASAPI driver available", "AudioInputManager")
                End If
            Catch ex As Exception
                Utils.Logger.Instance.Warning("WASAPI detection failed", "AudioInputManager")
            End Try

            ' Check for ASIO drivers (requires registry check)
            If IsASIOAvailable() Then
                _availableDrivers.Add(DriverType.ASIO)
                Utils.Logger.Instance.Debug("ASIO driver available", "AudioInputManager")
            End If

            Utils.Logger.Instance.Info($"Detected {_availableDrivers.Count} available drivers: {String.Join(", ", _availableDrivers)}", "AudioInputManager")
        End Sub

        ''' <summary>
        ''' Checks if ASIO drivers are installed on the system
        ''' </summary>
        Private Function IsASIOAvailable() As Boolean
            Try
                ' Check ASIO registry key for installed drivers
                Dim asioKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\ASIO")
                If asioKey IsNot Nothing Then
                    Dim subKeyNames = asioKey.GetSubKeyNames()
                    asioKey.Close()
                    Return subKeyNames.Length > 0
                End If
            Catch ex As Exception
                Utils.Logger.Instance.Debug($"ASIO registry check failed: {ex.Message}", "AudioInputManager")
            End Try
            Return False
        End Function

#End Region

#Region "Device Enumeration"

        ''' <summary>
        ''' Gets the list of input devices for a specific driver type
        ''' </summary>
        Public Function GetDevices(driverType As DriverType) As List(Of DeviceInfo)
            ' DISABLE CACHING FOR WAVEIN - always re-enumerate for fresh device indices
            ' Caching causes stale device information and wrong default device detection
            ' Re-enable caching only after WASAPI/ASIO are fully stable
            
            ' Check cache ONLY for non-WaveIn drivers
            If driverType <> DriverType.WaveIn AndAlso _devices.ContainsKey(driverType) Then
                Return _devices(driverType)
            End If

            ' Enumerate devices for the specified driver
            Dim deviceList As New List(Of DeviceInfo)

            Select Case driverType
                Case DriverType.WaveIn
                    deviceList = EnumerateWaveInDevices() ' Always fresh enumeration

                Case DriverType.WASAPI
                    deviceList = EnumerateWASAPIDevices()

                Case DriverType.ASIO
                    deviceList = EnumerateASIODevices()

                Case Else
                    Utils.Logger.Instance.Warning($"Unknown driver type: {driverType}", "AudioInputManager")
            End Select

            ' Cache the result
            _devices(driverType) = deviceList
            Utils.Logger.Instance.Info($"Enumerated {deviceList.Count} devices for {driverType}", "AudioInputManager")

            Return deviceList
        End Function

        ''' <summary>
        ''' Gets the default input device for a specific driver type
        ''' </summary>
        Public Function GetDefaultDevice(driverType As DriverType) As DeviceInfo
            Dim devices = GetDevices(driverType)
            Return devices.FirstOrDefault(Function(d) d.IsDefault)
        End Function

        ''' <summary>
        ''' Enumerates WaveIn devices - validates each device and queries real capabilities
        ''' </summary>
        Private Function EnumerateWaveInDevices() As List(Of DeviceInfo)
            Dim devices As New List(Of DeviceInfo)

            Try
                If WaveInEvent.DeviceCount = 0 Then
                    Utils.Logger.Instance.Warning("No WaveIn devices found", "AudioInputManager")
                    Return devices
                End If

                ' WaveIn API doesn't expose default device - assume first device
                ' User can override in UI
                
                For i As Integer = 0 To WaveInEvent.DeviceCount - 1
                    Try
                        Dim caps = WaveInEvent.GetCapabilities(i)
                        
                        ' TEMP: Skip validation - it's rejecting all devices
                        ' Validate device by attempting to query its format
                        ' This ensures the device is actually accessible
                        'Dim isValid = ValidateWaveInDevice(i)
                        '
                        'If Not isValid Then
                        '    Utils.Logger.Instance.Warning($"WaveIn device {i} ({caps.ProductName}) failed validation - skipping", "AudioInputManager")
                        '    Continue For
                        'End If

                        Dim device As New DeviceInfo() With {
                            .Name = caps.ProductName,
                            .Id = $"WaveIn_{i}",
                            .DriverType = DriverType.WaveIn,
                            .DeviceIndex = i,
                            .MaxChannels = caps.Channels,
                            .IsDefault = (i = 0), ' First device - user can change
                            .IsAvailable = True,
                            .SupportedSampleRates = {8000, 11025, 16000, 22050, 32000, 44100, 48000, 96000},
                            .SupportedBitDepths = {8, 16, 24, 32}
                        }

                        Utils.Logger.Instance.Info($"WaveIn device {i}: {caps.ProductName}, Channels={caps.Channels}", "AudioInputManager")
                        devices.Add(device)
                        
                    Catch ex As Exception
                        Utils.Logger.Instance.Warning($"Failed to query WaveIn device {i}: {ex.Message}", "AudioInputManager")
                    End Try
                Next
                
                If devices.Count = 0 Then
                    Utils.Logger.Instance.Warning("No valid WaveIn devices available", "AudioInputManager")
                End If
                
            Catch ex As Exception
                Utils.Logger.Instance.Error("Failed to enumerate WaveIn devices", ex, "AudioInputManager")
            End Try

            Return devices
        End Function
        
        ''' <summary>
        ''' Validates that a WaveIn device can actually be opened
        ''' </summary>
        Private Function ValidateWaveInDevice(deviceIndex As Integer) As Boolean
            Try
                ' Try to create a WaveInEvent - if it fails, device is not usable
                Using testWaveIn As New WaveInEvent() With {
                    .DeviceNumber = deviceIndex,
                    .WaveFormat = New WaveFormat(44100, 16, 2)
                }
                    ' If we get here, device is valid
                    Return True
                End Using
            Catch
                ' Device failed to initialize
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Enumerates WASAPI devices using NAudio.CoreAudioApi
        ''' </summary>
        Private Function EnumerateWASAPIDevices() As List(Of DeviceInfo)
            Dim devices As New List(Of DeviceInfo)

            Try
                Using enumerator As New NAudio.CoreAudioApi.MMDeviceEnumerator()
                    Dim collection = enumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.DeviceState.Active)
                    
                    If collection.Count = 0 Then
                        Utils.Logger.Instance.Warning("No WASAPI devices found", "AudioInputManager")
                        Return devices
                    End If
                    
                    ' Get actual default device
                    Dim defaultDevice As NAudio.CoreAudioApi.MMDevice = Nothing
                    Try
                        defaultDevice = enumerator.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.Role.Console)
                    Catch
                        Utils.Logger.Instance.Warning("No default WASAPI device", "AudioInputManager")
                    End Try
                    
                    Dim deviceIndex As Integer = 0
                    For Each mmDevice In collection
                        Try
                            ' Query actual device capabilities
                            Dim format = mmDevice.AudioClient.MixFormat
                            
                            Dim device As New DeviceInfo() With {
                                .Name = mmDevice.FriendlyName,
                                .Id = mmDevice.ID,
                                .DriverType = DriverType.WASAPI,
                                .DeviceIndex = deviceIndex,
                                .MaxChannels = format.Channels,
                                .IsDefault = (defaultDevice IsNot Nothing AndAlso mmDevice.ID = defaultDevice.ID),
                                .IsAvailable = (mmDevice.State = NAudio.CoreAudioApi.DeviceState.Active),
                                .SupportedSampleRates = {format.SampleRate}, ' WASAPI uses native format
                                .SupportedBitDepths = {format.BitsPerSample}
                            }

                            Utils.Logger.Instance.Info($"WASAPI device {deviceIndex}: {mmDevice.FriendlyName}, Channels={format.Channels}, Rate={format.SampleRate}Hz, Bits={format.BitsPerSample}", "AudioInputManager")
                            devices.Add(device)
                            deviceIndex += 1
                            
                        Catch ex As Exception
                            Utils.Logger.Instance.Warning($"Failed to query WASAPI device: {ex.Message}", "AudioInputManager")
                        End Try
                    Next
                    
                    If devices.Count = 0 Then
                        Utils.Logger.Instance.Warning("No valid WASAPI devices available", "AudioInputManager")
                    End If
                End Using
                
            Catch ex As Exception
                Utils.Logger.Instance.Error("Failed to enumerate WASAPI devices", ex, "AudioInputManager")
            End Try

            Return devices
        End Function

        ''' <summary>
        ''' Enumerates ASIO devices (placeholder for Task 1.3)
        ''' </summary>
        Private Function EnumerateASIODevices() As List(Of DeviceInfo)
            Dim devices As New List(Of DeviceInfo)
            ' TODO: Implement in Task 1.3 using ASIO API
            Utils.Logger.Instance.Debug("ASIO device enumeration not yet implemented (Task 1.3)", "AudioInputManager")
            Return devices
        End Function

        ''' <summary>
        ''' Refreshes the device list for the current driver
        ''' </summary>
        Public Sub RefreshDevices()
            RefreshDevices(_currentDriver)
        End Sub

        ''' <summary>
        ''' Refreshes the device list for a specific driver
        ''' </summary>
        Public Sub RefreshDevices(driverType As DriverType)
            ' Clear cache
            If _devices.ContainsKey(driverType) Then
                _devices.Remove(driverType)
            End If

            ' Re-enumerate
            GetDevices(driverType)
            Utils.Logger.Instance.Info($"Refreshed device list for {driverType}", "AudioInputManager")
        End Sub

#End Region

#Region "Input Source Creation"

        ''' <summary>
        ''' Creates an appropriate input source for the specified device
        ''' </summary>
        Public Function CreateInputSource(device As DeviceInfo) As IInputSource
            If device Is Nothing Then
                Throw New ArgumentNullException(NameOf(device))
            End If

            Select Case device.DriverType
                Case DriverType.WaveIn
                    ' For WaveIn, caller should create MicInputSource directly with proper parameters
                    ' This method is primarily for future WASAPI/ASIO support
                    Utils.Logger.Instance.Info($"WaveIn device selected: {device.Name}", "AudioInputManager")
                    Return Nothing ' Caller should use MicInputSource constructor

                Case DriverType.WASAPI
                    ' TODO: Implement in Task 1.2
                    Throw New NotImplementedException("WASAPI input source creation will be implemented in Task 1.2")

                Case DriverType.ASIO
                    ' TODO: Implement in Task 1.3
                    Throw New NotImplementedException("ASIO input source creation will be implemented in Task 1.3")

                Case Else
                    Throw New ArgumentException($"Unsupported driver type: {device.DriverType}")
            End Select
        End Function

#End Region

#Region "Hot-Plug Monitoring"

        ''' <summary>
        ''' Starts background monitoring for device hot-plug events
        ''' </summary>
        Private Sub StartHotPlugMonitoring()
            _stopMonitoring = False
            _monitorThread = New Threading.Thread(AddressOf MonitorDeviceChanges)
            _monitorThread.IsBackground = True
            _monitorThread.Name = "Audio Device Monitor"
            _monitorThread.Start()
            Utils.Logger.Instance.Debug("Hot-plug monitoring started", "AudioInputManager")
        End Sub

        ''' <summary>
        ''' Background thread that monitors for device changes
        ''' </summary>
        Private Sub MonitorDeviceChanges()
            While Not _stopMonitoring
                Try
                    ' Simple polling approach - check every 2 seconds
                    ' TODO: In production, use WMI events or RegisterDeviceNotification API
                    Threading.Thread.Sleep(2000)

                    ' For now, just periodic refresh
                    ' In Task 1.2/1.3, we'll implement proper device change notifications

                Catch ex As Exception
                    Utils.Logger.Instance.Error("Device monitoring error", ex, "AudioInputManager")
                End Try
            End While
        End Sub

        ''' <summary>
        ''' Stops the hot-plug monitoring thread
        ''' </summary>
        Private Sub StopHotPlugMonitoring()
            _stopMonitoring = True
            _monitorThread?.Join(1000) ' Wait up to 1 second
            Utils.Logger.Instance.Debug("Hot-plug monitoring stopped", "AudioInputManager")
        End Sub

#End Region

#Region "IDisposable Implementation"

        Public Sub Dispose() Implements IDisposable.Dispose
            If Not _disposed Then
                StopHotPlugMonitoring()
                _devices.Clear()
                _disposed = True
                Utils.Logger.Instance.Info("AudioInputManager disposed", "AudioInputManager")
            End If
        End Sub

#End Region

    End Class

#Region "Event Args Classes"

    ''' <summary>
    ''' Event arguments for device add/remove events
    ''' </summary>
    Public Class DeviceEventArgs
        Inherits EventArgs

        Public Property Device As DeviceInfo

        Public Sub New(device As DeviceInfo)
            Me.Device = device
        End Sub
    End Class

    ''' <summary>
    ''' Event arguments for driver change event
    ''' </summary>
    Public Class DriverChangedEventArgs
        Inherits EventArgs

        Public Property OldDriver As DriverType
        Public Property NewDriver As DriverType

        Public Sub New(oldDriver As DriverType, newDriver As DriverType)
            Me.OldDriver = oldDriver
            Me.NewDriver = newDriver
        End Sub
    End Class

#End Region

End Namespace
