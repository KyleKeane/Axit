# AxFile NVDA test plan

Run with NVDA before a release, on a folder with a few subfolders, text files and sound files (a folder of MP3 chapters, and one `.json` and one `.md`), after `run-axfile.ps1 C:\path\to\folder` (or `.\run.ps1 -App file <folder>`). Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axfile/SPEC.md` (AF-n.m).

## 1. Start and the list (AF-1, AF-2, AF-3)

1. Start AxFile on the folder: NVDA reads "<folder name> - AxFile window", then "<folder name>, N items list, <first entry>, 1 of N". Nothing else is spoken. A folder entry reads "docs, folder".
2. Down Arrow: the next item, nothing else. Home, End: the first and the last. Type the first letter of an entry further down: NVDA reads it. Tab: nothing happens (the list is the one control).
3. NVDA+End: the status bar: nothing on the left, the folder's full path on the right.
4. On a folder, Enter: NVDA reads the first entry of that folder; the title (NVDA+T) names it. Backspace: back in the folder above, on the folder you left. Alt+Up does the same. At `C:\`, Backspace: "Top of the drive". Enter on an empty folder: "<name>, empty, list, No items in this folder, 1 of 1"; Enter, Ctrl+I or F2 on that line: "No items in this folder".
5. Ctrl+I on a file: "<name>, <size>, modified <date time>"; on a folder: "<name>, folder, modified …".
6. On a `.md` file, Enter: a new AxDown window on it (Alt+A, O goes back). On a `.json` file: AxJSON. On a `.pdf` or similar: the program Windows has for it. On a file of a type nothing opens (make `x.axtest`): "No program opens .axtest. Ctrl+E opens it as text"; Ctrl+E: AxDown on it.
7. Ctrl+G: the notice "Go to folder" with the field "Path" holding the current folder; type `%USERPROFILE%\Downloads`, Enter: the list is Downloads. Ctrl+G, type a file's full path, Enter: its folder with the file selected. Ctrl+G, type `C:\nope`, Enter: "Not found: C:\nope".
8. Ctrl+O: the Windows folder picker; choose a folder: the list shows it, the focus back in the list. Folder, Open recent folder: the folders opened by command as "1. name (path)".
9. In File Explorer, add a file to the folder; Alt+Tab back to AxFile: the file is in the list, on the entry you had, nothing spoken. F5: "Refreshed".
10. Ctrl+F, type part of a name further down, Enter: NVDA reads that entry. F3: the next match, or "Not found". Shift+F3: back.
11. Ctrl+S: "Nothing to save here". F1: the Keyboard shortcuts notice; Escape closes it.
12. Delete the folder shown in File Explorer while AxFile is on it, then Alt+Tab back: the notice "Folder not found" naming it; Enter: the list shows the folder above.

## 2. From and to the other windows (AF-1.3)

1. In AxClaude on a project, Alt+S, I: "Show current folder in submenu", Down to "AxFile"; Enter: a new AxFile window on the project folder.
2. In AxDown on a file, Alt+F, I, F: AxFile on the file's folder with the file selected. The same from AxJSON.
3. In AxGit, Alt+R, I, F: AxFile on the repository. In AxTerminal, Alt+S, I, F: AxFile on the shell's folder.
4. In AxFile, Alt+F, I: "File Explorer Ctrl+W", "AxClaude", "AxGit", "AxTerminal"; Enter on AxTerminal: a shell in the folder. Ctrl+W: File Explorer on the folder. Ctrl+Shift+W on an entry: File Explorer with the entry selected.

## 3. File operations and the entry's menu (AF-4)

1. Ctrl+Shift+N: the notice "New folder" with the field "Name"; Enter on the empty field: "Type a name", the notice stays; type `notes`, Enter: "Created notes", the focus in the list on "notes, folder".
2. F2 on it: NVDA reads "New name, edit, notes, selected notes"; type `Notes`, Enter: "Renamed to Notes", the focus in the list on "Notes, folder". F2 on a file `a.txt`: the box holds "a.txt" with "a" selected, so typing replaces the name and keeps the extension; Escape: the box closes, the list reads "a.txt" again, nothing renamed. F2, Enter without typing: "Name unchanged".
3. Delete on Notes: the notice "Delete Notes?" with "It goes to the Recycle Bin with everything in it." and the buttons Delete and Cancel; Escape: nothing happens; Delete, Enter: "Deleted Notes", the focus on the next entry. Open the Recycle Bin in File Explorer: Notes is there.
4. Ctrl+Shift+C on a file: "Copied path"; paste somewhere: the full path.
5. Ctrl+Shift+N with the name of an existing entry: "Failed: There is already an entry named …". F2 with `a?b`, Enter: "A name cannot contain …", the box stays.
6. Ctrl+C on a file: "Copied a.txt"; Ctrl+V: "Pasted a.txt", a second file with a number in its name is in the list. Ctrl+X on it: "Cut …"; Ctrl+V here: "Already in this folder"; Enter on a folder, Ctrl+V: "Pasted …", the file is in the list, and Backspace shows it gone from the folder above. Copy a file in File Explorer, Ctrl+V in AxFile: it arrives; Ctrl+X in AxFile, paste in File Explorer: it moves. Ctrl+V with text on the clipboard: "Nothing to paste".
7. Shift+F10 on a file: the entry's menu opens as a context menu and NVDA reads its first item, "File operations submenu"; Right into it: Cut Ctrl+X, Copy Ctrl+C, Paste Ctrl+V, New folder, Rename F2, Delete, Copy path; Left, Down through the rest: Open Enter, Open in editor, Open in Axit submenu, Open with Windows, Open with another program, Show in File Explorer, Information, Properties Alt+Enter, More from Windows; Escape closes it and the focus is back on the entry. The Applications key does the same. Alt+I: the same items in the menu bar.
8. Shift+F10, then Ctrl+I: the entry's name, size and date are said and the menu stays open; Down still moves in it. The same from Alt+I.
9. Shift+F10, A: the Open in Axit submenu, AxClaude, AxDown, AxJSON, AxGit, AxTerminal, AxFile; D on a `.txt` file: a new AxDown window on it; Alt+F4 brings the focus back to the entry. On a folder, D: "Not a file". T on a folder: AxTerminal on it.
10. Alt+Enter on a file: the Windows Properties sheet, read by NVDA as any dialog, with the file's name in its title; Escape closes it and the focus is back on the entry in AxFile. Alt+Enter on a folder: the folder's sheet. Shift+F10, G on a `.txt` file: the Windows "How do you want to open this file?" dialog; Escape closes it. Shift+F10, G on a folder: "Not a file". Shift+F10, W on a file with no program for its type: the same dialog; Enter on such a file: the same.
11. Shift+F10, M on a file: the Windows menu for it, its first item read at once; Down through it, Right into Send to and back; Escape closes it and the focus is back on the entry, nothing done. Again, and choose Open: the file opens as it would from File Explorer.

## 4. The player (AF-5)

1. Enter on the first MP3 of a chapters folder: "Playing <name>" and the sound starts. NVDA+End: "Playing <name> 0:03 / 45:30", the time moving.
2. Space: "Paused at 0:05", the sound stops. Space: "Playing". Ctrl+Right: "0:20" and the sound jumps. Ctrl+Left: "0:05". Ctrl+T, type `1:00`, Enter: "1:00".
3. Ctrl+Shift+Period: "Speed 1.25"; the voice is faster at the same pitch. Ctrl+Shift+Period twice: "Speed 1.75". Ctrl+Shift+Comma: "Speed 1.5". Player, Normal speed: "Speed 1". Set 1.5 again, Alt+F4, start AxFile again and play: the speed is 1.5 (Ctrl+Q says so).
4. Ctrl+Q: "Playing <name>, 1:12 of 45:30, speed 1.5". Ctrl+Shift+Right: "Playing <next name>". Ctrl+Shift+Left: "Playing <first name>". Ctrl+Shift+Left again: "First file".
5. Ctrl+T, type `45:20` (near the end), Enter; wait: "Playing <next name>" spoken on its own, the next file plays. Player, Say each file as it starts, off; the same again: the next file plays and nothing is spoken.
6. Backspace to the folder above while it plays: the sound goes on; Space pauses it from there; Enter on another media file elsewhere: "Playing <that name>", the first queue is dropped.
7. On the last file, Ctrl+T near its end, Enter; wait: "End of folder" with the done chime; NVDA+End: the left of the status bar is empty. Ctrl+Q: "Nothing playing". Space: "Nothing playing".
8. Enter on a video file (`.mp4`): "Playing <name>", the picture appears at the top of the window, the focus stays in the list; Space pauses; Ctrl+Shift+Right or the end hides the picture.
9. Enter on a file Windows cannot decode (an `.opus` without the Store codec): "Cannot play <name>: …" and the next file plays.
10. Alt+Tab to another program while a file plays, let it end: whether "Playing <next>" is heard from the background; the sound carries on either way. Alt+F4 on AxFile: the sound stops.

## 5. Text, notices, the Axit menu (AF-6)

1. Ctrl+Plus: "Text size 13" (or the next size); Ctrl+Minus back. Options, Text, Windows text size: "Windows text size".
2. Help, User guide: this guide; Escape.
3. Alt+A: the Axit menu with New window (AxFile among the apps), Recently closed (the folder of a closed AxFile window), Open windows, Open windows by type, Close all windows, Check for updates, About. About, Version: "Axit <version>".
4. Options, Developer, Copy diagnostics: "Copied diagnostics"; paste somewhere: the versions, the folder, the player's state and the log's end.
5. Close the window and start AxFile without a folder: it opens on the same folder (the last one).
