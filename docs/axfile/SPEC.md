# axFile — Specification

axFile is an app of the Axit bundle (`docs/axit/SPEC.md`): a file manager for one folder, for a blind developer who
uses the NVDA screen reader. Specified 2026-10-18 and built as a first draft 2026-10-19; on 2026-10-22 the player it
had carried became axMedia and the preview pane axPreview, so that axFile is file operations and management only
(section 8; `docs/axit/axfile-split.md`). The document is kept in step with the code (a behaviour change updates it in
the same commit). Requirements are numbered `AF-n.m` and decisions `AF-Dn`; the bundle's rules, AX-7
(accessibility), AX-8 (keys) and AX-9 (the Axit menu), are cited by number and not repeated. In axFile's code and
documents, "SPEC.md" means this file.

## 1. Summary

One folder in one window: a list of its entries, folders first, named with its count (the folder is the title). Enter
does what File Explorer does: a folder opens in the list, a file opens in the program the user has made its default in
Windows (axDown, axJSON, axMedia, axPreview or any other, once chosen there); Shift+Enter reads a file in axPreview;
Backspace goes up. The operations, each one notice or one announcement: new folder, new text file, rename in place,
delete to the Recycle Bin, cut, copy and paste as files, move to a new folder, compress to zip and extract, copy the
path; marks with Space for several at once; sort, filter, Favorites, This PC and the drives. Every operation is a
line of the **Actions** list under the folder list, where it can be read again, undone (Ctrl+Z) and walked back to
(Enter). Sound and video go to axMedia (Play folder, Add to queue), reading to axPreview, everything else in Windows
is one item away (Open with, Properties, the Windows menu). The window is reached from the other windows on the
folder of the file or project in hand, and sends back. It works entirely offline (bundle G6).

## 2. Goals and non-goals

- **G1 One list, native.** A list box NVDA reads item by item, a status bar, the bundle's notices; no tree, no grid,
  no icons that carry meaning (AX-7.1).
- **G2 Enter is the verb.** A folder opens here; a file opens as it would in File Explorer, in the user's own default
  program; the user learns one key, and the same key in both places.
- **G3 One job.** Files and folders: listing, choosing, moving, naming, deleting, and undoing. Playing is axMedia's,
  reading is axPreview's, editing is the editors'; axFile hands over and says nothing more than it has to (AF-D2).
- **G4 Streamlined.** The plan's worry on record: a file tree's behaviour is endless. The operations are the ones
  named here and no more (AF-D5).
- **G5 Nothing spoken unprompted** (AX-7.3), and nothing answered with silence: a command's result is one short
  announcement, a change of folder names the folder, a key that moves nothing says so (AF-3.1, AF-3.6); the Actions
  list is where the rest can be read when wanted.
