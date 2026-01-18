Imports DSP_Processor.Cognitive

''' <summary>
''' Cognitive Dashboard Panel - Visualization for Cognitive Layer
''' NOW WITH DESIGNER SUPPORT! Edit visually like other panels!
''' </summary>
Partial Public Class CognitiveDashboardPanel
    Inherits UserControl

    Private _cognitiveLayer As CognitiveLayer

    Public Sub New()
        InitializeComponent()
    End Sub

    ''' <summary>
    ''' Initialize dashboard with cognitive layer
    ''' </summary>
    Public Sub Initialize(cognitiveLayer As CognitiveLayer)
        _cognitiveLayer = cognitiveLayer
        RefreshData()
    End Sub

    ''' <summary>
    ''' Refresh all dashboard data
    ''' </summary>
    Public Sub RefreshData()
        If _cognitiveLayer Is Nothing Then
            lblStatus.Text = "Cognitive Layer: Not Initialized"
            Return
        End If

        Try
            ' Update status label
            Dim report = _cognitiveLayer.GenerateReport()
            lblStatus.Text = $"Cognitive Layer: Active ({report.Timestamp:HH:mm:ss})"

            ' Refresh each section
            RefreshWorkingMemory()
            RefreshHabitAnalysis()
            RefreshAttentionSpotlight()

        Catch ex As Exception
            Utils.Logger.Instance.Error("Dashboard refresh failed", ex, "CognitiveDashboard")
        End Try
    End Sub

    Private Sub RefreshWorkingMemory()
        If Not _cognitiveLayer.WorkingMemory.Enabled Then
            lblWorkingMemoryCount.Text = "Working Memory: DISABLED"
            lstWorkingMemory.Items.Clear()
            Return
        End If

        ' Get recent transitions
        Dim transitions = _cognitiveLayer.WorkingMemory.GetRecentTransitions(20)
        lblWorkingMemoryCount.Text = $"Count: {transitions.Count}"

        ' Update list view
        lstWorkingMemory.Items.Clear()
        For Each trans In transitions
            Dim item As New ListViewItem(trans.Timestamp.ToString("HH:mm:ss.fff"))
            item.SubItems.Add(trans.TransitionID.Split("_"c)(0)) ' State machine name
            item.SubItems.Add($"{trans.TransitionID}: {trans.OldState} → {trans.NewState}")
            lstWorkingMemory.Items.Add(item)
        Next
    End Sub

    Private Sub RefreshHabitAnalysis()
        If Not _cognitiveLayer.HabitAnalyzer.Enabled Then
            lblHabitStats.Text = "Habit Analysis: DISABLED"
            txtHabits.Text = "Habit analysis is disabled."
            Return
        End If

        ' Get statistics
        Dim stats = CType(_cognitiveLayer.HabitAnalyzer.GetStatistics(), Object)
        lblHabitStats.Text = $"Total Habits: {stats.TotalHabits}"

        ' Get habit report
        Dim report = _cognitiveLayer.HabitAnalyzer.GenerateHabitReport()
        txtHabits.Text = report
    End Sub

    Private Sub RefreshAttentionSpotlight()
        If Not _cognitiveLayer.AttentionSpotlight.Enabled Then
            lblAttentionStats.Text = "Attention: DISABLED"
            txtAttention.Text = "Attention tracking is disabled."
            Return
        End If

        ' Get statistics
        Dim stats = CType(_cognitiveLayer.AttentionSpotlight.GetStatistics(), Object)
        lblAttentionStats.Text = $"Active: {stats.ActiveSubsystem} ({stats.TimeSinceLastActivity:F1}s ago)"

        ' Get attention report
        Dim report = _cognitiveLayer.AttentionSpotlight.GenerateAttentionReport(TimeSpan.FromSeconds(10))
        txtAttention.Text = report
    End Sub

#Region "Event Handlers"

    Private Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        RefreshData()
    End Sub

    Private Sub btnExport_Click(sender As Object, e As EventArgs) Handles btnExport.Click
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

    Private Sub btnSummary_Click(sender As Object, e As EventArgs) Handles btnSummary.Click
        If _cognitiveLayer Is Nothing Then
            MessageBox.Show("Cognitive Dashboard not initialized", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        Try
            Dim summary = _cognitiveLayer.GenerateSessionSummary()
            Dim form As New Form With {
                .Text = "Session Summary",
                .Width = 600,
                .Height = 450,
                .StartPosition = FormStartPosition.CenterParent,
                .MinimizeBox = False,
                .MaximizeBox = False
            }

            Dim textBox As New TextBox With {
                .Multiline = True,
                .ReadOnly = True,
                .ScrollBars = ScrollBars.Both,
                .Dock = DockStyle.Fill,
                .Font = New Font("Consolas", 10),
                .Text = summary
            }

            form.Controls.Add(textBox)
            form.ShowDialog()

        Catch ex As Exception
            MessageBox.Show($"Failed to generate summary: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub chkAutoRefresh_CheckedChanged(sender As Object, e As EventArgs) Handles chkAutoRefresh.CheckedChanged
        timerRefresh.Enabled = chkAutoRefresh.Checked
    End Sub

    Private Sub timerRefresh_Tick(sender As Object, e As EventArgs) Handles timerRefresh.Tick
        RefreshData()
    End Sub

#End Region

End Class
