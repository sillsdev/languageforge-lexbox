---
title: Local development
sidebar_position: 0
---

Two ways to run Lexbox locally: everything in Kubernetes via Tilt, or infrastructure in Kubernetes with the API and/or UI running directly on your machine.

Full detail lives in the [repo README](https://github.com/sillsdev/languageforge-lexbox/blob/develop/README.md); this page is the short version.

## Prerequisites

* Docker and Compose, with Kubernetes enabled in the Docker Desktop settings
* [Taskfile](https://taskfile.dev/installation/) and [Tilt](https://docs.tilt.dev/) on your PATH (check with `tilt version`)
* Node v20+ (SvelteKit UI) and the .NET SDK (API) if you want to run either directly
* Optionally [Kustomize](https://kubectl.docs.kubernetes.io/installation/kustomize/)

OS-specific steps (hosts file entries, install commands, quirks):

* [Windows setup](./setup-windows.md)
* [Linux setup](./setup-linux.md)
* [macOS setup](./setup-macos.md)

Then run `git push` once to confirm your GitHub credentials work, and `task setup`, which initializes `local.env`, points Git at the ignore-revs file, and downloads the FLEx seed-data repo.

## Kubernetes workflow

```bash
task up
```

The whole app comes up at http://localhost.

## Running services directly

| Goal | Commands |
|---|---|
| Develop the .NET API | `task infra-up`, then `task api:only` |
| Develop the SvelteKit UI | `task backend-up`, then `task ui:only` (or `task ui-dev` in one console) |
| Develop both | `task infra-up`, then `task api:only` and `task ui:only` |
| K8s already running | `task infra-forward` (API deps) or `task backend-forward` (UI deps) |

The UI running directly is at http://localhost:3000. The k8s-hosted UI at http://localhost is always there, but is only reliable when the whole project was started with `task up`.

## Service URLs

| URL | Service |
|---|---|
| http://localhost | k8s ingress |
| http://localhost:3000 | SvelteKit UI |
| http://localhost:5158/api | .NET API |
| http://localhost:5158/api/swagger | Swagger UI |
| http://localhost:5158/api/graphql | GraphQL API |
| http://localhost:5158/api/graphql/ui | GraphQL UI |
| http://localhost:8088/hg | hgweb (append the project code and use the URL in FLEx to clone) |
| http://localhost:1080 | maildev UI |
| http://localhost:4810 | pgadmin UI (`admin@test.com` / `pass`) |
| http://localhost:18888 | [Aspire dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard) (OTEL traces) |

## Seeded logins

The backend seeds these users when it creates the database. The password for all of them is `pass`.

| User | Role |
|---|---|
| admin@test.com | super admin |
| manager@test.com | project manager |
| editor@test.com | project editor |
| user@test.com | user with no projects |

One project, Sena 3, is seeded too. It has no hg repository unless you do the optional setup in the README.
