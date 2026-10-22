# AxFile — Specification

AxFile is an app of the Axit bundle (`docs/axit/SPEC.md`): a file manager for one folder, with a player for the
sound and video files in it, for a blind developer who uses the NVDA screen reader. Specified 2026-10-18 and built as a
first draft 2026-10-19 (section 8); the document is kept in step with the code (a behaviour change updates it in the
same commit). Requirements are numbered `AF-n.m` and decisions `AF-Dn`; the bundle's rules, AX-7
(accessibility), AX-8 (keys) and AX-9 (the Axit menu), are cited by number and not repeated. In AxFile's code and
documents, "SPEC.md" means this file.

## 1. Summary

One folder in one window: a list of its entries, folders first, named with its count (the folder is the title). Enter does the
obvious thing with the selected entry: a folder opens in the list, a Markdown or text file in AxDown, a JSON file in
AxJSON, a sound or video file plays, anything else opens in the program Windows has for it. Backspace goes up. A few
operations, each one notice or one announcement: new folder, rename, delete to the Recycle Bin, copy the path, and
"Show in File Explorer" for everything else. The player (asked for as AxMedia, AF-D2) plays the file under Enter and
then the rest of the folder's media files in order, with pause, seek, next and previous, speed and the position on
demand, the keyboard's own keys for the volume; what plays is in the status bar. The window is reached from the other
windows on the folder of the file or project in hand, and sends back. It works entirely offline (bundle G6).

## 2. Goals and non-goals

- **G1 One list, native.** A list box NVDA reads item by item, a status bar, the bundle's notices; no tree, no grid,
  no icons that carry meaning (AX-7.1).
- **G2 Enter is the verb.** The type of the entry decides what Enter does; the user learns one key.
- **G3 A folder is a playlist.** An audiobook, a lecture series or an album is a folder of files in order; Enter on
  one plays it and the rest follow; the transport keys work while the user goes on browsing.
- **G4 Streamlined.** The plan's worry on record: a file tree's behaviour is endless. Version one has the operations
  named here and no more (AF-D5).
- **G5 Nothing spoken unprompted** (AX-7.3) but one thing: the name of the next file when the player moves on, which
  the user asked for by playing the folder (AF-5.4, a setting).
