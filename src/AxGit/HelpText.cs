using System.Reflection;
using Axit.Core;

namespace AxGit;

/// <summary>The texts AxGit shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AG-6.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, AxGit's own, the controls' own.</summary>
    public const string Shortcuts = """
        In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match

        ## In every window of Axit
          F1: this list
          Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it
          Tab, Shift+Tab, Ctrl+Tab or F6: switch between the list and the details
          Ctrl+F: find text in the details. F3 and Shift+F3: next or previous match
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close AxGit
          Alt+A: the Axit menu: the command palette, a new window of any app, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, Check for updates, and About

        ## AxGit
          Ctrl+O: open a repository (the Windows folder picker; any folder inside it will do)
          F5: refresh the list and the status bar
          Ctrl+W: show the repository's folder in File Explorer
          Ctrl+1, Ctrl+2, Ctrl+3: the Changes, Log and Branches view of the list; pressed on the view already shown, the key says its name
          In the list
            Space: stage the file, or unstage it when it is staged; AxGit says which
            Enter: in Changes, the file's diff in the details; in Log, the commit's details; in Branches, switch to the branch
            Delete: discard the changes to the file, after a question
          Ctrl+Shift+A, Ctrl+Shift+U: stage all, unstage all
          Ctrl+E: open the file in AxDown, or AxJSON for a .json file
          Ctrl+Enter: commit what is staged; a notice asks for the message
          Ctrl+N: a new branch, after its name; AxGit switches to it
          Ctrl+Shift+F, Ctrl+Shift+L, Ctrl+Shift+H: fetch, pull, push
          Ctrl+S: nothing to save here; AxGit says so

        ## The details
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End: move as in any text
          Page Up, Page Down: one screen up or down, or the first or last line
          Ctrl+Up, Ctrl+Down: the previous or next paragraph (the first line after a blank line)
          Shift with any of them, or Ctrl+A: select. Ctrl+C: copy

        ## Notices
          AxGit's questions, errors and help texts appear inside the window, in place of the list and the details.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice. In a notice's text, h, 1 to 6 and Ctrl+F work as in this one
        """;

    /// <summary>docs/axgit/user-guide.md, embedded by AxGit.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axgit/user-guide.md");
}
