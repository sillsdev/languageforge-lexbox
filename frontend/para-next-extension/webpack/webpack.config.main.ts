import webpack from 'webpack';
import path from 'path';
import merge from 'webpack-merge';
import CopyPlugin from 'copy-webpack-plugin';
import configBase, { LIBRARY_TYPE, rootDir } from './webpack.config.base';
import WebViewResolveWebpackPlugin from './web-view-resolve-webpack-plugin';
import { outputFolder } from './webpack.util';

/** webpack configuration for building main */
const configMain: webpack.Configuration = merge(configBase, {
  // #region shared with https://github.com/paranext/paranext-core/blob/main/extensions/webpack/webpack.config.main.ts

  // Build for node since Paranext loads this in node https://webpack.js.org/concepts/targets/
  target: 'node',
  // configuration name
  name: 'main',
  // wait until webView bundling finishes - webpack.config.web-view.ts
  dependencies: ['webView'],
  // Instructions on what output to create
  output: {
    // extension output directory
    path: path.resolve(rootDir, outputFolder),
    // Exporting the library https://webpack.js.org/guides/author-libraries/#expose-the-library
    library: {
      type: LIBRARY_TYPE,
    },
    // Empty the output folder before building
    clean: true,
  },
  resolve: {
    plugins: [
      // Get web view files from the temp dir where they are built
      new WebViewResolveWebpackPlugin(),
    ],
  },

  // #endregion

  // extension main source file to build
  // Note: this could have just been the import string if we put the filename in `output`, but
  // splitting it out like this allows us to share `output` with `paranext-core`.
  entry: {
    main: {
      import: './src/main.ts',
      filename: 'fw-lite-extension.js',
    },
  },
  plugins: [
    // Copy static files to the output folder https://webpack.js.org/plugins/copy-webpack-plugin/
    new CopyPlugin({
      patterns: [
        // We want all files from the public folder copied into the output folder
        { from: 'public', to: './' },
        // Copy this extension's type declaration file into the output folder
        { from: 'src/types/fw-lite-extension.d.ts', to: './' },
        // We need to distribute the package.json for Paranext to read the extension properly
        { from: 'package.json', to: './' },
      ],
    }),
  ],
});

export default configMain;
