using System.Text;
using AxFile.Core;

namespace AxFile.Tests;

/// <summary>What Preview shows of a file (docs/axfile/SPEC.md AF-9.4), over a temporary folder.</summary>
public sealed class PreviewTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axfile-preview-" + Guid.NewGuid().ToString("N"));

    public PreviewTests()
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
    public void A_text_file_is_read_whole_and_a_binary_one_is_not_text()
    {
        var text = Preview.Read(Write("notes.log", Encoding.UTF8.GetBytes("one\r\ntwo")));
        Assert.Equal(new PreviewText("one\r\ntwo", Cut: false), text);
        Assert.Null(Preview.Read(Write("tool.exe", [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00])));
        Assert.Equal(string.Empty, Preview.Read(Write("empty.bin", []))!.Text);
    }

    [Fact]
    public void The_editors_kinds_and_a_utf16_mark_are_text_and_many_control_bytes_are_not()
    {
        Assert.True(Preview.IsText([0x00, 0x01], @"C:\x\settings.json"));
        Assert.True(Preview.IsText([0xFF, 0xFE, 0x41, 0x00], @"C:\x\wide.dat"));
        Assert.True(Preview.IsText(Encoding.UTF8.GetBytes("plain words\twith a tab\n"), @"C:\x\readme"));
        Assert.False(Preview.IsText([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], @"C:\x\blob.dat"));
        var wide = Preview.Read(Write("wide.dat", Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("wide text")).ToArray()));
        Assert.Equal("wide text", wide!.Text);
    }

    [Fact]
    public void A_file_over_a_megabyte_is_cut_there()
    {
        var line = Encoding.ASCII.GetBytes("0123456789abcdef\r\n");
        var bytes = new byte[Preview.Limit + 100];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = line[i % line.Length];
        }

        var text = Preview.Read(Write("big.log", bytes))!;
        Assert.True(text.Cut);
        Assert.Equal(Preview.Limit, text.Text.Length);
    }

    [Fact]
    public void A_cut_inside_a_multibyte_character_keeps_the_text_utf8()
    {
        // "é" is two bytes; the cut falls between them when every line is an odd length.
        var line = Encoding.UTF8.GetBytes("café\n");
        var bytes = new byte[Preview.Limit + 7];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = line[i % line.Length];
        }

        Assert.Equal((byte)0xC3, bytes[Preview.Limit - 1]);
        var text = Preview.Read(Write("accents.log", bytes))!;
        Assert.True(text.Cut);
        Assert.EndsWith("caf", text.Text);
        Assert.Contains("café", text.Text);
        Assert.DoesNotContain("Ã", text.Text);

        Assert.Equal([0x61], Preview.WithoutCutSequence([0x61, 0xE2, 0x82]));
        Assert.Equal([0x61, 0xC3, 0xA9], Preview.WithoutCutSequence([0x61, 0xC3, 0xA9]));
    }
}
