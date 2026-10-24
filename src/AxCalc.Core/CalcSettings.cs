using Axit.Core;

namespace AxCalc.Core;

/// <summary>
/// Everything axCalc remembers between runs (SPEC.md AC-9.1), in <c>%APPDATA%\Axit\axcalc.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the font, the update check and the window's place from
/// <see cref="WindowSettings"/>, the angle unit, and, while Keep history is on, the history and the variables.
/// </summary>
public sealed class CalcSettings : WindowSettings
{
    public const int MaxHistory = 500;

    /// <summary>The trigonometric functions in degrees; radians when false (AC-3.3).</summary>
    public bool Degrees { get; set; } = true;

    /// <summary>Options → Keep history between runs (AC-9.1): off, the file holds no history and no variables.</summary>
    public bool KeepHistory { get; set; } = true;

    /// <summary>The history, oldest first, at most <see cref="MaxHistory"/>.</summary>
    public List<CalcEntry> History { get; set; } = [];

    public Dictionary<string, double> Variables { get; set; } = [];

    public static string DefaultPath => AppPaths.SettingsFile("axcalc");

    public static CalcSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<CalcSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>The window's history and variables into the settings, or nothing of them with Keep history off.</summary>
    public void Keep(IEnumerable<CalcEntry> history, IEnumerable<KeyValuePair<string, double>> variables)
    {
        History = KeepHistory ? history.TakeLast(MaxHistory).ToList() : [];
        Variables = KeepHistory ? variables.ToDictionary(pair => pair.Key, pair => pair.Value) : [];
    }

    private void Sanitize()
    {
        SanitizeWindow();
        History = (History ?? []).Where(entry => entry is { Expression: not null, Answer: not null } && double.IsFinite(entry.Value)).TakeLast(MaxHistory).ToList();
        Variables = (Variables ?? []).Where(pair => Calculator.IsVariableName(pair.Key) && double.IsFinite(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        if (!KeepHistory)
        {
            History = [];
            Variables = [];
        }
    }
}
