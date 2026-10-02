# AxDown NVDA test plan

Manual checks with NVDA running, against `docs/axdown/SPEC.md`. Run the sections a change touches before a release;
run all of them before the first release of AxDown (plan step 10). Prepare a folder with `plain.txt` (UTF-8, no
mark, CRLF), `bom.md` (UTF-8 with a mark, LF), `wide.txt` (UTF-16 LE), `ansi.txt` (Windows-1252 with an accented
letter), and a `long.md` with headings and lines longer than the window.

## 1. Install and start

1. `.\publish.ps1` installs Axit. Press the Windows key, type `axd`: "Axit AxDown" is offered; Enter opens an
   empty window. NVDA reads the title "Untitled - AxDown" and then "Untitled edit multi line".
2. Right-click `plain.txt` in File Explorer: "Open in AxDown" is in the menu; Enter opens the file. "Open with"
   lists AxDown for `.md`, `.markdown` and `.txt`.
3. In a console: `axdown plain.txt` opens it; `axit plain.txt` opens it in AxDown, `axit <folder>` opens AxClaude;
   `axdown missing.md` opens an empty document named `missing.md`.
4. Two files from Explorer give two windows; Alt+Tab names both.

## 2. Reading and writing

1. In `long.md`, Down Arrow reads one line at a time; Ctrl+Right reads words; NVDA's say-all reads on. Nothing is
   spoken by AxDown on these keys.
2. Type a sentence; Backspace and Delete behave as in Notepad; Enter makes a line; Tab types a tab (the focus does
   not leave the editor). Ctrl+Z undoes the last change.
3. Page Down moves a screen and reaches the last line; Page Up the first. In a short file both do something.
4. Ctrl+Shift+W: "Word wrap off"; a long line is now one line; the caret stays on the same character. Again:
   "Word wrap on"; the caret stays.
5. Ctrl+M, Ctrl+H, Ctrl+I, Ctrl+J insert nothing.

## 3. Files

1. Open `bom.md`, change a letter, Ctrl+S: "Saved". A hex view shows the mark kept and LF endings only.
2. Open `wide.txt`, edit, save: still UTF-16 LE with its mark. `ansi.txt`: the accented letter survives a save.
3. Open `plain.txt`, Ctrl+S without a change: "Saved", the file unchanged byte for byte.
4. Ctrl+N with unsaved changes: the notice "Save changes to plain.txt?" with Save, Don't save, Cancel; Escape
   cancels and the text is still there; Enter saves and the document is empty, "Untitled".
5. Ctrl+S on Untitled opens the Windows Save dialog; cancelling returns to the editor with the focus where it was.
6. Ctrl+Shift+S saves under a new name; the title, the editor's name and the status bar show the new name.
7. Change the file in another editor while AxDown has it open, then Ctrl+S in AxDown: the notice "The file changed
   on disk since you opened it. Write over it?"; Cancel keeps both; Write over writes.
8. Close with Alt+F4 with unsaved changes: the notice; Don't save closes; Cancel stays.
9. Open a locked or missing file: a notice names the problem; Escape closes it; the window stays open.

## 4. Find and go to line

1. Ctrl+F: the notice with "Text to find"; type a word, Enter: the caret lands on the match, NVDA reads the line
   with the selection; F3 the next; Shift+F3 back; a word that is not there: "Not found".
2. Ctrl+G: the field "Go to line (now line n, column m)" is read with the correct numbers; type a number, Enter:
   "Line n of m" and the caret is at the start of that line; a number past the end lands on the last line; Escape
   returns to the editor where it was.
3. NVDA+End reads the status bar: the name, "Line n, Column m", the encoding and the line ending.

## 5. Text size and font

1. Ctrl+Plus: "Text size n"; the text is larger; Ctrl+Minus back. Close and reopen: the size is kept.
2. View, Font opens the Windows font dialog; a chosen font is used and kept.

## 6. Menus and help

1. Alt opens the menu; every item is read with its shortcut; Escape closes without changing the text.
2. F1 opens the shortcuts notice; Escape closes it and the focus is back in the editor at the same place.
3. Help, User guide shows this app's guide; Help, About names the version of Axit.

## 7. Updates and uninstall

1. Help, Check for updates reports in a notice either way.
2. Settings, Apps, Axit, Uninstall removes AxDown's Start entry, its file menu entries, Open with, and `axdown.cmd`
   together with everything else of Axit; `%APPDATA%\Axit\axdown.json` stays.

## 8. Navigation (from the version with section 9 of the spec)

1. In `long.md`, Ctrl+H: the caret moves to the next heading and its line is spoken; Ctrl+Shift+H back; at the last
   heading: "No next heading", at the first: "No previous heading".
2. A heading-looking line inside a fenced code block is skipped.
3. Plain arrow keys still speak nothing of AxDown's own.
