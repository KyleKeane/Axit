using Axit.Core;

namespace Axit.Forms;

public abstract partial class AxitForm
{
    // ---- the command palette (AX-10): the window's menu commands by name, in a notice ----

    private IReadOnlyList<PaletteEntry> _paletteAll = [];
    private IReadOnlyList<PaletteEntry> _paletteShown = [];

    /// <summary>The pause after typing in the palette before its first match is said (AX-10.4, AxStart's 300 ms).</summary>
    private readonly System.Windows.Forms.Timer _paletteSpeak = new() { Interval = 300 };

    /// <summary>
    /// The palette as a notice: one field, the matches under it, Enter runs the selected one with the focus back
    /// where it was, Escape closes (AX-10.1, AX-10.4). The entries are read from the menu bar now, before the notice
    /// disables it, so that what the palette offers is what the menus offer at this moment.
    /// </summary>
    protected void ShowCommandPalette()
    {
        if (NoticeOpen)
        {
            return;
        }

        _paletteAll = [.. CommandPalette.Commands(MainMenuStrip, text => Announce(text, true)), .. PaletteEntries()];
        _paletteShown = _paletteAll;
        ShowNotice(new Notice("Command palette", string.Empty,
        [
            new OverlayChoice("&Run", RunPaletteEntry, IsDefault: true, StaysOpen: true),
            new OverlayChoice("&Close", IsCancel: true),
        ])
        {
            Input = new OverlayInput("&Search commands:", "Search commands", string.Empty, string.Empty, Required: false,
                KeyHook: OnPaletteKey, Changed: OnPaletteTyped, ListName: "Commands"),
        });
        FillPalette();
    }

    private void OnPaletteTyped()
    {
        _paletteSpeak.Stop();
        FillPalette();
        if (Overlay.InputText.Trim().Length > 0)
        {
            _paletteSpeak.Start();
        }
    }

    private void FillPalette()
    {
        _paletteShown = Palette.Find(_paletteAll, Overlay.InputText);
        var lines = _paletteShown.Count > 0 ? _paletteShown.Select(entry => entry.Text).ToList() : [Palette.NoMatch(Overlay.InputText)];
        Overlay.SetMatches(lines, $"Commands, {_paletteShown.Count}");
    }

    /// <summary>The first match and the count after the typing paused, or that nothing matches (AX-10.4).</summary>
    private void SayFirstPaletteMatch()
    {
        _paletteSpeak.Stop();
        if (!NoticeOpen || Overlay.InputText.Trim().Length == 0)
        {
            return;
        }

        Announce(_paletteShown.Count > 0 ? Palette.Spoken(_paletteShown[0], 0, _paletteShown.Count) : Palette.NoMatch(Overlay.InputText), true);
    }

    /// <summary>Down, Up, Page Down, Page Up, Ctrl+Home and Ctrl+End in the field move through the matches (AX-10.4).</summary>
    private bool OnPaletteKey(Keys key)
    {
        var index = Overlay.MatchIndex;
        int? target = key switch
        {
            CommandPalette.NextMatch => index + 1,
            CommandPalette.PreviousMatch => index - 1,
            BundleKeys.PageDown => index + Overlay.MatchPage,
            BundleKeys.PageUp => index - Overlay.MatchPage,
            CommandPalette.FirstMatch => 0,
            CommandPalette.LastMatch => _paletteShown.Count - 1,
            _ => null,
        };
        if (target is not { } wanted)
        {
            return false;
        }

        _paletteSpeak.Stop();
        if (_paletteShown.Count == 0)
        {
            Sounds.Edge();
            Announce(Palette.NoMatch(Overlay.InputText), true);
            return true;
        }

        wanted = Math.Clamp(wanted, 0, _paletteShown.Count - 1);
        if (wanted == index)
        {
            // At an end nothing moves: the edge earcon, and then the match again, so that the last thing heard is
            // the position and not the typed text (AX-10.4).
            Sounds.Edge();
        }
        else
        {
            Overlay.SelectMatch(wanted);
        }

        Announce(Palette.Spoken(_paletteShown[wanted], wanted, _paletteShown.Count), true);
        return true;
    }

    /// <summary>Enter: the palette closes, the focus goes back to the control it came from (the notice rule), and then the entry runs where the user was (AX-10.4).</summary>
    private void RunPaletteEntry()
    {
        _paletteSpeak.Stop();
        var index = Overlay.MatchIndex;
        if (index < 0 || index >= _paletteShown.Count)
        {
            Announce(Palette.NoMatch(Overlay.InputText), true);
            return;
        }

        var entry = _paletteShown[index];
        CloseNotice(null);
        BeginInvoke(entry.Run);
    }
}
