using Axit.Core;

namespace AxFile.Core;

/// <summary>A file's text for the pane (SPEC.md AF-9.4), and whether only its first megabyte is there.</summary>
public sealed record PreviewText(string Text, bool Cut);

/// <summary>
/// What Preview shows of a file (SPEC.md AF-9.4): the text of a file the editors open (by its extension) or of any
/// file whose first bytes read as text, decoded as the editors decode it (<see cref="TextDocument"/>: the byte order
/// mark, the code page, the line endings), up to one megabyte; a longer file is cut there and says so.
/// </summary>
public static class Preview
{
    public const int Limit = 1024 * 1024;
    private const int Sample = 8192;

    /// <summary>The file's text, or null when the file is not text; a file over the limit gives its first megabyte, <c>Cut</c>.</summary>
    public static PreviewText? Read(string path)
    {
        using var stream = File.OpenRead(path);
        var length = stream.Length;
        var bytes = new byte[Math.Min(length, Limit)];
        stream.ReadExactly(bytes);
        if (!IsText(bytes, path))
        {
            return null;
        }

        return new PreviewText(TextDocument.FromBytes(bytes).Text, length > Limit);
    }

    /// <summary>
    /// Whether the bytes read as text: a file of the editors' kinds always is (AF-3.2), an empty file is, one with a
    /// UTF-16 mark is; any other is text when its first eight kilobytes hold no zero byte and few control characters.
    /// </summary>
    public static bool IsText(ReadOnlySpan<byte> bytes, string path)
    {
        if (FolderListing.WayOf(path) is OpenWay.AxDown or OpenWay.AxJSON || bytes.Length == 0)
        {
            return true;
        }

        if (bytes.Length >= 2 && ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF)))
        {
            return true;
        }

        var sample = bytes[..Math.Min(bytes.Length, Sample)];
        var control = 0;
        foreach (var b in sample)
        {
            if (b == 0)
            {
                return false;
            }

            if (b < 0x20 && b is not (9 or 10 or 12 or 13 or 27))
            {
                control++;
            }
        }

        return control * 20 < sample.Length;
    }
}
