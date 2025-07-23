import papi from '@papi/frontend';
import { useEffect, useRef, useState } from 'react';

globalThis.webViewComponent = function fwLiteMainWindow() {
  const [baseUrl, setBaseUrl] = useState('');
  const [dictionaryUrl, setDictionaryUrl] = useState('');

  const iframe = useRef<HTMLIFrameElement | null>(null);

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
