# axCalc NVDA test plan

Run with NVDA before a release, after `run-axcalc.ps1` (or `.\run.ps1 -App calc`). No files are needed. Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axcalc/SPEC.md` (AC-n.m).

## 1. Start (AC-1, AC-2)

1. Start axCalc: NVDA reads "Calculator - axCalc window", then "Expression, edit". Nothing else is spoken.
2. Shift+Tab: "History, empty list" (or "History, N entries" and the last line). Tab: the field again. F6 and Ctrl+Tab: the same two.
3. NVDA+End: the status bar: "No answer yet" (or "Answer …") and "Degrees, no variables".
4. Alt+F4 closes the window without a question.

## 2. Answers (AC-3, AC-6)

1. Type `1920*1080*4`, Enter: "8294400". The field is empty. Shift+Tab: the history's last line "1920*1080*4 = 8294400".
2. In the field, `*2`, Enter: "16588800". `-3`, Enter: "-3". `ans*2`, Enter: "-6".
3. `rate = 1.25`, Enter: "rate = 1.25". `400 * rate`, Enter: "500". NVDA+End: "Degrees, 1 variable".
4. `sqrt(-1)`, Enter: "Error: not a real number"; the field still holds `sqrt(-1)` (Shift+Home reads it). `(2+3`, Enter: "Error: missing closing parenthesis". `foo`, Enter: "Error: unknown name foo".
5. Escape with text: "Cleared". Escape again: "Empty". Enter on the empty field: "Type an expression".
6. Ctrl+Q: "Answer 500" (the last answer).
7. Up and Down in the field: the edit control's own; the field's text does not change to a history line.
8. `sin(30)`, Enter: "0.5". Options, Angles, Radians: "Radians"; `sin(pi/2)`, Enter: "1". Options, Angles, Degrees: "Degrees".

## 3. Other bases (AC-4)

1. `255`, Enter, then Ctrl+I: "255, hex FF, binary 1111 1111, octal 377".
2. `2.5`, Enter, Ctrl+I: "2.5 is not a whole number".
3. `255`, Enter; Calculator, Copy answer in, Hexadecimal: "Copied 0xFF"; paste into axDown: `0xFF`.

## 4. The history (AC-5)

1. Shift+Tab to the history; Up and Down: each line read alone, nothing else.
2. On `1920*1080*4 = 8294400`, Ctrl+Shift+C: "Copied 8294400". Ctrl+I: that line's bases.
3. Enter: the focus in the field, which holds `1920*1080*4` with the caret at its end (NVDA reads the field and its text). Change it, Enter: the new answer.
4. In the history, Delete: "Removed"; the line is gone and the next one is selected. Escape: back to the field.
5. Ctrl+F in the window: the Find notice; type `rate`, Enter: the line `rate = 1.25` selected and read; F3: the next match or the same again.
6. Ctrl+S: the Windows save dialog; save as `calc-test.txt`: "Saved calc-test.txt". Open the file in axDown: one line per entry.
7. Ctrl+Shift+Delete: the notice "Clear the history?" with "The N entries cannot be brought back."; Escape: nothing changes. Ctrl+Shift+Delete, Enter: "History cleared"; Shift+Tab: "History, empty".
8. Calculator, Variables: the notice lists `rate = 1.25`. Calculator, Clear variables, Enter: "Variables cleared".

## 5. Kept between runs (AC-9)

1. Make two answers, close axCalc, start it again: the history holds them; Ctrl+Q says the last.
2. Options, Keep history between runs (unchecked), close and start again: the history is empty.

## 6. Text, notices, the Axit menu (AC-7)

1. Ctrl+Plus: "Text size 13" (or the next size); Ctrl+Minus back.
2. F1: the Keyboard shortcuts notice; h moves by heading; Escape closes it. Help, User guide: this guide; Escape.
3. Alt+A: the Axit menu; New window lists axCalc; About, Version names axCalc among the apps.
4. Ctrl+Shift+P, type `bases`, Enter: the bases of the last answer are said.
