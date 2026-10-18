# AxDown NVDA test plan

Manual checks with NVDA running, against `docs/axdown/SPEC.md`. Run the sections a change touches before a release;
run all of them before a release that changes the window. Prepare a folder with `plain.txt` (UTF-8, no
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
3a. In the tutorial file, Ctrl+Down moves to the first line of the next paragraph (a line with text after a blank line), one paragraph per press however many rows it wraps to, a heading with blank lines around it being a paragraph of its own; Ctrl+Up goes to the start of the current paragraph, then to the one before; from the last paragraph Ctrl+Down goes to the end of the file. Ctrl+Shift+Down: NVDA says the text selected; Ctrl+Shift+Up: unselected. The status bar's line number follows.
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
9. Open a locked file, or one that is not text (a `.png`): a notice names the problem; Escape closes it; the window
   stays open on an empty "Untitled" document, and Ctrl+S opens the Save dialog instead of writing over the file. A
   missing file opens as an empty document of that name, which Ctrl+S creates.

## 4. Find and go to line

1. Ctrl+F: the notice with "Text to find"; type a word, Enter: the caret lands on the match, NVDA reads the line
   with the selection; F3 the next; Shift+F3 back; a word that is not there: "Not found".
2. Ctrl+G: the field "Go to line (now line n, column m)" is read with the correct numbers; type a number, Enter:
   "Line n of m" and the caret is at the start of that line; a number past the end lands on the last line; a word
   instead of a number: "Not a line number" and the caret has not moved; Escape returns to the editor where it was.
3. NVDA+End reads the status bar: the name, "Line n, Column m", the encoding and the line ending.

## 5. Text size and font

1. Ctrl+Plus: "Text size n"; the text is larger; Ctrl+Minus back. Close and reopen: the size is kept.
2. View, Font opens the Windows font dialog; a chosen font is used and kept.

## 6. Menus and help

1. Alt opens the menu; every item is read with its shortcut; Escape closes without changing the text.
2. F1 opens the shortcuts notice; Page Down and Page Up move a screen of its text, Ctrl+Down and Ctrl+Up a paragraph;
   Escape closes it and the focus is back in the editor at the same place.
3. Help, User guide shows this app's guide, and Ctrl+H, Ctrl+K, Ctrl+L and the other navigation keys move through it, spoken
   as in the editor, "No next table" when there is none; Axit, About, Version names the version of Axit, the two apps
   and the folders; About, Licence, Attribution and Axit guide open their notices, Escape closes each.
4. Alt+A: the Axit menu with New window, Recently closed, Open windows, Open windows by type, Close all windows, Check
   for updates and About (bundle AX-9); About, Axit on GitHub: the browser opens the repository page. With a second
   AxDown open, Open windows reads "1 <name> - AxDown, 1 of 1" and Enter brings that window to the front; Alt+A, B, D there
   lists both windows, the current one checked; Alt+A, N, D starts a new Untitled window. Alone, Open windows holds
   "(none)", unavailable. Close a window that has `plain.txt` open: in another window Alt+A, R reads "1 plain.txt -
   AxDown" and Enter opens it in a new window; File, Open on another file puts the one you left there too. Alt+A, C
   (Close all windows) with unsaved changes in one window: that window asks "Save changes?" and the others close.

## 7. Updates and uninstall

1. Axit, "Check for updates (Axit <version>)..." names the version you have and reports in a notice either way; Axit, About, Version names it too
   and "Latest release on GitHub: Axit <version>" with the releases page.
2. With an older build installed (set `<Version>` in `src/Axit/Axit.csproj` below the latest release, `.\publish.ps1`,
   set it back), open a file: the notice "Update to Axit <newest>" opens by itself with four buttons. Escape (Later):
   the Axit menu has "Update to Axit <newest>..." below Check for updates, and Check for updates asks GitHub again and opens
   the notice once more. Enter (Update and restart): the notice "Updating to Axit <newest>" with a progress bar that
   NVDA reports by its progress bar settings; "Downloaded. Installing" at about 80 percent, then "Installed.
   Restarting"; AxDown closes and comes back on the same file (or empty, as it was), Axit, About, Version naming the newest
   version. Again with the older build: Escape during the download says "Update cancelled" and keeps the version;
   Update and close says "Installed. Closing" and AxDown does not come back; the Start menu entry is the newest
   version. With unsaved changes, Update and restart: the "Save changes?" question; Escape (Cancel): "Update
   installed. It starts with the next AxDown", the window stays; a later Exit with Don't save closes without
   starting AxDown again.
3. Settings, Apps, Axit, Uninstall removes AxDown's Start entry, its file menu entries, Open with, and `axdown.cmd`
   together with everything else of Axit; `%APPDATA%\Axit\axdown.json` stays.

## 8. Navigation (from the version with section 9 of the spec)

1. In `long.md`, Ctrl+H: the caret moves to the next heading and its line is spoken; Ctrl+Shift+H back; at the last
   heading: "No next heading", at the first: "No previous heading".
2. A heading-looking line inside a fenced code block is skipped.
3. Plain arrow keys still speak nothing of AxDown's own.
4. Ctrl+2: the next level-2 heading, other levels skipped, spoken "…, heading level 2"; Ctrl+Shift+2 back; past the
   last one: "No next heading level 2". Navigate, Next and Navigate, Previous each list Heading and Heading level 1 to
   6 with their keys, and the items do the same as the keys.
5. Ctrl+K: the caret on the next link's first character, "GitHub, link"; Ctrl+Shift+K from inside a link: the link
   before it; a bare https address and an `<https://…>` address count, an image does not.
6. Ctrl+L: "List with 3 items, one" at the first item; Ctrl+I: the next item's text, nested items included; a numbered
   list after a bulleted one is a second list.
7. Ctrl+Q: "Block quote, " and the first line; Ctrl+T: "Table with 3 rows and 2 columns" at the header line; nothing
   inside a fenced code block counts. Navigate, Next and Navigate, Previous list Link, List, List item, Block quote
   and Table with their keys.
