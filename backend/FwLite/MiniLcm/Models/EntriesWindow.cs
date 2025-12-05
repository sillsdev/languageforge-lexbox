namespace MiniLcm.Models;

/// <summary>
/// Represents a paginated window of entries with total count information.
/// Used for virtual scrolling where only a subset of entries is loaded at a time.
/// </summary>
public record EntriesWindow(
    /// <summary>
    /// The entries in this window/page
    /// </summary>
    Entry[] Entries,
    /// <summary>
    /// Total number of entries matching the query (before pagination)
    /// </summary>
    int TotalCount,
    /// <summary>
    /// The offset (0-based index) of the first entry in this window
    /// </summary>
    int Offset,
    /// <summary>
    /// The index of the target entry if one was requested and found, otherwise null
    /// </summary>
    int? TargetIndex = null
);
