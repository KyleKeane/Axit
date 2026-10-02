# AxDown.Core, the pure code

Everything of AxDown that does not need a window. The spec is `docs/axdown/SPEC.md` ("SPEC.md" below); the window is `src/AxDown`, with its own `CLAUDE.md`; the bundle rules are in the root `CLAUDE.md`. Unit tested in `tests/AxDown.Tests` (`dotnet test tests/AxDown.Tests/AxDown.Tests.csproj`, which works while an app runs).

- `TextDocument` (AD-3): a file as text with `\r\n` line endings for the edit control, plus the encoding, the byte order mark and the line ending it came with, so that `ToBytes` writes it back as it was. `FromBytes` and `ToBytes` are the pure core; `Read` and `Write` add the file and the 32 MB limit.
- `EditorSettings` (AD-8): a plain class of properties, read and written through the bundle's `SettingsFile` into `%APPDATA%\Axit\axdown.json`.
- `MarkdownOutline` (SPEC.md §9): the headings of a text (`#` lines outside fenced code blocks, with level and text) and the next or previous one from a caret position, always on another line than the caret's. The other kinds of NVDA's letters (lists, links, quotes, tables) go here as they are built.

Gotchas: the Windows code pages (1252 and the rest) come from `CodePagesEncodingProvider`, registered in `TextDocument`'s static constructor; the system's ANSI page is asked with `GetACP`, since the executable runs with invariant globalization and the culture cannot tell. The encodings are constructed with their marks so that `GetPreamble` knows them; `GetBytes` never writes a mark, `ToBytes` adds it when the file had one.
