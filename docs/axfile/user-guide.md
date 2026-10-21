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

The list is the one control. Arrow keys, Home, End, Page Up and Page Down move through the entries; a typed letter jumps to the next name starting with it; nothing is spoken until you ask. Folders come first, then files, each in the order File Explorer uses, so `2` comes before `10`; folders read "docs, folder". Hidden and system entries are left out, as in File Explorer (the setting `showHidden` in the settings file turns them on). An empty folder shows one line, "No items in this folder", so that you hear why the list has nothing in it.

- **Enter** opens the entry: a folder in this list (its first entry selected), a `.md`, `.markdown` or `.txt` file in a new AxDown window, a `.json` file in AxJSON, a sound or video file in the player, and any other file in the program Windows has for it, or, when Windows has none, the Windows dialog that asks which program, as File Explorer does.
- **Backspace** or Alt+Up goes to the folder above, with the folder you left selected. At the top of a drive AxFile says "Top of the drive".
- **Ctrl+I** says the entry's size and date: "report.pdf, 1.2 MB, modified 2026-10-18 14:02".
- **Ctrl+E** opens the file in AxDown, or AxJSON for a `.json` file, whatever Enter would do with it.
- **Ctrl+Shift+W** shows the entry in File Explorer, selected; **Ctrl+W** shows the folder.
- **Ctrl+O** opens another folder with the Windows folder picker; Folder, Open recent folder lists the last ten you opened this way or from the command line. **Ctrl+G** goes to a typed path: `%USERPROFILE%\Downloads` works, and a file's path selects the file in its folder. "Not found" with the path when there is no such place.
- **Ctrl+N** opens a second AxFile window on the same folder, as in File Explorer, so you can go elsewhere in one and paste in the other; Alt+A, O moves between them.
- **F5** reads the folder again, "Refreshed". Coming back to the window after another program changed the folder does the same silently.
- **Ctrl+F** finds the next entry whose name contains the text; F3 and Shift+F3 the next and the previous match; "Not found" otherwise.
- **Ctrl+Shift+F** filters: type part of a name, Enter, and the list holds only the entries whose name contains it, named "Dune, 3 of 24 items, filter chap". The filter stays while you work in the folder and goes when you leave it; Escape in the list clears it, "Filter cleared", as does Ctrl+Shift+F with an empty field. Select all, Enter on a media file (the queue) and the find key all work on what is listed.
- Folder, **Sort by**: Name, Date (newest first) or Size (largest first), folders first in each; "Sorted by date". The order stays for every folder and the next window.
- **Space** selects the entry you are on, "Selected", and deselects it again, "Deselected"; while it is selected its line reads "notes.md, selected", so arrowing over it tells you. With more than one selected the key adds the count, "Selected, 3 selected". **Ctrl+A** selects every entry, "24 selected"; **Ctrl+Shift+A** deselects them all, "None selected"; Ctrl+I adds "3 selected" to the entry's information. Cut, Copy, Delete and Copy path then act on the selected entries; everything else, Enter, Rename, the ways to open, acts on the entry you are on. Going to another folder clears the selection.

## File operations

- **Ctrl+Shift+N**, New folder: a notice asks for the name; Enter makes it in the current folder, selects it and says "Created notes".
- **F2**, Rename: a box opens over the entry with its name, the part before the extension selected, so typing replaces the name and keeps the extension; Enter renames, "Renamed to Notes", and Escape keeps the old name. Leaving the box keeps it too.
- **Delete**: a notice asks "Delete notes? It goes to the Recycle Bin." (with everything in it, for a folder); Enter deletes, Escape keeps it. "Deleted notes", and you are on the next entry. With entries selected: "Delete 3 items? They go to the Recycle Bin.", "Deleted 3 items". The Recycle Bin can bring them back; AxFile has no permanent delete.
- **Ctrl+X** and **Ctrl+C** put the entry, or the selected entries, on the clipboard as files, "Cut notes.md", "Copied 3 items"; **Ctrl+V** pastes the clipboard's files into the folder shown, "Pasted notes.md" or "Pasted 3 items", moving them after a cut and copying them otherwise. File Explorer sees the same clipboard, so you can cut in AxFile and paste in File Explorer, and back. A name already in the folder gets a number; nothing is overwritten. A paste that takes more than a second says "Pasting, please wait" first. Pasting a cut entry into its own folder says "Already in this folder".
- **Ctrl+Shift+C** copies the entry's full path, "Copied path", or every selected entry's, one per line, "Copied 3 paths".

