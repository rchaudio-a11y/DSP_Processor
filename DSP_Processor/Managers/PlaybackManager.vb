Imports System.IO
Imports DSP_Processor.AudioIO
Imports DSP_Processor.Utils
Imports NAudio.Wave

Namespace Managers

    ''' <summary>
    ''' Manages audio playback lifecycle - file loading, play/stop/pause, position tracking
    ''' </summary>
    ''' <remarks>
    ''' Phase 0: MainForm Refactoring
    ''' Extracts all playback logic from MainForm into dedicated manager
    ''' </remarks>
    Public Class PlaybackManager
        Implements IDisposable

#Region "Events"

        ''' <summary>Raised when playback starts</summary>
        Public Event PlaybackStarted As EventHandler(Of String) ' filepath

        ''' <summary>Raised when playback stops (either by reaching end or user action)</summary>
        Public Event PlaybackStopped As EventHandler(Of NAudio.Wave.StoppedEventArgs)

        ''' <summary>Raised when playback position changes</summary>
        Public Event PositionChanged As EventHandler(Of TimeSpan)

#End Region

#Region "Fields"

        Private playbackEngine As PlaybackEngine
        Private currentFilePath As String = ""
        Private _volume As Single = 1.0F

#End Region

#Region "Properties"

        ''' <summary>Is currently playing?</summary>
        Public ReadOnly Property IsPlaying As Boolean
            Get
                Return playbackEngine IsNot Nothing AndAlso playbackEngine.IsPlaying
            End Get
        End Property

        ''' <summary>Current playback position</summary>
        Public ReadOnly Property CurrentPosition As TimeSpan
            Get
                If playbackEngine IsNot Nothing Then
                    Return playbackEngine.CurrentPosition
                End If
                Return TimeSpan.Zero
            End Get
        End Property

        ''' <summary>Total duration of loaded file</summary>
        Public ReadOnly Property TotalDuration As TimeSpan
            Get
                If playbackEngine IsNot Nothing Then
                    Return playbackEngine.TotalDuration
                End If
                Return TimeSpan.Zero
            End Get
        End Property

        ''' <summary>Playback volume (0.0 to 1.0, where 1.0 is 100%)</summary>
        Public Property Volume As Single
            Get
                Return _volume
            End Get
            Set(value As Single)
                _volume = Math.Max(0.0F, Math.Min(2.0F, value)) ' Allow up to 200%
                If playbackEngine IsNot Nothing Then
                    playbackEngine.Volume = _volume
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets the last rendered audio samples for level metering
        ''' </summary>
        Public ReadOnly Property LastSamples As Byte()
            Get
                Return playbackEngine.LastSamples
            End Get
        End Property

        ''' <summary>
        ''' Gets the currently loaded file path
        ''' </summary>
        Public ReadOnly Property CurrentFile As String
            Get
                Return currentFilePath
            End Get
        End Property

#End Region

#Region "Constructor"

        Public Sub New()
            Logger.Instance.Info("PlaybackManager initialized", "PlaybackManager")
        End Sub

#End Region

