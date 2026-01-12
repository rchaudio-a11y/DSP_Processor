Imports System.Diagnostics
Imports DSP_Processor.AudioIO
Imports DSP_Processor.Models

Namespace Recording

    Public Class RecordingEngine

        Public Property InputSource As IInputSource
        Public Property OutputFolder As String = "Recordings"
        Public Property AutoNamePattern As String = "Take_{0:yyyyMMdd}-{1:000}.wav"

        ' NEW: Recording options
        Public Property Options As RecordingOptions = New RecordingOptions()

        ' Legacy properties (kept for compatibility, but Options takes precedence)
        <Obsolete("Use Options.Mode instead")>
        Public Property TimedRecordingEnabled As Boolean = False
        <Obsolete("Use Options.TimedDurationSeconds instead")>
        Public Property RecordingDurationSeconds As Integer = 30
        <Obsolete("Use Options.LoopCount instead")>
        Public Property AutoRestartEnabled As Boolean = False
        <Obsolete("Use Options.LoopCount instead")>
        Public Property MaxRecordings As Integer = 1

        Private wavOut As WavFileOutput
        Private stopwatch As Stopwatch
        Private currentIndex As Integer = 1
        Private recordingActive As Boolean = False
        Private lastProcessedBuffer As Byte() = Nothing
        
        ' NEW: Loop mode state
        Private loopCurrentTake As Integer = 0
        Private loopDelayTimer As Stopwatch
        Private isInLoopDelay As Boolean = False

        ''' <summary>
        ''' Gets the last processed audio buffer for level metering
        ''' </summary>
        Public ReadOnly Property LastBuffer As Byte()
            Get
                Return lastProcessedBuffer
            End Get
        End Property

        ''' <summary>
        ''' Gets whether recording is currently active
        ''' </summary>
        Public ReadOnly Property IsRecording As Boolean
            Get
                Return recordingActive
            End Get
        End Property

        ''' <summary>
        ''' Gets the current recording duration
        ''' </summary>
        Public ReadOnly Property RecordingDuration As TimeSpan
            Get
                If recordingActive AndAlso stopwatch IsNot Nothing Then
                    Return stopwatch.Elapsed
                End If
                Return TimeSpan.Zero
            End Get
        End Property

        Public Sub StartRecording()
            If Not IO.Directory.Exists(OutputFolder) Then
                IO.Directory.CreateDirectory(OutputFolder)
            End If

            ' Generate filename with date and auto-incrementing index
            Dim today = DateTime.Now
            Dim dateStr = today.ToString("yyyyMMdd")
            Dim fullPath As String
            Dim index As Integer = 1

            ' Find next available index for today's date
            Do
                Dim filename = String.Format(AutoNamePattern, today, index)
                fullPath = IO.Path.Combine(OutputFolder, filename)

                If Not IO.File.Exists(fullPath) Then
                    Exit Do
                End If

                index += 1
            Loop While index < 1000 ' Safety limit

            ' Ensure file is not locked by releasing any handles
            GC.Collect()
            GC.WaitForPendingFinalizers()

            ' Create WAV file
            wavOut = New WavFileOutput(fullPath, InputSource.SampleRate, InputSource.Channels, InputSource.BitsPerSample)

            stopwatch = Stopwatch.StartNew()
            recordingActive = True

            ' Immediately flush initial audio buffers in a tight loop
            ' Process 10 times with minimal delay to capture startup audio
            For i = 1 To 10
                Process()
            Next
        End Sub

        Public Sub StopRecording()
            recordingActive = False
            stopwatch?.Stop()

            Try
                If wavOut IsNot Nothing Then
                    wavOut.Dispose() ' Use Dispose instead of CloseSink
                    wavOut = Nothing
                End If
            Catch ex As Exception
                ' Log error if logger is available
                Try
                    Utils.Logger.Instance.Error("Failed to close recording file", ex, "RecordingEngine")
                Catch
                    ' Logger might not be initialized
                End Try
            End Try

            currentIndex += 1

            ' Legacy auto-restart (deprecated, use Options.Mode = LoopMode instead)
            If AutoRestartEnabled AndAlso currentIndex <= MaxRecordings Then
                StartRecording()
            End If
        End Sub
        
        ''' <summary>
        ''' Start a loop recording session
        ''' </summary>
        Public Sub StartLoopRecording()
            If Options.Mode <> RecordingMode.LoopMode Then
                Throw New InvalidOperationException("StartLoopRecording requires Options.Mode = LoopMode")
            End If
            
            loopCurrentTake = 1
            isInLoopDelay = False
            Utils.Logger.Instance.Info($"Starting loop recording: {Options.LoopCount} takes × {Options.LoopDurationSeconds}s", "RecordingEngine")
            Services.LoggingServiceAdapter.Instance.LogInfo($"Loop mode: {Options.LoopCount} takes × {Options.LoopDurationSeconds}s")
            StartRecording()
        End Sub
        
        ''' <summary>
        ''' Cancel loop recording (stop all remaining takes)
        ''' </summary>
        Public Sub CancelLoopRecording()
            If Options.Mode = RecordingMode.LoopMode Then
                loopCurrentTake = Options.LoopCount ' Force loop to end
                isInLoopDelay = False
            End If
            StopRecording()
        End Sub

        Public Sub Process()
            If Not recordingActive Then
                ' Check if we're in loop delay
                If isInLoopDelay AndAlso loopDelayTimer IsNot Nothing Then
                    If loopDelayTimer.Elapsed.TotalSeconds >= Options.LoopDelaySeconds Then
                        ' Delay complete, start next take
                        isInLoopDelay = False
                        loopCurrentTake += 1
                        
                        If loopCurrentTake <= Options.LoopCount Then
                            Utils.Logger.Instance.Info($"Starting loop take {loopCurrentTake} of {Options.LoopCount}", "RecordingEngine")
                            Services.LoggingServiceAdapter.Instance.LogInfo($"Starting take {loopCurrentTake}/{Options.LoopCount}")
                            StartRecording()
                        Else
                            ' Loop complete
                            Utils.Logger.Instance.Info("Loop recording complete", "RecordingEngine")
                            Services.LoggingServiceAdapter.Instance.LogInfo($"Loop recording complete: {Options.LoopCount} takes finished")
                            loopCurrentTake = 0
                        End If
                    End If
                End If
                Exit Sub
            End If

            Dim buffer(4095) As Byte
            Dim read = InputSource.Read(buffer, 0, buffer.Length)

            If read > 0 Then
                ' Only write actual data, not the buffer underrun fills
                wavOut.Write(buffer, read)

                ' Store buffer for metering
                If read = buffer.Length Then
                    lastProcessedBuffer = buffer
                Else
                    ' Partial buffer - copy only valid data
                    ReDim lastProcessedBuffer(read - 1)
                    Array.Copy(buffer, lastProcessedBuffer, read)
                End If
            Else
                ' No data available - log occasionally at debug level (this is normal)
                Static lastLogTime As DateTime = DateTime.MinValue
                If DateTime.Now.Subtract(lastLogTime).TotalSeconds > 5 Then
                    Utils.Logger.Instance.Debug("Buffer queue temporarily empty (normal during recording)", "RecordingEngine")
                    lastLogTime = DateTime.Now
                End If
            End If

            ' Check auto-stop conditions based on mode
            Select Case Options.Mode
                Case RecordingMode.Manual
                    ' No auto-stop, user must stop manually
                    
                Case RecordingMode.Timed
                    ' Stop after specified duration
                    If stopwatch.Elapsed.TotalSeconds >= Options.TimedDurationSeconds Then
                        Utils.Logger.Instance.Info($"Timed recording complete: {stopwatch.Elapsed.TotalSeconds:F1}s", "RecordingEngine")
                        Services.LoggingServiceAdapter.Instance.LogInfo($"Timed recording complete ({Options.TimedDurationSeconds}s)")
                        StopRecording()
                    End If
                    
                Case RecordingMode.LoopMode
                    ' Stop after each take duration, then start delay
                    If stopwatch.Elapsed.TotalSeconds >= Options.LoopDurationSeconds Then
                        Utils.Logger.Instance.Info($"Loop take {loopCurrentTake} complete: {stopwatch.Elapsed.TotalSeconds:F1}s", "RecordingEngine")
                        Services.LoggingServiceAdapter.Instance.LogInfo($"Take {loopCurrentTake} complete ({Options.LoopDurationSeconds}s)")
                        StopRecording()
                        
                        ' Give file system time to fully close the file
                        System.Threading.Thread.Sleep(100)
                        
                        If loopCurrentTake < Options.LoopCount Then
                            ' Start delay before next take
                            isInLoopDelay = True
                            loopDelayTimer = Stopwatch.StartNew()
                            Services.LoggingServiceAdapter.Instance.LogInfo($"Waiting {Options.LoopDelaySeconds}s before next take...")
                        Else
                            ' All takes complete
                            loopCurrentTake = 0
                            Utils.Logger.Instance.Info("All loop takes complete", "RecordingEngine")
                        End If
                    End If
            End Select
        End Sub

    End Class
End Namespace