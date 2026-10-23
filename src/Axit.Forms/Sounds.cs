using System.Runtime.InteropServices;
using Axit.Core;
using Axit.Core.Audio;

namespace Axit.Forms;

/// <summary>
/// The app's sounds (axClaude FR-7.3, FR-7.8, FR-7.9), played through winmm wave-out devices that are opened at the
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

    // A typewriter-like strike, a burst of noise over a 1.5 kHz tone that dies away within 10 ms: a line from Claude
    // arrived. Two lower knocks in a row, 12 ms each and rising: a tool call arrived. Softer versions were tried in
    // 2.4.0 and went unheard under speech; these are the 1.3.0 sounds, which the user likes.
    private static readonly Sound LineTick = new(WaveTone.Strikes(0.4, 10, 0.5, 1500));
    private static readonly Sound ToolTick = new(WaveTone.Strikes(0.5, 12, 0.5, 650, 850));

    // C6 alone, a high ping: Claude is ready. A4 alone, low: the message went out. C5, E5, G5 up to a long note:
    // Claude starts replying. The same notes down, G5, E5, C5: the turn is done. Two E5s: Claude needs an answer.
    // A quick C6-E6 trill: the answer notice opened in place of the conversation.
    private static readonly Sound ReadyChime = new(WaveTone.Notes(0.35, (1046.5, 120)));
    private static readonly Sound SentChime = new(WaveTone.Notes(0.35, (440, 80)));
    private static readonly Sound RespondingChime = new(WaveTone.Notes(0.35, (523.25, 90), (659.25, 90), (783.99, 220)));
    private static readonly Sound DoneChime = new(WaveTone.Notes(0.35, (783.99, 90), (659.25, 90), (523.25, 220)));
    private static readonly Sound QuestionChime = new(WaveTone.Notes(0.35, (659.25, 70), (0, 40), (659.25, 110)));
    private static readonly Sound NoticeChime = new(WaveTone.Notes(0.35, (1046.5, 45), (1318.5, 45), (1046.5, 45), (1318.5, 110)));

    // A3 alone, short and low: the edge, nothing there (a list's first or last entry under Up or Down, a typed name no
    // entry starts with; axFile AF-3.6). An earcon where a sentence would be noise, asked 2026-10-22.
    private static readonly Sound EdgeTick = new(WaveTone.Notes(0.35, (220, 60)));

    // G5, C6 up to a held E6, and once more: longer and louder than the chimes, so that Claude finishing or asking is
    // heard from another window, where the done chime was missed (axClaude FR-7.3).
    private static readonly Sound AlertChime = new(WaveTone.Notes(0.6, (783.99, 100), (1046.5, 100), (1318.5, 300), (0, 200), (783.99, 100), (1046.5, 100), (1318.5, 500)));

    // Sixteen soft plucked notes, an arpeggio up C, A minor, F and G at 500 ms a note: 8 s that loop without a seam
    // while Claude works (axClaude FR-7.9). Quiet, slow and dull next to the chimes, on a device of its own; the first
    // version, at half the chimes' peak and 300 ms a note with a music-box ring, was too much over NVDA's speech.
    private static readonly Sound WorkingMusic = new(WaveTone.Plucks(0.08, 500,
        261.63, 329.63, 392.00, 523.25,
        220.00, 261.63, 329.63, 440.00,
        174.61, 220.00, 261.63, 349.23,
        196.00, 246.94, 293.66, 392.00), loops: true);

    private static readonly Device Chimes = new("sounds", LineTick, ToolTick, ReadyChime, SentChime, RespondingChime, DoneChime, QuestionChime, NoticeChime, EdgeTick, AlertChime);
    private static readonly Device Music = new("working music", WorkingMusic);

    /// <summary>A tiny typewriter-like tick: a line from Claude arrived.</summary>
    /// <summary>
    /// Every sound asked for, by its name ("Notice", "Edge", "Ready"): the automation channel's record
    /// (docs/axit/automation.md). In a test run the sounds are recorded and not played, so that a run on the test
    /// desktop is silent on the user's speakers.
    /// </summary>
    public static event Action<string>? Played;

    private static readonly bool Silent = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AXIT_AUTOMATION"));

    private static void Play(string name, Action play)
    {
        Played?.Invoke(name);
        if (!Silent)
        {
            play();
        }
    }

    public static void Click() => Play("Click", () => Chimes.Play(LineTick, replace: false));

    /// <summary>Two lower knocks: a tool call arrived.</summary>
    public static void Tool() => Play("Tool", () => Chimes.Play(ToolTick, replace: false));

    /// <summary>A single high ping: Claude is ready.</summary>
    public static void Ready() => Play("Ready", () => Chimes.Play(ReadyChime, replace: true));

    /// <summary>A single low note: the message went out.</summary>
    public static void Sent() => Play("Sent", () => Chimes.Play(SentChime, replace: true));

    /// <summary>Three rising notes ending on a long one: Claude started replying.</summary>
    public static void Responding() => Play("Responding", () => Chimes.Play(RespondingChime, replace: true));

    /// <summary>Three falling notes ending on a long one: the turn is done.</summary>
    public static void Done() => Play("Done", () => Chimes.Play(DoneChime, replace: true));

    /// <summary>Two equal notes: Claude needs an answer.</summary>
    public static void Question() => Play("Question", () => Chimes.Play(QuestionChime, replace: true));

    /// <summary>A quick high trill: the answer notice opened and waits for an answer.</summary>
    public static void Notice() => Play("Notice", () => Chimes.Play(NoticeChime, replace: true));

    /// <summary>One short low note: the edge, nothing there (a key that moves nothing, a typed name no entry starts with).</summary>
    public static void Edge() => Play("Edge", () => Chimes.Play(EdgeTick, replace: true));

    /// <summary>A longer, louder call, three rising notes to a held one and again: Claude is done or asks while the window is in the background.</summary>
    public static void Alert() => Play("Alert", () => Chimes.Play(AlertChime, replace: true));

    /// <summary>Starts the soft music loop that plays while Claude works (axClaude FR-7.9); nothing while it plays already.</summary>
    public static void StartMusic() => Play("StartMusic", () => Music.Play(WorkingMusic, replace: false));

    /// <summary>Stops the music loop at once; nothing when it is not playing.</summary>
    public static void StopMusic() => Play("StopMusic", () => Music.Reset());

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
