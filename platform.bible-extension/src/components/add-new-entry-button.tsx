import { useLocalizedStrings } from '@papi/frontend/react';
import type { DictionaryLanguages, PartialEntry } from 'fw-lite-extension';
import { Button } from 'platform-bible-react';
import { type ReactElement, useState } from 'react';
import AddNewEntry from './add-new-entry';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

/** Props for the AddNewEntryButton component */
interface AddNewEntryButtonProps extends DictionaryLanguages {
  addEntry: (entry: PartialEntry) => Promise<void>;
  headword?: string;
}

/** A button that, when clicked, expands to the AddNewEntry component. */
export default function AddNewEntryButton({
  addEntry,
  analysisLanguage,
  headword,
  vernacularLanguage,
}: AddNewEntryButtonProps): ReactElement {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [adding, setAdding] = useState(false);

  return adding ? (
    <div className="tw-border tw-rounded-lg tw-shadow-sm tw-p-4">
      <AddNewEntry
        addEntry={addEntry}
        analysisLanguage={analysisLanguage}
        headword={headword}
        onCancel={() => setAdding(false)}
        vernacularLanguage={vernacularLanguage}
      />
    </div>
  ) : (
    <Button onClick={() => setAdding(true)}>
      {localizedStrings['%fwLiteExtension_addWord_buttonAdd%']}
    </Button>
  );
}
