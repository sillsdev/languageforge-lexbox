import { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { IProjectModel } from 'lexicon';
import { Button, ComboBox } from 'platform-bible-react';
import { type ReactElement, useCallback, useState } from 'react';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

/** Props for the LexiconComboBox component */
interface LexiconComboBoxProps {
  lexicons?: IProjectModel[];
  selectLexicon: (lexiconCode: string) => Promise<void>;
}

/** A combo-box for selecting a lexicon for a project. */
export default function LexiconComboBox({
  lexicons,
  selectLexicon,
}: LexiconComboBoxProps): ReactElement {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [selectedLexiconCode, setSelectedLexiconCode] = useState('');
  const [settingSaved, setSettingSaved] = useState(false);
  const [settingSaving, setSettingSaving] = useState(false);

  const saveSetting = useCallback(
    (code: string): void => {
      if (!code) return;
      setSettingSaving(true);
      // eslint-disable-next-line promise/catch-or-return
      selectLexicon(code)
        .then(() => setSettingSaved(true))
        .catch((e) =>
          logger.error(localizedStrings['%lexicon_selectLexicon_saveError%'], JSON.stringify(e)),
        )
        .finally(() => setSettingSaving(false));
    },
    [localizedStrings, selectLexicon],
  );

  if (settingSaving) {
    return (
      <h3 className="tw-font-semibold tw-m-2">
        {localizedStrings['%lexicon_selectLexicon_saving%']} {selectedLexiconCode} ...
      </h3>
    );
  }

  if (settingSaved) {
    return (
      <h3 className="tw-font-semibold tw-m-2">
        {localizedStrings['%lexicon_selectLexicon_saved%']}
      </h3>
    );
  }

  return (
    <div className="tw-flex tw-flex-col tw-gap-2 tw-p-4">
      <ComboBox
        buttonPlaceholder={
          /* eslint-disable no-nested-ternary */
          !lexicons
            ? localizedStrings['%lexicon_selectLexicon_loading%']
            : !lexicons.length
              ? localizedStrings['%lexicon_selectLexicon_noneFound%']
              : !selectedLexiconCode
                ? localizedStrings['%lexicon_selectLexicon_select%']
                : `${localizedStrings['%lexicon_selectLexicon_selected%']} ${selectedLexiconCode}`
          /* eslint-enable no-nested-ternary */
        }
        commandEmptyMessage={localizedStrings['%lexicon_selectLexicon_noneFound%']}
        isDisabled={!lexicons?.length}
        onChange={setSelectedLexiconCode}
        options={lexicons?.map((p) => p.code)}
        textPlaceholder={localizedStrings['%lexicon_selectLexicon_select%']}
      />

      {!!selectedLexiconCode && (
        <div className="tw-flex tw-gap-2 tw-items-center">
          <Button onClick={() => saveSetting(selectedLexiconCode)} type="button">
            {localizedStrings['%lexicon_selectLexicon_confirm%']}
          </Button>

          <Button onClick={() => setSelectedLexiconCode('')} type="button" variant="secondary">
            {localizedStrings['%lexicon_selectLexicon_clear%']}
          </Button>
        </div>
      )}
    </div>
  );
}
