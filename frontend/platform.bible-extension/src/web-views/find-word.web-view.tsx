import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import type { IEntry, IEntryService, WordWebViewOptions } from 'fw-lite-extension';
import { Card, CardContent, CardHeader, SearchBar } from 'platform-bible-react';
import { debounce } from 'platform-bible-utils';
import { useCallback, useEffect, useMemo, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';

globalThis.webViewComponent = function fwLiteFindWord({ projectId, word }: WordWebViewOptions) {
  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [fwLiteNetworkObject, setFwLiteNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isFetching, setIsFetching] = useState(false);
  const [searchTerm, setSearchTerm] = useState(word ?? '');

  useEffect(() => {
    void papi.networkObjects
      .get<IEntryService>('fwliteextension.entryService')
      .then((networkObject) => {
        logger.info('Got network object:', networkObject);
        setFwLiteNetworkObject(networkObject);
      });
  }, []);

  const fetchEntries = useCallback(
    async (word: string) => {
      if (!projectId || !fwLiteNetworkObject) {
        logger.warn(
          `Missing required parameters: projectId=${projectId}, fwLiteNetworkObject=${fwLiteNetworkObject}`,
        );
        return;
      }

      word = word.trim();
      if (!word) {
        logger.warn('No word provided for search');
        return;
      }

      logger.info(`Fetching entries for ${word}`);
      setIsFetching(true);
      const entries = await fwLiteNetworkObject.getEntries(projectId, { surfaceForm: word });
      setIsFetching(false);
      setMatchingEntries(entries ?? []);
    },
    [fwLiteNetworkObject, projectId],
  );

  const addEntry = useCallback(
    async (entry: IEntry) => {
      if (!projectId || !fwLiteNetworkObject) {
        logger.warn(
          `Missing required parameters: projectId=${projectId}, fwLiteNetworkObject=${fwLiteNetworkObject}`,
        );
        return;
      }

      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      await fwLiteNetworkObject.addEntry(projectId, entry);
    },
    [fwLiteNetworkObject, projectId],
  );

  const debouncedFetchEntries = useMemo(() => debounce(fetchEntries, 500), [fetchEntries]);

  const onSearch = useCallback(
    (searchQuery: string) => {
      setSearchTerm(searchQuery);
      void debouncedFetchEntries(searchQuery);
    },
    [debouncedFetchEntries],
  );

  return (
    <div>
      <SearchBar placeholder="Find in dictionary..." value={searchTerm} onSearch={onSearch} />

      {isFetching && <p>Loading...</p>}
      {!matchingEntries?.length && !isFetching && <p>No matching entries</p>}
      {matchingEntries?.map((entry) => (
        <Card key={entry.id}>
          <CardHeader>
            {Object.keys(entry.citationForm).length
              ? JSON.stringify(entry.citationForm)
              : JSON.stringify(entry.lexemeForm)}
          </CardHeader>
          <CardContent>
            <p>Senses:</p>
            {entry.senses.map((sense) => (
              <div key={sense.id}>
                <strong>Gloss: {JSON.stringify(sense.gloss)}</strong>
                <p>Definition: {JSON.stringify(sense.definition)}</p>
              </div>
            ))}
          </CardContent>
        </Card>
      ))}

      <AddNewEntry
        addEntry={addEntry}
        analysisLang="en"
        headword={searchTerm}
        vernacularLang="qaa"
      />
    </div>
  );
};
