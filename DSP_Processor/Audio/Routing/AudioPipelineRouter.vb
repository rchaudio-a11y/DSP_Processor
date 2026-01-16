Imports DSP_Processor.Utils
Imports DSP_Processor.Managers
Imports DSP_Processor.DSP.FFT

Namespace Audio.Routing

    ''' <summary>
    ''' Central audio pipeline routing controller - Phase 3 Refactored!
    ''' 
    ''' NEW ARCHITECTURE:
    ''' - AudioPipeline handles all audio processing (fast path, no events)
    ''' - FFTMonitorThread runs independently (freewheeling, never blocks audio)
    ''' - Clean separation: Audio thread vs FFT thread
    ''' - No event flooding, no Task.Run spam
    ''' 
    ''' Old approach (Phase 2): Event-driven with Task.Run overhead
    ''' New approach (Phase 3): Lock-free dual-buffers with independent FFT thread
    ''' </summary>
    Public Class AudioPipelineRouter
        Implements IDisposable

#Region "Private Fields"

        ''' <summary>Current pipeline configuration</summary>
        Private _currentConfiguration As PipelineConfiguration

        ''' <summary>Audio processing pipeline (dual-buffer architecture)</summary>
        Private _pipeline As AudioPipeline

        ''' <summary>Freewheeling FFT monitor thread</summary>
        Private _fftMonitor As FFTMonitorThread

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

        ''' <summary>
        ''' Get FFT monitor thread (for subscribing to SpectrumReady event).
        ''' NEW in Phase 3 - replaces old BufferForMonitoring event.
        ''' </summary>
        Public ReadOnly Property FFTMonitor As FFTMonitorThread
            Get
                Return _fftMonitor
            End Get
        End Property

        ''' <summary>Get the audio pipeline (for advanced access if needed)</summary>
        Friend ReadOnly Property Pipeline As AudioPipeline
            Get
                Return _pipeline
            End Get
        End Property

#End Region

#Region "Events"

        ''' <summary>Fired when routing configuration changes</summary>
        Public Event RoutingChanged As EventHandler(Of RoutingChangedEventArgs)

        ''' <summary>
        ''' REMOVED in Phase 3: BufferForRecording, BufferForMonitoring, BufferForPlayback
        ''' These events caused event flooding and Task.Run overhead.
        ''' 
        ''' NEW approach:
        ''' - BufferForMonitoring ? Replaced by FFTMonitor.SpectrumReady (throttled, single event)
        ''' - BufferForRecording ? Will be handled directly by pipeline in Phase 3.1
        ''' - BufferForPlayback ? Will be handled directly by pipeline in Phase 3.2
        ''' </summary>

#End Region

#Region "Initialization"

        ''' <summary>
        ''' Initialize the router - loads configuration and starts FFT monitor thread.
        ''' Phase 3: Creates AudioPipeline and FFTMonitorThread.
        ''' </summary>
        Public Sub Initialize()
            Try
                Logger.Instance.Info("Initializing AudioPipelineRouter (Phase 3 architecture)", "AudioPipelineRouter")

                ' Load configuration from file (or defaults if missing)
                _currentConfiguration = PipelineConfigurationManager.Instance.LoadConfiguration()

                ' Create audio processing pipeline
                _pipeline = New AudioPipeline(_currentConfiguration)
                Logger.Instance.Info("AudioPipeline created", "AudioPipelineRouter")

                ' Create and start FFT monitor thread
                ' Input stage = Pre-DSP (raw audio)
                ' Gain stage = Post-DSP (processed audio) - THIS IS CORRECT!
                _fftMonitor = New FFTMonitorThread(
                    _pipeline.GetStage(PipelineStage.Input),
                    _pipeline.GetStage(PipelineStage.Gain))
                _fftMonitor.Start()
                Logger.Instance.Info("FFT monitor thread started", "AudioPipelineRouter")

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

                ' CRITICAL: Update the pipeline with new configuration!
                ' This makes the controls actually work!
                _pipeline?.UpdateConfiguration(newConfig)

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
        ''' Route an audio buffer through the pipeline - PHASE 3 REFACTORED!
        ''' 
        ''' NEW: TRUE BYPASS MODE!
        ''' - When DSP disabled: Direct routing (bypasses pipeline entirely)
        ''' - When DSP enabled: Full pipeline processing with FFT taps
        ''' 
        ''' This allows perfect A/B testing:
        ''' - Record file with DSP disabled (pure input)
        ''' - Record file with DSP enabled (processed)
        ''' - Compare the files to verify DSP behavior!
        ''' </summary>
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
                ' Get configuration snapshot
                Dim config As PipelineConfiguration
                SyncLock _configLock
                    config = _currentConfiguration
                End SyncLock

                ' === TRUE BYPASS: Check if DSP is enabled ===
                If Not config.Processing.EnableDSP Then
                    ' === BYPASS MODE: Skip pipeline entirely! ===
                    ' No FFT taps, no monitoring, no processing
                    ' Direct path: Input ? Output (zero overhead)
                    ' Perfect for A/B testing!
                    Logger.Instance.Debug("DSP BYPASS: Audio routed directly (pipeline skipped)", "AudioPipelineRouter")
                    Return
                End If

                ' === DSP ENABLED: Route through full pipeline ===
                ' This includes:
                ' - FFT tap points (if enabled)
                ' - DSP processing (gain, future: EQ, compressor, etc.)
                ' - Monitoring stages
                Dim processedBuffer = _pipeline.ProcessBuffer(buffer, sampleRate, bitsPerSample, channels)

                ' Processed buffer can now be routed to destinations
                ' Phase 3.1 will connect this to RecordingManager

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

#Region "IDisposable Implementation"

        Private _disposed As Boolean = False

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _disposed Then
                If disposing Then
                    ' Stop FFT monitor thread
                    If _fftMonitor IsNot Nothing Then
                        _fftMonitor.Stop()
                        _fftMonitor.Dispose()
                        Logger.Instance.Info("FFT monitor thread stopped", "AudioPipelineRouter")
                    End If

                    ' Clear pipeline
                    _pipeline?.Clear()
                End If
                _disposed = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

#End Region

    End Class

End Namespace
