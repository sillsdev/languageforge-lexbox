import papi from '@papi/frontend';
import type { BrowseWebViewOptions } from 'fw-lite-extension';
import { useEffect, useRef, useState } from 'react';

globalThis.webViewComponent = function fwLiteMainWindow({ url }: BrowseWebViewOptions) {
  const [baseUrl, setBaseUrl] = useState('');

  const iframe = useRef<HTMLIFrameElement | null>(null);

  useEffect(() => void updateUrl(), []);

  async function updateUrl(): Promise<void> {
    setBaseUrl(await papi.commands.sendCommand('fwLiteExtension.getBaseUrl'));
  }

  if (!baseUrl && !url) {
    return <button onClick={() => void updateUrl()}>Loading</button>;
  }
  return (
    <>
      <iframe ref={iframe} src={url || baseUrl} title="FieldWorks Lite" />
    </>
  );
};
