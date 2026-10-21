# AxFile — Specification

AxFile is an app of the Axit bundle (`docs/axit/SPEC.md`): a file manager for one folder, with a player for the
sound and video files in it, for a blind developer who uses the NVDA screen reader. Specified 2026-10-18 and built as a
first draft 2026-10-19 (section 8); the document is kept in step with the code (a behaviour change updates it in the
same commit). Requirements are numbered `AF-n.m` and decisions `AF-Dn`; the bundle's rules, AX-7
(accessibility), AX-8 (keys) and AX-9 (the Axit menu), are cited by number and not repeated. In AxFile's code and
documents, "SPEC.md" means this file.

## 1. Summary

One folder in one window: a list of its entries, folders first, named with the folder and its count. Enter does the
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

1. Start menu, "Axit AxFile": the last folder opens, the list focused; NVDA reads "Dune, 24 items, list, 01 Chapter
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
  (Ctrl+Shift+W) opens File Explorer with the selected entry selected; Item → **Open in editor** (Ctrl+E) opens the
  selected file in AxJSON for `.json` and AxDown for anything else, whatever Enter would do with it.

### AF-2 The window

- AF-2.1 From the top: the menu bar; the video area (AF-5.6), shown only while a video plays; the list, named
  "<folder name>, N items" ("Dune, 24 items"; "Dune, empty" for none); the status bar. The list is the one control; a
  notice takes its place while it shows (AX-6.4). Tab has nothing to move to.
- AF-2.2 Items: a file by its name as it is; a folder as "<name>, folder". Folders first, then files, each group in
  Windows' natural order (`StrCmpLogicalW`, so that `2` comes before `10`). Hidden and system entries are left out
  (`showHidden` in the settings file turns them on). The list is read on a background task (AX-7.5); a folder of ten
  thousand entries fills in one update. An empty folder shows one item, "No items in this folder", selected, so that
  the list is never empty and the reason is read (the bundle's rule for empty lists, AX-9.5); it is not an entry: a
  command on it says "No items in this folder" (AF-7).
- AF-2.3 The title is `<folder name> - AxFile`. The status bar's left label: the player's state, `Playing 03 Chapter
  3.mp3 12:52 / 45:30` while it plays (the time moving once a second), `Paused …`, empty otherwise; the right label:
  the folder's full path. No AccessibleName, no Spring (AxClaude §6.2).
- AF-2.4 **Information** (Ctrl+I; Item → Information): the selected entry on demand, "report.pdf, 1.2 MB, modified
  2026-10-18 14:02" or "docs, folder, modified 2026-10-18 14:02"; the items themselves stay names (AF-D7).

### AF-3 Browsing

- AF-3.1 **Enter** on a folder opens it in the list with its first entry selected. **Backspace** and Alt+Up go to the
  parent with the folder just left selected; at a drive's root, "Top of the drive". NVDA speaks the new selection; the
  app adds nothing.
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
  "Deselected, 2 selected"). **Ctrl+A** marks every entry, "n selected"; **Ctrl+Shift+A** unmarks them all, "None
  selected"; the three are in File operations ▸. Ctrl+I adds ", n selected" to the entry's information while any are
  marked (AF-2.4). Cut, Copy, Delete and Copy path act on the marked entries when there are any, and on the current
  entry alone otherwise (AF-4.3, AF-4.4, AF-4.8); Enter, Rename, the ways to open and the Windows items stay on the
  current entry. The marks live in one folder: showing another folder drops them, a refresh of the same folder keeps
  those still there, and a renamed or deleted entry loses its mark. A mouse click chooses the current entry as before
  and marks nothing. The marks are text in the items rather than the list box's own multiple selection (AF-D9).

### AF-4 Operations

- AF-4.1 **New folder…** (Ctrl+Shift+N): the notice "New folder" with the field "Name" and the buttons Create and
  Cancel; Create makes it in the current folder, selects it and says "Created <name>".
- AF-4.2 **Rename** (F2): in place, as in File Explorer: a box ("New name") opens over the entry holding its name, the
  part before the extension selected (the whole name for a folder), so that typing replaces the name and keeps the
  extension; Enter renames, "Renamed to <new name>", the entry selected under its new name; Escape, or leaving the box
  (a click elsewhere, a notice opening), keeps the name with no announcement, the list's reading of the entry being
  the feedback; the same name again, "Name unchanged". A change of case alone is a rename too. The box is a text box
  that is a child of the list, shown only while a rename runs; the first build (2026-10-19) asked in a notice.
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
  Its shape (AF-6.1): File operations ▸ first, then the ways to open (Open, Open in editor, Open in Axit ▸, Open with
  Windows, Open with another program…), then Show in File Explorer, Information and Properties, and More from
  Windows… last. One `ItemMenu` is the list's context menu and another the bar's Item drop-down, both filled by the
  same method, so that the bar's menu is an exact copy.
