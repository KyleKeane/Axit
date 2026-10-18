# AxFile — Specification

AxFile is a planned app of the Axit bundle (`docs/axit/SPEC.md`): a file manager for one folder, with a player for the
sound and video files in it, for a blind developer who uses the NVDA screen reader. Describes AxFile as planned on
2026-10-18; nothing is built yet, and once it is the document is kept in step with the code (a behaviour change updates
it in the same commit). Requirements are numbered `AF-n.m` and decisions `AF-Dn`; the bundle's rules, AX-7
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
   1.mp3, 1 of 24". Down twice, Enter: "Playing 03 Chapter 3.mp3". Space: "Paused at 12:40". Space: "Playing".
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

## 4. Functional requirements

### AF-1 Launch

- AF-1.1 `Axit.exe file [<folder or file>]` (a file opens its folder with the file selected); the console command
  `axfile`; the Start menu entry "Axit AxFile"; "Open in AxFile" on folders in File Explorer. A folder or file given
  alone to `Axit.exe` still means AxClaude or an editor (AX-1.2); AxFile is always named by its verb. Without a path:
  the last folder opened, or the user's profile folder.
- AF-1.2 `AxFileApp.Run(args)` is the entry point (AX-1.7), one call into `AppHost.Run` with the log name `axfile`,
  the usage text, the settings (`axfile.json`) and the window.
- AF-1.3 **To and from the other windows.** AxClaude's Session → Show current folder in ▸ **AxFile**, the editors'
  File → Show current file in ▸ **AxFile** (the file's folder, the file selected), AxGit's Repository → Show current
  repository in ▸ **AxFile** and AxTerminal's Terminal → Show current folder in ▸ **AxFile** each start a new AxFile
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
  thousand entries fills in one update.
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
  Enter in File Explorer); when Windows has none, "No program opens .xyz. Ctrl+E opens it as text".
- AF-3.3 **Open folder…** (Ctrl+O): the Windows folder picker. **Open recent folder ▸**: the last ten folders, "1.
  name (path)" (AX-9.5). **Go to folder…** (Ctrl+G): the notice "Go to folder" with the field "Path", which takes a
  path with environment variables (`%USERPROFILE%\Downloads`) and a file's path (its folder, the file selected); "Not
  found: <path>" when there is no such place.
