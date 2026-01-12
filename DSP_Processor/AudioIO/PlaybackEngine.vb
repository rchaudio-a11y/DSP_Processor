Imports NAudio.Wave
Imports System.IO

Namespace AudioIO

    ''' <summary>
    ''' Handles audio file playback with position tracking and event notifications.
    ''' Extracted from MainForm to maintain Single Responsibility Principle.
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 0, Task 0.1.1
    ''' Purpose: Centralize all playback logic separate from UI
    ''' </remarks>
    Public Class PlaybackEngine
        Implements IDisposable

        Private _playbackOutput As WaveOutEvent
        Private _playbackReader As AudioFileReader
        Private disposed As Boolean = False
        Private lastSampleBuffer As Byte() = Nothing

#Region "Properties"

        ''' <summary>
        ''' Gets whether audio is currently playing
        ''' </summary>
        Public ReadOnly Property IsPlaying As Boolean
            Get
                Return _playbackOutput IsNot Nothing AndAlso _playbackOutput.PlaybackState = PlaybackState.Playing
            End Get
        End Property

        ''' <summary>
        ''' Gets the current playback position
        ''' </summary>
        Public ReadOnly Property CurrentPosition As TimeSpan
            Get
                If _playbackReader IsNot Nothing Then
                    Return _playbackReader.CurrentTime
                End If
                Return TimeSpan.Zero
            End Get
        End Property

        ''' <summary>
        ''' Gets the total duration of the loaded audio file
        ''' </summary>
        Public ReadOnly Property TotalDuration As TimeSpan
            Get
                If _playbackReader IsNot Nothing Then
                    Return _playbackReader.TotalTime
                End If
                Return TimeSpan.Zero
            End Get
        End Property

        ''' <summary>
        ''' Gets or sets the playback volume (0.0 to 1.0)
        ''' </summary>
        Public Property Volume As Single
            Get
                If _playbackReader IsNot Nothing Then
                    Return _playbackReader.Volume
                End If
                Return 1.0F
            End Get
            Set(value As Single)
                If _playbackReader IsNot Nothing Then
                    _playbackReader.Volume = Math.Max(0.0F, Math.Min(1.0F, value))
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets the last rendered audio samples for level metering
        ''' </summary>
        Public ReadOnly Property LastSamples As Byte()
            Get
                Return lastSampleBuffer
            End Get
        End Property

#End Region

#Region "Events"

        ''' <summary>
        ''' Raised when playback stops (either by reaching end or user action)
        ''' </summary>
        Public Event PlaybackStopped As EventHandler(Of NAudio.Wave.StoppedEventArgs)

        ''' <summary>
        ''' Raised periodically during playback to report current position
        ''' </summary>
        Public Event PositionChanged As EventHandler(Of TimeSpan)

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Loads an audio file for playback
        ''' </summary>
        ''' <param name="filepath">Full path to the audio file</param>
        ''' <exception cref="FileNotFoundException">File does not exist</exception>
        ''' <exception cref="ArgumentException">Invalid audio file format</exception>
        Public Sub Load(filepath As String)
            ' Validate file exists
            If Not File.Exists(filepath) Then
                Utils.Logger.Instance.Error($"Audio file not found: {filepath}", Nothing, "PlaybackEngine")
                Throw New FileNotFoundException("Audio file not found", filepath)
            End If

            Utils.Logger.Instance.Debug($"Loading audio file for playback: {Path.GetFileName(filepath)}", "PlaybackEngine")

            ' Clean up previous playback
            StopAndCleanup()

            ' Load new file - NO SAMPLE MONITORING, direct playback
            Try
                _playbackReader = New AudioFileReader(filepath)
                Utils.Logger.Instance.Debug($"AudioFileReader created: {_playbackReader.WaveFormat.SampleRate}Hz, {_playbackReader.WaveFormat.Channels}ch", "PlaybackEngine")
                
                _playbackOutput = New WaveOutEvent()
                _playbackOutput.Init(_playbackReader)
                Utils.Logger.Instance.Debug("WaveOutEvent initialized", "PlaybackEngine")

                ' Subscribe to stop event
                AddHandler _playbackOutput.PlaybackStopped, AddressOf OnPlaybackStoppedInternal

            Catch ex As Exception
                Utils.Logger.Instance.Error($"Failed to load audio file: {ex.Message}", ex, "PlaybackEngine")
                ' Clean up on error
                StopAndCleanup()
                Throw New ArgumentException("Failed to load audio file: " & ex.Message, ex)
            End Try
        End Sub

        ''' <summary>
        ''' Starts or resumes playback
        ''' </summary>
        Public Sub Play()
            If _playbackOutput IsNot Nothing Then
                _playbackOutput.Play()
            End If
        End Sub

        ''' <summary>
        ''' Pauses playback at current position
        ''' </summary>
        Public Sub Pause()
            If _playbackOutput IsNot Nothing Then
                _playbackOutput.Pause()
            End If
        End Sub

        ''' <summary>
        ''' Stops playback and resets position to start
        ''' </summary>
        Public Sub [Stop]()
            If _playbackOutput IsNot Nothing Then
                _playbackOutput.Stop()
            End If

            If _playbackReader IsNot Nothing Then
                _playbackReader.Position = 0
            End If
        End Sub

        ''' <summary>
        ''' Seeks to a specific position in the audio file
        ''' </summary>
        ''' <param name="position">Target position</param>
        Public Sub Seek(position As TimeSpan)
            If _playbackReader IsNot Nothing Then
                _playbackReader.CurrentTime = position
                RaiseEvent PositionChanged(Me, position)
            End If
        End Sub

        ''' <summary>
        ''' Updates position tracking (call from timer)
        ''' </summary>
        Public Sub UpdatePosition()
            If IsPlaying Then
                RaiseEvent PositionChanged(Me, CurrentPosition)
            End If
        End Sub

#End Region

#Region "Private Methods"

        Private Sub OnPlaybackStoppedInternal(sender As Object, e As NAudio.Wave.StoppedEventArgs)
            RaiseEvent PlaybackStopped(Me, e)
        End Sub

        Private Sub StopAndCleanup()
            ' Stop playback
            If _playbackOutput IsNot Nothing Then
                Utils.Logger.Instance.Debug("Stopping and disposing WaveOutEvent", "PlaybackEngine")
                RemoveHandler _playbackOutput.PlaybackStopped, AddressOf OnPlaybackStoppedInternal
                _playbackOutput.Stop()
                _playbackOutput.Dispose()
                _playbackOutput = Nothing
            End If

            ' Close reader
            If _playbackReader IsNot Nothing Then
                Utils.Logger.Instance.Debug("Disposing AudioFileReader", "PlaybackEngine")
                _playbackReader.Dispose()
                _playbackReader = Nothing
            End If
        End Sub

#End Region

#Region "IDisposable Implementation"

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposed Then
                If disposing Then
                    ' Dispose managed resources
                    StopAndCleanup()
                End If
                disposed = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

#End Region

    End Class

End Namespace
