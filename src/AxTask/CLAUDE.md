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
- The panes, the moves between levels, the shared keys and every field command are the item apps' shared window,
  `Axit.Forms/ItemForm` (since axPass, its second use); `TaskForm` derives from it and fills in what is axTask's.
- `TaskForm.cs`: the menus (Folder; List, Item and Field, only the one for the selection enabled, `UpdateRowMenus`;
  View, Options, Axit, Help), axTask's own keys (`ProcessItemKey`), the levels of the folder (`Start`, `EnterList`,
  `LeaveList`, which goes straight back to the top, `ShowAllTasks`, `ReadLevel` with the rows' `Tag`: an `ItemFile`, a
  `SmartList`, a `SectionTag` on a header, `ArchivedItemsTag`, or nothing for a list; the top level in sections is
  `ReadSections`, TK-3a) and the detail (`DetailOf`, with a `FileSource` to save to; a link read by `SpokenValue`).
- `TaskForm.Commands.cs`: the commands and the one way a file changes: `Commit` (a `Step` applied, kept for Ctrl+Z,
  the panes filled, its words said), `CheckFresh` (the file still as shown, IA-5.5), `SaveFields` (the shared field
  commands' change, written), `MoveToSection` and `SetSection` (TK-3a.3), `NewList` in the selected row's section, the link's notice (`ChangeSpecial`). Rename and Archive add `Links.AfterMove`'s writes,
  so links follow what moved.
- `AxTaskKeys`: the window's keys, `Axit.Forms/ItemKeys.All` and its own; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every change goes through `Commit` with a `Step`; nothing writes a file another way.
2. Never write a file without checking it is as it was shown (`CheckFresh`): axJSON or a sync may have changed it.
3. The detail pane says nothing when it fills; NVDA reads it when the focus goes there.
4. The window, the field notices and the format are shared with axPass (IA-D5): a change to them changes both apps;
   keep what is axTask's own (Done, Due, All tasks, smart lists, links, files) in `TaskForm`.
5. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- The reads and writes run on the UI thread: items are small local files. All tasks reads every file under the top
  folder on each fill (TK-D3); a slow share would stall the window.
- Enter, Space, Delete, Escape, Backspace, Tab and the arrows reach `ProcessCmdKey` while a list has the focus; they are
  handled there with a `Focused` check, and returning true keeps the list box from its own move and its ding.
