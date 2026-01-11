Imports System.Diagnostics
Imports DSP_Processor.AudioIO

Namespace Recording

    Public Class RecordingEngine

        Public Property InputSource As IInputSource
        Public Property OutputFolder As String = "Recordings"
        Public Property AutoNamePattern As String = "Take_{0:yyyyMMdd}-{1:000}.wav"

        Public Property TimedRecordingEnabled As Boolean = False
        Public Property RecordingDurationSeconds As Integer = 30

        Public Property AutoRestartEnabled As Boolean = False
        Public Property MaxRecordings As Integer = 1

        Private wavOut As WavFileOutput
        Private stopwatch As Stopwatch
        Private currentIndex As Integer = 1
        Private isRecording As Boolean = False
        Private lastProcessedBuffer As Byte() = Nothing

        ''' <summary>
        ''' Gets the last processed audio buffer for level metering
        ''' </summary>
        Public ReadOnly Property LastBuffer As Byte()
            Get
                Return lastProcessedBuffer
            End Get
        End Property

        ''' <summary>
        ''' Gets the current recording duration
        ''' </summary>
        Public ReadOnly Property RecordingDuration As TimeSpan
            Get
                If isRecording AndAlso stopwatch IsNot Nothing Then
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
            isRecording = True

            ' Immediately flush initial audio buffers in a tight loop
            ' Process 10 times with minimal delay to capture startup audio
            For i = 1 To 10
                Process()
            Next
        End Sub

        Public Sub StopRecording()
            isRecording = False
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

            If AutoRestartEnabled AndAlso currentIndex <= MaxRecordings Then
                StartRecording()
            End If
        End Sub

        Public Sub Process()
            If Not isRecording Then Exit Sub

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
                ' No data available - log occasionally
                Static lastLogTime As DateTime = DateTime.MinValue
                If DateTime.Now.Subtract(lastLogTime).TotalSeconds > 5 Then
                    Utils.Logger.Instance.Warning("No audio data available from input source", "RecordingEngine")
                    lastLogTime = DateTime.Now
                End If
            End If

            If TimedRecordingEnabled AndAlso stopwatch.Elapsed.TotalSeconds >= RecordingDurationSeconds Then
                StopRecording()
            End If
        End Sub

    End Class
End Namespace