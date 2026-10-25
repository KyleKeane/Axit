using System.Text;

namespace AxPreview.Core;

/// <summary>
/// The readable text inside any other file (SPEC.md AP-7.2, 5): the runs of four or more printable characters, as
/// single bytes (ASCII) and as UTF-16 (a printable character and a zero byte), one run to a line in the order they
/// come, from the first 32 MB of the file. Tested in <c>tests/AxPreview.Tests/RenderersTests</c>.
/// </summary>
public static class ReadableText
{
    /// <summary>The shortest run that counts as text; shorter ones are mostly noise in a binary.</summary>
    public const int MinimumRun = 4;

    /// <summary>How much of the file is read: the editors' limit (<see cref="TextSniff.MaxBytes"/>).</summary>
    public const long MaxBytes = TextSniff.MaxBytes;

    public const string FirstLine = "No preview for this kind yet; the readable text inside it follows.";

    public static Rendered Render(string path) => Render(path, FirstLine);

    /// <summary>The runs under <paramref name="firstLine"/>, which says why the file is shown this way.</summary>
    public static Rendered Render(string path, string firstLine)
    {
        long length;
        byte[] bytes;
        using (var stream = File.OpenRead(path))
        {
            length = stream.Length;
            bytes = new byte[Math.Min(length, MaxBytes)];
            stream.ReadExactly(bytes);
        }

        var runs = Runs(bytes);
        var lines = new List<string>();
        if (runs.Count == 0)
        {
            lines.Add($"No readable text was found in {Path.GetFileName(path)}.");
        }
        else
        {
            lines.Add(firstLine);
            lines.AddRange(runs);
            if (length > MaxBytes)
            {
                lines.Add($"Shown from the first {MaxBytes / (1024 * 1024)} MB.");
            }
        }

        var contents = new List<string>
        {
            "Format: no preview for this kind",
            $"Readable runs: {runs.Count:N0}",
            $"Readable characters: {runs.Sum(run => run.Length):N0}",
        };
        if (length > MaxBytes)
        {
            contents.Add($"Read: the first {MaxBytes / (1024 * 1024)} MB");
        }

        return Rendered.Of(lines, "Readable text", contents, WayOut.Windows);
    }

    /// <summary>The runs of the bytes, single-byte and UTF-16 ones in the order they start.</summary>
    public static List<string> Runs(ReadOnlySpan<byte> bytes)
    {
        var runs = new List<string>();
        var i = 0;
        while (i < bytes.Length)
        {
            // UTF-16: a printable character followed by a zero byte, MinimumRun times at least.
            var wide = WideRun(bytes, i);
            if (wide >= MinimumRun)
            {
                var text = new StringBuilder(wide);
                for (var k = 0; k < wide; k++)
                {
                    text.Append((char)bytes[i + 2 * k]);
                }

                runs.Add(text.ToString().Trim());
                i += 2 * wide;
                continue;
            }

            var narrow = 0;
            while (i + narrow < bytes.Length && Printable(bytes[i + narrow]))
            {
                narrow++;
            }

            if (narrow >= MinimumRun)
            {
                runs.Add(Encoding.ASCII.GetString(bytes.Slice(i, narrow)).Trim());
                i += narrow;
            }
            else
            {
                i += Math.Max(narrow, 1);
            }
        }

        runs.RemoveAll(run => run.Length < MinimumRun);
        return runs;
    }

    private static int WideRun(ReadOnlySpan<byte> bytes, int start)
    {
        var count = 0;
        while (start + 2 * count + 1 < bytes.Length && Printable(bytes[start + 2 * count]) && bytes[start + 2 * count + 1] == 0)
        {
            count++;
        }

        return count;
    }

    /// <summary>A printable ASCII character or a tab.</summary>
    private static bool Printable(byte b) => b is >= 0x20 and < 0x7F or (byte)'\t';
}
