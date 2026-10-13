namespace Axit.Core.Audio;

/// <summary>
/// The app's sounds as WAV data (16-bit mono PCM), generated in code so that no media file ships: the tick for a
/// new line, the chimes for a sent message and for Claude replying, finishing and asking, and the music loop that
/// plays while Claude works (AxClaude FR-7.3, FR-7.8, FR-7.9).
/// </summary>
public static class WaveTone
{
    public const int SampleRate = 22050;

    /// <summary>The bytes of the WAV header before the samples.</summary>
    public const int HeaderLength = 44;

    /// <summary>
    /// A sequence of sine notes, each faded in and out over a few milliseconds so that the edges do not click.
    /// A frequency of 0 is a pause of that length.
    /// </summary>
    public static byte[] Notes(double amplitude, params (double Hertz, int Milliseconds)[] notes)
    {
        var total = 0;
        foreach (var note in notes)
        {
            total += Samples(note.Milliseconds);
        }

        var samples = new short[total];
        var at = 0;
        foreach (var (hertz, milliseconds) in notes)
        {
            var count = Samples(milliseconds);
            var fade = Math.Min(count / 2, Samples(4));
            for (var i = 0; i < count; i++)
            {
                var envelope = 1.0;
                if (i < fade)
                {
                    envelope = (double)i / fade;
                }
                else if (i >= count - fade)
                {
                    envelope = (double)(count - 1 - i) / fade;
                }

                samples[at + i] = Sample(hertz, i, amplitude * envelope);
            }

            at += count;
        }

        return Wav(samples);
    }

    /// <summary>
    /// Strikes, like a typewriter key or a tiny hammer: each is a burst of noise over a short tone that dies away at
    /// once, the noise gone within a couple of milliseconds and the tone within <paramref name="millisecondsEach"/>,
    /// the next strike following as soon as the last has faded. One strike is heard as a single tick; two or three in
    /// a row, a few milliseconds apart, as one ratchety tick. The tone rises over an eighth of the strike and the
    /// noise over four samples: a sound that starts at full volume is heard as a click followed by a separate falling
    /// note. The noise is the same every time.
    /// </summary>
    public static byte[] Strikes(double amplitude, int millisecondsEach, double noise, params double[] hertz)
    {
        var each = Samples(millisecondsEach);
        var attack = Math.Max(1, each / 8);
        var chiff = Math.Max(1, Samples(1));
        var random = new Random(7);
        var samples = new short[each * hertz.Length];
        for (var n = 0; n < hertz.Length; n++)
        {
            for (var i = 0; i < each; i++)
            {
                // Down to about a quarter of a percent at the end, so the cut-off is not heard.
                var envelope = Math.Exp(-6.0 * i / each);
                if (i < attack)
                {
                    envelope *= (double)i / attack;
                }

                var burst = noise * Math.Exp(-(double)i / chiff) * Math.Min(1.0, i / 4.0);
                var value = Math.Sin(2 * Math.PI * hertz[n] * i / SampleRate) * envelope + (random.NextDouble() * 2 - 1) * burst;
                samples[n * each + i] = (short)(Math.Clamp(value, -1.0, 1.0) * amplitude * short.MaxValue);
            }
        }

        return Wav(samples);
    }

    /// <summary>
    /// Soft plucked notes, one every <paramref name="millisecondsEach"/>: each is a tone with a little of its first
    /// two overtones, which die faster than the fundamental as a struck string's do, swelling over 20 ms and ringing
    /// into the next note's time, down to about two percent where the one after that starts. The tail of the last
    /// note wraps round to the start, so the sequence is exactly periodic and loops without a seam, which is what a
    /// sound that plays for as long as Claude works needs (AxClaude FR-7.9). The whole is scaled so that its loudest
    /// sample is <paramref name="amplitude"/>.
    /// </summary>
    public static byte[] Plucks(double amplitude, int millisecondsEach, params double[] hertz)
    {
        var each = Samples(millisecondsEach);
        var ring = 2 * each;
        var attack = Math.Max(1, Samples(20));
        var total = each * hertz.Length;
        var mix = new double[total];
        for (var n = 0; n < hertz.Length; n++)
        {
            for (var i = 0; i < ring; i++)
            {
                var decay = Math.Exp(-4.0 * i / ring);
                var envelope = i < attack ? decay * i / attack : decay;
                var phase = 2 * Math.PI * hertz[n] * i / SampleRate;
                var tone = Math.Sin(phase) + 0.3 * Math.Sin(2 * phase) * decay + 0.1 * Math.Sin(3 * phase) * decay * decay;
                mix[(n * each + i) % total] += tone * envelope;
            }
        }

        var peak = 0.0;
        foreach (var value in mix)
        {
            peak = Math.Max(peak, Math.Abs(value));
        }

        var scale = peak > 0 ? amplitude / peak : 0;
        var samples = new short[total];
        for (var i = 0; i < total; i++)
        {
            samples[i] = (short)(mix[i] * scale * short.MaxValue);
        }

        return Wav(samples);
    }

    private static int Samples(int milliseconds) => milliseconds * SampleRate / 1000;

    private static short Sample(double hertz, int index, double amplitude) =>
        (short)(Math.Sin(2 * Math.PI * hertz * index / SampleRate) * amplitude * short.MaxValue);

    private static byte[] Wav(short[] samples)
    {
        var dataLength = samples.Length * 2;
        var wav = new byte[HeaderLength + dataLength];
        var span = wav.AsSpan();
        "RIFF"u8.CopyTo(span);
        BitConverter.TryWriteBytes(span[4..], 36 + dataLength);
        "WAVE"u8.CopyTo(span[8..]);
        "fmt "u8.CopyTo(span[12..]);
        BitConverter.TryWriteBytes(span[16..], 16);                 // fmt chunk length
        BitConverter.TryWriteBytes(span[20..], (short)1);           // PCM
        BitConverter.TryWriteBytes(span[22..], (short)1);           // mono
        BitConverter.TryWriteBytes(span[24..], SampleRate);
        BitConverter.TryWriteBytes(span[28..], SampleRate * 2);     // bytes per second
        BitConverter.TryWriteBytes(span[32..], (short)2);           // block align
        BitConverter.TryWriteBytes(span[34..], (short)16);          // bits per sample
        "data"u8.CopyTo(span[36..]);
        BitConverter.TryWriteBytes(span[40..], dataLength);
        for (var i = 0; i < samples.Length; i++)
        {
            BitConverter.TryWriteBytes(span[(HeaderLength + i * 2)..], samples[i]);
        }

        return wav;
    }
}
