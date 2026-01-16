# Debug Plan: Playback Time Display Issue

**Date:** January 15, 2026  
**Issue:** Playback time not counting, play indicator not lit  
**Status:** ?? **INVESTIGATING**

---

## ?? **What We Know**

### **Code Review Results:**
? `TimerPlayback.Start()` is called in `OnPlaybackStarted()` (line 899)  
? `TimerPlayback_Tick()` handler exists and should update position (line 931)  
? `UpdateTransportState()` sets transport state to Playing (line 968)  
? `playbackManager.UpdatePosition()` is called every tick (line 934)  
? `transportControl.TrackPosition` is updated every tick (line 935)  
? Timer interval is 17ms (~60 FPS) (MainForm.Designer.vb line 174)  

### **The Code SHOULD Work!**

So why doesn't it? Let's add diagnostics.

---

## ?? **Diagnostic Steps**

### **Step 1: Check if Timer is Actually Firing**

Add logging to see if timer ticks:

```visualbasic
Private Sub TimerPlayback_Tick(sender As Object, e As EventArgs) Handles TimerPlayback.Tick
    ' DIAGNOSTIC: Log every tick
    Logger.Instance.Debug($"TimerPlayback_Tick fired! IsPlaying={playbackManager?.IsPlaying}", "MainForm")
    
    ' Update regular playback position
    If playbackManager IsNot Nothing AndAlso playbackManager.IsPlaying Then
        ' ... existing code ...
    End If
End Sub
```

**Expected:** Log should show "TimerPlayback_Tick fired!" every 17ms during playback  
**If not firing:** Timer isn't starting or is being stopped  

---

### **Step 2: Check if IsPlaying Returns True**

```visualbasic
Private Sub OnPlaybackStarted(sender As Object, filepath As String)
    ' Start the timer to update playback position
    TimerPlayback.Start()
    
    ' DIAGNOSTIC: Check if playing immediately after start
    Logger.Instance.Info($"Playback started! IsPlaying={playbackManager.IsPlaying}", "MainForm")
    
    ' ... rest of code ...
End Sub
```

**Expected:** Log should show "IsPlaying=True" immediately after start  
**If False:** Playback didn't actually start  

---

### **Step 3: Check CurrentPosition Value**

```visualbasic
Private Sub TimerPlayback_Tick(sender As Object, e As EventArgs) Handles TimerPlayback.Tick
    If playbackManager IsNot Nothing AndAlso playbackManager.IsPlaying Then
        playbackManager.UpdatePosition()
        
        ' DIAGNOSTIC: Log position
        Dim pos = playbackManager.CurrentPosition
        Dim dur = playbackManager.TotalDuration
        Logger.Instance.Debug($"Position: {pos} / {dur}", "MainForm")
        
        transportControl.TrackPosition = pos
        transportControl.TrackDuration = dur
    End If
End Sub
```

**Expected:** Position should increment (00:00:01, 00:00:02, etc.)  
**If Zero:** CurrentPosition is not advancing  

---

### **Step 4: Check if TransportControl Updates**

Add logging to TransportControl property setter:

```visualbasic
' TransportControl.vb
Public Property TrackPosition As TimeSpan
    Get
        Return trackPos
    End Get
    Set(value As TimeSpan)
        trackPos = value
        Logger.Instance.Debug($"TrackPosition set to: {value}", "TransportControl")
        UpdateTrackFillRect()
        Me.Invalidate()
    End Set
End Property
```

**Expected:** Log should show "TrackPosition set to: XX:XX:XX" every tick  
**If not:** Property not being set  

---

### **Step 5: Check if Paint is Called**

```visualbasic
' TransportControl.vb
Protected Overrides Sub OnPaint(e As PaintEventArgs)
    Static paintCount As Integer = 0
    paintCount += 1
    If paintCount Mod 30 = 0 Then
        Logger.Instance.Debug($"OnPaint called {paintCount} times, trackPos={trackPos}", "TransportControl")
    End If
    
    ' ... rest of paint code ...
End Sub
```

**Expected:** OnPaint should be called frequently (60 FPS)  
**If not:** Control not repainting  

