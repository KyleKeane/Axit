using System.Text;
using Axit.Core;
using Axit.Core.Pty;
using Axit.Core.Transcript;

namespace Axit.Forms;

public abstract partial class ConsoleForm
{
    // ---- find, export, recording (AxClaude FR-3.7, FR-3.8, FR-13.3; AxTerminal AT-4.5, AT-4.7, AT-8.2) ----

    private StreamRecorder? _recorder;

    /// <summary>Ctrl+F: the bundle's Find notice over the view's search. Find next closes it and searches from the caret; an empty field keeps it open.</summary>
    protected void FindDialog() => ShowFind(text => Transcript.Find(text, backward: false));

    /// <summary>F3 and Shift+F3: the last text again from the caret; the Find notice when there is none yet.</summary>
    protected void FindNext(bool backward)
    {
        if (FindText.Length == 0)
        {
            FindDialog();
            return;
        }

        Transcript.Find(FindText, backward);
    }

    /// <summary>
    /// The transcript as the reader sees it (hidden rows left out, the reading break the view adds left out) to a
    /// UTF-8 file chosen in the Windows save dialog: <paramref name="what"/> names it in the log and the error ("the
    /// conversation"), the rest is the dialog's title, filter, proposed name, extension and folder.
    /// </summary>
    protected void Export(string what, string title, string filter, string fileName, string extension, string folder)
    {
        using var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = fileName,
            AddExtension = true,
            DefaultExt = extension,
            InitialDirectory = folder,
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        try
        {
            File.WriteAllText(dialog.FileName, TranscriptMirror.Render(Model.Lines), new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail($"The {what} could not be exported: {ex.Message}");
            return;
        }

        Log.Info($"The {what} was exported to {dialog.FileName}");
        Announce("Exported", true);
    }

    /// <summary>Options → Developer → Record raw stream: the console stream to a .vt file in the PtyCapture format, for a fixture; the same item stops it.</summary>
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
            FileName = $"{AppName.ToLowerInvariant()}-{DateTime.Now:yyyyMMdd-HHmm}.vt",
            AddExtension = true,
            DefaultExt = "vt",
            // A recording holds everything on screen, account details and paths included: outside the project (AxClaude D34).
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (ShowSystemDialog(dialog) == DialogResult.OK)
        {
            StartRecording(dialog.FileName);
        }
    }

    /// <summary>Records the raw stream to <paramref name="path"/> from now (the menu, or AxClaude's --record).</summary>
    protected void StartRecording(string path)
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

        RecordItem.Text = "Stop &recording";
        Log.Info("Recording raw stream to " + path);
        Model.AddSystemLine("Recording to " + path);
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
        RecordItem.Text = RecordItemText;
        Log.Info($"Recording stopped: {recorder.Length} bytes in {recorder.Path}");
        if (announce)
        {
            Model.AddSystemLine($"Recording stopped: {recorder.Length} bytes in {recorder.Path}");
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
        Model.AddSystemLine("Recording stopped, the file could not be written: " + reason);
        Announce("Recording stopped", false);
    }
}
