using System.Text;
using AxPreview.Core;

namespace AxPreview.Tests;

/// <summary>Whether a file reads as text (docs/axpreview/SPEC.md AP-2.2), over bytes and a temporary folder.</summary>
public sealed class TextSniffTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axpreview-sniff-" + Guid.NewGuid().ToString("N"));

    public TextSniffTests()
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
    public void A_file_is_refused_when_it_is_not_text_and_opened_otherwise()
    {
        Assert.Null(TextSniff.Refuse(Write("notes.log", Encoding.UTF8.GetBytes("one\r\ntwo"))));
        Assert.Null(TextSniff.Refuse(Write("empty.bin", [])));
        var refusal = TextSniff.Refuse(Write("tool.exe", [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00]));
        Assert.Equal("Not a text file", refusal!.Title);
        Assert.Equal("tool.exe is not a text file. AxFile's Enter opens it in the program Windows has for it.", refusal.Text);
        // A file that cannot be read is left to the reader.
        Assert.Null(TextSniff.Refuse(Path.Combine(_folder, "gone.txt")));
    }

    [Fact]
    public void A_file_over_the_limit_is_refused_by_size()
    {
        var path = Path.Combine(_folder, "huge.log");
        using (var stream = File.Create(path))
        {
            stream.SetLength(TextSniff.MaxBytes + 1);
        }

        var refusal = TextSniff.Refuse(path);
        Assert.Equal("Too large", refusal!.Title);
        Assert.Equal("huge.log is 33,554,433 bytes, over the 32 MB a window can open.", refusal.Text);
    }
}