- AF-4.7 **Windows' own ways**, items of the entry's menu, each one of the system's own windows like the folder picker
  (AX-7.2): **Open with Windows** opens the entry with the program Windows has for it (a folder in File Explorer),
  or, when Windows has none, the Windows dialog that picks a program and runs it, as File Explorer does on Enter;
  **Open with another program…** shows that dialog always ("Not a file" on a folder); **Properties** (Alt+Enter, File
  Explorer's key) shows the Windows Properties sheet of a file or a folder; **More from Windows…**, the last item,
  shows the shell's own context menu for the entry, File Explorer's classic menu with every installed program's
  entries (`IShellFolder` and `IContextMenu` through P/Invoke, tracked as a native popup the window owns, its first
  item selected, the shell's submenus such as Send to filled through the window's messages), and runs what is chosen.
  A refusal by Windows is "Failed: <reason>"; closing the dialog or the menu is not one.
- AF-4.8 **Cut, Copy, Paste** (Ctrl+X, Ctrl+C, Ctrl+V, in the list; File operations ▸): the entry, or the marked
  entries (AF-3.7), go on the clipboard as files in File Explorer's own formats (`CF_HDROP` and "Preferred
  DropEffect", move for a cut, copy otherwise), "Cut <name>" or "Copied <name>", "Cut n items" or "Copied n items",
  so that File Explorer can paste them and AxFile can paste what File Explorer cut or copied. Paste copies the clipboard's files into the folder shown, or moves them after a cut, through
  the shell (`SHFileOperationW`, undo allowed, a number on a name already there rather than a question, nothing
  overwritten; `FileOperations.CopyOrMoveAsync`, tested): "Pasted <name>" or "Pasted n items", the first one selected;
  "Pasting, please wait" when it takes longer than a second; after a move the clipboard is cleared, as File Explorer
  does. "Nothing to paste" without files on the clipboard; "Already in this folder" for a cut entry pasted where it
  is.
- AF-4.9 **Open in Axit ▸**: one item per app of the bundle, from `Dispatch.Apps`, a new window of that app on the
  entry: AxDown and AxJSON on the file ("Not a file" on a folder), AxGit on the repository that holds the entry, AxFile
  on the folder (or the file in its folder), AxClaude and AxTerminal on the folder, or on the entry's folder for a
  file. Open in editor (Ctrl+E) stays the quick way to the right editor.
- AF-4.10 **Ctrl+I inside the open menu** says the entry's information (AF-7) without closing the menu, so that the
  entry a menu is about can be checked before choosing. WinForms routes the keys of an open menu to the menu, not to
  the window, so `ItemMenu` takes the key itself. The Windows menu (More from Windows…) is Windows' own and does not
  have it.

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
  Open recent folder ▸ | Go to folder… Ctrl+G | ─ | Up one level Backspace | Refresh F5 | ─ | Show current folder in ▸ (File Explorer
  Ctrl+W | AxClaude | AxGit | AxTerminal) | ─ | Exit Alt+F4. **Item (I):** File operations ▸ (Cut Ctrl+X | Copy Ctrl+C |
  Paste Ctrl+V | ─ | Select or deselect Space | Select all Ctrl+A | Select none Ctrl+Shift+A | ─ | New folder…
  Ctrl+Shift+N | Rename F2 | Delete… Delete | Copy path Ctrl+Shift+C) | ─ | Open Enter
  | Open in editor Ctrl+E | Open in Axit ▸ (AxClaude | AxDown | AxJSON | AxGit | AxTerminal | AxFile) | Open with
  Windows | Open with another program… | Show in File Explorer Ctrl+Shift+W | Information Ctrl+I | Properties
  Alt+Enter | ─ | More from Windows…; the same menu is the list's context menu (AF-4.6). **Player (P):** Play Enter | Pause or resume Ctrl+P | Next file Ctrl+Shift+Right | Previous
  file Ctrl+Shift+Left | Forward 15 seconds Ctrl+Right | Back 15 seconds Ctrl+Left | Go to time… Ctrl+T | ─ | Faster
  Ctrl+Shift+Period | Slower Ctrl+Shift+Comma | Normal speed | ─ | Say each file as it starts ✓ | What is playing
  Ctrl+Q. **Options (O):** Text ▸ (Font… | Larger text Ctrl+Plus | Smaller text Ctrl+Minus | Windows text size) |
  General ▸ (Check for updates at start ✓) | Developer ▸ (Open settings file | Copy diagnostics). **Axit (A)**
  (AX-9). **Help (H):** Keyboard shortcuts F1 | User guide | ─ | File resources ▸ (Keyboard shortcuts in Windows, the
  Microsoft page, in the browser). Every item has a mnemonic (AX-7.4).
- AF-6.2 Keys (AX-8). Bundle-wide: F1, the control keys, Ctrl+F, F3, Shift+F3, Ctrl+S ("Nothing to save here"),
  Ctrl+Plus, Ctrl+Minus, Page Up and Page Down (the list's own here), Ctrl+Up and Ctrl+Down with Shift (edit controls
  only; the list leaves them alone), Alt+F4. Window-specific (`AxFileKeys`): Enter, Backspace, Alt+Up, F5, F2, Delete,
  Ctrl+N, Ctrl+O, Ctrl+G, Ctrl+W, Ctrl+Shift+W, Ctrl+E, Ctrl+I, Ctrl+Shift+N, Ctrl+Shift+C, Space, Ctrl+A,
  Ctrl+Shift+A, Ctrl+P, Ctrl+Right, Ctrl+Left, Ctrl+Shift+Right, Ctrl+Shift+Left, Ctrl+T, Ctrl+Shift+Period,
  Ctrl+Shift+Comma, Ctrl+Q, Shift+F10, the Applications key, Alt+Enter, Ctrl+X, Ctrl+C, Ctrl+V. Enter, Backspace,
  Space, F2, Delete, Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A, Ctrl+Shift+A, Shift+F10 and the Applications key act only while
  the list has the focus (in a notice's field or the rename box they are the field's; the two menu keys are the list's
  own, which shows its context menu itself, so the key switch has no case for them; Ctrl+I has a second handler, the
  open menu's, AF-4.10). The collision test covers the table and lists the keys other windows use otherwise (Ctrl+N,
  Ctrl+G, Ctrl+E, Ctrl+T, Ctrl+P, Ctrl+C, Ctrl+Shift+C).
- AF-6.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axfile/user-guide.md` is embedded (Help → User
  guide); About is the bundle's (AX-9.9).

### AF-7 Announcements

One UI Automation notification each, on the list, or on the notice while one shows (AX-7.3): "Playing <name>",
"Paused at m:ss", "Playing" (resumed), "m:ss" after a seek or Go to time, "Speed n", "First file", "End of folder",
"Nothing playing", "Playing <name>, m:ss of m:ss, speed n", "Cannot play <name>: <reason>", "Created <name>", "Renamed
to <name>", "Deleted <name>", "Deleted n items", "Copied path", "Copied n paths", "Cut <name>", "Copied <name>", "Cut n
items", "Copied n items", "Selected", "Deselected", "Selected, n selected", "Deselected, n selected", "n selected",
"None selected" (AF-3.7), "Pasted <name>", "Pasted n items",
"Pasting, please wait" (a paste longer than a second), "Refreshed", "Top of the drive", "Not found: <path>", "No program opens
.xyz. Ctrl+E opens it as text", "Failed: <message>", "<name>, <size>, modified <date>" (Ctrl+I), "Not found" (find),
"Nothing to save here", "Text size n", "Windows text size", "Copied diagnostics", "<name>, <size>, modified <date>, n
selected" (Ctrl+I with marks); and the guards "No folder" (nothing
listed yet), "No items in this folder" (a command on an empty folder's one item, AF-2.2), "Nothing selected", "Not a file" (Ctrl+E or Open with… on a folder), "Not a sound or video file"
(Player → Play on another entry), "Not a time" (Go to time), "Speed n is not supported here", "Name unchanged" (a
rename to the same name), "Nothing to paste" (no files on the clipboard), "Already in this folder" (a cut entry
pasted where it is), "Type a name" and the
other reasons of `FileOperations.CheckName`. A new selection is NVDA's own reading.

### AF-8 Settings, log, status

- AF-8.1 `%APPDATA%\Axit\axfile.json` (AX-5.3; `FileSettings` on `WindowSettings`): `lastFolder`, `recentFolders`
  (ten), `showHidden` off, `sayEachFile` on, `speed` 1, `seekSeconds` 15, and the window base's font, update check
  and window fields.
- AF-8.2 The log is `%LOCALAPPDATA%\Axit\logs\axfile.log`: the start, the exit, the errors of the operations and the
  player with Windows' message; no names of files or folders.

## 5. Architecture

```
src/AxFile.Core/    class library (net10.0): FolderListing (a folder's entries as records, folders first in natural
                    order, hidden left out; what Enter does with an entry, by its extension; the list's name, the
                    information line, the parent, a typed path), NaturalOrder (StrCmpLogicalW), MediaFiles (the
                    extensions, the queue from a listing, the time format and the typed time, the speed steps),
                    MediaPlayer (MFPlay through P/Invoke: open, play, pause, position, length, speed, the started,
                    ended and failed events, a window handle for video; AF-5.2), FileOperations (the name check, new
                    folder, rename, delete to the Recycle Bin, copy and move into a folder, the last three through
                    SHFileOperationW, the messages), FileSettings;
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
  File Explorer, a number on a name already there rather than a question, §7). File Explorer stays one key away for
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
  explicit item away, More from Windows…, for the rest (Send to, an archiver's entries), chosen knowingly.
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
- A question on a name collision when pasting (replace, keep both, skip), where the shell now numbers the copy
  (AF-4.8); a paste of many items that names them.
- Shift+Delete, a permanent delete, after its own question.
- Sorting by date or size, and a "media only" filter of the list.
- Whether a `.m3u` file in the folder should be the queue when there is one.

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
  Ctrl+P). Not yet checked with NVDA.
