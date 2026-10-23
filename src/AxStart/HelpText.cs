using System.Reflection;
using Axit.Core;

namespace AxStart;

/// <summary>The texts AxStart shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AS-5.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then AxStart's own.</summary>
    public const string Shortcuts = """
        In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match

        ## In every window of Axit
          F1: this list
          Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: hide AxStart (AxStart, Quit AxStart ends it)
          Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, Check for updates, and About

        ## Bringing AxStart up
          The hotkey, Ctrl+Shift+Space unless you chose another in Options, Hotkey: from any program, the search field
          The same hotkey while AxStart shows hides it again

        ## In the search field
          The caret stays here: type and delete at any time
          Type part of a program's name; after a short pause the first match is said, "Calculator, 1 of 2"
          Down, Up: the next or the previous match, said with its place. Page Down, Page Up: a page of matches
          Ctrl+Home, Ctrl+End: the first or the last match. At the first or the last match, a key that cannot move plays one short low note and says the match again
          Enter: start the match. Ctrl+Shift+Enter: start it as administrator (Windows asks first)
          Ctrl+I: the match, its kind and where its program file is
          Ctrl+O: the program file's folder in AxFile, the file selected
          F5: read the list of programs again
          Escape: hide AxStart at once and return where you were; the field is empty the next time

        ## Notices
          AxStart's questions, errors and help texts appear inside the window, in place of the field.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice. In a notice's text, h, 1 to 6 and Ctrl+F work as in this one
        """;

    /// <summary>docs/axstart/user-guide.md, embedded by AxStart.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axstart/user-guide.md");
}
