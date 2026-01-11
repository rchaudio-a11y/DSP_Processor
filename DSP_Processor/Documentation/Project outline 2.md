**Updating outline structure**

I need to update the outline to include both global pre/post HP/LP for unused bands. This will keep things concise, but with enough structure. It'll require a little tweaking for flow and clarity. I don't want to overcomplicate it, but I also want to make sure I don’t miss any key points. It's essential there’s a balance between being too brief and too detailed. Hopefully, it’ll feel organized without redundancy. Let me figure out how to slot these additions in.
You’re right to catch that — those pre/post rolloff filters are important, both sonically and architecturally. Let’s fold them in cleanly so they’re first‑class citizens in the spec, not an afterthought.

I’ll update the outline with a focus on:

- Where the **Pre Low/High Pass** live in the chain  
- How they relate to the multiband crossovers  
- How they’re exposed in the UI  
- How they interact with “unused” bands

---

## 1. Updated high‑level signal flow (with pre/post filters)

Full chain with the global rolloff filters included:

```text
Audio In
    ↓
PreFilters (Global Low Cut / High Cut)
    ↓
CrossoverFilterBank (5 bands)
    ↓
BandProcessor[1..5] (EQ + Dynamics + Delay + Mix)
    ↓
BandMixer (Sum + Output Gain)
    ↓
PostFilters (Optional final Low Cut / High Cut for cleanup)
    ↓
Audio Out
```

So:

- **PreFilters**: roll off “unwanted” extremes before they even hit the multiband engine  
- **PostFilters**: optional cleanup / safety rolloff after band summing

---

## 2. PreFilters module (global low/high cut)

### Responsibilities
- Remove unused or problematic extremes **before** multiband splitting  
- Reduce workload on crossover filters  
- Avoid LF rumble / HF hash from ever exciting the bands  

### Typical use
- Low Cut: remove sub‑bass (e.g., 20–40 Hz)  
- High Cut: remove ultrasonic content (e.g., 18–22 kHz)

### Parameters
- **Pre Low Cut**
  - Frequency  
  - Slope (12/24/36/48 dB per octave)  
  - On/Off  
- **Pre High Cut**
  - Frequency  
  - Slope  
  - On/Off  

### Internal
- Implemented as **biquad HP/LP**  
- Can reuse the same Biquad / ParametricEQ core with dedicated modes  

---

## 3. PostFilters module (optional final rolloff)

### Responsibilities
- Clean up any residual aliasing / ringing from the multiband processing  
- Act as a master safety filter  
- Optionally shape the overall tone (e.g., darker/warmer top end, tighter low end)

### Parameters
- **Post Low Cut**
  - Frequency  
  - Slope  
  - On/Off  
- **Post High Cut**
  - Frequency  
  - Slope  
  - On/Off  

These can be subtle — more like mastering filters than aggressive sculpting tools.

---

## 4. How “unused bands” are handled

You mentioned rolloff of unused audio bands — here’s how that fits in conceptually.

There are two layers of control:

### 4.1 Via PreFilters (global)
If you know you **don’t want** certain ranges at all:

- High‑pass above sub‑bass  
- Low‑pass below ultra‑highs  

This prevents those ranges from entering any band at all.

### 4.2 Via band bypass / mute (per band)
At the band level:

- **Band Bypass**: band passes through unprocessed  
- **Band Mute**: band is removed from the mix entirely  

So, for example:

- Use PreFilters to kill “everything below 40 Hz” and “above 18 kHz”  
- Use band mute to disable specific mid bands if you don’t want them contributing

---

## 5. Updated multiband module list (with filters explicit)

### Core modules

1. **PreFilters**
   - Global low cut / high cut
   - Biquad HP/LP

2. **CrossoverFilterBank**
   - 4 crossovers → 5 bands
   - Constant‑Q behavior
   - Phase‑aware design

3. **BandProcessor (x5)**
   - Parametric EQ
   - Dynamics (Comp/Exp/Gate)
   - Gain
   - Delay
   - Dry/Wet
   - Mute / Bypass

4. **BandMixer**
   - Sum bands
   - Prevent clipping
   - Output gain

5. **PostFilters**
   - Optional global low cut / high cut
   - Final tone/cleanup

---

## 6. UI representation of pre/post filters

### Global filters section

Add a **Global Filters** strip at the top of the multiband UI:

- **Pre Section:**
  - Low Cut: Freq + Slope + On/Off  
  - High Cut: Freq + Slope + On/Off  

- **Post Section:**
  - Low Cut: Freq + Slope + On/Off  
  - High Cut: Freq + Slope + On/Off  

On the **global spectrum / crossover graph**, you can visualize:

- PreFilters as faint “shaded” regions at the extreme ends  
- Crossover points as draggable nodes in the remaining usable band

That makes it visually obvious which ranges are “discarded” versus “actively processed.”

---

## 7. Implementation order update

When you actually build this, the new order should be:

1. **Biquad core**
2. **PreFilters (HP/LP)**
3. **CrossoverFilterBank**
4. **ASRD envelope**
5. **DynamicsProcessor**
6. **ParametricEQ**
7. **BandProcessor**
8. **BandMixer**
9. **PostFilters**
10. **UI wiring / visualization**

That ensures your global rolloff is foundational and doesn’t get bolted on later as a hack.

---

If you want, next step we can:

- Sketch a **concrete class list** for these filters  
- Define **exact parameter ranges** (freq/slope defaults, safe limits)  
- Design the **Global Filters UI block** with the same visual language as your current app.
