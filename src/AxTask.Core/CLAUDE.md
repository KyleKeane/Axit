# AxTask.Core, the pure code

Everything of axTask that needs no window, unit tested in `tests/AxTask.Tests` over temporary folders. The item
format is `docs/axit/item-apps.md` (IA-n.m), shared with axPass; axTask's own rules are `docs/axtask/SPEC.md` (TK-n.m).
The window is `src/AxTask`, with its own `CLAUDE.md`.

## Commands

```
.\test.ps1 AxTask
```

## Where things are

- The item format itself (`ItemDocument`, `ItemField`, `FieldTypes`, `Parsed`: an item or a list's `list.json` as JSON
  kept whole, every change a new document; the types read out and typed) is `Axit.Core`'s since axPass, its second
  use (IA-D5), with its tests in `tests/Axit.Tests`.
- `ItemFolder` and `ItemFile`, `FolderLevel` (IA-4, IA-D4): a level's lists and items (`Read`; hidden and dot entries
  left out; the Archive last at the top; files that are not items counted), `ReadItem` (with the text read, for
  `CheckFresh`), `ReadList`, `NameOf`, `FileStem` and `FreePath` (IA-4.5), `RefusedListName`, `AllItems` (the Archive
  left out), `ArchiveFolder`, `IsArchive`, `ListName`, `LinkTo` and `Resolve` (IA-D3).
- `Changes`, `Step` and `FileChange` (IA-5.4): a change as parts, writes and moves, `Apply`, `CheckUndo` (every path
  as the step left it), `Undo` (last part first; a new list's folder goes with its `list.json`), `ReadOrNull`.
- `Links` (IA-D3): `AfterMove` (the writes that keep every link pointing where it did when a file or a folder moves,
  both the links to it and the links it holds), `Spoken` ("missing: name"), `Remap`.
- `TaskRules` (TK-4): Done, Start and Due, `DefaultTemplate`, `TemplateOf`, `IsTask`, `IsDone`, `Due`, `Summary`,
  `WithDone`, `WithoutDone`, `TemplateWithDone`, `Ordered`.
- `SmartList` (TK-5): All tasks and the `.filter.json` files: `InFolder`, `Read` (a file not understood is a smart
  list that shows nothing, with `NotUnderstood`), `Items`, `Matches`, `NewFileText`.
- `TaskSettings`: the top folder, the list open last, whether done tasks are hidden.

## Gotchas

- `TextDocument.Read` gives CRLF text whatever the file holds, and `ToText` writes CRLF, so the text kept for the
  freshness check compares equal after a round trip.
- `NaturalOrder` is `StrCmpLogicalW`: the tests run on Windows only, as the whole bundle does.
