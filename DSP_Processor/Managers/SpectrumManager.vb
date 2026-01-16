Imports DSP_Processor.DSP.FFT
Imports DSP_Processor.Models
Imports DSP_Processor.Utils

Namespace Managers

    ''' <summary>
    ''' Manages FFT processing for spectrum visualization (Task 2.0.4 - FINAL)
    ''' Simple, focused manager - owns FFT processors and settings logic
    ''' </summary>
    Public Class SpectrumManager
        Implements IDisposable

        Private fftProcessorInput As FFTProcessor
        Private fftProcessorOutput As FFTProcessor
        Private disposed As Boolean = False

        ''' <summary>
        ''' Initialize with default settings
        ''' </summary>
        Public Sub Initialize(Optional fftSize As Integer = 4096, Optional sampleRate As Integer = 44100)
            fftProcessorInput = New FFTProcessor(fftSize) With {
                .SampleRate = sampleRate,
                .WindowFunction = FFTProcessor.WindowType.Hann
            }

            fftProcessorOutput = New FFTProcessor(fftSize) With {
                .SampleRate = sampleRate,
                .WindowFunction = FFTProcessor.WindowType.Hann
            }

            Logger.Instance.Info("SpectrumManager initialized (Task 2.0.4)", "SpectrumManager")
        End Sub

        ''' <summary>
        ''' Apply spectrum settings (Task 2.0.4 - moved from MainForm.ApplySpectrumSettings)
        ''' </summary>
        Public Sub ApplySettings(settings As SpectrumSettings)
            Try
                ' Apply FFT size
                fftProcessorInput.FFTSize = settings.FFTSize
                fftProcessorOutput.FFTSize = settings.FFTSize

                ' Apply window function
                Dim windowType As FFTProcessor.WindowType
                Select Case settings.WindowFunction
                    Case "None"
                        windowType = FFTProcessor.WindowType.None
                    Case "Hann"
                        windowType = FFTProcessor.WindowType.Hann
                    Case "Hamming"
                        windowType = FFTProcessor.WindowType.Hamming
                    Case "Blackman"
                        windowType = FFTProcessor.WindowType.Blackman
                    Case Else
                        windowType = FFTProcessor.WindowType.Hann
                End Select

                fftProcessorInput.WindowFunction = windowType
                fftProcessorOutput.WindowFunction = windowType

                Logger.Instance.Info($"Spectrum settings applied: FFT={settings.FFTSize}, Window={settings.WindowFunction}", "SpectrumManager")

            Catch ex As Exception
                Logger.Instance.Error("Failed to apply spectrum settings", ex, "SpectrumManager")
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Process input (pre-DSP) samples and return spectrum (Task 2.0.4 - moved from MainForm)
        ''' </summary>
        Public Function ProcessInputSamples(samples As Byte(), count As Integer, bitsPerSample As Integer,
                                           channels As Integer, sampleRate As Integer) As Single()
            Try
                fftProcessorInput.SampleRate = sampleRate
                fftProcessorInput.AddSamples(samples, count, bitsPerSample, channels)
                Return fftProcessorInput.CalculateSpectrum()
            Catch ex As Exception
                Logger.Instance.Error("Error processing input samples", ex, "SpectrumManager")
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Process output (post-DSP) samples and return spectrum (Task 2.0.4 - moved from MainForm)
        ''' </summary>
        Public Function ProcessOutputSamples(samples As Byte(), count As Integer, bitsPerSample As Integer,
                                            channels As Integer, sampleRate As Integer) As Single()
            Try
                fftProcessorOutput.SampleRate = sampleRate
                fftProcessorOutput.AddSamples(samples, count, bitsPerSample, channels)
                Return fftProcessorOutput.CalculateSpectrum()
            Catch ex As Exception
                Logger.Instance.Error("Error processing output samples", ex, "SpectrumManager")
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Clear FFT buffers (e.g., when starting new recording)
        ''' </summary>
        Public Sub Clear()
            fftProcessorInput?.Clear()
            fftProcessorOutput?.Clear()
        End Sub

        ''' <summary>
        ''' Get current FFT size
        ''' </summary>
        Public ReadOnly Property FFTSize As Integer
            Get
                Return fftProcessorInput?.FFTSize
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
            If Not disposed Then
                fftProcessorInput?.Clear()
                fftProcessorOutput?.Clear()
                disposed = True
                Logger.Instance.Info("SpectrumManager disposed", "SpectrumManager")
            End If
        End Sub

    End Class

End Namespace
