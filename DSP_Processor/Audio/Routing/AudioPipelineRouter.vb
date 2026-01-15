Imports DSP_Processor.Utils
Imports DSP_Processor.Managers

Namespace Audio.Routing

    ''' <summary>
    ''' Central audio pipeline routing controller.
    ''' Manages all audio flow, processing options, and buffer routing.
    ''' All routing decisions made here - single source of truth.
    ''' </summary>
    Public Class AudioPipelineRouter

#Region "Private Fields"

        ''' <summary>Current pipeline configuration</summary>
        Private _currentConfiguration As PipelineConfiguration

        ''' <summary>Whether router has been initialized</summary>
        Private _isInitialized As Boolean = False

        ''' <summary>Lock object for thread-safe configuration updates</summary>
        Private ReadOnly _configLock As New Object()

#End Region

#Region "Public Properties"

        ''' <summary>Get the current pipeline configuration (read-only)</summary>
        Public ReadOnly Property CurrentConfiguration As PipelineConfiguration
            Get
                SyncLock _configLock
                    Return _currentConfiguration
                End SyncLock
            End Get
        End Property

        ''' <summary>Check if router has been initialized</summary>
        Public ReadOnly Property IsInitialized As Boolean
            Get
                Return _isInitialized
            End Get
        End Property

        ''' <summary>Get all available template names (built-in + user-defined)</summary>
        Public ReadOnly Property AvailableTemplates As List(Of String)
            Get
                Return PipelineTemplateManager.Instance.GetTemplateNames()
            End Get
        End Property

#End Region

#Region "Events"

        ''' <summary>Fired when routing configuration changes</summary>
        Public Event RoutingChanged As EventHandler(Of RoutingChangedEventArgs)

        ''' <summary>Fired when buffer should be recorded to file</summary>
        Public Event BufferForRecording As EventHandler(Of AudioBufferRoutingEventArgs)

        ''' <summary>Fired when buffer should be monitored (FFT/meters)</summary>
        Public Event BufferForMonitoring As EventHandler(Of AudioBufferRoutingEventArgs)

        ''' <summary>Fired when buffer should be played back to speakers</summary>
        Public Event BufferForPlayback As EventHandler(Of AudioBufferRoutingEventArgs)

#End Region

#Region "Initialization"

        ''' <summary>
        ''' Initialize the router - loads last saved configuration from JSON.
        ''' </summary>
        Public Sub Initialize()
            Try
                Logger.Instance.Info("Initializing AudioPipelineRouter", "AudioPipelineRouter")

                ' Load configuration from file (or defaults if missing)
                _currentConfiguration = PipelineConfigurationManager.Instance.LoadConfiguration()

                _isInitialized = True

                Logger.Instance.Info("AudioPipelineRouter initialized successfully", "AudioPipelineRouter")

            Catch ex As Exception
                Logger.Instance.Error("Failed to initialize AudioPipelineRouter", ex, "AudioPipelineRouter")
                Throw
            End Try
        End Sub

#End Region

