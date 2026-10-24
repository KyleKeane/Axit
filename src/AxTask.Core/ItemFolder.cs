using Axit.Core;

namespace AxTask.Core;

/// <summary>An item read from its file: where it is, its document, and the text it was read from (for the check against the file before a change, IA-5.5).</summary>
public sealed record ItemFile(string Path, ItemDocument Document, string Text);

/// <summary>A list's items (IA-2.2), and how many files and folders in it are not shown (IA-4.6; a folder inside a list, TK-D4).</summary>
public sealed record FolderLevel(IReadOnlyList<ItemFile> Items, int NotShown);

/// <summary>The top folder (TK-3a): its lists by name, the Archive's lists, how many items the Archive holds of its own, and what is not shown (items kept in the top folder itself, files that are not filters).</summary>
public sealed record TopLevel(IReadOnlyList<string> Lists, IReadOnlyList<string> ArchivedLists, int ArchivedItems, int NotShown);
/// <summary>
/// The folder of lists on disk (docs/axit/item-apps.md IA-4): a list is a folder, its details <c>list.json</c> in it,
/// an item a JSON file named after its Title. The archive is the top folder's <c>Archive</c> list (IA-D4), and the
/// smart lists are the top folder's <c>.filter.json</c> files (TK-5.2).
/// </summary>
public static class ItemFolder
{
    public const string ArchiveName = "Archive";
    public const string FilterExtension = ".filter.json";

    public static string DefaultFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Tasks");

    /// <summary>A list's items by title (TK-D4: a folder inside it is not shown, and counted), hidden and dot entries left out.</summary>
    public static FolderLevel Read(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return new FolderLevel([], 0);
        }

