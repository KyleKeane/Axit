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
        Assert.Equal("no decoder for this file is installed", MediaPlayer.Reason(unchecked((int)0xC00D5212)));
        Assert.Equal("the request is not valid now", MediaPlayer.Reason(unchecked((int)0xC00D36B2)));
        Assert.Equal("the player was shut down", MediaPlayer.Reason(unchecked((int)0xC00D3E85)));
        Assert.Equal("the speed is not supported", MediaPlayer.Reason(unchecked((int)0xC00D36E6)));
    }
}
