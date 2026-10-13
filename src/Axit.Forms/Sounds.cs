using System.Runtime.InteropServices;
using Axit.Core;
using Axit.Core.Audio;

namespace Axit.Forms;

/// <summary>
/// The app's sounds (AxClaude FR-7.3, FR-7.8, FR-7.9), played through winmm wave-out devices that are opened at the
/// first sound and stay open for the life of the process (D31). Every sound is generated once, pinned and prepared
/// for its device; playing it is a single <c>waveOutWrite</c>, which queues the buffer behind whatever is playing. A
/// chime clears the queue first, so it is never delayed and a tick never cuts it short. A tick is dropped while its
/// own buffer is still queued, so ticks never pile up, and a tick that arrives during a chime follows the chime.
/// Until 1.2.1 the sounds went through <c>PlaySound</c>, which opens and closes the device for every call: ticks in
/// quick succession stuttered and backed up. The working music is the one sound that loops: one buffer written once
/// with the loop flags, on a second device of its own, where no chime's reset can cut it off and no tick waits
/// behind it, and stopped with a reset of that device. UI thread only.
/// </summary>
public static class Sounds
{
    private const uint WAVE_MAPPER = 0xFFFFFFFF;
    private const int WHDR_BEGINLOOP = 0x0004;
    private const int WHDR_ENDLOOP = 0x0008;
    private const int WHDR_INQUEUE = 0x0010;
    private static readonly int HeaderSize = Marshal.SizeOf<WAVEHDR>();
    private static readonly int FlagsOffset = (int)Marshal.OffsetOf<WAVEHDR>(nameof(WAVEHDR.dwFlags));
    private static readonly int LoopsOffset = (int)Marshal.OffsetOf<WAVEHDR>(nameof(WAVEHDR.dwLoops));

    /// <summary>A sound: its samples, pinned, and the wave header the device reads and writes, in unmanaged memory.</summary>
    private sealed class Sound
    {
        public Sound(byte[] wav, bool loops = false)
        {
            var data = GCHandle.Alloc(wav, GCHandleType.Pinned).AddrOfPinnedObject();
            Header = Marshal.AllocHGlobal(HeaderSize);
            var header = new WAVEHDR { lpData = data + WaveTone.HeaderLength, dwBufferLength = (uint)(wav.Length - WaveTone.HeaderLength) };
            Marshal.StructureToPtr(header, Header, false);
            Loops = loops;
        }

        public nint Header { get; }

        /// <summary>Once written, plays again without end until its device is reset.</summary>
        public bool Loops { get; }

        /// <summary>The device has not finished with the buffer: written and not yet played to the end.</summary>
        public bool Queued => (Marshal.ReadInt32(Header, FlagsOffset) & WHDR_INQUEUE) != 0;

        /// <summary>Marks the buffer as a loop of itself without end: set before each write, after the header was prepared (which wants the flags clear).</summary>
        public void MarkLoop()
        {
            Marshal.WriteInt32(Header, FlagsOffset, Marshal.ReadInt32(Header, FlagsOffset) | WHDR_BEGINLOOP | WHDR_ENDLOOP);
            Marshal.WriteInt32(Header, LoopsOffset, -1);
        }
    }

    /// <summary>One wave-out device and the sounds prepared for it: opened at the first sound, closed when a write fails, silent for the run when it cannot open.</summary>
    private sealed class Device
    {
        private readonly string _name;
        private readonly Sound[] _sounds;
        private nint _handle;
        private bool _failed;

        public Device(string name, params Sound[] sounds)
        {
            _name = name;
            _sounds = sounds;
        }

