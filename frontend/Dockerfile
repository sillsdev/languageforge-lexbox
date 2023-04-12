# TODO: can't use vanilla alpine version since python is needed for gql-codegen stuff.  Could consider installing just python if the image size of node is too big
FROM node AS builder

WORKDIR /app

COPY . /app/
COPY src /app/src
COPY static /app/static

RUN npm i && npm run build
