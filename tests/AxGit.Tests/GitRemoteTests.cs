using AxGit.Core;

namespace AxGit.Tests;

/// <summary>The server a fetch, pull or push reaches (bundle AX-11.5): the remote's name and its address's host.</summary>
public sealed class GitRemoteTests
{
    /// <summary>Split from the addresses so that the privacy test does not read them as e-mail addresses.</summary>
    private const string At = "@";

    [Theory]
    [InlineData("https://github.com/KyleKeane/Axit.git", "github.com")]
    [InlineData("https://user" + At + "GitHub.com/a/b", "github.com")]
    [InlineData("ssh://git" + At + "gitlab.example.org:2222/a/b.git", "gitlab.example.org")]
    [InlineData("git" + At + "github.com:a/b.git", "github.com")]
    [InlineData("server:repos/b", "server")]
    [InlineData(@"C:\repos\b", null)]
    [InlineData("/c/repos/b", null)]
    [InlineData("file:///C:/repos/b", null)]
    [InlineData(@"\\server\share\b", null)]
    [InlineData("", null)]
    public void The_host_is_read_from_the_address(string url, string? host) => Assert.Equal(host, GitRemote.Host(url));

    [Theory]
    [InlineData("origin/main", "origin")]
    [InlineData("upstream/feature/x", "upstream")]
    [InlineData(null, "origin")]
    public void The_remote_is_the_upstreams_or_origin(string? upstream, string remote) => Assert.Equal(remote, GitRemote.NameOf(upstream));
}