        /// <summary>Queues the sound; with <paramref name="replace"/> it first stops and drops whatever is queued, otherwise it is skipped while it is queued itself.</summary>
        public void Play(Sound sound, bool replace)
        {
            if (_failed)
            {
                return;
            }

            try
            {
                if (_handle == 0 && !Open())
                {
                    return;
                }

                if (replace)
                {
                    waveOutReset(_handle);
                }
                else if (sound.Queued)
                {
                    return;
                }

                if (sound.Loops)
                {
                    sound.MarkLoop();
                }

                var result = waveOutWrite(_handle, sound.Header, HeaderSize);
                if (result != 0)
                {
                    // The device went away (unplugged, switched): the next sound opens the current one.
                    Log.Error($"waveOutWrite failed with {result}; the {_name} device is reopened at the next sound");
                    Close();
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                // No winmm on this Windows: log once and stay quiet; the announcements still work.
                _failed = true;
                Log.Error($"No {_name} on this Windows", ex);
            }
        }

        /// <summary>Stops and drops whatever is queued; nothing while the device is not open.</summary>
        public void Reset()
        {
            if (_handle != 0)
            {
                waveOutReset(_handle);
            }
        }

        /// <summary>Opens the default output device for the sounds' format (16-bit mono PCM) and prepares every sound's buffer for it.</summary>
        private bool Open()
        {
            var format = new WAVEFORMATEX
            {
                wFormatTag = 1, // PCM
                nChannels = 1,
                nSamplesPerSec = WaveTone.SampleRate,
                nAvgBytesPerSec = WaveTone.SampleRate * 2,
                nBlockAlign = 2,
                wBitsPerSample = 16,
            };
            var result = waveOutOpen(out _handle, WAVE_MAPPER, ref format, 0, 0, 0);
            if (result != 0)
            {
                _handle = 0;
                _failed = true;
                Log.Error($"waveOutOpen failed with {result}; no {_name} this run");
                return false;
            }

            foreach (var sound in _sounds)
            {
                result = waveOutPrepareHeader(_handle, sound.Header, HeaderSize);
                if (result != 0)
                {
                    Log.Error($"waveOutPrepareHeader failed with {result}; no {_name} this run");
                    Close();
                    _failed = true;
                    return false;
                }
            }

            return true;
        }

        private void Close()
        {
            waveOutReset(_handle);
            foreach (var sound in _sounds)
            {
                waveOutUnprepareHeader(_handle, sound.Header, HeaderSize);
            }

            waveOutClose(_handle);
            _handle = 0;
        }
    }

    // A soft tap, a 30 ms A5 tone that dies away, then 60 ms of silence in the same buffer so that ticks come at most
    // about eleven a second: a line from Claude arrived. Two soft low taps, E4 then A4, 40 ms each: a tool call
    // arrived. Until 2.4.0 these were a burst of noise over a tone and two knocks, at up to a hundred a second: sharp
    // enough to jitter over NVDA's speech and seem to pull its volume about.
    private static readonly Sound LineTick = new(WaveTone.Strikes(0.2, 30, 0, 880, 0, 0));
    private static readonly Sound ToolTick = new(WaveTone.Strikes(0.25, 40, 0, 329.63, 440));

    // C6 alone, a high ping: Claude is ready. A4 alone, low: the message went out. C5, E5, G5 up to a long note:
    // Claude starts replying. The same notes down, G5, E5, C5: the turn is done. Two E5s: Claude needs an answer.
    // A quick C6-E6 trill: the answer notice opened in place of the conversation.
    private static readonly Sound ReadyChime = new(WaveTone.Notes(0.35, (1046.5, 120)));
    private static readonly Sound SentChime = new(WaveTone.Notes(0.35, (440, 80)));
    private static readonly Sound RespondingChime = new(WaveTone.Notes(0.35, (523.25, 90), (659.25, 90), (783.99, 220)));
    private static readonly Sound DoneChime = new(WaveTone.Notes(0.35, (783.99, 90), (659.25, 90), (523.25, 220)));
    private static readonly Sound QuestionChime = new(WaveTone.Notes(0.35, (659.25, 70), (0, 40), (659.25, 110)));
    private static readonly Sound NoticeChime = new(WaveTone.Notes(0.35, (1046.5, 45), (1318.5, 45), (1046.5, 45), (1318.5, 110)));

