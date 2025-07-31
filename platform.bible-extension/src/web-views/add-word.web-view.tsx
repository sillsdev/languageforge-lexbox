import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import type { IEntryService, PartialEntry, WordWebViewOptions } from 'fw-lite-extension';
import { useCallback, useEffect, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';

/* eslint-disable react-hooks/rules-of-hooks */

globalThis.webViewComponent = function fwLiteAddWord({ projectId, word }: WordWebViewOptions) {
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
      .catch((err) => logger.error(err));
  }, []);

  const addEntry = useCallback(
    async (entry: PartialEntry) => {
      if (!projectId || !fwLiteNetworkObject) {
        if (!projectId) logger.warn('Missing required parameter: projectId');
        if (!fwLiteNetworkObject) logger.warn('Missing required parameter: fwLiteNetworkObject');
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
        logger.error('Failed to add entry!');
      }
    },
    [fwLiteNetworkObject, projectId],
  );

  return (
    <div>
      <AddNewEntry
        addEntry={addEntry}
        analysisLang="en"
        headword={word}
        isAdding
        vernacularLang="en"
      />
      {isSubmitting && <p>Adding entry to FieldWorks...</p>}
      {isSubmitted && <p>Entry added!</p>}
    </div>
  );
};
