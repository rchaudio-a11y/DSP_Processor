Imports NAudio.Wave
Imports NAudio.CoreAudioApi
Imports System.Collections.Concurrent

Namespace AudioIO

    ''' <summary>
    ''' Audio engine implementation for WASAPI (Windows Audio Session API).
    ''' Provides low-latency, high-quality audio capture using WASAPI Shared Mode.
    ''' </summary>
    ''' <remarks>
    ''' Created: Phase 1, Task 1.2
    ''' Purpose: Professional-grade audio input with lower latency than WaveIn
    ''' Note: Currently implements Shared Mode only. Exclusive Mode will be added later.
    ''' </remarks>
    Public Class WasapiEngine
        Implements IAudioEngine
        Implements IInputSource

#Region "Private Fields"

        Private _wasapiCapture As WasapiCapture
        Private _deviceInfo As DeviceInfo
        Private _disposed As Boolean = False
        Private _sampleRate As Integer = 48000
        Private _channels As Integer = 2
        Private _bitsPerSample As Integer = 16
        Private _latencyMs As Integer = 10
        
        ' Track native WASAPI format for conversion
        Private _nativeBitsPerSample As Integer = 16
        Private _nativeEncoding As WaveFormatEncoding = WaveFormatEncoding.Pcm
        
        ' Dual buffer queues (like MicInputSource)
        Private bufferQueue As New ConcurrentQueue(Of Byte())      ' CRITICAL: Recording
        Private fftQueue As New ConcurrentQueue(Of Byte())         ' FREEWHEELING: FFT
        Private Const MAX_FFT_QUEUE_DEPTH As Integer = 5            ' Max 5 frames (~100ms)
        
        ' Volume control
        Private _volume As Single = 1.0F

#End Region

#Region "Properties"

        ''' <summary>Gets whether audio is currently being captured</summary>
        Public ReadOnly Property IsRecording As Boolean Implements IAudioEngine.IsRecording
            Get
                Return _wasapiCapture IsNot Nothing AndAlso _wasapiCapture.CaptureState = CaptureState.Capturing
            End Get
        End Property

        ''' <summary>Gets the audio sample rate in Hz</summary>
        Public ReadOnly Property SampleRate As Integer Implements IAudioEngine.SampleRate, IInputSource.SampleRate
            Get
                Return _sampleRate
            End Get
        End Property

        ''' <summary>Gets the number of audio channels</summary>
        Public ReadOnly Property Channels As Integer Implements IAudioEngine.Channels, IInputSource.Channels
            Get
                Return _channels
            End Get
        End Property

        ''' <summary>Gets the bit depth per sample</summary>
        Public ReadOnly Property BitsPerSample As Integer Implements IAudioEngine.BitsPerSample, IInputSource.BitsPerSample
            Get
                Return _bitsPerSample
            End Get
        End Property

        ''' <summary>Gets the actual measured latency in milliseconds</summary>
        Public ReadOnly Property Latency As Integer Implements IAudioEngine.Latency
            Get
                Return _latencyMs
            End Get
        End Property

        ''' <summary>Gets the latency in milliseconds (Task 1.1 addition)</summary>
        Public ReadOnly Property LatencyMS As Integer Implements IAudioEngine.LatencyMS
            Get
                Return _latencyMs
            End Get
        End Property

        ''' <summary>Gets the audio engine name</summary>
        Public ReadOnly Property EngineName As String Implements IAudioEngine.EngineName
            Get
                Return "WASAPI"
            End Get
        End Property

        ''' <summary>Gets the driver type (Task 1.1 addition)</summary>
        Public ReadOnly Property DriverType As DriverType Implements IAudioEngine.DriverType
            Get
                Return AudioIO.DriverType.WASAPI
            End Get
        End Property

        ''' <summary>Gets the list of supported devices (Task 1.1 addition)</summary>
        Public ReadOnly Property SupportedDevices As IEnumerable(Of DeviceInfo) Implements IAudioEngine.SupportedDevices
            Get
                Return AudioInputManager.Instance.GetDevices(AudioIO.DriverType.WASAPI)
            End Get
        End Property
        
        ''' <summary>Gets or sets the input volume (0.0 to 2.0)</summary>
        Public Property Volume As Single
            Get
                Return _volume
            End Get
            Set(value As Single)
                _volume = Math.Max(0.0F, Math.Min(2.0F, value))
                Utils.Logger.Instance.Debug($"WasapiEngine volume set to {(_volume * 100)}%", "WasapiEngine")
            End Set
        End Property
        
        ''' <summary>Gets the current recording buffer queue depth</summary>
        Public ReadOnly Property BufferQueueCount As Integer
            Get
                Return bufferQueue.Count
            End Get
        End Property

