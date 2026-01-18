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

        ' Configuration
        Private _enableWorkingMemory As Boolean = True
        Private _enableHabitAnalysis As Boolean = True
        Private _enableAttentionTracking As Boolean = True

        ''' <summary>
        ''' Creates cognitive layer coordinator
        ''' </summary>
        ''' <param name="coordinator">State coordinator to observe</param>
        Public Sub New(coordinator As StateCoordinator)
            If coordinator Is Nothing Then
                Throw New ArgumentNullException(NameOf(coordinator))
            End If

            _coordinator = coordinator

            ' Initialize cognitive systems
            InitializeCognitiveSystems()

            ' Subscribe to events
            SubscribeToEvents()

            Utils.Logger.Instance.Info("CognitiveLayer initialized with v1.x systems", "CognitiveLayer")
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

            ' Update system states
            _workingMemory.Enabled = _enableWorkingMemory
            _habitAnalyzer.Enabled = _enableHabitAnalysis
            _attentionSpotlight.Enabled = _enableAttentionTracking

            Utils.Logger.Instance.Info($"CognitiveLayer configured: Memory={_enableWorkingMemory}, Habits={_enableHabitAnalysis}, Attention={_enableAttentionTracking}", "CognitiveLayer")
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
                    .AttentionStats = If(_attentionSpotlight.Enabled, _attentionSpotlight.GetStatistics(), Nothing)
                }

                Return report
            End SyncLock
        End Function

        ''' <summary>
        ''' Resets all cognitive systems
        ''' </summary>
        Public Sub ResetAll()
            SyncLock _lock
                If _workingMemory IsNot Nothing Then _workingMemory.Reset()
                If _habitAnalyzer IsNot Nothing Then _habitAnalyzer.Reset()
                If _attentionSpotlight IsNot Nothing Then _attentionSpotlight.Reset()

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

            Return summary.ToString()
        End Function

        ''' <summary>
        ''' Exports all cognitive data to logs
        ''' </summary>
        Public Sub ExportToLog()
            ' Write to separate cognitive log file
            Dim timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss")
            Dim logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs")
            Dim logFile = System.IO.Path.Combine(logDir, $"Cognitive_{timestamp}.log")

            Try
                ' Ensure Logs directory exists
                If Not System.IO.Directory.Exists(logDir) Then
                    System.IO.Directory.CreateDirectory(logDir)
                End If

                ' Build comprehensive cognitive report
                Dim report As New System.Text.StringBuilder()
                report.AppendLine("?????????????????????????????????????????????????????????")
                report.AppendLine($"   COGNITIVE LAYER EXPORT - {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                report.AppendLine("?????????????????????????????????????????????????????????")
                report.AppendLine()

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

                report.AppendLine("?????????????????????????????????????????????????????????")
                report.AppendLine("   END COGNITIVE EXPORT")
                report.AppendLine("?????????????????????????????????????????????????????????")

                ' Write to dedicated cognitive log file
                System.IO.File.WriteAllText(logFile, report.ToString())

                Utils.Logger.Instance.Info($"Cognitive data exported to: {System.IO.Path.GetFileName(logFile)}", "CognitiveLayer")

            Catch ex As Exception
                Utils.Logger.Instance.Error($"Failed to export cognitive data: {ex.Message}", ex, "CognitiveLayer")
                Throw
            End Try
        End Sub

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' Initialize all cognitive systems
        ''' </summary>
        Private Sub InitializeCognitiveSystems()
            ' Create Working Memory Buffer
            _workingMemory = New WorkingMemoryBuffer(_coordinator, maxSize:=50)
            _workingMemory.Enabled = _enableWorkingMemory

            ' Create Habit Loop Analyzer
            _habitAnalyzer = New HabitLoopAnalyzer(_coordinator, sequenceLength:=5, habitThreshold:=3)
            _habitAnalyzer.Enabled = _enableHabitAnalysis

            ' Create Attention Spotlight
            _attentionSpotlight = New AttentionSpotlight(_coordinator, maxHistorySize:=1000)
            _attentionSpotlight.Enabled = _enableAttentionTracking

            Utils.Logger.Instance.Info("Cognitive systems initialized: WorkingMemory, HabitAnalyzer, AttentionSpotlight", "CognitiveLayer")
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

            Utils.Logger.Instance.Info("Event subscriptions established for all state machines", "CognitiveLayer")
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
    End Structure

#End Region

End Namespace
