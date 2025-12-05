# LexBox API

Main backend API for LexBox. Provides GraphQL API, authentication, and project management.

## Run

```bash
dotnet run --project LexBoxApi.csproj

# Or with watch
dotnet watch run --project LexBoxApi.csproj
```

## Routes

| Route | Purpose |
|-------|---------|
| `/api/graphql` | GraphQL API endpoint |
| `/api/graphql/ui` | GraphQL UI (Banana Cake Pop) |
| `/api/swagger` | REST API Swagger UI |
| `/api/healthz` | Health check |
| `/api/quartz` | Job scheduler UI (CrystalQuartz) |
| `/{project-code}` | Mercurial Send/Receive |
| `/hg/{project-code}` | Mercurial Send/Receive (explicit) |
| `/api/v03` | hg-resumable S/R |

## Project Structure

| Directory | Purpose |
|-----------|---------|
| `GraphQL/` | GraphQL schema, queries, mutations |
| `Auth/` | Authentication (JWT, OAuth, Google) |
| `Controllers/` | REST controllers |
| `Services/` | Business logic services |
| `Jobs/` | Background jobs (Quartz.NET) |
| `Hub/` | SignalR hubs |

## GraphQL (Hot Chocolate)

### Queries & Mutations

- `LexQueries.cs` - Main queries (projects, users, orgs)
- `ProjectMutations.cs` - Project CRUD
- `OrgMutations.cs` - Organization CRUD
- `UserMutations.cs` - User management

### Patterns

```csharp
// Use projections for efficient queries
[UseProjection]
[UseFiltering]
[UseSorting]
public IQueryable<Project> Projects([Service] LexBoxDbContext db)
    => db.Projects;

// Mutations return the modified entity
public async Task<Project> CreateProject(CreateProjectInput input)
{
    // ... create logic
    return project;
}
```

## Authentication

- **JWT**: Token-based auth via `JwtOptions`
- **Google OAuth**: Via `GoogleOptions`
- **OpenID**: Generic OIDC support
- **Service**: `LexAuthService` handles auth logic
- **Context**: `LoggedInContext` for current user

### Auth Attributes

```csharp
[Authorize]  // Require authentication
[AdminRequired]  // Require admin role
[ProjectMember]  // Require project membership
```

## Important Files

- `Program.cs` - App entry point, middleware setup
- `LexBoxKernel.cs` - DI registration
- `GraphQL/GraphQlSetupKernel.cs` - GraphQL configuration
- `Auth/AuthKernel.cs` - Auth configuration
- `appsettings.json` - Configuration
