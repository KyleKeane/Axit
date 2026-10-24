# AxList.Core, the pure code

Everything of axList that needs no window. Unit tested in `tests/AxList.Tests`, the folder tests in a temporary folder. The spec is `docs/axlist/SPEC.md` ("SPEC.md" below; the file format is its section 5); the window is `src/AxList`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxList
```

## Where things are

- `ListLine` and `LineKind` (SPEC.md §5): one line read: blank, heading, text or item; its width (spaces, a tab counting two), its marker (the bullet, the numbered separator with its number, the heading's # signs), task and done, its text, and `Raw`, the line as it was. `With` and `Sibling` make changed and new lines with their raw text rendered (`Render`: two spaces a level, the marker, the box, the text).
- `ListDocument` (LI-4): the lines of a file and every change as a pure operation that returns the new document: `Toggle`, `SetText`, `ToggleTask`, `Insert` (after an item's nested lines, of its kind), `Remove` (with the nested lines), `Move` (past the sibling, with both nested blocks), `Indent` (under the sibling before, or out to the parent's width), each numbered run numbered again from 1 (`Renumbered`). `Shown`, `Levels`, `Label` and `NameOf` are the view's; `Holds` checks a line is still the one shown (LI-6.2); `SubtreeEnd` is what "nested" means: the following lines wider than the item, up to a blank line.
- `ListFolder`, `ListSummary`, `OpenTask` (LI-3, LI-5): the folder's `.md` files in File Explorer's order (`NaturalOrder`), each list's counts, the open tasks of all of them, a new list's refused names, `TryRead` through the editors' `TextDocument`.
- `ListSettings` (LI-8.1): the folder of lists (`Documents\Lists` by default), the list open last, Hide done.

## Gotchas

- `TextDocument.Text` has `\r\n` line ends whatever the file's; `ListDocument.Parse` splits on them and `ToText` joins with them, and `TextDocument.ToBytes` writes the file's own. A file's last line end is kept or left off as it was.
- `---` (a rule), `#tag` and `1.5 kg` are text lines, not items or headings: the item pattern wants a space after the marker, the heading pattern a space after the # signs.
