import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { IEntryService, LexiconWebViewProps, PartialEntry } from 'lexicon';
import { useCallback, useEffect, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

globalThis.webViewComponent = function LexiconAddWord({
  analysisLanguage,
  lexiconCode,
  projectId,
  vernacularLanguage,
  word,
}: LexiconWebViewProps) {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [lexiconNetworkObject, setLexiconNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);

  useEffect(() => {
    papi.networkObjects
      .get<IEntryService>('lexicon.entryService')
      // eslint-disable-next-line promise/always-return
      .then((networkObject) => {
        logger.info('Got network object:', networkObject);
        setLexiconNetworkObject(networkObject);
      })
      .catch((e) => logger.error(`${localizedStrings['%lexicon_error_gettingNetworkObject%']}`, e));
  }, [localizedStrings]);

  const addEntry = useCallback(
    async (entry: PartialEntry) => {
      if (!lexiconCode || !projectId || !lexiconNetworkObject) {
        const errMissingParam = localizedStrings['%lexicon_error_missingParam%'];
        if (!lexiconCode) logger.warn(`${errMissingParam}lexiconCode`);
        if (!projectId) logger.warn(`${errMissingParam}projectId`);
        if (!lexiconNetworkObject) logger.warn(`${errMissingParam}lexiconNetworkObject`);
        return;
      }

      setIsSubmitted(false);
      setIsSubmitting(true);
      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const entryId = (await lexiconNetworkObject.addEntry(lexiconCode, entry))?.id;
      setIsSubmitting(false);
      if (entryId) {
        setIsSubmitted(true);
        await papi.commands.sendCommand('lexicon.displayEntry', projectId, lexiconCode, entryId);
      } else {
        logger.error(`${localizedStrings['%lexicon_error_failedToAddEntry%']}`);
      }
    },
    [lexiconCode, lexiconNetworkObject, localizedStrings, projectId],
  );

  return (
    <div className="tw:p-4">
      <AddNewEntry
        addEntry={addEntry}
        analysisLanguage={analysisLanguage ?? ''}
        headword={word}
        vernacularLanguage={vernacularLanguage ?? ''}
      />
      {isSubmitting && <p>Adding entry to lexicon...</p>}
      {isSubmitted && <p>Entry added!</p>}
    </div>
  );
};
