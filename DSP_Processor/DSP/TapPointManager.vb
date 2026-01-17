Imports System.Collections.Generic

Namespace DSP

    ''' <summary>
    ''' Internal state for a tap point reader.
    ''' Tracks which buffer the reader is attached to and its unique ID.
    ''' </summary>
    Friend Class TapPointReader
        ''' <summary>User-friendly name for this reader (e.g., "InputMeter")</summary>
        Public Property Name As String

        ''' <summary>Which tap point this reader is monitoring</summary>
        Public Property TapLocation As TapPoint

        ''' <summary>Reference to the multi-reader ring buffer</summary>
        Public Property Buffer As Utils.MultiReaderRingBuffer

        ''' <summary>Ring buffer's internal reader ID (returned by CreateReader)</summary>
        Public Property RingBufferReaderId As String
    End Class

    ''' <summary>
    ''' Manages tap point reader lifecycle and provides unified API for accessing
    ''' DSP signal chain monitoring buffers. Thread-safe for multiple readers.
    ''' </summary>
    Public Class TapPointManager
        Implements IDisposable

#Region "Private Fields"

        Private ReadOnly dspThread As DSPThread
        Private ReadOnly readers As Dictionary(Of String, TapPointReader)
        Private ReadOnly readerLock As New Object()
        Private disposed As Boolean = False

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Create a new tap point manager for a DSP thread
        ''' </summary>
        ''' <param name="thread">DSPThread to monitor</param>
        Public Sub New(thread As DSPThread)
            If thread Is Nothing Then
                Throw New ArgumentNullException(NameOf(thread))
            End If

            dspThread = thread
            readers = New Dictionary(Of String, TapPointReader)()
        End Sub

#End Region

#Region "Public API"

        ''' <summary>
        ''' Create a named reader for a specific tap point.
        ''' Each reader maintains independent read position in the ring buffer.
        ''' </summary>
        ''' <param name="tap">Which tap point to read from</param>
        ''' <param name="readerName">Unique name for this reader (e.g., "InputMeter")</param>
        ''' <returns>Reader ID (same as readerName)</returns>
        ''' <exception cref="ArgumentException">If reader name already exists</exception>
        Public Function CreateReader(tap As TapPoint, readerName As String) As String
            If String.IsNullOrWhiteSpace(readerName) Then
                Throw New ArgumentException("Reader name cannot be empty", NameOf(readerName))
            End If

            SyncLock readerLock
                If readers.ContainsKey(readerName) Then
                    Throw New ArgumentException($"Reader '{readerName}' already exists")
                End If

                ' Get appropriate buffer based on tap point
                Dim buffer = GetBufferForTap(tap)
                If buffer Is Nothing Then
                    Throw New InvalidOperationException($"Buffer for tap point {tap} not available")
                End If

                ' Create reader in ring buffer
                Dim ringReaderId = buffer.CreateReader(readerName)

                ' Store reader state
                Dim reader As New TapPointReader With {
                    .Name = readerName,
                    .TapLocation = tap,
                    .Buffer = buffer,
                    .RingBufferReaderId = ringReaderId
                }

                readers.Add(readerName, reader)
                Utils.Logger.Instance.Info($"Created tap point reader '{readerName}' for {tap}", "TapPointManager")

                Return readerName
            End SyncLock
        End Function

        ''' <summary>
        ''' Read from a previously created reader.
        ''' Non-blocking - returns immediately with available data.
        ''' </summary>
        ''' <param name="readerId">Reader ID returned by CreateReader</param>
        ''' <param name="buffer">Destination buffer</param>
        ''' <param name="offset">Offset in destination buffer</param>
        ''' <param name="count">Maximum bytes to read</param>
        ''' <returns>Actual bytes read (may be less than count)</returns>
        ''' <exception cref="ArgumentException">If reader ID not found</exception>
        Public Function Read(readerId As String, buffer As Byte(), offset As Integer, count As Integer) As Integer
            ThrowIfDisposed()

            SyncLock readerLock
                If Not readers.ContainsKey(readerId) Then
                    Throw New ArgumentException($"Reader '{readerId}' not found. Did you call CreateReader?")
                End If

                Dim reader = readers(readerId)
                Return reader.Buffer.Read(reader.RingBufferReaderId, buffer, offset, count)
            End SyncLock
        End Function

        ''' <summary>
        ''' Check how many bytes are available without reading.
        ''' Useful for determining buffer size before reading.
        ''' </summary>
        ''' <param name="readerId">Reader ID</param>
        ''' <returns>Number of bytes available to read</returns>
        Public Function Available(readerId As String) As Integer
            ThrowIfDisposed()

            SyncLock readerLock
                If Not readers.ContainsKey(readerId) Then
                    Throw New ArgumentException($"Reader '{readerId}' not found")
                End If

                Dim reader = readers(readerId)
                Return reader.Buffer.Available(reader.RingBufferReaderId)
            End SyncLock
        End Function

        ''' <summary>
        ''' Destroy a reader when no longer needed.
        ''' Frees resources and removes reader from ring buffer.
        ''' </summary>
        ''' <param name="readerId">Reader ID to destroy</param>
        Public Sub DestroyReader(readerId As String)
            SyncLock readerLock
                If readers.ContainsKey(readerId) Then
                    Dim reader = readers(readerId)
                    reader.Buffer.RemoveReader(reader.RingBufferReaderId)
                    readers.Remove(readerId)
                    Utils.Logger.Instance.Info($"Destroyed tap point reader '{readerId}'", "TapPointManager")
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Get list of active reader names
        ''' </summary>
        Public Function GetActiveReaders() As String()
            SyncLock readerLock
                Return readers.Keys.ToArray()
            End SyncLock
        End Function

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' Get the appropriate ring buffer for a tap point.
        ''' Maps enum to actual buffer fields on DSPThread.
        ''' </summary>
        Private Function GetBufferForTap(tap As TapPoint) As Utils.MultiReaderRingBuffer
            Select Case tap
                Case TapPoint.PreDSP
                    Return dspThread.inputMonitorBuffer

                Case TapPoint.PostInputGain
                    Return dspThread.postGainMonitorBuffer

                Case TapPoint.PostOutputGain
                    Return dspThread.postOutputGainMonitorBuffer

                Case TapPoint.PreOutput
                    Return dspThread.outputMonitorBuffer

                Case Else
                    Utils.Logger.Instance.Error($"Unknown tap point: {tap}", Nothing, "TapPointManager")
                    Return Nothing
            End Select
        End Function

        Private Sub ThrowIfDisposed()
            If disposed Then
                Throw New ObjectDisposedException("TapPointManager")
            End If
        End Sub

#End Region

#Region "IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposed Then
                If disposing Then
                    ' Clean up all readers
                    SyncLock readerLock
                        For Each reader In readers.Values
                            Try
                                reader.Buffer.RemoveReader(reader.RingBufferReaderId)
                            Catch ex As Exception
                                Utils.Logger.Instance.Warning($"Failed to destroy reader '{reader.Name}': {ex.Message}", "TapPointManager")
                            End Try
                        Next
                        readers.Clear()
                    End SyncLock

                    Utils.Logger.Instance.Info("TapPointManager disposed", "TapPointManager")
                End If

                disposed = True
            End If
        End Sub

#End Region

    End Class

End Namespace
