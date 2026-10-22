# AxStart — Specification

AxStart is an app of the Axit bundle (`docs/axit/SPEC.md`): a program launcher for a blind developer who uses the
NVDA screen reader. Specified 2026-10-22 at the user's ask and not yet built: "the windows launcher has become very
bloated and i want a quick global key i hit to then search for a program to launch". The document is kept in step with
the code once it exists (a behaviour change updates it in the same commit). Requirements are numbered `AS-n.m` and
decisions `AS-Dn`; the bundle's rules, AX-7 (accessibility), AX-8 (keys) and AX-9 (the Axit menu), are cited by
number and not repeated. In AxStart's code and documents, "SPEC.md" means this file.

## 1. Summary

One key from anywhere, Ctrl+Shift+Space unless the user chose another (AS-4.1), brings up a small window with one search
field. Typing narrows the list of installed programs to the names that match; Up and Down move through the matches
while the caret stays in the field, so that typing and Backspace go on working; Enter starts the program and the window
goes away. The programs are the ones the Start menu has: the desktop programs and the Store apps (Settings, Calculator)
alike. AxStart runs in the background from sign-in, with no window until the key is pressed and nothing in the
taskbar. It works entirely offline (bundle G6).

## 2. Goals and non-goals

- **G1 One key, one field.** From any program, the hotkey puts the focus in the search field with nothing typed;
  NVDA says "AxStart window, Search programs, edit". Nothing else is in the way.
- **G2 The caret stays in the field.** Up, Down, Page Up, Page Down, Home and End with Ctrl move through the matches;
  the field keeps every editing key (AS-D2).
- **G3 Fast.** The list is in memory when the key is pressed; a match is said before the next key is typed.
- **G4 What the Start menu has, nothing more.** No web results, no files, no settings pages, no suggestions.
- **G5 Nothing spoken unprompted** (AX-7.3) beyond the match after typing, which is the answer to the typing (AS-3.2).
- **Non-goals.** Files and folders (AxFile has them), web search, a calculator in the field, commands with arguments
  (AxTerminal), pinning or groups, a tray icon (§9), a taskbar button while hidden.

## 3. Scenarios

1. In AxClaude, Ctrl+Shift+Space: "AxStart window, Search programs, edit, blank". `calc`: NVDA echoes the letters, and
   after the typing pauses, "Calculator, 1 of 2". Enter: Calculator starts and is in front; AxStart is gone.
2. Ctrl+Shift+Space, `note`: "Notepad, 1 of 3". Down: "Notepad++, 2 of 3". Down: "OneNote, 3 of 3". Down: the edge
   earcon (AX-7, `Sounds.Edge`). Backspace twice: "Notepad, 1 of 9" after the pause, the selection back on the first.
3. Ctrl+Shift+Space, `xyz`: "No programs match xyz". Escape: AxStart is gone and the program from before has the focus
   again, its own place kept.
4. Ctrl+Shift+Space, `power`, Down to "Windows PowerShell", Ctrl+Shift+Enter: Windows asks for administrator rights
   (its own prompt) and PowerShell starts elevated.
5. Options → Hotkey…: "Press the new key"; the user presses Ctrl+Alt+K: the field says "Ctrl+Alt+K";
   Enter: "Hotkey Ctrl+Alt+K". From then on that key brings AxStart up and Ctrl+Shift+Space is free again.
6. A program installed a minute ago: Ctrl+Shift+Space, its name: it is there, since the list is read again on each
   showing, in the background (AS-2.1).

## 4. Functional requirements

### AS-1 Launch, the background process, the hand-over

- AS-1.1 `Axit.exe start [--hidden]`: the background process, the window shown at once unless `--hidden` (the
  sign-in start). Start menu entry `Axit AxStart`, console command `axstart`, the Axit menu's New window ▸ AxStart
  (AX-9). No path is taken; AxStart is not in File Explorer's Open with Axit submenu.
