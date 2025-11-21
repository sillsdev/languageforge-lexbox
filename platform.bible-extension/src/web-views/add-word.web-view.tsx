import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { DictionaryWebViewProps, IEntryService, PartialEntry } from 'fw-lite-extension';
import { useCallback, useEffect, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

globalThis.webViewComponent = function FwLiteAddWord({
  analysisLanguage,
  projectId,
  vernacularLanguage,
  word,
}: DictionaryWebViewProps) {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [fwLiteNetworkObject, setFwLiteNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);

  useEffect(() => {
    papi.networkObjects
      .get<IEntryService>('fwliteextension.entryService')
      // eslint-disable-next-line promise/always-return
      .then((networkObject) => {
        logger.info('Got network object:', networkObject);
        setFwLiteNetworkObject(networkObject);
      })
      .catch((e) =>
        logger.error(`${localizedStrings['%fwLiteExtension_error_gettingNetworkObject%']}`, e),
      );
  }, [localizedStrings]);

  const addEntry = useCallback(
    async (entry: PartialEntry) => {
      if (!projectId || !fwLiteNetworkObject) {
        const errMissingParam = localizedStrings['%fwLiteExtension_error_missingParam%'];
        if (!projectId) logger.warn(`${errMissingParam}projectId`);
        if (!fwLiteNetworkObject) logger.warn(`${errMissingParam}fwLiteNetworkObject`);
        return;
      }

      setIsSubmitted(false);
      setIsSubmitting(true);
      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const entryId = (await fwLiteNetworkObject.addEntry(projectId, entry))?.id;
      setIsSubmitting(false);
      if (entryId) {
        setIsSubmitted(true);
        await papi.commands.sendCommand('fwLiteExtension.displayEntry', projectId, entryId);
      } else {
        logger.error(`${localizedStrings['%fwLiteExtension_error_failedToAddEntry%']}`);
      }
    },
    [fwLiteNetworkObject, localizedStrings, projectId],
  );

  return (
    <div className="tw-p-4">
      <AddNewEntry
        addEntry={addEntry}
        analysisLanguage={analysisLanguage ?? ''}
        headword={word}
        vernacularLanguage={vernacularLanguage ?? ''}
      />
      {isSubmitting && <p>Adding entry to FieldWorks...</p>}
      {isSubmitted && <p>Entry added!</p>}
    </div>
  );
};
