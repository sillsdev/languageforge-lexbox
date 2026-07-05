namespace MiniLcm.Models;

/// <summary>
/// A row in the sense-expanded entry list (see IMiniLcmReadApi.GetEntrySenseRows):
/// one row per sense, or a single row with a null SenseId for an entry without senses.
/// Rows of the same entry may share one Entry instance.
/// </summary>
public record EntrySenseRow(Guid? SenseId, Entry Entry);
