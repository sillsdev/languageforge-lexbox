namespace FwLiteProjectSync;

/// <summary>A human-readable record of a single write a dry-run sync would perform.</summary>
public record DryRunRecord(string Method, string Description);
