# axDown — Specification

axDown is the second app of the Axit bundle (`docs/axit/SPEC.md`): a barebones editor for Markdown and plain text files
for a blind developer who uses the NVDA screen reader. Everything in sections 4 and 9 is built and checked with NVDA (the record: `docs/axit/nvda-test-record.md`). The
document is kept in step with the code (a behaviour change updates it in the same commit). Requirements are numbered
`AD-n.m` and decisions `AD-Dn`. The bundle's rules that every app follows, AX-7 (accessibility) and AX-8 (keys), are
cited by number and not repeated.
In axDown's code and documents, "SPEC.md" means this file.

## 1. Summary

axDown opens one text file in one window: a menu bar, one plain multi-line edit control with the whole file in it,
and a status bar. It reads and writes the file exactly as it found it (encoding, byte order mark, line endings), asks
before unsaved changes are lost, finds text, goes to a line, and shows the line and column on demand. That is all of
version one. Navigation by Markdown structure (headings and their levels, links, lists and list items, block quotes,
tables) is section 9, and everything else people expect of an editor (several files, colouring, a preview, spelling) is
deliberately absent: those are other apps of the bundle or not wanted.

## 2. Goals and non-goals

- **G1 A plain Windows edit field.** The editor is the native EDIT control, the control NVDA reads best: characters,
  words, lines and selection are spoken by NVDA itself, with its own keys, and axDown adds nothing to that reading.
- **G2 Faithful files.** A file comes back from axDown byte for byte the same where it was not edited: the encoding,
  the byte order mark and the line endings are kept.
- **G3 Barebones.** Every feature in section 4 is there because a text file cannot be edited safely without it.
  Anything that can wait, waits.
- **G4 Information on demand.** Nothing is spoken on a plain key press; position, state and results are given when
  asked for or when a command finishes (AX-7.3).
- **G5 Shared behaviour.** Notices, the question area, paging, text size and the update check are the bundle's
  (AX-6), so a user of axClaude already knows them.

Non-goals (version one): several files or tabs in one window (open a second window instead, AD-D3); syntax
colouring or any formatting; a Markdown preview; auto-save; spelling; printing; a replace command; encodings beyond
UTF-8, UTF-16 and the system code page; files that are not text; files over 32 MB (AD-3.7).

## 3. Scenarios

- S1 Open from File Explorer: right-click `notes.md`, Open with Axit, axDown; the window opens with the caret at the start
  and NVDA reads the editor's name, which is the file's name.
- S2 Start empty: Start menu, Axit axDown; an empty document named Untitled; type; Ctrl+S asks for a name.
- S3 Edit and save: arrows and NVDA's reading keys to move, type, Ctrl+S; "Saved" is spoken, the title loses its
  changed mark.
- S4 Find: Ctrl+F, type, Enter lands on the match and NVDA reads its line; F3 for the next one; "Not found" when
  there is none.
- S5 Go to a line: Ctrl+G; the field is named with the current line and column; type a number, Enter.
- S6 Leave with unsaved changes: Alt+F4; a notice asks Save, Don't save or Cancel; Enter saves.
- S7 The file changed on disk while open: Ctrl+S warns and asks before writing over it.
- S8 Console: `axdown notes.md`, or `axit notes.md`; `axdown new.md` opens an empty document that gets the name on
  the first save.

## 4. Functional requirements

### AD-1 Launch

- AD-1.1 `Axit.exe down [<file>]`; the console command `axdown` stands for `Axit down`, and a file alone given to
  `Axit.exe` or `axit` reaches here (AX-1). `--help` and `--version` answer as in axClaude (FR-9.5 there).
- AD-1.2 With an existing file, it is opened (AD-3). With a path that does not exist, the document is empty and has
  that name; the file is created on the first save. Without a file, the document is empty and called Untitled.
