# AxFile user guide

AxFile is a file manager for one folder, with a player for the sound and video files in it, made for screen reader users. One window shows one folder as a plain list, folders first, and Enter does the obvious thing with what you are on: a folder opens in the list, a Markdown or text file in AxDown, a JSON file in AxJSON, a sound or video file plays, and anything else opens in the program Windows has for it. The folder's media files play on in order, like an audiobook or an album.

## Install

AxFile is part of Axit. Installing Axit (see the Axit guide in the Axit menu, About) gives you a Start menu entry `Axit AxFile`, a console command `axfile`, and AxFile in the `Open with Axit` submenu when you right-click a folder or a file in File Explorer.

## Start

- Start menu: `Axit AxFile`. Press the Windows key and type `axf`. AxFile opens the folder you had last, or your profile folder the first time.
- Console: `axfile C:\Music\Dune`, or `axfile C:\Music\Dune\03.mp3` to open the folder with that file selected.
- File Explorer: right-click a folder or a file, open `Open with Axit` and choose AxFile.
- From AxClaude: Session, Show current folder in, AxFile (Alt+S, I, F). From AxDown or AxJSON: File, Show current file in, AxFile (the file's folder, the file selected). From AxGit: Repository, Show current repository in, AxFile. From AxTerminal: Session, Show current folder in, AxFile. Each opens a new AxFile window.
- To go back: Alt+A, O lists the open Axit windows, most recent first, so Alt+A, O, 1 is the window you came from. From AxFile, Folder, Show current folder in offers File Explorer (Ctrl+W), AxClaude, AxGit and AxTerminal on the folder you are in.

The window is named after the folder: "Dune - AxFile". The list is named with the folder and its count, "Dune, 24 items", and NVDA reads the entry you are on. The status bar (NVDA+End) has what is playing on the left and the folder's full path on the right.

## The list

The list is the one control. Arrow keys, Home, End, Page Up and Page Down move through the entries; a typed letter jumps to the next name starting with it; nothing is spoken until you ask. Folders come first, then files, each in the order File Explorer uses, so `2` comes before `10`; folders read "docs, folder". Hidden and system entries are left out, as in File Explorer (the setting `showHidden` in the settings file turns them on).

- **Enter** opens the entry: a folder in this list (its first entry selected), a `.md`, `.markdown` or `.txt` file in a new AxDown window, a `.json` file in AxJSON, a sound or video file in the player, and any other file in the program Windows has for it. When Windows has none, AxFile says "No program opens .xyz. Ctrl+E opens it as text".
- **Backspace** or Alt+Up goes to the folder above, with the folder you left selected. At the top of a drive AxFile says "Top of the drive".
- **Ctrl+I** says the entry's size and date: "report.pdf, 1.2 MB, modified 2026-10-18 14:02".
- **Ctrl+E** opens the file in AxDown, or AxJSON for a `.json` file, whatever Enter would do with it.
- **Ctrl+Shift+W** shows the entry in File Explorer, selected; **Ctrl+W** shows the folder.
- **Ctrl+O** opens another folder with the Windows folder picker; Folder, Open recent folder lists the last ten you opened this way or from the command line. **Ctrl+G** goes to a typed path: `%USERPROFILE%\Downloads` works, and a file's path selects the file in its folder. "Not found" with the path when there is no such place.
- **F5** reads the folder again, "Refreshed". Coming back to the window after another program changed the folder does the same silently.
- **Ctrl+F** finds the next entry whose name contains the text; F3 and Shift+F3 the next and the previous match; "Not found" otherwise.

## The four operations

- **Ctrl+Shift+N**, New folder: a notice asks for the name; Enter makes it in the current folder, selects it and says "Created notes".
- **F2**, Rename: a notice shows the name with the part before the extension selected; type the new name, Enter: "Renamed to Notes".
- **Delete**: a notice asks "Delete notes? It goes to the Recycle Bin." (with everything in it, for a folder); Enter deletes, Escape keeps it. "Deleted notes", and you are on the next entry. The Recycle Bin can bring it back; AxFile has no permanent delete.
- **Ctrl+Shift+C** copies the entry's full path, "Copied path".

A name Windows refuses, a file in use or a folder you may not touch gives one line, "Failed: Access denied". Copying and moving files are not in this version; File Explorer is one key away (Ctrl+W).

## The player

Enter on a sound or video file plays it, "Playing 03 Chapter 3.mp3", and when it ends the next media file of the same folder plays, to the end of the folder: "End of folder" with the done chime. The queue is the folder as it was when you pressed Enter, so you can browse elsewhere in the window, or leave it in the background, while it plays; the keys work from the list in any folder.

- **Space** pauses, "Paused at 12:40", and resumes, "Playing".
- **Ctrl+Right** and **Ctrl+Left** move 15 seconds forward or back and say the new position, "12:25".
- **Ctrl+Shift+Right** and **Ctrl+Shift+Left** play the next or the previous file of the folder; "First file" and "End of folder" at the ends.
- **Ctrl+Shift+Period** and **Ctrl+Shift+Comma** make it faster or slower by a quarter, between a half and three times: "Speed 1.25". The voice keeps its pitch. Player, Normal speed goes back to 1. The speed stays for the next file and the next window.
- **Ctrl+T** goes to a time: `12:40`, `1:02:10`, or `750` for seconds.
- **Ctrl+Q** says what is playing: "Playing 03 Chapter 3.mp3, 12:52 of 45:30, speed 1.25", or "Nothing playing".
- Player, Say each file as it starts (on by default) is the one thing AxFile says on its own: the next file's name when it moves on.
- Enter on another media file switches to it. Closing the window stops the player; there is no Stop key, Space pauses.

The volume is Windows' own: your keyboard's volume keys work from anywhere. A video's picture is drawn in an area at the top of the window while it plays; the keys are the same. The formats are whatever Windows decodes: MP3, WAV, WMA, M4A and M4B, AAC and FLAC are built in; OGG, Opus and some video formats need the free codec extensions from the Microsoft Store. A file Windows cannot play is said, "Cannot play clip.ogg: this kind of file is not supported", and the player moves to the next.

## Menus

- **Folder**: Open folder (Ctrl+O), Open recent folder, Go to folder (Ctrl+G), Up one level (Backspace), Refresh (F5), Show current folder in (File Explorer Ctrl+W, AxClaude, AxGit, AxTerminal), Exit.
- **Item**: Open (Enter), Open in editor (Ctrl+E), Show in File Explorer (Ctrl+Shift+W), Information (Ctrl+I), New folder (Ctrl+Shift+N), Rename (F2), Delete (Delete), Copy path (Ctrl+Shift+C).
- **Player**: Play (Enter), Pause or resume (Space), Next file, Previous file, Forward 15 seconds, Back 15 seconds, Go to time (Ctrl+T), Faster, Slower, Normal speed, Say each file as it starts, What is playing (Ctrl+Q).
- **Options**: Text (font and size), General (the update check), Developer (the settings file, diagnostics).
- **Axit**: new windows of every app, the files and folders closed lately, the open windows, updates, About.
- **Help**: Keyboard shortcuts (F1), this guide, File resources.

## Settings

Everything you change is kept in `%APPDATA%\Axit\axfile.json`: the last folder, the recent folders, the speed, the font and text size, the update check, and two switches with no menu item: `showHidden` (hidden and system entries in the list) and `seekSeconds` (what Ctrl+Left and Ctrl+Right move by, 15). Options, Developer, Open settings file opens it in AxJSON.

## Updates

AxFile checks for a newer Axit when it starts and offers it in a notice; the Axit menu has Check for updates at any time. One update brings every app of Axit up to date.

## If something goes wrong

AxFile's log is `%LOCALAPPDATA%\Axit\logs\axfile.log`; Options, Developer, Copy diagnostics copies its end with the versions, for a bug report. "Folder not found" means the folder is gone or the drive unplugged; AxFile shows the nearest folder above it that exists. "Failed: Access denied" means Windows does not let your account change that place.

## About AxFile

AxFile is one app of Axit, by Dr. Kyle Keane (www.kylekeane.com), MIT licence. The Axit menu's About names the version and the licence.

## Disclaimer

Delete sends entries to the Recycle Bin through Windows' own file operation, and Rename and New folder change your files as File Explorer would; what Windows does with them is Windows' doing. The player decodes with the codecs Windows has.
