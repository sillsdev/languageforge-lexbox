import papi from '@papi/frontend';
import type { IProjectModel, LocalProjectsEvent } from 'fw-lite-extension';
import { ComboBox, useEvent } from 'platform-bible-react';
import { useState, useEffect } from 'react';

globalThis.webViewComponent = function fwLiteProjectSelect() {
  const [localProjects, setLocalProjects] = useState<IProjectModel[] | undefined>();
  const [selectedProjectName, setSelectedProjectName] = useState('');

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
              : !selectedProjectName
                ? 'Select a project'
                : `Selected: ${selectedProjectName}`
        }
        commandEmptyMessage="No projects found"
        isDisabled={!localProjects?.length}
        onChange={(projName) => setSelectedProjectName(projName)}
        options={localProjects?.map((p) => p.name)}
        textPlaceholder="Select a project"
      />
      {!!selectedProjectName && (
        <button onClick={() => setSelectedProjectName('')} type="button">
          Clear selection
        </button>
      )}
    </div>
  );
};
