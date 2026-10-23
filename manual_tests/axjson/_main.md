# axJSON manual tests

The steps and every expected announcement are in `docs/axjson/nvda-test-plan.md`. Every file of it is in this one folder. Run `.\manual_tests\prepare.ps1 axjson` first, and again after formatting, to have the files as new.

Start with `.\run-axjson.ps1 manual_tests\axjson\settings.json`.

## The files

- `settings.json`: the test plan's file; five keys, an object, two arrays. Section 2: Ctrl+K, Ctrl+J, Ctrl+L, Ctrl+I, Ctrl+E.
- `broken.json`: the same with the comma after "opus" missing and an extra comma after the empty array. Section 2, step 4, and section 3, step 2: Format says "repaired: 1 trailing comma, 1 missing comma".
- `one-line-format-me.json`: the same content on one line. Section 3, step 1: Ctrl+Shift+F formats it, Ctrl+Z puts the one line back.
- `already-formatted.json`: section 3, step 4, "Already formatted".
- `notes.txt`: in File Explorer, right-click, Open with Axit: axDown alone is listed for a text file. Section 1, step 3.
- `append-a-line-to-settings.json.cmd`: the file changed on disk, section 1, step 4. The line it adds breaks the JSON, which is fine for that step; run prepare again afterwards.
