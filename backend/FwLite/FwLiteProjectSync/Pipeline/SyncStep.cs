namespace FwLiteProjectSync.Pipeline;

/// <summary>
/// A single unit of work in the sync pipeline. Steps declare ordering constraints relative to other
/// steps (<see cref="After"/>/<see cref="Before"/>) by name; the pipeline topologically sorts them and
/// executes them sequentially. New steps may be added while a step runs (see <see cref="SyncStepContext.AddStep"/>).
/// </summary>
/// <param name="Name">Unique identifier used by other steps' ordering constraints. Defaults to a type name via <see cref="For{TKey}"/>.</param>
/// <param name="Execute">The work this step performs.</param>
/// <param name="After">Names of steps that must complete before this one runs.</param>
/// <param name="Before">Names of steps that must run after this one.</param>
public sealed record SyncStep(
    string Name,
    Func<SyncStepContext, Task> Execute,
    IReadOnlyList<string> After,
    IReadOnlyList<string> Before)
{
    /// <summary>Converts a type to the string name used internally as a step key.</summary>
    internal static string KeyOf(Type type) => type.Name;

    /// <summary>Start building a step whose name defaults to <c>typeof(TKey).Name</c>.</summary>
    public static SyncStepBuilder For<TKey>(Func<SyncStepContext, Task> execute) =>
        new(KeyOf(typeof(TKey)), execute);

    /// <summary>Start building a step with an explicit name (for dynamically-created instance steps).</summary>
    public static SyncStepBuilder Named(string name, Func<SyncStepContext, Task> execute) =>
        new(name, execute);
}

/// <summary>Fluent builder for <see cref="SyncStep"/>. Implicitly converts to a <see cref="SyncStep"/>.</summary>
public sealed class SyncStepBuilder(string name, Func<SyncStepContext, Task> execute)
{
    private readonly List<string> _after = [];
    private readonly List<string> _before = [];

    /// <summary>This step must run after the step named <c>typeof(TKey).Name</c>.</summary>
    public SyncStepBuilder After<TKey>() => After(SyncStep.KeyOf(typeof(TKey)));

    public SyncStepBuilder After(string stepName)
    {
        _after.Add(stepName);
        return this;
    }

    /// <summary>This step must run before the step named <c>typeof(TKey).Name</c>.</summary>
    public SyncStepBuilder Before<TKey>() => Before(SyncStep.KeyOf(typeof(TKey)));

    public SyncStepBuilder Before(string stepName)
    {
        _before.Add(stepName);
        return this;
    }

    public SyncStep Build() => new(name, execute, _after, _before);

    public static implicit operator SyncStep(SyncStepBuilder builder) => builder.Build();
}
