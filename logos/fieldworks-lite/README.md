# FieldWorks Lite logo

A navy dictionary with a thumb index: the coral tab is the word you are looking up, and the ล on the cover is shared with Lexbox.
Part of the SIL lexical tools logo family (FLEx, FieldWorks Lite, Lexbox, The Combine, Webonary).

**Start with `index.html`.** Open it in any browser: it shows every file at its real size, which file to use where, mock-ups,
the colours, and a download of the whole set. It shows the files in this folder, so it stays current when a drawing changes.

## Which file

The logo is drawn four times, from detailed to simple. Pick the drawing by the size it appears at on screen. Use the standard
drawing wherever it works; the `-reversed` file is the fallback for grounds it can't sit on: navy, dark, or a photo darker than mid-grey.

| On screen | Light grounds | Navy or dark grounds |
|---|---|---|
| 96 px and up (slides, documents, About screens, web pages) | `fieldworks-lite.svg` | `fieldworks-lite-reversed.svg` |
| 48 to 95 px (cards, project lists, splash screens) | `fieldworks-lite-small.svg` | `fieldworks-lite-small-reversed.svg` |
| 24 to 47 px (toolbars, tab bars, app headers, menus) | `fieldworks-lite-tiny.svg` | `fieldworks-lite-tiny-reversed.svg` |
| 16 to 23 px (inline, lists) | `fieldworks-lite-micro.svg` | `fieldworks-lite-micro-reversed.svg` |

| For | File |
|---|---|
| App stores and iOS | `fieldworks-lite-appicon-square.svg` (a full square with no transparency; the platform rounds the corners) |
| macOS | `fieldworks-lite-appicon-macos.svg` (Apple's icon grid, margin included) |
| An app icon shown as it is (web pages, documents) | `fieldworks-lite-appicon.svg` |
| Anything cropped to a circle (Android launcher layers, profile pictures) | `fieldworks-lite-circle.svg`; `fieldworks-lite-onecolor-circle.svg` for Android themed icons; `fieldworks-lite-reversed-circle.svg` only where the circle must be dark. As an Android launcher layer, put the book on white at 85% of the visible icon, moved halfway from the circle centring to the book's box centre (viewBox `2.875 1.8125 256 256`) |
| Windows and Linux taskbars | the drawing for the size, no box: `-tiny` at 24 to 32 px, `-small` at 48, the full drawing at 256; `-reversed` on a dark taskbar |
| Print in one ink | `fieldworks-lite-onecolor.svg` (any single colour; inline in a web page it takes the text colour) |
| Vinyl, embroidery, engraving, stamps | `fieldworks-lite-mono.svg` (black lines on white). Its lines are 1% of its width, so use it large: at 25 mm wide they are 0.26 mm |

Website favicon: put `favicon.svg` and `favicon.ico` in the site root and link both. The SVG switches to the dark drawing
when the browser is in dark mode; the ICO covers older browsers.

```html
<link rel="icon" href="/favicon.ico" sizes="16x16 32x32 48x48">
<link rel="icon" href="/favicon.svg" type="image/svg+xml">
```

The ICO holds the smallest drawing at 16 px and the tiny drawing (with the ล) at 32 and 48, but has no dark version. The SVG
always shows the smallest drawing, so on high-density screens browsers that use it show the plain book where the ICO would show
the ล. That is on purpose: Chrome and Edge draw every SVG favicon at 32 px, even on ordinary screens, and then shrink it to 16,
so an SVG that switched to the tiny drawing at 32 px would put the ล into 16 px there too. Tested in Chrome, Edge and Firefox
(not Safari), November 2026.

For places that don't take SVG, `render.mjs` writes PNGs to `png/`: light and reversed at 16, 32, 48, 128 and 512 px (each from the
right drawing), the app icons (the store one with no alpha channel, which App Store Connect requires), and the circle files at 512.
`png/` isn't committed; to commit a PNG something uses, add a `!` line for it to `logos/.gitignore`. The script also rebuilds the
favicons, so run it after editing a drawing, and add a row to its table for a new size.

```sh
cd logos
pnpm install
node fieldworks-lite/render.mjs
```

It only re-renders. A change to one drawing usually has to be repeated by hand in the other sizes and in the `-reversed` files.

## Spacing

The files have almost no margin (2% of the box), so the logo lines up with whatever is beside it. Leave clear space yourself: keep
other things at least one tab's height away from the book (about a sixth of the icon's box).

## Colours

| Light grounds | | Navy and dark grounds | |
|---|---|---|---|
| `#1f2a44` | navy: cover, outlines, page edge | `#3b4e7c` | night navy: cover |
| `#0e1425` | deep navy: spine | `#2a3c6c` | deep blue: spine |
| `#EE5D50` | coral: the word tab | `#5b70a1` | dusk: the ล |
| `#ffd864` `#7fc0fa` `#8cd793` | yellow, blue, green tabs | `#536897` | mid blue: tab edges |
| `#475470` | slate: the ล | `#9fb1d8` | light blue: edge round the cover, back cover edge |
| `#FBF7EE` | cream: pages | | |

The word tab casts a shadow of black at 18% opacity on light grounds.

## Please don't

- Shrink the full drawing below 96 px, or use any drawing below 16 px.
- Put a light-ground file on navy or dark grounds.
- Recolour it, swap the tab colours, or add shadows, glows or gradients.
- Straighten, rotate, stretch or crop the book: the slight turn is part of the logo.
- Use the ล or the coral tab on their own as a logo.
- Redraw it from a screenshot: download the file instead.

The logo is not under this repository's MIT licence: see [`../LICENSE.md`](../LICENSE.md).
