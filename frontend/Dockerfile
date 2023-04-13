# TODO: can't use vanilla alpine version since python is needed for gql-codegen stuff.  Could consider installing just python if the image size of node is too big
FROM node AS builder

WORKDIR /app

COPY . /app/
COPY src /app/src
COPY static /app/static

RUN npm i && npm run build

FROM node

COPY --from=builder /app/build /app/
COPY --from=builder /app/node_modules /app/node_modules
COPY --from=builder /app/package.json /app/

EXPOSE 3000

# container would not receive SIGTERM from docker when shutting down so Docker would force kill
# the container after 10s.  This will make shutdown faster because the SIGTERM will be handled appropriately.
# https://maximorlov.com/process-signals-inside-docker-containers/
RUN apt-get update && apt-get install tini
ENTRYPOINT ["tini", "--"]

CMD ["node", "/app/index.js"]