- AD-1.3 One window, one document (AD-D3). A second file opens a second window, as File Explorer does.
- AD-1.4 `AxDownApp.Run(args)` is the entry point (AX-1.7): one call into the editor shell's `EditorHost.Run` with
  axDown's `EditorApp` (its name, verb, settings file, file types, F1 text, guide, Markdown keys and resources),
  which names the log, reads the settings (AD-8), opens the shell's `TextEditorForm` and owns the crash handling.
  The window was axDown's own `EditorForm` until axJSON, the second editor, when it moved to `Axit.Forms` (AX-6.3).

### AD-2 The editor

- AD-2.1 A `TextBox`: Multiline, AcceptsTab, AcceptsReturn, WordWrap as the setting says (on by default),
  ScrollBars Vertical (Both while word wrap is off), HideSelection false, MaxLength 0. Its `AccessibleName` is the
  file's name (`notes.md`), or Untitled, so that NVDA says the name when the focus arrives (AX-7.1).
- AD-2.2 The text is the file's, with line endings normalised to `\r\n` for the control; the original line ending
  is remembered and written back (AD-3.4).
- AD-2.3 Page Up and Page Down move one screen and reach the ends through the bundle's `EditPaging` (as axClaude's
  FR-2.1): the control's own keys do nothing while the text fits. Ctrl+Up and Ctrl+Down move to the start of the
  previous and next paragraph, a paragraph starting at the first line or at a line with text directly after a blank
  line (Ctrl+Up from inside one goes to its start first, Ctrl+Down from the last to the end of the file), and
  Ctrl+Shift+Up and Ctrl+Shift+Down select to there from the selection's anchor, as in Word, through the bundle's
  `EditParagraphs` (AX-8.2): the control's own keys scroll a row and leave the caret where it is, as Notepad's do,
  which NVDA, reading at the caret after the key, reported as the same text again. NVDA reads the row the caret
  lands on or the selection change; the app says nothing.
- AD-2.4 Turning word wrap off and on keeps the caret where it was. (WinForms recreates the control's handle on that
  change and loses the position; the form saves `SelectionStart` and restores it, then scrolls to the caret.)
- AD-2.5 **Undo and redo** (specified and built 2026-10-21 at the user's ask; the control's own one-level Ctrl+Z
  before). The window keeps a history of its own of every change to the text since the file was opened in
  it: a run of typing is one step until a pause of a second or the caret leaves the run's end, a deletion the same;
  a cut, a paste, a Format (axJSON, AJ-2.3) and a reload from disk (AD-3.6) are one step each. **Ctrl+Z** takes the
  last step back and **Ctrl+Y** (or Ctrl+Shift+Z) puts it back, in Edit as Undo and Redo; the caret goes to where
  the change was (after the text put back: where a run of typing began, after a deletion restored), and the one
  announcement is "Undone" or "Redone", "Nothing to undo" or "Nothing to redo" at the ends (the line is read on
  demand through Go to line, AD-6.4). Save does not clear the history, so a save can be undone past; Open, New and
  the window's close end it: nothing reaches back into an earlier file, window or session, which is the user's
  rule. The history is pure code, `Axit.Core/EditHistory`, tested: each step holds the changed span's position, the
  text removed and the text inserted, found from the text before and after a change by the common prefix and
  suffix, the caret settling which of equal places it was when the texts allow several ("a" typed next to an "a");
  a change made by a typed key (a character, Backspace, Delete) at the open run's caret joins the run, any other
  change (a paste, a cut, a command, a reload) is a step of its own and closes the run; the step is applied back
  through the selection (`Select` and `SelectedText`, as Format is), so that NVDA hears nothing but the
  announcement and the control's own undo stays coherent. The history is bounded by the size of its deltas, a few
  megabytes, the oldest steps dropped first. An undo is a change: the unsaved mark stays until the next save.
- AD-2.6 The caret never moves except on the user's own command (AX-7.3): a save, a reload or a word wrap change
  leaves it where it was.
- AD-2.7 The font is the setting's (family, size, bold), the Windows system font when none is set; Ctrl+Plus and
  Ctrl+Minus change the size in steps and remember it (AX-8.2); Options, Font opens the Windows font picker (the only
  separate windows are the Windows pickers, AX-7.2).

