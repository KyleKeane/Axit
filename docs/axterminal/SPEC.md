# AxTerminal — Specification

AxTerminal is an app of the Axit bundle (`docs/axit/SPEC.md`): a terminal for a shell, shown the way AxClaude shows
Claude Code, for a blind developer who uses the NVDA screen reader. Specified and built 2026-10-18, released in
3.0.0, and the whole test plan passed under NVDA on 2026-10-22 (the record: `docs/axit/nvda-test-record.md`; what
each release brought is section 8). The document is kept in step with the code (a behaviour change updates it in
the same commit). Requirements are numbered `AT-n.m` and decisions `AT-Dn`; the bundle's rules, AX-7
(accessibility), AX-8 (keys) and AX-9 (the Axit menu), and AxClaude's requirements (`docs/axclaude/SPEC.md`, cited as
"AxClaude FR-n.m" and "AxClaude Dn") are cited by number and not repeated. In AxTerminal's code and documents,
"SPEC.md" means this file.

## 1. Summary

AxClaude turned Claude Code's console into a window a screen reader reads well: what the console prints becomes a
line-by-line transcript in a read-only text view, the user types in a plain field at the bottom, each exchange gets
its markers, and the end of a turn is spoken. AxTerminal is the same window around a shell. The shell (PowerShell by
default) runs in a Windows pseudo console (ConPTY); every row it prints is a line of the transcript; a command typed in
the field goes to the shell on Enter and gets an exchange block, `# Input n` with the command and `# Output n` before
what it printed, so that `i`, `o`, Ctrl+3 and the heading keys work as they do in AxClaude; the shell's prompt rows
stay out of the transcript and show in the status bar; and the shell's integration marks tell the window when a
command is done and with what exit code, which is spoken with the chimes the user knows.

It is not a terminal emulator with a screen: programs that take the screen over (an editor, a pager, a monitor) are
out of scope; programs that print and ask are in. It works entirely offline (bundle G6).

## 2. Goals and non-goals

- **G1 AxClaude's window, learnt once.** The same field, view, quick keys, markers, chimes, ticks and speech options;
  a user of AxClaude knows AxTerminal on opening it.
- **G2 A command and its output as one block.** Every command is found again with `i`, its output with `o` and Ctrl+3,
  read down with the arrows, exported as Markdown.
- **G3 Prompts answered in place.** A program that asks (`y/n`, a passphrase, a choice) shows its question as lines;
  the field answers it; the window says that something asks.
- **G4 Shared, not copied.** The ConPTY host, the parser, the screen model, the transcript mirror and the view move
  from AxClaude into the shared projects on this, their second use (bundle AX-6.3, B-3); AxTerminal adds the shell's
  own model and nothing twice.
- **Non-goals.** The alternate screen and programs that redraw the whole screen (vim, nano, less, more, htop, a TUI
  over ssh); colours, mouse reporting, tabs and panes; the shell's own line editing, history recall and Tab completion
  inside the field (AT-D5, §7); running Claude Code (AxClaude's job, with its screen reader mode); elevation (start an
  elevated terminal from Windows). None of them is an error: a full screen program's rows are shown as rows, and
  Ctrl+Shift+C, or `q` and Enter, get out of it.

## 3. Scenarios

1. In AxClaude on a project, Alt+S, I, T: AxTerminal opens on the project folder with the field focused; the status
   bar reads `PS C:\src\myproject>`. Type `dotnet test`, Enter: "Sent", a tick per frame of output, then the done
   chime and "Done". Ctrl+3 lands on `# Output 1`; Down reads the output line by line.
2. A command fails: the chime and "Failed, exit code 1". Ctrl+3, then Ctrl+End and Up for the last lines.
3. `git push` asks for a passphrase: after a moment of quiet, the question chime and "Asks: Enter passphrase for
   key". Ctrl+Enter sends the field's text without recording it; "Sent, not recorded". The push goes on; "Done".
4. `npm start` runs on: lines arrive with their ticks; Ctrl+Shift+C: "Interrupt sent"; the prompt returns, "Done".
5. Ctrl+R: the window's recent commands as a list, the newest first; Enter puts `dotnet test` back in the field, Enter
   again runs it. Or `i` in the view finds `# Input 1`, `2` twice finds `## Command`, Down, and Enter on the command line copies it into the field.
6. `cd ..`: the title and the status bar follow the shell's directory. Alt+S, I, F: AxFile on that folder; Alt+S, I,
   G: AxGit on its repository.
7. In AxDown on `notes.md`, Alt+F, I, T: a terminal in the file's folder.

