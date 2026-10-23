using System.Runtime.InteropServices;

namespace Axit.Forms;

public abstract partial class ConsoleForm
{
    // ---- the attention signals and the music (axClaude FR-7.3, FR-7.9; axTerminal AT-5.1, AT-5.5) ----

    private const uint FLASHW_ALL = 3;
    private const uint FLASHW_TIMERNOFG = 12;

    /// <summary>The working music is playing.</summary>
    private bool _musicOn;

    /// <summary>
    /// The chime (two equal notes for a question, three falling notes when the program is done) and the taskbar
    /// flash that go with an attention announcement; without <paramref name="chime"/> only the flash (axClaude's
    /// control area plays the notice chime for what it shows). While the window is in the background the alert,
    /// when it is on, plays in place of any chime: it is the one sound meant to carry into another window.
    /// </summary>
    protected void Signal(bool question, bool chime = true)
    {
        var away = !ContainsFocus;
        if (ConsoleSettings.BackgroundAlert && away)
        {
            Sounds.Alert();
        }
        else if (ConsoleSettings.SoundOnBell && chime)
        {
            if (question)
            {
                Sounds.Question();
            }
            else
            {
                Sounds.Done();
            }
        }

        if (ConsoleSettings.FlashTaskbar && away && IsHandleCreated)
        {
            var info = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = Handle,
                dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                uCount = 0,
                dwTimeout = 0,
            };
            FlashWindowEx(ref info);
        }
    }

    /// <summary>
    /// The working music plays exactly while <see cref="Working"/> says so: it starts with the first busy frame and
    /// stops with the frame in which the program is done, asks, or was stopped, and at once when the option is
    /// turned off.
    /// </summary>
    protected void UpdateMusic()
    {
        var play = ConsoleSettings.WorkingMusic && _host is not null && Working;
        if (play == _musicOn)
        {
            return;
        }

        _musicOn = play;
        if (play)
        {
            Sounds.StartMusic();
        }
        else
        {
            Sounds.StopMusic();
        }
    }

    /// <summary>
    /// Ctrl+Space anywhere in the window: the music option flips through its menu item (<see cref="CreateMusicItem"/>),
    /// which applies and saves it, and the result is said; when nothing is running at that moment nothing is heard
    /// yet, which is said too.
    /// </summary>
    protected void ToggleMusic()
    {
        if (_musicItem is null)
        {
            return;
        }

        _musicItem.Checked = !_musicItem.Checked;
        Announce(!ConsoleSettings.WorkingMusic ? "Background music off" : _musicOn ? "Background music on" : "Background music on, nothing is running", true);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);
}
