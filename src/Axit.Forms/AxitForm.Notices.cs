using Axit.Core;

namespace Axit.Forms;

public abstract partial class AxitForm
{
    // ---- notices: the window's own dialogs, drawn inside the window (AX-7.2, AX-6.4) ----
    //
    // The one focus rule (docs/axit/SPEC.md AX-6.5): when the first notice of a chain opens, the control that has the
    // focus is remembered; a notice opened from a notice's button, or shown while one is open, continues the chain;
    // when the last one closes, that control gets the focus back if it is still there and visible, and the window's
    // FocusTarget otherwise. Nothing else decides where the focus goes, and no window writes its own version.

    /// <summary>The control that had the focus when the first notice of a chain opened, and gets it back when the last one closes; null between chains.</summary>
    private Control? _focusBeforeNotice;

    /// <summary>Inside a notice button's action: a notice the action opens continues the chain, so nothing is remembered again and the hooks fire once.</summary>
    private bool _chaining;

    /// <summary>The crash handler's report, shown inside the window like every other notice.</summary>
    public void ShowError(string title, string message) => ShowNotice(Notice.Plain(title, message) with { Unprompted = true });

    /// <summary>
    /// The one place a notice is shown; callers pass only its content. It adds the chime of a notice the user did
    /// not open, the focus in the notice, the hidden main controls, the blocked menu and window shortcuts, and
    /// Enter and Escape on the default and cancel buttons; the panel's Populate adds the key line made from the
    /// buttons and the hold of an unprompted notice. A notice shown while another is open replaces it. The Axit
    /// menu, in this project, calls it for the About sections.
    /// </summary>
    protected internal void ShowNotice(Notice notice)
    {
        _paletteSpeak.Stop();
        if (notice.Unprompted && NoticeChime)
        {
            Sounds.Notice();
        }

        if (!NoticeOpen && !_chaining)
        {
            _focusBeforeNotice = FocusedControl();
            OnNoticeOpened(_focusBeforeNotice);
        }

        NoticeOpen = true;
        Overlay.Populate(notice);
        Overlay.Visible = true;
        Overlay.BringToFront();
        AcceptButton = Overlay.DefaultButton;
        CancelButton = Overlay.CancelButton;
        Overlay.FocusStart();
        HideForNotice();
        if (MainMenuStrip is { } menu)
        {
            menu.Enabled = false;
        }

        PerformLayout();
    }

    private void OnNoticeChoice(OverlayChoice choice)
    {
        if (choice.StaysOpen)
        {
            choice.Action?.Invoke();
            return;
        }

        CloseNotice(choice.Action);
    }

    /// <summary>Closes the notice, runs <paramref name="then"/>, and gives the focus back unless the action opened the next notice of the chain or closed the window.</summary>
    protected void CloseNotice(Action? then)
    {
        if (!NoticeOpen)
        {
            return;
        }

        _paletteSpeak.Stop();
        NoticeOpen = false;
        ShowAfterNotice();
        if (MainMenuStrip is { } menu)
        {
            menu.Enabled = true;
        }

        AcceptButton = null;
        CancelButton = null;
        PerformLayout();
        var outer = _chaining;
        _chaining = true;
        try
        {
            then?.Invoke();
        }
        finally
        {
            _chaining = outer;
        }

        // The action opened the next notice (the update's progress, an error, the changed-on-disk question after
        // Save): that one is showing now and the place from before carries over to it. A close inside an outer
        // close's action leaves the end of the chain to the outer one.
        if (IsDisposed || NoticeOpen || outer)
        {
            return;
        }

        OnNoticeClosed();
        var back = _focusBeforeNotice;
        _focusBeforeNotice = null;
        if (ActiveControl is null || Overlay.Contains(ActiveControl))
        {
            (back is { IsDisposed: false, Visible: true, Enabled: true } ? back : FocusTarget).Select();
        }

        Overlay.Visible = false;
    }

    /// <summary>The innermost control with the focus, outside the notice and the menu bar; null when none has it.</summary>
    private Control? FocusedControl()
    {
        var control = ActiveControl;
        while (control is ContainerControl { ActiveControl: { } inner })
        {
            control = inner;
        }

        return control is null || control is ToolStrip || Overlay.Contains(control) ? null : control;
    }

    /// <summary>
    /// After a paste into an editable text (AX-7.3, 2026-10-25): the paste is the control's own, and this says it
    /// once the control has done it, "Pasted" or "Pasted, 3 lines" (<see cref="PasteWords"/>); the text itself is never
    /// read out, since it may be a password. A menu item that pastes calls it the same way.
    /// </summary>
    protected internal void SayPasteAfter(TextBoxBase box)
    {
        string? text = null;
        try
        {
            if (Clipboard.ContainsText())
            {
                text = Clipboard.GetText();
            }
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            // The clipboard is held by another program for a moment: the paste is the control's, so say it plainly.
            text = string.Empty;
        }

        var said = PasteWords(text, box.Multiline);
        BeginInvoke(() => Announce(said, true));
    }

    /// <summary>What a paste says: "Pasted"; "Pasted, 3 lines" for several into a text of several lines; "Nothing to paste as text" when the clipboard holds none (a picture, files).</summary>
    public static string PasteWords(string? text, bool multiline)
    {
        if (text is null)
        {
            return "Nothing to paste as text";
        }

        var lines = text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n').Length;
        return multiline && lines > 1 ? $"Pasted, {lines} lines" : "Pasted";
    }

    /// <summary>An error the user must see: logged and shown as a notice; a console window adds a system line.</summary>
    protected virtual void Fail(string message)
    {
        Log.Error(message);
        ShowError("Error", message);
    }

    /// <summary>Speaks without moving the focus (AX-7.3): on the notice while one shows, since the window's controls are hidden then, and on <see cref="AnnounceSource"/> otherwise.</summary>
    protected bool Announce(string text, bool interrupt) =>
        NoticeOpen ? Overlay.Announce(text, interrupt) : Announcer.Raise(AnnounceSource, text, interrupt);
}
