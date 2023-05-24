//https://the-guild.dev/graphql/codegen/docs/guides/svelte
//config passed into vite instead of via codegen file, works the same though
import type { TypeScriptPluginConfig } from "@graphql-codegen/typescript/typings/config";
import type { Options } from "vite-plugin-graphql-codegen";

const devSchema: NonNullable<Options["config"]>["schema"] = {
    "http://localhost:5158/api/graphql/schema.graphql": {

    }
};

const generationConfig: TypeScriptPluginConfig = {
    useTypeImports: true,
    skipTypename: true,
    strictScalars: true,
    avoidOptionals: true,
    scalars: {
        "numeric": "number",
        "timestamp": "string | Date",
        "timestamptz": "string | Date",
        "jsonb": "unknown",
        "uuid": "string",
        "UUID": "string",
        "DateTime": "string | Date"
    }
};
type ConfiguredOutput = NonNullable<Options["config"]>["generates"][string];
const schemaPath = "schema.graphql";
const schemaGeneration: Record<string, ConfiguredOutput> = {
    [schemaPath]: {
        plugins: ["schema-ast"]
    }
};
const clientGeneration: Record<string, ConfiguredOutput> = {
    "./src/lib/gql/": {
        preset: "client",
        config: generationConfig,
        plugins: []
    }
};

export const gqlOptions: Options = {
    config: {
        documents: ["src/**/*.{ts,graphql}", "!src/lib/gql/**/*"],
        ignoreNoDocuments: true, // for better experience with the watcher
        // verbose: true,
        generates: {
            ...schemaGeneration,
            ...clientGeneration
        }
    },
    configOverrideWatcher: {
        schema: devSchema
    },
    configOverrideOnStart: {
        schema: devSchema
    },
    configOverrideOnBuild: {
        schema: schemaPath,
        generates: {
            ...clientGeneration
        }
    }
};
