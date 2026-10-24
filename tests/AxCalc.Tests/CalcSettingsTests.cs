using AxCalc.Core;

namespace AxCalc.Tests;

/// <summary>axCalc's settings file (docs/axcalc/SPEC.md AC-9.1): the defaults, the history and the variables kept or not.</summary>
public sealed class CalcSettingsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axcalc-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void A_missing_file_gives_the_defaults()
    {
        var settings = CalcSettings.Load(Path.Combine(_folder, "axcalc.json"), out var error);
        Assert.Null(error);
        Assert.True(settings.Degrees);
        Assert.True(settings.KeepHistory);
        Assert.Empty(settings.History);
        Assert.Empty(settings.Variables);
    }

    [Fact]
    public void The_history_and_the_variables_go_round_the_file()
    {
        var path = Path.Combine(_folder, "axcalc.json");
        var settings = new CalcSettings { Degrees = false };
        settings.Keep([new CalcEntry("2+3", "5", 5), new CalcEntry("rate = 1.25", "1.25", 1.25, "rate")], [new("rate", 1.25)]);
        settings.Save(path);

        var loaded = CalcSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.False(loaded.Degrees);
        Assert.Equal(["2+3 = 5", "rate = 1.25"], loaded.History.Select(entry => entry.Label));
        Assert.Equal(1.25, loaded.Variables["rate"]);
    }

    [Fact]
    public void Keep_history_off_keeps_nothing_and_the_list_is_cut_to_its_limit()
    {
        var settings = new CalcSettings { KeepHistory = false };
        settings.Keep([new CalcEntry("1", "1", 1)], [new("x", 1)]);
        Assert.Empty(settings.History);
        Assert.Empty(settings.Variables);

        settings.KeepHistory = true;
        settings.Keep(Enumerable.Range(1, CalcSettings.MaxHistory + 20).Select(i => new CalcEntry(i.ToString(), i.ToString(), i)), []);
        Assert.Equal(CalcSettings.MaxHistory, settings.History.Count);
        Assert.Equal("21", settings.History[0].Expression);
    }

    [Fact]
    public void Nonsense_in_the_file_is_dropped_on_loading()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "axcalc.json");
        File.WriteAllText(path, """
            {
              "history": [ { "expression": "1+1", "answer": "2", "value": 2 }, { "expression": null, "answer": "3", "value": 3 } ],
              "variables": { "rate": 2, "pi": 3, "2x": 1 }
            }
            """);
        var settings = CalcSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Single(settings.History);
        Assert.Equal(["rate"], settings.Variables.Keys);
    }
}
