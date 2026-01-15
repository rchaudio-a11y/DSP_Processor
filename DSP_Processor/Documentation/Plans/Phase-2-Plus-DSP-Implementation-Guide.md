# Phase 2+: Advanced DSP Implementation Guide ?????

## ?? Document Overview

**Purpose:** Guide for implementing real-time DSP processing pipeline  
**Scope:** Multiband crossover, EQ, dynamics, filters  
**Timeline:** Phases 2, 3, 4 (8-16 weeks)  
**Architecture:** Master Architecture (Threading & Performance) compliant

---

## ??? DSP Architecture Overview

### Thread Architecture (From Master Architecture Doc)

```
????????????????????????????????????????????????????????
? AUDIO THREAD (High Priority - Real-Time)            ?
?  - Captures PCM from microphone                      ?
?  - Writes to Ring Buffer (lock-free)                ?
?  - NO DSP PROCESSING HERE                            ?
????????????????????????????????????????????????????????
               ? Lock-Free Ring Buffer
               ?
????????????????????????????????????????????????????????
? DSP THREAD (Normal Priority - Background)           ?
?  - Reads from Ring Buffer                            ?
?  - Runs DSP Pipeline:                                ?
?    1. Pre-Filters (HP/LP)                            ?
?    2. Crossover (5-band split)                       ?
?    3. Per-Band Processing                            ?
?    4. Band Mixer                                     ?
?    5. Post-Filters (HP/LP)                           ?
?    6. FFT for spectrum                               ?
?  - Writes processed audio to Output Ring Buffer      ?
?  - Writes spectrum data to Visualization Buffer      ?
????????????????????????????????????????????????????????
               ? Triple Buffer
               ?
????????????????????????????????????????????????????????
? UI THREAD (Normal Priority - 30-60 FPS)             ?
?  - Reads spectrum data                               ?
?  - Draws spectrum analyzer                           ?
?  - Updates volume meters                             ?
?  - Handles user input                                ?
????????????????????????????????????????????????????????
```

---

## ?? Phase 2.1: DSP Foundation (Week 1-2)

### Task 2.1.1: Create Core DSP Infrastructure

#### Files to Create:

**1. `DSP\AudioBuffer.vb` - Buffer Management**
```vb
Namespace DSP

    ''' <summary>
    ''' Thread-safe audio buffer with sample rate and channel info
    ''' </summary>
    Public Class AudioBuffer
        Private _data() As Single  ' Float samples (-1.0 to 1.0)
        Private _sampleRate As Integer
        Private _channels As Integer
        Private _sampleCount As Integer
        
        Public Sub New(sampleRate As Integer, channels As Integer, sampleCount As Integer)
            _sampleRate = sampleRate
            _channels = channels
            _sampleCount = sampleCount
            ReDim _data(sampleCount * channels - 1)
        End Sub
        
        ''' <summary>Get/Set audio data as float array</summary>
        Public Property Data As Single()
            Get
                Return _data
            End Get
            Set(value As Single())
                _data = value
            End Set
        End Property
        
        Public ReadOnly Property SampleRate As Integer
            Get
                Return _sampleRate
            End Get
        End Property
        
        Public ReadOnly Property Channels As Integer
            Get
                Return _channels
            End Get
        End Property
        
        Public ReadOnly Property SampleCount As Integer
            Get
                Return _sampleCount
            End Get
        End Property
        
        ''' <summary>Get left channel sample</summary>
        Public Function GetLeft(index As Integer) As Single
            Return _data(index * _channels)
        End Function
        
        ''' <summary>Get right channel sample (or left if mono)</summary>
        Public Function GetRight(index As Integer) As Single
            If _channels = 1 Then Return _data(index)
            Return _data(index * _channels + 1)
        End Function
        
        ''' <summary>Set left channel sample</summary>
        Public Sub SetLeft(index As Integer, value As Single)
            _data(index * _channels) = value
        End Sub
        
        ''' <summary>Set right channel sample</summary>
        Public Sub SetRight(index As Integer, value As Single)
            If _channels = 1 Then
                _data(index) = value
            Else
                _data(index * _channels + 1) = value
            End If
        End Sub
        
        ''' <summary>Clear buffer (set to silence)</summary>
        Public Sub Clear()
            Array.Clear(_data, 0, _data.Length)
        End Sub
        
        ''' <summary>Copy from byte array (16-bit PCM)</summary>
        Public Sub CopyFrom16BitPCM(pcmData() As Byte, offset As Integer, count As Integer)
            Dim sampleIdx = 0
            For i = offset To offset + count - 1 Step 2
                Dim sample = BitConverter.ToInt16(pcmData, i)
                _data(sampleIdx) = sample / 32768.0F
                sampleIdx += 1
            Next
        End Sub
        
        ''' <summary>Copy to byte array (16-bit PCM)</summary>
        Public Sub CopyTo16BitPCM(pcmData() As Byte, offset As Integer)
            Dim byteIdx = offset
            For i = 0 To _data.Length - 1
                Dim sample = Math.Max(-1.0F, Math.Min(1.0F, _data(i)))
                Dim pcmValue = CShort(sample * 32767.0F)
                Dim bytes = BitConverter.GetBytes(pcmValue)
                pcmData(byteIdx) = bytes(0)
                pcmData(byteIdx + 1) = bytes(1)
                byteIdx += 2
            Next
        End Sub
    End Class

End Namespace
```

