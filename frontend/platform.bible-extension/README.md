# fw-lite-extension

Fully function 'hello world' extension template for Paranext

## Summary

This is a webpack project template pre-configured to build Paranext extensions. It contains a fully functional extension that can be used as learning material and inspiration when building your first extension. Should you already be familiar with developing extension, we suggest using the slimmed down [`fw-lite-extension`](https://github.com/paranext/fw-lite-extension).

- `package.json` contains information about this extension's npm package. It is required for Paranext to use the extension properly. It is copied into the build folder
- `src` contains the source code for the extension
  - `src/main.ts` is the main entry file for the extension
  - `src/types/fw-lite-extension.d.ts` is this extension's types file that defines how other extensions can use this extension through the `papi`. It is copied into the build folder
  - `*.web-view.tsx` files will be treated as React WebViews
  - `*.web-view.html` files are a conventional way to provide HTML WebViews (no special functionality)
- `public` contains static files that are copied into the build folder
  - `public/manifest.json` is the manifest file that defines the extension and important properties for Paranext
  - `public/package.json` defines the npm package for this extension and is required for Paranext to use it appropriately
  - `public/assets` contains asset files the extension and its WebViews can retrieve using the `papi-extension:` protocol
- `dist` is a generated folder containing your built extension files
- `release` is a generated folder containing a zip of your built extension files

## To install

### Configure paths to `paranext-core` repo

In order to interact with `paranext-core`, you must point `package.json` to your installed `paranext-core` repository:

1. Follow the instructions to install [`paranext-core`](https://github.com/paranext/paranext-core#developer-install). We recommend you clone `paranext-core` in the same parent directory in which you cloned this repository so you do not have to reconfigure paths to `paranext-core`.
2. If you cloned `paranext-core` anywhere other than in the same parent directory in which you cloned this repository, update the paths to `paranext-core` in this repository's `package.json` to point to the correct `paranext-core` directory.

### Install dependencies

Run `npm install` to install local and published dependencies

### Configure extension details

This section is a more compact version of the [`Your first extension` guide](https://github.com/paranext/fw-lite-extension/wiki/Your-First-Extension).

#### Search and replace placeholders

- **Search for:** fw-lite-extension
  **Replace with:** your-extension-name
- **Search for:** FW Lite Extension
  **Replace with:** Your Extension
  (Be sure to match case)

#### Filenames

You need to change the filename of the `.d.ts` file, which is located in `/src/types` and referenced in the `package.json` “types” field. See more information on the [.d.ts files](https://github.com/paranext/fw-lite-extension/wiki/Extension-Anatomy#type-declaration-files-dts).

#### Manifest

The `manifest.json` and `package.json` files makeup your extension manifest. Add your details in these two files based on your extension name and what you renamed the files described in 1 and 2. See more information on the `manifest.json` and `package.json` files in [Extension Anatomy](https://github.com/paranext/fw-lite-extension/wiki/Extension-Anatomy#extension-manifest).

#### Webpack

You will need to add your extension's name into `webpack.config.main.ts` and `webpack.util.ts`. The search and replace actions listed above will correct this for you.

## To run

### Running Paranext with your extension

To run Paranext with your extension:

`npm start`

Note: The built extension will be in the `dist` folder. In order for Paranext to run your extension, you must provide the directory to your built extension to Paranext via a command-line argument. This command-line argument is already provided in this `package.json`'s `start` script. If you want to start Paranext and use your extension any other way, you must provide this command-line argument or put the `dist` folder into Paranext's `extensions` folder.

### Building your extension independently

To watch extension files (in `src`) for changes:

`npm run watch`

To build the extension once:

`npm run build`

## To package for distribution

To package your extension into a zip file for distribution:

`npm run package`

## To update

The `fw-lite-extension` will be updated regularly, and will sometimes receive updates that help with breaking changes on `paranext-core`. So we recommend you periodically update your extension by merging the latest template updates into your extension. You can do so by following [these instructions](https://github.com/paranext/paranext-extension-template/wiki/Merging-Template-Changes-into-Your-Extension).

## Special features of the template

This template has special features and specific configuration to make building an extension for Paranext easier. Following are a few important notes:

### React WebView files - `.web-view.tsx`

Paranext WebViews must be treated differently than other code, so this template makes doing that simpler:

- WebView code must be bundled and can only import specific packages provided by Paranext (see `externals` in `webpack.config.base.ts`), so this template bundles React WebViews before bundling the main extension file to support this requirement. The template discovers and bundles files that end with `.web-view.tsx` in this way.
  - Note: while watching for changes, if you add a new `.web-view.tsx` file, you must either restart webpack or make a nominal change and save in an existing `.web-view.tsx` file for webpack to discover and bundle this new file.
- WebView code and styles must be provided to the `papi` as strings, so you can import WebView files with [`?inline`](#special-imports) after the file path to import the file as a string.

### Special imports

- Adding `?inline` to the end of a file import causes that file to be imported as a string after being transformed by webpack loaders but before bundling dependencies (except if that file is a React WebView file, in which case dependencies will be bundled). The contents of the file will be on the file's default export.
  - Ex: `import myFile from './file-path?inline`
- Adding `?raw` to the end of a file import treats a file the same way as `?inline` except that it will be imported directly without being transformed by webpack.

### Misc features

- Paranext extension code must be bundled all together in one file, so webpack bundles all the code together into one main extension file.
- Paranext extensions can interact with other extensions, but they cannot import and export like in a normal Node environment. Instead, they interact through the `papi`. As such, the `src/types` folder contains this extension's declarations file that tells other extensions how to interact with it through the `papi`.

### Two-step webpack build

This extension template is built by webpack (`webpack.config.ts`) in two steps: a WebView bundling step and a main bundling step:

#### Build 1: TypeScript WebView bundling

Webpack (`./webpack/webpack.config.web-view.ts`) prepares TypeScript WebViews for use and outputs them into temporary build folders adjacent to the WebView files:

- Formats WebViews to match how they should look to work in Paranext
- Transpiles React/TypeScript WebViews into JavaScript
- Bundles dependencies into the WebViews
- Embeds Sourcemaps into the WebViews inline

#### Build 2: Main and final bundling

Webpack (`./webpack/webpack.config.main.ts`) prepares the main extension file and bundles the extension together into the `dist` folder:

- Transpiles the main TypeScript file and its imported modules into JavaScript
- Injects the bundled WebViews into the main file
- Bundles dependencies into the main file
- Embeds Sourcemaps into the file inline
- Packages everything up into an extension folder `dist`
