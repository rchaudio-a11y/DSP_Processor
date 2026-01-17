Imports DSP_Processor.AudioIO
Imports DSP_Processor.Managers

Namespace UI.TabPanels

    ''' <summary>
    ''' Audio routing panel - input source and output device selection.
    ''' NOW USES DESIGNER! All controls are in RoutingPanel.Designer.vb
    ''' </summary>
    Partial Public Class RoutingPanel
        Inherits UserControl

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

            ' Wire up events (controls already exist in Designer!)
            AddHandler radioMicrophone.CheckedChanged, AddressOf OnInputSourceChanged
            AddHandler radioFilePlayback.CheckedChanged, AddressOf OnInputSourceChanged
            AddHandler btnBrowseInputFile.Click, AddressOf OnBrowseFileClick
            AddHandler cmbOutputDevice.SelectedIndexChanged, AddressOf OnOutputDeviceChanged
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
