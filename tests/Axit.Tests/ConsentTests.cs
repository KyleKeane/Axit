using Axit.Core;

namespace Axit.Tests;

/// <summary>What the user agreed to (docs/axit/SPEC.md AX-11.5, AX-11.8): recorded once per app and program, withdrawn, and asked again when the file is missing or damaged.</summary>
public sealed class ConsentTests
{
    private static string TempFile() => Path.Combine(Path.GetTempPath(), "axit-tests", Guid.NewGuid().ToString("N"), "consent.json");

    [Fact]
    public void An_agreement_is_kept_per_app_and_program_and_can_be_withdrawn()
    {
        var file = TempFile();
        Assert.False(Consent.Given(file, "axTerminal", "PowerShell"));

        Consent.Record(file, "axTerminal", "PowerShell", "3.8.0", new DateTime(2026, 10, 29));
        Assert.True(Consent.Given(file, "axTerminal", "PowerShell"));
        Assert.True(Consent.Given(file, "AXTERMINAL", "powershell"));
        Assert.False(Consent.Given(file, "axTerminal", "Command Prompt"));

        // Agreeing again replaces the first record rather than adding a second.
        Consent.Record(file, "axTerminal", "PowerShell", "3.8.1", new DateTime(2026, 11, 2));
        var only = Assert.Single(Consent.Read(file));
        Assert.Equal("3.8.1", only.Version);

        Consent.Withdraw(file, "axTerminal", "PowerShell");
        Assert.False(Consent.Given(file, "axTerminal", "PowerShell"));
        Directory.Delete(Path.GetDirectoryName(file)!, recursive: true);
    }

    [Fact]
    public void A_damaged_file_means_nothing_was_agreed()
    {
        var file = TempFile();
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "{ not json");
        Assert.False(Consent.Given(file, "axClaude", "Claude Code"));
        Assert.Empty(Consent.Read(file));
        Directory.Delete(Path.GetDirectoryName(file)!, recursive: true);
    }

    /// <summary>AX-11.5: a program installed from Install it now has its own target, so that agreeing there counts for the first connection; each says whose terms and where.</summary>
    [Fact]
    public void Every_program_install_has_its_consent_target()
    {
        foreach (var program in new[] { ProgramInstall.Git, ProgramInstall.KeePassXC, ProgramInstall.PowerShell7, ProgramInstall.ClaudeCode })
        {
            var target = ConsentTarget.For(program.Name);
            Assert.Equal(program.Name, target.Program);
            Assert.StartsWith("https://", target.TermsUrl);
            Assert.NotEmpty(target.Work);
        }

        Assert.Same(ConsentTarget.ClaudeCode, ConsentTarget.For("Claude Code"));
        Assert.Equal("github.com", ConsentTarget.Server("github.com").Program);
        Assert.Empty(ConsentTarget.Shell("Custom shell").TermsUrl);
    }

    [Fact]
    public void Each_agreement_reads_as_one_line()
    {
        Assert.Equal(
            "axClaude, Claude Code, 29 October 2026, Axit 3.8.0",
            Consent.Line(new ConsentRecord("axClaude", "Claude Code", "3.8.0", new DateTime(2026, 10, 29))));
        Assert.Equal(
            "Update to Axit 3.8.0, 29 October 2026",
            Consent.Line(new ConsentRecord(Consent.UpdateApp, "Axit 3.8.0", "3.7.6", new DateTime(2026, 10, 29))));
    }
}
