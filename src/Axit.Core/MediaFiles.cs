namespace Axit.Core;

/// <summary>
/// The rules of sound and video files that need no engine (docs/axmedia/SPEC.md AM-4.1, AM-4.3, AM-4.5): which
/// files are media, the time format, the time a user types, and the speed steps. AxMedia plays them and AxFile asks
/// which entries are media (docs/axfile/SPEC.md section 9), which is why they are the bundle's (AX-6.3; from
/// AxFile.Core, 2026-10-22). Tested in <c>tests/Axit.Tests</c>.
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

    /// <summary>Every extension the player takes (AM-4.1); whether Windows decodes one is its decoders' business.</summary>
    public static readonly IReadOnlySet<string> Extensions = new HashSet<string>(SoundExtensions.Concat(VideoExtensions), StringComparer.OrdinalIgnoreCase);

    public const double MinSpeed = 0.5;
    public const double MaxSpeed = 3;
    public const double SpeedStep = 0.25;

    public static bool IsMedia(string path) => Extensions.Contains(Path.GetExtension(path));

    /// <summary>"12:40" under an hour, "1:02:10" over (AM-4.5); never negative.</summary>
    public static string FormatTime(TimeSpan time)
    {
        var total = (long)Math.Max(0, Math.Floor(time.TotalSeconds));
        var hours = total / 3600;
        var minutes = total / 60 % 60;
        var seconds = total % 60;
        return hours > 0 ? $"{hours}:{minutes:00}:{seconds:00}" : $"{minutes}:{seconds:00}";
    }

    /// <summary>What Go to time takes (AM-4.3): "12:40", "1:02:10", or "750" for seconds; null for anything else.</summary>
    public static TimeSpan? ParseTime(string text)
    {
        var parts = text.Trim().Split(':');
        if (parts.Length is 0 or > 3 || parts.Any(part => part.Length == 0 || !part.All(char.IsAsciiDigit)))
        {
            return null;
        }

        var numbers = new long[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            // A run of digits too long for a number is nonsense too, not an exception (2026-10-23).
            if (!long.TryParse(parts[i], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out numbers[i]))
            {
                return null;
            }
        }

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

    /// <summary>One step faster, by 0.25 up to 3 (AM-4.3).</summary>
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

    /// <summary>"Speed 1.25", "Speed 1": the announcement (AM-6).</summary>
    public static string SpeedText(double speed) => "Speed " + speed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
