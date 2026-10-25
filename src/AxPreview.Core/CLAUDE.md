# AxPreview.Core, the pure code

Everything of axPreview that needs no window: whether a file reads as text, the renderers that turn every kind of file into the content pane's text, and the Details list's lines. Unit tested in `tests/AxPreview.Tests` over bytes and a temporary folder. The spec is `docs/axpreview/SPEC.md` ("SPEC.md" below); the app side is `src/AxPreview`, with its own `CLAUDE.md`; the window is the text editor shell in `src/Axit.Forms` in its read-only mode. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
dotnet test tests/AxPreview.Tests/AxPreview.Tests.csproj
```

## Where things are

- `TextSniff` and `Refusal` (SPEC.md AP-2.2): `IsText` (the editors' kinds by extension, an empty file, a UTF-16 mark, else no zero byte and under five per cent control characters in the first eight kilobytes; from axFile's preview pane, 2026-10-22), `Refuse` (the notice's title and text for a text over the editors' 32 MB, null when it opens: every other file opens since 2026-10-24; a file that cannot be read is left to the shell's reader, which reports it).
- `Renderers`, `Renderer`, `Rendered` and `WayOut` (SPEC.md AP-7, 2026-10-24): the table, first match by extension or first bytes, the last taking every file: `Text` (`TextDocument.Read`, with the counts), `Document` (`OfficeText`: Word's `word/document.xml`, PowerPoint's `ppt/slides/slideN.xml` in number order, OpenDocument's `content.xml`; headings as Markdown headings, list items with "- ", table rows with tabs; all the parts of a document limited to 32 MB unpacked, counted on the stream as it is read (S-47), an XML reader that refuses a DTD and resolves nothing (S-48); a part named with backslashes found too, as Windows PowerShell 5.1 writes them), `Archive` (`ZipListing`, the directory only, names with forward slashes, File Explorer's order), `Media` (a short text for pictures, sound and video), `Readable` (`ReadableText`: runs of four or more printable characters, ASCII and UTF-16, from the first 32 MB). `Render` falls back to the readable text, with the reason as its first line, when a document or zip cannot be read. Tested in `RenderersTests` over files made in a temporary folder.
- `FileDetails` (SPEC.md AP-6.2, 2026-10-24): `Lines` (the File, Contents and Windows' details groups under their heading lines), `FileGroup` (the size through `Axit.Core/FileSize`, the type name through `SHGetFileInfoW`), `Attributes`, `WindowsGroup` (the property store through `SHGetPropertyStoreFromParsingName`, each property's description through `PSGetPropertyDescription`, kept when viewable and not already in the File group, its value through `PSFormatForDisplayAlloc` with Windows' direction marks taken out; on an STA thread of its own, given up after five seconds), `ValueOf` and `NameOf`. Tested in `FileDetailsTests`. The settings are the editors' `EditorSettings` in `Axit.Core`, in `axpreview.json`.
