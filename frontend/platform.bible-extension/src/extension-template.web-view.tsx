import papi, { logger } from '@papi/frontend';
import type {FindEntryEvent} from 'fw-lite-extension';
import { useEvent } from 'platform-bible-react';
import 'viewer/component';



globalThis.webViewComponent = function ExtensionTemplate() {

  useEvent<FindEntryEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.findEntry'),
    ({ entry }) => {
      window.lexbox.Search.openSearch(entry);
    },
  );
  return (
    <>
        <fw-data-project-view projectName="sena-3" baseUrl="https://localhost:7238"></fw-data-project-view>
    </>
  );
};
