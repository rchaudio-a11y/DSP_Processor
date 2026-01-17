# Task: Implement TapPointManager + State Machine Architecture

**Task ID:** v1.3.2.0-001  
**Created:** 2026-01-16  
**Updated:** 2026-01-16 (Added State Machine)  
**Status:** Planning  
**Priority:** HIGH  
**Version:** v1.3.2.0  
**RDF Phase:** Phase 2 (Insight Bloom) ? Phase 3 (Build)  
**Estimated Time:** 10-12 hours

---

## ?? PRIORITY ORDER (Why This Sequence)

This task plan integrates **TWO** major architectural improvements in order of impact:

### **1. TapPointManager (Phases 1-3)** - FIXES USER-VISIBLE BUGS
**Why First:** Users can't see slider changes - HIGH IMPACT BUG  
**Time:** 5-6 hours  
**Deliverables:**
- Meters show DSP-processed audio
- Gain/pan sliders provide feedback
- Stereo L/R imaging visible

### **2. State Machine (Phase 4)** - PREVENTS FUTURE BUGS
**Why Second:** Prevents crashes and invalid states - STABILITY  
**Time:** 3-4 hours  
**Deliverables:**
- Centralized state control
- Invalid transitions prevented
- Predictable application behavior

### **3. Testing + Documentation (Phases 5-6)** - VERIFICATION
**Why Last:** Validates both systems work together  
**Time:** 3-4 hours

**Rationale:** Fix user-facing bugs FIRST (meters), then add stability layer (state machine), then verify and document everything together. RDF Phase 6 synthesis happens at end.

---

## ?? Goal

Implement centralized TapPointManager to provide type-safe, managed access to DSP tap point buffers, replacing direct Friend field access with a clean API.

---

## ?? Problem Statement

**Current State (v1.3.1.3):**
- Tap points accessed as direct `Friend` fields on DSPThread
- No type safety (string-based reader IDs)
- No lifecycle management (manual reader creation/destruction)
- Tight coupling to DSPThread internals
- No discoverability (must inspect code to find available taps)

**Desired State (v1.3.2.0):**
- Centralized TapPointManager with clean API
- Type-safe enum for tap locations (`TapPoint.PostInputGain`)
- Automatic reader lifecycle management
- Single source of truth for available tap points
- Intellisense support for tap point discovery

**Benefits:**
- ? Cleaner API for meters, FFT, and future consumers
- ? Easier to add new tap points (just add to enum)
- ? Type-safe (compile-time checking)
- ? Self-documenting (enum lists all taps)
- ? Better resource management (no reader leaks)

---

## ??? Architecture Overview

### **Components:**

```
???????????????????????????????????????????????????????????????
? TapPoint (Enum)                                             ?
? - PreDSP, PostInputGain, PostOutputGain, PreOutput         ?
???????????????????????????????????????????????????????????????
                              ?
???????????????????????????????????????????????????????????????
? TapPointManager (Class)                                     ?
? - CreateReader(TapPoint, name)                              ?
? - Read(readerId, buffer)                                    ?
? - Available(readerId)                                       ?
? - DestroyReader(readerId)                                   ?
? - Dispose()                                                 ?
???????????????????????????????????????????????????????????????
                              ?
???????????????????????????????????????????????????????????????
? TapPointReader (Private Class)                              ?
? - Name, TapLocation, Buffer, RingBufferReaderId            ?
???????????????????????????????????????????????????????????????
                              ?
???????????????????????????????????????????????????????????????
? DSPThread.MultiReaderRingBuffer                             ?
? (existing infrastructure)                                   ?
???????????????????????????????????????????????????????????????
```

### **Signal Flow:**

```
Mic Input
    ?
DSPThread.Process()
    ?
InputGainProcessor.ProcessInternal()
    ? SendToMonitor()
    ? [TapPoint.PostInputGain]
    ?
OutputGainProcessor.ProcessInternal()
    ? SendToMonitor()
    ? [TapPoint.PostOutputGain]
    ?
Output Buffer
    
Consumers:
    tapManager.CreateReader(TapPoint.PostInputGain, "InputMeter")
    tapManager.Read("InputMeter", buffer)
```

---

## ?? Implementation Plan

### **Phase 1: Core Infrastructure (2-3 hours)**

#### **Task 1.1: Create TapPoint Enum**
**File:** `DSP/TapPoint.vb` (new file)  
**Time:** 15 minutes  
**Priority:** HIGH

**Implementation:**
```visualbasic
Namespace DSP

    ''' <summary>
    ''' Identifies available DSP tap points in the audio signal chain.
    ''' Each tap point represents a location where processed audio can be monitored.
    ''' </summary>
    Public Enum TapPoint
        ''' <summary>Raw audio before any DSP processing</summary>
        PreDSP = 0
        
        ''' <summary>After InputGainProcessor (first gain stage)</summary>
        PostInputGain = 1
        
        ''' <summary>After OutputGainProcessor (final gain stage)</summary>
        PostOutputGain = 2
        
        ''' <summary>Before final output (currently same as PostOutputGain)</summary>
        PreOutput = 3
    End Enum

End Namespace
```

**Validation:**
- [ ] Build succeeds
- [ ] Enum visible in intellisense
- [ ] XML comments display correctly

---

#### **Task 1.2: Create TapPointReader (Private Class)**
**File:** `DSP/TapPointManager.vb` (new file)  
**Time:** 20 minutes  
**Priority:** HIGH

**Implementation:**
```visualbasic
Namespace DSP

    ''' <summary>
    ''' Internal state for a tap point reader.
    ''' Tracks which buffer the reader is attached to and its unique ID.
    ''' </summary>
    Friend Class TapPointReader
        ''' <summary>User-friendly name for this reader (e.g., "InputMeter")</summary>
        Public Property Name As String
        
        ''' <summary>Which tap point this reader is monitoring</summary>
        Public Property TapLocation As TapPoint
        
        ''' <summary>Reference to the multi-reader ring buffer</summary>
        Public Property Buffer As Utils.MultiReaderRingBuffer
        
        ''' <summary>Ring buffer's internal reader ID (returned by CreateReader)</summary>
        Public Property RingBufferReaderId As String
    End Class

End Namespace
```

**Validation:**
- [ ] Class is Friend (not Public)
- [ ] Properties compile correctly

---

