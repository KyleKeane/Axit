using System.Runtime.InteropServices;

namespace AxMedia.Core;

/// <summary>Where the player is (SPEC.md AM-4): nothing loaded, a file on its way in, playing, paused, or stopped at the end.</summary>
public enum PlayerState
{
    Empty,
    Opening,
    Playing,
    Paused,
    Stopped,
}

/// <summary>
/// The one seam to Windows' media stack (SPEC.md AM-4.2, AM-D5): Media Foundation through MFPlay (<c>mfplay.dll</c>,
/// one function and two COM interfaces, called through P/Invoke; no package). It plays one file at a time into the
/// window handle it is given for video; the sound goes to the default device; a rate other than 1 is stretched in
/// time by Media Foundation's audio renderer, which keeps the pitch. MFPlay reports its events on a thread of its
/// own choosing; the player reads two numbers there and hands the rest to <c>post</c>, the window's way onto its
/// thread, so that the engine is only ever called from the thread that made it. The queue, the keys and the
/// announcements are the window's.
/// </summary>
public sealed class MediaPlayer : IDisposable
{
    private readonly Action<Action> _post;
    private readonly Callback _callback;
    private readonly IMFPMediaPlayer _player;
    private IMFPMediaItem? _item;
    private bool _opening;
    private bool _disposed;
    private double _rate = 1;

    /// <param name="videoWindow">The handle a video's picture is drawn in; a sound file draws nothing there.</param>
    /// <param name="post">Runs an action on the thread that made the player (the window's <c>BeginInvoke</c>); every event is raised through it.</param>
    public MediaPlayer(nint videoWindow, Action<Action> post)
    {
        _post = post;
        _callback = new Callback(this);
        int hr;
        IMFPMediaPlayer? player;
        try
        {
            hr = MFPCreateMediaPlayer(null, false, MFP_OPTION_NONE, _callback, videoWindow, out player);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // An N edition of Windows without the Media Feature Pack has no mfplay.dll: the window's "Cannot play", not a crash (AM-4.2).
            throw new InvalidOperationException("The media player could not be created: Windows' media components are missing (the Media Feature Pack on an N edition of Windows).", ex);
        }

        if (hr < 0 || player is null)
        {
            throw new InvalidOperationException("The media player could not be created: " + Reason(hr));
        }

        _player = player;
        _player.SetAspectRatioMode(MFVideoARMode_PreservePicture);
    }

    /// <summary>The file given to <see cref="Play"/> is playing now.</summary>
    public event Action? Started;

    /// <summary>The file played to its end.</summary>
    public event Action? Ended;

    /// <summary>The file could not be opened or played, with the reason in one line.</summary>
    public event Action<string>? Failed;

    /// <summary>Whether the file given to <see cref="Play"/> has a picture (AM-2.4).</summary>
    public bool HasVideo { get; private set; }

    /// <summary>The playback rate, 1 being normal; kept across files.</summary>
    public double Rate => _rate;

    public PlayerState State
    {
        get
        {
            if (_disposed)
            {
                return PlayerState.Empty;
            }

            if (_opening)
            {
                return PlayerState.Opening;
            }

            return _player.GetState(out var state) < 0 ? PlayerState.Empty : state switch
            {
                MFP_MEDIAPLAYER_STATE_PLAYING => PlayerState.Playing,
                MFP_MEDIAPLAYER_STATE_PAUSED => PlayerState.Paused,
                MFP_MEDIAPLAYER_STATE_STOPPED => _item is null ? PlayerState.Empty : PlayerState.Stopped,
                _ => PlayerState.Empty,
            };
        }
    }

    /// <summary>Where the file is; zero when nothing is loaded.</summary>
    public TimeSpan Position => _disposed || _item is null ? TimeSpan.Zero : Time(_player.GetPosition(in MFP_POSITIONTYPE_100NS, out var value), value);

    /// <summary>How long the file is; zero when nothing is loaded or the length is unknown.</summary>
    public TimeSpan Duration => _disposed || _item is null ? TimeSpan.Zero : Time(_player.GetDuration(in MFP_POSITIONTYPE_100NS, out var value), value);

