namespace Axit.Core;

/// <summary>
/// What every window of the bundle remembers between runs (docs/axit/SPEC.md AX-5.3, AX-6.5): the text font, the
/// update check and the window's place. Each app's settings class derives from it and adds its own; the file keeps
/// the app's shape, since every property serializes under its own name. The window base (<c>Axit.Forms/AxitForm</c>)
/// reads and writes these without knowing the app.
/// </summary>
public abstract class WindowSettings
{
    /// <summary>Null means the Windows system font (which follows the text size setting under Accessibility).</summary>
    public string? FontFamily { get; set; }

    /// <summary>Point size; 0 means the system font size.</summary>
    public float FontSize { get; set; }

    public bool FontBold { get; set; }

    /// <summary>Ask GitHub once at startup whether a newer release exists (AX-3).</summary>
    public bool CheckForUpdates { get; set; } = true;

    public WindowPlacement? Window { get; set; }

    /// <summary>Into the file; each class writes its own type, so that the serializer sees every property, not only these.</summary>
    public abstract void Save(string path);

    /// <summary>The common fields back to their defaults when the file holds nonsense; each class's Sanitize calls it first.</summary>
    protected void SanitizeWindow()
    {
        if (FontSize is < 0 or > 96)
        {
            FontSize = 0;
        }

        if (string.IsNullOrWhiteSpace(FontFamily))
        {
            FontFamily = null;
        }

        if (Window is { } w && (w.Width < 200 || w.Height < 150))
        {
            Window = null;
        }
    }
}
