Namespace UI

    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class TransportControl
        Inherits System.Windows.Forms.UserControl

        'UserControl overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()>
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                    timeFont?.Dispose()
                    labelFont?.Dispose()
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
            Me.components = New System.ComponentModel.Container()
            Me.ledTimer = New System.Windows.Forms.Timer(Me.components)
            Me.SuspendLayout()
            '
            'ledTimer
            '
            Me.ledTimer.Enabled = True
            Me.ledTimer.Interval = 50
            '
            'TransportControl
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 20.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer))
            Me.Name = "TransportControl"
            Me.Size = New System.Drawing.Size(800, 194)
            Me.ResumeLayout(False)

        End Sub

        Friend WithEvents ledTimer As Timer

    End Class

End Namespace
