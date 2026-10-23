using AxTerminal.Core;

namespace AxTerminal.Tests;

public sealed class TerminalSettingsTests
{
    [Fact]
    public void Missing_file_gives_the_defaults()
    {
        var path = Path.Combine(Path.GetTempPath(), "axterminal-tests-" + Guid.NewGuid().ToString("N"), "axterminal.json");
        var settings = TerminalSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Null(settings.Shell);
        Assert.Equal(ShellLauncher.DefaultKind(), settings.ShellKind);
        Assert.Equal(240, settings.PtyColumns);
        Assert.Equal(50, settings.PtyRows);
        Assert.True(settings.SoundOnBell);
        Assert.True(settings.ClickOnNewLine);
        Assert.False(settings.WorkingMusic);
        Assert.True(settings.AnnounceQuestions);
        Assert.Equal(OutputSpeechMode.All, settings.OutputSpeech);
        Assert.Equal(20000, settings.MaxTranscriptLines);
        Assert.True(settings.CheckForUpdates);
    }

    [Fact]
    public void Nonsense_falls_back_and_values_round_trip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "axterminal-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "axterminal.json");
        try
        {
            File.WriteAllText(path, """{"shell":"fish","ptyColumns":3,"ptyRows":9000,"maxTranscriptLines":5,"recentFolders":["", "C:\\src"],"outputSpeech":7}""");
            var settings = TerminalSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.Null(settings.Shell);
            Assert.Equal(240, settings.PtyColumns);
            Assert.Equal(50, settings.PtyRows);
            Assert.Equal(20000, settings.MaxTranscriptLines);
            Assert.Equal(["C:\\src"], settings.RecentFolders);
            Assert.Equal(OutputSpeechMode.All, settings.OutputSpeech);

            settings.Shell = "cmd";
            settings.OutputSpeech = OutputSpeechMode.All;
            settings.RememberFolder("C:\\work");
            settings.RememberFolder("C:\\src");
            settings.Save(path);
            var again = TerminalSettings.Load(path, out error);
            Assert.Null(error);
            Assert.Equal(ShellKind.CommandPrompt, again.ShellKind);
            Assert.Equal(OutputSpeechMode.All, again.OutputSpeech);
            Assert.Equal(["C:\\src", "C:\\work"], again.RecentFolders);
            Assert.Equal("C:\\src", again.LastFolder);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
