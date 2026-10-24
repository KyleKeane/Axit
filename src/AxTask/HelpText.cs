using System.Reflection;
using Axit.Core;

namespace AxTask;

/// <summary>The texts axTask shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
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
          Alt: the menu. Alt+F4: close axTask
          Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, Close all windows, Check for updates, and About

        ## The navigator, on the left
          Up, Down, Home, End: move; the edge sound at an end. Type the start of a name to jump to it, Shift with a letter backwards
          The top level is in sections: Smart lists, User lists, your own sections, Archived lists, each under a header line
          Ctrl+Up, Ctrl+Down: the section before or after; the edge sound past the first or the last
          Right: into a list or a smart list. On an item or a section's header, nothing: the edge sound
          Left or Backspace: back to the sections
          Enter: open: a list shows its items, an item moves to its fields
          Tab: to the detail pane
          Space: done or not done, on a task
          Ctrl+N: a new item in this list. Ctrl+Shift+N: a new list, in the section of the line selected
          Ctrl+M on a list: move it to a section, or to a new one; its Section field in the detail pane does the same
          F2: rename. Delete: move to the Archive list; Ctrl+Z brings it back
          Ctrl+Q: read every field of the item
          Ctrl+1: All tasks, every task not done
          Ctrl+Shift+D: hide or show the done tasks of a list
          Shift+F10 or the Applications key: the menu of the selection

        ## The detail pane, on the right
          It shows the fields of the item selected in the navigator, or the details of the list
          Up, Down: move through the fields
          Enter: change the field. Space: done or not done, on a check
          Ctrl+N: a new field. F2: rename the field. Delete: remove it. Alt+Up, Alt+Down: move it
          Ctrl+Q: read the field's whole value
          Escape, Tab or Shift+Tab: back to the navigator

        ## Everywhere in axTask
          Ctrl+Z: undo the last change
          F5: read the folder again
          Ctrl+J: open the item, the list's details or the smart list in axJSON
          Ctrl+E: show it in axFile
          Ctrl+S: nothing to save here; every change is written at once

        ## Notices
          A field's new value, a new item and every question appear inside the window, in place of the panes.
          Tab moves between the field and the buttons. Enter chooses the main button. Escape closes the notice
        """;

    /// <summary>docs/axtask/user-guide.md, embedded by AxTask.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axtask/user-guide.md");
}
