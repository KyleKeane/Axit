using System.Reflection;
using Axit.Core;

namespace AxList;

/// <summary>The texts axList shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md LI-6.6): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2).</summary>
    public const string Shortcuts = """
        In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match

        ## In every window of Axit
          F1: this list
          Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it
          Ctrl+F: find an item in the list. F3 and Shift+F3: next or previous match
          Ctrl+S: nothing to save: every change is written to the file at once
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close axList
          Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, Check for updates, and About

        ## axList
          Ctrl+1: the Lists view, the folder's lists. Ctrl+2: the Items view, the open list. Ctrl+3: the Tasks view, the open tasks of every list
          F5: read the folder and the list again
          In the Lists view
            Enter: open the list
            Ctrl+N: a new list, after its name
          In the Items view
            Space: check or uncheck a task
            Enter: change the item's text
            Ctrl+N: a new item after this one, of its kind
            Delete: remove the item and the items nested under it
            Alt+Up, Alt+Down: move the item up or down
            Alt+Right, Alt+Left: indent the item under the one before, or take it out a level
            Ctrl+T: make the item a task, or a plain item again
            Ctrl+Shift+D: hide the done tasks, or show them
            Ctrl+Z: undo the last change
            Escape or Backspace: back to the Lists view
          In the Tasks view
            Space: check the task off
            Enter: the task in its list
          Ctrl+C: copy the text. Ctrl+E: show the list's file in axFile

        ## Notices
          The questions appear inside the window, in place of the list.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice
        """;

    /// <summary>docs/axlist/user-guide.md, embedded by AxList.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axlist/user-guide.md");
}
