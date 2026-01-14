Imports NAudio.Wave

Namespace DSP

    ''' <summary>
    ''' Managed audio buffer with format information.
    ''' Stores audio samples with associated metadata (sample rate, channels, bit depth).
    ''' </summary>
    Public Class AudioBuffer
        Implements IDisposable

#Region "Private Fields"

        Private _buffer As Byte()
        Private _sampleCount As Integer
        Private disposed As Boolean = False

#End Region

#Region "Properties"

        ''' <summary>Gets the wave format (sample rate, channels, bits per sample)</summary>
        Public ReadOnly Property Format As WaveFormat

        ''' <summary>Gets the raw byte buffer</summary>
        Public ReadOnly Property Buffer As Byte()
            Get
                Return _buffer
            End Get
        End Property

        ''' <summary>Gets or sets the number of valid bytes in the buffer</summary>
        Public Property ByteCount As Integer

        ''' <summary>Gets the number of samples in the buffer</summary>
        Public ReadOnly Property SampleCount As Integer
            Get
                Return _sampleCount
            End Get
        End Property

        ''' <summary>Gets the buffer capacity in bytes</summary>
        Public ReadOnly Property Capacity As Integer
            Get
                Return If(_buffer IsNot Nothing, _buffer.Length, 0)
            End Get
        End Property

        ''' <summary>Gets the duration of audio in the buffer</summary>
        Public ReadOnly Property Duration As TimeSpan
            Get
                If Format Is Nothing OrElse ByteCount = 0 Then Return TimeSpan.Zero
                Dim seconds = ByteCount / CDbl(Format.AverageBytesPerSecond)
                Return TimeSpan.FromSeconds(seconds)
            End Get
        End Property

#End Region

#Region "Constructors"

        ''' <summary>
        ''' Creates a new audio buffer with specified format and capacity
        ''' </summary>
        ''' <param name="format">Wave format (sample rate, channels, bits)</param>
        ''' <param name="durationMs">Buffer duration in milliseconds</param>
        Public Sub New(format As WaveFormat, durationMs As Integer)
            If format Is Nothing Then
                Throw New ArgumentNullException(NameOf(format))
            End If
            If durationMs <= 0 Then
                Throw New ArgumentException("Duration must be greater than zero", NameOf(durationMs))
            End If

            Me.Format = format

            ' Calculate buffer size
            Dim bytesPerMs = format.AverageBytesPerSecond / 1000
            Dim bufferSize = bytesPerMs * durationMs

            ' Align to block boundary
            bufferSize = (bufferSize \ format.BlockAlign) * format.BlockAlign

            _buffer = New Byte(bufferSize - 1) {}
            ByteCount = 0
            UpdateSampleCount()
        End Sub

        ''' <summary>
        ''' Creates a new audio buffer with specified format and byte capacity
        ''' </summary>
        ''' <param name="format">Wave format</param>
        ''' <param name="bufferSize">Buffer size in bytes</param>
        ''' <param name="dummy">Dummy parameter to differentiate constructor</param>
        Public Sub New(format As WaveFormat, bufferSize As Integer, dummy As Boolean)
            If format Is Nothing Then
                Throw New ArgumentNullException(NameOf(format))
            End If
            If bufferSize <= 0 Then
                Throw New ArgumentException("Buffer size must be greater than zero", NameOf(bufferSize))
            End If

            Me.Format = format

            ' Align to block boundary
            bufferSize = (bufferSize \ format.BlockAlign) * format.BlockAlign

            _buffer = New Byte(bufferSize - 1) {}
            ByteCount = 0
            UpdateSampleCount()
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Clears the buffer (sets all bytes to zero)
        ''' </summary>
        Public Sub Clear()
            If _buffer IsNot Nothing Then
                Array.Clear(_buffer, 0, _buffer.Length)
            End If
            ByteCount = 0
            _sampleCount = 0
        End Sub

        ''' <summary>
        ''' Copies data from source array to this buffer
        ''' </summary>
        ''' <param name="source">Source array</param>
        ''' <param name="sourceOffset">Offset in source</param>
        ''' <param name="count">Number of bytes to copy</param>
        Public Sub CopyFrom(source As Byte(), sourceOffset As Integer, count As Integer)
            If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
            If sourceOffset < 0 OrElse sourceOffset >= source.Length Then
                Throw New ArgumentOutOfRangeException(NameOf(sourceOffset))
            End If
            If count < 0 OrElse sourceOffset + count > source.Length Then
                Throw New ArgumentOutOfRangeException(NameOf(count))
            End If
            If count > Capacity Then
                Throw New ArgumentException("Count exceeds buffer capacity", NameOf(count))
            End If

            Array.Copy(source, sourceOffset, _buffer, 0, count)
            ByteCount = count
            UpdateSampleCount()
        End Sub

        ''' <summary>
        ''' Copies data from this buffer to destination array
        ''' </summary>
        ''' <param name="destination">Destination array</param>
        ''' <param name="destinationOffset">Offset in destination</param>
        ''' <param name="count">Number of bytes to copy</param>
        Public Sub CopyTo(destination As Byte(), destinationOffset As Integer, count As Integer)
            If destination Is Nothing Then Throw New ArgumentNullException(NameOf(destination))
            If destinationOffset < 0 OrElse destinationOffset >= destination.Length Then
                Throw New ArgumentOutOfRangeException(NameOf(destinationOffset))
            End If
            If count < 0 OrElse count > ByteCount Then
                Throw New ArgumentOutOfRangeException(NameOf(count))
            End If
            If destinationOffset + count > destination.Length Then
                Throw New ArgumentException("Destination array too small")
            End If

            Array.Copy(_buffer, 0, destination, destinationOffset, count)
        End Sub

        ''' <summary>
        ''' Resizes the buffer to a new capacity
        ''' </summary>
        ''' <param name="newSize">New size in bytes</param>
        Public Sub Resize(newSize As Integer)
            If newSize <= 0 Then
                Throw New ArgumentException("Size must be greater than zero", NameOf(newSize))
            End If

            ' Align to block boundary
            newSize = (newSize \ Format.BlockAlign) * Format.BlockAlign

            Array.Resize(_buffer, newSize)

            ' Adjust byte count if necessary
            If ByteCount > newSize Then
                ByteCount = newSize
            End If

            UpdateSampleCount()
        End Sub

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' Updates the sample count based on current byte count and format
        ''' </summary>
        Private Sub UpdateSampleCount()
            If Format IsNot Nothing AndAlso ByteCount > 0 Then
                _sampleCount = ByteCount \ Format.BlockAlign
            Else
                _sampleCount = 0
            End If
        End Sub

#End Region

#Region "IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            If Not disposed Then
                Clear()
                _buffer = Nothing
                disposed = True
            End If
        End Sub

#End Region

    End Class

End Namespace
