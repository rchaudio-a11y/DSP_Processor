# Reader Management Design v1.0.0
## Naming Convention & Registry Standard

**Date:** 2026-01-17  
**Version:** 1.0.0  
**Status:** ?? DESIGN COMPLETE  
**Purpose:** Define reader naming convention, registry pattern, and lifecycle management

---

## ?? **OVERVIEW**

This document defines the **naming convention and registry pattern** for FFT and meter readers in DSP Processor. It establishes standards that prevent collisions, clarify ownership, and enable self-documenting code.

**Key Principles:**
1. **Consistent Naming** - All readers follow `{Owner}_{TapPoint}_{Type}` pattern
2. **Central Registry** - MonitoringController tracks all readers
3. **Validation Enforcement** - Invalid names rejected at registration
4. **Clear Ownership** - Every reader has explicit owner
5. **Self-Documenting** - Names reveal purpose and location

**Problem Solved:**
- ? `_default_` pattern is unclear and unmaintainable
- ? No central tracking of reader ownership
- ? Name collisions possible
- ? Debugging difficult with generic names

**Solution:**
- ? Structured naming convention enforced at registration
- ? MonitoringController central registry
- ? Validation prevents invalid names
- ? Clear ownership and purpose from name alone

---

## ?? **PART 1: NAMING CONVENTION**

### **The Pattern:**

```
{Owner}_{TapPoint}_{Type}

Owner    - Component that owns the reader (AudioRouter, RecordingManager)
TapPoint - Location in signal chain (Input, Output, PreDSP, PostDSP)
Type     - Reader type (FFT, Meter, Waveform)
```

### **Examples:**

**Valid Names:**
```
? AudioRouter_Input_FFT        - AudioRouter's input FFT reader
? AudioRouter_Output_FFT       - AudioRouter's output FFT reader
? RecordingManager_PreDSP_Meter - RecordingManager's pre-DSP meter
? RecordingManager_PostDSP_Meter - RecordingManager's post-DSP meter
? AudioRouter_Input_Waveform   - AudioRouter's input waveform
```

**Invalid Names:**
```
? _default_                    - Legacy, unclear owner/purpose
? fft1                         - Missing owner and tap point
? AudioRouter_FFT              - Missing tap point
? Input_FFT                    - Missing owner
? AudioRouter__FFT             - Empty tap point (double underscore)
? AudioRouter_Input            - Missing type
```

### **Validation Rules:**

**Rule 1: Three Parts Required**
- Name MUST have exactly 3 parts separated by single underscore
- No leading/trailing underscores
- No double underscores

**Rule 2: Owner Must Be Known Component**
- Valid owners: `AudioRouter`, `RecordingManager`, `DSPThread`, `Pipeline`
- Owner determines lifecycle responsibility
- Owner component creates and destroys reader

**Rule 3: TapPoint Must Be Valid Location**
- Valid tap points: `Input`, `Output`, `PreDSP`, `PostDSP`, `PreGain`, `PostGain`
- TapPoint maps to TapPointManager location
- TapPoint defines where in signal chain reader samples

**Rule 4: Type Must Be Known Reader Type**
- Valid types: `FFT`, `Meter`, `Waveform`, `Histogram`
- Type determines reader interface
- Type defines data format

**Rule 5: Name Must Be Globally Unique**
- No two readers can have same name
- Registry enforces uniqueness
- Registration fails if name exists

---

## ??? **PART 2: REGISTRY PATTERN**

### **MonitoringController as Central Registry:**

```visualbasic
' MonitoringController owns reader registry
Private ReadOnly _readers As New Dictionary(Of String, ReaderInfo)
Private ReadOnly _readerLock As New Object()

' Thread-safe registration
Public Sub RegisterReader(readerName As String, owner As String, tapLocation As String)
    ' 1. Validate naming convention
    ' 2. Check uniqueness
    ' 3. Create immutable ReaderInfo
    ' 4. Add to registry
End Sub

' Thread-safe lookup
Public Function GetRegisteredReaders() As IReadOnlyList(Of ReaderInfo)
    ' Returns immutable snapshot
End Function
```

### **Registration Flow:**

```
Component wants reader
    ?
Call MonitoringController.RegisterReader(name, owner, tapLocation)
    ?
Validate name format ({Owner}_{TapPoint}_{Type})
    ?
Check name uniqueness in registry
    ?
Create immutable ReaderInfo
    ?
Add to registry dictionary
    ?
Create actual TapPointManager reader
    ?
Reader ready for use
```

