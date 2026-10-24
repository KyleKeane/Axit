using AxTerminal.Core;
using Axit.Forms;

namespace AxTerminal;

internal sealed partial class TerminalForm
{
    // ---- what the model reports (AT-5): the console window has synced the view, the status bar and the music first ----

    protected override void OnTranscriptChanged()
    {
        UpdateTitle();

        // One pass over the transcript for the tick (AT-5.2) and the spoken output (AT-5.3).
        var arrival = _arrivals.Update(_model.Lines, _model.Running, _settings.OutputSpeech == OutputSpeechMode.All);
        if (_settings.ClickOnNewLine && arrival.NewLine)
        {
            Sounds.Click();
        }

        if (arrival.Speech is { } speech)
        {
            Announce(speech, false);
        }

        SayFinished();
    }

    /// <summary>"Done" or "Failed" of the command that ended, waiting for the frame that ends it to be spoken first.</summary>
    private string? _finished;

    /// <summary>Half a second at most for that frame; then the word goes anyway.</summary>
    private readonly System.Windows.Forms.Timer _finishedLate = new() { Interval = 500 };

    private void SayFinished()
    {
        _finishedLate.Stop();
        if (_finished is { } word)
        {
            _finished = null;
            Announce(word, false);
            Signal(question: false);
        }
    }

    /// <summary>The first prompt of a run (AT-5.1): "Ready" with its ping.</summary>
    private void OnReady()
    {
        Announce("Ready", false);
        if (_settings.SoundOnBell)
        {
            Sounds.Ready();
        }

        UpdateStatus();
    }

    /// <summary>The prompt came back after a command (AT-5.1): Done, or Failed with the code, with the done chime and the attention signals.</summary>
    private void OnCommandFinished(int? code)
    {
        _askedRow = null;
        // The end of a command is reported while its output is still being read, before the frame that makes its last
        // line final: the word waits for that frame's speech (OnTranscriptChanged), so that Speak output: all lines
        // says the last line before "Done", not after it (found by tools/AxAutomate, 2026-10-23).
        _finished = code is null or 0 ? "Done" : code == -1 ? "Failed" : $"Failed, exit code {code}";
        if (!_finishedLate.Enabled)
        {
            _finishedLate.Tick -= OnFinishedLate;
            _finishedLate.Tick += OnFinishedLate;
            _finishedLate.Start();
        }
    }

    private void OnFinishedLate(object? sender, EventArgs e) => SayFinished();

    /// <summary>
    /// AT-5.4: while a command runs and output has stopped for two seconds, a cursor row that ends like a question
    /// (a colon, a question mark, a closing bracket or parenthesis) is announced once as "Asks: …", with the question
    /// chime. A heuristic: a program's prompt is the only sign the window has that it waits.
    /// </summary>
    private void CheckForQuestion()
    {
        if (!_settings.AnnounceQuestions || !ProcessRunning || !_model.Running || Environment.TickCount64 - LastOutputTick < AsksQuietMs)
        {
            return;
        }

        var row = _model.CursorRowText?.TrimEnd();
        if (row is null || row == _askedRow || row.Length == 0 || row[^1] is not (':' or '?' or ']' or ')'))
        {
            return;
        }

        _askedRow = row;
        Announce($"Asks: {row}", false);
        Signal(question: true);
    }

    /// <summary>Ctrl+Q (AT-2.3, AT-D8): the state, the command and how long it runs, and the prompt, from anywhere.</summary>
    private void SayWhatIsRunning()
    {
        string text;
        if (!ProcessRunning)
        {
            text = "Shell ended";
        }
        else if (_model.Running)
        {
            var seconds = _model.RunningSince is { } since ? (int)(DateTime.Now - since).TotalSeconds : 0;
            text = $"Running: {_model.RunningCommand}, {seconds} seconds";
        }
        else if (!_model.HasMarks)
        {
            text = "No marks from this shell";
        }
        else
        {
            text = _model.Prompt is null ? "Starting" : $"Idle, {ShellLauncher.Describe(_shell)}";
        }

        Announce(_model.Prompt is { } prompt ? $"{text}, {prompt}" : $"{text}, {CurrentDirectory}", true);
    }

    /// <summary>Session → Say current folder (Ctrl+P, AT-D9): the full path of the shell's directory, or of the start folder while the shell gives no marks.</summary>
    private void SayFolder() => Announce(CurrentDirectory, true);

    /// <summary>Session → Clear transcript (Ctrl+L, AT-4.4): the window empties; the shell goes on.</summary>
    private void ClearTranscript()
    {
        _model.Clear();
        _arrivals.Reset();
        Announce("Cleared", true);
    }
}
