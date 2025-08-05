import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import type {
  IEntry,
  IEntryService,
  ISemanticDomain,
  PartialEntry,
  WordWebViewOptions,
} from 'fw-lite-extension';
import { Card, SearchBar } from 'platform-bible-react';
import { debounce } from 'platform-bible-utils';
import { type ReactElement, useCallback, useEffect, useMemo, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';
import EntryCard from '../components/entry-card';

/* eslint-disable react-hooks/rules-of-hooks */

globalThis.webViewComponent = function fwLiteFindRelatedWords({
  analysisLanguage,
  projectId,
  vernacularLanguage,
  word,
}: WordWebViewOptions) {
  const [fwLiteNetworkObject, setFwLiteNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isFetching, setIsFetching] = useState(false);
  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [relatedEntries, setRelatedEntries] = useState<IEntry[] | undefined>();
  const [searchTerm, setSearchTerm] = useState(word ?? '');
  const [selectedDomain, setSelectedDomain] = useState<ISemanticDomain | undefined>();

  useEffect(() => {
    papi.networkObjects
      .get<IEntryService>('fwliteextension.entryService')
      // eslint-disable-next-line promise/always-return
      .then((networkObject) => {
        logger.info('Got network object:', networkObject);
        setFwLiteNetworkObject(networkObject);
      })
      .catch((e) => logger.error('Error getting network object:', JSON.stringify(e)));
  }, []);

  useEffect(() => {
    setSelectedDomain(undefined);
    const domains = matchingEntries?.flatMap((e) => e.senses.flatMap((s) => s.semanticDomains));
    if (!domains?.length) return;
    if (domains.every((d) => d.code === domains[0].code)) setSelectedDomain(domains[0]);
  }, [matchingEntries]);

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
      let entries = (await fwLiteNetworkObject.getEntries(projectId, { surfaceForm })) ?? [];
      // Only consider entries and senses with at least one semantic domain.
      entries = entries
        .map((e) => ({ ...e, senses: e.senses.filter((s) => s.semanticDomains.length) }))
        .filter((e) => e.senses.length);
      setIsFetching(false);
      setMatchingEntries(entries);
    },
    [fwLiteNetworkObject, projectId],
  );

  const fetchRelatedEntries = useCallback(
    async (semanticDomain: string) => {
      if (!projectId || !fwLiteNetworkObject) {
        if (!projectId) logger.warn('Missing required parameter: projectId');
        if (!fwLiteNetworkObject) logger.warn('Missing required parameter: fwLiteNetworkObject');
        return;
      }

      logger.info(`Fetching entries in semantic domain ${semanticDomain}`);
      setIsFetching(true);
      const entries = await fwLiteNetworkObject.getEntries(projectId, { semanticDomain });
      setIsFetching(false);
      setRelatedEntries(entries ?? []);
    },
    [fwLiteNetworkObject, projectId],
  );

  useEffect(() => {
    if (selectedDomain) fetchRelatedEntries(selectedDomain.code);
  }, [fetchRelatedEntries, selectedDomain]);

  const debouncedFetchEntries = useMemo(() => debounce(fetchEntries, 500), [fetchEntries]);

  const onSearch = useCallback(
    (searchQuery: string) => {
      setSearchTerm(searchQuery);
      debouncedFetchEntries(searchQuery);
    },
    [debouncedFetchEntries],
  );

  const addEntryInDomain = useCallback(
    async (entry: PartialEntry) => {
      if (!fwLiteNetworkObject || !projectId || !selectedDomain || !entry.senses?.length) {
        if (!fwLiteNetworkObject) logger.warn('Missing required parameter: fwLiteNetworkObject');
        if (!projectId) logger.warn('Missing required parameter: projectId');
        if (!selectedDomain) logger.warn('Missing required parameter: selectedDomain');
        if (!entry.senses?.length) logger.warn('Cannot add entry without senses');
        return;
      }

      if (!entry.senses[0].semanticDomains) entry.senses[0].semanticDomains = [];
      entry.senses[0].semanticDomains.push(selectedDomain);
      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const addedEntry = await fwLiteNetworkObject.addEntry(projectId, entry);
      if (addedEntry) {
        onSearch(Object.values<string | undefined>(addedEntry.lexemeForm).pop() ?? '');
        await papi.commands.sendCommand('fwLiteExtension.displayEntry', projectId, addedEntry.id);
      } else {
        logger.error('Failed to add entry!');
      }
    },
    [fwLiteNetworkObject, onSearch, projectId, selectedDomain],
  );

  return (
    <div>
      <SearchBar
        placeholder="Find related words in dictionary..."
        value={searchTerm}
        onSearch={onSearch}
      />

      {isFetching && <p>Loading...</p>}
      {!isFetching && !matchingEntries?.length && (
        <p>No matching entries with a semantic domain.</p>
      )}

      {matchingEntries && !selectedDomain && (
        <>
          <p>Select a semantic domain for related words in that domain</p>
          {matchingEntries.map((e) => (
            <EntryCard entry={e} key={e.id} onClickSemanticDomain={setSelectedDomain} />
          ))}
        </>
      )}

      {selectedDomain && relatedEntries && (
        <EntriesInSemanticDomain entries={relatedEntries} semanticDomain={selectedDomain} />
      )}

      {selectedDomain && (
        <AddNewEntry
          addEntry={addEntryInDomain}
          analysisLang={analysisLanguage ?? ''}
          headword={searchTerm}
          vernacularLang={vernacularLanguage ?? ''}
        />
      )}
    </div>
  );
};

interface EntriesInSemanticDomainProps {
  entries: IEntry[];
  semanticDomain: ISemanticDomain;
}

function EntriesInSemanticDomain({
  entries,
  semanticDomain,
}: EntriesInSemanticDomainProps): ReactElement {
  return (
    <>
      <Card>{`${semanticDomain.code}: ${JSON.stringify(semanticDomain.name)}`}</Card>
      {entries.length ? (
        entries.map((entry) => <EntryCard entry={entry} key={entry.id} />)
      ) : (
        <p>No entries in this semantic domain.</p>
      )}
    </>
  );
}
