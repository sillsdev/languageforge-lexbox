# syntax=docker/dockerfile:1
# TODO: can't use vanilla alpine version since python is needed for gql-codegen stuff.
FROM node:20 AS builder

RUN corepack enable
WORKDIR /app

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml /app/
COPY viewer/package.json viewer/pnpm-lock.yaml viewer/.npmrc /app/viewer/

RUN --mount=type=cache,target=/root/.local/share/pnpm/store pnpm install

COPY . /app/
COPY src /app/src
COPY static /app/static
ENV DockerDev=true
CMD [ "pnpm", "run", "-r", "--parallel", "--include-workspace-root", "lexbox-dev" ]
