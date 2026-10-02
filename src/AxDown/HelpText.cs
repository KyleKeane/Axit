using System.Reflection;

namespace AxDown;

/// <summary>The texts AxDown shows as notices: the keyboard shortcuts (F1), the user guide, the disclaimer in About.</summary>
internal static class HelpText
{
    /// <summary>Shown in About and at the end of the user guide. The LICENSE file is the binding text; this repeats it in full words.</summary>
    public const string Disclaimer = """
        Disclaimer
        AxDown is free software offered under the MIT licence. It is provided "as is" and "as available", without warranty of any kind, express or implied, including but not limited to the implied warranties of merchantability, fitness for a particular purpose, title and non-infringement. The developer has no obligation to provide support, maintenance, updates or corrections. To the fullest extent permitted by law, the developer is not liable for any claim, damages or other liability, whether in contract, tort or otherwise, arising from or in connection with the software or its use, including loss of data or work. You use AxDown at your own risk.
        """;

    /// <summary>The F1 text (SPEC.md AD-5). Bundle-wide keys first, then AxDown's own, then what the edit control does itself.</summary>
    public const string Shortcuts = """
        In every window of Axit
          F1: this list
          Ctrl+F: find text. F3 and Shift+F3: next or previous match
          Ctrl+S: save the file
          Ctrl+Plus, Ctrl+Minus: bigger or smaller text
          Alt: the menu. Alt+F4: close AxDown (asks first when there are unsaved changes)

        AxDown
          Ctrl+N: new, empty document
          Ctrl+O: open a file
          Ctrl+Shift+S: save under a new name
          Ctrl+G: go to a line; the field also says where you are, like "Go to line (now line 12, column 4)"
          Ctrl+Shift+W: word wrap on or off

        The text
          Arrow keys, Home, End, Ctrl+Home, Ctrl+End, Ctrl+Left, Ctrl+Right: move as in any text
          Page Up, Page Down: one screen up or down, or the first or last line
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
