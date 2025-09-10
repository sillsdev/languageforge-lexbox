import type {StorybookConfig} from '@storybook/svelte-vite';

const config: StorybookConfig = {
  "stories": [
    "../src/**/*.mdx",
    "../src/**/*.stories.@(ts|svelte)"
  ],
  "addons": [
    "@storybook/addon-svelte-csf",
    "@chromatic-com/storybook",
    "@storybook/addon-docs",
    "@storybook/addon-a11y",
    "@storybook/addon-vitest"
  ],
  "framework": {
    "name": "@storybook/svelte-vite",
    "options": {}
  },
  async viteFinal(config) {
    config.server = {
      ...config.server,
      // specifying it here instead of the cli prevents the browser from trying to use 0.0.0.0
      host: '0.0.0.0',
    };
    return config;
  },
  experimental_indexers: (existingIndexers) => {
    existingIndexers?.forEach(indexer => {
      const currCreateIndex = indexer.createIndex;
      indexer.createIndex = async (...args) => {
        const entries = await currCreateIndex(...args);
        entries.forEach(entry => {
          // strip "stories/" prefix from titles, because it's an unnecessary nesting level
          entry.title = entry.title?.replace(/^stories\//, '');
        });
        return entries;
      };
    });
    return existingIndexers;
  },
};
export default config;
