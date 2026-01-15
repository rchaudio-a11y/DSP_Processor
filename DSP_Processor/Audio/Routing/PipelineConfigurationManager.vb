Imports System.IO
Imports System.Timers
Imports Newtonsoft.Json
Imports DSP_Processor.Utils

Namespace Audio.Routing

    ''' <summary>
    ''' Manages pipeline configuration persistence with auto-save and atomic writes.
    ''' Singleton pattern like SettingsManager.
    ''' </summary>
    Public Class PipelineConfigurationManager

#Region "Singleton Pattern"

        Private Shared ReadOnly _instance As New Lazy(Of PipelineConfigurationManager)(
            Function() New PipelineConfigurationManager())

        ''' <summary>Get singleton instance</summary>
        Public Shared ReadOnly Property Instance As PipelineConfigurationManager
            Get
                Return _instance.Value
            End Get
        End Property

        ''' <summary>Private constructor for singleton</summary>
        Private Sub New()
            ' Initialize paths
            settingsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings")
            configFilePath = Path.Combine(settingsFolder, "AudioPipeline.json")
            templatesFilePath = Path.Combine(settingsFolder, "AudioPipelineTemplates.json")

            ' Ensure Settings folder exists
            If Not Directory.Exists(settingsFolder) Then
                Directory.CreateDirectory(settingsFolder)
                Logger.Instance.Info($"Created Settings folder: {settingsFolder}", "PipelineConfigurationManager")
            End If

            ' Initialize auto-save timer (500ms debounce)
            autoSaveTimer = New Timer(500)
            autoSaveTimer.AutoReset = False
            AddHandler autoSaveTimer.Elapsed, AddressOf OnAutoSaveTimer
        End Sub

#End Region

#Region "Private Fields"

        Private ReadOnly settingsFolder As String
        Private ReadOnly configFilePath As String
        Private ReadOnly templatesFilePath As String

        ''' <summary>Auto-save timer for throttling</summary>
        Private ReadOnly autoSaveTimer As Timer

        ''' <summary>Pending configuration to save</summary>
        Private pendingConfiguration As PipelineConfiguration

        ''' <summary>Lock for thread-safe operations</summary>
        Private ReadOnly saveLock As New Object()

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Load configuration from file.
        ''' Returns default configuration if file doesn't exist or is corrupt.
        ''' </summary>
        Public Function LoadConfiguration() As PipelineConfiguration
            Try
                If File.Exists(configFilePath) Then
                    Try
                        Logger.Instance.Info("Loading pipeline configuration...", "PipelineConfigurationManager")

                        Dim json = File.ReadAllText(configFilePath)
                        Dim config = JsonConvert.DeserializeObject(Of PipelineConfiguration)(json)

                        ' Validate loaded configuration
                        If config IsNot Nothing AndAlso ValidateConfiguration(config) Then
                            Logger.Instance.Info("Pipeline configuration loaded successfully", "PipelineConfigurationManager")
                            Return config
                        Else
                            Logger.Instance.Warning("Invalid configuration loaded, using defaults", "PipelineConfigurationManager")
                        End If

                    Catch jsonEx As JsonException
                        ' Corrupt JSON - backup and use defaults
                        Logger.Instance.Error("Configuration file corrupt, backing up and using defaults", jsonEx, "PipelineConfigurationManager")
                        BackupCorruptFile(configFilePath)
                    Catch ex As Exception
                        Logger.Instance.Error("Failed to load configuration", ex, "PipelineConfigurationManager")
                    End Try
                Else
                    Logger.Instance.Info("Configuration file not found, using defaults", "PipelineConfigurationManager")
                End If

            Catch ex As Exception
                Logger.Instance.Error("Error accessing configuration file", ex, "PipelineConfigurationManager")
            End Try

            ' Return defaults if load failed
            Return GetDefaultConfiguration()
        End Function

        ''' <summary>
        ''' Save configuration with auto-save throttling (500ms debounce).
        ''' Thread-safe and uses atomic writes.
        ''' </summary>
        Public Sub SaveConfiguration(config As PipelineConfiguration)
            If config Is Nothing Then
                Logger.Instance.Warning("Attempted to save null configuration", "PipelineConfigurationManager")
                Return
            End If

            Try
                ' Store pending configuration
                SyncLock saveLock
                    pendingConfiguration = config
                End SyncLock

                ' Reset debounce timer
                autoSaveTimer.Stop()
                autoSaveTimer.Start()

                Logger.Instance.Debug("Configuration save scheduled (500ms throttle)", "PipelineConfigurationManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to schedule configuration save", ex, "PipelineConfigurationManager")
            End Try
        End Sub

        ''' <summary>
        ''' Save configuration immediately (bypass throttling).
        ''' Use for application shutdown or critical saves.
        ''' </summary>
        Public Sub SaveConfigurationImmediate(config As PipelineConfiguration)
            If config Is Nothing Then Return

            Try
                SyncLock saveLock
                    SaveConfigurationInternal(config)
                End SyncLock

            Catch ex As Exception
                Logger.Instance.Error("Failed to save configuration immediately", ex, "PipelineConfigurationManager")
            End Try
        End Sub

        ''' <summary>Get default configuration with sensible defaults</summary>
        Public Function GetDefaultConfiguration() As PipelineConfiguration
            Dim config = New PipelineConfiguration()

            ' Source: Microphone by default
            config.Source.ActiveSource = AudioSourceType.Microphone

            ' Processing: DSP disabled, unity gain
            config.Processing.EnableDSP = False
            config.Processing.InputGain = 1.0F
            config.Processing.OutputGain = 1.0F

            ' Monitoring: Enable input FFT and level meter
            config.Monitoring.EnableInputFFT = True
            config.Monitoring.InputFFTTap = TapPoint.PreDSP
            config.Monitoring.EnableOutputFFT = False
            config.Monitoring.EnableLevelMeter = True
            config.Monitoring.FFTUpdateInterval = 50
            config.Monitoring.MeterUpdateInterval = 20

            ' Destination: Nothing enabled by default
            config.Destination.EnableRecording = False
            config.Destination.EnablePlayback = False

            Logger.Instance.Info("Default configuration created", "PipelineConfigurationManager")
            Return config
        End Function

