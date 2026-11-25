// Rebuilds png/, the favicons and the copies elsewhere in the repo from the SVGs. After editing a drawing: cd logos && pnpm install && node fieldworks-lite/render.mjs
import { render } from '../render-logos.mjs'

render(import.meta.dirname, {
  // Each size range has its own drawing: micro below 24 px, tiny 24-47, small 48-95, full 96 and up.
  pngs: {
    'fieldworks-lite-16.png': ['fieldworks-lite-micro.svg', 16],
    'fieldworks-lite-24.png': ['fieldworks-lite-tiny.svg', 24],
    'fieldworks-lite-32.png': ['fieldworks-lite-tiny.svg', 32],
    'fieldworks-lite-48.png': ['fieldworks-lite-small.svg', 48],
    'fieldworks-lite-128.png': ['fieldworks-lite.svg', 128],
    'fieldworks-lite-256.png': ['fieldworks-lite.svg', 256],
    'fieldworks-lite-512.png': ['fieldworks-lite.svg', 512],

    'fieldworks-lite-reversed-16.png': ['fieldworks-lite-micro-reversed.svg', 16],
    'fieldworks-lite-reversed-24.png': ['fieldworks-lite-tiny-reversed.svg', 24],
    'fieldworks-lite-reversed-32.png': ['fieldworks-lite-tiny-reversed.svg', 32],
    'fieldworks-lite-reversed-48.png': ['fieldworks-lite-small-reversed.svg', 48],
    'fieldworks-lite-reversed-128.png': ['fieldworks-lite-reversed.svg', 128],
    'fieldworks-lite-reversed-256.png': ['fieldworks-lite-reversed.svg', 256],
    'fieldworks-lite-reversed-512.png': ['fieldworks-lite-reversed.svg', 512],

    'fieldworks-lite-circle-512.png': ['fieldworks-lite-circle.svg', 512],
    'fieldworks-lite-reversed-circle-512.png': ['fieldworks-lite-reversed-circle.svg', 512],
    'fieldworks-lite-splash-512.png': ['fieldworks-lite-splash.svg', 512],

    // Platform.Bible tab icon: shown at 16 px, so the micro drawing, at 4x for high-density screens.
    'fieldworks-lite-micro-64.png': ['fieldworks-lite-micro.svg', 64],

    'fieldworks-lite-appicon-512.png': ['fieldworks-lite-appicon.svg', 512],
    'fieldworks-lite-appicon-1024.png': ['fieldworks-lite-appicon.svg', 1024],
    'fieldworks-lite-appicon-macos-512.png': ['fieldworks-lite-appicon-macos.svg', 512],
    'fieldworks-lite-appicon-macos-1024.png': ['fieldworks-lite-appicon-macos.svg', 1024],
    // App Store Connect rejects an icon with an alpha channel.
    'fieldworks-lite-appicon-square-1024.png': ['fieldworks-lite-appicon-square.svg', 1024, 'opaque'],
  },
  icos: {
    'favicon.ico': ['fieldworks-lite-16.png', 'fieldworks-lite-32.png', 'fieldworks-lite-48.png'],
    // The Windows app icon (exe, window, portable app's taskbar): reversed, since it can't follow the taskbar theme.
    'png/fieldworks-lite-reversed.ico': ['fieldworks-lite-reversed-16.png', 'fieldworks-lite-reversed-24.png', 'fieldworks-lite-reversed-32.png', 'fieldworks-lite-reversed-48.png', 'fieldworks-lite-reversed-256.png'],
  },
  // Files other folders need at a fixed path.
  copies: {
    '../../frontend/viewer/public/icon.svg': 'favicon.svg',
    '../../docs/site/static/img/favicon.svg': 'favicon.svg',
    '../../docs/site/static/img/favicon.ico': 'favicon.ico',
    '../../docs/site/static/img/logo.svg': 'fieldworks-lite-tiny.svg',
    '../../docs/site/static/img/logo-dark.svg': 'fieldworks-lite-tiny-reversed.svg',
  },
  faviconSvg: {
    title: 'FieldWorks Lite, favicon (reversed in dark mode)',
    light: 'fieldworks-lite-micro.svg',
    dark: 'fieldworks-lite-micro-reversed.svg',
  },
})
