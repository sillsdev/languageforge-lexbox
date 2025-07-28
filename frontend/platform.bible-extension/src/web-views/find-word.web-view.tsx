import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import type { IEntry, IEntryService, IMultiString, WordWebViewOptions } from 'fw-lite-extension';
import { SearchBar } from 'platform-bible-react';
import { debounce } from 'platform-bible-utils';
import { useCallback, useEffect, useMemo, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';
import EntryCard from '../components/entry-card';

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
        if (!projectId) logger.warn('Missing required parameter: projectId');
        if (!fwLiteNetworkObject) logger.warn('Missing required parameter: fwLiteNetworkObject');
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
    async (entry: Partial<IEntry>) => {
      if (!projectId || !fwLiteNetworkObject) {
        if (!projectId) logger.warn('Missing required parameter: projectId');
        if (!fwLiteNetworkObject) logger.warn('Missing required parameter: fwLiteNetworkObject');
        return;
      }

      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const addedEntry = await fwLiteNetworkObject.addEntry(projectId, entry);
      if (addedEntry) {
        onSearch(Object.values(addedEntry.lexemeForm as IMultiString).pop() ?? '');
        await papi.commands.sendCommand('fwLiteExtension.displayEntry', projectId, addedEntry.id);
      } else {
        logger.error('Failed to add entry!');
      }
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
        <EntryCard entry={entry} />
      ))}

      <AddNewEntry
        addEntry={addEntry}
        analysisLang="en"
        headword={searchTerm}
        vernacularLang="en"
      />
    </div>
  );
};
