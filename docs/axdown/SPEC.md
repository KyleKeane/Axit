# AxDown — Specification

AxDown is the second app of the Axit bundle (`docs/axit/SPEC.md`): a barebones editor for Markdown and plain text files
for a blind developer who uses the NVDA screen reader. Written 2026-10-02, before the code (plan step 6 of
`docs/axit/plan.md`); every requirement is **planned** until the status at the end says otherwise. Requirements are
numbered `AD-n.m` and decisions `AD-Dn`. The bundle's rules that every app follows, AX-7 (accessibility) and AX-8
(keys), are cited by number and not repeated. In AxDown's code and documents, "SPEC.md" means this file.

## 1. Summary

AxDown opens one text file in one window: a menu bar, one plain multi-line edit control with the whole file in it,
and a status bar. It reads and writes the file exactly as it found it (encoding, byte order mark, line endings), asks
before unsaved changes are lost, finds text, goes to a line, and shows the line and column on demand. That is all of
version one. Navigation by Markdown structure (headings, list items, links, code blocks) follows in the next version
(section 9), and everything else people expect of an editor (several files, colouring, a preview, spelling) is
deliberately absent: those are other apps of the bundle or not wanted.

## 2. Goals and non-goals

- **G1 A plain Windows edit field.** The editor is the native EDIT control, the control NVDA reads best: characters,
  words, lines and selection are spoken by NVDA itself, with its own keys, and AxDown adds nothing to that reading.
- **G2 Faithful files.** A file comes back from AxDown byte for byte the same where it was not edited: the encoding,
  the byte order mark and the line endings are kept.
- **G3 Barebones.** Every feature in section 4 is there because a text file cannot be edited safely without it.
  Anything that can wait, waits.
- **G4 Information on demand.** Nothing is spoken on a plain key press; position, state and results are given when
  asked for or when a command finishes (AX-7.3).
- **G5 Shared behaviour.** Notices, the question area, paging, text size and the update check are the bundle's
  (AX-6), so a user of AxClaude already knows them.

Non-goals (version one): several files or tabs in one window (open a second window instead, AD-D3); syntax
colouring or any formatting; a Markdown preview; auto-save; spelling; printing; a replace command; encodings beyond
UTF-8, UTF-16 and the system code page; files that are not text; files over 32 MB (AD-3.7).

## 3. Scenarios

- S1 Open from File Explorer: right-click `notes.md`, Open in AxDown; the window opens with the caret at the start
  and NVDA reads the editor's name, which is the file's name.
- S2 Start empty: Start menu, Axit AxDown; an empty document named Untitled; type; Ctrl+S asks for a name.
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
  `Axit.exe` or `axit` reaches here (AX-1). `--help` and `--version` answer as in AxClaude (FR-9.5 there).
- AD-1.2 With an existing file, it is opened (AD-3). With a path that does not exist, the document is empty and has
  that name; the file is created on the first save. Without a file, the document is empty and called Untitled.
- AD-1.3 One window, one document (AD-D3). A second file opens a second window, as File Explorer does.
- AD-1.4 `AxDownApp.Run(args)` is the entry point (AX-1.7): it names the log (`Log.UseApp("axdown")`), reads the
  settings (AD-8), opens `EditorForm` and owns the crash handling, as `AxClaudeApp` does.

### AD-2 The editor

- AD-2.1 A `TextBox`: Multiline, AcceptsTab, AcceptsReturn, WordWrap as the setting says (on by default),
  ScrollBars Vertical (Both while word wrap is off), HideSelection false, MaxLength 0. Its `AccessibleName` is the
  file's name (`notes.md`), or Untitled, so that NVDA says the name when the focus arrives (AX-7.1).
- AD-2.2 The text is the file's, with line endings normalised to `\r\n` for the control; the original line ending
  is remembered and written back (AD-3.4).
- AD-2.3 Page Up and Page Down move one screen and reach the ends through the bundle's `EditPaging` (as AxClaude's
  FR-2.1): the control's own keys do nothing while the text fits.
- AD-2.4 Turning word wrap off and on keeps the caret where it was. (WinForms recreates the control's handle on that
  change and loses the position; the form saves `SelectionStart` and restores it, then scrolls to the caret.)
- AD-2.5 Undo is the control's own (Ctrl+Z, one level, as in Notepad before Windows 11). Nothing more in version one.
- AD-2.6 The caret never moves except on the user's own command (AX-7.3): a save, a reload or a word wrap change
  leaves it where it was.
