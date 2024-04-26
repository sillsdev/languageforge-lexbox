using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using CrdtLib.Db;
using CrdtLib.Entities;

namespace CrdtLib.Changes;

[JsonPolymorphic]
public interface IChange
{
    [JsonIgnore]
    Guid Id { get; set; }

    [JsonIgnore]
    Guid CommitId { get; set; }

    [JsonIgnore]
    Guid EntityId { get; set; }

    [JsonIgnore]
    Type EntityType { get; }

    ValueTask ApplyChange(IObjectBase entity, ChangeContext context);
    IObjectBase NewEntity(Commit commit);
}

public abstract class Change<T> : IChange where T : IObjectBase
{
    protected Change(Guid entityId)
    {
        Id = Guid.NewGuid();
        EntityId = entityId;
    }

    [JsonIgnore]
    public Guid Id { get; set; }

    [JsonIgnore]
    public Guid CommitId { get; set; }

    public Guid EntityId { get; set; }

    public abstract IObjectBase NewEntity(Commit commit);
    public abstract ValueTask ApplyChange(T entity, ChangeContext context);

    public async ValueTask ApplyChange(IObjectBase entity, ChangeContext context)
    {
        if (entity is T entityT) await ApplyChange(entityT, context);
    }

    [JsonIgnore]
    public Type EntityType => typeof(T);

}

public abstract class SingleObjectChange<T> : Change<T> where T : IObjectBase, INewableObject<T>
{
    protected SingleObjectChange(Guid entityId) : base(entityId)
    {
    }

    public override ValueTask ApplyChange(T entity, ChangeContext context)
    {
        ApplyChange(entity);
        return ValueTask.CompletedTask;
    }

    public override IObjectBase NewEntity(Commit commit)
    {
        var entity = T.New(EntityId, commit);
        return entity;
    }

    public abstract void ApplyChange(T value);
}