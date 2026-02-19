using System.Text.Json.Serialization;
using MiniLcm.Models;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class EditCustomViewChange : EditChange<CustomView>, ISelfNamedType<EditCustomViewChange>
{
    public EditCustomViewChange(Guid entityId, CustomView customView) : base(entityId)
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
    private EditCustomViewChange(Guid entityId) : base(entityId)
    {
    }

    public string Name { get; set; } = string.Empty;
    public ViewBase Base { get; set; }
    public ViewField[] EntryFields { get; set; } = [];
    public ViewField[] SenseFields { get; set; } = [];
    public ViewField[] ExampleFields { get; set; } = [];
    public ViewWritingSystem[]? Vernacular { get; set; }
    public ViewWritingSystem[]? Analysis { get; set; }

    public override ValueTask ApplyChange(CustomView entity, IChangeContext context)
    {
        entity.Name = Name;
        entity.Base = Base;
        entity.EntryFields = EntryFields;
        entity.SenseFields = SenseFields;
        entity.ExampleFields = ExampleFields;
        entity.Vernacular = Vernacular;
        entity.Analysis = Analysis;
        return ValueTask.CompletedTask;
    }
}
