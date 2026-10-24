using Axit.Core;

namespace AxTask.Core;

/// <summary>An item read from its file: where it is, its document, and the text it was read from (for the check against the file before a change, IA-5.5).</summary>
public sealed record ItemFile(string Path, ItemDocument Document, string Text);

/// <summary>A folder's level of the navigator (IA-2.2): its sub-lists, its items, and how many files are not shown (IA-4.6).</summary>
public sealed record FolderLevel(IReadOnlyList<string> Lists, IReadOnlyList<ItemFile> Items, int NotShown);

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

    /// <summary>A folder's level: the sub-lists in natural order (the Archive last at the top), the items by title, folders and files starting with a period or hidden left out.</summary>
    public static FolderLevel Read(string folder, bool isTop)
    {
        if (!Directory.Exists(folder))
        {
            return new FolderLevel([], [], 0);
        }

        var lists = new List<string>();
        foreach (var directory in new DirectoryInfo(folder).EnumerateDirectories())
        {
            if (!Hidden(directory))
            {
                lists.Add(directory.FullName);
            }
        }

        lists.Sort((a, b) => Order(a, b, isTop));
        var items = new List<ItemFile>();
        var notShown = 0;
        foreach (var file in new DirectoryInfo(folder).EnumerateFiles())
        {
            if (Hidden(file) || file.Name.Equals(ItemDocument.ListFile, StringComparison.OrdinalIgnoreCase)
                || (isTop && file.Name.EndsWith(FilterExtension, StringComparison.OrdinalIgnoreCase)))
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
        return new FolderLevel(lists, items, notShown);
    }

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

    /// <summary>Every item under a folder, the Archive left out (TK-5.1), with the list it is in.</summary>
    public static IReadOnlyList<ItemFile> AllItems(string top)
    {
        var items = new List<ItemFile>();
        if (!Directory.Exists(top))
        {
            return items;
        }

        var pending = new Stack<string>();
        pending.Push(top);
        while (pending.Count > 0)
        {
            var folder = pending.Pop();
            var level = Read(folder, folder.Equals(top, StringComparison.OrdinalIgnoreCase));
            items.AddRange(level.Items);
            foreach (var list in level.Lists)
            {
                if (!IsArchive(top, list))
                {
                    pending.Push(list);
                }
            }
        }

        return items;
    }

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

    private static int Order(string a, string b, bool isTop)
    {
        if (isTop)
        {
            var archiveA = Path.GetFileName(a).Equals(ArchiveName, StringComparison.OrdinalIgnoreCase);
            var archiveB = Path.GetFileName(b).Equals(ArchiveName, StringComparison.OrdinalIgnoreCase);
            if (archiveA != archiveB)
            {
                return archiveA ? 1 : -1;
            }
        }

        return NaturalOrder.Instance.Compare(Path.GetFileName(a), Path.GetFileName(b));
    }
}