#### **Task 1.3: Create TapPointManager Class (Core)**
**File:** `DSP/TapPointManager.vb`  
**Time:** 1.5 hours  
**Priority:** HIGH

**Implementation:**
```visualbasic
Imports System.Collections.Generic

Namespace DSP

    ''' <summary>
    ''' Manages tap point reader lifecycle and provides unified API for accessing
    ''' DSP signal chain monitoring buffers. Thread-safe for multiple readers.
    ''' </summary>
    Public Class TapPointManager
        Implements IDisposable

#Region "Private Fields"

        Private ReadOnly dspThread As DSPThread
        Private ReadOnly readers As Dictionary(Of String, TapPointReader)
        Private ReadOnly readerLock As New Object()
        Private disposed As Boolean = False

#End Region

#Region "Constructor"

        ''' <summary>
        ''' Create a new tap point manager for a DSP thread
        ''' </summary>
        ''' <param name="thread">DSPThread to monitor</param>
        Public Sub New(thread As DSPThread)
            If thread Is Nothing Then
                Throw New ArgumentNullException(NameOf(thread))
            End If
            
            dspThread = thread
            readers = New Dictionary(Of String, TapPointReader)()
        End Sub

#End Region

#Region "Public API"

        ''' <summary>
        ''' Create a named reader for a specific tap point.
        ''' Each reader maintains independent read position in the ring buffer.
        ''' </summary>
        ''' <param name="tap">Which tap point to read from</param>
        ''' <param name="readerName">Unique name for this reader (e.g., "InputMeter")</param>
        ''' <returns>Reader ID (same as readerName)</returns>
        ''' <exception cref="ArgumentException">If reader name already exists</exception>
        Public Function CreateReader(tap As TapPoint, readerName As String) As String
            If String.IsNullOrWhiteSpace(readerName) Then
                Throw New ArgumentException("Reader name cannot be empty", NameOf(readerName))
            End If
            
            SyncLock readerLock
                If readers.ContainsKey(readerName) Then
                    Throw New ArgumentException($"Reader '{readerName}' already exists")
                End If
                
                ' Get appropriate buffer based on tap point
                Dim buffer = GetBufferForTap(tap)
                If buffer Is Nothing Then
                    Throw New InvalidOperationException($"Buffer for tap point {tap} not available")
                End If
                
                ' Create reader in ring buffer
                Dim ringReaderId = buffer.CreateReader(readerName)
                
                ' Store reader state
                Dim reader As New TapPointReader With {
                    .Name = readerName,
                    .TapLocation = tap,
                    .Buffer = buffer,
                    .RingBufferReaderId = ringReaderId
                }
                
                readers.Add(readerName, reader)
                Utils.Logger.Instance.Info($"Created tap point reader '{readerName}' for {tap}", "TapPointManager")
                
                Return readerName
            End SyncLock
        End Function
        
        ''' <summary>
        ''' Read from a previously created reader.
        ''' Non-blocking - returns immediately with available data.
        ''' </summary>
        ''' <param name="readerId">Reader ID returned by CreateReader</param>
        ''' <param name="buffer">Destination buffer</param>
        ''' <param name="offset">Offset in destination buffer</param>
        ''' <param name="count">Maximum bytes to read</param>
        ''' <returns>Actual bytes read (may be less than count)</returns>
        ''' <exception cref="ArgumentException">If reader ID not found</exception>
        Public Function Read(readerId As String, buffer As Byte(), offset As Integer, count As Integer) As Integer
            ThrowIfDisposed()
            
            SyncLock readerLock
                If Not readers.ContainsKey(readerId) Then
                    Throw New ArgumentException($"Reader '{readerId}' not found. Did you call CreateReader?")
                End If
                
                Dim reader = readers(readerId)
                Return reader.Buffer.Read(reader.RingBufferReaderId, buffer, offset, count)
            End SyncLock
        End Function
        
        ''' <summary>
        ''' Check how many bytes are available without reading.
        ''' Useful for determining buffer size before reading.
        ''' </summary>
        ''' <param name="readerId">Reader ID</param>
        ''' <returns>Number of bytes available to read</returns>
        Public Function Available(readerId As String) As Integer
            ThrowIfDisposed()
            
            SyncLock readerLock
                If Not readers.ContainsKey(readerId) Then
                    Throw New ArgumentException($"Reader '{readerId}' not found")
                End If
                
                Dim reader = readers(readerId)
                ' MultiReaderRingBuffer doesn't expose Available()
                ' For now, return 0 and rely on non-blocking Read
                ' TODO: Add Available() to MultiReaderRingBuffer
                Return 0
            End SyncLock
        End Function
        
        ''' <summary>
        ''' Destroy a reader when no longer needed.
        ''' Frees resources and removes reader from ring buffer.
        ''' </summary>
        ''' <param name="readerId">Reader ID to destroy</param>
        Public Sub DestroyReader(readerId As String)
            SyncLock readerLock
                If readers.ContainsKey(readerId) Then
                    Dim reader = readers(readerId)
                    reader.Buffer.DestroyReader(reader.RingBufferReaderId)
                    readers.Remove(readerId)
                    Utils.Logger.Instance.Info($"Destroyed tap point reader '{readerId}'", "TapPointManager")
                End If
            End SyncLock
        End Sub
        
        ''' <summary>
        ''' Get list of active reader names
        ''' </summary>
        Public Function GetActiveReaders() As String()
            SyncLock readerLock
                Return readers.Keys.ToArray()
            End SyncLock
        End Function

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' Get the appropriate ring buffer for a tap point.
        ''' Maps enum to actual buffer fields on DSPThread.
        ''' </summary>
        Private Function GetBufferForTap(tap As TapPoint) As Utils.MultiReaderRingBuffer
            Select Case tap
                Case TapPoint.PreDSP
                    Return dspThread.inputMonitorBuffer
                    
                Case TapPoint.PostInputGain
                    Return dspThread.postGainMonitorBuffer
                    
                Case TapPoint.PostOutputGain
                    Return dspThread.postOutputGainMonitorBuffer
                    
                Case TapPoint.PreOutput
                    Return dspThread.outputMonitorBuffer
                    
                Case Else
                    Utils.Logger.Instance.Error($"Unknown tap point: {tap}", Nothing, "TapPointManager")
                    Return Nothing
            End Select
        End Function
        
        Private Sub ThrowIfDisposed()
            If disposed Then
                Throw New ObjectDisposedException("TapPointManager")
            End If
        End Sub

#End Region

#Region "IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
        
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposed Then
                If disposing Then
                    ' Clean up all readers
                    SyncLock readerLock
                        For Each reader In readers.Values
                            Try
                                reader.Buffer.DestroyReader(reader.RingBufferReaderId)
                            Catch ex As Exception
                                Utils.Logger.Instance.Warning($"Failed to destroy reader '{reader.Name}': {ex.Message}", "TapPointManager")
                            End Try
                        Next
                        readers.Clear()
                    End SyncLock
                    
                    Utils.Logger.Instance.Info("TapPointManager disposed", "TapPointManager")
                End If
                
                disposed = True
            End If
        End Sub

#End Region

    End Class

End Namespace
```

