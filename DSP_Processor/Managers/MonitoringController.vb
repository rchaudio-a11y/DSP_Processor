Imports System.Threading
Imports System.Text.RegularExpressions

Namespace Managers

    ''' <summary>
    ''' Centralized monitoring and reader management
    ''' Controls registration, health tracking, and lifecycle of tap point readers
    ''' Thread-safe: All operations protected by SyncLock
    ''' Ownership: Does NOT own TapPointManager (only references)
    ''' </summary>
    Public Class MonitoringController
        Implements IDisposable

        ' Reader registry (thread-safe collection)
        Private ReadOnly _readers As New Dictionary(Of String, ReaderInfo)
        Private ReadOnly _readerLock As New Object()

        ' Reference to TapPointManager (does NOT own - just controls)
        Private ReadOnly _tapManager As DSP.TapPointManager

        ' Enabled state (thread-safe using Interlocked)
        Private _enabled As Integer = 0 ' 0=disabled, 1=enabled

        ' Disposed flag
        Private _disposed As Integer = 0

        ' Naming convention pattern: {Owner}_{TapPoint}_{Type}
        Private Shared ReadOnly NamingPattern As New Regex("^[A-Za-z0-9]+_[A-Za-z0-9]+_[A-Za-z0-9]+$", RegexOptions.Compiled)

