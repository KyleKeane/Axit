namespace Axit.Core;

/// <summary>
/// What every window of the bundle remembers between runs (docs/axit/SPEC.md AX-5.3, AX-6.5): the text font, the
/// update check and the window's place. Each app's settings class has these among its own and implements this, so
/// that the shared window base (<c>Axit.Forms/AxitForm</c>) reads and writes them without knowing the app; the file
/// keeps the app's own shape.
/// </summary>
public interface IWindowSettings
{
    /// <summary>Null means the Windows system font (which follows the text size setting under Accessibility).</summary>
    string? FontFamily { get; set; }

    /// <summary>Point size; 0 means the system font size.</summary>
    float FontSize { get; set; }

    bool FontBold { get; set; }

    /// <summary>Ask GitHub once at startup whether a newer release exists (AX-3).</summary>
    bool CheckForUpdates { get; set; }

    WindowPlacement? Window { get; set; }

    void Save(string path);
}