## 4. Functional requirements

### AT-1 Launch and the shell

- AT-1.1 `Axit.exe terminal [<folder>]`; the console command `axterminal`; the Start menu entry "Axit AxTerminal";
  AxTerminal under "Open with Axit" on folders in File Explorer (AX-2.3). A folder given alone to `Axit.exe` still
  means AxClaude (AX-1.2); AxTerminal is always named by its verb. Without a folder: the process's current folder
  (the Start menu entry starts it in the user's profile folder, as the shells do).
- AT-1.2 **The shell.** One of PowerShell 7 (`pwsh.exe`, found on the PATH or under `%ProgramFiles%\PowerShell\7`),
  Windows PowerShell (`powershell.exe`), Command Prompt (`cmd.exe`), Git Bash (Git for Windows' `Git\bin\bash.exe`
  under Program Files, Program Files (x86) or `%LOCALAPPDATA%\Programs`, or next to the `git.exe` on the PATH; never
  a `bash.exe` on the PATH, which in System32 is WSL's launcher; started `--login -i`, as Git's own shortcut does) or
  a custom command line; the default is PowerShell 7 when it is installed, otherwise Windows PowerShell. The choice
  is made in the New session notice and the Switch shell submenu (AT-1.6)
  and remembered in the settings (`shell`, `customShell`) for the next window. The shell starts in the folder with the
  window's environment plus `TERM=xterm-256color` for the programs that look, and `GIT_PAGER=cat` and `GH_PAGER=cat`,
  which switch git's and the GitHub CLI's pager off (AT-D7); nothing is taken out of the environment.
- AT-1.3 **Integration marks.** The window asks the shell to mark its prompt the way Windows Terminal's shell
  integration does (OSC 133 `A` the prompt starts, `B` the prompt ends and the command begins, `D;<exit code>` the
  previous command finished; OSC 9;9 the current directory), without touching the user's profile: PowerShell starts
  with `-NoLogo -NoExit -EncodedCommand` and a snippet that runs after the profile, keeps the user's `prompt` function and wraps its
  text in the marks (the shape Windows Terminal documents for oh-my-posh), written with `[char]27` so that Windows
  PowerShell takes it too, and sets `$ProgressPreference` to `SilentlyContinue`, since a progress bar drawn over the
  top rows of the screen would rewrite lines already read; Command Prompt starts with the `PROMPT` variable wrapping
  the current `%PROMPT%` in the same marks (cmd has no exit code to give); Git Bash starts with a `PROMPT_COMMAND`
  in its environment that prints `D` with the exit code (from the second prompt on, the first following no
  command), `A` and the directory as a Windows path, and appends `B` to `PS1` once the profile has set it, so the
  user's prompt text stays (a profile that sets `PROMPT_COMMAND` itself replaces it, and the shell then runs
  without marks, as a custom one). The parser reports the marks as events (an
  addition in the shared parser; AxClaude, whose Claude emits 133 marks too, goes on ignoring them). A custom shell
  gets no snippet and no marks unless its own prompt emits them; a shell started inside the shell (`pwsh` typed) has
  none until it exits. With a screen reader running, PowerShell turns PSReadLine off at startup and prints a warning
  line saying so, the first line of the transcript: the input is then the console host's own, with `>> ` continuation
  rows printed by the host, which is what the window wants (the recorded fixtures show it).
- AT-1.4 **State from the marks.** From Enter until the next `A` mark the window counts the command as running: the
  status bar says `Running: <the command's first line>` and the music plays when it is on (AT-5.5); the `D` mark's
  code decides between "Done" and "Failed, exit code n" (AT-5.1); the `A` mark's row is the prompt (AT-4.2); the 9;9
  directory and the shell's name make the title. Without marks the window has no state: the status's left label stays empty, nothing is
  announced at the end of a command, and the cursor row shows as a line.
- AT-1.5 `AxTerminalApp.Run(args)` is the entry point (AX-1.7), one call into `AppHost.Run` with the log name
  `axterminal`, the usage text, the settings (`axterminal.json`) and the window.