    /// <summary>
    /// Opens the file and starts it: the item is made here, synchronously, so that a file that cannot be opened
    /// throws with the reason; the playing itself starts when Media Foundation reports the item set, and
    /// <see cref="Started"/> fires when it reports playing. The state is <see cref="PlayerState.Opening"/> until
    /// then. A file already playing is replaced.
    /// </summary>
    public void Play(string path)
    {
        ThrowIfDisposed();
        _opening = false;
        HasVideo = false;
        if (_item is not null)
        {
            _player.Stop();
            ReleaseItem();
        }

        var hr = _player.CreateMediaItemFromURL(path, 1, 0, out var item);
        if (hr < 0 || item is null)
        {
            throw new IOException(Reason(hr));
        }

        _item = item;
        if (item.HasVideo(out var hasVideo, out var selected) >= 0)
        {
            HasVideo = hasVideo != 0 && selected != 0;
        }

        hr = _player.SetMediaItem(item);
        if (hr < 0)
        {
            ReleaseItem();
            throw new IOException(Reason(hr));
        }

        _opening = true;
    }

    public void Pause()
    {
        if (!_disposed)
        {
            _player.Pause();
        }
    }

    public void Resume()
    {
        if (!_disposed)
        {
            _player.Play();
        }
    }

    public void Stop()
    {
        if (!_disposed)
        {
            _player.Stop();
        }
    }

    /// <summary>Moves to <paramref name="position"/>, clamped to the file; takes effect at once while playing or paused.</summary>
    public void Seek(TimeSpan position)
    {
        if (_disposed || _item is null)
        {
            return;
        }

        var duration = Duration;
        if (position < TimeSpan.Zero)
        {
            position = TimeSpan.Zero;
        }

        if (duration > TimeSpan.Zero && position > duration)
        {
            position = duration;
        }

        // TimeSpan ticks are 100-nanosecond units, the unit MFPlay takes.
        var value = new PropVariant { vt = VT_I8, value = position.Ticks };
        _player.SetPosition(in MFP_POSITIONTYPE_100NS, in value);
    }

    /// <summary>Sets the rate, kept for the files that follow; false when the engine refuses it (AM-4.3).</summary>
    public bool SetRate(double rate)
    {
        if (_disposed)
        {
            return false;
        }

        _rate = rate;
        return _item is null || _player.SetRate((float)rate) >= 0;
    }

    /// <summary>The video window was resized or needs painting (AM-2.4).</summary>
    public void UpdateVideo()
    {
        if (!_disposed && HasVideo)
        {
            _player.UpdateVideo();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _player.Shutdown();
        }
        catch (COMException)
        {
        }

        ReleaseItem();
    }

    /// <summary>An event from MFPlay, on its thread: only the two numbers are read here; the rest happens on the window's thread.</summary>
    private void OnEvent(int type, int hr) => _post(() => Handle(type, hr));

    private void Handle(int type, int hr)
    {
        if (_disposed)
        {
            return;
        }

        switch (type)
        {
            case MFP_EVENT_TYPE_MEDIAITEM_SET:
                if (hr < 0)
                {
                    _opening = false;
                    Failed?.Invoke(Reason(hr));
                    return;
                }

                // The item is in; playing starts now, and the engine says so with its play event, which is when
                // GetState stops answering Stopped.
                var play = _player.Play();
                if (play < 0)
                {
                    _opening = false;
                    Failed?.Invoke(Reason(play));
                    return;
                }

                if (_rate != 1)
                {
                    _player.SetRate((float)_rate);
                }

                break;
            case MFP_EVENT_TYPE_PLAY:
                if (hr < 0)
                {
                    _opening = false;
                    Failed?.Invoke(Reason(hr));
                }
                else if (_opening)
                {
                    _opening = false;
                    Started?.Invoke();
                }

                break;
            case MFP_EVENT_TYPE_PLAYBACK_ENDED:
                Ended?.Invoke();
                break;
            case MFP_EVENT_TYPE_ERROR:
                _opening = false;
                Failed?.Invoke(Reason(hr));
                break;
        }
    }

