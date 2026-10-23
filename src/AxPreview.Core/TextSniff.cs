using Axit.Core;

namespace AxPreview.Core;

/// <summary>Why a file is not opened (SPEC.md AP-2.2): the notice's title and text.</summary>
public sealed record Refusal(string Title, string Text);

/// <summary>
/// Whether a file reads as text (SPEC.md AP-2.2): a file of the editors' kinds always does, an empty file does, one
/// with a UTF-16 mark does; any other is text when its first eight kilobytes hold no zero byte and few control
/// characters. The rule came from axFile's preview pane (2026-10-22). Tested in <c>tests/AxPreview.Tests</c>.
/// </summary>
public static class TextSniff
{
    /// <summary>The editors' limit (<see cref="TextDocument.MaxBytes"/>), in the viewer's own words.</summary>
    public const long MaxBytes = TextDocument.MaxBytes;

    private const int Sample = 8192;

    /// <summary>The kinds the editors open, text whatever their bytes say (axDown's and axJSON's).</summary>
    public static readonly IReadOnlySet<string> TextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".md", ".markdown", ".txt", ".json" };

    /// <summary>Whether the bytes read as text, by the rule above; <paramref name="path"/> for the extension.</summary>
    public static bool IsText(ReadOnlySpan<byte> bytes, string path)
    {
        if (TextExtensions.Contains(Path.GetExtension(path)) || bytes.Length == 0)
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

    /// <summary>
    /// Why the file at <paramref name="path"/> is not shown, or null when it is: over the editors' limit ("<name> is
    /// 33,554,433 bytes, over the 32 MB a window can open.", the editors' wording since 2026-10-23), or not text ("<name> is not a text file. axFile's Enter opens it in
    /// the program Windows has for it."). A file that cannot be read is left to the reader, which reports it.
    /// </summary>
    public static Refusal? Refuse(string path)
    {
        var name = Path.GetFileName(path);
        try
        {
            using var stream = File.OpenRead(path);
            if (stream.Length > MaxBytes)
            {
                return new Refusal("Too large", $"{name} is {stream.Length:N0} bytes, over the {MaxBytes / (1024 * 1024)} MB a window can open.");
            }

            var bytes = new byte[Math.Min(stream.Length, Sample)];
            stream.ReadExactly(bytes);
            return IsText(bytes, path) ? null : new Refusal("Not a text file", $"{name} is not a text file. axFile's Enter opens it in the program Windows has for it.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
