# axPreview, the app side

axPreview has no window of its own: it is the text editor shell (`src/Axit.Forms/TextEditorForm`) in its read-only mode, with what differs as data (`EditorApp` with `ReadOnly` and `Refuse`). This folder holds that data, the key table and the texts. The pure code (`TextSniff`) is `src/AxPreview.Core`, with its own `CLAUDE.md`. The spec is `docs/axpreview/SPEC.md` ("SPEC.md" below); the guide `docs/axpreview/user-guide.md` is embedded by `AxPreview.csproj` for Help → User guide; the manual test plan is `docs/axpreview/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axpreview.ps1 C:\path\to\README.md   # build Axit and start axPreview on the file (run.ps1 -App preview)
.\run-axpreview.ps1                        # an empty window
dotnet build Axit.sln
dotnet test tests/AxPreview.Tests/AxPreview.Tests.csproj
```

## Where things are

- `AxPreviewApp`: the entry point `Run` that Axit's `Program` calls (bundle AX-1.7): `--new` read off the command line, the one file through `AppHost.OnePath`, then, with a file and an open axPreview window, one `AppMessage.Send` with the verb `open` and no window of this process (SPEC.md AP-1.2); otherwise the shell's window on `App`, axPreview's `EditorApp`: name, verb, the settings file `axpreview.json` (the editors' `EditorSettings`), the file filter, the F1 text, the guide, axDown's key pairs, the Markdown resources, no commands, `ReadOnly: true`, and `Refuse`, which turns `TextSniff.Refuse` into the notice the shell shows for a file that is not text or too large.
- The window is `Axit.Forms/TextEditorForm` in read-only mode (SPEC.md AP-D4, decided at the build 2026-10-22: a flag on the record rather than a second window on the shared pieces, since the viewer is the editor without editing): the editor control read-only with the window's colours, no Edit menu, the File menu with New window, Open, Open recent, Reload (F5), Edit in axDown or axJSON (Ctrl+E, named after the file's kind), Show current file in and Exit; Ctrl+S says "Nothing to save here"; `l` says the line while the text has the focus (a command with `When` the text is focused); the shell's commands that write (Save as, Undo, Redo) are not made in read-only mode, so their keys do nothing (the window's menus and keys are one command list, docs/axit/app-kit.md AK-2); a hand-over message in `WndProc` loads the file (`AppMessage.Receive`); the undo history, Save and the unsaved-changes questions never run, since the text never changes. The viewer's own part of the shell is `TextEditorForm.Viewer.cs` (2026-10-23): a path that does not exist is the notice "File not found" (AP-1.1), a file the sniff passes and the reader refuses keeps the text (AP-2.2), and a hand-over leaves the update's progress notice alone.
- `AxPreviewKeys`: the window's keys (SPEC.md AP-3.2): the shell's viewer keys (`Axit.Forms.EditorKeys.Viewer`) and axDown's navigation letters, `Axit.Forms.MarkdownKeys`, the one table of both; `All` feeds the collision test and the F1 text names the same keys; `Jumps` is the list the shell and every notice text take.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Nothing moves the caret except the user's command; a reload, a word wrap change or a font change leaves it where it was. Nothing speaks on a plain key; the announcements are SPEC.md AP-4.
2. No second window: the notices are the shell's; the Windows file and font pickers are the only separate windows.
3. Run the `accessibility-review` skill on a change to the shell's read-only mode before committing, and check it with NVDA following the test plan.

## Gotchas already learned

- A read-only `TextBox` paints itself in the control colour unless its `BackColor` and `ForeColor` are set to the window's; the shell sets them in read-only mode.
- `l` is a plain letter: the shell takes it only while the text has the focus, so that the Go to line field still accepts it.
- The hand-over's `SendMessage` waits for the answer: the shell sets `m.Result` to 1 and posts the load with `BeginInvoke`, so that a 32 MB file never holds the starting process.