- AD-2.7 The font is the setting's (family, size, bold), the Windows system font when none is set; Ctrl+Plus and
  Ctrl+Minus change the size in steps and remember it (AX-8.2); View, Font opens the Windows font picker (the only
  separate windows are the Windows pickers, AX-7.2).

### AD-3 Files

- AD-3.1 Reading: the bytes are decoded as UTF-8 with a byte order mark, UTF-16 little or big endian with a mark,
  otherwise UTF-8 when the bytes are valid UTF-8, otherwise the system code page (`Encoding.Default` is UTF-8 in
  .NET; the ANSI page comes from `CodePagesEncodingProvider`, built into .NET, no package).
- AD-3.2 The encoding and whether there was a mark are remembered with the document and shown in the status bar
  ("UTF-8", "UTF-8 with BOM", "UTF-16 LE", "Windows-1252").
- AD-3.3 Writing: the text goes back in the remembered encoding, with the mark when there was one. A new document is
  UTF-8 without a mark.
- AD-3.4 Line endings: the file's first line ending (`\r\n`, `\n` or `\r`) is remembered and used for every line on
  save; a file with none keeps `\r\n`. The status bar shows it ("CRLF", "LF").
- AD-3.5 Save writes the file in place (as Notepad does) and then reads its last write time, which Ctrl+S and the
  close check compare against (AD-3.6). Save as asks for a path with the Windows file picker, then saves and renames
  the document (title, editor name, status bar).
- AD-3.6 Changed on disk: when the file's last write time is not the one remembered, Save first asks through a
  notice ("The file changed on disk since you opened it. Write over it?" with Write over and Cancel). Nothing is
  watched or polled; the check happens at the moment of saving.
- AD-3.7 A file over 32 MB is refused with a notice, since the EDIT control stops being usable long before that. A
  file that cannot be read (missing, locked, not text, no permission) is reported in a notice and the window stays
  open with an empty document of that name.
- AD-3.8 Open, New and Exit check for unsaved changes first (AD-6.1).
- AD-3.9 All of this is pure code in `AxDown.Core/TextDocument` (bytes in, text plus encoding plus line ending out,
  and back), unit tested for every encoding and line ending case, with the window only calling it.

### AD-4 Commands and menus

