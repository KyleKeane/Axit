# axClip — Specification

axClip is an app of the Axit bundle (`docs/axit/SPEC.md`): a clipboard history behind a hotkey, for a blind developer
who uses the NVDA screen reader. Built 2026-10-23 on the branch `axclip` (the long run of that day,
`docs/axit/plan.md`), not yet heard under NVDA. It runs as axStart does, a background process with one hidden window,
and that window is the bundle's `HotkeyForm`, which came out of axStart for it. The document is kept in step with the
code. Requirements are numbered `CP-n.m` and decisions `CP-Dn`; the bundle's rules, AX-7, AX-8 and AX-9, are cited
by number and not repeated; axStart's specification (`docs/axstart/SPEC.md`, AS-n.m) is cited for what the two share.
In axClip's code and documents, "SPEC.md" means this file.

## 1. Summary

Windows' own clipboard history (Windows+V) is a panel that NVDA reads poorly. axClip keeps the text of every copy
while it runs; its hotkey, Windows+Alt+V unless the user chose another, brings up one search field over that history,
the newest first. Typing narrows it, Down and Up move through it with the caret kept in the field, as in axStart, and
Enter pastes the item into the program that was in front. Nothing is spoken but the answer to a key.

## 2. Goals and non-goals

- **G1 Everything copied, a key away.** The text of each copy, newest first, the same text once (a copy again moves
  it to the top).
- **G2 axStart's way.** The same window, field and keys as the launcher (AS-3), so that one habit serves both.
- **G3 Private by default** (CP-D2). The history lives in memory: it ends with the process unless the user turns on
  Keep history between runs. A copy that a password manager marks as not for history is never kept. Recording can be
  paused.
- **Non-goals.** Pictures, files and formatted text (only the plain text of a copy is kept), pinned items, sync
  between computers, editing an item, pasting as keystrokes.

## 3. Scenarios

1. In an editor, Ctrl+C on a line; later Windows+Alt+V: "axClip window, Search the clipboard, edit"; Down: "git status
   --porcelain, 1 of 12". Enter: axClip hides, the editor is in front again, and the line is pasted.
2. Windows+Alt+V, type `token`: after the pause, "export TOKEN=…, 1 of 2". Ctrl+Q: the whole text in a notice, to read
   with the arrows.
3. Before copying a password by hand: axClip's Options, Pause recording; the copies after it are not kept until it is
   turned on again.

## 4. Functional requirements

### CP-1 Launch, the background, the hotkey

- CP-1.1 `Axit.exe clip [--hidden]`; the console command `axclip`; the Start menu entry "Axit axClip". One axClip runs
  at a time (the mutex `Local\Axit.AxClip`); a second start shows the running one's window (AS-1.2), or with
  `--hidden` just ends. Without `--hidden` the window shows at once.
- CP-1.2 Start with Windows (a setting, on at first): the value "Axit axClip" under the user's Run key,
  `"…\Axit.exe" clip --hidden`, written only by the installed program (`SignInStart`, AS-1.3). The installer stops
  axClip before it changes anything and starts it again, hidden, after an install when the setting is on (AS-1.5).
- CP-1.3 The hotkey is Windows+Alt+V at first (CP-D1); Options, Hotkey… chooses another the way axStart's does (AS-4.2),
  and a hotkey another program holds is said in a notice with the window shown (AS-4.3).
