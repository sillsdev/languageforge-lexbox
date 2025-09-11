import { useLocalizedStrings } from '@papi/frontend/react';
import type { PartialEntry } from 'fw-lite-extension';
import { Button } from 'platform-bible-react';
import { type ReactElement, useState } from 'react';
import AddNewEntry from './add-new-entry';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

interface AddNewEntryButtonProps {
  addEntry: (entry: PartialEntry) => Promise<void>;
  analysisLang: string;
  headword?: string;
  vernacularLang: string;
}

export default function AddNewEntryButton({
  addEntry,
  analysisLang,
  headword,
  vernacularLang,
}: AddNewEntryButtonProps): ReactElement {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [adding, setAdding] = useState(false);

  return adding ? (
    <div className="tw-border tw-rounded-lg tw-shadow-sm tw-p-4">
      <AddNewEntry
        addEntry={addEntry}
        analysisLang={analysisLang}
        headword={headword}
        onCancel={() => setAdding(false)}
        vernacularLang={vernacularLang}
      />
    </div>
  ) : (
    <Button onClick={() => setAdding(true)}>
      {localizedStrings['%fwLiteExtension_addWord_buttonAdd%']}
    </Button>
  );
}
