import papi from '@papi/frontend';
import type {
  FindEntryEvent,
  FwProject,
  LaunchServerEvent,
  LocalProjectsEvent,
} from 'fw-lite-extension';
import { useEvent } from 'platform-bible-react';
import { useState, useRef, useEffect } from 'react';

globalThis.webViewComponent = function fwLiteMainWindow() {
  const [baseUrl, setBaseUrl] = useState('');
  const [localProjects, setLocalProjects] = useState<FwProject[] | undefined>();

  const iframe = useRef<HTMLIFrameElement | null>(null);
  useEvent<FindEntryEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.findEntry'),
    ({ entry }) => {
      iframe.current?.contentWindow?.postMessage(
        { type: 'notification', message: `Hello from Paratext ${entry}` },
        new URL(baseUrl).origin,
      );
    },
  );

  useEvent<LaunchServerEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.launchServer'),
    ({ baseUrl }) => {
      // console.log('launchServer', baseUrl);
      setBaseUrl(baseUrl);
    },
  );

  useEvent<LocalProjectsEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.localProjects'),
    ({ projects }) => setLocalProjects(projects),
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
      {!!localProjects?.length && (
        <>
          <p>Projects: {localProjects.map((p) => p.name).join(', ')}</p>
          <button onClick={() => setLocalProjects(undefined)} type="button">
            Close
          </button>
        </>
      )}
      <iframe ref={iframe} src={baseUrl} title="FieldWorks Lite" />
    </>
  );
};
