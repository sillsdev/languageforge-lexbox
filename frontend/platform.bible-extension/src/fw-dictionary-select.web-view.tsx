import type { WebViewProps } from '@papi/core';
import papi from '@papi/frontend';
import type { IProjectModel } from 'fw-lite-extension';
import { ComboBox } from 'platform-bible-react';
import { useState, useEffect } from 'react';

globalThis.webViewComponent = function fwDictionarySelect(props: WebViewProps) {
  const [fwDictionaries, setFwDictionaries] = useState<IProjectModel[] | undefined>();
  const [selectedDictionaryCode, setSelectedDictionaryCode] = useState('');

  useEffect(() => {
    papi.logger.info(`This web view was opened for project '${props.projectId}'`);
    papi.commands.sendCommand('fwLiteExtension.fwDictionaries').then(setFwDictionaries);
  }, []);

  return (
    <div>
      <ComboBox
        buttonPlaceholder={
          !fwDictionaries
            ? 'Loading dictionaries...'
            : !fwDictionaries.length
              ? 'No dictionaries found'
              : !selectedDictionaryCode
                ? 'Select a dictionary'
                : `Selected: ${selectedDictionaryCode}`
        }
        commandEmptyMessage="No dictionaries found"
        isDisabled={!fwDictionaries?.length}
        onChange={setSelectedDictionaryCode}
        options={fwDictionaries?.map((p) => p.code)}
        textPlaceholder="Select a dictionary"
      />
      {!!selectedDictionaryCode && (
        <>
          <button
            onClick={() =>
              papi.commands.sendCommand(
                'fwLiteExtension.selectDictionary',
                props.projectId!,
                selectedDictionaryCode,
              )
            }
            type="button"
          >
            Open project
          </button>
          <button onClick={() => setSelectedDictionaryCode('')} type="button">
            Clear selection
          </button>
        </>
      )}
    </div>
  );
};
