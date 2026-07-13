Imports NAudio.Dsp

Namespace DSP.FFT

    ''' <summary>
    ''' FFT processor using NAudio for real-time spectrum analysis
    ''' </summary>
    Public Class FFTProcessor
        Private fftLength As Integer
        Private fftBuffer() As Complex  ' NAudio's Complex type
        Private windowBuffer() As Single
        Private sampleBuffer As New List(Of Single)
        Private m As Integer ' FFT order (2^m = fftLength)
        
        ''' <summary>
        ''' FFT size (must be power of 2)
        ''' </summary>
        Public Property FFTSize As Integer
            Get
                Return fftLength
            End Get
            Set(value As Integer)
                If Not IsPowerOfTwo(value) Then
                    Throw New ArgumentException("FFT size must be a power of 2")
                End If
                
                fftLength = value
                m = CInt(Math.Log(fftLength, 2))
                ReDim fftBuffer(fftLength - 1)
                CalculateWindow()
                sampleBuffer.Clear()
            End Set
        End Property
        
        ''' <summary>
        ''' Window function type
        ''' </summary>
        Public Enum WindowType
            None
            Hann
            Hamming
            Blackman
        End Enum
        
        ''' <summary>
        ''' Current window type
        ''' </summary>
        Public Property WindowFunction As WindowType = WindowType.Hann
        
        ''' <summary>
        ''' Sample rate for frequency calculation
        ''' </summary>
        Public Property SampleRate As Integer = 44100
        
        Public Sub New(Optional fftSize As Integer = 4096)
            Me.FFTSize = fftSize
        End Sub
        
        ''' <summary>
        ''' Add audio samples to the buffer
        ''' </summary>
        ''' <param name="buffer">Audio data as bytes (PCM16)</param>
        ''' <param name="count">Number of bytes in buffer</param>
        ''' <param name="bitsPerSample">Bits per sample (16, 24, or 32)</param>
        ''' <param name="channels">Number of audio channels (1=mono, 2=stereo)</param>
        Public Sub AddSamples(buffer() As Byte, count As Integer, bitsPerSample As Integer, channels As Integer)
            ' DIAGNOSTIC: Log first call to check what we're receiving
            Static firstCall As Boolean = True
            If firstCall Then
                Utils.Logger.Instance.Info($"AddSamples FIRST CALL: count={count} bytes, bitsPerSample={bitsPerSample}, channels={channels}", "FFTProcessor")
                If count >= 4 AndAlso bitsPerSample = 16 Then
                    Dim sample0 = BitConverter.ToInt16(buffer, 0)
                    Dim sample1 = BitConverter.ToInt16(buffer, 2)
                    Utils.Logger.Instance.Info($"  First 2 samples (RAW PCM16): {sample0}, {sample1}", "FFTProcessor")
                End If
                firstCall = False
            End If
            
            ' Convert bytes to floats
            Select Case bitsPerSample
                Case 16
                    If channels = 2 Then
                        ' STEREO: Mix to mono by averaging L+R channels
                        For i As Integer = 0 To count - 1 Step 4  ' 4 bytes = 2 samples (L+R)
                            If i + 3 < count Then
                                Dim sampleL As Short = BitConverter.ToInt16(buffer, i)
                                Dim sampleR As Short = BitConverter.ToInt16(buffer, i + 2)
                                Dim monoSample As Single = ((sampleL / 32768.0F) + (sampleR / 32768.0F)) / 2.0F
                                sampleBuffer.Add(monoSample)
                            End If
                        Next
                    Else
                        ' MONO: Just read single channel
                        For i As Integer = 0 To count - 1 Step 2
                            If i + 1 < count Then
                                Dim sample As Short = BitConverter.ToInt16(buffer, i)
                                Dim normalized As Single = sample / 32768.0F
                                sampleBuffer.Add(normalized)
                            End If
                        Next
                    End If
                    
                Case 24
                    For i As Integer = 0 To count - 1 Step 3
                        If i + 2 < count Then
                            ' Read 24-bit sample (little-endian)
                            Dim sample As Integer = buffer(i) Or (buffer(i + 1) << 8) Or (buffer(i + 2) << 16)
                            ' Sign extend
                            If (sample And &H800000) <> 0 Then
                                sample = sample Or &HFF000000
                            End If
                            Dim normalized As Single = sample / 8388608.0F
                            sampleBuffer.Add(normalized)
                        End If
                    Next
                    
                Case 32
                    ' IEEE float32 processing domain (feature 001) - no int32-PCM
                    ' sources exist in this application (analysis B1).
                    If channels = 2 Then
                        ' STEREO: mix to mono by averaging L+R (parity with Case 16)
                        For i As Integer = 0 To count - 1 Step 8 ' 8 bytes = 2 float samples
                            If i + 7 < count Then
                                Dim sampleL As Single = BitConverter.ToSingle(buffer, i)
                                Dim sampleR As Single = BitConverter.ToSingle(buffer, i + 4)
                                sampleBuffer.Add((sampleL + sampleR) * 0.5F)
                            End If
                        Next
                    Else
                        For i As Integer = 0 To count - 1 Step 4
                            If i + 3 < count Then
                                sampleBuffer.Add(BitConverter.ToSingle(buffer, i))
                            End If
                        Next
                    End If
            End Select
        End Sub
        
        ''' <summary>
        ''' Calculate FFT and return magnitude spectrum in dB
        ''' </summary>
        ''' <returns>Array of dB values (DC to Nyquist)</returns>
        Public Function CalculateSpectrum() As Single()
            If sampleBuffer.Count < fftLength Then
                ' Not enough samples yet
                Return New Single(fftLength \ 2 - 1) {}
            End If
            
            ' DIAGNOSTIC: Log buffer state periodically
            Static diagCount As Integer = 0
            diagCount += 1
            If diagCount Mod 30 = 0 Then
                Dim diagStartIndex = Math.Max(0, sampleBuffer.Count - fftLength)
                Dim maxSample As Single = 0.0F
                For i = diagStartIndex To Math.Min(diagStartIndex + 99, sampleBuffer.Count - 1)
                    maxSample = Math.Max(maxSample, Math.Abs(sampleBuffer(i)))
                Next
                Dim maxDB = 20.0F * Math.Log10(Math.Max(maxSample, 0.00001F))
                Utils.Logger.Instance.Info($"FFT CalculateSpectrum: sampleBuffer.Count={sampleBuffer.Count}, last 100 samples peak={maxDB:F1}dB", "FFTProcessor")
            End If
            
            ' Take last fftLength samples
            Dim startIndex = Math.Max(0, sampleBuffer.Count - fftLength)
            
            ' Fill FFT buffer with windowed samples
            For i As Integer = 0 To fftLength - 1
                Dim sampleIndex = startIndex + i
                If sampleIndex < sampleBuffer.Count Then
                    fftBuffer(i).X = sampleBuffer(sampleIndex) * windowBuffer(i)
                    fftBuffer(i).Y = 0
                Else
                    fftBuffer(i).X = 0
                    fftBuffer(i).Y = 0
                End If
            Next
            
            ' Perform FFT
            FastFourierTransform.FFT(True, m, fftBuffer)
            
            ' Calculate magnitude spectrum (only positive frequencies)
            Dim halfSize = fftLength \ 2
            Dim spectrum(halfSize - 1) As Single
            
            ' Simple calibration: add 15 dB to compensate for window loss + FFT scaling
            ' This brings -12 dBFS audio to approximately -12 dB on the spectrum
            Const calibrationOffset As Single = 15.0F
            
            For i As Integer = 0 To halfSize - 1
                Dim real = fftBuffer(i).X
                Dim imag = fftBuffer(i).Y
                Dim magnitude = Math.Sqrt(real * real + imag * imag)
                
                ' Convert to dB with calibration offset
                magnitude = Math.Max(magnitude, 0.0000001F)
                spectrum(i) = 20.0F * Math.Log10(magnitude) + calibrationOffset
            Next
            
            Return spectrum
        End Function
        
        ''' <summary>
        ''' Get frequency for a given bin index
        ''' </summary>
        Public Function GetFrequencyForBin(binIndex As Integer) As Single
            Return CSng(binIndex * SampleRate / fftLength)
        End Function
        
        ''' <summary>
        ''' Get bin index for a given frequency
        ''' </summary>
        Public Function GetBinForFrequency(frequency As Single) As Integer
            Return CInt(frequency * fftLength / SampleRate)
        End Function
        
        ''' <summary>
        ''' Clear the sample buffer
        ''' </summary>
        Public Sub Clear()
            sampleBuffer.Clear()
        End Sub
        
        ''' <summary>
        ''' Calculate window function
        ''' </summary>
        Private Sub CalculateWindow()
            ReDim windowBuffer(fftLength - 1)
            
            Select Case WindowFunction
                Case WindowType.None
                    ' Rectangular window (no window)
                    For i As Integer = 0 To fftLength - 1
                        windowBuffer(i) = 1.0F
                    Next
                    
                Case WindowType.Hann
                    ' Hann window (raised cosine)
                    For i As Integer = 0 To fftLength - 1
                        windowBuffer(i) = CSng(0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / fftLength)))
                    Next
                    
                Case WindowType.Hamming
                    ' Hamming window
                    For i As Integer = 0 To fftLength - 1
                        windowBuffer(i) = CSng(0.54 - 0.46 * Math.Cos(2.0 * Math.PI * i / fftLength))
                    Next
                    
                Case WindowType.Blackman
                    ' Blackman window
                    For i As Integer = 0 To fftLength - 1
                        windowBuffer(i) = CSng(0.42 - 0.5 * Math.Cos(2.0 * Math.PI * i / fftLength) + 
                                        0.08 * Math.Cos(4.0 * Math.PI * i / fftLength))
                    Next
            End Select
        End Sub
        
        ''' <summary>
        ''' Check if value is power of 2
        ''' </summary>
        Private Function IsPowerOfTwo(value As Integer) As Boolean
            Return value > 0 AndAlso (value And (value - 1)) = 0
        End Function
        
    End Class

End Namespace
