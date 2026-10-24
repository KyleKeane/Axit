# axProcess — Specification

axProcess is an app of the Axit bundle (`docs/axit/SPEC.md`): what runs on the computer, as lists, for a blind
developer who uses the NVDA screen reader. Built 2026-10-23 on the branch `axprocess` (the long run of that day,
`docs/axit/plan.md`), not yet heard under NVDA. The document is kept in step with the code (a behaviour change updates
it in the same commit). Requirements are numbered `PR-n.m` and decisions `PR-Dn`; the bundle's rules, AX-7
(accessibility), AX-8 (keys) and AX-9 (the Axit menu), are cited by number and not repeated. In axProcess's code and
documents, "SPEC.md" means this file.

## 1. Summary

Task Manager answers "what is using the memory", "what hangs", "what starts with Windows", but its lists are grids
that NVDA reads in part, and its columns move. axProcess is one window with one list and three views: the running
**processes** with their memory and processor use, the **services** with their state, and the programs that
**start** at sign-in. Arrows explore; Enter shows an item's details with what can be done to it; Delete ends a
process after a question. Nothing refreshes by itself: F5 reads the computer again (PR-D2).

## 2. Goals and non-goals

- **G1 The questions Task Manager answers.** Which process uses the most memory or processor time, end one that
  hangs, see which services run and start or stop one, see what starts at sign-in.
- **G2 One list, the facts in the words** (axGIT AG-D3): "chrome, 350 MB, 2% CPU, process 1234". A list box, which
  NVDA reads best; no grid, no columns.
- **G3 Nothing moves under the reader** (AX-7.3): the list changes on the user's F5 or command, never on a timer.
- **G4 Windows' own APIs, nothing else** (B-10): the processes through .NET, the services through the Service Control
  Manager by P/Invoke, the startup programs from the registry's Run keys and the Startup folders.
- **Non-goals.** Performance graphs, per-process network and disk use, GPU, users, the details of threads and
  handles, changing a process's priority or affinity, editing the startup list (read-only in the first version,
  PR-D4), anything that needs administrator rights to read. Several computers.

## 3. Scenarios

1. Start axProcess (Start menu, `axprocess`, the Axit menu): "This computer - axProcess window", then "Processes, 214
   list, chrome, 1.2 GB, 3% CPU, process 9120, 1 of 214": the list sorted by memory, the largest first.
2. Down to a program that hangs, Delete: the notice "End notepad (process 4410)? Anything unsaved in it is lost." with
   End process and Cancel; Enter: "Ended notepad"; the list is read again.
3. Ctrl+Q: "214 processes. Memory 9.1 of 15.9 GB in use, 57%. Processor 12%".
4. Ctrl+2: "Services, 280 list, Windows Update (wuauserv), running, manual, 1 of 280". Enter on a stopped one: its
   details with Start, and a "needs administrator rights" notice when Windows refuses.
5. Ctrl+3: "Startup, 9 list, OneDrive, enabled, current user's Run key". Enter: the command and where it is set.

## 4. Functional requirements

### PR-1 Launch

- PR-1.1 `Axit.exe process`; the console command `axprocess`; the Start menu entry "Axit axProcess". No File Explorer
  entry. Any argument is reported with the usage text (`AppHost`).
- PR-1.2 `AxProcessApp.Run(args)` is the entry point (AX-1.7): the host's log (`axprocess.log`), crash handling,
  `--help` and `--version`, the settings (`axprocess.json`), then `ProcessForm`.
- PR-1.3 axProcess runs as the user, never elevated. What Windows refuses to a user (ending a system process,
  starting most services) is said as such (PR-3.3, PR-4.3).

### PR-2 The window

- PR-2.1 From the top: the menu bar; the list, named with its view and count ("Processes, 214", "Services, 280",
  "Startup, 9"); the status bar. A notice takes the list's place while it shows (AX-6.4). The focus starts in the list.
- PR-2.2 Views: Ctrl+1 Processes, Ctrl+2 Services, Ctrl+3 Startup (View menu). The key says the list's name and the
  item the selection is on, also when the list already shows the view (axClaude D26). A view is read the first time it
  shows and on F5; each keeps its selection by the item's key (the process ID, the service's name, the startup entry's
  name and place) across a refresh, as axGIT's views do (AG-2.2).
- PR-2.3 F5 reads the view shown again, on a background task (AX-7.5), and says "Refreshed: <the list's name>".
  While a read runs the status bar says "Reading…" and a second F5 says "Working…".
