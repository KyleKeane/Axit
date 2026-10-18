# AxDown.Core, the pure code

Everything of AxDown that does not need a window. The spec is `docs/axdown/SPEC.md` ("SPEC.md" below); the window is `src/AxDown`, with its own `CLAUDE.md`; the bundle rules are in the root `CLAUDE.md`. Unit tested in `tests/AxDown.Tests` (`dotnet test tests/AxDown.Tests/AxDown.Tests.csproj`, which works while an app runs).

- `TextDocument` (AD-3): a file as text with `\r\n` line endings for the edit control, plus the encoding, the byte order mark and the line ending it came with, so that `ToBytes` writes it back as it was. `FromBytes` and `ToBytes` are the pure core; `Read` and `Write` add the file and the 32 MB limit.
- `EditorSettings` (AD-8): a plain class of properties, read and written through the bundle's `SettingsFile` into `%APPDATA%\Axit\axdown.json`.
- `MarkdownOutline` lives in `Axit.Core` since its second use (bundle AX-6.1); AxDown's window uses it from there (SPEC.md §9).

Gotchas: the Windows code pages (1252 and the rest) come from `CodePagesEncodingProvider`, registered in `TextDocument`'s static constructor; the system's ANSI page is asked with `GetACP`, since the executable runs with invariant globalization and the culture cannot tell; an answer of 65001 (Windows set to UTF-8 worldwide) falls back to 1252, since the bytes were just found not to be UTF-8. The encodings are constructed with their marks so that `GetPreamble` knows them; `GetBytes` never writes a mark, `ToBytes` adds it when the file had one.
