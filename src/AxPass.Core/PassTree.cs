using Axit.Core;

namespace AxPass.Core;

/// <summary>A group or an entry of the database: its full path ("Email/Gmail"), the group it is in ("Email"; empty at the top), its name.</summary>
public sealed record PassNode(string Path, string Group, string Name, bool IsGroup);

/// <summary>
/// The database's groups and entries as <c>ls -R -f</c> lists them (docs/axpass/kdbx-notes.md section 8): one line per
/// group and entry with its full path, a group ending in a slash, an empty group's line <c>group/[empty]</c>. The
/// levels of the navigator come from it (docs/axpass/SPEC.md section 1).
/// </summary>
public sealed class PassTree
{
    public const string RecycleBin = "Recycle Bin";

    private const string EmptyMark = "[empty]";

    private readonly List<PassNode> _nodes;

    private PassTree(List<PassNode> nodes)
    {
        _nodes = nodes;
    }

    public IReadOnlyList<PassNode> Nodes => _nodes;

    public static PassTree Parse(string listing)
    {
        var nodes = new List<PassNode>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in listing.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.Length == 0)
            {
                continue;
            }

            var isGroup = line.EndsWith('/');
            var path = isGroup ? line[..^1] : line;
            if (!isGroup && (path == EmptyMark || path.EndsWith("/" + EmptyMark, StringComparison.Ordinal)))
            {
                continue;
            }

            if (!seen.Add((isGroup ? "g:" : "e:") + path))
            {
                continue;
            }

            var slash = path.LastIndexOf('/');
            nodes.Add(new PassNode(path, slash < 0 ? string.Empty : path[..slash], slash < 0 ? path : path[(slash + 1)..], isGroup));
        }

        return new PassTree(nodes);
    }

    /// <summary>A group's level: its groups, the Recycle Bin last at the top, then its entries, each in the database's order.</summary>
    public IReadOnlyList<PassNode> Level(string group)
    {
        var inside = _nodes.Where(node => node.Group == group).ToList();
        var groups = inside.Where(node => node.IsGroup).OrderBy(node => group.Length == 0 && node.Name == RecycleBin ? 1 : 0).ToList();
        return [.. groups, .. inside.Where(node => !node.IsGroup)];
    }

    /// <summary>The groups and entries directly in a group.</summary>
    public int CountIn(string group) => _nodes.Count(node => node.Group == group);

    public bool HasGroup(string group) => group.Length == 0 || _nodes.Any(node => node.IsGroup && node.Path == group);

    public bool HasEntry(string path) => _nodes.Any(node => !node.IsGroup && node.Path == path);

    /// <summary>Whether a path is the Recycle Bin or in it.</summary>
    public static bool InRecycleBin(string path) => path == RecycleBin || path.StartsWith(RecycleBin + "/", StringComparison.Ordinal);

    /// <summary>"Email/Gmail" from a group and a name; the name alone at the top.</summary>
    public static string Join(string group, string name) => group.Length == 0 ? name : group + "/" + name;

    /// <summary>Why a title or a group's name is refused (the tool's paths have no escape for a slash, kdbx-notes 8); null when it is fine.</summary>
    public static string? RefusedName(string name) =>
        name.Trim().Length == 0 ? "Type a name"
        : name.Contains('/') ? "A name cannot hold a slash: KeePassXC's tool reads it as a group"
        : null;

    /// <summary>"mine": the database's name for the window, its file name without .kdbx.</summary>
    public static string DatabaseName(string path) => System.IO.Path.GetFileNameWithoutExtension(path);
}
