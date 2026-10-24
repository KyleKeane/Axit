using System.Reflection;
using Axit.Core;

namespace AxStart;

/// <summary>The texts axStart shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AS-5.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axStart's own. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axStart",
        [
            "Ctrl+S: nothing to save here; axStart says so",
        ],
        altF4: "hide axStart (axStart, Quit axStart ends it)") + """
        ## Bringing axStart up
          The hotkey, Ctrl+Shift+Space unless you chose another in Options, Hotkey: from any program, the search field
          The same hotkey while axStart shows hides it again

        ## In the search field
          The caret stays here: type and delete at any time
          Type part of a program's name; after a short pause the first match is said, "Calculator, 1 of 2"
          Down, Up: the next or the previous match, said with its place. Page Down, Page Up: a page of matches
          Ctrl+Home, Ctrl+End: the first or the last match. At the first or the last match, a key that cannot move plays one short low note and says the match again
          Enter: start the match. Ctrl+Shift+Enter: start it as administrator (Windows asks first)
          Ctrl+I: the match, its kind and where its program file is
          Ctrl+O: the program file's folder in axFile, the file selected
          F5: read the list of programs again
          Escape: hide axStart at once and return where you were; the field is empty the next time

        ## Notices
          axStart's questions, errors and help texts appear inside the window, in place of the field.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice. In a notice's text, h, 1 to 6 and Ctrl+F work as in this one
        """;

    /// <summary>docs/axstart/user-guide.md, embedded by AxStart.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axstart/user-guide.md");
}
