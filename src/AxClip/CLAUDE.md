# axClip, the window

The WinForms side of axClip, the clipboard history: a background process with one hidden window that a global hotkey shows, a search field with the caret always in it, the items under it that never take the focus, a status bar, the bundle's notices. The window stands on `Axit.Forms/HotkeyForm`, which came out of axStart for it (the hidden window, the hotkey, the showing and hiding, the rule of the foreground); the gotchas of the foreground are in `src/AxStart/CLAUDE.md`. The pure code (the history, what a copy is kept for, the settings) is `src/AxClip.Core`, with its own `CLAUDE.md`. The spec is `docs/axclip/SPEC.md` ("SPEC.md" below); the guide `docs/axclip/user-guide.md` is embedded by `AxClip.csproj`; the manual test plan is `docs/axclip/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axclip.ps1                  # build Axit and start axClip with its field shown (run.ps1 -App clip); quit an installed axClip first
.\test.ps1 AxClip
```

## Where things are

- `AxClipApp`: the entry point `Run` (bundle AX-1.7): `--hidden`, the mutex `Local\Axit.AxClip`, a second start's "show" through `HotkeyForm.FindRunning` and `AppMessage.SendTo`, as axStart's.
- `ClipForm`: the controls and menus (SPEC.md CP-5.1); the hooks of `HotkeyForm` (`CurrentHotkey`, `KeepHotkey`, `PrepareShowing` with the list made again, `AfterShowing`, `OnHiding`, which writes a kept history); `OnHandleCreated` (the Run value, `AddClipboardFormatListener`); `WndProc` (`WM_CLIPBOARDUPDATE` posted to `ReadClipboard`); `ReadClipboard` (the excluding formats through `IsClipboardFormatAvailable`, `HistoryValue` through `OpenClipboard` and `GetClipboardData`, since WinForms' `GetData` is obsolete in .NET 10; the text through `Clipboard.GetText`; a clipboard held open read again after 100 ms, three times at most; `ClipPolicy.Keeps`); the items (`Fill` through `HotkeyForm.ShowMatches` and the hooks `MatchCount`, `SpokenMatch`, `NothingSaid`, `SayFirstMatch`, `Refill`; the 300 ms pause, `SelectMatch` with the edge sound and the keys are `HotkeyForm`'s since 2026-10-24); the commands (`Paste`: on the clipboard, `HideWindow`, then after 150 ms Ctrl+V through `SendInput` only when `ForegroundBefore` is in front again; `CopyOnly`, `ReadInFull`, `RemoveSelected`, `ClearHistory`, `TogglePause`).
- `AxClipKeys`: the window's keys (SPEC.md CP-5.2; bundle AX-8); the arrow and end keys keep axStart's names. Shift+Enter is axFile's Preview too, in KeyTests' known list.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. The caret stays in the field: no key moves the focus to the list; every move is one announcement on the field.
2. Nothing is said when a copy is kept or when the window hides; a copied text never goes into the log (bundle rule 6).
3. A paste is Ctrl+V to the window the user came from, and to no other (SPEC.md CP-D3).
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan, axStart's too while the two share `HotkeyForm`.

## Gotchas

- The window's own `Clipboard.SetText` (a paste, a copy) raises `WM_CLIPBOARDUPDATE` too; the item then moves to the top, which is what Enter means anyway.
- `SendInput` into a program running as administrator is refused by Windows without an error (UIPI); the text is on the clipboard and the guide says to press Ctrl+V there.
