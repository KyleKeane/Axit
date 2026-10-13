using System.Text;
using Axit.Core.Audio;

namespace Axit.Tests;

public class WaveToneTests
{
    [Fact]
    public void Notes_produce_a_valid_pcm_wav_of_the_requested_length()
    {
        // Lengths that are whole sample counts at 22050 Hz (40 ms is 882 samples; 50 ms would not be whole).
        var wav = WaveTone.Notes(0.5, (440, 100), (0, 40), (880, 60));

        var samples = 200 * WaveTone.SampleRate / 1000;
        Assert.Equal(WaveTone.HeaderLength + samples * 2, wav.Length);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal(wav.Length - 8, BitConverter.ToInt32(wav, 4));
        Assert.Equal("WAVEfmt ", Encoding.ASCII.GetString(wav, 8, 8));
        Assert.Equal(16, BitConverter.ToInt32(wav, 16));
        Assert.Equal(1, BitConverter.ToInt16(wav, 20));
        Assert.Equal(1, BitConverter.ToInt16(wav, 22));
        Assert.Equal(WaveTone.SampleRate, BitConverter.ToInt32(wav, 24));
        Assert.Equal(WaveTone.SampleRate * 2, BitConverter.ToInt32(wav, 28));
        Assert.Equal(2, BitConverter.ToInt16(wav, 32));
        Assert.Equal(16, BitConverter.ToInt16(wav, 34));
        Assert.Equal("data", Encoding.ASCII.GetString(wav, 36, 4));
        Assert.Equal(samples * 2, BitConverter.ToInt32(wav, 40));
    }

    [Fact]
    public void Notes_stay_within_the_amplitude_and_a_pause_is_silent()
    {
        var wav = WaveTone.Notes(0.5, (440, 100), (0, 40));
        var limit = (int)(0.5 * short.MaxValue) + 1;
        var loudest = 0;
        for (var i = WaveTone.HeaderLength; i < wav.Length; i += 2)
        {
            loudest = Math.Max(loudest, Math.Abs((int)BitConverter.ToInt16(wav, i)));
        }

        Assert.InRange(loudest, limit / 2, limit);

        var pauseStart = WaveTone.HeaderLength + 100 * WaveTone.SampleRate / 1000 * 2;
        for (var i = pauseStart; i < wav.Length; i += 2)
        {
            Assert.Equal(0, BitConverter.ToInt16(wav, i));
        }
    }

    [Fact]
    public void Strikes_die_away_one_after_the_other()
    {
        var wav = WaveTone.Strikes(0.3, 6, 0.5, 3000, 2000);
        var samples = (wav.Length - WaveTone.HeaderLength) / 2;
        var each = 6 * WaveTone.SampleRate / 1000;
        Assert.Equal(2 * each, samples);

        int Peak(int from, int to)
        {
            var peak = 0;
            for (var i = from; i < to; i++)
            {
                peak = Math.Max(peak, Math.Abs((int)BitConverter.ToInt16(wav, WaveTone.HeaderLength + i * 2)));
            }

            return peak;
        }

        // Each strike is loud at its start and all but gone at its end; the second starts afresh once the first has faded.
        Assert.True(Peak(0, each / 4) > Peak(each * 3 / 4, each) * 4, "the first strike should die away");
        Assert.True(Peak(each, each + each / 4) > Peak(each * 3 / 4, each) * 4, "the second strike should start afresh");
        Assert.True(Peak(each, each + each / 4) > Peak(2 * each - each / 4, 2 * each) * 4, "the second strike should die away");
        Assert.InRange(Peak(0, samples), 0.1 * short.MaxValue, 0.3 * short.MaxValue + 1);

        // The attack: the second sample is still far below the peak (a sound that starts at full volume clicks), and the first is silent.
        Assert.Equal(0, BitConverter.ToInt16(wav, WaveTone.HeaderLength));
        var second = Math.Abs((int)BitConverter.ToInt16(wav, WaveTone.HeaderLength + 2));
        Assert.True(second < Peak(0, each / 4) / 4, $"{second} should be well below the peak");
    }

    [Fact]
    public void Strikes_leave_a_rest_silent()
    {
        var wav = WaveTone.Strikes(0.3, 6, 0, 3000, 0);
        var each = 6 * WaveTone.SampleRate / 1000;
        Assert.Equal(WaveTone.HeaderLength + 2 * each * 2, wav.Length);
        var loudest = 0;
        for (var i = 0; i < each; i++)
        {
            loudest = Math.Max(loudest, Math.Abs((int)BitConverter.ToInt16(wav, WaveTone.HeaderLength + i * 2)));
        }

        Assert.True(loudest > 0.1 * short.MaxValue, "the strike should sound");
        for (var i = each; i < 2 * each; i++)
        {
            Assert.Equal(0, BitConverter.ToInt16(wav, WaveTone.HeaderLength + i * 2));
        }
    }

    [Fact]
    public void Plucks_ring_into_the_next_note_and_loop_without_a_seam()
    {
        var wav = WaveTone.Plucks(0.2, 20, 440, 330, 220);
        var samples = (wav.Length - WaveTone.HeaderLength) / 2;
        var each = 20 * WaveTone.SampleRate / 1000;
        Assert.Equal(3 * each, samples);

        int At(int i) => BitConverter.ToInt16(wav, WaveTone.HeaderLength + i * 2);

        int Peak(int from, int to)
        {
            var peak = 0;
            for (var i = from; i < to; i++)
            {
                peak = Math.Max(peak, Math.Abs(At(i)));
            }

            return peak;
        }

        // The loop is scaled as a whole: its loudest sample is the amplitude.
        var peak = Peak(0, samples);
        Assert.InRange(peak, 0.2 * short.MaxValue - 2, 0.2 * short.MaxValue + 1);

        // Each note starts well over the tail of the one before, which has died away under it.
        for (var n = 1; n < 3; n++)
        {
            Assert.True(Peak(n * each, n * each + each / 4) > 2 * Peak(n * each - each / 4, n * each), $"note {n} should start over the last one's tail");
        }

        // The last note's tail wraps round to the start, so the first sample follows on from the last: no seam when it loops.
        Assert.True(Math.Abs(At(0) - At(samples - 1)) < peak / 10, "the loop should join up");
    }
}
