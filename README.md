# Lexbox Language Depot replacement

## Repo Structure

* backend - contains dotnet api
* frontend - contains svelte
* hasura - contains hasura metadata
* hgweb - contains hgweb Dockerfile and config
* otel - contains open telemitry collector config
* deployment - contains k8s config for staging and prod

files related to a specific service should be in a folder named after the service.
There are some exceptions:
* `LexBox.sln` visual studio expects the sln to be at the root of the repo and can make things difficult otherwise

Other files, like `docker-compose.yaml`, should be at the root of the repo, because they're related to all services.

## Development

### Prerequisites
 * docker and compose
#### for local dev also:
 * node version 18+
 * dotnet 7 sdk

### Docker workflow

```bash
docker compose up -d proxy
```
You might run into some issues with ports already being used, you can change the ports in the `docker-compose.yaml` file if you need to.
The full app will be running on http://localhost after everything starts.
There are some additional urls below to access specific parts of the system.

### Local workflow
```bash
docker compose up -d db hasura otel-collector maildev
```
then you will want to execute in 2 separate consoles:

frontend
```bash
cd frontend
npm run dev
```
backend
```bash
cd backend/LexBoxApi
dotnet watch
```
---
### Helpful urls
* http://localhost:5173 - sveltekit frontend
* http://localhost:5158/api/swagger - swagger docs for the api
* http://localhost:5158/api/graphql/ui - graphiql UI
* http://localhost:5158/api/graphql - graphiql endpoint
* http://localhost:8088/hg - hg web UI add the project code and use the url in FLEx to clone
* http://localhost:1080 - maildev UI

### Seeded data

Once the database is created by the dotnet backend, it will also seed some data in the database.
The following users are available, password for them all is just `pass`:

* admin@test.com: super admin
* manager@test.com: project manager
* editor@test.com: project editor

There will also be a single project, Sena 3.
There will not be an hg repository however, see optional setup below if this is desired.

### Optional setup

If you want to test Send & Recieve execute `setup.sh` or `setup.bat`,
to create the Sena 3 repo for the seed project.

If you want to test out Honeycomb traces, you will need to set the `HONEYCOMB_API_KEY` environment variable in
the `.env` file.
You can get the key from [here](https://ui.honeycomb.io/sil-language-forge/environments/test/api_keys)

---
### Hasura workflow
In order to modify Hasura table relations and permissions in hasura we need to use the hasura console.
We first will need to install the hasura cli from [here](https://hasura.io/docs/latest/hasura-cli/install-hasura-cli/) and add it to your path.

Next we need to run the following command from the root of the repo:
```bash
hasura console --project hasura
```
This should open a window in the browser. You will need hasura running in docker for this to work.
Once you make some changes in the console you should notice some metadata under `hasura/metadata` has been updated, you will want to check that in to git.

##### Hasura troubleshooting

Sometimes Hasura can get out of sync with the database.
To troubleshoot this you should open the hasura console at `localhost:8081` and navigate to settings > metadata status.
If you see some errors there try reloading your metadata on the Metadata Actions tab.
It may be that dotnet did not apply migrations yet, 
so you might try restarting dotnet and wait for it to update the database schema.
Then come back and reload the metadata again.

### Proxy Diagram

Development:
```mermaid
graph TD
    Chorus --> Proxy

    Proxy[Proxy] --> Api
    Proxy --> hg-keeper
    Proxy --> hgresumable
    hg-keeper --> hg[hg file system]
    hgresumable --> hg
    Api --> hg

    Frontend --> Api
    Api --> Hasura[hasura]
    Api --> db
    Hasura --> db[postgres]
```

Production:
```mermaid
graph TD
    Chorus --> Api

    Api --> hg-keeper
    Api --> hgresumable
    hg-keeper --> hg[hg file system]
    hgresumable --> hg
    Api[API & Proxy] --> hg

    Frontend --> Api
    Api --> Hasura[hasura]
    Api --> db
    Hasura --> db[postgres]
```

More info on the frontend and backend can be found in their respective READMEs:
* [frontend](frontend/README.md)
* [backend](backend/README.md)

## Operational environment

### Staging

```mermaid

flowchart LR
    FLEx -- "https:(hg-public-qa|hg-private-qa|admin-qa|resumable-qa)" --- proxy
    Web -- https://staging.languagedepot.org --- proxy([ingress])

    proxy -- http:80/api --- api([lexbox-api])
    proxy -- http:3000 --- node([sveltekit])

    api -- postgres:5432 --- db([db])
    db -- volume-map:db-data --- data[//var/lib/postgresql/]

    api -- http:8080 --- hasura([hasura])
    hasura -- postgres:5432 --- db
    hasura -- volume-map --- metadata[//hasura-metadata/]

    api -- http:8080 --- hgkeeper([hgkeeper])
    hgkeeper -- volume-map:hg-repos --- repos[//repos/]
    api -- volume-map:hg-repos --- hg-repos[//hg-repos/]

    api -- http:8080 --- hgresume([hgresume])
    hgresume -- volume-map:hgresume-cache --- cache[//var/cache/hgresume/]

    node -- http:80/api --- api

    api -- gRPC:4317 --- otel-collector([otel-collector])
    proxy -- http:4318/traces --- otel-collector
    node -- gRPC:4317 --- otel-collector

```

## Analytics

This project is instrumented with OpenTelemetry (OTEL). The exported telemetry data can be viewed in [Honeycomb](https://ui.honeycomb.io/sil-language-forge/).