### **Benefits:**

| Benefit | Description |
|---------|-------------|
| **Central Tracking** | All readers in one place |
| **Ownership Clarity** | Owner explicit in name and ReaderInfo |
| **Collision Prevention** | Uniqueness enforced at registration |
| **Easy Debugging** | Query registry for all readers |
| **Lifecycle Management** | Registry knows when reader created/destroyed |

---

## ?? **PART 3: READERINFO METADATA**

### **ReaderInfo Class:**

```visualbasic
Public Class ReaderInfo
    ' Immutable core properties
    Public ReadOnly Property Name As String
    Public ReadOnly Property Owner As String
    Public ReadOnly Property TapLocation As String
    Public ReadOnly Property CreatedAt As DateTime
    
    ' Mutable health (atomic updates)
    Public Property Health As ReaderHealth
    Public Property LastAccess As DateTime
    
    ' Parse name components
    Public ReadOnly Property OwnerComponent As String
        Get
            Return Name.Split("_"c)(0)
        End Get
    End Property
    
    Public ReadOnly Property TapPoint As String
        Get
            Return Name.Split("_"c)(1)
        End Get
    End Property
    
    Public ReadOnly Property ReaderType As String
        Get
            Return Name.Split("_"c)(2)
        End Get
    End Property
End Class
```

### **Metadata Usage:**

```visualbasic
' Query readers by owner
Dim audioRouterReaders = controller.GetRegisteredReaders()
    .Where(Function(r) r.Owner = "AudioRouter")
    .ToList()

' Query readers by tap point
Dim inputReaders = controller.GetRegisteredReaders()
    .Where(Function(r) r.TapPoint = "Input")
    .ToList()

' Query readers by type
Dim fftReaders = controller.GetRegisteredReaders()
    .Where(Function(r) r.ReaderType = "FFT")
    .ToList()

' Query unhealthy readers
Dim faultedReaders = controller.GetRegisteredReaders()
    .Where(Function(r) r.Health = ReaderHealth.Faulted)
    .ToList()
```

---

## ?? **PART 4: LIFECYCLE MANAGEMENT**

### **Reader Lifecycle:**

```
1. REGISTRATION
   MonitoringController.RegisterReader(name, owner, tapLocation)
   ??> Validates name
   ??> Creates ReaderInfo
   ??> Adds to registry

2. CREATION (during Initialize)
   MonitoringController.Initialize()
   ??> For each registered reader
       ??> TapPointManager.CreateReader(name, tapLocation)

3. ACTIVATION (during Enable)
   MonitoringController.Enable()
   ??> Readers start receiving data
   ??> Health monitoring begins

4. DEACTIVATION (during Disable)
   MonitoringController.Disable()
   ??> Readers pause (no new data)
   ??> Health monitoring pauses

5. DESTRUCTION (during Dispose)
   MonitoringController.Dispose()
   ??> For each reader
       ??> TapPointManager.DestroyReader(name)
   ??> Registry cleared
```

### **Ownership Responsibilities:**

| Phase | Owner Responsibility |
|-------|---------------------|
| **Registration** | Component calls RegisterReader with valid name |
| **Creation** | MonitoringController creates via TapPointManager |
| **Lifecycle** | MonitoringController manages Enable/Disable |
| **Destruction** | MonitoringController destroys via TapPointManager |
| **Health** | MonitoringController monitors via ReaderInfo.Health |

---

## ?? **PART 5: NAMING STANDARD BY COMPONENT**

### **AudioRouter Readers:**

```visualbasic
' AudioRouter owns these readers
"AudioRouter_Input_FFT"        - Input spectrum analyzer
"AudioRouter_Output_FFT"       - Output spectrum analyzer
"AudioRouter_Input_Meter"      - Input level meter
"AudioRouter_Output_Meter"     - Output level meter
"AudioRouter_Input_Waveform"   - Input waveform display
```

**Registration:**
```visualbasic
' During AudioRouter initialization
Private Sub InitializeMonitoring()
    Dim controller = StateCoordinator.Instance.MonitoringController
    
    controller.RegisterReader("AudioRouter_Input_FFT", "AudioRouter", "Input")
    controller.RegisterReader("AudioRouter_Output_FFT", "AudioRouter", "Output")
    controller.RegisterReader("AudioRouter_Input_Meter", "AudioRouter", "Input")
    controller.RegisterReader("AudioRouter_Output_Meter", "AudioRouter", "Output")
End Sub
```

