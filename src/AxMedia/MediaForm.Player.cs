using AxMedia.Core;
using Axit.Core;
using Axit.Forms;

namespace AxMedia;

/// <summary>axMedia's window, playing (AM-4): the engine started on the queue's file, the transport, the position and the engine's events.</summary>
public sealed partial class MediaForm
{
    // ---- playing (AM-4) ----

    /// <summary>The engine, made on first use with the video area's handle and the remembered speed.</summary>
    private MediaPlayer Player()
    {
        if (_player is null)
        {
            _player = new MediaPlayer(_video.Handle, action =>
            {
                if (!IsDisposed)
                {
                    BeginInvoke(action);
                }
            });
            _player.Started += OnStarted;
            _player.Ended += OnEnded;
            _player.Failed += OnFailed;
            _player.SetRate(_settings.Speed);
        }

        return _player;
    }

    /// <summary>Opens the file at the queue's index; a file that cannot be opened is said and the next one is tried (AM-4.1).</summary>
    private void PlayCurrent(bool prompted)
    {
        if (_queue.PlayingPath is not { } path)
        {
            return;
        }

        _prompted = prompted;
        var name = _queue.PlayingName ?? string.Empty;
        try
        {
            Player().Play(path);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            Log.Error("The file could not be played", ex);
            Announce($"Cannot play {name}: {ex.Message}", true);
            MoveOn();
            return;
        }

        UpdateTitle();
        UpdateStatusBar();
    }

    private void OnStarted()
    {
        FitVideo();
        _clock.Start();
        UpdateTitle();
        UpdateStatusBar();
        FillList();
        if (_prompted)
        {
            Announce($"Playing {_queue.PlayingName}", true);
        }
        else if (_settings.SayEachFile)
        {
            // The one unprompted announcement (G5): the next file of the queue the user set playing.
            Announce($"Playing {_queue.PlayingName}", false);
        }
    }

    /// <summary>The file ended (AM-3.3): the next file of the queue, or "End of queue" with the done chime.</summary>
    private void OnEnded() => MoveOn();

    private void OnFailed(string reason)
    {
        Log.Error($"The player failed: {reason}");
        Announce($"Cannot play {_queue.PlayingName}: {reason}", true);
        MoveOn();
    }

    private void MoveOn()
    {
        if (_queue.MoveOn())
        {
            PlayCurrent(prompted: false);
            return;
        }

        StopPlayback();
        Sounds.Done();
        Announce("End of queue", false);
    }

    /// <summary>Playing stopped (the end of the queue, Clear queue, the playing file taken out): the clock, the engine, the video area, the title, the status bar and the list's mark.</summary>
    private void StopPlayback()
    {
        _clock.Stop();
        _player?.Stop();
        FitVideo();
        UpdateTitle();
        UpdateStatusBar();
        FillList();
    }

    /// <summary>Space in the list, Ctrl+P anywhere, Play → Pause or resume (AM-4.3).</summary>
    private void PauseResume()
    {
        switch (_player?.State)
        {
            case PlayerState.Playing:
                _player.Pause();
                _clock.Stop();
                UpdateStatusBar();
                Announce($"Paused at {MediaFiles.FormatTime(_player.Position)}", true);
                break;
            case PlayerState.Paused:
                _player.Resume();
                _clock.Start();
                UpdateStatusBar();
                Announce("Playing", true);
                break;
            default:
                Announce("Nothing playing", true);
                break;
        }
    }

    /// <summary>Ctrl+Right and Ctrl+Left: <paramref name="seconds"/> forward or back, the position reached said (the file's end at most, AM-4.3).</summary>
    private void Seek(int seconds)
    {
        if (_player is not { State: PlayerState.Playing or PlayerState.Paused } player)
        {
            Announce("Nothing playing", true);
            return;
        }

        var reached = player.Seek(player.Position + TimeSpan.FromSeconds(seconds));
        UpdateStatusBar();
        Announce(MediaFiles.FormatTime(reached), true);
    }

    /// <summary>Ctrl+Shift+Right and Ctrl+Shift+Left: the next or the previous file of the queue; "First file" and "End of queue" at the ends.</summary>
    private void MoveInQueue(int direction)
    {
        if (_queue.Playing < 0 || _player is null)
        {
            Announce("Nothing playing", true);
            return;
        }

        var index = _queue.Playing + direction;
        if (index < 0)
        {
            Announce("First file", true);
            return;
        }

        if (index >= _queue.Count)
        {
            Announce("End of queue", true);
            return;
        }

        _queue.Select(index);
        PlayCurrent(prompted: true);
    }

