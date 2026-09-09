# syntax=docker/dockerfile:1
# TODO: can't use vanilla alpine version since python is needed for gql-codegen stuff.
FROM node:26 AS builder

RUN npm install -g pnpm@12.3.4
WORKDIR /app

# Project-local virtual store so node_modules/.pnpm lands in the image
# (global store is cache-mount-only and would dangle at runtime).
# PNPM_CONFIG_* is required: virtualStoreType is an enum and is validated
# before ${...} interpolation in pnpm-workspace.yaml.
ENV PNPM_CONFIG_VIRTUAL_STORE_TYPE=project

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml /app/

RUN --mount=type=cache,target=/root/.local/share/pnpm/store pnpm install

COPY . /app/
COPY src /app/src
COPY static /app/static
ENV DockerDev=true
ENV NODE_OPTIONS="--max-old-space-size=1024"
RUN pnpm svelte-kit sync
CMD [ "pnpm", "run", "-r", "--include-workspace-root", "lexbox-dev" ]
