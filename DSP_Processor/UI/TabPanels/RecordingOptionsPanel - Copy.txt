Imports DSP_Processor.Models
Imports DSP_Processor.UI

Namespace UI.TabPanels

    ''' <summary>
    ''' Recording options tab panel for configuring recording modes
    ''' NOW USES DESIGNER! All controls are in RecordingOptionsPanel.Designer.vb
    ''' </summary>
    Partial Public Class RecordingOptionsPanel
        Inherits UserControl

        Public Event OptionsChanged As EventHandler(Of RecordingOptions)

        Private suppressEvents As Boolean = False

        Public Sub New()
            InitializeComponent()

            ' Wire up events (controls already exist in Designer!)
            AddHandler radioManual.CheckedChanged, AddressOf OnModeChanged
            AddHandler radioTimed.CheckedChanged, AddressOf OnModeChanged
            AddHandler radioLoop.CheckedChanged, AddressOf OnModeChanged
            AddHandler numTimedHours.ValueChanged, AddressOf OnControlChanged
            AddHandler numTimedMinutes.ValueChanged, AddressOf OnControlChanged
            AddHandler numTimedSeconds.ValueChanged, AddressOf OnControlChanged
            AddHandler numLoopCount.ValueChanged, AddressOf OnControlChanged
            AddHandler numLoopHours.ValueChanged, AddressOf OnControlChanged
            AddHandler numLoopMinutes.ValueChanged, AddressOf OnControlChanged
            AddHandler numLoopSeconds.ValueChanged, AddressOf OnControlChanged
            AddHandler numLoopDelay.ValueChanged, AddressOf OnControlChanged
            AddHandler chkAutoIncrement.CheckedChanged, AddressOf OnControlChanged
            AddHandler btn30Sec.Click, AddressOf btn30Sec_Click
            AddHandler btn60Sec.Click, AddressOf btn60Sec_Click
            AddHandler btn5Takes.Click, AddressOf btn5Takes_Click

            ' Apply dark theme
            DarkTheme.ApplyToControl(Me)
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
            ' Enable/disable appropriate controls based on mode
            Dim timedEnabled = radioTimed.Checked
            Dim loopEnabled = radioLoop.Checked

            ' Timed controls
            lblTimedDuration.Enabled = timedEnabled
            numTimedHours.Enabled = timedEnabled
            numTimedMinutes.Enabled = timedEnabled
            numTimedSeconds.Enabled = timedEnabled
            lblTimedFormat.Enabled = timedEnabled

            ' Loop controls
            lblLoopCount.Enabled = loopEnabled
            numLoopCount.Enabled = loopEnabled
            lblLoopDuration.Enabled = loopEnabled
            numLoopHours.Enabled = loopEnabled
            numLoopMinutes.Enabled = loopEnabled
            numLoopSeconds.Enabled = loopEnabled
            lblLoopDelay.Enabled = loopEnabled
            numLoopDelay.Enabled = loopEnabled
            chkAutoIncrement.Enabled = loopEnabled

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
