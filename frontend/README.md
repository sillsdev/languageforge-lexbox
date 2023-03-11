## Frontend

[Sveltekit](https://kit.svelte.dev) app stack with a [daisyUI](https://daisyui.com) UI library.  Icons are gathered from various sources, see attribution in `/src/lib/icons`.

Requests come into a node app managed by Sveltekit for static, CSR, or SSR.  Data calls are made directly from the client to the dotnet backend via AJAX.  Authn will be handled by a JWT cookie (http-only).

### Development

Node needs to be installed locally.

Once you've installed dependencies with `npm install`, start a development server:

```bash
npm run dev

# or start the server and open the app in a new browser tab
npm run dev -- --open
```

The app will be running at [http://localhost:5173](http://localhost:5173) by default.

> In order to configure the robot protection in forms, you'll need to `cp .env.example .env`

#### Building

To create a production version of the app:

```bash
npm run build
```

You can preview the production build with `npm run preview`.

> To deploy your app, you may need to install an [adapter](https://kit.svelte.dev/docs/adapters) for your target environment.
