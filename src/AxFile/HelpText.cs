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
          Ctrl+F: find an entry whose name contains the text. F3 and Shift+F3: the next or the previous match
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close AxFile
          Alt+A: the Axit menu: a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, Check for updates, and About

        The list
          Arrow keys, Home, End, Page Up, Page Down: move through the entries; a typed letter jumps to the next name starting with it
          Enter: open the entry: a folder in this list, a Markdown or text file in AxDown, a JSON file in AxJSON, a sound or video file in the player, anything else in the program Windows has for it, or the Windows dialog that picks one when there is none
          Backspace, or Alt+Up: the folder above, with the folder you left selected
          Ctrl+I: say the entry's size and date
          Ctrl+E: open the file in AxDown, or AxJSON for a .json file, whatever Enter would do
          Ctrl+Shift+W: show the entry in File Explorer. Ctrl+W: show the folder in File Explorer
          Ctrl+O: open a folder with the Windows folder picker. Ctrl+G: go to a typed path, %USERPROFILE%\Downloads for instance
          Ctrl+N: a new AxFile window on this folder
          F5: read the folder again
          Ctrl+Shift+N: a new folder, after its name. F2: rename in place, in a box over the entry with the name before the extension selected; Enter renames, Escape keeps the name. Delete: delete to the Recycle Bin, after a question
          Space: select the entry, or deselect it; it reads "selected" after its name while it is. Ctrl+A: select all. Ctrl+Shift+A: select none. Ctrl+I says how many are selected
          Ctrl+X, Ctrl+C, Ctrl+V: cut, copy or paste as files, to and from File Explorer too; a pasted name already in the folder gets a number. Cut, Copy, Delete and Copy path act on the selected entries, or on the entry you are on when none is selected
          Ctrl+Shift+C: copy the entry's full path, or every selected entry's, one per line
          Shift+F10, or the Applications key: the entry's menu: File operations, Open, Open in editor, Open in Axit, Open with Windows, Open with another program, Show in File Explorer, Information, Properties, More from Windows. Ctrl+I inside the menu says the entry without closing it
          Alt+Enter: the entry's Properties, the Windows sheet
          Ctrl+S: nothing to save here; AxFile says so

        The player
          Enter on a sound or video file plays it, and the folder's other media files follow in order
          Ctrl+P: pause, or resume
          Ctrl+Right, Ctrl+Left: 15 seconds forward or back. Ctrl+T: go to a time, 12:40 or 1:02:10 or seconds
          Ctrl+Shift+Right, Ctrl+Shift+Left: the next or the previous file of the folder
          Ctrl+Shift+Period, Ctrl+Shift+Comma: faster or slower by a quarter; Player, Normal speed goes back to 1
          Ctrl+Q: what is playing, where it is, and the speed
          The volume is Windows' own: your keyboard's volume keys

        Notices
          AxFile's questions, errors and help texts appear inside the window, in place of the list.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice.
        """;

    /// <summary>docs/axfile/user-guide.md, embedded by AxFile.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axfile/user-guide.md");
}
