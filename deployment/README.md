# Deployments

## k8s Deployments

### Production, Staging & Develop

- lexbox
  - .net API (GQL API, REST API, hg proxy etc. (see [backend README](../backend/README.md)))
  - OTEL collector
  - db-migrations (init-container that ensures DB migrations are applied)
- db (PostgresDB)
- hg
  - hg
  - hg-resumable
- ui (SvelteKit app)

### Everything except Production

Same as above plus:
- hg
  - populate-test-repos (init-container that provides a test repo)

### Local development

Same as above plus:

- lexbox
  - maildev
  - aspire
- hg
  - ~~db-migrations~~ (Removed: migrations are done at app startup)

## Hostnames

| Domain | Description |
| - | - |
| `lexbox.org`<br>`languagedepot.org` | route well known paths[^1] to `lexbox/` and everything else to the UI |
| `resumable.languagedepot.org`<br>`resumable.languageforge.org`<br>`hg-public.languagedepot.org`<br>`hg-public.languageforge.org`<br>`hg-private.languagedepot.org`<br>`hg-private.languageforge.org`<br>`admin.languagedepot.org`<br>`admin.languageforge.org` | route everything to `lexbox/` |

### Staging

| Domain | Description |
| - | - |
| `staging.languagedepot.org` | routes well known paths[^1] to `lexbox/` and everything else to the UI |
| `hg-staging.languageforge.org`<br>`resumable-staging.languagedepot.org`<br>`admin-staging.languagedepot.org` | route everything to `lexbox/` |

### Develop

| Domain | Description |
| - | - |
| `develop.lexbox.org` | routes well known paths[^1] to `lexbox/` and everything else to the UI |
| `resumable-develop.lexbox.org`<br>`hg-develop.lexbox.org` | route everything to `lexbox/` |


### Local development

| Domain | Description |
| - | - |
| `localhost[:80}]` | routes well known paths[^1] to `lexbox/` and everything else to the UI |
| `localhost[:5158]` | .NET API (exposed for Vite proxy and API tools) |
| `localhost:{4317,4318}` | OTEL Collector (exposed for Vite proxy) |
| `localhost:1080` | Maildev UI |
| `localhost:1025` | Maildev SMTP |
| `localhost:18888` | [Aspire dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard) |
| `hg.localhost`<br>`resumable.localhost`<br>`admin.localhost` | route everything to `lexbox/` |

[^1]: Well known paths include: `/api`, `/hg` (see [backend README](../backend/README.md)), `/traces/v1` (OpenTelemetry trace export) etc.
