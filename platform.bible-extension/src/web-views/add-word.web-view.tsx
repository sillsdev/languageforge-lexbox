import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { DictionaryWebViewProps, IEntryService, PartialEntry } from 'dictionary';
import { useCallback, useEffect, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

globalThis.webViewComponent = function DictionaryAddWord({
  analysisLanguage,
  projectId,
  vernacularLanguage,
  word,
}: DictionaryWebViewProps) {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [dictionaryNetworkObject, setDictionaryNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);

  useEffect(() => {
    papi.networkObjects
      .get<IEntryService>('dictionary.entryService')
      // eslint-disable-next-line promise/always-return
      .then((networkObject) => {
        logger.info('Got network object:', networkObject);
        setDictionaryNetworkObject(networkObject);
      })
      .catch((e) =>
        logger.error(`${localizedStrings['%dictionary_error_gettingNetworkObject%']}`, e),
      );
  }, [localizedStrings]);

  const addEntry = useCallback(
    async (entry: PartialEntry) => {
      if (!projectId || !dictionaryNetworkObject) {
        const errMissingParam = localizedStrings['%dictionary_error_missingParam%'];
        if (!projectId) logger.warn(`${errMissingParam}projectId`);
        if (!dictionaryNetworkObject) logger.warn(`${errMissingParam}dictionaryNetworkObject`);
        return;
      }

      setIsSubmitted(false);
      setIsSubmitting(true);
      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const entryId = (await dictionaryNetworkObject.addEntry(projectId, entry))?.id;
      setIsSubmitting(false);
      if (entryId) {
        setIsSubmitted(true);
        await papi.commands.sendCommand('dictionary.displayEntry', projectId, entryId);
      } else {
        logger.error(`${localizedStrings['%dictionary_error_failedToAddEntry%']}`);
      }
    },
    [dictionaryNetworkObject, localizedStrings, projectId],
  );

  return (
    <div className="tw-p-4">
      <AddNewEntry
        addEntry={addEntry}
        analysisLanguage={analysisLanguage ?? ''}
        headword={word}
        vernacularLanguage={vernacularLanguage ?? ''}
      />
      {isSubmitting && <p>Adding entry to dictionary...</p>}
      {isSubmitted && <p>Entry added!</p>}
    </div>
  );
};
