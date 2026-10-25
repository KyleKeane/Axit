using System.Text;
using Axit.Core;

namespace AxPreview.Core;

/// <summary>Where Ctrl+E goes from a file (SPEC.md AP-7.3): its editor, axMedia, or the program Windows has for it.</summary>
public enum WayOut
{
    Editor,
    Player,
    Windows,
}

/// <summary>
/// What the content pane shows of a file (SPEC.md AP-7.1): the text as a document (a text file's own, with its
/// encoding and line ending; any other kind's as UTF-8 with Windows line endings), the renderer's name, the lines of
/// the details' Contents group (AP-6.2) and the way out.
/// </summary>
public sealed record Rendered(TextDocument Document, string Kind, IReadOnlyList<string> Contents, WayOut WayOut)
{
    /// <summary>A rendered text of another kind than text: the lines joined with Windows line endings.</summary>
    public static Rendered Of(IEnumerable<string> lines, string kind, IReadOnlyList<string> contents, WayOut wayOut) =>
        new(TextDocument.FromBytes(Encoding.UTF8.GetBytes(string.Join("\r\n", lines))), kind, contents, wayOut);
}

/// <summary>
/// One renderer of the table (SPEC.md AP-7.1): its name, the extensions it takes, an optional test of the first bytes,
/// and the function that renders a file. <see cref="Takes"/> is the extension or the bytes; the last renderer of the
/// table takes every file.
/// </summary>
public sealed record Renderer(string Kind, IReadOnlySet<string> Extensions, Func<string, ReadOnlyMemory<byte>, bool>? Starts, Func<string, Rendered> Render)
{
    public bool Takes(string path, ReadOnlyMemory<byte> head) =>
        Extensions.Contains(Path.GetExtension(path)) || (Starts is { } starts && starts(path, head));
}

/// <summary>
/// The renderers in order, first match (SPEC.md AP-7.2, AP-D9): text, the documents, zip archives, pictures and
/// media, and the readable text of anything else. Adding a kind is one entry here and its tests
/// (<c>tests/AxPreview.Tests/RenderersTests</c>); nothing in the window changes.
/// </summary>
public static class Renderers
{
    /// <summary>The first bytes read to choose a renderer, as many as the text rule looks at.</summary>
    private const int Head = 8192;

    /// <summary>The picture kinds the media renderer names (their content is not text; Windows' details say what they are); before the renderers, which read it as they are made.</summary>
    public static readonly IReadOnlyList<string> PictureExtensions = [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tif", ".tiff", ".webp", ".heic", ".ico"];

    public static readonly Renderer Text = new("Text", Set(), (path, head) => TextSniff.IsText(head.Span, path), RenderText);

    public static readonly Renderer Document = new("Document", Set(".docx", ".pptx", ".odt"), null, OfficeText.Render);

    public static readonly Renderer Archive = new("Zip archive", Set(".zip"), (_, head) => IsZip(head.Span), ZipListing.Render);

    public static readonly Renderer Media = new("Picture or media", Set([.. MediaFiles.SoundExtensions, .. MediaFiles.VideoExtensions, .. PictureExtensions]), null, RenderMedia);

    public static readonly Renderer Readable = new("Readable text", Set(), (_, _) => true, ReadableText.Render);

    /// <summary>The table, in the order it is asked.</summary>
    public static readonly IReadOnlyList<Renderer> All = [Text, Document, Archive, Media, Readable];

    /// <summary>The renderer that takes the file: the first of <see cref="All"/>, by its extension or its first bytes.</summary>
    public static Renderer For(string path)
    {
        var head = ReadHead(path);
        return All.First(renderer => renderer.Takes(path, head));
    }

    /// <summary>
    /// The file rendered by the renderer that takes it. A file its renderer cannot read (a damaged zip, a document whose
    /// XML is broken) is shown as its readable text, under a first line that says why, rather than refused: every file
    /// opens (AP-7.5). A file that cannot be opened at all is left to the caller, which reports it.
    /// </summary>
    public static Rendered Render(string path)
    {
        var renderer = For(path);
        try
        {
            return renderer.Render(path);
        }
        catch (Exception ex) when (renderer != Text && renderer != Readable && ex is InvalidDataException or System.Xml.XmlException)
        {
            return ReadableText.Render(path, $"{Path.GetFileName(path)} could not be read as a {renderer.Kind.ToLowerInvariant()} ({ex.Message}); the readable text inside it follows.");
        }
    }

    private static IReadOnlySet<string> Set(params string[] extensions) => new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

    private static byte[] ReadHead(string path)
    {
        using var stream = File.OpenRead(path);
        var bytes = new byte[Math.Min(stream.Length, Head)];
        stream.ReadExactly(bytes);
        return bytes;
    }

    /// <summary>A zip's local file header, or the end record alone of an empty zip.</summary>
    private static bool IsZip(ReadOnlySpan<byte> head) =>
        head.Length >= 4 && head[0] == 'P' && head[1] == 'K' && ((head[2] == 3 && head[3] == 4) || (head[2] == 5 && head[3] == 6));

    /// <summary>A text file (AP-7.2, 1): read as the editors read it, with the counts of the Contents group.</summary>
    private static Rendered RenderText(string path)
    {
        var document = TextDocument.Read(path);
        var text = document.Text;
        var lines = text.Length == 0 ? 0 : text.Split('\n').Length;
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return new Rendered(document, Text.Kind,
        [
            $"Encoding: {document.EncodingName}",
            $"Byte order mark: {(document.ByteOrderMark ? "yes" : "no")}",
            $"Line ending: {document.LineEndingName}",
            $"Lines: {lines:N0}",
            $"Words: {words:N0}",
            $"Characters: {text.Length:N0}",
        ], WayOut.Editor);
    }

    /// <summary>A picture, a sound or a video (AP-7.2, 4): what it is, where its details are, and for media where it plays.</summary>
    private static Rendered RenderMedia(string path)
    {
        var name = Path.GetFileName(path);
        var extension = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
        var sound = MediaFiles.SoundExtensions.Contains(Path.GetExtension(path));
        var video = MediaFiles.VideoExtensions.Contains(Path.GetExtension(path));
        var kind = sound ? "a sound file" : video ? "a video" : "a picture";
        var lines = new List<string>
        {
            $"{name} is {kind} ({extension}). Its details are in the Details list below: Tab or F6 moves there.",
        };
        lines.Add(sound || video ? "Ctrl+E plays it in axMedia." : "Ctrl+E opens it in the program Windows has for it.");
        return Rendered.Of(lines, sound ? "Sound" : video ? "Video" : "Picture", [], sound || video ? WayOut.Player : WayOut.Windows);
    }
}