#Region "Public Methods"

        ''' <summary>Initialize playback engine</summary>
        Public Sub Initialize()
            Try
                ' Create playback engine
                playbackEngine = New PlaybackEngine()

                ' Wire up events
                AddHandler playbackEngine.PlaybackStopped, AddressOf OnPlaybackStoppedInternal
                AddHandler playbackEngine.PositionChanged, AddressOf OnPositionChangedInternal

                Logger.Instance.Info("PlaybackManager initialized successfully", "PlaybackManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to initialize PlaybackManager", ex, "PlaybackManager")
                Throw
            End Try
        End Sub

        ''' <summary>Load audio file and start playback immediately</summary>
        ''' <param name="filepath">Full path to audio file</param>
        Public Sub LoadAndPlay(filepath As String)
            Try
                ' Validate file
                If Not File.Exists(filepath) Then
                    Dim ex = New FileNotFoundException("Audio file not found", filepath)
                    Logger.Instance.Error($"File not found: {filepath}", ex, "PlaybackManager")
                    Throw ex
                End If

                Logger.Instance.Info($"Loading file for playback: {Path.GetFileName(filepath)}", "PlaybackManager")

                ' Stop any current playback
                If IsPlaying Then
                    [Stop]()
                    System.Threading.Thread.Sleep(100) ' Give time to clean up
                End If

                ' Ensure engine exists
                If playbackEngine Is Nothing Then
                    Initialize()
                End If

                ' Load file
                playbackEngine.Load(filepath)

                ' Apply volume
                playbackEngine.Volume = _volume

                ' Start playback
                playbackEngine.Play()

                currentFilePath = filepath

                ' Raise event
                RaiseEvent PlaybackStarted(Me, filepath)

                Logger.Instance.Info($"Playback started: {Path.GetFileName(filepath)} (Volume: {_volume * 100:F0}%)", "PlaybackManager")

            Catch ex As Exception
                Logger.Instance.Error($"Failed to load/play file: {ex.Message}", ex, "PlaybackManager")
                Throw
            End Try
        End Sub

        ''' <summary>Load audio file without starting playback</summary>
        Public Sub Load(filepath As String)
            Try
                If Not File.Exists(filepath) Then
                    Throw New FileNotFoundException("Audio file not found", filepath)
                End If

                Logger.Instance.Info($"Loading file: {Path.GetFileName(filepath)}", "PlaybackManager")

                ' Stop any current playback
                If IsPlaying Then
                    [Stop]()
                End If

                ' Ensure engine exists
                If playbackEngine Is Nothing Then
                    Initialize()
                End If

                ' Load file
                playbackEngine.Load(filepath)
                playbackEngine.Volume = _volume

                currentFilePath = filepath

                Logger.Instance.Info($"File loaded: {Path.GetFileName(filepath)}", "PlaybackManager")

            Catch ex As Exception
                Logger.Instance.Error($"Failed to load file: {ex.Message}", ex, "PlaybackManager")
                Throw
            End Try
        End Sub

        ''' <summary>Start or resume playback</summary>
        Public Sub Play()
            Try
                If playbackEngine Is Nothing Then
                    Logger.Instance.Warning("Cannot play - no file loaded", "PlaybackManager")
                    Return
                End If

                playbackEngine.Play()
                Logger.Instance.Info("Playback started", "PlaybackManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to start playback", ex, "PlaybackManager")
                Throw
            End Try
        End Sub

        ''' <summary>Stop playback and reset position</summary>
        Public Sub [Stop]()
            Try
                If playbackEngine Is Nothing Then Return

                Logger.Instance.Info("Stopping playback...", "PlaybackManager")
                playbackEngine.Stop()

                Logger.Instance.Info("Playback stopped", "PlaybackManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to stop playback", ex, "PlaybackManager")
            End Try
        End Sub

        ''' <summary>Pause playback at current position</summary>
        Public Sub Pause()
            Try
                If playbackEngine IsNot Nothing Then
                    playbackEngine.Pause()
                    Logger.Instance.Info("Playback paused", "PlaybackManager")
                End If
            Catch ex As Exception
                Logger.Instance.Error("Failed to pause playback", ex, "PlaybackManager")
            End Try
        End Sub

        ''' <summary>Seek to specific position</summary>
        Public Sub Seek(position As TimeSpan)
            Try
                If playbackEngine IsNot Nothing Then
                    playbackEngine.Seek(position)
                    Logger.Instance.Debug($"Seeked to: {position}", "PlaybackManager")
                End If
            Catch ex As Exception
                Logger.Instance.Error("Failed to seek", ex, "PlaybackManager")
            End Try
        End Sub

        ''' <summary>Update position (call from timer) and raise PositionChanged event</summary>
        Public Sub UpdatePosition()
            Try
                If playbackEngine IsNot Nothing AndAlso IsPlaying Then
                    playbackEngine.UpdatePosition()
                    ' PositionChanged event will be raised by OnPositionChangedInternal
                End If
            Catch ex As Exception
                Logger.Instance.Error("Failed to update position", ex, "PlaybackManager")
            End Try
        End Sub

#End Region

#Region "Private Methods"

        Private Sub OnPlaybackStoppedInternal(sender As Object, e As NAudio.Wave.StoppedEventArgs)
            Logger.Instance.Info("Playback stopped event received", "PlaybackManager")
            RaiseEvent PlaybackStopped(Me, e)
        End Sub

        Private Sub OnPositionChangedInternal(sender As Object, position As TimeSpan)
            RaiseEvent PositionChanged(Me, position)
        End Sub

#End Region

#Region "IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            Logger.Instance.Info("Disposing PlaybackManager", "PlaybackManager")

            [Stop]()

            If playbackEngine IsNot Nothing Then
                RemoveHandler playbackEngine.PlaybackStopped, AddressOf OnPlaybackStoppedInternal
                RemoveHandler playbackEngine.PositionChanged, AddressOf OnPositionChangedInternal
                playbackEngine.Dispose()
                playbackEngine = Nothing
            End If

            Logger.Instance.Info("PlaybackManager disposed", "PlaybackManager")
        End Sub

#End Region

    End Class

End Namespace