        var notShown = new DirectoryInfo(folder).EnumerateDirectories().Count(directory => !Hidden(directory));
        var items = new List<ItemFile>();
        foreach (var file in new DirectoryInfo(folder).EnumerateFiles())
        {
            if (Hidden(file) || file.Name.Equals(ItemDocument.ListFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (ReadItem(file.FullName) is { } item)
            {
                items.Add(item);
            }
            else
            {
                notShown++;
            }
        }

        items.Sort((a, b) => NaturalOrder.Instance.Compare(NameOf(a), NameOf(b)));
        return new FolderLevel(items, notShown);
    }

    /// <summary>The top folder's lists and the Archive's (TK-3a.1), each by name.</summary>
    public static TopLevel ReadTop(string top)
    {
        if (!Directory.Exists(top))
        {
            return new TopLevel([], [], 0, 0);
        }

        var lists = new List<string>();
        foreach (var directory in new DirectoryInfo(top).EnumerateDirectories())
        {
            if (!Hidden(directory) && !directory.Name.Equals(ArchiveName, StringComparison.OrdinalIgnoreCase))
            {
                lists.Add(directory.FullName);
            }
        }

        var notShown = new DirectoryInfo(top).EnumerateFiles()
            .Count(file => !Hidden(file) && !file.Name.EndsWith(FilterExtension, StringComparison.OrdinalIgnoreCase) && !file.Name.Equals(ItemDocument.ListFile, StringComparison.OrdinalIgnoreCase));
        var archive = ArchiveFolder(top);
        var archived = new List<string>();
        var archivedItems = 0;
        if (Directory.Exists(archive))
        {
            archived.AddRange(new DirectoryInfo(archive).EnumerateDirectories().Where(directory => !Hidden(directory)).Select(directory => directory.FullName));
            archivedItems = Read(archive).Items.Count;
        }

        lists.Sort((a, b) => NaturalOrder.Instance.Compare(Path.GetFileName(a), Path.GetFileName(b)));
        archived.Sort((a, b) => NaturalOrder.Instance.Compare(Path.GetFileName(a), Path.GetFileName(b)));
        return new TopLevel(lists, archived, archivedItems, notShown);
    }

    /// <summary>A list's section (TK-3a.3): its details' Section field, trimmed; empty for User lists.</summary>
    public static string SectionOf(string list) =>
        ReadList(list) is { } details && details.IndexOf(SectionField) is var i && i >= 0 ? FieldTypes.TextOf(details.Fields[i].Value).Trim() : string.Empty;

    /// <summary>The field of a list's details that names its section (TK-3a.3).</summary>
    public const string SectionField = "Section";
    /// <summary>The item in a file; null when the file is not a JSON item or cannot be read (IA-4.6).</summary>
    public static ItemFile? ReadItem(string path)
    {
        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var text = TextDocument.Read(path).Text;
            return new ItemFile(path, ItemDocument.Parse(text), text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>A list's details from its <c>list.json</c>; null when there is none or it is not a list (IA-4.4).</summary>
    public static ItemDocument? ReadList(string folder) =>
        ReadItem(Path.Combine(folder, ItemDocument.ListFile))?.Document;

    /// <summary>The item's name: its Title, or its file name when the Title is empty.</summary>
    public static string NameOf(ItemFile item) =>
        item.Document.Title.Trim() is { Length: > 0 } title ? title : Path.GetFileNameWithoutExtension(item.Path);

    /// <summary>The file name a Title gives (IA-4.5): the characters Windows forbids as spaces, no full stop at the end, "Untitled" for nothing.</summary>
    public static string FileStem(string title)
    {
        var chars = title.Trim().Select(c => Path.GetInvalidFileNameChars().Contains(c) || char.IsControl(c) ? ' ' : c).ToArray();
        var stem = string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.', ' ');
        if (stem.Length > 120)
        {
            stem = stem[..120].TrimEnd('.', ' ');
        }

        return stem.Length == 0 ? "Untitled" : stem;
    }

    /// <summary>"name.json", or "name - 2.json" when taken (IA-4.5); <paramref name="own"/> is the item's own file, which never counts as taken.</summary>
    public static string FreePath(string folder, string stem, string extension, string? own = null)
    {
        for (var n = 1; ; n++)
        {
            var path = Path.Combine(folder, n == 1 ? stem + extension : $"{stem} - {n}{extension}");
            if (own is not null && path.Equals(own, StringComparison.OrdinalIgnoreCase))
            {
                return own;
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return path;
            }
        }
    }

    /// <summary>Why a new list's name is refused; null when it is fine.</summary>
    public static string? RefusedListName(string folder, string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return "Type the list's name";
        }

        if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || trimmed.EndsWith('.') || trimmed.StartsWith('.'))
        {
            return "A list's name cannot hold \\ / : * ? \" < > | or start or end in a full stop";
        }

        return Directory.Exists(Path.Combine(folder, trimmed)) || File.Exists(Path.Combine(folder, trimmed))
            ? $"There is a list called {trimmed} here already"
            : null;
    }

    /// <summary>Every item in a list of the top folder, the Archive left out (TK-5.1).</summary>
    public static IReadOnlyList<ItemFile> AllItems(string top) =>
        ReadTop(top).Lists.SelectMany(list => Read(list).Items).ToList();
    public static string ArchiveFolder(string top) => Path.Combine(top, ArchiveName);

    /// <summary>Whether a path is the Archive or inside it.</summary>
    public static bool IsArchive(string top, string path)
    {
        var archive = ArchiveFolder(top);
        return path.Equals(archive, StringComparison.OrdinalIgnoreCase) || path.StartsWith(archive + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>"Home", "Work, Course plan": the list a path is in, from the top folder; "the top" for the top itself.</summary>
    public static string ListName(string top, string folder)
    {
        var relative = Path.GetRelativePath(top, folder);
        return relative == "." ? "the top" : relative.Replace(Path.DirectorySeparatorChar.ToString(), ", ");
    }

    /// <summary>A link's path (IA-D3): from the item's own folder to the other item, with forward slashes.</summary>
    public static string LinkTo(string fromItem, string toItem) =>
        Path.GetRelativePath(Path.GetDirectoryName(fromItem)!, toItem).Replace('\\', '/');

    /// <summary>The file a link names, from the item that holds it.</summary>
    public static string Resolve(string fromItem, string link) =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fromItem)!, link.Replace('/', Path.DirectorySeparatorChar)));

    private static bool Hidden(FileSystemInfo entry) =>
        entry.Name.StartsWith('.') || (entry.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0;
}
