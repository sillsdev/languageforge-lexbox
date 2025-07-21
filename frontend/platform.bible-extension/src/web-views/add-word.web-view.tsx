import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import type { IEntry, IEntryService, WordWebViewOptions } from 'fw-lite-extension';
import { useCallback, useEffect, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';

globalThis.webViewComponent = function fwLiteAddWord({ projectId, word }: WordWebViewOptions) {
  const [fwLiteNetworkObject, setFwLiteNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    void papi.networkObjects
      .get<IEntryService>('fwliteextension.entryService')
      .then((networkObject) => {
        logger.info('Got network object:', networkObject);
        setFwLiteNetworkObject(networkObject);
      });
  }, []);

  const addEntry = useCallback(
    async (entry: IEntry) => {
      if (!projectId || !fwLiteNetworkObject) {
        logger.warn(
          `Missing required parameters: projectId=${projectId}, fwLiteNetworkObject=${fwLiteNetworkObject}`,
        );
        return;
      }

      setIsSubmitting(true);
      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      await fwLiteNetworkObject.addEntry(projectId, entry);
      setIsSubmitting(false);
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
    </div>
  );
};
