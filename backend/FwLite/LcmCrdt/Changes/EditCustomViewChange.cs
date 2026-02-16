using MiniLcm.Models;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class EditCustomViewChange(
    Guid entityId,
    CustomView customView)
    : EditChange<CustomView>(entityId), ISelfNamedType<EditCustomViewChange>
{
    public CustomView CustomView { get; } = customView;

    public override ValueTask ApplyChange(CustomView entity, IChangeContext context)
    {
        entity.Name = CustomView.Name;
        entity.Base = CustomView.Base;
        entity.EntryFields = [.. CustomView.EntryFields.Select(f => new ViewField { FieldId = f.FieldId })];
        entity.SenseFields = [.. CustomView.SenseFields.Select(f => new ViewField { FieldId = f.FieldId })];
        entity.ExampleFields = [.. CustomView.ExampleFields.Select(f => new ViewField { FieldId = f.FieldId })];
        entity.Vernacular = CustomView.Vernacular is null ? null : [.. CustomView.Vernacular];
        entity.Analysis = CustomView.Analysis is null ? null : [.. CustomView.Analysis];
        return ValueTask.CompletedTask;
    }
}
