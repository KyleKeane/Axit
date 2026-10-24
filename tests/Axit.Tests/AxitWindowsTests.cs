using Axit.Core;

namespace Axit.Tests;

/// <summary>The window lists of the Axit menu (docs/axit/SPEC.md AX-9): the app from a title, Open windows and each app's windows from the Z order, and the labels.</summary>
public sealed class AxitWindowsTests
{
    /// <summary>In Z order, the most recently used first; handle 3 is the bundle's own message box, no app's window.</summary>
    private static readonly IReadOnlyList<AxitWindow> Windows =
    [
        new(1, "*notes.md - axDown"),
        new(2, "my - project - axClaude"),
        new(3, "Axit"),
        new(4, "axClaude"),
        new(5, "Untitled - axDown"),
    ];

    [Theory]
    [InlineData("notes.md - axDown", "axDown")]
    [InlineData("*notes.md - axDown", "axDown")]
    [InlineData("my - project - axClaude", "axClaude")]
    [InlineData("axClaude", "axClaude")]
    public void The_app_is_what_the_title_ends_with(string title, string app) => Assert.Equal(app, AxitWindows.AppOf(title));

    [Fact]
    public void Open_windows_is_every_other_app_window_in_the_order_given()
    {
        Assert.Equal([2, 4, 5], AxitWindows.Others(Windows, 1).Select(w => (int)w.Handle));
        Assert.Equal([1, 2, 4, 5], AxitWindows.Others(Windows, 0).Select(w => (int)w.Handle));
    }

    [Fact]
    public void An_apps_list_keeps_the_current_window()
    {
        Assert.Equal("axClaude", Dispatch.Of(Dispatch.AxClaude).Name);
        Assert.Equal([2, 4], AxitWindows.Of(Windows, Dispatch.Of(Dispatch.AxClaude)).Select(w => (int)w.Handle));
        Assert.Equal([1, 5], AxitWindows.Of(Windows, Dispatch.Of(Dispatch.AxDown)).Select(w => (int)w.Handle));
    }

    [Fact]
    public void Labels_number_the_items_and_name_the_apps_with_the_letter_after_Ax()
    {
        Assert.Equal("&1. notes.md - axDown", AxitWindows.Label(0, "notes.md - axDown"));
        Assert.Equal("&9. a", AxitWindows.Label(8, "a"));
        Assert.Equal("&a. a", AxitWindows.Label(9, "a"));
        Assert.Equal("&2. Q && A - axDown", AxitWindows.Label(1, "Q & A - axDown"));
        Assert.Equal("ax&Claude", AxitWindows.AppLabel(Dispatch.Of(Dispatch.AxClaude)));
        Assert.Equal("ax&Down", AxitWindows.AppLabel(Dispatch.Of(Dispatch.AxDown)));
    }
}
