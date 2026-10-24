using Axit.Core;

namespace AxProcess.Core;

/// <summary>
/// Everything axProcess remembers between runs (SPEC.md PR-7.1), in <c>%APPDATA%\Axit\axprocess.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font, the update check and the window's place from
/// <see cref="WindowSettings"/>, and the order of the Processes view.
/// </summary>
public sealed class ProcessSettings : WindowSettings
{
    public ProcessSort SortBy { get; set; } = ProcessSort.Memory;

    public static string DefaultPath => AppPaths.SettingsFile("axprocess");

    public static ProcessSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<ProcessSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    private void Sanitize()
    {
        SanitizeWindow();
        if (!Enum.IsDefined(SortBy))
        {
            SortBy = ProcessSort.Memory;
        }
    }
}