- AD-4.1 File: New (Ctrl+N), Open… (Ctrl+O), Save (Ctrl+S), Save as… (Ctrl+Shift+S), Exit (Alt+F4).
- AD-4.2 Edit: Undo (Ctrl+Z), Cut, Copy, Paste, Select all (the control's own, listed for discoverability), Find…
  (Ctrl+F), Find next (F3), Find previous (Shift+F3), Go to line… (Ctrl+G).
- AD-4.3 View: Word wrap (checked; Ctrl+Shift+W), Larger text (Ctrl+Plus), Smaller text (Ctrl+Minus), Font…
- AD-4.4 Help: Keyboard shortcuts (F1), User guide, Installed: Axit n (About), Latest release and the update items as
  AxClaude has them (FR-1.10 there, the bundle's `Updater`, AX-3), Copy diagnostics, About AxDown.
- AD-4.5 Every menu item has a mnemonic and a shortcut or a shortcut text (AX-7.1).

### AD-5 Keys

Every key is bundle-wide (the same in every window of Axit, `Axit.Forms/BundleKeys`) or window-specific
(`AxDown/AxDownKeys`) (AX-8). The table is the spec of `AxDownKeys`; the F1 text reads from it.

Bundle-wide (AX-8.2): F1 shortcuts; Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the window's
controls (in AxDown there is one, so they stay in the editor; in a notice they move between its text, field and
buttons); Ctrl+F find, F3 and Shift+F3 next and previous; Ctrl+S save; Ctrl+Plus and Ctrl+Minus text size; Enter
and Escape on a notice; Page Up and Page Down one screen in an edit control; Alt+F4 exit; Alt and F10 the menu.

Window-specific: Ctrl+N new document; Ctrl+O open; Ctrl+Shift+S save as; Ctrl+G go to line; Ctrl+Shift+W word wrap.
Everything else in the editor is the control's own: arrows, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left and
Ctrl+Right by word, Shift with any of them to select, Ctrl+A, Ctrl+C, Ctrl+X, Ctrl+V, Ctrl+Z, Delete, Backspace,
Ctrl+Backspace, Tab (types a tab), Enter (a new line).

Reserved for section 9 (navigation, AD-D7): Ctrl+letter the next item of a kind, Ctrl+Shift+letter the previous,
with NVDA's browse-mode letters: H, 1 to 6, K, L, I, Q, T.

- AD-5.1 In an EDIT control Ctrl+H, Ctrl+I, Ctrl+J and Ctrl+M arrive as the characters Backspace, Tab, line feed
  and Enter. A handled key sets `SuppressKeyPress`; a key in that group that AxDown does not handle is swallowed
  the same way, so that Ctrl+M never inserts a line.
- AD-5.2 NVDA's own key combinations are never intercepted; no global hook (AX-7.4).

### AD-6 Notices and questions

All through the bundle's `OverlayPanel` and `ControlArea` (AX-6.4, AX-7.2): drawn in the window over the editor,
never a separate window.

- AD-6.1 Unsaved changes: "Save changes to notes.md?" with Save (default), Don't save and Cancel; before New, Open and
  Exit, and when Windows closes the window. Save with no name yet opens the file picker first.
- AD-6.2 Changed on disk (AD-3.6); file errors (AD-3.7); the update notice (AX-3); Keyboard shortcuts; User guide;
  About.
- AD-6.3 Find: the notice with the field "Text to find" (as AxClaude's); Enter finds the next match from the caret,
  wrapping once at the end, selects it and returns the focus to the editor, where NVDA reads the selection's line.
  "Not found" is spoken when there is none. F3 and Shift+F3 repeat without the notice.
- AD-6.4 Go to line: a field in the question area under the editor, named "Go to line (now line 12, column 4)";
  Enter moves the caret to the start of that line (clamped to the last), Escape cancels; the name is how the position
  is read on demand (G4), and the status bar carries it too (AD-9).

### AD-7 Announcements

Spoken through one UI Automation notification on the editor (AX-7.3), and only these: "Saved" (after a save and
after Save as); "Not found" (find); "Line n of m" after Go to line; "Word wrap on" / "Word wrap off"; "Text size n";
"Copied" is NVDA's own. Opening a file speaks nothing of its own: the focus arrives in the editor and NVDA reads
its name (the file's) and the first line.

### AD-8 Settings

`%APPDATA%\Axit\axdown.json` through the bundle's `SettingsFile` (AX-5): `fontFamily` (null = system), `fontSize`
(0 = system), `fontBold`, `wordWrap` (true), `checkForUpdates` (true), `window` (placement). A plain class,
`EditorSettings`, in `AxDown.Core`, with the sanity checks after loading as `AppSettings` has.

### AD-9 Status bar

Two labels through the bundle's `StatusLayout` (no `Spring`, fixed widths when the texts do not fit, as AxClaude's
§6.2): the left one the document's state ("notes.md", "notes.md, unsaved changes"), the right one "Line 12,
Column 4 · UTF-8 · CRLF", updated on caret moves (cheap: `EM_LINEFROMCHAR` and `EM_LINEINDEX`, no text copy). NVDA
reads it with NVDA+End; nothing is spoken automatically.

### AD-10 Title

`notes.md - AxDown`, `*notes.md - AxDown` with unsaved changes (the Notepad convention, AD-D5), `Untitled - AxDown`.

### AD-11 Errors and diagnostics

As AxClaude's FR-13: an exception in a handler is logged to `%LOCALAPPDATA%\Axit\logs\axdown.log` and shown as a
notice; Help, Copy diagnostics puts the version, the paths and the log's tail on the clipboard.

## 5. User interface

```
| File  Edit  View  Help                                            |
| notes.md (multi-line edit, the whole file)                        |
|                                                                   |
| notes.md, unsaved changes | Line 12, Column 4 · UTF-8 · CRLF      |
```

Controls and accessible names: the editor `TextBox` named after the file; the `MenuStrip`; the `StatusStrip` with
two labels and no names (NVDA+End reads the text); notices and the question area as the bundle's (AX-6.4). Minimum
size 500×300; the system font and colours.

## 6. Architecture

```
src/AxDown.Core/   class library (net10.0): TextDocument (read and write with encoding and line ending), EditorSettings,
                   MarkdownOutline (section 9)
src/AxDown/        class library (net10.0-windows): AxDownApp (Run), EditorForm, AxDownKeys, HelpText (F1 text, the
                   embedded user guide); references AxDown.Core, Axit.Core, Axit.Forms
tests/AxDown.Tests/ TextDocumentTests (every encoding and line ending case, a new file, a file with no line ending),
                   MarkdownOutlineTests (section 9)
docs/axdown/       this spec, user-guide.md (embedded, Help, User guide), nvda-test-plan.md
```

Shared pieces AxDown uses (AX-6, moved to `Axit.Forms` at step 8 one by one): `OverlayPanel` and `Notice`,
`ControlArea` (the Go to line field), `EditPaging`, `StatusLayout`, the font handling, `Sounds` (the notice chime),
`BundleKeys`; from `Axit.Core`: `SettingsFile`, `AppPaths`, `Log`, `Updater`, `BundleInfo`.

Data flow: `TextDocument.Read(path)` gives text, encoding, mark and line ending; the form puts the text in the
control; `TextDocument.Write(path, text, encoding, mark, lineEnding)` writes it back. No background thread: files
are read and written on the UI thread (a 32 MB file is the ceiling, AD-3.7).

## 7. Design decisions

- **AD-D1 The EDIT control, not RichTextBox.** It is the control AxClaude already proved with NVDA; RichEdit reads
  differently, wraps differently and would invite formatting. The known costs (re-wrap near the top of a large text,
  one-level undo) are accepted for a document editor.
- **AD-D2 Barebones first.** Version one has what section 4 lists and nothing more, so that the window is simple
  to learn and the code simple to change (AX B-10); the navigation keys come second (section 9); collaborative and
  AI-assisted editing are other apps of the bundle, not features of this one.
- **AD-D3 One document per window.** Tabs or a file list are another control to explain and to focus; a second
  window costs nothing and Alt+Tab already handles it. The dispatcher starts a new process per file.
- **AD-D4 Files come back as they went in.** Encoding, mark and line ending are read once and written back; AxDown
  never converts a file unless asked (there is no such command in version one).
- **AD-D5 The Notepad title convention**, `*name - AxDown`: short, known, read by NVDA as "star name".
- **AD-D6 Go to line carries the position.** One key, Ctrl+G, both tells the line and column (in the field's name)
  and moves; a separate "where am I" key would be one more key to learn, and the status bar is there for NVDA+End.
- **AD-D7 Navigation letters are NVDA's browse-mode letters (Kyle, 2026-10-02).** The defaults are the keys NVDA
  users already know from web pages, with Ctrl for the next item and Ctrl+Shift for the previous: H heading, 1 to 6
  a heading of that level, K link, L list, I list item, Q block quote, T table. They are built in that order,
  headings first (Ctrl+H, Ctrl+Shift+H, then the levels), the others as the outline learns them. Known difference:
  Ctrl+1 and Ctrl+2 move the focus in AxClaude's window; the collision test lists it, and step 11 (AxClaude's key
  table) is where to decide whether AxClaude's focus keys move. Ctrl+H is otherwise Replace in some editors, which
  AxDown does not have.
- **AD-D8 No file watching.** The changed-on-disk check at save time catches the case that loses work; a watcher
  would speak at the wrong moments and add a thread.
- **AD-D9 Open with, not the default.** AxDown registers for `.md`, `.markdown` and `.txt` and appears in the
  right-click menu of every file; Windows decides defaults (AX B-9).

## 8. Testing

Unit tests (xunit, `tests/AxDown.Tests`): `TextDocumentTests` with byte arrays for UTF-8 with and without a mark,
UTF-16 LE and BE with marks, Windows-1252, `\r\n`, `\n`, `\r` and no line ending, round trips that reproduce the
bytes, a new document's defaults. The window has no automated tests; `docs/axdown/nvda-test-plan.md` covers it, and
the off-screen probe approach of AxClaude checks layout and keys without disturbing the user.

## 9. Next version: Markdown navigation (plan step 12)

- `AxDown.Core/MarkdownOutline`: from the text and a caret position, the next and previous heading (`#` lines, with
  the level), ignoring heading-looking lines inside fenced code blocks (` ``` ` or `~~~`). Pure, tested. Lists,
  list items, links, block quotes and tables follow, each with NVDA's letter (AD-D7).
- Keys per AD-D7: Ctrl+H and Ctrl+Shift+H first, then Ctrl+1 to Ctrl+6 for the levels, then K, L, I, Q, T. The
  jump moves the caret to the start of the line and speaks the line through one notification; "No next heading" /
  "No previous heading" and the like when there is none; nothing on a plain arrow key.

## 10. Status

- 2026-10-02: specification written (plan step 6); AD-D7 settled the same day. Step 7 built: `AxDown.Core` with
  `TextDocument` (AD-3.1 to AD-3.4, AD-3.7, AD-3.9) and `EditorSettings` (AD-8), 14 tests. Step 8 built: the window
  (`EditorForm`, AD-1 to AD-11 as specified), `AxDownKeys` and `BundleKeys` with the collision test; checked off
  screen with a probe, not yet with NVDA (step 10). Step 9 built: the installer's entries (AD-D9), `AxDown.ico`,
  and the updater restarting AxDown on its file (`-StartFile`).
