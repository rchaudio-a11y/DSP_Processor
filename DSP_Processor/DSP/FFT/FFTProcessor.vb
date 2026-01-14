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
        Public Sub AddSamples(buffer() As Byte, count As Integer, bitsPerSample As Integer)
            ' Convert bytes to floats
            Select Case bitsPerSample
                Case 16
                    For i As Integer = 0 To count - 1 Step 2
                        If i + 1 < count Then
                            Dim sample As Short = BitConverter.ToInt16(buffer, i)
                            Dim normalized As Single = sample / 32768.0F
                            sampleBuffer.Add(normalized)
                        End If
                    Next
                    
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
                    For i As Integer = 0 To count - 1 Step 4
                        If i + 3 < count Then
                            Dim sample As Single = BitConverter.ToSingle(buffer, i)
                            sampleBuffer.Add(sample)
                        End If
                    Next
            End Select
        End Sub
        
        ''' <summary>
        ''' Calculate FFT and return magnitude spectrum in dB
        ''' INDUSTRY STANDARD: Properly normalized and calibrated to dBFS
        ''' </summary>
        ''' <returns>Array of dB values (DC to Nyquist)</returns>
        Public Function CalculateSpectrum() As Single()
            If sampleBuffer.Count < fftLength Then
                ' Not enough samples yet
                Return New Single(fftLength \ 2 - 1) {}
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
            
            ' Calculate window coherent gain (for proper amplitude calibration)
            Dim windowGain As Single = GetWindowCoherentGain()
            
            For i As Integer = 0 To halfSize - 1
                Dim real = fftBuffer(i).X
                Dim imag = fftBuffer(i).Y
                
                ' Calculate magnitude
                Dim magnitude = Math.Sqrt(real * real + imag * imag)
                
                ' STEP 1: Normalize by FFT length (convert FFT output to amplitude)
                ' This makes a full-scale sine wave have magnitude = 1.0
                magnitude /= (fftLength / 2.0F)
                
                ' STEP 2: Compensate for window coherent gain
                ' Hann window reduces amplitude by ~6 dB, we compensate here
                magnitude /= windowGain
                
                ' STEP 3: Convert to dBFS (referenced to full scale)
                ' Avoid log(0) with a very small floor (-120 dB)
                magnitude = Math.Max(magnitude, 0.000001F) ' -120 dBFS floor
                
                ' Convert to dB: 20*log10(magnitude)
                ' Now a -12 dBFS sine will show as -12 dB in the spectrum!
                spectrum(i) = 20.0F * Math.Log10(magnitude)
            Next
            
            Return spectrum
        End Function
        
        ''' <summary>
        ''' Get coherent gain of current window function
        ''' This is the amplitude reduction factor that must be compensated
        ''' </summary>
        Private Function GetWindowCoherentGain() As Single
            Select Case WindowFunction
                Case WindowType.None
                    Return 1.0F ' Rectangular window has no loss
                    
                Case WindowType.Hann
                    Return 0.5F ' Hann window coherent gain = 0.5 (-6.02 dB)
                    
                Case WindowType.Hamming
                    Return 0.54F ' Hamming window coherent gain ? 0.54
                    
                Case WindowType.Blackman
                    Return 0.42F ' Blackman window coherent gain ? 0.42
                    
                Case Else
                    Return 1.0F
            End Select
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
