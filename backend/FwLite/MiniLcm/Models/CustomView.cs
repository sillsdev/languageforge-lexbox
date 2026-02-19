using System.Text.Json.Serialization;

namespace MiniLcm.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ViewBase { FwLite, FieldWorks }

public class ViewField
{
    public required string FieldId { get; set; }
    // Future possibilities: IsReadOnly, HideIfEmpty, custom writing-system selection etc.

    public ViewField Copy()
    {
        return new ViewField
        {
            FieldId = FieldId
        };
    }
}

public class ViewWritingSystem
{
    public required WritingSystemId WsId { get; set; }
    // Future possibilities: IsReadOnly etc.

    public ViewWritingSystem Copy()
    {
        return new ViewWritingSystem
        {
            WsId = WsId
        };
    }
}

public record CustomView : IObjectWithId<CustomView>
{
    public virtual Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public virtual required string Name { get; set; }
    public ViewBase Base { get; set; }

    // Visibility = presence in the array. Field order in the list is ignored; builtin order is used,
    // If view-level ordering is desired, an explicit flag should be introduced on the view,
    // since project-level field ordering is more likely.
    public virtual ViewField[] EntryFields { get; set; } = [];
    public virtual ViewField[] SenseFields { get; set; } = [];
    public virtual ViewField[] ExampleFields { get; set; } = [];

    // Visibility = presence in the array. WS order comes from the project.
    // If view-level WS ordering were introduced, it should probably propagate to views,
    // so an explicit flag would be appropriate.
    public virtual ViewWritingSystem[]? Vernacular { get; set; } // null = inherit project defaults
    public virtual ViewWritingSystem[]? Analysis { get; set; } // null = inherit project defaults

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public CustomView Copy()
    {
        return new CustomView
        {
            Id = Id,
            DeletedAt = DeletedAt,
            Name = Name,
            Base = Base,
            EntryFields = [.. EntryFields.Select(f => f.Copy())],
            SenseFields = [.. SenseFields.Select(f => f.Copy())],
            ExampleFields = [.. ExampleFields.Select(f => f.Copy())],
            Vernacular = Vernacular is null ? null : [.. Vernacular.Select(ws => ws.Copy())],
            Analysis = Analysis is null ? null : [.. Analysis.Select(ws => ws.Copy())]
        };
    }
}
