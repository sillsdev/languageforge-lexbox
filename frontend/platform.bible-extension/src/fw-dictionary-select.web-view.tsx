import type { WebViewProps } from '@papi/core';
import papi from '@papi/frontend';
import type { IProjectModel } from 'fw-lite-extension';
import { ComboBox } from 'platform-bible-react';
import { useCallback, useEffect, useState } from 'react';

globalThis.webViewComponent = function fwDictionarySelect(props: WebViewProps) {
  const [fwDictionaries, setFwDictionaries] = useState<IProjectModel[] | undefined>();
  const [selectedDictionaryCode, setSelectedDictionaryCode] = useState('');
  const [settingSaved, setSettingSaved] = useState(false);
  const [settingSaving, setSettingSaving] = useState(false);

  const saveSetting = useCallback(() => {
    if (!selectedDictionaryCode) return;
    setSettingSaving(true);
    papi.commands
      .sendCommand('fwLiteExtension.selectDictionary', props.projectId!, selectedDictionaryCode)
      .then(() => setSettingSaved(true))
      .catch((e) => papi.logger.error('Error saving FieldWorks dictionary selection:', e))
      .finally(() => setSettingSaving(false));
  }, [selectedDictionaryCode, props.projectId]);

  useEffect(() => {
    papi.logger.info(`This web view was opened for project '${props.projectId}'`);
    papi.commands
      .sendCommand('fwLiteExtension.fwDictionaries')
      .then(setFwDictionaries)
      .catch((e) => papi.logger.error('Error fetching FieldWorks dictionaries:', e));
  }, [props.projectId]);

  if (settingSaving) {
    return <p>Saving dictionary selection {selectedDictionaryCode}...</p>;
  }

  if (settingSaved) {
    return <p>Dictionary selection saved. You can close this window.</p>;
  }

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
          <button onClick={saveSetting} type="button">
            Confirm selection
          </button>
          <button onClick={() => setSelectedDictionaryCode('')} type="button">
            Clear selection
          </button>
        </>
      )}
    </div>
  );
};
