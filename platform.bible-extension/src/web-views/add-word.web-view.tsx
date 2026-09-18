import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import type { IEntryService, LexiconWebViewProps, PartialEntry } from 'lexicon';
import { useCallback, useEffect, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';

globalThis.webViewComponent = function LexiconAddWord({
  analysisLanguage,
  lexiconCode,
  projectId,
  vernacularLanguage,
  word,
}: LexiconWebViewProps) {
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
      .catch((e) => logger.error('Error getting network object:', e));
  }, []);

  const addEntry = useCallback(
    async (entry: PartialEntry): Promise<boolean> => {
      if (!lexiconCode || !projectId || !lexiconNetworkObject) {
        if (!lexiconCode) logger.warn('Missing required parameter: lexiconCode');
        if (!projectId) logger.warn('Missing required parameter: projectId');
        if (!lexiconNetworkObject) logger.warn('Missing required parameter: lexiconNetworkObject');
        return false;
      }

      setIsSubmitted(false);
      setIsSubmitting(true);
      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      let entryId: string | undefined;
      try {
        entryId = (await lexiconNetworkObject.addEntry(lexiconCode, entry))?.id;
      } finally {
        setIsSubmitting(false);
      }
      if (!entryId) {
        logger.error('Failed to add entry!');
        return false;
      }

      setIsSubmitted(true);
      // The entry is written, so failing to show it must not read as a failed add.
      try {
        await papi.commands.sendCommand('lexicon.displayEntry', projectId, lexiconCode, entryId);
      } catch (e) {
        logger.error('Error displaying the new entry:', e);
      }
      return true;
    },
    [lexiconCode, lexiconNetworkObject, projectId],
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
