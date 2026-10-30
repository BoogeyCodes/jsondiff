using JsonDiff.Core;

namespace JsonDiff.Tests;

public sealed class JsonComparerTests
{
    private static IReadOnlyList<Change> Diff(string left, string right, bool ignoreArrayOrder = false) =>
        JsonComparer.Compare(left, right, new JsonDiffOptions { IgnoreArrayOrder = ignoreArrayOrder });

    [Fact]
    public void Identical_documents_have_no_changes()
    {
        var changes = Diff("""{"a":1,"b":[1,2,3],"c":{"d":true}}""", """{"a":1,"b":[1,2,3],"c":{"d":true}}""");
        Assert.Empty(changes);
    }

    [Fact]
    public void Detects_a_scalar_change_with_path_and_values()
    {
        var change = Assert.Single(Diff("""{"name":"ann"}""", """{"name":"bob"}"""));
        Assert.Equal(ChangeKind.Changed, change.Kind);
        Assert.Equal("/name", change.Path);
        Assert.Equal("ann", change.OldValue);
        Assert.Equal("bob", change.NewValue);
    }

    [Fact]
    public void Detects_added_and_removed_keys()
    {
        var changes = Diff("""{"keep":1,"gone":2}""", """{"keep":1,"fresh":3}""");

        Assert.Contains(changes, c => c.Kind == ChangeKind.Removed && c.Path == "/gone" && c.OldValue == "2");
        Assert.Contains(changes, c => c.Kind == ChangeKind.Added && c.Path == "/fresh" && c.NewValue == "3");
        Assert.Equal(2, changes.Count);
    }

    [Fact]
    public void A_type_change_is_reported_once_not_recursed_into()
    {
        var change = Assert.Single(Diff("""{"v":{"nested":1}}""", """{"v":5}"""));
        Assert.Equal(ChangeKind.Changed, change.Kind);
        Assert.Equal("/v", change.Path);
    }

    [Fact]
    public void Recurses_into_nested_objects()
    {
        var change = Assert.Single(Diff("""{"user":{"role":"viewer"}}""", """{"user":{"role":"admin"}}"""));
        Assert.Equal("/user/role", change.Path);
    }

    [Fact]
    public void Arrays_are_compared_by_index()
    {
        var changes = Diff("""[1,2,3]""", """[1,9,3,4]""");

        Assert.Contains(changes, c => c.Kind == ChangeKind.Changed && c.Path == "/1" && c.OldValue == "2" && c.NewValue == "9");
        Assert.Contains(changes, c => c.Kind == ChangeKind.Added && c.Path == "/3" && c.NewValue == "4");
        Assert.Equal(2, changes.Count);
    }

    [Fact]
    public void Shorter_array_on_the_right_reports_removals()
    {
        var change = Assert.Single(Diff("""[1,2]""", """[1]"""));
        Assert.Equal(ChangeKind.Removed, change.Kind);
        Assert.Equal("/1", change.Path);
    }

    [Fact]
    public void Numbers_are_compared_by_value_not_text()
    {
        Assert.Empty(Diff("""{"n":1}""", """{"n":1.0}"""));
        Assert.Empty(Diff("""{"n":100}""", """{"n":1e2}"""));
        Assert.Single(Diff("""{"n":1}""", """{"n":2}"""));
    }

    [Fact]
    public void Ignore_array_order_treats_arrays_as_multisets()
    {
        Assert.Empty(Diff("""[1,2,3]""", """[3,2,1]""", ignoreArrayOrder: true));
        // But by position, reordering is three changes.
        Assert.NotEmpty(Diff("""[1,2,3]""", """[3,2,1]"""));
    }

    [Fact]
    public void Ignore_array_order_still_reports_genuine_differences()
    {
        var changes = Diff("""[1,2,2]""", """[2,1,3]""", ignoreArrayOrder: true);
        Assert.Contains(changes, c => c.Kind == ChangeKind.Removed && c.OldValue == "2");
        Assert.Contains(changes, c => c.Kind == ChangeKind.Added && c.NewValue == "3");
        Assert.Equal(2, changes.Count);
    }

    [Fact]
    public void Keys_with_slash_or_tilde_are_json_pointer_escaped()
    {
        var change = Assert.Single(Diff("""{"a/b":1}""", """{"a/b":2}"""));
        Assert.Equal("/a~1b", change.Path);

        var tilde = Assert.Single(Diff("""{"a~b":1}""", """{"a~b":2}"""));
        Assert.Equal("/a~0b", tilde.Path);
    }

    [Fact]
    public void Root_scalar_change_uses_root_path()
    {
        var change = Assert.Single(Diff("1", "2"));
        Assert.Equal("/", change.Path);
    }

    [Fact]
    public void Object_and_array_values_render_canonically_and_order_independently()
    {
        // Same object, keys in a different order: equal.
        Assert.Empty(Diff("""{"o":{"x":1,"y":2}}""", """{"o":{"y":2,"x":1}}"""));
    }
}
