using System.Reflection;
using Axit.Core;

namespace AxFile;

/// <summary>The texts AxFile shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AF-6.3). Bundle-wide keys first, then AxFile's own.</summary>
    public const string Shortcuts = """
        In every window of Axit
          F1: this list
          Tab, Shift+Tab, Ctrl+Tab or F6: between the folder list and the Actions list, once there is one
          Ctrl+F: find an entry whose name contains the text. F3 and Shift+F3: the next or the previous match
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close AxFile
          Alt+A: the Axit menu: a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, Check for updates, and About

        The folder list
          Arrow keys, Home, End, Page Up, Page Down: move through the entries; a typed letter jumps to the next name starting with it
          Enter: open the entry: a folder in this list, a file in the program Windows has for it (the one you made its default: AxDown, AxMedia, AxPreview or any other), or the Windows dialog that picks one when there is none
          Shift+Enter: read a file in AxPreview; a folder opens in a new AxFile window
          Ctrl+E: open the file in AxDown, or AxJSON for a .json file
          Ctrl+Enter: play the folder's sound and video files in AxMedia. Ctrl+Shift+Q: add the entry, or the selected entries, a folder's media files included, to AxMedia's queue, staying here
          Backspace, or Alt+Up: the folder above, with the folder you left selected; from a drive's top, This PC with the drives
          Ctrl+D: add this folder to Favorites, or remove it; Folder, Favorites lists them by number
          Ctrl+I: say the entry's size and date; for a drive, the free space
          Ctrl+Shift+W: show the entry in File Explorer. Ctrl+W: show the folder in File Explorer
          Ctrl+O: open a folder with the Windows folder picker. Ctrl+G: go to a typed path, %USERPROFILE%\Downloads for instance
          Ctrl+N: a new AxFile window on this folder
          F5: read the folder again
          Ctrl+Shift+F: filter the list to the names containing a text; Escape in the list clears the filter. Folder, Sort by: name, date or size
          Space: select the entry, or deselect it; it reads "selected" after its name while it is. Shift+Down, Shift+Up, Shift+Home, Shift+End: select from where you started to there, as in File Explorer, keeping what was selected before. Ctrl+A: select all. Ctrl+Shift+A: deselect all; both in the Select menu with the Space toggle. Ctrl+I says how many are selected
          Shift+F10, or the Applications key: the entry's menu, one list in File Explorer's order: Open, Open with, Open in Axit, Preview, Play folder in AxMedia, Add to AxMedia's queue, Cut, Copy, Paste, Rename, Delete, New folder, New text file, Move to new folder, Compress to zip, Extract here, Copy path, Show in File Explorer, Information, Properties, Windows menu. Ctrl+I inside this menu says the entry without closing it
          Alt+Enter: the entry's Properties, the Windows sheet
          Ctrl+S: nothing to save here; AxFile says so

        File operations
          Ctrl+Shift+N: a new folder, after its name. Ctrl+Shift+T: a new empty text file, with the rename box open on it and its whole name selected: type the name with its extension, Enter. Move to new folder, in the entry's menu: a new folder here, after its name, with the entry, or the selected entries, moved into it
          F2: rename in place, in a box over the entry with the name before the extension selected; Enter renames, Escape keeps the name. Delete: delete to the Recycle Bin, after a question
          Ctrl+X, Ctrl+C, Ctrl+V: cut, copy or paste as files, to and from File Explorer too; a pasted name already in the folder gets a number. Cut, Copy, Delete and Copy path act on the selected entries, or on the entry you are on when none is selected
          Ctrl+Shift+C: copy the entry's full path, or every selected entry's, one per line
          In the entry's menu without a key: Move to new folder, Compress to zip, Extract here, Open with (Windows default, Choose a program, your own programs), Windows menu
          Ctrl+Z: undo the newest file operation of this window that can be undone, one per press, the last twenty: a rename renamed back, a new folder or file removed while still empty, a move moved back, a paste's copies, a zip or an extracted folder sent to the Recycle Bin, a delete restored from it; "Undone: …", or "Not undone: …" when the entry has changed since. Folder, Undo names what it would undo

        The Actions list
          A list under the folder list, "Actions", shown once an operation has run and kept; Tab, Ctrl+Tab or F6 moves between the folder list and it, Escape goes back to the folder list; Folder, Actions goes there
          One line per operation, newest last: "Renamed notes.md to todo.md", "Copied 3 items", "Deleted 2 items"; a running operation reads what it is doing, "Compressing, 40 of 120 files"; a failed one "Failed: …"; one taken back ends in "undone"
          Enter: go to the action's place, the folder list on the entry it made or moved. Ctrl+Z: undo this line, whatever its place. Ctrl+I: its number and time, "3 of 12, 21:04"

        Notices
          AxFile's questions, errors and help texts appear inside the window, in place of the list.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice.
        """;

    /// <summary>docs/axfile/user-guide.md, embedded by AxFile.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axfile/user-guide.md");
}
