Imports System.IO
Imports DSP_Processor.Utils
Imports NAudio.Wave

Namespace Managers

    ''' <summary>
    ''' Manages recording files - list, delete, validate, metadata
    ''' </summary>
    ''' <remarks>
    ''' Phase 0: MainForm Refactoring
    ''' Extracts file management logic from MainForm
    ''' </remarks>
    Public Class FileManager

#Region "Events"

        ''' <summary>Raised when the file list has changed (add/delete/refresh)</summary>
        Public Event FileListChanged As EventHandler

        ''' <summary>Raised when a file is selected</summary>
        Public Event FileSelected As EventHandler(Of String) ' filepath

        ''' <summary>Raised when a file is deleted</summary>
        Public Event FileDeleted As EventHandler(Of String) ' filepath

        ''' <summary>Raised when file validation fails</summary>
        Public Event FileValidationFailed As EventHandler(Of String) ' error message

#End Region

#Region "Fields"

        Private _recordingsFolder As String
        Private _files As New List(Of FileInfo)

#End Region

#Region "Properties"

        ''' <summary>Path to recordings folder</summary>
        Public ReadOnly Property RecordingsFolder As String
            Get
                Return _recordingsFolder
            End Get
        End Property

        ''' <summary>Number of recordings in folder</summary>
        Public ReadOnly Property FileCount As Integer
            Get
                Return _files.Count
            End Get
        End Property

        ''' <summary>List of recording files</summary>
        Public ReadOnly Property Files As List(Of FileInfo)
            Get
                Return New List(Of FileInfo)(_files) ' Return copy
            End Get
        End Property

#End Region

