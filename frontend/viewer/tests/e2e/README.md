# FW Lite E2E Tests

End-to-end tests for FieldWorks Lite that drive the actual published binary against a real (kind-hosted) LexBox server.

> **Status: in progress / WIP.** Most of this code was AI-generated; expect rough edges. The first goal is to get a smoke test through CI; the rich integration scenarios will follow.

## What's tested (so far)

The suite has two layers:

### 1. Launcher integration tests — `tests/integration/fw-lite-launcher.test.ts` (Vitest)

These exercise [`FwLiteLauncher`](./helpers/fw-lite-launcher.ts), the helper that spawns and shuts down the published `FwLiteWeb` binary. They run via vitest (no browser):

- Synthetic checks: `isRunning()` defaults, error on unknown binary path, error if launched twice, port-finding sanity.
- **Real binary smoke test**: spawn the actual `FwLiteWeb` binary, wait for it to become healthy on `/health`, hit it over HTTP, then shut it down. Repeated with a second port to confirm "already running" behaviour.

In CI these run *before* the Playwright suite as a fast pre-flight: if the binary won't start at all, fail fast.

Task entry point: `task e2e-test-helper-unit-tests`.

### 2. Playwright E2E — `tests/e2e/fw-lite-integration.test.ts`

These drive a real browser against a real `FwLiteWeb` binary, with a real (kind-cluster-hosted) LexBox server. Two tests so far:

- **Smoke test: Application launch and server connectivity.** Boots FwLiteWeb, opens it in Chromium, logs in to LexBox, asserts at least one server project is visible.
- **Project download: Download and verify project structure.** Logs in, downloads `sena-3` from the configured server, then opens the downloaded local copy and asserts the project page renders.

Each test launches its own `FwLiteWeb` process (and shuts it down afterwards via stdin `shutdown` on Windows / SIGTERM elsewhere) and uses an isolated user-data directory so logins don't bleed between tests. After-each cleanup logs out from the server and deletes the local CRDT project copy via `DELETE /api/crdt/{code}` (a helper endpoint added on this branch).

Task entry point: `task test:e2e`.

## How CI runs this

The `e2e-test` job in [`.github/workflows/fw-lite.yaml`](../../../../.github/workflows/fw-lite.yaml):

1. Depends on `frontend` only — does *not* wait for the Windows `build-and-test` job, so feedback comes ~20+ minutes sooner.
2. Downloads the viewer JS artifact built by `frontend`.
3. Runs `dotnet publish -r linux-x64 -p:PublishSingleFile=true` to produce `FwLiteWeb`.
4. Spins up an in-cluster LexBox via the local `setup-k8s` action (port 6579, HTTP).
5. Runs the launcher unit tests, then the Playwright suite.
6. Uploads `tests/e2e/test-results/` as the `fw-lite-e2e-test-results` artifact on failure (traces, screenshots, videos, FW Lite server log).

## Backend changes that landed for this

- `GET /health` (`AddHealthChecks` + `MapHealthChecks`) — used by the launcher for its readiness probe.
- `DELETE /api/crdt/{code}` — used by per-test cleanup so re-running the suite doesn't leave stale local CRDT projects.
- Stdin-triggered shutdown in `FwLiteWeb/Program.cs` — Windows can't send SIGTERM to a child process, so the launcher writes `shutdown\n` to stdin instead.
- `--Auth:LexboxServers:0:Authority` overrides — the test launches `FwLiteWeb` pointed at the kind-hosted LexBox, with `--environment Development` so OAuth accepts the self-signed cert.
- Anchor IDs on `HomeView.svelte` (`#local-projects`) and `Server.svelte` (`#{server.id}`) for stable Playwright selectors.

## Running locally

```bash
# Build the FwLiteWeb binary for the host platform
task -d frontend/viewer test:e2e-setup

# Then either:
task -d frontend/viewer e2e-test-helper-unit-tests   # launcher checks only
task -d frontend/viewer test:e2e                     # full Playwright suite (needs a LexBox server)
```

`config.ts` reads from env: `FW_LITE_BINARY_PATH`, `TEST_SERVER_HOSTNAME`, `TEST_SERVER_PORT`, `TEST_USER`, `TEST_DEFAULT_PASSWORD`, `TEST_PROJECT_CODE`. The default binary path (`./dist/fw-lite-server/FwLiteWeb.exe`) assumes Windows; override on Linux/Mac.

## Known rough edges

- `helpers/test-data.ts` `cleanupTestData` is a stub — it logs but doesn't actually call any API.
- The default `binaryPath` in `config.ts` is Windows-only (`.exe`); use `FW_LITE_BINARY_PATH` to override.
- Playwright config runs serially (`workers: 1`) — each test owns its own `FwLiteWeb` process, so parallelism would need port coordination.
