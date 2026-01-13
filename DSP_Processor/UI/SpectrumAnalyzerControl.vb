Imports DSP_Processor.Visualization
Imports System.ComponentModel

Namespace UI

    ''' <summary>
    ''' User control containing side-by-side spectrum displays (PRE-DSP and POST-DSP)
    ''' Can be added to forms via Designer
    ''' </summary>
    Public Class SpectrumAnalyzerControl
        Inherits UserControl

        Private splitSpectrum As SplitContainer
        Private lblPreDSP As Label
        Private lblPostDSP As Label

        ' Backing fields for spectrum displays
        Private m_InputDisplay As SpectrumDisplayControl
        Private m_OutputDisplay As SpectrumDisplayControl

        ' Public read-only properties for the spectrum displays
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Browsable(False)>
        Public ReadOnly Property InputDisplay As SpectrumDisplayControl
            Get
                Return m_InputDisplay
            End Get
        End Property

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Browsable(False)>
        Public ReadOnly Property OutputDisplay As SpectrumDisplayControl
            Get
                Return m_OutputDisplay
            End Get
        End Property

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            ' Create split container
            splitSpectrum = New SplitContainer() With {
                .Dock = DockStyle.Fill,
                .Orientation = Orientation.Vertical
            }

            ' Create spectrum displays
            m_InputDisplay = New SpectrumDisplayControl() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.Black,
                .SpectrumColor = Color.Cyan,
                .PeakHoldColor = Color.Red,
                .MinFrequency = 20,
                .MaxFrequency = 20000,
                .MinDB = -60,
                .MaxDB = 0,
                .PeakHoldEnabled = False,
                .SmoothingFactor = 0.7F
            }

            m_OutputDisplay = New SpectrumDisplayControl() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.Black,
                .SpectrumColor = Color.Lime,
                .PeakHoldColor = Color.Orange,
                .MinFrequency = 20,
                .MaxFrequency = 20000,
                .MinDB = -60,
                .MaxDB = 0,
                .PeakHoldEnabled = False,
                .SmoothingFactor = 0.7F
            }

            ' Create labels
            lblPreDSP = New Label() With {
                .Text = "PRE-DSP (Input)",
                .Dock = DockStyle.Top,
                .TextAlign = ContentAlignment.MiddleCenter,
                .ForeColor = Color.Cyan,
                .BackColor = Color.FromArgb(30, 30, 30),
                .Height = 30,
                .Font = New Font("Segoe UI", 10, FontStyle.Bold)
            }

            lblPostDSP = New Label() With {
                .Text = "POST-DSP (Output)",
                .Dock = DockStyle.Top,
                .TextAlign = ContentAlignment.MiddleCenter,
                .ForeColor = Color.Lime,
                .BackColor = Color.FromArgb(30, 30, 30),
                .Height = 30,
                .Font = New Font("Segoe UI", 10, FontStyle.Bold)
            }

            ' Add to split panels
            splitSpectrum.Panel1.Controls.Add(m_InputDisplay)
            splitSpectrum.Panel1.Controls.Add(lblPreDSP)
            splitSpectrum.Panel2.Controls.Add(m_OutputDisplay)
            splitSpectrum.Panel2.Controls.Add(lblPostDSP)

            ' Add to this control
            Me.Controls.Add(splitSpectrum)

            ' Set control properties
            Me.BackColor = Color.Black
            Me.Size = New Size(800, 400)

            ' Handle resize to keep splitter centered
            AddHandler Me.Resize, AddressOf OnControlResize

            ' Set initial splitter position when control is first laid out
            AddHandler Me.Layout, Sub(s, ev)
                                      If splitSpectrum.Width > 0 Then
                                          splitSpectrum.SplitterDistance = splitSpectrum.Width \ 2
                                      End If
                                  End Sub

            Me.ResumeLayout(False)
        End Sub

        Private Sub OnControlResize(sender As Object, e As EventArgs)
            ' Keep splitter at 50% of width when control resizes
            If splitSpectrum IsNot Nothing AndAlso splitSpectrum.Width > 0 Then
                splitSpectrum.SplitterDistance = splitSpectrum.Width \ 2
            End If
        End Sub

        ''' <summary>
        ''' Clear both spectrum displays
        ''' </summary>
        Public Sub Clear()
            m_InputDisplay?.Clear()
            m_OutputDisplay?.Clear()
        End Sub

    End Class

End Namespace
