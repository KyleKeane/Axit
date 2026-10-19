namespace AxFile.Core;

/// <summary>
/// The rules of the player that need no engine (SPEC.md AF-5): which files are media, the queue a folder makes, the
/// time format, the time a user types, and the speed steps. Tested in <c>tests/AxFile.Tests</c>.
/// </summary>
public static class MediaFiles
{
    public static readonly IReadOnlySet<string> SoundExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".wma", ".m4a", ".m4b", ".aac", ".flac", ".ogg", ".opus", ".aiff",
    };

    public static readonly IReadOnlySet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".m4v", ".mov", ".avi", ".wmv", ".mkv", ".webm",
    };

    /// <summary>Every extension the player takes (AF-5.1); whether Windows decodes one is its decoders' business.</summary>
    public static readonly IReadOnlySet<string> Extensions = new HashSet<string>(SoundExtensions.Concat(VideoExtensions), StringComparer.OrdinalIgnoreCase);

    public const double MinSpeed = 0.5;
    public const double MaxSpeed = 3;
    public const double SpeedStep = 0.25;

    public static bool IsMedia(string path) => Extensions.Contains(Path.GetExtension(path));

    /// <summary>The folder as a playlist (AF-5.3, AF-D6): the paths of its media files in the list's order.</summary>
    public static IReadOnlyList<string> Queue(IEnumerable<Entry> entries) =>
        entries.Where(entry => !entry.IsFolder && IsMedia(entry.Path)).Select(entry => entry.Path).ToList();

    /// <summary>"12:40" under an hour, "1:02:10" over (AF-5.7); never negative.</summary>
    public static string FormatTime(TimeSpan time)
    {
        var total = (long)Math.Max(0, Math.Floor(time.TotalSeconds));
        var hours = total / 3600;
        var minutes = total / 60 % 60;
        var seconds = total % 60;
        return hours > 0 ? $"{hours}:{minutes:00}:{seconds:00}" : $"{minutes}:{seconds:00}";
    }

    /// <summary>What Go to time takes (AF-5.4): "12:40", "1:02:10", or "750" for seconds; null for anything else.</summary>
    public static TimeSpan? ParseTime(string text)
    {
        var parts = text.Trim().Split(':');
        if (parts.Length is 0 or > 3 || parts.Any(part => part.Length == 0 || !part.All(char.IsAsciiDigit)))
        {
            return null;
        }

        var numbers = parts.Select(part => long.Parse(part, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        if (numbers.Length == 1)
        {
            return TimeSpan.FromSeconds(numbers[0]);
        }

        // Minutes and seconds after the first part stay under 60, as a clock reads.
        if (numbers.Skip(1).Any(number => number >= 60))
        {
            return null;
        }

        return numbers.Length == 2
            ? TimeSpan.FromSeconds(numbers[0] * 60 + numbers[1])
            : TimeSpan.FromSeconds(numbers[0] * 3600 + numbers[1] * 60 + numbers[2]);
    }

    /// <summary>One step faster, by 0.25 up to 3 (AF-5.4).</summary>
    public static double Faster(double speed) => Math.Min(MaxSpeed, SanitizeSpeed(speed) + SpeedStep);

    /// <summary>One step slower, by 0.25 down to 0.5.</summary>
    public static double Slower(double speed) => Math.Max(MinSpeed, SanitizeSpeed(speed) - SpeedStep);

    /// <summary>A speed from the settings onto the grid of steps and into the range; 1 for nonsense.</summary>
    public static double SanitizeSpeed(double speed)
    {
        if (double.IsNaN(speed) || double.IsInfinity(speed))
        {
            return 1;
        }

        var onGrid = Math.Round(speed / SpeedStep) * SpeedStep;
        return Math.Clamp(onGrid, MinSpeed, MaxSpeed);
    }

    /// <summary>"Speed 1.25", "Speed 1": the announcement (AF-7).</summary>
    public static string SpeedText(double speed) => "Speed " + speed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
