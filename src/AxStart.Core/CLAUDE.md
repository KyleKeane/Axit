# AxStart.Core, the pure code

Everything of axStart that needs no window: the programs as entries, the matching and its order, the settings (the hotkey as text is `Axit.Core/Hotkey`). Unit tested in `tests/AxStart.Tests`. The spec is `docs/axstart/SPEC.md` ("SPEC.md" below); the window is `src/AxStart`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 axStart
```

## Where things are

- `ProgramEntry` and `ProgramKind` (AS-2.1, AS-3.3): a program of the shell's Applications folder, made by `From` from its display name and parsing name: a '!' means a packaged app (Store), a whole path or a known folder's id followed by a path means a desktop program with its file (the id turned into the folder by the function the window passes, `SHGetKnownFolderPath`), anything else an id the shell keeps to itself; `Information` is what Ctrl+I says.
- `ProgramMatch` (AS-2.2): `Find` (`Axit.Core.TextMatch` since the command palette: every word of the text in the name, case and accents folded away; the rank by the first word: the name starts with it, a word of the name does, or anywhere; then the start counts, then `Axit.Core.NaturalOrder`), `Distinct` (one entry per name), `Spoken` ("Calculator, 1 of 2"), `NoMatch`.
- `Hotkey` (AS-4) is in `Axit.Core` since axClip, its second use (2026-10-23), with its tests in `tests/Axit.Tests`: the modifiers as `RegisterHotKey` takes them and a virtual key code; `ToString` in the order Ctrl, Windows, Alt, Shift ("Ctrl+Shift+Space"), `Parse` in any order and case, `Refusal` (a key needs Ctrl, Alt or the Windows key, and must be in `Names`), `Default`.
- `StartSettings` (AS-5.1): the settings file, on the bundle's `WindowSettings`, with `HotkeyText` (the file's `hotkey`, read through `Key`, the default when it is not a usable hotkey), `StartWithWindows`, `SayFirstMatch` and `Starts` (case-insensitive; names differing in case only are added together on loading).
