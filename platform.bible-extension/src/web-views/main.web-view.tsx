import type { BrowseWebViewProps } from 'fw-lite-extension';
import { useRef } from 'react';

globalThis.webViewComponent = function FwLiteMainWindow({ url }: BrowseWebViewProps) {
  // eslint-disable-next-line no-null/no-null
  const iframe = useRef<HTMLIFrameElement | null>(null);

  return url ? <iframe ref={iframe} src={url} title="FieldWorks Lite" /> : <p>Loading...</p>;
};
