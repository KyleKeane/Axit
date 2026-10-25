namespace Axit.Core;

/// <summary>
/// A file's size as people say it (from axFile's <c>FolderListing.FormatSize</c> on its second use, axPreview's
/// details and listings, 2026-10-24): "512 bytes", "12 KB", "1.2 MB", "3.5 GB", in units of 1024.
/// </summary>
public static class FileSize
{
    /// <summary>"512 bytes", "12 KB", "1.2 MB", "3.5 GB".</summary>
    public static string Text(long bytes)
    {
        const double kilo = 1024;
        return bytes switch
        {
            1 => "1 byte",
            < 1024 => $"{bytes} bytes",
            < 1024 * 1024 => $"{Math.Round(bytes / kilo)} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (kilo * kilo):0.#} MB",
            _ => $"{bytes / (kilo * kilo * kilo):0.#} GB",
        };
    }
}
