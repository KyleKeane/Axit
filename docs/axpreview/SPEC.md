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

Since the user's direction of 2026-10-24 axPreview is the general viewer, offered by Windows for every file type: two
panes, the **content** above (the read-only text as before) and the **details** under it, a list of everything that
can be read about the file (its size, its format, its dates, and whatever Windows' own property system and the
file's renderer know). What the content pane shows is chosen by the file's kind through one table of **renderers**
(AP-7): a text file's text, an archive's listing, a document's text, and for any other file the readable text inside
it, so that every file shows something and the details say what it is.

## 2. Goals and non-goals

- **G1 One text, native.** A read-only EDIT control NVDA reads line by line, a status bar, the bundle's notices
  (AX-7.1); nothing is edited here, so nothing can be lost here.
- **G2 Reading first.** Every key is a reading key; the editors' Ctrl letters work the same, so that a file read in
  axPreview and edited in axDown is the same text under the same keys.
- **G3 One viewer.** axFile hands files to the axPreview window that is open; a second window is a deliberate act (the
  Axit menu's New window).
- **G4 Every file, as text.** Text files in version one (3.3.1); since 2026-10-24 every kind, each through its
  renderer into the same read-only text (AP-D6), with the details pane beside it in the reading order (AP-6).
- **G5 Nothing spoken unprompted** (AX-7.3).
- **Non-goals.** Editing of any kind (Ctrl+E is the way out); pictures, sound or video shown or played (the details
  say what they are; axMedia plays, axOCR is planned for the text in a picture); a rendering of Markdown (the text is
  the text; the navigation keys are the structure); syntax colouring; tabs or a list of open files.

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
  every file, axPreview under Windows' own Open with for every file type (since 3.7.2, the general viewer to come,
  `docs/axit/plan.md`; the text kinds `.txt`, `.md`, `.markdown`, `.log`, `.json` until then; type name
  "File (axPreview)"), so that the user can make it the default program for a kind in Windows'
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
Ctrl+W, F5 and the ways out on Untitled), "Text size n", "Windows text size", "Copied diagnostics"; in the Details
list (AP-6.3) "Copied", "The clipboard is busy; try again" and "Enter opens the Folder line in axFile". The notices:
"Too large" ("Not a text file" until 2026-10-24, AP-7.5), Keyboard shortcuts, User guide, About, the update notice,
and an error naming a file that could not be opened.

### AP-5 Settings, status bar, title, log

- AP-5.1 `%APPDATA%\Axit\axpreview.json` (AX-5.3; the editors' `EditorSettings`, since the viewer is the editor shell,
  AP-D4): `wordWrap` on, `recentFiles` (ten), and the window base's font, update check and window fields.
- AP-5.2 The status bar: the file's name on the left; "Line n, Column m · encoding · line ending" on the right, 300 ms
  after the last key (axDown AD-9). The title: "<name> - axPreview", "Untitled - axPreview" with no file.
- AP-5.3 The log is `%LOCALAPPDATA%\Axit\logs\axpreview.log`: the start, the exit, the files that could not be read
  with Windows' message; no file names.

### AP-6 The details pane (2026-10-24)

- AP-6.1 **Two panes.** The content, the read-only text of AP-2, above; the details under it, the bundle's list
  (`AxitList`, AX-8.6) named "Details", a third of the window's height, always there ("No file" as its one line in an
  untitled window, "Reading details" while another file's are read; the same file's lines stay until the new ones
  come, so that a reload does not move the user). Tab and Shift+Tab (the read-only text takes no Tab; the Go to line
  field comes between while it shows), and the bundle's Ctrl+Tab, Ctrl+Shift+Tab and F6, move between the two; a file
  opened or handed over puts the focus on the content, as now.
- AP-6.2 **What it lists**, one line each, "Name: value", in three groups with a heading line each, as axFile's
  Actions list has sections:
  1. **File:** Name, Folder, Size ("12.1 KB, 12,345 bytes"), Type (Windows' name for the kind, "Markdown File", and
     the extension), Modified, Created, Accessed (the dates as the bundle writes them, with the time), Attributes
     (Read-only, Hidden, System, Compressed, Encrypted, Offline, a link; Archive and Normal, set on nearly every file,
  are not said; the line left out when none is set).
  2. **Contents:** what the renderer found (AP-7): for a text, the encoding, the byte order mark, the line ending,
     the lines, the words and the characters; for an archive, the entries, the files and folders, the packed and
     unpacked sizes; for a document, the paragraphs or slides; for any other file, how much readable text was found.
  3. **Windows:** every other property Windows' property system holds for the file, by its own display name and
     formatted as Windows formats it (`SHGetPropertyStoreFromParsingName`, `PSGetPropertyDescription`,
     `PSFormatForDisplay`): a picture's dimensions and resolution, a sound's length, bit rate, artist and album, a
     video's frame size and rate, a document's author, title, pages and words, a program's version and company,
     whatever the kind's property handler gives. Empty values and the ones already listed above are left out.
