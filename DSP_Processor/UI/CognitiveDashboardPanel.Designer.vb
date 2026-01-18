<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class CognitiveDashboardPanel
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        panelButtons = New FlowLayoutPanel()
        btnRefresh = New Button()
        btnExport = New Button()
        btnSummary = New Button()
        chkAutoRefresh = New CheckBox()
        lblStatus = New Label()
        groupWorkingMemory = New GroupBox()
        lstWorkingMemory = New ListView()
        colTime = New ColumnHeader()
        colStateMachine = New ColumnHeader()
        colTransition = New ColumnHeader()
        lblWorkingMemoryCount = New Label()
        groupHabits = New GroupBox()
        txtHabits = New TextBox()
        lblHabitStats = New Label()
        groupAttention = New GroupBox()
        txtAttention = New TextBox()
        lblAttentionStats = New Label()
        timerRefresh = New Timer(components)
        panelButtons.SuspendLayout()
        groupWorkingMemory.SuspendLayout()
        groupHabits.SuspendLayout()
        groupAttention.SuspendLayout()
        SuspendLayout()
        ' 
        ' panelButtons
        ' 
        panelButtons.Controls.Add(btnRefresh)
        panelButtons.Controls.Add(btnExport)
        panelButtons.Controls.Add(btnSummary)
        panelButtons.Controls.Add(chkAutoRefresh)
        panelButtons.Dock = DockStyle.Top
        panelButtons.Location = New Point(10, 10)
        panelButtons.Name = "panelButtons"
        panelButtons.Padding = New Padding(0, 5, 0, 5)
        panelButtons.Size = New Size(1066, 40)
        panelButtons.TabIndex = 0
        ' 
        ' btnRefresh
        ' 
        btnRefresh.Location = New Point(3, 8)
        btnRefresh.Name = "btnRefresh"
        btnRefresh.Size = New Size(90, 30)
        btnRefresh.TabIndex = 0
        btnRefresh.Text = "?? Refresh"
        btnRefresh.UseVisualStyleBackColor = True
        ' 
        ' btnExport
        ' 
        btnExport.Location = New Point(99, 8)
        btnExport.Name = "btnExport"
        btnExport.Size = New Size(100, 30)
        btnExport.TabIndex = 1
        btnExport.Text = "?? Export"
        btnExport.UseVisualStyleBackColor = True
        ' 
        ' btnSummary
        ' 
        btnSummary.Location = New Point(205, 8)
        btnSummary.Name = "btnSummary"
        btnSummary.Size = New Size(100, 30)
        btnSummary.TabIndex = 2
        btnSummary.Text = "?? Summary"
        btnSummary.UseVisualStyleBackColor = True
        ' 
        ' chkAutoRefresh
        ' 
        chkAutoRefresh.AutoSize = True
        chkAutoRefresh.Location = New Point(311, 8)
        chkAutoRefresh.Name = "chkAutoRefresh"
        chkAutoRefresh.Size = New Size(142, 24)
        chkAutoRefresh.TabIndex = 3
        chkAutoRefresh.Text = "Auto-refresh (1s)"
        chkAutoRefresh.UseVisualStyleBackColor = True
        ' 
        ' lblStatus
        ' 
        lblStatus.Dock = DockStyle.Top
        lblStatus.Font = New Font("Segoe UI", 10F, FontStyle.Bold)
        lblStatus.Location = New Point(10, 50)
        lblStatus.Name = "lblStatus"
        lblStatus.Padding = New Padding(5)
        lblStatus.Size = New Size(1066, 30)
        lblStatus.TabIndex = 1
        lblStatus.Text = "Cognitive Layer Status"
        ' 
        ' groupWorkingMemory
        ' 
        groupWorkingMemory.Controls.Add(lstWorkingMemory)
        groupWorkingMemory.Controls.Add(lblWorkingMemoryCount)
        groupWorkingMemory.Dock = DockStyle.Top
        groupWorkingMemory.Location = New Point(10, 80)
        groupWorkingMemory.Name = "groupWorkingMemory"
        groupWorkingMemory.Padding = New Padding(10)
        groupWorkingMemory.Size = New Size(1066, 250)
        groupWorkingMemory.TabIndex = 2
        groupWorkingMemory.TabStop = False
        groupWorkingMemory.Text = "Working Memory (Recent Transitions)"
        ' 
        ' lstWorkingMemory
        ' 
        lstWorkingMemory.Columns.AddRange(New ColumnHeader() {colTime, colStateMachine, colTransition})
        lstWorkingMemory.Dock = DockStyle.Fill
        lstWorkingMemory.FullRowSelect = True
        lstWorkingMemory.GridLines = True
        lstWorkingMemory.Location = New Point(10, 30)
        lstWorkingMemory.Name = "lstWorkingMemory"
        lstWorkingMemory.Size = New Size(1046, 190)
        lstWorkingMemory.TabIndex = 0
        lstWorkingMemory.UseCompatibleStateImageBehavior = False
        lstWorkingMemory.View = View.Details
        ' 
        ' colTime
        ' 
        colTime.Text = "Time"
        colTime.Width = 120
        ' 
        ' colStateMachine
        ' 
        colStateMachine.Text = "State Machine"
        colStateMachine.Width = 150
        ' 
        ' colTransition
        ' 
        colTransition.Text = "Transition"
        colTransition.Width = 450
        ' 
        ' lblWorkingMemoryCount
        ' 
        lblWorkingMemoryCount.Dock = DockStyle.Bottom
        lblWorkingMemoryCount.Location = New Point(10, 220)
        lblWorkingMemoryCount.Name = "lblWorkingMemoryCount"
        lblWorkingMemoryCount.Size = New Size(1046, 20)
        lblWorkingMemoryCount.TabIndex = 1
        lblWorkingMemoryCount.Text = "Count: 0"
        ' 
        ' groupHabits
        ' 
        groupHabits.Controls.Add(txtHabits)
        groupHabits.Controls.Add(lblHabitStats)
        groupHabits.Dock = DockStyle.Top
        groupHabits.Location = New Point(10, 330)
        groupHabits.Name = "groupHabits"
        groupHabits.Padding = New Padding(10)
        groupHabits.Size = New Size(1066, 200)
        groupHabits.TabIndex = 3
        groupHabits.TabStop = False
        groupHabits.Text = "Habit Loop Analysis"
        ' 
        ' txtHabits
        ' 
        txtHabits.BackColor = Color.White
        txtHabits.Dock = DockStyle.Fill
        txtHabits.Font = New Font("Consolas", 9F)
        txtHabits.Location = New Point(10, 30)
        txtHabits.Multiline = True
        txtHabits.Name = "txtHabits"
        txtHabits.ReadOnly = True
        txtHabits.ScrollBars = ScrollBars.Vertical
        txtHabits.Size = New Size(1046, 140)
        txtHabits.TabIndex = 0
        ' 
        ' lblHabitStats
        ' 
        lblHabitStats.Dock = DockStyle.Bottom
        lblHabitStats.Location = New Point(10, 170)
        lblHabitStats.Name = "lblHabitStats"
        lblHabitStats.Size = New Size(1046, 20)
        lblHabitStats.TabIndex = 1
        lblHabitStats.Text = "Habits: 0"
        ' 
        ' groupAttention
        ' 
        groupAttention.Controls.Add(txtAttention)
        groupAttention.Controls.Add(lblAttentionStats)
        groupAttention.Dock = DockStyle.Fill
        groupAttention.Location = New Point(10, 530)
        groupAttention.Name = "groupAttention"
        groupAttention.Padding = New Padding(10)
        groupAttention.Size = New Size(1066, 202)
        groupAttention.TabIndex = 4
        groupAttention.TabStop = False
        groupAttention.Text = "Attention Spotlight"
        ' 
        ' txtAttention
        ' 
        txtAttention.BackColor = Color.White
        txtAttention.Dock = DockStyle.Fill
        txtAttention.Font = New Font("Consolas", 9F)
        txtAttention.Location = New Point(10, 30)
        txtAttention.Multiline = True
        txtAttention.Name = "txtAttention"
        txtAttention.ReadOnly = True
        txtAttention.ScrollBars = ScrollBars.Vertical
        txtAttention.Size = New Size(1046, 142)
        txtAttention.TabIndex = 0
        ' 
        ' lblAttentionStats
        ' 
        lblAttentionStats.Dock = DockStyle.Bottom
        lblAttentionStats.Location = New Point(10, 172)
        lblAttentionStats.Name = "lblAttentionStats"
        lblAttentionStats.Size = New Size(1046, 20)
        lblAttentionStats.TabIndex = 1
        lblAttentionStats.Text = "Active: None"
        ' 
        ' timerRefresh
        ' 
        timerRefresh.Interval = 1000
        ' 
        ' CognitiveDashboardPanel
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        AutoScroll = True
        BackColor = Color.FromArgb(CByte(240), CByte(240), CByte(240))
        Controls.Add(groupAttention)
        Controls.Add(groupHabits)
        Controls.Add(groupWorkingMemory)
        Controls.Add(lblStatus)
        Controls.Add(panelButtons)
        Name = "CognitiveDashboardPanel"
        Padding = New Padding(10)
        Size = New Size(1086, 742)
        panelButtons.ResumeLayout(False)
        panelButtons.PerformLayout()
        groupWorkingMemory.ResumeLayout(False)
        groupHabits.ResumeLayout(False)
        groupHabits.PerformLayout()
        groupAttention.ResumeLayout(False)
        groupAttention.PerformLayout()
        ResumeLayout(False)

    End Sub

    Friend WithEvents panelButtons As FlowLayoutPanel
    Friend WithEvents btnRefresh As Button
    Friend WithEvents btnExport As Button
    Friend WithEvents btnSummary As Button
    Friend WithEvents chkAutoRefresh As CheckBox
    Friend WithEvents lblStatus As Label
    Friend WithEvents groupWorkingMemory As GroupBox
    Friend WithEvents lstWorkingMemory As ListView
    Friend WithEvents colTime As ColumnHeader
    Friend WithEvents colStateMachine As ColumnHeader
    Friend WithEvents colTransition As ColumnHeader
    Friend WithEvents lblWorkingMemoryCount As Label
    Friend WithEvents groupHabits As GroupBox
    Friend WithEvents txtHabits As TextBox
    Friend WithEvents lblHabitStats As Label
    Friend WithEvents groupAttention As GroupBox
    Friend WithEvents txtAttention As TextBox
    Friend WithEvents lblAttentionStats As Label
    Friend WithEvents timerRefresh As Timer

End Class
