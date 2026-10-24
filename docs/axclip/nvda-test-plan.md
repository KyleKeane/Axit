# axClip NVDA test plan

Run with NVDA before a release, after `run-axclip.ps1` (or `.\run.ps1 -App clip`), with axDown open on a scratch file. Quit an installed axClip first. Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axclip/SPEC.md` (CP-n.m). axStart's plan (`docs/axstart/nvda-test-plan.md`) is run in the same pass while the two share `HotkeyForm`.

## 1. Start and the hotkey (CP-1)

1. Start axClip: "axClip window", then "Search the clipboard, edit". Escape: axClip hides and NVDA reads the window you were in; typing goes there.
2. In axDown, copy three different lines (Ctrl+C on each). Nothing is spoken by axClip.
3. Windows+Alt+V: "axClip window, Search the clipboard, edit". Down: the third line you copied, "…, 1 of 3". Windows+Alt+V again: axClip hides.
4. Run `axclip` in a console while axClip runs: the running window comes to the front; no second axClip starts.
5. Alt+F4 in axClip: it hides, it does not end; Windows+Alt+V brings it back with the items.

## 2. The items (CP-2, CP-3)

1. Down, Up: each item read with its place; at the last one, Down plays the low edge sound and reads it again. Ctrl+Home, Ctrl+End: the first and the last.
2. Type a word of one item: after a moment "<that item>, 1 of 1". Backspace the word: the list shows all items again.
3. Copy the first line again in axDown, then Windows+Alt+V, Down: that line is first, and it is in the list once.
4. Copy a text of three lines: its item reads "<first line>, 3 lines". Ctrl+Q: the notice "Clipboard text, 3 lines" with the whole text; Escape.
5. Ctrl+P: "Recording paused"; copy a line in axDown; Windows+Alt+V: that line is not in the list. Ctrl+P: "Recording". NVDA+End: "N items" and "Recording".
6. Copy a password from a password manager that marks it private (KeePassXC, Bitwarden, 1Password): it is not in the list.

## 3. Pasting (CP-4)

1. In axDown, put the caret on an empty line. Windows+Alt+V, Down to an item, Enter: axClip hides, NVDA reads axDown, and the item is pasted at the caret.
2. Windows+Alt+V, an item, Shift+Enter: "Copied", axClip hides; nothing is pasted; Ctrl+V in axDown pastes it.
3. Delete on an item: "Removed". Ctrl+Shift+Delete: the notice "Clear the clipboard history?"; Escape: nothing changes; again and Enter: "Clipboard history cleared"; Down: the low sound and "The clipboard history is empty".

## 4. Settings and the rest (CP-5, CP-6)

1. Options, Hotkey: the notice "Press the key that brings axClip up…"; press Ctrl+Alt+Shift+V: the field says it; Enter: "Hotkey Ctrl+Alt+Shift+V"; the new key shows axClip. Set it back to Windows+Alt+V.
2. Options, Keep history between runs (checked), copy two lines, AxClip, Quit axClip, start it again: the two lines are there. Turn the option off, quit, start again: the history is empty.
3. F1: the Keyboard shortcuts notice; Escape. Help, User guide: this guide.
4. Alt+A: the Axit menu; About, Version names axClip among the apps.
