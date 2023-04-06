# https://github.com/hasura/graphql-engine
FROM hasura/graphql-engine:v2.20.0-ce.cli-migrations-v3

ENV HASURA_GRAPHQL_ENABLE_TELEMETRY=${HASURA_GRAPHQL_ENABLE_TELEMETRY:-false}
ENV HASURA_GRAPHQL_ENABLED_LOG_TYPES=${HASURA_GRAPHQL_ENABLED_LOG_TYPES:-"startup, http-log, webhook-log, websocket-log, query-log"}
ENV HASURA_GRAPHQL_EXPERIMENTAL_FEATURES=${HASURA_GRAPHQL_EXPERIMENTAL_FEATURES:-naming_convention}
ENV HASURA_GRAPHQL_DEFAULT_NAMING_CONVENTION=${HASURA_GRAPHQL_DEFAULT_NAMING_CONVENTION:-graphql-default}

COPY ./metadata /hasura-metadata
