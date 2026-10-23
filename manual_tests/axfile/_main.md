# axFile manual tests

The steps and every expected announcement are in `docs/axfile/nvda-test-plan.md`. This file says which folder each section uses. Run `.\manual_tests\prepare.ps1 axfile` first, and again after any section that changed files.

Start axFile on a folder with `.\run-axfile.ps1 manual_tests\axfile\<folder>`, or with Ctrl+G and the path once axFile runs.

## navigate-arrows-type-ahead-and-levels

Test plan section 1, steps 1 to 5, and step 13's arrow parts.

- Ten fruit files: apple, apricot, avocado, banana, blueberry, cherry, coconut, date, elderberry, fig. Type `apr` quickly: apricot. Type `a`, pause, `a`: the second name starting with a. Type `zzz`: the low note.
- `level-1-enter-me`, `level-2-enter-me`, `level-3-enter-me`: Enter three times reaches `you-reached-the-bottom.txt`; Backspace three times comes back, each time on the folder you left, saying "folder, count, entry".
- `empty-folder-no-items`: Enter says "No items in this folder".
- Up at the first entry and Down at the last: the low note.
- Right on `level-1-enter-me` goes into it, as Enter; Left comes back up, as Backspace; Right on a file plays the low note and opens nothing. Left all the way up reaches This PC (test plan 1.4b).
- Ctrl+G, then in the Path field Down and Up complete names from what you typed, Right goes into a completed folder (test plan 1.7). Try it with this folder's path and `a`: apple, apricot, avocado.

## hidden-and-period-entries

Section 1, step 3a. `visible-file.txt` always shows; `hidden-file.txt` has the hidden attribute; `.period-file` starts with a period. With both options off, the last line reads "Not shown: 1 hidden, 1 starting with a period".

## open-with-windows-and-editors

Section 1, step 6, and section 3, steps 10 and 12.

- `markdown-for-enter-and-ctrl-e.md`: Enter opens Windows' program for .md; Ctrl+E opens axDown.
- `json-ctrl-e-opens-axjson.json`: Ctrl+E opens axJSON.
- `type-nothing-opens.axtest`: Enter shows the Windows "How do you want to open this file?" dialog.
- `text-for-notepad-1.txt` and `text-for-notepad-2.txt`: Add Notepad as your own program, select both, open them with it.

## filter-sort-and-find

Section 1, steps 10 and 10a.

- Ctrl+Shift+F, `chap`, Enter: "5 of 9 items, filter chap" and the five chapter files.
- Sort by date: `sort-newest-and-largest.bin` first, `sort-middle-age-and-size.bin` next (10 days old), `sort-oldest-and-smallest.txt` last of the three (30 days old). Sort by size: the same three in size order.
- Ctrl+F, `find`, Enter: `find-me-somewhere-down-the-list.txt`.

## selection-space-shift-and-escape

Section 1, step 13; section 3, step 7b; section 6, steps 8 and 9.

- Twelve files, `select-01.txt` to `select-12.txt`, and two folders.
- Space, Shift+Down, Shift+Up, Shift+Home, Shift+End, Shift+Space, Ctrl+A, Ctrl+Shift+A, Escape.
- Escape with entries selected: "None selected". With a filter on and entries selected, the first Escape deselects and the second clears the filter.
- Alt+S, the Selection menu, with and without selected entries.
- Tab to the Actions list: the Selection section has a line for each change; Ctrl+Z on one puts the selection back.

## file-operations

Section 3, steps 1 to 13.

- `a.txt`: F2 keeps the extension; Ctrl+C, Ctrl+V makes `a - 2.txt`.
- `rename-me.txt`, `copy-me-then-paste.txt`, `delete-me.txt`, `delete-me-too.txt`: as named.
- `move-into-new-folder-1.txt` and `-2.txt`: select both, Shift+F10, V.
- `docs`: Shift+F10, Z makes `docs.zip`, again `docs - 2.zip`.
- `existing-archive-extract-me.zip`: Shift+F10, X extracts it. `not-a-zip.txt`: "Not a zip file".
- `paste-into-this-folder`: the target for Ctrl+X and Ctrl+V.
- `big-folder-copy-twice-at-once`: 200 files, 50 MB; Ctrl+C, then Ctrl+V twice quickly, to see two copies run side by side in the Actions list.
- `in-use-open-a-terminal-in-me-then-rename`: Shift+F10, A, T opens axTerminal in it; then F2 fails with "In use by another program". Close the terminal and F2 works.
- Ctrl+Shift+N and Ctrl+Shift+T make new entries here.

## undo-ctrl-z

Section 3, step 14. Each file says by its name what to do before Ctrl+Z: `rename-then-undo.txt`, `copy-then-undo.txt`, `delete-then-undo.txt`, the two `delete-two-then-undo` files, the two `move-two-then-undo` files, `rename-here-then-in-file-explorer.txt` (for "Not undone"), and the folder `compress-then-undo`.

## actions-list

Section 6, steps 1 to 7. Start on this folder: Tab reads "No actions yet". `start-here.txt` sits beside where you make `temp`; `subfolder-for-inner` is where step 4 makes `inner`; the two `zip-two-files` files are for step 5. For the long copy of step 8, use `file-operations\big-folder-copy-twice-at-once`.

## changes-from-file-explorer

Section 1, steps 9 and 12.

- Add a file in this folder from File Explorer, then Alt+Tab back: it is in the list.
- Enter `delete-this-folder-while-axfile-is-in-it`, delete that folder in File Explorer, Alt+Tab back: the "Folder not found" notice.

## hand-over-to-axmedia-and-axpreview

Section 4, and axMedia's and axPreview's hand-over sections.

- `README.md`, `plain.txt`, `app.log`: Shift+Enter shows each in one axPreview window. `binary-not-text.bin`: "Not a text file".
- `chapters-ctrl-enter-plays-me`: enter it, Ctrl+Enter plays its four chapters. Each says "Play folder chapter, track n of 4".
- `queue-folder-a-add-with-ctrl-shift-q` and `queue-folder-b-add-with-ctrl-shift-q`: select both from this folder, Ctrl+Shift+Q: "Added 6 to axMedia's queue". In axMedia you hear "Queue folder A" and then "Queue folder B" after the chapters.
- `no-media-here`: Ctrl+Enter inside it says "No sound or video files here".

## Not in a folder

- Section 1, step 4's This PC and drives, step 12a's Drives submenu (Folder, Drives): start anywhere and Backspace up to This PC. A USB stick is needed for Eject.
- Section 2, from the other windows: open axClaude, axDown, axGIT or axTerminal on any folder here.
- Section 5: any folder.
