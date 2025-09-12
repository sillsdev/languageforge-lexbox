import { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { IProjectModel } from 'fw-lite-extension';
import { Button, ComboBox } from 'platform-bible-react';
import { type ReactElement, useCallback, useState } from 'react';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

/** Props for the DictionaryComboBox component */
interface DictionaryComboBoxProps {
  dictionaries?: IProjectModel[];
  selectDictionary: (dictionaryCode: string) => Promise<void>;
}

/** A combo-box for selecting a FieldWorks dictionary for a project. */
export default function DictionaryComboBox({
  dictionaries,
  selectDictionary,
}: DictionaryComboBoxProps): ReactElement {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

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
        .catch((e) =>
          logger.error(
            localizedStrings['%fwLiteExtension_dictionarySelect_saveError%'],
            JSON.stringify(e),
          ),
        )
        .finally(() => setSettingSaving(false));
    },
    [localizedStrings, selectDictionary],
  );

  if (settingSaving) {
    return (
      <h3 className="tw-font-semibold tw-m-2">
        {localizedStrings['%fwLiteExtension_dictionarySelect_saving%']} {selectedDictionaryCode} ...
      </h3>
    );
  }

  if (settingSaved) {
    return (
      <h3 className="tw-font-semibold tw-m-2">
        {localizedStrings['%fwLiteExtension_dictionarySelect_saved%']}
      </h3>
    );
  }

  return (
    <div className="tw-flex tw-flex-col tw-gap-2 tw-p-4">
      <ComboBox
        buttonPlaceholder={
          /* eslint-disable no-nested-ternary */
          !dictionaries
            ? localizedStrings['%fwLiteExtension_dictionarySelect_loading%']
            : !dictionaries.length
              ? localizedStrings['%fwLiteExtension_dictionarySelect_noneFound%']
              : !selectedDictionaryCode
                ? localizedStrings['%fwLiteExtension_dictionarySelect_select%']
                : `${localizedStrings['%fwLiteExtension_dictionarySelect_selected%']} ${selectedDictionaryCode}`
          /* eslint-enable no-nested-ternary */
        }
        commandEmptyMessage={localizedStrings['%fwLiteExtension_dictionarySelect_noneFound%']}
        isDisabled={!dictionaries?.length}
        onChange={setSelectedDictionaryCode}
        options={dictionaries?.map((p) => p.code)}
        textPlaceholder={localizedStrings['%fwLiteExtension_dictionarySelect_select%']}
      />

      {!!selectedDictionaryCode && (
        <div className="tw-flex tw-gap-2 tw-items-center">
          <Button onClick={() => saveSetting(selectedDictionaryCode)} type="button">
            {localizedStrings['%fwLiteExtension_dictionarySelect_confirm%']}
          </Button>

          <Button onClick={() => setSelectedDictionaryCode('')} type="button" variant="secondary">
            {localizedStrings['%fwLiteExtension_dictionarySelect_clear%']}
          </Button>
        </div>
      )}
    </div>
  );
}
