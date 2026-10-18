using System.Runtime.InteropServices;
using AxClaude.Core;
using AxClaude.Core.Pty;
using Axit.Core;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- diagnostics ----

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
            FileName = $"axclaude-{DateTime.Now:yyyyMMdd-HHmm}.vt",
            AddExtension = true,
            DefaultExt = "vt",
            // A recording holds everything on screen, /status's account details included: outside the project (D34).
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

    protected override void CopyDiagnostics()
    {
        var claude = ClaudeLauncher.Find(_options.ClaudePath ?? _settings.ClaudePath);
        var text = string.Join("\r\n",
        [
            $"AxClaude, Axit {BundleInfo.Version} on .NET {Environment.Version}, {Environment.OSVersion}",
            $"Program: {Environment.ProcessPath ?? "(unknown)"} (process {Environment.ProcessId})",
            $"Claude: {claude ?? "(not found)"}",
            $"Project folder: {_folder ?? "(none)"}",
            $"Console size: {_model.Columns}x{_model.Rows}",
            $"Settings: {AppSettings.DefaultPath}",
            $"Log: {Log.FilePath}",
            $"Claude state: {_state.Text}",
            string.Empty,
            Log.Tail(200),
        ]);
        if (CopyToClipboard(text))
        {
            Announce("Copied", true);
        }
    }

    /// <summary>Another program can hold the clipboard for a moment; that is announced rather than reported as a crash.</summary>
    private bool CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (ExternalException ex)
        {
            Log.Error("The clipboard could not be written", ex);
            Announce("Could not copy. Try again", true);
            return false;
        }
    }
}
