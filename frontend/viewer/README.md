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
this will update the files under `/src/locales/`
if you want you can then feed those files to an AI and it'll translate them for you.


#### Adding a new langauge
The `/src/locales/` folder contains one file per language, named appropriately using its language code e.g. es.json for Spanish

To add a new language for localization, copy the existing en.json file in the folder above and name it for the new language.  Ask AI to translate the "translation" strings into the language you want, and then commit those changes.
You also need to update the `frontend/viewer/lingui.config.ts` with the additional language code

#### advanced usage

for formatted values you can do this:
```sveltehtml
<span>{$t`Hello ${name}, how are you today?`}</span>
```

### ShadCN

add a new component
```bash
pnpx shadcn-svelte@next add context-menu
```
