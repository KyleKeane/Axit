namespace Axit.Core;

/// <summary>A top-level window of one of the bundle's processes: its handle and its title.</summary>
public sealed record AxitWindow(nint Handle, string Title)
{
    /// <summary>The app the title ends with ("notes.md - axDown" is axDown's); the whole title when it has no " - ".</summary>
    public string App => AxitWindows.AppOf(Title);
}

/// <summary>
/// The window lists of the Axit menu (docs/axit/SPEC.md AX-9), pure: which windows go under Open windows and under
/// each app, and what an item says. The windows come in the order Windows keeps its top-level windows, the most recently used
/// first, which is Alt+Tab's order, so recency needs nothing stored. Tested in <c>tests/Axit.Tests</c>.
/// </summary>
public static class AxitWindows
{
    public static string AppOf(string title)
    {
        var at = title.LastIndexOf(" - ", StringComparison.Ordinal);
        var app = at < 0 ? title : title[(at + 3)..];
        // An access layer's title ends with its whole title, "axClaude, access layer for Claude Code" (AX-11.2); no app's name has a comma.
        var comma = app.IndexOf(", ", StringComparison.Ordinal);
        return comma < 0 ? app : app[..comma];
    }

    /// <summary>The Open windows list: the windows of the bundle's apps other than the current one, most recently used first.</summary>
    public static IReadOnlyList<AxitWindow> Others(IReadOnlyList<AxitWindow> windows, nint current) =>
        windows.Where(w => w.Handle != current && IsApp(w)).ToList();

    /// <summary>The app's windows, the current one included, most recently used first.</summary>
    public static IReadOnlyList<AxitWindow> Of(IReadOnlyList<AxitWindow> windows, App app) =>
        windows.Where(w => string.Equals(w.App, app.Name, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// "&amp;1. notes.md - axDown": the item's number as its mnemonic, 1 to 9 and then a to z, a full stop, which the
    /// synthesizer takes as a pause before the name (AX-8.5), and the title; an ampersand in a title is doubled, as a
    /// menu needs. The same shape serves the apps' own recent lists (FR-8.5, AD-4.1).
    /// </summary>
    public static string Label(int index, string title)
    {
        var text = title.Replace("&", "&&");
        var mnemonic = index < 9 ? (char)('1' + index) : index < 35 ? (char)('a' + index - 9) : '\0';
        return mnemonic == '\0' ? text : $"&{mnemonic}. {text}";
    }

    /// <summary>"ax&amp;Claude, access layer for Claude Code": the app's title (AX-11.2) with the letter after ax as the mnemonic.</summary>
    public static string AppLabel(App app) =>
        app.Title.StartsWith("ax", StringComparison.OrdinalIgnoreCase) ? app.Title[..2] + "&" + app.Title[2..] : "&" + app.Title;

    private static bool IsApp(AxitWindow window) => Dispatch.Apps.Any(a => string.Equals(a.Name, window.App, StringComparison.OrdinalIgnoreCase));
}
