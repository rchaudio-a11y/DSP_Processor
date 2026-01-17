Imports System.Threading

Namespace Utils

    ''' <summary>
    ''' Multi-reader circular buffer implementation for DSP tap points.
    ''' Supports multiple independent readers without contention.
    ''' Thread-safe for concurrent reads and writes.
    ''' 
    ''' Architecture Rule #4: "Tap points use multi-reader ring buffers so multiple 
    ''' instruments can read the same audio independently."
    ''' </summary>
    Public Class MultiReaderRingBuffer
        Implements IDisposable

#Region "Private Fields"

        Private ReadOnly buffer As Byte()
        Private ReadOnly capacity As Integer
        Private writePosition As Integer = 0
        Private ReadOnly readerLock As New Object()
        Private disposed As Boolean = False

        ''' <summary>Tracks state for each registered reader</summary>
        Private Class ReaderState
            Public Property Position As Integer
            Public Property LastReadTime As DateTime
            Public Property Name As String
        End Class

        Private ReadOnly readers As New Dictionary(Of String, ReaderState)

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Creates a new multi-reader ring buffer
        ''' </summary>
        ''' <param name="bufferSize">Size of buffer in bytes (must be power of 2 for optimal performance)</param>
        Public Sub New(bufferSize As Integer)
            If bufferSize <= 0 Then
                Throw New ArgumentException("Buffer size must be greater than zero", NameOf(bufferSize))
            End If

            capacity = bufferSize
            buffer = New Byte(capacity - 1) {}

            Utils.Logger.Instance.Debug($"MultiReaderRingBuffer created: {capacity} bytes", "MultiReaderRingBuffer")
        End Sub

#End Region

#Region "Writer Methods"

        ''' <summary>
        ''' Write data to the ring buffer (all readers will see this data)
        ''' Thread-safe: Can be called from audio callback thread
        ''' </summary>
        Public Function Write(data As Byte(), offset As Integer, count As Integer) As Integer
            If disposed Then
                Throw New ObjectDisposedException(NameOf(MultiReaderRingBuffer))
            End If

            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data))
            End If

            If offset < 0 OrElse count < 0 OrElse offset + count > data.Length Then
                Throw New ArgumentOutOfRangeException()
            End If

            If count = 0 Then Return 0

            SyncLock readerLock
                ' Calculate how much we can write
                Dim toWrite = Math.Min(count, capacity)

                ' Write in up to two chunks (handling wraparound)
                Dim writePos = writePosition
                Dim remaining = capacity - writePos

                If toWrite <= remaining Then
                    ' Single chunk write
                    Array.Copy(data, offset, buffer, writePos, toWrite)
                Else
                    ' Two chunk write (wraparound)
                    Array.Copy(data, offset, buffer, writePos, remaining)
                    Array.Copy(data, offset + remaining, buffer, 0, toWrite - remaining)
                End If

                ' Advance write position
                writePosition = (writePosition + toWrite) Mod capacity

                Return toWrite
            End SyncLock
        End Function

        ''' <summary>
        ''' Get current write position (for diagnostics)
        ''' </summary>
        Public ReadOnly Property CurrentWritePosition As Integer
            Get
                SyncLock readerLock
                    Return writePosition
                End SyncLock
            End Get
        End Property

#End Region

#Region "Reader Management"

        ''' <summary>
        ''' Create a new independent reader cursor
        ''' </summary>
        ''' <param name="name">Unique name for this reader (e.g., "InputFFT", "OutputMeter")</param>
        ''' <returns>Reader name for use in Read() calls</returns>
        Public Function CreateReader(name As String) As String
            If disposed Then
                Throw New ObjectDisposedException(NameOf(MultiReaderRingBuffer))
            End If

            If String.IsNullOrWhiteSpace(name) Then
                Throw New ArgumentException("Reader name cannot be empty", NameOf(name))
            End If

            SyncLock readerLock
                If readers.ContainsKey(name) Then
                    Throw New InvalidOperationException($"Reader '{name}' already exists!")
                End If

                ' Create new reader starting at current write position
                ' (will read new data written after creation)
                readers(name) = New ReaderState With {
                    .Position = writePosition,
                    .LastReadTime = DateTime.Now,
                    .Name = name
                }

                Utils.Logger.Instance.Debug($"Reader created: '{name}' at position {writePosition}", "MultiReaderRingBuffer")

                Return name
            End SyncLock
        End Function

        ''' <summary>
        ''' Remove a reader cursor
        ''' </summary>
        Public Sub RemoveReader(name As String)
            If String.IsNullOrWhiteSpace(name) Then Return

            SyncLock readerLock
                If readers.Remove(name) Then
                    Utils.Logger.Instance.Debug($"Reader removed: '{name}'", "MultiReaderRingBuffer")
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Check if a reader exists
        ''' </summary>
        Public Function HasReader(name As String) As Boolean
            SyncLock readerLock
                Return readers.ContainsKey(name)
            End SyncLock
        End Function

        ''' <summary>
        ''' Get count of active readers
        ''' </summary>
        Public ReadOnly Property ReaderCount As Integer
            Get
                SyncLock readerLock
                    Return readers.Count
                End SyncLock
            End Get
        End Property

