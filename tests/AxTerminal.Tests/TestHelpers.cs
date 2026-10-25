using System.Text;
using AxTerminal.Core;
using Axit.Core.Transcript;

namespace AxTerminal.Tests;

/// <summary>What the test classes share: the fixture files, feeding a model, the rendering of a transcript.</summary>
internal static class TestHelpers
{
    public const string Esc = "\x1b";
    public const string Bel = "\a";

    public static byte[] Fixture(string name) => File.ReadAllBytes(Path.Combine(Fixtures.Folder(), name));

    /// <summary>The console size a fixture was recorded at, from its name (<c>-240x50.vt</c>).</summary>
    public static (int Columns, int Rows) FixtureSize(string name)
    {
        var match = System.Text.RegularExpressions.Regex.Match(name, @"-(\d+)x(\d+)\.vt$");
        return match.Success ? (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value)) : (120, 40);
    }

    /// <summary>The chunk index of a recording (<c>&lt;fixture&gt;.chunks.txt</c>): the offset, length and elapsed milliseconds of each read.</summary>
    public static List<(int Offset, int Length, int Ms)> Chunks(string fixture) =>
        File.ReadAllLines(Path.Combine(Fixtures.Folder(), fixture + ".chunks.txt"))
            .Where(l => l.Length > 0 && char.IsDigit(l[0]))
            .Select(l => l.Split(' '))
            .Select(p => (Offset: int.Parse(p[0]), Length: int.Parse(p[1]), Ms: int.Parse(p[2])))
            .ToList();

    /// <summary>
    /// Replays a recording chunk by chunk with the app's quiet rule (a chunk 100 ms or more after the one before
    /// ends the frame first; the last frame is ended at the end), sending each command as the recording script did:
    /// once the output holds its trigger (the prompt's B mark, or a program's question), the command is sent before
    /// the next chunk. Every trigger must be met.
    /// </summary>
    public static TerminalModel Replay(string fixture, IReadOnlyList<(string Trigger, string Command)> sends, int? randomSeed = null, Action<TerminalModel>? subscribe = null)
    {
        var (columns, rows) = FixtureSize(fixture);
        var model = new TerminalModel(columns, rows) { Clock = () => new DateTime(2026, 10, 18, 12, 0, 0) };
        subscribe?.Invoke(model);
        var bytes = Fixture(fixture);
        var next = 0;
        var previousMs = 0;
        var since = new StringBuilder();
        foreach (var chunk in randomSeed is { } seed ? RandomChunks(bytes.Length, seed) : Chunks(fixture))
        {
            if (chunk.Ms - previousMs >= 100)
            {
                model.EndFrame();
            }

            // A chunk (a random one especially) can hold the trigger and what the shell printed after the script's send:
            // the bytes up to the trigger's end are fed, the command is sent, and the rest follows, as it happened.
            var data = bytes.AsSpan(chunk.Offset, chunk.Length);
            var text = Encoding.UTF8.GetString(data);
            since.Append(text);
            var sinceText = since.ToString();
            var chunkStart = sinceText.Length - text.Length;
            var fed = 0;
            var searchFrom = 0;
            while (next < sends.Count)
            {
                var trigger = Unescape(sends[next].Trigger);
                var at = sinceText.IndexOf(trigger, searchFrom, StringComparison.Ordinal);
                if (at < 0)
                {
                    break;
                }

                var end = Math.Clamp(at + trigger.Length - chunkStart, fed, text.Length);
                model.Feed(data[Encoding.UTF8.GetByteCount(text.AsSpan(0, fed))..Encoding.UTF8.GetByteCount(text.AsSpan(0, end))]);
                fed = end;
                model.EndFrame();
                model.Send(sends[next].Command);
                next++;
                searchFrom = at + trigger.Length;
            }

            model.Feed(data[Encoding.UTF8.GetByteCount(text.AsSpan(0, fed))..]);
            since.Remove(0, searchFrom);
            previousMs = chunk.Ms;
        }

        model.EndFrame();
        if (next < sends.Count)
        {
            throw new InvalidOperationException($"The trigger of command {next + 1} ({sends[next].Command}) was not met in {fixture}.");
        }

        return model;
    }

    /// <summary>Chunks of 1 to 299 bytes drawn from the seed, all at the same instant: the split points fall anywhere and no quiet frame ends.</summary>
    private static List<(int Offset, int Length, int Ms)> RandomChunks(int length, int seed)
    {
        var random = new Random(seed);
        var chunks = new List<(int, int, int)>();
        var offset = 0;
        while (offset < length)
        {
            var size = Math.Min(random.Next(1, 300), length - offset);
            chunks.Add((offset, size, 0));
            offset += size;
        }

        return chunks;
    }

    public static void Feed(TerminalModel model, string text) => model.Feed(Encoding.UTF8.GetBytes(text));

    /// <summary>The visible transcript as the reader sees it, one line per row, joined rows as one.</summary>
    public static string Render(TerminalModel model) => TranscriptMirror.Render(model.Lines.Where(l => !l.Hidden));

    private static string Unescape(string s) => s.Replace("\\e", Esc).Replace("\\a", Bel);
}
