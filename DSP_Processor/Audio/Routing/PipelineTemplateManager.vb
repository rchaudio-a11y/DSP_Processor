Imports System.IO
Imports Newtonsoft.Json
Imports DSP_Processor.Utils

Namespace Audio.Routing

    ''' <summary>
    ''' Manages pipeline configuration templates (user-defined and built-in presets).
    ''' Provides quick-switch functionality for common routing scenarios.
    ''' </summary>
    Public Class PipelineTemplateManager

#Region "Singleton Pattern"

        Private Shared ReadOnly _instance As New Lazy(Of PipelineTemplateManager)(
            Function() New PipelineTemplateManager())

        ''' <summary>Get singleton instance</summary>
        Public Shared ReadOnly Property Instance As PipelineTemplateManager
            Get
                Return _instance.Value
            End Get
        End Property

        ''' <summary>Private constructor for singleton</summary>
        Private Sub New()
            ' Initialize paths
            settingsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings")
            templatesFilePath = Path.Combine(settingsFolder, "AudioPipelineTemplates.json")

            ' Load templates
            templates = New Dictionary(Of String, PipelineConfiguration)(StringComparer.OrdinalIgnoreCase)
            LoadTemplates()
        End Sub

#End Region

#Region "Private Fields"

        Private ReadOnly settingsFolder As String
        Private ReadOnly templatesFilePath As String

        ''' <summary>User-defined templates</summary>
        Private templates As Dictionary(Of String, PipelineConfiguration)

#End Region

#Region "Public Methods - Template Operations"

        ''' <summary>Get all available template names (built-in + user-defined)</summary>
        Public Function GetTemplateNames() As List(Of String)
            Dim names = New List(Of String)()

            ' Add built-in presets
            names.Add("Simple Record")
            names.Add("Pro Record")
            names.Add("Playback Only")

            ' Add user templates
            For Each kvp In templates
                If Not names.Contains(kvp.Key) Then
                    names.Add(kvp.Key)
                End If
            Next

            Return names
        End Function

        ''' <summary>Get template by name (checks built-in first, then user-defined)</summary>
        Public Function GetTemplate(name As String) As PipelineConfiguration
            If String.IsNullOrWhiteSpace(name) Then
                Return Nothing
            End If

            ' Check built-in presets first
            Dim builtIn = GetBuiltInPreset(name)
            If builtIn IsNot Nothing Then
                Return CloneConfiguration(builtIn)
            End If

            ' Check user templates
            If templates.ContainsKey(name) Then
                Return CloneConfiguration(templates(name))
            End If

            Logger.Instance.Warning($"Template not found: {name}", "PipelineTemplateManager")
            Return Nothing
        End Function

        ''' <summary>Save current configuration as a user template</summary>
        Public Function SaveTemplate(name As String, config As PipelineConfiguration) As Boolean
            If String.IsNullOrWhiteSpace(name) Then
                Logger.Instance.Warning("Cannot save template with empty name", "PipelineTemplateManager")
                Return False
            End If

            If config Is Nothing Then
                Logger.Instance.Warning("Cannot save null configuration", "PipelineTemplateManager")
                Return False
            End If

            ' Don't allow overwriting built-in presets
            If IsBuiltInPreset(name) Then
                Logger.Instance.Warning($"Cannot overwrite built-in preset: {name}", "PipelineTemplateManager")
                Return False
            End If

            Try
                ' Save to dictionary
                templates(name) = CloneConfiguration(config)

                ' Persist to file
                SaveTemplatesInternal()

                Logger.Instance.Info($"Template saved: {name}", "PipelineTemplateManager")
                Return True

            Catch ex As Exception
                Logger.Instance.Error($"Failed to save template: {name}", ex, "PipelineTemplateManager")
                Return False
            End Try
        End Function

        ''' <summary>Delete a user template</summary>
        Public Function DeleteTemplate(name As String) As Boolean
            If String.IsNullOrWhiteSpace(name) Then
                Return False
            End If

            ' Don't allow deleting built-in presets
            If IsBuiltInPreset(name) Then
                Logger.Instance.Warning($"Cannot delete built-in preset: {name}", "PipelineTemplateManager")
                Return False
            End If

            Try
                If templates.ContainsKey(name) Then
                    templates.Remove(name)
                    SaveTemplatesInternal()
                    Logger.Instance.Info($"Template deleted: {name}", "PipelineTemplateManager")
                    Return True
                End If

                Return False

            Catch ex As Exception
                Logger.Instance.Error($"Failed to delete template: {name}", ex, "PipelineTemplateManager")
                Return False
            End Try
        End Function

        ''' <summary>Check if template name is a built-in preset</summary>
        Public Function IsBuiltInPreset(name As String) As Boolean
            If String.IsNullOrWhiteSpace(name) Then Return False

            Return name.Equals("Simple Record", StringComparison.OrdinalIgnoreCase) OrElse
                   name.Equals("Pro Record", StringComparison.OrdinalIgnoreCase) OrElse
                   name.Equals("Playback Only", StringComparison.OrdinalIgnoreCase)
        End Function

#End Region

