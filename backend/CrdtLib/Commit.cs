using System.Text.Json.Serialization;
using Crdt.Core;
using CrdtLib.Changes;

namespace CrdtLib.Db;

public class Commit : CommitBase<IChange>
{
    [JsonConstructor]
    protected Commit(Guid id, string hash, string parentHash, HybridDateTime hybridDateTime) : base(id,
        hash,
        parentHash,
        hybridDateTime)
    {
    }


    public Commit(Guid id) : base(id)
    {
    }

    public Commit()
    {
    }

    [JsonIgnore]
    public List<ObjectSnapshot> Snapshots { get; init; } = [];
}
