# AxClip.Core, the pure code

Everything of axClip that needs no window. Unit tested in `tests/AxClip.Tests`. The spec is `docs/axclip/SPEC.md` ("SPEC.md" below); the window is `src/AxClip`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxClip
```

## Where things are

- `ClipEntry` (SPEC.md CP-2.5): one item, its text and time; `Label` (the first line with text, its spaces run together, cut at 120 characters, ", N lines" after it) and `LineCount` (from the first line to the last, blank ends left out).
- `ClipHistory` (CP-3.2, CP-2.3): the items newest first; `Add` moves a text kept already to the top and keeps 200; `Remove`, `Clear`; `Find` through `Axit.Core.TextMatch` (every word, case and accents folded away), newest first; `Spoken` and `Nothing` for the announcements.
- `ClipPolicy` (CP-3.1, CP-D2): `Keeps` from the text, the excluding formats, the value of `CanIncludeInClipboardHistory` and the pause; the format names are here, the reading of them is the window's.
- `ClipSettings` (CP-6.1): the settings file with the hotkey (`Axit.Core/Hotkey`, Windows+Alt+V by default), Start with Windows, Say the first match while typing, Keep history between runs, and the history only while that is on (`Keep`, and `Sanitize` drops a history the file holds with it off).
