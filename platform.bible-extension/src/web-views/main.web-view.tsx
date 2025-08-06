import papi from '@papi/frontend';
import type { BrowseWebViewOptions } from 'fw-lite-extension';
import { Button } from 'platform-bible-react';
import { useEffect, useRef, useState } from 'react';

/* eslint-disable react-hooks/rules-of-hooks */

globalThis.webViewComponent = function fwLiteMainWindow({ url }: BrowseWebViewOptions) {
  // TODO: Use of baseUrl for development; remove before publishing.
  const [baseUrl, setBaseUrl] = useState('');
  const [src, setSrc] = useState('');

  // eslint-disable-next-line no-null/no-null
  const iframe = useRef<HTMLIFrameElement | null>(null);

  useEffect(() => {
    updateUrl();
  }, []);
  useEffect(() => setSrc(url || baseUrl), [baseUrl, url]);

  async function updateUrl(): Promise<void> {
    setBaseUrl(await papi.commands.sendCommand('fwLiteExtension.getBaseUrl'));
  }

  if (!src) {
    return <Button onClick={() => updateUrl()}>Loading</Button>;
  }
  return <iframe ref={iframe} src={src} title="FieldWorks Lite" />;
};
