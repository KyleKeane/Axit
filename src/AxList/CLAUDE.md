# axList, the window

The WinForms side of axList: one list box with three views (Lists, Items, Tasks), a status bar, the bundle's notices. The pure code (a list file's lines and every change to them, the folder of lists, the settings) is `src/AxList.Core`, with its own `CLAUDE.md`. The spec is `docs/axlist/SPEC.md` ("SPEC.md" below; the file format is its section 5); the guide `docs/axlist/user-guide.md` is embedded by `AxList.csproj`; the manual test plan is `docs/axlist/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axlist.ps1 C:\scratch\lists   # build Axit and start axList on a folder of lists (run.ps1 -App list)
.\test.ps1 AxList                   # the tests in Release, as CI runs them
```

## Where things are

- `AxListApp`: the entry point `Run` (bundle AX-1.7), one call into `Axit.Forms/AppHost.Run` with the one file or folder of the command line.
- `ListForm`: the window (SPEC.md LI-2) on `Axit.Forms/AxitForm`. `Start` (a `.md` file, a folder, or the last run's list), `ShowView` and `Fill` (the one place the list box, its name, the title and the status bar are filled; the selection by a list's path, a file line, or an index), `Refresh`, the commands (`Act`, `Check`, `ChangeItem`, `NewItem`, `NewList` with `MakeList`, `MakeTask`, `RemoveItem`, `MoveItem`, `IndentItem`, `ToggleHideDone`, `Undo`, `CopyText`, `ShowInAxFile`, `OpenInAxDown`, `ChooseListsFolder`). Every change goes through `Change` (through `ChangeSelected` for the Items view's selection): the file read again, `ListDocument.Holds` checked against the line shown (SPEC.md LI-6.2), the change as a function of the document and the line, the file written through `TextDocument`, the step kept for Ctrl+Z (`Step`: the text before and after), the view filled again on the line the change gives, its words announced; it returns an `Outcome`, so that Move plays the edge sound only when the document refused.
- `AxListKeys`: the window's keys (SPEC.md LI-6.5; bundle AX-8); `All` feeds the collision test and the F1 text names the same keys. Alt+Up is axFile's Up a folder too, in KeyTests' known list.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; the list box is named with its view and count; nothing speaks on an arrow key; every change is one announcement.
2. Never write a file without reading it first and checking the line (SPEC.md LI-D5): another program may have it open.
3. A line nothing changed is written back exactly as it was read (`ListLine.Raw`); the tests hold the round trip.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- The reads and writes run on the UI thread: a list is a small local file, and the read before a change must happen in the same turn as the change. A folder of lists on a slow share would stall the window; that is an open question, not handled.
- Enter, Space, Delete, Escape and Backspace in a `ListBox` reach `ProcessCmdKey`; they are handled there with a `Focused` check, and returning true keeps the list from dinging and from its own letter search.