- AP-6.3 **Keys in the pane:** the list's own (the arrows, Home, End, type-ahead, the edge earcon); Ctrl+C copies the
  line's value ("Copied", the value alone, without its name); Enter on the Folder line opens the folder in axFile with
  the file selected (AP-1.4's way out). Nothing is spoken on an arrow key beyond NVDA's own reading of the line.
- AP-6.4 **Read off the UI thread** (bundle rule 3): the file group from the file system, the contents from the
  renderer, the Windows group from the property store on a thread of its own (an STA thread, the property handlers
  being COM objects), each filling its group when it is done; a read that comes back after another file was opened is
  dropped. A property store that takes more than five seconds (a file on a slow share) leaves the Windows group with
  the line "Windows' details took too long", the rest shown.
- AP-6.5 Reload (F5) and the reload on coming back read the details again with the text; the line the user is on stays
  by its name.

### AP-7 The renderers (2026-10-24)

- AP-7.1 **One table** (`Renderers` in `AxPreview.Core`): each renderer has a name ("Text", "Zip archive"), the file
  types it takes by extension, an optional test of the first bytes (a zip's "PK"), and a function from the file to
  what the content pane shows, a text and the lines of the Contents group (AP-6.2). The first renderer that takes a file
  shows it; the last takes every file. A new kind is one entry and its tests; nothing in the window changes.
- AP-7.2 **The renderers of the first build**, in the table's order:
  1. **Text:** a file that reads as text by AP-2.2's rule, shown as before, with every key of AP-2.
  2. **Word document** (`.docx`), **PowerPoint** (`.pptx`), **OpenDocument text** (`.odt`): the document's text, a
     paragraph to a line, read from the XML inside the file (`System.IO.Compression`, no package added): headings
     written as Markdown headings ("# ", "## ") where the document marks them, a slide as "## Slide 3" followed by
     its text, so that Ctrl+H and the level keys move by heading and slide; tables as their cells separated by tabs.
  3. **Zip archive** (`.zip` and any file that starts as one): the listing, one entry to a line, the path, the
     unpacked size and the date ("docs/SPEC.md, 41 KB, 2026-10-24 14:02"), folders first.
  4. **Picture, sound, video:** a short text that names the kind and says that its details are in the pane below, and
     for sound and video that Ctrl+E plays it in axMedia; the content of these kinds is not text and their readable
     strings are noise.
  5. **Anything else:** the readable text inside the file, the runs of four or more printable characters (ASCII and
     UTF-16) one to a line, under a first line "No preview for this kind yet; the readable text inside it follows.",
     from the first 32 MB of the file (the editors' limit), then "Shown from the first 32 MB." for a larger one. A
     file with no readable text says so in one line, "No readable text was found in <name>."
- AP-7.3 **The way out follows the renderer** (Ctrl+E, AP-1.4): Edit in axDown or axJSON for a text; Play in axMedia
  for sound and video; Open in Windows' program for everything else (the program Windows has for the kind, axFile's
  Enter, and Windows' Open with dialog when it has none). The File menu's item is named after it as the file opens.
  **File → Open with ▸** (the user's choice of 2026-10-24, over a list of defaults of Axit's own): the Axit apps that
  are programs for the file's type (their Open with types of `Dispatch.Apps`) and axDown for any text, numbered
  ("&1. axDown"), then Windows' program for it and Choose another program… (Windows' own dialog,
  `Axit.Forms/WindowsOpenWith`). A choice opens the file once; a default is set where Windows keeps them, with that
  dialog's Always or in Settings, Default apps (B-9), never in a table of axPreview's (AP-D10).
- AP-7.4 The status bar's right part is the renderer's: "Line n, Column m · encoding · line ending" for a text,
  "Line n, Column m · Word document" for the others.
- AP-7.5 The 32 MB file limit (AP-2.2) is the text renderer's; the others read the file as far as they need (an archive
  its directory, a document its text parts) and stop at 32 MB of text. "Not a text file" goes: every file opens.

## 5. User interface

The menu bar; the read-only text above, the Details list under it (AP-6); the question area under the text for Go to
line; the status bar. The notices are the bundle's overlay in place of the panes (AX-7.2). The window has the
accessible role Window (AX-7.1).

## 6. Architecture

```
src/AxPreview.Core/  class library (net10.0): TextSniff (whether a file's first bytes read as text, and the refusal
                     texts; axFile's Preview.IsText moved); Renderers and Rendered (AP-7: the table, the text, the
                     archive, the document, the media and the readable-text renderers); FileDetails (AP-6.2: the
                     File group, and the Windows group through the property store by P/Invoke); references Axit.Core
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
the viewer is made (`src/Axit.Forms/CLAUDE.md`). Since 2026-10-24 the record also carries the viewer's two
functions in place of `Refuse`: `Render` (a path to the text and the renderer, AP-7) and `Details` (a path to the
details' lines, AP-6), and the shell's read-only mode adds the Details list under the text when `Details` is set
(AP-D8).

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
- **AP-D6 Every kind becomes text** (2026-10-24). The content pane stays the one read-only EDIT control for every
  file, and a renderer turns a kind into text: NVDA reads one control the same way whatever the file, every reading
  key works on every kind, and no web view, picture control or third-party library enters the bundle (B-10).
- **AP-D7 The details come from Windows and the renderer.** Windows' property system already knows a picture's
  dimensions, a song's length and a document's author for every kind it has a handler for, through one API; the
  pane lists all of it, the Details tab of File Explorer's Properties as one list, with what the renderer counted.
- **AP-D8 Two panes in the shell's read-only mode**, not a window of axPreview's own: the details list is one more
  control of the read-only mode, set by a function on the record, so AP-D4 holds and axDown and axJSON are unchanged.
- **AP-D9 The routing is one table, first match** (the user's ask, "a way to route the correct file type to the
  right renderer"): the renderers in order, each by its extensions and its first bytes, the last taking every file,
  so that what opens a kind is one place to read and change.
- **AP-D10 Defaults stay Windows'** (2026-10-24). The user asked for an Edit with of axPreview's own, a list of
  default programs like Windows' for the Axit apps, and a key to edit with the default or choose one, then asked for
  the level that avoids feature creep. The recommendation, taken: Ctrl+E's one way out and an Open with submenu, with
  Windows' dialog for any other program and for setting a default. A list of Axit's own would copy what Windows does
  accessibly already, and would disagree with it whenever the user changed one and not the other.

## 8. Testing

- `tests/AxPreview.Tests`: the text rule over bytes and over files in a temporary folder (`TextSniffTests`); the
  settings are the editors' (`EditorSettingsTests` in `tests/Axit.Tests`); since 2026-10-24 `RenderersTests` (the
  table's choice for each kind, and each renderer over files made in a temporary folder: a .docx, a .pptx and an .odt
  written as their XML in a zip, a zip, a binary with strings in it, an empty file) and `FileDetailsTests` (the File
  group of a temporary file, and that the Windows group of a text file holds at least its size and type).
- `docs/axpreview/nvda-test-plan.md`: opening from axFile (Shift+Enter and Item → Preview, with and without a window
  open), from File Explorer (Open with Axit, and as the default for a kind) and the Start menu; the reading keys and the jumps on the tutorial file; `l`, Go to line, find; a
  binary file; a changed-on-disk reload; Ctrl+E into the editor; two viewer windows.

## 9. Open questions

- The caret handed to the editor on Ctrl+E (a line argument for axDown and axJSON on the command line).
- JSON's navigation keys in the viewer, once axJSON's outline is shared on this second use.
- Other kinds: a PDF's text (Windows has no text API for PDF; a reader of its own is a larger piece), an Excel
  workbook's sheets, an e-book's chapters (an EPUB is a zip of XHTML), a picture's text through axOCR (planned).
- Rendering on a task of its own: a renderer runs on the UI thread, as the text read always has; a large binary's
  readable text (up to 32 MB scanned) holds the window for a moment.
- Markdown links followed (Enter on a link opening the browser or the linked file in this window).

## 10. Status

- 2026-10-22: specified at the user's ask, with the inventory of the split in `docs/axit/axfile-split.md`.
- 2026-10-22, later the same day, on the branch `split`: built as the editor shell's read-only mode (AP-D4), with
  `TextSniff` in `AxPreview.Core` (tested), the keys, the texts, the guide, the test plan and the wiring (the
  dispatcher, the installer's block with Open with for the text kinds, the icon, `run-axpreview.ps1`); a file alone
  on the command line still goes to its editor (AP-1.1). axFile handed over and lost its preview pane in the next
  step of the inventory, the same day.
- 2026-10-22: released in 3.3.1 (3.3.0 tagged, not published). The whole NVDA test plan marked passed at the user's word, no step reported failing; the record by step is `docs/axit/nvda-test-record.md`.
- 2026-10-24: the general viewer specified at the user's ask on the branch `preview` (AP-6, AP-7, AP-D6 to AP-D10):
  two panes, the details, the renderers, the way on; built the same day on the branch, with `RenderersTests`,
  `FileDetailsTests` and the script `tests/scripts/axpreview/04-details-and-kinds.txt`. Checked with NVDA the
  same day at the user's word (test plan section 5 and step 1.9, `docs/axit/nvda-test-record.md`); released in 3.7.2.