---

**2. `DSP\ProcessorChain.vb` - Sequential DSP Pipeline**
```vb
Namespace DSP

    ''' <summary>
    ''' Manages a chain of DSP processors executed in sequence
    ''' </summary>
    Public Class ProcessorChain
        Implements IProcessor
        
        Private processors As New List(Of IProcessor)
        Private _bypassed As Boolean = False
        
        ''' <summary>Add processor to end of chain</summary>
        Public Sub AddProcessor(processor As IProcessor)
            processors.Add(processor)
        End Sub
        
        ''' <summary>Insert processor at specific position</summary>
        Public Sub InsertProcessor(index As Integer, processor As IProcessor)
            processors.Insert(index, processor)
        End Sub
        
        ''' <summary>Remove processor from chain</summary>
        Public Sub RemoveProcessor(processor As IProcessor)
            processors.Remove(processor)
        End Sub
        
        ''' <summary>Get processor at index</summary>
        Public Function GetProcessor(index As Integer) As IProcessor
            Return processors(index)
        End Function
        
        ''' <summary>Get number of processors</summary>
        Public ReadOnly Property Count As Integer
            Get
                Return processors.Count
            End Get
        End Property
        
        ''' <summary>Process audio through entire chain</summary>
        Public Sub Process(buffer As AudioBuffer) Implements IProcessor.Process
            If _bypassed Then Return
            
            For Each processor In processors
                If Not processor.Bypassed Then
                    processor.Process(buffer)
                End If
            Next
        End Sub
        
        ''' <summary>Total latency of all processors</summary>
        Public ReadOnly Property Latency As Integer Implements IProcessor.Latency
            Get
                Dim total = 0
                For Each processor In processors
                    total += processor.Latency
                Next
                Return total
            End Get
        End Property
        
        ''' <summary>Bypass entire chain</summary>
        Public Property Bypassed As Boolean Implements IProcessor.Bypassed
            Get
                Return _bypassed
            End Get
            Set(value As Boolean)
                _bypassed = value
            End Set
        End Property
        
        ''' <summary>Reset all processors</summary>
        Public Sub Reset() Implements IProcessor.Reset
            For Each processor In processors
                processor.Reset()
            Next
        End Sub
        
        Public ReadOnly Property Name As String = "Processor Chain" Implements IProcessor.Name
        
    End Class

End Namespace
```

---