### **RecordingManager Readers:**

```visualbasic
' RecordingManager owns these readers
"RecordingManager_PreDSP_Meter"   - Pre-DSP processing meter
"RecordingManager_PostDSP_Meter"  - Post-DSP processing meter
"RecordingManager_PreDSP_FFT"     - Pre-DSP spectrum
"RecordingManager_PostDSP_FFT"    - Post-DSP spectrum
```

**Registration:**
```visualbasic
' During RecordingManager initialization
Private Sub InitializeMonitoring()
    Dim controller = StateCoordinator.Instance.MonitoringController
    
    controller.RegisterReader("RecordingManager_PreDSP_Meter", "RecordingManager", "PreDSP")
    controller.RegisterReader("RecordingManager_PostDSP_Meter", "RecordingManager", "PostDSP")
End Sub
```

### **Pipeline Readers (Future):**

```visualbasic
' Pipeline-specific monitoring (Phase 2)
"Pipeline_PreGain_Meter"       - Pre-gain stage meter
"Pipeline_PostGain_Meter"      - Post-gain stage meter
"Pipeline_PreWidth_Meter"      - Pre-width stage meter
"Pipeline_PostWidth_Meter"     - Post-width stage meter
```

---

## ? **PART 6: VALIDATION IMPLEMENTATION**

### **Validation Function:**

```visualbasic
''' <summary>
''' Validate reader name follows convention
''' Returns True if valid, False otherwise
''' </summary>
Private Function ValidateReaderName(name As String) As Boolean
    ' Rule 1: Not null or whitespace
    If String.IsNullOrWhiteSpace(name) Then
        Return False
    End If
    
    ' Rule 2: Exactly 3 parts separated by underscore
    Dim parts = name.Split("_"c)
    If parts.Length <> 3 Then
        Return False
    End If
    
    ' Rule 3: All parts non-empty
    If Not parts.All(Function(p) Not String.IsNullOrWhiteSpace(p)) Then
        Return False
    End If
    
    ' Rule 4: Owner is known component (optional strict validation)
    Dim validOwners = {"AudioRouter", "RecordingManager", "DSPThread", "Pipeline"}
    If Not validOwners.Contains(parts(0)) Then
        ' Warning: Unknown owner (allow but log)
        _logger.Warning($"Unknown owner in reader name: {parts(0)}", "MonitoringController")
    End If
    
    ' Rule 5: TapPoint is valid location (optional strict validation)
    Dim validTapPoints = {"Input", "Output", "PreDSP", "PostDSP", "PreGain", "PostGain"}
    If Not validTapPoints.Contains(parts(1)) Then
        ' Warning: Unknown tap point (allow but log)
        _logger.Warning($"Unknown tap point in reader name: {parts(1)}", "MonitoringController")
    End If
    
    ' Rule 6: Type is known reader type (optional strict validation)
    Dim validTypes = {"FFT", "Meter", "Waveform", "Histogram"}
    If Not validTypes.Contains(parts(2)) Then
        ' Warning: Unknown type (allow but log)
        _logger.Warning($"Unknown type in reader name: {parts(2)}", "MonitoringController")
    End If
    
    Return True
End Function
```

### **Registration with Validation:**

```visualbasic
Public Sub RegisterReader(readerName As String, owner As String, tapLocation As String)
    ' Shutdown barrier check
    If _shuttingDown Then Return
    If _disposed Then Throw New ObjectDisposedException("MonitoringController")
    
    ' ? Validate naming convention
    If Not ValidateReaderName(readerName) Then
        Throw New ArgumentException(
            $"Invalid reader name: '{readerName}'. " &
            $"Must follow pattern: {{Owner}}_{{TapPoint}}_{{Type}}" &
            $"Example: AudioRouter_Input_FFT",
            NameOf(readerName)
        )
    End If
    
    SyncLock _readerLock
        ' ? Check uniqueness
        If _readers.ContainsKey(readerName) Then
            _logger.Warning($"Reader already registered: {readerName}", "MonitoringController")
            Return
        End If
        
        ' ? Create immutable ReaderInfo
        Dim info As New ReaderInfo(
            name:=readerName,
            owner:=owner,
            tapLocation:=tapLocation,
            createdAt:=DateTime.Now
        )
        
        ' ? Add to registry
        _readers.Add(readerName, info)
        
        _logger.Debug($"? Registered reader: {info}", "MonitoringController")
    End SyncLock
End Sub
```