#Region "Properties"

        ''' <summary>
        ''' Is monitoring enabled? (thread-safe read)
        ''' </summary>
        Public ReadOnly Property IsEnabled As Boolean
            Get
                Return Interlocked.CompareExchange(_enabled, 0, 0) = 1
            End Get
        End Property

        ''' <summary>
        ''' Get all registered readers (thread-safe snapshot)
        ''' </summary>
        Public ReadOnly Property RegisteredReaders As IReadOnlyList(Of ReaderInfo)
            Get
                SyncLock _readerLock
                    Return _readers.Values.ToList().AsReadOnly()
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Count of registered readers (thread-safe)
        ''' </summary>
        Public ReadOnly Property ReaderCount As Integer
            Get
                SyncLock _readerLock
                    Return _readers.Count
                End SyncLock
            End Get
        End Property

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Create MonitoringController with reference to TapPointManager
        ''' </summary>
        ''' <param name="tapManager">TapPointManager to monitor (does NOT take ownership)</param>
        Public Sub New(tapManager As DSP.TapPointManager)
            If tapManager Is Nothing Then
                Throw New ArgumentNullException(NameOf(tapManager))
            End If

            _tapManager = tapManager

            Utils.Logger.Instance.Info("MonitoringController created", "MonitoringController")
        End Sub

#End Region

#Region "Public Methods - Enable/Disable"

        ''' <summary>
        ''' Enable monitoring (thread-safe)
        ''' Creates readers for all registered tap points
        ''' </summary>
        Public Sub Enable()
            CheckDisposed()

            ' Atomic test-and-set
            If Interlocked.CompareExchange(_enabled, 1, 0) = 1 Then
                Utils.Logger.Instance.Warning("Monitoring already enabled", "MonitoringController")
                Return
            End If

            Utils.Logger.Instance.Info("Monitoring enabled", "MonitoringController")
        End Sub

        ''' <summary>
        ''' Disable monitoring (thread-safe)
        ''' Does NOT unregister readers (they can still be used)
        ''' </summary>
        Public Sub Disable()
            CheckDisposed()

            ' Atomic test-and-set
            If Interlocked.CompareExchange(_enabled, 0, 1) = 0 Then
                Utils.Logger.Instance.Warning("Monitoring already disabled", "MonitoringController")
                Return
            End If

            Utils.Logger.Instance.Info("Monitoring disabled", "MonitoringController")
        End Sub

#End Region

#Region "Public Methods - Reader Registration"

        ''' <summary>
        ''' Register a new tap point reader with naming validation
        ''' Thread-safe: All registration is synchronized
        ''' </summary>
        ''' <param name="name">Reader name (must follow {Owner}_{TapPoint}_{Type} convention)</param>
        ''' <param name="tapPoint">Which tap point to read from</param>
        ''' <param name="owner">Owner component (e.g., "MainForm", "FFT")</param>
        ''' <returns>ReaderInfo for the registered reader</returns>
        ''' <exception cref="ArgumentException">If name doesn't follow convention</exception>
        Public Function RegisterReader(name As String, tapPoint As DSP.TapPoint, owner As String) As ReaderInfo
            CheckDisposed()

            ' Validate naming convention
            If Not ValidateReaderName(name) Then
                Dim ex As New ArgumentException($"Reader name '{name}' does not follow naming convention: {{Owner}}_{{TapPoint}}_{{Type}}")
                Utils.Logger.Instance.Error($"Invalid reader name: {name}", ex, "MonitoringController")
                Throw ex
            End If

            ' Reject legacy "_default_" names
            If name.Contains("_default_") Then
                Dim ex As New ArgumentException($"Reader name '{name}' contains legacy '_default_' pattern. Use new convention: {{Owner}}_{{TapPoint}}_{{Type}}")
                Utils.Logger.Instance.Error($"Legacy reader name rejected: {name}", ex, "MonitoringController")
                Throw ex
            End If

            SyncLock _readerLock
                ' Check if already exists
                If _readers.ContainsKey(name) Then
                    Utils.Logger.Instance.Warning($"Reader '{name}' already registered", "MonitoringController")
                    Return _readers(name)
                End If

                ' Create ReaderInfo
                Dim info As New ReaderInfo(name, tapPoint, owner)
                _readers.Add(name, info)

                ' Create actual reader in TapPointManager
                Try
                    Dim readerId = _tapManager.CreateReader(tapPoint, name)
                    Utils.Logger.Instance.Info($"✅ Reader registered: {name} (Tap={tapPoint}, Owner={owner})", "MonitoringController")
                Catch ex As Exception
                    ' Remove from registry if TapPointManager creation failed
                    _readers.Remove(name)
                    Utils.Logger.Instance.Error($"Failed to create reader '{name}' in TapPointManager", ex, "MonitoringController")
                    Throw
                End Try

                Return info
            End SyncLock
        End Function

        ''' <summary>
        ''' Unregister a reader (thread-safe)
        ''' Removes from registry and destroys in TapPointManager
        ''' </summary>
        Public Sub UnregisterReader(name As String)
            CheckDisposed()

            SyncLock _readerLock
                If Not _readers.ContainsKey(name) Then
                    Utils.Logger.Instance.Warning($"Reader '{name}' not found in registry", "MonitoringController")
                    Return
                End If

                Dim info = _readers(name)

                ' Remove from TapPointManager
                Try
                    _tapManager.DestroyReader(info.Name)
                Catch ex As Exception
                    Utils.Logger.Instance.Warning($"Failed to destroy reader '{name}' in TapPointManager", "MonitoringController")
                End Try

                ' Remove from registry
                _readers.Remove(name)

                Utils.Logger.Instance.Info($"Reader unregistered: {name}", "MonitoringController")
            End SyncLock
        End Sub

        ''' <summary>
        ''' Check if a reader is registered (thread-safe)
        ''' </summary>
        Public Function IsReaderRegistered(name As String) As Boolean
            CheckDisposed()

            SyncLock _readerLock
                Return _readers.ContainsKey(name)
            End SyncLock
        End Function

#End Region

#Region "Public Methods - Monitoring & Health"

        ''' <summary>
        ''' Get current monitoring snapshot (thread-safe)
        ''' Returns immutable snapshot of all reader states
        ''' </summary>
        Public Function GetSnapshot() As MonitoringSnapshot
            CheckDisposed()

            SyncLock _readerLock
                Return New MonitoringSnapshot(_readers.Values)
            End SyncLock
        End Function

        ''' <summary>
        ''' Get health status for a specific reader (thread-safe)
        ''' </summary>
        Public Function GetReaderHealth(name As String) As ReaderHealth
            CheckDisposed()

            SyncLock _readerLock
                If Not _readers.ContainsKey(name) Then
                    Return ReaderHealth.Unknown
                End If

                Return _readers(name).GetHealth()
            End SyncLock
        End Function

        ''' <summary>
        ''' Get ReaderInfo for a specific reader (thread-safe)
        ''' </summary>
        Public Function GetReaderInfo(name As String) As ReaderInfo
            CheckDisposed()

            SyncLock _readerLock
                If _readers.ContainsKey(name) Then
                    Return _readers(name)
                End If
                Return Nothing
            End SyncLock
        End Function

        ''' <summary>
        ''' Update read statistics for a reader (thread-safe)
        ''' Called by TapPointManager after successful read
        ''' </summary>
        Public Sub RecordRead(name As String, bytesRead As Integer)
            CheckDisposed()

            SyncLock _readerLock
                If _readers.ContainsKey(name) Then
                    _readers(name).RecordRead(bytesRead)
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Get all unhealthy readers (Stale + Dead) for diagnostics
        ''' </summary>
        Public Function GetUnhealthyReaders() As List(Of ReaderInfo)
            CheckDisposed()

            Return GetSnapshot().GetUnhealthyReaders()
        End Function

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' Validate reader name follows convention: {Owner}_{TapPoint}_{Type}
        ''' Examples:
        '''   "MainForm_PostInputGain_Meters"   ✅
        '''   "FFT_PreDSP_Analysis"             ✅
        '''   "_default_"                        ❌
        '''   "reader123"                        ❌
        ''' </summary>
        Private Shared Function ValidateReaderName(name As String) As Boolean
            If String.IsNullOrWhiteSpace(name) Then Return False

            ' Check pattern: 3 parts separated by underscores
            Dim parts = name.Split("_"c)
            If parts.Length <> 3 Then Return False

            ' Each part must be non-empty and alphanumeric
            For Each part In parts
                If String.IsNullOrWhiteSpace(part) Then Return False
                If Not part.All(Function(c) Char.IsLetterOrDigit(c)) Then Return False
            Next

            ' Reject legacy patterns
            If name.Contains("default") OrElse name.Contains("legacy") Then Return False

            Return True
        End Function

        ''' <summary>
        ''' Check if disposed (disposal guard pattern)
        ''' </summary>
        Private Sub CheckDisposed()
            If Interlocked.CompareExchange(_disposed, 0, 0) = 1 Then
                Throw New ObjectDisposedException(NameOf(MonitoringController))
            End If
        End Sub

#End Region

#Region "IDisposable"

        ''' <summary>
        ''' Dispose MonitoringController
        ''' Unregisters all readers and cleans up
        ''' Does NOT dispose TapPointManager (not owned)
        ''' </summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Atomic test-and-set
            If Interlocked.CompareExchange(_disposed, 1, 0) = 1 Then
                Return ' Already disposed
            End If

            Utils.Logger.Instance.Info("MonitoringController disposing...", "MonitoringController")

            ' Disable monitoring
            Interlocked.Exchange(_enabled, 0)

            ' Unregister all readers
            SyncLock _readerLock
                Dim readerNames = _readers.Keys.ToList()
                For Each name In readerNames
                    Try
                        _tapManager.DestroyReader(name)
                    Catch ex As Exception
                        Utils.Logger.Instance.Warning($"Failed to destroy reader '{name}' during disposal", "MonitoringController")
                    End Try
                Next

                _readers.Clear()
            End SyncLock

            Utils.Logger.Instance.Info("MonitoringController disposed", "MonitoringController")
        End Sub

#End Region

    End Class

End Namespace
