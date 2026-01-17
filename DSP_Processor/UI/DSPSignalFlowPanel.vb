Imports DSP_Processor.DSP
Imports DSP_Processor.AudioIO
Imports DSP_Processor.UI

''' <summary>
''' DSP Signal Flow Panel - Professional mixing console style UI
''' Shows stereo input meters, gain/pan controls, filters, and output meters
''' </summary>
Partial Public Class DSPSignalFlowPanel
    Inherits UserControl

#Region "Controls"

    ' Stereo meter controls
    Private meterInputLeft As UI.VolumeMeterControl
    Private meterInputRight As UI.VolumeMeterControl
    Private meterOutputLeft As UI.VolumeMeterControl
    Private meterOutputRight As UI.VolumeMeterControl

#End Region

#Region "Fields"

    Private gainProcessor As GainProcessor
    Private suppressEvents As Boolean = False

#End Region

#Region "Constructor"

    Public Sub New()
        InitializeComponent()
        InitializeMeters()
        InitializeControls()
    End Sub

#End Region

#Region "Initialization"

    ''' <summary>Initialize stereo volume meters</summary>
    Private Sub InitializeMeters()
        ' ===== INPUT METERS =====
        ' Remove the existing label (we'll create a new layout)
        panelInputMeters.Controls.Clear()

        ' Create TableLayoutPanel for proper layout
        Dim inputLayout = New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .RowCount = 2,
            .ColumnCount = 2,
            .Padding = New Padding(0)
        }

        ' Row 0: Label (25px high, spans both columns)
        inputLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 25.0F))
        ' Row 1: Meters (fill remaining space)
        inputLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        ' Columns: 50% each
        inputLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        inputLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))

        ' Create label
        Dim lblInput = New Label() With {
            .Text = "INPUT (L/R)",
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        inputLayout.Controls.Add(lblInput, 0, 0)
        inputLayout.SetColumnSpan(lblInput, 2)  ' Span both columns

        ' Create left meter
        meterInputLeft = New UI.VolumeMeterControl() With {
            .Dock = DockStyle.Fill
        }
        inputLayout.Controls.Add(meterInputLeft, 0, 1)

        ' Create right meter
        meterInputRight = New UI.VolumeMeterControl() With {
            .Dock = DockStyle.Fill
        }
        inputLayout.Controls.Add(meterInputRight, 1, 1)

        ' Add layout to panel
        panelInputMeters.Controls.Add(inputLayout)

        ' ===== OUTPUT METERS =====
        ' Clear existing controls
        panelOutputMeters.Controls.Clear()

        ' Create TableLayoutPanel for proper layout
        Dim outputLayout = New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .RowCount = 2,
            .ColumnCount = 2,
            .Padding = New Padding(0)
        }

        ' Row 0: Label (25px high, spans both columns)
        outputLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 25.0F))
        ' Row 1: Meters (fill remaining space)
        outputLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        ' Columns: 50% each
        outputLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        outputLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))

        ' Create label
        Dim lblOutput = New Label() With {
            .Text = "OUTPUT (L/R)",
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        outputLayout.Controls.Add(lblOutput, 0, 0)
        outputLayout.SetColumnSpan(lblOutput, 2)  ' Span both columns

        ' Create left meter
        meterOutputLeft = New UI.VolumeMeterControl() With {
            .Dock = DockStyle.Fill
        }
        outputLayout.Controls.Add(meterOutputLeft, 0, 1)

        ' Create right meter
        meterOutputRight = New UI.VolumeMeterControl() With {
            .Dock = DockStyle.Fill
        }
        outputLayout.Controls.Add(meterOutputRight, 1, 1)

        ' Add layout to panel
        panelOutputMeters.Controls.Add(outputLayout)
    End Sub

    ''' <summary>Initialize controls and wire events</summary>
    Private Sub InitializeControls()
        ' Setup gain trackbar (-60dB to +20dB)
        trackGain.Minimum = -600  ' -60.0 dB * 10
        trackGain.Maximum = 200   ' +20.0 dB * 10
        trackGain.Value = 0       ' 0 dB (unity gain)
        trackGain.TickFrequency = 60  ' 6dB ticks
        lblGainValue.Text = "0.0 dB"

        ' Setup pan trackbar (-100 to +100, center = 0)
        trackPan.Minimum = -100  ' Full left
        trackPan.Maximum = 100   ' Full right
        trackPan.Value = 0       ' Center
        trackPan.TickFrequency = 25
        lblPanValue.Text = "Center"

        ' Wire events (using MouseDoubleClick instead of DoubleClick for reliability)
        AddHandler trackGain.Scroll, AddressOf OnGainChanged
        AddHandler trackGain.ValueChanged, AddressOf OnGainChanged ' ALSO wire ValueChanged for redundancy
        AddHandler trackGain.MouseDoubleClick, AddressOf OnGainDoubleClick
        AddHandler trackPan.Scroll, AddressOf OnPanChanged
        AddHandler trackPan.ValueChanged, AddressOf OnPanChanged ' ALSO wire ValueChanged
        AddHandler trackPan.MouseDoubleClick, AddressOf OnPanDoubleClick

        ' TODO: Wire filter events when filters are implemented
        AddHandler chkHPFEnable.CheckedChanged, AddressOf OnFilterChanged
        AddHandler trackHPFFreq.Scroll, AddressOf OnFilterChanged
        AddHandler trackHPFFreq.MouseDoubleClick, AddressOf OnHPFDoubleClick
        AddHandler chkLPFEnable.CheckedChanged, AddressOf OnFilterChanged
        AddHandler trackLPFFreq.Scroll, AddressOf OnFilterChanged
        AddHandler trackLPFFreq.MouseDoubleClick, AddressOf OnLPFDoubleClick

        ' Initialize filter labels
        lblHPFFreqValue.Text = $"{trackHPFFreq.Value} Hz"
        lblLPFFreqValue.Text = $"{trackLPFFreq.Value / 1000.0F:F1} kHz"
        trackHPFFreq.Enabled = chkHPFEnable.Checked
        trackLPFFreq.Enabled = chkLPFEnable.Checked

        ' TODO: Wire output mixer events
        AddHandler trackMaster.Scroll, AddressOf OnMasterChanged
        AddHandler trackMaster.MouseDoubleClick, AddressOf OnMasterDoubleClick
        AddHandler trackWidth.Scroll, AddressOf OnWidthChanged
        AddHandler trackWidth.MouseDoubleClick, AddressOf OnWidthDoubleClick
    End Sub

