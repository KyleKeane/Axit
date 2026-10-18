using System.Text;
using Axit.Core;
using Axit.Core.Pty;
using Axit.Core.Transcript;
using Axit.Forms;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- find, export, recording, diagnostics (AT-4.5, AT-4.7, AT-8.2) ----

    /// <summary>Ctrl+F: the Find notice. Find next closes it and searches from the caret; an empty field keeps it open.</summary>
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

    /// <summary>Exports the transcript as the reader sees it (hidden rows left out) to a UTF-8 Markdown file in the current directory (AT-4.7).</summary>
    private void ExportTranscript()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export the transcript",
            Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"axterminal-{DateTime.Now:yyyy-MM-dd-HHmm}.md",
            AddExtension = true,
            DefaultExt = "md",
            InitialDirectory = Directory.Exists(CurrentDirectory) ? CurrentDirectory : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            File.WriteAllText(dialog.FileName, TranscriptMirror.Render(_model.Lines), new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail("The transcript could not be exported: " + ex.Message);
            return;
        }

        Log.Info("Transcript exported to " + dialog.FileName);
        Announce("Exported", true);
    }

    private void ToggleRecording()
    {
        if (_recorder is not null)
        {
            StopRecording(announce: true);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Record the raw console stream",
            Filter = "Console recording (*.vt)|*.vt|All files (*.*)|*.*",
            FileName = $"axterminal-{DateTime.Now:yyyyMMdd-HHmm}.vt",
            AddExtension = true,
            DefaultExt = "vt",
            // A recording holds everything the shell printed, prompts and paths included: outside the folder.
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            StartRecording(dialog.FileName);
        }
    }

    private void StartRecording(string path)
    {
        try
        {
            _recorder = new StreamRecorder(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail("The recording could not be started: " + ex.Message);
            return;
        }

        _recordItem.Text = "Stop &recording";
        Log.Info("Recording raw stream to " + path);
        _model.AddSystemLine("Recording to " + path);
        Announce("Recording started", false);
    }

    private void StopRecording(bool announce)
    {
        var recorder = _recorder;
        if (recorder is null)
        {
            return;
        }

        _recorder = null;
        recorder.Dispose();
        _recordItem.Text = RecordItemText;
        Log.Info($"Recording stopped: {recorder.Length} bytes in {recorder.Path}");
        if (announce)
        {
            _model.AddSystemLine($"Recording stopped: {recorder.Length} bytes in {recorder.Path}");
            Announce("Recording stopped", false);
        }
    }

    /// <summary>The recording file could not be written (reported from the reader thread): stop recording and say so.</summary>
    private void RecordingFailed(string reason)
    {
        if (_recorder is null)
        {
            return;
        }

        StopRecording(announce: false);
        Log.Error("Recording failed: " + reason);
        _model.AddSystemLine("Recording stopped, the file could not be written: " + reason);
        Announce("Recording stopped", false);
    }
}
