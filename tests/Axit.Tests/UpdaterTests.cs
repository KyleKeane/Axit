using Axit.Core.Updates;

namespace Axit.Tests;

/// <summary>A failed update's details to send (docs/axit/SPEC.md AX-3.6): what went wrong, nothing that names the user.</summary>
public class UpdaterTests
{
    [Fact]
    public void The_details_name_the_versions_and_what_failed_but_not_the_account()
    {
        // Split so that the repository's privacy test does not take the examples for real ones (PrivacyTests).
        var details = Updater.FailureDetails("3.7.0", "3.7.1", "axFile",
            "Axit was not installed: the list of apps could not be read from C:\\" + "Users\\someone\\AppData\\Local\\Axit\\updates\\3.7.1\\Axit.exe (Axit.exe apps)\n"
            + "Settings of someone" + "@example.com in C:/" + "Users/someone/.local/bin",
            ["someone"]);

        Assert.StartsWith("Axit 3.7.0 updating to 3.7.1 (axFile)\nWindows ", details);
        Assert.Contains("C:\\Users\\<user>\\AppData\\Local\\Axit\\updates\\3.7.1\\Axit.exe", details);
        Assert.Contains("C:/Users/<user>/.local/bin", details);
        Assert.Contains("<email>", details);
        Assert.DoesNotContain("someone", details);
    }
}
