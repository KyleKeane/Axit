using System.Text.Json;
using System.Text.Json.Serialization;

namespace Axit.Core;

/// <summary>
/// How every app's settings file is read and written (docs/axit/SPEC.md AX-5.3): JSON with camel-case keys and enum
/// names, comments and trailing commas tolerated. A missing or unreadable file gives the defaults and, when it was
/// unreadable, an error text for the app to report once. Each app's settings class is a plain class of properties
/// with defaults; it calls these two methods and does its own sanity checks after loading.
/// </summary>
public static class SettingsFile
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Loads the file, or returns defaults. <paramref name="error"/> describes an unreadable file.</summary>
    public static T Load<T>(string path, out string? error) where T : class, new()
    {
        error = null;
        if (!File.Exists(path))
        {
            return new T();
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? new T();
        }
        catch (JsonException ex)
        {
            error = $"{path}: {ex.Message}";
            if (KeepBadCopy(path))
            {
                error += $" A copy is kept as {path}.bad.";
                // Logged here once per file, for every app, when the copy is made; the window says it too, as a notice.
                Log.Error("Settings not readable, using defaults: " + error);
            }

            return new T();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = $"{path}: {ex.Message}";
            Log.Error("Settings not readable, using defaults: " + error);
            return new T();
        }
    }

    /// <summary>
    /// The unreadable file copied next to itself as .bad for mending, since the app saves its settings soon after
    /// loading, which would write the defaults over the one typo. Once: not again while the copy is as new as the
    /// file (the Axit menu reads the recently closed list at every opening; until 2026-10-23 it copied and logged
    /// each time). False when the copy is there already or cannot be made.
    /// </summary>
    private static bool KeepBadCopy(string path)
    {
        var bad = path + ".bad";
        try
        {
            if (File.Exists(bad) && File.GetLastWriteTimeUtc(bad) >= File.GetLastWriteTimeUtc(path))
            {
                return false;
            }

            File.Copy(path, bad, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Writes the file atomically: a temporary file next to it is moved into place.</summary>
    public static void Save<T>(string path, T settings) where T : class
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporary, path, overwrite: true);
    }

    /// <summary>
    /// The first start under a new file name takes over the file of an earlier version (AX-5.2): copied, not moved,
    /// so that the earlier version still finds its own. Nothing happens when the new file exists or the old one does not.
    /// </summary>
    public static void TakeOverOldFile(string oldPath, string path)
    {
        if (File.Exists(path) || !File.Exists(oldPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(oldPath, path);
    }
}
