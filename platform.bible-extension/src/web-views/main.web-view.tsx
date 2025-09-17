import type { BrowseWebViewOptions } from 'fw-lite-extension';
import { useRef } from 'react';

/* eslint-disable react-hooks/rules-of-hooks */

globalThis.webViewComponent = function fwLiteMainWindow({ url }: BrowseWebViewOptions) {
  // eslint-disable-next-line no-null/no-null
  const iframe = useRef<HTMLIFrameElement | null>(null);

  return url ? <iframe ref={iframe} src={url} title="FieldWorks Lite" /> : <p>Loading...</p>;
};
