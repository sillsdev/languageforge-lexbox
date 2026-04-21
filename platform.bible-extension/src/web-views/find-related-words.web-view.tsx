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
import { debounce } from 'platform-bible-utils';
import { useCallback, useEffect, useMemo, useState } from 'react';
import AddNewEntryButton from '../components/add-new-entry-button';
import EntryList from '../components/entry-list';
import EntryListWrapper from '../components/entry-list-wrapper';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';
import { domainText } from '../utils/entry-display-text';

globalThis.webViewComponent = function LexiconFindRelatedWords({
  analysisLanguage,
  projectId,
  vernacularLanguage,
  word,
}: LexiconWebViewProps) {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [lexiconNetworkObject, setLexiconNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isFetching, setIsFetching] = useState(false);
  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [relatedEntries, setRelatedEntries] = useState<IEntry[] | undefined>();
  const [searchTerm, setSearchTerm] = useState(word ?? '');
  const [selectedDomain, setSelectedDomain] = useState<ISemanticDomain | undefined>();

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

  const fetchEntries = useCallback(
    async (untrimmedSurfaceForm: string) => {
      if (!projectId || !lexiconNetworkObject) {
        const errMissingParam = localizedStrings['%lexicon_error_missingParam%'];
        if (!projectId) logger.warn(`${errMissingParam}projectId`);
        if (!lexiconNetworkObject) logger.warn(`${errMissingParam}lexiconNetworkObject`);
        return;
      }

      const surfaceForm = untrimmedSurfaceForm.trim();
      if (!surfaceForm) {
        logger.warn('No word provided for search');
        return;
      }

      logger.info(`Fetching entries for ${surfaceForm}`);
      setIsFetching(true);
      let entries = (await lexiconNetworkObject.getEntries(projectId, { surfaceForm })) ?? [];
      // Only consider entries and senses with at least one semantic domain.
      entries = entries
        .map((e) => ({ ...e, senses: e.senses.filter((s) => s.semanticDomains.length) }))
        .filter((e) => e.senses.length);
      setIsFetching(false);
      setMatchingEntries(entries);
    },
    [lexiconNetworkObject, localizedStrings, projectId],
  );

  const fetchRelatedEntries = useCallback(
    async (semanticDomain: string) => {
      if (!projectId || !lexiconNetworkObject) {
        const errMissingParam = localizedStrings['%lexicon_error_missingParam%'];
        if (!projectId) logger.warn(`${errMissingParam}projectId`);
        if (!lexiconNetworkObject) logger.warn(`${errMissingParam}lexiconNetworkObject`);
        return;
      }

      logger.info(`Fetching entries in semantic domain ${semanticDomain}`);
      setIsFetching(true);
      const entries = await lexiconNetworkObject.getEntries(projectId, { semanticDomain });
      setIsFetching(false);
      setRelatedEntries(entries ?? []);
    },
    [lexiconNetworkObject, localizedStrings, projectId],
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
      if (!lexiconNetworkObject || !projectId || !selectedDomain || !entry.senses?.length) {
        const errMissingParam = localizedStrings['%lexicon_error_missingParam%'];
        if (!lexiconNetworkObject) logger.warn(`${errMissingParam}lexiconNetworkObject`);
        if (!projectId) logger.warn(`${errMissingParam}projectId`);
        if (!selectedDomain) logger.warn(`${errMissingParam}selectedDomain`);
        if (!entry.senses?.length) logger.warn('Cannot add entry without senses');
        return;
      }

      if (!entry.senses[0].semanticDomains) entry.senses[0].semanticDomains = [];
      entry.senses[0].semanticDomains.push(selectedDomain);
      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const addedEntry = await lexiconNetworkObject.addEntry(projectId, entry);
      if (addedEntry) {
        onSearch(Object.values<string | undefined>(addedEntry.lexemeForm).pop() ?? '');
        await papi.commands.sendCommand('lexicon.displayEntry', projectId, addedEntry.id);
      } else {
        logger.error(`${localizedStrings['%lexicon_error_failedToAddEntry%']}`);
      }
    },
    [lexiconNetworkObject, localizedStrings, onSearch, projectId, selectedDomain],
  );

  return (
    <EntryListWrapper
      elementHeader={
        <div className="tw-flex tw-flex-col tw-gap-2">
          <div className="tw-flex tw-gap-2">
            <div className="tw-max-w-128">
              <SearchBar
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
            <h3 className="tw-font-semibold tw-m-2">
              {localizedStrings['%lexicon_findRelatedWord_selectInstruction%']}
            </h3>
          )}

          {selectedDomain && (
            <h3 className="tw-flex tw-font-semibold tw-gap-1 tw-items-center tw-m-2">
              <Network className="tw-inline tw-mr-1 tw-h-4 tw-w-4" />
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
          <div className="tw-flex tw-justify-center tw-m-4 ">
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
      isLoading={isFetching}
      hasItems={!!matchingEntries?.length}
    />
  );
};