#End Region

#Region "Events"

        ''' <summary>Raised when audio data is available</summary>
        Public Event DataAvailable As EventHandler(Of AudioDataEventArgs) Implements IAudioEngine.DataAvailable

        ''' <summary>Raised when recording stops</summary>
        Public Event RecordingStopped As EventHandler(Of StoppedEventArgs) Implements IAudioEngine.RecordingStopped

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Creates a new WasapiEngine for the specified device
        ''' </summary>
        Public Sub New(deviceInfo As DeviceInfo, sampleRate As Integer, channels As Integer, bitsPerSample As Integer, Optional latencyMs As Integer = 10)
            If deviceInfo Is Nothing Then
                Throw New ArgumentNullException(NameOf(deviceInfo))
            End If

            If deviceInfo.DriverType <> AudioIO.DriverType.WASAPI Then
                Throw New ArgumentException("DeviceInfo must be for WASAPI driver type")
            End If

            _deviceInfo = deviceInfo
            _sampleRate = sampleRate
            _channels = channels
            _bitsPerSample = bitsPerSample
            _latencyMs = latencyMs

            Utils.Logger.Instance.Info($"WasapiEngine created: Device={deviceInfo.Name}, Channels={_channels}, Rate={_sampleRate}Hz, Bits={_bitsPerSample}, Latency={_latencyMs}ms", "WasapiEngine")
        End Sub

        ''' <summary>
        ''' Creates a new WasapiEngine with default device and settings
        ''' </summary>
        Public Sub New()
            Dim defaultDevice = AudioInputManager.Instance.GetDefaultDevice(AudioIO.DriverType.WASAPI)
            If defaultDevice Is Nothing Then
                Throw New InvalidOperationException("No WASAPI devices available")
            End If

            _deviceInfo = defaultDevice
            Utils.Logger.Instance.Info("WasapiEngine created with default settings", "WasapiEngine")
        End Sub

#End Region