**Validation:**
- [ ] Build succeeds
- [ ] Thread-safe (multiple readers)
- [ ] Proper disposal cleanup
- [ ] Logger integration works

---

### **Phase 2: DSPThread Integration (1 hour)**

#### **Task 2.1: Expose Monitor Buffers as Public**
**File:** `DSP/DSPThread.vb`  
**Time:** 15 minutes  
**Priority:** HIGH  
**Depends On:** Task 1.3

**Changes:**
```visualbasic
' OLD (line 20-23):
Private ReadOnly inputMonitorBuffer As Utils.MultiReaderRingBuffer
Private ReadOnly outputMonitorBuffer As Utils.MultiReaderRingBuffer
Friend ReadOnly postGainMonitorBuffer As Utils.MultiReaderRingBuffer
Friend ReadOnly postOutputGainMonitorBuffer As Utils.MultiReaderRingBuffer

' NEW:
Friend ReadOnly inputMonitorBuffer As Utils.MultiReaderRingBuffer ' TapPointManager needs access
Friend ReadOnly outputMonitorBuffer As Utils.MultiReaderRingBuffer
Friend ReadOnly postGainMonitorBuffer As Utils.MultiReaderRingBuffer
Friend ReadOnly postOutputGainMonitorBuffer As Utils.MultiReaderRingBuffer
```

**Rationale:** TapPointManager needs Friend access to all buffers

**Validation:**
- [ ] Build succeeds
- [ ] No breaking changes to RecordingManager

---

#### **Task 2.2: Add TapPointManager to RecordingManager**
**File:** `Managers/RecordingManager.vb`  
**Time:** 30 minutes  
**Priority:** HIGH  
**Depends On:** Task 2.1

**Changes:**
```visualbasic
' Add field (around line 50):
Private tapPointManager As DSP.TapPointManager

' In ArmMicrophone, after DSPThread creation (around line 415):
If useDSP AndAlso dspThread IsNot Nothing Then
    ' Create tap point manager
    tapPointManager = New DSP.TapPointManager(dspThread)
    Logger.Instance.Info("TapPointManager created", "RecordingManager")
End If

' In DisarmMicrophone (cleanup):
If tapPointManager IsNot Nothing Then
    tapPointManager.Dispose()
    tapPointManager = Nothing
End If

' Add public property:
Public ReadOnly Property TapManager As DSP.TapPointManager
    Get
        Return tapPointManager
    End Get
End Property
```

**Validation:**
- [ ] TapPointManager created when DSP enabled
- [ ] Proper disposal when disarming
- [ ] No memory leaks

---

### **Phase 3: MainForm Integration (2 hours)**

#### **Task 3.1: Create Tap Point Readers in MainForm**
**File:** `MainForm.vb`  
**Time:** 45 minutes  
**Priority:** HIGH  
**Depends On:** Task 2.2

**Changes:**
```visualbasic
' Add field:
Private tapReaders As New List(Of String)() ' Track reader IDs for cleanup

' Add method (called after microphone armed):
Private Sub InitializeTapPointReaders()
    If recordingManager Is Nothing OrElse recordingManager.TapManager Is Nothing Then
        Logger.Instance.Warning("Cannot initialize tap readers - manager not available", "MainForm")
        Return
    End If
    
    Try
        ' Create readers for meters
        Dim inputReader = recordingManager.TapManager.CreateReader(
            DSP.TapPoint.PostInputGain, 
            "MainForm_InputMeters")
        tapReaders.Add(inputReader)
        
        Dim outputReader = recordingManager.TapManager.CreateReader(
            DSP.TapPoint.PostOutputGain, 
            "MainForm_OutputMeters")
        tapReaders.Add(outputReader)
        
        Logger.Instance.Info("Tap point readers initialized", "MainForm")
    Catch ex As Exception
        Logger.Instance.Error("Failed to create tap readers", ex, "MainForm")
    End Try
End Sub

' Call from OnMicrophoneArmed:
Private Sub OnMicrophoneArmed(sender As Object, isArmed As Boolean)
    If isArmed Then
        ' ... existing code ...
        
        ' Initialize tap point readers
        InitializeTapPointReaders()
    Else
        ' Cleanup tap readers
        CleanupTapPointReaders()
    End If
End Sub

' Add cleanup method:
Private Sub CleanupTapPointReaders()
    If recordingManager IsNot Nothing AndAlso recordingManager.TapManager IsNot Nothing Then
        For Each readerId In tapReaders
            Try
                recordingManager.TapManager.DestroyReader(readerId)
            Catch ex As Exception
                Logger.Instance.Warning($"Failed to destroy reader {readerId}", "MainForm")
            End Try
        Next
    End If
    tapReaders.Clear()
End Sub
```

**Validation:**
- [ ] Readers created successfully
- [ ] No exceptions on arm/disarm cycle
- [ ] Proper cleanup on disarm

---

#### **Task 3.2: Update OnRecordingBufferAvailable to Use Tap Points**
**File:** `MainForm.vb`  
**Time:** 1 hour  
**Priority:** HIGH  
**Depends On:** Task 3.1

