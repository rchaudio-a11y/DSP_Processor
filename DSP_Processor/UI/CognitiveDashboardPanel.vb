Imports System.Windows.Forms
Imports System.Drawing
Imports DSP_Processor.Cognitive

Namespace UI

    ''' <summary>
    ''' Cognitive Dashboard Panel - v1.x Visualization
    ''' Displays Working Memory, Habit Analysis, and Attention Tracking data
    ''' UserControl - can be added to TabControl or docked
    ''' </summary>
    Public Class CognitiveDashboardPanel
        Inherits UserControl

        Private _cognitiveLayer As CognitiveLayer
        Private _refreshTimer As Timer

        ' UI Controls
        Private WithEvents _refreshButton As Button
        Private WithEvents _exportButton As Button
        Private WithEvents _autoRefreshCheckbox As CheckBox
        Private _statusLabel As Label

        ' Working Memory Section
        Private _workingMemoryGroup As GroupBox
        Private _workingMemoryList As ListView
        Private _workingMemoryCountLabel As Label

        ' Habit Analysis Section
        Private _habitGroup As GroupBox
        Private _habitTextBox As TextBox
        Private _habitStatsLabel As Label

        ' Attention Section
        Private _attentionGroup As GroupBox
        Private _attentionTextBox As TextBox
        Private _attentionStatsLabel As Label

        Public Sub New()
            InitializeComponent()
            InitializeControls()
            LayoutControls()
        End Sub

        ''' <summary>
        ''' Initialize the dashboard with cognitive layer
        ''' </summary>
        Public Sub Initialize(cognitiveLayer As CognitiveLayer)
            If cognitiveLayer Is Nothing Then
                Throw New ArgumentNullException(NameOf(cognitiveLayer))
            End If

            _cognitiveLayer = cognitiveLayer

            ' Initial refresh
            RefreshData()

            _statusLabel.Text = "Cognitive Dashboard initialized"
            _statusLabel.ForeColor = Color.Green
        End Sub

