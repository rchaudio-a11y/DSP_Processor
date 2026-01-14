Imports System.Threading

Namespace Utils

    ''' <summary>
    ''' Lock-free circular buffer for audio data transfer between threads.
    ''' Uses atomic operations for thread-safe producer/consumer pattern.
    ''' </summary>
    ''' <remarks>
    ''' Thread-safe for single producer, single consumer (SPSC).
    ''' Designed for low-latency audio processing with no locks.
    ''' </remarks>
    Public Class RingBuffer
        Implements IDisposable

#Region "Private Fields"

        Private ReadOnly buffer As Byte()
        Private ReadOnly _capacity As Integer
        Private writePosition As Integer = 0
        Private readPosition As Integer = 0
        Private disposed As Boolean = False

#End Region

#Region "Properties"

        ''' <summary>Gets the total capacity of the ring buffer in bytes</summary>
        Public ReadOnly Property Capacity As Integer
            Get
                Return _capacity
            End Get
        End Property

        ''' <summary>Gets the number of bytes available to read</summary>
        Public ReadOnly Property Available As Integer
            Get
                Dim write = Volatile.Read(writePosition)
                Dim read = Volatile.Read(readPosition)

                If write >= read Then
                    Return write - read
                Else
                    Return _capacity - read + write
                End If
            End Get
        End Property

        ''' <summary>Gets the number of bytes available to write</summary>
        Public ReadOnly Property FreeSpace As Integer
            Get
                Return _capacity - Available - 1
            End Get
        End Property

        ''' <summary>Gets whether the buffer is empty</summary>
        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return Available = 0
            End Get
        End Property

        ''' <summary>Gets whether the buffer is full</summary>
        Public ReadOnly Property IsFull As Boolean
            Get
                Return FreeSpace = 0
            End Get
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Creates a new ring buffer with specified capacity
        ''' </summary>
        ''' <param name="size">Size in bytes (will be rounded up to power of 2)</param>
        Public Sub New(size As Integer)
            If size <= 0 Then
                Throw New ArgumentException("Size must be greater than zero", NameOf(size))
            End If

            ' Round up to next power of 2 for efficient wrapping
            _capacity = RoundUpToPowerOfTwo(size)
            buffer = New Byte(_capacity - 1) {}
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Writes data to the ring buffer
        ''' </summary>
        ''' <param name="data">Data to write</param>
        ''' <param name="offset">Offset in source array</param>
        ''' <param name="count">Number of bytes to write</param>
        ''' <returns>Number of bytes actually written</returns>
        Public Function Write(data As Byte(), offset As Integer, count As Integer) As Integer
            If disposed Then Throw New ObjectDisposedException(NameOf(RingBuffer))
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            If offset < 0 OrElse offset >= data.Length Then Throw New ArgumentOutOfRangeException(NameOf(offset))
            If count < 0 OrElse offset + count > data.Length Then Throw New ArgumentOutOfRangeException(NameOf(count))

            ' Limit write to available space
            Dim toWrite = Math.Min(count, FreeSpace)
            If toWrite = 0 Then Return 0

            Dim writePos = Volatile.Read(writePosition)
            Dim firstPart = Math.Min(toWrite, _capacity - writePos)

            ' Write first part (up to end of buffer)
            Array.Copy(data, offset, buffer, writePos, firstPart)

            ' Write second part (wrapped around to beginning)
            If firstPart < toWrite Then
                Array.Copy(data, offset + firstPart, buffer, 0, toWrite - firstPart)
            End If

            ' Update write position atomically
            Volatile.Write(writePosition, (writePos + toWrite) Mod _capacity)

            Return toWrite
        End Function

        ''' <summary>
        ''' Reads data from the ring buffer
        ''' </summary>
        ''' <param name="data">Destination array</param>
        ''' <param name="offset">Offset in destination array</param>
        ''' <param name="count">Number of bytes to read</param>
        ''' <returns>Number of bytes actually read</returns>
        Public Function Read(data As Byte(), offset As Integer, count As Integer) As Integer
            If disposed Then Throw New ObjectDisposedException(NameOf(RingBuffer))
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            If offset < 0 OrElse offset >= data.Length Then Throw New ArgumentOutOfRangeException(NameOf(offset))
            If count < 0 OrElse offset + count > data.Length Then Throw New ArgumentOutOfRangeException(NameOf(count))

            ' Limit read to available data
            Dim toRead = Math.Min(count, Available)
            If toRead = 0 Then Return 0

            Dim readPos = Volatile.Read(readPosition)
            Dim firstPart = Math.Min(toRead, _capacity - readPos)

            ' Read first part (up to end of buffer)
            Array.Copy(buffer, readPos, data, offset, firstPart)

            ' Read second part (wrapped around to beginning)
            If firstPart < toRead Then
                Array.Copy(buffer, 0, data, offset + firstPart, toRead - firstPart)
            End If

            ' Update read position atomically
            Volatile.Write(readPosition, (readPos + toRead) Mod _capacity)

            Return toRead
        End Function

        ''' <summary>
        ''' Clears all data from the buffer
        ''' </summary>
        Public Sub Clear()
            Volatile.Write(readPosition, 0)
            Volatile.Write(writePosition, 0)
            Array.Clear(buffer, 0, buffer.Length)
        End Sub

        ''' <summary>
        ''' Advances the read position without actually reading data (skip/discard)
        ''' </summary>
        ''' <param name="count">Number of bytes to skip</param>
        ''' <returns>Number of bytes actually skipped</returns>
        Public Function Skip(count As Integer) As Integer
            If disposed Then Throw New ObjectDisposedException(NameOf(RingBuffer))
            If count < 0 Then Throw New ArgumentOutOfRangeException(NameOf(count))

            Dim toSkip = Math.Min(count, Available)
            If toSkip = 0 Then Return 0

            Dim readPos = Volatile.Read(readPosition)
            Volatile.Write(readPosition, (readPos + toSkip) Mod _capacity)

            Return toSkip
        End Function

#End Region

#Region "Helper Methods"

        ''' <summary>
        ''' Rounds up to the next power of 2
        ''' </summary>
        Private Shared Function RoundUpToPowerOfTwo(value As Integer) As Integer
            value -= 1
            value = value Or (value >> 1)
            value = value Or (value >> 2)
            value = value Or (value >> 4)
            value = value Or (value >> 8)
            value = value Or (value >> 16)
            Return value + 1
        End Function

#End Region

#Region "IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            disposed = True
            Clear()
        End Sub

#End Region

    End Class

End Namespace
