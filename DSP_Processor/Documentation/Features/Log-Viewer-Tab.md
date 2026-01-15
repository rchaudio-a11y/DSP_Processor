# Log Viewer Tab - Quick Start Guide! ??

## ?? **What's Added**

A new **?? Logs** tab with real-time log viewing!

---

## ?? **Log Tab Features**

### **UI Controls:**

```
???????????????????????????????????????????????
? ?? Logs Tab                                 ?
???????????????????????????????????????????????
? [Clear] [Save] [?Auto Scroll]  Level: [All?]?
???????????????????????????????????????????????
? [2024-01-15 14:23:45.123] [INFO] App start..?
? [2024-01-15 14:23:45.234] [INFO] Audio dev..?
? [2024-01-15 14:23:46.123] [INFO] Mic armed..?
? [2024-01-15 14:23:47.456] [INFO] Recording..?
? [2024-01-15 14:23:50.789] [WARNING] Buffer..?
? [2024-01-15 14:23:52.123] [ERROR] Failed...?
?                                             ?
?                (scrollable)                 ?
?                                             ?
???????????????????????????????????????????????
```

### **Color Coding:**
- **White** - Info messages
- **Orange** - Warnings
- **Red** - Errors

---

## ??? **Controls**

### **1. Clear Button**
- Clears the log viewer
- Clears in-memory buffer
- Logs the clear action

### **2. Save Button**
- Opens save dialog
- Exports all logs to `.txt` or `.log` file
- Default filename: `DSP_Logs_YYYYMMDD_HHMMSS.txt`
- Logs the export action

### **3. Auto Scroll Checkbox**
- **Checked (default):** Auto-scrolls to latest message
- **Unchecked:** Stays at current scroll position
- Useful for reviewing old messages

### **4. Log Level Filter**
- **All** - Show all messages (no filter)
- **Debug** - Show all messages
- **Info** - Show Info, Warning, Error
- **Warn** - Show Warning, Error
- **Error** - Show Error only

---

## ?? **Logging Locations**

### **1. UI Display (Logs Tab)**
- Real-time display
- Last 1000 messages
- Color-coded by level
- Filtered by level dropdown

### **2. File System**
**Location:** `DSP_Processor/bin/Debug/net10.0-windows/Logs/`

**Files:**
```
DSP_Processor_20240115_143045.log  (current)
DSP_Processor_20240115_140312.log  (previous)
DSP_Processor_20240115_135521.log  (older)
```

**File Format:**
```
[2024-01-15 14:30:45.123] [INFO] [DSP] Application started
[2024-01-15 14:30:45.234] [INFO] [DSP] Audio devices: 2 input device(s) found
[2024-01-15 14:30:46.123] [INFO] [DSP] Arming microphone: Stereo (2), 44100Hz, 16-bit
[2024-01-15 14:30:47.456] [INFO] [DSP] Starting recording...
[2024-01-15 14:30:47.567] [INFO] [DSP] Audio buffers cleared
[2024-01-15 14:30:47.678] [INFO] [DSP] Recording started successfully (IsRecording=True)
```

**File Rotation:**
- Max size: 10MB
- Max files: 10
- Oldest deleted automatically

---

## ?? **What Gets Logged**

### **Application Lifecycle:**
- ? Application start/stop
- ? Audio device enumeration
- ? Microphone arming

### **Recording Operations:**
- ? Recording start/stop
- ? Buffer clearing
- ? File creation
- ? Timer status
- ? Success/failure states

### **Playback Operations:**
- ? Playback start/stop
- ? File loading
- ? Volume changes (future)

### **Errors & Warnings:**
- ? All exceptions with stack traces
- ? Failed operations
- ? Resource cleanup failures
- ? Mic arming issues

### **User Actions:**
- ? Log filter changes
- ? Log clearing
- ? Log exporting

---

## ?? **Log Levels**

