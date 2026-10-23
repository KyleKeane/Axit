using AxClaude.Core.Privacy;
using Axit.Core;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- the export and the git exclude (FR-3.8, D34); find and the recording are the console window's ----

    /// <summary>Ctrl+S, Session → Export conversation…: the conversation as the reader sees it to a UTF-8 text file (FR-3.8), named axclaude-… outside the project so that the exclude patterns catch it wherever it goes (D34).</summary>
    private void ExportConversation() =>
        Export("conversation", "Export the conversation as text", "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            $"axclaude-{(_folder is null ? "conversation" : PathName.Of(_folder))}-{DateTime.Now:yyyyMMdd-HHmm}.txt", "txt",
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

    /// <summary>
    /// Adds AxClaude's patterns to the local exclude file of the project's git repository, or removes them, as the
    /// option says (D34). A failure is logged and changes nothing else.
    /// </summary>
    private void KeepFilesOutOfGit()
    {
        if (_folder is null)
        {
            return;
        }

        try
        {
            if (GitExclude.Update(_folder, _settings.KeepFilesOutOfGit) is { } path)
            {
                Log.Info($"{(_settings.KeepFilesOutOfGit ? "Added AxClaude's patterns to" : "Removed AxClaude's patterns from")} {path}");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Could not update the git exclude file: {ex.Message}");
        }
    }
}