**3. `DSP\ProcessorBase.vb` - Base Class for DSP Modules**
```vb
Namespace DSP

    ''' <summary>
    ''' Abstract base class for DSP processors
    ''' </summary>
    Public MustInherit Class ProcessorBase
        Implements IProcessor
        
        Protected _bypassed As Boolean = False
        Protected _sampleRate As Integer = 44100
        Protected _name As String = "Processor"
        
        Public Sub New(sampleRate As Integer, name As String)
            _sampleRate = sampleRate
            _name = name
        End Sub
        
        ''' <summary>Process audio buffer (must be implemented by derived class)</summary>
        Public MustOverride Sub Process(buffer As AudioBuffer) Implements IProcessor.Process
        
        ''' <summary>Get processor latency in samples</summary>
        Public MustOverride ReadOnly Property Latency As Integer Implements IProcessor.Latency
        
        ''' <summary>Bypass processing</summary>
        Public Property Bypassed As Boolean Implements IProcessor.Bypassed
            Get
                Return _bypassed
            End Get
            Set(value As Boolean)
                _bypassed = value
            End Set
        End Property
        
        ''' <summary>Reset processor state</summary>
        Public MustOverride Sub Reset() Implements IProcessor.Reset
        
        ''' <summary>Processor name for display</summary>
        Public ReadOnly Property Name As String Implements IProcessor.Name
            Get
                Return _name
            End Get
        End Property
        
        ''' <summary>Current sample rate</summary>
        Protected ReadOnly Property SampleRate As Integer
            Get
                Return _sampleRate
            End Get
        End Property
        
    End Class

End Namespace
```

---

### Task 2.1.2: Create Lock-Free Ring Buffer

**File:** `Utils\RingBuffer.vb`
```vb
Imports System.Threading

Namespace Utils

    ''' <summary>
    ''' Lock-free single-producer single-consumer ring buffer for audio
    ''' </summary>
    Public Class RingBuffer
        Private buffer() As Byte
        Private _size As Integer
        Private _writePos As Integer = 0
        Private _readPos As Integer = 0
        Private _available As Integer = 0
        
        Public Sub New(sizeInBytes As Integer)
            ' Round up to power of 2 for efficient modulo
            _size = RoundUpToPowerOfTwo(sizeInBytes)
            ReDim buffer(_size - 1)
        End Sub
        
        ''' <summary>Write data to buffer (producer)</summary>
        Public Function Write(data() As Byte, offset As Integer, count As Integer) As Integer
            Dim free = _size - Interlocked.CompareExchange(_available, 0, 0)
            Dim toWrite = Math.Min(count, free)
            
            If toWrite <= 0 Then Return 0
            
            Dim writePos = _writePos
            
            ' Copy data (may wrap around)
            If writePos + toWrite <= _size Then
                ' Simple copy
                System.Buffer.BlockCopy(data, offset, buffer, writePos, toWrite)
            Else
                ' Wrap around
                Dim firstPart = _size - writePos
                Dim secondPart = toWrite - firstPart
                System.Buffer.BlockCopy(data, offset, buffer, writePos, firstPart)
                System.Buffer.BlockCopy(data, offset + firstPart, buffer, 0, secondPart)
            End If
            
            ' Update write position (atomic)
            Interlocked.Exchange(_writePos, (writePos + toWrite) And (_size - 1))
            Interlocked.Add(_available, toWrite)
            
            Return toWrite
        End Function
        
        ''' <summary>Read data from buffer (consumer)</summary>
        Public Function Read(data() As Byte, offset As Integer, count As Integer) As Integer
            Dim available = Interlocked.CompareExchange(_available, 0, 0)
            Dim toRead = Math.Min(count, available)
            
            If toRead <= 0 Then Return 0
            
            Dim readPos = _readPos
            
            ' Copy data (may wrap around)
            If readPos + toRead <= _size Then
                ' Simple copy
                System.Buffer.BlockCopy(buffer, readPos, data, offset, toRead)
            Else
                ' Wrap around
                Dim firstPart = _size - readPos
                Dim secondPart = toRead - firstPart
                System.Buffer.BlockCopy(buffer, readPos, data, offset, firstPart)
                System.Buffer.BlockCopy(buffer, 0, data, offset + firstPart, secondPart)
            End If
            
            ' Update read position (atomic)
            Interlocked.Exchange(_readPos, (readPos + toRead) And (_size - 1))
            Interlocked.Add(_available, -toRead)
            
            Return toRead
        End Function
        
        ''' <summary>Get available bytes for reading</summary>
        Public ReadOnly Property AvailableRead As Integer
            Get
                Return Interlocked.CompareExchange(_available, 0, 0)
            End Get
        End Property
        
        ''' <summary>Get available space for writing</summary>
        Public ReadOnly Property AvailableWrite As Integer
            Get
                Return _size - Interlocked.CompareExchange(_available, 0, 0)
            End Get
        End Property
        
        ''' <summary>Clear buffer</summary>
        Public Sub Clear()
            Interlocked.Exchange(_writePos, 0)
            Interlocked.Exchange(_readPos, 0)
            Interlocked.Exchange(_available, 0)
        End Sub
        
        Private Function RoundUpToPowerOfTwo(value As Integer) As Integer
            value -= 1
            value = value Or (value >> 1)
            value = value Or (value >> 2)
            value = value Or (value >> 4)
            value = value Or (value >> 8)
            value = value Or (value >> 16)
            Return value + 1
        End Function
        
    End Class

End Namespace
```

