# axPreview — Specification

axPreview is an app of the Axit bundle (`docs/axit/SPEC.md`): a read-only viewer for one file, for a blind developer
who uses the NVDA screen reader. Specified and built 2026-10-22 at the user's ask, released in 3.3.1 and checked with NVDA the same day (the record: `docs/axit/nvda-test-record.md`): the preview that axFile
gained the night before (`docs/axfile/SPEC.md` AF-9.4) moves here and grows into a window of its own, so that axFile
is file operations and management only and viewing, like playing (`docs/axmedia/SPEC.md`), is another app's with a
tight hand-over (AP-D1). The document is kept in step with the code (a behaviour change updates it in
the same commit). Requirements are numbered `AP-n.m` and decisions `AP-Dn`; the bundle's rules, AX-7 (accessibility),
AX-8 (keys) and AX-9 (the Axit menu), and axDown's (`docs/axdown/SPEC.md`, cited as "axDown AD-n.m") are cited by
number and not repeated. In axPreview's code and documents, "SPEC.md" means this file. The inventory of the split
of axFile, of which this app is one part, is `docs/axit/axfile-split.md`.

## 1. Summary

One file in one window, to read, not to change: a read-only text named after the file, with every reading key the
bundle's texts have (AX-7.2): the arrows, Page Up and Page Down, Ctrl+Up and Ctrl+Down by paragraph, find, Go to
line, `l` for the line number, and for a Markdown file axDown's navigation keys (headings by level, links, lists,
list items, block quotes, tables). Ctrl+E opens the same file in its editor, axDown or axJSON, for the one time in ten
the reader wants to change something. The window is reached from axFile (Shift+Enter on a file, or Item → Preview),
from File Explorer (Open with Axit on a file, and as the file's default program when the user makes it so), from the
Start menu, the Axit menu and the command line; a file opened while an axPreview window is open goes into that window
rather than a new one (AP-1.2). It works entirely offline (bundle G6).

## 2. Goals and non-goals

- **G1 One text, native.** A read-only EDIT control NVDA reads line by line, a status bar, the bundle's notices
  (AX-7.1); nothing is edited here, so nothing can be lost here.
- **G2 Reading first.** Every key is a reading key; the editors' Ctrl letters work the same, so that a file read in
  axPreview and edited in axDown is the same text under the same keys.
- **G3 One viewer.** axFile hands files to the axPreview window that is open; a second window is a deliberate act (the
  Axit menu's New window).
- **G4 Streamlined.** Text files in version one; other kinds wait (§9).
- **G5 Nothing spoken unprompted** (AX-7.3).
- **Non-goals.** Editing of any kind (Ctrl+E is the way out); images, PDFs, office documents, archives (§9; axFile's
  Enter hands them to Windows' own program); a rendering of Markdown (the text is the text; the navigation keys are
  the structure); syntax colouring; tabs or a list of open files.

## 3. Scenarios

1. In axFile on a project, Shift+Enter on `README.md`: axPreview opens on it, the window in front: NVDA reads
   "README.md - axPreview window", then "README.md, read only edit, multi line" and the first line. Ctrl+H: "Install,
   heading level 2"; Ctrl+K: the next link; `l`: "Line 12 of 40"; Ctrl+F `update`, Enter: the match selected and its
   line read. Escape does nothing here; Alt+Tab or the Axit menu goes back to axFile. Ctrl+E: axDown on the same
   file, the caret on the same line.
2. Shift+Enter on `notes.txt` in axFile while axPreview is open on `README.md`: the same window shows `notes.txt`;
   "Open recent" lists both.
3. Shift+Enter on `report.log` in axFile, a file no editor takes: axPreview shows it, since its first bytes read as
   text. Shift+Enter on `tool.exe`: axPreview says "Not a text file" in a notice and keeps what it had.
4. In File Explorer, right-click `CHANGELOG.md`, Open with Axit, axPreview: the window opens on it. The user makes
   axPreview the default program for `.log` files in Windows' settings (the guide says how); from then on Enter on
   a `.log` in axFile, and a double click in File Explorer, open axPreview.
5. The file changes on disk while axPreview is in the background (Claude edited it); Alt+Tab back: "Reloaded,
   README.md changed on disk", the caret on the same line and column (axDown AD-3.6).

## 4. Functional requirements

### AP-1 Launch and the hand-over

- AP-1.1 `Axit.exe preview [<file>] [--new]`: the file shown; no path opens the window empty, named "Untitled" (Ctrl+O
  then); a path that does not exist is the notice "File not found" naming it, the window empty or keeping what it
  showed (decided 2026-10-23; it opened an empty window under the missing name, and Edit in axDown created the file). Start menu entry `Axit axPreview`, console command `axpreview`, File Explorer's Open with Axit ▸ axPreview on
  every file, axPreview under Windows' own Open with for text kinds (`.txt`, `.md`, `.markdown`, `.log`, `.json`,
  type name "Text file (axPreview)"), so that the user can make it the default program for a kind in Windows'
  settings (AP-D2), the Axit menu's New window (AX-9). A file alone on the command line keeps going to its editor
  (AX-1.2); the viewer is chosen by name or by Windows' association.
- AP-1.2 **One viewer.** A start with a file, from axFile, from File Explorer or from the command line, hands the
  file to the axPreview window that is open, the most recent by Z order when there are several, brings it to the
  front and ends; it opens a window of its own only when there is none, or with `--new` (the Axit menu's New window
  passes it). axFile does nothing special: it starts the app on the file.
- AP-1.3 **The hand-over** between the starting process and the open window is the bundle's `WM_COPYDATA` message
  (`Axit.Forms/AppMessage`, `docs/axmedia/SPEC.md` AM-1.3) with the verb `open` and one path; the window answers
  nothing; the starting process ends once the message is taken.
- AP-1.4 Ways out: File → Edit in axDown / Edit in axJSON (Ctrl+E; the editor by the file's kind, `.json` to axJSON,
  anything else to axDown, a new editor window on the file with the caret put on the viewer's line through the
  editor's own Go to line argument, §9 until the editors take one), Show current file in ▸ File Explorer (Ctrl+W),
  axFile (the file selected in its folder), axGIT (the repository that holds it), axTerminal (its folder). The Axit
  menu (AX-9).

### AP-2 The text

- AP-2.1 One read-only, multi-line EDIT control named with the file's name (axDown AD-2.1), word wrap on by default
  (Options → Text → Word wrap, Ctrl+Shift+W, a setting), the caret on the first line when a file opens; the arrows,
  Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left and Ctrl+Right, Shift with any of them, Ctrl+A and Ctrl+C are the
  control's own; Page Up and Page Down one screen (AX-8.2); Ctrl+Up and Ctrl+Down the previous and next paragraph,
  with Shift selecting to there (AX-8.2). Typing changes nothing and says nothing (the control is read-only).
- AP-2.2 **What opens:** a file whose first bytes read as text (the editors' kinds by extension, an empty file, a
  UTF-16 mark, or no zero byte and few control characters in the first eight kilobytes; the rule of axFile's
  `Preview.IsText`, moved) up to 32 MB (axDown AD-3.1's limit), decoded as the editors decode (`TextDocument`: the
  mark, the code page, the line endings). Anything else: the notice "Not a text file" ("tool.exe is not a text file.
  axFile's Enter opens it in the program Windows has for it."), the window keeping what it had; a file over the limit:
  the notice "Too large", "<name> is 33,554,433 bytes, over the 32 MB a window can open." (the editors' wording, AD-3.7; until 2026-10-23 "is n MB", which said 32 MB for a file just over); a reload of a file that cannot be read
  (held by another program): the error notice, the text kept (axDown AD-3.6); a file whose first eight kilobytes pass
  and which the reader refuses (a NUL further in): the error notice, the text kept as well (2026-10-23; the window
  emptied). A reload (AP-2.4) of a file that has
  become one of these shows the same notice and keeps the text it had. A file handed over while a notice shows
  closes the notice first, so that the window read on activation is the file's; the update's progress notice stays,
  the file loading behind it (2026-10-23).
- AP-2.3 **Navigation**, the window-specific keys of `AxPreviewKeys` (AX-8), axDown's letters (axDown §9): Ctrl+H
  heading, Ctrl+1 to Ctrl+6 a heading of that level, Ctrl+K link, Ctrl+L list, Ctrl+I list item, Ctrl+Q block quote,
  Ctrl+T table, each the next one and with Shift the previous one, through the bundle's `TextJumps` over
  `MarkdownOutline`, announcing "Install, heading level 2" or "No next heading"; they work in every file (a plain
  text file has no headings, "No next heading"); Navigate → Next ▸ and Previous ▸ list them (axDown AD-D7). **`l`**
  says the line, "Line 12 of 40"; **Ctrl+G** Go to line, the field in the question area under the text (axDown
  AD-6.4). **Ctrl+F**, **F3**, **Shift+F3** find (axDown AD-6.3). JSON files are text here; JSON's own keys come
  when axJSON's outline is shared (§9).
- AP-2.4 **Reload** (File → Reload, F5): the file read again, the caret on the same line and column, "Reloaded"; and
  on coming back to the window, when the file's write time moved, the same by itself, "Reloaded, <name> changed on
  disk" (axDown AD-3.6, the branch with nothing unsaved, since nothing is ever unsaved here).
- AP-2.5 The caret moves only on the user's command (AX-7.3): a reload, a word wrap change or a font change leaves it
  where it was.

### AP-3 Menus, keys, texts

- AP-3.1 Menus (AX-6): **File (F):** New window Ctrl+N | Open… Ctrl+O | Open recent ▸ | ─ | Reload F5 | ─ | Edit in
  axDown Ctrl+E (or Edit in axJSON, by the file) | Show current file in ▸ (File Explorer Ctrl+W | axClaude | axTerminal |
  axGIT | axFile, bundle AX-9.10) | ─ | Exit Alt+F4. No Edit menu: the control's own Ctrl+A and Ctrl+C work as they do. **Navigate (N):** Find… Ctrl+F | Find next F3 | Find previous Shift+F3 | Go to line… Ctrl+G | ─ |
  Next ▸ | Previous ▸ (Heading, Heading level 1 to 6, Link, List, List item, Block quote, Table). **Options (O):**
  Text ▸ (Word wrap Ctrl+Shift+W | ─ | Font… | Larger text | Smaller text | Windows text size) | General ▸ (Check for
  updates at start ✓) | Developer ▸ (Open settings file | Copy diagnostics). **Axit (A)** (AX-9). **Help (H):**
  Keyboard shortcuts F1 | User guide | ─ | Markdown resources ▸ (axDown's pages). Every item has a mnemonic (AX-7.4).
- AP-3.2 Keys (AX-8). Bundle-wide: F1, the control keys (the text, or the Go to line field while it shows), Ctrl+F,
  F3, Shift+F3, Ctrl+S ("Nothing to save here"), Ctrl+Plus, Ctrl+Minus, Page Up, Page Down, Ctrl+Up, Ctrl+Down with
  Shift, Alt+F4. Window-specific (`AxPreviewKeys`): Ctrl+O, Ctrl+N (New window, AX-9), F5, Ctrl+E, Ctrl+W, Ctrl+G,
  Ctrl+Shift+W, `l`, and the fourteen navigation keys of AP-2.3. Ctrl+H, Ctrl+I, Ctrl+J and Ctrl+M never reach the
  control as characters (axDown AD-5.1; a read-only control takes none, the guard stays for the sound of it). The
  collision test covers the table.
- AP-3.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axpreview/user-guide.md` is embedded (Help → User
  guide); About is the bundle's (AX-9.9).

### AP-4 Announcements

On opening, NVDA speaks the window ("README.md - axPreview window") and the text ("README.md read only edit multi
line") and its first line; nothing of axPreview's own. Afterwards, one UI Automation notification each on the text,
or on the notice while one shows (AX-7.3): "Line n of m" (`l`, and after Go to line; "Not a line number"), "Install,
heading level 2" and the other elements, "No next <kind>" / "No previous <kind>", "Not found", "Reloaded",
"Reloaded, <name> changed on disk", "Word wrap on" / "Word wrap off", "Nothing to save here", "No file yet" (Ctrl+E,
Ctrl+W, F5 and the ways out on Untitled), "Text size n", "Windows text size", "Copied diagnostics". The notices: "Not a
text file", "Too large", Keyboard shortcuts, User guide, About, the update notice.

### AP-5 Settings, status bar, title, log

- AP-5.1 `%APPDATA%\Axit\axpreview.json` (AX-5.3; the editors' `EditorSettings`, since the viewer is the editor shell,
  AP-D4): `wordWrap` on, `recentFiles` (ten), and the window base's font, update check and window fields.
- AP-5.2 The status bar: the file's name on the left; "Line n, Column m · encoding · line ending" on the right, 300 ms
  after the last key (axDown AD-9). The title: "<name> - axPreview", "Untitled - axPreview" with no file.
- AP-5.3 The log is `%LOCALAPPDATA%\Axit\logs\axpreview.log`: the start, the exit, the files that could not be read
  with Windows' message; no file names.

## 5. User interface

The menu bar; the read-only text filling the window; the question area under it for Go to line; the status bar. The
notices are the bundle's overlay in place of the text (AX-7.2). The window has the accessible role Window (AX-7.1).

## 6. Architecture

```
src/AxPreview.Core/  class library (net10.0): TextSniff (whether a file's first bytes read as text, and the refusal
                     texts; axFile's Preview.IsText moved); references Axit.Core
src/AxPreview/       class library (net10.0-windows): AxPreviewApp (Run, Usage, the hand-over to an open window, the
                     EditorApp record with ReadOnly and Refuse), AxPreviewKeys (axDown's navigation letters, the
                     viewer's keys from EditorKeys.Viewer), HelpText; references AxPreview.Core, Axit.Core, Axit.Forms
tests/AxPreview.Tests/ TextSniffTests (the rule over bytes and files)
docs/axpreview/      this spec, user-guide.md (embedded), nvda-test-plan.md
```

The window is `Axit.Forms/TextEditorForm`, the text editor shell, in its read-only mode (AP-D4, decided at the build):
`EditorApp.ReadOnly` turns the editor control read-only, drops the Edit menu, Save and the history, and adds the
viewer's commands (Reload, Edit in the editor, `l` for the line, the hand-over received through `AppMessage`); the
settings are the editors' `EditorSettings` in `axpreview.json`. Nothing of the shell is copied; axDown and axJSON are
unchanged. What the viewer adds to the shell is marked with the flag in the shell's code, the one place a change to
the viewer is made (`src/Axit.Forms/CLAUDE.md`).

## 7. Design decisions

- **AP-D1 A viewer app, not a pane.** axFile's preview pane (AF-9.4, one night old) read a file in place, under the
  list. The user's direction of 2026-10-22: axFile is file operations and management; viewing is handled elsewhere;
  the apps stay simple and well scoped. A viewer of its own has the whole window, the editors' keys unchanged, Go to
  line and the status bar, and is reachable from File Explorer and the command line.
- **AP-D2 Enter is Windows', Shift+Enter is the viewer.** The user's decision of 2026-10-22: Enter on a file in axFile
  opens it in the program the user has made its default in Windows, so that axFile mirrors File Explorer, and the
  Axit apps register themselves as programs for their kinds so that the user can choose them there (the installer
  cannot set a default itself: Windows lets only the user do that, in Settings → Apps → Default apps or through
  Open with → Always; the guide says so). axPreview is then one Open with Axit choice among others, with Shift+Enter
  in axFile as its own key (Shift+Enter has no other meaning on a file in a list; on a folder axFile gives it File
  Explorer's: the folder in a new window). The started viewer takes the focus, since the user asked to read.
- **AP-D3 Text first, whole.** The pane cut a file at a megabyte because it sat under a list; the viewer takes what
  the editors take, 32 MB, and the EDIT control copes as axDown's does. Other kinds come when there is a way to read
  them that NVDA reads well (§9).
- **AP-D4 Shared with the editors, not copied.** The viewer is the editor shell without editing; the build (2026-10-22)
  gave `TextEditorForm` a read-only mode, two fields on `EditorApp` (`ReadOnly`, `Refuse`), rather than a second
  window on the shared pieces: the smaller change (B-10), and the editors' reload on activation, Go to line, status
  bar, word wrap, recent files, find and jumps come for nothing. Nothing is copied; axDown and axJSON are unchanged.
- **AP-D5 The hand-over is a window message** (axMedia's AM-D4), the same shared piece.

## 8. Testing

- `tests/AxPreview.Tests`: the text rule over bytes and over files in a temporary folder (`TextSniffTests`); the
  settings are the editors' (`EditorSettingsTests` in `tests/Axit.Tests`).
- `docs/axpreview/nvda-test-plan.md`: opening from axFile (Shift+Enter and Item → Preview, with and without a window
  open), from File Explorer (Open with Axit, and as the default for a kind) and the Start menu; the reading keys and the jumps on the tutorial file; `l`, Go to line, find; a
  binary file; a changed-on-disk reload; Ctrl+E into the editor; two viewer windows.

## 9. Open questions

- The caret handed to the editor on Ctrl+E (a line argument for axDown and axJSON on the command line).
- JSON's navigation keys in the viewer, once axJSON's outline is shared on this second use.
- Other kinds: a PDF's text, an image's description, an archive's listing; each a way of reading that NVDA reads well.
- Markdown links followed (Enter on a link opening the browser or the linked file in this window).

## 10. Status

- 2026-10-22: specified at the user's ask, with the inventory of the split in `docs/axit/axfile-split.md`.
- 2026-10-22, later the same day, on the branch `split`: built as the editor shell's read-only mode (AP-D4), with
  `TextSniff` in `AxPreview.Core` (tested), the keys, the texts, the guide, the test plan and the wiring (the
  dispatcher, the installer's block with Open with for the text kinds, the icon, `run-axpreview.ps1`); a file alone
  on the command line still goes to its editor (AP-1.1). axFile handed over and lost its preview pane in the next
  step of the inventory, the same day.
- 2026-10-22: released in 3.3.1 (3.3.0 tagged, not published). The whole NVDA test plan marked passed at the user's word, no step reported failing; the record by step is `docs/axit/nvda-test-record.md`.
