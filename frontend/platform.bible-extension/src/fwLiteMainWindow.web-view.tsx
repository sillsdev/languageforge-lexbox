import papi, { logger } from '@papi/frontend';
import type { FindEntryEvent } from 'fw-lite-extension';
import { useEvent } from 'platform-bible-react';
import { useEffect, useRef, useState } from 'react';

globalThis.webViewComponent = function fwLiteMainWindow() {
  const [baseUrl, setBaseUrl] = useState('');
  const [dictionaryUrl, setDictionaryUrl] = useState('');

  const iframe = useRef<HTMLIFrameElement | null>(null);
  useEvent<FindEntryEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.findEntryEvent'),
    ({ entry }) => {
      logger.info('fwLiteExtension.findEntryEvent', entry);
      iframe.current?.contentWindow?.postMessage(
        { type: 'notification', message: `Hello from Paratext ${entry}` },
        new URL(baseUrl).origin,
      );
    },
  );

  useEffect(() => void updateUrl(), []);

  async function updateUrl(): Promise<void> {
    const result = await papi.commands.sendCommand('fwLiteExtension.getBaseUrl');
    setBaseUrl(result.baseUrl);
    setDictionaryUrl(result.dictionaryUrl);
  }

  if (!baseUrl) {
    return <button onClick={() => void updateUrl()}>Loading</button>;
  }
  return (
    <>
      <iframe ref={iframe} src={dictionaryUrl || baseUrl} title="FieldWorks Lite" />
    </>
  );
};