| Level | Color | When Used | Example |
|-------|-------|-----------|---------|
| **Info** | White | Normal operations | "Recording started" |
| **Warning** | Orange | Non-critical issues | "Mic not armed" |
| **Error** | Red | Failures | "Failed to start recording" |

---

## ?? **How to Use**

### **During Normal Operation:**
1. **Open Logs tab** (??)
2. **Watch real-time logs** as you use the app
3. **Verify operations** are working correctly

### **When Debugging:**
1. **Set filter to "All"** to see everything
2. **Try to reproduce** the issue
3. **Check for red/orange** messages
4. **Click Save** to export for analysis

### **After an Error:**
1. **Open Logs tab**
2. **Find the red error** message
3. **Read the exception** details
4. **Save logs** if needed for bug report

---

## ?? **API Usage**

Want to add logging to your code?

```visualbasic
' Get the logger (singleton)
Dim logger = Services.LoggingServiceAdapter.Instance

' Log messages
logger.LogInfo("Operation completed successfully")
logger.LogWarning("Buffer size is low")
logger.LogError("Failed to open device")

' Log with exception
Try
    ' ... risky operation ...
Catch ex As Exception
    logger.LogError($"Failed to process: {ex.Message}", ex)
End Try
```

---

## ?? **Example Log Session**

```
[2024-01-15 14:30:45.123] [INFO] DSP Processor started successfully
[2024-01-15 14:30:45.234] [INFO] Audio devices: 2 input device(s) found
[2024-01-15 14:30:46.123] [INFO] Arming microphone: Stereo (2), 44100Hz, 16-bit
[2024-01-15 14:30:46.234] [INFO] Microphone armed and ready
[2024-01-15 14:30:47.456] [INFO] Starting recording...
[2024-01-15 14:30:47.567] [INFO] Audio timer started
[2024-01-15 14:30:47.678] [INFO] Audio buffers cleared
[2024-01-15 14:30:47.789] [INFO] Recording started successfully (IsRecording=True)
[2024-01-15 14:30:52.123] [INFO] Stopping recording...
[2024-01-15 14:30:52.234] [INFO] Recording stopped (mic still armed)
[2024-01-15 14:30:53.456] [INFO] Log filter changed to: Error
[2024-01-15 14:30:55.678] [INFO] DSP Processor shutting down
```

---

## ?? **Benefits**

### **1. Real-Time Visibility**
- ? See what's happening as it happens
- ? No need to check file system
- ? Color-coded for quick scanning

### **2. Easy Debugging**
- ? All errors in one place
- ? Exception details included
- ? Stack traces for diagnosis
- ? Export logs for sharing

### **3. Audit Trail**
- ? Complete operation history
- ? Timestamped to millisecond
- ? Persistent file storage
- ? Auto-rotation prevents bloat

### **4. User Feedback**
- ? See when operations complete
- ? Understand what app is doing
- ? Verify settings applied
- ? Confirm actions succeeded

---

## ?? **Status**

**Build:** ? Successful  
**UI:** ? Complete  
**File Logging:** ? Active  
**Event Logging:** ? Active  
**Color Coding:** ? Implemented  
**Filtering:** ? Implemented  
**Export:** ? Implemented  

**The Log Viewer is ready! Open the ?? Logs tab and watch your app in action!** ???

---

## ?? **File Locations**

### **Log Files:**
```
DSP_Processor\
  bin\
    Debug\
      net10.0-windows\
        Logs\                    ? Log files here
          DSP_Processor_*.log
        DSP_Processor.exe
```

### **Exported Logs (Save button):**
```
DSP_Processor\
  bin\
    Debug\
      net10.0-windows\
        Logs\                    ? Default save location
          DSP_Logs_*.txt         ? Exported logs
```

---

**Document Version:** 1.0  
**Created:** 2024  
**For:** Rick (DSP Processor Project)  
**Feature:** Log Viewer Tab with Real-Time Display

**END OF GUIDE**