- **Non-goals.** A tree or a second folder pane; drag and drop; a properties dialog of our own; search across folders;
  file type associations of our own (Windows' own, AF-D2); a player, a viewer or an editor inside the window (the
  other apps'); a permanent delete (§7).

## 3. Scenarios

1. Start menu, "Axit axFile": the last folder opens, the list focused; NVDA reads "24 items, list, 01 Chapter
   1.mp3, 1 of 24". Up: one short low note, nothing moves. Down twice, Enter: the file opens in the program Windows has
   for `.mp3`, axMedia when the user has made it so, with the folder's other chapters after it. Backspace: "Music, 8
   items, Dune, folder".
2. In axClaude on a project, Alt+S, I, F: axFile opens on the project folder. Down to `docs`, Enter: "docs, 5 items,
   plan.md"; Shift+Enter on `plan.md`: axPreview shows it to read; Ctrl+E on it: axDown opens it to edit. Backspace:
   "myproject, 12 items, docs, folder".
3. Ctrl+Shift+N, type `notes`, Enter: "Created notes", the folder selected. F2 on it, type `Notes`, Enter: "Renamed to
   Notes". Delete: "Delete Notes? It goes to the Recycle Bin with everything in it.", Enter: "Deleted Notes". Tab:
   "Actions, list, Deleted Notes, 3 of 3"; Ctrl+Z: "Undone: Notes restored", the folder list on Notes; Tab: the line
   reads "Deleted Notes, undone".
4. Ctrl+Shift+C on `report.pdf`: "Copied path"; Enter on it: the PDF opens in the program Windows has for it.
5. Ctrl+F, type `chap`, Enter: the first entry whose name holds it is selected; F3 the next.
6. Ctrl+G, type `%USERPROFILE%\Downloads`, Enter: the list is the Downloads folder.
7. Space on `a.txt`: "Selected"; Down, Space: "Selected, 2 selected"; Up: "a.txt, selected"; Ctrl+X: "Cut 2 items";
   Enter on `docs`, Ctrl+V: "Pasted 2 items". Later, in another folder, Tab to the Actions list, Up to "Moved 2
   items", Enter: the list is `docs` with `a.txt` selected.
8. Space on two folders of chapters, Ctrl+Shift+Q: "Added 24 to axMedia's queue"; the focus stays here and the sound
   goes on in axMedia.

## 4. Functional requirements

### AF-1 Launch

- AF-1.1 `Axit.exe file [<folder or file>]` (a file opens its folder with the file selected); the console command
  `axfile`; the Start menu entry "Axit axFile"; axFile under "Open with Axit" on folders and on files in File
  Explorer (AX-2.3). A folder or file given alone to `Axit.exe` still means axClaude or an editor (AX-1.2); axFile is
  always named by its verb. Without a path: the last folder opened, or the user's profile folder.
- AF-1.2 `AxFileApp.Run(args)` is the entry point (AX-1.7), one call into `AppHost.Run` with the log name `axfile`,
  the usage text, the settings (`axfile.json`) and the window. The process moves its working directory to the
  program's own folder first (the path of the command line already being full): a folder that is some process's
  current directory cannot be renamed or moved while that process runs (Windows' "being used by another process"),
  so a file manager holds none, and the apps it starts inherit none (found 2026-10-22: a folder renamed once could
  not be renamed again while a program had it as its current directory; AF-4.5).
- AF-1.3 **To and from the other windows.** axClaude's Session → Show current folder in ▸ **axFile**, the editors'
  File → Show current file in ▸ **axFile** (the file's folder, the file selected), axGIT's Repository → Show current
  repository in ▸ **axFile**, axTerminal's Session → Show current folder in ▸ **axFile** and axMedia's Queue → Show
  current file in ▸ **axFile** each start a new axFile window (`AxitMenu.Start`, found again in the Axit menu, AX-9.3).
  From axFile, Folder → Show current folder in ▸ **File Explorer** (Ctrl+W), **axClaude**, **axTerminal**, **axGIT** (bundle AX-9.10);
  Item → **Show in File Explorer** (Ctrl+Shift+W) opens File Explorer with the selected entry selected; Item → Open in
  Axit ▸ **Editor** (Ctrl+E) opens the selected file in axJSON for `.json` and axDown for anything else
  (`Dispatch.EditorOf`, the bundle's one rule), whatever Enter would do with it; Item → **Preview** (Shift+Enter) and the axMedia items
  are AF-5.

### AF-2 The window

- AF-2.1 From the top: the menu bar; the folder list, named with its count alone, "N items" ("24 items"; "1 item";
  "Empty" for none; "3 of 24 items, filter chap" under a filter, AF-3.8); the Actions list under it, always (AF-9);
  the status bar. A notice takes the place of both lists while it shows (AX-6.4). Tab, Shift+Tab, Ctrl+Tab,
  Ctrl+Shift+Tab and F6 move between the two lists (AX-8.2), the window's two controls.
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
- AF-2.3 The title is `<folder name> - axFile`. The status bar's left label: the newest file operation's line in the Actions list
  ("Copied 3 items", "Compressing, 40 of 120 files"; never a selection line, AF-9.2), empty before the first operation; the right label: the folder's
  full path. No AccessibleName, no Spring (axClaude §6.2).
- AF-2.4 **Information** (Ctrl+I; Item → Information): the selected entry on demand, "report.pdf, 1.2 MB, modified
  2026-10-18 14:02" or "docs, folder, modified 2026-10-18 14:02"; the items themselves stay names (AF-D7).

### AF-3 Browsing

- AF-3.1 **Enter** on a folder opens it in the list with its first entry selected. **Backspace** and Alt+Up go to the
  parent with the folder just left selected; at a drive's root, to This PC with that drive selected (AF-3.10), and at
  This PC, "Top". **Right Arrow** on a folder or a drive opens it, as Enter; **Left Arrow** goes up, as Backspace
  (asked 2026-10-22: the keys of a tree on the flat list, AF-D12). Right never opens a file: on a file, or on a line
  that is not an entry, it sounds the edge earcon (AF-3.6), as Left does at This PC. Every change of folder (Enter, Backspace, Go to folder, Open folder, a recent folder or a favorite,
  a drive, This PC, Enter on a line of the Actions list) is announced as the folder's name, the list's count and the
  entry landed on, "docs, 5 items, plan.md" ("This PC, 3 items, Local Disk (C:), drive"; "temp, No items in this
  folder"), through one notification that cuts short NVDA's own reading of the new entry (asked 2026-10-22: the entry
  alone did not tell the folder landed in from the folder under the focus; the first build left it to NVDA). The
  start is NVDA's own reading of the window, the list and its entry, and an undo says what it undid instead (AF-4.14).
- AF-3.2 **Enter** on a file opens it with the program the user has made its default in Windows (`ShellExecute`, as
  Enter in File Explorer; AF-D2); when Windows has none, the Windows dialog that picks a program, as File Explorer
  shows it (AF-4.7). The Axit apps register as programs for their kinds when Axit is installed (AX-2.3), so that
  axDown, axJSON, axMedia or axPreview can be chosen there (the guide says how; an installer cannot set a default).
  **Shift+Enter** on a file opens it in axPreview to read (Item → Preview; AF-5.3); on a folder it is File
  Explorer's Shift+Enter, the folder in a new axFile window. **Ctrl+E** opens a file in its editor (AF-1.3).
- AF-3.3 **Open folder…** (Ctrl+O): the Windows folder picker. **Open recent folder ▸**: the last ten folders opened
  by command, "1. name (path)" (AX-9.5): the start, Open folder, Go to folder, a recent item, and the folder the
  window closed on; Enter and Backspace browse without joining the list, so that a walk through a tree does not fill
  it. **Go to folder…** (Ctrl+G, or Alt+D, File Explorer's key for its address bar): the notice "Go to folder" with the field "Path" holding the current folder with a backslash at its end (empty at This PC), which
  takes a path with environment variables (`%USERPROFILE%\Downloads`) and a file's path (its folder, the file
  selected); "Not found: <path>" when there is no such place. In the field, **Down Arrow** and **Up Arrow** complete the
  path (asked 2026-10-22, AF-D12): the text up to its last backslash is a folder (with environment variables; a
  name without one is in the folder shown; an empty field is the drives), the rest the start of a name; the entries
  of that folder whose names start with it, folders first in natural order and hidden or period-named ones as the
  list shows them, replace the text one per press, a folder with a backslash at its end, each said as "Documents, folder, 2 of 5" or "notes.md, 3 of 5"; past either end the edge earcon and then the
  completion again, as the command palette and axStart do at an end (AX-10.4, 2026-10-23); when no name starts with
  the text, the edge earcon alone. **Right Arrow** at the end of a completed folder lists its own entries, the first one taken, as Right in the
  list goes into a folder. What is typed in between is what the next Down completes. The folder is
  read on a task (`PathCompletion` in the pure code, tested). **New window** (Ctrl+N, File Explorer's key; Folder →
  New window): another axFile window on the folder shown, through the bundle's starter (AX-9.2), so that two folders
  can be worked between; Ctrl+N is the other windows' New session, New document and New branch, and a new window of
  any app is the Axit menu's (AX-9).
- AF-3.4 **Refresh** (F5) reads the folder again keeping the selection by name; "Refreshed". Coming back to the window
  after another program changed the folder refreshes it the same way, silently (the list is a view and holds nothing
  unsaved; axDown AD-3.6 is the editors' rule).
- AF-3.5 A folder that is gone when it is asked for (deleted, an unplugged drive) gives the notice "Folder not found"
  naming it, and the list shows the nearest parent that exists.
- AF-3.6 The list's own keys stay native: arrows, Home, End, Page Up and Page Down. **Typing a name** jumps to it as
  in File Explorer (asked 2026-10-22): letters typed within a second of each other build one prefix, "cha", and the
  first entry whose name starts with it is selected, searched from the entry in hand and round the end, NVDA reading
  it; a new prefix begins after a second's pause and is searched from the next entry on; a run of the same letter ("a",
  "aa") cycles through the names starting with it, from the next entry on, as File Explorer does, while any other
  prefix is searched whole, so that a name with a doubled letter inside it ("aab") is found (`Axit.Core/TypeAhead`,
  tested, the bundle's since 2026-10-24, AX-8.6; until 2026-10-23 the doubled letter made the one-letter cycle); Shift
  with a letter searches the same way backwards (2026-10-24); the edge earcon when no name starts with it (both lists are
  the bundle's `AxitList` since 2026-10-24, which has the type-ahead and the edge earcon built in). The Actions
  list takes typed letters the same way. The list box's own
  one-letter jump gives way to this (`OnListKeyPress`); Space is the selection's key (AF-3.7), so a name is typed up
  to its first space. At the ends, where a key moves nothing, the window sounds the **edge earcon** rather than
  nothing (asked 2026-10-22, an earcon rather than words, since "First item" and the entry again were noise): one
  short low note (`Sounds.Edge`, A3 for 60 ms) on Up or Home at the first entry and Down or End at the last, in the
  folder list and the Actions list alike (`AxFileKeys.FirstItem`, `LastItem` and their seconds; the keys are the
  list's own everywhere else, `AtEdge`). **Find** (Ctrl+F, the bundle's Find notice with the field "Name contains") selects the first entry
  whose name holds the text, F3 and Shift+F3 the next and the previous. NVDA reads the selected entry; the app adds nothing.
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
  empty folder's, the not-shown line) the keys say that line. **Shift+Space** (asked 2026-10-22) marks every entry
  from the nearest marked entry to the current one, above or below, the one above when both are as near, "Selected, n
  selected"; with no other entry marked it is Space: the current entry marked, or unmarked again (decided 2026-10-23; it said "1 selected" on the one marked entry and changed nothing). **Ctrl+A** marks every entry, "n selected";
  **Ctrl+Shift+A** unmarks them all, "None selected", and so does **Escape** in the list while any are marked (asked 2026-10-22; a filter in force is cleared by the next Escape). Ctrl+I adds ", n selected" to the entry's information while
  any are marked (AF-2.4). Cut, Copy, Delete, Copy path, Move to folder, Compress to zip and Add to axMedia's queue
  act on the marked entries when there are any, and on the current entry alone otherwise (AF-4.3, AF-4.4, AF-4.8,
  AF-5.2; kept 2026-10-22 over File Explorer's right-click rule, AF-D10); Enter, Rename, the ways to open and the
  Windows items stay on the current entry. The **Selection** menu of the bar (S) is the marks from anywhere, whatever
  entry is current: Select all, Deselect all, Select or deselect this entry, Select to here (Shift+Space), then the
  operations that take the marked entries, Cut, Copy, Delete, Move to folder…, Compress to zip, Add to axMedia's
  queue, Copy path; its operations never fall back to the current entry: "None selected" when nothing is marked (asked
  2026-10-22: one menu for the entry, Item, one for the marks, Selection, one for the folder, Folder, so that the
  target is the menu's name and no mode is needed). Every change of the marks is a line of the Actions list's
  Selection section, which Ctrl+Z on the line takes back (AF-9.2, AF-9.3). The marks live in one
  folder: showing another folder drops them, a refresh of the same folder keeps those still there, and a renamed or
  deleted entry loses its mark. The marks are on the entries the list shows: a filter, a new order or a read drops
  the marks on the entries it leaves out, with no line, so that the count said and the entries acted on are one set
  (until 2026-10-23 a mark hidden by the filter was counted and not acted on). The marks are one pure piece, `Marks`
  in AxFile.Core (each key one method over the shown entries, returning the rows whose mark flipped, the line and
  the words; tested), which the window applies to its items and the Actions list (`FileForm.ApplyMarks`). A mouse click chooses the current entry as before and marks nothing. The marks are
  text in the items rather than the list box's own multiple selection (AF-D9).
- AF-3.8 **Sort and filter** (asked 2026-10-21). **Folder → Sort by ▸** Name, Date, Size, one checked: folders first
  in every order, then by name (the natural order), by date newest first, or by size largest first, ties by name
  (`FolderListing.Sort`, tested); the entry in hand stays selected; "Sorted by date"; the order is a setting
  (`sortBy`) and holds for every folder and the next window. **Filter…** (Ctrl+Shift+F; Folder → Filter…): the notice
  "Filter" with the field "Name contains" holding the filter in force; Filter lists only the entries whose name
  contains the text, any case, names the list "3 of 24 items, filter chap" so that the count is read with the list,
  and says the name and the entry at once, "3 of 24 items, filter chap, Chapter 1.mp3"; an empty field clears it,
  "Filter cleared", as does **Escape** in the list while a filter is on and no entry is marked (Escape clears the marks first, AF-3.7, and does nothing when there is neither). A filter that nothing passes shows the one item "No names contain <text>", and a command on
  it says the same. The filter lives in the folder: another folder drops it, a refresh keeps it. What the list shows
  is what the commands see: Select all marks the shown entries, find moves among the shown entries, and a mark on an entry the filter hides is gone (AF-3.7). Find (AF-3.6)
  jumps, the filter narrows; both are kept. Play folder (AF-5.1) hands the folder itself to axMedia, filter or not.
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
  "Local Disk (C:), 120 GB free of 476 GB"; the title is "This PC" and the list is named by its count, as everywhere (AF-2.1); Backspace there, "Top". It
  is not a folder: the last folder, the recent list and the Recently closed list keep the folder before it (and Ctrl+N and the restart after an
  update start the new window on the last folder, since This PC is no path; until 2026-10-23 the new window said
  it does not exist), the sort
  order leaves the letter order alone, and the commands that need a folder say "Choose a drive first" (New folder,
  Paste, Rename, Delete, Cut, Copy, Ctrl+D, Play folder, Add to queue, Show current folder in ▸ the other apps); Show
  current folder in ▸ File Explorer opens File Explorer's own This PC. The other items of the entry's menu work on a
  drive as on a folder (Open in Axit ▸, Show in File Explorer, Properties, Windows menu…).
- AF-3.11 **The Drives menu** (asked 2026-10-21, for moving things to a stick and taking it out): **Folder → Drives ▸**
  (its own menu on Alt+D until 2026-10-22, when it became a submenu at the user's ask and Alt+D went to Go to folder,
  AF-3.3) lists the ready drives as "1. Local Disk (C:)" (AX-9.5), each opening the drive's root and joining the
  recent list, then **This PC** (the drives list, AF-3.10, the folder's drive selected), then **Eject this drive**
  and **Drive properties**, both for the drive in hand: the entry at This PC, or the folder's own drive. Eject is
  the shell's own: the drive's context menu is built as for Windows menu… (AF-4.7) and its item whose verb is
  `eject` is invoked (`Shell.Eject`), so a stick, a card or a disc comes out the way File Explorer's Eject does and
  Windows says the rest (safe to remove, or in use); "Ejecting <name>", and the list goes to This PC when it was on
  that drive; "Failed: This drive has no Eject" on a fixed disk, "Failed: <reason>" when Windows refuses. Drive
  properties is the Windows sheet of the drive (space, tools). The menu is refilled each time it opens, since a stick
  may have come or gone. Without a folder, "No folder".
- AF-3.12 **The command palette** (bundle AX-10, 2026-10-22): after the window's commands, the entries the folder list
  shows (the filter and the order applied), each "notes.md, file", "docs, folder" or, at This PC, "Local Disk (C:),
  drive". Enter on one selects it in the folder list and opens it as Enter there does (AF-3.2): a folder or drive
  here, a file in its program. The marks are not changed.

### AF-4 Operations

Every operation here is one line of the Actions list (AF-9) as well as its announcement.

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
  (a click elsewhere, a notice opening, a change of folder or a refresh, which until 2026-10-23 left the box open over
  the new list), keeps the name with no announcement, the list's reading of the entry being the feedback; the same
  name again, "Name unchanged". A change of case alone is a rename too. The box is a text box
  that is a child of the list, shown only while a rename runs; the first build (2026-10-19) asked in a notice. A name
  that **reads the same** as another entry's without being it, differing only in case plus a trailing or doubled
  space, a different kind of space, an invisible character (a zero-width space, a joiner, a mark) or an accent
  written as one or two characters, is refused with "Reads the same as <that name>, which is already here" and the
  box stays (`FileOperations.Lookalike`, tested; the same for New folder…, AF-4.1, and Move to folder…,
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
  on a line, "Copied n paths". Not an operation on disk: no line in the Actions list.
- AF-4.5 A failure (a name taken, a file in use, access denied) is one announcement, "Failed: <Windows' message>",
  when it is one line, a notice otherwise (axGIT AG-7), and the operation's line reads "Failed: <reason>". Two of
  Windows' messages are said with what they mean here, since both come from something outside axFile: "In use by
  another program: a terminal or an app runs in it, or the file is open" (a folder that is a process's current
  directory, axTerminal or axClaude opened on it, a program started from Open with ▸ in it, or a file a program holds;
  verified 2026-10-22, when a folder renamed once could not be renamed again for this reason), and "Access denied: a
  file inside is open in another program, or Windows protects the place". **Every operation runs on a thread of its
  own** (a task for the reads, the rename and the new folder; a single-threaded-apartment thread per shell operation
  for copy, move, delete and the Recycle Bin's Restore) and the window never waits for one: a second paste, delete or
  zip starts while the first runs, each with its own line in the Actions list (AF-9.2). One runner, `FileForm.RunOperation`, runs each of them:
  its line from Begin to End, the paths it writes or moves away held in flight while it runs, "<doing>, please
  wait" when it passes a second (Pasting, Deleting, Moving, Compressing, Extracting), its failure on the line and
  said, and its result shown and said; an operation that ends at once (a rename, a new folder or file) goes through
  `RunAtOnceAsync` the same way (2026-10-23; five hand-written runners before, Delete and Move to folder without
  the wait notice). Two operations at once are
  kept apart: a paste chooses its names around the paths a running paste is still writing ("notes - 2.md" for the
  same clipboard pasted twice, `FileOperations.Destinations` with the reserved paths), a cut whose files a paste is
  still moving says "Already pasting" rather than moving them twice, and when only some of its files are still
  on their way the others are pasted and the ones left out are counted, "Pasted 2 items, 1 already pasting" (until
  2026-10-23 they were left out without a word), and a compress takes its zip's name at once so
  that a second compress picks the next (AF-4.12). An operation that ends in the folder shown reads the list again
  with its result selected; one that ends while another folder is shown says its result and leaves the list where
  the user is (2026-10-23: it pulled the list back to its own folder, a move the user did not make, AX-7.3).
- AF-4.6 **The entry's menu** (Alt+I; Shift+F10, the Applications key and a right click open the same menu bar menu, AF-D13): the Item menu as the list's context
  menu, the same items in the same order, on the selected entry (a right click selects the entry under it first, as
  File Explorer does). It is the window's own menu, fixed, with mnemonics and listed in F1, not the shell's (AF-D8).
  Its shape (AF-6.1; reshaped 2026-10-21 at the user's ask for File Explorer's plain words and one flat list): Open,
  Open with ▸, Open in Axit ▸, Preview, Play folder in axMedia, Add to axMedia's queue | Cut, Copy, Paste | Rename,
  Delete | New folder…, New text file, Move to folder…, Compress to zip, Extract here | Copy path, Show in File
  Explorer, Information, Properties | Windows menu…. There is one menu, the bar's Item menu, the standard drop-down
  (AX-6.6); the list has no context menu of its own (AF-D13).
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
  DropEffect", move for a cut, copy otherwise), with the paths as text too, one per line, so that a text field pastes them, 2026-10-23), "Cut <name>" or "Copied <name>", "Cut n items" or "Copied n items",
  so that File Explorer can paste them and axFile can paste what File Explorer cut or copied (cut and copy touch no
  disk and have no line, AF-9). Paste copies the clipboard's files into the folder shown, or moves them after a cut,
  through the shell (`SHFileOperationW`, undo allowed; the names are chosen first, `FileOperations.Destinations`, a
  name already there becoming "name - 2", "name - 3", one destination per source with `FOF_MULTIDESTFILES`, rather
  than a question, nothing overwritten; `FileOperations.CopyOrMoveAsync`, tested): "Pasted <name>" or "Pasted n
  items", the first one selected, the line "Copied n items" or "Moved n items"; "Pasting, please wait" when it takes
  longer than a second; after a move the clipboard is cleared, as File Explorer does. "Nothing to paste" without
  files on the clipboard; "Already in this folder" for a cut entry pasted where it is; "Already pasting" for a cut
  whose files a paste is still moving (AF-4.5).
- AF-4.9 **Open in Axit ▸**: one item per app of the bundle that opens a path (`Dispatch.Apps` whose `Opens` is not
  Nothing: the utilities, axStart, axClip, axCalc and axProcess, take none), in the bundle's groups with their
  separators (AX-9.2), a new window of that app on the entry by what it opens (`Dispatch.PathFor`): axDown, axJSON,
  axPreview and axPass on the file ("Not a file" on a folder), axMedia on the file with its folder's media files after
  it, or on a folder's (axMedia hands them to its open window itself), axGIT on the repository that holds the entry,
  axFile on the folder (or the file in its folder), axClaude, axTerminal and axTask on the folder, or on the entry's
  folder for a file; Editor (Ctrl+E), the submenu's first item, is the quick way to the right editor.
- AF-4.10 (Ctrl+I inside the open context menu) went with the context menu on 2026-10-23 (AF-D13): the bar's Item
  menu is a standard drop-down and does not take it; Ctrl+I in the list says the entry.
- AF-4.11 **The user's own programs in Open with ▸** (asked 2026-10-21): a list of programs and scripts, kept in the
  settings (`programs`, a name and a path each, AF-8.1), shown under Windows default and Choose a program… (AF-4.7)
  as "1. name (path)" (AX-9.5), or "No programs of your own yet" while the list is empty, then **Add a program…**
  and **Remove a program ▸**. Add a program… opens the
  Windows file picker on programs and scripts (`.exe`, `.cmd`, `.bat`, `.ps1`, `.py`; every file behind the second
  filter), then the notice "Add a program" with the field "Name" holding the file's name without its extension and
  the buttons Add and Cancel; "Added <name>". Remove a program ▸ lists them by name; choosing one takes it off,
  "Removed <name>". Choosing a program runs it once on the selected entries, or the current one (AF-3.7), every path
  an argument, in the first entry's folder: a `.ps1` through Windows PowerShell by its full path (`-NoProfile
  -ExecutionPolicy Bypass -File <script>`), since Windows opens a script file in an editor; a `.bat` or `.cmd` program
  is refused an entry whose name holds one of cmd.exe's special characters (`& | < > ^ % ! ( ) , ; =` or a quote),
  which cmd would run as a command ("<name> not started", security audit S-3); anything else as the shell starts it
  (`ShellExecute` with the arguments, so a batch file and a script whose type has a program registered run too);
  "Started <name>", or "Failed: <reason>" when it cannot start (the file gone, no program for the type). The list is
  refilled each time the submenu opens, so two windows see each other's additions after a restart only (the settings
  are the window's, AF-8.1). It is the shape the user-defined shells of axTerminal are to take (docs/axit/plan.md):
  a list in the app's settings, added through the file picker and a notice for the name, removed from a submenu.
  `UserPrograms` in `AxFile.Core` is tested; the menu and the start are the window's.
- AF-4.12 **Compress to zip, Extract here** (the Item menu; asked 2026-10-21), through .NET's own `ZipFile`, no
  package (`Archives` in `AxFile.Core`, tested). **Compress to zip** puts the selected entries, or the current one
  (AF-3.7), into a new zip in the folder shown, named after the one entry without its extension ("notes.zip") or
  after the folder for several ("Dune.zip"), "name - 2.zip" when the name is taken (the name is taken the moment the
  compress starts, so two at once never share one), a folder's contents under its name; "Compressed to <name>", the
  zip selected; "Compressing, please wait" when it passes a second, the line
  counting the files meanwhile (AF-9.2). **Extract here** on a `.zip` makes a new folder named after it next to it
  ("Dune", or "Dune - 2" when taken) and extracts into it, so that nothing is written over; "Extracted to <name>",
  the folder selected; "Not a zip file" on anything else. A failure is "Failed: <reason>" (AF-4.5). Other archive
  kinds are the shell's (Windows menu…) or an installed program's (AF-4.11).
- AF-4.13 **Move to folder…** (the Item menu; asked 2026-10-21, "like zip but not compressed"; named Move to new
  folder until 2026-10-23, when the user asked for Go to folder's completion in it): the notice "Move to folder" with
  the field "Folder" and the buttons Move and Cancel, its text naming what moves ("a.txt will move into the folder
  typed …"). Down and Up complete the text as in Go to folder (AF-3.3), folders and drives alone, Right going into a
  completed folder. Move: a folder that exists (a name here, a relative or a full path) takes the selected entries, or
  the current one (AF-3.7); any other name is a new folder made here first (the name rules and "Reads the same as …"
  of New folder); a path to a folder that does not exist elsewhere is "Not found: <text>"; a folder moved into itself
  or its own subfolder is "Cannot move a folder into itself". The move is the shell's (AF-4.8, undo allowed, names
  chosen first); "Moved <name> into <folder>" or "Moved n items into <folder>", the folder selected when it is here.
- AF-4.14 **Undo** (asked 2026-10-21). **Ctrl+Z** (Folder → Undo <operation>, named after what it would undo, "Undo
  rename") takes back the newest file operation of the Actions list that can be taken back (AF-9.3; never a change of the
  marks, which is taken back on its own line only, decided 2026-10-22, so that Ctrl+Z after a paste does not unmark
  an entry instead), one per press, and selects the entry it touched: Rename, renamed back; New folder and New text file, deleted while still empty ("Not undone:
  notes has files now" otherwise); a paste that moved and Move to folder, moved back where they were, the new
  folder removed when it is empty; a paste that copied, Compress to zip and Extract here, the copies, the zip or the
  extracted folder sent to the Recycle Bin; Delete, brought back from the Recycle Bin, the shell's own undelete on the
  Recycle Bin's item, the one step that needs the shell again (the bin's items are matched by the original path their
  `$I` record holds, the latest deletion of each, and restored in one Restore command; "Not in the Recycle Bin any
  more" when none is there). "Undone: <what>" each time ("Undone: renamed back to <name>", "Undone: <name> removed",
  "Undone: moved back", "Undone: <what> sent to the Recycle Bin", "Undone: <what> restored"), the line reading ",
  undone" afterwards; an entry changed since by anything else (renamed in File Explorer meanwhile), "Not undone:
  <name> has changed", "Not undone: <name> has files now" / "has content now" (a new folder or file filled
  meanwhile), "Not undone: <name> is already here" (the place it would go back to is taken; a rename that changed
  only the case is not "already here"), the line saying so and dropped from what Undo takes next, as is a delete
  whose entries are "Not in the Recycle Bin any more" (emptied meanwhile); a failure of the undo itself is "Failed:
  <reason>" (AF-4.5) and the line stays to try again (a zip open in another program, closed and Ctrl+Z again); a second Ctrl+Z while one runs says "Undo is still running"; in the rename box Ctrl+Z is the
  box's own (AF-6.2). Eject, Open, the hand-overs, Favorites and the settings are not operations and have no undo;
  there is no redo. The last twenty operations of this window can be undone, gone with the window. The lines and the
  operations are `ActionLog`, `Operation` and `FileOperations.CheckUndo` and `UndoAsync` in `AxFile.Core`, tested
  over a temporary folder; the Recycle Bin's records are `RecycleBin`, tested on synthetic records; the Restore itself
  is `Shell.Undelete`, checked by a console run on this machine.

### AF-5 Sound, video and reading: the hand-overs

axFile plays nothing and shows no text (G3, AF-D2): it starts the app on the paths, as the Axit menu starts a window,
and the started process finds its open window and hands the paths over with the bundle's window message
(`Axit.Forms/AppMessage`; axMedia AM-1.2, axPreview AP-1.2). Nothing of it is on the network (G6).

- AF-5.1 **Play folder in axMedia** (Ctrl+Enter in the list; the Item menu): axMedia on the folder shown, its sound
  and video files in name order from the first, the axMedia window in front; "No sound or video files here" when the
  list shows none (`MediaFiles.IsMedia` over the shown entries); "Choose a drive first" at This PC.
- AF-5.2 **Add to axMedia's queue** (Ctrl+Shift+Q in the list; the Item menu): the entry, or the marked entries
  (AF-3.7), to the end of axMedia's queue, a folder as its media files, with `--add`; the focus stays here and axFile
  says "Added n to axMedia's queue", n being its own count of the media files among the entries (a folder read on a
  task, the hidden ones as the settings say); "No sound or video files to add" when the count is none. With no axMedia
  window open, one opens behind this window, without taking the focus (AM-D3), holding the queue for later.
- AF-5.3 **Preview** (Shift+Enter in the list; the Item menu): the file read in axPreview, the window in front; a
  folder in a new axFile window (AF-3.2). axPreview says "Not a text file" itself for a file that is none.
- AF-5.4 **Enter** on a sound or video file is Windows' default program (AF-3.2): axMedia when the user has made it
  so, which plays the file with its folder's other media files after it (AM-D2).

### AF-6 Menus, keys, texts

- AF-6.1 Menus, in the standard order of a first menu (AX-6): **Folder (F):** New window Ctrl+N | Open folder… Ctrl+O |
  Open recent folder ▸ | Favorites ▸ (1. name (path) … or No favorites yet | ─ | Add or remove this folder Ctrl+D) |
  Drives ▸ (1. Local Disk (C:) … | ─ | This PC | ─ | Eject this drive | Drive properties; AF-3.11) | Go to folder…
  Ctrl+G | ─ | Up one level Backspace | Refresh F5 | Undo <operation> Ctrl+Z (AF-4.14) | Actions
  (AF-9.1) | ─ | Sort by ▸ (Name ✓ | Date | Size) | Filter… Ctrl+Shift+F | ─ | Show current folder in ▸ (File Explorer
  Ctrl+W | axClaude | axTerminal | axGIT) | ─ | Exit Alt+F4. **Item (I):** Open Enter | Open with ▸ (Windows default |
  Choose a program… | ─ | 1. name (path) … or No programs of your own yet | Add a program… | Remove a program ▸) |
  Open in Axit ▸ (Editor Ctrl+E | ─ | axClaude | axTerminal | axGIT | ─ | axFile | axDown | axJSON | axPreview | axMedia | ─ | axTask | axPass) |
  Preview Shift+Enter | Play folder in axMedia Ctrl+Enter | Add to axMedia's queue Ctrl+Shift+Q | ─ | Cut Ctrl+X |
  Copy Ctrl+C | Paste Ctrl+V | ─ | Rename F2 | Delete Delete | ─ | New folder… Ctrl+Shift+N | New text file
  Ctrl+Shift+T | Move to folder… | Compress to zip | Extract here | ─ | Copy path Ctrl+Shift+C | Show in File
  Explorer Ctrl+Shift+W | Information Ctrl+I | Properties Alt+Enter | ─ | Windows menu…; the same menu is the list's
  context menu (AF-4.6). **Selection (S):** Select all Ctrl+A | Deselect all Ctrl+Shift+A | Select or deselect this
  entry Space | Select to here Shift+Space | ─ | Cut Ctrl+X | Copy Ctrl+C | Delete Delete | ─ | Move to folder… |
  Compress to zip | Add to axMedia's queue Ctrl+Shift+Q | Copy path Ctrl+Shift+C (AF-3.7; the operations on the marks
  alone). **Options (O):** Text ▸ (Font… | Larger text Ctrl+Plus | Smaller text Ctrl+Minus | Windows text size) |
  General ▸ (Check for updates at start ✓ | Show hidden files and folders | Show files and folders starting with a
  period) | Developer ▸ (Open settings file | Copy diagnostics). **Axit (A)** (AX-9). **Help (H):** Keyboard
  shortcuts F1 | User guide | ─ | File resources ▸ (Keyboard shortcuts in Windows, the Microsoft page, in the
  browser). Every item has a mnemonic (AX-7.4).
- AF-6.2 Keys (AX-8). Bundle-wide: F1, the control keys (Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6, between
  the folder list and the Actions list, AF-9.1), Ctrl+F, F3, Shift+F3, Ctrl+S ("Nothing to save here"), Ctrl+Plus,
  Ctrl+Minus, Page Up and Page Down (the list's own here), Alt+F4. Window-specific (`AxFileKeys`): Enter,
  Shift+Enter, Backspace, Alt+Up, Left, Right, F5, F2, Delete, Ctrl+N, Ctrl+O, Ctrl+G, Alt+D, Ctrl+D, Ctrl+W, Ctrl+Shift+W, Ctrl+E,
  Ctrl+I, Ctrl+Shift+N, Ctrl+Shift+T, Ctrl+Shift+C, Ctrl+Shift+F, Escape, Shift+Up, Shift+Down, Shift+Home,
  Shift+End, Space, Shift+Space, Ctrl+A, Ctrl+Shift+A, Ctrl+Enter, Ctrl+Shift+Q (AF-5), Shift+F10, the Applications key,
  Alt+Enter, Ctrl+Z (AF-4.14), Ctrl+X, Ctrl+C, Ctrl+V. Enter, Shift+Enter, Backspace, Left, Right, Space, Escape, F2, Delete,
  Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A, Ctrl+Shift+A, Shift+Space, Ctrl+Enter, Ctrl+Shift+Q, the Shift arrows, Shift+F10 and the
  Applications key act only while the folder list has the focus (in the Actions list, Enter, Escape, Ctrl+Z and Ctrl+I
  act on its line, AF-9.3; in a notice's field or the rename box they are the field's; the two menu keys open the
  bar's Item menu once they are up, AF-D13). The collision test covers the table and lists the keys other windows use
  otherwise (Ctrl+N, Ctrl+G, Ctrl+E, Ctrl+C, Ctrl+Shift+C, Ctrl+Shift+F, Ctrl+Enter).
- AF-6.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axfile/user-guide.md` is embedded (Help → User
  guide); About is the bundle's (AX-9.9).

### AF-7 Announcements

One UI Automation notification each, on the list that has the focus, or on the notice while one shows (AX-7.3):
"<folder>, <N items>, <entry>" (a change of folder, AF-3.1), "<N of M items, filter text>, <entry>" (AF-3.8), "Created
<name>", "Moved <what> into <name>" (AF-4.13), "Renamed to <name>", "Deleted <name>", "Deleted n items",
"Copied path", "Copied n paths", "Cut <name>", "Copied <name>", "Cut n items", "Copied n items", "Selected",
"Deselected", "Selected, n selected", "Deselected, n selected", "n selected", "None selected" (AF-3.7), "Sorted by
<order>", "Filter cleared" (AF-3.8), "Added to Favorites", "Removed from Favorites" (AF-3.9), "Top" (This PC,
AF-3.10), "Ejecting <name>" (AF-3.11), "Added <name>", "Removed <name>", "Started <name>" (AF-4.11), "Compressed to
<name>", "Extracted to <name>", "Compressing, please wait", "Extracting, please wait" (AF-4.12), "Pasted <name>",
"Pasted n items", "Pasted n items, m already pasting" (AF-4.5), "Pasting, please wait" (a paste longer than a second), "Deleting, please wait", "Moving, please wait" (the same, AF-4.5), "Added n to axMedia's queue", "No sound or
video files here", "No sound or video files to add" (AF-5), "Undone: …", "Not undone: …", "Nothing to undo", "Undo is
still running" (AF-4.14), "Undone: n selected", "Undone: none selected" (a selection line, AF-9.3), "No actions yet",
"Still running", "Already undone", "Nothing to undo here", "<section>, n of m, hh:mm", "Running", "Operations", "Selection" (a header line),
"Folder not found" (the Actions list, AF-9.3), "Refreshed", "Not found: <path>", "Failed: <message>", "<name>, <size>,
modified <date>" (Ctrl+I), "Not found" (find), "Nothing to save here", "Text size n", "Windows text size", "Copied
diagnostics", "<name>, <size>, modified <date>, n selected" (Ctrl+I with marks), "Failed: In use by another program: a
terminal or an app runs in it, or the file is open", "Failed: Access denied: a file inside is open in another program,
or Windows protects the place" (AF-4.5); and the guards "No folder" (nothing
listed yet), "No items in this folder" (a command on an empty folder's one item, AF-2.2), "No names contain <text>"
(the same under a filter, AF-3.8), "Not shown: …" (the same on the list's last line, AF-2.2), "Nothing selected",
"Choose a drive first" (a folder command at This PC, AF-3.10), "Not a file" (Ctrl+E or Open with… on a folder), "Not
a zip file" (Extract here, AF-4.12), "Name unchanged" (a rename to the same name), "Nothing to paste" (no files on the
clipboard), "Already in this folder" (a cut entry pasted where it is), "Already pasting" (a cut still on its way,
AF-4.5), "Reads the same as <name>, which is already here" (AF-4.2), "Type a name" and the other reasons of `FileOperations.CheckName`. A new selection is NVDA's own
reading. One earcon, the edge (`Sounds.Edge`, one short low note): a key that moves nothing at either end of a list,
and a typed name no entry starts with (AF-3.6).

### AF-8 Settings, log, status

- AF-8.1 `%APPDATA%\Axit\axfile.json` (AX-5.3; `FileSettings` on `WindowSettings`): `lastFolder`, `recentFolders`
  (ten), `favorites` (the pinned folders in the order added, AF-3.9; blanks and repeats dropped), `showHidden` off,
  `showDotFiles` off (AF-2.2), `sortBy` name (`name`, `date` or `size`, AF-3.8), `programs` (the user's own, each a
  `name` and a `path`, AF-4.11; an entry without either is dropped), and the window base's font, update check and
  window fields. The player's keys of an older file (`speed`, `seekSeconds`, `sayEachFile`) are axMedia's now
  (`axmedia.json`, with its own defaults) and are ignored here.
- AF-8.2 The log is `%LOCALAPPDATA%\Axit\logs\axfile.log`: the start, the exit, the errors of the operations with
  Windows' message; no names of files or folders.

### AF-9 The Actions list

- AF-9.1 **What it is.** A second list under the folder list, "Actions", with every file operation of this window and
  every change of its marks (AF-3.7) as one line, in three sections (AF-9.2), so that what was done can be read again,
  taken back and walked back to. It is there from the first
  moment, with the one line "No actions yet" until the first operation (AX-9.5; asked 2026-10-22: a pane that appears
  later leaves Tab and Shift+Tab with nowhere to go), hidden only while a notice shows; a third of the window at
  most; **Tab**, **Shift+Tab**, **Ctrl+Tab**, **Ctrl+Shift+Tab** and **F6** move between the folder list and it
  (AX-8.2), **Escape** in it goes back to the folder list, Folder → **Actions** puts the focus in it. One list, not tabs: NVDA reads a list and its place in it, and the two lists are reached with the
  keys every Axit window uses for its controls (AF-D1). Each command's result is one announcement on the list that has
  the focus (AF-7); nothing is spoken on an arrow key (AF-D7). The newest line is the status bar's left text (AF-2.3).
- AF-9.2 **The lines.** Three sections, each a header line ("Running", "Operations", "Selection") over its lines, a
  section present only while it has lines (asked 2026-10-22; AF-D11): **Running**, the operations under way, pinned at
  the top so that what is going on is one Tab and Home away; **Operations**, the file operations done, failed or undone;
  **Selection**, the changes of the marks: "Selected notes.md", "Deselected notes.md", "Selected 5 items" (a Shift range
  or Shift+Space, one line for the whole range, rewritten as it grows or shrinks, counting the entries from its start
  to its end), "Selected all, 24 items", "Deselected all, 3 items" (the count unmarked; no line when nothing changed),
  each taking back the marks as they were before it (AF-9.3); the marks live in one folder (AF-3.7), so the section
  empties with a change of folder, and keeps its last twenty lines. Within a section the lines are in time order,
  newest last; "No actions yet" is the one line while all three are empty. The operations' lines, the last fifty kept,
  gone with the window: "Renamed notes.md to
  todo.md", "Created temp", "Created New file.txt", "Moved 3 items into bundle", "Copied 3 items", "Moved 2 items",
  "Compressed to docs.zip", "Extracted to docs - 2", "Deleted notes.md", "Deleted 2 items". A running operation reads
  what it is doing, "Copying 3 items", "Deleting 2 items", "Compressing, 40 of 120 files" (a few times a second at
  most; a paste through the shell has no count to give), and its line is replaced when it ends; a failure, "Failed:
  <reason>"; a line that was undone, "Renamed notes.md to todo.md, undone"; one that could not be undone when asked,
  "…, not undone: notes.md has changed". When a line is added while the focus is in the folder list, it is the
  selected line of the Actions list, so that Tab lands on it; while the focus is in the Actions list the line the user
  is on stays selected (by line, not by place, since at fifty the oldest falls off) and only the changed line is
  rewritten, a running line under the focus being left until it ends, so that a zip's count is not reselected under
  NVDA four times a second. Nothing is spoken for a line (the operation's own
  announcement, AF-7, is what is heard). Cut, Copy, Copy path, Open, the hand-overs and Favorites change nothing on
  disk and have no line. The status bar shows the newest operation's line, never a selection's (AF-2.3).
- AF-9.3 **On a line.** **Enter** goes where the action left things: the folder list shows the folder and selects the
  entry the action made, renamed or moved (the new folder for a move into one, the first entry for a paste; the folder
  alone for a delete), and takes the focus, before the folder is said (AF-3.1), so that NVDA's reading of the list
  does not cut the announcement short (2026-10-23); once the line is undone, where the undo put things back; "Still running"
  on a running line, "Folder not found" when the place is gone. **Ctrl+Z** takes that line back, whatever its place,
  with AF-4.14's checks and texts; a line that cannot be says why: "Still running", "Already undone", "Not undone:
  <reason>" again, or "Nothing to undo here" (a failed line, or one older than the twenty undoable). Ctrl+Z in the
  folder list takes the newest undoable operation (AF-4.14), never a selection line. On a selection line, Ctrl+Z puts
  the marks back as they were before it, in the folder shown (the section empties with another folder, so the line is
  always of this one; entries gone since are skipped), "Undone: 3 selected" or "Undone: none selected", the line reading
  ", undone"; Enter on it is the folder list on the entry it marked, or where the user was. **Ctrl+I** says the line's
  section, number in it and time, "Operations, 3 of 12, 21:04". On a header line and on "No actions yet", Enter, Ctrl+Z
  and Ctrl+I say the line. Delete does nothing (an action is a record, not an
  entry). There is no redo.
- AF-9.4 **Later uses**, noted so that the shape allows them: the matches of a search through a folder's tree, a
  folder's summary on Ctrl+I over a folder (its counts and size, which take a walk of the tree). None is in the
  first build.

## 5. Architecture

```
src/AxFile.Core/    class library (net10.0): FolderListing (a folder's entries as records, folders first in natural
                    order, hidden left out; where a typed name jumps, the editor for a file; the list's name, the
                    information line, the parent, a typed path, the drives of This PC), FileOperations (the name
                    check, new folder, rename, delete to the Recycle Bin, copy and move into a folder, the last three
                    through SHFileOperationW, the undo checks and inverses, the messages), Operation and
                    OperationKind (one operation with what its undo needs and where it left things), ActionLog and
                    ActionEntry (the Actions list's lines and states, the limits), Marks and MarkChange (the marks on the
                    shown entries and the Shift range, each key one change; AF-3.7), RecycleBin (the $I records),
                    UserPrograms and UserProgram (the user's own programs; AF-4.11), Archives (a zip from entries, a
                    zip into a new folder, free names; AF-4.12), FileSettings; references Axit.Core (MediaFiles for
                    the hand-over counts, NaturalOrder, Dispatch for the editor's verb)
src/AxFile/         class library (net10.0-windows): AxFileApp (Run, Usage), FileForm (the window, on AxitForm, in
                    four partial files: the controls, menus, hooks and keys; Browse; Operations; Actions), AxFileKeys,
                    HelpText (F1 text, the embedded guide), Shell (Windows' own ways with an entry: the
                    Open with dialog, the Properties sheet, the shell's context menu through IShellFolder and
                    IContextMenu, Eject, the Recycle Bin's Restore; AF-4.7); references AxFile.Core, Axit.Core, Axit.Forms
tests/AxFile.Tests/ FolderListingTests, MarksTests, FileOperationsTests, UndoTests, ArchivesTests, PathCompletionTests, ActionLogTests,
                    UserProgramsTests and FileSettingsTests over temporary folders (the
                    Recycle Bin is not exercised), ActionLogTests, UserProgramsTests, FileSettingsTests
docs/axfile/        this spec, user-guide.md (embedded), nvda-test-plan.md
```

`FileForm` derives from the window base (AX-6.5): the notices, the fonts, the window's place, find in a list and the
update flow are the base's. The folder reads and the operations run on background tasks, the shell's delete on a
single-threaded-apartment thread of its own (AX-7.5). The player seam, the queue and the text rule live in AxMedia.Core
and AxPreview.Core since the split; `MediaFiles` and `NaturalOrder` moved to `Axit.Core` on their second use.

## 6. Design decisions

- **AF-D1 One list, no tree; two lists, no tabs.** A tree view asks a screen reader user to track depth and expansion;
  a list of one folder with Backspace for up is what File Explorer's main pane is, and NVDA reads a list box best. The
  path in the status bar is the depth. The Actions list is a second list under it rather than a tab control: the
  bundle's control keys already move between a window's controls, and a tab control would be a third thing to learn.
- **AF-D2 Enter is Windows'; playing and reading are other apps'.** The first build routed Enter itself (`.md` to
  axDown, `.json` to axJSON, media to its own player) and carried the player and later a preview pane as features of
  this window (the first AF-D2: a folder's files are a playlist and the list was already here). The user's direction
  of 2026-10-22: axFile is file operations and management only; playing is axMedia's and viewing axPreview's, each an
  app with a tight hand-over; and Enter on a file opens it in the program the user has made its default in Windows,
  exactly as a double click in File Explorer does, the Axit apps registering as programs for their kinds so that they
  can be chosen there (an installer cannot set a default: Windows lets only the user). The costs are known and
  accepted: a second window for the sound, and a Windows setting to make before Enter plays in axMedia. The gains:
  one player for the whole bundle, a viewer with the whole window and the editors' keys, a file manager with one job,
  and a list that mirrors what Explorer does. Shift+Enter is the viewer's own key, since it has no other meaning on a
  file in a list; the hand-overs are one window message (`AppMessage`, AM-D4), nothing on the network.
- **AF-D3 The Actions list over a history.** Undo came first as a hidden history of twenty operations (2026-10-21);
  the user's ask of 2026-10-22, a list of every operation taken, to review, undo and jump to, made the history
  visible: one line per operation with its state, Ctrl+Z on any line, Enter to its place. The list is the record,
  not a second folder view: it holds text, and the folder list does the showing.
- **AF-D5 Streamlined operations.** New folder, rename, delete and copy the path were the first four; cut, copy and
  paste came at the user's ask on 2026-10-20, as files on the clipboard in File Explorer's own formats (`CF_HDROP`
  and the preferred drop effect) so that the two exchange them, with the shell doing the copy or the move (a dash and
  a number on a name already there rather than a question, §7); the zip, the move into a new folder and Undo on
  2026-10-21. File Explorer stays one key away for the rest (Ctrl+W, Ctrl+Shift+W).
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
  announcement of the app's own; a sighted helper sees the word.
- **AF-D10 The marks win; three menus name three targets.** Asked 2026-10-22, "how do you recommend we manage" the
  target of a key when some entries are marked and the current one is not, without File Explorer's modes. Offered: the
  entry you hear decides (a marked current entry takes the whole selection, an unmarked one itself alone, File
  Explorer's right-click rule, audible on every arrow key since the item says ", selected"); the marks win when there
  are any (the rule since 2026-10-21); the keys on the current entry only, the marks through the menu. The user chose
  to keep the marks winning, and the menus carry the rest: Item is the entry, Selection is the marks (its operations
  say "None selected" rather than fall back), Folder is the folder. The decision is recorded so that the first rule
  is not proposed again.
- **AF-D11 Sections as header lines in one list.** Asked 2026-10-22: the running operations pinned at the top, the
  operations next, the changes of the marks last. Three list boxes would be three Tab stops and a plain list cannot
  mark a group; so one list with a header line over each section that has lines, a header being an item NVDA reads
  like any other and Enter, Ctrl+Z and Ctrl+I answer with its name. The marks' changes are in the list, at the user's
  ask, but outside Ctrl+Z in the folder list (AF-4.14): a selection undone by the key meant for a paste would surprise.

- **AF-D12 No tree; the tree's keys on the list, and completion in Go to folder.** Asked 2026-10-22: a tree to drive
  through folders and drives, or an AxOpen app with a tree and typing. A tree (always, or as a pane beside the list as
  File Explorer has it) adds a control to lose one's place in, and its one gain of its own, selecting across folders,
  is rare. The flat list takes the tree's Left and Right instead, and typing a path gets completion where a path is
  typed, the Go to folder field. No separate app: the other windows reach axFile through Show current folder in; if
  an editor wants a quick-open, the completion moves to the shared pieces on its second use.

- **AF-D13 One Item menu, opened by every way.** 2026-10-23: the list's own context menu (Shift+F10, the Applications key, a right click) never read its first item the first time it opened, whether shown from code, with its first item selected, with a Down posted into it, or through the list's own WM_CONTEXTMENU (which leaves nothing selected), while Alt+I read the bar's Item menu every time. At the user's word the three ways open the bar's Item menu: the window posts SC_KEYMENU and then the letter I, as Alt and I typed, once Shift, F10 and the Applications key are up (with them down F10 closed the menu and the letter was Shift+I, a beep). Ctrl+I inside the menu (AF-4.10) went with the context menu.

AF-D4 (the volume) and AF-D6 (the queue fixed at Enter) went to axMedia with the player (AM-D6, AM-D2).

## 7. Open questions

- A question on a name collision when pasting (replace, keep both, skip), where axFile now adds a dash and a number
  (AF-4.8); a paste of many items that names them.
- Shift+Delete, a permanent delete, after its own question.
- A "media only" filter of the list (the name filter and the sort orders came 2026-10-21, AF-3.8).
- Keeping the Actions list for the next start of the window on the same folder.

## 8. Status

- Planned 2026-10-18 at the user's ask (as axMedia, then as axFile's player).
- Built 2026-10-19 as a first draft, on the branch `axfile`: the window, the four operations, the player on MFPlay, the
  wiring into the bundle, the tests of the pure code, the guide and the test plan.
- 2026-10-20: the entry's menu (AF-4.6) and Windows' own ways (AF-4.7, AF-D8); File operations with cut, copy and
  paste (AF-4.8), the rename in place (AF-4.2), Open in Axit ▸ (AF-4.9), Enter's fallback to the Open with dialog
  (AF-3.2, AF-4.7), Ctrl+I inside the open menu (AF-4.10).
- Released as 3.1.0 on 2026-10-21; the user's NVDA pass of the whole test plan passed the same day.
- 2026-10-21, after the release: the one item of an empty folder (AF-2.2), Ctrl+N (AF-3.3), the selection with Space
  (AF-3.7, AF-D9), the user's own programs (AF-4.11), Sort by and the filter (AF-3.8), Favorites (AF-3.9), This PC
  (AF-3.10), Compress to zip and Extract here (AF-4.12), the list named by its count (AF-2.1), the two switches with
  the not-shown line (AF-2.2), "name - 2" (AF-4.8), New text file (AF-4.1), Move to folder… (AF-4.13), the entry's
  menu as one flat list (AF-4.6, AX-6.6), the Drives menu (AF-3.11); then the queue pane, Undo (AF-4.14), the preview
  pane, the lookalike guard (AF-4.2) and the Shift range (AF-3.7); released in 3.2.0, 3.2.1 and 3.2.2. Not yet checked
  with NVDA from Ctrl+N on.
- 2026-10-22: the split, on the branch `split` (`docs/axit/axfile-split.md`): the player became axMedia and the
  preview axPreview; Enter follows Windows' default program (AF-D2); the pane became the Actions list (AF-9, AF-D3)
  with Enter to the action's place and Ctrl+Z on a line, at the user's ask; the Player menu, its keys, the video area,
  the queue and the settings `speed`, `seekSeconds` and `sayEachFile` left the window; `MediaFiles` and `NaturalOrder`
  moved to `Axit.Core`. The window shrank by a third. Not yet checked with NVDA (test plan sections 3.14, 4 and 6).
- 2026-10-22, later: the operations side by side (AF-4.5), at the user's ask: a second paste chooses its names around
  a running one, a cut on its way says "Already pasting", a compress takes its name at once; the process's working
  directory moved off the user's folders (AF-1.2) and Windows' "being used by another process" said with its cause,
  after the user found a folder renamed once could not be renamed again: verified by experiment that a process whose
  current directory the folder is (a terminal or an app started in it) is what refuses, and that axFile itself holds
  nothing (renaming twice in a row works, test `A_folder_is_renamed_twice_in_a_row_and_a_current_directory_says_in_use`).
- 2026-10-22, later: a change of folder announced with the folder's name, count and entry (AF-3.1), "First item" and
  "Last item" at the ends of either list (AF-3.6), the filter's name and entry said (AF-3.8), at the user's ask: the
  entry alone did not tell the folder landed in from the folder under the focus, and silence at an end told nothing;
  then File Explorer's type-ahead, several letters into one prefix (AF-3.6), at the user's ask.
- 2026-10-22, later: the edge earcon in place of "First item", "Last item" and "No names start with" (AF-3.6, the
  bundle's `Sounds.Edge`), the Actions list there from the start with "No actions yet" (AF-9.1), so that Tab and
  Shift+Tab always have the second control, and a pass over the F1 text, the guide and the notes to put every feature
  in one place each, at the user's ask ("I have lost track of all the features").
- 2026-10-22, later: Shift+Space (AF-3.7), the Selection menu with the operations on the marks (AF-3.7, AF-6.1,
  AF-D10), the changes of the marks as lines of the Actions list, taken back on their line (AF-9.2, AF-9.3), and the
  list in three sections under header lines, the running operations first (AF-9.2, AF-D11), at the user's ask;
  specified first, built the same day. Not yet checked with NVDA (test plan 3.7b, 6.8, 6.9).
- 2026-10-22, later: Escape deselects (AF-3.7); Left and Right as a tree's keys and completion in the Go to folder
  field (AF-3.1, AF-3.3, AF-D12), at the user's ask; then the Drives menu as a submenu of Folder and Alt+D for Go to
  folder (AF-3.11, AF-6.1). Not yet checked with NVDA (test plan 1.4b, 1.7, 1.12a, 3.7a).
- 2026-10-22: released in 3.3.1 (3.3.0 tagged, not published). The whole NVDA test plan marked passed at the user's word, no step reported failing; the record by step is `docs/axit/nvda-test-record.md`.