#Region "Control Initialization"

        Private Sub InitializeControls()
            ' Set panel properties
            Me.BackColor = Color.FromArgb(240, 240, 240)
            Me.Padding = New Padding(10)

            ' Status label
            _statusLabel = New Label With {
                .Dock = DockStyle.Top,
                .Height = 25,
                .Text = "Cognitive Dashboard - Not initialized",
                .ForeColor = Color.Gray,
                .Font = New Font("Segoe UI", 9, FontStyle.Italic)
            }

            ' Button panel
            Dim buttonPanel = New FlowLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 40,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(0, 5, 0, 5)
            }

            _refreshButton = New Button With {
                .Text = "?? Refresh",
                .Width = 100,
                .Height = 30
            }

            _exportButton = New Button With {
                .Text = "?? Export to Log",
                .Width = 120,
                .Height = 30
            }

            _autoRefreshCheckbox = New CheckBox With {
                .Text = "Auto-refresh (1s)",
                .Width = 130,
                .Height = 30,
                .Checked = False
            }

            buttonPanel.Controls.Add(_refreshButton)
            buttonPanel.Controls.Add(_exportButton)
            buttonPanel.Controls.Add(_autoRefreshCheckbox)

            ' Working Memory Section
            _workingMemoryGroup = New GroupBox With {
                .Text = "Working Memory (Recent Transitions)",
                .Dock = DockStyle.Top,
                .Height = 250,
                .Padding = New Padding(10)
            }

            _workingMemoryCountLabel = New Label With {
                .Dock = DockStyle.Top,
                .Height = 20,
                .Text = "Count: 0",
                .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            }

            _workingMemoryList = New ListView With {
                .Dock = DockStyle.Fill,
                .View = View.Details,
                .FullRowSelect = True,
                .GridLines = True,
                .Font = New Font("Consolas", 8)
            }

            _workingMemoryList.Columns.Add("Time", 120)
            _workingMemoryList.Columns.Add("TransitionID", 200)
            _workingMemoryList.Columns.Add("Transition", 300)
            _workingMemoryList.Columns.Add("Reason", 300)

            _workingMemoryGroup.Controls.Add(_workingMemoryList)
            _workingMemoryGroup.Controls.Add(_workingMemoryCountLabel)

            ' Habit Analysis Section
            _habitGroup = New GroupBox With {
                .Text = "Habit Loop Analysis",
                .Dock = DockStyle.Top,
                .Height = 200,
                .Padding = New Padding(10)
            }

            _habitStatsLabel = New Label With {
                .Dock = DockStyle.Top,
                .Height = 20,
                .Text = "Patterns: 0 | Habits: 0",
                .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            }

            _habitTextBox = New TextBox With {
                .Dock = DockStyle.Fill,
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical,
                .ReadOnly = True,
                .Font = New Font("Consolas", 8),
                .BackColor = Color.White
            }

            _habitGroup.Controls.Add(_habitTextBox)
            _habitGroup.Controls.Add(_habitStatsLabel)

            ' Attention Section
            _attentionGroup = New GroupBox With {
                .Text = "Attention Spotlight",
                .Dock = DockStyle.Fill,
                .Padding = New Padding(10)
            }

            _attentionStatsLabel = New Label With {
                .Dock = DockStyle.Top,
                .Height = 20,
                .Text = "Active: None",
                .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            }

            _attentionTextBox = New TextBox With {
                .Dock = DockStyle.Fill,
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical,
                .ReadOnly = True,
                .Font = New Font("Consolas", 8),
                .BackColor = Color.White
            }

            _attentionGroup.Controls.Add(_attentionTextBox)
            _attentionGroup.Controls.Add(_attentionStatsLabel)

            ' Auto-refresh timer
            _refreshTimer = New Timer With {
                .Interval = 1000
            }
            AddHandler _refreshTimer.Tick, AddressOf OnRefreshTimerTick
        End Sub

        Private Sub LayoutControls()
            ' Add controls in order (top to bottom)
            Me.Controls.Add(_attentionGroup) ' Fill remaining space
            Me.Controls.Add(_habitGroup) ' Fixed height
            Me.Controls.Add(_workingMemoryGroup) ' Fixed height
            Me.Controls.Add(Me._statusLabel) ' Top
            
            ' Create button panel container
            Dim buttonPanel = New FlowLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 40,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(0, 5, 0, 5)
            }
            
            buttonPanel.Controls.Add(_refreshButton)
            buttonPanel.Controls.Add(_exportButton)
            buttonPanel.Controls.Add(_autoRefreshCheckbox)
            
            Me.Controls.Add(buttonPanel) ' Top (below status)
        End Sub

#End Region

