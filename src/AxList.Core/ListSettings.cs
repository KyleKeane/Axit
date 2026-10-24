using Axit.Core;

namespace AxList.Core;

/// <summary>
/// Everything axList remembers between runs (SPEC.md LI-8.1), in <c>%APPDATA%\Axit\axlist.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font, the update check and the window's place from
/// <see cref="WindowSettings"/>, the folder of lists, the list open last, and whether done tasks are hidden.
/// </summary>
public sealed class ListSettings : WindowSettings
{
    /// <summary>The folder of lists; null for <see cref="ListFolder.DefaultFolder"/>.</summary>
    public string? Folder { get; set; }

    /// <summary>The file name of the list open last, opened again at the next start; null for none.</summary>
    public string? LastList { get; set; }

    public bool HideDone { get; set; }

    public static string DefaultPath => AppPaths.SettingsFile("axlist");

    /// <summary>The folder in use: the chosen one, or the default.</summary>
    public string ListsFolder => Folder ?? ListFolder.DefaultFolder;

    public static ListSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<ListSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    private void Sanitize()
    {
        SanitizeWindow();
        if (string.IsNullOrWhiteSpace(Folder))
        {
            Folder = null;
        }

        if (string.IsNullOrWhiteSpace(LastList) || LastList.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            LastList = null;
        }
    }
}
