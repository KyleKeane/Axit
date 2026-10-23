using System.Text.Json.Serialization;
using AxClaude.Core.Transcript;
using Axit.Core;

namespace AxClaude.Core;

/// <summary>
/// Everything the app remembers between runs. Stored as JSON in <c>%APPDATA%\Axit\axclaude.json</c> (see SPEC.md
/// Appendix C) through the bundle's <see cref="SettingsFile"/>; the first start under Axit copies AxClaude 1.x's
/// <c>%APPDATA%\AxClaude\settings.json</c> there (<see cref="TakeOverOldFile"/>). A missing or unreadable file falls
/// back to the defaults. The console fields (the size, the recent folders, the sounds, the signals, the transcript
/// options) are <see cref="ConsoleSettings"/>', shared with AxTerminal; the font, the update check and the window's
/// place are <see cref="WindowSettings"/>' (bundle AX-6.5).
/// </summary>
public sealed class AppSettings : ConsoleSettings
{
    public string? LastProjectFolder { get; set; }
    public string? ClaudePath { get; set; }

    public bool AnnounceBell { get; set; } = true;

    /// <summary>How much of a reply is spoken as it arrives (FR-7.4): none, the first line of each message, or all.</summary>
    public ReplySpeechMode ReplySpeech { get; set; }

    /// <summary>The 1.1 key, true meaning every line: read from an old file, turned into <see cref="ReplySpeech"/> and no longer written.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SpeakReplies { get; set; }

    /// <summary>Speak the <c>tool:</c> row of each tool call as it arrives, whatever <see cref="ReplySpeech"/> says (FR-7.4).</summary>
    public bool SpeakToolCalls { get; set; }

    /// <summary>
    /// Recognise what Claude waits on and show it in the control area in the message field's place, and keep
    /// messages out of a waiting question (D33, FR-2.10). Off: questions are only announced, as before 1.4.0.
    /// </summary>
    public bool QuestionNotices { get; set; } = true;

    /// <summary>
    /// A question Claude asks takes the focus wherever it is and cuts off what NVDA was reading, a reply included
    /// (D33). Off: the focus moves only from the bottom of the window, and the question waits after what is read.
    /// </summary>
    public bool InterruptForQuestions { get; set; } = true;

    /// <summary>
    /// Show Claude's "Review your answers" after the last of its questions (D33). Off: the app submits the review for
    /// the user, unless it warns that a question has no answer.
    /// </summary>
    public bool ReviewAnswers { get; set; }

    /// <summary>
    /// AxClaude's recordings and exported conversations (<c>axclaude-…</c>) are listed in the project repository's local
    /// exclude file, so git never offers them for a commit (D34). Off removes the lines.
    /// </summary>
    public bool KeepFilesOutOfGit { get; set; } = true;

    public static string DefaultPath => AppPaths.SettingsFile("axclaude");

    /// <summary>Where AxClaude 1.x kept the file, before the Axit bundle.</summary>
    public static string OldPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AxClaude", "settings.json");

    /// <summary>Copies the 1.x file to <see cref="DefaultPath"/> when there is one and no new file yet (docs/axit/SPEC.md AX-5.2).</summary>
    public static void TakeOverOldFile() => SettingsFile.TakeOverOldFile(OldPath, DefaultPath);

    /// <summary>Loads the file, or returns defaults. <paramref name="error"/> describes an unreadable file.</summary>
    public static AppSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<AppSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    /// <summary>Writes the file atomically: a temporary file next to it is moved into place.</summary>
    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>The folder Claude starts in: the last one, and the front of the recent list (FR-8.1).</summary>
    public void RememberFolder(string folder)
    {
        LastProjectFolder = folder;
        RememberRecentFolder(folder);
    }

    private void Sanitize()
    {
        SanitizeConsole();
        if (SpeakReplies && ReplySpeech == ReplySpeechMode.None)
        {
            ReplySpeech = ReplySpeechMode.All;
        }

        SpeakReplies = false;
    }
}