---

### Task 2.1.3: Create DSP Thread Manager

**File:** `DSP\DSPThread.vb`
```vb
Imports System.Threading
Imports DSP_Processor.Utils

Namespace DSP

    ''' <summary>
    ''' Manages background DSP processing thread
    ''' </summary>
    Public Class DSPThread
        Implements IDisposable
        
        Private thread As Thread
        Private running As Boolean = False
        Private inputRingBuffer As RingBuffer
        Private outputRingBuffer As RingBuffer
        Private processorChain As ProcessorChain
        Private sampleRate As Integer
        Private channels As Integer
        Private bufferSize As Integer = 2048
        
        Public Sub New(sampleRate As Integer, channels As Integer)
            Me.sampleRate = sampleRate
            Me.channels = channels
            
            ' Create ring buffers (1 second capacity)
            Dim bytesPerSecond = sampleRate * channels * 2 ' 16-bit
            inputRingBuffer = New RingBuffer(bytesPerSecond)
            outputRingBuffer = New RingBuffer(bytesPerSecond)
            
            ' Create processor chain
            processorChain = New ProcessorChain()
        End Sub
        
        ''' <summary>Access to processor chain for adding processors</summary>
        Public ReadOnly Property Chain As ProcessorChain
            Get
                Return processorChain
            End Get
        End Property
        
        ''' <summary>Start DSP processing thread</summary>
        Public Sub Start()
            If running Then Return
            
            running = True
            thread = New Thread(AddressOf ProcessLoop)
            thread.Name = "DSP Processing Thread"
            thread.Priority = ThreadPriority.Normal
            thread.IsBackground = True
            thread.Start()
            
            Logger.Instance.Info("DSP thread started", "DSPThread")
        End Sub
        
        ''' <summary>Stop DSP processing thread</summary>
        Public Sub [Stop]()
            If Not running Then Return
            
            running = False
            thread?.Join(1000) ' Wait up to 1 second
            
            Logger.Instance.Info("DSP thread stopped", "DSPThread")
        End Sub
        
        ''' <summary>Write audio to DSP input (from audio thread)</summary>
        Public Function WriteInput(data() As Byte, offset As Integer, count As Integer) As Integer
            Return inputRingBuffer.Write(data, offset, count)
        End Function
        
        ''' <summary>Read processed audio from DSP output (to playback)</summary>
        Public Function ReadOutput(data() As Byte, offset As Integer, count As Integer) As Integer
            Return outputRingBuffer.Read(data, offset, count)
        End Function
        
        ''' <summary>Main DSP processing loop</summary>
        Private Sub ProcessLoop()
            Dim pcmBuffer(bufferSize * channels * 2 - 1) As Byte
            Dim audioBuffer As New AudioBuffer(sampleRate, channels, bufferSize)
            
            While running
                ' Read from input ring buffer
                Dim bytesRead = inputRingBuffer.Read(pcmBuffer, 0, pcmBuffer.Length)
                
                If bytesRead > 0 Then
                    ' Convert PCM to float
                    audioBuffer.CopyFrom16BitPCM(pcmBuffer, 0, bytesRead)
                    
                    ' Process through chain
                    Try
                        processorChain.Process(audioBuffer)
                    Catch ex As Exception
                        Logger.Instance.Error("DSP processing error", ex, "DSPThread")
                    End Try
                    
                    ' Convert float to PCM
                    audioBuffer.CopyTo16BitPCM(pcmBuffer, 0)
                    
                    ' Write to output ring buffer
                    outputRingBuffer.Write(pcmBuffer, 0, bytesRead)
                Else
                    ' No data available, sleep briefly
                    Thread.Sleep(1)
                End If
            End While
        End Sub
        
        Public Sub Dispose() Implements IDisposable.Dispose
            [Stop]()
        End Sub
        
    End Class

End Namespace
```