- **Non-goals.** Copying, moving and pasting between folders (§7, the first candidate for version two); drag and drop;
  a tree or a second pane; a properties dialog; search across folders; file type associations (Windows' own);
  playlist files (`.m3u`), tags (ID3), album art, an equalizer, shuffle, streaming addresses; the picture of a video
  beyond a plain area; remembering positions across windows (§7).

## 3. Scenarios

1. Start menu, "Axit AxFile": the last folder opens, the list focused; NVDA reads "24 items, list, 01 Chapter
   1.mp3, 1 of 24". Down twice, Enter: "Playing 03 Chapter 3.mp3". Ctrl+P: "Paused at 12:40". Ctrl+P: "Playing".
   Ctrl+Left: "12:25". Ctrl+Shift+Period: "Speed 1.25". Ctrl+Q: "Playing 03 Chapter 3.mp3, 12:52 of 45:30, speed
   1.25".
2. The chapter ends: "Playing 04 Chapter 4.mp3" and the next chapter plays; the window can be left in the background,
   Alt+Tab brings the keys back.
3. In AxClaude on a project, Alt+S, I, F: AxFile opens on the project folder. Down to `docs`, Enter: the list is the
   `docs` folder; Enter on `plan.md`: AxDown opens it. Backspace: back in the project with `docs` selected.
4. Ctrl+Shift+N, type `notes`, Enter: "Created notes", the folder selected. F2 on it, type `Notes`, Enter: "Renamed to
   Notes". Delete: "Delete Notes? It goes to the Recycle Bin with everything in it.", Enter: "Deleted Notes".
5. Ctrl+Shift+C on `report.pdf`: "Copied path"; Enter on it: the PDF opens in the program Windows has for it.
6. Ctrl+F, type `chap`, Enter: the first entry whose name holds it is selected; F3 the next.
7. Ctrl+G, type `%USERPROFILE%\Downloads`, Enter: the list is the Downloads folder.
8. Space on `a.txt`: "Selected"; Down, Space: "Selected, 2 selected"; Up: "a.txt, selected"; Ctrl+X: "Cut 2 items";
   Enter on `docs`, Ctrl+V: "Pasted 2 items". Space on one of them, Delete: "Delete a.txt?", Enter: "Deleted a.txt".

## 4. Functional requirements

### AF-1 Launch

- AF-1.1 `Axit.exe file [<folder or file>]` (a file opens its folder with the file selected); the console command
  `axfile`; the Start menu entry "Axit AxFile"; AxFile under "Open with Axit" on folders and on files in File
  Explorer (AX-2.3). A folder or file given alone to `Axit.exe` still means AxClaude or an editor (AX-1.2); AxFile is
  always named by its verb. Without a path: the last folder opened, or the user's profile folder.
- AF-1.2 `AxFileApp.Run(args)` is the entry point (AX-1.7), one call into `AppHost.Run` with the log name `axfile`,
  the usage text, the settings (`axfile.json`) and the window.
- AF-1.3 **To and from the other windows.** AxClaude's Session → Show current folder in ▸ **AxFile**, the editors'
  File → Show current file in ▸ **AxFile** (the file's folder, the file selected), AxGit's Repository → Show current
  repository in ▸ **AxFile** and AxTerminal's Session → Show current folder in ▸ **AxFile** each start a new AxFile
  window (`AxitMenu.Start`, found again in the Axit menu, AX-9.3). From AxFile, Folder → Show current folder in ▸
  **File Explorer** (Ctrl+W), **AxClaude**, **AxGit**, **AxTerminal**; Item → **Show in File Explorer**
  (Ctrl+Shift+W) opens File Explorer with the selected entry selected; Item → Open in Axit ▸ **Editor** (Ctrl+E) opens the
  selected file in AxJSON for `.json` and AxDown for anything else, whatever Enter would do with it.

### AF-2 The window

- AF-2.1 From the top: the menu bar; the video area (AF-5.6), shown only while a video plays; the list, named
  with its count alone, "N items" ("24 items"; "1 item"; "Empty" for none; "3 of 24 items, filter chap" under a
  filter, AF-3.8); the status bar. The list is the one control; a
  notice takes its place while it shows (AX-6.4). Tab has nothing to move to.
- AF-2.2 Items: a file by its name as it is; a folder as "<name>, folder"; a drive of This PC as "<label> (C:), drive"
  (AF-3.10). Folders first, then files, each group in
  Windows' natural order (`StrCmpLogicalW`, so that `2` comes before `10`). Hidden and system entries are left out,
  and so are entries whose name starts with a period (`.git`, `.env`), each behind a switch in Options → General,
  **Show hidden files and folders** (`showHidden`) and **Show files and folders starting with a period**
  (`showDotFiles`), both off; a flip reads the folder again. What was left out is counted, and the list ends with one
  line that says so, "Not shown: 3 hidden, 2 starting with a period" (one part when one kind; no line when nothing was
  left out; `FolderListing.NotShownText`, tested), so that the count is there when wanted and never spoken otherwise
  (asked 2026-10-21). The list is read on a background task (AX-7.5); a folder of ten thousand entries fills in one
  update. An empty folder shows one item, "No items in this folder", selected, so that the list is never empty and the
  reason is read (the bundle's rule for empty lists, AX-9.5). Neither that item nor the not-shown line is an entry: a
  command on either says the line itself (AF-7).
- AF-2.3 The title is `<folder name> - AxFile`. The status bar's left label: the player's state, `Playing 03 Chapter
  3.mp3 12:52 / 45:30` while it plays (the time moving once a second), `Paused …`, empty otherwise; the right label:
  the folder's full path. No AccessibleName, no Spring (AxClaude §6.2).
- AF-2.4 **Information** (Ctrl+I; Item → Information): the selected entry on demand, "report.pdf, 1.2 MB, modified
  2026-10-18 14:02" or "docs, folder, modified 2026-10-18 14:02"; the items themselves stay names (AF-D7).

### AF-3 Browsing

- AF-3.1 **Enter** on a folder opens it in the list with its first entry selected. **Backspace** and Alt+Up go to the
  parent with the folder just left selected; at a drive's root, to This PC with that drive selected (AF-3.10), and at
  This PC, "Top". NVDA speaks the new selection; the app adds nothing.
- AF-3.2 **Enter** on a file: `.md`, `.markdown` and `.txt` open in AxDown and `.json` in AxJSON, each a new window of
  that app; a media file plays (AF-5); any other file opens with the program Windows has for it (`ShellExecute`, as
  Enter in File Explorer); when Windows has none, the Windows dialog that picks a program, as File Explorer shows it
  (AF-4.7).
- AF-3.3 **Open folder…** (Ctrl+O): the Windows folder picker. **Open recent folder ▸**: the last ten folders opened
  by command, "1. name (path)" (AX-9.5): the start, Open folder, Go to folder, a recent item, and the folder the
  window closed on; Enter and Backspace browse without joining the list, so that a walk through a tree does not fill
  it. **Go to folder…** (Ctrl+G): the notice "Go to folder" with the field "Path" holding the current folder, which
  takes a path with environment variables (`%USERPROFILE%\Downloads`) and a file's path (its folder, the file
  selected); "Not found: <path>" when there is no such place. **New window** (Ctrl+N, File Explorer's key; Folder →
  New window): another AxFile window on the folder shown, through the bundle's starter (AX-9.2), so that two folders
  can be worked between; Ctrl+N is the other windows' New session, New document and New branch, and a new window of
  any app is the Axit menu's (AX-9).
- AF-3.4 **Refresh** (F5) reads the folder again keeping the selection by name; "Refreshed". Coming back to the window
  after another program changed the folder refreshes it the same way, silently (the list is a view and holds nothing
  unsaved; AxDown AD-3.6 is the editors' rule).
- AF-3.5 A folder that is gone when it is asked for (deleted, an unplugged drive) gives the notice "Folder not found"
  naming it, and the list shows the nearest parent that exists.
- AF-3.6 The list's own keys stay native: arrows, Home, End, Page Up and Page Down, a typed letter to the next name
  starting with it. **Find** (Ctrl+F, the notice "Find" with the field "Name contains") selects the first entry after
  the current one whose name contains the text, wrapping round the end once, "Not found" otherwise; F3 and Shift+F3
  the next and the previous. NVDA reads the selected entry; the app adds nothing.
- AF-3.7 **Selection** (asked 2026-10-21). The list stays a single-selection list box, where the arrow keys choose
  the current entry (AF-3.6); the selection the operations act on is a set of marks on top of it. **Space** marks the
  current entry, or unmarks it again: its item reads "<name>, selected" while it is marked ("notes.md, selected";
  "docs, folder, selected"), so that an arrow key onto it says so and nothing is said on the others; the key answers
  "Selected" or "Deselected", with ", n selected" when more than that one is marked ("Selected, 3 selected";
  "Deselected, 2 selected"). **Shift+Down**, **Shift+Up**, **Shift+Home** and **Shift+End** (asked 2026-10-21, "the
  Shift multiselect File Explorer has") move the current entry and mark every entry from where the range began to
  there: the range begins where the first Shift key finds the current entry, moving back with Shift shrinks it, and
  the marks made before it stay; an arrow without Shift ends it, as does a change of the list's order or filter or
  a read of the folder, and the next Shift key begins a new one. Each entry passed reads ", selected" by itself and
  the keys say nothing more (only the items whose mark changed are rewritten); on a line after the entries (an
  empty folder's, the not-shown line) the keys say that line. **Ctrl+A** marks every entry, "n selected";
  **Ctrl+Shift+A** unmarks them all, "None selected"; the **Select** menu of the bar holds Select all, Deselect all and the Space toggle, "Select or deselect this entry" (asked 2026-10-21; they were in the Item menu as Select all and Select none, which is one entry's menu). Ctrl+I adds ", n selected" to the entry's information while any are
  marked (AF-2.4). Cut, Copy, Delete and Copy path act on the marked entries when there are any, and on the current
  entry alone otherwise (AF-4.3, AF-4.4, AF-4.8); Enter, Rename, the ways to open and the Windows items stay on the
  current entry. The marks live in one folder: showing another folder drops them, a refresh of the same folder keeps
  those still there, and a renamed or deleted entry loses its mark. A mouse click chooses the current entry as before
  and marks nothing. The marks are text in the items rather than the list box's own multiple selection (AF-D9).
- AF-3.8 **Sort and filter** (asked 2026-10-21). **Folder → Sort by ▸** Name, Date, Size, one checked: folders first
  in every order, then by name (the natural order), by date newest first, or by size largest first, ties by name
  (`FolderListing.Sort`, tested); the entry in hand stays selected; "Sorted by date"; the order is a setting
  (`sortBy`) and holds for every folder and the next window. **Filter…** (Ctrl+Shift+F; Folder → Filter…): the notice
  "Filter" with the field "Name contains" holding the filter in force; Filter lists only the entries whose name
  contains the text, any case, and names the list "3 of 24 items, filter chap" so that the count is read with
  the list; an empty field clears it, "Filter cleared", as does **Escape** in the list while a filter is on (Escape
  does nothing otherwise). A filter that nothing passes shows the one item "No names contain <text>", and a command on
  it says the same. The filter lives in the folder: another folder drops it, a refresh keeps it. What the list shows
  is what the commands see: Select all marks the shown entries, the player's queue is the shown media files (AF-5.3),
  find moves among the shown entries. Find (AF-3.6) jumps, the filter narrows; both are kept.
- AF-3.9 **Favorites** (asked 2026-10-21). **Ctrl+D** (Folder → Favorites ▸ → Add or remove this folder) pins the
  folder shown, "Added to Favorites", or unpins it when it is one, "Removed from Favorites"; the browsers' bookmark
  key. **Folder → Favorites ▸** lists them as "1. name (path)" (AX-9.5) in the order they were added, those that
  still exist, or "No favorites yet", with the key's item under a separator; choosing one shows the folder and it
  joins the recent list. The favorites are a setting (`favorites`, AF-8.1), so every window and the next start
  have them.
- AF-3.10 **This PC** (asked 2026-10-21): the level above the drives. Backspace or Alt+Up at a drive's root shows it,
  the drive just left selected; its entries are the ready drives in letter order, "Local Disk (C:), drive" (the
  volume's label, or the kind's name, Local Disk, Removable Disk, CD Drive, Network Drive, when it has none;
  `FolderListing.Drives`, tested), each a folder entry marked as a drive; Enter opens the drive's root; Ctrl+I says
  "Local Disk (C:), 120 GB free of 476 GB"; the title and the list are named "This PC"; Backspace there, "Top". It
  is not a folder: the last folder, the recent list and the Recently closed list keep the folder before it, the sort
  order leaves the letter order alone, and the commands that need a folder say "Choose a drive first" (New folder,
  Paste, Rename, Delete, Cut, Copy, Ctrl+D, Show current folder in ▸ the other apps); Show current folder in ▸ File
  Explorer opens File Explorer's own This PC. The other items of the entry's menu work on a drive as on a folder
  (Open in Axit ▸, Show in File Explorer, Properties, Windows menu…).
- AF-3.11 **The Drives menu** (asked 2026-10-21, for moving things to a stick and taking it out): **Drives (D)** on
  the menu bar lists the ready drives as "1. Local Disk (C:)" (AX-9.5), each opening the drive's root and joining the
  recent list, then **This PC** (the drives list, AF-3.10, the folder's drive selected), then **Eject this drive**
  and **Drive properties**, both for the drive in hand: the entry at This PC, or the folder's own drive. Eject is
  the shell's own: the drive's context menu is built as for Windows menu… (AF-4.7) and its item whose verb is
  `eject` is invoked (`Shell.Eject`), so a stick, a card or a disc comes out the way File Explorer's Eject does and
  Windows says the rest (safe to remove, or in use); "Ejecting <name>", and the list goes to This PC when it was on
  that drive; "Failed: This drive has no Eject" on a fixed disk, "Failed: <reason>" when Windows refuses. Drive
  properties is the Windows sheet of the drive (space, tools). The menu is refilled each time it opens, since a stick
  may have come or gone. Without a folder, "No folder".

### AF-4 Operations

- AF-4.1 **New folder…** (Ctrl+Shift+N): the notice "New folder" with the field "Name" and the buttons Create and
  Cancel; Create makes it in the current folder, selects it and says "Created <name>". **New text file**
  (Ctrl+Shift+T; asked 2026-10-21): an empty file "New file.txt" ("New file - 2.txt" when that is taken,
  `FileOperations.CreateFile`, tested) is made in the current folder and selected, and the rename box (AF-4.2) opens
  on it at once with the whole name selected, not the stem alone, so that the name typed with its extension
  replaces it; Enter renames it, Escape keeps "New file.txt". When the box cannot open (the file hidden by the
  filter, the focus not on the list), "Created <name>" instead.
- AF-4.2 **Rename** (F2): in place, as in File Explorer: a box ("New name") opens over the entry holding its name, the
  part before the extension selected (the whole name for a folder), so that typing replaces the name and keeps the
  extension; Enter renames, "Renamed to <new name>", the entry selected under its new name; Escape, or leaving the box
  (a click elsewhere, a notice opening), keeps the name with no announcement, the list's reading of the entry being
  the feedback; the same name again, "Name unchanged". A change of case alone is a rename too. The box is a text box
  that is a child of the list, shown only while a rename runs; the first build (2026-10-19) asked in a notice. A name
  that **reads the same** as another entry's without being it, differing only in case plus a trailing or doubled
  space, a different kind of space, an invisible character (a zero-width space, a joiner, a mark) or an accent
  written as one or two characters, is refused with "Reads the same as <that name>, which is already here" and the
  box stays (`FileOperations.Lookalike`, tested; the same for New folder…, AF-4.1, and Move to new folder…,
  AF-4.13): Windows would keep both files, and the list would then read one name twice, which the user met on
  2026-10-21 and took for a rename onto an existing name. A name that is exactly taken is Windows' own refusal,
  "Failed: There is already an entry named <name>".
- AF-4.3 **Delete…** (Delete): the notice "Delete <name>? It goes to the Recycle Bin." (for a folder: "… with
  everything in it.") with the buttons Delete and Cancel; the deletion goes through the shell (`SHFileOperationW` with
  `FOF_ALLOWUNDO`, silent and without the shell's own confirmation), so that the Recycle Bin takes it; "Deleted
  <name>", the selection moves to the next entry. With entries marked (AF-3.7): "Delete n items? They go to the
  Recycle Bin." ("…, the folders with everything in them." when one is a folder), one shell operation for them all,
  "Deleted n items", and the first entry after the current one that stays is selected, else the last one before it.
  A permanent delete is not offered (§7).
- AF-4.4 **Copy path** (Ctrl+Shift+C): the full path to the clipboard, "Copied path"; with entries marked, each path
  on a line, "Copied n paths".
- AF-4.5 A failure (a name taken, a file in use, access denied) is one announcement, "Failed: <Windows' message>",
  when it is one line, a notice otherwise (AxGit AG-7). Every operation runs on a background task; the list refreshes
  after it.
- AF-4.6 **The entry's menu** (Shift+F10, the Applications key, a right click): the Item menu as the list's context
  menu, the same items in the same order, on the selected entry (a right click selects the entry under it first, as
  File Explorer does). It is the window's own menu, fixed, with mnemonics and listed in F1, not the shell's (AF-D8).
  Its shape (AF-6.1; reshaped 2026-10-21 at the user's ask for File Explorer's plain words and one flat list): Open,
  Open with ▸, Open in Axit ▸ | Cut, Copy, Paste | Rename, Delete | New folder…, New text file, Move to new folder…,
  Compress to zip, Extract here | Select all, Select none | Copy path, Show in File Explorer, Information,
  Properties | Windows menu…. The list's context menu is an `ItemMenu` (AF-4.10) and the bar's Item menu the
  standard drop-down, both filled by the same method, so that the bar's menu is an exact copy; the bar's drop-down is
  never a hand-made one (AX-6.6).
- AF-4.7 **Windows' own ways**, items of the entry's menu, each one of the system's own windows like the folder picker
  (AX-7.2): Open with ▸ **Windows default** opens the entry with the program Windows has for it (a folder in File Explorer),
  or, when Windows has none, the Windows dialog that picks a program and runs it, as File Explorer does on Enter;
  **Choose a program…** shows that dialog always ("Not a file" on a folder); **Properties** (Alt+Enter, File
  Explorer's key) shows the Windows Properties sheet of a file or a folder; **Windows menu…**, the last item,
  shows the shell's own context menu for the entry, File Explorer's classic menu with every installed program's
  entries (`IShellFolder` and `IContextMenu` through P/Invoke, tracked as a native popup the window owns, its first
  item selected, the shell's submenus such as Send to filled through the window's messages), and runs what is chosen.
  A refusal by Windows is "Failed: <reason>"; closing the dialog or the menu is not one.
- AF-4.8 **Cut, Copy, Paste** (Ctrl+X, Ctrl+C, Ctrl+V, in the list; the Item menu): the entry, or the marked
  entries (AF-3.7), go on the clipboard as files in File Explorer's own formats (`CF_HDROP` and "Preferred
  DropEffect", move for a cut, copy otherwise), "Cut <name>" or "Copied <name>", "Cut n items" or "Copied n items",
  so that File Explorer can paste them and AxFile can paste what File Explorer cut or copied. Paste copies the clipboard's files into the folder shown, or moves them after a cut, through
  the shell (`SHFileOperationW`, undo allowed; the names are chosen first, `FileOperations.Destinations`, a name already there becoming "name - 2", "name - 3", one destination per source with `FOF_MULTIDESTFILES`, rather than a question, nothing
  overwritten; `FileOperations.CopyOrMoveAsync`, tested): "Pasted <name>" or "Pasted n items", the first one selected;
  "Pasting, please wait" when it takes longer than a second; after a move the clipboard is cleared, as File Explorer
  does. "Nothing to paste" without files on the clipboard; "Already in this folder" for a cut entry pasted where it
  is.
- AF-4.9 **Open in Axit ▸**: one item per app of the bundle, from `Dispatch.Apps`, a new window of that app on the
  entry: AxDown and AxJSON on the file ("Not a file" on a folder), AxGit on the repository that holds the entry, AxFile
  on the folder (or the file in its folder), AxClaude and AxTerminal on the folder, or on the entry's folder for a
  file; Editor (Ctrl+E), the submenu's first item, is the quick way to the right editor.
- AF-4.10 **Ctrl+I inside the open context menu** says the entry's information (AF-7) without closing the menu, so
  that the entry a menu is about can be checked before choosing. WinForms routes the keys of an open menu to the
  menu, not to the window, so `ItemMenu`, the context menu, takes the key itself. The bar's Item menu is the standard
  drop-down (AX-6.6) and does not have it, nor does the Windows menu, which is Windows' own.
- AF-4.11 **The user's own programs in Open with ▸** (asked 2026-10-21): a list of programs and scripts, kept in the
  settings (`programs`, a name and a path each, AF-8.1), shown under Windows default and Choose a program… (AF-4.7)
  as "1. name (path)" (AX-9.5), or "No programs of your own yet" while the list is empty, then **Add a program…**
  and **Remove a program ▸**. Add a program… opens the
  Windows file picker on programs and scripts (`.exe`, `.cmd`, `.bat`, `.ps1`, `.py`; every file behind the second
  filter), then the notice "Add a program" with the field "Name" holding the file's name without its extension and
  the buttons Add and Cancel; "Added <name>". Remove a program ▸ lists them by name; choosing one takes it off,
  "Removed <name>". Choosing a program runs it once on the selected entries, or the current one (AF-3.7), every path
  an argument, in the first entry's folder: a `.ps1` through `powershell.exe -NoProfile -ExecutionPolicy Bypass
  -File <script>`, since Windows opens a script file in an editor; anything else as the shell starts it
  (`ShellExecute` with the arguments, so a batch file and a script whose type has a program registered run too);
  "Started <name>", or "Failed: <reason>" when it cannot start (the file gone, no program for the type). The list is
  refilled each time the submenu opens, so two windows see each other's additions after a restart only (the settings
  are the window's, AF-8.1). It is the shape the user-defined shells of AxTerminal are to take (docs/axit/plan.md):
  a list in the app's settings, added through the file picker and a notice for the name, removed from a submenu.
  `UserPrograms` in `AxFile.Core` is tested; the menu and the start are the window's.
- AF-4.12 **Compress to zip, Extract here** (the Item menu; asked 2026-10-21), through .NET's own `ZipFile`, no
  package (`Archives` in `AxFile.Core`, tested). **Compress to zip** puts the selected entries, or the current one
  (AF-3.7), into a new zip in the folder shown, named after the one entry without its extension ("notes.zip") or
  after the folder for several ("Dune.zip"), "name - 2.zip" when the name is taken, a folder's contents under its
  name; "Compressed to <name>", the zip selected; "Compressing, please wait" when it passes a second. **Extract
  here** on a `.zip` makes a new folder named after it next to it ("Dune", or "Dune - 2" when taken) and extracts
  into it, so that nothing is written over; "Extracted to <name>", the folder selected; "Not a zip file" on anything
  else. A failure is "Failed: <reason>" (AF-4.5). Other archive kinds are the shell's (Windows menu…) or an
  installed program's (AF-4.11).
- AF-4.13 **Move to new folder…** (the Item menu; asked 2026-10-21, "like zip but not compressed"): the notice
  "Move to new folder" with the field "Name" and the buttons Move and Cancel, its text naming what moves ("a.txt
  will move into the new folder", "3 items will move into the new folder"); Move makes the folder in the current
  folder (a taken name is "Failed: There is already an entry named …", as New folder) and moves the selected entries,
  or the current one (AF-3.7), into it through the shell (AF-4.8, undo allowed, names chosen first); "Moved <name>
  into <folder>" or "Moved n items into <folder>", the new folder selected.
- AF-4.14 **Undo** (asked 2026-10-21; specified and built the same night, after the pane, AF-9, whose operation
  lines show the same operations). **Ctrl+Z** (Folder → Undo <operation>, named after what it would undo, "Undo rename") takes back the
  last operation of this window that can be taken back, one step per press, and selects the entry it touched:
  Rename, renamed back; New folder and New text file, deleted while still empty ("Not undone: notes has files
  now" otherwise); a paste that moved and Move to new folder, moved back where they were, the new folder removed
  when it is empty; a paste that copied, Compress to zip and Extract here, the copies, the zip or the extracted
  folder sent to the Recycle Bin; Delete, brought back from the Recycle Bin, the shell's own undelete on the Recycle
  Bin's item, the one step that needs the shell again (the bin's items are matched by the original path their
  `$I` record holds, the latest deletion of each, and restored in one Restore command; "Not in the Recycle Bin any
  more" when none is there). "Undone: <what>" each time ("Undone: renamed back to <name>", "Undone: <name> removed",
  "Undone: moved back", "Undone: <what> sent to the Recycle Bin", "Undone: <what> restored"); an entry changed since
  by anything else (renamed in File Explorer meanwhile), "Not undone: <name> has changed", "Not undone: <name> has
  files now" / "has content now" (a new folder or file filled meanwhile), "Not undone: <name> is already here" (the
  place it would go back to is taken; a rename that changed only the case is not "already here"), and the
  operation is dropped from the history either way; a failure of the undo itself is "Failed: <reason>" (AF-4.5)
  and the operation stays to try again (a zip open in another program, closed and Ctrl+Z again); a second Ctrl+Z
  while one runs says "Undo is still running"; in the rename box Ctrl+Z is the box's own (AF-6.2). Eject, Open, the player, Favorites and the settings are not operations
  and have no undo; there is no redo. The history is this window's, the last twenty operations, gone with the
  window. The history and each operation's inverse are `OperationHistory`, `Operation` and `FileOperations.CheckUndo`
  and `UndoAsync` in `AxFile.Core`, tested over a temporary folder; the Recycle Bin's records are `RecycleBin`, tested
  on synthetic records; the Restore itself is `Shell.Undelete`, checked by a console run on this machine.

### AF-5 The player

- AF-5.1 **Media files**, by extension (`MediaFiles.Extensions`, tested): sound `.mp3`, `.wav`, `.wma`, `.m4a`,
  `.m4b`, `.aac`, `.flac`, `.ogg`, `.opus`, `.aiff`; video `.mp4`, `.m4v`, `.mov`, `.avi`, `.wmv`, `.mkv`, `.webm`.
  Whether Windows plays one depends on its decoders (the Store's extensions for some); a file that cannot be opened
  says "Cannot play <name>: <reason>" and the player moves to the next.
- AF-5.2 **Engine**: Windows' own media stack through one P/Invoke seam, `MediaPlayer` in `AxFile.Core`, no package
  (B-10): Media Foundation through MFPlay (`MFPCreateMediaPlayer`, `IMFPMediaPlayer` and `IMFPMediaItem` of
  `mfplay.dll`, COM through P/Invoke; AF-D3). The file is opened synchronously, so that one that cannot be opened
  says so at once; playing starts when the engine reports the item set; its events (started, ended, failed) come back
  on the window's thread. A rate other than 1 is stretched in time by Media Foundation's audio renderer, which keeps
  the pitch, the user's criterion (an audiobook at 1.5 with the voice unchanged; heard in the test plan, §4.3). A
  rate the engine refuses: "Speed n is not supported here", and the speed stays.
- AF-5.3 **Play** (Enter on a media file; Player → Play): plays it; "Playing <name>". When it ends, the next media
  file of the same folder, in the list's order, plays, to the end of the folder: "End of folder" with the done chime
  (the shared `Sounds`, AxClaude FR-7.3). The queue is the folder's media files as they were when Enter was pressed,
  so browsing elsewhere in the window changes nothing (AF-D6). Enter on another media file while one plays switches
  to it.
- AF-5.4 **Keys**, from anywhere in the window, whatever folder the list shows: **Ctrl+P** pauses or resumes ("Paused
  at 12:40" / "Playing"; Space until 2026-10-21, when Space became the selection's key, AF-3.7); **Ctrl+Right** and **Ctrl+Left** move 15 seconds forward or back (`seekSeconds`), announcing the new
  position ("12:25"); **Ctrl+Shift+Right** and **Ctrl+Shift+Left** the next and the previous file of the queue
  ("Playing <name>"; "First file" and "End of folder" at the ends); **Ctrl+Shift+Period** and **Ctrl+Shift+Comma**
  faster and slower by 0.25 between 0.5 and 3 ("Speed 1.25"), Player → Normal speed back to 1 ("Speed 1"), the
  speed kept in the settings (`speed`) for the next file and the next window; **Ctrl+T** Go to time…, the notice with
  the field "Time" (`12:40`, `1:02:10`, or `750` for seconds), "12:40" when it is done; **Ctrl+Q** What is playing:
  "Playing <name>, 12:52 of 45:30, speed 1.25", "Paused …" or "Nothing playing". "Say each file as it starts"
  (`sayEachFile`, default on; the Player menu) is the one unprompted announcement (G5). The volume is Windows' own:
  the keyboard's volume keys work from anywhere and the app has none (AF-D4).
- AF-5.5 Closing the window stops the player. Stop as a command is not offered: Ctrl+P pauses, and the next Enter
  plays something else (§7).
- AF-5.6 A video's picture is drawn in an area at the top of the window (`Video`, not focusable; never a window of its
  own, AX-7.2), its sound through the same path; the area is hidden while nothing plays or sound plays.
- AF-5.7 The time format is `m:ss` under an hour and `h:mm:ss` over (`MediaFiles.FormatTime`, tested).

### AF-6 Menus, keys, texts

- AF-6.1 Menus, in the standard order of a first menu (AX-6): **Folder (F):** New window Ctrl+N | Open folder… Ctrl+O |
  Open recent folder ▸ | Favorites ▸ (1. name (path) … or No favorites yet | ─ | Add or remove this folder Ctrl+D) |
  Go to folder… Ctrl+G | ─ | Up one level Backspace | Refresh F5 | Undo <operation> Ctrl+Z (AF-4.14) | ─ | Sort by ▸ (Name ✓ |
  Date | Size) | Filter… Ctrl+Shift+F | ─ | Show current folder in ▸ (File Explorer
  Ctrl+W | AxClaude | AxGit | AxTerminal) | ─ | Exit Alt+F4. **Item (I):** Open Enter | Open with ▸ (Windows default |
  Choose a program… | ─ | 1. name (path) … or No programs of your own yet | Add a program… | Remove a program ▸) |
  Open in Axit ▸ (Editor Ctrl+E | ─ | AxClaude | AxDown | AxJSON | AxGit | AxTerminal | AxFile) | Preview
  Ctrl+Shift+P (AF-9.4) | ─ | Cut Ctrl+X |
  Copy Ctrl+C | Paste Ctrl+V | ─ | Rename F2 | Delete Delete | ─ | New folder… Ctrl+Shift+N | New text file
  Ctrl+Shift+T | Move to new folder… | Compress to zip | Extract here | ─ | Copy path Ctrl+Shift+C | Show in File
  Explorer Ctrl+Shift+W | Information Ctrl+I | Properties
  Alt+Enter | ─ | Windows menu…; the same menu is the list's context menu (AF-4.6). **Select (S):** Select all
  Ctrl+A | Deselect all Ctrl+Shift+A | ─ | Select or deselect this entry Space (AF-3.7). **Drives (D):** 1. Local Disk
  (C:) … | ─ | This PC | ─ | Eject this drive | Drive properties (AF-3.11). **Player (P):** Play Enter | Play folder
  Ctrl+Enter | Add to queue Ctrl+Shift+Q | Clear queue | Show queue (AF-9.2) | ─ | Pause or resume Ctrl+P | Next file Ctrl+Shift+Right | Previous
  file Ctrl+Shift+Left | Forward 15 seconds Ctrl+Right | Back 15 seconds Ctrl+Left | Go to time… Ctrl+T | ─ | Faster
  Ctrl+Shift+Period | Slower Ctrl+Shift+Comma | Normal speed | ─ | Say each file as it starts ✓ | What is playing
  Ctrl+Q. **Options (O):** Text ▸ (Font… | Larger text Ctrl+Plus | Smaller text Ctrl+Minus | Windows text size) |
  General ▸ (Check for updates at start ✓ | Show hidden files and folders | Show files and folders starting with a
  period) | Developer ▸ (Open settings file | Copy diagnostics). **Axit (A)**
  (AX-9). **Help (H):** Keyboard shortcuts F1 | User guide | ─ | File resources ▸ (Keyboard shortcuts in Windows, the
  Microsoft page, in the browser). Every item has a mnemonic (AX-7.4).
- AF-6.2 Keys (AX-8). Bundle-wide: F1, the control keys (Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6, between
  the list and the pane, AF-9.1), Ctrl+F, F3, Shift+F3, Ctrl+S ("Nothing to save here"),
  Ctrl+Plus, Ctrl+Minus, Page Up and Page Down (the list's own here), Ctrl+Up and Ctrl+Down with Shift (edit controls
  only; the list leaves them alone), Alt+F4. Window-specific (`AxFileKeys`): Enter, Backspace, Alt+Up, F5, F2, Delete,
  Ctrl+N, Ctrl+O, Ctrl+G, Ctrl+D, Ctrl+W, Ctrl+Shift+W, Ctrl+E, Ctrl+I, Ctrl+Shift+N, Ctrl+Shift+T, Ctrl+Shift+C,
  Ctrl+Shift+F, Escape, Shift+Up, Shift+Down, Shift+Home, Shift+End,
  Space, Ctrl+A, Ctrl+Shift+A, Ctrl+P, Ctrl+Enter, Ctrl+Shift+Q (AF-9.2), Ctrl+Shift+P and, in the preview, Ctrl+H, Ctrl+K, Ctrl+L with Shift and l (AF-9.4), Ctrl+Right, Ctrl+Left, Ctrl+Shift+Right, Ctrl+Shift+Left, Ctrl+T,
  Ctrl+Shift+Period, Ctrl+Shift+Comma, Ctrl+Q, Shift+F10, the Applications key, Alt+Enter, Ctrl+Z (AF-4.14), Ctrl+X, Ctrl+C, Ctrl+V.
  Enter, Backspace, Space, Escape, F2, Delete, Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A, Ctrl+Shift+A, Ctrl+Enter, Ctrl+Shift+Q, the Shift arrows, Shift+F10 and the
  Applications key act only while the list has the focus (in the pane, Enter, Delete, Escape and Ctrl+I act on its line, AF-9.6; in a notice's field or the rename box they are the field's;
  the two menu keys are the list's own, which shows its context menu itself, so the key switch has no case for them;
  Ctrl+I has a second handler, the open menu's, AF-4.10). The collision test covers the table and lists the keys other
  windows use otherwise (Ctrl+N, Ctrl+G, Ctrl+E, Ctrl+T, Ctrl+P, Ctrl+C, Ctrl+Shift+C, Ctrl+Shift+F).
- AF-6.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axfile/user-guide.md` is embedded (Help → User
  guide); About is the bundle's (AX-9.9).

### AF-7 Announcements

One UI Automation notification each, on the list, or on the notice while one shows (AX-7.3): "Playing <name>",
"Paused at m:ss", "Playing" (resumed), "m:ss" after a seek or Go to time, "Speed n", "First file", "End of folder",
"Nothing playing", "Playing <name>, m:ss of m:ss, speed n", "Cannot play <name>: <reason>", "Created <name>", "Moved
<what> into <name>" (AF-4.13), "Renamed
to <name>", "Deleted <name>", "Deleted n items", "Copied path", "Copied n paths", "Cut <name>", "Copied <name>", "Cut n
items", "Copied n items", "Selected", "Deselected", "Selected, n selected", "Deselected, n selected", "n selected",
"None selected" (AF-3.7), "Sorted by <order>", "Filter cleared" (AF-3.8), "Added to Favorites", "Removed from
Favorites" (AF-3.9), "Top" (This PC, AF-3.10), "Ejecting <name>" (AF-3.11), "Added <name>", "Removed <name>", "Started <name>" (AF-4.11),
"Compressed to <name>", "Extracted to <name>", "Compressing, please wait", "Extracting, please wait" (AF-4.12), "Pasted
<name>", "Pasted n items",
"Pasting, please wait" (a paste longer than a second), "Added n to the queue", "Queue cleared", "Removed <name>" (a
queue line), "<name>, n of m" and "<name>, n of m, playing, m:ss of m:ss" (Ctrl+I in the queue), "No sound or video
files here", "No sound or video files to add" (AF-9.2), "Not a text file", "Line n of m", "<kind>, …" and "No next <kind>" / "No previous <kind>" (the preview, AF-9.4), "Undone: …", "Not undone: …", "Nothing to undo", "Undo is still running" (AF-4.14), "Refreshed", "Not found: <path>", "No program opens
.xyz. Ctrl+E opens it as text", "Failed: <message>", "<name>, <size>, modified <date>" (Ctrl+I), "Not found" (find),
"Nothing to save here", "Text size n", "Windows text size", "Copied diagnostics", "<name>, <size>, modified <date>, n
selected" (Ctrl+I with marks); and the guards "No folder" (nothing
listed yet), "No items in this folder" (a command on an empty folder's one item, AF-2.2), "No names contain <text>"
(the same under a filter, AF-3.8), "Not shown: …" (the same on the list's last line, AF-2.2), "Nothing selected", "Choose a drive first" (a folder command at This PC, AF-3.10), "Not a file" (Ctrl+E or Open with… on a folder), "Not a zip file" (Extract here, AF-4.12), "Not a sound or video file"
(Player → Play on another entry), "Not a time" (Go to time), "Speed n is not supported here", "Name unchanged" (a
rename to the same name), "Nothing to paste" (no files on the clipboard), "Already in this folder" (a cut entry
pasted where it is), "Reads the same as <name>, which is already here" (AF-4.2), "Type a name" and the
other reasons of `FileOperations.CheckName`. A new selection is NVDA's own reading.

### AF-8 Settings, log, status

- AF-8.1 `%APPDATA%\Axit\axfile.json` (AX-5.3; `FileSettings` on `WindowSettings`): `lastFolder`, `recentFolders`
  (ten), `favorites` (the pinned folders in the order added, AF-3.9; blanks and repeats dropped), `showHidden` off,
  `showDotFiles` off (AF-2.2), `sortBy` name (`name`, `date` or `size`, AF-3.8), `sayEachFile` on, `speed` 1,
  `seekSeconds` 15, `programs` (the user's own, each a `name` and a `path`, AF-4.11; an entry without either is
  dropped), and the window base's font, update check and window fields.
- AF-8.2 The log is `%LOCALAPPDATA%\Axit\logs\axfile.log`: the start, the exit, the errors of the operations and the
  player with Windows' message; no names of files or folders.

### AF-9 The pane (specified 2026-10-21, built the same day)

- AF-9.1 **What it is.** A second control under the folder list, holding one thing at a time and named after it:
  the player's queue, the operations' status, or a file's text to read. It appears when it first has something to
  show and stays for the window's life; **Ctrl+Tab**, **F6** and Tab move between the folder list and the pane
  (AX-8.2), the way AxClaude's conversation and message field are moved between. One control, not tabs: NVDA reads a
  list and its place in it, or a text and its line; a tab control would be a third thing to learn (AF-D1). Each
  command's result is one announcement (AF-7) on the control that has the focus; nothing is spoken on an arrow key
  (AF-D7). The pane takes a third of the window at most; the folder list keeps the rest; the status bar is unchanged.
  **Escape** in the pane goes back to the folder list. Once shown, an empty pane holds one line, "Queue is empty"
  (AX-9.5), rather than vanishing from under the keys.
- AF-9.2 **The queue.** A list named "Queue": the queued media files in order, "03 Chapter 3.mp3", the one playing
  marked "(playing)", the ones played left in place until the queue is cleared. **Play folder** (Player → Play
  folder, Ctrl+Enter in the folder list) replaces the queue with the folder's media files and plays from the first;
  Enter on a media file keeps today's meaning, the folder's files from that one (AF-5.3). **Add to queue** (Player
  → Add to queue, Ctrl+Shift+Q in the folder list) appends the entry, the marked entries (AF-3.7), or a folder's
  media files in the list's order, "Added 12 to the queue"; nothing starts playing if nothing was. **Clear queue**
  (Player → Clear queue) empties it and stops, "Queue cleared"; **Show queue** (Player → Show queue) shows the pane
  with the queue in it and puts the focus there. In the queue: **Enter** plays that item (a status line is read
  again), **Delete** takes the line out ("Removed <name>"; the one playing goes on to the next, or stops at the end;
  a status line goes the same way), **Ctrl+I** says its name and place, "03 Chapter 3.mp3, 3 of 12", with the
  position while it is the one playing ("…, playing, 12:52 of 45:30"; another file's length would need opening it,
  a later step), **Ctrl+Q** what is playing. Ctrl+Shift+Right and Ctrl+Shift+Left move through the queue as now
  (AF-5.4). The queue is kept across folders (AF-D6) and goes with the window; keeping it for the next start is an
  open question with the resume position (§7). Reordering by key is left out of the first version. "No sound or
  video files here" (Play folder on a folder without any), "No sound or video files to add" (Add to queue the same).
- AF-9.3 **The operations' status.** The same list: an operation that runs on a task (a paste, a move to a new
  folder, a compress, an extract, a delete of several) adds a line under the queue's items when it starts, "Copying 3
  items", and replaces it when it ends, "Copied 3 items" or "Failed: <reason>"; the announcements of AF-7 stay as
  they are, the pane is where they can be read again and a slow operation watched. A compress and an extract count
  their files, "Compressing, 40 of 120 files" (a few times a second at most); a paste through the shell has no
  count to give (`SHFileOperationW` is silent) and shows "Copying 3 items" or "Moving 3 items" until it ends; a
  count for it needs the shell's newer file operation interface with its progress sink, a later step. The last
  twenty lines stay; Clear queue clears them too; nothing of this is kept across windows or starts.
- AF-9.4 **A file to read.** **Preview** (Item → Preview, Ctrl+Shift+P on a text file: the types AxDown and AxJSON
  open, and any file whose first bytes are text, up to 1 MB) shows the file's text in the pane as a read-only text
  named "<name>, preview", the focus on its first line, and the player's list comes back with the next queue
  command or Player → Show queue (a status line arriving meanwhile leaves the preview in place). The text has what
  every read-only text of the bundle has (AX-7.2): Ctrl+F, F3 and
  Shift+F3, the page keys, Ctrl+Up and Ctrl+Down by paragraph, `l` for the line number ("Line 12 of 40"), and for a
  Markdown file the heading, link and list keys of AxDown (Ctrl+H, Ctrl+K, Ctrl+L, with Shift for the previous one)
  through the bundle's `TextJumps`; the list item, block quote and table keys are not there, their letters being
  Information, What is playing and Go to time in this window; Ctrl+A and Ctrl+C copy. Nothing is edited
  there: Ctrl+E opens the file in its editor as before. A file that is not text ("text": the editors' kinds by
  extension, an empty file, a UTF-16 mark, or no zero byte and few control characters in the first eight
  kilobytes; `Preview` in `AxFile.Core`, tested), "Not a text file"; one over the limit shows its first 1 MB and
  says so in its name (", first megabyte"); a file cut inside a multi-byte character loses that character rather
  than its encoding. The folder list keeps the focus's place, so Escape returns to the entry.
- AF-9.5 **Later uses**, noted so that the pane's shape allows them, each a list or a text named after itself: the
  matches of a search through a folder's tree, "Search results, 12", Enter going to the file (a find across folders
  does not exist today); a folder's summary on Ctrl+I over a folder, its counts and size, which take a walk of the
  tree; a `.m3u` or `.pls` file loaded as the queue (§7). None is in the first build.
- AF-9.6 **Keys new to the table** (AF-6.2): Ctrl+Enter (Play folder), Ctrl+Shift+Q (Add to queue), Ctrl+Shift+P
  (Preview), and in the preview Ctrl+H, Ctrl+K, Ctrl+L with Shift and `l`; Ctrl+Tab, F6 and Tab are the bundle's; in the pane, Enter, Delete, Escape, Ctrl+I and Ctrl+Q act on the
  pane's item, and the list's own keys (AF-3.6) stay native.

## 5. Architecture

```
src/AxFile.Core/    class library (net10.0): FolderListing (a folder's entries as records, folders first in natural
                    order, hidden left out; what Enter does with an entry, by its extension; the list's name, the
                    information line, the parent, a typed path, the drives of This PC), NaturalOrder (StrCmpLogicalW), MediaFiles (the
                    extensions, the queue from a listing, the time format and the typed time, the speed steps),
                    MediaPlayer (MFPlay through P/Invoke: open, play, pause, position, length, speed, the started,
                    ended and failed events, a window handle for video; AF-5.2), FileOperations (the name check, new
                    folder, rename, delete to the Recycle Bin, copy and move into a folder, the last three through
                    SHFileOperationW, the messages), UserPrograms and UserProgram (the user's own programs: the
                    default name, how each kind is started on the entries; AF-4.11), Archives (a zip from entries, a
                    zip into a new folder, free names; AF-4.12), FileSettings;
                    references Axit.Core
src/AxFile/         class library (net10.0-windows): AxFileApp (Run, Usage), FileForm (the window, on AxitForm, in
                    four partial files: the controls, menus, hooks and keys; Browse; Operations; Player), AxFileKeys,
                    HelpText (F1 text, the embedded guide), ItemMenu (the entry's menu, a context menu that takes
                    the Information key while open; AF-4.6, AF-4.10), Shell (Windows' own ways with an entry: the
                    Open with dialog, the Properties sheet, the shell's context menu through IShellFolder and
                    IContextMenu; AF-4.7); references AxFile.Core, Axit.Core, Axit.Forms
tests/AxFile.Tests/ FolderListingTests and FileOperationsTests over temporary folders (the Recycle Bin is not
                    exercised), MediaFilesTests (extensions, queue order, time format, speed steps), FileSettingsTests;
                    the player itself is checked by ear
docs/axfile/        this spec, user-guide.md (embedded), nvda-test-plan.md
```

`FileForm` derives from the window base (AX-6.5): the notices, the fonts, the window's place and the update flow are
the base's (find is the window's own, over the list). The player is driven from the UI thread and its events are
posted back to it; the folder reads and the operations run on background tasks, the shell's delete on a
single-threaded-apartment thread of its own (AX-7.5).

## 6. Design decisions

- **AF-D1 One list, no tree.** A tree view asks a screen reader user to track depth and expansion; a list of one
  folder with Backspace for up is what File Explorer's main pane is, and NVDA reads a list box best. The path in the
  status bar is the depth.
- **AF-D2 The player is AxFile's feature, not an app.** The ask was a player for everything in a folder; a folder's
  files in order are a playlist, and the list is already here. A separate AxMedia would have been this list again with
  fewer verbs and a second window for the same folder; the Axit menu names an AxFile window by its folder, which is
  how a playlist is named anyway. The player's keys are with Ctrl and Shift, so that the list's own letters and arrows
  stay what they are.
- **AF-D3 Windows' own decoders through one seam: MFPlay.** No codec ships with the bundle (B-10); Windows decodes
  what the user's other players decode. The first build (2026-10-19) chose Media Foundation's MFPlay over MCI without
  measuring MCI: MCI's digital video device plays through DirectShow, whose documentation says a changed rate changes
  the pitch of audio, and DirectShow has no decoder for FLAC, AAC in MP4 or Opus, whereas Media Foundation's audio
  renderer is documented to adjust the pitch at a changed rate and decodes those formats. MFPlay is the plainest way
  in (one function, two interfaces, the events on the creating thread), and `mfplay.dll` ships with Windows 11; its
  documentation calls it superseded by `IMFMediaEngine` and the WinRT `MediaPlayer`, so the Media Engine, the same
  pipeline with more plumbing, is the fallback should a Windows drop it. The user's NVDA pass of 2026-10-21 (test
  plan §4) played MP3 chapters and an MP4 video on his machine through this engine, with the voice's pitch kept at
  1.5 and the other speeds; the other formats of AF-5.1 are not yet heard.
- **AF-D4 Volume is Windows'.** The keyboard's volume keys change the system volume from any window and are read by
  NVDA; two more keys in the app would duplicate them, and the bundle's Ctrl+Up and Ctrl+Down are the paragraph keys
  (AX-8.2).
- **AF-D5 Streamlined operations.** New folder, rename, delete and copy the path were the first four; cut, copy and
  paste came at the user's ask on 2026-10-20, as files on the clipboard in File Explorer's own formats (`CF_HDROP`
  and the preferred drop effect) so that the two exchange them, with the shell doing the copy or the move (undo from
  File Explorer, a dash and a number on a name already there rather than a question, §7). File Explorer stays one key away for
  the rest (Ctrl+W, Ctrl+Shift+W).
- **AF-D6 The queue is the folder at the moment of Enter.** A playlist that changed under the listener because the
  window browsed elsewhere would be a surprise; the queue is fixed when play starts and the list is free to roam.
- **AF-D7 Items are names; details on demand.** "report.pdf, 1.2 MB, 2026-10-18" on every arrow key is noise for a
  user who wants the name; Ctrl+I says the rest when asked (AX-7.3).
- **AF-D8 Our own context menu, Windows' behind one item.** Asked 2026-10-20: the shell's context menu on every entry,
  or a menu of our own. The shell's menu is built by every installed program's shell extension, loaded into this
  process: a slow or broken one hangs or crashes the window, and its contents and order change with what is
  installed and with Windows updates, under the user's fingers. The window's own Item menu is fixed, has mnemonics,
  is listed in F1 and is covered by the collision test, so it is the context menu (AF-4.6), and the two things people
  reach for in the shell's menu, Open with and Properties, are items of it (AF-4.7). The whole shell menu stays one
  explicit item away, Windows menu…, for the rest (Send to, an archiver's entries), chosen knowingly.
- **AF-D9 Marks in the items, not the list box's multiple selection.** Asked 2026-10-21: select and deselect with
  Space. A multiple-selection list box (`LBS_MULTIPLESEL`) does that natively, but its arrow keys move a caret the
  app has to read and set through messages for everything "current" (Enter, F2, the folder's first entry, find), a
  screen reader may add "not selected" to every unmarked entry, and whether it speaks the toggle at all was not
  known. The marks are kept by the window and written into the items (", selected"), which NVDA reads as part of
  the name, so that the single-selection list the NVDA pass covered stays as it was and the feedback is one
  announcement of the app's own; a sighted helper sees the word. Space was the player's pause key; pause moved to
  Ctrl+P, from anywhere in the window, since Space in a list is selection in every file manager.

## 7. Open questions

- Remembering the position in a file across windows (an audiobook resumed where it stopped): per file in the
  settings, with a cap; the first candidate for version two.
- A question on a name collision when pasting (replace, keep both, skip), where AxFile now adds a dash and a number
  (AF-4.8); a paste of many items that names them.
- Shift+Delete, a permanent delete, after its own question.
- A "media only" filter of the list (the name filter and the sort orders came 2026-10-21, AF-3.8).
- Whether a `.m3u` file in the folder should be the queue when there is one; with the queue pane (AF-9), Enter on a
  `.m3u` could load it into the pane.
- Whether the queue (AF-9) is kept for the next start, with the resume position above.

## 8. Status

- Planned 2026-10-18 at the user's ask (as AxMedia, then as AxFile's player, AF-D2).
- Built 2026-10-19 as a first draft, on the branch `axfile`: the window, the four operations, the player on MFPlay,
  the wiring into the bundle (the dispatcher, the installer's table, the icon, the other windows' submenus), the tests
  of the pure code (77), the guide and the test plan. Not yet checked with NVDA; the player not yet heard by the user;
  the window and the player exercised by an off-screen probe only.
- 2026-10-20, on the same branch: the entry's menu (AF-4.6) and Windows' own ways, Open with…, Properties and More
  from Windows… (AF-4.7, AF-D8), at the user's ask. The shell's menu was built for a file by the probe and its items
  read; showing it, the dialogs and the sheet wait for the NVDA pass (test plan §3). Then, the same day and ask: File
  operations ▸ with cut, copy and paste (AF-4.8), the rename in place (AF-4.2), Open in Axit ▸ (AF-4.9), Open with
  Windows and Enter's fallback to the Open with dialog (AF-3.2, AF-4.7), and Ctrl+I inside the open menu (AF-4.10);
  the rename and the clipboard round trip probed off screen; the key inside the menu waits for the NVDA pass.
- Released as 3.1.0 on 2026-10-21, and the user's NVDA pass of the whole test plan passed the same day: the list and
  the ways round, the windows out and back, the operations, the entry's menu with the rename box, the clipboard, Ctrl+I
  inside the menu, the Windows dialogs and menu, the player (MP3, MP4, the speeds with the pitch kept), the texts and
  the notices. AxFile is no longer unchecked; it stays a first draft in scope (§7).
- 2026-10-21, after the release, on the branch `axfile`, at the user's ask: the one item of an empty folder (AF-2.2),
  Ctrl+N for a new window (AF-3.3), the selection with Space and the operations on it (AF-3.7, AF-D9; pause moved to
  Ctrl+P), the user's own programs in Open with ▸ (AF-4.11), and, chosen by the user from a
  list of what a file browser makes easy, Sort by and the filter (AF-3.8), Favorites (AF-3.9), This PC (AF-3.10)
  and Compress to zip with Extract here (AF-4.12); then, from one more message: the list named by its count alone
  (AF-2.1), the two switches of Options → General with the not-shown line (AF-2.2), "name - 2" on a pasted or zipped
  name already there (AF-4.8), New text file into the rename box (AF-4.1) and Move to new folder… (AF-4.13); then the entry's menu reshaped as one flat list in File Explorer's words and the bar's Item menu made a standard drop-down (AF-4.6, AX-6.6), at the user's ask after Left and Right opened the menus; and the Drives menu with the shell's Eject (AF-3.11). Not
  yet checked with NVDA.
- 2026-10-21, late: the pane specified at the user's ask (AF-9): a second control under the folder list, one thing at a time, for
  the player's queue with Play folder and Add to queue, the operations' status lines, and a read-only preview of a text file, with
  later uses noted; to be built next, after the user has read it.
- 2026-10-21, late: Undo specified at the user's ask (AF-4.14), Ctrl+Z over the pane's history of operations, each with its
  inverse; to be built with the pane. The Shift range (AF-3.7) and the lookalike guard (AF-4.2) were built the same night.
- 2026-10-21, late: the pane built (AF-9.1 to AF-9.3): the queue list under the folder list (`PlayQueue` in
  `AxFile.Core`, tested; `FileForm.Pane.cs`), Play folder, Add to queue, Clear queue, Show queue, the queue's own
  keys, the operations' status lines with the zip counts; the preview (AF-9.4) is next; none of it checked with NVDA
  yet (test plan section 6).
- 2026-10-21, late: Undo built (AF-4.14): Ctrl+Z and Folder → Undo <operation> over the window's last twenty operations,
  each with its inverse in `AxFile.Core` (tested) and a delete restored through the shell's Restore on the Recycle
  Bin's item (`Shell.Undelete`, checked on this machine); not yet checked with NVDA (test plan 3.14).
- 2026-10-21, late: the preview built (AF-9.4): Item → Preview, Ctrl+Shift+P, a read-only text in the pane with
  the bundle's reading keys and three of AxDown's Markdown jumps; `Preview` in `AxFile.Core`, tested; not yet checked
  with NVDA (test plan 6.8). Released in 3.2.2 with the rest of the night's work.
- 2026-10-22: the split decided and specified (section 9; `docs/axit/axfile-split.md`): the player becomes AxMedia,
  the preview becomes AxPreview, the pane becomes the Actions list; sections 1 to 8 describe the code as it is until
  the build rewrites them.

## 9. The planned shape (decided 2026-10-22, not yet built)

The user's direction: AxFile focuses on file operations and management only; playing is AxMedia's
(`docs/axmedia/SPEC.md`), viewing and previewing are AxPreview's (`docs/axpreview/SPEC.md`), each an app of its own
with a tight hand-over from here. This section is what AF-1, AF-2, AF-3.2, AF-5, AF-6, AF-7, AF-8, AF-9 and the
decisions become on the build; the inventory of every change and the order of the commits is
`docs/axit/axfile-split.md`. Until the build, the sections above are the truth about the code.

- AF-9.1 (planned) **What changes for the user.** Enter on a sound or video file opens it in AxMedia, which plays it
  with the folder's media files after it; Enter on a text file opens it in AxPreview to read (assumed; to confirm:
  the editors stay one key away, Ctrl+E), Enter on a folder opens it here, Enter on anything else opens the program
  Windows has for it. Play folder in AxMedia (Ctrl+Enter) and Add to AxMedia's queue (Ctrl+Shift+Q) hand the folder
  or the marked entries over; Preview (Ctrl+Shift+P) hands any file to AxPreview. The hand-over goes to the AxMedia
  or AxPreview window already open, else starts one (AM-1.2, AP-1.2); a play and an open take the focus there, an
  add does not: "Added 12 to AxMedia's queue" is said here. The Player menu, the player's keys (Ctrl+P, Ctrl+Left,
  Ctrl+Right, Ctrl+Shift+Left, Ctrl+Shift+Right, Ctrl+T, Ctrl+Shift+Period, Ctrl+Shift+Comma, Ctrl+Q), the video
  area, the queue and the preview leave this window; the settings `speed`, `seekSeconds` and `sayEachFile` leave
  `axfile.json`.
- AF-9.2 (planned) **The Actions list.** The pane under the folder list holds one list, "Actions", shown the first
  time an operation runs and kept for the window's life (AX-9.5: "No actions yet" as its one line until then is not
  needed, since it appears with the first); Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the folder
  list and it, Escape goes back to the folder list; a third of the window at most. Every file operation of this
  window is one line, in time order, newest last, the last fifty kept, gone with the window: "Renamed notes.md to
  todo.md", "Created temp", "Created New file.txt", "Moved 3 items into bundle", "Copied 3 items", "Moved 2 items",
  "Compressed to docs.zip", "Extracted to docs - 2", "Deleted 2 items". A running operation reads what it is doing,
  "Copying 3 items", "Compressing, 40 of 120 files" (a few times a second at most), and its line is replaced when it
  ends; a failure, "Failed: <reason>"; a line that was undone, "Renamed notes.md to todo.md, undone"; one that could
  not be undone when asked, "…, not undone: notes.md has changed". When a line is added while the focus is in the
  folder list, it is the selected line of the Actions list, so that Tab lands on it; nothing is spoken for it (the
  operation's own announcement, AF-7, is what is heard).
- AF-9.3 (planned) **Undo on a line.** Ctrl+Z anywhere in the window takes back the newest line that can be taken
  back (AF-4.14 as built, with the checks and the texts there); Ctrl+Z on a line of the Actions list takes back that
  line, whatever its place, with the same checks ("Not undone: … has changed" when a later action touched the entry;
  the line says so afterwards). Enter on a line reads it again; Ctrl+I says its number and time, "3 of 12, 21:04";
  Delete does nothing (an action is a record, not an entry). There is no redo.
- AF-9.4 (planned) **The hand-over** is the bundle's window message, `Axit.Forms/AppMessage` (AM-1.3): the window
  of the app found by its title through the Axit menu's window list, `WM_COPYDATA` with a verb and the paths; the
  receiving app acts, the sending one says nothing but the add's count. Nothing of it is on the network (G6).
- AF-9.5 (planned) **What AxFile is then.** One folder in one window: the list of its entries, the operations (new
  folder, new text file, rename, delete, cut, copy, paste, move to new folder, compress, extract, copy path), the
  marks, the sort and the filter, Favorites, This PC and the drives, the ways to the other apps on an entry or the
  folder, Undo with the Actions list, and the entry's menu with Windows' own items behind it. The summary (§1), the
  goals (G3 and G5 go; G4 holds), the scenarios 1 and 2, the architecture block, AF-D2 (superseded by AM-D1), AF-D3,
  AF-D4 and AF-D6 (moved to AxMedia) and AF-D9's last sentence are rewritten to this on the build.
