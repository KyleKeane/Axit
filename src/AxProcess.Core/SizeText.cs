using System.Globalization;

namespace AxProcess.Core;

/// <summary>A size as it is spoken (SPEC.md PR-3.1): "900 KB", "350 MB", "1.2 GB", in units of 1024.</summary>
public static class SizeText
{
    private const long Kilo = 1024;
    private const long Mega = Kilo * 1024;
    private const long Giga = Mega * 1024;

    public static string Format(long bytes)
    {
        if (bytes < Mega)
        {
            return $"{Math.Round(bytes / (double)Kilo).ToString(CultureInfo.InvariantCulture)} KB";
        }

        if (bytes < Giga)
        {
            return $"{Math.Round(bytes / (double)Mega).ToString(CultureInfo.InvariantCulture)} MB";
        }

        return $"{Gigabytes(bytes)} GB";
    }

    /// <summary>Gigabytes with one decimal place, "15.9", for the summary.</summary>
    public static string Gigabytes(long bytes) => (Math.Round(bytes / (double)Giga, 1)).ToString("0.0", CultureInfo.InvariantCulture);
}
