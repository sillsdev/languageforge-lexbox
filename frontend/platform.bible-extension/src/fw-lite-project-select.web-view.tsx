import papi, { logger } from '@papi/frontend';
import type { IProjectModel, LocalProjectsEvent } from 'fw-lite-extension';
import { useEvent } from 'platform-bible-react';
import { useState, useEffect } from 'react';

globalThis.webViewComponent = function fwLiteProjectSelect() {
  const [localProjects, setLocalProjects] = useState<IProjectModel[] | undefined>();

  useEvent<LocalProjectsEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.localProjects'),
    ({ projects }) => {
      logger.info('localProjects', projects);
      setLocalProjects(projects);
    },
  );

  useEffect(() => {
    papi.commands.sendCommand('fwLiteExtension.localProjects');
  }, []);

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
    </>
  );
};
