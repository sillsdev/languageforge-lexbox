# Lexbox Language Depot replacement

## Repo Structure

* backend - contains dotnet api
* frontend - contains svelte
* hasura - contains hasura metadata

files related to a specific service should be in a folder named after the service.
There are some exceptions:
* `LexBox.sln` visual studio expects the sln to be at the root of the repo and can make things difficult otherwise

Other files, like docker-compose, should be at the root of the repo, because they're related to all services.

## Development

this project contains some seed data. The database will have that data loaded automatically.
The following users are available, password for them all is just `pass`:
* KindLion@test.com: super admin
* InnocentMoth@test.com: project manager
* PlayfulFish@test.com: project editor

There will also be a single project, Sena 3. But the repo needs to be setup, to do that execute `setup.sh` or `setup.bat`.

### Docker workflow

#### Windows

```bash
docker-compose up -d
```
#### Mac

```bash
docker-compose up -d db
```

(watch logs until db is ready)

```bash
docker-compose up -d hasura
```

(watch logs until hasura is ready)

```bash
docker-compose up -d lex-box-api
```

Try some of the helpful urls below to determine whether api is responding or not.

### Local workflow
```bash
docker-compose up -d db hasura
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

Some helpful urls:
* localhost:7075/swagger - swagger docs for the api
* localhost:7075/api/graphiql/ui - graphiql UI
* localhost:7075/api/graphiql - graphiql endpoint
* localhost:8088 - hgkeeper UI add the project code and use the url in FLEx to clone

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
