Imports System.IO
Imports DSP_Processor.Models
Imports DSP_Processor.Utils

Namespace Managers

    ''' <summary>
    ''' Centralized settings management - handles load/save/apply for all application settings
    ''' </summary>
    ''' <remarks>
    ''' Phase 0: MainForm Refactoring
    ''' Consolidates settings persistence that was scattered throughout MainForm
    ''' </remarks>
    Public Class SettingsManager

#Region "Events"

        ''' <summary>Raised when settings have been loaded from disk</summary>
        Public Event SettingsLoaded As EventHandler

        ''' <summary>Raised when settings have been saved to disk</summary>
        Public Event SettingsSaved As EventHandler

        ''' <summary>Raised when settings have been applied to subsystems</summary>
        Public Event SettingsApplied As EventHandler

#End Region

#Region "Fields"

        Private ReadOnly settingsFolder As String
        Private meterSettingsPath As String
        Private recordingOptionsPath As String
        Private spectrumSettingsPath As String
        Private audioSettingsPath As String

#End Region

#Region "Properties"

        ''' <summary>Audio level meter settings</summary>
        Public Property MeterSettings As MeterSettings

        ''' <summary>Recording mode and options</summary>
        Public Property RecordingOptions As RecordingOptions

        ''' <summary>Spectrum analyzer settings</summary>
        Public Property SpectrumSettings As SpectrumSettings

        ''' <summary>Audio device and format settings</summary>
        Public Property AudioSettings As AudioDeviceSettings

#End Region

#Region "Constructor"

        Public Sub New()
            ' Initialize settings folder path
            settingsFolder = Path.Combine(Application.StartupPath, "Settings")

            ' Create settings folder if it doesn't exist
            If Not Directory.Exists(settingsFolder) Then
                Directory.CreateDirectory(settingsFolder)
            End If

            ' Define settings file paths
            meterSettingsPath = Path.Combine(settingsFolder, "meter_settings.json")
            recordingOptionsPath = Path.Combine(settingsFolder, "recording_options.json")
            spectrumSettingsPath = Path.Combine(settingsFolder, "spectrum_settings.json")
            audioSettingsPath = Path.Combine(settingsFolder, "audio_settings.json")

            ' Initialize with defaults
            MeterSettings = New MeterSettings()
            RecordingOptions = New RecordingOptions()
            SpectrumSettings = New SpectrumSettings()
            AudioSettings = New AudioDeviceSettings()

            Logger.Instance.Info("SettingsManager initialized", "SettingsManager")
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>Load all settings from disk</summary>
        Public Sub LoadAll()
            Try
                Logger.Instance.Info("Loading all settings...", "SettingsManager")

                MeterSettings = LoadMeterSettings()
                RecordingOptions = LoadRecordingOptions()
                SpectrumSettings = LoadSpectrumSettings()
                AudioSettings = LoadAudioSettings()

                RaiseEvent SettingsLoaded(Me, EventArgs.Empty)
                Logger.Instance.Info("All settings loaded successfully", "SettingsManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to load settings", ex, "SettingsManager")
                ' Fall back to defaults (already initialized)
            End Try
        End Sub

        ''' <summary>Save all settings to disk</summary>
        Public Sub SaveAll()
            Try
                Logger.Instance.Info("Saving all settings...", "SettingsManager")

                SaveMeterSettings(MeterSettings)
                SaveRecordingOptions(RecordingOptions)
                SaveSpectrumSettings(SpectrumSettings)
                SaveAudioSettings(AudioSettings)

                RaiseEvent SettingsSaved(Me, EventArgs.Empty)
                Logger.Instance.Info("All settings saved successfully", "SettingsManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to save settings", ex, "SettingsManager")
            End Try
        End Sub

        ''' <summary>Reset all settings to defaults</summary>
        Public Sub ResetToDefaults()
            MeterSettings = New MeterSettings()
            RecordingOptions = New RecordingOptions()
            SpectrumSettings = New SpectrumSettings()
            AudioSettings = New AudioDeviceSettings()

            Logger.Instance.Info("Settings reset to defaults", "SettingsManager")
            SaveAll()
        End Sub

#End Region

#Region "Private Methods - Load"

        Private Function LoadMeterSettings() As MeterSettings
            If File.Exists(meterSettingsPath) Then
                Try
                    Dim json = File.ReadAllText(meterSettingsPath)
                    Dim settings = MeterSettings.FromJson(json)
                    Logger.Instance.Debug("Meter settings loaded", "SettingsManager")
                    Return settings
                Catch ex As Exception
                    Logger.Instance.Warning("Failed to load meter settings, using defaults", "SettingsManager")
                End Try
            End If
            Return New MeterSettings()
        End Function

        Private Function LoadRecordingOptions() As RecordingOptions
            If File.Exists(recordingOptionsPath) Then
                Try
                    Dim json = File.ReadAllText(recordingOptionsPath)
                    Dim options = RecordingOptions.FromJson(json)
                    Logger.Instance.Debug("Recording options loaded", "SettingsManager")
                    Return options
                Catch ex As Exception
                    Logger.Instance.Warning("Failed to load recording options, using defaults", "SettingsManager")
                End Try
            End If
            Return New RecordingOptions()
        End Function

        Private Function LoadSpectrumSettings() As SpectrumSettings
            If File.Exists(spectrumSettingsPath) Then
                Try
                    Dim json = File.ReadAllText(spectrumSettingsPath)
                    Dim settings = SpectrumSettings.FromJson(json)
                    Logger.Instance.Debug("Spectrum settings loaded", "SettingsManager")
                    Return settings
                Catch ex As Exception
                    Logger.Instance.Warning("Failed to load spectrum settings, using defaults", "SettingsManager")
                End Try
            End If
            Return New SpectrumSettings()
        End Function

        Private Function LoadAudioSettings() As AudioDeviceSettings
            If File.Exists(audioSettingsPath) Then
                Try
                    Dim json = File.ReadAllText(audioSettingsPath)
                    Dim settings = AudioDeviceSettings.FromJson(json)
                    Logger.Instance.Debug("Audio settings loaded", "SettingsManager")
                    Return settings
                Catch ex As Exception
                    Logger.Instance.Warning("Failed to load audio settings, using defaults", "SettingsManager")
                End Try
            End If
            Return New AudioDeviceSettings()
        End Function

