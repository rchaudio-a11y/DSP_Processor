# Task 0.4: Unit Testing Framework Setup

**Priority:** ?? **MEDIUM** (Deferred from Phase 0)  
**Status:** ? Not Started  
**Estimated Time:** 3 days  
**Dependencies:** None

---

## ?? Task Overview

Set up a comprehensive unit testing framework for the DSP_Processor project. This was deferred from Phase 0 but should be completed before major new features are added.

---

## ?? Objectives

1. Create test project with xUnit or NUnit framework
2. Set up test infrastructure and helpers
3. Write sample tests for core modules
4. Configure CI/CD pipeline
5. Achieve >50% code coverage for core modules

---

## ?? Implementation Checklist

### **Step 1: Create Test Project**
- [ ] Create new project `DSP_Processor.Tests`
- [ ] Add reference to main DSP_Processor project
- [ ] Install testing framework (xUnit recommended)
- [ ] Install test runner
- [ ] Install coverage tools (Coverlet, ReportGenerator)

### **Step 2: Set Up Test Infrastructure**
- [ ] Create `TestHelpers/` folder
- [ ] Create `AudioTestHelpers.vb` - Audio data generators
- [ ] Create `MockAudioDevice.vb` - Mock audio devices
- [ ] Create `TestConstants.vb` - Test data paths, settings

### **Step 3: Write Core Module Tests**
- [ ] **AudioBuffer Tests:**
  - [ ] Test PCM16 to float conversion
  - [ ] Test float to PCM16 conversion
  - [ ] Test buffer allocation
  - [ ] Test channel access
- [ ] **RingBuffer Tests:**
  - [ ] Test write/read operations
  - [ ] Test wrap-around behavior
  - [ ] Test thread safety (concurrent writes/reads)
  - [ ] Test boundary conditions (full/empty)
- [ ] **ProcessorChain Tests:**
  - [ ] Test processor addition/removal
  - [ ] Test sequential processing
  - [ ] Test bypass functionality
  - [ ] Test latency calculation
- [ ] **GainProcessor Tests:**
  - [ ] Test dB to linear conversion
  - [ ] Test gain application
  - [ ] Test bypass
  - [ ] Test parameter range limits

### **Step 4: Create Integration Tests**
- [ ] Test complete recording pipeline
- [ ] Test playback pipeline
- [ ] Test DSP processing pipeline
- [ ] Test file I/O operations

### **Step 5: Configure CI/CD**
- [ ] Set up GitHub Actions workflow (or similar)
- [ ] Run tests on every commit
- [ ] Generate code coverage reports
- [ ] Fail build if tests fail

---

## ?? Reference Materials

### **Testing Frameworks:**
- xUnit.net - Modern, lightweight framework
- NUnit - Traditional, mature framework
- MSTest - Microsoft's built-in framework

### **Mocking:**
- Moq - Popular mocking library for .NET
- NSubstitute - Alternative mocking framework

### **Coverage Tools:**
- Coverlet - Cross-platform coverage tool
- ReportGenerator - Coverage report visualization

---

## ?? Implementation Tips

### **Test Organization:**
```
DSP_Processor.Tests/
??? AudioIO/
?   ??? MicInputSourceTests.vb
?   ??? PlaybackEngineTests.vb
?   ??? AudioRouterTests.vb
??? DSP/
?   ??? AudioBufferTests.vb
?   ??? ProcessorChainTests.vb
?   ??? GainProcessorTests.vb
?   ??? DSPThreadTests.vb
??? Utils/
?   ??? RingBufferTests.vb
?   ??? LoggerTests.vb
??? TestHelpers/
?   ??? AudioTestHelpers.vb
?   ??? MockAudioDevice.vb
?   ??? TestConstants.vb
??? IntegrationTests/
    ??? RecordingPipelineTests.vb
    ??? PlaybackPipelineTests.vb
```

### **Sample Test Structure:**
```vb
Imports Xunit

Public Class AudioBufferTests
    
    <Fact>
    Public Sub Constructor_ValidParameters_CreatesBuffer()
        ' Arrange
        Dim format = New WaveFormat(44100, 16, 2)
        Dim sampleCount = 2048
        
        ' Act
        Dim buffer = New AudioBuffer(format, sampleCount)
        
        ' Assert
        Assert.NotNull(buffer)
        Assert.Equal(sampleCount, buffer.SampleCount)
        Assert.Equal(format.SampleRate, buffer.Format.SampleRate)
    End Sub
    
    <Fact>
    Public Sub CopyFrom16BitPCM_ValidData_ConvertsCorrectly()
        ' Arrange
        Dim buffer = New AudioBuffer(New WaveFormat(44100, 16, 1), 256)
        Dim pcmData(511) As Byte ' 256 samples × 2 bytes
        
        ' Fill with test pattern (max positive value)
        For i = 0 To 255
            pcmData(i * 2) = &HFF
            pcmData(i * 2 + 1) = &H7F
        Next
        
        ' Act
        buffer.CopyFrom16BitPCM(pcmData, 0, pcmData.Length)
        
        ' Assert
        ' Max value (32767) should convert to ~1.0
        ' Allow small error due to float precision
        For i = 0 To buffer.SampleCount - 1
            Assert.InRange(buffer.GetSample(i, 0), 0.99F, 1.0F)
        Next
    End Sub
    
End Class
```

---

## ? Definition of Done

- [ ] Test project builds successfully
- [ ] At least 30 unit tests implemented
- [ ] All tests passing
- [ ] Code coverage >50% for core modules:
  - AudioBuffer
  - RingBuffer
  - ProcessorChain
  - GainProcessor
  - DSPThread
- [ ] Integration tests for main pipelines
- [ ] CI/CD pipeline configured
- [ ] Documentation for writing new tests

---

## ?? Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **Unit Tests** | >30 tests | Test project report |
| **Code Coverage** | >50% | Coverlet report |
| **Test Execution Time** | <10 seconds | Test runner output |
| **CI/CD Integration** | 100% passing | GitHub Actions status |

---

**Task Created:** January 14, 2026  
**Target Completion:** TBD (Post Phase 2)  
**Dependencies:** None  
**Recommendation:** Complete before Phase 3 to ensure quality