- AS-1.2 **One AxStart.** A start while AxStart runs hands over to the running one, which shows its window, and ends
  (the bundle's `AppMessage`, `docs/axmedia/SPEC.md` AM-1.3, with a new verb `show`); with `--hidden` it ends without
  showing anything.
- AS-1.3 **Start with Windows** (Options → General, a setting, on by default): the value `Axit AxStart` under the
  user's Run key (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`), `"<installed Axit.exe>" start --hidden`,
  written or removed when the setting flips and checked at each start; only the installed program writes it, naming
  itself, so a development build never puts itself, or an older installed copy that does not know `start`, into the
  sign-in. The installer stops AxStart before an install or a removal
  and starts it hidden after an install or an update unless the setting is off (AX-2.3); the removal deletes the
  value.
- AS-1.4 **Quit AxStart** (AxStart menu) ends the process and frees the hotkey until the next sign-in or start. Alt+F4
  and Escape only hide the window (AS-3.4); Close all windows (AX-9.8) hides it too.
- AS-1.5 An update in place (AX-3.2) replaces the program file while AxStart runs: the installer stops the background
  process (told by its verb, `start`, on its command line) and starts the new version hidden (AS-1.3).

### AS-2 The programs

- AS-2.1 **The list** is the shell's Applications folder (`shell:AppsFolder`), which holds what the Start menu's All
  apps list holds: the shortcuts of both Start menu folders and the packaged apps, each with its display name and its
  parsing name (a path or an application user model id). Read on a background thread at start and again each time
  the window shows; the window searches the list it has meanwhile and swaps in the new one when it arrives, keeping
  the selection by name. Names that occur twice (the same program in the user's and the machine's Start menu) are
  one entry. Uninstallers and help files the folder holds ("Uninstall …", "… Help", "… Readme") stay, since they are
  what the Start menu shows (§9).
- AS-2.2 **Matching**, in `AxStart.Core` and tested: the typed text, trimmed, against each name, ignoring case and
  accents; a name matches when it contains every word of the text (`win power` matches "Windows PowerShell"). The
  order: a name that starts with the text first, then one whose word starts with it, then any other match; within
  each (placed by the text's first word, so that `win power` sits with the names starting with `win`), the programs
  started from AxStart more often first, then by name in File Explorer's order
  (`Axit.Core/NaturalOrder`). An empty field matches every program, the most started first.
- AS-2.3 **Starting** (Enter): the selected match, or the first when the selection was never moved, through the shell
  (`ShellExecuteEx` on `shell:AppsFolder\<parsing name>`), so that a packaged app starts as the Start menu starts it;
  the window hides first, so that the program comes up in front. Ctrl+Shift+Enter starts it as administrator (the
  verb `runas`; Windows' own prompt); a packaged app that cannot be elevated says "Cannot start as administrator".
  A start that fails says "Could not start <name>: <Windows' message>" in the window, which stays. Each start counts
  one for the program's name (AS-5.1).
- AS-2.4 **Open file location** (Ctrl+O) on a desktop program: AxFile on the shortcut's target's folder, the target
  selected; on a packaged app, "No file location for <name>".

### AS-3 The window

- AS-3.1 One field named "Search programs" with the caret in it, and under it a list of the matches named "Programs,
  n" that never takes the focus (Tab and the mouse leave the focus in the field; AS-D2); the list is there for the
  eyes and for NVDA's object navigation. A status bar with "n programs" on the left. The window is small, centred on
  the screen with the focus, top-most while shown, without a taskbar button, and its title is "AxStart". Before the first read of the programs
  has come back the list's one line is "Reading the programs".
- AS-3.2 **Typing** refills the list with the matches of the new text and selects the first; 300 ms after the last
  change, one notification says the first match and the count, "Calculator, 1 of 2", or "No programs match xyz";
  with an empty field nothing is said. Options → General → Say the first match while typing (on) turns this off, and
  then Ctrl+I says it on demand.
- AS-3.3 **The keys in the field** (AS-D2): Down and Up move the selection one match and say it, "Notepad++, 2 of 3";
  Page Down and Page Up a page of matches; Ctrl+Home and Ctrl+End the first and the last; at an end the edge earcon
  and nothing said. Left, Right, Home, End, Backspace, Delete, Ctrl+Backspace, Shift with the arrows, Ctrl+A, the
  clipboard keys and the field's own undo stay the field's. Enter starts (AS-2.3); Ctrl+I says the selected match and
  its kind and place, "Notepad++, desktop program, C:\Program Files\Notepad++\notepad++.exe" or "Calculator, Store
  app".
- AS-3.4 **Escape** hides the window at once, whatever the field holds (the user's ask, 2026-10-22: it first cleared
  a field with text and hid only an empty one); Alt+F4 hides it the same way; Backspace and Ctrl+A clear the text. Hiding gives the focus back to the
  window that had it before the hotkey (Windows does this when the window hides). The text is empty each time the
  window shows; the selection starts on the first match.
- AS-3.5 The window hides by itself when it loses the focus to another program (Alt+Tab, a click elsewhere), as the
  Start menu does; a notice of its own (Hotkey…, Keyboard shortcuts, About) keeps it.

### AS-4 The hotkey

- AS-4.1 One system-wide key, `RegisterHotKey` on the background process's own window, Ctrl+Shift+Space unless the
  user set another. Pressed while AxStart is hidden, it shows the window in front with the focus in the field (the
  process that receives a hotkey may take the foreground); pressed while the window shows, it hides it.
- AS-4.2 **Options → Hotkey…**: a notice "Hotkey" with one field, "Press the new key", that takes the next key
  pressed with its modifiers and shows it as a menu would ("Ctrl+Alt+K"); a key without Ctrl, Alt or the
  Windows key, or Escape, Enter or Tab alone, is not taken ("Use Ctrl, Alt or the Windows key with it"). Enter
  saves: the old key unregistered, the new one registered, "Hotkey Ctrl+Alt+K"; a key another program holds
  says "Ctrl+Alt+K is taken by another program" and the old key stays. Escape keeps the old key.
- AS-4.3 A hotkey that cannot be registered at start (another program took it) shows the window once with the
  notice "The hotkey Ctrl+Shift+Space is taken by another program. Options, Hotkey, to choose another.", so that the
  user is never left with a launcher that cannot be reached.

### AS-5 Settings, menus, keys, texts

- AS-5.1 `%APPDATA%\Axit\axstart.json` (AX-5.3): `hotkey` (the key as text, "Ctrl+Shift+Space"), `startWithWindows`
  (on), `sayFirstMatch` (on), `starts` (each program's display name and how often AxStart started it, for the order),
  and the window base's font, update check and window fields (the place is not used: the window is centred).
- AS-5.2 Menus (AX-6): **AxStart (S):** Start Enter | Start as administrator Ctrl+Shift+Enter | Open file location
  Ctrl+O | ─ | Refresh programs F5 | ─ | Hide Escape | Quit AxStart. **Options (O):** Hotkey… | Text ▸ (Font… |
  Larger text | Smaller text | Windows text size) | General ▸ (Start with Windows ✓ | Say the first match while typing
  ✓ | Check for updates at start ✓) | Developer ▸ (Open settings file | Copy diagnostics). **Axit (A)** (AX-9).
  **Help (H):** Keyboard shortcuts F1 | User guide. Every item has a mnemonic (AX-7.4).
- AS-5.3 Keys (AX-8). Bundle-wide: F1, Ctrl+Plus, Ctrl+Minus, Alt+F4 (hides, AS-3.4). Window-specific
  (`AxStartKeys`): Up, Down, Page Up, Page Down, Ctrl+Home, Ctrl+End (in the field, AS-3.3), Enter,
  Ctrl+Shift+Enter, Ctrl+O, Ctrl+I, F5, Escape. The hotkey is a setting, not in a table; the collision test covers
  the table, and Hotkey… refuses a key of the table only when it has no Ctrl, Alt or Windows key (AS-4.2).
- AS-5.4 Announcements, one UI Automation notification each on the field, or on the notice while one shows (AX-7.3):
  "<name>, n of m", "No programs match <text>", the edge earcon, "<name>, desktop program, <path>" / "<name>, Store
  app", "Programs refreshed, n programs" (F5), "Could not start <name>: …", "Cannot start as administrator", "No file
  location for <name>", "Hotkey <key>", "<key> is taken by another program", "Text size n". The notices: Hotkey…, the
  hotkey taken at start, Keyboard shortcuts, User guide, About, the update notice.
- AS-5.5 The log is `%LOCALAPPDATA%\Axit\logs\axstart.log`: the start, the hotkey registered or refused, the count of
  programs read, a start that failed with Windows' message; no program names (they can say what the user runs).

## 5. User interface

The menu bar; the search field; the list of matches under it; the status bar. The notices are the bundle's overlay in
place of the field and the list (AX-7.2). The window has the accessible role Window (AX-7.1); it is about a quarter of
the screen's width and twelve lines of the list high.

## 6. Architecture

```
src/AxStart.Core/    class library (net10.0): ProgramEntry (display name, parsing name, kind, target, from what the
                     shell gives), ProgramMatch (the matching and the order of AS-2.2), Hotkey (a key as text and
                     back, and the rule of AS-4.2), StartSettings; references Axit.Core
src/AxStart/         class library (net10.0-windows): AxStartApp (Run, Usage, the one-instance hand-over), StartForm
                     (the window, on AxitForm: the field with its KeyDown hook, the list, the menus, the keys, the
                     hotkey through RegisterHotKey, the hiding), AppsFolder (the shell's Applications folder read
                     through IShellItem and IEnumShellItems, and the start through the shell), StartupEntry (the
                     Run key's value), AxStartKeys, HelpText; references
                     AxStart.Core, Axit.Core, Axit.Forms
tests/AxStart.Tests/ ProgramMatchTests (the order, the words, accents, the kinds), HotkeyTests (the text both ways,
                     the refusals, the settings)
docs/axstart/        this spec, user-guide.md (embedded), nvda-test-plan.md
```

`Axit.Forms/AppMessage` gains the verb `show` and `SendTo`, a hand-over to a window the caller found: AxStart's is
hidden, so the Axit menu's list of visible windows does not have it, and the starting process finds it by its title
and program (`StartForm.FindRunning`), one AxStart being held by the mutex `Local\Axit.AxStart`. The installer stops
AxStart before it changes anything, starts it hidden afterwards, and its removal deletes the Run value.

## 7. Design decisions

- **AS-D1 The Start menu's list, through the shell.** `shell:AppsFolder` is the one list that holds the desktop
  programs and the packaged apps together, with the names the Start menu shows, and the shell starts either kind from
  its parsing name. Reading the Start menu folders' shortcuts would miss Settings, Calculator and every Store app.
- **AS-D2 The caret stays in the field.** The user's words: "i want to keep the cursor in the input field so i can keep
  typing or delete characters even while using up and down to select the program to launch". The list never takes
  the focus; Up and Down are the field's keys to the list, and each move is said through one notification, since
  NVDA does not follow a selection in a control without the focus. The field has one line, so Up and Down have no
  meaning of their own there.
- **AS-D3 A background process with a hidden window.** A global hotkey needs a process that runs: `RegisterHotKey`
  on the window of a process started at sign-in, the window hidden until the key. No tray icon and no taskbar button:
  the hotkey and the Start menu entry are the ways in, Quit AxStart the way out.
- **AS-D4 The hotkey is the user's.** Asked 2026-10-22: a setting chosen in the app, Ctrl+Shift+Space at first. The first
  default, Ctrl+Alt+Space, was taken on the user's first try: Windows Magnifier holds it (a preview of the whole desktop)
  while it runs. The second, Windows+Alt+Space, let the Alt reach the program in front before AxStart came up (its
  menu bar), and Win+Space, the user's next wish, is Windows' own (the input language). Caps Lock with a letter was
  asked for and ruled out: Caps Lock is no modifier for `RegisterHotKey`, a keyboard hook is not allowed (AX-7), and it
  is NVDA's key in the laptop layout. Ctrl+Shift+Space, with no Alt and no Windows key, was chosen the same day.
- **AS-D6 The Run key, not a Startup shortcut.** One registry value that AxStart writes and removes itself as the
  setting flips, plain .NET (`Microsoft.Win32.Registry`), in the place Task Manager's Startup apps lists; a shortcut
  would need the shell's link interfaces for the same effect.
- **AS-D5 The name.** AxStart, chosen 2026-10-22 over AxLaunch and AxRun: a stand-in for the Start menu's search.

## 8. Testing

- `tests/AxStart.Tests`: the matching and its order over a fixed list of names (starts, word starts, words in any
  order, accents, the start counts, an empty text), the hotkey text both ways and the refused keys, the settings.
- `docs/axstart/nvda-test-plan.md`: the hotkey from AxClaude, File Explorer and the desktop; typing, Up and Down with
  the caret kept, Backspace, the edge; Enter on a desktop program and a Store app; Ctrl+Shift+Enter; Escape with text and
  the focus given back; Alt+Tab away; Hotkey… with a free key and a taken one; a program installed while AxStart
  runs; Start with Windows across a sign-out; the update in place.

## 9. Open questions

- Leaving out uninstallers and help entries, as some launchers do.
- A tray icon for sighted helpers; recent documents of a program (jump lists); Control Panel and Settings pages as
  entries.
- What NVDA says when the window shows: whether "AxStart window" before the field is wanted or noise.

## 10. Status

- 2026-10-22: specified at the user's ask on the branch `axstart`.
- 2026-10-22, the same day, on `axstart`: built from the spec at the user's word ("Build it now"): the pure code with
  its tests, the window, the Applications folder read (99 programs on the development machine, desktop programs,
  Store apps and the shell's own ids, through an off-screen probe without a window), the wiring, the installer and
  the documents. Not yet checked with NVDA.