    /// <summary>Play → Faster, Slower, Normal speed: the rate for this file and the next ones, kept in the settings (AM-4.3).</summary>
    private void ChangeSpeed(double speed)
    {
        bool accepted;
        try
        {
            accepted = Player().SetRate(speed);
        }
        catch (InvalidOperationException ex)
        {
            // The engine could not be made (AM-4.2): a notice, not a crash.
            Log.Error("The media player could not be created", ex);
            Fail(ex.Message);
            return;
        }

        if (!accepted)
        {
            Announce($"{MediaFiles.SpeedText(speed)} is not supported here", true);
            return;
        }

        _settings.Speed = speed;
        SaveSettings();
        UpdateStatusBar();
        Announce(MediaFiles.SpeedText(speed), true);
    }

    /// <summary>Play → Go to time… (Ctrl+T): a time in a notice, "12:40" when it is done.</summary>
    private void GoToTime()
    {
        if (_player is not { State: PlayerState.Playing or PlayerState.Paused })
        {
            Announce("Nothing playing", true);
            return;
        }

        ShowNotice(new Notice("Go to time", "Minutes and seconds, 12:40; with hours, 1:02:10; or seconds alone, 750.",
        [
            new OverlayChoice("&Go", () =>
            {
                if (MediaFiles.ParseTime(Overlay.InputText) is not { } time)
                {
                    // The notice stays for another try (plan section 3 step 2; it closed until 2026-10-23).
                    Announce("Not a time", true);
                    return;
                }

                CloseNotice(null);
                if (_player is { } player)
                {
                    var reached = player.Seek(time);
                    UpdateStatusBar();
                    Announce(MediaFiles.FormatTime(reached), true);
                }
            }, IsDefault: true, StaysOpen: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Time:", "Time", string.Empty, "Type a time"),
        });
    }

    /// <summary>Ctrl+Q: what plays, where it is and how fast; "Nothing playing" otherwise.</summary>
    private void SayWhatIsPlaying()
    {
        var state = _player?.State;
        if (_player is null || state is not (PlayerState.Playing or PlayerState.Paused))
        {
            Announce("Nothing playing", true);
            return;
        }

        var word = state == PlayerState.Playing ? "Playing" : "Paused";
        Announce($"{word} {_queue.PlayingName}, {MediaFiles.FormatTime(_player.Position)} of {MediaFiles.FormatTime(_player.Duration)}, {MediaFiles.SpeedText(Speed).ToLowerInvariant()}", true);
    }

    /// <summary>The title (AM-2.3): "<name> - axMedia" while a file plays or is paused, "axMedia" otherwise; the Axit menu names the window by it.</summary>
    private void UpdateTitle() => Text = PlayerActive && _queue.PlayingName is { } name ? $"{name} - {AxMediaApp.Name}" : AxMediaApp.Name;

    /// <summary>The status bar (AM-2.2): the state, the file and the time on the left while something plays; the place and the speed on the right.</summary>
    private void UpdateStatusBar()
    {
        var state = _player?.State;
        if (_player is null || state is not (PlayerState.Playing or PlayerState.Paused or PlayerState.Opening))
        {
            _stateLabel.Text = string.Empty;
            _placeLabel.Text = string.Empty;
        }
        else
        {
            var word = state switch
            {
                PlayerState.Playing => "Playing",
                PlayerState.Paused => "Paused",
                _ => "Opening",
            };
            _stateLabel.Text = $"{word} {_queue.PlayingName} {MediaFiles.FormatTime(_player.Position)} / {MediaFiles.FormatTime(_player.Duration)}";
            _placeLabel.Text = _queue.PlaceText(Speed);
        }

        StatusLayout.Fit(_status, _stateLabel, _placeLabel);
    }

    /// <summary>The video area (AM-2.4): two fifths of the window while a video plays or is paused, hidden otherwise.</summary>
    private void FitVideo()
    {
        var show = !NoticeOpen && _player is { HasVideo: true, State: PlayerState.Playing or PlayerState.Paused or PlayerState.Opening };
        _video.Height = show ? Math.Max(60, ClientSize.Height * 2 / 5) : 0;
        _video.Visible = show;
        _player?.UpdateVideo();
    }

    private void StopPlayer()
    {
        _clock.Stop();
        if (_player is { } player)
        {
            _player = null;
            player.Dispose();
        }
    }
}
