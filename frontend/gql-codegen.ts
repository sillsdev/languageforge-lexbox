import type { Options } from 'vite-plugin-graphql-codegen';
import type { TypeScriptPluginConfig } from '@graphql-codegen/typescript/typings/config';

//https://the-guild.dev/graphql/codegen/docs/guides/svelte
//config passed into vite instead of via codegen file, works the same though


const generationConfig: TypeScriptPluginConfig = {
  useTypeImports: true,
  skipTypename: true,
  strictScalars: true,
  avoidOptionals: false,
  scalars: {
    /* eslint-disable @typescript-eslint/naming-convention */
    'numeric': 'number',
    'timestamp': 'string | Date',
    'timestamptz': 'string | Date',
    'jsonb': 'unknown',
    'uuid': 'string',
    'UUID': 'string',
    'DateTime': 'string | Date'
    /* eslint-enable @typescript-eslint/naming-convention */
  }
};
type ConfiguredOutput = NonNullable<Options['config']>['generates'][string];
const schemaPath = 'schema.graphql';
const clientGeneration: Record<string, ConfiguredOutput> = {
  './src/lib/gql/generated/': {
    preset: 'client',
    config: generationConfig,
    plugins: []
  }
};

export const gqlOptions: Options = {
  matchOnSchemas: true,
  config: {
    schema: schemaPath,
    documents: ['src/**/*.{ts,graphql}', '!src/lib/gql/generated/**/*'],
    ignoreNoDocuments: true, // for better experience with the watcher
    // verbose: true,
    generates: {
      ...clientGeneration
    }
  }
};
