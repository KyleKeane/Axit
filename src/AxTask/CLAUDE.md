# axTask, the window

The WinForms side of axTask: the navigator and the detail pane, each the bundle's `AxitList`, a status bar, the
bundle's notices. The pure code (the item format, the field values, the folder, the changes with their undo, the links,
the tasks and the smart lists, the settings) is `src/AxTask.Core`, with its own `CLAUDE.md`. The shared shape of the
item apps is `docs/axit/item-apps.md` (IA-n.m); axTask's own spec is `docs/axtask/SPEC.md` ("SPEC.md" below); the guide
`docs/axtask/user-guide.md` is embedded by `AxTask.csproj`; the manual test plan is `docs/axtask/nvda-test-plan.md`.
The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axtask.ps1 manual_tests\axtask\tasks   # build Axit and start axTask on the test folder (run.ps1 -App task)
.\test.ps1 AxTask                            # the tests in Release, as CI runs them
```

## Where things are

- `AxTaskApp`: the entry point `Run`, one call into `Axit.Forms/AppHost.Run` with the one folder or item file.
- `TaskForm.cs`: the window on `AxitForm`: the menus (Folder; List, Item and Field, only the one for the selection
  enabled, `UpdateMenus`; View, Options, Axit, Help), the two panes in a `SplitContainer`, the key switch
  (`ProcessWindowKey`: the keys of everywhere, then the navigator's, then the detail pane's), the levels (`Start`,
  `Into`, `OpenSelected`, `OpenRow`, `UpLevel`, `ShowAllTasks`, `Refresh`), `Fill` (the one place the navigator, its
  name, the title and the status bar are filled; the selection by a row's path, or All tasks' name), `FillDetail`
  (the detail pane for the selection, silently, on every selection change; `_shown`, `_shownPath` and `_shownText` are
  the document it shows and the text it was read from).
- `TaskForm.Commands.cs`: the commands and the one way a file changes: `Commit` (a `Step` applied, kept for Ctrl+Z,
  the panes filled, its words said), `CheckFresh` (the file still as shown, IA-5.5), `ChangeShown` (a change to the
  detail pane's document), `ChangeItemFile`, `AskText` (every one-field notice, its check in `Accepts`). Rename and
  Archive add `Links.AfterMove`'s writes, so links follow what moved.
- `AxTaskKeys`: the window's keys; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every change goes through `Commit` with a `Step`; nothing writes a file another way.
2. Never write a file without checking it is as it was shown (`CheckFresh`): axJSON or a sync may have changed it.
3. The detail pane says nothing when it fills; NVDA reads it when the focus goes there.
4. When axPass is built, the window, the field notices and the format move to `Axit.Core` and `Axit.Forms` unchanged,
   one piece per commit (IA-D5); keep what is axTask's own (Done, Due, All tasks, smart lists) out of the shared parts.
5. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- The reads and writes run on the UI thread: items are small local files. All tasks reads every file under the top
  folder on each fill (TK-D3); a slow share would stall the window.
- Enter, Space, Delete, Escape, Backspace, Tab and the arrows reach `ProcessCmdKey` while a list has the focus; they are
  handled there with a `Focused` check, and returning true keeps the list box from its own move and its ding.
