# FwLite (FieldWorks Lite)

Lightweight FieldWorks application for dictionary editing with CRDT-based sync.

## Quick Start

```bash
# Run FwLite Web (typical workflow)
task fw-lite-web   # from repo root

# Run tests
dotnet test FwLiteOnly.slnf

# Build MAUI app (Windows)
dotnet build FwLiteMaui/FwLiteMaui.csproj --framework net9.0-windows10.0.19041.0

# Build for specific platform
dotnet build -f net9.0-ios
dotnet build -f net9.0-android
```

## Project Structure

| Directory | Purpose |
|-----------|---------|
| `MiniLcm/` | Core dictionary API - entries, senses, definitions |
| `LcmCrdt/` | CRDT implementation for sync (extends MiniLcm) |
| `FwLiteMaui/` | .NET MAUI desktop/mobile app |
| `FwLiteWeb/` | ASP.NET Core web host for FwLite |
| `FwDataMiniLcmBridge/` | Bridge to full FieldWorks FwData format |
| `FwLiteProjectSync/` | Sync logic between FwLite and LexBox |
| `FwLiteShared/` | Shared code across FwLite apps |

## Architecture

```
┌─────────────────┐     ┌─────────────────┐
│  FwLite MAUI    │     │   FwLite Web    │
└────────┬────────┘     └────────┬────────┘
         │                       │
         └───────────┬───────────┘
                     │
              ┌──────▼──────┐
              │   MiniLcm   │  ← Core API
              └──────┬──────┘
                     │
              ┌──────▼──────┐
              │   LcmCrdt   │  ← CRDT sync layer
              └──────┬──────┘
                     │
              ┌──────▼──────┐
              │   Harmony   │  ← Sync to LexBox
              └─────────────┘
```

## Adding a New Harmony Change

1. Create change class in `LcmCrdt/Changes/`
   - Extend `CreateChange<T>` or `EditChange<T>`
   - See `CreateComplexFormType.cs` for create example
   - See `AddComplexFormTypeChange.cs` for edit example

2. Register in `LcmCrdt/LcmCrdtKernel.cs` → `ConfigureCrdt()`

3. Add test in `LcmCrdt.Tests/Changes/UseChangesTests.cs` → `GetAllChanges()`

### Important: JSON Serialization

Constructor parameter names **must match** property names (camelCase → PascalCase):

```csharp
// ❌ Wrong - userName doesn't match Name
public MyChange(string userName) { Name = userName; }

// ✅ Correct - name matches Name
public MyChange(string name) { Name = name; }
```

### Handle Deleted References

Changes may reference deleted objects (due to sync). Always check:

```csharp
if (complexFormType?.DeletedAt is not null) return;
```

## Important Files

- `MiniLcm/ILexboxApi.cs` - Core API interface
- `LcmCrdt/LcmCrdtKernel.cs` - CRDT registration
- `LcmCrdt/CrdtMiniLcmApi.cs` - CRDT implementation of API
- `FwLiteMaui/LocalDbContext.cs` - SQLite storage
- `FwDataMiniLcmBridge/FwDataMiniLcmApi.cs` - Bridge to full FW
