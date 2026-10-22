# AxTerminal manual tests

The steps and every expected announcement are in `docs/axterminal/nvda-test-plan.md`. Run `.\manual_tests\prepare.ps1 axterminal` first.

Start with `.\run-axterminal.ps1 manual_tests\axterminal\start-here`.

## start-here

- `hello.txt`: `Get-Content hello.txt` prints one line.
- `ask-a-question.ps1`: `.\ask-a-question.ps1` asks "Continue? (y/n)"; after two seconds of quiet, the question chime and "Asks". Section 2, step 3.
- `print-many-lines.ps1`: 60 lines, for the ticks, Ctrl+3 and "Speak output: all lines". Section 2.
- `run-for-30-seconds.ps1`: a long run, for NVDA+End "Running", Ctrl+Q, Ctrl+Shift+C, the restart question and Alt+F4's question. Sections 2, 3 and 4.
- `fail-with-exit-code-3.cmd`: `.\fail-with-exit-code-3.cmd` gives "Failed, exit code 3".
- `subfolder-for-cd`: `cd subfolder-for-cd` and `cd ..`: the title and the status bar follow. Section 3.

The test plan's own commands (`Write-Output hello`, `cmd /c exit 3`, `Start-Sleep 30`) work here as written; the scripts are the same things with names that say what they are for.