**Changes:**
```visualbasic
Private Sub OnRecordingBufferAvailable(sender As Object, e As AudioBufferEventArgs)
    ' Route through AudioPipelineRouter (existing)
    pipelineRouter?.RouteAudioBuffer(
        e.Buffer,
        Audio.Routing.AudioSourceType.Microphone,
        e.BitsPerSample,
        e.Channels,
        e.SampleRate)

    ' NEW: Update meters using tap point processed audio
    Try
        If recordingManager IsNot Nothing AndAlso recordingManager.TapManager IsNot Nothing Then
            ' Read from INPUT tap (after InputGainProcessor)
            Dim inputBuffer(4095) As Byte
            Dim inputBytes = recordingManager.TapManager.Read("MainForm_InputMeters", inputBuffer, 0, 4096)
            
            ' Read from OUTPUT tap (after OutputGainProcessor)  
            Dim outputBuffer(4095) As Byte
            Dim outputBytes = recordingManager.TapManager.Read("MainForm_OutputMeters", outputBuffer, 0, 4096)
            
            ' Analyze INPUT levels (after InputGainProcessor)
            Dim inputLevels = AudioLevelMeter.AnalyzeSamples(inputBuffer, e.BitsPerSample, e.Channels)
            
            ' Analyze OUTPUT levels (after OutputGainProcessor)
            Dim outputLevels = AudioLevelMeter.AnalyzeSamples(outputBuffer, e.BitsPerSample, e.Channels)
            
            ' Update main recording meter (use output tap)
            meterRecording.SetLevel(outputLevels.PeakDB, outputLevels.RMSDB, outputLevels.IsClipping)
            
            ' Update DSP Signal Flow Panel meters
            ' TODO Task 3.3: Separate L/R channels
            DspSignalFlowPanel1.UpdateMeters(
                inputLevels.PeakDB,   ' Input Left (TODO: separate L/R)
                inputLevels.PeakDB,   ' Input Right
                outputLevels.PeakDB,  ' Output Left
                outputLevels.PeakDB)  ' Output Right
        Else
            ' Fallback to raw buffer if tap points not available
            Dim levelData = AudioLevelMeter.AnalyzeSamples(e.Buffer, e.BitsPerSample, e.Channels)
            meterRecording.SetLevel(levelData.PeakDB, levelData.RMSDB, levelData.IsClipping)
            
            DspSignalFlowPanel1.UpdateMeters(
                levelData.PeakDB, levelData.PeakDB, 
                levelData.PeakDB, levelData.PeakDB)
        End If
    Catch ex As Exception
        Logger.Instance.Error("Meter update failed", ex, "MainForm")
    End Try
End Sub
```

**Validation:**
- [ ] Meters update without crashes
- [ ] Input meters show different levels than output
- [ ] Gain changes visible in meters
- [ ] Fallback works if tap points unavailable

---

#### **Task 3.3: Implement Stereo L/R Channel Separation**
**File:** `Utils/AudioLevelMeter.vb` OR `MainForm.vb`  
**Time:** 30 minutes  
**Priority:** MEDIUM  
**Depends On:** Task 3.2

**Implementation:**
```visualbasic
' Add to AudioLevelMeter or MainForm as helper:
Public Shared Function AnalyzeStereoChannels(
    buffer As Byte(), 
    bitsPerSample As Integer, 
    byteCount As Integer
) As (leftPeakDb As Single, rightPeakDb As Single)
    
    If bitsPerSample <> 16 Then
        ' Fallback for non-16-bit
        Dim mono = AnalyzeSamples(buffer, bitsPerSample, 2)
        Return (mono.PeakDB, mono.PeakDB)
    End If
    
    Dim leftPeak As Single = 0
    Dim rightPeak As Single = 0
    Dim sampleCount = byteCount \ 4 ' 2 bytes per sample * 2 channels
    
    For i = 0 To sampleCount - 1
        Dim offset = i * 4
        
        ' Left channel (first 2 bytes)
        Dim leftSample = Math.Abs(BitConverter.ToInt16(buffer, offset))
        If leftSample > leftPeak Then leftPeak = leftSample
        
        ' Right channel (next 2 bytes)
        Dim rightSample = Math.Abs(BitConverter.ToInt16(buffer, offset + 2))
        If rightSample > rightPeak Then rightPeak = rightSample
    Next
    
    ' Convert to dB (same formula as AnalyzeSamples)
    Dim leftDb = If(leftPeak > 0, 20.0F * Math.Log10(leftPeak / 32768.0F), -96.0F)
    Dim rightDb = If(rightPeak > 0, 20.0F * Math.Log10(rightPeak / 32768.0F), -96.0F)
    
    Return (leftDb, rightDb)
End Function

' Update OnRecordingBufferAvailable to use it:
Dim (inputLeftDb, inputRightDb) = AnalyzeStereoChannels(inputBuffer, e.BitsPerSample, inputBytes)
Dim (outputLeftDb, outputRightDb) = AnalyzeStereoChannels(outputBuffer, e.BitsPerSample, outputBytes)

DspSignalFlowPanel1.UpdateMeters(
    inputLeftDb,   ' Input Left (now separate!)
    inputRightDb,  ' Input Right
    outputLeftDb,  ' Output Left
    outputRightDb) ' Output Right
```

**Validation:**
- [ ] Panning left shows high L, low R
- [ ] Panning right shows high R, low L
- [ ] Center pan shows equal L/R
- [ ] Meters accurately reflect stereo image

---

### **Phase 4: State Machine Architecture (3 hours)** ?? HIGH PRIORITY

**Why Now:** State management is critical for app stability. Implementing it alongside tap points ensures proper lifecycle control from the start.

#### **Task 4.0: Create ApplicationState Enum**
**File:** `State/ApplicationState.vb` (new file)  
**Time:** 20 minutes  
**Priority:** HIGH

**Implementation:**
```visualbasic
Namespace State

    ''' <summary>
    ''' Defines all possible application states.
    ''' State machine controls valid transitions between these states.
    ''' </summary>
    Public Enum ApplicationState
        ''' <summary>Initial state - no audio system initialized</summary>
        Uninitialized = 0
        
        ''' <summary>Audio system initialized but microphone not armed</summary>
        Idle = 1
        
        ''' <summary>Microphone armed and ready to record</summary>
        Armed = 2
        
        ''' <summary>Actively recording audio</summary>
        Recording = 3
        
        ''' <summary>Playing back recorded audio</summary>
        Playing = 4
        
        ''' <summary>Error state - requires reset</summary>
        [Error] = 99
    End Enum

    ''' <summary>
    ''' Raised when application state changes
    ''' </summary>
    Public Class StateChangedEventArgs
        Inherits EventArgs
        
        Public Property OldState As ApplicationState
        Public Property NewState As ApplicationState
        Public Property Reason As String
        
        Public Sub New(oldState As ApplicationState, newState As ApplicationState, Optional reason As String = "")
            Me.OldState = oldState
            Me.NewState = newState
            Me.Reason = reason
        End Sub
    End Class

End Namespace
```