#End Region

#Region "Private Methods - Save"

        Private Sub SaveMeterSettings(settings As MeterSettings)
            Try
                File.WriteAllText(meterSettingsPath, settings.ToJson())
                Logger.Instance.Debug("Meter settings saved", "SettingsManager")
            Catch ex As Exception
                Logger.Instance.Error("Failed to save meter settings", ex, "SettingsManager")
            End Try
        End Sub

        Private Sub SaveRecordingOptions(options As RecordingOptions)
            Try
                File.WriteAllText(recordingOptionsPath, options.ToJson())
                Logger.Instance.Debug("Recording options saved", "SettingsManager")
            Catch ex As Exception
                Logger.Instance.Error("Failed to save recording options", ex, "SettingsManager")
            End Try
        End Sub

        Private Sub SaveSpectrumSettings(settings As SpectrumSettings)
            Try
                File.WriteAllText(spectrumSettingsPath, settings.ToJson())
                Logger.Instance.Debug("Spectrum settings saved", "SettingsManager")
            Catch ex As Exception
                Logger.Instance.Error("Failed to save spectrum settings", ex, "SettingsManager")
            End Try
        End Sub

        Private Sub SaveAudioSettings(settings As AudioDeviceSettings)
            Try
                File.WriteAllText(audioSettingsPath, settings.ToJson())
                Logger.Instance.Debug("Audio settings saved", "SettingsManager")
            Catch ex As Exception
                Logger.Instance.Error("Failed to save audio settings", ex, "SettingsManager")
            End Try
        End Sub

#End Region

    End Class

#Region "Supporting Classes"

    ''' <summary>Audio device and format settings</summary>
    Public Class AudioDeviceSettings
        Public Property DriverType As AudioIO.DriverType = AudioIO.DriverType.WaveIn
        Public Property InputDeviceIndex As Integer = 0
        Public Property SampleRate As Integer = 44100
        Public Property BitDepth As Integer = 16
        Public Property Channels As Integer = 2
        Public Property BufferMilliseconds As Integer = 20

        ''' <summary>Serialize to JSON</summary>
        Public Function ToJson() As String
            Return Newtonsoft.Json.JsonConvert.SerializeObject(Me, Newtonsoft.Json.Formatting.Indented)
        End Function

        ''' <summary>Deserialize from JSON</summary>
        Public Shared Function FromJson(json As String) As AudioDeviceSettings
            Return Newtonsoft.Json.JsonConvert.DeserializeObject(Of AudioDeviceSettings)(json)
        End Function
        
        ''' <summary>Get driver-specific default settings</summary>
        Public Shared Function GetDefaultsForDriver(driverType As AudioIO.DriverType) As AudioDeviceSettings
            Dim defaults = New AudioDeviceSettings()
            
            Select Case driverType
                Case AudioIO.DriverType.WaveIn
                    ' WaveIn defaults - standard CD quality
                    defaults.DriverType = AudioIO.DriverType.WaveIn
                    defaults.SampleRate = 44100
                    defaults.BitDepth = 16
                    defaults.Channels = 2
                    defaults.BufferMilliseconds = 20
                    defaults.InputDeviceIndex = 0
                    
                Case AudioIO.DriverType.WASAPI
                    ' WASAPI defaults - let device use native format
                    ' Note: WASAPI will override with native format (usually 48kHz/16bit after conversion)
                    defaults.DriverType = AudioIO.DriverType.WASAPI
                    defaults.SampleRate = 48000  ' Common WASAPI native rate
                    defaults.BitDepth = 16       ' After float conversion
                    defaults.Channels = 2
                    defaults.BufferMilliseconds = 10  ' Lower latency for WASAPI
                    defaults.InputDeviceIndex = 0
                    
                Case AudioIO.DriverType.ASIO
                    ' ASIO defaults - professional low-latency
                    defaults.DriverType = AudioIO.DriverType.ASIO
                    defaults.SampleRate = 48000
                    defaults.BitDepth = 24
                    defaults.Channels = 2
                    defaults.BufferMilliseconds = 5  ' Lowest latency
                    defaults.InputDeviceIndex = 0
            End Select
            
            Return defaults
        End Function
    End Class

#End Region

End Namespace
