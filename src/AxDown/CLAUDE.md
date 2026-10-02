# AxDown, the window

The WinForms side of AxDown: one plain edit control named after the file, a menu bar and a status bar. The pure code (files, settings) is `src/AxDown.Core`, with its own `CLAUDE.md`. The spec is `docs/axdown/SPEC.md` ("SPEC.md" below); the guide `docs/axdown/user-guide.md` is embedded by `AxDown.csproj`; the manual test plan is `docs/axdown/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run.ps1 -Down C:\path\to\notes.md      # build Axit and start AxDown on the file (close a running Axit first)
dotnet build Axit.sln
dotnet test tests/AxDown.Tests/AxDown.Tests.csproj
```

## Where things are

- `AxDownApp`: the entry point `Run` that Axit's `Program` calls (bundle AX-1.7): names the log, reads the settings, opens `EditorForm`; the crash handler, `--help`, `--version`.
- `EditorForm`: everything of the window. Sections in the file: menus (File, Edit, Navigate, View, Help), the editor, the document (load, save, save as, the changed-on-disk question, the unsaved-changes question), find, go to line and the heading jumps (`JumpToHeading` on `MarkdownOutline`), title and status, word wrap, fonts and the window, notices, help and updates, the menu items made from the key tables.
- `AxDownKeys`: the window's keys (SPEC.md AD-5); the bundle-wide ones are `Axit.Forms.BundleKeys`. Every key is handled in `EditorForm.ProcessCmdKey` from these tables and nowhere else; the menu items only display them (`ShortcutKeyDisplayString`), so that the keys stay off while a notice shows.
- `HelpText`: the F1 text, the embedded guide, the disclaimer.
- Shared pieces used from `Axit.Forms`: `OverlayPanel` and `Notice` (every question of the window's own), `ControlArea` (the Go to line field), `EditPaging`, `StatusLayout`, `Sounds.Notice`, `WindowAccessibleObject`; from `Axit.Core`: `SettingsFile`, `AppPaths`, `Log`, `Updater`, `UpdateCheck`, `BundleInfo`, `WindowPlacement`.

## Rules

1. Every control gets an `AccessibleName` (the editor's is the file's name). Nothing moves the caret except the user's command (find, go to line); a save, a reload or a word wrap change puts it back where it was. Nothing speaks on a plain key; the announcements are the list in SPEC.md AD-7, through one UI Automation notification on the editor (or on the notice while one shows).
2. No second window: questions and errors go through `ShowNotice`; the Windows file and font pickers are the only separate windows.
3. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas already learned

- In an EDIT control Ctrl+H, Ctrl+I, Ctrl+J and Ctrl+M arrive as Backspace, Tab, line feed and Enter characters: `OnEditorKeyDown` swallows the unhandled ones (SPEC.md AD-5.1), and a handled one sets `SuppressKeyPress`.
- Changing `WordWrap` recreates the control's handle and loses the caret; `ToggleWordWrap` saves and restores the selection.
- `GetLineFromCharIndex` counts wrapped rows while word wrap is on; the line and column of the status bar and Go to line count the file's lines by scanning the text for `\n` (`Position`), 300 ms after the last key so that typing stays cheap.
- `_editor.Text` copies the whole text on every read; it is read on command and by the status timer, never per keystroke.
- The form's `AccessibleRole` is `Window`: NVDA speaks a window with that role when it opens ("README.md - AxDown window"); without it the window is a nameless pane and only the field is heard (2.1.1).
- The notice state is the flag `_noticeOpen`, not the panel's `Visible` (false for every child while the form is not shown); the same flag keeps the window's shortcuts and Alt/F10 off while a notice shows (see `src/AxClaude/CLAUDE.md` for the full story of these behaviours, which AxDown shares).
- The notice flow (`ShowNotice`, `CloseNotice`, `WithKeyLine`, `Announce`) and the update flow are copies of AxClaude's; plan step 11 decides whether they become a shared window base in `Axit.Forms`.
