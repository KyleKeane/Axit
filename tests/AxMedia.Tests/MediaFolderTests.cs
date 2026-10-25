using AxMedia.Core;

namespace AxMedia.Tests;

/// <summary>A folder's media files and the queue a start makes (docs/axmedia/SPEC.md AM-1.1, AM-3.2, AM-D2), over a temporary folder.</summary>
public sealed class MediaFolderTests : IDisposable
{
    private readonly TempFolder _folder = new("axmedia-folder");

    public MediaFolderTests()
    {
        Directory.CreateDirectory(Path.Combine(_folder.Path, "sub"));
        foreach (var name in new[] { "10.mp3", "2.mp3", "1.mp3", "cover.jpg", "notes.md", "clip.mp4" })
        {
            File.WriteAllBytes(Path.Combine(_folder.Path, name), []);
        }

        File.WriteAllBytes(Path.Combine(_folder.Path, "sub", "a.flac"), []);
        var hidden = Path.Combine(_folder.Path, "hidden.mp3");
        File.WriteAllBytes(hidden, []);
        File.SetAttributes(hidden, FileAttributes.Hidden);
    }

    public void Dispose() => _folder.Dispose();

    private string In(string name) => Path.Combine(_folder.Path, name);

    [Fact]
    public void The_folders_media_files_come_in_natural_order_without_hidden_ones()
    {
        Assert.Equal([In("1.mp3"), In("2.mp3"), In("10.mp3"), In("clip.mp4")], MediaFolder.Files(_folder.Path));
    }

    [Fact]
    public void One_file_plays_with_its_folders_media_files_around_it()
    {
        var (paths, start) = MediaFolder.Queue([In("10.mp3")]);
        Assert.Equal([In("1.mp3"), In("2.mp3"), In("10.mp3"), In("clip.mp4")], paths);
        Assert.Equal(2, start);

        // A file that is not among its folder's media files (hidden, or not media) is queued alone.
        var (alone, first) = MediaFolder.Queue([In("notes.md")]);
        Assert.Equal([In("notes.md")], alone);
        Assert.Equal(0, first);
    }

    [Fact]
    public void A_folder_plays_from_the_first_and_several_paths_are_expanded_in_order()
    {
        var (paths, start) = MediaFolder.Queue([_folder.Path]);
        Assert.Equal(4, paths.Count);
        Assert.Equal(0, start);

        var (several, _) = MediaFolder.Queue([In("sub"), In("2.mp3"), In("gone.mp3")]);
        Assert.Equal([Path.Combine(_folder.Path, "sub", "a.flac"), In("2.mp3")], several);

        var (none, index) = MediaFolder.Queue([In("gone.mp3")]);
        Assert.Empty(none);
        Assert.Equal(-1, index);
    }

    [Fact]
    public void An_add_expands_folders_and_keeps_files()
    {
        Assert.Equal([In("2.mp3"), Path.Combine(_folder.Path, "sub", "a.flac")], MediaFolder.Expand([In("2.mp3"), In("sub")]));
        Assert.Empty(MediaFolder.Expand([In("gone")]));
    }
}