---

## ?? **Likely Causes (Ranked)**

### **1. Timer Not Starting** (Most Likely)
**Symptom:** No timer ticks in log  
**Cause:** `TimerPlayback.Start()` not being called or failing  
**Fix:** Check if `OnPlaybackStarted` is actually being called  

### **2. IsPlaying Returns False**
**Symptom:** Timer ticks, but condition fails  
**Cause:** Playback not actually started  
**Fix:** Check PlaybackEngine state  

### **3. CurrentPosition Not Advancing**
**Symptom:** Position stays at 00:00:00  
**Cause:** NAudio WaveOutEvent not playing  
**Fix:** Check NAudio initialization  

### **4. TransportControl Not Repainting**
**Symptom:** Position updates, but display doesn't change  
**Cause:** Invalidate() not working  
**Fix:** Force repaint with Refresh()  

### **5. Play Indicator State Issue**
**Symptom:** Time works, but LED doesn't light  
**Cause:** `currentState` not being set correctly  
**Fix:** Check `UpdateTransportState()` is called  

---

## ?? **Quick Fix to Try First**

**Instead of relying on timer, update directly in PlaybackStarted:**

```visualbasic
Private Sub OnPlaybackStarted(sender As Object, filepath As String)
    ' Start the timer
    TimerPlayback.Start()
    
    ' FIX Issue #2: Set transport state IMMEDIATELY
    transportControl.State = UI.TransportControl.TransportState.Playing
    
    ' FIX Issue #1: Set track duration IMMEDIATELY
    If playbackManager IsNot Nothing Then
        transportControl.TrackDuration = playbackManager.TotalDuration
        transportControl.TrackPosition = TimeSpan.Zero
        transportControl.Invalidate()  ' Force repaint
        transportControl.Refresh()     ' Force immediate update
    End If
    
    ' Log for diagnostics
    Logger.Instance.Info($"Playback started: {filepath}, Duration={playbackManager.TotalDuration}, IsPlaying={playbackManager.IsPlaying}", "MainForm")
    
    ' Update UI
    panelLED.BackColor = Color.RoyalBlue
    lblStatus.Text = $"Status: Playing {Path.GetFileName(filepath)}"
    btnStopPlayback.Enabled = True
End Sub
```

---

## ?? **User's Testing Steps**

1. **Clear the log file**
2. **Start app and play a file**
3. **Wait 10 seconds**
4. **Check log for:**
   - "Playback started: ..." line
   - "TimerPlayback_Tick fired!" lines (should be many)
   - "Position: ..." lines showing advancement
   - "TrackPosition set to: ..." lines
5. **Share log excerpt** showing these lines (or their absence)

---

## ?? **Expected Log Pattern (If Working)**

```
[INFO] Playback started: Take_20260115-001.wav, Duration=00:00:30, IsPlaying=True
[DEBUG] TimerPlayback_Tick fired! IsPlaying=True
[DEBUG] Position: 00:00:00 / 00:00:30
[DEBUG] TrackPosition set to: 00:00:00
[DEBUG] TimerPlayback_Tick fired! IsPlaying=True
[DEBUG] Position: 00:00:00.017 / 00:00:30
[DEBUG] TrackPosition set to: 00:00:00.017
[DEBUG] TimerPlayback_Tick fired! IsPlaying=True
[DEBUG] Position: 00:00:00.034 / 00:00:30
...
```

---

## ?? **If User Reports:**

### **"Timer never fires"**
? Problem: Timer.Start() not working  
? Fix: Check if timer is created, check if Start() is actually called  

### **"Timer fires but IsPlaying=False"**
? Problem: Playback not starting  
? Fix: Check PlaybackEngine.Play() is called  

### **"Position stays at 00:00:00"**
? Problem: NAudio not advancing  
? Fix: Check WaveOutEvent state  

### **"Everything updates in log but display doesn't change"**
? Problem: TransportControl paint issue  
? Fix: Force Refresh() instead of Invalidate()  

---

**Status:** ?? **READY FOR DIAGNOSTIC TESTING**  
**Next:** Add diagnostics, test, share log