- AT-1.6 Session → **New session…** (Ctrl+N), the shape and the name of AxClaude's New session (AxClaude FR-8.6,
  D30): the notice "New session" with `Folder: C:\path` as its first line, "Choose the shell and the folder. Starting
  ends the current shell and starts a new session in the folder." and the key line, preceded by "<the command> is
  still running. A new session ends it." while one runs; the **Choose folder…** button (the folder picker); the group
  **Shell** with the radio buttons **PowerShell 7**, **Windows PowerShell**, **Command Prompt**, **Git Bash** and
  **Custom**, and, while Custom is selected, the field **Command line**; the buttons **Start session** (Enter) and
  **Cancel** (Escape). Start clears the transcript and gives the new shell a fresh screen, as a restart does (until
  2026-10-23 it drew on the old shell's rows), prints the system line `New session: PowerShell 7 in C:\path`,
  announces "New session in <folder name>" and remembers the folder and the shell. Session → **Switch shell ▸** lists
  the shells (PowerShell 7, Windows PowerShell, Command Prompt, Git Bash, the current one checked, then Custom…,
  which opens the New session notice): choosing one ends the shell and starts the chosen one in the current
  directory, keeping the transcript, with the system line `Switched to Git Bash in C:\path` and the shell's name
  announced; while a command runs, the notice "Switch the shell?" comes first ("Enter: Switch now. Escape: Keep it
  running."). The title names the shell (AT-2.3) and Ctrl+Q says it while idle (AT-D8), so the shell in hand is
  always one key away. Session → **Restart shell** (Ctrl+Shift+R) ends the shell and starts the same
  one again in its current directory, keeping the transcript, after the notice "Restart the shell?" while a command
  runs ("Enter: Restart now. Escape: Keep it running."); a shell that ended restarts at once. "Restarting" is
  announced.
- AT-1.7 **The shell ends** (`exit` typed, a crash): the system line `The shell ended with code 0`, the announcement
  "The shell ended", the status `Shell ended`; Enter in the field then keeps its text and announces "The shell ended.
  Ctrl+Shift+R starts it again" (AxClaude FR-2.1). Closing the window ends the shell and whatever runs in it; while a
  command runs the notice "A command is still running" asks first, with **Close anyway** (Enter) and **Keep running**
  (Escape), as AxClaude asks for Claude (AxClaude FR-1.7).
- AT-1.8 The console is 240 columns by 50 rows (`ptyColumns`, `ptyRows`; AxClaude D9), so that programs wrap little;
  the window never resizes it.

### AT-2 The window

- AT-2.1 From the top: the menu bar; the transcript, a read-only word-wrapped text named "Terminal", the same view as
  AxClaude's conversation (`TranscriptView`, AxClaude FR-3.1); the command field "Command", three rows high, in the
  place of AxClaude's message field; the status bar. A notice takes the place of the view and the field (AX-6.4); the
  recent commands list (AT-3.6) takes the field's place while it shows.
- AT-2.2 Focus: Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 toggle between the view and the bottom control
  (bundle AX-8.2); Ctrl+1 the field, Ctrl+2 the view, Ctrl+3 the view at the latest output marker, from anywhere, each
  naming the control when it has the focus already (AxClaude FR-6.5, D26). The focus starts in the field and stays
  there after sending.
- AT-2.3 The title is `<current directory's name> - <the shell's name> - AxTerminal`, the directory being the shell's (9;9) and the start
  folder before the first mark. The status bar's left label: `Idle`, `Running: dotnet test`, `Shell ended`,
  `Starting`; the right label: the shell's last prompt, the row's text between the `A` and `B` marks
  (`PS C:\src\myproject>`), or the full directory before the first prompt. No AccessibleName, no Spring (AxClaude
  §6.2). Ctrl+Q, from anywhere, says both with the time since the command started: "Running: dotnet test, 40 seconds,
  PS C:\src\myproject>" or "Idle, Windows PowerShell, PS C:\src\myproject>" (AT-D8), the shell named while idle.

### AT-3 The command field and the sending

- AT-3.1 **Enter** sends the field's text and clears it: each line as one write, a single `\r` as a separate write
  after it, paced as AxClaude's writes are (AxClaude FR-2.2, FR-2.5), 60 ms after each line and 120 ms after each
  `\r`, so that the shell reads a multi-line command line by line as it would from the keyboard, PowerShell's `>>` continuation taking
  the rest; for PowerShell a command of several lines gets one more Enter at the end, since with a screen reader
  running PowerShell turns PSReadLine off and its console host ends a block only on an empty line (with PSReadLine on
  the extra Enter is an empty prompt, which announces nothing). **Shift+Enter** is a new line in the field (AxClaude
  D11). An empty field sends a bare `\r` (a new prompt, or the default of a program's question). "Sent" is announced
  with the sent note (AT-5.1).
- AT-3.2 **The exchange block** (AxClaude FR-4.1): `# Input n`, then `## Folder` with the directory the command runs
  in on the line under it (the shell's current directory at the moment of sending: the 9;9 mark's, or the start
  folder while a shell gives no marks; left out when neither is known; AT-D9), then `## Command` with the command's
  lines as sent under it, a blank line, `# Output n`, a blank line, inserted at the end of the transcript above the
  trailing prompt row at the moment of sending; the `##` lines are level 2 headings, so `h` and `2` find them, and
  a jump speaks a heading without its marker, "Input 1 heading level 1" (AxClaude §6.4); *n* counts since the window started or the transcript was cleared; both markers are level 1 headings;
  Options → Text → Time stamps adds the time to both (AxClaude FR-4.6), and Options → Text → Markers in the text (AxClaude FR-4.6a) shows or hides the `#` markers of every heading line in the view, the lines, the keys and the export unchanged. The shell's echo of the command, the prompt
  row holding the command after its prompt, stays hidden: the block stands for it (AxClaude D22), and so do the
  continuation rows of a multi-line command, the shell's continuation prompt with the line after it (PowerShell's
  `>> `, bash's `> `, cmd's `More? `; a custom shell has none known, and its rows all stay). Every Enter is an
  exchange, also one that answers a running program (AT-D3).
- AT-3.3 **No send lock** (AxClaude D12): while a command runs, the text goes at once as input to the running program
  (an answer, or what the shell reads next), with its block; the status keeps the running command's name.
- AT-3.4 **Send without recording** (Ctrl+Enter in the field; Shell → Send without recording): the text goes as on
  Enter, with no block, no entry in the recent commands and nothing in the log; "Sent, not recorded"; while it runs,
  the status bar and Ctrl+Q say "a command sent without recording" in the command's place (until 2026-10-23 the
  command itself stood there). For passphrases and tokens that a program asks for (AT-D3).
- AT-3.5 **Keys to the shell.** Ctrl+Shift+C sends Ctrl+C, the interrupt ("Interrupt sent"; Shell → Interrupt);
  Shift+Escape sends Escape ("Escape sent"); Shell → Send key ▸ Up, Down, Ctrl+D, from the menu only. Plain Escape in
  the field sends nothing and announces "Shift+Escape sends Escape, Ctrl+Shift+C interrupts" (AxClaude D20). Nothing
  else typed reaches the shell before Enter.
- AT-3.6 **Recent commands** (Ctrl+R; Shell → Recent commands…): the window's last fifty commands, the newest first,
  as a list in the field's place (`ControlArea`, AxClaude D33, named "Recent commands"); Enter puts the chosen one in
  the field and focuses it, Escape brings the field back; "No recent commands" when there are none. Plain Up and Down
  in the field only move the caret (AxClaude D13, AT-D5). The list is the window's own and is not persisted (§7).
- AT-3.7 **Enter on a line of the view** copies the line's text into the field, after a new line when the field is
  not empty, moves the focus there with the caret at the end and announces "Line copied"; no quote markers (AT-D4).
  A joined row copies as one line; a marker line copies its text like any other.
- AT-3.8 Page Up, Page Down, Ctrl+Up and Ctrl+Down, with Shift, work in the field as in every edit control of the
  bundle (AX-8.2).

### AT-4 The transcript

- AT-4.1 Every row the shell prints is a line, trimmed on the right, committed once it scrolls off the screen and
  rewritable until then (AxClaude FR-3.2, §7.5); the classification is the shell's own (AT-4.2) and nothing of
  Claude's: no labels, no headings but the markers, no chrome patterns. A row that fills all 240 columns and is
  followed by a row with text is a hard wrap and is joined to it in the view (`joinWrappedLines`, AxClaude FR-3.2a).
  Every row with text is transcript, the rows below the cursor included (PSReadLine, which draws its completion
  menu there, is off while a screen reader runs).
- AT-4.2 **Prompt rows are hidden.** The row that holds an `A` mark is the prompt row: hidden while the shell is idle,
  when it is the cursor row with the prompt on it; hidden still once it holds the command after its `B` mark, the echo
  the block stands for (AT-3.2). Its text between the marks goes to the status bar (AT-2.3). A `D` mark comes before
  the prompt on the same row and is nothing by itself. Without marks nothing is hidden (AT-1.4).
- AT-4.3 **System lines** (`System: …`, found with `s`): the shell started (`Shell started: PowerShell 7 in C:\path`),
  ended, restarted, a new shell, the update's steps (the window base, AX-6.5), a recording started or stopped.
- AT-4.4 **Clear transcript** (Ctrl+L; Session → Clear transcript): empties the window, the recent commands
  staying; the count of the blocks starts again; "Cleared". The lines off the screen go; the lines still on the
  screen are hidden until the shell writes over their rows, since the shell goes on drawing on that screen. The
  shell's own `cls` or `clear` clears only its screen: the window lets the rows go first (the lines keep their text,
  the way AxClaude keeps a repainted conversation), so what was read stays and what the shell prints next goes on at
  the end; a program that homes the cursor to redraw the screen is treated the same, and prints its redraws as new
  lines (the alternate screen is a non-goal).
