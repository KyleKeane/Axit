using Axit.Core;

namespace AxTask.Core;

/// <summary>
/// Everything axTask remembers between runs (SPEC.md TK-7), in <c>%APPDATA%\Axit\axtask.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font, the update check and the window's place from
/// <see cref="WindowSettings"/>, the top folder, the list open last, and whether done tasks are hidden.
/// </summary>
public sealed class TaskSettings : WindowSettings
{
    /// <summary>The top folder of lists; null for <see cref="ItemFolder.DefaultFolder"/>.</summary>
    public string? Folder { get; set; }

    /// <summary>The list open last, from the top folder ("Home", "Work\Course plan"), opened again at the next start; null for the top.</summary>
    public string? LastList { get; set; }

    public bool HideDone { get; set; }

    public static string DefaultPath => AppPaths.SettingsFile("axtask");

    /// <summary>The top folder in use: the chosen one, or the default.</summary>
    public string TopFolder => Folder ?? ItemFolder.DefaultFolder;

    public static TaskSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<TaskSettings>(path, out error);
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

        if (string.IsNullOrWhiteSpace(LastList) || LastList.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(LastList))
        {
            LastList = null;
        }
    }
}
