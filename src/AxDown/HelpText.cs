using System.Reflection;

namespace AxDown;

/// <summary>The texts AxDown shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>Axit.Core.BundleAbout</c>).</summary>
internal static class HelpText
{
    /// <summary>The F1 text (SPEC.md AD-5). Bundle-wide keys first, then AxDown's own, then what the edit control does itself.</summary>
    public const string Shortcuts = """
        In every window of Axit
          F1: this list
          Ctrl+F: find text. F3 and Shift+F3: next or previous match
          Ctrl+S: save the file
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close AxDown (asks first when there are unsaved changes)
          Alt+A: the Axit menu: a new AxDown or AxClaude window, the files and folders closed lately, the open Axit windows, most recent first (Alt+A, O, 1 is the one you were in last) and by type, Close all windows, Check for updates, and About

        AxDown
          Ctrl+N: new, empty document
          Ctrl+O: open a file
          Ctrl+Shift+S: save under a new name
          Ctrl+G: go to a line; the field also says where you are, like "Go to line (now line 12, column 4)"
          Ctrl+H and Ctrl+Shift+H: next and previous Markdown heading (a line starting with #), spoken as "Install, heading level 2"
          Ctrl+1 to Ctrl+6 and Ctrl+Shift+1 to Ctrl+Shift+6: next and previous heading of that level only
          Ctrl+K, Ctrl+L, Ctrl+I, Ctrl+Q, Ctrl+T: next link, list, list item, block quote or table; with Shift the previous one
            All of them work in the user guide and the other notices too
          Ctrl+Shift+W: word wrap on or off

        The text
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left, Ctrl+Right: move as in any text
          Page Up, Page Down: one screen up or down, or the first or last line
          Ctrl+Up, Ctrl+Down: the previous or next paragraph (the first line after a blank line)
          Shift with any of them, or Ctrl+A: select. Ctrl+C, Ctrl+X, Ctrl+V: copy, cut, paste. Ctrl+Z: undo
          Enter: a new line. Tab: a tab character

        Notices
          AxDown's questions, errors and help texts appear inside the window, in place of the text.
          Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice.
        """;

    /// <summary>docs/axdown/user-guide.md, embedded in the executable at build time.</summary>
    public static string UserGuide()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UserGuide.md");
        if (stream is null)
        {
            return "The user guide is not included in this build. It is docs/axdown/user-guide.md in the source repository.";
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
