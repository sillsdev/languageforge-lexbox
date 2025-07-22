import { logger } from '@papi/frontend';
import type { IProjectModel } from 'fw-lite-extension';
import { ComboBox } from 'platform-bible-react';
import { useCallback, useState } from 'react';

interface DictionaryComboBoxProps {
  dictionaries?: IProjectModel[];
  selectDictionary: (dictionaryCode: string) => Promise<void>;
}

// eslint-disable-next-line @typescript-eslint/naming-convention
export default function DictionaryComboBox({
  dictionaries,
  selectDictionary,
}: DictionaryComboBoxProps) {
  const [selectedDictionaryCode, setSelectedDictionaryCode] = useState('');
  const [settingSaved, setSettingSaved] = useState(false);
  const [settingSaving, setSettingSaving] = useState(false);

  const saveSetting = useCallback(
    (code: string): void => {
      if (!code) return;
      setSettingSaving(true);
      selectDictionary(code)
        .then(() => void setSettingSaved(true))
        .catch((e) => void logger.error('Error saving dictionary selection:', e))
        .finally(() => void setSettingSaving(false));
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
          !dictionaries
            ? 'Loading dictionaries...'
            : !dictionaries.length
            ? 'No dictionaries found'
            : !selectedDictionaryCode
            ? 'Select a dictionary'
            : `Selected: ${selectedDictionaryCode}`
        }
        commandEmptyMessage="No dictionaries found"
        isDisabled={!dictionaries?.length}
        onChange={setSelectedDictionaryCode}
        options={dictionaries?.map((p) => p.code)}
        textPlaceholder="Select a dictionary"
      />
      {!!selectedDictionaryCode && (
        <>
          <button onClick={() => void saveSetting(selectedDictionaryCode)} type="button">
            Confirm selection
          </button>
          <button onClick={() => void setSelectedDictionaryCode('')} type="button">
            Clear selection
          </button>
        </>
      )}
    </div>
  );
}
