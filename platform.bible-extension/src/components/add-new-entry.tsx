import { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { DictionaryLanguages, PartialEntry } from 'fw-lite-extension';
import { Button, Input, Label } from 'platform-bible-react';
import { type ReactElement, useCallback, useEffect, useState } from 'react';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

/** Props for the AddNewEntry component */
interface AddNewEntryProps extends DictionaryLanguages {
  addEntry: (entry: PartialEntry) => Promise<void>;
  headword?: string;
  onCancel?: () => void;
}

/** A panel for creating a simple entry: headword with gloss and/or definition. */
export default function AddNewEntry({
  addEntry,
  analysisLanguage,
  headword,
  onCancel,
  vernacularLanguage,
}: AddNewEntryProps): ReactElement {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [definition, setDefinition] = useState('');
  const [gloss, setGloss] = useState('');
  const [ready, setReady] = useState(false);
  const [tempHeadword, setTempHeadword] = useState('');

  useEffect(() => setTempHeadword(headword || ''), [headword]);

  useEffect(() => {
    setReady(!!(vernacularLanguage && tempHeadword.trim() && (gloss.trim() || definition.trim())));
  }, [definition, gloss, tempHeadword, vernacularLanguage]);

  const clearEntry = useCallback((): void => {
    setDefinition('');
    setGloss('');
    setTempHeadword(headword || '');
    onCancel?.();
  }, [headword, onCancel]);

  async function onSubmit(): Promise<void> {
    const entry = createEntry(
      vernacularLanguage,
      tempHeadword.trim(),
      analysisLanguage || 'en',
      gloss.trim(),
      definition.trim(),
    );
    await addEntry(entry)
      .then(() => clearEntry())
      .catch((e) => logger.error('Error adding entry:', JSON.stringify(e)));
  }

  return (
    <div className="tw-flex tw-flex-col tw-items-start">
      <h3 className="tw-font-semibold tw-mb-2">
        {localizedStrings['%fwLiteExtension_addWord_title%']}
      </h3>

      <div className="tw-flex tw-flex-col tw-gap-1">
        <div>
          <Label htmlFor="newEntryHeadword">
            {localizedStrings['%fwLiteExtension_entryDisplay_headword%']} ({vernacularLanguage}):
          </Label>
          <Input
            id="newEntryHeadword"
            onChange={(e) => setTempHeadword(e.target.value)}
            value={tempHeadword}
          />
        </div>

        <div>
          <Label htmlFor="newEntryGloss">
            {localizedStrings['%fwLiteExtension_entryDisplay_gloss%']} ({analysisLanguage}):
          </Label>
          <Input id="newEntryGloss" onChange={(e) => setGloss(e.target.value)} value={gloss} />
        </div>

        <div>
          <Label htmlFor="newEntryDefinition">
            {localizedStrings['%fwLiteExtension_entryDisplay_definition%']} ({analysisLanguage}):
          </Label>
          <Input
            id="newEntryDefinition"
            onChange={(e) => setDefinition(e.target.value)}
            value={definition}
          />
        </div>

        <div className="tw-flex tw-gap-1 tw-mt-2">
          <Button disabled={!ready} onClick={() => onSubmit()}>
            {localizedStrings['%fwLiteExtension_addWord_buttonSubmit%']}
          </Button>
          <Button onClick={clearEntry}>
            {localizedStrings['%fwLiteExtension_button_cancel%']}
          </Button>
        </div>
      </div>
    </div>
  );
}

function createEntry(
  vernacularLang: string,
  headword: string,
  analysisLang: string,
  gloss?: string,
  definition?: string,
): PartialEntry {
  return {
    lexemeForm: { [vernacularLang]: headword },
    senses: [
      {
        definition: definition ? { [analysisLang]: definition } : {},
        gloss: gloss ? { [analysisLang]: gloss } : {},
      },
    ],
  };
}
