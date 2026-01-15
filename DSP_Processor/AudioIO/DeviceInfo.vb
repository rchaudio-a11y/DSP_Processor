Namespace AudioIO

    ''' <summary>
    ''' Contains metadata and capabilities for an audio input device.
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 1, Task 1.1
    ''' Purpose: Provide unified device information across different driver types
    ''' </remarks>
    Public Class DeviceInfo

#Region "Properties"

        ''' <summary>User-friendly device name</summary>
        Public Property Name As String

        ''' <summary>Unique device identifier (stable across reboots for WASAPI/ASIO)</summary>
        Public Property Id As String

        ''' <summary>Audio driver type associated with this device</summary>
        Public Property DriverType As DriverType

        ''' <summary>Maximum number of input channels supported</summary>
        Public Property MaxChannels As Integer

        ''' <summary>Supported sample rates (Hz)</summary>
        Public Property SupportedSampleRates As Integer()

        ''' <summary>Supported bit depths (bits per sample)</summary>
        Public Property SupportedBitDepths As Integer()

        ''' <summary>Is this the system default input device?</summary>
        Public Property IsDefault As Boolean

        ''' <summary>Is the device currently connected and available?</summary>
        Public Property IsAvailable As Boolean

        ''' <summary>Device index (for WaveIn driver type, may change on reboot)</summary>
        Public Property DeviceIndex As Integer = -1

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Creates a new DeviceInfo instance
        ''' </summary>
        Public Sub New()
            ' Initialize with sensible defaults
            Name = "Unknown Device"
            Id = String.Empty
            DriverType = AudioIO.DriverType.WaveIn
            MaxChannels = 2
            SupportedSampleRates = {44100, 48000}
            SupportedBitDepths = {16, 24}
            IsDefault = False
            IsAvailable = True
        End Sub

        ''' <summary>
        ''' Creates a new DeviceInfo instance with basic properties
        ''' </summary>
        Public Sub New(name As String, id As String, driverType As DriverType)
            Me.New()
            Me.Name = name
            Me.Id = id
            Me.DriverType = driverType
        End Sub

#End Region

#Region "Methods"

        ''' <summary>
        ''' Checks if a specific sample rate is supported by this device
        ''' </summary>
        Public Function SupportsSampleRate(sampleRate As Integer) As Boolean
            If SupportedSampleRates Is Nothing Then Return False
            Return SupportedSampleRates.Contains(sampleRate)
        End Function

        ''' <summary>
        ''' Checks if a specific bit depth is supported by this device
        ''' </summary>
        Public Function SupportsBitDepth(bitDepth As Integer) As Boolean
            If SupportedBitDepths Is Nothing Then Return False
            Return SupportedBitDepths.Contains(bitDepth)
        End Function

        ''' <summary>
        ''' Returns a user-friendly string representation of this device
        ''' </summary>
        Public Overrides Function ToString() As String
            Dim status = If(IsAvailable, "Available", "Unavailable")
            Dim defaultStr = If(IsDefault, " [Default]", "")
            Return $"{Name} ({DriverType}){defaultStr} - {status}"
        End Function

        ''' <summary>
        ''' Creates a copy of this DeviceInfo
        ''' </summary>
        Public Function Clone() As DeviceInfo
            Return New DeviceInfo() With {
                .Name = Me.Name,
                .Id = Me.Id,
                .DriverType = Me.DriverType,
                .MaxChannels = Me.MaxChannels,
                .SupportedSampleRates = If(Me.SupportedSampleRates IsNot Nothing,
                                          CType(Me.SupportedSampleRates.Clone(), Integer()),
                                          Nothing),
                .SupportedBitDepths = If(Me.SupportedBitDepths IsNot Nothing,
                                        CType(Me.SupportedBitDepths.Clone(), Integer()),
                                        Nothing),
                .IsDefault = Me.IsDefault,
                .IsAvailable = Me.IsAvailable,
                .DeviceIndex = Me.DeviceIndex
            }
        End Function

#End Region

    End Class

End Namespace
