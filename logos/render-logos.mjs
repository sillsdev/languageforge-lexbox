import { Resvg } from '@resvg/resvg-js'
import { PNG } from 'pngjs'
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { join } from 'node:path'

// Writes the PNGs to dir/png/ (not committed) and favicon.ico and favicon.svg to dir, all from the SVGs in dir.
export function render(dir, { pngs, ico, faviconSvg }) {
  const src = name => readFileSync(join(dir, name))
  const out = name => join(dir, 'png', name)
  mkdirSync(join(dir, 'png'), { recursive: true })

  for (const [name, [svg, px, opaque]] of Object.entries(pngs)) {
    const image = new Resvg(src(svg), { fitTo: { mode: 'width', value: px } }).render()
    const png = opaque
      ? PNG.sync.write({ width: image.width, height: image.height, data: image.pixels }, { colorType: 2 })
      : image.asPng()
    writeFileSync(out(name), png)
  }

  // An .ico is a 6-byte header, a 16-byte entry per image, then the PNGs themselves.
  const images = ico.map(name => readFileSync(out(name)))
  const header = Buffer.alloc(6 + 16 * images.length)
  header.writeUInt16LE(1, 2)
  header.writeUInt16LE(images.length, 4)
  let offset = header.length
  images.forEach((png, i) => {
    const px = pngs[ico[i]][1]
    const entry = 6 + 16 * i
    header.writeUInt8(px, entry)
    header.writeUInt8(px, entry + 1)
    header.writeUInt16LE(1, entry + 4)
    header.writeUInt16LE(32, entry + 6)
    header.writeUInt32LE(png.length, entry + 8)
    header.writeUInt32LE(offset, entry + 12)
    offset += png.length
  })
  writeFileSync(join(dir, 'favicon.ico'), Buffer.concat([header, ...images]))

  // Browsers draw an SVG favicon at a single size, so unlike the .ico it can't switch drawings by size;
  // it switches light to dark instead. Classes carry the folder name so they can't hide anything on a page
  // that inlines the file.
  const cls = dir.split(/[\\/]/).pop()
  const body = svg => src(svg).toString('utf8')
    .replace(/<title>.*?<\/title>/s, '')
    .replace(/^\s*<svg\b[^>]*>/, '')
    .replace(/<\/svg>\s*$/, '')
  writeFileSync(join(dir, 'favicon.svg'),
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256"><title>${faviconSvg.title}</title>` +
    `<style>.${cls}-fav-d{display:none}@media (prefers-color-scheme:dark){.${cls}-fav-l{display:none}.${cls}-fav-d{display:inline}}</style>` +
    `<g class="${cls}-fav-l">${body(faviconSvg.light)}</g>` +
    `<g class="${cls}-fav-d">${body(faviconSvg.dark)}</g></svg>\n`)
}
