using System.Globalization;

namespace Axit.Core;

/// <summary>
/// One thing the user agreed to (AX-11.5): an access layer's first connection to a program (App "axClaude", Target
/// "Claude Code"), or the terms of an update (App <see cref="Consent.UpdateApp"/>, Target "Axit 3.8.0"), with Axit's
/// version at the time and the date.
/// </summary>
public sealed record ConsentRecord(string App, string Target, string Version, DateTime When);

/// <summary>The shape of the file: one list, oldest first.</summary>
public sealed class ConsentList
{
    public List<ConsentRecord> Items { get; set; } = [];
}

/// <summary>
/// What the user has agreed to (docs/axit/SPEC.md AX-11.4 to AX-11.8), in one file for the bundle
/// (<see cref="AppPaths.ConsentFile"/>). An access layer asks <see cref="Given"/> before it connects to a program and
/// calls <see cref="Record"/> when the user checks the box and connects; About, Consents lists <see cref="Read"/> and
/// <see cref="Withdraw"/> takes one back, so that the app asks again. A missing or damaged file means nothing was agreed,
/// so the user is asked again rather than taken to have agreed. Tested in <c>tests/Axit.Tests</c>.
/// </summary>
public static class Consent
{
    /// <summary>The App of an update's acceptance (AX-11.7).</summary>
    public const string UpdateApp = "Update";

    public static IReadOnlyList<ConsentRecord> Read(string file) => SettingsFile.Load<ConsentList>(file, out _).Items;

    public static bool Given(string file, string app, string target) => Read(file).Any(item => Same(item, app, target));

    /// <summary>Keeps the agreement, replacing an earlier one for the same app and program. A file that cannot be written is logged: the user is then asked again next time.</summary>
    public static void Record(string file, string app, string target, string version, DateTime when)
    {
        var items = Read(file).Where(item => !Same(item, app, target)).Append(new ConsentRecord(app, target, version, when));
        Write(file, items);
        Log.Info($"Consent recorded: {app}, {target}, Axit {version}");
    }

    public static void Withdraw(string file, string app, string target)
    {
        Write(file, Read(file).Where(item => !Same(item, app, target)));
        Log.Info($"Consent withdrawn: {app}, {target}");
    }

    /// <summary>"axClaude, Claude Code, 29 October 2026, Axit 3.8.0"; "Update to Axit 3.8.0, 29 October 2026".</summary>
    public static string Line(ConsentRecord item)
    {
        var date = item.When.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        return item.App == UpdateApp ? $"Update to {item.Target}, {date}" : $"{item.App}, {item.Target}, {date}, Axit {item.Version}";
    }

    private static void Write(string file, IEnumerable<ConsentRecord> items)
    {
        try
        {
            SettingsFile.Save(file, new ConsentList { Items = [.. items] });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("The consent file was not written", ex);
        }
    }

    private static bool Same(ConsentRecord item, string app, string target) =>
        string.Equals(item.App, app, StringComparison.OrdinalIgnoreCase) && string.Equals(item.Target, target, StringComparison.OrdinalIgnoreCase);
}
