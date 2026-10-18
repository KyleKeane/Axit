namespace Axit.Core;

/// <summary>A top-level window of one of the bundle's processes: its handle and its title.</summary>
public sealed record AxitWindow(nint Handle, string Title)
{
    /// <summary>The app the title ends with ("notes.md - AxDown" is AxDown's); the whole title when it has no " - ".</summary>
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
        return at < 0 ? title : title[(at + 3)..];
    }

    /// <summary>The Open windows list: the windows of the bundle's apps other than the current one, most recently used first.</summary>
    public static IReadOnlyList<AxitWindow> Others(IReadOnlyList<AxitWindow> windows, nint current) =>
        windows.Where(w => w.Handle != current && IsApp(w)).ToList();

    /// <summary>The app's windows, the current one included, most recently used first.</summary>
    public static IReadOnlyList<AxitWindow> Of(IReadOnlyList<AxitWindow> windows, App app) =>
        windows.Where(w => w.App == app.Name).ToList();

    /// <summary>
    /// "&amp;1 notes.md - AxDown": the item's number as its mnemonic, 1 to 9 and then a to z, and the window's title;
    /// an ampersand in a title is doubled, as a menu needs.
    /// </summary>
    public static string Label(int index, string title)
    {
        var text = title.Replace("&", "&&");
        var mnemonic = index < 9 ? (char)('1' + index) : index < 35 ? (char)('a' + index - 9) : '\0';
        return mnemonic == '\0' ? text : $"&{mnemonic} {text}";
    }

    /// <summary>"Ax&amp;Claude": the app's name with the letter after Ax as the mnemonic.</summary>
    public static string AppLabel(App app) =>
        app.Name.StartsWith("Ax", StringComparison.Ordinal) ? "Ax&" + app.Name[2..] : "&" + app.Name;

    private static bool IsApp(AxitWindow window) => Dispatch.Apps.Any(a => a.Name == window.App);
}