#Region "Built-In Presets"

        ''' <summary>Get a built-in preset configuration</summary>
        Public Function GetBuiltInPreset(name As String) As PipelineConfiguration
            If String.IsNullOrWhiteSpace(name) Then Return Nothing

            Select Case name.ToLowerInvariant()
                Case "simple record"
                    Return CreateSimpleRecordPreset()

                Case "pro record"
                    Return CreateProRecordPreset()

                Case "playback only"
                    Return CreatePlaybackOnlyPreset()

                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>Simple Record: Basic recording with no DSP</summary>
        Private Function CreateSimpleRecordPreset() As PipelineConfiguration
            Dim config = New PipelineConfiguration()

            ' Source: Microphone
            config.Source.ActiveSource = AudioSourceType.Microphone

            ' Processing: DSP disabled
            config.Processing.EnableDSP = False
            config.Processing.InputGain = 1.0F
            config.Processing.OutputGain = 1.0F

            ' Monitoring: Input FFT and meter only
            config.Monitoring.EnableInputFFT = True
            config.Monitoring.InputFFTTap = TapPoint.PreDSP
            config.Monitoring.EnableOutputFFT = False
            config.Monitoring.EnableLevelMeter = True
            config.Monitoring.FFTUpdateInterval = 50
            config.Monitoring.MeterUpdateInterval = 20

            ' Destination: Recording enabled
            config.Destination.EnableRecording = True
            config.Destination.EnablePlayback = False

            Return config
        End Function

        ''' <summary>Pro Record: Recording with DSP processing</summary>
        Private Function CreateProRecordPreset() As PipelineConfiguration
            Dim config = New PipelineConfiguration()

            ' Source: Microphone
            config.Source.ActiveSource = AudioSourceType.Microphone

            ' Processing: DSP enabled with gain
            config.Processing.EnableDSP = True
            config.Processing.ProcessingChain.Add(ProcessorType.Gain)
            config.Processing.InputGain = 1.0F
            config.Processing.OutputGain = 1.0F

            ' Monitoring: Both FFTs enabled
            config.Monitoring.EnableInputFFT = True
            config.Monitoring.InputFFTTap = TapPoint.PreDSP
            config.Monitoring.EnableOutputFFT = True
            config.Monitoring.OutputFFTTap = TapPoint.PostDSP
            config.Monitoring.EnableLevelMeter = True
            config.Monitoring.FFTUpdateInterval = 50
            config.Monitoring.MeterUpdateInterval = 20

            ' Destination: Recording enabled
            config.Destination.EnableRecording = True
            config.Destination.EnablePlayback = False

            Return config
        End Function

        ''' <summary>Playback Only: For file playback with DSP</summary>
        Private Function CreatePlaybackOnlyPreset() As PipelineConfiguration
            Dim config = New PipelineConfiguration()

            ' Source: File
            config.Source.ActiveSource = AudioSourceType.File

            ' Processing: DSP enabled
            config.Processing.EnableDSP = True
            config.Processing.ProcessingChain.Add(ProcessorType.Gain)
            config.Processing.InputGain = 0.8F  ' Slightly lower for playback
            config.Processing.OutputGain = 1.0F

            ' Monitoring: Both FFTs enabled
            config.Monitoring.EnableInputFFT = True
            config.Monitoring.InputFFTTap = TapPoint.PreDSP
            config.Monitoring.EnableOutputFFT = True
            config.Monitoring.OutputFFTTap = TapPoint.PostDSP
            config.Monitoring.EnableLevelMeter = False
            config.Monitoring.FFTUpdateInterval = 50

            ' Destination: Playback enabled
            config.Destination.EnableRecording = False
            config.Destination.EnablePlayback = True

            Return config
        End Function

#End Region

#Region "Private Methods"

        ''' <summary>Load user templates from file</summary>
        Private Sub LoadTemplates()
            Try
                If File.Exists(templatesFilePath) Then
                    Try
                        Dim json = File.ReadAllText(templatesFilePath)
                        Dim loadedTemplates = JsonConvert.DeserializeObject(Of Dictionary(Of String, PipelineConfiguration))(json)

                        If loadedTemplates IsNot Nothing Then
                            templates = loadedTemplates
                            Logger.Instance.Info($"Loaded {templates.Count} user templates", "PipelineTemplateManager")
                        End If

                    Catch jsonEx As JsonException
                        Logger.Instance.Error("Templates file corrupt, starting fresh", jsonEx, "PipelineTemplateManager")
                        BackupCorruptFile(templatesFilePath)
                    End Try
                Else
                    Logger.Instance.Info("No user templates file found", "PipelineTemplateManager")
                End If

            Catch ex As Exception
                Logger.Instance.Error("Error loading templates", ex, "PipelineTemplateManager")
            End Try
        End Sub

        ''' <summary>Save user templates to file</summary>
        Private Sub SaveTemplatesInternal()
            Try
                Dim json = JsonConvert.SerializeObject(templates, Formatting.Indented)
                File.WriteAllText(templatesFilePath, json)
                Logger.Instance.Info($"Saved {templates.Count} user templates", "PipelineTemplateManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to save templates", ex, "PipelineTemplateManager")
                Throw
            End Try
        End Sub

        ''' <summary>Clone configuration (deep copy)</summary>
        Private Function CloneConfiguration(config As PipelineConfiguration) As PipelineConfiguration
            Try
                ' Use JSON serialization for deep clone
                Dim json = JsonConvert.SerializeObject(config)
                Return JsonConvert.DeserializeObject(Of PipelineConfiguration)(json)
            Catch ex As Exception
                Logger.Instance.Error("Failed to clone configuration", ex, "PipelineTemplateManager")
                Return Nothing
            End Try
        End Function

        ''' <summary>Backup corrupt templates file</summary>
        Private Sub BackupCorruptFile(filePath As String)
            Try
                Dim backupPath = filePath & $".corrupt.{DateTime.Now:yyyyMMddHHmmss}"
                If File.Exists(filePath) Then
                    File.Copy(filePath, backupPath, overwrite:=True)
                    Logger.Instance.Info($"Corrupt templates backed up to: {backupPath}", "PipelineTemplateManager")
                End If
            Catch ex As Exception
                Logger.Instance.Error("Failed to backup corrupt templates", ex, "PipelineTemplateManager")
            End Try
        End Sub

#End Region

    End Class

End Namespace
