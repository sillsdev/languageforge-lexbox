using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateCustomViewChange(
    Guid entityId,
    CustomView customView)
    : CreateChange<CustomView>(entityId), ISelfNamedType<CreateCustomViewChange>
{
    public CustomView CustomView { get; } = customView;

    public override ValueTask<CustomView> NewEntity(Commit commit, IChangeContext context)
    {
        return ValueTask.FromResult(new CustomView
        {
            Id = EntityId,
            Name = CustomView.Name,
            Base = CustomView.Base,
            EntryFields = CustomView.EntryFields,
            SenseFields = CustomView.SenseFields,
            ExampleFields = CustomView.ExampleFields,
            Vernacular = CustomView.Vernacular,
            Analysis = CustomView.Analysis,
        });
    }
}
