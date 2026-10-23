using System.Globalization;
using System.Text.Json;

namespace JsonDiff.Core;

public sealed class JsonDiffOptions
{
    /// <summary>
    /// When true, the order of array elements is ignored: arrays are compared as multisets, so
    /// <c>[1, 2]</c> and <c>[2, 1]</c> are equal. When false (the default) arrays are compared by
    /// position, which also reports reordering.
    /// </summary>
    public bool IgnoreArrayOrder { get; init; }
}

/// <summary>
/// Structurally compares two JSON documents and reports the differences as a flat,
/// path-addressed list. Objects are matched by key, arrays by index (or as multisets),
/// and scalars by type and value.
/// </summary>
public static class JsonComparer
{
    public static IReadOnlyList<Change> Compare(string leftJson, string rightJson, JsonDiffOptions? options = null)
    {
        using var left = JsonDocument.Parse(leftJson);
        using var right = JsonDocument.Parse(rightJson);
        var changes = new List<Change>();
        Walk(left.RootElement, right.RootElement, "", options ?? new JsonDiffOptions(), changes);
        return changes;
    }

    private static void Walk(JsonElement left, JsonElement right, string path, JsonDiffOptions options, List<Change> changes)
    {
        var display = path.Length == 0 ? "/" : path;

        if (left.ValueKind != right.ValueKind)
        {
            changes.Add(new Change(ChangeKind.Changed, display, Render(left), Render(right)));
            return;
        }

        switch (left.ValueKind)
        {
            case JsonValueKind.Object:
                WalkObject(left, right, path, options, changes);
                break;
            case JsonValueKind.Array:
                WalkArray(left, right, path, options, changes);
                break;
            default:
                if (!ScalarsEqual(left, right))
                {
                    changes.Add(new Change(ChangeKind.Changed, display, Render(left), Render(right)));
                }
                break;
        }
    }

    private static void WalkObject(JsonElement left, JsonElement right, string path, JsonDiffOptions options, List<Change> changes)
    {
        var rightProps = new Dictionary<string, JsonElement>();
        foreach (var prop in right.EnumerateObject())
        {
            rightProps[prop.Name] = prop.Value;
        }

        // Keys present on the left: changed or removed.
        foreach (var prop in left.EnumerateObject())
        {
            var childPath = $"{path}/{Escape(prop.Name)}";
            if (rightProps.TryGetValue(prop.Name, out var rightValue))
            {
                Walk(prop.Value, rightValue, childPath, options, changes);
                rightProps.Remove(prop.Name);
            }
            else
            {
                changes.Add(new Change(ChangeKind.Removed, childPath, Render(prop.Value), null));
            }
        }

        // Keys left over on the right were added. Sorted for deterministic output.
        foreach (var name in rightProps.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            changes.Add(new Change(ChangeKind.Added, $"{path}/{Escape(name)}", null, Render(rightProps[name])));
        }
    }

    private static void WalkArray(JsonElement left, JsonElement right, string path, JsonDiffOptions options, List<Change> changes)
    {
        if (options.IgnoreArrayOrder)
        {
            WalkArrayAsMultiset(left, right, path, changes);
            return;
        }

        var leftItems = left.EnumerateArray().ToArray();
        var rightItems = right.EnumerateArray().ToArray();
        var shared = Math.Min(leftItems.Length, rightItems.Length);

        for (var i = 0; i < shared; i++)
        {
            Walk(leftItems[i], rightItems[i], $"{path}/{i}", options, changes);
        }
        for (var i = shared; i < leftItems.Length; i++)
        {
            changes.Add(new Change(ChangeKind.Removed, $"{path}/{i}", Render(leftItems[i]), null));
        }
        for (var i = shared; i < rightItems.Length; i++)
        {
            changes.Add(new Change(ChangeKind.Added, $"{path}/{i}", null, Render(rightItems[i])));
        }
    }

    private static void WalkArrayAsMultiset(JsonElement left, JsonElement right, string path, List<Change> changes)
    {
        // Compare by canonical value regardless of position. Each element matched once.
        var rightCanon = right.EnumerateArray().Select(Canonical).ToList();
        foreach (var item in left.EnumerateArray())
        {
            var canon = Canonical(item);
            var index = rightCanon.IndexOf(canon);
            if (index >= 0)
            {
                rightCanon.RemoveAt(index);
            }
            else
            {
                changes.Add(new Change(ChangeKind.Removed, $"{path}/*", Render(item), null));
            }
        }
        foreach (var leftover in rightCanon)
        {
            changes.Add(new Change(ChangeKind.Added, $"{path}/*", null, leftover));
        }
    }

    private static bool ScalarsEqual(JsonElement left, JsonElement right) => left.ValueKind switch
    {
        JsonValueKind.String => left.GetString() == right.GetString(),
        JsonValueKind.Number => NumbersEqual(left, right),
        _ => true, // true/false/null already matched on ValueKind
    };

    private static bool NumbersEqual(JsonElement left, JsonElement right)
    {
        // Compare numerically so 1 and 1.0 are equal, with the raw text as a fallback.
        if (left.TryGetDecimal(out var l) && right.TryGetDecimal(out var r))
        {
            return l == r;
        }
        return left.GetRawText() == right.GetRawText();
    }

    private static string Render(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? "null",
        JsonValueKind.Null => "null",
        JsonValueKind.Object or JsonValueKind.Array => Canonical(element),
        _ => element.GetRawText(),
    };

    /// <summary>A stable, whitespace-free rendering used for value equality and compact display.</summary>
    private static string Canonical(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.TryGetDecimal(out var d)
                    ? d.ToString(CultureInfo.InvariantCulture)
                    : element.GetRawText();
            case JsonValueKind.String:
                return JsonSerializer.Serialize(element.GetString());
            case JsonValueKind.Object:
                var members = element.EnumerateObject()
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .Select(p => $"{JsonSerializer.Serialize(p.Name)}:{Canonical(p.Value)}");
                return "{" + string.Join(",", members) + "}";
            case JsonValueKind.Array:
                return "[" + string.Join(",", element.EnumerateArray().Select(Canonical)) + "]";
            default:
                return element.GetRawText();
        }
    }

    // JSON Pointer escaping: '~' -> '~0', '/' -> '~1'.
    private static string Escape(string token) => token.Replace("~", "~0").Replace("/", "~1");
}
