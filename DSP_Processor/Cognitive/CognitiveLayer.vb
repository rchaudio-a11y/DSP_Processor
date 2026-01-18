Imports System.Collections.Generic

Namespace Cognitive

    ''' <summary>
    ''' CognitiveLayer - Central coordinator for all cognitive systems
    ''' Wires v1.x cognitive modules together into unified interface
    ''' Foundation for v2.0 Introspective Engine
    ''' Zero core impact - purely observational
    ''' </summary>
    Public Class CognitiveLayer
        Implements IDisposable

        Private ReadOnly _coordinator As StateCoordinator
        Private ReadOnly _lock As New Object()
        Private _disposed As Boolean = False

        ' v1.x Cognitive Systems
        Private _workingMemory As WorkingMemoryBuffer
        Private _habitAnalyzer As HabitLoopAnalyzer
        Private _attentionSpotlight As AttentionSpotlight
        Private _conflictDetector As ConflictDetector
        Private _narrativeGenerator As NarrativeGenerator

        ' v2.0 Introspective Systems
        Private _predictionEngine As PredictionEngine
        Private _anomalyDetector As AnomalyDetector
        Private _adaptiveThresholds As AdaptiveThresholdManager

        ' Auto-export timer (exports every 5 seconds for crash recovery)
        Private WithEvents _autoExportTimer As System.Timers.Timer
        Private _lastExportTime As DateTime = DateTime.MinValue
        Private ReadOnly _autoExportIntervalMs As Integer = 5000 ' 5 seconds
        Private _currentSessionNumber As Integer = 0 ' Track session number for unique filenames

        ' Configuration
        Private _enableWorkingMemory As Boolean = True
        Private _enableHabitAnalysis As Boolean = True
        Private _enableAttentionTracking As Boolean = True
        Private _enableConflictDetection As Boolean = True
        Private _enableV2Features As Boolean = True ' v2.0 features

        ''' <summary>
        ''' Creates cognitive layer coordinator
        ''' </summary>
        ''' <param name="coordinator">State coordinator to observe</param>
        Public Sub New(coordinator As StateCoordinator)
            If coordinator Is Nothing Then
                Throw New ArgumentNullException(NameOf(coordinator))
            End If

            _coordinator = coordinator

            ' DIAGNOSTIC: Log where constructor is being called from
            Dim stackTrace = New System.Diagnostics.StackTrace(1, True)
            Dim caller = stackTrace.GetFrame(0)
            Utils.Logger.Instance.Info($"CognitiveLayer constructor called from: {caller?.GetMethod()?.Name} in {caller?.GetFileName()}:Line {caller?.GetFileLineNumber()}", "CognitiveLayer")

            ' Generate unique session number (find next available)
            _currentSessionNumber = FindNextSessionNumber()
            Utils.Logger.Instance.Info($"CognitiveLayer session #{_currentSessionNumber:D3} starting", "CognitiveLayer")

            ' Initialize cognitive systems
            InitializeCognitiveSystems()

            ' Subscribe to events
            SubscribeToEvents()

            ' Start auto-export timer (exports every 5 seconds for crash recovery)
            StartAutoExportTimer()

            ' Export initial state immediately
            ExportToLog()

            Utils.Logger.Instance.Info($"CognitiveLayer initialized - Session #{_currentSessionNumber:D3}", "CognitiveLayer")
        End Sub

        ''' <summary>
        ''' Creates cognitive layer with custom configuration
        ''' </summary>
        Public Sub New(coordinator As StateCoordinator, config As CognitiveConfig)
            Me.New(coordinator)

            ' Apply configuration
            _enableWorkingMemory = config.EnableWorkingMemory
            _enableHabitAnalysis = config.EnableHabitAnalysis
            _enableAttentionTracking = config.EnableAttentionTracking
            _enableConflictDetection = config.EnableConflictDetection

            ' Update system states
            _workingMemory.Enabled = _enableWorkingMemory
            _habitAnalyzer.Enabled = _enableHabitAnalysis
            _attentionSpotlight.Enabled = _enableAttentionTracking
            _conflictDetector.Enabled = _enableConflictDetection

            Utils.Logger.Instance.Info($"CognitiveLayer configured: Memory={_enableWorkingMemory}, Habits={_enableHabitAnalysis}, Attention={_enableAttentionTracking}, Conflicts={_enableConflictDetection}", "CognitiveLayer")
        End Sub

#Region "Public Properties - Cognitive Systems"

        ''' <summary>Working Memory Buffer - Short-term memory</summary>
        Public ReadOnly Property WorkingMemory As WorkingMemoryBuffer
            Get
                Return _workingMemory
            End Get
        End Property

        ''' <summary>Habit Loop Analyzer - Pattern detection</summary>
        Public ReadOnly Property HabitAnalyzer As HabitLoopAnalyzer
            Get
                Return _habitAnalyzer
            End Get
        End Property

        ''' <summary>Attention Spotlight - Focus tracking</summary>
        Public ReadOnly Property AttentionSpotlight As AttentionSpotlight
            Get
                Return _attentionSpotlight
            End Get
        End Property

        ''' <summary>Conflict Detector - Consistency monitoring</summary>
        Public ReadOnly Property ConflictDetector As ConflictDetector
            Get
                Return _conflictDetector
            End Get
        End Property

        ''' <summary>Narrative Generator - Human-readable summaries</summary>
        Public ReadOnly Property NarrativeGenerator As NarrativeGenerator
            Get
                Return _narrativeGenerator
            End Get
        End Property

        ''' <summary>Prediction Engine - v2.0 Forecasting</summary>
        Public ReadOnly Property PredictionEngine As PredictionEngine
            Get
                Return _predictionEngine
            End Get
        End Property

        ''' <summary>Anomaly Detector - v2.0 Unusual behavior detection</summary>
        Public ReadOnly Property AnomalyDetector As AnomalyDetector
            Get
                Return _anomalyDetector
            End Get
        End Property

        ''' <summary>Adaptive Thresholds - v2.0 Self-tuning</summary>
        Public ReadOnly Property AdaptiveThresholds As AdaptiveThresholdManager
            Get
                Return _adaptiveThresholds
            End Get
        End Property

#End Region

#Region "Configuration Properties"

        ''' <summary>Enable/disable Working Memory</summary>
        Public Property EnableWorkingMemory As Boolean
            Get
                SyncLock _lock
                    Return _enableWorkingMemory
                End SyncLock
            End Get
            Set(value As Boolean)
                SyncLock _lock
                    _enableWorkingMemory = value
                    If _workingMemory IsNot Nothing Then
                        _workingMemory.Enabled = value
                    End If
                End SyncLock
            End Set
        End Property

        ''' <summary>Enable/disable Habit Analysis</summary>
        Public Property EnableHabitAnalysis As Boolean
            Get
                SyncLock _lock
                    Return _enableHabitAnalysis
                End SyncLock
            End Get
            Set(value As Boolean)
                SyncLock _lock
                    _enableHabitAnalysis = value
                    If _habitAnalyzer IsNot Nothing Then
                        _habitAnalyzer.Enabled = value
                    End If
                End SyncLock
            End Set
        End Property

        ''' <summary>Enable/disable Attention Tracking</summary>
        Public Property EnableAttentionTracking As Boolean
            Get
                SyncLock _lock
                    Return _enableAttentionTracking
                End SyncLock
            End Get
            Set(value As Boolean)
                SyncLock _lock
                    _enableAttentionTracking = value
                    If _attentionSpotlight IsNot Nothing Then
                        _attentionSpotlight.Enabled = value
                    End If
                End SyncLock
            End Set
        End Property

        ''' <summary>Enable/disable Conflict Detection</summary>
        Public Property EnableConflictDetection As Boolean
            Get
                SyncLock _lock
                    Return _enableConflictDetection
                End SyncLock
            End Get
            Set(value As Boolean)
                SyncLock _lock
                    _enableConflictDetection = value
                    If _conflictDetector IsNot Nothing Then
                        _conflictDetector.Enabled = value
                    End If
                End SyncLock
            End Set
        End Property

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Generates comprehensive cognitive report
        ''' </summary>
        Public Function GenerateReport() As CognitiveReport
            SyncLock _lock
                Dim report As New CognitiveReport With {
                    .Timestamp = DateTime.Now,
                    .WorkingMemoryStats = If(_workingMemory.Enabled, _workingMemory.GetStatistics(), Nothing),
                    .HabitStats = If(_habitAnalyzer.Enabled, _habitAnalyzer.GetStatistics(), Nothing),
                    .AttentionStats = If(_attentionSpotlight.Enabled, _attentionSpotlight.GetStatistics(), Nothing),
                    .ConflictStats = If(_conflictDetector.Enabled, _conflictDetector.GetStatistics(), Nothing)
                }

                Return report
            End SyncLock
        End Function

        ''' <summary>
        ''' Generates a human-readable session summary
        ''' </summary>
        Public Function GenerateSessionSummary() As String
            If _narrativeGenerator IsNot Nothing AndAlso _narrativeGenerator.Enabled Then
                Return _narrativeGenerator.GenerateSessionSummary()
            Else
                Return "Narrative Generator is not available."
            End If
        End Function

        ''' <summary>
        ''' Generates a brief one-line summary
        ''' </summary>
        Public Function GenerateBriefSummary() As String
            If _narrativeGenerator IsNot Nothing AndAlso _narrativeGenerator.Enabled Then
                Return _narrativeGenerator.GenerateBriefSummary()
            Else
                Return "No summary available."
            End If
        End Function

        ''' <summary>
        ''' Resets all cognitive systems
        ''' </summary>
        Public Sub ResetAll()
            SyncLock _lock
                If _workingMemory IsNot Nothing Then _workingMemory.Reset()
                If _habitAnalyzer IsNot Nothing Then _habitAnalyzer.Reset()
                If _attentionSpotlight IsNot Nothing Then _attentionSpotlight.Reset()
                If _conflictDetector IsNot Nothing Then _conflictDetector.Reset()
                If _narrativeGenerator IsNot Nothing Then _narrativeGenerator.Reset()

                Utils.Logger.Instance.Info("All cognitive systems reset", "CognitiveLayer")
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets summary of all cognitive systems
        ''' </summary>
        Public Function GetSummary() As String
            Dim summary As New System.Text.StringBuilder()
            summary.AppendLine("Cognitive Layer Status")
            summary.AppendLine(New String("="c, 60))
            summary.AppendLine()

            ' Working Memory
            If _workingMemory.Enabled Then
                summary.AppendLine($"Working Memory: {_workingMemory.Count} transitions")
            Else
                summary.AppendLine("Working Memory: DISABLED")
            End If

            ' Habit Analysis
            If _habitAnalyzer.Enabled Then
                Dim stats = CType(_habitAnalyzer.GetStatistics(), Object)
                Dim totalHabits = If(stats.TotalHabits IsNot Nothing, stats.TotalHabits.ToString(), "0")
                summary.AppendLine($"Habit Analysis: {totalHabits} habits detected")
            Else
                summary.AppendLine("Habit Analysis: DISABLED")
            End If

            ' Attention
            If _attentionSpotlight.Enabled Then
                Dim active = _attentionSpotlight.GetActiveSubsystem()
                summary.AppendLine($"Attention: {If(active, "None")}")
            Else
                summary.AppendLine("Attention: DISABLED")
            End If

            ' Conflicts
            If _conflictDetector.Enabled Then
                Dim healthScore = _conflictDetector.GetHealthScore()
                summary.AppendLine($"Health: {healthScore:P0} ({GetHealthRating(healthScore)})")
            Else
                summary.AppendLine("Conflict Detection: DISABLED")
            End If

            Return summary.ToString()
        End Function

        Private Function GetHealthRating(score As Double) As String
            If score >= 0.95 Then Return "Excellent"
            If score >= 0.85 Then Return "Good"
            If score >= 0.70 Then Return "Fair"
            If score >= 0.50 Then Return "Poor"
            Return "Critical"
        End Function

        ''' <summary>
        ''' Exports all cognitive data to logs (auto-updated every 5s)
        ''' Each session gets a unique numbered file (no overwrite!)
        ''' </summary>
        Public Sub ExportToLog()
            ' Generate session-based filename (e.g., Cognitive_Session_001.log)
            Dim logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs")
            Dim logFile = System.IO.Path.Combine(logDir, $"Cognitive_Session_{_currentSessionNumber:D3}.log")

            Try
                ' Ensure Logs directory exists
                If Not System.IO.Directory.Exists(logDir) Then
                    System.IO.Directory.CreateDirectory(logDir)
                End If

                ' Build comprehensive cognitive report
                Dim report As New System.Text.StringBuilder()
                report.AppendLine("?????????????????????????????????????????????????????????")
                report.AppendLine($"   COGNITIVE LAYER EXPORT - {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                report.AppendLine($"   SESSION #{_currentSessionNumber:D3}")
                report.AppendLine("?????????????????????????????????????????????????????????")
                report.AppendLine()

                ' SESSION SUMMARY (NarrativeGenerator - NEW!)
                If _narrativeGenerator IsNot Nothing AndAlso _narrativeGenerator.Enabled Then
                    report.AppendLine("?????????????????????????????????????????????????????????")
                    report.AppendLine("   SESSION SUMMARY")
                    report.AppendLine("?????????????????????????????????????????????????????????")
                    report.AppendLine(_narrativeGenerator.GenerateSessionSummary())
                    report.AppendLine()
                End If

                ' Working Memory
                If _workingMemory.Enabled Then
                    report.AppendLine("?????????????????????????????????????????????????????????")
                    report.AppendLine("   WORKING MEMORY BUFFER")
                    report.AppendLine("?????????????????????????????????????????????????????????")
                    report.AppendLine(_workingMemory.GenerateSummary())
                    report.AppendLine()
                End If

                ' Habit Analysis
                If _habitAnalyzer.Enabled Then
                    report.AppendLine("?????????????????????????????????????????????????????????")
                    report.AppendLine("   HABIT LOOP ANALYSIS")
                    report.AppendLine("?????????????????????????????????????????????????????????")
                    report.AppendLine(_habitAnalyzer.GenerateHabitReport())
                    report.AppendLine()
                End If

                ' Attention Spotlight
                If _attentionSpotlight.Enabled Then
                    report.AppendLine("?????????????????????????????????????????????????????????")
                    report.AppendLine("   ATTENTION SPOTLIGHT")
                    report.AppendLine("?????????????????????????????????????????????????????????")
                    report.AppendLine(_attentionSpotlight.GenerateAttentionReport(TimeSpan.FromSeconds(30)))
                    report.AppendLine()
                End If

                ' Conflict Detector
                If _conflictDetector.Enabled Then
                    report.AppendLine("?????????????????????????????????????????????????????????")
                    report.AppendLine("   CONFLICT DETECTOR")
                    report.AppendLine("?????????????????????????????????????????????????????????")
                    report.AppendLine(_conflictDetector.GenerateConflictReport())
                    report.AppendLine()
                End If

                report.AppendLine("?????????????????????????????????????????????????????????")
                report.AppendLine("   END COGNITIVE EXPORT")
                report.AppendLine("?????????????????????????????????????????????????????????")

                ' Write to session-specific log file (OVERWRITES same session file each time)
                System.IO.File.WriteAllText(logFile, report.ToString())

                Utils.Logger.Instance.Debug($"Cognitive data auto-exported: Session {_currentSessionNumber:D3}", "CognitiveLayer")

            Catch ex As Exception
                Utils.Logger.Instance.Error($"Failed to export cognitive data: {ex.Message}", ex, "CognitiveLayer")
                ' Don't throw - let auto-export continue
            End Try
        End Sub

        ''' <summary>
        ''' Find next available session number by checking existing log files
        ''' </summary>
        Private Function FindNextSessionNumber() As Integer
            Try
                Dim logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs")
                If Not System.IO.Directory.Exists(logDir) Then
                    Return 1 ' Start with session 1
                End If

                ' Find all existing Cognitive_Session_*.log files
                Dim existingFiles = System.IO.Directory.GetFiles(logDir, "Cognitive_Session_*.log")
                
                If existingFiles.Length = 0 Then
                    Return 1 ' No sessions yet
                End If

                ' Extract session numbers and find max
                Dim maxSession = 0
                For Each file In existingFiles
                    Dim filename = System.IO.Path.GetFileNameWithoutExtension(file)
                    ' Extract number from "Cognitive_Session_001" format
                    Dim parts = filename.Split("_"c)
                    If parts.Length >= 3 Then
                        Dim sessionNum As Integer
                        If Integer.TryParse(parts(2), sessionNum) Then
                            maxSession = Math.Max(maxSession, sessionNum)
                        End If
                    End If
                Next

                Return maxSession + 1 ' Next session

            Catch ex As Exception
                Utils.Logger.Instance.Warning($"Failed to determine session number: {ex.Message}", "CognitiveLayer")
                Return 1 ' Fallback to session 1
            End Try
        End Function

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' Initialize all cognitive systems
        ''' </summary>
        Private Sub InitializeCognitiveSystems()
            ' Create Working Memory Buffer
            _workingMemory = New WorkingMemoryBuffer(_coordinator, maxSize:=50)
            _workingMemory.Enabled = _enableWorkingMemory

            ' Create Habit Loop Analyzer (sequence length 2 = detects short cycles like "Play?Stop")
            _habitAnalyzer = New HabitLoopAnalyzer(_coordinator, sequenceLength:=2, habitThreshold:=3)
            _habitAnalyzer.Enabled = _enableHabitAnalysis

            ' Create Attention Spotlight
            _attentionSpotlight = New AttentionSpotlight(_coordinator, maxHistorySize:=1000)
            _attentionSpotlight.Enabled = _enableAttentionTracking

            ' Create Conflict Detector
            _conflictDetector = New ConflictDetector(_coordinator)
            _conflictDetector.Enabled = _enableConflictDetection

            ' Create Narrative Generator (depends on all other systems)
            _narrativeGenerator = New NarrativeGenerator(_coordinator, _workingMemory, _habitAnalyzer, _attentionSpotlight, _conflictDetector)
            _narrativeGenerator.Enabled = True

            ' v2.0 Systems (depends on v1.x)
            If _enableV2Features Then
                _predictionEngine = New PredictionEngine(_coordinator, _habitAnalyzer)
                _predictionEngine.Enabled = True

                _anomalyDetector = New AnomalyDetector(_coordinator, _habitAnalyzer, _workingMemory)
                _anomalyDetector.Enabled = True

                _adaptiveThresholds = New AdaptiveThresholdManager(_coordinator, _habitAnalyzer)
                _adaptiveThresholds.Enabled = True

                Utils.Logger.Instance.Info("? v2.0 Introspective Engine ENABLED!", "CognitiveLayer")
            End If

            Utils.Logger.Instance.Info("Cognitive systems initialized: v1.x + v2.0 features", "CognitiveLayer")
        End Sub

        ''' <summary>
        ''' Subscribe to state machine events
        ''' </summary>
        Private Sub SubscribeToEvents()
            ' Subscribe to GlobalStateMachine
            AddHandler _coordinator.GlobalStateMachine.StateChanged, AddressOf OnGlobalStateChanged

            ' Subscribe to SSMs
            AddHandler _coordinator.RecordingManagerSSM.StateChanged, AddressOf OnRecordingStateChanged
            AddHandler _coordinator.PlaybackSSM.StateChanged, AddressOf OnPlaybackStateChanged
            AddHandler _coordinator.UIStateMachine.StateChanged, AddressOf OnUIStateChanged

            ' DSPThreadSSM might not exist yet (created after microphone arming)
            If _coordinator.DSPThreadSSM IsNot Nothing Then
                AddHandler _coordinator.DSPThreadSSM.StateChanged, AddressOf OnDSPStateChanged
            End If

            ' Initialize attention spotlight (subscribes internally)
            _attentionSpotlight.Initialize(_coordinator)

            ' Initialize habit analyzer (subscribes internally)
            _habitAnalyzer.Initialize(_coordinator)

            ' Initialize conflict detector (subscribes internally)
            _conflictDetector.Initialize(_coordinator)

            ' Initialize narrative generator (subscribes internally)
            _narrativeGenerator.Initialize(_coordinator)

            ' v2.0 Initialization
            If _enableV2Features Then
                _predictionEngine.Initialize(_coordinator)
                _anomalyDetector.Initialize(_coordinator)
                _adaptiveThresholds.Initialize(_coordinator)
            End If

            Utils.Logger.Instance.Info("Event subscriptions established for all cognitive systems", "CognitiveLayer")
        End Sub

        ''' <summary>
        ''' Unsubscribe from events
        ''' </summary>
        Private Sub UnsubscribeFromEvents()
            Try
                ' Unsubscribe from GlobalStateMachine
                RemoveHandler _coordinator.GlobalStateMachine.StateChanged, AddressOf OnGlobalStateChanged

                ' Unsubscribe from SSMs
                RemoveHandler _coordinator.RecordingManagerSSM.StateChanged, AddressOf OnRecordingStateChanged
                RemoveHandler _coordinator.PlaybackSSM.StateChanged, AddressOf OnPlaybackStateChanged
                RemoveHandler _coordinator.UIStateMachine.StateChanged, AddressOf OnUIStateChanged

                If _coordinator.DSPThreadSSM IsNot Nothing Then
                    RemoveHandler _coordinator.DSPThreadSSM.StateChanged, AddressOf OnDSPStateChanged
                End If

                Utils.Logger.Instance.Info("Event subscriptions removed", "CognitiveLayer")
            Catch ex As Exception
                Utils.Logger.Instance.Warning($"Error unsubscribing from events: {ex.Message}", "CognitiveLayer")
            End Try
        End Sub

#End Region

#Region "Event Handlers"

        ''' <summary>
        ''' Handle GlobalStateMachine state changes
        ''' </summary>
        Private Sub OnGlobalStateChanged(sender As Object, e As StateChangedEventArgs(Of GlobalState))
            ' Working Memory captures all transitions
            ' (Already happens via existing GetTransitionHistory() access)

            ' Habit Analyzer is already subscribed
            ' Attention Spotlight is already subscribed
        End Sub

        ''' <summary>
        ''' Handle RecordingManagerSSM state changes
        ''' </summary>
        Private Sub OnRecordingStateChanged(sender As Object, e As Object)
            ' SSM events (attention tracking happens in AttentionSpotlight)
        End Sub

        ''' <summary>
        ''' Handle PlaybackSSM state changes
        ''' </summary>
        Private Sub OnPlaybackStateChanged(sender As Object, e As Object)
            ' SSM events (attention tracking happens in AttentionSpotlight)
        End Sub

        ''' <summary>
        ''' Handle UIStateMachine state changes
        ''' </summary>
        Private Sub OnUIStateChanged(sender As Object, e As Object)
            ' SSM events (attention tracking happens in AttentionSpotlight)
        End Sub

        ''' <summary>
        ''' Handle DSPThreadSSM state changes
        ''' </summary>
        Private Sub OnDSPStateChanged(sender As Object, e As Object)
            ' SSM events (attention tracking happens in AttentionSpotlight)
        End Sub

#End Region

#Region "Auto-Export Timer"

        ''' <summary>
        ''' Start auto-export timer (exports every 5 seconds)
        ''' </summary>
        Private Sub StartAutoExportTimer()
            Try
                _autoExportTimer = New System.Timers.Timer(_autoExportIntervalMs)
                _autoExportTimer.AutoReset = True
                AddHandler _autoExportTimer.Elapsed, AddressOf OnAutoExportTimerElapsed
                _autoExportTimer.Start()

                Utils.Logger.Instance.Info($"Auto-export timer started (every {_autoExportIntervalMs / 1000}s)", "CognitiveLayer")
            Catch ex As Exception
                Utils.Logger.Instance.Error("Failed to start auto-export timer", ex, "CognitiveLayer")
            End Try
        End Sub

        ''' <summary>
        ''' Stop auto-export timer
        ''' </summary>
        Private Sub StopAutoExportTimer()
            Try
                If _autoExportTimer IsNot Nothing Then
                    RemoveHandler _autoExportTimer.Elapsed, AddressOf OnAutoExportTimerElapsed
                    _autoExportTimer.Stop()
                    _autoExportTimer.Dispose()
                    _autoExportTimer = Nothing

                    Utils.Logger.Instance.Info("Auto-export timer stopped", "CognitiveLayer")
                End If
            Catch ex As Exception
                Utils.Logger.Instance.Warning($"Error stopping auto-export timer: {ex.Message}", "CognitiveLayer")
            End Try
        End Sub

        ''' <summary>
        ''' Auto-export timer elapsed handler
        ''' </summary>
        Private Sub OnAutoExportTimerElapsed(sender As Object, e As System.Timers.ElapsedEventArgs)
            Try
                ' Only export if enough time has passed (avoid rapid exports)
                Dim elapsed = (DateTime.Now - _lastExportTime).TotalMilliseconds
                If elapsed < _autoExportIntervalMs Then
                    Return
                End If

                _lastExportTime = DateTime.Now

                ' Export to log (background thread - safe)
                ExportToLog()

            Catch ex As Exception
                ' Don't let timer errors crash the system
                Utils.Logger.Instance.Debug($"Auto-export error: {ex.Message}", "CognitiveLayer")
            End Try
        End Sub

#End Region

#Region "IDisposable Implementation"

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _disposed Then
                If disposing Then
            ' Unsubscribe from events
            UnsubscribeFromEvents()

            ' Stop auto-export timer
            StopAutoExportTimer()

            ' PHASE 6 FIX (Issue #4): REMOVED duplicate ExportToLog() call
            ' Auto-export timer already saves every 5s, no need for final export
            ' This was creating duplicate session files with same data
            
            ' Final export removed - timer handles all exports
            ' Try
            '     ExportToLog()
            ' Catch
            '     ' Ignore errors during shutdown
            ' End Try

            ' Dispose cognitive systems
            If _workingMemory IsNot Nothing Then
                _workingMemory.Dispose()
            End If

                    If _habitAnalyzer IsNot Nothing Then
                        _habitAnalyzer.Dispose()
                    End If

                    If _attentionSpotlight IsNot Nothing Then
                        _attentionSpotlight.Dispose()
                    End If

                    If _conflictDetector IsNot Nothing Then
                        _conflictDetector.Dispose()
                    End If

                    If _narrativeGenerator IsNot Nothing Then
                        _narrativeGenerator.Dispose()
                    End If

                    Utils.Logger.Instance.Info("CognitiveLayer disposed", "CognitiveLayer")
                End If

                _disposed = True
            End If
        End Sub

#End Region

    End Class

#Region "Configuration Classes"

    ''' <summary>
    ''' Configuration for CognitiveLayer
    ''' </summary>
    Public Class CognitiveConfig
        ''' <summary>Enable Working Memory Buffer</summary>
        Public Property EnableWorkingMemory As Boolean = True

        ''' <summary>Enable Habit Loop Analyzer</summary>
        Public Property EnableHabitAnalysis As Boolean = True

        ''' <summary>Enable Attention Spotlight</summary>
        Public Property EnableAttentionTracking As Boolean = True

        ''' <summary>Enable Conflict Detection</summary>
        Public Property EnableConflictDetection As Boolean = True

        ''' <summary>Maximum working memory size</summary>
        Public Property WorkingMemoryMaxSize As Integer = 50

        ''' <summary>Habit sequence length</summary>
        Public Property HabitSequenceLength As Integer = 5

        ''' <summary>Habit frequency threshold</summary>
        Public Property HabitThreshold As Integer = 3

        ''' <summary>Attention history size</summary>
        Public Property AttentionHistorySize As Integer = 1000

        ''' <summary>
        ''' Creates default configuration
        ''' </summary>
        Public Shared Function CreateDefault() As CognitiveConfig
            Return New CognitiveConfig()
        End Function

        ''' <summary>
        ''' Creates minimal configuration (low memory)
        ''' </summary>
        Public Shared Function CreateMinimal() As CognitiveConfig
            Return New CognitiveConfig With {
                .EnableWorkingMemory = True,
                .EnableHabitAnalysis = False,
                .EnableAttentionTracking = False,
                .EnableConflictDetection = True,
                .WorkingMemoryMaxSize = 20
            }
        End Function

        ''' <summary>
        ''' Creates full configuration (all systems enabled)
        ''' </summary>
        Public Shared Function CreateFull() As CognitiveConfig
            Return New CognitiveConfig With {
                .EnableWorkingMemory = True,
                .EnableHabitAnalysis = True,
                .EnableAttentionTracking = True,
                .WorkingMemoryMaxSize = 100,
                .HabitSequenceLength = 7,
                .AttentionHistorySize = 2000
            }
        End Function
    End Class

    ''' <summary>
    ''' Comprehensive cognitive report
    ''' </summary>
    Public Structure CognitiveReport
        ''' <summary>Report timestamp</summary>
        Public Timestamp As DateTime

        ''' <summary>Working Memory statistics</summary>
        Public WorkingMemoryStats As Object

        ''' <summary>Habit analysis statistics</summary>
        Public HabitStats As Object

        ''' <summary>Attention tracking statistics</summary>
        Public AttentionStats As Object

        ''' <summary>Conflict detection statistics</summary>
        Public ConflictStats As Object
    End Structure

#End Region

End Namespace
