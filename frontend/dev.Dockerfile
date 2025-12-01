# syntax=docker/dockerfile:1
# TODO: can't use vanilla alpine version since python is needed for gql-codegen stuff.
FROM node:20 AS builder

# Version of corepack distributed with node currently (2025-02-04) has a bug that prevents PNPM installation; latest version has the bugfix
RUN npm install -g corepack@latest
RUN corepack enable && corepack prepare pnpm@10.24.0 --activate
WORKDIR /app

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml /app/

RUN --mount=type=cache,target=/root/.local/share/pnpm/store pnpm install

COPY . /app/
COPY src /app/src
COPY static /app/static
ENV DockerDev=true
ENV NODE_OPTIONS="--max-old-space-size=1024"
RUN pnpm svelte-kit sync
CMD [ "pnpm", "run", "-r", "--include-workspace-root", "lexbox-dev" ]
