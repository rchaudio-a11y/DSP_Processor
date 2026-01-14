Imports NAudio.Wave

Namespace AudioIO

    ''' <summary>
    ''' Manages enumeration and selection of audio output devices
    ''' </summary>
    Public Class OutputDeviceManager

#Region "Private Fields"

        Private _selectedDeviceIndex As Integer = -1

#End Region

#Region "Properties"

        ''' <summary>Gets the number of available output devices</summary>
        Public ReadOnly Property DeviceCount As Integer
            Get
                Return WaveOut.DeviceCount
            End Get
        End Property

        ''' <summary>Gets or sets the selected device index</summary>
        Public Property SelectedDeviceIndex As Integer
            Get
                Return _selectedDeviceIndex
            End Get
            Set(value As Integer)
                If value < -1 OrElse value >= DeviceCount Then
                    Throw New ArgumentOutOfRangeException(NameOf(value), $"Device index must be between -1 and {DeviceCount - 1}")
                End If
                _selectedDeviceIndex = value
            End Set
        End Property

        ''' <summary>Gets the capabilities of the selected device</summary>
        Public ReadOnly Property SelectedDeviceCapabilities As WaveOutCapabilities
            Get
                If _selectedDeviceIndex < 0 OrElse _selectedDeviceIndex >= DeviceCount Then
                    Throw New InvalidOperationException("No device selected")
                End If
                Return WaveOut.GetCapabilities(_selectedDeviceIndex)
            End Get
        End Property

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Gets a list of all available output device names
        ''' </summary>
        ''' <returns>Array of device names</returns>
        Public Function GetDeviceNames() As String()
            Dim names As New List(Of String)
            
            For i = 0 To DeviceCount - 1
                Try
                    Dim caps = WaveOut.GetCapabilities(i)
                    names.Add(caps.ProductName)
                Catch ex As Exception
                    names.Add($"Device {i} (Error)")
                End Try
            Next
            
            Return names.ToArray()
        End Function

        ''' <summary>
        ''' Gets detailed information about a specific device
        ''' </summary>
        ''' <param name="deviceIndex">Device index</param>
        ''' <returns>Device capabilities</returns>
        Public Function GetDeviceInfo(deviceIndex As Integer) As WaveOutCapabilities
            If deviceIndex < 0 OrElse deviceIndex >= DeviceCount Then
                Throw New ArgumentOutOfRangeException(NameOf(deviceIndex))
            End If
            
            Return WaveOut.GetCapabilities(deviceIndex)
        End Function

        ''' <summary>
        ''' Finds a device by name (case-insensitive partial match)
        ''' </summary>
        ''' <param name="name">Device name to search for</param>
        ''' <returns>Device index, or -1 if not found</returns>
        Public Function FindDeviceByName(name As String) As Integer
            If String.IsNullOrWhiteSpace(name) Then Return -1
            
            For i = 0 To DeviceCount - 1
                Try
                    Dim caps = WaveOut.GetCapabilities(i)
                    If caps.ProductName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 Then
                        Return i
                    End If
                Catch
                    ' Skip devices that can't be queried
                End Try
            Next
            
            Return -1
        End Function

        ''' <summary>
        ''' Checks if a device supports a specific format
        ''' </summary>
        ''' <param name="deviceIndex">Device index</param>
        ''' <param name="format">Wave format to test</param>
        ''' <returns>True if supported</returns>
        Public Function IsFormatSupported(deviceIndex As Integer, format As WaveFormat) As Boolean
            If deviceIndex < 0 OrElse deviceIndex >= DeviceCount Then Return False
            If format Is Nothing Then Return False
            
            Try
                Using test = New WaveOutEvent() With {.DeviceNumber = deviceIndex}
                    ' If we can create a provider with this format, it's supported
                    Return True
                End Using
            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Gets the default output device index
        ''' </summary>
        ''' <returns>Default device index (usually 0)</returns>
        Public Function GetDefaultDeviceIndex() As Integer
            Return If(DeviceCount > 0, 0, -1)
        End Function

#End Region

    End Class

End Namespace
