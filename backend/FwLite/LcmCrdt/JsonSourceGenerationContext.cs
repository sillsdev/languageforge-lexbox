using System.Text.Json.Serialization;
using LcmCrdt.Objects;
using SIL.Harmony;
using SIL.Harmony.Core;
using SIL.Harmony.Db;

namespace LcmCrdt;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(WritingSystem))]
[JsonSerializable(typeof(MiniLcmCrdtAdapter))]
[JsonSerializable(typeof(IObjectWithId))]
[JsonSerializable(typeof(IObjectBase))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Commit))]
[JsonSerializable(typeof(ObjectSnapshot))]
[JsonSerializable(typeof(Entry))]
[JsonSerializable(typeof(List<SemanticDomain>))]
[JsonSerializable(typeof(IList<SemanticDomain>))]
[JsonSerializable(typeof(List<ComplexFormType>))]
public partial class JsonSourceGenerationContext: JsonSerializerContext
{

}