#End Region

#Region "Reader Methods"

        ''' <summary>
        ''' Read data for a specific reader cursor
        ''' Each reader maintains independent position
        ''' Thread-safe: Multiple readers can read simultaneously
        ''' </summary>
        Public Function Read(readerName As String, output As Byte(), offset As Integer, count As Integer) As Integer
            If disposed Then
                Throw New ObjectDisposedException(NameOf(MultiReaderRingBuffer))
            End If

            If String.IsNullOrWhiteSpace(readerName) Then
                Throw New ArgumentException("Reader name cannot be empty", NameOf(readerName))
            End If

            If output Is Nothing Then
                Throw New ArgumentNullException(NameOf(output))
            End If

            If offset < 0 OrElse count < 0 OrElse offset + count > output.Length Then
                Throw New ArgumentOutOfRangeException()
            End If

            If count = 0 Then Return 0

            SyncLock readerLock
                ' Verify reader exists
                If Not readers.ContainsKey(readerName) Then
                    Throw New InvalidOperationException($"Reader '{readerName}' does not exist! Call CreateReader() first.")
                End If

                Dim reader = readers(readerName)

                ' Calculate available data for THIS reader
                Dim available = CalculateAvailable(reader.Position, writePosition)
                Dim toRead = Math.Min(count, available)

                If toRead = 0 Then Return 0

                ' Read from reader's independent position
                Dim bytesRead = ReadInternal(output, offset, toRead, reader.Position)

                ' Advance ONLY this reader's cursor
                reader.Position = (reader.Position + bytesRead) Mod capacity
                reader.LastReadTime = DateTime.Now

                Return bytesRead
            End SyncLock
        End Function

        ''' <summary>
        ''' Get bytes available for a specific reader
        ''' </summary>
        Public Function Available(readerName As String) As Integer
            If String.IsNullOrWhiteSpace(readerName) Then Return 0

            SyncLock readerLock
                If Not readers.ContainsKey(readerName) Then Return 0

                Dim reader = readers(readerName)
                Return CalculateAvailable(reader.Position, writePosition)
            End SyncLock
        End Function

        ''' <summary>
        ''' Internal read that doesn't advance position (used by Read())
        ''' </summary>
        Private Function ReadInternal(output As Byte(), offset As Integer, count As Integer, fromPosition As Integer) As Integer
            Dim remaining = capacity - fromPosition

            If count <= remaining Then
                ' Single chunk read
                Array.Copy(buffer, fromPosition, output, offset, count)
                Return count
            Else
                ' Two chunk read (wraparound)
                Array.Copy(buffer, fromPosition, output, offset, remaining)
                Array.Copy(buffer, 0, output, offset + remaining, count - remaining)
                Return count
            End If
        End Function

        ''' <summary>
        ''' Calculate bytes available between reader and writer positions
        ''' </summary>
        Private Function CalculateAvailable(readerPos As Integer, writerPos As Integer) As Integer
            If writerPos >= readerPos Then
                Return writerPos - readerPos
            Else
                ' Wraparound case
                Return capacity - readerPos + writerPos
            End If
        End Function

#End Region

#Region "Maintenance"

        ''' <summary>
        ''' Detect readers that haven't read in a while (potential memory leaks)
        ''' </summary>
        ''' <param name="timeoutSeconds">Seconds of inactivity before considered stale</param>
        ''' <returns>List of stale reader names</returns>
        Public Function GetStaleReaders(timeoutSeconds As Integer) As List(Of String)
            Dim stale As New List(Of String)
            Dim now = DateTime.Now

            SyncLock readerLock
                For Each kvp In readers
                    If (now - kvp.Value.LastReadTime).TotalSeconds > timeoutSeconds Then
                        stale.Add(kvp.Key)
                    End If
                Next
            End SyncLock

            Return stale
        End Function

        ''' <summary>
        ''' Remove all stale readers (cleanup)
        ''' </summary>
        Public Function RemoveStaleReaders(timeoutSeconds As Integer) As Integer
            Dim staleReaders = GetStaleReaders(timeoutSeconds)

            For Each name In staleReaders
                RemoveReader(name)
            Next

            If staleReaders.Count > 0 Then
                Utils.Logger.Instance.Warning($"Removed {staleReaders.Count} stale readers", "MultiReaderRingBuffer")
            End If

            Return staleReaders.Count
        End Function

        ''' <summary>
        ''' Clear all readers (for reset/cleanup)
        ''' </summary>
        Public Sub ClearAllReaders()
            SyncLock readerLock
                Dim count = readers.Count
                readers.Clear()
                Utils.Logger.Instance.Debug($"Cleared {count} readers", "MultiReaderRingBuffer")
            End SyncLock
        End Sub

#End Region

#Region "IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            If Not disposed Then
                SyncLock readerLock
                    readers.Clear()
                End SyncLock
                disposed = True
                Utils.Logger.Instance.Debug("MultiReaderRingBuffer disposed", "MultiReaderRingBuffer")
            End If
        End Sub

#End Region

    End Class

End Namespace
