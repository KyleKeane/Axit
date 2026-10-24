using System.Reflection;
using Axit.Core;

namespace AxGit;

/// <summary>The texts axGIT shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AG-6.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, axGIT's own, the controls' own. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axGIT",
        [
            "Tab, Shift+Tab, Ctrl+Tab or F6: switch between the list and the details",
            "Ctrl+F: find text in the details. F3 and Shift+F3: next or previous match",
        ]) + """
        ## axGIT
          Type the start of an item to jump to it, the same letter again to the next; Shift with a letter backwards
          Ctrl+O: open a repository (the Windows folder picker; any folder inside it will do)
          F5: refresh the list and the status bar
          Ctrl+W: show the repository's folder in File Explorer
          Ctrl+1, Ctrl+2, Ctrl+3: the Changes, Log and Branches view of the list; pressed on the view already shown, the key says its name
          In the list
            Space: stage the file, or unstage it when it is staged; axGIT says which
            Enter: in Changes, the file's diff in the details; in Log, the commit's details; in Branches, switch to the branch
            Delete: discard the changes to the file, after a question
          Ctrl+Shift+A, Ctrl+Shift+U: stage all, unstage all
          Ctrl+E: open the file in axDown, or axJSON for a .json file
          Ctrl+Enter: commit what is staged; a notice asks for the message
          Ctrl+N: a new branch, after its name; axGIT switches to it
          Ctrl+Shift+F, Ctrl+Shift+L, Ctrl+Shift+H: fetch, pull, push
          Ctrl+S: nothing to save here; axGIT says so

        ## The details
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End: move as in any text
          Page Up, Page Down: one screen up or down, or the first or last line
          Ctrl+Up, Ctrl+Down: the previous or next paragraph (the first line after a blank line)
          Shift with any of them, or Ctrl+A: select. Ctrl+C: copy

        ## Notices
          axGIT's questions, errors and help texts appear inside the window, in place of the list and the details.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice. In a notice's text, h, 1 to 6 and Ctrl+F work as in this one
        """;

    /// <summary>docs/axgit/user-guide.md, embedded by AxGit.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axgit/user-guide.md");
}
