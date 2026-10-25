using System.IO.Compression;
using System.Text;
using AxPreview.Core;

namespace AxPreview.Tests;

/// <summary>The renderer table's choice and each renderer (docs/axpreview/SPEC.md AP-7), over files made in a temporary folder.</summary>
public sealed class RenderersTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axpreview-render-" + Guid.NewGuid().ToString("N"));

    public RenderersTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string Write(string name, byte[] bytes)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    /// <summary>A zip with the given parts, as Word, PowerPoint and OpenDocument files are.</summary>
    private string Zip(string name, params (string Entry, string Text)[] parts)
    {
        var path = Path.Combine(_folder, name);
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            foreach (var (entry, text) in parts)
            {
                using var writer = new StreamWriter(zip.CreateEntry(entry).Open(), new UTF8Encoding(false));
                writer.Write(text);
            }
        }

        return path;
    }

    private static string[] Lines(Rendered rendered) => rendered.Document.Text.Split("\r\n");

    [Fact]
    public void Each_kind_goes_to_its_renderer_and_the_last_takes_the_rest()
    {
        Assert.Same(Renderers.Text, Renderers.For(Write("notes.md", Encoding.UTF8.GetBytes("# Notes"))));
        Assert.Same(Renderers.Text, Renderers.For(Write("app.log", Encoding.UTF8.GetBytes("started\r\n"))));
        Assert.Same(Renderers.Text, Renderers.For(Write("empty.bin", [])));
        Assert.Same(Renderers.Document, Renderers.For(Zip("report.docx", ("word/document.xml", "<x/>"))));
        Assert.Same(Renderers.Archive, Renderers.For(Zip("bundle.zip", ("a.txt", "a"))));
        Assert.Same(Renderers.Archive, Renderers.For(Zip("app.jar", ("META-INF/MANIFEST.MF", "x"))));
        Assert.Same(Renderers.Media, Renderers.For(Write("song.MP3", [0x49, 0x44, 0x33, 0x00, 0x00])));
        Assert.Same(Renderers.Media, Renderers.For(Write("photo.png", [0x89, 0x50, 0x4E, 0x47, 0x00])));
        Assert.Same(Renderers.Readable, Renderers.For(Write("tool.exe", [0x4D, 0x5A, 0x00, 0x00])));
    }

    [Fact]
    public void A_text_file_is_its_own_text_with_its_counts()
    {
        var rendered = Renderers.Render(Write("notes.txt", Encoding.UTF8.GetBytes("one two\r\nthree\r\n")));
        Assert.Equal("one two\r\nthree\r\n", rendered.Document.Text);
        Assert.Equal(WayOut.Editor, rendered.WayOut);
        Assert.Contains("Encoding: UTF-8", rendered.Contents);
        Assert.Contains("Words: 3", rendered.Contents);
    }

    [Fact]
    public void A_word_document_gives_its_paragraphs_headings_lists_and_tables()
    {
        const string w = "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"";
        var path = Zip("report.docx", ("word/document.xml", $"""
            <w:document {w}><w:body>
              <w:p><w:pPr><w:pStyle w:val="Heading1"/></w:pPr><w:r><w:t>Install</w:t></w:r></w:p>
              <w:p><w:r><w:t xml:space="preserve">Run the </w:t></w:r><w:r><w:t>installer.</w:t></w:r></w:p>
              <w:p><w:pPr><w:numPr><w:ilvl w:val="0"/></w:numPr></w:pPr><w:r><w:t>First step</w:t></w:r></w:p>
              <w:tbl><w:tr><w:tc><w:p><w:r><w:t>Key</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>F1</w:t></w:r></w:p></w:tc></w:tr></w:tbl>
              <w:p/>
            </w:body></w:document>
            """));
        var rendered = Renderers.Render(path);
        Assert.Equal(["# Install", "Run the installer.", "- First step", "Key\tF1", ""], Lines(rendered));
        Assert.Equal("Word document", rendered.Kind);
        Assert.Contains("Headings: 1", rendered.Contents);
        Assert.Contains("Tables: 1", rendered.Contents);
    }

    [Fact]
    public void A_presentation_gives_its_slides_in_number_order()
    {
        const string a = "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"";
        var path = Zip("talk.pptx",
            ("ppt/slides/slide10.xml", $"<s {a}><a:p><a:r><a:t>Ten</a:t></a:r></a:p></s>"),
            ("ppt/slides/slide2.xml", $"<s {a}><a:p><a:r><a:t>Two</a:t></a:r></a:p></s>"),
            ("ppt/slides/slide1.xml", $"<s {a}><a:p><a:r><a:t>Hello</a:t></a:r><a:r><a:t> world</a:t></a:r></a:p></s>"));
        var rendered = Renderers.Render(path);
        Assert.Equal(["## Slide 1", "Hello world", "", "## Slide 2", "Two", "", "## Slide 3", "Ten", ""], Lines(rendered));
        Assert.Contains("Slides: 3", rendered.Contents);
    }

    [Fact]
    public void An_opendocument_text_gives_its_headings_paragraphs_and_lists()
    {
        const string ns = "xmlns:office=\"urn:oasis:names:tc:opendocument:xmlns:office:1.0\" xmlns:text=\"urn:oasis:names:tc:opendocument:xmlns:text:1.0\"";
        var path = Zip("letter.odt", ("content.xml", $"""
            <office:document-content {ns}><office:body><office:text>
              <text:h text:outline-level="2">Dear reader</text:h>
              <text:p>Two<text:s text:c="2"/>spaces</text:p>
              <text:list><text:list-item><text:p>An item</text:p></text:list-item></text:list>
            </office:text></office:body></office:document-content>
            """));
        Assert.Equal(["## Dear reader", "Two  spaces", "- An item"], Lines(Renderers.Render(path)));
    }

    [Fact]
    public void A_zip_lists_its_entries_in_name_order_with_its_counts()
    {
        var rendered = Renderers.Render(Zip("bundle.zip", ("docs/b10.txt", "x"), ("docs/b2.txt", "yy"), ("docs/", "")));
        var lines = Lines(rendered);
        Assert.Equal("docs/, folder", lines[0]);
        Assert.StartsWith("docs/b2.txt, 2 bytes, ", lines[1]);
        Assert.StartsWith("docs/b10.txt, 1 byte, ", lines[2]);
        Assert.Contains("Files: 2", rendered.Contents);
        Assert.Contains("Folders: 1", rendered.Contents);
    }

    [Fact]
    public void A_damaged_document_shows_its_readable_text_and_says_why()
    {
        var rendered = Renderers.Render(Write("broken.docx", Encoding.ASCII.GetBytes("PK\u0003\u0004 not really a zip at all, readable")));
        Assert.Equal("Readable text", rendered.Kind);
        Assert.StartsWith("broken.docx could not be read as a document", Lines(rendered)[0]);
    }

    [Fact]
    public void Any_other_file_shows_its_readable_runs_both_narrow_and_wide()
    {
        var bytes = new List<byte> { 0x4D, 0x5A, 0x00, 0x01 };
        bytes.AddRange(Encoding.ASCII.GetBytes("Hello there"));
        bytes.AddRange([0x00, 0x02, 0x03]);
        bytes.AddRange(Encoding.Unicode.GetBytes("Wide text"));
        bytes.AddRange([0x00, 0x00, 0x01, 0x41, 0x42, 0x00]);
        var rendered = Renderers.Render(Write("tool.exe", [.. bytes]));
        Assert.Equal([ReadableText.FirstLine, "Hello there", "Wide text"], Lines(rendered));
        Assert.Equal(WayOut.Windows, rendered.WayOut);
        Assert.Contains("Readable runs: 2", rendered.Contents);
    }

    [Fact]
    public void A_file_with_no_readable_text_says_so()
    {
        Assert.Equal(["No readable text was found in blob.bin."], Lines(Renderers.Render(Write("blob.bin", [0x00, 0x01, 0x02, 0x41, 0x00]))));
    }

    [Fact]
    public void Media_says_what_it_is_and_where_it_plays()
    {
        var sound = Renderers.Render(Write("song.mp3", [0x49, 0x44, 0x33, 0x00]));
        Assert.Equal(WayOut.Player, sound.WayOut);
        Assert.StartsWith("song.mp3 is a sound file (MP3).", Lines(sound)[0]);
        var picture = Renderers.Render(Write("photo.png", [0x89, 0x50, 0x00]));
        Assert.Equal(WayOut.Windows, picture.WayOut);
        Assert.Equal("Picture", picture.Kind);
    }
}
