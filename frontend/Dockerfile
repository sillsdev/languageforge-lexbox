# TODO: can't use vanilla alpine version since python is needed for gql-codegen stuff.  Could consider installing just python if the image size of node is too big
FROM node AS builder

WORKDIR /app

COPY .eslintrc.cjs \
	 .prettierrc \
	 gql-codegen.ts schema.graphql \
	 package-lock.json package.json \
	 postcss.config.cjs \
	 svelte.config.js \
	 tailwind.config.cjs \
	 tsconfig.json \
	 vite.config.ts \
	 /app/
COPY src /app/src
COPY static /app/static

RUN npm i && npm run build
