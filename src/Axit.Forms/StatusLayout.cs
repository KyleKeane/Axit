namespace Axit.Forms;

/// <summary>
/// Keeps both status bar labels inside the strip. A ToolStrip item that does not fit is neither drawn nor exposed to
/// UI Automation, so behind a long session title NVDA+End used to lose the folder path. When the two texts do not
/// both fit at their natural width, the folder path is clipped first (it is also in the title bar and the Project
/// menu), down to a quarter of the strip, then the state. NVDA reads the full text of a clipped label.
/// </summary>
public static class StatusLayout
{
    /// <summary>Pixels kept free: the strip lays items out from x = 1, and an item that ends past the edge overflows.</summary>
    private const int Slack = 8;

    /// <summary>
    /// The two labels of a window's status bar, added to the strip and kept inside it as it resizes. No
    /// AccessibleName on them (it would replace the text NVDA reads with NVDA+End) and no Spring (this WinForms
    /// version places a Spring label outside the strip, where it is neither drawn nor exposed).
    /// </summary>
    public static (ToolStripStatusLabel Left, ToolStripStatusLabel Right) Pair(StatusStrip strip)
    {
        var left = new ToolStripStatusLabel { TextAlign = ContentAlignment.MiddleLeft };
        var right = new ToolStripStatusLabel { TextAlign = ContentAlignment.MiddleLeft };
        strip.Items.AddRange([left, right]);
        strip.SizeChanged += (_, _) => Fit(strip, left, right);
        return (left, right);
    }

    public static void Fit(ToolStrip strip, ToolStripStatusLabel state, ToolStripStatusLabel folder)
    {
        var available = strip.DisplayRectangle.Width - state.Margin.Horizontal - folder.Margin.Horizontal - Slack;
        if (available <= 0)
        {
            return;
        }

        state.AutoSize = true;
        folder.AutoSize = true;
        var stateWidth = state.GetPreferredSize(Size.Empty).Width;
        var folderWidth = folder.GetPreferredSize(Size.Empty).Width;
        if (stateWidth + folderWidth <= available)
        {
            return;
        }

        var folderMinimum = Math.Min(folderWidth, available / 4);
        stateWidth = Math.Min(stateWidth, available - folderMinimum);
        state.AutoSize = false;
        state.Width = stateWidth;
        folder.AutoSize = false;
        folder.Width = available - stateWidth;
    }
}