    private void ReleaseItem()
    {
        if (_item is { } item)
        {
            _item = null;
            try
            {
                Marshal.ReleaseComObject(item);
            }
            catch (Exception ex) when (ex is COMException or InvalidOperationException)
            {
            }
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private static TimeSpan Time(int hr, PropVariant value) => hr < 0 || value.value < 0 ? TimeSpan.Zero : TimeSpan.FromTicks(value.value);

    /// <summary>One line for an HRESULT: Windows' message where it has one, the known Media Foundation codes by name, the number otherwise.</summary>
    public static string Reason(int hr)
    {
        switch (unchecked((uint)hr))
        {
            case 0xC00D36C4:
                return "this kind of file is not supported";
            case 0xC00D5212:
                return "no decoder for this file is installed";
            case 0xC00D36B2:
                return "the request is not valid now";
            case 0xC00D3E85:
                return "the player was shut down";
            case 0xC00D36E6:
                return "the speed is not supported";
        }

        // .NET writes the code after a message it knows, "… specified. (0x80070002)", and only the code otherwise.
        var message = Marshal.GetExceptionForHR(hr)?.Message ?? string.Empty;
        var code = message.IndexOf("(0x", StringComparison.OrdinalIgnoreCase);
        if (code >= 0)
        {
            message = message[..code];
        }

        message = message.Trim().TrimEnd('.');
        var unknown = message.Length == 0
            || message.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Exception from HRESULT", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Unknown error", StringComparison.OrdinalIgnoreCase);
        return unknown ? $"error 0x{hr:X8}" : message;
    }

    // ---- MFPlay (mfplay.h), as little of it as the player needs ----

    private const int MFP_OPTION_NONE = 0;
    private const int MFVideoARMode_PreservePicture = 1;
    private const ushort VT_I8 = 20;

    private const int MFP_EVENT_TYPE_PLAY = 0;
    private const int MFP_EVENT_TYPE_MEDIAITEM_SET = 6;
    private const int MFP_EVENT_TYPE_ERROR = 10;
    private const int MFP_EVENT_TYPE_PLAYBACK_ENDED = 11;

    private const int MFP_MEDIAPLAYER_STATE_STOPPED = 1;
    private const int MFP_MEDIAPLAYER_STATE_PLAYING = 2;
    private const int MFP_MEDIAPLAYER_STATE_PAUSED = 3;

    /// <summary>Positions in 100-nanosecond units: GUID_NULL.</summary>
    private static readonly Guid MFP_POSITIONTYPE_100NS = Guid.Empty;

    [DllImport("mfplay.dll", CharSet = CharSet.Unicode)]
    private static extern int MFPCreateMediaPlayer(
        [MarshalAs(UnmanagedType.LPWStr)] string? url,
        [MarshalAs(UnmanagedType.Bool)] bool startPlayback,
        int options,
        IMFPMediaPlayerCallback? callback,
        nint window,
        out IMFPMediaPlayer? player);

    /// <summary>A PROPVARIANT holding a 64-bit integer: the type, three reserved words, the value, and the rest of the union.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant
    {
        public ushort vt;
        public ushort reserved1;
        public ushort reserved2;
        public ushort reserved3;
        public long value;
        public nint rest;
    }

    /// <summary>The events, delivered as a header whose first two fields are the type and the HRESULT; the rest is not read.</summary>
    [ComVisible(true)]
    private sealed class Callback(MediaPlayer owner) : IMFPMediaPlayerCallback
    {
        public void OnMediaPlayerEvent(nint header) =>
            owner.OnEvent(Marshal.ReadInt32(header, 0), Marshal.ReadInt32(header, 4));
    }

    [ComImport]
    [Guid("766C8FFB-5FDB-4fea-A28D-B912996F51BD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMFPMediaPlayerCallback
    {
        [PreserveSig]
        void OnMediaPlayerEvent(nint header);
    }

    /// <summary>The interface in its vtable order; the methods the player does not call keep their slots.</summary>
    [ComImport]
    [Guid("A714590A-58AF-430a-85BF-44F5EC838D85")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMFPMediaPlayer
    {
        [PreserveSig]
        int Play();

        [PreserveSig]
        int Pause();

        [PreserveSig]
        int Stop();

        [PreserveSig]
        int FrameStep();

        [PreserveSig]
        int SetPosition(in Guid positionType, in PropVariant position);

        [PreserveSig]
        int GetPosition(in Guid positionType, out PropVariant position);

        [PreserveSig]
        int GetDuration(in Guid positionType, out PropVariant duration);

        [PreserveSig]
        int SetRate(float rate);

        [PreserveSig]
        int GetRate(out float rate);

        [PreserveSig]
        int GetSupportedRates(int forward, out float slowest, out float fastest);

        [PreserveSig]
        int GetState(out int state);

        [PreserveSig]
        int CreateMediaItemFromURL([MarshalAs(UnmanagedType.LPWStr)] string url, int sync, nint userData, out IMFPMediaItem? item);

        [PreserveSig]
        int CreateMediaItemFromObject(nint unknown, int sync, nint userData, out IMFPMediaItem? item);

        [PreserveSig]
        int SetMediaItem(IMFPMediaItem item);

        [PreserveSig]
        int ClearMediaItem();

        [PreserveSig]
        int GetMediaItem(out IMFPMediaItem? item);

        [PreserveSig]
        int GetVolume(out float volume);

        [PreserveSig]
        int SetVolume(float volume);

        [PreserveSig]
        int GetBalance(out float balance);

        [PreserveSig]
        int SetBalance(float balance);

        [PreserveSig]
        int GetMute(out int mute);

        [PreserveSig]
        int SetMute(int mute);

        [PreserveSig]
        int GetNativeVideoSize(nint size, nint aspectSize);

        [PreserveSig]
        int GetIdealVideoSize(nint minimum, nint maximum);

        [PreserveSig]
        int SetVideoSourceRect(nint rect);

        [PreserveSig]
        int GetVideoSourceRect(nint rect);

        [PreserveSig]
        int SetAspectRatioMode(uint mode);

        [PreserveSig]
        int GetAspectRatioMode(out uint mode);

        [PreserveSig]
        int GetVideoWindow(out nint window);

        [PreserveSig]
        int UpdateVideo();

        [PreserveSig]
        int SetBorderColor(uint color);

        [PreserveSig]
        int GetBorderColor(out uint color);

        [PreserveSig]
        int InsertEffect(nint effect, int optional);

        [PreserveSig]
        int RemoveEffect(nint effect);

        [PreserveSig]
        int RemoveAllEffects();

        [PreserveSig]
        int Shutdown();
    }

    [ComImport]
    [Guid("90EB3E6B-ECBF-45cc-B1DA-C6FE3EA70D57")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMFPMediaItem
    {
        [PreserveSig]
        int GetMediaPlayer(out nint player);

        [PreserveSig]
        int GetURL(out nint url);

        [PreserveSig]
        int GetObject(out nint unknown);

        [PreserveSig]
        int GetUserData(out nint userData);

        [PreserveSig]
        int SetUserData(nint userData);

        [PreserveSig]
        int GetStartStopPosition(nint startType, nint start, nint stopType, nint stop);

        [PreserveSig]
        int SetStartStopPosition(nint startType, nint start, nint stopType, nint stop);

        [PreserveSig]
        int HasVideo(out int hasVideo, out int selected);

        [PreserveSig]
        int HasAudio(out int hasAudio, out int selected);

        [PreserveSig]
        int IsProtected(out int isProtected);

        [PreserveSig]
        int GetDuration(in Guid positionType, out PropVariant duration);

        [PreserveSig]
        int GetNumberOfStreams(out uint count);

        [PreserveSig]
        int GetStreamSelection(uint index, out int enabled);

        [PreserveSig]
        int SetStreamSelection(uint index, int enabled);

        [PreserveSig]
        int GetStreamAttribute(uint index, in Guid attribute, out PropVariant value);

        [PreserveSig]
        int GetPresentationAttribute(in Guid attribute, out PropVariant value);

        [PreserveSig]
        int GetCharacteristics(out uint characteristics);

        [PreserveSig]
        int SetStreamSink(uint index, nint sink);

        [PreserveSig]
        int GetMetadata(out nint store);
    }
}
