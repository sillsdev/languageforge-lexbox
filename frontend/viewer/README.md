Typical development workflow should be to run `task fw-lite-web` in the repo root.

### i18n 

We're using a different library here from the lexbox UI. The core lib is https://lingui.dev/
with a Svelte plugin https://github.com/HenryLie/svelte-i18n-lingui

Basic usage looks like this:
```sveltehtml
<span>{$t`Logout`}</span>
```
for the english text 'Logout'. Then to make localization files run:
```bash
pnpm run i18n:extract
```
This will update the files under `/src/locales/`. If you want, you can then feed those files to an AI and it will translate them for you.

#### Adding a new language
The `/src/locales/` folder contains one file per language, named using its language code (e.g. `es.json` for Spanish).
To add a new language, copy `en.json`, rename it, translate the `"translation"` fields, and commit the changes.
You must also update `frontend/viewer/lingui.config.ts` with the additional language code.

#### Advanced Usage

for formatted values you can do this:
```sveltehtml
<span>{$t`Hello ${name}, how are you today?`}</span>
```

### ShadCN

add a new component
```bash
pnpx shadcn-svelte@next add context-menu
```