- AT-4.5 Bookmarks (`m`, `k`, Ctrl+Shift+K; AxClaude FR-3.9), `l` (the line number), Backspace (back from a jump,
  AxClaude FR-3.12), the reading breaks of a line being written (AxClaude FR-3.10), the selection that holds output
  back (AxClaude D29), Ctrl+C "Copied", Ctrl+A, find with Ctrl+F, F3 and Shift+F3: all the view's, unchanged.
- AT-4.6 The transcript is capped at 20 000 lines (`maxTranscriptLines`), trimmed from the top (AxClaude FR-3.6).
- AT-4.7 **Export transcript…** (Ctrl+S, the bottom of the Session menu before Exit; AxClaude FR-3.8): the file
  picker, the default name `axterminal-<yyyy-MM-dd-HHmm>.md` in the shell's current directory (the Documents folder
  when it is gone), the visible lines as text, "Exported". No git exclude of its own: AxClaude's (D34) is about the
  files AxClaude writes into the project it runs in; an exported transcript is the user's file where they put it.

### AT-5 Announcements and signals

- AT-5.1 **Done and Failed.** When the `A` mark follows a command: "Done" with the done chime when the last `D` mark
  carried `0` or no code (Command Prompt), "Failed, exit code 1" with the same chime when it carried another
  (PowerShell gives `-1` for an error of its own: "Failed"). Nothing for a bare Enter on an empty field: the shell
  gives a `D` without a code and no command was sent. "Sent" has the sent note; the chimes are AxClaude's
  (`soundOnBell`, AxClaude FR-7.3), the ready ping with "Ready" when the first prompt appears after a start.
