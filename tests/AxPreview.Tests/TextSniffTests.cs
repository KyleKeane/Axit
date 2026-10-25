using System.Text;
using AxPreview.Core;

namespace AxPreview.Tests;

/// <summary>Whether a file reads as text (docs/axpreview/SPEC.md AP-2.2), over bytes and a temporary folder.</summary>
public sealed class TextSniffTests : IDisposable
{
    private readonly TempFolder _folder = new("axpreview-sniff");

    public void Dispose() => _folder.Dispose();

    private string Write(string name, byte[] bytes) => _folder.File(name, bytes);

    [Fact]
    public void The_editors_kinds_an_empty_file_and_a_utf16_mark_are_text()
    {
        Assert.True(TextSniff.IsText([0x00, 0x01], @"C:\x\settings.json"));
        Assert.True(TextSniff.IsText([0x00, 0x01], @"C:\x\notes.MD"));
        Assert.True(TextSniff.IsText([], @"C:\x\empty.bin"));
        Assert.True(TextSniff.IsText([0xFF, 0xFE, 0x41, 0x00], @"C:\x\wide.dat"));
        Assert.True(TextSniff.IsText([0xFE, 0xFF, 0x00, 0x41], @"C:\x\wide.dat"));
    }

    [Fact]
    public void Plain_bytes_are_text_and_a_zero_byte_or_many_control_bytes_are_not()
    {
        Assert.True(TextSniff.IsText(Encoding.UTF8.GetBytes("plain words\twith a tab\n"), @"C:\x\readme"));
        Assert.True(TextSniff.IsText(Encoding.UTF8.GetBytes("2026-10-22 12:00 started\r\n"), @"C:\x\app.log"));
        Assert.False(TextSniff.IsText([0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00], @"C:\x\tool.exe"));
        Assert.False(TextSniff.IsText([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], @"C:\x\blob.dat"));
    }

    [Fact]
    public void Every_file_under_the_limit_opens_text_or_not()
    {
        Assert.Null(TextSniff.Refuse(Write("notes.log", Encoding.UTF8.GetBytes("one\r\ntwo"))));
        Assert.Null(TextSniff.Refuse(Write("empty.bin", [])));
        // Since 2026-10-24 a file that is not text opens through its renderer (AP-7.5).
        Assert.Null(TextSniff.Refuse(Write("tool.exe", [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00])));
        // A file that cannot be read is left to the reader.
        Assert.Null(TextSniff.Refuse(Path.Combine(_folder.Path, "gone.txt")));
    }

    [Fact]
    public void A_text_over_the_limit_is_refused_by_size_and_any_other_file_opens()
    {
        var path = Path.Combine(_folder.Path, "huge.log");
        using (var stream = File.Create(path))
        {
            stream.Write(Encoding.ASCII.GetBytes(new string('a', 8192)));
            stream.SetLength(TextSniff.MaxBytes + 1);
        }

        var refusal = TextSniff.Refuse(path);
        Assert.Equal("Too large", refusal!.Title);
        Assert.Equal("huge.log is 33,554,433 bytes, over the 32 MB a window can open.", refusal.Text);

        var binary = Path.Combine(_folder.Path, "huge.bin");
        using (var stream = File.Create(binary))
        {
            stream.SetLength(TextSniff.MaxBytes + 1);
        }

        Assert.Null(TextSniff.Refuse(binary));
    }
}