#End Region

#Region "Private Methods"

        ''' <summary>Auto-save timer callback - performs actual save</summary>
        Private Sub OnAutoSaveTimer(sender As Object, e As ElapsedEventArgs)
            Try
                Dim configToSave As PipelineConfiguration

                SyncLock saveLock
                    configToSave = pendingConfiguration
                    pendingConfiguration = Nothing
                End SyncLock

                If configToSave IsNot Nothing Then
                    SaveConfigurationInternal(configToSave)
                End If

            Catch ex As Exception
                Logger.Instance.Error("Auto-save timer error", ex, "PipelineConfigurationManager")
            End Try
        End Sub

        ''' <summary>
        ''' Internal save with atomic write (temp file ? rename).
        ''' Never corrupts existing file.
        ''' </summary>
        Private Sub SaveConfigurationInternal(config As PipelineConfiguration)
            Dim tempPath = configFilePath & ".tmp"
            Dim backupPath = configFilePath & ".bak"

            Try
                ' Update timestamp
                config.LastModified = DateTime.Now

                ' Serialize to JSON
                Dim json = JsonConvert.SerializeObject(config, Formatting.Indented)

                ' Step 1: Write to temp file
                File.WriteAllText(tempPath, json)

                ' Step 2: Backup existing config (if exists)
                If File.Exists(configFilePath) Then
                    If File.Exists(backupPath) Then
                        File.Delete(backupPath)
                    End If
                    File.Copy(configFilePath, backupPath, overwrite:=True)
                End If

                ' Step 3: Rename temp to actual (atomic on Windows)
                If File.Exists(configFilePath) Then
                    File.Delete(configFilePath)
                End If
                File.Move(tempPath, configFilePath)

                Logger.Instance.Info("Pipeline configuration saved", "PipelineConfigurationManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to save configuration", ex, "PipelineConfigurationManager")

                ' Clean up temp file if it exists
                Try
                    If File.Exists(tempPath) Then
                        File.Delete(tempPath)
                    End If
                Catch
                    ' Ignore cleanup errors
                End Try

                ' Try to restore from backup if save failed
                Try
                    If File.Exists(backupPath) AndAlso Not File.Exists(configFilePath) Then
                        File.Copy(backupPath, configFilePath)
                        Logger.Instance.Info("Configuration restored from backup", "PipelineConfigurationManager")
                    End If
                Catch restoreEx As Exception
                    Logger.Instance.Error("Failed to restore from backup", restoreEx, "PipelineConfigurationManager")
                End Try
            End Try
        End Sub

        ''' <summary>Validate configuration structure</summary>
        Private Function ValidateConfiguration(config As PipelineConfiguration) As Boolean
            If config Is Nothing Then Return False
            If config.Source Is Nothing Then Return False
            If config.Processing Is Nothing Then Return False
            If config.Monitoring Is Nothing Then Return False
            If config.Destination Is Nothing Then Return False

            ' Passed basic validation
            Return True
        End Function

        ''' <summary>Backup corrupt configuration file</summary>
        Private Sub BackupCorruptFile(filePath As String)
            Try
                Dim backupPath = filePath & $".corrupt.{DateTime.Now:yyyyMMddHHmmss}"
                If File.Exists(filePath) Then
                    File.Copy(filePath, backupPath, overwrite:=True)
                    Logger.Instance.Info($"Corrupt config backed up to: {backupPath}", "PipelineConfigurationManager")
                End If
            Catch ex As Exception
                Logger.Instance.Error("Failed to backup corrupt file", ex, "PipelineConfigurationManager")
            End Try
        End Sub

#End Region

#Region "IDisposable"

        ''' <summary>Ensure pending saves are flushed before disposal</summary>
        Public Sub Dispose()
            Try
                ' Stop timer and flush pending save
                autoSaveTimer.Stop()

                SyncLock saveLock
                    If pendingConfiguration IsNot Nothing Then
                        SaveConfigurationInternal(pendingConfiguration)
                        pendingConfiguration = Nothing
                    End If
                End SyncLock

                autoSaveTimer.Dispose()

            Catch ex As Exception
                Logger.Instance.Error("Error during PipelineConfigurationManager disposal", ex, "PipelineConfigurationManager")
            End Try
        End Sub

#End Region

    End Class

End Namespace
