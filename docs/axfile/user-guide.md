# AxFile user guide

AxFile is a file manager for one folder, made for screen reader users. One window shows one folder as a plain list, folders first, and Enter does what File Explorer does: a folder opens in the list, a file opens in the program you have made its default in Windows. The file operations are all here, new folder, rename, delete, cut, copy and paste, move to a new folder, zip and unzip, copy the path, each one announcement, and every one of them is a line of the Actions list under the folder list, where you can read it again, undo it or jump back to where it happened. Sound and video go to AxMedia, reading goes to AxPreview, with one key each.

## Install

AxFile is part of Axit. Installing Axit (see the Axit guide in the Axit menu, About) gives you a Start menu entry `Axit AxFile`, a console command `axfile`, and AxFile in the `Open with Axit` submenu when you right-click a folder or a file in File Explorer. The same install registers AxDown, AxJSON, AxMedia and AxPreview as programs for their kinds of file, so that you can make any of them the program Enter uses: Settings, Apps, Default apps, the app, or right-click a file, Open with, Choose another app, the app, Always. Windows lets only you set that; no program can.

## Start

- Start menu: `Axit AxFile`. Press the Windows key and type `axf`. AxFile opens the folder you had last, or your profile folder the first time.
- Console: `axfile C:\Music\Dune`, or `axfile C:\Music\Dune\03.mp3` to open the folder with that file selected.
- File Explorer: right-click a folder or a file, open `Open with Axit` and choose AxFile.
- From AxClaude: Session, Show current folder in, AxFile (Alt+S, I, F). From AxDown, AxJSON or AxPreview: File, Show current file in, AxFile (the file's folder, the file selected). From AxGit: Repository, Show current repository in, AxFile. From AxTerminal: Session, Show current folder in, AxFile. From AxMedia: Queue, Show current file in, AxFile. Each opens a new AxFile window.
- To go back: Alt+A, O lists the open Axit windows, most recent first, so Alt+A, O, 1 is the window you came from. From AxFile, Folder, Show current folder in offers File Explorer (Ctrl+W), AxClaude, AxGit and AxTerminal on the folder you are in.

The window is named after the folder: "Dune - AxFile". The list is named with its count, "24 items", so that the folder is not read twice, and NVDA reads the entry you are on. The status bar (NVDA+End) has the newest line of the Actions list on the left, "Copied 3 items", and the folder's full path on the right.

## The folder list

The folder list is the main control. Arrow keys, Home, End, Page Up and Page Down move through the entries; a typed letter jumps to the next name starting with it; nothing is spoken until you ask. Folders come first, then files, each in the order File Explorer uses, so `2` comes before `10`; folders read "docs, folder". Hidden and system entries are left out, as in File Explorer, and so are entries whose name starts with a period, such as `.git`; Options, General has a switch for each, Show hidden files and folders and Show files and folders starting with a period. When something was left out, the list ends with one line that says how much, "Not shown: 3 hidden, 2 starting with a period", so you can check without turning them on. An empty folder shows one line, "No items in this folder", so that you hear why the list has nothing in it.

- **Enter** opens the entry: a folder in this list (its first entry selected), a file in the program Windows has for it, exactly as a double click in File Explorer: a `.md` file in AxDown once you have made AxDown its default, an `.mp3` in AxMedia once you have made AxMedia its default, a PDF in your PDF reader. When Windows has no program for a file, the Windows dialog that asks which program appears, as in File Explorer.
- **Shift+Enter** reads a file in AxPreview, the read-only viewer, with the window in front; on a folder it opens a new AxFile window on that folder, as in File Explorer.
- **Ctrl+E** opens the file in AxDown, or AxJSON for a `.json` file, to edit.
- **Ctrl+Enter** plays the folder's sound and video files in AxMedia, from the first; **Ctrl+Shift+Q** adds the entry, or the selected entries, a folder's media files included, to the end of AxMedia's queue, "Added 24 to AxMedia's queue", and leaves you where you are. AxMedia opens if it is not open, and keeps one window.
- **Backspace** or Alt+Up goes to the folder above, with the folder you left selected. From the top of a drive it goes to This PC, the list of your drives, "Local Disk (C:), drive", with the drive you left selected; Enter opens a drive, Ctrl+I says its free space, and Backspace at This PC says "Top". This PC is not a folder: making, renaming, pasting or deleting there says "Choose a drive first".
- The **Drives** menu (Alt+D) lists your drives by number, so Alt+D, 2 opens the second drive; then This PC, and for the drive you are on, or the one selected at This PC, **Eject this drive** and **Drive properties**. Eject is Windows' own, the one File Explorer offers for a USB stick, a card or a disc: "Ejecting Removable Disk (E:)", then Windows says whether it is safe to remove it, and the list goes to This PC. A fixed disk says "Failed: This drive has no Eject".
- **Ctrl+D** adds the folder to your Favorites, "Added to Favorites", and removes it again when it is one. Folder, Favorites lists them by number, "1. Dune (C:\Music\Dune)", so Alt+F, V, 1 is the first favorite.
- **Ctrl+I** says the entry's size and date: "report.pdf, 1.2 MB, modified 2026-10-18 14:02".
- **Ctrl+Shift+W** shows the entry in File Explorer, selected; **Ctrl+W** shows the folder.
- **Ctrl+O** opens another folder with the Windows folder picker; Folder, Open recent folder lists the last ten you opened this way or from the command line. **Ctrl+G** goes to a typed path: `%USERPROFILE%\Downloads` works, and a file's path selects the file in its folder. "Not found" with the path when there is no such place.
- **Ctrl+N** opens a second AxFile window on the same folder, as in File Explorer, so you can go elsewhere in one and paste in the other; Alt+A, O moves between them.
- **F5** reads the folder again, "Refreshed". Coming back to the window after another program changed the folder does the same silently.
- **Ctrl+F** finds the next entry whose name contains the text; F3 and Shift+F3 the next and the previous match; "Not found" otherwise.
- **Ctrl+Shift+F** filters: type part of a name, Enter, and the list holds only the entries whose name contains it, named "3 of 24 items, filter chap". The filter stays while you work in the folder and goes when you leave it; Escape in the list clears it, "Filter cleared", as does Ctrl+Shift+F with an empty field. Select all and the find key work on what is listed.
- Folder, **Sort by**: Name, Date (newest first) or Size (largest first), folders first in each; "Sorted by date". The order stays for every folder and the next window.
- **Space** selects the entry you are on, "Selected", and deselects it again, "Deselected"; while it is selected its line reads "notes.md, selected", so arrowing over it tells you. With more than one selected the key adds the count, "Selected, 3 selected". **Shift+Down**, **Shift+Up**, **Shift+Home** and **Shift+End** select a run of entries as in File Explorer: from where you first pressed Shift to where you are, each entry reading "selected" as you pass it; moving back with Shift takes them out again, and what you had selected before stays. **Ctrl+A** selects every entry, "24 selected"; **Ctrl+Shift+A** deselects them all, "None selected"; Ctrl+I adds "3 selected" to the entry's information. Cut, Copy, Delete, Copy path, Move to new folder, Compress to zip and Add to AxMedia's queue then act on the selected entries; everything else, Enter, Rename, the ways to open, acts on the entry you are on. Going to another folder clears the selection.

## File operations

- **Ctrl+Shift+N**, New folder: a notice asks for the name; Enter makes it in the current folder, selects it and says "Created notes".
- **Ctrl+Shift+T**, New text file: an empty "New file.txt" is made and the rename box opens on it with the whole name selected, so type the name you want, extension included, `notes.md`, and press Enter; Escape keeps "New file.txt".
- **Move to new folder** (in the entry's menu): a notice asks for the name; Enter makes the folder here and moves the entry, or the selected entries, into it, "Moved 3 items into notes", with the new folder selected. Like Compress to zip, without the compression.
- **F2**, Rename: a box opens over the entry with its name, the part before the extension selected, so typing replaces the name and keeps the extension; Enter renames, "Renamed to Notes", and Escape keeps the old name. Leaving the box keeps it too. A name already in the folder is refused, "Failed: There is already an entry named …", and so is one that would only sound the same, "Reads the same as notes.md, which is already here": Windows tells a trailing space, a different kind of space or an invisible character apart, but you would hear one name twice.
- **Delete**: a notice asks "Delete notes? It goes to the Recycle Bin." (with everything in it, for a folder); Enter deletes, Escape keeps it. "Deleted notes", and you are on the next entry. With entries selected: "Delete 3 items? They go to the Recycle Bin.", "Deleted 3 items". The Recycle Bin can bring them back, and so can Ctrl+Z; AxFile has no permanent delete.
- **Ctrl+X** and **Ctrl+C** put the entry, or the selected entries, on the clipboard as files, "Cut notes.md", "Copied 3 items"; **Ctrl+V** pastes the clipboard's files into the folder shown, "Pasted notes.md" or "Pasted 3 items", moving them after a cut and copying them otherwise. File Explorer sees the same clipboard, so you can cut in AxFile and paste in File Explorer, and back. A name already in the folder gets a dash and a number, "notes - 2.md"; nothing is overwritten. A paste that takes more than a second says "Pasting, please wait" first. Pasting a cut entry into its own folder says "Already in this folder".
- **Ctrl+Shift+C** copies the entry's full path, "Copied path", or every selected entry's, one per line, "Copied 3 paths".
- **Compress to zip** (in the entry's menu) puts the entry, or the selected entries, into a new zip file in the folder, named after the entry or, for several, after the folder, "Compressed to notes.zip"; a name already there gets a dash and a number, "notes - 2.zip". **Extract here** on a zip file makes a folder named after it next to it and unpacks into it, "Extracted to notes"; nothing is ever written over. "Not a zip file" on anything else; for other archive kinds use the Windows menu or a program of your own.

A name Windows refuses, a file in use or a folder you may not touch gives one line, "Failed: Access denied". All of these are in the entry's menu.

**Ctrl+Z**, Undo (Folder, Undo, which names what it would undo, "Undo rename"): the newest of these operations that can still be taken back is undone, one per press, and the entry is selected. A rename is renamed back, "Undone: renamed back to notes.md"; a new folder or a new text file is removed while it is still empty, "Undone: temp removed"; a move, after Cut and Paste or Move to new folder, is moved back, "Undone: moved back", the new folder removed when it is empty again; a paste's copies, a zip or an extracted folder go to the Recycle Bin, "Undone: notes - 2.md sent to the Recycle Bin"; a delete comes back from the Recycle Bin through Windows' own Restore, "Undone: notes.md restored". When something else has moved or filled the entry since, nothing is done and AxFile says why: "Not undone: notes.md has changed", "Not undone: temp has files now", "Not undone: notes.md is already here". "Nothing to undo" at the end. The last twenty operations of a window can be undone; there is no redo, and a new window starts with nothing to undo.

## The Actions list

Under the folder list a second list, "Actions", appears the first time an operation runs and stays. Tab, Ctrl+Tab or F6 moves between the folder list and it, Escape in it goes back to the folder list, and Folder, Actions puts you in it. Every file operation of this window is one line, newest last: "Created notes", "Renamed notes.md to todo.md", "Moved 3 items into bundle", "Copied 3 items", "Compressed to docs.zip", "Deleted 2 items". While an operation runs its line says so, "Copying 3 items", "Compressing, 40 of 120 files", and is replaced when it ends; one that failed reads "Failed: …"; one you undid ends in "undone", and one that could not be undone says why. The last fifty lines stay, and go with the window.

- **Enter** on a line goes where it happened: the folder list shows that folder with the entry the action made or moved selected (the new folder for a move into one, the first entry for a paste), and you are back in the folder list. For a line you undid, Enter goes to where the undo put things back.
- **Ctrl+Z** on a line undoes that one, whatever its place, with the same checks and answers as Ctrl+Z in the folder list; a line that cannot be undone says why: "Already undone", "Still running", "Nothing to undo here".
- **Ctrl+I** says the line's number and time, "3 of 12, 21:04".

## The entry's menu, and what Windows does for you

**Shift+F10**, the Applications key or a right click opens the entry's menu as a context menu; the menu bar's Item menu is the same menu. It is one flat list in File Explorer's order: Open, Open with, Open in Axit, Preview, Play folder in AxMedia, Add to AxMedia's queue; Cut, Copy, Paste; Rename, Delete; New folder, New text file, Move to new folder, Compress to zip, Extract here; Copy path, Show in File Explorer, Information, Properties; Windows menu. **Ctrl+I** inside the context menu says the entry's name, size and date without closing it.

- **Open in Axit** starts with Editor (Ctrl+E), AxDown or AxJSON by the file's type, then lists every app of the bundle: AxDown, AxJSON and AxPreview open the file, AxMedia plays it with its folder's other media files after it (or a folder's files), AxGit the repository that holds the entry, AxClaude and AxTerminal the folder (the entry's folder, for a file), AxFile a new window on the folder. "Not a file" when an editor or the viewer is chosen on a folder.
- **Open with** holds the ways outside Axit. **Windows default** opens the entry with the program Windows has for it, a folder in File Explorer; when Windows has none it asks which program, in the Windows dialog, as File Explorer does; Enter does the same. **Choose a program...** always asks which program, in that dialog, and runs it. Under those, your own programs and scripts, "1. Notepad++ (C:\Program Files\Notepad++\notepad++.exe)", and "No programs of your own yet" before you add one. **Add a program** opens the Windows file picker on programs and scripts (`.exe`, `.cmd`, `.bat`, `.ps1`, `.py`; All files for anything else), then asks for the name the menu shows, with the file's name filled in; "Added Notepad++". **Remove a program** lists them; choose one to take it off, "Removed Notepad++". Choosing a program runs it on the entry, or on every selected entry at once, each path an argument, in the entry's folder; "Started Notepad++". A PowerShell script runs through `powershell.exe -File` with the paths after it; a batch file or a Python script runs as Windows runs it. A script of yours that takes files as arguments is a command of AxFile this way.
- **Properties** (Alt+Enter, as in File Explorer) shows the Properties sheet of the file or the folder.
- **Windows menu...** shows the full Windows menu for the entry, the one File Explorer shows, with every installed program's entries: Send to, an archiver, a cloud drive. Its first item is selected when it opens; Escape closes it. That menu is Windows' own, so its contents change with what you install; AxFile's own menu does not.

## Menus

- **Folder**: New window (Ctrl+N), Open folder (Ctrl+O), Open recent folder, Favorites (your pinned folders, Add or remove this folder Ctrl+D), Go to folder (Ctrl+G), Up one level (Backspace), Refresh (F5), Undo (Ctrl+Z, named after what it would undo), Actions, Sort by (Name, Date, Size), Filter (Ctrl+Shift+F), Show current folder in (File Explorer Ctrl+W, AxClaude, AxGit, AxTerminal), Exit.
- **Item**: Open (Enter), Open with (Windows default, Choose a program, your own programs with Add a program and Remove a program), Open in Axit (Editor Ctrl+E and every app), Preview (Shift+Enter), Play folder in AxMedia (Ctrl+Enter), Add to AxMedia's queue (Ctrl+Shift+Q), Cut Ctrl+X, Copy Ctrl+C, Paste Ctrl+V, Rename F2, Delete, New folder Ctrl+Shift+N, New text file Ctrl+Shift+T, Move to new folder, Compress to zip, Extract here, Copy path Ctrl+Shift+C, Show in File Explorer Ctrl+Shift+W, Information Ctrl+I, Properties Alt+Enter, Windows menu. The same menu opens on the entry with Shift+F10, the Applications key or a right click; Ctrl+I inside the context menu says the entry.
- **Select**: Select all (Ctrl+A), Deselect all (Ctrl+Shift+A), Select or deselect this entry (Space).
- **Drives**: the drives by number, This PC, Eject this drive, Drive properties.
- **Options**: Text (font and size), General (the update check, hidden files, files starting with a period), Developer (the settings file, diagnostics).
- **Axit**: new windows of every app, the files and folders closed lately, the open windows, updates, About.
- **Help**: Keyboard shortcuts (F1), this guide, File resources.

## Settings

Everything you change is kept in `%APPDATA%\Axit\axfile.json`: the last folder, the recent folders, the favorites, the sort order, the font and text size, the update check, your custom programs (`programs`, a name and a path each), and the two switches of Options, General (`showHidden`, `showDotFiles`). Options, Developer, Open settings file opens it in AxJSON. The player's settings of earlier versions are AxMedia's now, in `axmedia.json`.

## Updates

AxFile checks for a newer Axit when it starts and offers it in a notice; the Axit menu has Check for updates at any time. One update brings every app of Axit up to date.

## If something goes wrong

AxFile's log is `%LOCALAPPDATA%\Axit\logs\axfile.log`; Options, Developer, Copy diagnostics copies its end with the versions, for a bug report. "Folder not found" means the folder is gone or the drive unplugged; AxFile shows the nearest folder above it that exists. "Failed: Access denied" means Windows does not let your account change that place.

## About AxFile

AxFile is one app of Axit, by Dr. Kyle Keane (www.kylekeane.com), MIT licence. The Axit menu's About names the version and the licence.

## Disclaimer

Delete sends entries to the Recycle Bin through Windows' own file operation, and the other operations change your files as File Explorer would; what Windows does with them is Windows' doing.
