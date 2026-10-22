# AxPreview.Core, the pure code

Everything of AxPreview that needs no window, which is one rule: whether a file reads as text. Unit tested in `tests/AxPreview.Tests` over bytes and a temporary folder. The spec is `docs/axpreview/SPEC.md` ("SPEC.md" below); the app side is `src/AxPreview`, with its own `CLAUDE.md`; the window is the text editor shell in `src/Axit.Forms` in its read-only mode. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
dotnet test tests/AxPreview.Tests/AxPreview.Tests.csproj
```

## Where things are

- `TextSniff` and `Refusal` (SPEC.md AP-2.2): `IsText` (the editors' kinds by extension, an empty file, a UTF-16 mark, else no zero byte and under five per cent control characters in the first eight kilobytes; from AxFile's preview pane, 2026-10-22), `Refuse` (the notice's title and text when a file is over the editors' 32 MB or not text, null when it opens; a file that cannot be read is left to the shell's reader, which reports it). The settings are the editors' `EditorSettings` in `Axit.Core`, in `axpreview.json`.
