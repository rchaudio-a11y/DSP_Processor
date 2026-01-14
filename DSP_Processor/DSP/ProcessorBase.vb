Imports NAudio.Wave

Namespace DSP

    ''' <summary>
    ''' Abstract base class for all DSP processors.
    ''' Provides common functionality for audio processing effects.
    ''' </summary>
    Public MustInherit Class ProcessorBase
        Implements IDisposable

#Region "Private Fields"

        Private _enabled As Boolean = True
        Private _bypassed As Boolean = False
        Protected disposed As Boolean = False

#End Region

#Region "Properties"

        ''' <summary>Gets or sets whether this processor is enabled</summary>
        Public Property Enabled As Boolean
            Get
                Return _enabled
            End Get
            Set(value As Boolean)
                If _enabled <> value Then
                    _enabled = value
                    OnEnabledChanged()
                End If
            End Set
        End Property

        ''' <summary>Gets or sets whether this processor is bypassed (passes audio through unmodified)</summary>
        Public Property Bypassed As Boolean
            Get
                Return _bypassed
            End Get
            Set(value As Boolean)
                If _bypassed <> value Then
                    _bypassed = value
                    OnBypassedChanged()
                End If
            End Set
        End Property

        ''' <summary>Gets the name of this processor</summary>
        Public MustOverride ReadOnly Property Name As String

        ''' <summary>Gets the wave format this processor operates on</summary>
        Public ReadOnly Property Format As WaveFormat

        ''' <summary>Gets the latency introduced by this processor in samples (default: 0)</summary>
        Public Overridable ReadOnly Property LatencySamples As Integer
            Get
                Return 0 ' Default: no latency
            End Get
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Creates a new processor with specified format
        ''' </summary>
        ''' <param name="format">Wave format to process</param>
        Protected Sub New(format As WaveFormat)
            If format Is Nothing Then
                Throw New ArgumentNullException(NameOf(format))
            End If
            Me.Format = format
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Processes audio data
        ''' </summary>
        ''' <param name="buffer">Audio buffer to process</param>
        Public Sub Process(buffer As AudioBuffer)
            If disposed Then
                Throw New ObjectDisposedException(Me.GetType().Name)
            End If

            If buffer Is Nothing Then
                Throw New ArgumentNullException(NameOf(buffer))
            End If

            ' Validate format compatibility
            If Not IsFormatCompatible(buffer.Format) Then
                Throw New InvalidOperationException($"Buffer format incompatible with processor. Expected: {Format}, Got: {buffer.Format}")
            End If

            ' If disabled or bypassed, do nothing
            If Not _enabled OrElse _bypassed Then
                Return
            End If

            ' Process the audio
            ProcessInternal(buffer)
        End Sub

        ''' <summary>
        ''' Resets the processor to initial state
        ''' </summary>
        Public Overridable Sub Reset()
            ' Base implementation does nothing
            ' Derived classes can override to reset internal state
        End Sub

#End Region

#Region "Protected Methods"

        ''' <summary>
        ''' Derived classes implement actual processing logic here
        ''' </summary>
        ''' <param name="buffer">Audio buffer to process</param>
        Protected MustOverride Sub ProcessInternal(buffer As AudioBuffer)

        ''' <summary>
        ''' Called when Enabled property changes
        ''' </summary>
        Protected Overridable Sub OnEnabledChanged()
            ' Base implementation does nothing
            ' Derived classes can override to respond to enable/disable
        End Sub

        ''' <summary>
        ''' Called when Bypassed property changes
        ''' </summary>
        Protected Overridable Sub OnBypassedChanged()
            ' Base implementation does nothing
            ' Derived classes can override to respond to bypass changes
        End Sub

        ''' <summary>
        ''' Checks if the given format is compatible with this processor
        ''' </summary>
        ''' <param name="format">Format to check</param>
        ''' <returns>True if compatible</returns>
        Protected Overridable Function IsFormatCompatible(format As WaveFormat) As Boolean
            If format Is Nothing Then Return False
            
            ' Default: must match sample rate, channels, and bits per sample
            Return format.SampleRate = Me.Format.SampleRate AndAlso
                   format.Channels = Me.Format.Channels AndAlso
                   format.BitsPerSample = Me.Format.BitsPerSample
        End Function

#End Region

#Region "IDisposable"

        ''' <summary>
        ''' Disposes of resources
        ''' </summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        ''' <summary>
        ''' Disposes of resources
        ''' </summary>
        ''' <param name="disposing">True if disposing managed resources</param>
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposed Then
                If disposing Then
                    ' Dispose managed resources
                    ' Derived classes can override to clean up
                End If
                disposed = True
            End If
        End Sub

#End Region

    End Class

End Namespace
