# axTerminal user guide

axTerminal is a terminal for a blind developer who uses a screen reader: a shell (PowerShell by default) runs in a hidden console, and everything it prints comes into a plain text window, one line at a time, the way axClaude shows Claude Code. You type a command in a field at the bottom, press Enter, and the command and what it printed stand in the text under their own Input and Output lines, so that the keys you know from axClaude find them again. When a command is done you hear it, and whether it failed. It is one of the apps of Axit and shares its installation, updates and the Axit menu with them.

## What you need

- Windows 10 version 1809 or later, or Windows 11.
- NVDA, or another screen reader that reads ordinary Windows text fields.
- A shell: Windows PowerShell is part of Windows; PowerShell 7 is used when it is installed; Command Prompt is always there.

## Install

Axit's installer installs axTerminal with the other apps: see the Axit guide (Alt+A, About, Axit guide) or the README that came with the download. It adds the Start menu entry `Axit axTerminal`, the console command `axterminal` and axTerminal in the `Open with Axit` submenu on folders in File Explorer.

## Start

- Start menu: `Axit axTerminal`. Press the Windows key and type `axt`.
- Console: `axterminal`, or `axterminal C:\src\myproject` to start the shell in that folder.
- File Explorer: right-click a folder, open `Open with Axit` and choose axTerminal.
- From axClaude: Session, Show current folder in, axTerminal. From axDown or axJSON: File, Show current file in, axTerminal (the file's folder). From axGIT: Repository, Show current repository in, axTerminal. From axFile: Folder, Show current folder in, axTerminal. Each opens a new axTerminal window.

The window opens with the command field focused. When the shell's first prompt appears you hear "Ready" with a short ping. The status bar shows the state on the left (Idle, or Running with the command) and the shell's prompt on the right, which is where you are. The title is the name of the current folder and the shell. Ctrl+P says the current folder, the full path.

With a screen reader running, PowerShell turns its line editor (PSReadLine) off and prints a line saying so. That is expected: axTerminal's field is the editor.

## The window

From the top: the menu bar (Alt), the terminal (the text of everything so far), the command field (three lines), and the status bar.

- Ctrl+1 goes to the command field, Ctrl+2 to the terminal, Ctrl+3 to the latest output. Each says where you are when you are there already.
- Tab, Shift+Tab, Ctrl+Tab or F6 switch between the terminal and the field.

## Run a command

Type the command in the field and press Enter. You hear "Sent" with a low note. The terminal gets a `# Input 1` heading, `## Folder` with the folder the command runs in under it, `## Command` with your command under it, then `# Output 1`, and what the shell prints follows, with a short tick for each new line. When the prompt comes back you hear "Done" with the falling chime, or "Failed, exit code 1" when the command failed (PowerShell says "Failed" for an error of its own; Command Prompt gives no code). Ctrl+3 then takes you to the start of the output; read down with the arrow keys.

- Shift+Enter starts a new line in the field. A command of several lines goes to the shell line by line; for PowerShell one more Enter follows, which its console host wants to end a block.
- An empty field sends a plain Enter.
- Ctrl+Enter sends the text without recording it: no Input and Output lines, not in the recent commands, not in the log. Use it for a passphrase or a token a program asks for.
- Ctrl+R lists the recent commands of this window, newest first; Enter puts one back in the field, Escape goes back to the field. Ctrl+Up brings back the previous command and Ctrl+Down the next one, each with two quick rising notes; past the newest, Ctrl+Down gives back what you had typed, or the last command you changed and left. The plain arrows only move the caret.
- Enter on a line of the terminal copies that line into the field, to run it again or change it.
- Ctrl+Shift+C interrupts (it sends Ctrl+C to the shell). Shift+Escape sends Escape. The Shell menu's Send key sends Up, Down or Ctrl+D for a program that wants them.
- Escape in the field does nothing but remind you of those two keys.

A program that asks a question (y/n, a passphrase) prints it as a line; when the output has been quiet for a couple of seconds and the last line ends like a question, you hear "Asks:" and the line, with the question chime. Type the answer in the field and press Enter. Ctrl+Q says what is running, for how long, and the prompt, at any time.

## Read the terminal

The terminal is an ordinary read-only text: arrows, Home and End, Page Up and Page Down, Shift with them to select, Ctrl+C to copy ("Copied"). Nothing moves your caret while you read, and nothing is spoken on an arrow key. Single letters jump, Shift with the letter jumps back:

- i: the next Input line (a command you sent). o: the next Output line.
- h: the next heading (the Input and Output lines are headings). 1 to 6: a heading of that level.
- p: the next paragraph, the first line after a blank line. s: the next system line (the shell started, ended, a recording).
- m: bookmark the line (a "Bookmark 1" line appears above it); k: the next bookmark. Ctrl+Shift+K bookmarks from anywhere.
- l: the line number. Backspace: back to the line you jumped from.
- Ctrl+Alt with one of those letters reads every line of that kind from here to the end in one go; Control stops the reading.
- Ctrl+F finds text; F3 and Shift+F3 the next and previous match.
- Escape clears a selection, or goes to the field.

Options, Speech, Speak output: all lines, the default, reads each line as it arrives, joined into one utterance per burst. Speak output: none leaves only the ticks, and you read the output when you want.

## The shell

Session, New session (Ctrl+N) opens a notice with the folder, a Choose folder button, the shells (PowerShell 7, Windows PowerShell, Command Prompt, Git Bash, or Custom with a command line of your own) and Start session. Starting ends the current shell and starts the new one in the folder; the terminal is cleared. Session, Switch shell lists the shells: choosing one starts it in the same folder and keeps the text. The title names the shell you are in, and Ctrl+Q says it while nothing runs. Open recent folder does what New session does for a folder you had before. Restart shell (Ctrl+Shift+R) starts the same shell again in its current folder and keeps the text. While a command runs, all of them ask first.

The shell's prompt is not shown in the terminal: it is in the status bar, and the folder it names follows your `cd`. The prompt rows are recognised through marks the shell prints for terminals (the Windows Terminal convention), which axTerminal adds to PowerShell's prompt, to Command Prompt's PROMPT and to Git Bash's prompt command for the session; your profile is not touched. A shell of your own gives no marks unless its prompt prints them, and then the window shows every row and says nothing when a command ends.

`exit` ends the shell: a system line says so, and Ctrl+Shift+R starts it again. Closing the window ends the shell and whatever runs in it, after a question while a command runs.

Clear transcript (Ctrl+L) empties the window; the shell's own `cls` clears only its screen.

## Menus

Alt opens the menu bar. Session: new session, the recent folders, the recent folders in a new window (this one stays as it is), switch shell, restart, clear, say current folder (Ctrl+P), Show current folder in (File Explorer, axClaude, axGIT, axFile), export the transcript as a Markdown file (Ctrl+S), exit. Shell: interrupt, Escape, the keys, the recent commands, send without recording, what is running. Navigate: the field, the terminal, the latest output, find, the bookmarks. Options: Sounds (the chimes, the ticks, background music while a command runs, Ctrl+Space), Speech (speak output, say when a program asks), When a command is done (the taskbar flash, the louder alert for the background), Text (time stamps, markers in the text, the font and size), General, Developer. Axit: the bundle's menu. Help: the shortcuts (F1), this guide, shell resources.

## Settings

Every option in the menus is saved as you change it, in `%APPDATA%\Axit\axterminal.json`, which Options, Developer, Open settings file opens in axJSON. The console is 240 columns by 50 rows; `ptyColumns` and `ptyRows` in the file change it.

## Updates

Alt+A, Check for updates asks GitHub for a newer Axit; Update to installs it and starts axTerminal again with a new shell in the same folder. Options, General, Check for updates at start turns the check at start off.

## If something goes wrong

- "PowerShell 7 was not found": Ctrl+N and choose Windows PowerShell or Command Prompt, or install PowerShell 7.
- Nothing is spoken when a command ends: the shell gives no marks (a custom shell, or a shell started inside the shell). Ctrl+Q says what the window knows.
- A program took the whole screen (an editor, a pager): press q and Enter, or Ctrl+Shift+C. axTerminal shows such programs row by row and is not meant for them.
- Options, Developer, Record raw stream writes everything the shell prints to a file, for a bug report; Copy diagnostics copies the state and the last log lines. The log is `%LOCALAPPDATA%\Axit\logs\axterminal.log`; it never holds a command or a line of output.

## About axTerminal

axTerminal is part of Axit, made by Dr. Kyle Keane, www.kylekeane.com, free under the MIT licence. Alt+A, About has the version, the licence and the Axit guide.

## Disclaimer

The software is provided "as is" and "as available", without warranty of any kind, express or implied. The developer has no obligation to provide support, maintenance, updates or corrections, and, to the fullest extent permitted by law, is not liable for any claim, damages or other liability arising from or in connection with the software or its use, including loss of data or work and anything a command run through it does. You use axTerminal at your own risk.
