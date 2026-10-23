using Axit.Core;

namespace AxTerminal.Core;

/// <summary>What is spoken of the shell's output as it arrives (SPEC.md AT-5.3).</summary>
public enum OutputSpeechMode
{
    None,
    All,
}

/// <summary>
/// Everything axTerminal remembers between runs (SPEC.md AT-8.1), in <c>%APPDATA%\Axit\axterminal.json</c> through
/// the bundle's <see cref="SettingsFile"/>: the shell, the last folder and the speech options here; the console size,
/// the recent folders, the sounds, the signals and the transcript options from <see cref="ConsoleSettings"/>, shared
/// with axClaude; the font, the update check and the window's place from <see cref="WindowSettings"/>. A missing or
/// unreadable file falls back to the defaults; out-of-range values do too.
/// </summary>
public sealed class TerminalSettings : ConsoleSettings
{
    /// <summary>The shell's word (<see cref="ShellLauncher.Key"/>); null means the default of the machine.</summary>
    public string? Shell { get; set; }

    /// <summary>The command line of the custom shell, kept for the New session notice.</summary>
    public string? CustomShell { get; set; }

    public string? LastFolder { get; set; }

    public OutputSpeechMode OutputSpeech { get; set; }

    /// <summary>"Asks: …" when a running program seems to wait on a question (AT-5.4).</summary>
    public bool AnnounceQuestions { get; set; } = true;

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

    /// <summary>The folder the shell starts in: the last one, and the front of the recent list (AT-1.5).</summary>
    public void RememberFolder(string folder)
    {
        LastFolder = folder;
        RememberRecentFolder(folder);
    }

    private void Sanitize()
    {
        SanitizeConsole();
        if (Shell is not null && Shell is not ("pwsh" or "powershell" or "cmd" or "bash" or "custom"))
        {
            Shell = null;
        }

        if (!Enum.IsDefined(OutputSpeech))
        {
            OutputSpeech = OutputSpeechMode.None;
        }
    }
}