#Region "Configuration Management"

        ''' <summary>
        ''' Update routing configuration.
        ''' Thread-safe. Raises RoutingChanged event.
        ''' Auto-saves to JSON via PipelineConfigurationManager (500ms throttle).
        ''' </summary>
        Public Sub UpdateRouting(newConfig As PipelineConfiguration)
            If newConfig Is Nothing Then
                Throw New ArgumentNullException(NameOf(newConfig))
            End If

            If Not _isInitialized Then
                Throw New InvalidOperationException("Router must be initialized before updating configuration")
            End If

            Try
                ' Thread-safe configuration update
                Dim oldConfig As PipelineConfiguration
                SyncLock _configLock
                    oldConfig = _currentConfiguration
                    _currentConfiguration = newConfig
                    _currentConfiguration.LastModified = DateTime.Now
                End SyncLock

                ' Auto-save to JSON (throttled - 500ms debounce)
                PipelineConfigurationManager.Instance.SaveConfiguration(newConfig)

                ' Log the change
                Logger.Instance.Info($"Routing configuration updated", "AudioPipelineRouter")

                ' Raise event (outside lock to prevent deadlock)
                RaiseEvent RoutingChanged(Me, New RoutingChangedEventArgs(oldConfig, newConfig))

            Catch ex As Exception
                Logger.Instance.Error("Failed to update routing configuration", ex, "AudioPipelineRouter")
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Apply a template by name (built-in or user-defined).
        ''' Loads template and calls UpdateRouting().
        ''' </summary>
        Public Function ApplyTemplate(templateName As String) As Boolean
            Try
                Dim template = PipelineTemplateManager.Instance.GetTemplate(templateName)
                If template Is Nothing Then
                    Logger.Instance.Warning($"Template not found: {templateName}", "AudioPipelineRouter")
                    Return False
                End If

                UpdateRouting(template)
                Logger.Instance.Info($"Applied template: {templateName}", "AudioPipelineRouter")
                Return True

            Catch ex As Exception
                Logger.Instance.Error($"Failed to apply template: {templateName}", ex, "AudioPipelineRouter")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Save current configuration as a user template.
        ''' </summary>
        Public Function SaveCurrentAsTemplate(templateName As String) As Boolean
            Try
                Dim currentConfig As PipelineConfiguration
                SyncLock _configLock
                    currentConfig = _currentConfiguration
                End SyncLock

                Return PipelineTemplateManager.Instance.SaveTemplate(templateName, currentConfig)

            Catch ex As Exception
                Logger.Instance.Error($"Failed to save template: {templateName}", ex, "AudioPipelineRouter")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Delete a user template by name.
        ''' </summary>
        Public Function DeleteTemplate(templateName As String) As Boolean
            Return PipelineTemplateManager.Instance.DeleteTemplate(templateName)
        End Function

#End Region

#Region "Routing Logic"

        ''' <summary>
        ''' Route an audio buffer through the pipeline based on current configuration.
        ''' This is a STUB for now - full implementation in Phase 3.
        ''' </summary>
        ''' <param name="buffer">Audio buffer data</param>
        ''' <param name="source">Source of the audio</param>
        ''' <param name="bitsPerSample">Bits per sample</param>
        ''' <param name="channels">Number of channels</param>
        ''' <param name="sampleRate">Sample rate</param>
        Public Sub RouteAudioBuffer(buffer As Byte(), source As AudioSourceType,
                                   bitsPerSample As Integer, channels As Integer, sampleRate As Integer)

            If Not _isInitialized Then
                Logger.Instance.Warning("RouteAudioBuffer called before initialization", "AudioPipelineRouter")
                Return
            End If

            If buffer Is Nothing OrElse buffer.Length = 0 Then
                Return
            End If

            Try
                ' STUB: Just log for now
                ' Full implementation in Phase 3 will:
                ' 1. Apply DSP if enabled
                ' 2. Tap for monitoring at configured points
                ' 3. Route to destinations (recording, playback)

                Logger.Instance.Debug($"RouteAudioBuffer: {buffer.Length} bytes from {source}", "AudioPipelineRouter")

                ' TODO: Phase 3 - Implement actual routing logic

            Catch ex As Exception
                Logger.Instance.Error("Error routing audio buffer", ex, "AudioPipelineRouter")
            End Try
        End Sub

#End Region

#Region "Visualization"

        ''' <summary>
        ''' Get current routing map for visualization/debugging
        ''' </summary>
        Public Function GetActiveRoutingMap() As RoutingMap
            Dim map = New RoutingMap()

            If Not _isInitialized Then
                Return map ' Empty map
            End If

            Try
                SyncLock _configLock
                    ' Build routing map from current configuration
                    Dim config = _currentConfiguration

                    ' Source path
                    Dim sourceName = config.Source.ActiveSource.ToString()

                    ' Processing path
                    Dim processingName = If(config.Processing.EnableDSP, "DSP Pipeline", "Direct (Bypass)")

                    ' Recording path
                    If config.Destination.EnableRecording Then
                        map.ActivePaths.Add(New PathInfo With {
                            .Source = sourceName,
                            .Destination = "Recording (File)",
                            .Processing = processingName,
                            .IsActive = True
                        })
                    End If

                    ' Playback path
                    If config.Destination.EnablePlayback Then
                        map.ActivePaths.Add(New PathInfo With {
                            .Source = sourceName,
                            .Destination = "Playback (Speakers)",
                            .Processing = processingName,
                            .IsActive = True
                        })
                    End If

                    ' Monitoring paths
                    If config.Monitoring.EnableInputFFT Then
                        map.ActivePaths.Add(New PathInfo With {
                            .Source = sourceName,
                            .Destination = "Input FFT",
                            .Processing = $"Tap: {config.Monitoring.InputFFTTap}",
                            .IsActive = True
                        })
                    End If

                    If config.Monitoring.EnableOutputFFT Then
                        map.ActivePaths.Add(New PathInfo With {
                            .Source = sourceName,
                            .Destination = "Output FFT",
                            .Processing = $"Tap: {config.Monitoring.OutputFFTTap}",
                            .IsActive = True
                        })
                    End If

                    If config.Monitoring.EnableLevelMeter Then
                        map.ActivePaths.Add(New PathInfo With {
                            .Source = sourceName,
                            .Destination = "Level Meter",
                            .Processing = $"Tap: {config.Monitoring.LevelMeterTap}",
                            .IsActive = True
                        })
                    End If

                End SyncLock

            Catch ex As Exception
                Logger.Instance.Error("Error building routing map", ex, "AudioPipelineRouter")
            End Try

            Return map
        End Function

#End Region

    End Class

End Namespace
