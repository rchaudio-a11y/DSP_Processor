Imports System.Threading

Namespace Utils

    ''' <summary>
    ''' Per-reader statistics snapshot (feature 003-tap-consolidation).
    ''' Counters are monotonic - querying never clears them.
    ''' </summary>
    Public Structure ReaderStats
        ''' <summary>Every detected overrun occurrence</summary>
        Public OverrunEvents As Integer
        ''' <summary>Transitions into overrun after a clean read (rate-limit unit)</summary>
        Public OverrunEpisodeCount As Integer
        ''' <summary>Accumulated bytes skipped by resyncs</summary>
        Public TotalBytesLost As Long
        ''' <summary>Bytes available right now (post-detection, clamped to capacity)</summary>
        Public Pending As Long
        ''' <summary>When the rate-limited overrun log last fired (DateTime.MinValue = never)</summary>
        Public LastOverrunLogEmitted As DateTime
    End Structure

    ''' <summary>
    ''' Multi-reader circular buffer implementation for DSP tap points.
    ''' Supports multiple independent readers, each with its own cursor and
    ''' overrun (data-loss) detection.
    '''
    ''' THREADING: writer (DSP thread) and readers (UI thread) share ONE lock
    ''' (readerLock) - a reader mid-copy briefly blocks the writer. This
    ''' single-lock design is retained deliberately (feature 003 scope
    ''' decision; contention redesign deferred - see review finding P4).
    '''
    ''' OVERRUN DETECTION (feature 003, research R2): positions are monotonic
    ''' Long byte counters that never wrap - totalBytesWritten for the writer,
    ''' TotalBytesRead per reader. pending = written - read; overrun iff
    ''' pending > capacity (loss = pending - capacity). Mod is used ONLY for
    ''' physical indexing, never for distance math, so a reader lapped by
    ''' exactly N full buffers is arithmetically visible, not aliased to zero.
    '''
    ''' Architecture Rule #4: "Tap points use multi-reader ring buffers so multiple
    ''' instruments can read the same audio independently."
    ''' </summary>
    Public Class MultiReaderRingBuffer
        Implements IDisposable

#Region "Private Fields"

        Private ReadOnly buffer As Byte()
        Private ReadOnly capacity As Integer
        Private totalBytesWritten As Long = 0 ' monotonic, guarded by readerLock
        Private ReadOnly readerLock As New Object()
        Private disposed As Boolean = False

        ''' <summary>Minimum interval between overrun log emissions per reader (analysis B1)</summary>
        Private Shared ReadOnly OverrunLogInterval As TimeSpan = TimeSpan.FromSeconds(1)

        ''' <summary>Tracks state for each registered reader</summary>
        Private Class ReaderState
            Public Property TotalBytesRead As Long ' monotonic, guarded by readerLock
            Public Property LastReadTime As DateTime
            Public Property Name As String
            Public Property OverrunEvents As Integer
            Public Property OverrunEpisodeCount As Integer
            Public Property TotalBytesLost As Long
            Public Property LastOverrunLogEmitted As DateTime = DateTime.MinValue
            Public Property InOverrunEpisode As Boolean = False
        End Class

        Private ReadOnly readers As New Dictionary(Of String, ReaderState)

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Creates a new multi-reader ring buffer
        ''' </summary>
        ''' <param name="bufferSize">Size of buffer in bytes</param>
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
        ''' Write data to the ring buffer (all readers will see this data).
        ''' Thread-safe: called from the DSP worker thread.
        ''' CONSTITUTION IV: no allocation, no logging, no overrun bookkeeping
        ''' on this path - detection happens on reader-side calls only.
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
                ' A single write can deposit at most one full buffer
                Dim toWrite = Math.Min(count, capacity)

                ' Physical index from the monotonic counter
                Dim writePos = CInt(totalBytesWritten Mod capacity)
                Dim remaining = capacity - writePos

                If toWrite <= remaining Then
                    ' Single chunk write
                    Array.Copy(data, offset, buffer, writePos, toWrite)
                Else
                    ' Two chunk write (wraparound)
                    Array.Copy(data, offset, buffer, writePos, remaining)
                    Array.Copy(data, offset + remaining, buffer, 0, toWrite - remaining)
                End If

                ' Advance the monotonic counter (never wraps: Long overflows in ~1.6M years at audio rates)
                totalBytesWritten += toWrite

                Return toWrite
            End SyncLock
        End Function

        ''' <summary>
        ''' Get current physical write position (for diagnostics)
        ''' </summary>
        Public ReadOnly Property CurrentWritePosition As Integer
            Get
                SyncLock readerLock
                    Return CInt(totalBytesWritten Mod capacity)
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

                ' New readers start at the live edge (no phantom backlog, no false overrun)
                readers(name) = New ReaderState With {
                    .TotalBytesRead = totalBytesWritten,
                    .LastReadTime = DateTime.Now,
                    .Name = name
                }

                Utils.Logger.Instance.Debug($"Reader created: '{name}' at byte position {totalBytesWritten}", "MultiReaderRingBuffer")

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
        ''' Read data for a specific reader cursor. Each reader maintains an
        ''' independent position. If the writer overwrote data this reader had
        ''' not consumed, the reader is resynchronized to the oldest valid byte
        ''' first (loss recorded in stats), so the returned audio is ALWAYS
        ''' contiguous - never a splice across the loss (FR-007).
        ''' </summary>
        Public Function Read(readerName As String, output As Byte(), offset As Integer, count As Integer) As Integer
            Dim ignoredLoss As Long
            Return Read(readerName, output, offset, count, ignoredLoss)
        End Function

        ''' <summary>
        ''' Read overload reporting data loss at the call site: bytesLost is the
        ''' number of bytes skipped by a resync performed during THIS call
        ''' (0 if the reader had not been lapped). See contracts/monitor-reader-api.md.
        ''' </summary>
        Public Function Read(readerName As String, output As Byte(), offset As Integer, count As Integer, ByRef bytesLost As Long) As Integer
            bytesLost = 0

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

                ' Detect lap, record loss, resync to oldest valid byte (FR-005/006/007)
                bytesLost = CheckOverrunAndResync(reader)

                ' Pending is now guaranteed <= capacity
                Dim pending = totalBytesWritten - reader.TotalBytesRead
                Dim toRead = CInt(Math.Min(CLng(count), pending))

                If toRead = 0 Then Return 0

                ' Read from this reader's physical position
                Dim fromPos = CInt(reader.TotalBytesRead Mod capacity)
                Dim bytesRead = ReadInternal(output, offset, toRead, fromPos)

                ' Advance ONLY this reader's monotonic counter
                reader.TotalBytesRead += bytesRead
                reader.LastReadTime = DateTime.Now

                Return bytesRead
            End SyncLock
        End Function

        ''' <summary>
        ''' Get bytes available for a specific reader (0..capacity).
        ''' Performs the same detect-and-resync as Read, so a stalled poller
        ''' learns of loss on its usual path (research R3).
        ''' </summary>
        Public Function Available(readerName As String) As Integer
            If String.IsNullOrWhiteSpace(readerName) Then Return 0

            SyncLock readerLock
                If Not readers.ContainsKey(readerName) Then Return 0

                Dim reader = readers(readerName)
                CheckOverrunAndResync(reader)
                Return CInt(totalBytesWritten - reader.TotalBytesRead)
            End SyncLock
        End Function

        ''' <summary>
        ''' Per-reader statistics snapshot. Monotonic - never clears counters.
        ''' Performs detect-and-resync first (a stats query is a reader-side
        ''' interaction per FR-005).
        ''' </summary>
        Public Function GetReaderStats(readerName As String) As ReaderStats
            If disposed Then
                Throw New ObjectDisposedException(NameOf(MultiReaderRingBuffer))
            End If

            SyncLock readerLock
                If Not readers.ContainsKey(readerName) Then
                    Throw New InvalidOperationException($"Reader '{readerName}' does not exist! Call CreateReader() first.")
                End If

                Dim reader = readers(readerName)
                CheckOverrunAndResync(reader)

                Return New ReaderStats With {
                    .OverrunEvents = reader.OverrunEvents,
                    .OverrunEpisodeCount = reader.OverrunEpisodeCount,
                    .TotalBytesLost = reader.TotalBytesLost,
                    .Pending = totalBytesWritten - reader.TotalBytesRead,
                    .LastOverrunLogEmitted = reader.LastOverrunLogEmitted
                }
            End SyncLock
        End Function

        ''' <summary>
        ''' Overrun predicate + resync (data-model.md invariant 3). MUST be
        ''' called under readerLock. Returns bytes lost (0 if no overrun).
        ''' Episode semantics (analysis B1/E1): an episode begins on the first
        ''' overrun after a clean check; the log fires only on that transition,
        ''' rate-limited to one emission per OverrunLogInterval per reader.
        ''' </summary>
        Private Function CheckOverrunAndResync(reader As ReaderState) As Long
            Dim pending = totalBytesWritten - reader.TotalBytesRead

            If pending <= capacity Then
                ' Clean check - any overrun episode is over
                reader.InOverrunEpisode = False
                Return 0
            End If

            Dim lost = pending - capacity
            reader.TotalBytesRead = totalBytesWritten - capacity ' oldest still-valid byte
            reader.OverrunEvents += 1
            reader.TotalBytesLost += lost

            If Not reader.InOverrunEpisode Then
                reader.InOverrunEpisode = True
                reader.OverrunEpisodeCount += 1

                ' Rate-limited log, emitted on episode transition only (FR-008)
                Dim now = DateTime.Now
                If now - reader.LastOverrunLogEmitted >= OverrunLogInterval Then
                    reader.LastOverrunLogEmitted = now
                    Utils.Logger.Instance.Warning(
                        $"Overrun: reader '{reader.Name}' lost {lost} bytes (episode {reader.OverrunEpisodeCount}, events {reader.OverrunEvents}, total lost {reader.TotalBytesLost})",
                        "MultiReaderRingBuffer")
                End If
            End If

            Return lost
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
