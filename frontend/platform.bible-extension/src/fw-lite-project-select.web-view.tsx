import papi from '@papi/frontend';
import type { IProjectModel, LocalProjectsEvent } from 'fw-lite-extension';
import { ComboBox, useEvent } from 'platform-bible-react';
import { useState, useEffect } from 'react';

globalThis.webViewComponent = function fwLiteProjectSelect() {
  const [localProjects, setLocalProjects] = useState<IProjectModel[] | undefined>();
  const [selectedProjectCode, setSelectedProjectCode] = useState('');

  useEvent<LocalProjectsEvent>(
    papi.network.getNetworkEvent('fwLiteExtension.localProjects'),
    ({ projects }) => setLocalProjects(projects),
  );

  useEffect(() => {
    papi.commands.sendCommand('fwLiteExtension.localProjects');
  }, []);

  return (
    <div>
      <ComboBox
        buttonPlaceholder={
          !localProjects
            ? 'Loading projects...'
            : !localProjects.length
              ? 'No projects found'
              : !selectedProjectCode
                ? 'Select a project'
                : `Selected: ${selectedProjectCode}`
        }
        commandEmptyMessage="No projects found"
        isDisabled={!localProjects?.length}
        onChange={setSelectedProjectCode}
        options={localProjects?.map((p) => p.code)}
        textPlaceholder="Select a project"
      />
      {!!selectedProjectCode && (
        <>
          <button
            onClick={() =>
              papi.commands.sendCommand('fwLiteExtension.openProject', selectedProjectCode)
            }
            type="button"
          >
            Open project
          </button>
          <button onClick={() => setSelectedProjectCode('')} type="button">
            Clear selection
          </button>
        </>
      )}
    </div>
  );
};
