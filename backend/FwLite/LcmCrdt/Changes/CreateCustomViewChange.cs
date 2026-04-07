using System.Text.Json.Serialization;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateCustomViewChange : CreateChange<CustomView>, ISelfNamedType<CreateCustomViewChange>
{
    public CreateCustomViewChange(Guid entityId, CustomView customView) : base(entityId)
    {
        Name = customView.Name;
        Base = customView.Base;
        EntryFields = customView.EntryFields;
        SenseFields = customView.SenseFields;
        ExampleFields = customView.ExampleFields;
        Vernacular = customView.Vernacular;
        Analysis = customView.Analysis;
    }

    [JsonConstructor]
    public CreateCustomViewChange(Guid entityId) : base(entityId)
    {
    }

    public string Name { get; set; } = string.Empty;
    public ViewBase Base { get; set; }
    public ViewField[] EntryFields { get; set; } = [];
    public ViewField[] SenseFields { get; set; } = [];
    public ViewField[] ExampleFields { get; set; } = [];
    public ViewWritingSystem[]? Vernacular { get; set; }
    public ViewWritingSystem[]? Analysis { get; set; }

    public override ValueTask<CustomView> NewEntity(Commit commit, IChangeContext context)
    {
        return ValueTask.FromResult(new CustomView
        {
            Id = EntityId,
            Name = Name,
            Base = Base,
            EntryFields = EntryFields,
            SenseFields = SenseFields,
            ExampleFields = ExampleFields,
            Vernacular = Vernacular,
            Analysis = Analysis,
        });
    }
}
