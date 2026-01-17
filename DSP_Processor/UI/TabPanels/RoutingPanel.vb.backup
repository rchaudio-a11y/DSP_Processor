Imports DSP_Processor.AudioIO
Imports DSP_Processor.Managers

Namespace UI.TabPanels

    ''' <summary>
    ''' Audio routing panel - input source and output device selection.
    ''' Replaces hard-coded grpRouting on Program tab.
    ''' </summary>
    Public Class RoutingPanel
        Inherits UserControl

#Region "Controls"

        Private grpRouting As GroupBox
        Private lblInputSource As Label
        Private radioMicrophone As RadioButton
        Private radioFilePlayback As RadioButton
        Private btnBrowseInputFile As Button
        Private lblSelectedFile As Label
        Private lblOutputDevice As Label
        Private cmbOutputDevice As ComboBox

#End Region

#Region "Events"

        ''' <summary>Raised when input source changes</summary>
        Public Event InputSourceChanged As EventHandler(Of String)

        ''' <summary>Raised when output device changes</summary>
        Public Event OutputDeviceChanged As EventHandler(Of Integer)

        ''' <summary>Raised when browse file button clicked</summary>
        Public Event BrowseFileClicked As EventHandler

#End Region

#Region "Fields"

        Private suppressEvents As Boolean = False

#End Region

#Region "Properties"

        ''' <summary>Get current input source</summary>
        Public ReadOnly Property InputSource As String
            Get
                If radioMicrophone.Checked Then
                    Return "Microphone"
                ElseIf radioFilePlayback.Checked Then
                    Return "FilePlayback"
                Else
                    Return "Unknown"
                End If
            End Get
        End Property

        ''' <summary>Get/Set selected output device index</summary>
        <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
        Public Property OutputDeviceIndex As Integer
            Get
                Return cmbOutputDevice.SelectedIndex
            End Get
            Set(value As Integer)
                suppressEvents = True
                If value >= 0 AndAlso value < cmbOutputDevice.Items.Count Then
                    cmbOutputDevice.SelectedIndex = value
                End If
                suppressEvents = False
            End Set
        End Property

        ''' <summary>Set selected file path display</summary>
        <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
        Public Property SelectedFilePath As String
            Get
                Return lblSelectedFile.Text
            End Get
            Set(value As String)
                lblSelectedFile.Text = If(String.IsNullOrEmpty(value), "No file selected", value)
            End Set
        End Property

#End Region

#Region "Constructor"

        Public Sub New()
            InitializeComponent()
        End Sub

#End Region

#Region "Initialization"

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            ' Main container
            Me.AutoScaleMode = AutoScaleMode.Font
            Me.Size = New Size(400, 270)
            Me.BackColor = Color.FromArgb(45, 45, 48)

            ' Create group box
            grpRouting = New GroupBox With {
                .Text = "Audio Routing",
                .Location = New Point(10, 10),
                .Size = New Size(380, 250),
                .ForeColor = Color.White
            }

            ' Input source label
            lblInputSource = New Label With {
                .Text = "Input Source:",
                .Location = New Point(10, 25),
                .Size = New Size(95, 20),
                .ForeColor = Color.White
            }

            ' Radio buttons
            radioMicrophone = New RadioButton With {
                .Text = "?? Microphone",
                .Location = New Point(10, 50),
                .Size = New Size(135, 24),
                .Checked = True,
                .ForeColor = Color.White
            }
            AddHandler radioMicrophone.CheckedChanged, AddressOf OnInputSourceChanged

            radioFilePlayback = New RadioButton With {
                .Text = "?? File Playback",
                .Location = New Point(10, 80),
                .Size = New Size(140, 24),
                .ForeColor = Color.White
            }
            AddHandler radioFilePlayback.CheckedChanged, AddressOf OnInputSourceChanged

            ' Browse button
            btnBrowseInputFile = New Button With {
                .Text = "Browse for audio file...",
                .Location = New Point(30, 110),
                .Size = New Size(270, 30),
                .Enabled = False,
                .BackColor = Color.FromArgb(60, 60, 60),
                .FlatStyle = FlatStyle.Flat,
                .ForeColor = Color.White
            }
            AddHandler btnBrowseInputFile.Click, AddressOf OnBrowseFileClick

            ' Selected file label
            lblSelectedFile = New Label With {
                .Text = "No file selected",
                .Location = New Point(30, 145),
                .Size = New Size(270, 40),
                .ForeColor = Color.Cyan
            }

            ' Output device label
            lblOutputDevice = New Label With {
                .Text = "Output Device:",
                .Location = New Point(10, 190),
                .Size = New Size(110, 20),
                .ForeColor = Color.White
            }

            ' Output device combo
            cmbOutputDevice = New ComboBox With {
                .Location = New Point(10, 215),
                .Size = New Size(290, 21),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(60, 60, 60),
                .ForeColor = Color.White
            }
            AddHandler cmbOutputDevice.SelectedIndexChanged, AddressOf OnOutputDeviceChanged

            ' Add controls to group box
            grpRouting.Controls.AddRange(New Control() {
                lblInputSource,
                radioMicrophone,
                radioFilePlayback,
                btnBrowseInputFile,
                lblSelectedFile,
                lblOutputDevice,
                cmbOutputDevice
            })

            ' Add group box to panel
            Me.Controls.Add(grpRouting)

            Me.ResumeLayout(False)
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>Populate output devices combo box</summary>
        Public Sub LoadOutputDevices(deviceNames As List(Of String), selectedIndex As Integer)
            suppressEvents = True
            Try
                cmbOutputDevice.Items.Clear()
                For Each deviceName In deviceNames
                    cmbOutputDevice.Items.Add(deviceName)
                Next

                If selectedIndex >= 0 AndAlso selectedIndex < cmbOutputDevice.Items.Count Then
                    cmbOutputDevice.SelectedIndex = selectedIndex
                ElseIf cmbOutputDevice.Items.Count > 0 Then
                    cmbOutputDevice.SelectedIndex = 0
                End If

            Finally
                suppressEvents = False
            End Try
        End Sub

        ''' <summary>Set input source to microphone</summary>
        Public Sub SetMicrophoneInput()
            suppressEvents = True
            radioMicrophone.Checked = True
            btnBrowseInputFile.Enabled = False
            suppressEvents = False
        End Sub

        ''' <summary>Set input source to file playback</summary>
        Public Sub SetFilePlaybackInput()
            suppressEvents = True
            radioFilePlayback.Checked = True
            btnBrowseInputFile.Enabled = True
            suppressEvents = False
        End Sub

#End Region

#Region "Event Handlers"

        Private Sub OnInputSourceChanged(sender As Object, e As EventArgs)
            ' Enable/disable browse button based on selection
            btnBrowseInputFile.Enabled = radioFilePlayback.Checked

            If Not suppressEvents Then
                RaiseEvent InputSourceChanged(Me, InputSource)
            End If
        End Sub

        Private Sub OnOutputDeviceChanged(sender As Object, e As EventArgs)
            If Not suppressEvents AndAlso cmbOutputDevice.SelectedIndex >= 0 Then
                RaiseEvent OutputDeviceChanged(Me, cmbOutputDevice.SelectedIndex)
            End If
        End Sub

        Private Sub OnBrowseFileClick(sender As Object, e As EventArgs)
            RaiseEvent BrowseFileClicked(Me, EventArgs.Empty)
        End Sub

#End Region

    End Class

End Namespace
