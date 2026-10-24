using System.Text.Json.Nodes;
using Axit.Core;

namespace AxTask.Core;

/// <summary>
/// Links kept whole when an item or a list moves (docs/axit/item-apps.md IA-D3): a rename, an archive, a list renamed
/// or archived. A link is a path from its item's folder, so both the links to what moved and the links held by what
/// moved (whose folder may now be deeper) are written again.
/// </summary>
public static class Links
{
    /// <summary>
    /// The writes that keep every link under <paramref name="top"/> pointing where it did after <paramref name="from"/>
    /// (a file or a folder) moves to <paramref name="to"/>: one write per item whose links change, at the item's path
    /// after the move. <paramref name="skip"/> is an item the caller writes itself (the renamed one, its own text
    /// changed); <paramref name="items"/> are the items as read before the move.
    /// </summary>
    public static IReadOnlyList<FileChange> AfterMove(IReadOnlyList<ItemFile> items, string from, string to, string? skip = null)
    {
        var writes = new List<FileChange>();
        foreach (var item in items)
        {
            if (skip is not null && item.Path.Equals(skip, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var newPath = Remap(item.Path, from, to);
            var document = item.Document;
            var fields = document.Fields;
            for (var i = 0; i < fields.Count; i++)
            {
                if (fields[i].Type != FieldTypes.Link || FieldTypes.TextOf(fields[i].Value) is not { Length: > 0 } link)
                {
                    continue;
                }

                var target = ItemFolder.Resolve(item.Path, link);
                var newLink = ItemFolder.LinkTo(newPath, Remap(target, from, to));
                if (newLink != link)
                {
                    document = document.WithValue(i, JsonValue.Create(newLink));
                }
            }

            if (!ReferenceEquals(document, item.Document))
            {
                writes.Add(FileChange.Write(newPath, item.Text, document.ToText()));
            }
        }

        return writes;
    }

    /// <summary>"Washer", or "missing: Washer" when the link's file is not there (IA-D3).</summary>
    public static string Spoken(string fromItem, string link)
    {
        var target = ItemFolder.Resolve(fromItem, link);
        return ItemFolder.ReadItem(target) is { } item ? ItemFolder.NameOf(item) : "missing: " + Path.GetFileNameWithoutExtension(target);
    }

    /// <summary>Where a path is after the move: itself, or under <paramref name="to"/> when it was <paramref name="from"/> or under it.</summary>
    public static string Remap(string path, string from, string to)
    {
        if (path.Equals(from, StringComparison.OrdinalIgnoreCase))
        {
            return to;
        }

        var prefix = from.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? Path.Combine(to, path[prefix.Length..]) : path;
    }
}
