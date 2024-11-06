# syntax=docker/dockerfile:1
# TODO: can't use vanilla alpine version since python is needed for gql-codegen stuff.
FROM node:20 AS builder-base
WORKDIR /app
RUN corepack enable

FROM builder-base AS builder-viewer
COPY pnpm-lock.yaml pnpm-workspace.yaml /app/
WORKDIR /app/viewer
COPY viewer/package.json viewer/.npmrc /app/viewer/
RUN --mount=type=cache,target=/root/.local/share/pnpm/store pnpm install

COPY ./viewer /app/viewer
RUN --mount=type=cache,target=/root/.local/share/pnpm/store pnpm run build

FROM builder-base AS builder

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml /app/
COPY viewer/package.json /app/viewer/
RUN --mount=type=cache,target=/root/.local/share/pnpm/store pnpm install

COPY . /app/
COPY src /app/src
COPY static /app/static
COPY --from=builder-viewer /app/viewer/dist-web-component /app/viewer/dist-web-component
ARG APP_VERSION
ENV VITE_APP_VERSION=$APP_VERSION

RUN --mount=type=cache,target=/root/.local/share/pnpm/store pnpm run build

FROM node:20-alpine
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