---

## ??? Phase 2.2: Biquad Filters (Week 3)

### Task 2.2.1: Create Biquad Filter Core

**File:** `DSP\Filters\BiquadFilter.vb`
```vb
Namespace DSP.Filters

    ''' <summary>
    ''' Biquad filter (2-pole, 2-zero IIR filter)
    ''' Supports: LPF, HPF, BPF, Notch, Peaking, Low/High Shelf, All-pass
    ''' </summary>
    Public Class BiquadFilter
        Inherits ProcessorBase
        
        ' Filter coefficients
        Private a0, a1, a2, b1, b2 As Double
        
        ' Filter state (per channel)
        Private x1, x2, y1, y2 As Double ' Mono or left channel
        Private x1r, x2r, y1r, y2r As Double ' Right channel (stereo)
        
        Private _type As FilterType
        Private _frequency As Double
        Private _q As Double
        Private _gain As Double ' For peaking/shelf filters
        
        Public Sub New(sampleRate As Integer, type As FilterType, frequency As Double, q As Double, Optional gain As Double = 0.0)
            MyBase.New(sampleRate, "Biquad Filter")
            
            _type = type
            _frequency = frequency
            _q = q
            _gain = gain
            
            CalculateCoefficients()
        End Sub
        
        ''' <summary>Filter type</summary>
        Public Property FilterType As FilterType
            Get
                Return _type
            End Get
            Set(value As FilterType)
                _type = value
                CalculateCoefficients()
            End Set
        End Property
        
        ''' <summary>Cutoff/center frequency (Hz)</summary>
        Public Property Frequency As Double
            Get
                Return _frequency
            End Get
            Set(value As Double)
                _frequency = value
                CalculateCoefficients()
            End Set
        End Property
        
        ''' <summary>Q factor (resonance)</summary>
        Public Property Q As Double
            Get
                Return _q
            End Get
            Set(value As Double)
                _q = value
                CalculateCoefficients()
            End Set
        End Property
        
        ''' <summary>Gain in dB (for peaking/shelf filters)</summary>
        Public Property Gain As Double
            Get
                Return _gain
            End Get
            Set(value As Double)
                _gain = value
                CalculateCoefficients()
            End Set
        End Property
        
        ''' <summary>Calculate filter coefficients using Audio EQ Cookbook formulas</summary>
        Private Sub CalculateCoefficients()
            Dim w0 = 2.0 * Math.PI * _frequency / _sampleRate
            Dim cosW0 = Math.Cos(w0)
            Dim sinW0 = Math.Sin(w0)
            Dim alpha = sinW0 / (2.0 * _q)
            Dim A = Math.Pow(10.0, _gain / 40.0) ' For peaking/shelf
            
            Dim b0, a1Temp, a2Temp As Double
            
            Select Case _type
                Case Filters.FilterType.LowPass
                    b0 = (1.0 - cosW0) / 2.0
                    Dim b1Temp = 1.0 - cosW0
                    b2 = (1.0 - cosW0) / 2.0
                    a0 = 1.0 + alpha
                    a1Temp = -2.0 * cosW0
                    a2Temp = 1.0 - alpha
                    
                    ' Normalize
                    a1 = a1Temp / a0
                    a2 = a2Temp / a0
                    a0 = b0 / a0
                    a1 = b1Temp / a0
                    a2 = b2 / a0
                    
                Case Filters.FilterType.HighPass
                    b0 = (1.0 + cosW0) / 2.0
                    Dim b1Temp = -(1.0 + cosW0)
                    b2 = (1.0 + cosW0) / 2.0
                    a0 = 1.0 + alpha
                    a1Temp = -2.0 * cosW0
                    a2Temp = 1.0 - alpha
                    
                    ' Normalize
                    a1 = a1Temp / a0
                    a2 = a2Temp / a0
                    a0 = b0 / a0
                    a1 = b1Temp / a0
                    a2 = b2 / a0
                    
                ' ... Implement other filter types (BandPass, Notch, Peaking, etc.)
                
            End Select
        End Sub
        
        ''' <summary>Process audio buffer</summary>
        Public Overrides Sub Process(buffer As AudioBuffer)
            If _bypassed Then Return
            
            For i = 0 To buffer.SampleCount - 1
                ' Left channel
                Dim xL = buffer.GetLeft(i)
                Dim yL = a0 * xL + a1 * x1 + a2 * x2 - b1 * y1 - b2 * y2
                
                ' Update state
                x2 = x1
                x1 = xL
                y2 = y1
                y1 = yL
                
                buffer.SetLeft(i, CSng(yL))
                
                ' Right channel (if stereo)
                If buffer.Channels = 2 Then
                    Dim xR = buffer.GetRight(i)
                    Dim yR = a0 * xR + a1 * x1r + a2 * x2r - b1 * y1r - b2 * y2r
                    
                    x2r = x1r
                    x1r = xR
                    y2r = y1r
                    y1r = yR
                    
                    buffer.SetRight(i, CSng(yR))
                End If
            Next
        End Sub
        
        Public Overrides Sub Reset()
            x1 = 0 : x2 = 0 : y1 = 0 : y2 = 0
            x1r = 0 : x2r = 0 : y1r = 0 : y2r = 0
        End Sub
        
        Public Overrides ReadOnly Property Latency As Integer
            Get
                Return 2 ' 2-sample latency (2-pole filter)
            End Get
        End Property
        
    End Class
    
    ''' <summary>Filter types</summary>
    Public Enum FilterType
        LowPass
        HighPass
        BandPass
        Notch
        Peaking
        LowShelf
        HighShelf
        AllPass
    End Enum

End Namespace
```

---

## ?? Implementation Pattern Summary

### For Each DSP Module:
1. **Inherit from `ProcessorBase`**
2. **Implement `Process(buffer As AudioBuffer)`**
3. **Implement `Reset()`**
4. **Return `Latency` property**
5. **Add to `ProcessorChain`**

### Performance Guidelines:
- Target: <20ms processing time per 2048-sample buffer
- Use `Double` for filter coefficients (precision)
- Use `Single` for audio samples (speed)
- Pre-calculate coefficients in property setters
- Avoid allocations in `Process()` loop
- Use array indexing, not LINQ
- Profile with `PerformanceMonitor`

---

## ?? Success Metrics

### Phase 2 Complete When:
- ? DSP thread running independently
- ? Lock-free ring buffers working
- ? Processor chain executes in order
- ? Biquad filters working (LPF, HPF)
- ? Audio passes through DSP with <20ms latency
- ? No glitches or dropouts
- ? CPU usage <40% on mid-range system

---

**Ready to implement future DSP phases!** ??

**END OF PHASE 2+ IMPLEMENTATION GUIDE**