- AF-3.4 **Refresh** (F5) reads the folder again keeping the selection by name; "Refreshed". Coming back to the window
  after another program changed the folder refreshes it the same way, silently (the list is a view and holds nothing
  unsaved; AxDown AD-3.6 is the editors' rule).
- AF-3.5 A folder that is gone when it is asked for (deleted, an unplugged drive) gives the notice "Folder not found"
  naming it, and the list shows the nearest parent that exists.
- AF-3.6 The list's own keys stay native: arrows, Home, End, Page Up and Page Down, a typed letter to the next name
  starting with it. **Find** (Ctrl+F, the base's find notice) selects the first entry after the current one whose name
  contains the text, "Not found" otherwise; F3 and Shift+F3 the next and the previous.

### AF-4 Operations

- AF-4.1 **New folder…** (Ctrl+Shift+N): the notice "New folder" with the field "Name" and the buttons Create and
  Cancel; Create makes it in the current folder, selects it and says "Created <name>".
- AF-4.2 **Rename…** (F2): the notice "Rename <name>" with the field "New name" holding the name, the part before the
  extension selected, and the buttons Rename and Cancel; "Renamed to <new name>".
- AF-4.3 **Delete…** (Delete): the notice "Delete <name>? It goes to the Recycle Bin." (for a folder: "… with
  everything in it.") with the buttons Delete and Cancel; the deletion goes through the shell (`SHFileOperationW` with
  `FOF_ALLOWUNDO`, silent and without the shell's own confirmation), so that the Recycle Bin takes it; "Deleted
  <name>", the selection moves to the next entry. A permanent delete is not offered (§7).
- AF-4.4 **Copy path** (Ctrl+Shift+C): the full path to the clipboard, "Copied path".
- AF-4.5 A failure (a name taken, a file in use, access denied) is one announcement, "Failed: <Windows' message>",
  when it is one line, a notice otherwise (AxGit AG-7). Every operation runs on a background task; the list refreshes
  after it.

### AF-5 The player

- AF-5.1 **Media files**, by extension (`MediaFiles.Extensions`, tested): sound `.mp3`, `.wav`, `.wma`, `.m4a`,
  `.m4b`, `.aac`, `.flac`, `.ogg`, `.opus`, `.aiff`; video `.mp4`, `.m4v`, `.mov`, `.avi`, `.wmv`, `.mkv`, `.webm`.
  Whether Windows plays one depends on its decoders (the Store's extensions for some); a file that cannot be opened
  says "Cannot play <name>: <reason>" and the player moves to the next.
- AF-5.2 **Engine**: Windows' own media stack through one P/Invoke seam, `MediaPlayer` in `AxFile.Core`, no package
  (B-10). The first build measures the two candidates and records the choice here (AF-D3): MCI (`mciSendString` of
  winmm, the DLL the sounds already use: open, play, pause, seek, speed, position and length, as strings) for its
  plainness, and Media Foundation's MFPlay (`MFPCreateMediaPlayer`, COM through P/Invoke) for the formats Windows
  decodes today (FLAC, AAC) and a speed change that keeps the pitch. The criterion is the user's: an audiobook at 1.5
  with the voice unchanged.
- AF-5.3 **Play** (Enter on a media file; Player → Play): plays it; "Playing <name>". When it ends, the next media
  file of the same folder, in the list's order, plays, to the end of the folder: "End of folder" with the done chime
  (the shared `Sounds`, AxClaude FR-7.3). The queue is the folder's media files as they were when Enter was pressed,
  so browsing elsewhere in the window changes nothing (AF-D6). Enter on another media file while one plays switches
  to it.
- AF-5.4 **Keys**, from the list, whatever folder it shows: **Space** pauses or resumes ("Paused at 12:40" /
  "Playing"); **Ctrl+Right** and **Ctrl+Left** move 15 seconds forward or back (`seekSeconds`), announcing the new
  position ("12:25"); **Ctrl+Shift+Right** and **Ctrl+Shift+Left** the next and the previous file of the queue
  ("Playing <name>"; "First file" and "End of folder" at the ends); **Ctrl+Shift+Period** and **Ctrl+Shift+Comma**
  faster and slower by 0.25 between 0.5 and 3 ("Speed 1.25"), Player → Normal speed back to 1 ("Speed 1"), the
  speed kept in the settings (`speed`) for the next file and the next window; **Ctrl+T** Go to time…, the notice with
  the field "Time" (`12:40`, `1:02:10`, or `750` for seconds), "12:40" when it is done; **Ctrl+Q** What is playing:
  "Playing <name>, 12:52 of 45:30, speed 1.25", "Paused …" or "Nothing playing". "Say each file as it starts"
  (`sayEachFile`, default on; the Player menu) is the one unprompted announcement (G5). The volume is Windows' own:
  the keyboard's volume keys work from anywhere and the app has none (AF-D4).
- AF-5.5 Closing the window stops the player. Stop as a command is not offered: Space pauses, and the next Enter plays
  something else (§7).
- AF-5.6 A video's picture is drawn in an area at the top of the window (`Video`, not focusable; never a window of its
  own, AX-7.2), its sound through the same path; the area is hidden while nothing plays or sound plays.
- AF-5.7 The time format is `m:ss` under an hour and `h:mm:ss` over (`MediaFiles.FormatTime`, tested).

### AF-6 Menus, keys, texts

- AF-6.1 Menus, in the standard order of a first menu (AX-6): **Folder (F):** Open folder… Ctrl+O | Open recent folder
  ▸ | Go to folder… Ctrl+G | ─ | Up one level Backspace | Refresh F5 | ─ | Show current folder in ▸ (File Explorer
  Ctrl+W | AxClaude | AxGit | AxTerminal) | ─ | Exit Alt+F4. **Item (I):** Open Enter | Open in editor Ctrl+E | Show
  in File Explorer Ctrl+Shift+W | Information Ctrl+I | ─ | New folder… Ctrl+Shift+N | Rename… F2 | Delete… Delete |
  Copy path Ctrl+Shift+C. **Player (P):** Play Enter | Pause or resume Space | Next file Ctrl+Shift+Right | Previous
  file Ctrl+Shift+Left | Forward 15 seconds Ctrl+Right | Back 15 seconds Ctrl+Left | Go to time… Ctrl+T | ─ | Faster
  Ctrl+Shift+Period | Slower Ctrl+Shift+Comma | Normal speed | ─ | Say each file as it starts ✓ | What is playing
  Ctrl+Q. **Options (O):** Text ▸ (Font… | Larger text Ctrl+Plus | Smaller text Ctrl+Minus | Windows text size) |
  General ▸ (Check for updates at start ✓) | Developer ▸ (Open settings file | Copy diagnostics). **Axit (A)**
  (AX-9). **Help (H):** Keyboard shortcuts F1 | User guide. Every item has a mnemonic (AX-7.4).
- AF-6.2 Keys (AX-8). Bundle-wide: F1, the control keys, Ctrl+F, F3, Shift+F3, Ctrl+S ("Nothing to save here"),
  Ctrl+Plus, Ctrl+Minus, Page Up and Page Down (the list's own here), Ctrl+Up and Ctrl+Down with Shift (edit controls
  only; the list leaves them alone), Alt+F4. Window-specific (`AxFileKeys`): Enter, Backspace, Alt+Up, F5, F2, Delete,
  Ctrl+O, Ctrl+G, Ctrl+W, Ctrl+Shift+W, Ctrl+E, Ctrl+I, Ctrl+Shift+N, Ctrl+Shift+C, Space, Ctrl+Right, Ctrl+Left,
  Ctrl+Shift+Right, Ctrl+Shift+Left, Ctrl+T, Ctrl+Shift+Period, Ctrl+Shift+Comma, Ctrl+Q. Enter, Backspace, Space, F2
  and Delete act only while the list has the focus (in a notice's field they are the field's). The collision test
  covers the table and lists the keys other windows use otherwise (Ctrl+G, Ctrl+E, Ctrl+T, Ctrl+Shift+C).
- AF-6.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axfile/user-guide.md` is embedded (Help → User
  guide); About is the bundle's (AX-9.9).

### AF-7 Announcements

One UI Automation notification each, on the list, or on the notice while one shows (AX-7.3): "Playing <name>",
"Paused at m:ss", "Playing" (resumed), "m:ss" after a seek or Go to time, "Speed n", "First file", "End of folder",
"Nothing playing", "Playing <name>, m:ss of m:ss, speed n", "Cannot play <name>: <reason>", "Created <name>", "Renamed
to <name>", "Deleted <name>", "Copied path", "Refreshed", "Top of the drive", "Not found: <path>", "No program opens
.xyz. Ctrl+E opens it as text", "Failed: <message>", "<name>, <size>, modified <date>" (Ctrl+I), "Not found" (find),
"Nothing to save here", "Text size n", "Windows text size", "Copied diagnostics". A new selection is NVDA's own
reading.

### AF-8 Settings, log, status

- AF-8.1 `%APPDATA%\Axit\axfile.json` (AX-5.3; `FileSettings` on `WindowSettings`): `lastFolder`, `recentFolders`
  (ten), `showHidden` off, `sayEachFile` on, `speed` 1, `seekSeconds` 15, and the window base's font, update check
  and window fields.
- AF-8.2 The log is `%LOCALAPPDATA%\Axit\logs\axfile.log`: the start, the exit, the errors of the operations and the
  player with Windows' message; no names of files or folders.

## 5. Architecture

```
src/AxFile.Core/    class library (net10.0): FolderListing (a folder's entries as records, folders first in natural
                    order, hidden left out; what Enter does with an entry, by its extension), MediaFiles (the extensions,
                    the queue from a listing, the time format, the speed steps), MediaPlayer (the Windows media API
                    through P/Invoke: open, play, pause, position, length, speed, the end-of-file event, a window handle
                    for video; AF-5.2), FileOperations (new folder, rename, delete to the Recycle Bin through
                    SHFileOperationW, the messages), FileSettings; references Axit.Core
src/AxFile/         class library (net10.0-windows): AxFileApp (Run, Usage), FileForm (the window, on AxitForm),
                    AxFileKeys, HelpText (F1 text, the embedded guide); references AxFile.Core, Axit.Core, Axit.Forms
tests/AxFile.Tests/ FolderListingTests and FileOperationsTests over temporary folders (the Recycle Bin is not
                    exercised), MediaFilesTests (extensions, queue order, time format, speed steps), FileSettingsTests;
                    the player itself is checked by ear
docs/axfile/        this spec, user-guide.md (embedded), nvda-test-plan.md
```

`FileForm` derives from the window base (AX-6.5): the notices, the fonts, the window's place, find and the update flow
are the base's. The player runs on the UI thread through the API's own events; the folder reads and the operations run
on background tasks (AX-7.5).

## 6. Design decisions

- **AF-D1 One list, no tree.** A tree view asks a screen reader user to track depth and expansion; a list of one
  folder with Backspace for up is what File Explorer's main pane is, and NVDA reads a list box best. The path in the
  status bar is the depth.
- **AF-D2 The player is AxFile's feature, not an app.** The ask was a player for everything in a folder; a folder's
  files in order are a playlist, and the list is already here. A separate AxMedia would have been this list again with
  fewer verbs and a second window for the same folder; the Axit menu names an AxFile window by its folder, which is
  how a playlist is named anyway. The player's keys are with Ctrl and Shift, so that the list's own letters and arrows
  stay what they are.
- **AF-D3 Windows' own decoders through one seam, chosen by measurement.** No codec ships with the bundle (B-10);
  Windows decodes what the user's other players decode. MCI is a single function and strings; MFPlay is a COM
  interface but decodes more and may keep the pitch at speed. The spec records the choice when the build has made it,
  with the formats that worked on the user's machine.
- **AF-D4 Volume is Windows'.** The keyboard's volume keys change the system volume from any window and are read by
  NVDA; two more keys in the app would duplicate them, and the bundle's Ctrl+Up and Ctrl+Down are the paragraph keys
  (AX-8.2).
- **AF-D5 Streamlined operations.** New folder, rename, delete and copy the path are the daily four; copy, move and
  paste wait for the ask (§7), and File Explorer is one key away for the rest (Ctrl+W, Ctrl+Shift+W).
- **AF-D6 The queue is the folder at the moment of Enter.** A playlist that changed under the listener because the
  window browsed elsewhere would be a surprise; the queue is fixed when play starts and the list is free to roam.
- **AF-D7 Items are names; details on demand.** "report.pdf, 1.2 MB, 2026-10-18" on every arrow key is noise for a
  user who wants the name; Ctrl+I says the rest when asked (AX-7.3).

## 7. Open questions

- Remembering the position in a file across windows (an audiobook resumed where it stopped): per file in the
  settings, with a cap; the first candidate for version two.
- Copy, cut and paste of entries as files on the clipboard (`CF_HDROP`), so that File Explorer and AxFile exchange
  them; a copy within the window.
- Shift+Delete, a permanent delete, after its own question.
- Sorting by date or size, and a "media only" filter of the list.
- Whether a `.m3u` file in the folder should be the queue when there is one.

## 8. Status

- Planned 2026-10-18 at the user's ask (as AxMedia, then as AxFile's player, AF-D2). Nothing built.
