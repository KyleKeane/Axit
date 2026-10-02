using System.Runtime.InteropServices;
using System.Text;

namespace AxDown.Core;

/// <summary>
/// A text file as AxDown holds it (SPEC.md AD-3): the text with <c>\r\n</c> line endings for the edit control, and
/// what is needed to write it back as it was found: the encoding, whether it had a byte order mark, and its line
/// ending. <see cref="FromBytes"/> and <see cref="ToBytes"/> are the pure core; <see cref="Read"/> and
/// <see cref="Write"/> only add the file.
/// </summary>
public sealed record TextDocument(string Text, Encoding Encoding, bool ByteOrderMark, string LineEnding)
{
    /// <summary>AD-3.7: the EDIT control stops being usable long before this.</summary>
    public const long MaxBytes = 32L * 1024 * 1024;

    // Constructed to know their marks (GetPreamble); GetBytes never writes a mark, ToBytes adds it when the file had one.
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly Encoding Utf16LittleEndian = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
    private static readonly Encoding Utf16BigEndian = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);

    static TextDocument()
    {
        // The Windows code pages (1252 and the others) are not built into .NET's defaults; this provider is part of .NET.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>A new document: UTF-8 without a mark, Windows line endings (AD-3.3, AD-3.4).</summary>
    public static TextDocument Empty() => new(string.Empty, Utf8, ByteOrderMark: false, "\r\n");

    /// <summary>Reads the file. Throws <see cref="InvalidOperationException"/> for a file over <see cref="MaxBytes"/>, and the usual I/O exceptions.</summary>
    public static TextDocument Read(string path)
    {
        var length = new FileInfo(path).Length;
        if (length > MaxBytes)
        {
            throw new InvalidOperationException($"{Path.GetFileName(path)} is {length / (1024 * 1024)} MB. AxDown opens files up to {MaxBytes / (1024 * 1024)} MB.");
        }

        return FromBytes(File.ReadAllBytes(path));
    }

    /// <summary>Writes <paramref name="text"/> (with <c>\r\n</c> line endings, as the edit control gives it) back in this document's encoding and line ending.</summary>
    public void Write(string path, string text) => File.WriteAllBytes(path, ToBytes(text));

    /// <summary>
    /// AD-3.1: a byte order mark decides UTF-8, UTF-16 little or big endian; without one, valid UTF-8 is UTF-8 and
    /// anything else the system's ANSI code page. AD-3.4: the first line ending in the file is the file's.
    /// </summary>
    public static TextDocument FromBytes(byte[] bytes)
    {
        Encoding encoding;
        var mark = true;
        int start;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            encoding = Utf8;
            start = 3;
        }
        else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            encoding = Utf16LittleEndian;
            start = 2;
        }
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            encoding = Utf16BigEndian;
            start = 2;
        }
        else
        {
            encoding = IsValidUtf8(bytes) ? Utf8 : SystemCodePage();
            mark = false;
            start = 0;
        }

        var raw = encoding.GetString(bytes, start, bytes.Length - start);
        return new TextDocument(NormalizeLineEndings(raw), encoding, mark, DetectLineEnding(raw));
    }

    /// <summary>The bytes of <paramref name="text"/> in this document's encoding, with its mark and its line ending (AD-3.3, AD-3.4).</summary>
    public byte[] ToBytes(string text)
    {
        var body = Encoding.GetBytes(LineEnding == "\r\n" ? text : text.Replace("\r\n", LineEnding));
        if (!ByteOrderMark)
        {
            return body;
        }

        var preamble = Encoding.GetPreamble();
        var all = new byte[preamble.Length + body.Length];
        preamble.CopyTo(all, 0);
        body.CopyTo(all, preamble.Length);
        return all;
    }

    /// <summary>For the status bar (AD-3.2): "UTF-8", "UTF-8 with BOM", "UTF-16 LE", "UTF-16 BE", "Windows-1252".</summary>
    public string EncodingName => Encoding.CodePage switch
    {
        65001 => ByteOrderMark ? "UTF-8 with BOM" : "UTF-8",
        1200 => "UTF-16 LE",
        1201 => "UTF-16 BE",
        _ => char.ToUpperInvariant(Encoding.WebName[0]) + Encoding.WebName[1..],
    };

    /// <summary>For the status bar (AD-3.4): "CRLF", "LF" or "CR".</summary>
    public string LineEndingName => LineEnding switch
    {
        "\r\n" => "CRLF",
        "\n" => "LF",
        _ => "CR",
    };

    private static bool IsValidUtf8(byte[] bytes)
    {
        try
        {
            StrictUtf8.GetCharCount(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static string DetectLineEnding(string text)
    {
        var at = text.IndexOfAny(['\r', '\n']);
        if (at < 0)
        {
            return "\r\n";
        }

        if (text[at] == '\n')
        {
            return "\n";
        }

        return at + 1 < text.Length && text[at + 1] == '\n' ? "\r\n" : "\r";
    }

    /// <summary>Every line ending becomes <c>\r\n</c>, which is what a multi-line edit control holds.</summary>
    private static string NormalizeLineEndings(string text)
    {
        if (!text.Contains('\r') && !text.Contains('\n'))
        {
            return text;
        }

        return text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
    }

    /// <summary>The ANSI code page of this Windows; 1252 when it cannot be asked (another OS, an unknown page).</summary>
    private static Encoding SystemCodePage()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return Encoding.GetEncoding((int)GetACP());
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
        }

        return Encoding.GetEncoding(1252);
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetACP();
}
