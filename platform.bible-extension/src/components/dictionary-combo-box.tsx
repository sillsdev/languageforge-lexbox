import { logger } from '@papi/frontend';
import type { IProjectModel } from 'fw-lite-extension';
import { ComboBox } from 'platform-bible-react';
import { type ReactElement, useCallback, useState } from 'react';

interface DictionaryComboBoxProps {
  dictionaries?: IProjectModel[];
  selectDictionary: (dictionaryCode: string) => Promise<void>;
}

export default function DictionaryComboBox({
  dictionaries,
  selectDictionary,
}: DictionaryComboBoxProps): ReactElement {
  const [selectedDictionaryCode, setSelectedDictionaryCode] = useState('');
  const [settingSaved, setSettingSaved] = useState(false);
  const [settingSaving, setSettingSaving] = useState(false);

  const saveSetting = useCallback(
    (code: string): void => {
      if (!code) return;
      setSettingSaving(true);
      // eslint-disable-next-line promise/catch-or-return
      selectDictionary(code)
        .then(() => setSettingSaved(true))
        .catch((e) => logger.error('Error saving dictionary selection:', JSON.stringify(e)))
        .finally(() => setSettingSaving(false));
    },
    [selectDictionary],
  );

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
          /* eslint-disable no-nested-ternary */
          !dictionaries
            ? 'Loading dictionaries...'
            : !dictionaries.length
              ? 'No dictionaries found'
              : !selectedDictionaryCode
                ? 'Select a dictionary'
                : `Selected: ${selectedDictionaryCode}`
          /* eslint-enable no-nested-ternary */
        }
        commandEmptyMessage="No dictionaries found"
        isDisabled={!dictionaries?.length}
        onChange={setSelectedDictionaryCode}
        options={dictionaries?.map((p) => p.code)}
        textPlaceholder="Select a dictionary"
      />
      {!!selectedDictionaryCode && (
        <>
          <button onClick={() => saveSetting(selectedDictionaryCode)} type="button">
            Confirm selection
          </button>
          <button onClick={() => setSelectedDictionaryCode('')} type="button">
            Clear selection
          </button>
        </>
      )}
    </div>
  );
}
