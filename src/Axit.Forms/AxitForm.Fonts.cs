namespace Axit.Forms;

public abstract partial class AxitForm
{
    // ---- the text font (AX-7.6): the Windows font unless the settings say otherwise, the size keys, the picker ----

    private static Font SystemTextFont() => SystemFonts.MessageBoxFont ?? DefaultFont;

    /// <summary>The text font from the settings: the Windows font unless a family, a size or bold is set; a family Windows does not have falls back to the Windows family at the set size.</summary>
    protected Font TextFont()
    {
        var system = SystemTextFont();
        if (Settings.FontFamily is null && Settings.FontSize <= 0 && !Settings.FontBold)
        {
            return system;
        }

        var size = Settings.FontSize > 0 ? Settings.FontSize : system.SizeInPoints;
        var style = Settings.FontBold ? FontStyle.Bold : FontStyle.Regular;
        try
        {
            return new Font(Settings.FontFamily ?? system.FontFamily.Name, size, style);
        }
        catch (ArgumentException)
        {
            return new Font(system.FontFamily, size, style);
        }
    }

    /// <summary>The settings' font onto the window's controls and the notice.</summary>
    protected void ApplyTextFont()
    {
        var font = TextFont();
        ApplyFont(font);
        Overlay.SetTextFont(font);
    }

    /// <summary>Ctrl+Plus and Ctrl+Minus (AX-8.2), Options → Text → Larger text and Smaller text: one point, between 6 and 72, kept in the settings and said.</summary>
    protected void ChangeTextSize(int delta)
    {
        var size = Math.Clamp((int)Math.Round(TextFont().SizeInPoints) + delta, 6, 72);
        Settings.FontSize = size;
        ApplyTextFont();
        SaveSettings();
        Announce($"Text size {size}", true);
    }

    /// <summary>Options → Text → Windows text size: back to the Windows font, which follows the text size setting under Accessibility.</summary>
    protected void UseSystemFont()
    {
        Settings.FontFamily = null;
        Settings.FontSize = 0;
        Settings.FontBold = false;
        ApplyTextFont();
        SaveSettings();
        Announce("Windows text size", true);
    }

    /// <summary>The Windows font picker, one of the system's own windows; the choice is said.</summary>
    protected void ChooseFont()
    {
        using var dialog = new FontDialog
        {
            Font = TextFont(),
            ShowEffects = false,
            FontMustExist = true,
            MinSize = 6,
            MaxSize = 72,
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        Settings.FontFamily = dialog.Font.FontFamily.Name;
        Settings.FontSize = dialog.Font.SizeInPoints;
        Settings.FontBold = dialog.Font.Bold;
        ApplyTextFont();
        SaveSettings();
        Announce($"Font {dialog.Font.FontFamily.Name} {Math.Round(dialog.Font.SizeInPoints)} point", true);
    }
}
