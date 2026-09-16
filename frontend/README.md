## Frontend

[Sveltekit](https://kit.svelte.dev) app stack with a [daisyUI](https://daisyui.com) UI library.  Icons are gathered from various sources, see attribution in `/src/lib/icons`.

Requests come into a node app managed by Sveltekit for static, CSR, or SSR.  Data calls are made directly from the client to the dotnet backend via AJAX.  Authn will be handled by a JWT cookie (http-only).

### Development

Node needs to be installed locally. The `pnpm` package manager should be installed: see [pnpm.io/installation](https://pnpm.io/installation).

Once you've installed dependencies with `pnpm install`, start a development server:

```bash
pnpm run dev

# or start the server and open the app in a new browser tab
pnpm run dev -- --open
```

The app will be running at [http://localhost:3000](http://localhost:3000) by default.

#### Building

To create a production version of the app:

```bash
pnpm run build
```

You can preview the production build with `pnpm run preview`.

> To deploy your app, you may need to install an [adapter](https://kit.svelte.dev/docs/adapters) for your target environment.

#### Bundle analysis

Set `ANALYZE=1` when building to emit [vite-bundle-analyzer](https://github.com/nonzzz/vite-bundle-analyzer) reports (gzip sizes). Use the Vite plugin in `vite.config.ts`, not `pnpm dlx vite-bundle-analyzer`: SvelteKit builds both client and server, and the CLI report is easy to lose under the Node adapter output.

```bash
ANALYZE=1 pnpm run build
```

PowerShell:

```powershell
$env:ANALYZE='1'; pnpm run build
```

Open the **client** HTML treemap (or the JSON) — ignore the server reports, which include email/mjml:

- `.svelte-kit/output/client/bundle-stats.html`
- `.svelte-kit/output/client/bundle-stats.json`

The adapter also copies those files to `build/client/`.

#### Testing

To run an end-to-end test in the frontend folder:

```bash
pnpm test
```

#### Linting

Linting depends partially on generated code, so first:

```bash
pnpm run -r build
```

And then:
```bash
pnpm run -r lint
```
