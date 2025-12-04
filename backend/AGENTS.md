# Backend

.NET 9 backend for LexBox and FwLite applications.

## Build & Test

```bash
# Build all backend
dotnet build

# Run all tests
dotnet test

# Build specific project
dotnet build LexBoxApi/LexBoxApi.csproj
dotnet build FwLite/FwLiteMaui/FwLiteMaui.csproj --framework net9.0-windows10.0.19041.0
```

## Project Structure

| Directory | Purpose |
|-----------|---------|
| `LexBoxApi/` | Main API - GraphQL, auth, project management |
| `LexCore/` | Core domain models shared across projects |
| `LexData/` | EF Core data access, PostgreSQL |
| `FwLite/` | FwLite apps (MAUI, Web) and MiniLcm API |
| `FwHeadless/` | Headless service for hg sync, FwData processing |
| `Testing/` | Integration and API tests |
| `Ycs/` | Yjs CRDT implementation in C# |
| `harmony/` | Git submodule - Harmony sync protocol |

## Code Conventions

- **Nullable**: Enabled globally, `Nullable` warnings are errors
- **Implicit usings**: Enabled
- **Target framework**: net9.0 (unless platform-specific)
- **Async**: Use `async/await`, not `.Result` or `.Wait()`
- **Records**: Prefer for DTOs and immutable data

## Key Patterns

### GraphQL (Hot Chocolate)

- Schema in `LexBoxApi/GraphQL/`
- Use `[UseProjection]`, `[UseFiltering]`, `[UseSorting]` attributes
- Mutations return the modified entity

### Entity Framework Core

- DbContext in `LexData/`
- Migrations: `dotnet ef migrations add <Name> --project LexData`
- Use `IQueryable` projections, avoid loading full entities

### Dependency Injection

- Register services in `*Kernel.cs` files (e.g., `LexBoxKernel.cs`)
- Use `AddScoped` for request-scoped, `AddSingleton` for app-wide

## Important Files

- `Directory.Build.props` - Shared MSBuild properties
- `Directory.Packages.props` - Central package management
- `LexBoxApi/Program.cs` - API entry point
- `LexCore/Entities/` - Domain entities
- `Testing/Fixtures/` - Test fixtures and utilities
