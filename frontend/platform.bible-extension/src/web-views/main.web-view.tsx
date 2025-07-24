import papi from '@papi/frontend';
import type { BrowseWebViewOptions } from 'fw-lite-extension';
import { useEffect, useRef, useState } from 'react';

globalThis.webViewComponent = function fwLiteMainWindow({ url }: BrowseWebViewOptions) {
  const [baseUrl, setBaseUrl] = useState('');
  const [src, setSrc] = useState('');

  const iframe = useRef<HTMLIFrameElement | null>(null);

  useEffect(() => void updateUrl(), []);
  useEffect(() => void setSrc(url || baseUrl), [baseUrl, url]);

  async function updateUrl(): Promise<void> {
    setBaseUrl(await papi.commands.sendCommand('fwLiteExtension.getBaseUrl'));
  }

  if (!src) {
    return <button onClick={() => void updateUrl()}>Loading</button>;
  }
  return (
    <>
      <iframe ref={iframe} src={src} title="FieldWorks Lite" />
    </>
  );
};