- CP-1.4 The window hides on Enter, Shift+Enter, Escape, Alt+F4 and another program coming to the front, and gives the
  front back to the program that had it (AS-3.4, the bundle's `HotkeyForm`). Only axClip, Quit axClip ends the
  process, and with it the history unless it is kept (CP-3.4).

### CP-2 The window

- CP-2.1 From the top: the menu bar; the field "Search the clipboard", where the caret always is; the list of items
  under it, which never takes the focus (AS-D2), named "Clipboard, N" (N the items shown); the status bar, with the
  number of items kept on the left and "Recording" or "Paused" on the right.
- CP-2.2 The window shows centred on the screen of the program in front, the field empty and the newest item selected.
- CP-2.3 Typing narrows the list to the items that hold every word typed, case and accents ignored
  (`Axit.Core/TextMatch`), newest first; after a pause of 300 ms the first match is said with the count, "<label>, 1
  of 2", or "Nothing in the clipboard history matches <text>" (a setting, Say the first match while typing, on).
- CP-2.4 Down and Up move through the matches, Ctrl+Home and Ctrl+End to the first and the last, Page Down and Page Up
  by a screen; each says "<label>, <n> of <count>"; at an end the edge sound plays and the item is said again (AS-3.3).
- CP-2.5 An item's label is its first line that has text, the spaces in it run together, cut at 120 characters with
  "…", followed by ", <n> lines" when the text has more than one line.

### CP-3 What is kept

- CP-3.1 Each change of the clipboard (`AddClipboardFormatListener`) that holds text adds that text as the newest item,
  unless recording is paused, the text is empty or only spaces, it is over one million characters, or the copy carries
  one of the formats a program sets to keep it out of clipboard history: `ExcludeClipboardContentFromMonitorProcessing`,
  `Clipboard Viewer Ignore`, or `CanIncludeInClipboardHistory` with the value 0 (CP-D2).
- CP-3.2 A text that is in the history already moves to the top instead of being added again. At most 200 items; the
  oldest goes.
- CP-3.3 A clipboard another program holds open is read again after 100 ms, up to three times; a copy that cannot be
  read then is left out and logged ("The clipboard could not be read").
- CP-3.4 Options, Keep history between runs (off at first): on, the history is written to `axclip.json` when the window
  hides and when the process ends, and read at the next start; off, the file holds none, and turning it off empties the
  file's history at once.
- CP-3.5 Options, Pause recording (Ctrl+P): "Recording paused", "Recording" again; it is off at every start.

### CP-4 Using an item

- CP-4.1 Enter pastes the selected item: it is put on the clipboard, the window hides and gives the front back, and
  after 150 ms, when that program's window is in front again, Ctrl+V is sent to it (`SendInput`). When another window
  is in front by then, nothing is pasted and the text stays on the clipboard. The item becomes the newest.
- CP-4.2 Shift+Enter puts the item on the clipboard without pasting: "Copied", and the window hides.
- CP-4.3 Ctrl+Q shows the item's whole text in a notice titled "Clipboard text, <n> lines", read with the arrows;
  Escape closes it.
- CP-4.4 Delete removes the selected item: "Removed". Ctrl+Shift+Delete asks "Clear the clipboard history? Its N items
  cannot be brought back." with Clear and Cancel; Clear says "Clipboard history cleared". The clipboard itself is not
  changed.
- CP-4.5 A command with no item says "The clipboard history is empty" or the no-match line.

### CP-5 Menus, keys, texts

- CP-5.1 Menus (AX-6): **AxClip (C):** Paste Enter | Copy without pasting Shift+Enter | Read in full Ctrl+Q | Remove
  Delete | Clear history… Ctrl+Shift+Delete | ─ | Hide Escape | Quit axClip. **Options (O):** Hotkey… | Pause
  recording Ctrl+P ✓ | Keep history between runs ✓ | Text ▸ | General ▸ (Check for updates at start | Start with
  Windows ✓ | Say the first match while typing ✓) | Developer ▸. **Axit (A)** (AX-9). **Help (H):** Keyboard
  shortcuts F1 | User guide | ─ | Windows resources ▸ (Clipboard in Windows). Every item has a mnemonic.
- CP-5.2 Keys (AX-8). Bundle-wide: F1, Ctrl+Shift+P, Page Up, Page Down, Ctrl+Plus, Ctrl+Minus, Alt+F4. Window-specific
  (`AxClipKeys`): Down, Up, Ctrl+Home, Ctrl+End, Enter, Shift+Enter, Ctrl+Q, Delete, Ctrl+Shift+Delete, Ctrl+P, Escape.
  The hotkey is a setting, not a key of the window. Delete and the arrows act on the list while the caret is in the
  field: the field's own Delete and Up and Down are not used (a search text is short; Backspace edits it).
- CP-5.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axclip/user-guide.md` is embedded; About is the
  bundle's (AX-9.9).

### CP-6 Settings, announcements, log

- CP-6.1 `%APPDATA%\Axit\axclip.json` (AX-5.3; `ClipSettings`): `fontFamily`, `fontSize`, `fontBold`,
  `checkForUpdates`, `hotkey` (`Windows+Alt+V`), `startWithWindows` (true), `sayFirstMatch` (true), `keepHistory`
  (false), `protectedHistory` (the items, newest first, only while Keep history is on, as JSON of `text` and `copied`, the time, encrypted for the Windows account with DPAPI, `Axit.Core/UserSecret`, so that the file elsewhere shows nothing; security audit S-21; Clear history also removes a `.bad` copy of the file).
- CP-6.2 Announcements (AX-7.3): the matches as in CP-2.3 and CP-2.4; "Copied"; "Removed"; "Clipboard history
  cleared"; "Recording paused"; "Recording"; "The clipboard history is empty"; the Hotkey notice's words (AS-4.2);
  the bundle's. Nothing is said when the window hides, and nothing when a copy is kept.
- CP-6.3 The log is `%LOCALAPPDATA%\Axit\logs\axclip.log`: the start, the hotkey, the showing and hiding, a paste or a
  paste left undone, errors; never a copied text.

## 5. Architecture

```
src/AxClip.Core/    class library (net10.0): ClipEntry (one item: the text, the time, the label), ClipHistory (the
                    items newest first: add with the move to the top and the limit, remove, clear, find),
                    ClipPolicy (whether a copy is kept, from the formats it carries), ClipSettings; references
                    Axit.Core (Hotkey, TextMatch, SettingsFile)
src/AxClip/         class library (net10.0-windows): AxClipApp (Run, Usage, the mutex), ClipForm (the window on
                    Axit.Forms/HotkeyForm: the listener, the field and the list, the paste), AxClipKeys, HelpText
tests/AxClip.Tests/ ClipHistoryTests, ClipPolicyTests, ClipSettingsTests
docs/axclip/        this spec, user-guide.md (embedded), nvda-test-plan.md
```

## 6. Design decisions

- **CP-D1 Windows+Alt+V.** Windows+V is Windows' own history, and Ctrl+Shift+V pastes plain text in many programs; a
  hotkey with the Windows key and Alt is free in Windows and in NVDA, and the V keeps the paste in mind. The user can
  choose another (CP-1.3).
- **CP-D2 Private by default.** A clipboard holds passwords, tokens and personal text. The history is in memory unless
  the user asks for the file; the formats password managers set are honoured; recording can be paused; the log never
  holds a copied text (bundle rule 6).
- **CP-D3 Paste with Ctrl+V, after the front is back.** Pasting is the program's own command; axClip only puts the
  text on the clipboard and presses Ctrl+V once the program's window is in front again, so that nothing is typed into
  another window. A program whose paste is another key gets the text with Shift+Enter.
- **CP-D4 One window base with axStart.** The hidden window, the hotkey and the rule of the foreground are the
  bundle's `HotkeyForm`, moved out of axStart on this second use, so that a fix to the foreground reaches both.

## 7. Open questions

- Pinned items that stay through Clear and the limit.
- Pictures and files, said by kind ("Picture, 1920 by 1080"; "3 files").
- A Today view, or the copy's time in the label.

## 8. Status

- 2026-10-23: built on the branch `axclip` (CP-1 to CP-6) with its tests, after `HotkeyForm` came out of axStart in
  three commits; not yet run under NVDA. axStart's own test plan is to be run again before the branch merges, since
  its window now stands on `HotkeyForm`. The test plan is `docs/axclip/nvda-test-plan.md`.
