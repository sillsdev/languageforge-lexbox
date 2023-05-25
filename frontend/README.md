## Frontend

[Sveltekit](https://kit.svelte.dev) app stack with a [daisyUI](https://daisyui.com) UI library.  Icons are gathered from various sources, see attribution in `/src/lib/icons`.

Requests come into a node app managed by Sveltekit for static, CSR, or SSR.  Data calls are made directly from the client to the dotnet backend via AJAX.  Authn will be handled by a JWT cookie (http-only).

### Development

Node needs to be installed locally. The `pnpm` package manager should be installed: if you don't already have it, run `corepack enable` and it will be set up.

Once you've installed dependencies with `pnpm install`, start a development server:

```bash
pnpm run dev

# or start the server and open the app in a new browser tab
pnpm run dev -- --open
```

The app will be running at [http://localhost:5173](http://localhost:5173) by default.

> In order to configure the robot protection in forms, you'll need to `cp .env.example .env`

#### Building

To create a production version of the app:

```bash
pnpm run build
```

You can preview the production build with `pnpm run preview`.

> To deploy your app, you may need to install an [adapter](https://kit.svelte.dev/docs/adapters) for your target environment.
