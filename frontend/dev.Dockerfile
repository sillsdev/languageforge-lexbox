# syntax=docker/dockerfile:1
# TODO: can't use vanilla alpine version since python is needed for gql-codegen stuff.
FROM node:26 AS builder

RUN npm install -g pnpm@10.24.0
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
