# Docs screenshots

Generates the screenshots embedded in the docs site (`docs/`). The images are written to
`docs/static/img/fw-lite/` and committed, so the site build needs nothing from the viewer.

```bash
# from frontend/viewer; starts the vite dev server and Storybook itself
task docs:screenshots
# one shot only
task docs:screenshots -- -g "entry selected"
```

Each shot is captured twice: `<name>.png` (light) and `<name>-dark.png`. In a doc page:

```mdx
import ThemedImage from '@theme/ThemedImage';
import useBaseUrl from '@docusaurus/useBaseUrl';

<ThemedImage
  alt="An entry open in FieldWorks Lite"
  sources={{
    light: useBaseUrl('/img/fw-lite/project-view-entry.png'),
    dark: useBaseUrl('/img/fw-lite/project-view-entry-dark.png'),
  }}
/>
```

## Sources

- **The app with the in-browser demo project** (`/testing/project-view`): whole-screen shots, the
  same route the UI tests use. No backend, no login.
- **Storybook stories** (`openStory('<story-id>')`): cropped single-component shots. Stories already
  carry realistic fake data, so screens that normally need a server (e.g. the home page project
  list) can still be captured. The story id is the one in the Storybook URL.

## Keeping them fresh

The screenshot names are the contract with the docs: a doc page references `/img/fw-lite/<name>.png`,
so rename a shot only together with the pages that use it. Re-run the task whenever the UI it shows
changes and commit the updated PNGs with the UI change. Fonts, viewport and color scheme are fixed
by the config, so re-runs produce comparable images; tiny anti-aliasing differences between machines
are expected and harmless.

Set `CHROMIUM_PATH` to use a preinstalled Chromium instead of the Playwright-managed one.