    // G5, C6 up to a held E6, and once more: longer and louder than the chimes, so that Claude finishing or asking is
    // heard from another window, where the done chime was missed (AxClaude FR-7.3).
    private static readonly Sound AlertChime = new(WaveTone.Notes(0.6, (783.99, 100), (1046.5, 100), (1318.5, 300), (0, 200), (783.99, 100), (1046.5, 100), (1318.5, 500)));

    // Sixteen soft plucked notes, like a music box, an arpeggio up C, A minor, F and G at 300 ms a note: 4.8 s that
    // loop without a seam while Claude works (AxClaude FR-7.9). Quieter than the chimes, on a device of its own.
    private static readonly Sound WorkingMusic = new(WaveTone.Plucks(0.18, 300,
        261.63, 329.63, 392.00, 523.25,
        220.00, 261.63, 329.63, 440.00,
        174.61, 220.00, 261.63, 349.23,
        196.00, 246.94, 293.66, 392.00), loops: true);

    private static readonly Device Chimes = new("sounds", LineTick, ToolTick, ReadyChime, SentChime, RespondingChime, DoneChime, QuestionChime, NoticeChime, AlertChime);
    private static readonly Device Music = new("working music", WorkingMusic);

    /// <summary>A soft tap: a line from Claude arrived.</summary>
    public static void Click() => Chimes.Play(LineTick, replace: false);

    /// <summary>Two soft low taps: a tool call arrived.</summary>
    public static void Tool() => Chimes.Play(ToolTick, replace: false);

    /// <summary>A single high ping: Claude is ready.</summary>
    public static void Ready() => Chimes.Play(ReadyChime, replace: true);

    /// <summary>A single low note: the message went out.</summary>
    public static void Sent() => Chimes.Play(SentChime, replace: true);

    /// <summary>Three rising notes ending on a long one: Claude started replying.</summary>
    public static void Responding() => Chimes.Play(RespondingChime, replace: true);

    /// <summary>Three falling notes ending on a long one: the turn is done.</summary>
    public static void Done() => Chimes.Play(DoneChime, replace: true);

    /// <summary>Two equal notes: Claude needs an answer.</summary>
    public static void Question() => Chimes.Play(QuestionChime, replace: true);

    /// <summary>A quick high trill: the answer notice opened and waits for an answer.</summary>
    public static void Notice() => Chimes.Play(NoticeChime, replace: true);

    /// <summary>A longer, louder call, three rising notes to a held one and again: Claude is done or asks while the window is in the background.</summary>
    public static void Alert() => Chimes.Play(AlertChime, replace: true);

    /// <summary>Starts the soft music loop that plays while Claude works (AxClaude FR-7.9); nothing while it plays already.</summary>
    public static void StartMusic() => Music.Play(WorkingMusic, replace: false);

    /// <summary>Stops the music loop at once; nothing when it is not playing.</summary>
    public static void StopMusic() => Music.Reset();

    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEHDR
    {
        public nint lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public nint dwUser;
        public uint dwFlags;
        public uint dwLoops;
        public nint lpNext;
        public nint reserved;
    }

    [DllImport("winmm.dll")]
    private static extern uint waveOutOpen(out nint hwo, uint uDeviceID, ref WAVEFORMATEX pwfx, nint dwCallback, nint dwInstance, uint fdwOpen);

    [DllImport("winmm.dll")]
    private static extern uint waveOutPrepareHeader(nint hwo, nint pwh, int cbwh);

    [DllImport("winmm.dll")]
    private static extern uint waveOutUnprepareHeader(nint hwo, nint pwh, int cbwh);

    [DllImport("winmm.dll")]
    private static extern uint waveOutWrite(nint hwo, nint pwh, int cbwh);

    [DllImport("winmm.dll")]
    private static extern uint waveOutReset(nint hwo);

    [DllImport("winmm.dll")]
    private static extern uint waveOutClose(nint hwo);
}
