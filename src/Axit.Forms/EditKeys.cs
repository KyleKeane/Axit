namespace Axit.Forms;

/// <summary>
/// The bundle's page and paragraph keys in an edit control (AX-8.2): Page Up and Page Down one screen at a time and
/// to the ends (<see cref="EditPaging"/>), Ctrl+Up and Ctrl+Down to the start of the previous or next paragraph, with
/// Shift growing the selection to there (<see cref="EditParagraphs"/>). The control's own keys do neither: they scroll
/// a row and leave the caret, or nothing while the text fits. Subscribe <see cref="Handle"/> to the control's KeyDown.
/// </summary>
public static class EditKeys
{
    public static void Handle(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBoxBase box)
        {
            return;
        }

        if (e.KeyData is BundleKeys.PageUp or BundleKeys.PageDown)
        {
            EditPaging.Page(box, e.KeyCode == Keys.PageDown ? 1 : -1);
        }
        else if (e.KeyData is BundleKeys.ParagraphUp or BundleKeys.ParagraphDown or BundleKeys.SelectParagraphUp or BundleKeys.SelectParagraphDown)
        {
            EditParagraphs.Move(box, e.KeyCode == Keys.Down ? 1 : -1, e.Shift);
        }
        else
        {
            return;
        }

        e.Handled = e.SuppressKeyPress = true;
    }
}
