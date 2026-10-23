# axPreview NVDA test plan

The files for this plan are in `manual_tests/axpreview`, made and reset by `.\manual_tests\prepare.ps1 axpreview`; its `_main.md` says which folder or file each section uses.

Run with NVDA before a release, on a folder with `README.md` (headings, links, a list), `plain.txt`, `app.log`, a `.json` file, an `.exe` or `.mp3`, and a file over 32 MB, after `run-axpreview.ps1 C:\path\to\README.md` (or `.\run.ps1 -App preview <file>`) and from axFile on the same folder. Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axpreview/SPEC.md` (AP-n.m).

## 1. Start and the text (AP-1, AP-2)

1. Start axPreview on `README.md`: NVDA reads "README.md - axPreview window", then "README.md, read only, edit, multi line" and the first line. Nothing else is spoken. The menu bar (Alt): File, Navigate, Options, Axit, Help; no Edit menu.
2. Down Arrow reads the next line; Ctrl+Right reads words; typing a letter changes nothing and says nothing; Ctrl+V does nothing. Shift+Down selects a line; Ctrl+A, Ctrl+C: the text on the clipboard.
3. `l`: "Line 2 of 40" (your numbers). Ctrl+G: the field "Go to line (now line 2, column 1)" under the text; type `5`, Enter: "Line 5 of 40", the caret there; Ctrl+G, type `x`, Enter: "Not a line number". Typing `l` into the Go to line field puts an l in the field.
4. Ctrl+H: "Install, heading level 2" (the next heading); Ctrl+Shift+H back; Ctrl+2: the next level 2 heading; Ctrl+K: the next link; Ctrl+L: the next list; Ctrl+I: the next list item; Ctrl+Q: "No next block quote" when there is none; Ctrl+T: a table or "No next table". Alt+N, X: the Next submenu lists them with their keys.
5. Ctrl+F, type `update`, Enter: the match selected and its line read; F3: the next; Shift+F3: back. Page Down, Page Up, Ctrl+Down, Ctrl+Up as in axDown.
6. NVDA+End: "README.md", then "Line 5, Column 1 · UTF-8 · CRLF" (the file's values). Ctrl+Shift+W: "Word wrap off", the caret where it was; again: "Word wrap on".
7. Ctrl+S: "Nothing to save here". F1: the Keyboard shortcuts notice; Ctrl+H in it jumps to its headings; Escape.
8. Ctrl+O: the Windows file picker on text files; pick `plain.txt`: the window shows it, the title "plain.txt - axPreview"; Ctrl+H: "No next heading". Alt+F, R: "Open recent file submenu", "1. plain.txt (…)", "2. README.md (…)"; 2: README.md is back.
9. Ctrl+O, All files, pick the `.exe`: the notice "Not a text file" saying "<name> is not a text file. axFile's Enter opens it in the program Windows has for it."; Enter closes it and README.md is still there. Pick the file over 32 MB: the notice "Too large" naming the size. Pick `app.log`: it opens, since its bytes read as text.
9a. Console: `axpreview gone.txt` (a file that does not exist): the notice "File not found" naming it, the window empty, "Untitled - axPreview"; Enter closes it; Ctrl+E: "No file yet". Ctrl+O on a file whose first eight kilobytes are text with a NUL further in (make one): the error notice, and README.md is still shown.
10. Edit `README.md` in another program while axPreview is in the background, then Alt+Tab back: "Reloaded, README.md changed on disk", the caret on the same line. F5: "Reloaded".
11. Ctrl+E on README.md: a new axDown window on it, the focus there; Alt+F4 closes it. On the `.json` file, Alt+F: the item reads "Edit in axJSON Ctrl+E"; Ctrl+E: axJSON. Ctrl+W: File Explorer with the file selected. Alt+F, I: File Explorer Ctrl+W, axGIT, axTerminal, axFile; Enter on axFile: axFile on the folder with the file selected.
12. Ctrl+N: a second axPreview window, "Untitled - axPreview", empty; Ctrl+E and Ctrl+W there: "No file yet"; Alt+F4.

## 2. The hand-over (AP-1.2, AP-1.3)

1. With an axPreview window open on README.md, in axFile on the folder, Shift+Enter on `plain.txt`: the axPreview window comes to the front showing `plain.txt`, NVDA reads its title and the text; no second window (Alt+A, O lists one axPreview). Shift+F10, E (Preview) on `app.log`: the same.
2. Shift+Enter on the `.exe` in axFile: the axPreview window comes to the front with the notice "Not a text file" and keeps `app.log`. Without closing it, back in axFile, Shift+Enter on `plain.txt`: the notice is gone and the window shows `plain.txt`, NVDA reading its title and text.
3. Shift+Enter on a folder in axFile: a new axFile window on that folder, not axPreview.
4. Close the axPreview window; Shift+Enter on `README.md` in axFile: a new axPreview window opens on it.
5. In File Explorer, right-click `README.md`, Open with Axit, axPreview: the open window takes it. Make axPreview the default for `.log` (Settings, Apps, Default apps, or Open with, Always): Enter on `app.log` in axFile and a double click in File Explorer open axPreview. Set the default back if wanted.
6. Console: `axpreview C:\path\to\plain.txt`: the open window takes it and comes to the front; `axpreview --new C:\path\to\plain.txt`: a second window.

## 3. Text, notices, the Axit menu (AP-3)

1. Ctrl+Plus: "Text size 13" (or the next size), the caret where it was; Ctrl+Minus back. Options, Text, Font: the font picker; Escape.
2. Help, User guide: this guide; Ctrl+H moves through its headings; Escape.
3. Alt+A: the Axit menu with New window (axPreview among the apps), Recently closed (the file an axPreview window had when it closed), Open windows, Open windows by type, Close all windows, Check for updates, About.
4. Options, Developer, Copy diagnostics: "Copied diagnostics"; paste somewhere: the versions, the file with its encoding and the log's end. Alt+F4 closes without a question.