- AT-5.2 **Tick** (`clickOnNewLine`, AxClaude FR-7.8): one tick for a frame that adds a visible line; no tool tick,
  since nothing here is a tool call.
- AT-5.3 **Speak output** (`outputSpeech`: `none` (default) or `all`; Options → Speech → Speak output: none | all
  lines, checked like a radio group): the lines that became final in a frame, as one notification (AxClaude FR-7.4,
  D32), the markers and the hidden rows left out. The default stays none (AxClaude D10); whether a terminal wants all
  lines by default is for the NVDA pass (§7).
- AT-5.4 **A program asks** (`announceQuestions`, default on; Options → Speech → Say when a program asks): while a
  command runs, when output has stopped for two seconds and the cursor row has text that ends in `:`, `?`, `]` or
  `)`, with or without a trailing space, "Asks: <the row's text>" is announced once for that text, with the question
  chime, the analog of "Claude asks" (AxClaude FR-7.2) without Claude's bell. A heuristic, kept until a better signal
  exists; off, the tick and the status are what tell.
- AT-5.5 AxClaude's attention options, unchanged in meaning: the taskbar flash when the window is not active
  (`flashTaskbar`), the louder background alert (`backgroundAlert`) and the working music while a command runs, with
  Ctrl+Space (`workingMusic`, AxClaude FR-7.9), all through the shared `Sounds`.
- AT-5.6 Nothing speaks on an arrow key; a jump announces its line; Ctrl+1 and Ctrl+2 name the control they land on
  (AX-7.3, AxClaude D26).

### AT-6 Menus, keys, texts

