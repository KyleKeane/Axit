# axPass, the window

The WinForms side of axPass: one KeePass database in the item apps' shared window, `Axit.Forms/ItemForm` (the
navigator and the detail pane, the moves, the shared keys, every field command). The pure code (KeePassXC's tool as a
process, its output read into groups, entries and fields, a field's change into the tool's options, the settings) is
`src/AxPass.Core`, with its own `CLAUDE.md`. The shared shape is `docs/axit/item-apps.md` (IA-n.m); axPass's own spec
is `docs/axpass/SPEC.md` ("SPEC.md" below), with the format and what the tool does in `docs/axpass/kdbx-notes.md`; the
guide `docs/axpass/user-guide.md` is embedded by `AxPass.csproj`; the manual test plan is `docs/axpass/nvda-test-plan.md`.

## Commands

```
.\manual_tests\prepare.ps1 axpass                 # the test database, password test-master-1 (needs KeePassXC)
.\run-axpass.ps1 manual_tests\axpass\test.kdbx    # build Axit and start axPass on it (run.ps1 -App pass)
.\test.ps1 AxPass                                 # the tests in Release, as CI runs them
```

## Where things are

- `AxPassApp`: the entry point `Run`, one call into `Axit.Forms/AppHost.Run` with the one database of the command line.
- `PassForm.cs`: the window on `ItemForm`: the menus (File; Group, Entry and Field, only the one for the selection
  enabled, `UpdateRowMenus`; Options with Lock after and the tool's place; Axit; Help), axPass's own keys
  (`ProcessItemKey`, Enter unlocking a locked window), the levels from the `PassTree` (`ReadLevel`, `EnterList`,
  `LeaveList`; a group's key ends in a slash, so that a group and an entry of one name differ), the detail (`DetailOf`;
  an entry's fields read with `show` when first selected, `ReadEntry`, kept in `_entries` until the window locks),
  unlocking and locking (`ShowUnlock`, `UnlockAsync`, `Lock`, `LockWhenIdle` from `GetLastInputInfo`), `ReloadAsync`,
  and `Cli`, the one way a command of the tool runs: off the UI thread, the master password first on its input, one
  at a time (`_busy`), its failure said.
- `PassForm.Commands.cs`: `SaveFields` (`edit` with `EntryFields.EditOptions`), `RenameItem`, `RenameRow`, `DeleteRow`
  (`rm` with its `mv` back; a group or the Recycle Bin's entries after a question), `NewItem`, `NewList`, `Undo` (the
  runs a `Step` keeps), the password's notices (`ChangeSpecial`, Generate, typed twice, `edit -p`), the copies (the
  user name by axPass, the password by the tool's `clip` with its 30-second clear, `KeePassCli.StartLasting`), the URL,
  the database's picker and `db-create`, the tool's place.
- `AxPassKeys`: the window's keys, `Axit.Forms/ItemKeys.All` and its own; `All` feeds the collision test.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. The master password and an entry's password never reach a log, the diagnostics, a file, a command line or an
   announcement; they go to the tool on standard input only, and the notice's field is emptied as soon as it is read.
2. Every command of the tool goes through `Cli` (or `KeePassCli` from a `Task.Run`), never on the UI thread.
3. axPass never decrypts the database itself (PW-D1); a need the tool cannot meet is said, not worked around.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- The tool's paths have no escape for a slash, so a title or a group name with one is refused, and `mv` moves entries
  only: a group in the Recycle Bin comes back in KeePassXC's own window.
- `clip` runs for its 30 seconds; `StartLasting` waits 1.5 s for a failure and leaves it running.
- A notice holds the window's own keys, so Generate is its button's Alt+G, not a window key.