### AD-3 Files

- AD-3.1 Reading: the bytes are decoded as UTF-8 with a byte order mark, UTF-16 little or big endian with a mark,
  otherwise UTF-8 when the bytes are valid UTF-8, otherwise the system code page (`Encoding.Default` is UTF-8 in
  .NET; the ANSI page comes from `CodePagesEncodingProvider`, built into .NET, no package). When the system page is
  UTF-8 itself (Windows' "Use Unicode UTF-8 for worldwide language support" option), Windows-1252 is used instead:
  the bytes were just found not to be UTF-8, and decoding them as UTF-8 would replace every non-ASCII byte.
- AD-3.2 The encoding and whether there was a mark are remembered with the document and shown in the status bar
  ("UTF-8", "UTF-8 with BOM", "UTF-16 LE", "Windows-1252").
- AD-3.3 Writing: the text goes back in the remembered encoding, with the mark when there was one. A new document is
  UTF-8 without a mark.
- AD-3.4 Line endings: the file's first line ending (`\r\n`, `\n` or `\r`) is remembered and used for every line on
  save; a file with none keeps `\r\n`. The status bar shows it ("CRLF", "LF"). A lone LF or CR in the text (pasted
  from a Unix clipboard) goes out as the file's ending too (`TextDocument.ToBytes`, tested; until 2026-10-23 a CRLF
  file gained bare LFs).
- AD-3.5 Save writes the file in place (as Notepad does) and then reads its last write time, which Ctrl+S and the
  close check compare against (AD-3.6). Save as asks for a path with the Windows file picker, then saves and renames
  the document (title, editor name, status bar) once the write has succeeded; a write that fails is the error notice
  and the document stays as it was (until 2026-10-23 it was renamed first).
