# Test commands

The narrowest test command per touched area, used by `/polish`, `/implement` and `/merge-risk`. `fwlite-sentinel.md` adds sync-specific commands.

| Touched | Command |
| --- | --- |
| FwLite sync (`MiniLcm/SyncHelpers/**`, `FwLiteProjectSync/**`) | `dotnet test backend/FwLite/FwLiteProjectSync.Tests --filter Sena3` (~2 min) |
| FwLite (other) | `task fw-lite:test-quick` |
| viewer | `cd frontend && pnpm --filter viewer run check && pnpm --filter viewer run test:unit` |
| lexbox frontend | `cd frontend && pnpm run check && pnpm run test:unit` |
| LexBox API | `dotnet test LexBoxOnly.slnf --filter "Category!=Integration&Category!=FlakyIntegration&Category!=RequiresDb"` |
| LexBox API + GraphQL schema | also `task api:generate-gql-schema` |
| viewer + .NET types | `dotnet build backend/FwLite/FwLiteShared/FwLiteShared.csproj` |

Don't suggest integration tests unless the user asks.