**Validation:**
- [ ] Enum values don't conflict
- [ ] XML comments clear
- [ ] EventArgs compiles

---

#### **Task 4.1: Create StateManager Class**
**File:** `State/StateManager.vb` (new file)  
**Time:** 1.5 hours  
**Priority:** HIGH  
**Depends On:** Task 4.0

**Implementation:**
```visualbasic
Namespace State

    ''' <summary>
    ''' Centralized state machine for application.
    ''' Controls all state transitions and enforces valid state flow.
    ''' Thread-safe singleton pattern.
    ''' </summary>
    Public NotInheritable Class StateManager
        
#Region "Singleton"

        Private Shared ReadOnly _instance As New Lazy(Of StateManager)(Function() New StateManager())
        
        Public Shared ReadOnly Property Instance As StateManager
            Get
                Return _instance.Value
            End Get
        End Property
        
        Private Sub New()
            ' Private constructor for singleton
            _currentState = ApplicationState.Uninitialized
            _validTransitions = BuildTransitionTable()
        End Sub

#End Region

#Region "Fields"

        Private _currentState As ApplicationState
        Private ReadOnly _stateLock As New Object()
        Private ReadOnly _validTransitions As Dictionary(Of ApplicationState, HashSet(Of ApplicationState))

#End Region

#Region "Events"

        ''' <summary>Raised before state transition (can be cancelled)</summary>
        Public Event StateChanging As EventHandler(Of StateChangingEventArgs)
        
        ''' <summary>Raised after successful state transition</summary>
        Public Event StateChanged As EventHandler(Of StateChangedEventArgs)

#End Region

#Region "Properties"

        ''' <summary>Current application state (thread-safe read)</summary>
        Public ReadOnly Property CurrentState As ApplicationState
            Get
                SyncLock _stateLock
                    Return _currentState
                End SyncLock
            End Get
        End Property
        
        ''' <summary>Check if currently in a specific state</summary>
        Public ReadOnly Property IsState(state As ApplicationState) As Boolean
            Get
                Return CurrentState = state
            End Get
        End Property
        
        ''' <summary>Check if microphone is armed (Armed or Recording)</summary>
        Public ReadOnly Property IsMicrophoneArmed As Boolean
            Get
                Dim state = CurrentState
                Return state = ApplicationState.Armed OrElse state = ApplicationState.Recording
            End Get
        End Property

#End Region

#Region "State Transition"

        ''' <summary>
        ''' Attempt to transition to a new state.
        ''' Returns True if transition succeeded, False if invalid.
        ''' </summary>
        Public Function TransitionTo(newState As ApplicationState, Optional reason As String = "") As Boolean
            SyncLock _stateLock
                Dim oldState = _currentState
                
                ' Check if transition is valid
                If Not IsValidTransition(oldState, newState) Then
                    Utils.Logger.Instance.Warning($"Invalid state transition: {oldState} ? {newState}", "StateManager")
                    Return False
                End If
                
                ' Raise StateChanging event (can be cancelled)
                Dim changingArgs As New StateChangingEventArgs(oldState, newState, reason)
                RaiseEvent StateChanging(Me, changingArgs)
                
                If changingArgs.Cancel Then
                    Utils.Logger.Instance.Info($"State transition cancelled: {oldState} ? {newState}", "StateManager")
                    Return False
                End If
                
                ' Apply state change
                _currentState = newState
                Utils.Logger.Instance.Info($"State transition: {oldState} ? {newState} ({reason})", "StateManager")
                
                ' Raise StateChanged event
                RaiseEvent StateChanged(Me, New StateChangedEventArgs(oldState, newState, reason))
                
                Return True
            End SyncLock
        End Function
        
        ''' <summary>Check if transition is valid without executing it</summary>
        Public Function CanTransitionTo(newState As ApplicationState) As Boolean
            Return IsValidTransition(CurrentState, newState)
        End Function

#End Region

#Region "Validation"

        ''' <summary>
        ''' Build state transition table.
        ''' Defines all valid state transitions.
        ''' </summary>
        Private Function BuildTransitionTable() As Dictionary(Of ApplicationState, HashSet(Of ApplicationState))
            Dim table As New Dictionary(Of ApplicationState, HashSet(Of ApplicationState))
            
            ' Uninitialized ? Idle (startup)
            table(ApplicationState.Uninitialized) = New HashSet(Of ApplicationState) From {
                ApplicationState.Idle,
                ApplicationState.Error
            }
            
            ' Idle ? Armed (arm microphone)
            ' Idle ? Playing (play file)
            table(ApplicationState.Idle) = New HashSet(Of ApplicationState) From {
                ApplicationState.Armed,
                ApplicationState.Playing,
                ApplicationState.Error
            }
            
            ' Armed ? Recording (start recording)
            ' Armed ? Idle (disarm microphone)
            table(ApplicationState.Armed) = New HashSet(Of ApplicationState) From {
                ApplicationState.Recording,
                ApplicationState.Idle,
                ApplicationState.Error
            }
            
            ' Recording ? Armed (stop recording, mic still armed)
            ' Recording ? Idle (stop and disarm)
            table(ApplicationState.Recording) = New HashSet(Of ApplicationState) From {
                ApplicationState.Armed,
                ApplicationState.Idle,
                ApplicationState.Error
            }
            
            ' Playing ? Idle (stop playback)
            table(ApplicationState.Playing) = New HashSet(Of ApplicationState) From {
                ApplicationState.Idle,
                ApplicationState.Error
            }
            
            ' Error ? Idle (reset)
            table(ApplicationState.Error) = New HashSet(Of ApplicationState) From {
                ApplicationState.Idle
            }
            
            Return table
        End Function
        
        Private Function IsValidTransition(fromState As ApplicationState, toState As ApplicationState) As Boolean
            ' Same state is always valid (no-op)
            If fromState = toState Then Return True
            
            ' Check transition table
            If _validTransitions.ContainsKey(fromState) Then
                Return _validTransitions(fromState).Contains(toState)
            End If
            
            Return False
        End Function

#End Region

#Region "Helper Methods"

        ''' <summary>Force state to Error (for exception handlers)</summary>
        Public Sub SetErrorState(reason As String)
            TransitionTo(ApplicationState.Error, reason)
        End Sub
        
        ''' <summary>Get human-readable state description</summary>
        Public Function GetStateDescription() As String
            Select Case CurrentState
                Case ApplicationState.Uninitialized
                    Return "Application starting..."
                Case ApplicationState.Idle
                    Return "Ready"
                Case ApplicationState.Armed
                    Return "Ready to record"
                Case ApplicationState.Recording
                    Return "Recording..."
                Case ApplicationState.Playing
                    Return "Playing..."
                Case ApplicationState.Error
                    Return "Error - reset required"
                Case Else
                    Return "Unknown state"
            End Select
        End Function

#End Region

    End Class
    
    ''' <summary>
    ''' Event args for state change that can be cancelled
    ''' </summary>
    Public Class StateChangingEventArgs
        Inherits StateChangedEventArgs
        
        Public Property Cancel As Boolean = False
        
        Public Sub New(oldState As ApplicationState, newState As ApplicationState, Optional reason As String = "")
            MyBase.New(oldState, newState, reason)
        End Sub
    End Class

End Namespace
```

