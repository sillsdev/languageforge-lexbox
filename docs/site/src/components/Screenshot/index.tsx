import type {ReactNode} from 'react';
import ThemedImage from '@theme/ThemedImage';
import useBaseUrl from '@docusaurus/useBaseUrl';

// `name` is a shot from frontend/viewer/tests/docs-screenshots, which writes a light and a dark PNG.
// They're captured at 2x, so `width` (CSS px) should be half the PNG's width to show them at 1:1.
export default function Screenshot({name, alt, width}: {name: string; alt: string; width?: number}): ReactNode {
  return (
    <ThemedImage
      className="screenshot"
      alt={alt}
      style={width ? {maxWidth: width} : undefined}
      sources={{
        light: useBaseUrl(`/img/fw-lite/${name}.png`),
        dark: useBaseUrl(`/img/fw-lite/${name}-dark.png`),
      }}
    />
  );
}