---

## ?? **PART 7: USAGE EXAMPLES**

### **Example 1: Register Readers During Component Initialization**

```visualbasic
Public Class AudioRouter
    Private _monitoringInitialized As Boolean = False
    
    Public Sub Initialize()
        ' ... existing initialization ...
        
        ' Register readers with MonitoringController
        InitializeMonitoring()
    End Sub
    
    Private Sub InitializeMonitoring()
        If _monitoringInitialized Then Return
        
        Try
            Dim controller = StateCoordinator.Instance.MonitoringController
            
            ' Register input FFT reader
            controller.RegisterReader(
                readerName:="AudioRouter_Input_FFT",
                owner:="AudioRouter",
                tapLocation:="Input"
            )
            
            ' Register output FFT reader
            controller.RegisterReader(
                readerName:="AudioRouter_Output_FFT",
                owner:="AudioRouter",
                tapLocation:="Output"
            )
            
            _logger.Info("? AudioRouter monitoring registered", "AudioRouter")
            _monitoringInitialized = True
            
        Catch ex As ArgumentException
            _logger.Error($"Failed to register AudioRouter readers: {ex.Message}", ex, "AudioRouter")
            Throw
        End Try
    End Sub
End Class
```

### **Example 2: Query Registry for Debugging**

```visualbasic
' Debug menu - show all readers
Private Sub ShowAllReaders()
    Dim controller = StateCoordinator.Instance.MonitoringController
    Dim readers = controller.GetRegisteredReaders()
    
    Dim sb As New StringBuilder()
    sb.AppendLine($"Total Readers: {readers.Count}")
    sb.AppendLine()
    
    ' Group by owner
    For Each ownerGroup In readers.GroupBy(Function(r) r.Owner)
        sb.AppendLine($"{ownerGroup.Key}:")
        For Each reader In ownerGroup
            sb.AppendLine($"  - {reader.Name}")
            sb.AppendLine($"    Tap: {reader.TapLocation}, Health: {reader.Health}")
        Next
        sb.AppendLine()
    Next
    
    MessageBox.Show(sb.ToString(), "Reader Registry", MessageBoxButtons.OK, MessageBoxIcon.Information)
End Sub
```

### **Example 3: Find Unhealthy Readers**

```visualbasic
' Monitor health and log warnings
Private Sub CheckReaderHealth()
    Dim controller = StateCoordinator.Instance.MonitoringController
    Dim readers = controller.GetRegisteredReaders()
    
    ' Find unhealthy readers
    Dim unhealthy = readers.Where(Function(r) r.Health <> ReaderHealth.Healthy).ToList()
    
    If unhealthy.Any() Then
        _logger.Warning($"Found {unhealthy.Count} unhealthy readers:", "HealthMonitor")
        
        For Each reader In unhealthy
            _logger.Warning($"  - {reader.Name}: {reader.Health}", "HealthMonitor")
        Next
    End If
End Sub
```

### **Example 4: Validate Name Before Registration**

```visualbasic
' Utility to validate reader name before attempting registration
Public Function IsValidReaderName(name As String) As Boolean
    If String.IsNullOrWhiteSpace(name) Then Return False
    
    Dim parts = name.Split("_"c)
    If parts.Length <> 3 Then Return False
    
    Return parts.All(Function(p) Not String.IsNullOrWhiteSpace(p))
End Function

' Usage
Private Sub RegisterCustomReader()
    Dim name = "MyComponent_Custom_FFT"
    
    If Not IsValidReaderName(name) Then
        MessageBox.Show(
            "Invalid reader name format!" & Environment.NewLine &
            "Must be: {Owner}_{TapPoint}_{Type}",
            "Validation Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        )
        Return
    End If
    
    ' Safe to register
    StateCoordinator.Instance.MonitoringController.RegisterReader(name, "MyComponent", "Custom")
End Sub
```

---

## ?? **PART 8: MIGRATION FROM LEGACY NAMES**

### **Legacy Pattern (`_default_`):**

**Problem:**
```visualbasic
' ? Legacy code - unclear ownership
_tapPointManager.CreateReader("_default_", "Input")
_tapPointManager.CreateReader("_default_", "Output")
```

