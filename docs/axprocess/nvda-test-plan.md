# axProcess NVDA test plan

Run with NVDA before a release, after `run-axprocess.ps1` (or `.\run.ps1 -App process`). Start Notepad first, to have a process to end. Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axprocess/SPEC.md` (PR-n.m).

## 1. Start (PR-1, PR-2)

1. Start axProcess: NVDA reads "This computer - axProcess window", then the list; about a second later "Processes, N. <first item>", not interrupting. Nothing else is spoken.
2. Down Arrow: the next item alone, "<name>, <memory>, <n>% CPU, process <id>". The list is largest memory first.
3. NVDA+End: the status bar: "Processes, N" and "Memory N% in use".
4. Ctrl+Q: "N processes. Memory x of y GB in use, z%. Processor n%".
5. Wait ten seconds without a key: nothing is spoken and the item under the cursor stays the same.
6. F5: "Refreshed: Processes, N"; the selection is on the same process.
7. Home, then Up: the low edge sound, nothing spoken. End, then Down: the same.
8. Tab: "Services, N. <service>"; Tab: "Startup, N. <entry>"; Tab: "Processes, N. <process>". Shift+Tab: "Startup, …".
9. On a process, Shift+F10: the Process menu opens and NVDA reads "Details Enter". Escape. Ctrl+2, Shift+F10: the Service menu; Ctrl+3, Shift+F10: the Startup menu. The Applications key does the same.
9a. In the Processes view, Alt, then Right along the bar: Computer, Process, View, Options, Axit, Help; Service and Startup are passed over. Ctrl+2, Alt, Right: Computer, Service, View…
10. Close axProcess, then in another Axit window Alt+A, R: "1. axProcess" is in Recently closed; Enter starts it.
7. Alt+F4 closes the window without a question.

## 2. Processes (PR-3)

1. View, Sort processes by, Name: "Sorted by name"; the list is in the alphabet's order on the item it had. Sort by Processor: "Sorted by processor". Back to Memory.
2. Ctrl+F, type `notepad`, Enter: the Notepad item selected and read.
3. Enter: a notice titled "Notepad" with "Process ID …", "Memory …", "Program C:\Windows\…\notepad.exe", "Started …", "Threads …"; the buttons End process, Show program in axFile, Close; Escape closes it.
4. Ctrl+E on Notepad: axFile opens on its folder with notepad.exe selected.
5. Delete on Notepad: the notice "End Notepad (process N)?" with "Anything unsaved in it is lost."; Escape: nothing happens. Delete, Enter: "Ended Notepad"; the list no longer holds it.
6. Delete on a system process (System, or svchost): the question, then Enter: "Could not end …: access is denied. It may need administrator rights".
7. Find axProcess's own item (Ctrl+F, `Axit`; the one whose ID is this window's), Delete: "That is this window: close it with Alt+F4" if it is this process, otherwise the question (another Axit window).
8. Ctrl+C: "Copied"; paste in axDown: the line.

## 3. Services (PR-4)

1. Ctrl+2: "Services, N. <first service>" after a moment. Down: "<display name> (<name>), running, automatic" and the like.
2. Enter on a running service: its details with Stop, Restart, Close. Stop: "Could not stop …: access is denied. Services need administrator rights to start and stop" (unless axProcess runs elevated).
3. Delete in the Services view: "Processes view first: Ctrl+1". Ctrl+E: the same.
4. Ctrl+2 again: "Services, N. <item>"; Ctrl+1: back on the process you had.

## 4. Startup (PR-5)

1. Ctrl+3: "Startup, N. <first entry>". Down: "<name>, enabled, current user's Run key" and the like.
2. Enter: the details, "Command …", "Set in …", "Enabled", "Program …"; Show program in axFile opens axFile on it.
3. An entry turned off in Task Manager's Startup apps reads "disabled".

## 5. Text, notices, the Axit menu (PR-6)

1. Ctrl+S: "Nothing to save here. F5 refreshes, Delete ends a process".
2. Ctrl+Plus: "Text size 13" (or the next size); Ctrl+Minus back.
3. F1: the Keyboard shortcuts notice; Escape. Help, User guide: this guide; Escape.
4. Alt+A: the Axit menu; About, Version names axProcess among the apps.
5. Ctrl+Shift+P, type `summary`, Enter: the summary is said.
