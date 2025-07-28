import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import type {
  IEntry,
  IEntryService,
  IMultiString,
  ISemanticDomain,
  WordWebViewOptions,
} from 'fw-lite-extension';
import { Card, SearchBar } from 'platform-bible-react';
import { debounce } from 'platform-bible-utils';
import { useCallback, useEffect, useMemo, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';
import EntryCard from '../components/entry-card';

globalThis.webViewComponent = function fwLiteFindRelatedWords({
  projectId,
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
  const [semanticDomains, setSemanticDomains] = useState<ISemanticDomain[] | undefined>();

  useEffect(() => {
    void papi.networkObjects
      .get<IEntryService>('fwliteextension.entryService')
      .then((networkObject) => {
        logger.info('Got network object:', networkObject);
        setFwLiteNetworkObject(networkObject);
      });
  }, []);

  useEffect(() => {
    if (!matchingEntries) {
      setSemanticDomains(undefined);
      return;
    }

    const domains = matchingEntries.flatMap((entry) =>
      entry.senses.flatMap((s) => s.semanticDomains),
    );
    const codes = [...new Set(domains.map((dom) => dom.code).filter((c) => c))].sort();
    setSemanticDomains(codes.map((code) => domains.find((dom) => dom.code == code)!));
  }, [matchingEntries]);

  useEffect(() => {
    setSelectedDomain(semanticDomains?.length === 1 ? semanticDomains[0] : undefined);
  }, [semanticDomains]);

  useEffect(() => {
    if (selectedDomain) fetchRelatedEntries(selectedDomain.code);
  }, [selectedDomain]);

  const addEntryInDomain = useCallback(
    async (entry: Partial<IEntry>) => {
      if (!fwLiteNetworkObject || !projectId || !selectedDomain || !entry.senses?.length) {
        if (!fwLiteNetworkObject) logger.warn('Missing required parameter: fwLiteNetworkObject');
        if (!projectId) logger.warn('Missing required parameter: projectId');
        if (!selectedDomain) logger.warn('Missing required parameter: selectedDomain');
        if (!entry.senses?.length) logger.warn('Cannot add entry without senses');
        return;
      }

      entry.senses[0].semanticDomains.push(selectedDomain);
      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const addedEntry = await fwLiteNetworkObject.addEntry(projectId, entry);
      if (addedEntry) {
        onSearch(Object.values(addedEntry.lexemeForm as IMultiString).pop() ?? '');
        await papi.commands.sendCommand('fwLiteExtension.displayEntry', projectId, addedEntry.id);
      } else {
        logger.error('Failed to add entry!');
      }
    },
    [fwLiteNetworkObject, projectId, selectedDomain],
  );

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
      <SearchBar
        placeholder="Find related words in dictionary..."
        value={searchTerm}
        onSearch={onSearch}
      />

      {isFetching && <p>Loading...</p>}
      {!isFetching && !matchingEntries?.length && <p>No matching entries.</p>}
      {semanticDomains && !semanticDomains.length && (
        <p>No matching entries with a semantic domain.</p>
      )}
      {matchingEntries && semanticDomains && semanticDomains.length > 1 && !selectedDomain && (
        <SelectSemanticDomain
          entries={matchingEntries}
          selectDomain={setSelectedDomain}
          semanticDomains={semanticDomains}
        />
      )}
      {selectedDomain && relatedEntries && (
        <EntriesInSemanticDomain entries={relatedEntries} semanticDomain={selectedDomain} />
      )}

      {selectedDomain && (
        <AddNewEntry
          addEntry={addEntryInDomain}
          analysisLang="en"
          headword={searchTerm}
          vernacularLang="en"
        />
      )}
    </div>
  );
};

interface EntriesInSemanticDomainProps {
  entries: IEntry[];
  semanticDomain: ISemanticDomain;
}

// eslint-disable-next-line @typescript-eslint/naming-convention
function EntriesInSemanticDomain({ entries, semanticDomain }: EntriesInSemanticDomainProps) {
  return (
    <>
      <Card>{`${semanticDomain.code}: ${semanticDomain.name}`}</Card>
      {entries.length ? (
        entries.map((entry) => <EntryCard entry={entry} />)
      ) : (
        <p>No entries in this semantic domain.</p>
      )}
    </>
  );
}

function filterEntriesToSensesInDomain(entries: IEntry[], domainCode: string): IEntry[] {
  return entries
    .map((entry) => ({
      ...entry,
      senses: entry.senses.filter((s) => s.semanticDomains.findIndex((d) => d.code === domainCode)),
    }))
    .filter((entry) => entry.senses.length);
}

interface SelectSemanticDomainProps {
  entries: IEntry[];
  selectDomain: (domain: ISemanticDomain) => void;
  semanticDomains: ISemanticDomain[];
}

// eslint-disable-next-line @typescript-eslint/naming-convention
function SelectSemanticDomain({
  entries,
  selectDomain,
  semanticDomains,
}: SelectSemanticDomainProps) {
  if (!semanticDomains.length || !entries.length) {
    return <p>No semantic domains to choose from.</p>;
  }

  return (
    <>
      {semanticDomains.map((dom) => {
        const entriesInDomain = filterEntriesToSensesInDomain(entries, dom.code);
        if (!entriesInDomain.length) return;
        return (
          <>
            <hr />
            <button onClick={() => void selectDomain(dom)}>{`Select domain ${
              dom.code
            }: ${JSON.stringify(dom.name)}`}</button>
            {entriesInDomain.map((entry) => (
              <EntryCard entry={entry} />
            ))}
          </>
        );
      })}
    </>
  );
}
