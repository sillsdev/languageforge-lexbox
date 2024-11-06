using SIL.Harmony;
using SIL.Harmony.Adapters;
using SIL.Harmony.Entities;

namespace LcmCrdt.Objects;

public class MiniLcmCrdtAdapter : ICustomAdapter<MiniLcmCrdtAdapter, IObjectWithId>
{
    public static string AdapterTypeName => "MiniLcmCrdtAdapter";

    public MiniLcmCrdtAdapter(IObjectWithId obj)
    {
        Obj = obj;
    }

    public IObjectWithId Obj { get; set; }
    public Guid Id => Obj.Id;

    public DateTimeOffset? DeletedAt
    {
        get => Obj.DeletedAt;
        set => Obj.DeletedAt = value;
    }

    public Guid[] GetReferences()
    {
        return Obj.GetReferences();
    }

    public void RemoveReference(Guid id, Commit commit)
    {
        Obj.RemoveReference(id, commit.DateTime);
    }

    public IObjectBase Copy()
    {
        return Create(Obj.Copy());
    }

    public string GetObjectTypeName()
    {
        //todo we might not want to do this as a refactor rename of any of our objects will cause problems
        return Obj.GetType().Name;
    }

    public object DbObject => Obj;

    public static MiniLcmCrdtAdapter Create(IObjectWithId obj)
    {
        return new MiniLcmCrdtAdapter(obj);
    }
}
