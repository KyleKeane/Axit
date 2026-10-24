using System.Reflection;
using Axit.Core;

namespace AxPass;

/// <summary>The texts axPass shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text: a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2).</summary>
    public const string Shortcuts = """
        In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match

        ## In every window of Axit
          F1: this list
          Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it
          Ctrl+F: find an entry in the navigator. F3 and Shift+F3: next or previous match
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close axPass, which locks it
          Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, Close all windows, Check for updates, and About

        ## The navigator, on the left
          Up, Down, Home, End: move; the edge sound at an end. Type the start of a name to jump to it, Shift with a letter backwards
          Right: into a group. On an entry, nothing: the edge sound
          Left or Backspace: up a level
          Ctrl+Up, Ctrl+Down: the section before or after, on a level in sections (the other item apps'); here the edge sound
          Enter: open: a group shows what is in it, an entry moves to its fields. On a locked window, Enter unlocks
          Tab: to the detail pane
          Ctrl+N: a new entry here. Ctrl+Shift+N: a new group here
          F2: rename the entry in place: a box over the line, Enter keeps the new title, Escape the old. Delete: move it to the Recycle Bin; Ctrl+Z brings it back
          Ctrl+M: move the entry to another group, chosen from a list
          Ctrl+Q: read every field of the entry; the password is never read
          Shift+F10 or the Applications key: the menu of the selection

        ## The detail pane, on the right
          It shows the fields of the entry selected in the navigator: Title, UserName, Password (read as hidden), URL, Notes
          Up, Down: move through the fields
          Enter: change the field. On Password: type a new one twice, or Generate (Alt+G) makes one
          Ctrl+Q: read the field's whole value
          Ctrl+N, F2, Delete, Alt+Up, Alt+Down: the other item apps' field commands; here they say that an entry keeps KeePass's own fields
          Escape, Tab or Shift+Tab: back to the navigator

        ## Everywhere in axPass
          Ctrl+B: copy the user name
          Ctrl+C: copy the password; the clipboard clears after 30 seconds
          Ctrl+U: open the URL in the browser
          Ctrl+L: lock: the master password is forgotten until you type it again
          Ctrl+O: open another database
          Ctrl+Z: undo the last change. F5: read the database again
          Ctrl+S: nothing to save here; every change is written to the database at once, by KeePassXC's tool

        ## Notices
          The master password, a field's new value and every question appear inside the window, in place of the panes.
          Tab moves between the field and the buttons. Enter chooses the main button. Escape closes the notice
        """;

    /// <summary>docs/axpass/user-guide.md, embedded by AxPass.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axpass/user-guide.md");
}
