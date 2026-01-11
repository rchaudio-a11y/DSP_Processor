Imports System.Drawing
Imports System.Windows.Forms

Namespace UI

    ''' <summary>
    ''' Dark mode theme manager for the application.
    ''' Provides consistent dark colors across all controls.
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 1
    ''' Purpose: Professional dark theme like modern DAWs
    ''' </remarks>
    Public NotInheritable Class DarkTheme

        ' Prevent instantiation
        Private Sub New()
        End Sub

#Region "Color Palette"

        ''' <summary>Background for main windows and panels</summary>
        Public Shared ReadOnly Property BackgroundDark As Color = Color.FromArgb(30, 30, 30)

        ''' <summary>Background for controls (buttons, textboxes, etc.)</summary>
        Public Shared ReadOnly Property ControlBackground As Color = Color.FromArgb(45, 45, 48)

        ''' <summary>Background for hover states</summary>
        Public Shared ReadOnly Property ControlBackgroundHover As Color = Color.FromArgb(62, 62, 66)

        ''' <summary>Background for selected/active controls</summary>
        Public Shared ReadOnly Property ControlBackgroundActive As Color = Color.FromArgb(0, 122, 204)

        ''' <summary>Border color for controls</summary>
        Public Shared ReadOnly Property BorderColor As Color = Color.FromArgb(63, 63, 70)

        ''' <summary>Main text color</summary>
        Public Shared ReadOnly Property TextColor As Color = Color.FromArgb(241, 241, 241)

        ''' <summary>Secondary/dimmed text</summary>
        Public Shared ReadOnly Property TextColorDim As Color = Color.FromArgb(153, 153, 153)

        ''' <summary>Accent color (buttons, highlights)</summary>
        Public Shared ReadOnly Property AccentBlue As Color = Color.FromArgb(0, 122, 204)

        ''' <summary>Success color (green indicators)</summary>
        Public Shared ReadOnly Property SuccessGreen As Color = Color.FromArgb(16, 185, 129)

        ''' <summary>Warning color (yellow indicators)</summary>
        Public Shared ReadOnly Property WarningYellow As Color = Color.FromArgb(251, 191, 36)

        ''' <summary>Error/Record color (red indicators)</summary>
        Public Shared ReadOnly Property ErrorRed As Color = Color.FromArgb(239, 68, 68)

        ''' <summary>Meter green zone</summary>
        Public Shared ReadOnly Property MeterGreen As Color = Color.FromArgb(34, 197, 94)

        ''' <summary>Meter yellow zone</summary>
        Public Shared ReadOnly Property MeterYellow As Color = Color.FromArgb(234, 179, 8)

        ''' <summary>Meter red zone</summary>
        Public Shared ReadOnly Property MeterRed As Color = Color.FromArgb(239, 68, 68)

#End Region

#Region "Apply Theme Methods"

        ''' <summary>
        ''' Applies dark theme to an entire form and all its controls
        ''' </summary>
        Public Shared Sub ApplyToForm(form As Form)
            If form Is Nothing Then Return

            ' Apply to form itself
            form.BackColor = BackgroundDark
            form.ForeColor = TextColor

            ' Apply to all controls recursively
            ApplyToControl(form)
        End Sub

        ''' <summary>
        ''' Applies dark theme to a control and all its children
        ''' </summary>
        Public Shared Sub ApplyToControl(ctrl As Control)
            If ctrl Is Nothing Then Return

            ' Apply theme based on control type
            Select Case True
                Case TypeOf ctrl Is Form
                    ApplyToFormInternal(DirectCast(ctrl, Form))

                Case TypeOf ctrl Is Button
                    ApplyToButton(DirectCast(ctrl, Button))

                Case TypeOf ctrl Is Label
                    ApplyToLabel(DirectCast(ctrl, Label))

                Case TypeOf ctrl Is TextBox
                    ApplyToTextBox(DirectCast(ctrl, TextBox))

                Case TypeOf ctrl Is ComboBox
                    ApplyToComboBox(DirectCast(ctrl, ComboBox))

                Case TypeOf ctrl Is ListBox
                    ApplyToListBox(DirectCast(ctrl, ListBox))

                Case TypeOf ctrl Is Panel
                    ApplyToPanel(DirectCast(ctrl, Panel))

                Case TypeOf ctrl Is ProgressBar
                    ApplyToProgressBar(DirectCast(ctrl, ProgressBar))

                Case TypeOf ctrl Is TrackBar
                    ApplyToTrackBar(DirectCast(ctrl, TrackBar))

                Case TypeOf ctrl Is PictureBox
                    ApplyToPictureBox(DirectCast(ctrl, PictureBox))

                Case Else
                    ' Default styling for unknown controls
                    ctrl.BackColor = ControlBackground
                    ctrl.ForeColor = TextColor
            End Select

            ' Recursively apply to child controls
            For Each child As Control In ctrl.Controls
                ApplyToControl(child)
            Next
        End Sub

        Private Shared Sub ApplyToFormInternal(form As Form)
            form.BackColor = BackgroundDark
            form.ForeColor = TextColor
        End Sub

        Private Shared Sub ApplyToButton(btn As Button)
            btn.BackColor = ControlBackground
            btn.ForeColor = TextColor
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderColor = BorderColor
            btn.FlatAppearance.BorderSize = 1
            btn.FlatAppearance.MouseOverBackColor = ControlBackgroundHover
            btn.FlatAppearance.MouseDownBackColor = ControlBackgroundActive
        End Sub

        Private Shared Sub ApplyToLabel(lbl As Label)
            ' Labels should be transparent to show form background
            lbl.BackColor = Color.Transparent
            lbl.ForeColor = TextColor
        End Sub

        Private Shared Sub ApplyToTextBox(txt As TextBox)
            txt.BackColor = ControlBackground
            txt.ForeColor = TextColor
            txt.BorderStyle = BorderStyle.FixedSingle
        End Sub

        Private Shared Sub ApplyToComboBox(cmb As ComboBox)
            cmb.BackColor = ControlBackground
            cmb.ForeColor = TextColor
            cmb.FlatStyle = FlatStyle.Flat
        End Sub

        Private Shared Sub ApplyToListBox(lst As ListBox)
            lst.BackColor = ControlBackground
            lst.ForeColor = TextColor
            lst.BorderStyle = BorderStyle.FixedSingle
        End Sub

        Private Shared Sub ApplyToPanel(pnl As Panel)
            pnl.BackColor = ControlBackground
            pnl.ForeColor = TextColor
        End Sub

        Private Shared Sub ApplyToProgressBar(prg As ProgressBar)
            ' ProgressBar doesn't support BackColor/ForeColor well
            ' Leave as is or use custom drawing
        End Sub

        Private Shared Sub ApplyToTrackBar(trk As TrackBar)
            trk.BackColor = BackgroundDark
            ' TrackBar doesn't support much customization without custom drawing
        End Sub

        Private Shared Sub ApplyToPictureBox(pic As PictureBox)
            pic.BackColor = Color.Black ' Keep black for waveform display
        End Sub

#End Region

#Region "Specific Styling"

        ''' <summary>
        ''' Styles a panel as a titled section (like groupbox)
        ''' </summary>
        Public Shared Sub ApplyTitledSection(pnl As Panel, title As String)
            pnl.BackColor = ControlBackground
            pnl.ForeColor = TextColor

            ' Add a label for the title if it doesn't exist
            Dim titleLabel = pnl.Controls.OfType(Of Label)().FirstOrDefault(Function(l) l.Name = "titleLabel")
            If titleLabel Is Nothing Then
                titleLabel = New Label() With {
                    .Name = "titleLabel",
                    .Text = title,
                    .Dock = DockStyle.Top,
                    .Height = 25,
                    .BackColor = BackgroundDark,
                    .ForeColor = TextColor,
                    .Font = New Font(pnl.Font.FontFamily, 10, FontStyle.Bold),
                    .TextAlign = ContentAlignment.MiddleLeft,
                    .Padding = New Padding(5, 0, 0, 0)
                }
                pnl.Controls.Add(titleLabel)
                titleLabel.BringToFront()
            End If
        End Sub

        ''' <summary>
        ''' Styles a button as a primary action (Record, Play, etc.)
        ''' </summary>
        Public Shared Sub ApplyPrimaryButton(btn As Button)
            ApplyToButton(btn)
            btn.BackColor = AccentBlue
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 150, 250)
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 100, 180)
        End Sub

        ''' <summary>
        ''' Styles a button as a danger action (Stop, Delete, etc.)
        ''' </summary>
        Public Shared Sub ApplyDangerButton(btn As Button)
            ApplyToButton(btn)
            btn.BackColor = ErrorRed
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(250, 100, 100)
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 50, 50)
        End Sub

#End Region

    End Class

End Namespace
