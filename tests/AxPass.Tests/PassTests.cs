using System.Text.Json.Nodes;
using AxPass.Core;
using Axit.Core;

namespace AxPass.Tests;

/// <summary>The tool's output read into groups, entries and fields, as KeePassXC 2.8.0 printed it on a test database (docs/axpass/kdbx-notes.md section 8).</summary>
public class PassTests
{
    private const string Listing = "Top entry\r\nCafé, ünïcode\r\nEmail/\r\nEmail/Gmail\r\nEmail/Work/\r\nEmail/Work/[empty]\r\nRecycle Bin/\r\nRecycle Bin/Old\r\n";

    private const string Show = "Title: Gmail\r\nUserName: sam\r\nPassword: PROTECTED\r\nURL: https://mail.example.com\r\nNotes: line one\r\nline two: with a colon\r\nUuid: {6095b140-e977-4884-bcea-35d49c7adfaa}\r\nTags: \r\n";

    [Fact]
    public void ReadsTheGroupsAndEntries()
    {
        var tree = PassTree.Parse(Listing);
        Assert.Equal(["Email", "Recycle Bin", "Top entry", "Café, ünïcode"], tree.Level(string.Empty).Select(node => node.Name));
        Assert.Equal(["Work", "Gmail"], tree.Level("Email").Select(node => node.Name));
        Assert.Empty(tree.Level("Email/Work"));
        Assert.True(tree.HasGroup("Email/Work"));
        Assert.True(tree.HasEntry("Email/Gmail"));
        Assert.Equal(2, tree.CountIn("Email"));
        var gmail = tree.Nodes.Single(node => node.Name == "Gmail");
        Assert.Equal("Email", gmail.Group);
        Assert.False(gmail.IsGroup);
    }

    [Fact]
    public void TheRecycleBinComesLastAtTheTop()
    {
        var tree = PassTree.Parse("Recycle Bin/\r\nRecycle Bin/[empty]\r\nBanking/\r\nBanking/[empty]\r\n");
        Assert.Equal(["Banking", "Recycle Bin"], tree.Level(string.Empty).Select(node => node.Name));
        Assert.True(PassTree.InRecycleBin("Recycle Bin/Old"));
        Assert.False(PassTree.InRecycleBin("Recycle Bins/Old"));
    }

    [Fact]
    public void ReadsAnEntryWithoutItsPassword()
    {
        var entry = EntryFields.Parse(Show);
        Assert.Equal(["Title", "UserName", "Password", "URL", "Notes"], entry.Fields.Select(field => field.Title));
        Assert.Equal("Gmail", entry.Title);
        Assert.Equal(FieldTypes.UserName, entry.Fields[1].Type);
        Assert.Equal(FieldTypes.Password, entry.Fields[2].Type);
        Assert.Equal(string.Empty, FieldTypes.TextOf(entry.Fields[2].Value));
        Assert.Equal("hidden", FieldTypes.Spoken(entry.Fields[2]));
        Assert.Equal("line one\nline two: with a colon", FieldTypes.TextOf(entry.Fields[4].Value));
    }

    [Fact]
    public void AChangeBecomesTheToolsOptions()
    {
        var before = EntryFields.Parse(Show);
        var after = before.WithValue(1, JsonValue.Create("sam2")).WithValue(3, JsonValue.Create(string.Empty));
        Assert.Equal(["-u", "sam2", "--url", ""], EntryFields.EditOptions(before, after));
        Assert.Empty(EntryFields.EditOptions(before, before));
        Assert.Equal(["-t", "Gmail personal"], EntryFields.EditOptions(before, before.WithTitle("Gmail personal")));
    }

    [Theory]
    [InlineData("Gmail", null)]
    [InlineData("a/b", "A name cannot hold a slash: KeePassXC's tool reads it as a group")]
    [InlineData("  ", "Type a name")]
    public void ASlashIsRefused(string name, string? why) => Assert.Equal(why, PassTree.RefusedName(name));

    [Fact]
    public void TheArgumentsPutTheDatabaseAfterTheOptions()
    {
        Assert.Equal(["edit", "-q", "-u", "x", "db.kdbx", "Email/Gmail"], KeePassCli.On("edit", "db.kdbx", ["-u", "x"], "Email/Gmail"));
        Assert.Equal("Email/Gmail", PassTree.Join("Email", "Gmail"));
        Assert.Equal("Gmail", PassTree.Join(string.Empty, "Gmail"));
    }

    /// <summary>The tool itself on a throwaway database, when KeePassXC is installed (not on the build servers, where it passes without running).</summary>
    [Fact]
    public void TheToolTakesThePasswordOnAPipe()
    {
        if (KeePassCli.Find(null) is not { } cli)
        {
            return;
        }

        var folder = Path.Combine(Path.GetTempPath(), "axpass-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var db = Path.Combine(folder, "test.kdbx");
            const string pw = "test-master-1";
            Assert.True(KeePassCli.Run(cli, KeePassCli.On("db-create", db, ["-p"]), [pw, pw]).Succeeded);
            Assert.True(KeePassCli.Run(cli, KeePassCli.On("mkdir", db, null, "Email"), [pw]).Succeeded);
            Assert.True(KeePassCli.Run(cli, KeePassCli.On("add", db, ["-u", "sam", "-p"], "Email/Gmail"), [pw, "entry-1", "entry-1"]).Succeeded);
            var tree = PassTree.Parse(KeePassCli.Run(cli, KeePassCli.On("ls", db, ["-R", "-f"]), [pw]).Output);
            Assert.True(tree.HasEntry("Email/Gmail"));
            var entry = EntryFields.Parse(KeePassCli.Run(cli, KeePassCli.On("show", db, null, "Email/Gmail"), [pw]).Output);
            Assert.Equal("sam", EntryFields.ValueOf(entry, EntryFields.UserName));
            Assert.False(KeePassCli.Run(cli, KeePassCli.On("ls", db), ["wrong"]).Succeeded);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
