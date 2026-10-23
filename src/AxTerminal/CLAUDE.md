# AxTerminal, the window

The WinForms side of AxTerminal: AxClaude's window around a shell, a transcript with NVDA-style single-key navigation and a command field at the bottom. The pure code (the launcher, the model, the arrivals, the history, the settings) is `src/AxTerminal.Core`, with its own `CLAUDE.md`; the console pieces (ConPTY, the parser, the screen, the line store, the mirror) are `src/Axit.Core`'s and the view is `src/Axit.Forms/TranscriptView`. The spec is `docs/axterminal/SPEC.md` ("SPEC.md" below); the guide `docs/axterminal/user-guide.md` is embedded by `AxTerminal.csproj` for Help → User guide; the manual test plan is `docs/axterminal/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axterminal.ps1 C:\src\myproject    # build Axit and start AxTerminal there (run.ps1 -App terminal; -NoBuild)
dotnet build src/AxTerminal/AxTerminal.csproj -o <scratch dir>   # while Axit runs: never into src/Axit/bin
dotnet test tests/AxTerminal.Tests/AxTerminal.Tests.csproj
```

## Where things are

- `TerminalForm` (on `Axit.Forms/ConsoleForm`, the bundle's console window shared with AxClaude since 2026-10-23, which owns the view, the field, the control area, the status bar pair, the pty bridge, the paced writes with their one pump, the recording, the export, find, the signals, the music, the focus keys, the bookmark toggle and the notice hooks; on `AxitForm` under it, bundle AX-6.5), one class in partial files, one per section: `TerminalForm.cs` (fields, the two constructors, `OnShown`, the closing question, the key switch of the shell's own keys, the hooks: `Working`, `SaveCommand` with the export, the restart arguments, the diagnostics lines, `FolderInHand`), `TerminalForm.Ui.cs` (the menus, Speak output, the title, the status bar), `TerminalForm.Input.cs` (the field's keys, sending with PowerShell's extra Enter through the base's queue, interrupt and Escape, Enter on a line, the recent commands in the control area), `TerminalForm.Process.cs` (starting, stopping and restarting the shell around the base's `StartProcess` and `StopProcess`, the exit), `TerminalForm.Model.cs` (what the model reports: the tick and the speech, Ready, Done or Failed with the base's signals, the "a program asks" heuristic, Ctrl+Q, Clear), `TerminalForm.NewShell.cs` (the New session notice on the shared session panel, Switch shell, the folder picker, the recent folders' action, Show in). A change goes in the file of its section; what both console windows need goes to `ConsoleForm`.
- `AxTerminalKeys`: the window's keys (SPEC.md AT-6.2; bundle AX-8), the one place a key is changed, with `Transcript` (the view's keys as data); a notice's heading letters are the bundle's (`TextJump.HeadingLetters`). `HelpText`: the F1 text and the embedded guide. `AxTerminalApp`: the entry point through `Axit.Forms/AppHost`.

## Rules

1. Every control gets an `AccessibleName`; every action in the SPEC.md key table works without a mouse; the caret in the terminal moves only on the user's own command; results of navigation are one UI Automation notification.
2. Never block the UI thread on the pseudo console: reads come on a background thread and are posted in bursts (the console window's `StartProcess`).
3. No second window (AX-7.2): questions and errors are notices (`ShowNotice`), the recent commands take the field's place (`ControlArea`); the folder and file pickers are the only separate windows.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas already learned (do not rediscover)

- The gotchas of the EDIT control, NVDA's reading after a key press, the notice state and the sounds are AxClaude's and live in `src/AxClaude/CLAUDE.md`; the shell's own are in `src/AxTerminal.Core/CLAUDE.md` (PSReadLine off with a screen reader, the empty line that ends a block, the exit code rule, cls).
- The state in the status bar is empty until the shell's first mark arrives: a custom shell without marks shows no state at all, by design (SPEC.md AT-1.4).
