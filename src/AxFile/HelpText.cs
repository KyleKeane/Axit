using System.Reflection;
using Axit.Core;

namespace AxFile;

/// <summary>The texts AxFile shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AF-6.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then AxFile's own by what they do.</summary>
    public const string Shortcuts = """
        In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match

        ## In every window of Axit
          F1: this list
          Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it
          Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab or F6: between the folder list and the Actions list
          Ctrl+F: find an entry whose name contains the text. F3 and Shift+F3: the next or the previous match
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close AxFile
          Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, Check for updates, and About

        ## Moving around
          Arrow keys, Home, End, Page Up, Page Down: through the entries. At the first or the last entry, a key that cannot move plays one short low note
          Type the start of a name, the letters within a second of each other: the first name starting with it, as in File Explorer; the same letter again, the next such name; the low note when no name starts with it
          Enter or Right Arrow on a folder: into it; Right on a file plays the low note and never opens it. Backspace, Left Arrow or Alt+Up: the folder above, the one you left selected; from a drive's top, This PC with the drives. Each says the folder, its count and the entry you land on: "docs, 5 items, plan.md"
          Ctrl+G or Alt+D: go to a typed path, %USERPROFILE%\Downloads for instance; in its field, Down and Up complete the path from what you typed, "Documents, folder, 2 of 5", and Right at the end of a completed folder goes into it. Ctrl+O: the Windows folder picker. Folder, Open recent folder and Folder, Favorites: by number. Ctrl+D: add this folder to Favorites, or remove it
          Ctrl+N: a new AxFile window on this folder. F5: read the folder again
          Ctrl+Shift+F: filter the list to the names containing a text, "3 of 24 items, filter chap"; Escape in the list clears it once nothing is selected. Folder, Sort by: name, date or size
          Folder, Drives: the drives by number, This PC, Eject this drive, Drive properties

        ## Opening
          Enter on a file: the program Windows has for it (the one you made its default: AxDown, AxMedia, AxPreview or any other), or the Windows dialog that picks one when there is none
          Shift+Enter: read the file in AxPreview; a folder opens in a new AxFile window
          Ctrl+E: edit the file in AxDown, or AxJSON for a .json file
          Ctrl+Enter: play the folder's sound and video files in AxMedia. Ctrl+Shift+Q: add the entry, or the selected entries, to AxMedia's queue, staying here
          Shift+F10, or the Applications key: the entry's menu, one list in File Explorer's order, with Open with (Windows default, Choose a program, your own programs), Open in Axit (every app) and Windows menu, the full Windows menu for the entry. Ctrl+I inside this menu says the entry without closing it
          Alt+Enter: the entry's Properties, the Windows sheet

        ## Selecting
          Space: select the entry, or deselect it; it reads "selected" after its name while it is
          Shift+Down, Shift+Up, Shift+Home, Shift+End: select from where you started to there, as in File Explorer, keeping what was selected before
          Shift+Space: select from the nearest selected entry to the one you are on, above or below; with none selected, this entry
          Ctrl+A: select all. Ctrl+Shift+A, or Escape: deselect all. Ctrl+I says how many are selected
          Cut, Copy, Delete, Copy path, Move to new folder, Compress to zip and Add to AxMedia's queue act on the selected entries, or on the entry you are on when none is selected
          Alt+S: the Selection menu, the selected entries from anywhere: select all, deselect all, select to here, then Cut, Copy, Delete, Move to new folder, Compress to zip, Add to AxMedia's queue and Copy paths on them alone; "None selected" when there are none
          Every change of the selection is a line in the Actions list's Selection section; Ctrl+Z on that line puts the selection back as it was

        ## File operations
          Ctrl+Shift+N: a new folder, after its name. Ctrl+Shift+T: a new empty text file, with the rename box open on its whole name: type the name with its extension, Enter
          F2: rename in place, in a box over the entry with the name before the extension selected; Enter renames, Escape keeps the name. Delete: to the Recycle Bin, after a question
          Ctrl+X, Ctrl+C, Ctrl+V: cut, copy or paste as files, to and from File Explorer too; a pasted name already in the folder gets a number. Ctrl+Shift+C: copy the entry's full path, or every selected entry's, one per line
          In the entry's menu: Move to new folder, Compress to zip, Extract here
          Ctrl+Z: undo the newest operation that can be undone, one per press, the last twenty: a rename renamed back, a new folder or file removed while still empty, a move moved back, a paste's copies or a zip sent to the Recycle Bin, a delete restored from it. Folder, Undo names what it would undo
          Every operation runs on its own: a second paste, delete or zip can start while the first runs

        ## Information
          Ctrl+I: the entry's size and date; for a drive, the free space
          Ctrl+Shift+W: show the entry in File Explorer. Ctrl+W: show the folder in File Explorer. Folder, Show current folder in: AxClaude, AxGit, AxTerminal
          NVDA+End: the status bar: the newest operation on the left, the folder's full path on the right
          Ctrl+S: nothing to save here; AxFile says so

        ## The Actions list
          Under the folder list, always; "No actions yet" until the first operation. Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab or F6 moves between the two lists, Escape goes back to the folder list, Folder, Actions goes there
          Three sections, each under a header line: Running, the operations under way; Operations, the ones done; Selection, the changes of the selection in this folder. Newest last in each
          One line per operation: "Renamed notes.md to todo.md", "Copied 3 items", "Compressing, 40 of 120 files" while it runs, "Failed: …", "…, undone" once taken back. One per change of the selection: "Selected notes.md", "Selected 5 items", "Deselected all, 3 items"
          Enter: go to where it happened, the folder list on the entry it made, moved or selected. Ctrl+Z: undo this line, whatever its place; Ctrl+Z in the folder list undoes operations only. Ctrl+I: its section, number and time, "Operations, 3 of 12, 21:04"

        ## Notices
          AxFile's questions, errors and help texts appear inside the window, in place of the lists.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice. In a notice's text, h, 1 to 6 and Ctrl+F work as in this one
        """;

    /// <summary>docs/axfile/user-guide.md, embedded by AxFile.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axfile/user-guide.md");
}