#Region "Constructor"

        Public Sub New()
            ' Default recordings folder
            _recordingsFolder = Path.Combine(Application.StartupPath, "Recordings")
            EnsureFolderExists()

            Logger.Instance.Info("FileManager initialized", "FileManager")
        End Sub

        Public Sub New(folder As String)
            _recordingsFolder = folder
            EnsureFolderExists()

            Logger.Instance.Info($"FileManager initialized with folder: {folder}", "FileManager")
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>Refresh the file list from disk</summary>
        Public Sub RefreshFileList()
            Try
                _files.Clear()

                If Not Directory.Exists(_recordingsFolder) Then
                    Logger.Instance.Warning($"Recordings folder not found: {_recordingsFolder}", "FileManager")
                    RaiseEvent FileListChanged(Me, EventArgs.Empty)
                    Return
                End If

                ' Get all WAV files
                For Each filePath In Directory.GetFiles(_recordingsFolder, "*.wav")
                    _files.Add(New FileInfo(filePath))
                Next

                ' Sort by date (newest first)
                _files.Sort(Function(a, b) b.LastWriteTime.CompareTo(a.LastWriteTime))

                Logger.Instance.Debug($"File list refreshed: {_files.Count} recording(s) found", "FileManager")
                RaiseEvent FileListChanged(Me, EventArgs.Empty)

            Catch ex As Exception
                Logger.Instance.Error("Failed to refresh file list", ex, "FileManager")
            End Try
        End Sub

        ''' <summary>Delete a recording file</summary>
        Public Function DeleteFile(filepath As String) As Boolean
            Try
                ' Validate file exists
                If Not File.Exists(filepath) Then
                    Logger.Instance.Warning($"File not found for deletion: {filepath}", "FileManager")
                    Return False
                End If

                ' Attempt delete
                File.Delete(filepath)

                Logger.Instance.Info($"File deleted: {Path.GetFileName(filepath)}", "FileManager")
                RaiseEvent FileDeleted(Me, filepath)

                ' Refresh list
                RefreshFileList()

                Return True

            Catch ex As UnauthorizedAccessException
                Logger.Instance.Error($"Access denied deleting file: {filepath}", ex, "FileManager")
                RaiseEvent FileValidationFailed(Me, "Access denied. File may be in use.")
                Return False

            Catch ex As IOException
                Logger.Instance.Error($"File in use, cannot delete: {filepath}", ex, "FileManager")
                RaiseEvent FileValidationFailed(Me, "File is in use by another process.")
                Return False

            Catch ex As Exception
                Logger.Instance.Error($"Failed to delete file: {filepath}", ex, "FileManager")
                RaiseEvent FileValidationFailed(Me, $"Delete failed: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>Validate that a file exists and is accessible</summary>
        Public Function ValidateFile(filepath As String) As Boolean
            Try
                ' Check existence
                If Not File.Exists(filepath) Then
                    Logger.Instance.Warning($"File not found: {filepath}", "FileManager")
                    Return False
                End If

                ' Check size (WAV files should be >100 bytes)
                Dim fileInfo As New FileInfo(filepath)
                If fileInfo.Length < 100 Then
                    Logger.Instance.Warning($"File too small (corrupt?): {filepath}", "FileManager")
                    Return False
                End If

                ' Try to open file (check if locked)
                Using fs As New FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read)
                    ' File is accessible
                End Using

                Return True

            Catch ex As IOException
                Logger.Instance.Warning($"File is locked: {filepath}", "FileManager")
                Return False

            Catch ex As Exception
                Logger.Instance.Error($"File validation failed: {filepath}", ex, "FileManager")
                Return False
            End Try
        End Function

        ''' <summary>Get audio file metadata (duration, format, etc.)</summary>
        Public Function GetFileMetadata(filepath As String) As AudioFileMetadata
            Dim metadata As New AudioFileMetadata With {
                .FilePath = filepath,
                .FileName = Path.GetFileName(filepath),
                .IsValid = False
            }

            Try
                If Not File.Exists(filepath) Then Return metadata

                ' Get file info
                Dim fileInfo As New FileInfo(filepath)
                metadata.FileSizeBytes = fileInfo.Length
                metadata.LastModified = fileInfo.LastWriteTime

                ' Get audio info using NAudio
                Using reader As New AudioFileReader(filepath)
                    metadata.Duration = reader.TotalTime
                    metadata.SampleRate = reader.WaveFormat.SampleRate
                    metadata.BitsPerSample = reader.WaveFormat.BitsPerSample
                    metadata.Channels = reader.WaveFormat.Channels
                    metadata.IsValid = True
                End Using

                Logger.Instance.Debug($"Metadata retrieved: {metadata.FileName}", "FileManager")

            Catch ex As Exception
                Logger.Instance.Warning($"Failed to get metadata for {filepath}", "FileManager")
            End Try

            Return metadata
        End Function

        ''' <summary>Wait for file to become accessible (retry logic)</summary>
        Public Function WaitForFileAccess(filepath As String, maxRetries As Integer, retryDelayMs As Integer) As Boolean
            For attempt = 1 To maxRetries
                Try
                    Using fs As New FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read)
                        ' File is accessible
                        Return True
                    End Using

                Catch ex As IOException
                    If attempt < maxRetries Then
                        Logger.Instance.Debug($"File locked, retrying... (attempt {attempt}/{maxRetries})", "FileManager")
                        System.Threading.Thread.Sleep(retryDelayMs)
                    End If
                End Try
            Next

            Logger.Instance.Warning($"File remained locked after {maxRetries} attempts: {filepath}", "FileManager")
            Return False
        End Function

        ''' <summary>Ensure recordings folder exists</summary>
        Public Sub EnsureFolderExists()
            Try
                If Not Directory.Exists(_recordingsFolder) Then
                    Directory.CreateDirectory(_recordingsFolder)
                    Logger.Instance.Info($"Created recordings folder: {_recordingsFolder}", "FileManager")
                End If
            Catch ex As Exception
                Logger.Instance.Error($"Failed to create recordings folder: {_recordingsFolder}", ex, "FileManager")
            End Try
        End Sub

#End Region

    End Class

#Region "Supporting Classes"

    ''' <summary>Audio file metadata</summary>
    Public Class AudioFileMetadata
        Public Property FilePath As String
        Public Property FileName As String
        Public Property FileSizeBytes As Long
        Public Property Duration As TimeSpan
        Public Property SampleRate As Integer
        Public Property BitsPerSample As Integer
        Public Property Channels As Integer
        Public Property LastModified As DateTime
        Public Property IsValid As Boolean

        Public ReadOnly Property FileSizeMB As Double
            Get
                Return FileSizeBytes / 1024.0 / 1024.0
            End Get
        End Property

        Public ReadOnly Property DurationFormatted As String
            Get
                Return $"{Duration.Minutes:00}:{Duration.Seconds:00}"
            End Get
        End Property

        Public ReadOnly Property FormatDescription As String
            Get
                Return $"{SampleRate}Hz, {BitsPerSample}-bit, {If(Channels = 1, "Mono", "Stereo")}"
            End Get
        End Property
    End Class

#End Region

End Namespace
