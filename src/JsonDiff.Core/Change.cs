namespace JsonDiff.Core;

public enum ChangeKind
{
    Added,
    Removed,
    Changed,
}

/// <summary>
/// A single difference between two JSON documents, located by a JSON-Pointer-style path.
/// </summary>
/// <param name="Kind">Whether the value was added, removed or changed.</param>
/// <param name="Path">Location of the value, e.g. <c>/user/roles/0</c>. The root is <c>/</c>.</param>
/// <param name="OldValue">The left-hand value (null for an addition).</param>
/// <param name="NewValue">The right-hand value (null for a removal).</param>
public sealed record Change(ChangeKind Kind, string Path, string? OldValue, string? NewValue)
{
    public override string ToString() => Kind switch
    {
        ChangeKind.Added => $"+ {Path}: {NewValue}",
        ChangeKind.Removed => $"- {Path}: {OldValue}",
        ChangeKind.Changed => $"~ {Path}: {OldValue} -> {NewValue}",
        _ => $"? {Path}",
    };
}