- AT-6.1 Menus, in the standard order of a first menu (AX-6): **Session (S):** New session… Ctrl+N | Open recent
  folder ▸ (ten, "1. name (path)", AX-9.5) | Switch shell ▸ (PowerShell 7 | Windows PowerShell | Command Prompt |
  Git Bash | Custom…) | ─ | Restart shell Ctrl+Shift+R | Clear transcript Ctrl+L | ─ | Say current folder Ctrl+P |
  Show current folder in ▸ (File Explorer Ctrl+W | AxClaude | AxGit | AxFile), on the shell's current directory | ─ |
  Export transcript… Ctrl+S | Exit Alt+F4. **Shell (E):** Interrupt Ctrl+Shift+C | Send Escape Shift+Escape | Send
  key ▸ (Up | Down | Ctrl+D (end of input)) | ─ | Recent commands… Ctrl+R | Send without recording (Ctrl+Enter in the
  command field) | ─ | What is running Ctrl+Q. **Navigate (N):** AxClaude's Navigate menu with the kinds the terminal
  has: Command field Ctrl+1 | Terminal Ctrl+2 | Latest output Ctrl+3 | ─ | Find… Ctrl+F | Find next F3 | Find
  previous Shift+F3 | ─ | Bookmark this line Ctrl+Shift+K | Next bookmark | Previous bookmark (the other quick keys
  are the terminal's letters, named in the F1 text). **Options (O):** Sounds ▸ (Chimes ✓ | Ticks for new lines ✓ |
  Background music while a command runs Ctrl+Space) | Speech ▸ (Speak output: none ○ | Speak output: all lines ○ |
  ─ | Say when a program asks ✓) | When a command is done ▸ (Flash the taskbar button ✓ | Louder alert when the window
  is in the background) | Text ▸ (Time stamps | Markers in the text ✓ | ─ | Font… | Larger text | Smaller text | Windows text size) | General
  ▸ (Check for updates at start ✓) | Developer ▸ (Open settings file | Record raw stream | Copy diagnostics). **Axit
  (A)** (AX-9). **Help (H):** Keyboard shortcuts F1 | User guide | ─ | Shell resources ▸ (PowerShell documentation |
  Windows Terminal shell integration | Bash manual). Every item has a mnemonic (AX-7.4).
  The first menu is Session, as in AxClaude, since what it holds is the shell in its folder, the thing the window
  is about; Shell takes E, S being Session's.
- AT-6.2 Keys (AX-8). Bundle-wide: F1, Ctrl+Shift+P (the command palette, AX-10), the control keys, Ctrl+F, F3,
  Shift+F3, Ctrl+S, Ctrl+Plus, Ctrl+Minus, Page Up, Page Down, Ctrl+Up and Ctrl+Down with Shift, Alt+F4. Window-specific (`AxTerminalKeys`): Ctrl+N, Ctrl+Shift+R,
  Ctrl+L, Ctrl+W, Ctrl+1, Ctrl+2, Ctrl+3, Ctrl+Shift+C, Shift+Escape, Ctrl+R, Ctrl+Q, Ctrl+P, Ctrl+Space, Ctrl+Shift+K; in
  the field Enter, Shift+Enter, Ctrl+Enter, Escape; in the view the quick keys `i`, `o`, `h`, `1` to `6`, `p`, `s`,
  `k` with Shift, `m`, `l`, Enter, Escape, Backspace, and Ctrl+Alt with `i`, `o`, `h`, `p`, `s`, `k`. AxClaude's `c`,
  `t`, `d`, `e`, `q` and `r` have no kind here and do nothing. The collision test covers the table and lists the keys
  AxClaude uses otherwise (Ctrl+N, Ctrl+R, Ctrl+L, Ctrl+Enter).