- AD-3.6 Changed on disk: the file's last write time is compared with the one remembered when the window becomes
  active again (`OnActivated`, but not as one of the Windows dialogs closes, which activates the window before Save as has
  written, 2026-10-23; nothing is watched or polled while it stays active, since the user leaves it to let
  another program, Claude, work and comes back) and at the moment of saving. Back in the window with no unsaved
  changes and a changed file, the editor reloads it, keeps the caret on its line and column (on a line that got shorter, at its end, never
  between a CR and its LF: `TextLines`, tested, 2026-10-23), and says "Reloaded,
  <name> changed on disk"; a reload that cannot read the file (another program still holds it) is the error notice
  with the text kept, and the file is not compared again until the next read or save (until 2026-10-23 the editor
  was emptied and "Reloaded" said over the error); with unsaved changes it says once, until the file changes again,
  "<name> changed on disk; Save will ask". Save with a changed file asks through a notice ("<name> was changed by another program since you
  opened it. Write over it, or reload it and lose your changes?") with Write over (Enter), Reload and lose mine, and
  Cancel (Escape); Reload drops the unsaved text and reloads with the caret kept, saying "Reloaded". No merging,
  which is git's work, and no deleted-file case (a file that is gone is simply not compared). Asked for on
  2026-10-18 with axClaude editing files open in axDown; the same in axJSON, the same shell.
- AD-3.7 A file over 32 MB is refused with a notice, since the EDIT control stops being usable long before that, and
  so is a file that is not text: one whose decoded text holds a NUL character (a PNG, UTF-16 without a mark), since
  the EDIT control keeps only the text before the first NUL and a save would write that truncation back. A file that
  cannot be opened (over the limit, not text, locked, no permission) is reported in a notice (the limit's names the
  size in bytes and the limit, without an app's name, since the reader is every editor's; until 2026-10-23 it said
  axDown in axJSON, and a file one byte over said it was 32 MB) and the window stays
  open with an empty, untitled document, so that Save cannot write the empty editor over the file (Save as names a
  new one). A missing file opens as an empty document of that name, created by the first save.
- AD-3.8 Open, New and Exit check for unsaved changes first (AD-6.1).
- AD-3.9 All of this is pure code in `Axit.Core/TextDocument` (shared since axJSON; bytes in, text plus encoding plus line ending out,
  and back), unit tested for every encoding and line ending case, with the window only calling it.

### AD-4 Commands and menus

- AD-4.1 File, shaped as axClaude's Session menu: New (Ctrl+N), Open… (Ctrl+O), Open recent file ▸ (the last ten
  files opened or saved under a name, newest first, those that still exist, kept in the settings, each as its number, a
  full stop, the file's name and its full path, "1. notes.md (C:\notes\notes.md)", the Axit menu's shape, AX-9.5; choosing one opens it
  in this window after the unsaved-changes question; "No recent files", an item that does nothing, when there are
  none, AX-9.5); then Save (Ctrl+S), Save as… (Ctrl+Shift+S), where every Windows program has them (the standard
  order of a File menu, shared with axClaude's Session menu, whose Save slot is empty, 2.11.0); then Show current file in ▸
  File Explorer (Ctrl+W; the file selected in File Explorer, one of the system's own windows; "No file yet" on
  Untitled), axGIT (a new axGIT window on the repository that holds the file, docs/axgit/SPEC.md AG-1.4), axTerminal
  (a shell in the file's folder, docs/axterminal/SPEC.md AT-1.5) and axFile (the file's folder listed with the file
  selected, docs/axfile/SPEC.md AF-1.3); then Exit (Alt+F4).
- AD-4.2 Edit: Undo (Ctrl+Z) and Redo (Ctrl+Y) over the window's own history (AD-2.5), then Cut, Copy, Paste,
  Select all (the control's own, listed for discoverability).
  Navigate (as axClaude's): Find… (Ctrl+F), Find next (F3), Find previous (Shift+F3), Go to line… (Ctrl+G), then the
  submenus Next and Previous with one item per kind of section 9, made from the key pairs of the table: the kind's
  name with NVDA's letter as the mnemonic ("Heading", "Heading level 1" to "6", "Link", "List", "List item", "Block
  quote", "Table") and the key, Ctrl+letter under Next and Ctrl+Shift+letter under Previous (AD-5, §9).
- AD-4.3 Options, as axClaude's: every setting lives in the submenu for what it is about, none outside one. Text ▸:
  Word wrap (checked; Ctrl+Shift+W), Font…, Larger text (Ctrl+Plus), Smaller text (Ctrl+Minus), Windows text size.
  General ▸: Check for updates at start (bundle AX-3.1). Developer ▸: Open settings file (axdown.json, written first, in a new axJSON window; the window base's
  `OpenSettingsFile`, the same in every window since 2.12.0, a new axDown window before), Copy diagnostics (AD-11).
- AD-4.4 Help: Keyboard shortcuts (F1), User guide, Markdown resources ▸ (Markdown basics, `https://commonmark.org/help/`;
  GitHub Flavored Markdown, `https://github.github.com/gfm/`; in the browser), as axClaude's Help has Claude Code
  resources; the app's own items only.
- AD-4.5 Axit: the bundle's menu (AX-9), between Options and Help, the same in every window of Axit: New window (axClaude,
  axDown), Recently closed (the last ten files and folders any Axit window held, AX-9.4; axDown adds its file when the
  window closes or opens another); Open windows (the other open Axit windows, most recently used first), Open windows
  by type (axClaude and axDown, that app's windows, this one checked), Close all windows (every Axit window closes as
  on Alt+F4, asking about unsaved changes where there are any); Check for updates (Axit n) and, while a newer release
  is known, Update to Axit n, as axClaude has them (FR-1.10 there, the bundle's `Updater`, AX-3: Update and restart,
  Update and close, Open release page, Later; one progress bar for the download and the in-place install, Cancel
  during the download; after Update and restart axDown starts the new version itself, on the file or empty), and
  About, the bundle's sections (AX-9.9: Version, Licence, Attribution, Axit guide, Axit on GitHub). Enter on a window
  brings it to the front, on a closed file opens it in a new window.
- AD-4.6 Every menu item has a mnemonic and a shortcut or a shortcut text (AX-7.1); the Axit menu's window items,
  made when it opens, have their number as the mnemonic and no shortcut.

### AD-5 Keys

Every key is bundle-wide (the same in every window of Axit, `Axit.Forms/BundleKeys`) or window-specific
(`axDown/AxDownKeys`) (AX-8). The table is the spec of `AxDownKeys`; the F1 text reads from it.

Bundle-wide (AX-8.2): F1 shortcuts; Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the window's
controls (in axDown there is one, so they stay in the editor; in a notice they move between its text, field and
buttons); Ctrl+F find, F3 and Shift+F3 next and previous; Ctrl+S save; Ctrl+Plus and Ctrl+Minus text size; Enter
and Escape on a notice; Page Up and Page Down one screen in an edit control; Ctrl+Up and Ctrl+Down the previous and
next paragraph, with Shift selecting to there (AD-2.3); Alt+F4 exit; Alt and F10 the menu.

Window-specific: Ctrl+N new document; Ctrl+O open; Ctrl+Shift+S save as; Ctrl+W show the file in File Explorer (axClaude's key for its folder); Ctrl+Z undo, Ctrl+Y or Ctrl+Shift+Z redo (AD-2.5); Ctrl+G go to line; Ctrl+Shift+W word wrap;
Ctrl+H heading, Ctrl+1 to Ctrl+6 a heading of that level, Ctrl+K link, Ctrl+L list, Ctrl+I list item, Ctrl+Q block
quote, Ctrl+T table, each the next one, and with Shift the previous one (section 9).
Everything else in the editor is the control's own: arrows, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left and
Ctrl+Right by word, Shift with any of them to select, Ctrl+A, Ctrl+C, Ctrl+X, Ctrl+V, Delete, Backspace,
Ctrl+Backspace, Tab (types a tab), Enter (a new line).

- AD-5.1 In an EDIT control Ctrl+H, Ctrl+I, Ctrl+J and Ctrl+M arrive as the characters Backspace, Tab, line feed
  and Enter. A handled key sets `SuppressKeyPress`; a key in that group that axDown does not handle is swallowed
  the same way, so that Ctrl+M never inserts a line.
- AD-5.2 NVDA's own key combinations are never intercepted; no global hook (AX-7.4).

### AD-6 Notices and questions

All through the bundle's `OverlayPanel` and `ControlArea` (AX-6.4, AX-7.2): drawn in the window over the editor,
never a separate window.

- AD-6.1 Unsaved changes: "Save changes to notes.md?" with Save (default), Don't save and Cancel; before New, Open and
  Exit, and when Windows closes the window. Save with no name yet opens the file picker first. Cancel on the way out
  after Update and restart keeps this window on the old program file and says "Update installed. It starts with the
  next axDown" (AX-3.2); until then a later Exit started the new version as if the restart had just been chosen.
- AD-6.2 Changed on disk (AD-3.6); file errors (AD-3.7); the update notice (AX-3); Keyboard shortcuts; User guide;
  About. In the text of any of them, the navigation keys of section 9 work as in the editor (bundle AX-7.2).
- AD-6.3 Find: the notice with the field "Text to find" (as axClaude's); Enter finds the next match from the caret,
  wrapping once at the end, selects it and returns the focus to the editor, where NVDA reads the selection's line.
  "Not found" is spoken when there is none. F3 and Shift+F3 repeat without the notice.
- AD-6.4 Go to line: a field in the question area under the editor, named "Go to line (now line 12, column 4)";
  Enter moves the caret to the start of that line (clamped to the last) and says "Line n of m", or "Not a line
  number" when the text is not a number, the caret staying; Escape cancels; the name is how the position is read on
  demand (G4), and the status bar carries it too (AD-9).

### AD-7 Announcements

On opening, NVDA speaks the window ("README.md - axDown window"), then the field ("README.md edit multi line"): the
window has the accessible role Window, as axClaude's has, so that NVDA names it; nothing of axDown's own is spoken
then. Afterwards, spoken through one UI Automation notification on the editor (AX-7.3), and only these: "Saved" (after a save and
after Save as); "Reloaded, <name> changed on disk" and "<name> changed on disk; Save will ask" on coming back to the window, "Reloaded" after Reload and lose mine (AD-3.6); "Not found" (find); "Line n of m" after Go to line, or "Not a line number"; "Word wrap on" / "Word wrap off"; "Undone", "Redone", "Nothing to undo", "Nothing to redo" (AD-2.5); "Text size n"; "No file yet" (Show current file in on Untitled);
"Install, heading level 2" after a heading jump, "No next heading" / "No previous heading" when there is none;
"Copied" is NVDA's own. Opening a file speaks nothing of its own: the focus arrives in the editor and NVDA reads
its name (the file's) and the first line.

### AD-8 Settings

`%APPDATA%\Axit\axdown.json` through the bundle's `SettingsFile` (AX-5): `fontFamily` (null = system), `fontSize`
(0 = system), `fontBold`, `wordWrap` (true), `checkForUpdates` (true), `recentFiles` (the last ten files opened or saved under a name, newest first, AD-4.1), `window` (placement). A plain class,
`EditorSettings`, in `Axit.Core` (shared since axJSON, each editor app with its own file), with the sanity checks after loading as `AppSettings` has.

### AD-9 Status bar

Two labels through the bundle's `StatusLayout` (no `Spring`, fixed widths when the texts do not fit, as axClaude's
§6.2): the left one the document's state ("notes.md", "notes.md, unsaved changes"), the right one "Line 12,
Column 4 · UTF-8 · CRLF", updated on caret moves (the file's lines counted by scanning the text 300 ms after the last key, `Position`, since `EM_LINEFROMCHAR` counts wrapped rows while word wrap is on). NVDA
reads it with NVDA+End; nothing is spoken automatically.

### AD-10 Title

`notes.md - axDown`, `*notes.md - axDown` with unsaved changes (the Notepad convention, AD-D5), `Untitled - axDown`.

### AD-11 Errors and diagnostics

As axClaude's FR-13: an exception in a handler is logged to `%LOCALAPPDATA%\Axit\logs\axdown.log` and shown as a
notice; Options, Developer, Copy diagnostics puts the version, the paths and the log's tail on the clipboard.

## 5. User interface

```
| File  Edit  Navigate  Options  Axit  Help                         |
| notes.md (multi-line edit, the whole file)                        |
|                                                                   |
| notes.md, unsaved changes | Line 12, Column 4 · UTF-8 · CRLF      |
```

Controls and accessible names: the editor `TextBox` named after the file; the `MenuStrip`; the `StatusStrip` with
two labels and no names (NVDA+End reads the text); notices and the question area as the bundle's (AX-6.4). Minimum
size 500×300; the system font and colours, light or dark as Windows is (AX-7.7).

## 6. Architecture

```
src/AxDown/        class library (net10.0-windows): AxDownApp (Run, and axDown's EditorApp record), AxDownKeys,
                   HelpText (F1 text, the embedded user guide); references Axit.Core, Axit.Forms
src/Axit.Forms/    TextEditorForm (the window), EditorApp, EditorKeys, EditorHost: the text editor shell, axDown's
                   window until axJSON, shared since (AX-6.2, AX-6.3)
src/Axit.Core/     TextDocument (read and write with encoding and line ending), EditorSettings and MarkdownOutline,
                   axDown's pure code, shared since the second use (AX-6.3); tests in tests/Axit.Tests
docs/axdown/       this spec, user-guide.md (embedded, Help, User guide), nvda-test-plan.md
```

Shared pieces axDown uses (AX-6): `OverlayPanel` and `Notice`,
`ControlArea` (the Go to line field), `EditKeys` (the page and paragraph keys), `Announcer`, `StatusLayout`, the font handling, `Sounds` (the notice chime),
`BundleKeys`; from `Axit.Core`: `SettingsFile`, `AppPaths`, `Log`, `Updater`, `BundleInfo`.

Data flow: `TextDocument.Read(path)` gives text, encoding, mark and line ending; the form puts the text in the
control; `document.Write(path, text)` writes it back with the encoding, the mark and the line ending it was read with. No background thread: files
are read and written on the UI thread (a 32 MB file is the ceiling, AD-3.7).

## 7. Design decisions

- **AD-D1 The EDIT control, not RichTextBox.** It is the control axClaude already proved with NVDA; RichEdit reads
  differently, wraps differently and would invite formatting. The known costs (re-wrap near the top of a large text,
  one-level undo) are accepted for a document editor; the undo has been the window's own history since AD-2.5 (2026-10-21), and the control's own undo is out of
  reach since 2026-10-23: its buffer is emptied after each change, Alt+Backspace is the window's Undo, and the text's
  context menu is the window's own (Undo, Redo, Cut, Copy, Paste, Select all; the control's menu undid through its
  buffer, which the history then recorded as another step).
- **AD-D2 Barebones first.** Version one has what section 4 lists and nothing more, so that the window is simple
  to learn and the code simple to change (AX B-10); the navigation keys come second (section 9); collaborative and
  AI-assisted editing are other apps of the bundle, not features of this one.
- **AD-D3 One document per window.** Tabs or a file list are another control to explain and to focus; a second
  window costs nothing and Alt+Tab already handles it. The dispatcher starts a new process per file.
- **AD-D4 Files come back as they went in.** Encoding, mark and line ending are read once and written back; axDown
  never converts a file unless asked (there is no such command in version one).
- **AD-D5 The Notepad title convention**, `*name - axDown`: short, known, read by NVDA as "star name".
- **AD-D6 Go to line carries the position.** One key, Ctrl+G, both tells the line and column (in the field's name)
  and moves; a separate "where am I" key would be one more key to learn, and the status bar is there for NVDA+End.
- **AD-D7 Navigation letters are NVDA's browse-mode letters (2026-10-02).** The defaults are the keys NVDA
  users already know from web pages, with Ctrl for the next item and Ctrl+Shift for the previous: H heading, 1 to 6
  a heading of that level, K link, L list, I list item, Q block quote, T table. They are built in that order,
  headings first (Ctrl+H, Ctrl+Shift+H, then the levels), the others as the outline learns them. Known difference:
  Ctrl+1 and Ctrl+2 move the focus in axClaude's window; the collision test lists it, and `AxClaudeKeys` is where to
  decide whether axClaude's focus keys move. Ctrl+H is otherwise Replace in some editors, which
  axDown does not have.
- **AD-D8 No file watching.** The changed-on-disk check at save time catches the case that loses work; a watcher
  would speak at the wrong moments and add a thread.
- **AD-D9 Open with, not the default.** axDown registers for `.md`, `.markdown` and `.txt` and is in the Open
  with Axit submenu of every file; Windows decides defaults (AX B-9).

## 8. Testing

Unit tests (xunit, `tests/Axit.Tests`, where axDown's shared pure code is tested): `TextDocumentTests` with byte arrays for UTF-8 with and without a mark,
UTF-16 LE and BE with marks, Windows-1252, `\r\n`, `\n`, `\r` and no line ending, round trips that reproduce the
bytes, a new document's defaults; `EditorSettingsTests` (AD-8) and `EditHistoryTests` (AD-2.5); `TextJumpsTests`
and `MarkdownOutlineTests` for the navigation keys (§9). The window has no automated tests; `docs/axdown/nvda-test-plan.md` covers it, and
the off-screen probe approach of axClaude checks layout and keys without disturbing the user.

## 9. Markdown navigation (headings since 2.1.0, the rest since 2.9.0)

- `Axit.Core/MarkdownOutline` (the bundle's since its second use, AX-6.1): from the text and a caret position, the next and previous element of each kind, outside
  fenced code blocks (` ``` ` or `~~~`); pure, tested. Headings: a line of one to six `#` followed by a space (or
  hashes alone), after at most three spaces; the text without the marks and closing hashes, and the level, which the
  level keys filter by. Links: `[text](target)`, `[text][ref]`, `<https://…>` and bare addresses starting with
  http:// or https://; not images (`![alt](src)`) and nothing inside a code span. List items: a line with a bullet
  (`-`, `*`, `+`) or a number with `.` or `)` and a space, at any indentation. Lists: a run of items of one marker
  kind at one indentation, with everything indented deeper and the blank lines followed by more of it; an item
  indented less or of another kind starts the next list; the items counted are the list's own, not the nested ones.
  Block quotes: a run of lines starting with `>` after at most three spaces. Tables: a header line with cells between
  pipes, a delimiter row of dashes under it, then the body lines with a pipe. The block kinds are found by line: next
  means on a line after the caret's, previous on a line before it, so a jump from inside one never lands on itself.
  Links are inline: next starts after the caret, previous ends at or before it.
- Keys per AD-D7, NVDA's browse-mode letters with Ctrl for the next and Ctrl+Shift for the previous: H any heading,
  1 to 6 a heading of that level, K link, L list, I list item, Q block quote, T table. The jump moves the caret to the
  start of the element (a block's first line, a link's first character) and speaks it as NVDA's browse mode would,
  through one notification: "Install, heading level 2"; "GitHub, link" (the address itself for a bare one); "List
  with 3 items, one" (the first item follows the count); the item's text for a list item ("blank" for an empty one);
  "Block quote, first line"; "Table with 3 rows and 2 columns". "No next heading", "No previous link", "No next
  heading level 2" and so on when there is none, and the caret stays; nothing on a plain arrow key. The Navigate
  menu lists the kinds under Next and Previous, one item per kind. The same keys work in a notice's text (AD-6.2).

## 10. Status

- 2.0.0 (2026-10-02): everything in section 4 built (`TextDocument`, `EditorSettings`, `EditorForm`, `AxDownKeys`,
  the installer's entries, `AxDown.ico`), checked off screen with a probe and by the user with NVDA (test plan sections 1
  to 7). 2.1.0: heading navigation (section 9), checked (section 8). 2.1.1: the window spoken on opening; it had no
  accessible role, so NVDA took it for a nameless pane and spoke only the field, while axClaude's window, with
  `AccessibleRole.Window`, is spoken as "axclaude - axClaude window" (the folder's name, then the app's). 2.3.0: the
  in-place update (bundle AX-2.8, AX-3.2) run by the user with NVDA from an older build and no file open: one progress
  bar through download and install, "Installed. Restarting", axDown back empty as it was (test plan 7.2).
- 2.9.0 (2026-10-18): the rest of section 9 (heading levels, links, lists, list items, block quotes, tables; the Next
  and Previous submenus), the navigation keys in every notice text, the Axit menu (bundle AX-9), and the menus in
  axClaude's shape (AD-4: Open recent file, Show current file in, Options with Text and Developer, Markdown resources);
  checked by the user with NVDA (test plan sections 3, 5, 6, 7 and 8).
- 2.9.1 (2026-10-18): the window moved to `Axit.Forms` as the text editor shell shared with axJSON (AD-1.4, AX-6.3);
  axDown is its `EditorApp` record, its Markdown keys and its texts. Nothing changed for the user.
- 2026-10-21: undo and redo over a history of the window's own specified at the user's ask (AD-2.5), for axDown and axJSON
  alike through the shared shell, and built the same day (`Axit.Core/EditHistory` with its tests; Ctrl+Z, Ctrl+Y and
  Ctrl+Shift+Z in the shell; Edit → Undo and Redo); the NVDA pass (test plan 2.2 and 2.6) is still to run.
- 2026-10-22: released in 3.3.1 (3.3.0 tagged, not published). The whole NVDA test plan marked passed at the user's word, no step reported failing; the record by step is `docs/axit/nvda-test-record.md`.
