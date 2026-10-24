# axCalc, the window

The WinForms side of axCalc: the history as a list, the expression field under it, a status bar, the bundle's notices. The pure code (the evaluator, the answer's text and bases, the history entry, the settings) is `src/AxCalc.Core`, with its own `CLAUDE.md`. The spec is `docs/axcalc/SPEC.md` ("SPEC.md" below); the guide `docs/axcalc/user-guide.md` is embedded by `AxCalc.csproj` for Help → User guide; the manual test plan is `docs/axcalc/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axcalc.ps1                  # build Axit and start axCalc (run.ps1 -App calc)
.\test.ps1 AxCalc                 # the tests in Release, as CI runs them
```

## Where things are

- `AxCalcApp`: the entry point `Run` that Axit's `Program` calls (bundle AX-1.7), one call into `Axit.Forms/AppHost.Run`; axCalc takes no arguments, and any is reported with the usage text.
- `CalcForm`: the window (SPEC.md AC-2) on the bundle's window base (`Axit.Forms/AxitForm`). The menus (AC-7.1); `ProcessWindowKey`, the one key switch, from `BundleKeys` and `AxCalcKeys`, with Enter, Escape and Delete meaning what the control with the focus needs (the field: answer, clear; the history: recall, back, remove); `Evaluate` (one `Calculator.Evaluate`, the entry added, the field emptied, the answer announced; an error announced with the field kept); `CurrentValue` (the selected entry's value while the history has the focus, the last answer otherwise), which Ctrl+Shift+C, Ctrl+I and Copy answer in act on; the history kept in `_entries` beside the list's labels, the two changed together; `AfterChange` for the list's name and the status bar; the settings written on closing through `CalcSettings.Keep`, and at once after a clear. `AnnounceSource` is the control with the focus.
- `AxCalcKeys`: the window's keys (SPEC.md AC-7.2; bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; the history is named with its count; nothing speaks on an arrow key; an answer is one announcement, interrupting.
2. The arrow keys in the field are the edit control's own and never pull a history line into it (SPEC.md AC-D5); the history and Enter are the way back.
3. No second window (bundle AX-7.2): questions are notices; the save dialog is the system's own.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- Enter in a `ListBox` and in a single-line `TextBox` reaches `ProcessCmdKey` as `Keys.Return`; both are handled there with a `Focused` check, and returning true keeps the list from dinging (as in axGIT).
