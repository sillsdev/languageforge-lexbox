import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type {
  IEntry,
  IEntryService,
  ISemanticDomain,
  LexiconWebViewProps,
  PartialEntry,
} from 'lexicon';
import { Network } from 'lucide-react';
import { Label, SearchBar } from 'platform-bible-react';
import { useCallback, useEffect, useState } from 'react';
import AddNewEntryButton from '../components/add-new-entry-button';
import EntryList from '../components/entry-list';
import EntryListWrapper from '../components/entry-list-wrapper';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';
import type { EntryLookupRequest } from '../utils/use-entry-lookup';
import useEntryLookup from '../utils/use-entry-lookup';
import { domainText } from '../utils/entry-display-text';

globalThis.webViewComponent = function LexiconFindRelatedWords({
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
  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [relatedEntries, setRelatedEntries] = useState<IEntry[] | undefined>();
  const [searchTerm, setSearchTerm] = useState(word ?? '');
  const [selectedDomain, setSelectedDomain] = useState<ISemanticDomain | undefined>();
  // One count for both lookups: either one starting means the user has moved on from the other.
  const { didFail, isPending, lookup } = useEntryLookup();

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

  useEffect(() => {
    setSelectedDomain(undefined);
    const domains = matchingEntries?.flatMap((e) => e.senses.flatMap((s) => s.semanticDomains));
    if (!domains?.length) return;
    if (domains.every((d) => d.code === domains[0].code)) setSelectedDomain(domains[0]);
  }, [matchingEntries]);

  const entriesLookup = useCallback(
    (untrimmedSurfaceForm: string): EntryLookupRequest<IEntry[] | undefined> | undefined => {
      if (!lexiconCode || !lexiconNetworkObject) {
        const errMissingParam = localizedStrings['%lexicon_error_missingParam%'];
        if (!lexiconCode) logger.warn(`${errMissingParam}lexiconCode`);
        if (!lexiconNetworkObject) logger.warn(`${errMissingParam}lexiconNetworkObject`);
        return undefined;
      }

      const surfaceForm = untrimmedSurfaceForm.trim();
      if (!surfaceForm) {
        logger.warn('No word provided for search');
        return undefined;
      }

      logger.info(`Fetching entries for ${surfaceForm}`);
      return {
        failureMessage: 'Error fetching entries:',
        // Drop what the last query found: kept, it would sit under the new search term as though
        // it answered it. The domain derived from it would also file a new entry under a domain
        // belonging to a search the user has left.
        onFailure: () => {
          setMatchingEntries(undefined);
          setRelatedEntries(undefined);
        },
        // Only entries and senses carrying a semantic domain can be related to anything.
        onResult: (entries) =>
          setMatchingEntries(
            (entries ?? [])
              .map((e) => ({ ...e, senses: e.senses.filter((s) => s.semanticDomains.length) }))
              .filter((e) => e.senses.length),
          ),
        request: () => lexiconNetworkObject.getEntries(lexiconCode, { surfaceForm }),
      };
    },
    [lexiconCode, lexiconNetworkObject, localizedStrings],
  );

  const fetchRelatedEntries = useCallback(
    (semanticDomain: string) => {
      if (!lexiconCode || !lexiconNetworkObject) {
        const errMissingParam = localizedStrings['%lexicon_error_missingParam%'];
        if (!lexiconCode) logger.warn(`${errMissingParam}lexiconCode`);
        if (!lexiconNetworkObject) logger.warn(`${errMissingParam}lexiconNetworkObject`);
        return;
      }

      logger.info(`Fetching entries in semantic domain ${semanticDomain}`);
      lookup.run({
        failureMessage: 'Error fetching related entries:',
        onFailure: () => setRelatedEntries(undefined),
        onResult: (entries) => setRelatedEntries(entries ?? []),
        request: () => lexiconNetworkObject.getEntries(lexiconCode, { semanticDomain }),
      });
    },
    [lexiconCode, lexiconNetworkObject, localizedStrings, lookup],
  );

  useEffect(() => {
    if (selectedDomain) fetchRelatedEntries(selectedDomain.code);
  }, [fetchRelatedEntries, selectedDomain]);

  const onSearch = useCallback(
    (searchQuery: string) => {
      setSearchTerm(searchQuery);
      if (!searchQuery.trim()) {
        // The query is withdrawn, so nothing should answer it and what is on screen answers a
        // query that is gone.
        lookup.reset();
        setMatchingEntries(undefined);
        setRelatedEntries(undefined);
        return;
      }
      lookup.schedule(() => entriesLookup(searchQuery));
    },
    [entriesLookup, lookup],
  );

  const addEntryInDomain = useCallback(
    async (entry: PartialEntry): Promise<boolean> => {
      if (
        !lexiconCode ||
        !lexiconNetworkObject ||
        !projectId ||
        !selectedDomain ||
        !entry.senses?.length
      ) {
        const errMissingParam = localizedStrings['%lexicon_error_missingParam%'];
        if (!lexiconCode) logger.warn(`${errMissingParam}lexiconCode`);
        if (!lexiconNetworkObject) logger.warn(`${errMissingParam}lexiconNetworkObject`);
        if (!projectId) logger.warn(`${errMissingParam}projectId`);
        if (!selectedDomain) logger.warn(`${errMissingParam}selectedDomain`);
        if (!entry.senses?.length) logger.warn('Cannot add entry without senses');
        return false;
      }

      if (!entry.senses[0].semanticDomains) entry.senses[0].semanticDomains = [];
      entry.senses[0].semanticDomains.push(selectedDomain);
      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const addedEntry = await lexiconNetworkObject.addEntry(lexiconCode, entry);
      if (!addedEntry) {
        logger.error(`${localizedStrings['%lexicon_error_failedToAddEntry%']}`);
        return false;
      }

      onSearch(Object.values<string | undefined>(addedEntry.lexemeForm).pop() ?? '');
      await papi.commands.sendCommand(
        'lexicon.displayEntry',
        projectId,
        lexiconCode,
        addedEntry.id,
      );
      return true;
    },
    [lexiconCode, lexiconNetworkObject, localizedStrings, onSearch, projectId, selectedDomain],
  );

  return (
    <EntryListWrapper
      elementHeader={
        <div className="tw:flex tw:flex-col tw:gap-2">
          <div className="tw:flex tw:gap-2">
            <div className="tw:w-full tw:max-w-lg">
              <SearchBar
                isFullWidth
                onSearch={onSearch}
                placeholder={localizedStrings['%lexicon_findRelatedWord_textField%']}
                value={searchTerm}
              />
            </div>

            {selectedDomain && (
              <div>
                <AddNewEntryButton
                  addEntry={addEntryInDomain}
                  analysisLanguage={analysisLanguage ?? ''}
                  headword={searchTerm}
                  vernacularLanguage={vernacularLanguage ?? ''}
                />
              </div>
            )}
          </div>

          {matchingEntries && !selectedDomain && (
            <h3 className="tw:font-semibold tw:m-2">
              {localizedStrings['%lexicon_findRelatedWord_selectInstruction%']}
            </h3>
          )}

          {selectedDomain && (
            <h3 className="tw:flex tw:font-semibold tw:gap-1 tw:items-center tw:m-2">
              <Network className="tw:inline tw:mr-1 tw:h-4 tw:w-4" />
              {domainText(selectedDomain, analysisLanguage)}
            </h3>
          )}
        </div>
      }
      elementList={
        /* eslint-disable no-nested-ternary */
        !matchingEntries ? undefined : !selectedDomain ? (
          <EntryList
            analysisLanguage={analysisLanguage ?? ''}
            entries={matchingEntries}
            onClickSemanticDomain={setSelectedDomain}
            vernacularLanguage={vernacularLanguage ?? ''}
          />
        ) : !relatedEntries?.length ? (
          <div className="tw:flex tw:justify-center tw:m-4">
            <Label>{localizedStrings['%lexicon_findRelatedWord_noResultsInDomain%']}</Label>
          </div>
        ) : (
          <EntryList
            analysisLanguage={analysisLanguage ?? ''}
            entries={relatedEntries}
            vernacularLanguage={vernacularLanguage ?? ''}
          />
        )
      }
      hasError={didFail}
      isLoading={isPending}
      hasItems={!!matchingEntries?.length}
    />
  );
};
