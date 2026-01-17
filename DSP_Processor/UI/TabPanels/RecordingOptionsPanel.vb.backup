Imports DSP_Processor.Models
Imports DSP_Processor.UI

Namespace UI.TabPanels

    ''' <summary>
    ''' Recording options tab panel for configuring recording modes
    ''' </summary>
    Public Class RecordingOptionsPanel
        Inherits UserControl
        
        ' Mode selection
        Private grpRecordingMode As GroupBox
        Private radioManual As RadioButton
        Private radioTimed As RadioButton
        Private radioLoop As RadioButton
        Private lblModeDescription As Label
        
        ' Timed mode controls
        Private grpTimedOptions As GroupBox
        Private lblTimedDuration As Label
        Private numTimedHours As NumericUpDown
        Private numTimedMinutes As NumericUpDown
        Private numTimedSeconds As NumericUpDown
        Private lblTimedFormat As Label
        
        ' Loop mode controls
        Private grpLoopOptions As GroupBox
        Private lblLoopCount As Label
        Private numLoopCount As NumericUpDown
        Private lblLoopDuration As Label
        Private numLoopHours As NumericUpDown
        Private numLoopMinutes As NumericUpDown
        Private numLoopSeconds As NumericUpDown
        Private lblLoopDelay As Label
        Private numLoopDelay As NumericUpDown
        Private chkAutoIncrement As CheckBox
        
        ' Quick presets
        Private grpPresets As GroupBox
        Private btn30Sec As Button
        Private btn60Sec As Button
        Private btn5Takes As Button
        
        Public Event OptionsChanged As EventHandler(Of RecordingOptions)
        
        Private suppressEvents As Boolean = False
        
        Public Sub New()
            InitializeComponent()
        End Sub
        
        Private Sub InitializeComponent()
            CreateModeSelectionGroup()
            CreateTimedOptionsGroup()
            CreateLoopOptionsGroup()
            CreatePresetsGroup()
            LayoutControls()
            DarkTheme.ApplyToControl(Me)
        End Sub
        
        Private Sub CreateModeSelectionGroup()
            grpRecordingMode = New GroupBox() With {
                .Text = "Recording Mode",
                .Location = New Point(10, 10),
                .Size = New Size(320, 150)
            }
            
            radioManual = New RadioButton() With {
                .Text = "Manual - Record until stopped",
                .Location = New Point(10, 25),
                .Size = New Size(300, 25),
                .Checked = True
            }
            AddHandler radioManual.CheckedChanged, AddressOf OnModeChanged
            
            radioTimed = New RadioButton() With {
                .Text = "Timed - Record for specific duration",
                .Location = New Point(10, 55),
                .Size = New Size(300, 25)
            }
            AddHandler radioTimed.CheckedChanged, AddressOf OnModeChanged
            
            radioLoop = New RadioButton() With {
                .Text = "Loop - Multiple automatic takes",
                .Location = New Point(10, 85),
                .Size = New Size(300, 25)
            }
            AddHandler radioLoop.CheckedChanged, AddressOf OnModeChanged
            
            lblModeDescription = New Label() With {
                .Location = New Point(10, 115),
                .Size = New Size(300, 25),
                .ForeColor = Color.LightGray,
                .Text = "Record until manually stopped"
            }
            
            grpRecordingMode.Controls.AddRange({radioManual, radioTimed, radioLoop, lblModeDescription})
            Me.Controls.Add(grpRecordingMode)
        End Sub
        
        Private Sub CreateTimedOptionsGroup()
            grpTimedOptions = New GroupBox() With {
                .Text = "Timed Recording Options",
                .Location = New Point(10, 170),
                .Size = New Size(320, 100),
                .Enabled = False
            }
            
            lblTimedDuration = New Label() With {
                .Text = "Duration:",
                .Location = New Point(10, 25),
                .Size = New Size(70, 20)
            }
            
            numTimedHours = New NumericUpDown() With {
                .Location = New Point(80, 23),
                .Size = New Size(50, 25),
                .Maximum = 23,
                .Minimum = 0,
                .Value = 0
            }
            AddHandler numTimedHours.ValueChanged, AddressOf OnControlChanged
            
            numTimedMinutes = New NumericUpDown() With {
                .Location = New Point(140, 23),
                .Size = New Size(50, 25),
                .Maximum = 59,
                .Minimum = 0,
                .Value = 1
            }
            AddHandler numTimedMinutes.ValueChanged, AddressOf OnControlChanged
            
            numTimedSeconds = New NumericUpDown() With {
                .Location = New Point(200, 23),
                .Size = New Size(50, 25),
                .Maximum = 59,
                .Minimum = 0,
                .Value = 0
            }
            AddHandler numTimedSeconds.ValueChanged, AddressOf OnControlChanged
            
            lblTimedFormat = New Label() With {
                .Text = "HH : MM : SS",
                .Location = New Point(80, 50),
                .Size = New Size(170, 20),
                .ForeColor = Color.Gray
            }
            
            grpTimedOptions.Controls.AddRange({lblTimedDuration, numTimedHours, numTimedMinutes, numTimedSeconds, lblTimedFormat})
            Me.Controls.Add(grpTimedOptions)
        End Sub
        
        Private Sub CreateLoopOptionsGroup()
            grpLoopOptions = New GroupBox() With {
                .Text = "Loop Recording Options",
                .Location = New Point(10, 280),
                .Size = New Size(320, 180),
                .Enabled = False
            }
            
            lblLoopCount = New Label() With {
                .Text = "Number of takes:",
                .Location = New Point(10, 25),
                .Size = New Size(120, 20)
            }
            
            numLoopCount = New NumericUpDown() With {
                .Location = New Point(140, 23),
                .Size = New Size(70, 25),
                .Maximum = 100,
                .Minimum = 1,
                .Value = 3
            }
            AddHandler numLoopCount.ValueChanged, AddressOf OnControlChanged
            
            lblLoopDuration = New Label() With {
                .Text = "Duration per take:",
                .Location = New Point(10, 55),
                .Size = New Size(120, 20)
            }
            
            numLoopHours = New NumericUpDown() With {
                .Location = New Point(140, 53),
                .Size = New Size(40, 25),
                .Maximum = 23,
                .Minimum = 0,
                .Value = 0
            }
            AddHandler numLoopHours.ValueChanged, AddressOf OnControlChanged
            
            numLoopMinutes = New NumericUpDown() With {
                .Location = New Point(190, 53),
                .Size = New Size(40, 25),
                .Maximum = 59,
                .Minimum = 0,
                .Value = 0
            }
            AddHandler numLoopMinutes.ValueChanged, AddressOf OnControlChanged
            
            numLoopSeconds = New NumericUpDown() With {
                .Location = New Point(240, 53),
                .Size = New Size(40, 25),
                .Maximum = 59,
                .Minimum = 0,
                .Value = 30
            }
            AddHandler numLoopSeconds.ValueChanged, AddressOf OnControlChanged
            
            lblLoopDelay = New Label() With {
                .Text = "Delay between (sec):",
                .Location = New Point(10, 85),
                .Size = New Size(120, 20)
            }
            
            numLoopDelay = New NumericUpDown() With {
                .Location = New Point(140, 83),
                .Size = New Size(70, 25),
                .Maximum = 60,
                .Minimum = 0,
                .Value = 2
            }
            AddHandler numLoopDelay.ValueChanged, AddressOf OnControlChanged
            
            chkAutoIncrement = New CheckBox() With {
                .Text = "Auto-increment take numbers",
                .Location = New Point(10, 115),
                .Size = New Size(300, 25),
                .Checked = True
            }
            AddHandler chkAutoIncrement.CheckedChanged, AddressOf OnControlChanged
            
            grpLoopOptions.Controls.AddRange({lblLoopCount, numLoopCount, lblLoopDuration, 
                                               numLoopHours, numLoopMinutes, numLoopSeconds,
                                               lblLoopDelay, numLoopDelay, chkAutoIncrement})
            Me.Controls.Add(grpLoopOptions)
        End Sub
        
        Private Sub CreatePresetsGroup()
            grpPresets = New GroupBox() With {
                .Text = "Quick Presets",
                .Location = New Point(10, 470),
                .Size = New Size(320, 80)
            }
            
            btn30Sec = New Button() With {
                .Text = "30 Second Take",
                .Location = New Point(10, 25),
                .Size = New Size(95, 35)
            }
            AddHandler btn30Sec.Click, AddressOf btn30Sec_Click
            
            btn60Sec = New Button() With {
                .Text = "60 Second Take",
                .Location = New Point(112, 25),
                .Size = New Size(95, 35)
            }
            AddHandler btn60Sec.Click, AddressOf btn60Sec_Click
            
            btn5Takes = New Button() With {
                .Text = "5 × 30sec Loop",
                .Location = New Point(214, 25),
                .Size = New Size(95, 35)
            }
            AddHandler btn5Takes.Click, AddressOf btn5Takes_Click
            
            grpPresets.Controls.AddRange({btn30Sec, btn60Sec, btn5Takes})
            Me.Controls.Add(grpPresets)
        End Sub
        
        Private Sub LayoutControls()
            Me.AutoScroll = True
            Me.BackColor = Color.FromArgb(45, 45, 48)
        End Sub
        
        Public Function GetOptions() As RecordingOptions
            Dim options = New RecordingOptions()
            
            ' Determine mode
            If radioManual.Checked Then
                options.Mode = RecordingMode.Manual
            ElseIf radioTimed.Checked Then
                options.Mode = RecordingMode.Timed
                options.TimedDurationSeconds = CInt(numTimedHours.Value * 3600 + numTimedMinutes.Value * 60 + numTimedSeconds.Value)
            ElseIf radioLoop.Checked Then
                options.Mode = RecordingMode.LoopMode
                options.LoopCount = CInt(numLoopCount.Value)
                options.LoopDurationSeconds = CInt(numLoopHours.Value * 3600 + numLoopMinutes.Value * 60 + numLoopSeconds.Value)
                options.LoopDelaySeconds = CInt(numLoopDelay.Value)
                options.AutoIncrementTakeNumbers = chkAutoIncrement.Checked
            End If
            
            Return options
        End Function
        
        Public Sub LoadOptions(options As RecordingOptions)
            suppressEvents = True
            
            Try
                ' Set mode
                Select Case options.Mode
                    Case RecordingMode.Manual
                        radioManual.Checked = True
                    Case RecordingMode.Timed
                        radioTimed.Checked = True
                    Case RecordingMode.LoopMode
                        radioLoop.Checked = True
                End Select
                
                ' Load timed settings
                Dim timedHours = options.TimedDurationSeconds \ 3600
                Dim timedMinutes = (options.TimedDurationSeconds Mod 3600) \ 60
                Dim timedSeconds = options.TimedDurationSeconds Mod 60
                numTimedHours.Value = timedHours
                numTimedMinutes.Value = timedMinutes
                numTimedSeconds.Value = timedSeconds
                
                ' Load loop settings
                numLoopCount.Value = options.LoopCount
                Dim loopHours = options.LoopDurationSeconds \ 3600
                Dim loopMinutes = (options.LoopDurationSeconds Mod 3600) \ 60
                Dim loopSeconds = options.LoopDurationSeconds Mod 60
                numLoopHours.Value = loopHours
                numLoopMinutes.Value = loopMinutes
                numLoopSeconds.Value = loopSeconds
                numLoopDelay.Value = options.LoopDelaySeconds
                chkAutoIncrement.Checked = options.AutoIncrementTakeNumbers
                
            Finally
                suppressEvents = False
            End Try
            
            UpdateModeDescription()
        End Sub
        
        Private Sub OnModeChanged(sender As Object, e As EventArgs)
            ' Enable/disable appropriate groups
            grpTimedOptions.Enabled = radioTimed.Checked
            grpLoopOptions.Enabled = radioLoop.Checked
            
            UpdateModeDescription()
            
            If Not suppressEvents Then
                RaiseEvent OptionsChanged(Me, GetOptions())
            End If
        End Sub
        
        Private Sub OnControlChanged(sender As Object, e As EventArgs)
            UpdateModeDescription()
            
            If Not suppressEvents Then
                RaiseEvent OptionsChanged(Me, GetOptions())
            End If
        End Sub
        
        Private Sub UpdateModeDescription()
            Dim options = GetOptions()
            lblModeDescription.Text = options.GetDescription()
        End Sub
        
        Private Sub btn30Sec_Click(sender As Object, e As EventArgs)
            LoadOptions(RecordingOptions.QuickTake30Sec())
            RaiseEvent OptionsChanged(Me, GetOptions())
        End Sub
        
        Private Sub btn60Sec_Click(sender As Object, e As EventArgs)
            LoadOptions(RecordingOptions.QuickTake60Sec())
            RaiseEvent OptionsChanged(Me, GetOptions())
        End Sub
        
        Private Sub btn5Takes_Click(sender As Object, e As EventArgs)
            LoadOptions(RecordingOptions.QuickLoop5Takes())
            RaiseEvent OptionsChanged(Me, GetOptions())
        End Sub
        
    End Class

End Namespace