- PR-2.4 The title is "This computer - axProcess". The status bar's left label is the list's name, the right one the
  memory in use ("Memory 57% in use"). No AccessibleName, no Spring (`StatusLayout`).
- PR-2.5 Ctrl+Q says the summary: the number of processes, the memory in use of the total and its percentage, and the
  processor's use over the last read ("214 processes. Memory 9.1 of 15.9 GB in use, 57%. Processor 12%").
- PR-2.6 Ctrl+C copies the selected item's line ("Copied"); Ctrl+F finds an item by its text, F3 and Shift+F3 the next
  and the previous, wrapping once (the bundle's find in a list).

### PR-3 Processes

- PR-3.1 Each process is "<name>, <memory>, <n>% CPU, process <id>": the name without `.exe`, the working set
  ("350 MB", "1.2 GB", "900 KB"), the processor use over one second, rounded to a whole percent of the whole computer
  ("0% CPU" for an idle one; left out for a process Windows does not let the user measure), and the process ID.
- PR-3.2 Sort by ▸ Memory (the default), Processor, Name (View menu; the choice is kept, PR-7.1). Memory and processor
  sort the largest first; ties go by name.
- PR-3.3 Delete, or Process → End process, asks "End <name> (process <id>)? Anything unsaved in it is lost." with End
  process and Cancel; End runs `Process.Kill` on a background task, says "Ended <name>" and reads the list again. End
  process tree (Process → End process tree) ends its child processes too, with the question "End <name> and the
  processes it started?". A process that is already gone says "<name> has already ended"; one Windows refuses says
  "Could not end <name>: access is denied. It may need administrator rights". axProcess's own process is refused:
  "That is this window: close it with Alt+F4".
- PR-3.4 Enter, or Process → Details, shows a notice titled with the process's name: the process ID, the memory, the
  processor use, the program file, the start time and the number of threads, each on its own line, those Windows lets
  the user read; with the buttons End process, Show program in axFile and Close (the default and the cancel button).
- PR-3.5 Ctrl+E, or Process → Show program in axFile, opens axFile on the program file's folder with the file selected
  (`AxitMenu.Start`); "Windows does not say where <name>'s program is" when it cannot be read.

### PR-4 Services

- PR-4.1 Each service is "<display name> (<name>), <state>, <start type>", the name once when the display name holds it already ("ActiveX Installer (AxInstSV)") or is it: the states running, stopped, starting,
  stopping, paused; the start types automatic, automatic (delayed), manual, disabled. Sorted by display name.
- PR-4.2 Enter, or Service → Details, shows a notice titled with the display name: the name, the state, the start
  type, the process ID while it runs, with the buttons Start (while stopped), Stop (while running and it accepts a stop),
  Restart (while running), Close.
- PR-4.3 Start, Stop and Restart go to the Service Control Manager on a background task and wait up to 30 seconds for
  the new state: "Started <display name>", "Stopped <display name>", "Restarted <display name>", "<display name> is
  still starting" (or "still stopping" for a stop) when the wait runs out. A refusal is "Could not start <display name>: access is denied. Services
  need administrator rights to start and stop"; other errors are Windows' own message.

### PR-5 Startup

- PR-5.1 The list holds what starts at sign-in from the five usual places: the Run key of the current user, the Run
  key of the computer and its 32-bit twin, the user's Startup folder and the Startup folder of all users. Each is
  "<name>, <enabled or disabled>, <place>"; the place is "current user's Run key", "computer's Run key", "computer's
  Run key (32-bit)", "your Startup folder", "all users' Startup folder". Sorted by name.
- PR-5.2 Disabled is what Task Manager's Startup tab set: the entry's value under the matching `StartupApproved` key
  has an odd first byte. Without such a value the entry is enabled.
- PR-5.3 Enter, or Startup → Details, shows a notice titled with the name: the command, the place, enabled or
  disabled, and the program file read from the command (the quoted path, or the text up to `.exe`); Show program in
  axFile (also Ctrl+E) opens it. The list is read-only (PR-D4).

### PR-6 Menus, keys, texts

- PR-6.1 Menus (AX-6): **Process (P):** Details Enter | End process… Delete | End process tree… | Show program in
  axFile Ctrl+E | Copy line Ctrl+C | ─ | Refresh F5 | Summary Ctrl+Q | ─ | Exit Alt+F4. **Service (S):** Details |
  Start | Stop | Restart. **View (V):** Processes Ctrl+1 | Services Ctrl+2 | Startup Ctrl+3 | ─ | Sort processes by ▸
  (Memory ✓ | Processor | Name). **Options (O):** Text ▸ | General ▸ | Developer ▸. **Axit (A)** (AX-9). **Help (H):**
  Keyboard shortcuts F1 | User guide | ─ | Windows resources ▸ (Task Manager | Services). Every item has a mnemonic
  (AX-7.4). The Startup view's Details and Show program are the Process menu's, which act on the view shown.
- PR-6.2 Keys (AX-8). Bundle-wide: F1, Ctrl+Shift+P, Ctrl+F, F3, Shift+F3, Ctrl+S ("Nothing to save here. F5
  refreshes, Delete ends a process"), Ctrl+Plus, Ctrl+Minus, Alt+F4. Window-specific (`AxProcessKeys`): Ctrl+1,
  Ctrl+2, Ctrl+3, F5, Enter, Delete, Ctrl+E, Ctrl+C, Ctrl+Q. Enter and Delete act while the list has the focus. A
  command of one view asked for in another says which view first: "Processes view first: Ctrl+1", "Services view first:
  Ctrl+2".
- PR-6.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axprocess/user-guide.md` is embedded (Help, User
  guide); About is the bundle's (AX-9.9).

### PR-7 Settings, announcements, log

- PR-7.1 `%APPDATA%\Axit\axprocess.json` (AX-5.3; `ProcessSettings`): `fontFamily`, `fontSize`, `fontBold`,
  `checkForUpdates`, `window`, `sortBy` (`memory`, `processor` or `name`).
- PR-7.2 Announcements, one UI Automation notification each (AX-7.3): the view's name with the selected item;
  "Refreshed: <name>"; "Working…"; the summary; "Ended <name>", "<name> has already ended", the refusals of PR-3.3;
  "Started", "Stopped", "Restarted <display name>", "<display name> is still starting" and the refusals of PR-4.3;
  "Sorted by memory", "Sorted by processor", "Sorted by name"; "Copied"; "Nothing selected"; "Processes view first:
  Ctrl+1", "Services view first: Ctrl+2"; "Not found" (find); the bundle's text size and diagnostics announcements.
  Nothing is spoken on an arrow key.
- PR-7.3 The log is `%LOCALAPPDATA%\Axit\logs\axprocess.log`: the start, the exit, every process ended and every
  service started or stopped by its name, and errors.

## 5. Architecture

```
src/AxProcess.Core/    class library (net10.0): ProcessTable (the samples, the processor use between two of them,
                       the rows and their order), ProcessRow, SizeText, ServiceManager (the Service Control Manager
                       by P/Invoke: the list, start, stop, the wait) with ServiceRow, StartupList (the Run keys and
                       the Startup folders) with StartupEntry and CommandLine (the program of a command),
                       SystemInfo (the memory, a process's program file and details, by P/Invoke) with
                       SystemTimes (the processors' busy share), ProcessSettings; references Axit.Core
src/AxProcess/         class library (net10.0-windows): AxProcessApp (Run, Usage), ProcessForm (the window),
                       AxProcessKeys, HelpText; references AxProcess.Core, Axit.Core, Axit.Forms
tests/AxProcess.Tests/ ProcessTableTests, ServiceAndStartupTests (labels, order, the processor arithmetic, the
                       command parsing, the StartupApproved bytes, and a read of this computer's lists)
docs/axprocess/        this spec, user-guide.md (embedded), nvda-test-plan.md
```

## 6. Design decisions

- **PR-D1 One list with views**, as axGIT (AG-D3): one control to learn, the view's name said on its key.
- **PR-D2 No refresh on a timer.** Task Manager's lists change every second, which moves what a screen reader is
  reading; F5 reads again on the user's word, and every command that changes something reads again after it.
- **PR-D3 The processor use over one second.** One reading of a process's processor time says nothing; two, a
  second apart, on a background task, give the share of the last second, which is what Task Manager shows.
- **PR-D4 The startup list read-only at first.** Turning an entry off writes the `StartupApproved` keys that Task
  Manager writes, and the computer's half needs administrator rights; reading is the need, and writing waits for the
  ask.

## 7. Open questions

- Grouping a program's processes into one item ("chrome, 12 processes, 1.2 GB").
- Turning a startup entry off and on (PR-D4).
- A filter field for the service list, which is long.

## 8. Status

- 2026-10-23: built on the branch `axprocess` (PR-1 to PR-7) with its tests; not yet run under NVDA. The test plan is
  `docs/axprocess/nvda-test-plan.md`.
