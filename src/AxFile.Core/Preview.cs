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

        if (length > Limit)
        {
            bytes = WithoutCutSequence(bytes);
        }

        return new PreviewText(TextDocument.FromBytes(bytes).Text, length > Limit);
    }

    /// <summary>
    /// The cut's last bytes without a UTF-8 sequence the cut split (a lead byte whose continuation bytes are beyond
    /// it), which the editors' strict decoding would take for a file that is not UTF-8 at all.
    /// </summary>
    public static byte[] WithoutCutSequence(byte[] bytes)
    {
        var end = bytes.Length;
        for (var i = bytes.Length - 1; i >= 0 && i >= bytes.Length - 4; i--)
        {
            if ((bytes[i] & 0xC0) == 0x80)
            {
                continue;
            }

            var needed = (bytes[i] & 0xE0) == 0xC0 ? 2 : (bytes[i] & 0xF0) == 0xE0 ? 3 : (bytes[i] & 0xF8) == 0xF0 ? 4 : 1;
            if (bytes.Length - i < needed)
            {
                end = i;
            }

            break;
        }

        return end == bytes.Length ? bytes : bytes[..end];
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
