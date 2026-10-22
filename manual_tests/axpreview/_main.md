# AxPreview manual tests

The steps and every expected announcement are in `docs/axpreview/nvda-test-plan.md`. Every file of it is in this one folder. Run `.\manual_tests\prepare.ps1 axpreview` first.

Start with `.\run-axpreview.ps1 manual_tests\axpreview\README.md`.

## The files

- `README.md`: headings (Install is the first level 2 heading), two links, a list of three items, a block quote, a table, and the word "update" twice for find and F3. Section 1, steps 1 to 7.
- `plain.txt`: four lines; no headings. Section 1, step 8.
- `app.log`: a log; it opens since its bytes read as text. Section 1, step 9.
- `data.json`: on it, Alt+F reads "Edit in AxJSON Ctrl+E". Section 1, step 11.
- `binary-not-text.bin`: "Not a text file". Section 1, step 9, and section 2, step 2.
- `too-large-over-32-mb.txt`: about 35 MB of text, "Too large". Section 1, step 9.
- `append-a-line-to-README.md.cmd`: run it (Enter on it in AxFile, or double-click in File Explorer) while AxPreview is in the background, then Alt+Tab back: "Reloaded, README.md changed on disk". Section 1, step 10.

## The hand-over

Section 2: open AxFile on this folder (`.\run-axfile.ps1 manual_tests\axpreview`), then Shift+Enter on each file in turn: one AxPreview window takes each.

## The console

Section 2, step 6: `axpreview <path of plain.txt>`, `axpreview --new <path of plain.txt>`.
