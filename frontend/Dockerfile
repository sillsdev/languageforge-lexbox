# syntax=docker/dockerfile:1
# TODO: can't use vanilla alpine version since python is needed for gql-codegen stuff.
FROM node:26 AS builder-base
WORKDIR /app
RUN npm install -g pnpm@10.24.0

FROM builder-base AS builder

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml /app/
RUN --mount=type=cache,target=/root/.local/share/pnpm/store pnpm install --frozen-lockfile

COPY . /app/
COPY src /app/src
COPY static /app/static
ARG APP_VERSION
ENV VITE_APP_VERSION=$APP_VERSION

RUN --mount=type=cache,target=/root/.local/share/pnpm/store pnpm run build
RUN node scripts/check-runtime-deps.mjs

# Drop devDependencies from node_modules so the runtime image ships only prod deps.
# adapter-node bundles devDeps (svelte, kit, vite, etc.) into build/; the packages it leaves
# external — and thus reads from node_modules at runtime — are all in "dependencies", so pruning
# devDeps is safe. This is the copy the runtime stage below picks up.
# CI=true lets pnpm prune run non-interactively (it otherwise aborts without a TTY).
# --ignore-scripts skips the "prepare" hook (svelte-kit sync), which would fail here since
# svelte-kit is a devDep being removed and the build already ran.
RUN --mount=type=cache,target=/root/.local/share/pnpm/store CI=true pnpm prune --prod --ignore-scripts

FROM node:26-alpine
# container would not receive SIGTERM from docker when shutting down so Docker would force kill
# the container after 10s.  This will make shutdown faster because the SIGTERM will be handled appropriately.
# https://maximorlov.com/process-signals-inside-docker-containers/
RUN apk update && apk add tini

COPY --from=builder /app/build /app/
COPY --from=builder /app/node_modules /app/node_modules
COPY --from=builder /app/package.json /app/

EXPOSE 3000

ENTRYPOINT ["tini", "--"]

CMD ["node", "/app/index.js"]
