using System.Text.Json.Serialization;
using FwLiteShared.Auth;
using FwLiteShared.Events;
using FwLiteShared.Projects;
using FwLiteShared.Services;
using LcmCrdt;
using MiniLcm.Models;
using SIL.Harmony.Db;

namespace FwLiteShared.Json;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(IAsyncEnumerable<HistoryLineItem>))]
[JsonSerializable(typeof(IAsyncEnumerable<ProjectActivity>))]
[JsonSerializable(typeof(IAsyncEnumerable<Entry>))]
[JsonSerializable(typeof(IProjectIdentifier))]
[JsonSerializable(typeof(ServerStatus[]))]
[JsonSerializable(typeof(LexboxServer))]
[JsonSerializable(typeof(CrdtProject))]
[JsonSerializable(typeof(ProjectData))]
[JsonSerializable(typeof(FwLiteConfig))]
[JsonSerializable(typeof(ObjectSnapshot))]
[JsonSerializable(typeof(ProjectScope))]
[JsonSerializable(typeof(IFwEvent))]
[JsonSerializable(typeof(Dictionary<string, ProjectModel[]>))]
[JsonSerializable(typeof(IReadOnlyCollection<ProjectModel>))]
public partial class SharedSourceGenerationContext : JsonSerializerContext
{

}

