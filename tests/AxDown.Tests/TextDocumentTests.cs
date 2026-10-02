using System.Text;
using AxDown.Core;

namespace AxDown.Tests;

/// <summary>Files come back as they went in (SPEC.md AD-3, AD-D4): every encoding and line ending case, and the round trip.</summary>
public sealed class TextDocumentTests
{
    private static byte[] Bytes(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    [Fact]
    public void Utf8_without_a_mark_and_crlf()
    {
        var bytes = Encoding.UTF8.GetBytes("héllo\r\nworld");
        var document = TextDocument.FromBytes(bytes);
        Assert.Equal("héllo\r\nworld", document.Text);
        Assert.Equal("UTF-8", document.EncodingName);
        Assert.False(document.ByteOrderMark);
        Assert.Equal("CRLF", document.LineEndingName);
        Assert.Equal(bytes, document.ToBytes(document.Text));
    }

    [Fact]
    public void Utf8_with_a_mark_and_lf()
    {
        var bytes = Bytes([0xEF, 0xBB, 0xBF], Encoding.UTF8.GetBytes("a\nb\n"));
        var document = TextDocument.FromBytes(bytes);
        Assert.Equal("a\r\nb\r\n", document.Text);
        Assert.Equal("UTF-8 with BOM", document.EncodingName);
        Assert.Equal("LF", document.LineEndingName);
        Assert.Equal(bytes, document.ToBytes(document.Text));
    }

    [Fact]
    public void Utf16_little_and_big_endian_by_their_marks()
    {
        var little = Bytes([0xFF, 0xFE], Encoding.Unicode.GetBytes("x\r\ny"));
        var document = TextDocument.FromBytes(little);
        Assert.Equal("x\r\ny", document.Text);
        Assert.Equal("UTF-16 LE", document.EncodingName);
        Assert.Equal(little, document.ToBytes(document.Text));

        var big = Bytes([0xFE, 0xFF], Encoding.BigEndianUnicode.GetBytes("x\ny"));
        document = TextDocument.FromBytes(big);
        Assert.Equal("x\r\ny", document.Text);
        Assert.Equal("UTF-16 BE", document.EncodingName);
        Assert.Equal("LF", document.LineEndingName);
        Assert.Equal(big, document.ToBytes(document.Text));
    }

    [Fact]
    public void Bytes_that_are_not_utf8_are_the_system_code_page_and_survive_a_round_trip()
    {
        // 0xE9 alone is not valid UTF-8 (é in Windows-1252); the machine's ANSI page decides the letter, the round trip must hold.
        var bytes = Bytes(Encoding.ASCII.GetBytes("caf"), [0xE9], Encoding.ASCII.GetBytes("\r\n"));
        var document = TextDocument.FromBytes(bytes);
        Assert.StartsWith("Windows-", document.EncodingName);
        Assert.False(document.ByteOrderMark);
        Assert.Equal(4, document.Text.IndexOf('\r'));
        Assert.Equal(bytes, document.ToBytes(document.Text));
    }

    [Fact]
    public void Cr_alone_is_a_line_ending_too()
    {
        var bytes = Encoding.UTF8.GetBytes("a\rb");
        var document = TextDocument.FromBytes(bytes);
        Assert.Equal("a\r\nb", document.Text);
        Assert.Equal("CR", document.LineEndingName);
        Assert.Equal(bytes, document.ToBytes(document.Text));
    }

    [Fact]
    public void The_first_line_ending_is_the_files_and_is_used_for_every_line()
    {
        var document = TextDocument.FromBytes(Encoding.UTF8.GetBytes("a\r\nb\nc\rd"));
        Assert.Equal("a\r\nb\r\nc\r\nd", document.Text);
        Assert.Equal("CRLF", document.LineEndingName);
        Assert.Equal("a\r\nb\r\nc\r\nd", Encoding.UTF8.GetString(document.ToBytes(document.Text)));

        document = TextDocument.FromBytes(Encoding.UTF8.GetBytes("a\nb\r\nc"));
        Assert.Equal("LF", document.LineEndingName);
        Assert.Equal("a\nb\nc", Encoding.UTF8.GetString(document.ToBytes(document.Text)));
    }

    [Fact]
    public void No_line_ending_means_crlf_and_the_text_is_untouched()
    {
        var document = TextDocument.FromBytes(Encoding.UTF8.GetBytes("abc"));
        Assert.Equal("abc", document.Text);
        Assert.Equal("CRLF", document.LineEndingName);
        Assert.Equal("abc", Encoding.UTF8.GetString(document.ToBytes("abc")));
    }

    [Fact]
    public void Empty_bytes_and_a_new_document_are_utf8_without_a_mark()
    {
        var document = TextDocument.FromBytes([]);
        Assert.Equal(string.Empty, document.Text);
        Assert.Equal("UTF-8", document.EncodingName);
        Assert.Equal("CRLF", document.LineEndingName);

        var fresh = TextDocument.Empty();
        Assert.Equal("UTF-8", fresh.EncodingName);
        Assert.False(fresh.ByteOrderMark);
        Assert.Equal("one\r\ntwo", Encoding.UTF8.GetString(fresh.ToBytes("one\r\ntwo")));
    }

    [Fact]
    public void Edited_text_is_written_in_the_files_encoding_and_ending()
    {
        var document = TextDocument.FromBytes(Bytes([0xFF, 0xFE], Encoding.Unicode.GetBytes("old\n")));
        var written = document.ToBytes("new\r\nlines\r\n");
        Assert.Equal(Bytes([0xFF, 0xFE], Encoding.Unicode.GetBytes("new\nlines\n")), written);
    }

    [Fact]
    public void Read_and_write_go_through_the_file()
    {
        var folder = Path.Combine(Path.GetTempPath(), "axdown-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "notes.md");
        try
        {
            File.WriteAllBytes(path, Bytes([0xEF, 0xBB, 0xBF], Encoding.UTF8.GetBytes("# Title\n\ntext\n")));
            var document = TextDocument.Read(path);
            Assert.Equal("# Title\r\n\r\ntext\r\n", document.Text);
            document.Write(path, "# Title\r\n\r\nmore text\r\n");
            Assert.Equal(Bytes([0xEF, 0xBB, 0xBF], Encoding.UTF8.GetBytes("# Title\n\nmore text\n")), File.ReadAllBytes(path));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void A_file_over_the_limit_is_refused_before_it_is_read()
    {
        var folder = Path.Combine(Path.GetTempPath(), "axdown-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "huge.txt");
        try
        {
            using (var file = File.Create(path))
            {
                file.SetLength(TextDocument.MaxBytes + 1);
            }

            var error = Assert.Throws<InvalidOperationException>(() => TextDocument.Read(path));
            Assert.Contains("huge.txt", error.Message);
            Assert.Contains("32 MB", error.Message);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
