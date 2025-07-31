import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import type { IEntry, IEntryService, IMultiString, WordWebViewOptions } from 'fw-lite-extension';
import { SearchBar } from 'platform-bible-react';
import { debounce } from 'platform-bible-utils';
import { useCallback, useEffect, useMemo, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';
import EntryCard from '../components/entry-card';

/* eslint-disable react-hooks/rules-of-hooks */

globalThis.webViewComponent = function fwLiteFindWord({ projectId, word }: WordWebViewOptions) {
  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [fwLiteNetworkObject, setFwLiteNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isFetching, setIsFetching] = useState(false);
  const [searchTerm, setSearchTerm] = useState(word ?? '');

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

  const fetchEntries = useCallback(
    async (untrimmedSurfaceForm: string) => {
      if (!projectId || !fwLiteNetworkObject) {
        if (!projectId) logger.warn('Missing required parameter: projectId');
        if (!fwLiteNetworkObject) logger.warn('Missing required parameter: fwLiteNetworkObject');
        return;
      }

      const surfaceForm = untrimmedSurfaceForm.trim();
      if (!surfaceForm) {
        logger.warn('No word provided for search');
        return;
      }

      logger.info(`Fetching entries for ${surfaceForm}`);
      setIsFetching(true);
      const entries = await fwLiteNetworkObject.getEntries(projectId, { surfaceForm });
      setIsFetching(false);
      setMatchingEntries(entries ?? []);
    },
    [fwLiteNetworkObject, projectId],
  );

  const debouncedFetchEntries = useMemo(() => debounce(fetchEntries, 500), [fetchEntries]);

  const onSearch = useCallback(
    (searchQuery: string) => {
      setSearchTerm(searchQuery);
      debouncedFetchEntries(searchQuery);
    },
    [debouncedFetchEntries],
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
        // eslint-disable-next-line no-type-assertion/no-type-assertion
        onSearch(Object.values(addedEntry.lexemeForm as IMultiString).pop() ?? '');
        await papi.commands.sendCommand('fwLiteExtension.displayEntry', projectId, addedEntry.id);
      } else {
        logger.error('Failed to add entry!');
      }
    },
    [fwLiteNetworkObject, onSearch, projectId],
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
