namespace Axit.Core;

/// <summary>A file or folder a window of the bundle held when it closed or replaced it: the app's name and the path.</summary>
public sealed record ClosedItem(string App, string Path);

/// <summary>The shape of the file: one list, the newest first.</summary>
public sealed class RecentlyClosedList
{
    public List<ClosedItem> Items { get; set; } = [];
}

/// <summary>
/// The Recently closed list of the Axit menu (docs/axit/SPEC.md AX-9.4): the last ten files and folders the windows of
/// every app held when they closed or swapped them, in one file for the bundle (<see cref="AppPaths.RecentlyClosedFile"/>,
/// AX-5.1). A window calls <see cref="Remember"/> when it closes or replaces its file; the menu calls <see cref="Load"/>
/// when it opens. The file is written whole through <see cref="SettingsFile"/>; two windows closing in the same instant
/// leave the last writer's list, which is accepted. Tested in <c>tests/Axit.Tests</c>.
/// </summary>
public static class RecentlyClosed
{
    public const int Capacity = 10;

    /// <summary>The list with the item at the front and its earlier entry, same app and path, gone; at most <see cref="Capacity"/> entries.</summary>
    public static IReadOnlyList<ClosedItem> Add(IReadOnlyList<ClosedItem> items, ClosedItem item) =>
        [item, .. items.Where(i => !Same(i, item)).Take(Capacity - 1)];

    /// <summary>Puts the app's file or folder at the front of the bundle's list. A file that cannot be written is logged and nothing else.</summary>
    public static void Remember(string app, string path)
    {
        try
        {
            Write(AppPaths.RecentlyClosedFile, Add(Read(AppPaths.RecentlyClosedFile), new ClosedItem(app, path)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("The recently closed list was not written", ex);
        }
    }

    /// <summary>The list for the menu: the entries whose file or folder still exists, the newest first.</summary>
    public static IReadOnlyList<ClosedItem> Load() =>
        Read(AppPaths.RecentlyClosedFile).Where(i => File.Exists(i.Path) || Directory.Exists(i.Path)).ToList();

    public static IReadOnlyList<ClosedItem> Read(string file) => SettingsFile.Load<RecentlyClosedList>(file, out _).Items;

    public static void Write(string file, IReadOnlyList<ClosedItem> items) =>
        SettingsFile.Save(file, new RecentlyClosedList { Items = [.. items] });

    /// <summary>"notes.md (C:\notes\notes.md) - AxDown", "myproject (C:\src\myproject) - AxClaude": the name, the full path in parentheses, the app (AX-9.5).</summary>
    public static string Title(ClosedItem item)
    {
        var name = System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(item.Path));
        return (name.Length == 0 ? item.Path : $"{name} ({item.Path})") + " - " + item.App;
    }

    private static bool Same(ClosedItem a, ClosedItem b) =>
        a.App == b.App && string.Equals(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
}
