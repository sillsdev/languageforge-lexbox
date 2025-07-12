![Alt](https://repobeats.axiom.co/api/embed/141687e565b553d7d31c4b2b95f9d6762a4a901d.svg "Repobeats analytics image")

[![Staging workflow](https://github.com/sillsdev/languageforge-lexbox/actions/workflows/lexbox-api.yaml/badge.svg?branch=develop)](https://github.com/sillsdev/languageforge-lexbox/actions/workflows/lexbox-api.yaml?query=branch%3Adevelop)

# Lexbox (formerly Language Depot)
Looking for [FieldWorks Lite](backend/FwLite/README.md)?

## Repo Structure

* [backend](backend/README.md) - dotnet API
* [backend/FwLite](backend/FwLite/README.md) - FieldWorks Lite application
* [frontend](frontend/README.md) - SvelteKit app
* hgweb - hgweb Dockerfile and config
* otel - Open Telemetry collector config
* deployment - k8s config for production, staging, develop and local development environments

files related to a specific service should be in a folder named after the service.
There are some exceptions:
* `LexBox.sln` visual studio expects the sln to be at the root of the repo and can make things difficult otherwise

## Development

Summary of setup steps below. See the appropriate file for your operating system for more details:

* [Windows](docs/DEVELOPER-win.md)
* [Linux](docs/DEVELOPER-linux.md)
* [Mac](docs/DEVELOPER-osx.md)

### Prerequisites
 * docker and compose
   * enable Kubernetes in the Docker Desktop settings

### Setup
 * install [Taskfile](https://taskfile.dev/installation/)
 * optionally install [Kustomize](https://kubectl.docs.kubernetes.io/installation/kustomize/)
 * install [Tilt](https://docs.tilt.dev/) and add it to your path (don't forget to read the script before running it)
 * run `tilt version` to check that Tilt is installed correctly
 * clone the repo
 * run `git push` to make sure your GitHub credentials are set up
   * on Windows, allow the Git Credential Manager to log in to GitHub via your browser
   * on Linux, upload your SSH key to your GitHub account if you haven't done so already, then run `git remote set-url --push origin git@github.com:sillsdev/languageforge-lexbox`
 * on Windows, open PowerShell and run `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned`
   * this is necessary before running `task setup` below, which uses a PowerShell script to download seed data
 * run `task setup`, which:
   * initializes a local.env file
   * tells Git to use our ignore revs file
   * checks out Git submodules
   * downloads the FLEx repo for the project seed data

### Kubernetes workflow

```bash
task up
```
The full app will be running at http://localhost after everything starts.
There are some additional urls below to access specific parts of the system.

### Local workflow

#### Prerequisites
- The SvelteKit UI requires: node v20+
- The .NET API requires: dotnet sdk v8+

#### Running the project

There are various ways to run the project. Here are a few suggestions:

**For developing the .NET API**
- `task infra-up` starts all necessary infrastructure in k8s
- `task api:only` starts the api locally

**For developing the SvelteKit UI**
1) In two seperate consoles:
- `task backend-up` starts all necessary infrastructure + the .NET API in k8s
- `task ui:only` starts the ui locally
1) In a shared console:
- `task ui-dev`

The SvelteKit UI will be available at http://localhost:3000.

> [!IMPORTANT]
> The SvelteKit UI is always available in k8s at http://localhost, but will not be reliable unless the entire project is started with `task up`.
> 
**For developing the .NET API and the SvelteKit UI**
- `task infra-up` starts all necessary infrastructure in k8s
- `task api:only` starts the api locally
- `task ui:only` starts the ui locally

**If the k8s deployments are already running**
- `infra-forward` forwards the infrastructure ports for the API
- `backend-forward` forwards the infrastructure + backend ports for the UI

---
### Project urls
* http://localhost - k8s ingress
* http://localhost:3000 - SvelteKit UI
* http://localhost:5158/api - .NET API
* http://localhost:5158/api/swagger - .NET Swagger UI
* http://localhost:5158/api/graphql - GraphQL API
* http://localhost:5158/api/graphql/ui - GraphQL UI
* http://localhost:8088/hg - hg web UI (add the project code and use the url in FLEx to clone)
* http://localhost:1080 - maildev UI
* http://localhost:4810 - pgadmin UI (username admin@test.com, password pass)
* http://localhost:18888 - [aspire dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard) (OTEL traces)

### Seeded data

Once the database is created by the dotnet backend, it will also seed some data in the database.
The following users are available, password for them all is just `pass`:

* admin@test.com: super admin
* manager@test.com: project manager
* editor@test.com: project editor
* user@test.com: user without any projects

There will also be a single project, Sena 3.
There will not be an hg repository however, see optional setup below if this is desired.

---
## Architecture

```mermaid
flowchart TD
    Chorus --> lexbox-api

    subgraph lexbox pod 
        lexbox-api --> otel
    end
    lexbox-api --> hgweb
    lexbox-api --> hgresumable
    subgraph hg pod 
        hgweb
        hgresumable
    end
    hgweb --> hg[hg file system]
    hgresumable --> hg
    lexbox-api --> hg

    ui["ui (sveltekit)"] --> lexbox-api
    lexbox-api ---> db[(postgres)]
```

More info on the frontend and backend can be found in their respective READMEs:
* [frontend](frontend/README.md)
* [backend](backend/README.md)

## Operational environment

### Staging

```mermaid

flowchart LR
    Chorus(["Chorus (e.g. FLEx)"]) -- "https:(hg-staging|resumable-staging)" --- proxy
    Web -- https://staging.languagedepot.org --- proxy([ingress])

    proxy ---|http:5158/api or /hg| api([lexbox-api])
    proxy ---|http:3000| node([sveltekit])

    api -- postgres:5432 --- db([db])
    db -- volume-map:db-data --- data[//var/lib/postgresql/]
  
    api -- http:8088/hg --- hgweb([hgweb])
    hgweb -- /var/hg/repos --- repos
    api -- /hg-repos --- repos

    api -- http:80 --- hgresumable([hgresumable])
    hgresumable -- /var/vcs/public --- repos
    hgresumable -- hgresumable-cache --- cache[//var/cache/hgresume/]

    node <-->|http:5158/api & email| api

    api -- gRPC:4317 --- otel-collector([otel-collector])
    proxy ---|http:4318/traces| otel-collector
    node -- gRPC:4317 --- otel-collector

```

## Monitoring & Analytics

This project is instrumented with OpenTelemetry (OTEL). The exported telemetry data can be viewed in [Honeycomb](https://ui.honeycomb.io/sil-language-forge/).

For your local environment to send traces to Honeycomb, you will need to set the `HONEYCOMB_API_KEY` environment variable in
the `deployment/local-dev/local.env` file.
You can get the key from [here](https://ui.honeycomb.io/sil-language-forge/environments/test/api_keys).

Traces can be accessed directly with a URL like this: [https://ui.honeycomb.io/sil-language-forge/environments/[test|staging|prod]/trace?trace_id=_TRACE_ID\_](https://ui.honeycomb.io/sil-language-forge/environments/test/trace?trace_id=). Yes, bookmark it!

In the application, a trace ID (aka "Error code") shown at the bottom of an error message can be Ctrl+clicked to navigate to the trace in Honeycomb.

![Error example](./docs/img/error-example.png)

## Testing

Run the Playwright tests with `npx playwright test` or `npx playwright test some_test_filter`. You can also use `npx playwright test --ui` to step through individual tests.


## Backup

We're using Kopia to backup the Postgres DB and HG repos to an S3 bucket
Tim E and Kevin H both have access to the credentials. The k8s secret `backups` in prod has everything needed to run a restore job

If you need to restore a backup take a look at this [readme](deployment/restore-scripts/README.md).
