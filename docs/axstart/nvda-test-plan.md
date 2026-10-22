# AxStart — NVDA test plan

The manual checks of AxStart (`docs/axstart/SPEC.md`), run with NVDA before a release. AxStart needs no files of its
own: it reads the Start menu's programs. Write each pass into `docs/axit/nvda-test-record.md`, by step.

Before starting: install the build (`publish.ps1`), or quit any running AxStart (AxStart, Quit AxStart) and run
`.\run-axstart.ps1`. Have Notepad and Calculator installed (they are, on Windows 11).

## 1. Bringing it up and putting it away

1. In AxClaude, press Ctrl+Alt+Space: NVDA says "AxStart window, Search programs, edit" and nothing else.
2. Escape: AxStart is gone and AxClaude has the focus again, on the line it was on.
3. From File Explorer, then from the desktop (Windows+D): Ctrl+Alt+Space brings the field up each time, in front.
4. Ctrl+Alt+Space while the field shows: it hides.
5. Bring it up, Alt+Tab to another program: AxStart hides itself.
6. Bring it up, Alt+F4: it hides; Ctrl+Alt+Space brings it back (the process still runs).
7. Start menu, `Axit AxStart`, while AxStart runs: the field comes up; no second AxStart starts (Task Manager shows one `Axit.exe start`).

## 2. Typing and moving

1. Type `calc`: NVDA echoes the letters; after a pause, "Calculator, 1 of n".
2. Type `note`: "Notepad, 1 of n" (or the most started one). Down: the next match with its place. Up: back. Up at the first: the low note, nothing said.
3. Ctrl+End: the last match; Down there: the low note. Ctrl+Home: the first. Page Down and Page Up: a page of matches.
4. While on the second match, Backspace: the field loses a letter, NVDA reads it, and after the pause the first match of the new text is said. Left and Right move in the text; the caret was never in the list.
5. Type `xyz`: "No programs match xyz". Down: the low note.
6. Type `win power`: "Windows PowerShell" among the matches; `cafe` finds a name with "Café" if one is installed.
7. Escape with text: the field is emptied (NVDA reads it blank). Escape again: hidden.
8. Tab: the focus stays in the field. A click on the list: the focus comes back to the field.

## 3. Starting

1. `calc`, Enter: Calculator starts in front; AxStart is gone.
2. `notepad`, Enter: Notepad starts in front.
3. Bring it up, empty field: the most started programs first (Notepad and Calculator now lead).
4. `powershell`, Ctrl+Shift+Enter: Windows' administrator prompt; Yes: an elevated PowerShell. Again, No at the prompt: nothing said, AxStart gone.
5. `calc`, Ctrl+Shift+Enter: "Cannot start as administrator".
6. `notepad++` (or any desktop program), Ctrl+I: "<name>, desktop program, <path>". `calc`, Ctrl+I: "Calculator, Store app".
7. A desktop program, Ctrl+O: AxFile opens on its folder with the program file selected. A Store app, Ctrl+O: "No file location for <name>".
8. F5: "Programs refreshed, n programs".

## 4. The hotkey

1. Options, Hotkey: the notice says the key in use; the field says "Ctrl+Alt+Space".
2. Press A alone: "Use Ctrl, Alt or the Windows key with it". Press Shift+A: the same.
3. Press Windows+Alt+Space: the field says it and NVDA reads it. Enter: "Hotkey Windows+Alt+Space".
4. Hide; Windows+Alt+Space brings AxStart up; Ctrl+Alt+Space no longer does.
5. Options, Hotkey, a key another program holds (if one is known), Enter: "<key> is taken by another program"; the old key still works.
6. Options, Hotkey, Escape: the key in use is unchanged. Set it back to Ctrl+Alt+Space.

## 5. Sign-in and update

1. Options, General, Start with Windows is checked; sign out and in: Ctrl+Alt+Space works without starting anything.
2. Uncheck it, sign out and in: the hotkey does nothing until AxStart is started from the Start menu. Check it again.
3. Options, General, Say the first match while typing off: typing says nothing; Ctrl+I says the match. On again.
4. An update in place from another Axit window: afterwards Ctrl+Alt+Space still works (AxStart was stopped and started again hidden).
5. AxStart, Quit AxStart: the hotkey does nothing until AxStart is started again.
