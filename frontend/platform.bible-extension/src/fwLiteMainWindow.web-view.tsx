import papi, { logger } from '@papi/frontend';
import type { FindEntryEvent, LaunchServerEvent } from 'fw-lite-extension';
import { useEvent } from 'platform-bible-react';
import { useState, useRef, useEffect } from 'react';

globalThis.webViewComponent = function fwLiteMainWindow() {
  const [baseUrl, setBaseUrl] = useState('');

  const iframe = useRef<HTMLIFrameElement | null>(null);
  useEvent<FindEntryEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.findEntry'),
    ({ entry }) => {
      logger.info('findEntry', entry);
      iframe.current?.contentWindow?.postMessage(
        { type: 'notification', message: `Hello from Paratext ${entry}` },
        new URL(baseUrl).origin,
      );
    },
  );

  useEvent<LaunchServerEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.launchServer'),
    ({ baseUrl }) => {
      logger.info('launchServer', baseUrl);
      setBaseUrl(baseUrl);
    },
  );

  useEffect(() => void updateUrl(), []);

  async function updateUrl(): Promise<void> {
    const result = await papi.commands.sendCommand('fwLiteExtension.getBaseUrl');
    setBaseUrl(result.baseUrl);
  }

  if (!baseUrl) {
    return <button onClick={() => void updateUrl()}>Loading</button>;
  }
  return (
    <>
      <iframe ref={iframe} src={baseUrl} title="FieldWorks Lite" />
    </>
  );
};
