using System.Reflection;
using Axit.Core;

namespace AxClip;

/// <summary>The texts axClip shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md CP-5.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2).</summary>
    public const string Shortcuts = """
        In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match

        ## In every window of Axit
          F1: this list
          Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it
          Ctrl+F, F3 and Shift+F3: find in a notice's text
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: hide axClip (Quit axClip, in its first menu, ends it)
          Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, the AI tools, Check for updates, and About

        ## axClip
          Windows+Alt+V, or the hotkey you chose: show axClip from anywhere; again: hide it
          Type: only the items that hold every word typed
          Down, Up: the next or the previous item. Ctrl+Home, Ctrl+End: the first or the last. Page Down, Page Up: a screen
          Enter: paste the item into the program you were in
          Shift+Enter: put the item on the clipboard without pasting
          Ctrl+Q: read the item in full
          Delete: remove the item. Ctrl+Shift+Delete: clear the history, after a question
          Ctrl+P: pause recording, or record again
          Escape: hide axClip
        """;

    /// <summary>docs/axclip/user-guide.md, embedded by AxClip.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axclip/user-guide.md");
}
