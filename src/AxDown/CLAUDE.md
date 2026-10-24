# axDown, the window

The WinForms side of axDown: one plain edit control named after the file, a menu bar and a status bar. Its pure code (`TextDocument`, `EditorSettings`, `MarkdownOutline`) is in `src/Axit.Core` since its second use, tested in `tests/Axit.Tests`. The spec is `docs/axdown/SPEC.md` ("SPEC.md" below); the guide `docs/axdown/user-guide.md` is embedded by `AxDown.csproj`; the manual test plan is `docs/axdown/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axdown.ps1 C:\path\to\notes.md     # build Axit and start axDown on the file (run.ps1 -App down; close a running Axit first)
.\run-axdown.ps1                         # docs\axdown\tutorial.md, the guided tour with every Markdown structure to practise on
dotnet build Axit.sln
dotnet test tests/Axit.Tests/Axit.Tests.csproj     # TextDocumentTests, EditorSettingsTests, MarkdownOutlineTests, TextJumpsTests
```

## Where things are

- `AxDownApp`: the entry point `Run` that Axit's `Program` calls (bundle AX-1.7), one call into `Axit.Forms.EditorHost.Run` with `App`, axDown's `EditorApp` record: name, verb, settings file, file filter, F1 text, guide, the Markdown key pairs, the Markdown resources pages, no commands.
- The window is `Axit.Forms/TextEditorForm`, the text editor shell shared with axJSON since 2026-10-18 (it was `EditorForm` here). Sections in that file: menus (File, Edit, Navigate, Options, the bundle's Axit menu, Help), the editor, the document (load, save, save as, the changed-on-disk question, the unsaved-changes question), find, go to line and the navigation jumps and the Next and Previous submenus (from the app's `Jumps` through the shared `TextJumps`, section 9), the app's commands, title and status, word wrap, fonts and the window, notices, help and updates, the menu items made from the key tables.
- `AxDownKeys`: the window's keys (SPEC.md AD-5): the Markdown letters (`Axit.Forms.MarkdownKeys`, shared with axPreview) and the shell's keys (`Axit.Forms.EditorKeys`), together in `All`; the bundle-wide ones are `Axit.Forms.BundleKeys`. Every key is a command of the window's one command list (docs/axit/app-kit.md AK-2): `TextEditorForm` makes each menu item and its key from one `Command` (`Add`, or `AddKey` for a key without an item: Tab, and the Show in File Explorer and Word wrap items, which go into submenus the base makes), and the base runs the list after the common keys, first match in registration order; the app's `EditorCommand`s (axJSON's Format) and `Jumps` become commands the same way. The keys the edit control would otherwise act on first are its `OnEditorKeyDown` (Page Up and Page Down, Ctrl+Up and Ctrl+Down, the Ctrl+H, Ctrl+I, Ctrl+J and Ctrl+M guard, Alt+Backspace as Undo), and nowhere else; the menu items only display the keys (`ShortcutKeyDisplayString` from `BundleKeys.Text`), so that the keys stay off while a notice shows.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's (`Axit.Core.BundleAbout`).
- Shared pieces used from `Axit.Forms`: `OverlayPanel` and `Notice` (every question of the window's own), `ControlArea` (the Go to line field), `EditKeys` (the page and paragraph keys), `Announcer`, `StatusLayout`, `Sounds.Notice`, `WindowAccessibleObject`; from `Axit.Core`: `SettingsFile`, `AppPaths`, `Log`, `Updater`, `UpdateCheck`, `BundleInfo`, `WindowPlacement`.

## Rules

1. Every control gets an `AccessibleName` (the editor's is the file's name). Nothing moves the caret except the user's command (find, go to line); a save, a reload or a word wrap change puts it back where it was. Nothing speaks on a plain key; the announcements are the list in SPEC.md AD-7, through one UI Automation notification on the editor (or on the notice while one shows).
2. No second window: questions and errors go through `ShowNotice`; the Windows file and font pickers are the only separate windows.
3. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas already learned

- In an EDIT control Ctrl+H, Ctrl+I, Ctrl+J and Ctrl+M arrive as Backspace, Tab, line feed and Enter characters: `OnEditorKeyDown` swallows the unhandled ones (SPEC.md AD-5.1), and a handled one sets `SuppressKeyPress`.
- Changing `WordWrap` recreates the control's handle and loses the caret; `ToggleWordWrap` saves and restores the selection.
- `GetLineFromCharIndex` counts wrapped rows while word wrap is on; the line and column of the status bar and Go to line count the file's lines by scanning the text for `\n` (`Position`), 300 ms after the last key so that typing stays cheap.
- `_editor.Text` copies the whole text on every read; it is read on command, by the status timer and, since the undo history (SPEC.md AD-2.5, 2026-10-21), once per change in `TextChanged`, accepted for the files the editor holds; nothing else reads it per keystroke.
- The form's `AccessibleRole` is `Window`: NVDA speaks a window with that role when it opens ("README.md - axDown window"); without it the window is a nameless pane and only the field is heard (2.1.1).
- The notice state is the window base's `NoticeOpen`, not the panel's `Visible` (false for every child while the form is not shown); the same flag keeps the window's shortcuts and Alt/F10 off while a notice shows (see `src/AxClaude/CLAUDE.md` for the full story of these behaviours, which every window shares).
- The notice flow (`ShowNotice`, `CloseNotice`, `Announce`), the fonts, the window's place and the update flow are the window base's, `Axit.Forms/AxitForm` (bundle AX-6.5), since 2.12.0.
