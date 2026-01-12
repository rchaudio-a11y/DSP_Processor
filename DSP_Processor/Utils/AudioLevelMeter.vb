Imports System.Math

Namespace Utils

    ''' <summary>
    ''' Data structure for audio level measurements
    ''' </summary>
    Public Structure LevelData
        ''' <summary>Peak level in dB (-60 to 0)</summary>
        Public PeakDB As Single

        ''' <summary>RMS (average) level in dB (-60 to 0)</summary>
        Public RMSDB As Single

        ''' <summary>True if signal is clipping (>-0.3dB)</summary>
        Public IsClipping As Boolean

        ''' <summary>Left channel peak in dB</summary>
        Public PeakLeftDB As Single

        ''' <summary>Right channel peak in dB</summary>
        Public PeakRightDB As Single
    End Structure

    ''' <summary>
    ''' Analyzes PCM audio buffers and calculates peak and RMS levels in dB
    ''' Used for real-time volume metering during recording and playback
    ''' Now with configurable metering parameters for professional control
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 0, Volume Meter Feature
    ''' Updated: Phase 1, Input Settings Tab - Configurable Metering
    ''' Thread-safe and optimized for real-time operation
    ''' </remarks>
    Public NotInheritable Class AudioLevelMeter

        Private Const MinDB As Single = -60.0F

        ' Configurable metering parameters (static/shared)
        Public Shared Property PeakHoldMs As Integer = 500
        Public Shared Property PeakDecayDbPerSec As Single = 3.0F
        Public Shared Property RmsWindowMs As Integer = 50
        Public Shared Property AttackMs As Integer = 0
        Public Shared Property ReleaseMs As Integer = 300
        Public Shared Property ClipThresholdDb As Single = -0.1F

        ' Peak hold/decay state tracking
        Private Shared lastPeakTime As DateTime = DateTime.MinValue
        Private Shared lastPeakValueDb As Single = MinDB

        ' Attack/release smoothing state
        Private Shared lastSmoothValueDb As Single = MinDB
        Private Shared lastUpdateTime As DateTime = DateTime.Now

        ''' <summary>
        ''' Analyzes PCM audio buffer and returns level data
        ''' </summary>
        ''' <param name="buffer">PCM sample buffer</param>
        ''' <param name="bits">Bit depth (16, 24, or 32)</param>
        ''' <param name="channels">Channel count (1=mono, 2=stereo)</param>
        ''' <returns>Level data structure</returns>
        Public Shared Function AnalyzeSamples(buffer As Byte(), bits As Integer, channels As Integer) As LevelData
            If buffer Is Nothing OrElse buffer.Length = 0 Then
                Return New LevelData With {
                    .PeakDB = MinDB,
                    .RMSDB = MinDB,
                    .PeakLeftDB = MinDB,
                    .PeakRightDB = MinDB,
                    .IsClipping = False
                }
            End If

            Dim rawData As LevelData
            Select Case bits
                Case 16
                    rawData = Analyze16Bit(buffer, channels)
                Case 24
                    rawData = Analyze24Bit(buffer, channels)
                Case 32
                    rawData = Analyze32Bit(buffer, channels)
                Case Else
                    rawData = Analyze16Bit(buffer, channels) ' Default fallback
            End Select
            
            ' Apply peak hold and decay
            rawData.PeakDB = ApplyPeakHoldAndDecay(rawData.PeakDB)
            
            ' Apply attack/release smoothing
            rawData.PeakDB = ApplyAttackRelease(rawData.PeakDB)
            
            ' Update clipping based on threshold
            rawData.IsClipping = (rawData.PeakDB >= ClipThresholdDb)
            
            Return rawData
        End Function
        
        ''' <summary>
        ''' Apply peak hold and decay to the measured peak value
        ''' </summary>
        Private Shared Function ApplyPeakHoldAndDecay(currentDb As Single) As Single
            Dim now = DateTime.Now
            Dim timeSinceLastPeak = (now - lastPeakTime).TotalMilliseconds
            
            ' If new peak, update and hold
            If currentDb > lastPeakValueDb Then
                lastPeakValueDb = currentDb
                lastPeakTime = now
                Return currentDb
            End If
            
            ' During hold time, return held peak
            If timeSinceLastPeak < PeakHoldMs Then
                Return Math.Max(currentDb, lastPeakValueDb)
            End If
            
            ' After hold, apply decay
            If PeakDecayDbPerSec >= 999.0F Then
                ' Instant decay (effectively no hold after hold time)
                lastPeakValueDb = currentDb
                Return currentDb
            End If
            
            Dim decayTimeSeconds = (timeSinceLastPeak - PeakHoldMs) / 1000.0F
            Dim decayAmount = PeakDecayDbPerSec * decayTimeSeconds
            Dim decayedPeak = lastPeakValueDb - decayAmount
            
            ' Return max of current or decayed peak
            Dim result = Math.Max(currentDb, decayedPeak)
            
            ' Update last peak if it decayed
            If result < lastPeakValueDb Then
                lastPeakValueDb = result
            End If
            
            Return result
        End Function
        
        ''' <summary>
        ''' Apply attack/release smoothing for meter ballistics
        ''' </summary>
        Private Shared Function ApplyAttackRelease(currentDb As Single) As Single
            If AttackMs = 0 AndAlso ReleaseMs = 0 Then
                lastSmoothValueDb = currentDb
                Return currentDb ' Instant response
            End If
            
            Dim now = DateTime.Now
            Dim deltaTimeMs = (now - lastUpdateTime).TotalMilliseconds
            lastUpdateTime = now
            
            ' Prevent division by zero and handle first call
            If deltaTimeMs < 0.1 OrElse deltaTimeMs > 1000 Then
                deltaTimeMs = 33.33 ' Assume ~30fps
            End If
            
            ' Determine if rising (attack) or falling (release)
            Dim timeConstantMs = If(currentDb > lastSmoothValueDb, AttackMs, ReleaseMs)
            
            If timeConstantMs = 0 Then
                lastSmoothValueDb = currentDb
                Return currentDb ' Instant for this direction
            End If
            
            ' Calculate smoothing coefficient (exponential decay)
            Dim alpha = CSng(1.0 - Math.Exp(-deltaTimeMs / timeConstantMs))
            
            ' Apply smoothing
            lastSmoothValueDb = lastSmoothValueDb + alpha * (currentDb - lastSmoothValueDb)
            
            Return lastSmoothValueDb
        End Function
        
        ''' <summary>
        ''' Resets the peak hold and smoothing algorithms to their default state.
        ''' Call this method when starting a new recording or when peak tracking needs to be reset.
        ''' </summary>
        Public Shared Sub Reset()
            lastPeakTime = DateTime.MinValue
            lastPeakValueDb = MinDB
            lastSmoothValueDb = MinDB
            lastUpdateTime = DateTime.Now
        End Sub

        Private Shared Function Analyze16Bit(buffer As Byte(), channels As Integer) As LevelData
            Dim result As New LevelData
            Dim peakL As Single = 0
            Dim peakR As Single = 0
            Dim rmsL As Double = 0
            Dim rmsR As Double = 0
            Dim sampleCount As Integer = 0

            Try
                For i = 0 To buffer.Length - 2 Step (2 * channels)
                    ' Left channel (or mono)
                    Dim sampleL As Short = BitConverter.ToInt16(buffer, i)
                    Dim absL As Single = Math.Abs(sampleL / 32768.0F)
                    If absL > peakL Then peakL = absL
                    rmsL += absL * absL
                    sampleCount += 1

                    ' Right channel (if stereo)
                    If channels >= 2 AndAlso i + 2 < buffer.Length Then
                        Dim sampleR As Short = BitConverter.ToInt16(buffer, i + 2)
                        Dim absR As Single = Math.Abs(sampleR / 32768.0F)
                        If absR > peakR Then peakR = absR
                        rmsR += absR * absR
                    End If
                Next

                ' Calculate RMS
                If sampleCount > 0 Then
                    rmsL = Math.Sqrt(rmsL / sampleCount)
                    If channels >= 2 Then
                        rmsR = Math.Sqrt(rmsR / sampleCount)
                    Else
                        rmsR = rmsL
                        peakR = peakL
                    End If
                End If

                ' Convert to dB
                result.PeakLeftDB = AmplitudeToDB(peakL)
                result.PeakRightDB = AmplitudeToDB(peakR)
                result.PeakDB = Math.Max(result.PeakLeftDB, result.PeakRightDB)
                result.RMSDB = AmplitudeToDB(CSng((rmsL + rmsR) / 2))
                result.IsClipping = (result.PeakDB > ClipThresholdDb)

            Catch ex As Exception
                ' Return silent on error
                result.PeakDB = MinDB
                result.RMSDB = MinDB
                result.PeakLeftDB = MinDB
                result.PeakRightDB = MinDB
                result.IsClipping = False
            End Try


            ' Apply attack/release smoothing
            If lastUpdateTime <> DateTime.MinValue Then
                Dim timeElapsed As Double = (DateTime.Now - lastUpdateTime).TotalMilliseconds
                Dim attackFactor As Double = Math.Exp(-1.0 / (AttackMs * 0.001))
                Dim releaseFactor As Double = Math.Exp(-1.0 / (ReleaseMs * 0.001))
                result.RMSDB = CSng(If(result.RMSDB > lastSmoothValueDb, Math.Max(result.RMSDB, lastSmoothValueDb * attackFactor), Math.Min(result.RMSDB, lastSmoothValueDb * releaseFactor)))
            End If
            lastUpdateTime = DateTime.Now
            lastSmoothValueDb = result.RMSDB

            Return result
        End Function

        Private Shared Function Analyze24Bit(buffer As Byte(), channels As Integer) As LevelData
            ' 24-bit is stored as 3 bytes per sample
            Dim result As New LevelData
            Dim peakL As Single = 0
            Dim peakR As Single = 0
            Dim rmsL As Double = 0
            Dim rmsR As Double = 0
            Dim sampleCount As Integer = 0
            Dim bytesPerSample = 3

            Try
                For i = 0 To buffer.Length - bytesPerSample Step (bytesPerSample * channels)
                    ' Left channel - construct 24-bit integer
                    Dim sampleL As Integer = buffer(i) Or (buffer(i + 1) << 8) Or (buffer(i + 2) << 16)
                    If sampleL >= &H800000 Then sampleL -= &H1000000 ' Sign extend
                    Dim absL As Single = Math.Abs(sampleL / 8388608.0F)
                    If absL > peakL Then peakL = absL
                    rmsL += absL * absL
                    sampleCount += 1

                    ' Right channel
                    If channels >= 2 AndAlso i + (bytesPerSample * 2) <= buffer.Length Then
                        Dim offset = i + bytesPerSample
                        Dim sampleR As Integer = buffer(offset) Or (buffer(offset + 1) << 8) Or (buffer(offset + 2) << 16)
                        If sampleR >= &H800000 Then sampleR -= &H1000000
                        Dim absR As Single = Math.Abs(sampleR / 8388608.0F)
                        If absR > peakR Then peakR = absR
                        rmsR += absR * absR
                    End If
                Next

                If sampleCount > 0 Then
                    rmsL = Math.Sqrt(rmsL / sampleCount)
                    If channels >= 2 Then
                        rmsR = Math.Sqrt(rmsR / sampleCount)
                    Else
                        rmsR = rmsL
                        peakR = peakL
                    End If
                End If

                result.PeakLeftDB = AmplitudeToDB(peakL)
                result.PeakRightDB = AmplitudeToDB(peakR)
                result.PeakDB = Math.Max(result.PeakLeftDB, result.PeakRightDB)
                result.RMSDB = AmplitudeToDB(CSng((rmsL + rmsR) / 2))
                result.IsClipping = (result.PeakDB > ClipThresholdDb)

            Catch ex As Exception
                result.PeakDB = MinDB
                result.RMSDB = MinDB
                result.PeakLeftDB = MinDB
                result.PeakRightDB = MinDB
                result.IsClipping = False
            End Try


            ' Apply attack/release smoothing
            If lastUpdateTime <> DateTime.MinValue Then
                Dim timeElapsed As Double = (DateTime.Now - lastUpdateTime).TotalMilliseconds
                Dim attackFactor As Double = Math.Exp(-1.0 / (AttackMs * 0.001))
                Dim releaseFactor As Double = Math.Exp(-1.0 / (ReleaseMs * 0.001))
                result.RMSDB = CSng(If(result.RMSDB > lastSmoothValueDb, Math.Max(result.RMSDB, lastSmoothValueDb * attackFactor), Math.Min(result.RMSDB, lastSmoothValueDb * releaseFactor)))
            End If
            lastUpdateTime = DateTime.Now
            lastSmoothValueDb = result.RMSDB

            Return result
        End Function

        Private Shared Function Analyze32Bit(buffer As Byte(), channels As Integer) As LevelData
            ' 32-bit float samples
            Dim result As New LevelData
            Dim peakL As Single = 0
            Dim peakR As Single = 0
            Dim rmsL As Double = 0
            Dim rmsR As Double = 0
            Dim sampleCount As Integer = 0

            Try
                For i = 0 To buffer.Length - 4 Step (4 * channels)
                    ' Left channel
                    Dim sampleL As Single = BitConverter.ToSingle(buffer, i)
                    Dim absL As Single = Math.Abs(sampleL)
                    If absL > peakL Then peakL = absL
                    rmsL += absL * absL
                    sampleCount += 1

                    ' Right channel
                    If channels >= 2 AndAlso i + 4 < buffer.Length Then
                        Dim sampleR As Single = BitConverter.ToSingle(buffer, i + 4)
                        Dim absR As Single = Math.Abs(sampleR)
                        If absR > peakR Then peakR = absR
                        rmsR += absR * absR
                    End If
                Next

                If sampleCount > 0 Then
                    rmsL = Math.Sqrt(rmsL / sampleCount)
                    If channels >= 2 Then
                        rmsR = Math.Sqrt(rmsR / sampleCount)
                    Else
                        rmsR = rmsL
                        peakR = peakL
                    End If
                End If

                result.PeakLeftDB = AmplitudeToDB(peakL)
                result.PeakRightDB = AmplitudeToDB(peakR)
                result.PeakDB = Math.Max(result.PeakLeftDB, result.PeakRightDB)
                result.RMSDB = AmplitudeToDB(CSng((rmsL + rmsR) / 2))
                result.IsClipping = (result.PeakDB > ClipThresholdDb)

            Catch ex As Exception
                result.PeakDB = MinDB
                result.RMSDB = MinDB
                result.PeakLeftDB = MinDB
                result.PeakRightDB = MinDB
                result.IsClipping = False
            End Try


            ' Apply attack/release smoothing
            If lastUpdateTime <> DateTime.MinValue Then
                Dim timeElapsed As Double = (DateTime.Now - lastUpdateTime).TotalMilliseconds
                Dim attackFactor As Double = Math.Exp(-1.0 / (AttackMs * 0.001))
                Dim releaseFactor As Double = Math.Exp(-1.0 / (ReleaseMs * 0.001))
                result.RMSDB = CSng(If(result.RMSDB > lastSmoothValueDb, Math.Max(result.RMSDB, lastSmoothValueDb * attackFactor), Math.Min(result.RMSDB, lastSmoothValueDb * releaseFactor)))
            End If
            lastUpdateTime = DateTime.Now
            lastSmoothValueDb = result.RMSDB

            Return result
        End Function

        ''' <summary>
        ''' Converts linear amplitude (0.0 to 1.0) to dB scale
        ''' </summary>
        Private Shared Function AmplitudeToDB(amplitude As Single) As Single
            If amplitude < 0.00001F Then Return MinDB
            Dim db = CSng(20 * Math.Log10(amplitude))
            If db < MinDB Then Return MinDB
            If db > 0 Then Return 0
            Return db
        End Function

    End Class

End Namespace
