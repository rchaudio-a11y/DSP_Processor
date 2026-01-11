Imports System.Diagnostics
Imports DSP_Processor.AudioIO

Namespace Recording

    Public Class RecordingEngine

        Public Property InputSource As IInputSource
        Public Property OutputFolder As String = "Recordings"
        Public Property AutoNamePattern As String = "Take_{0:000}.wav"

        Public Property TimedRecordingEnabled As Boolean = False
        Public Property RecordingDurationSeconds As Integer = 30

        Public Property AutoRestartEnabled As Boolean = False
        Public Property MaxRecordings As Integer = 1

        Private wavOut As WavFileOutput
        Private stopwatch As Stopwatch
        Private currentIndex As Integer = 1
        Private isRecording As Boolean = False

        Public Sub StartRecording()
            If Not IO.Directory.Exists(OutputFolder) Then
                IO.Directory.CreateDirectory(OutputFolder)
            End If

            Dim filename = IO.Path.Combine(OutputFolder, String.Format(AutoNamePattern, currentIndex))
            wavOut = New WavFileOutput(filename, InputSource.SampleRate, InputSource.Channels, InputSource.BitsPerSample)

            stopwatch = Stopwatch.StartNew()
            isRecording = True
        End Sub

        Public Sub StopRecording()
            Try
                If wavOut IsNot Nothing Then
                    wavOut.CloseSink()
                    wavOut = Nothing
                End If
            Catch ex As Exception
                ' Optional: log or handle
            End Try

            stopwatch?.Stop()
            isRecording = False

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
                wavOut.Write(buffer, read)
            End If

            If TimedRecordingEnabled AndAlso stopwatch.Elapsed.TotalSeconds >= RecordingDurationSeconds Then
                StopRecording()
            End If
        End Sub

    End Class
End Namespace