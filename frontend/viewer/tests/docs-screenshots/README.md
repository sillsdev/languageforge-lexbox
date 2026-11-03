# Docs screenshots

Generates the screenshots in the docs site's FW Lite pages. They're written to
`docs/site/static/img/fw-lite/` and committed, so the site build needs nothing from the viewer.

```bash
# from frontend/viewer; starts the vite dev server and Storybook itself
task docs:screenshots
# one shot only
task docs:screenshots -- -g "word open"
```

Each shot is captured at 2x in light (`<name>.png`) and dark (`<name>-dark.png`). A doc page shows
the pair with `<Screenshot name="<name>" alt="..." />` from `docs/site/src/components/Screenshot`.

## Sources

- **The in-browser demo project** (`/testing/project-view`) for whole-window shots, the same route
  the UI tests use. No backend, no login.
- **Storybook stories** (`openStory(page, '<story-id>')`) for screens that need services the demo
  doesn't have: the home screen (`home/home-view`) and the Sync Changes dialog (`sync/sync-dialog`).

## What is captured

| Shot | Page and section |
| --- | --- |
| `home-logged-out` | Getting started: Try it first, or log in |
| `home-logged-in` | Getting started: Download your project |
| `project-word-open` | Getting started: Open it and edit a word |
| `view-options` | Getting started: the FieldWorks Classic tip |
| `sync-fw-lite-tab`, `sync-lexbox-tab` | How sync works: If someone doesn't see your changes |

Add a shot only where it answers something the text can't easily say; every image is one more
thing to keep current.

## Keeping them fresh

Shot names are the contract with the pages, so rename one only together with the page that uses
it. Re-run the task when the UI a shot shows changes and commit the new PNGs with that change.
Fonts, viewport and color scheme are fixed by the config, so re-runs are comparable; small
anti-aliasing differences between machines are harmless.

Set `CHROMIUM_PATH` to use a preinstalled Chromium instead of the Playwright-managed one.
