using AxMedia.Core;

namespace AxMedia.Tests;

/// <summary>The player's reason texts (docs/axmedia/SPEC.md AM-4.1); the engine itself is checked by ear.</summary>
public sealed class MediaPlayerTests
{
    [Fact]
    public void Reasons_name_the_known_codes()
    {
        Assert.Equal("this kind of file is not supported", MediaPlayer.Reason(unchecked((int)0xC00D36C4)));
        Assert.Equal("error 0xC00D0000", MediaPlayer.Reason(unchecked((int)0xC00D0000)));
        Assert.Contains("cannot find the file", MediaPlayer.Reason(unchecked((int)0x80070002)));
    }
}
