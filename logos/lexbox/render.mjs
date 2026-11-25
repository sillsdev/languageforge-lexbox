// Rebuilds png/ (not committed) and the favicons from the SVGs. After editing a drawing: cd logos && pnpm install && node lexbox/render.mjs
import { render } from '../render-logos.mjs'

render(import.meta.dirname, {
  // Each size range has its own drawing: tiny below 40 px, small 40-95, full 96 and up.
  pngs: {
    'lexbox-16.png': ['lexbox-tiny.svg', 16],
    'lexbox-32.png': ['lexbox-tiny.svg', 32],
    'lexbox-48.png': ['lexbox-small.svg', 48],
    'lexbox-128.png': ['lexbox.svg', 128],
    'lexbox-512.png': ['lexbox.svg', 512],

    'lexbox-appicon-512.png': ['lexbox-appicon.svg', 512],
    'lexbox-appicon-1024.png': ['lexbox-appicon.svg', 1024],
  },
  icos: { 'favicon.ico': ['lexbox-16.png', 'lexbox-32.png', 'lexbox-48.png'] },
  // No dark drawing: the light ones read on dark grounds too.
  faviconSvg: { title: 'Lexbox', light: 'lexbox-tiny.svg' },
})
