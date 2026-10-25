using System.Globalization;

namespace AxProcess.Core;

/// <summary>
/// Gigabytes with one decimal place, "15.9", for the summary's "Memory 15.9 of 31.8 GB" (SPEC.md PR-3.3). A process's
/// size is the bundle's <c>Axit.Core/FileSize</c> since 2026-10-25 (the user's choice of one format for every app,
/// inventory item 9): "900 KB", "350.4 MB", "1.2 GB"; until then axProcess rounded megabytes to whole numbers.
/// </summary>
public static class SizeText
{
    private const long Giga = 1024L * 1024 * 1024;

    /// <summary>Gigabytes with one decimal place, "15.9", for the summary.</summary>
    public static string Gigabytes(long bytes) => (Math.Round(bytes / (double)Giga, 1)).ToString("0.0", CultureInfo.InvariantCulture);
}