using AxClaude.Core.Pty;

namespace AxClaude.Tests;

/// <summary>The recorder writes the <c>.vt</c> file and the chunk index in the format the fixtures under tests/fixtures use.</summary>
public class StreamRecorderTests
{
    [Fact]
    public void Each_write_goes_to_the_vt_file_and_is_listed_in_the_index_with_its_offset_length_and_time()
    {
        var directory = Directory.CreateTempSubdirectory("axclaude-recorder-");
        try
        {
            var path = Path.Combine(directory.FullName, "probe.vt");
            using (var recorder = new StreamRecorder(path))
            {
                recorder.Write("abc"u8);
                recorder.Write([]);
                recorder.Write("defgh"u8);
                Assert.Equal(8, recorder.Length);
            }

            Assert.Equal("abcdefgh"u8.ToArray(), File.ReadAllBytes(path));

            // The index has no byte order mark, a header row and one "offset length elapsedMs" row per write that carried bytes.
            var index = File.ReadAllBytes(path + ".chunks.txt");
            Assert.Equal((byte)'#', index[0]);
            var rows = File.ReadAllLines(path + ".chunks.txt");
            Assert.Equal(3, rows.Length);
            Assert.Equal("# offset length elapsedMs", rows[0]);
            Assert.Matches(@"^0 3 \d+$", rows[1]);
            Assert.Matches(@"^3 5 \d+$", rows[2]);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
