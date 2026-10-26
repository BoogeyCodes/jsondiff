using System.Text;
using System.Text.Json;

namespace JsonDiff.Core;

public static class DiffReport
{
    /// <summary>Human-readable, one change per line, grouped added/removed/changed, sorted by path.</summary>
    public static string ToText(IReadOnlyList<Change> changes)
    {
        if (changes.Count == 0)
        {
            return "No differences.";
        }

        var sb = new StringBuilder();
        var ordered = changes
            .OrderBy(c => c.Kind)
            .ThenBy(c => c.Path, StringComparer.Ordinal);
        foreach (var change in ordered)
        {
            sb.AppendLine(change.ToString());
        }

        var added = changes.Count(c => c.Kind == ChangeKind.Added);
        var removed = changes.Count(c => c.Kind == ChangeKind.Removed);
        var changed = changes.Count(c => c.Kind == ChangeKind.Changed);
        sb.AppendLine();
        sb.Append($"{changes.Count} difference(s): {added} added, {removed} removed, {changed} changed.");
        return sb.ToString();
    }

    /// <summary>Machine-readable output for piping into other tools.</summary>
    public static string ToJson(IReadOnlyList<Change> changes)
    {
        var payload = changes
            .OrderBy(c => c.Kind)
            .ThenBy(c => c.Path, StringComparer.Ordinal)
            .Select(c => new
            {
                kind = c.Kind.ToString().ToLowerInvariant(),
                path = c.Path,
                oldValue = c.OldValue,
                newValue = c.NewValue,
            });

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }
}
