using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>
/// The player (SPEC.md AF-5): the folder's media files as the queue from the moment of Enter (AF-D6), the transport
/// keys, the speed, the status bar's clock, the video area, and the announcements (AF-7). The engine is
/// <see cref="MediaPlayer"/>; its events arrive on this thread.
/// </summary>
public sealed partial class FileForm
{
    private readonly System.Windows.Forms.Timer _clock = new() { Interval = 1000 };
    private MediaPlayer? _player;
    private IReadOnlyList<string> _queue = [];
    private int _queueIndex = -1;

    /// <summary>How the file now opening was asked for: by the user (said, interrupting), by the queue (said when the setting says, not interrupting), or not said.</summary>
    private bool _prompted;

    private string? PlayingName => _queueIndex >= 0 && _queueIndex < _queue.Count ? Path.GetFileName(_queue[_queueIndex]) : null;

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

    /// <summary>Player → Play (Enter on a media file): the entry, with the folder's other media files to follow (AF-5.3).</summary>
    private void PlaySelected()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        if (FolderListing.WayOf(entry) != OpenWay.Play)
        {
            Announce("Not a sound or video file", true);
            return;
        }

        PlayFromList(entry);
    }

    private void PlayFromList(Entry entry)
    {
        _queue = MediaFiles.Queue(_shown);
        _queueIndex = _queue.ToList().FindIndex(path => string.Equals(path, entry.Path, StringComparison.OrdinalIgnoreCase));
        if (_queueIndex < 0)
        {
            _queue = [entry.Path];
            _queueIndex = 0;
        }

        PlayCurrent(prompted: true);
    }

    /// <summary>Opens the file at the queue's index; a file that cannot be opened is said and the next one is tried (AF-5.1).</summary>
    private void PlayCurrent(bool prompted)
    {
        _prompted = prompted;
        var name = PlayingName ?? string.Empty;
        try
        {
            Player().Play(_queue[_queueIndex]);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            Log.Error("The file could not be played", ex);
            Announce($"Cannot play {name}: {ex.Message}", true);
            MoveOn();
            return;
        }

        UpdateStatusBar();
    }

    private void OnStarted()
    {
        FitVideo();
        _clock.Start();
        UpdateStatusBar();
        if (_prompted)
        {
            Announce($"Playing {PlayingName}", true);
        }
        else if (_settings.SayEachFile)
        {
            // The one unprompted announcement (G5): the next file of the folder the user set playing.
            Announce($"Playing {PlayingName}", false);
        }
    }

    /// <summary>The file ended (AF-5.3): the next media file of the folder, or "End of folder" with the done chime.</summary>
    private void OnEnded() => MoveOn();

    private void OnFailed(string reason)
    {
        Log.Error($"The player failed: {reason}");
        Announce($"Cannot play {PlayingName}: {reason}", true);
        MoveOn();
    }

    private void MoveOn()
    {
        if (_queueIndex + 1 < _queue.Count)
        {
            _queueIndex++;
            PlayCurrent(prompted: false);
            return;
        }

        _clock.Stop();
        _player?.Stop();
        FitVideo();
        UpdateStatusBar();
        Sounds.Done();
        Announce("End of folder", false);
    }

    /// <summary>Ctrl+P, Player → Pause or resume (AF-5.4), from anywhere in the window.</summary>
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

    /// <summary>Ctrl+Right and Ctrl+Left: <paramref name="seconds"/> forward or back, the new position said.</summary>
    private void Seek(int seconds)
    {
        if (_player is not { State: PlayerState.Playing or PlayerState.Paused } player)
        {
            Announce("Nothing playing", true);
            return;
        }

        var target = player.Position + TimeSpan.FromSeconds(seconds);
        if (target < TimeSpan.Zero)
        {
            target = TimeSpan.Zero;
        }

        player.Seek(target);
        UpdateStatusBar();
        Announce(MediaFiles.FormatTime(target), true);
    }

    /// <summary>Ctrl+Shift+Right and Ctrl+Shift+Left: the next or the previous file of the queue; "First file" and "End of folder" at the ends.</summary>
    private void MoveInQueue(int direction)
    {
        if (_queue.Count == 0 || _player is null)
        {
            Announce("Nothing playing", true);
            return;
        }

        var index = _queueIndex + direction;
        if (index < 0)
        {
            Announce("First file", true);
            return;
        }

        if (index >= _queue.Count)
        {
            Announce("End of folder", true);
            return;
        }

        _queueIndex = index;
        PlayCurrent(prompted: true);
    }

    /// <summary>Player → Faster, Slower, Normal speed: the rate for this file and the next ones, kept in the settings (AF-5.4).</summary>
    private void ChangeSpeed(double speed)
    {
        if (!Player().SetRate(speed))
        {
            Announce($"{MediaFiles.SpeedText(speed)} is not supported here", true);
            return;
        }

        _settings.Speed = speed;
        SaveSettings();
        Announce(MediaFiles.SpeedText(speed), true);
    }

    /// <summary>Player → Go to time… (Ctrl+T): a time in a notice, "12:40" when it is done.</summary>
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
                    Announce("Not a time", true);
                    return;
                }

                if (_player is { } player)
                {
                    player.Seek(time);
                    UpdateStatusBar();
                    Announce(MediaFiles.FormatTime(time), true);
                }
            }, IsDefault: true),
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
        Announce($"{word} {PlayingName}, {MediaFiles.FormatTime(_player.Position)} of {MediaFiles.FormatTime(_player.Duration)}, speed {_settings.Speed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}", true);
    }

    /// <summary>The status bar's left text (AF-2.3): the state, the file and the time while something plays; empty otherwise.</summary>
    private string PlayerStatusText()
    {
        var state = _player?.State;
        if (_player is null || state is not (PlayerState.Playing or PlayerState.Paused or PlayerState.Opening))
        {
            return string.Empty;
        }

        var word = state switch
        {
            PlayerState.Playing => "Playing",
            PlayerState.Paused => "Paused",
            _ => "Opening",
        };
        return $"{word} {PlayingName} {MediaFiles.FormatTime(_player.Position)} / {MediaFiles.FormatTime(_player.Duration)}";
    }

    /// <summary>The video area (AF-5.6): two fifths of the window while a video plays or is paused, hidden otherwise.</summary>
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