- AT-6.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axterminal/user-guide.md` is embedded (Help → User
  guide); About is the bundle's (AX-9.9).

### AT-7 Announcements

One UI Automation notification each, on the view, or on the notice or the list while one shows (AX-7.3): "Sent",
"Sent, not recorded", "Done", "Failed, exit code n", "Failed", "Ready", "Asks: <row>", "Interrupt sent", "Escape
sent", "Line copied", "No recent commands", "Shift+Escape sends Escape, Ctrl+Shift+C interrupts", "The shell ended",
"The shell ended. Ctrl+Shift+R starts it again", "Restarting", "New session in <folder>", the shell's name on Switch shell, "Cleared", "Exported",
"Copied", "Not found", "Line n of m", "Bookmark n added" and "Bookmark n removed", "Line gone", "No next input" and
the other jump results with the kind's name, "Command", "Terminal" and the other names when the control already has
the focus, "Running: …", "Idle, …", "Shell ended" and "No marks from this shell" on Ctrl+Q, the folder's full path on Ctrl+P, "Up sent", "Down sent"
and "Ctrl+D sent" (the Send key menu), "Folder <name>" (a recent folder), "Folder missing", "Speak output: none" and
"Speak output: all lines" (the choice made), "Background music on" and "off" (AxClaude FR-7.9), "Text size n",
"Windows text size", "Copied diagnostics", "Recording started" and "Recording stopped" (the system lines name the
file). The speech of output (AT-5.3) and the texts of the notices are the rest.

### AT-8 Settings, log, status

- AT-8.1 `%APPDATA%\Axit\axterminal.json` (AX-5.3; `TerminalSettings` on the bundle's `ConsoleSettings`, the console
  fields shared with AxClaude under the same names, on `WindowSettings`): `shell` (`pwsh`,
  `powershell`, `cmd`, `bash`, `custom`), `customShell`, `lastFolder`, `recentFolders` (ten), `ptyColumns` 240, `ptyRows` 50,
  `soundOnBell` on, `clickOnNewLine` on, `workingMusic` off, `backgroundAlert` off, `flashTaskbar` on, `outputSpeech`
  `none`, `announceQuestions` on, `markerTimeStamps` off, `showMarkers` on, `joinWrappedLines` on, `maxTranscriptLines` 20000, and the
  window base's font, update check and window fields. Unknown or out-of-range values fall back to the defaults.
- AT-8.2 The log is `%LOCALAPPDATA%\Axit\logs\axterminal.log`: the shell's start with its command line and folder,
  its exit code, restarts, errors; never a command or a line of output (a command can hold a secret). Options →
  Developer → Record raw stream writes the console stream to a `.vt` file in the PtyCapture format for a fixture
  (AxClaude FR-13), read back only through the redacting dump (bundle rule 6).

## 5. Architecture

```
src/Axit.Core/Pty, Vt, Transcript  from AxClaude.Core, moved on this second use (B-3) with the namespaces of their
                          folders, unchanged apart from one seam: Pty/PtyHost, Pty/StreamRecorder; Vt/VtParser,
                          Vt/Screen (gains the OSC 133 and 9;9 events, and PullRowText), Vt/Wcwidth; Transcript/Line,
                          LineKind, TranscriptMirror, Utterance; and LineStore, the part of SessionModel that holds the
                          lines (the list, CreateLine, Commit, Remove, TextChanged), which Screen writes to; SessionModel
                          keeps that part by deriving from it. Since 2026-10-23 LineStore also holds what both models
                          had copied: the UTF-8 decoder and Feed, the parser and the screen (NewScreen, to which each
                          model attaches its own handlers), EndFrame, the bookmarks, the cap and the Changed and Bell
                          events. Tested where they were (AxClaude's fixtures, the shell fixtures).
src/Axit.Forms/TranscriptView  from AxClaude, its quick-key table given by the window as data (the QuickKey records with
                          the window's keys) instead of read from AxClaudeKeys; AxClaude hands it its own table.
src/AxTerminal.Core/      class library (net10.0): ShellLauncher (find the shells, the command line with the prompt
                          snippet or the PROMPT variable, the environment), TerminalModel (on LineStore: frames to lines,
                          the prompt rows from the marks, the running state and the exit code, the exchange blocks, system
                          lines, bookmarks, the join flag, the cap; the parts of SessionModel that are not Claude's, written
                          again against the shell fixtures rather than carved out of it), CommandHistory, TerminalSettings;
                          references Axit.Core
src/AxTerminal/           class library (net10.0-windows): AxTerminalApp (Run, Usage), TerminalForm (the window, on
                          AxitForm, in partial files by section: the base's hooks and the key switch, the menus and the
                          status, the field and the sending, the process, what the model reports, New session, Switch shell and the
                          folders, find and export and recording), AxTerminalKeys (with Transcript, the view's keys as
                          data), HelpText (F1 text, the embedded guide); references AxTerminal.Core, Axit.Core, Axit.Forms
tests/AxTerminal.Tests/   fixtures recorded with tools/PtyCapture of powershell, cmd and bash under the launcher's command
                          line (a dir, a failing command, a y/n prompt, a multi-line command, cls, exit): golden
                          transcripts, the state at the end and the codes of the commands, the same transcript
                          whatever the chunk boundaries; TerminalModelTests on hand-written streams; ShellLauncherTests,
                          CommandHistoryTests, TerminalSettingsTests
docs/axterminal/          this spec, user-guide.md (embedded), nvda-test-plan.md
```

The data flow and the threading are AxClaude's (AxClaude §7.2, §7.3): the reader thread posts bursts to the UI
thread, which owns the parser, the screen and the model; a frame ends at `ESC[?25h` or after 100 ms of quiet; the view
applies the mirror's edits and maps the caret; writes go through the paced queue. The order of the work, one commit
each (bundle AX-6.3): the `LineStore` seam in `Screen` and `SessionModel` with AxClaude's tests green; the move of the
Core pieces; the parser's two events; `TranscriptView` to `Axit.Forms` with its table as data; `AxTerminal.Core` with
its fixtures; the window; the wiring, the installer and the documents (`adding-an-app.md`); AxClaude's spec §7.1 and
its notes record what moved.

## 6. Design decisions

- **AT-D1 AxClaude's window, not a terminal emulator.** A screen reader already has a terminal with a screen, Windows
  Terminal with NVDA's console support, and it reads badly: output scrolls under the cursor, nothing marks where a
  command's output begins, and a blind user reads the screen's rows rather than the program's lines. AxClaude's shape,
  a transcript of lines with markers around each exchange and a field of the app's own, is what the user asked for
  ("just like AxClaude"), and the pieces exist.
- **AT-D2 The shell's integration marks tell the state; no prompt regexes.** What a prompt looks like is the user's
  business, and guessing it from text is where terminal wrappers rot. The marks are a documented convention the shells
  already support through their prompt; the app injects its wrapper for the session without touching the user's
  profile; without marks the app degrades to a plain transcript rather than guessing. The `C` mark is not needed: the
  app sends the `\r` itself and knows when a command starts.
- **AT-D3 Every Enter is an exchange; Ctrl+Enter is the one that leaves no trace.** One rule for the record (the
  transcript is the log of what was typed and what came back, a program's questions and their answers included) and
  one key for the secrets. A separate "input to a running command" kind was considered and dropped: a second block
  shape to learn for the rare case.
- **AT-D4 Enter on a line copies the bare line.** AxClaude's quote markers exist so that Claude can tell the quoted
  line from the comment; a shell wants the command itself, to run or edit it.
- **AT-D5 No history on the arrows.** The bundle's rule (AxClaude D13: text appearing in a field on an arrow key
  confuses the reading) holds; Ctrl+R's list and `i` in the view are the history, on demand.
- **AT-D6 Shared by moving, one piece per commit.** The ConPTY host, the parser, the screen and the mirror are
  AxClaude's best-tested code, and the second use is the moment the bundle's rule names (B-3). The one seam,
  `LineStore`, is the smallest cut that frees `Screen` from `SessionModel`; the terminal's own model is written
  against recorded shell sessions rather than carved out of Claude's, so that neither app's behaviour depends on the
  other's.
- **AT-D7 No pager.** A pager takes the screen over, the one thing the window does not do; `GIT_PAGER=cat` and
  `GH_PAGER=cat` are the settings those two programs document for switching theirs off, and the user's profile can set
  them otherwise. Other pagers are left alone (`q` and Enter leave them).
- **AT-D8 Ctrl+Q says what the window is doing.** On demand, from anywhere, like AxClaude's Ctrl+Q for a question:
  the state, the command, how long, the prompt.
- **AT-D9 Every block names its folder.** The user asked for the directory a command ran in to be on hand in the
  transcript itself: the prompt rows that would say it are hidden, a `cd` between two commands leaves the output's
  place unknown when the text is read back or exported, and the status bar only has the directory of now. A `## Folder`
  heading with the path under it and a `## Command` heading over the command give the block the shape of a small
  document, which the heading keys walk and the export keeps; Ctrl+P (Session → Say current folder) says the directory of now, the full path, on
  demand.

## 7. Open questions

- Tab completion through the field: the field's text sent to the shell with a Tab, the completed line read back from
  the prompt row after the `B` mark and put into the field, a cheap round trip once the marks are there; not in
  version one.
- Running a program instead of a shell (`axterminal -- npm start`), a window per long-running process.
- Whether Speak output should default to all lines in a terminal (AxClaude's default is none).
- The "a program asks" heuristic (AT-5.4): whether two seconds and the four end characters are right, after the NVDA
  pass.
- A history that persists across windows (`CommandHistory` to the settings file; AT-3.4 keeps the secrets out of it).
- Whether a `D` mark with a non-zero code should classify the output's last lines as errors for `e`.
- PowerShell 7 is untested here (the machine has Windows PowerShell 5.1 only); its snippet is the same.

## 8. Status

- Planned 2026-10-18 at the user's ask, with AxFile (`docs/axfile/SPEC.md`), AxKeePassium and AxCryptomator
  (`docs/axit/plan.md`).
- Built the same day as a first draft, in the order of §5: the `LineStore` seam and the moves into the shared
  projects (AxClaude's tests unchanged), the marks as events, the view with its keys as data, `AxTerminal.Core` with
  the recorded sessions (Windows PowerShell 5.1 and cmd, then Git Bash in 3.0.0, `tests/fixtures/shell-*`), the window, the wiring. AT-1
  to AT-8 as described above, the git exclude left out (AT-4.7). Not yet heard under NVDA:
  `docs/axterminal/nvda-test-plan.md` is to run before a release names it more than a first draft.
- 2026-10-22: released in 3.3.1 (3.3.0 tagged, not published). The whole NVDA test plan marked passed at the user's word, no step reported failing; the record by step is `docs/axit/nvda-test-record.md`.