#Region "Methods"

        ''' <summary>
        ''' Starts capturing audio using WASAPI Shared Mode
        ''' </summary>
        Public Sub Start() Implements IAudioEngine.Start
            If _wasapiCapture Is Nothing Then
                Try
                    ' Get the MMDevice by ID
                    Using enumerator As New MMDeviceEnumerator()
                        Dim mmDevice = enumerator.GetDevice(_deviceInfo.Id)

                        ' Create WASAPI capture in Shared Mode
                        ' Shared mode uses the Windows audio engine's mix format
                        _wasapiCapture = New WasapiCapture(mmDevice, True, _latencyMs)

                        ' Get actual format being used and store native format
                        Dim format = _wasapiCapture.WaveFormat
                        _sampleRate = format.SampleRate  ' Use WASAPI's native rate (48kHz)
                        _channels = format.Channels
                        _nativeBitsPerSample = format.BitsPerSample  ' Store native (32-bit)
                        _nativeEncoding = format.Encoding             ' Store encoding (IeeeFloat)
                        ' Always report 16-bit to consumers (we convert internally)
                        _bitsPerSample = 16

                        Utils.Logger.Instance.Info($"WASAPI initialized: {format.SampleRate}Hz, {format.Channels}ch, {format.BitsPerSample}bit/{format.Encoding} (native) -> converted to {_sampleRate}Hz/16bit PCM, {_latencyMs}ms latency", "WasapiEngine")

                        ' Wire up events
                        AddHandler _wasapiCapture.DataAvailable, AddressOf OnWasapiDataAvailable
                        AddHandler _wasapiCapture.RecordingStopped, AddressOf OnWasapiRecordingStopped
                    End Using

                Catch ex As Exception
                    Utils.Logger.Instance.Error($"Failed to initialize WASAPI: {ex.Message}", ex, "WasapiEngine")
                    _wasapiCapture?.Dispose()
                    _wasapiCapture = Nothing
                    Throw
                End Try
            End If

            _wasapiCapture.StartRecording()
            Utils.Logger.Instance.Debug("WasapiEngine started", "WasapiEngine")
        End Sub

        ''' <summary>
        ''' Stops capturing audio
        ''' </summary>
        Public Sub [Stop]() Implements IAudioEngine.Stop
            If _wasapiCapture IsNot Nothing Then
                _wasapiCapture.StopRecording()
                Utils.Logger.Instance.Debug("WasapiEngine stopped", "WasapiEngine")
            End If
        End Sub
        
        ''' <summary>
        ''' Read audio data from the recording queue (IInputSource interface for RecordingManager)
        ''' </summary>
        Public Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer Implements IInputSource.Read
            Dim totalRead As Integer = 0
            Dim outBuffer As Byte() = Nothing
            
            ' Read from recording queue (critical path)
            While totalRead < count AndAlso bufferQueue.TryDequeue(outBuffer)
                Dim bytesToCopy = Math.Min(count - totalRead, outBuffer.Length)
                Array.Copy(outBuffer, 0, buffer, offset + totalRead, bytesToCopy)
                totalRead += bytesToCopy
            End While
            
            Return totalRead
        End Function
        
        ''' <summary>
        ''' Read audio data for FFT processing (freewheeling path)
        ''' </summary>
        Public Function ReadForFFT(buffer() As Byte, offset As Integer, count As Integer) As Integer
            Dim totalRead As Integer = 0
            
            ' Read from FFT queue (can drop frames)
            Dim outBuffer As Byte() = Nothing
            While totalRead < count AndAlso fftQueue.TryDequeue(outBuffer)
                Dim bytesToCopy = Math.Min(count - totalRead, outBuffer.Length)
                Array.Copy(outBuffer, 0, buffer, offset + totalRead, bytesToCopy)
                totalRead += bytesToCopy
            End While
            
            Return totalRead
        End Function
        
        ''' <summary>
        ''' Clear all buffers (both recording and FFT)
        ''' </summary>
        Public Sub ClearBuffers()
            ' Clear recording queue
            While bufferQueue.TryDequeue(Nothing)
                ' Keep dequeuing until empty
            End While
            
            ' Clear FFT queue
            While fftQueue.TryDequeue(Nothing)
                ' Keep dequeuing until empty
            End While
            
            Utils.Logger.Instance.Debug("WasapiEngine buffers cleared", "WasapiEngine")
        End Sub

        ''' <summary>
        ''' Checks if this engine supports exclusive mode (not yet implemented)
        ''' </summary>
        Public Function SupportsExclusiveMode() As Boolean Implements IAudioEngine.SupportsExclusiveMode
            ' TODO: Implement exclusive mode in future enhancement
            Return False
        End Function

        ''' <summary>
        ''' Gets the optimal buffer size for WASAPI
        ''' </summary>
        Public Function GetOptimalBufferSize() As Integer Implements IAudioEngine.GetOptimalBufferSize
            ' WASAPI works best with very small buffers (3-10ms)
            ' At 48kHz, 10ms = 480 samples
            Dim targetMs = 10
            Dim samplesPerMs = _sampleRate / 1000
            Return targetMs * samplesPerMs
        End Function

#End Region

#Region "Private Event Handlers"

        Private Sub OnWasapiDataAvailable(sender As Object, e As WaveInEventArgs)
            Try
                ' WASAPI typically uses 32-bit float format
                ' We need to convert to 16-bit PCM for compatibility
                Dim convertedBuffer() As Byte
                
                If _nativeBitsPerSample = 32 AndAlso _nativeEncoding = WaveFormatEncoding.IeeeFloat Then
                    ' Convert from 32-bit float to 16-bit PCM
                    convertedBuffer = ConvertFloatToPCM16(e.Buffer, e.BytesRecorded)
                Else
                    ' Already in correct format, just copy
                    ReDim convertedBuffer(e.BytesRecorded - 1)
                    Array.Copy(e.Buffer, convertedBuffer, e.BytesRecorded)
                End If
                
                ' Apply volume if not 1.0
                If _volume <> 1.0F Then
                    ApplyVolume(convertedBuffer, convertedBuffer.Length)
                End If
                
                ' CRITICAL PATH: Always enqueue to recording queue (never drop!)
                bufferQueue.Enqueue(convertedBuffer)
                
                ' FREEWHEELING PATH: Drop old frames if FFT queue full
                If fftQueue.Count >= MAX_FFT_QUEUE_DEPTH Then
                    Dim discarded As Byte() = Nothing
                    fftQueue.TryDequeue(discarded)  ' Drop oldest
                End If
                
                ' Enqueue to FFT queue (separate copy)
                Dim fftCopy(convertedBuffer.Length - 1) As Byte
                Array.Copy(convertedBuffer, fftCopy, convertedBuffer.Length)
                fftQueue.Enqueue(fftCopy)

                ' Raise event with buffer and bytes recorded (for compatibility)
                RaiseEvent DataAvailable(Me, New AudioDataEventArgs(convertedBuffer, convertedBuffer.Length))

            Catch ex As Exception
                Utils.Logger.Instance.Error($"Error in WASAPI data callback: {ex.Message}", ex, "WasapiEngine")
            End Try
        End Sub
        
        ''' <summary>
        ''' Convert 32-bit IEEE float samples to 16-bit PCM
        ''' </summary>
        Private Function ConvertFloatToPCM16(floatBuffer() As Byte, byteCount As Integer) As Byte()
            Dim floatCount = byteCount \ 4 ' 4 bytes per float
            Dim pcmBuffer(floatCount * 2 - 1) As Byte ' 2 bytes per PCM16 sample
            
            For i = 0 To floatCount - 1
                ' Read float value (-1.0 to +1.0)
                Dim floatSample = BitConverter.ToSingle(floatBuffer, i * 4)
                
                ' Clamp to valid range
                floatSample = Math.Max(-1.0F, Math.Min(1.0F, floatSample))
                
                ' Convert to 16-bit signed integer (-32768 to +32767)
                Dim pcmSample = CShort(floatSample * 32767.0F)
                
                ' Write as 16-bit little-endian
                Dim pcmBytes = BitConverter.GetBytes(pcmSample)
                pcmBuffer(i * 2) = pcmBytes(0)
                pcmBuffer(i * 2 + 1) = pcmBytes(1)
            Next
            
            Return pcmBuffer
        End Function
        
        ''' <summary>Apply volume to buffer (16-bit PCM)</summary>
        Private Sub ApplyVolume(buffer() As Byte, count As Integer)
            If _bitsPerSample = 16 Then
                For i = 0 To count - 1 Step 2
                    If i + 1 < count Then
                        Dim sample = BitConverter.ToInt16(buffer, i)
                        sample = CShort(sample * _volume)
                        Dim bytes = BitConverter.GetBytes(sample)
                        buffer(i) = bytes(0)
                        buffer(i + 1) = bytes(1)
                    End If
                Next
            End If
        End Sub

        Private Sub OnWasapiRecordingStopped(sender As Object, e As NAudio.Wave.StoppedEventArgs)
            Utils.Logger.Instance.Debug("WASAPI recording stopped", "WasapiEngine")

            ' Convert NAudio StoppedEventArgs to our StoppedEventArgs
            Dim args = New StoppedEventArgs(e.Exception)
            RaiseEvent RecordingStopped(Me, args)
        End Sub

#End Region

#Region "IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            If Not _disposed Then
                [Stop]()

                If _wasapiCapture IsNot Nothing Then
                    RemoveHandler _wasapiCapture.DataAvailable, AddressOf OnWasapiDataAvailable
                    RemoveHandler _wasapiCapture.RecordingStopped, AddressOf OnWasapiRecordingStopped
                    _wasapiCapture.Dispose()
                    _wasapiCapture = Nothing
                End If
                
                ' Clear buffers
                While bufferQueue.TryDequeue(Nothing)
                End While
                While fftQueue.TryDequeue(Nothing)
                End While

                _disposed = True
                Utils.Logger.Instance.Debug("WasapiEngine disposed", "WasapiEngine")
            End If
        End Sub

#End Region

    End Class

End Namespace