**Validation:**
- [ ] Singleton pattern works
- [ ] Thread-safe state access
- [ ] Transition validation enforced
- [ ] Events fire correctly

---

#### **Task 4.2: Integrate StateManager into MainForm**
**File:** `MainForm.vb`  
**Time:** 1 hour  
**Priority:** HIGH  
**Depends On:** Task 4.1

**Changes:**
```visualbasic
' Add at top of MainForm class:
Private ReadOnly stateManager As State.StateManager = State.StateManager.Instance

' In Form_Load:
Private Sub Form_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    ' ... existing initialization ...
    
    ' Subscribe to state changes
    AddHandler stateManager.StateChanged, AddressOf OnStateChanged
    
    ' Transition to Idle after initialization
    stateManager.TransitionTo(State.ApplicationState.Idle, "Application initialized")
End Sub

' New method: Handle state changes
Private Sub OnStateChanged(sender As Object, e As State.StateChangedEventArgs)
    If InvokeRequired Then
        BeginInvoke(Sub() OnStateChanged(sender, e))
        Return
    End If
    
    ' Update status display
    lblStatus.Text = $"Status: {stateManager.GetStateDescription()}"
    
    ' Update LED color based on state
    Select Case e.NewState
        Case State.ApplicationState.Uninitialized
            panelLED.BackColor = Color.Gray
            
        Case State.ApplicationState.Idle
            panelLED.BackColor = Color.Gray
            
        Case State.ApplicationState.Armed
            panelLED.BackColor = Color.Yellow
            
        Case State.ApplicationState.Recording
            panelLED.BackColor = Color.Red
            
        Case State.ApplicationState.Playing
            panelLED.BackColor = Color.Green
            
        Case State.ApplicationState.Error
            panelLED.BackColor = Color.DarkRed
    End Select
    
    ' Update UI controls (enable/disable based on state)
    UpdateUIForState(e.NewState)
    
    Utils.Logger.Instance.Info($"UI updated for state: {e.NewState}", "MainForm")
End Sub

' New method: Enable/disable controls based on state
Private Sub UpdateUIForState(state As State.ApplicationState)
    Select Case state
        Case State.ApplicationState.Idle
            ' Can arm microphone or play file
            transportControl.IsRecordArmed = False
            transportControl.IsRecording = False
            transportControl.IsPlaying = False
            ' Enable file operations
            
        Case State.ApplicationState.Armed
            ' Can start recording
            transportControl.IsRecordArmed = True
            transportControl.IsRecording = False
            transportControl.IsPlaying = False
            
        Case State.ApplicationState.Recording
            ' Can stop recording
            transportControl.IsRecordArmed = True
            transportControl.IsRecording = True
            transportControl.IsPlaying = False
            
        Case State.ApplicationState.Playing
            ' Can stop playback
            transportControl.IsRecordArmed = False
            transportControl.IsRecording = False
            transportControl.IsPlaying = True
            
        Case State.ApplicationState.Error
            ' Disable most controls
            transportControl.IsRecordArmed = False
            transportControl.IsRecording = False
            transportControl.IsPlaying = False
    End Select
End Sub

' Update OnMicrophoneArmed:
Private Sub OnMicrophoneArmed(sender As Object, isArmed As Boolean)
    If isArmed Then
        ' Transition to Armed state
        If stateManager.TransitionTo(State.ApplicationState.Armed, "Microphone armed") Then
            ' ... existing tap point initialization ...
            InitializeTapPointReaders()
        End If
    Else
        ' Transition back to Idle
        stateManager.TransitionTo(State.ApplicationState.Idle, "Microphone disarmed")
        CleanupTapPointReaders()
    End If
End Sub

' Update recording start:
Private Sub OnRecordingStarted(sender As Object, e As EventArgs)
    stateManager.TransitionTo(State.ApplicationState.Recording, "Recording started")
End Sub

' Update recording stop:
Private Sub OnRecordingStopped(sender As Object, e As EventArgs)
    ' Transition to Armed (mic still armed) or Idle (if disarming)
    If stateManager.IsMicrophoneArmed Then
        stateManager.TransitionTo(State.ApplicationState.Armed, "Recording stopped")
    Else
        stateManager.TransitionTo(State.ApplicationState.Idle, "Recording stopped, mic disarmed")
    End If
End Sub

' Update playback:
Private Sub OnPlaybackStarted(sender As Object, e As EventArgs)
    stateManager.TransitionTo(State.ApplicationState.Playing, "Playback started")
End Sub

Private Sub OnPlaybackStopped(sender As Object, e As EventArgs)
    stateManager.TransitionTo(State.ApplicationState.Idle, "Playback stopped")
End Sub
```

**Validation:**
- [ ] State transitions work correctly
- [ ] LED colors update
- [ ] Status text updates
- [ ] UI controls enable/disable correctly

---

#### **Task 4.3: Add State Validation Guards**
**File:** `MainForm.vb` (various methods)  
**Time:** 30 minutes  
**Priority:** MEDIUM  
**Depends On:** Task 4.2

