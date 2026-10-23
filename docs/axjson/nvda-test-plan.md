# AxJSON NVDA test plan

The files for this plan are in `manual_tests/axjson`, made and reset by `.\manual_tests\prepare.ps1 axjson`; its `_main.md` says which folder or file each section uses.

Manual checks with NVDA running, against `docs/axjson/SPEC.md`. AxJSON is the editor shell AxDown is, so
`docs/axdown/nvda-test-plan.md` sections 1 to 7 (start, reading and writing, files, find and go to line, text size,
menus, updates) apply as they are, with "AxJSON" for "AxDown" and `settings.json` for the files; run them once on
AxJSON after a change to the shell. What follows is AxJSON's own. Prepare `settings.json`:

```json
{
  "model": "opus",
  "permissions": { "allow": ["Bash", "Read"], "deny": [] },
  "count": 3,
  "on": true
}
```

and `broken.json`, the same with the comma after `"opus"` removed and a comma added after `[]`.

## 1. Start

1. Start AxJSON from the Start menu: NVDA reads "Untitled - AxJSON window", then "Untitled edit multi line".
2. `axjson settings.json` in a console, and `axit settings.json`: both open AxJSON on the file, "settings.json - AxJSON window".
3. In File Explorer, right-click `settings.json`: the Open with Axit submenu lists AxDown and AxJSON, and AxJSON opens
   the file; on a `.txt` file the submenu lists AxDown only. Windows' own Open with lists AxJSON for `.json`.
4. The file flows are the shell's: run AxDown's section 3, step 7 (a file changed by another program) on a `.json`
   file once; the announcements name the file.

## 2. Navigation (AJ-2.2)

1. In `settings.json` with the caret at the top, Ctrl+K: "model: opus", the caret on the opening quote of `"model"`.
   Ctrl+K again: "permissions, object with 2 keys"; then "allow, array with 2 items"; "deny, empty array"; "count: 3";
   "on: true"; then "No next key", the caret staying. Ctrl+Shift+K goes back the same way; at the top "No previous key".
2. Ctrl+Home, Ctrl+J: "permissions, object with 2 keys" (the next object: the caret on the first brace is on the whole file already, as on a heading in AxDown); Ctrl+Shift+J from there: "object with 4 keys" (the whole file); Ctrl+J, Ctrl+J: "permissions,
   object with 2 keys"; Ctrl+J: "No next object".
3. Ctrl+Home, Ctrl+L: "allow, array with 2 items"; Ctrl+L: "deny, empty array". Ctrl+Home, Ctrl+I: "Item 1 of 2,
   Bash"; Ctrl+I: "Item 2 of 2, Read"; Ctrl+I: "No next item".
4. Ctrl+E in `settings.json`: "No next error". In `broken.json`, Ctrl+Home, Ctrl+E: "No next error" as well: its missing
   comma and its trailing comma are repairs (AJ-2.2), which Format makes. A value left out is an error: in a copy with `"model": ,`,
   Ctrl+Home, Ctrl+E: "Error, line 2: expected a value, found ','', the caret there. Ctrl+Shift+E from the end lands there too.
5. Navigate, Next lists Key, Object, Array, Item and Error with their keys, Navigate, Previous the same; each item does
   what its key does. Help, User guide: Ctrl+K and the other keys move through the guide's text too, "No next key"
   when there is none.
6. Plain arrow keys speak nothing of AxJSON's own.

## 3. Format (AJ-2.3)

1. In `settings.json`, put everything on one line (join the lines), then Ctrl+Shift+F: "Formatted"; Ctrl+Home and
   Down Arrow read `{`, then `  "model": "opus",`, two spaces per level, one key per line; Ctrl+Z puts the one line back
   ("Undone"); Ctrl+Y formats it again ("Redone"). AxDown's section 2, step 6 (the history) holds here too.
2. In `broken.json`, Ctrl+Shift+F: "Formatted, repaired: 1 trailing comma, 1 missing comma"; Ctrl+E then says "No next
   error". Type `'x'` as a value somewhere and Ctrl+Shift+F: "Formatted, repaired: 1 single-quoted string".
3. Delete a value, leaving `"count": ,`, then Ctrl+Shift+F: "Not valid JSON: line 4, expected a value, found ','"; the
   text is unchanged. Edit, Format does the same as the key.
4. Ctrl+Shift+F on an already formatted file: "Already formatted", nothing changes, and the title shows no star.

## 4. The Axit menu and About

1. Alt+A, N: New window lists every app of the bundle; Alt+A, N, J starts a new AxJSON window.
2. Alt+A, B: Open windows by type lists AxJSON (n) with this window checked. Alt+A, About, Version names the nine apps.
