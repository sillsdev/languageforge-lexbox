<general_rules>
- **Service Organization**: When creating new backend services, place them in `backend/LexBoxApi/Services/` and follow the namespace pattern `LexBoxApi.Services`. Always check if a similar service already exists before creating a new one.
- **Controller Organization**: When creating new API controllers, place them in `backend/LexBoxApi/Controllers/` and follow the namespace pattern `LexBoxApi.Controllers`. Controllers should be thin and delegate business logic to services.
- **Frontend Components**: When creating new UI components, place them in `frontend/src/lib/components/` organized by feature or functionality. Always check existing components before creating duplicates.
- **Task Automation**: Use Taskfile commands for all development workflows:
  - `task up` - Start full development environment with Tilt
  - `task api` - Backend-only development
  - `task ui` - Frontend-only development
  - `task test:unit` - Run backend unit tests
  - `task fw-lite-web` - Run FwLite web application
- **Code Formatting**: 
  - Backend: Follow .NET conventions defined in `.editorconfig` (4-space indentation for C#)
  - Frontend: Use ESLint and Prettier (2-space indentation, single quotes, 120 char line width)
  - Run `pnpm run lint` and `pnpm run format` for frontend code quality
- **Namespace Conventions**: Follow established patterns:
  - Backend services: `LexBoxApi.Services`, `FwHeadless.Services`, `LexCore.*`
  - Backend controllers: `LexBoxApi.Controllers`, `FwHeadless.Controllers`
  - Test namespaces: `Testing.*`, `*.Tests`
- **Database Migrations**: Use Entity Framework migrations via `task api:add-migration -- "MigrationName"` and `task api:db-update`
</general_rules>

<repository_structure>
- **Multi-Service Architecture**: LexBox is a complex linguistic application with multiple interconnected services:
  - `backend/LexBoxApi/` - Main .NET 9.0 API (GraphQL, REST, authentication)
  - `backend/FwHeadless/` - FieldWorks headless service for linguistic data processing
  - `backend/SyncReverseProxy/` - Proxy service for Mercurial sync operations
  - `backend/LexCore/` - Shared core library (entities, interfaces, utilities)
  - `backend/LexData/` - Data access layer with Entity Framework
  - `backend/FwLite/` - FieldWorks Lite application (Maui + Web)
- **Frontend Structure**:
  - `frontend/src/routes/` - SvelteKit file-based routing with authenticated/unauthenticated route groups
  - `frontend/src/lib/components/` - Reusable UI components organized by feature
  - `frontend/viewer/` - Separate FwLite viewer application with its own build system
- **Infrastructure**:
  - `deployment/` - Kubernetes configurations for local, staging, and production environments
  - `Tiltfile` - Local development orchestration with Docker and Kubernetes
  - `hgweb/` - Mercurial web interface configuration
  - `otel/` - OpenTelemetry collector configuration for monitoring
- **Additional Services**:
  - `platform.bible-extension/` - Platform.Bible extension for integration
  - `docs/` - Platform-specific developer setup guides
</repository_structure>

<dependencies_and_installation>
- **Prerequisites**: 
  - .NET SDK 9.0 for backend development
  - Node.js >=20 and PNPM >=9 for frontend development
  - Docker Desktop with Kubernetes enabled for local development
  - Taskfile for task automation
  - Tilt for development orchestration
- **Backend Dependencies**: Use `dotnet restore` in backend projects or `task api:dotnet -- restore`
- **Frontend Dependencies**: Use `pnpm install` in frontend directory or `task ui:install`
- **Initial Setup**: Run `task setup` to configure the development environment, including:
  - Git configuration and submodule initialization
  - Kubernetes namespace setup
  - Test data download
  - Local environment file creation
- **Development Environment**: 
  - `task up` starts the full stack with Tilt orchestration
  - Docker Desktop manages containers and Kubernetes cluster
  - Local services run on localhost with port forwarding for API access
</dependencies_and_installation>

<testing_instructions>
- **Backend Testing**:
  - Framework: xUnit with categorized tests (Unit, Integration, FlakyIntegration)
  - Run unit tests: `task test:unit` (excludes database and integration tests)
  - Run integration tests: `task test:integration`
  - Test organization: `backend/Testing/` with subdirectories by service/component
  - Database tests require running infrastructure via `task test:unit-with-db`
- **Frontend Testing**:
  - E2E Testing: Playwright for end-to-end browser tests
    - Run: `pnpm run test` or `task ui:playwright-tests`
    - Configuration: `playwright.config.ts`
  - Unit Testing: Vitest for component and utility testing
    - Run: `pnpm run test:unit`
  - Test files located in `frontend/tests/` and `frontend/viewer/tests/`
- **FwLite Testing**:
  - .NET tests: `dotnet test FwLiteOnly.slnf` for FwLite-specific tests
  - Separate test projects: `*.Tests` projects within `backend/FwLite/`
- **Test Categories**: Backend tests use attributes like `[Trait("Category", "Integration")]` to organize test execution
- **Browser Testing**: Playwright tests can be run with `--ui` flag for interactive debugging
</testing_instructions>

<pull_request_formatting>
</pull_request_formatting>
