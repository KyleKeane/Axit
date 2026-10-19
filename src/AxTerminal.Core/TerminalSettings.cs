using Axit.Core;

namespace AxTerminal.Core;

/// <summary>What is spoken of the shell's output as it arrives (SPEC.md AT-5.3).</summary>
public enum OutputSpeechMode
{
    None,
    All,
}

/// <summary>
/// Everything AxTerminal remembers between runs (SPEC.md AT-8.1), in <c>%APPDATA%\Axit\axterminal.json</c> through
/// the bundle's <see cref="SettingsFile"/>: the font, the update check and the window's place from
/// <see cref="WindowSettings"/>, the shell, the folders, the console size and the sound and speech options. A missing
/// or unreadable file falls back to the defaults; out-of-range values do too.
/// </summary>
public sealed class TerminalSettings : WindowSettings
{
    public const int MaxRecentFolders = 10;

    /// <summary>The shell's word (<see cref="ShellLauncher.Key"/>); null means the default of the machine.</summary>
    public string? Shell { get; set; }

    /// <summary>The command line of the custom shell, kept for the New session notice.</summary>
    public string? CustomShell { get; set; }

    public string? LastFolder { get; set; }

    public List<string> RecentFolders { get; set; } = [];

    public int PtyColumns { get; set; } = 240;
    public int PtyRows { get; set; } = 50;

    /// <summary>The chimes (AT-5.1).</summary>
    public bool SoundOnBell { get; set; } = true;

    /// <summary>The tick for a frame with a new line (AT-5.2).</summary>
    public bool ClickOnNewLine { get; set; } = true;

    public bool WorkingMusic { get; set; }
    public bool BackgroundAlert { get; set; }
    public bool FlashTaskbar { get; set; } = true;

    public OutputSpeechMode OutputSpeech { get; set; }

    /// <summary>"Asks: …" when a running program seems to wait on a question (AT-5.4).</summary>
    public bool AnnounceQuestions { get; set; } = true;

    public bool MarkerTimeStamps { get; set; }
    public bool JoinWrappedLines { get; set; } = true;
    public int MaxTranscriptLines { get; set; } = 20000;

    public static string DefaultPath => AppPaths.SettingsFile("axterminal");

    public static TerminalSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<TerminalSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>The shell the settings name, or the machine's default.</summary>
    public ShellKind ShellKind => ShellLauncher.Parse(Shell);

    public void RememberFolder(string folder)
    {
        LastFolder = folder;
        RecentFolders.RemoveAll(f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
        RecentFolders.Insert(0, folder);
        TrimRecent();
    }

    private void TrimRecent()
    {
        if (RecentFolders.Count > MaxRecentFolders)
        {
            RecentFolders.RemoveRange(MaxRecentFolders, RecentFolders.Count - MaxRecentFolders);
        }
    }

    private void Sanitize()
    {
        SanitizeWindow();
        RecentFolders ??= [];
        RecentFolders.RemoveAll(string.IsNullOrWhiteSpace);
        TrimRecent();
        if (Shell is not null && Shell is not ("pwsh" or "powershell" or "cmd" or "bash" or "custom"))
        {
            Shell = null;
        }

        if (PtyColumns is < 20 or > 1000)
        {
            PtyColumns = 240;
        }

        if (PtyRows is < 5 or > 500)
        {
            PtyRows = 50;
        }

        if (MaxTranscriptLines is < 1000 or > 1000000)
        {
            MaxTranscriptLines = 20000;
        }

        if (!Enum.IsDefined(OutputSpeech))
        {
            OutputSpeech = OutputSpeechMode.None;
        }
    }
}
