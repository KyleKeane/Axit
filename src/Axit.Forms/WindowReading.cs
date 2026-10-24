namespace Axit.Forms;

/// <summary>
/// What a window shows, read as NVDA reads it (docs/axit/automation.md; docs/axit/mcp.md MC-6.5): a control's value, a
/// list, a text, a hidden field as "protected" (security audit S-12). From the automation channel on its second use,
/// the MCP channel, 2026-10-24. On the window's thread only.
/// </summary>
internal static class WindowReading
{
    /// <summary>What NVDA reads of a control besides its name: the selected item of a list, the caret's line of a text, the text of the rest.</summary>
    public static string? ValueOf(Control control) => control switch
    {
        ListBox { SelectedItem: { } item } => item.ToString(),
        ListBox => string.Empty,
        // A hidden field (axPass's passwords) is never read out, as NVDA reads it: protected (S-12).
        TextBoxBase box when IsSecret(box) => "protected",
        TextBoxBase box => CaretLine(box),
        _ => control.Text,
    };

    /// <summary>Whether a text control hides what is typed in it (a password field): its text never leaves the window (S-12).</summary>
    public static bool IsSecret(Control control) =>
        control is TextBox { UseSystemPasswordChar: true } or TextBox { PasswordChar: not '\0' };

    /// <summary>The first visible list under the control, depth first: a window's list when the focus is elsewhere.</summary>
    public static ListBox? FirstVisibleList(Control? parent)
    {
        if (parent is null)
        {
            return null;
        }

        foreach (Control child in parent.Controls)
        {
            if (child is ListBox { Visible: true } list)
            {
                return list;
            }

            if (child.Visible && FirstVisibleList(child) is { } inner)
            {
                return inner;
            }
        }

        return null;
    }

    /// <summary>The first visible text of several lines under the control: an editor's file, a viewer's, a transcript.</summary>
    public static TextBoxBase? FirstVisibleText(Control? parent)
    {
        if (parent is null)
        {
            return null;
        }

        foreach (Control child in parent.Controls)
        {
            if (child is TextBoxBase { Visible: true, Multiline: true } box)
            {
                return box;
            }

            if (child.Visible && FirstVisibleText(child) is { } inner)
            {
                return inner;
            }
        }

        return null;
    }

    /// <summary>The caret's line as the text has it (up to a line break), not the wrapped row GetLineFromCharIndex counts.</summary>
    public static int LogicalLine(TextBoxBase box)
    {
        var text = box.Text;
        var end = Math.Min(box.SelectionStart, text.Length);
        var line = 0;
        for (var i = 0; i < end; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    public static string CaretLine(TextBoxBase box)
    {
        var line = LogicalLine(box);
        var lines = box.Lines;
        return line < lines.Length ? lines[line] : string.Empty;
    }
}
