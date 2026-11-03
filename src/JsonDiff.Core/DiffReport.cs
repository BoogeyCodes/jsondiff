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

        sb.AppendLine();
        sb.Append(Summarize(changes).ToString());
        return sb.ToString();
    }

    /// <summary>Counts of each kind of change.</summary>
    public static DiffStats Summarize(IReadOnlyList<Change> changes) => new(
        changes.Count(c => c.Kind == ChangeKind.Added),
        changes.Count(c => c.Kind == ChangeKind.Removed),
        changes.Count(c => c.Kind == ChangeKind.Changed));

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
