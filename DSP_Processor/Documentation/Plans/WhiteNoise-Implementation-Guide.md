# White Noise Test Audio - Implementation Guide

## Overview
This guide explains how to add the white noise WAV file to your project and have it automatically deployed to the Recordings folder when the application is compiled and run.

## Solution Components

### 1. ResourceDeployer Class (`Utils/ResourceDeployer.vb`)
? **Created** - Handles embedded resource deployment

**Features:**
- Deploys embedded test audio files on application startup
- Skips deployment if file already exists (won't overwrite user recordings)
- Logs all deployment activities
- Can list all embedded resources for debugging

### 2. MainForm Integration
? **Updated** - Calls `ResourceDeployer.DeployTestAudioFiles()` in `MainForm_Load`

### 3. File Browser UI
? **Added** - New controls in Program tab routing section:
- `btnBrowseInputFile` - Button to browse for audio files
- `lblSelectedFile` - Label showing selected filename
- Auto-enables/disables based on input source selection

---

## How to Add Your White Noise File

### Step 1: Add WAV File to Project

1. **Create Test Audio Folder** in your project:
   ```
   DSP_Processor/
   ??? TestAudio/          <-- Create this folder
       ??? WhiteNoise.wav  <-- Add your file here
   ```

2. **In Visual Studio:**
   - Right-click `DSP_Processor` project ? Add ? New Folder ? Name it `TestAudio`
   - Right-click `TestAudio` folder ? Add ? Existing Item ? Select your `WhiteNoise.wav`

### Step 2: Configure as Embedded Resource

1. **Select the WAV file** in Solution Explorer
2. **Open Properties window** (F4 or View ? Properties Window)
3. **Change properties:**
   - **Build Action**: `Embedded Resource`
   - **Copy to Output Directory**: `Do not copy`

### Step 3: Verify Embedded Resource Name

The embedded resource name will be:
```
DSP_Processor.TestAudio.WhiteNoise.wav
```

Format: `<Namespace>.<FolderPath>.<FileName>`

### Step 4: Build and Run

1. Build the solution (F6)
2. Run the application (F5)
3. **Check Recordings folder** - `WhiteNoise_Test.wav` should be created automatically

---

## How to Use the White Noise File

### Option 1: Manual Selection
1. Go to **Program** tab
2. Select **?? File Playback** radio button
3. Click **Browse for audio file...**
4. Select `WhiteNoise_Test.wav` from Recordings folder
5. The file path will display below the button

### Option 2: From Files Tab
1. Go to **Files** tab
2. Double-click `WhiteNoise_Test.wav` in the list
3. It will start playing through the audio output device

---

## File Location

After first run, your white noise file will be at:
```
<Application Folder>/Recordings/WhiteNoise_Test.wav
```

Example:
```
C:\Users\rchau\source\repos\DSP_Processor\DSP_Processor\bin\Debug\net8.0-windows\Recordings\WhiteNoise_Test.wav
```

---

## Adding More Test Files

To add more embedded test files (e.g., pink noise, sine wave, etc.):

### 1. Add to DeployTestAudioFiles method:
```vb
Public Shared Sub DeployTestAudioFiles()
    Try
        Dim recordingsFolder = Path.Combine(Application.StartupPath, "Recordings")
        
        If Not Directory.Exists(recordingsFolder) Then
            Directory.CreateDirectory(recordingsFolder)
        End If
        
        ' Deploy white noise
        DeployEmbeddedResource("DSP_Processor.TestAudio.WhiteNoise.wav", 
                              Path.Combine(recordingsFolder, "WhiteNoise_Test.wav"))
        
        ' ADD MORE FILES HERE:
        DeployEmbeddedResource("DSP_Processor.TestAudio.PinkNoise.wav", 
                              Path.Combine(recordingsFolder, "PinkNoise_Test.wav"))
        
        DeployEmbeddedResource("DSP_Processor.TestAudio.Sine1kHz.wav", 
                              Path.Combine(recordingsFolder, "Sine1kHz_Test.wav"))
        
        Logger.Instance.Info("Test audio files deployed successfully", "ResourceDeployer")
    Catch ex As Exception
        Logger.Instance.Error("Failed to deploy test audio files", ex, "ResourceDeployer")
    End Try
End Sub
```

### 2. Add files to TestAudio folder
### 3. Set Build Action = Embedded Resource
### 4. Build and run

---

## Troubleshooting

### File Not Appearing
**Check:**
1. Build Action is set to `Embedded Resource`
2. File is in `TestAudio` folder
3. Resource name matches: `DSP_Processor.TestAudio.WhiteNoise.wav`

**Debug:**
```vb
' In MainForm_Load, add this to see all embedded resources:
Dim resources = Utils.ResourceDeployer.ListEmbeddedResources()
For Each resource In resources
    Console.WriteLine(resource)
Next
```

### File Exists But Different
If you update the WAV file:
1. Delete old file from Recordings folder
2. Rebuild and run (it will deploy the new version)

OR

Change the deployment code to overwrite:
```vb
' Remove this check to force overwrite:
If File.Exists(targetPath) Then
    Return
End If
```

---

## Technical Details

### Why Embedded Resources?
? **Advantages:**
- File is **compiled into the EXE**
- No need to distribute separate files
- File is always available
- Can't be accidentally deleted by user
- Works on any machine

### Resource Naming Convention
```
<RootNamespace>.<FolderPath>.<FileName>

Examples:
DSP_Processor.TestAudio.WhiteNoise.wav
DSP_Processor.TestAudio.Calibration.Tone440Hz.wav
DSP_Processor.Resources.Images.Logo.png
```

### Deployment Strategy
- **First Run:** File is extracted from EXE to Recordings folder
- **Subsequent Runs:** File exists, skip extraction (don't overwrite)
- **User Can Delete:** If user deletes it, it will re-deploy on next run

---

## Future Enhancements

### Phase 2.1: Enhance AudioRouter
Add property to store selected file path:
```vb
Public Class AudioRouter
    Public Property SelectedInputFile As String
    
    Public Sub SelectInputFile(filepath As String)
        If Not File.Exists(filepath) Then
            Throw New FileNotFoundException("Input file not found", filepath)
        End If
        
        SelectedInputFile = filepath
        Logger.Instance.Info($"Input file selected: {filepath}", "AudioRouter")
    End Sub
End Class
```

### Phase 2.2: Actual Playback Routing
Connect file playback to DSP thread for processing:
```vb
' In AudioRouter:
Public Sub StartFilePlayback()
    If String.IsNullOrEmpty(SelectedInputFile) Then
        Throw New InvalidOperationException("No input file selected")
    End If
    
    ' Create file reader
    ' Route to DSP thread
    ' Output to speakers
End Sub
```

---

## Summary

? **ResourceDeployer** created - handles embedded resource deployment  
? **MainForm updated** - deploys test files on startup  
? **UI added** - file browser button in routing section  
? **Build successful** - ready to add your white noise file  

**Next Steps:**
1. Add `WhiteNoise.wav` to `TestAudio` folder
2. Set Build Action = Embedded Resource
3. Build and run - file will appear in Recordings folder automatically!

---

**Created:** 2026-01-14  
**Phase:** 2.0 (Audio Routing Foundation)  
**Status:** ? Complete

