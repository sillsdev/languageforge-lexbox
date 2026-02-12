# Custom View — Final Design (First Pass)

Based on issues [#1049](https://github.com/sillsdev/languageforge-lexbox/issues/1049), [#1050](https://github.com/sillsdev/languageforge-lexbox/issues/1050), [#1985](https://github.com/sillsdev/languageforge-lexbox/issues/1985).

## Model

```csharp
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
    public required ViewField[] Fields { get; set; }       // array position = display order
    public WritingSystemId[]? Vernacular { get; set; }     // null = inherit project defaults
    public WritingSystemId[]? Analysis { get; set; }       // null = inherit project defaults
    public DateTimeOffset? DefaultAsOf { get; set; }       // LWW register: Max(DefaultAsOf) wins
    public string? DefaultFilter { get; set; }             // Gridify filter string

    public Guid[] GetReferences() => [];
    public void RemoveReference(Guid id, DateTimeOffset time) { }
    public CustomView Copy() => this with
    {
        Fields = [.. Fields.Select(f => new ViewField { FieldId = f.FieldId })],
        Vernacular = Vernacular is null ? null : [.. Vernacular],
        Analysis = Analysis is null ? null : [.. Analysis],
    };
}
```

## Field Decisions

| Field | Decision | Rationale |
|-------|----------|-----------|
| `Id` | ✅ Keep | Required by `IObjectWithId` |
| `DeletedAt` | ✅ Add | Required by `IObjectWithId` (soft-delete) |
| `Name` | ✅ Keep | User-facing view name |
| `Base` | ✅ Keep as enum | Only two values (`FwLite`, `FieldWorks`). Use `[JsonStringEnumConverter]` per codebase convention. No WeSay view. |
| `Fields` | ✅ Keep, change to `ViewField[]` | Array position = order, presence = visible. Using objects (not strings) for forward-compat: adding optional properties to `ViewField` later is a non-breaking JSON change in CRDT; changing `string[]` → `ViewField[]` later would be painful. |
| `Vernacular` | ✅ Keep, make nullable | `null` = inherit project WS defaults. `WritingSystemId` already exists in `MiniLcm.Models`. |
| `Analysis` | ✅ Keep, make nullable | Same as Vernacular. |
| `DefaultAsOf` | ✅ Keep | LWW (last-writer-wins) CRDT pattern. Query: `Max(DefaultAsOf, Id)` for deterministic tiebreak. No need to "unset" old default — new timestamp just wins. |
| `DefaultFilter` | ✅ Keep | Gridify filter format is stable and already used throughout the codebase. Just a nullable string. |

## Deferred (add later as needed)

| Field | Why Deferred |
|-------|-------------|
| `Description` | Nice-to-have, trivially added later as optional string |
| `CreatedBy` / `CreatedAt` | Metadata lives in the Harmony commit, not on the entity |
| `IsSystemView` | Built-in views (FW Lite, FW Classic) are hardcoded in frontend `view-data.ts`, not stored in CRDT |
| Permissions / roles | Team decision to defer. Can be added as a "view thing" or "user/role thing" later |
| Per-field WS visibility | Kevin's issue explicitly says this is not supported; fits better with tasks |

## Change Types Needed

### Must-have for first pass:

| Change | Base Class | Purpose |
|--------|-----------|---------|
| `CreateCustomViewChange` | `CreateChange<CustomView>` | Create a new custom view with all initial properties |
| `JsonPatchChange<CustomView>` | `EditChange<CustomView>` | Edit any property (Name, Base, Fields, WS arrays, DefaultAsOf, DefaultFilter). Replaces whole arrays — acceptable since views change rarely. |
| `DeleteChange<CustomView>` | — | Soft-delete (sets DeletedAt) |

No special change classes needed for setting fields, defaults, etc. — `JsonPatchChange` handles all of it by replacing the property value. Concurrent edits to the same property = last writer wins, which is acceptable for view configuration.

### Registration in `LcmCrdtKernel.ConfigureCrdt()`:

```csharp
// Object type
.Add<CustomView>(builder =>
{
    builder.Property(v => v.Fields)
        .HasColumnType("jsonb")
        .HasConversion(
            list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<ViewField[]>(json, (JsonSerializerOptions?)null) ?? []);
    builder.Property(v => v.Vernacular)
        .HasColumnType("jsonb")
        .HasConversion(
            list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
            json => json == null ? null : JsonSerializer.Deserialize<WritingSystemId[]>(json, (JsonSerializerOptions?)null));
    builder.Property(v => v.Analysis)
        .HasColumnType("jsonb")
        .HasConversion(
            list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
            json => json == null ? null : JsonSerializer.Deserialize<WritingSystemId[]>(json, (JsonSerializerOptions?)null));
})

// Change types
.Add<CreateCustomViewChange>()
.Add<JsonPatchChange<CustomView>>()
.Add<DeleteChange<CustomView>>()
```

## API Methods Needed

On `IMiniLcmReadApi` / `IMiniLcmWriteApi` (or a new `ICustomViewApi`):

| Method | Purpose |
|--------|---------|
| `GetCustomViews()` | List all non-deleted custom views |
| `GetCustomView(Guid id)` | Get a single view |
| `CreateCustomView(CustomView view)` | Create and return |
| `UpdateCustomView(Guid id, UpdateObjectInput<CustomView> update)` | JSON patch update |
| `DeleteCustomView(Guid id)` | Soft-delete |

## Key Behaviors

- **Setting default:** `UpdateCustomView(id, patch { DefaultAsOf = DateTimeOffset.UtcNow })`
- **Unsetting default on old view:** Not needed — new timestamp wins automatically
- **Finding default view:** `views.Where(v => v.DefaultAsOf != null).OrderByDescending(v => v.DefaultAsOf).ThenByDescending(v => v.Id).First()`
- **Field ordering:** Array position in `Fields` is the display order
- **Hidden fields:** Any field not in the `Fields` array is hidden
- **WS inheritance:** `Vernacular == null` means use all project vernacular writing systems
- **Concurrent edits:** Last writer wins for the whole property (acceptable for view config)
- **Selected view per user:** Stored locally (localStorage / preferences), NOT in CRDT

## Files to Create/Modify

1. **Create:** `MiniLcm/Models/CustomView.cs` — model, ViewField, ViewBase enum
2. **Create:** `LcmCrdt/Changes/CreateCustomViewChange.cs` — create change
3. **Modify:** `MiniLcm/Models/IObjectWithId.cs` — add `JsonDerivedType` for CustomView
4. **Modify:** `LcmCrdt/LcmCrdtKernel.cs` — register object + change types in `ConfigureCrdt()`
5. **Modify:** API interfaces and implementations (CrdtMiniLcmApi, FwDataMiniLcmApi stub)
