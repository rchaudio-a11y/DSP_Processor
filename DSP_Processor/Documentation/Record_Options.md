🎛️ Recording Mode Panel (UI Outline)
1. Mode Selection (CheckBoxes or RadioButtons)
Only one mode should be active at a time.
• 	[ ] Manual Record
• 	[ ] Timed Record
• 	[ ] Loop Record
When a mode is selected:
• 	The other modes disable
• 	The relevant options panel becomes visible

⏱️ 2. Timed Record Options Panel
Visible only when Timed Record is selected.
Controls
• 	Label: “Record Duration (seconds):”
• 	TextBox: 
• 	Label: “Total Time:”
• 	Label:  (calculated live)
Behavior
• 	User enters duration in seconds
• 	 simply mirrors the duration
(because timed record = one take)

🔁 3. Loop Record Options Panel
Visible only when Loop Record is selected.
Controls
• 	Label: “Duration per Take (seconds):”
• 	TextBox: 
• 	Label: “Number of Loops:”
• 	TextBox: 
• 	Label: “Total Recording Time:”
• 	Label: 
(auto‑calculated: duration × loops)
• 	CheckBox: “Infinite Loop Mode”
• 	When checked:
• 	 is disabled
• 	 displays “∞ (until stopped)”
Behavior
• 	When user changes duration or loop count:
• 	 updates instantly
• 	If Infinite Loop Mode is checked:
• 	Loop count is ignored
• 	Total time shows infinity

🗂️ 4. File Naming Panel
Visible for all modes.
Controls
• 	Label: “File Naming Template:”
• 	TextBox: 
• 	Label: “Preview:”
• 	Label: 
Behavior
• 	As user types template, preview updates
• 	Template supports tokens:
• 	Take Number: {take}
• 	Date: {date}
• 	time: {time}
• 	duration: {duration}
• 	mode
• 	Index
• 	Custom
• 	