#Region "Data Refresh"

        ''' <summary>
        ''' Refresh all cognitive data
        ''' </summary>
        Public Sub RefreshData()
            If _cognitiveLayer Is Nothing Then
                _statusLabel.Text = "Not initialized - call Initialize() first"
                _statusLabel.ForeColor = Color.Red
                Return
            End If

            Try
                RefreshWorkingMemory()
                RefreshHabitAnalysis()
                RefreshAttentionSpotlight()

                _statusLabel.Text = $"Last refresh: {DateTime.Now:HH:mm:ss}"
                _statusLabel.ForeColor = Color.Green
            Catch ex As Exception
                _statusLabel.Text = $"Refresh error: {ex.Message}"
                _statusLabel.ForeColor = Color.Red
            End Try
        End Sub

        Private Sub RefreshWorkingMemory()
            _workingMemoryList.Items.Clear()

            If Not _cognitiveLayer.WorkingMemory.Enabled Then
                _workingMemoryCountLabel.Text = "Working Memory: DISABLED"
                Return
            End If

            ' Get recent transitions
            Dim recent = _cognitiveLayer.WorkingMemory.GetRecentTransitions(20) ' Last 20

            _workingMemoryCountLabel.Text = $"Count: {_cognitiveLayer.WorkingMemory.Count} (showing last {recent.Count})"

            For Each transition In recent
                Dim item = New ListViewItem(transition.Timestamp.ToString("HH:mm:ss.fff"))
                item.SubItems.Add(transition.TransitionID)
                item.SubItems.Add($"{transition.OldStateUID} ? {transition.NewStateUID}")
                item.SubItems.Add(transition.Reason)
                _workingMemoryList.Items.Add(item)
            Next

            ' Auto-scroll to top (most recent)
            If _workingMemoryList.Items.Count > 0 Then
                _workingMemoryList.Items(0).EnsureVisible()
            End If
        End Sub

        Private Sub RefreshHabitAnalysis()
            If Not _cognitiveLayer.HabitAnalyzer.Enabled Then
                _habitStatsLabel.Text = "Habit Analysis: DISABLED"
                _habitTextBox.Text = "Habit analysis is disabled."
                Return
            End If

            ' Get statistics
            Dim stats = CType(_cognitiveLayer.HabitAnalyzer.GetStatistics(), Object)
            _habitStatsLabel.Text = $"Patterns: {stats.TotalPatterns} | Habits: {stats.TotalHabits} | Most Common: {stats.MostCommonHabit}"

            ' Get habit report
            Dim report = _cognitiveLayer.HabitAnalyzer.GenerateHabitReport()
            _habitTextBox.Text = report
        End Sub

        Private Sub RefreshAttentionSpotlight()
            If Not _cognitiveLayer.AttentionSpotlight.Enabled Then
                _attentionStatsLabel.Text = "Attention: DISABLED"
                _attentionTextBox.Text = "Attention tracking is disabled."
                Return
            End If

            ' Get statistics
            Dim stats = CType(_cognitiveLayer.AttentionSpotlight.GetStatistics(), Object)
            _attentionStatsLabel.Text = $"Active: {stats.ActiveSubsystem} ({stats.TimeSinceLastActivity:F1}s ago)"

            ' Get attention report
            Dim report = _cognitiveLayer.AttentionSpotlight.GenerateAttentionReport(TimeSpan.FromSeconds(10))
            _attentionTextBox.Text = report
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub _refreshButton_Click(sender As Object, e As EventArgs) Handles _refreshButton.Click
            RefreshData()
        End Sub

        Private Sub _exportButton_Click(sender As Object, e As EventArgs) Handles _exportButton.Click
            If _cognitiveLayer Is Nothing Then
                MessageBox.Show("Cognitive Dashboard not initialized", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            Try
                _cognitiveLayer.ExportToLog()
                MessageBox.Show("Cognitive data exported to log file", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub _autoRefreshCheckbox_CheckedChanged(sender As Object, e As EventArgs) Handles _autoRefreshCheckbox.CheckedChanged
            _refreshTimer.Enabled = _autoRefreshCheckbox.Checked

            If _autoRefreshCheckbox.Checked Then
                _statusLabel.Text = "Auto-refresh enabled (1s interval)"
            Else
                _statusLabel.Text = "Auto-refresh disabled"
            End If
        End Sub

        Private Sub OnRefreshTimerTick(sender As Object, e As EventArgs)
            RefreshData()
        End Sub

#End Region

#Region "Designer Support"

        Private components As System.ComponentModel.IContainer

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If components IsNot Nothing Then
                    components.Dispose()
                End If

                If _refreshTimer IsNot Nothing Then
                    _refreshTimer.Stop()
                    _refreshTimer.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        Private Sub InitializeComponent()
            Me.components = New System.ComponentModel.Container()
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Name = "CognitiveDashboardPanel"
            Me.Size = New System.Drawing.Size(800, 600)
        End Sub

#End Region

    End Class

End Namespace
