import papi, { logger } from '@papi/frontend';
import type {FindEntryEvent, LaunchServerEvent} from 'fw-lite-extension';
import { useEvent } from 'platform-bible-react';
import {useState, useRef, useEffect} from 'react';



globalThis.webViewComponent = function ExtensionTemplate() {
  const [baseUrl, setBaseUrl] = useState('');
  const iframe = useRef<HTMLIFrameElement | null>(null);
  useEvent<FindEntryEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.findEntry'),
    ({ entry }) => {
      iframe.current?.contentWindow.postMessage({type: 'notification', message: 'hello from Paratext'}, new URL(baseUrl).origin);
    },
  );

  useEvent<LaunchServerEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.launchServer'),
    ({ baseUrl }) => {
      // console.log('launchServer', baseUrl);
      setBaseUrl(baseUrl);
    },
  );

  useEffect(() => void updateUrl(), []);

  async function updateUrl() {
    const result = await papi.commands.sendCommand('fwLiteExtension.getBaseUrl');
    setBaseUrl(result.baseUrl);
  }

  if (!baseUrl) {
    return <button onClick={updateUrl}>Loading</button>;
  }
  return (
    <>
        <iframe ref={iframe} src={baseUrl}></iframe>
    </>
  );
};