**Add guards to critical methods:**
```visualbasic
Private Sub btnRecord_Click(sender As Object, e As EventArgs)
    ' Guard: Can only record when Armed
    If Not stateManager.IsState(State.ApplicationState.Armed) Then
        MessageBox.Show("Cannot record - microphone not armed", "Invalid State")
        Utils.Logger.Instance.Warning("Record button clicked but not in Armed state", "MainForm")
        Return
    End If
    
    ' Proceed with recording...
End Sub

Private Sub btnPlay_Click(sender As Object, e As EventArgs)
    ' Guard: Can only play when Idle
    If Not stateManager.IsState(State.ApplicationState.Idle) Then
        MessageBox.Show("Cannot play - stop current operation first", "Invalid State")
        Return
    End If
    
    ' Proceed with playback...
End Sub

' Add to RecordingManager.ArmMicrophone:
Public Sub ArmMicrophone()
    ' Guard: Can only arm from Idle state
    If Not State.StateManager.Instance.CanTransitionTo(State.ApplicationState.Armed) Then
        Throw New InvalidOperationException($"Cannot arm microphone from {State.StateManager.Instance.CurrentState} state")
    End If
    
    ' Proceed with arming...
End Sub
```

**Validation:**
- [ ] Guards prevent invalid operations
- [ ] Clear error messages to user
- [ ] Logging captures guard violations

---

### **Phase 5: Testing & Validation (1.5 hours ? 2 hours)**

#### **Task 5.1: Unit Tests (Optional)**
**Time:** 30 minutes  
**Priority:** LOW

Create tests for:
- TapPointManager.CreateReader
- TapPointManager.Read
- TapPointManager.DestroyReader
- Disposal cleanup
- StateManager.TransitionTo (valid/invalid transitions)

---

#### **Task 5.2: Manual Integration Testing**
**Time:** 1 hour  
**Priority:** HIGH

**Test Scenarios:**

1. **Basic Functionality:**
   - [ ] Arm microphone ? tap readers created
   - [ ] Speak into mic ? meters update
   - [ ] Disarm microphone ? readers destroyed
   - [ ] Re-arm ? readers recreated successfully

2. **Gain Control:**
   - [ ] Move AudioPipelinePanel Input Gain ? meters reflect change
   - [ ] Move AudioPipelinePanel Output Gain ? meters reflect change
   - [ ] Move DSPSignalFlowPanel trackGain ? meters reflect change
   - [ ] Verify input/output meters show different levels

3. **Pan Control:**
   - [ ] Pan center ? L/R meters equal
   - [ ] Pan left ? L high, R low
   - [ ] Pan right ? R high, L low
   - [ ] Smooth transition during pan sweep

4. **Recording:**
   - [ ] Record with 0 dB gain ? normal level
   - [ ] Record with -20 dB gain ? quiet file
   - [ ] Record with +6 dB gain ? louder file
   - [ ] Play back ? verify gain applied

5. **Error Handling:**
   - [ ] Arm/disarm 10 times rapidly ? no crashes
   - [ ] Create duplicate reader ? exception thrown
   - [ ] Read from non-existent reader ? exception thrown
   - [ ] Dispose manager ? subsequent calls fail gracefully

---

#### **Task 5.3: State Machine Testing** ??
**Time:** 30 minutes  
**Priority:** HIGH  
**Depends On:** Task 4.2

**Test Scenarios:**

1. **Valid State Transitions:**
   - [ ] Uninitialized ? Idle (app startup)
   - [ ] Idle ? Armed (arm microphone)
   - [ ] Armed ? Recording (start recording)
   - [ ] Recording ? Armed (stop recording, mic still armed)
   - [ ] Armed ? Idle (disarm microphone)
   - [ ] Idle ? Playing (play file)
   - [ ] Playing ? Idle (stop playback)