**Issues:**
- Same name for multiple readers (collision!)
- No owner information
- No type information
- Impossible to track lifecycle
- Debugging nightmare

### **Migration Strategy:**

**Step 1: Identify Legacy Readers**
```visualbasic
' Find all CreateReader calls with "_default_"
' Replace with proper naming convention
```

**Step 2: Update to New Convention**
```visualbasic
' ? New code - clear ownership
controller.RegisterReader("AudioRouter_Input_FFT", "AudioRouter", "Input")
_tapPointManager.CreateReader("AudioRouter_Input_FFT", "Input")

controller.RegisterReader("AudioRouter_Output_FFT", "AudioRouter", "Output")
_tapPointManager.CreateReader("AudioRouter_Output_FFT", "Output")
```

**Step 3: Remove Legacy Code**
```visualbasic
' Remove old _default_ pattern
' All readers now registered with MonitoringController
```

### **Migration Checklist:**

- [ ] Identify all `_default_` usage
- [ ] Map to new naming convention
- [ ] Register with MonitoringController
- [ ] Update TapPointManager calls
- [ ] Test all readers work
- [ ] Remove legacy code
- [ ] Update documentation

---

## ? **PART 9: VALIDATION CHECKLIST**

Before implementation, verify:

**Naming Convention:**
- [ ] Pattern defined: `{Owner}_{TapPoint}_{Type}`
- [ ] Validation rules documented (5 rules)
- [ ] Examples provided (valid/invalid)
- [ ] Component-specific standards defined

**Registry Pattern:**
- [ ] MonitoringController owns registry
- [ ] Thread-safe registration
- [ ] Uniqueness enforcement
- [ ] Immutable ReaderInfo
- [ ] Snapshot pattern for queries

**Validation:**
- [ ] ValidateReaderName() function defined
- [ ] ArgumentException on invalid names
- [ ] Logging for warnings
- [ ] Registration integration

**Lifecycle:**
- [ ] Registration ? Creation ? Activation ? Deactivation ? Destruction
- [ ] Ownership responsibilities clear
- [ ] Component integration examples

**Migration:**
- [ ] Legacy pattern identified (`_default_`)
- [ ] Migration strategy defined
- [ ] Checklist provided

**Ready for Implementation:**
- [ ] Design reviewed
- [ ] Step 18-19 ready
- [ ] Phase 1 complete (8/8)

---

## ?? **REFERENCES**

**Related Documents:**
- MonitoringController-Design.md - Registry implementation
- Thread-Safety-Patterns.md - Applied patterns
- State-Coordinator-Design.md - Integration point

**Thread Safety Patterns Applied:**
- Part 2.5: Immutable Data (ReaderInfo)
- Part 6: Snapshot Pattern (GetRegisteredReaders())
- Part 12: Thread Ownership (MonitoringController owns registry)

**Implementation Files:**
- `Managers\MonitoringController.vb` - Registry + validation (Step 18)
- `Managers\ReaderInfo.vb` - Metadata class (Step 18)

**Migration Impact:**
- `AudioIO\AudioRouter.vb` - Update reader registration
- `Managers\RecordingManager.vb` - Update reader registration
- `DSP\TapPointManager.vb` - No changes (already supports named readers)

---

## ?? **NEXT STEPS**

1. **Review this design** - Validate naming convention
2. **Phase gate review** - All Phase 1 documents complete (8/8 = 100%)
3. **Begin Phase 2** - State machine implementation (Steps 9-15)
4. **Implement monitoring** - Phase 4 (Steps 18-20 use this design)

---

## ?? **PHASE 1 COMPLETE!**

**All Design Documents Finished:**
1. ? Architecture Assessment
2. ? State Machine Design
3. ? Satellite State Machines
4. ? State Coordinator Design
5. ? Thread Safety Audit
6. ? Thread Safety Patterns v2.0
7. ? MonitoringController Design v2.0
8. ? **Reader Management Design v1.0** ? YOU ARE HERE

**Phase 1 Status:** ?? **COMPLETE (8/8 = 100%)**

**Next Phase:** Phase 2 - State Machine Implementation (Steps 9-15)

---

**Design Complete:** ? **v1.0.0 - Reader Management Standard**  
**Date:** 2026-01-17  
**By:** Rick + GitHub Copilot  
**Status:** Production-grade naming convention and registry pattern  
**Impact:** Eliminates `_default_` pattern, enables tracking and debugging  
**Phase 1:** **COMPLETE** ??
