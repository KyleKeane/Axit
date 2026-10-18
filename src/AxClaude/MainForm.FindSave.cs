using System.Text;
using AxClaude.Core.Privacy;
using AxClaude.Core.Transcript;
using Axit.Core;
using Axit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- find and save ----

    /// <summary>Ctrl+F: the Find notice (FR-3.7). Find next closes it and searches from the caret; an empty field keeps it open.</summary>
    private void FindDialog()
    {
        ShowNotice(new Notice("Find", string.Empty,
        [
            new OverlayChoice("Find &next", () =>
            {
                _findText = Overlay.InputText;
                _transcript.Find(_findText, backward: false);
            }, IsDefault: true),
            new OverlayChoice("Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("&Find:", "Text to find", _findText, "Type the text to find"),
        });
    }

    private void FindNext(bool backward)
    {
        if (_findText.Length == 0)
        {
            FindDialog();
            return;
        }

        _transcript.Find(_findText, backward);
    }

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

    /// <summary>Exports the conversation as the reader sees it (hidden rows left out) to a UTF-8 text file (FR-3.8).</summary>
    private void ExportConversation()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export the conversation as text",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            // Outside the project, and named axclaude-… so the exclude patterns catch it wherever it goes (D34).
            FileName = $"axclaude-{(_folder is null ? "conversation" : RecentMenu.FolderName(_folder))}-{DateTime.Now:yyyyMMdd-HHmm}.txt",
            AddExtension = true,
            DefaultExt = "txt",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            // The lines as the view shows them, without the reading break the view adds while a reply streams (FR-3.10).
            File.WriteAllText(dialog.FileName, TranscriptMirror.Render(_model.Lines), new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail("The conversation could not be exported: " + ex.Message);
            return;
        }

        Log.Info("Conversation exported to " + dialog.FileName);
        Announce("Exported", true);
    }
}
