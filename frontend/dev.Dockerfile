# TODO: can't use vanilla alpine version since python is needed for gql-codegen stuff.
FROM node:18 AS builder

WORKDIR /app

COPY package.json pnpm-lock.yaml /app/

RUN corepack enable
RUN pnpm install

COPY . /app/
COPY src /app/src
COPY static /app/static

CMD [ "pnpm", "run", "dev", "--", "--port", "3000" ]
