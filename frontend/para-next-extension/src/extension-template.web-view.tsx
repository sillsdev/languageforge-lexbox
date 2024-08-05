import papi, { logger } from '@papi/frontend';
import { useData, useDataProvider } from '@papi/frontend/react';
import { useCallback, useState } from 'react';
import type { DoStuffEvent } from 'fw-lite-extension';
import { Button, useEvent } from 'platform-bible-react';
import 'viewer/component';



globalThis.webViewComponent = function ExtensionTemplate() {

  return (
    <>
        <fw-data-project-view projectName="sena-3" baseUrl="https://localhost:7238"></fw-data-project-view>
    </>
  );
};
