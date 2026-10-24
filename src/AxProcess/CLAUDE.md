# axProcess, the window

The WinForms side of axProcess: one list with three views (Processes, Services, Startup), a status bar, the bundle's notices. The pure code (the process table, the services, the startup list, the system's memory and times, the settings) is `src/AxProcess.Core`, with its own `CLAUDE.md`. The spec is `docs/axprocess/SPEC.md` ("SPEC.md" below); the guide `docs/axprocess/user-guide.md` is embedded by `AxProcess.csproj`; the manual test plan is `docs/axprocess/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axprocess.ps1               # build Axit and start axProcess (run.ps1 -App process)
.\test.ps1 AxProcess              # the tests in Release, as CI runs them
```

## Where things are

- `AxProcessApp`: the entry point `Run` (bundle AX-1.7), one call into `Axit.Forms/AppHost.Run`; no arguments.
- `ProcessForm`: the window (SPEC.md PR-2) on `Axit.Forms/AxitForm`. The views as `Axit.Forms/ListViewState`, as axGIT's (the list's name, the labels, the keys: the process ID, the service's name, the startup entry's place and name; whether read; the selected key); `LoadAsync` reads one on a background task with `_busy` set ("Reading…" in the status bar, "Working…" for a second command); `ReadProcesses` takes two samples a second apart (PR-D3); `FillList` and `RememberSelection` keep the item across a refresh; `InView` says which view first. The commands: `ActAsync` (the details notice of the view's item), `EndProcess` and `Kill` (the question, then `Process.Kill` off the UI thread, the ID checked against the name since Windows reuses IDs), `ShowProgram` (axFile on the program file), `CopyLine`, `SaySummary`, `ShowServiceDetails` and `ControlService` (start, stop, restart through `ServiceManager` with its wait), `SetSort` (the rows put in order again without a read).
- `AxProcessKeys`: the window's keys (SPEC.md PR-6.2; bundle AX-8); `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; the list is named with its view and count; nothing speaks on an arrow key; nothing refreshes on a timer (SPEC.md PR-D2).
2. Every read of the system and every end, start and stop runs on a background task; the window awaits it.
3. No second window (bundle AX-7.2): the details and the questions are notices.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- The list is empty for the second the first read takes, while it already has the focus; `RefreshAsync` therefore says the list's name and first item once after that read, not interrupting.
- Enter in a `ListBox` reaches `ProcessCmdKey` as `Keys.Return`; it is handled there with a `Focused` check (as in axGIT).
