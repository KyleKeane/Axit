namespace Axit.Forms;

public abstract partial class AxitForm
{
    // ---- find (AX-8.2): one Find notice, one last text, for a text box, a list, a transcript and a notice's text ----

    private string _findText = string.Empty;

    /// <summary>The text last searched for in this window, empty before the first search; F3 in a window with its own search reads it.</summary>
    protected string FindText => _findText;

    /// <summary>Ctrl+F on a text box: the Find notice; Find next closes it and searches <paramref name="box"/> from the selection; an empty field keeps it open.</summary>
    protected void ShowFind(TextBox box) => ShowNotice(FindNotice("Text to find:", "Text to find", () => FindNext(box, 1)));

    /// <summary>Ctrl+F in a window with a search of its own (a transcript): the Find notice; Find next closes it and hands <paramref name="search"/> the text.</summary>
    protected void ShowFind(Action<string> search) => ShowNotice(FindNotice("Text to find:", "Text to find", () => search(_findText)));

    /// <summary>Ctrl+F on a list: the Find notice with the field "Name contains"; Find next closes it and selects the next item of <paramref name="list"/> whose name (from <paramref name="names"/>, one per item) contains the text, wrapping once.</summary>
    protected void ShowFindInList(ListBox list, Func<IReadOnlyList<string>> names) =>
        ShowNotice(FindNotice("Name contains:", "Name contains", () => FindInList(list, names, 1)));

    /// <summary>
    /// The Find notice: one field with the last text selected, Find next (Enter) and Cancel (Escape). Find next keeps
    /// the field's text as <see cref="FindText"/> and runs <paramref name="search"/>; with <paramref name="staysOpen"/>
    /// the buttons leave the notice to the search (the find in a notice's text puts the notice back itself), and
    /// <paramref name="cancel"/> is what Cancel does then. An empty field keeps the notice open (OverlayPanel).
    /// </summary>
    private Notice FindNotice(string label, string accessibleName, Action search, bool staysOpen = false, Action? cancel = null) =>
        new("Find", string.Empty,
        [
            new OverlayChoice("&Find next", () =>
            {
                _findText = Overlay.InputText;
                search();
            }, IsDefault: true, StaysOpen: staysOpen),
            new OverlayChoice("&Cancel", cancel, IsCancel: true, StaysOpen: staysOpen),
        ])
        {
            Input = new OverlayInput(label, accessibleName, _findText, "Type the text to find"),
        };

    /// <summary>
    /// The next match after the selection (or the previous one before it), wrapping once; the match is selected and
    /// the box takes the focus. False, with "Not found" said, when there is none; the Find notice when nothing was
    /// searched for yet.
    /// </summary>
    protected bool FindNext(TextBox box, int direction)
    {
        if (_findText.Length == 0)
        {
            ShowFind(box);
            return false;
        }

        var text = box.Text;
        int found;
        if (direction > 0)
        {
            var from = Math.Min(box.SelectionStart + box.SelectionLength, text.Length);
            found = text.IndexOf(_findText, from, StringComparison.OrdinalIgnoreCase);
            if (found < 0 && from > 0)
            {
                found = text.IndexOf(_findText, 0, StringComparison.OrdinalIgnoreCase);
            }
        }
        else
        {
            var from = box.SelectionStart - 1;
            found = from >= 0 ? text.LastIndexOf(_findText, from, StringComparison.OrdinalIgnoreCase) : -1;
            if (found < 0)
            {
                found = text.LastIndexOf(_findText, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (found < 0)
        {
            Announce("Not found", true);
            return false;
        }

        box.Select(found, _findText.Length);
        box.ScrollToCaret();
        box.Select();
        return true;
    }

    /// <summary>F3 and Shift+F3 on a list: the next (or previous) item whose name contains the text, wrapping once, selected; "Not found" otherwise; the Find notice when nothing was searched for yet. The selection is NVDA's own reading.</summary>
    protected void FindInList(ListBox list, Func<IReadOnlyList<string>> names, int direction)
    {
        if (_findText.Length == 0)
        {
            ShowFindInList(list, names);
            return;
        }

        var items = names();
        var count = Math.Min(items.Count, list.Items.Count);
        var from = Math.Max(0, list.SelectedIndex);
        for (var step = 1; step <= count; step++)
        {
            var i = ((from + direction * step) % count + count) % count;
            if (items[i].Contains(_findText, StringComparison.OrdinalIgnoreCase))
            {
                list.SelectedIndex = i;
                return;
            }
        }

        Announce("Not found", true);
    }

    // ---- find in a notice's text (AX-7.2, 2026-10-23) ----

    /// <summary>
    /// Ctrl+F in a notice: the Find notice takes its place, and Find next or Cancel show it again, Find next with the
    /// match selected and the focus in the text (the same find as the window's, on the notice's text). The notice
    /// comes back as the same record, with its chime not played a second time.
    /// </summary>
    private void ShowFindInNotice()
    {
        if (Overlay.Shown is not { } notice)
        {
            return;
        }

        var back = notice with { Unprompted = false };
        var caret = Overlay.Body.SelectionStart;
        void ShowAgain()
        {
            ShowNotice(back);
            Overlay.Body.Select(caret, 0);
        }

        ShowNotice(FindNotice("Text to find:", "Text to find", () =>
        {
            ShowAgain();
            FindNext(Overlay.Body, 1);
        }, staysOpen: true, cancel: ShowAgain));
    }

    /// <summary>F3 and Shift+F3 in a notice: the next or the previous match in its text, selected; the Find notice first when nothing was searched for yet.</summary>
    private void FindInNotice(int direction)
    {
        if (_findText.Length == 0)
        {
            ShowFindInNotice();
            return;
        }

        FindNext(Overlay.Body, direction);
    }
}
