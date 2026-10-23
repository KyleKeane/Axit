namespace Axit.Core;

/// <summary>
/// What the two console windows, AxClaude and AxTerminal, remember between runs beyond <see cref="WindowSettings"/>
/// (docs/axit/SPEC.md AX-6.5): the console size, the recent folders, the sounds and the attention signals, and the
/// transcript's options. Their shared window base (<c>Axit.Forms/ConsoleForm</c>) reads these without knowing the
/// app; each app's settings class derives from it and adds its own, every property keeping the JSON name it had
/// (the two files were written with the same names before the class existed, 2026-10-23).
/// </summary>
public abstract class ConsoleSettings : WindowSettings
{
    public const int MaxRecentFolders = 10;

    /// <summary>The folders the program was started in, newest first, for the Open recent folder submenu (AxClaude FR-8.1, AxTerminal AT-1.5).</summary>
    public List<string> RecentFolders { get; set; } = [];

    /// <summary>The pseudo console's size; 240 columns keep Claude Code's rows from wrapping before the model joins them.</summary>
    public int PtyColumns { get; set; } = 240;

    public int PtyRows { get; set; } = 50;

    /// <summary>The chimes: ready, sent, replying, done, question (AxClaude FR-7.3, AxTerminal AT-5.1). The key keeps its 1.0 name.</summary>
    public bool SoundOnBell { get; set; } = true;

    /// <summary>A tick whenever a line from the program arrives (AxClaude FR-7.8, AxTerminal AT-5.2).</summary>
    public bool ClickOnNewLine { get; set; } = true;

    /// <summary>A soft music loop while the program works (AxClaude FR-7.9, AxTerminal AT-5.5).</summary>
    public bool WorkingMusic { get; set; }

    /// <summary>The taskbar button flashes when the program is done or asks while the window is in the background.</summary>
    public bool FlashTaskbar { get; set; } = true;

    /// <summary>A longer, louder call in place of the done or question chime while the window is in the background (AxClaude FR-7.3).</summary>
    public bool BackgroundAlert { get; set; }

    /// <summary>The time of sending in the Input and Output markers (AxClaude FR-4.6, AxTerminal AT-3.2).</summary>
    public bool MarkerTimeStamps { get; set; }

    /// <summary>The # markers of the heading lines shown in the view (AxClaude FR-4.6a); the export keeps them either way.</summary>
    public bool ShowMarkers { get; set; } = true;

    /// <summary>Show rows the program hard-wrapped at the console width as one line (AxClaude FR-3.2a, AxTerminal AT-4.1). File only, no menu item.</summary>
    public bool JoinWrappedLines { get; set; } = true;

    /// <summary>Above this many lines the oldest tenth of the transcript is dropped (AxClaude FR-3.6).</summary>
    public int MaxTranscriptLines { get; set; } = 20000;

    /// <summary>The folder the program starts in goes to the front of the recent list; each class keeps the last folder under its own name as well.</summary>
    protected void RememberRecentFolder(string folder) => RecentPaths.Remember(RecentFolders, folder, MaxRecentFolders);

    /// <summary>The console fields back to their defaults when the file holds nonsense, the window fields with them; each class's Sanitize calls it first.</summary>
    protected void SanitizeConsole()
    {
        SanitizeWindow();
        RecentFolders = RecentPaths.Sanitize(RecentFolders, MaxRecentFolders);
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
    }
}
