using System.Text.Json.Serialization;

namespace MiniLcm.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ViewBase { FwLite, FieldWorks }

public class ViewField
{
    public required string FieldId { get; set; }
    // Future: IsReadOnly, Width, Label, etc. — add as optional properties (non-breaking in CRDT)
}

public record CustomView : IObjectWithId<CustomView>
{
    public Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public required string Name { get; set; }
    public ViewBase Base { get; set; }
    public required ViewField[] EntryFields { get; set; } = [];  // array position = display order
    public required ViewField[] SenseFields { get; set; } = [];  // array position = display order
    public required ViewField[] ExampleFields { get; set; } = []; // array position = display order
    public WritingSystemId[]? Vernacular { get; set; }     // null = inherit project defaults
    public WritingSystemId[]? Analysis { get; set; }       // null = inherit project defaults

    public Guid[] GetReferences() => [];
    public void RemoveReference(Guid id, DateTimeOffset time) { }
    public CustomView Copy() => this with
    {
        EntryFields = [.. EntryFields.Select(f => new ViewField { FieldId = f.FieldId })],
        SenseFields = [.. SenseFields.Select(f => new ViewField { FieldId = f.FieldId })],
        ExampleFields = [.. ExampleFields.Select(f => new ViewField { FieldId = f.FieldId })],
        Vernacular = Vernacular is null ? null : [.. Vernacular],
        Analysis = Analysis is null ? null : [.. Analysis],
    };
}
