# AxCalc.Core, the pure code

Everything of axCalc that needs no window. Unit tested in `tests/AxCalc.Tests`. The spec is `docs/axcalc/SPEC.md` ("SPEC.md" below); the window is `src/AxCalc`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxCalc
```

## Where things are

- `Calculator` and `Evaluation` (SPEC.md AC-3): the evaluator, a recursive descent parser over doubles with one method per level of the grammar (written out in the class comment, loosest first), the variables (case-insensitive names), the last answer (`Answer`, which `ans` and a leading operator continue from) and the angle unit (`Degrees`). `Evaluate` returns an `Evaluation` with the value, its text, the variable an assignment set and the error; `Spoken` is the announcement. The errors are a private exception inside the parser, turned into `Evaluation.Failed` with the message that follows "Error: ".
- `NumberText` (AC-3.7, AC-4): `Format` (plain digits for a whole number under 10^15, 15 significant digits otherwise, `-0` as `0`), `Bases` (the spoken line with hexadecimal and binary in groups of four) and `Code` (`0xFF`, `0b…`, `0o…` for the clipboard).
- `CalcEntry` (AC-5.1): one history line, the expression, the answer's text and value, and the variable's name for an assignment; `Label` is the list's text.
- `CalcSettings` (AC-9.1): the settings file with the angle unit, Keep history, the history (500 at most) and the variables; `Keep` takes the window's state, or nothing of it with Keep history off; `Sanitize` drops entries and variables a hand-edited file got wrong.

## Gotchas

- A trigonometric result is rounded at the 15th decimal place, so that sin(30°) is 0.5 and sin(180°) is 0 rather than 1.2E-16 (`Trig`); tan of an odd multiple of 90° is the error "not a real number".
- `2e3` is a number (2000) but `2e` is 2 followed by the unexpected name e: an exponent is read only when a digit follows the e and its sign.
- The executable runs with invariant globalization; every number is parsed and formatted with `CultureInfo.InvariantCulture` anyway, so that a decimal comma never appears.