#End Region

#Region "Public Methods"

    ''' <summary>Set the GainProcessor instance to control</summary>
    Public Sub SetGainProcessor(processor As GainProcessor)
        gainProcessor = processor
        Utils.Logger.Instance.Info($"SetGainProcessor called. Processor IsNot Nothing={processor IsNot Nothing}", "DSPSignalFlowPanel")

        If gainProcessor IsNot Nothing Then
            ' Load current values
            UpdateGainFromProcessor()
            UpdatePanFromProcessor()
            Utils.Logger.Instance.Info("GainProcessor wired successfully!", "DSPSignalFlowPanel")
        Else
            Utils.Logger.Instance.Warning("SetGainProcessor called with Nothing!", "DSPSignalFlowPanel")
        End If
    End Sub

    ''' <summary>Update meter levels (call from audio thread callback)</summary>
    ''' <param name="inputLeftDb">Left input level in dB</param>
    ''' <param name="inputRightDb">Right input level in dB</param>
    ''' <param name="outputLeftDb">Left output level in dB</param>
    ''' <param name="outputRightDb">Right output level in dB</param>
    Public Sub UpdateMeters(inputLeftDb As Single, inputRightDb As Single, outputLeftDb As Single, outputRightDb As Single)
        If InvokeRequired Then
            BeginInvoke(Sub() UpdateMeters(inputLeftDb, inputRightDb, outputLeftDb, outputRightDb))
            Return
        End If

        ' Update individual meters with mono data
        meterInputLeft.SetLevel(inputLeftDb, inputLeftDb, inputLeftDb >= -0.1F)
        meterInputRight.SetLevel(inputRightDb, inputRightDb, inputRightDb >= -0.1F)
        meterOutputLeft.SetLevel(outputLeftDb, outputLeftDb, outputLeftDb >= -0.1F)
        meterOutputRight.SetLevel(outputRightDb, outputRightDb, outputRightDb >= -0.1F)
    End Sub

    ''' <summary>Reset all meters to minimum</summary>
    Public Sub ResetMeters()
        meterInputLeft.Reset()
        meterInputRight.Reset()
        meterOutputLeft.Reset()
        meterOutputRight.Reset()
    End Sub

#End Region

#Region "Event Handlers - Gain/Pan"

    Private Sub OnGainChanged(sender As Object, e As EventArgs)
        ' Debug logging
        Utils.Logger.Instance.Debug($"OnGainChanged called. suppressEvents={suppressEvents}, gainProcessor IsNot Nothing={gainProcessor IsNot Nothing}", "DSPSignalFlowPanel")

        If suppressEvents OrElse gainProcessor Is Nothing Then
            If gainProcessor Is Nothing Then
                Utils.Logger.Instance.Warning("OnGainChanged: gainProcessor is Nothing! Not wired yet?", "DSPSignalFlowPanel")
            End If
            Return
        End If

        ' Convert trackbar value to dB (divided by 10)
        Dim gainDb = trackGain.Value / 10.0F
        lblGainValue.Text = $"{gainDb:F1} dB"

        ' Update processor
        gainProcessor.GainDB = gainDb
        Utils.Logger.Instance.Info($"Gain updated to {gainDb:F1} dB", "DSPSignalFlowPanel")
    End Sub

    Private Sub OnGainDoubleClick(sender As Object, e As MouseEventArgs)
        ' Reset to default (0 dB = unity gain)
        trackGain.Value = 0
        Utils.Logger.Instance.Info("Gain reset to default (0 dB)", "DSPSignalFlowPanel")
    End Sub

    Private Sub OnPanChanged(sender As Object, e As EventArgs)
        If suppressEvents OrElse gainProcessor Is Nothing Then Return

        ' Convert trackbar value to pan position (-1.0 to +1.0)
        Dim panPosition = trackPan.Value / 100.0F

        ' Update label
        If Math.Abs(panPosition) < 0.05F Then
            lblPanValue.Text = "Center"
        ElseIf panPosition < 0 Then
            lblPanValue.Text = $"L{Math.Abs(CInt(panPosition * 100))}"
        Else
            lblPanValue.Text = $"R{CInt(panPosition * 100)}"
        End If

        ' Update processor
        gainProcessor.PanPosition = panPosition
    End Sub

    Private Sub OnPanDoubleClick(sender As Object, e As MouseEventArgs)
        ' Reset to default (0 = center)
        trackPan.Value = 0
        Utils.Logger.Instance.Info("Pan reset to default (Center)", "DSPSignalFlowPanel")
    End Sub

#End Region

#Region "Event Handlers - Filters"

    Private Sub OnFilterChanged(sender As Object, e As EventArgs)
        ' TODO: Implement filter control when filter processors are added
        If chkHPFEnable.Checked Then
            lblHPFFreqValue.Text = $"{trackHPFFreq.Value} Hz"
            trackHPFFreq.Enabled = True
        Else
            trackHPFFreq.Enabled = False
        End If

        If chkLPFEnable.Checked Then
            lblLPFFreqValue.Text = $"{trackLPFFreq.Value / 1000.0F:F1} kHz"  ' Show in kHz
            trackLPFFreq.Enabled = True
        Else
            trackLPFFreq.Enabled = False
        End If
    End Sub

    Private Sub OnHPFDoubleClick(sender As Object, e As MouseEventArgs)
        ' Reset to default (80 Hz typical high-pass)
        trackHPFFreq.Value = 80
        Utils.Logger.Instance.Info("High-pass filter reset to default (80 Hz)", "DSPSignalFlowPanel")
    End Sub

    Private Sub OnLPFDoubleClick(sender As Object, e As MouseEventArgs)
        ' Reset to default (15000 Hz typical low-pass)
        trackLPFFreq.Value = 15000
        Utils.Logger.Instance.Info("Low-pass filter reset to default (15 kHz)", "DSPSignalFlowPanel")
    End Sub

#End Region

#Region "Event Handlers - Output Mixer"

    Private Sub OnMasterChanged(sender As Object, e As EventArgs)
        ' TODO: Implement master volume control
        Dim masterDb = trackMaster.Value / 10.0F
        lblMasterValue.Text = $"{masterDb:F1} dB"
    End Sub

    Private Sub OnMasterDoubleClick(sender As Object, e As MouseEventArgs)
        ' Reset to default (0 dB = unity gain)
        trackMaster.Value = 0
        Utils.Logger.Instance.Info("Master volume reset to default (0 dB)", "DSPSignalFlowPanel")
    End Sub

    Private Sub OnWidthChanged(sender As Object, e As EventArgs)
        ' TODO: Implement stereo width control
        Dim width = trackWidth.Value
        lblWidthValue.Text = $"{width}%"
    End Sub

    Private Sub OnWidthDoubleClick(sender As Object, e As MouseEventArgs)
        ' Reset to default (100% = normal stereo)
        trackWidth.Value = 100
        Utils.Logger.Instance.Info("Stereo width reset to default (100%)", "DSPSignalFlowPanel")
    End Sub

#End Region

#Region "Private Helper Methods"

    Private Sub UpdateGainFromProcessor()
        If gainProcessor Is Nothing Then Return

        suppressEvents = True
        Try
            Dim gainDb = gainProcessor.GainDB
            trackGain.Value = CInt(gainDb * 10)
            lblGainValue.Text = $"{gainDb:F1} dB"
        Finally
            suppressEvents = False
        End Try
    End Sub

    Private Sub UpdatePanFromProcessor()
        If gainProcessor Is Nothing Then Return

        suppressEvents = True
        Try
            Dim panPosition = gainProcessor.PanPosition
            trackPan.Value = CInt(panPosition * 100)

            If Math.Abs(panPosition) < 0.05F Then
                lblPanValue.Text = "Center"
            ElseIf panPosition < 0 Then
                lblPanValue.Text = $"L{Math.Abs(CInt(panPosition * 100))}"
            Else
                lblPanValue.Text = $"R{CInt(panPosition * 100)}"
            End If
        Finally
            suppressEvents = False
        End Try
    End Sub

#End Region

End Class