2. **Invalid State Transitions (Should Fail):**
   - [ ] Idle ? Recording (must arm first) - blocked ?
   - [ ] Armed ? Playing (can't play while armed) - blocked ?
   - [ ] Recording ? Playing (can't switch) - blocked ?
   - [ ] Playing ? Recording (can't switch) - blocked ?

3. **UI State Sync:**
   - [ ] LED color matches state
   - [ ] Status text matches state
   - [ ] Controls enable/disable correctly per state
   - [ ] Transport control buttons reflect state

4. **Error Recovery:**
   - [ ] Trigger error ? state becomes Error
   - [ ] Reset from Error ? returns to Idle
   - [ ] Error state disables dangerous operations

5. **Rapid State Changes:**
   - [ ] Arm/disarm 20 times rapidly ? stable
   - [ ] Start/stop recording 10 times ? no deadlock
   - [ ] State events fire in correct order

---

### **Phase 6: Documentation (1 hour ? 1.5 hours)**

#### **Task 6.1: Update Architecture Documents**
**Files:**
- `Documentation/Architecture/Audio-Signal-Flow-v1_3_1_3.md`
- `Documentation/Architecture/TapPointManager-Architecture.md` (new)
- `Documentation/Architecture/State-Machine-Architecture.md` (new) ??

**Time:** 45 minutes (was 30)

**Updates:**
- Add TapPointManager to signal flow diagram
- Document tap point enum values
- Update meter wiring section
- Mark Issue #003 as resolved
- **Document state machine architecture** ??
- **Document valid state transitions** ??
- **Add state machine diagram** ??

---

#### **Task 6.2: Create Session Notes**
**File:** `Documentation/Active/Sessions/YYYY-MM-DD-TapPointManager-StateManager.md`  
**Time:** 30 minutes

Use Session-Notes-v1_0_0.md template:
- RDF phases executed
- Discoveries and learnings
- Metrics (time spent, files changed)
- Meta-reflection
- **State machine design decisions** ??

---

#### **Task 6.3: Update Issues**
**Time:** 10 minutes

- [ ] Close Issue #001 (meters bypass DSP) - RESOLVED
- [ ] Close Issue #002 (meters same value) - RESOLVED
- [ ] Close Issue #003 (tap points unused) - RESOLVED
- [ ] Keep Issue #004 open (master/width - future)
- [ ] Close Issue #005 (no manager) - RESOLVED

Move all resolved issues to `Completed/Issues/`

---

#### **Task 6.4: Update Changelog**
**File:** `Documentation/Changelog/CURRENT.md`  
**Time:** 20 minutes (was 15)

**Entry:**
```markdown
## [v1.3.2.0] - YYYY-MM-DD - TapPointManager + State Machine

**RDF Phase:** Phase 2-6 (Architecture ? Synthesis)

### Features
- **TapPointManager:** Centralized, type-safe tap point access
- **TapPoint Enum:** PreDSP, PostInputGain, PostOutputGain, PreOutput
- **Stereo Metering:** Separate L/R channel analysis and display
- **State Machine:** Centralized application state management ??
- **State Validation:** Invalid transitions prevented ??

### Bug Fixes
- Meters now show DSP-processed audio (not raw)
- Gain/pan controls provide immediate visual feedback
- DSPSignalFlowPanel meters display true stereo imaging

### Technical
- Created TapPointManager class with reader lifecycle management
- Added AnalyzeStereoChannels helper for L/R separation
- Thread-safe multi-reader support
- Proper resource disposal (IDisposable pattern)
- **StateManager singleton with transition validation** ??
- **UI state synchronization via events** ??

### Breaking Changes
None - all changes internal
```

---

#### **Task 6.5: Git Commit & Tag**
**Time:** 15 minutes

**Commits:**
```bash
git add .
git commit -m "v1.3.2.0 - TapPointManager + State Machine architecture

TapPointManager:
- Created TapPointManager with type-safe tap point access
- Added TapPoint enum (PreDSP, PostInputGain, PostOutputGain, PreOutput)
- Implemented stereo L/R channel separation for meters
- Updated MainForm to use tap points for metering
- Added reader lifecycle management (IDisposable)

State Machine:
- Created StateManager singleton for centralized state control
- Added ApplicationState enum with 6 states
- Implemented state transition validation
- UI elements sync with state automatically
- Invalid operations prevented with guard clauses

Resolves: Issue #001, #002, #003, #005
RDF Phase: 2-6 (Architecture -> Synthesis)"

git tag -a v1.3.2.0 -m "TapPointManager + State Machine complete"
git push origin master --tags
```

---

## ?? Progress Tracking

### **Phase 1: Core Infrastructure**
- [ ] Task 1.1: TapPoint enum (15 min)
- [ ] Task 1.2: TapPointReader class (20 min)
- [ ] Task 1.3: TapPointManager class (1.5 hours)

**Estimated:** 2 hours 5 minutes  
**Actual:** _____

---

### **Phase 2: DSPThread Integration**
- [ ] Task 2.1: Expose buffers (15 min)
- [ ] Task 2.2: Add to RecordingManager (30 min)

**Estimated:** 45 minutes  
**Actual:** _____

---

### **Phase 3: MainForm Integration**
- [ ] Task 3.1: Create readers (45 min)
- [ ] Task 3.2: Update meter code (1 hour)
- [ ] Task 3.3: Stereo separation (30 min)

**Estimated:** 2 hours 15 minutes  
**Actual:** _____

---

### **Phase 4: State Machine Architecture** ??
- [ ] Task 4.0: ApplicationState enum (20 min)
- [ ] Task 4.1: StateManager class (1.5 hours)
- [ ] Task 4.2: MainForm integration (1 hour)
- [ ] Task 4.3: State validation guards (30 min)

**Estimated:** 3 hours 20 minutes  
**Actual:** _____

---

### **Phase 5: Testing**
- [ ] Task 5.1: Unit tests (30 min) - OPTIONAL
- [ ] Task 5.2: Manual testing (1 hour)
- [ ] Task 5.3: State machine testing (30 min) ??

**Estimated:** 2 hours  
**Actual:** _____

---

### **Phase 6: Documentation**
- [ ] Task 5.1: Architecture docs (30 min)
- [ ] Task 5.2: Session notes (30 min)
- [ ] Task 5.3: Update issues (10 min)
- [ ] Task 5.4: Changelog (15 min)
- [ ] Task 5.5: Git commit (15 min)

**Estimated:** 1 hour 40 minutes  
**Actual:** _____

---

## ?? Total Time Estimates

**Minimum (Skip unit tests + state machine):** 7 hours 45 minutes  
**With State Machine (Recommended):** 11 hours  
**Full implementation (all features):** 12 hours

**Recommended Session Plan:**
- **Session 1 (3 hours):** Phase 1 + Phase 2 (Core infrastructure)
- **Session 2 (4 hours):** Phase 3 + Phase 6 (MainForm + State Machine)
- **Session 3 (2 hours):** Phase 4 + Phase 5 (Testing + Documentation)
- **Session 4 (2 hours):** Phase 7 (State Machine Testing + Polish)

---

## ? Acceptance Criteria

### **Functional:**
1. TapPointManager provides clean API for tap point access
2. Meters display DSP-processed audio (not raw)
3. Gain/pan sliders affect meter levels immediately
4. Stereo imaging visible in L/R meters
5. Recording still works correctly
6. **State machine controls application flow**
7. **UI elements enable/disable based on state**
8. **Invalid state transitions prevented**

### **Code Quality:**
1. Thread-safe operation (multiple readers)
2. Proper resource cleanup (IDisposable)
3. Exception handling for error cases
4. Logging for debugging
5. XML documentation comments
6. **Centralized state management**
7. **State transition validation**

### **User Experience:**
1. Sliders provide immediate visual feedback
2. Pan control shows L/R differences
3. Input/output stages distinguishable
4. No performance degradation
5. **Clear visual state feedback (LED colors, button states)**
6. **Predictable application behavior**
7. **No "impossible" states (e.g., record while idle)**

---

## ?? Related Documents

- [Issue #005](../Active/Issues/v1_3_1_3-005-No-TapPointManager.md) - Original issue
- [Audio-Signal-Flow-v1_3_1_3.md](../Architecture/Audio-Signal-Flow-v1_3_1_3.md) - Current architecture
- [Issue #001](../Active/Issues/v1_3_1_3-001-Meters-Bypass-DSP.md) - Will be resolved
- [Issue #002](../Active/Issues/v1_3_1_3-002-Meters-Show-Same-Value.md) - Will be resolved
- [Issue #003](../Active/Issues/v1_3_1_3-003-Tap-Points-Unused.md) - Will be resolved

---

**Created By:** Rick + GitHub Copilot (RDF Task Planning)  
**Version:** 1.0  
**Status:** Ready to Execute  
**Next Step:** Begin Phase 1 - Create TapPoint enum
