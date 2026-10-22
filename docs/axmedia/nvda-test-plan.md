# AxMedia NVDA test plan

Run with NVDA before a release, on a folder of MP3 chapters with an MP4 video and a `.txt` in it, after `run-axmedia.ps1` (or `.\run.ps1 -App media`) and from AxFile on the same folder. Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axmedia/SPEC.md` (AM-n.m).

## 1. Start and the queue (AM-1, AM-2, AM-3)

1. Start AxMedia without a path: NVDA reads "AxMedia window", then "Queue list, Queue is empty, 1 of 1". Nothing else is spoken. Enter, Delete, Ctrl+I on that line: "Queue is empty". Ctrl+Q: "Nothing playing". Space: "Nothing playing".
2. Ctrl+O: the Windows file picker titled "Choose sound or video files", on sound and video files; pick three chapters: "Added 3", the focus back in the list, which reads "Queue, 3 items list, <first>, 1 of 3". Nothing plays. Ctrl+Shift+O, pick the chapters folder: "Added 12" (its count), the list "Queue, 15 items". Ctrl+Shift+O on a folder without media: "No sound or video files in <name>".
3. Alt+Q, R: "Open recent submenu", "1. <folder> (<path>)", "2. <file> (<path>)"; Enter on the folder: "Added 12" again.
4. Down Arrow: the next item, nothing else. Home, End. Type a letter: the next name starting with it. Tab, F6, Ctrl+Tab: nothing happens (one control). Ctrl+F, part of a name, Enter: that file is read; F3: the next match, or "Not found".
5. Enter on the second file: "Playing <name>" and the sound starts; Down then Up: the item reads "<name> (playing)". NVDA+T: "<name> - AxMedia". NVDA+End: "Playing <name> 0:03 / 45:30" moving, then "2 of 15, speed 1".
6. Ctrl+I on the playing file: "<name>, 2 of 15, playing, 0:12 of 45:30"; on another: "<name>, 3 of 15".
7. Delete on another file: "Removed <name>", the list one shorter, the mark still on the playing file. Delete on the playing file: "Removed <name>" and the next plays ("Playing <next>" is heard only when Say each file is on). Queue, Clear queue: "Queue cleared", the sound stops, the list reads "Queue is empty"; NVDA+T: "AxMedia".
8. Ctrl+N: a second AxMedia window, empty; Alt+A, O, 1 goes back; Alt+F4 closes the second.

## 2. The hand-over (AM-1.2, AM-1.3)

1. With an AxMedia window open, in AxFile on the chapters folder, Shift+F10, A, M (Open in Axit, AxMedia) on the third chapter: the AxMedia window comes to the front, NVDA reads its title and "Queue, 12 items list, <third> (playing), 3 of 12", "Playing <third>", the sound starts; the queue is the folder's files, the third playing. No second window opened (Alt+A, O lists one AxMedia).
2. Back in AxFile (Alt+A, O, 1), Ctrl+Enter: "Playing <first>" in AxMedia, in front again, the queue the whole folder from the first.
3. In AxFile, Space on two folders with media, Ctrl+Shift+Q: "Added 24 to AxMedia's queue", the focus stays in AxFile, the sound goes on; Alt+Tab to AxMedia: the queue holds the folder's files, then the 24; Ctrl+Shift+Q on a text file in AxFile: "No sound or video files to add".
4. Close the AxMedia window. In AxFile, Shift+F10, A, M on a chapter: a new AxMedia window opens and plays it with the folder after it.
5. Make AxMedia the default program for `.mp3` (Settings, Apps, Default apps, AxMedia, or Open with, Always): Enter on a chapter in AxFile plays it in AxMedia, the window in front; in File Explorer, Enter on a chapter does the same; right-click a chapter, Open with Axit, AxMedia: the same. Set the default back if wanted.
6. Console: `axmedia C:\path\to\03.mp3`: the open window takes it and comes to the front; `axmedia --add C:\path\to\folder`: appended, the console command returns, the focus stays where it was; `axmedia --new C:\path\to\03.mp3`: a second window.

## 3. Playing (AM-4)

1. Enter on the first chapter: "Playing <name>". Space: "Paused at 0:05", the sound stops. Space: "Playing". Ctrl+P: "Paused at …"; Ctrl+P: "Playing". Ctrl+T, then Ctrl+P in the notice's field: nothing happens (the notice holds the keys); Escape.
2. Ctrl+Right: "0:20" and the sound jumps. Ctrl+Left: "0:05". Ctrl+T, type `1:00`, Enter: "1:00". Ctrl+T, type `abc`, Enter: "Not a time", the notice stays; Escape.
3. Ctrl+Shift+Period: "Speed 1.25"; the voice is faster at the same pitch. Twice more: "Speed 1.75". Ctrl+Shift+Comma: "Speed 1.5". Play, Normal speed: "Speed 1". Set 1.5, Alt+F4, start again and play: Ctrl+Q ends in "speed 1.5".
4. Ctrl+Q: "Playing <name>, 1:12 of 45:30, speed 1.5". Ctrl+Shift+Right: "Playing <next>". Ctrl+Shift+Left: "Playing <first>". Again: "First file".
5. Ctrl+T near the end, Enter; wait: "Playing <next>" spoken on its own, the next file plays, the selection stays where it was and the mark moves. Play, Say each file as it starts, off; the same again: nothing is spoken.
6. On the last file, Ctrl+T near its end; wait: "End of queue" with the done chime; NVDA+End: both labels empty; NVDA+T: "AxMedia". Ctrl+Q: "Nothing playing".
7. Enter on the video: "Playing <name>", the picture appears at the top of the window, the focus stays in the list; Space pauses; Ctrl+Shift+Right or the end hides the picture.
8. Enter on a file Windows cannot decode (an `.opus` without the Store codec): "Cannot play <name>: …" and the next plays. Ctrl+O and pick the `.txt` (All files): "Added 1"; Enter on it: "Cannot play <name>: …", the next plays.
9. Alt+Tab to another program while a file plays, let it end: "Playing <next>" from the background, or silence with the setting off; the sound carries on. Ctrl+W: File Explorer with the playing file selected. Queue, Show current file in, AxFile: AxFile on the folder with the file selected. Alt+F4 on AxMedia: the sound stops.

## 4. Text, notices, the Axit menu (AM-5)

1. Ctrl+Plus: "Text size 13" (or the next size); Ctrl+Minus back. Options, Text, Windows text size: "Windows text size".
2. F1: the Keyboard shortcuts notice; Escape. Help, User guide: this guide; Escape. Ctrl+S: "Nothing to save here".
3. Alt+A: the Axit menu with New window (AxMedia among the apps), Recently closed (the file an AxMedia window was playing when it closed; chosen, it plays with its folder), Open windows, Open windows by type, Close all windows, Check for updates, About.
4. Options, Developer, Copy diagnostics: "Copied diagnostics"; paste somewhere: the versions, the queue, the player's state and the log's end.
