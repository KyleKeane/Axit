using System.Reflection;
using Axit.Core;

namespace AxPreview;

/// <summary>The texts axPreview shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AP-3.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axPreview's own. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axPreview",
        [
            "Ctrl+F: find text. F3 and Shift+F3: next or previous match",
        ],
        navigateMenu: true) + """
        ## axPreview
          Ctrl+O: open a file to read. Ctrl+N: a new axPreview window
          F5: read the file again; coming back to the window after another program changed it does the same by itself
          Ctrl+E: open the same file in its editor, axDown, or axJSON for a .json file
          Ctrl+W: show the file in File Explorer
          Ctrl+G: go to a line; the field also says where you are, like "Go to line (now line 12, column 4)"
          l: say the line, "Line 12 of 40"
          Ctrl+H and Ctrl+Shift+H: next and previous Markdown heading (a line starting with #), spoken as "Install, heading level 2"
          Ctrl+1, Ctrl+2, Ctrl+3, Ctrl+4, Ctrl+5, Ctrl+6: the next heading of that level only; Ctrl+Shift+1, Ctrl+Shift+2, Ctrl+Shift+3, Ctrl+Shift+4, Ctrl+Shift+5, Ctrl+Shift+6: the previous one
          Ctrl+K and Ctrl+Shift+K: next and previous link
          Ctrl+L and Ctrl+Shift+L: next and previous list
          Ctrl+I and Ctrl+Shift+I: next and previous list item
          Ctrl+Q and Ctrl+Shift+Q: next and previous block quote
          Ctrl+T and Ctrl+Shift+T: next and previous table
            All of them work in the user guide and the other notices too
          Ctrl+Shift+W: word wrap on or off
          Ctrl+S: nothing to save here; axPreview says so

        ## The text
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left, Ctrl+Right: move as in any text
          Page Up, Page Down: one screen up or down, or the first or last line
          Ctrl+Up, Ctrl+Down: the previous or next paragraph (the first line after a blank line)
          Shift with any of them, or Ctrl+A: select. Ctrl+C: copy. Typing changes nothing

        ## Notices
          axPreview's errors and help texts appear inside the window, in place of the text.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice. In a notice's text, h, 1 to 6, the Navigate keys and Ctrl+F work as in this one
        """;

    /// <summary>docs/axpreview/user-guide.md, embedded by AxPreview.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axpreview/user-guide.md");
}
