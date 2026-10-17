using System.Text;
using System.Text.RegularExpressions;
using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>What the test classes share: the fixture files, feeding and replaying a model, and the lines and edits of the mirror tests.</summary>
internal static partial class TestHelpers
{
    /// <summary>The escape character that starts every control sequence in a fed frame.</summary>
    public const string Esc = "\x1b";

    /// <summary>The recordings replayed with the messages they were made with (<see cref="Replay"/>).</summary>
    public const string LongMessage = "long-message-claude2.1.278-240x50.vt";
    public const string Steering = "steering-claude2.1.278-240x50.vt";

    public static readonly string[] LongMessages =
    [
        "Please reply with exactly the two words OK DONE and nothing else. The rest of this message is filler so that it is longer than one console row of two hundred and forty columns: alpha bravo charlie delta echo foxtrot golf hotel india juliet kilo lima mike november oscar papa quebec romeo sierra tango uniform victor whiskey xray yankee zulu alpha bravo charlie delta echo foxtrot golf hotel india juliet kilo lima mike november oscar papa quebec romeo sierra tango uniform victor whiskey xray yankee zulu alpha bravo charlie delta echo foxtrot golf hotel india juliet kilo lima mike november oscar papa quebec romeo sierra tango uniform victor whiskey xray yankee zulu end of filler.",
        "Second message, also long, reply again with exactly OK DONE. Filler: one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen nineteen twenty twentyone twentytwo twentythree twentyfour twentyfive twentysix twentyseven twentyeight twentynine thirty thirtyone thirtytwo thirtythree thirtyfour thirtyfive thirtysix thirtyseven thirtyeight thirtynine forty fortyone fortytwo fortythree fortyfour fortyfive fortysix fortyseven fortyeight fortynine fifty end of filler.",
    ];

    public static readonly string[] SteeringMessages =
    [
        "Use the Bash tool to run the command: sleep 20. After it finishes reply with exactly OK DONE.",
        "Second message sent while you were busy: after OK DONE also write the word HELLO on its own line.",
        "Reply with the number of lines in this message and the text of its last line.\nThis is the second line.",
    ];

    /// <summary>The tests/fixtures folder, found above the test binary.</summary>
    public static string FixtureDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tests", "fixtures");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("tests/fixtures was not found above " + AppContext.BaseDirectory);
    }

    /// <summary>The bytes of a recording in tests/fixtures.</summary>
    public static byte[] Fixture(string name) => File.ReadAllBytes(Path.Combine(FixtureDirectory(), name));

    /// <summary>The console size a fixture was recorded at, from its name (<c>-240x50.vt</c>).</summary>
    public static (int Columns, int Rows) FixtureSize(string name)
    {
        var match = SizeRegex().Match(name);
        return match.Success ? (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value)) : (120, 40);
    }

    /// <summary>A model of the fixture's size fed the whole recording, with the last frame ended.</summary>
    public static SessionModel ParseFixture(string name)
    {
        var (columns, rows) = FixtureSize(name);
        var model = new SessionModel(columns, rows);
        model.Feed(Fixture(name));
        model.EndFrame();
        return model;
    }

    public static void Feed(SessionModel model, string text) => model.Feed(Encoding.UTF8.GetBytes(text));

    /// <summary>
    /// Feeds a recording in chunks of 1 to 299 bytes drawn from the seed, so the split points fall anywhere, differ
    /// between fixtures and repeat between runs. The caller ends the last frame.
    /// </summary>
    public static void FeedInRandomChunks(SessionModel model, byte[] bytes, int seed)
    {
        var random = new Random(seed);
        var offset = 0;
        while (offset < bytes.Length)
        {
            var length = Math.Min(random.Next(1, 300), bytes.Length - offset);
            model.Feed(bytes.AsSpan(offset, length));
            offset += length;
        }
    }

    /// <summary>
    /// The chunk index of a recording (<c>&lt;fixture&gt;.chunks.txt</c>, as StreamRecorder and PtyCapture write it):
    /// the offset, length and elapsed milliseconds of each read. The header row, with or without a byte order mark,
    /// is skipped.
    /// </summary>
    public static List<(int Offset, int Length, int Ms)> Chunks(string fixture) =>
        File.ReadAllLines(Path.Combine(FixtureDirectory(), fixture + ".chunks.txt"))
            .Where(l => l.Length > 0 && char.IsDigit(l[0]))
            .Select(l => l.Split(' '))
            .Select(p => (Offset: int.Parse(p[0]), Length: int.Parse(p[1]), Ms: int.Parse(p[2])))
            .ToList();

    /// <summary>
    /// Replays a recording chunk by chunk, as its index lists them, with the app's quiet rule: a chunk that arrives
    /// 100 ms or more after the one before ends the frame first, and the last frame is ended at the end. Each message
    /// in <paramref name="sent"/> is sent, as the app does, just before the chunk that holds the first 30 characters
    /// of its first line, so the markers are in place when the draft echo arrives; with
    /// <paramref name="quietFrameDuringDraft"/> that chunk ends its frame as well. Every message must be found.
    /// </summary>
    public static void Replay(SessionModel model, string fixture, string[]? sent = null, bool quietFrameDuringDraft = false)
    {
        sent ??= [];
        var bytes = Fixture(fixture);
        var next = 0;
        var previousMs = 0;
        foreach (var chunk in Chunks(fixture))
        {
            var data = bytes.AsSpan(chunk.Offset, chunk.Length);
            if (chunk.Ms - previousMs >= 100)
            {
                model.EndFrame();
            }

            var firstLine = next < sent.Length ? sent[next].Split('\n')[0] : null;
            var probe = firstLine is null ? null : firstLine[..Math.Min(30, firstLine.Length)];
            if (probe is not null && data.Length > 20 && Encoding.UTF8.GetString(data).Contains(probe, StringComparison.Ordinal))
            {
                model.Send(sent[next++]);
                model.Feed(data);
                if (quietFrameDuringDraft)
                {
                    model.EndFrame();
                }
            }
            else
            {
                model.Feed(data);
            }

            previousMs = chunk.Ms;
        }

        model.EndFrame();
        Assert.Equal(sent.Length, next);
    }

    /// <summary>The texts of the lines the reader sees.</summary>
    public static List<string> Visible(SessionModel model) => model.Lines.Where(l => !l.Hidden).Select(l => l.Text).ToList();

    /// <summary>A plain line for the mirror tests.</summary>
    public static Line L(string text) => new(0, LineKind.Plain, text);

    /// <summary>A line that continues the row before it (FR-3.2a).</summary>
    public static Line Joined(string text) => new(0, LineKind.Plain, text) { JoinedToPrevious = true };

    /// <summary>Applies a mirror update to a text the way the view applies it to the edit control.</summary>
    public static StringBuilder Apply(StringBuilder text, MirrorUpdate update)
    {
        foreach (var edit in update.Edits)
        {
            text.Remove(edit.Start, edit.OldLength).Insert(edit.Start, edit.Text);
        }

        return text;
    }

    [GeneratedRegex(@"-(\d+)x(\d+)\.vt$")]
    private static partial Regex SizeRegex();
}
