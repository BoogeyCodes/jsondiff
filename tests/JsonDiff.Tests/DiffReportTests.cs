using System.Text.Json;
using JsonDiff.Core;

namespace JsonDiff.Tests;

public sealed class DiffReportTests
{
    private static readonly IReadOnlyList<Change> Sample =
    [
        new Change(ChangeKind.Changed, "/name", "ann", "bob"),
        new Change(ChangeKind.Added, "/age", null, "30"),
        new Change(ChangeKind.Removed, "/legacy", "true", null),
    ];

    [Fact]
    public void Text_report_shows_each_change_and_a_summary()
    {
        var text = DiffReport.ToText(Sample);

        Assert.Contains("+ /age: 30", text);
        Assert.Contains("- /legacy: true", text);
        Assert.Contains("~ /name: ann -> bob", text);
        Assert.Contains("3 difference(s): 1 added, 1 removed, 1 changed.", text);
    }

    [Fact]
    public void Text_report_says_no_differences_when_empty()
    {
        Assert.Equal("No differences.", DiffReport.ToText([]));
    }

    [Fact]
    public void Summarize_counts_each_kind()
    {
        var stats = DiffReport.Summarize(Sample);
        Assert.Equal(1, stats.Added);
        Assert.Equal(1, stats.Removed);
        Assert.Equal(1, stats.Changed);
        Assert.Equal(3, stats.Total);
        Assert.Equal("3 difference(s): 1 added, 1 removed, 1 changed.", stats.ToString());
        Assert.Equal("No differences.", DiffReport.Summarize([]).ToString());
    }

    [Fact]
    public void Json_report_is_valid_and_carries_the_fields()
    {
        using var doc = JsonDocument.Parse(DiffReport.ToJson(Sample));
        var items = doc.RootElement.EnumerateArray().ToArray();

        Assert.Equal(3, items.Length);
        Assert.Contains(items, e => e.GetProperty("kind").GetString() == "added" && e.GetProperty("path").GetString() == "/age");
        Assert.Contains(items, e => e.GetProperty("kind").GetString() == "changed" && e.GetProperty("newValue").GetString() == "bob");
    }

    [Fact]
    public void Json_report_of_no_changes_is_an_empty_array()
    {
        Assert.Equal("[]", DiffReport.ToJson([]));
    }
}
