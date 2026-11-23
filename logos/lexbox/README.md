# Lexbox logo

A wooden crate with a blue side carrying the Thai letter ล and a rack of four word tiles: the blue tile with the folded corner is
the word. Part of the SIL lexical tools logo family (FLEx, FieldWorks Lite, Lexbox, The Combine, Webonary).

**Start with `index.html`.** Open it in any browser: it shows every file at its real size, which file to use where, mock-ups,
the colours, and a download of every file. It shows the files in this folder, so it stays current when a drawing changes.

## Which file

The logo is drawn three times, from detailed to simple. Pick the drawing by the size it appears at on screen. The same files go
on light and dark grounds (dark mode included): there is no dark version.

| On screen | File |
|---|---|
| 96 px and up (login page, documents, slides, emails) | `lexbox.svg` |
| 40 to 95 px (cards, headers, project lists) | `lexbox-small.svg` |
| 16 to 39 px (browser tabs, toolbars, menus, inline) | `lexbox-tiny.svg` |

| For | File |
|---|---|
| One colour on a coloured bar or in print (the app's top bar uses it in white) | `lexbox-onecolor.svg`: inline in a web page it takes the text colour (`currentColor`); as an `<img>` it is black |
| Vinyl, embroidery, engraving, stamps | `lexbox-mono.svg` (black and white). Its lines are about 1.2% of its width, so use it at least 45 mm wide |
| An app icon shown as it is (web pages, documents) | `lexbox-appicon.svg`. The blue behind the crate is the app's blue, as an example; the crate fills 78% of the icon's width |

The SVGs have no built-in size: give the `<img>` a width and height, or size it in CSS.

Website favicon: put `favicon.svg` and `favicon.ico` in the site root and link both. The ICO covers older browsers.

```html
<link rel="icon" href="/favicon.ico" sizes="16x16 32x32 48x48">
<link rel="icon" href="/favicon.svg" type="image/svg+xml">
```

The ICO holds the tiny drawing at 16 and 32 px and the small drawing at 48. The SVG always shows the tiny drawing.

For places that don't take SVG, `render.mjs` writes PNGs to `png/`: 16, 32, 48, 128 and 512 px (each from the right drawing) and the
app icon at 512 and 1024. `png/` isn't committed; to commit a PNG something uses, add a `!` line for it to
`logos/.gitignore`. The script also rebuilds the favicons, so run it after editing a drawing, and add a row to its table for a new size.

```sh
cd logos
pnpm install
node lexbox/render.mjs
```

It only re-renders. A change to one drawing usually has to be repeated by hand in the other sizes.

## Spacing

The files have almost no margin (2% of the box), so the logo lines up with whatever is beside it. Leave a little room round the
mark yourself, about a sixth of its width.

## Colours

| Colour | |
|---|---|
| `#27aae1` | azure: the blue side and the word tile |
| `#1e81ab` | deep azure: their edges |
| `#F6EFE3` | cream: the other three tiles |
| `#cfc4ae` | sand: the cream tiles' edges |
| `#1f2a44` | navy: tile outlines and marks |
| `#ffffff` | white: the ล |
| `#b56c19` `#c2782a` `#7c4b1e` | oak, light oak, dark oak: the lid |

## Please don't

- Shrink the full drawing below 96 px, or use any drawing below 16 px.
- Recolour the tiles or the crate, or add outlines, shadows, glows or gradients.
- Stretch, rotate or crop the crate.
- Use the ล or the blue tile on their own as a logo.
- Redraw it from a screenshot: download the file instead.

The logo is not under this repository's MIT licence: see [`../LICENSE.md`](../LICENSE.md).
