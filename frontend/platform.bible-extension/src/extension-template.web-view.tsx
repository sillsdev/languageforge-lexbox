import papi, { logger } from '@papi/frontend';
import type {FindEntryEvent, LaunchServerEvent} from 'fw-lite-extension';
import { useEvent } from 'platform-bible-react';
import 'viewer/component';
import {useState} from 'react';



globalThis.webViewComponent = function ExtensionTemplate() {
  const [baseUrl, setBaseUrl] = useState('');
  useEvent<FindEntryEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.findEntry'),
    ({ entry }) => {
      window.lexbox.Search.openSearch(entry);
    },
  );

  useEvent<LaunchServerEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.launchServer'),
    ({ baseUrl }) => {
      setBaseUrl(baseUrl);
    },
  );
  if (!baseUrl) {
    return <button onClick={async () => {
      const result = await papi.commands.sendCommand('fwLiteExtension.getBaseUrl');
      setBaseUrl(result.baseUrl);
    }}>Loading</button>;
  }
  return (
    <>
        <fw-data-project-view projectName="sena-3" baseUrl={baseUrl}></fw-data-project-view>
    </>
  );
};