A name Windows refuses, a file in use or a folder you may not touch gives one line, "Failed: Access denied". All of these, and Select or deselect, Select all and Select none, are in the File operations submenu, the first item of the entry's menu.

## The entry's menu, and what Windows does for you

**Shift+F10**, the Applications key or a right click opens the entry's menu as a context menu; the menu bar's Item menu is the same menu. In order: File operations (the submenu above), Open, Open in editor, Open in Axit, Open with Windows, Open with another program, Open with a custom program, Show in File Explorer, Information, Properties, and More from Windows. **Ctrl+I** inside the menu says the entry's name, size and date without closing it.

- **Open in Axit** lists every app of the bundle: AxDown and AxJSON open the file, AxGit the repository that holds the entry, AxClaude and AxTerminal the folder (the entry's folder, for a file), AxFile a new window on the folder. "Not a file" when an editor is chosen on a folder.
- **Open with Windows** opens the entry with the program Windows has for it, a folder in File Explorer; when Windows has none it asks which program, in the Windows dialog, as File Explorer does. Enter on a file AxFile has no app for does the same.
- **Open with another program...** always asks which program, in that dialog, and runs it.
- **Open with a custom program** is your own list of programs and scripts, "1. Notepad++ (C:\Program Files\Notepad++\notepad++.exe)", and "No programs yet" before you add one. **Add a program** opens the Windows file picker on programs and scripts (`.exe`, `.cmd`, `.bat`, `.ps1`, `.py`; All files for anything else), then asks for the name the menu shows, with the file's name filled in; "Added Notepad++". **Remove a program** lists them; choose one to take it off, "Removed Notepad++". Choosing a program runs it on the entry, or on every selected entry at once, each path an argument, in the entry's folder; "Started Notepad++". A PowerShell script runs through `powershell.exe -File` with the paths after it; a batch file or a Python script runs as Windows runs it. A script of yours that takes files as arguments is a command of AxFile this way.
- **Properties** (Alt+Enter, as in File Explorer) shows the Properties sheet of the file or the folder.
- **More from Windows...** shows the full Windows menu for the entry, the one File Explorer shows, with every installed program's entries: Send to, an archiver, a cloud drive. Its first item is selected when it opens; Escape closes it. That menu is Windows' own, so its contents change with what you install; AxFile's own menu does not.

## The player

Enter on a sound or video file plays it, "Playing 03 Chapter 3.mp3", and when it ends the next media file of the same folder plays, to the end of the folder: "End of folder" with the done chime. The queue is the folder as it was when you pressed Enter, so you can browse elsewhere in the window, or leave it in the background, while it plays; the keys work from the list in any folder.

- **Ctrl+P** pauses, "Paused at 12:40", and resumes, "Playing". It works from anywhere in the window; Space is for selecting entries.
- **Ctrl+Right** and **Ctrl+Left** move 15 seconds forward or back and say the new position, "12:25".
- **Ctrl+Shift+Right** and **Ctrl+Shift+Left** play the next or the previous file of the folder; "First file" and "End of folder" at the ends.
- **Ctrl+Shift+Period** and **Ctrl+Shift+Comma** make it faster or slower by a quarter, between a half and three times: "Speed 1.25". The voice keeps its pitch. Player, Normal speed goes back to 1. The speed stays for the next file and the next window.
- **Ctrl+T** goes to a time: `12:40`, `1:02:10`, or `750` for seconds.
- **Ctrl+Q** says what is playing: "Playing 03 Chapter 3.mp3, 12:52 of 45:30, speed 1.25", or "Nothing playing".
- Player, Say each file as it starts (on by default) is the one thing AxFile says on its own: the next file's name when it moves on.
- Enter on another media file switches to it. Closing the window stops the player; there is no Stop key, Ctrl+P pauses.

The volume is Windows' own: your keyboard's volume keys work from anywhere. A video's picture is drawn in an area at the top of the window while it plays; the keys are the same. The formats are whatever Windows decodes: MP3, WAV, WMA, M4A and M4B, AAC and FLAC are built in; OGG, Opus and some video formats need the free codec extensions from the Microsoft Store. A file Windows cannot play is said, "Cannot play clip.ogg: this kind of file is not supported", and the player moves to the next.

## Menus

- **Folder**: New window (Ctrl+N), Open folder (Ctrl+O), Open recent folder, Go to folder (Ctrl+G), Up one level (Backspace), Refresh (F5), Sort by (Name, Date, Size), Filter (Ctrl+Shift+F), Show current folder in (File Explorer Ctrl+W, AxClaude, AxGit, AxTerminal), Exit.
- **Item**: File operations (Cut Ctrl+X, Copy Ctrl+C, Paste Ctrl+V, Select or deselect Space, Select all Ctrl+A, Select none Ctrl+Shift+A, New folder Ctrl+Shift+N, Rename F2, Delete, Copy path Ctrl+Shift+C), Open (Enter), Open in editor (Ctrl+E), Open in Axit (every app), Open with Windows, Open with another program, Open with a custom program (your list, Add a program, Remove a program), Show in File Explorer (Ctrl+Shift+W), Information (Ctrl+I), Properties (Alt+Enter), More from Windows. The same menu opens on the entry with Shift+F10, the Applications key or a right click; Ctrl+I inside it says the entry.
- **Player**: Play (Enter), Pause or resume (Ctrl+P), Next file, Previous file, Forward 15 seconds, Back 15 seconds, Go to time (Ctrl+T), Faster, Slower, Normal speed, Say each file as it starts, What is playing (Ctrl+Q).
- **Options**: Text (font and size), General (the update check), Developer (the settings file, diagnostics).
- **Axit**: new windows of every app, the files and folders closed lately, the open windows, updates, About.
- **Help**: Keyboard shortcuts (F1), this guide, File resources.

## Settings

Everything you change is kept in `%APPDATA%\Axit\axfile.json`: the last folder, the recent folders, the sort order, the speed, the font and text size, the update check, your custom programs (`programs`, a name and a path each), and two switches with no menu item: `showHidden` (hidden and system entries in the list) and `seekSeconds` (what Ctrl+Left and Ctrl+Right move by, 15). Options, Developer, Open settings file opens it in AxJSON.

## Updates

AxFile checks for a newer Axit when it starts and offers it in a notice; the Axit menu has Check for updates at any time. One update brings every app of Axit up to date.

## If something goes wrong

AxFile's log is `%LOCALAPPDATA%\Axit\logs\axfile.log`; Options, Developer, Copy diagnostics copies its end with the versions, for a bug report. "Folder not found" means the folder is gone or the drive unplugged; AxFile shows the nearest folder above it that exists. "Failed: Access denied" means Windows does not let your account change that place.

## About AxFile

AxFile is one app of Axit, by Dr. Kyle Keane (www.kylekeane.com), MIT licence. The Axit menu's About names the version and the licence.

## Disclaimer

Delete sends entries to the Recycle Bin through Windows' own file operation, and Rename and New folder change your files as File Explorer would; what Windows does with them is Windows' doing. The player decodes with the codecs Windows has.
