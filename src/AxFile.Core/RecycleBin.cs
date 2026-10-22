using System.Buffers.Binary;
using System.Text;

namespace AxFile.Core;

/// <summary>An item of the Recycle Bin as its record tells it: where it came from and when it was deleted (a FILETIME).</summary>
public sealed record RecycleRecord(string OriginalPath, long DeletedAt);

/// <summary>
/// What the Recycle Bin remembers about an item (SPEC.md AF-4.14): a deleted file lives in the bin's folder as
/// <c>$R</c> followed by a random name, and next to it a <c>$I</c> file of the same name holds the original path and
/// the time of the deletion. The window reads the record of each item the shell lists to find the ones it deleted.
/// </summary>
public static class RecycleBin
{
    private const long Version2 = 2;
    private const int HeaderSize = 24;

    /// <summary>The record of the item whose <c>$R…</c> path this is, read from its <c>$I…</c> file; null when the path is not such an item or the record cannot be read.</summary>
    public static RecycleRecord? ReadRecord(string recycledPath)
    {
        var name = Path.GetFileName(recycledPath);
        if (!name.StartsWith("$R", StringComparison.Ordinal) || Path.GetDirectoryName(recycledPath) is not { } folder)
        {
            return null;
        }

        try
        {
            return Parse(File.ReadAllBytes(Path.Combine(folder, "$I" + name[2..])));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// The record's bytes: a version (8 bytes), the size (8), the deletion time (8), then the path, with its length in
    /// characters (4 bytes) before it from version 2 (Windows 10) and as a fixed field before that; null when the
    /// bytes are not a record.
    /// </summary>
    public static RecycleRecord? Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length <= HeaderSize)
        {
            return null;
        }

        var version = BinaryPrimitives.ReadInt64LittleEndian(bytes);
        var deleted = BinaryPrimitives.ReadInt64LittleEndian(bytes[16..]);
        string path;
        if (version == Version2)
        {
            if (bytes.Length < HeaderSize + 4)
            {
                return null;
            }

            var length = BinaryPrimitives.ReadInt32LittleEndian(bytes[HeaderSize..]);
            var available = bytes.Length - HeaderSize - 4;
            path = Encoding.Unicode.GetString(bytes.Slice(HeaderSize + 4, Math.Clamp(length * 2, 0, available)));
        }
        else
        {
            path = Encoding.Unicode.GetString(bytes[HeaderSize..]);
        }

        var end = path.IndexOf('\0');
        if (end >= 0)
        {
            path = path[..end];
        }

        return path.Length == 0 ? null : new RecycleRecord(path, deleted);
    }
}
