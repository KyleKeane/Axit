# axCalc user guide

axCalc is a calculator made for screen reader users: one field to type an expression in, and a history of every answer above it. Type, press Enter, hear the answer. Nothing else is spoken unless you ask.

## Install

axCalc is part of Axit. Installing Axit (see the Axit guide in the Axit menu, About) gives you a Start menu entry `Axit axCalc` and a console command `axcalc`. Any Axit window starts one too: Alt+A, New window, axCalc.

## Start

The window is called "Calculator - axCalc", and the focus starts in the field named Expression. Above it is the history, a list; Tab, Shift+Tab, Ctrl+Tab or F6 move between the two.

## Answers

- Type an expression and press **Enter**: axCalc says the answer, adds the line "expression = answer" to the end of the history, and empties the field.
- An expression that starts with an operator continues from the last answer: after `1920*1080`, typing `*4` gives the answer times four. A leading minus is a negative number, so `-3` is minus three; to subtract from the last answer, type `ans-3`.
- **Escape** empties the field. **Ctrl+Q** says the last answer again.
- A mistake says what went wrong, "Error: missing closing parenthesis", and leaves your text in the field to mend.

What you can type:

- `+ - * /` as usual; `×` and `÷` work too. `%` or the word `mod` gives the remainder. `^` or `**` is a power: `2^10` is 1024. `!` after a whole number is its factorial: `5!` is 120.
- Parentheses group: `(2+3)*4` is 20.
- Numbers: `2.5`, `.5`, `6.02e23`; underscores as separators, `1_000_000`; hexadecimal `0xFF`, binary `0b1010`, octal `0o17`.
- Functions, with their arguments in parentheses: `sqrt`, `cbrt`, `abs`, `exp`, `ln`, `log` (base 10), `log2`, `sin`, `cos`, `tan`, `asin`, `acos`, `atan`, `floor`, `ceil`, `trunc`, `round` (`round(3.14159, 2)` is 3.14), `min`, `max`, `hypot`.
- Names: `pi`, `e`, and `ans` for the last answer.
- Variables: `rate = 1.25` sets one; then `400 * rate` is 500. Calculator, Variables lists them.

Angles are in degrees unless you choose Options, Angles, Radians.

## Other bases

**Ctrl+I** says the answer in hexadecimal, binary and octal: "255, hex FF, binary 1111 1111, octal 377". The digits come in groups of four, so the reading has pauses. Calculator, Copy answer in, copies the answer as `0xFF`, `0b11111111` or `0o377`, ready to paste into code.

## The history

The newest line is at the bottom, so Up from the end reads back from the latest. axCalc keeps the history and your variables between runs, in plain text in its settings file (`%APPDATA%\Axit\axcalc.json`); Calculator, Clear history empties it. In the history:

- **Enter** puts the line's expression back in the field, to change it and press Enter again.
- **Ctrl+Shift+C** copies the line's answer. In the field, it copies the last answer.
- **Ctrl+I** says the line's answer in other bases.
- **Delete** removes the line; **Escape** goes back to the field.
- **Ctrl+F** finds a line by its text, F3 the next one.

**Ctrl+S** saves the whole history as a text file, one line per answer. Calculator, Clear history (Ctrl+Shift+Delete) empties it after a question. The history and your variables are kept until next time, unless you turn off Options, Keep history between runs.

## Menus

- **Calculator**: Answer (Enter), Copy answer (Ctrl+Shift+C), Copy answer in, Answer in other bases (Ctrl+I), Say the last answer (Ctrl+Q), Variables, Clear variables, Save history (Ctrl+S), Clear history (Ctrl+Shift+Delete), Exit.
- **Options**: Angles (Degrees, Radians), Keep history between runs, Text (font and size), General (the update check), Developer (the settings file, diagnostics).
- **Axit**: new windows of every app, the files and folders closed lately, the open windows, updates, About.
- **Help**: Keyboard shortcuts (F1), this guide, Calculator resources.

## Settings

Everything axCalc keeps is in `%APPDATA%\Axit\axcalc.json`: the font and text size, the angle unit, the update check, and the history and variables while Keep history is on. Options, Developer, Open settings file opens it in axJSON.

## Updates

axCalc checks for a newer Axit when it starts and offers it in a notice; the Axit menu has Check for updates at any time. One update brings every app of Axit up to date.

## If something goes wrong

axCalc's log is `%LOCALAPPDATA%\Axit\logs\axcalc.log`; it never holds what you calculated. Options, Developer, Copy diagnostics copies its end with the versions, for a bug report.

## About axCalc

axCalc is one app of Axit, by Dr. Kyle Keane (www.kylekeane.com), GNU AGPL 3 licence. The Axit menu's About names the version and the licence.

## Disclaimer

axCalc calculates with double-precision numbers, as most calculators and programming languages do: about 15 significant digits, and a decimal fraction such as 0.1 is held as the nearest binary value. Check an answer that matters another way.
