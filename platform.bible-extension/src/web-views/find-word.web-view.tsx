import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { IEntry, IEntryService, LexiconWebViewProps, PartialEntry } from 'lexicon';
import { SearchBar } from 'platform-bible-react';
import { debounce } from 'platform-bible-utils';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import AddNewEntryButton from '../components/add-new-entry-button';
import EntryList from '../components/entry-list';
import EntryListWrapper from '../components/entry-list-wrapper';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

globalThis.webViewComponent = function LexiconFindWord({
  analysisLanguage,
  lexiconCode,
  projectId,
  vernacularLanguage,
  word,
}: LexiconWebViewProps) {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [lexiconNetworkObject, setLexiconNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isFetching, setIsFetching] = useState(false);
  const [fetchFailed, setFetchFailed] = useState(false);
  const [searchTerm, setSearchTerm] = useState(word ?? '');
  // Which search the view's state belongs to. Debouncing spaces requests out but does not stop one
  // from outliving the next, so a reply lands only while its request is still the current one.
  const requestIdRef = useRef(0);

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

  const fetchEntries = useCallback(
    async (untrimmedSurfaceForm: string) => {
      if (!lexiconCode || !lexiconNetworkObject) {
        const errMissingParam = localizedStrings['%lexicon_error_missingParam%'];
        if (!lexiconCode) logger.warn(`${errMissingParam}lexiconCode`);
        if (!lexiconNetworkObject) logger.warn(`${errMissingParam}lexiconNetworkObject`);
        return;
      }

      const surfaceForm = untrimmedSurfaceForm.trim();
      if (!surfaceForm) {
        logger.warn('No word provided for search');
        return;
      }

      logger.info(`Fetching entries for ${surfaceForm}`);
      requestIdRef.current += 1;
      const requestId = requestIdRef.current;
      setFetchFailed(false);
      setIsFetching(true);
      try {
        const entries = await lexiconNetworkObject.getEntries(lexiconCode, { surfaceForm });
        if (requestId !== requestIdRef.current) return;
        setMatchingEntries(entries ?? []);
      } catch (e) {
        logger.error('Error fetching entries:', e);
        if (requestId !== requestIdRef.current) return;
        // Drop the last query's entries: kept, they would sit under the new search term as
        // though they answered it.
        setMatchingEntries(undefined);
        setFetchFailed(true);
      } finally {
        if (requestId === requestIdRef.current) setIsFetching(false);
      }
    },
    [lexiconCode, lexiconNetworkObject, localizedStrings],
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
    async (entry: PartialEntry) => {
      if (!lexiconCode || !projectId || !lexiconNetworkObject) {
        const errMissingParam = localizedStrings['%lexicon_error_missingParam%'];
        if (!lexiconCode) logger.warn(`${errMissingParam}lexiconCode`);
        if (!projectId) logger.warn(`${errMissingParam}projectId`);
        if (!lexiconNetworkObject) logger.warn(`${errMissingParam}lexiconNetworkObject`);
        return;
      }

      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const addedEntry = await lexiconNetworkObject.addEntry(lexiconCode, entry);
      if (addedEntry) {
        onSearch(Object.values<string | undefined>(addedEntry.lexemeForm).pop() ?? '');
        await papi.commands.sendCommand(
          'lexicon.displayEntry',
          projectId,
          lexiconCode,
          addedEntry.id,
        );
      } else {
        logger.error(`${localizedStrings['%lexicon_error_failedToAddEntry%']}`);
      }
    },
    [lexiconCode, lexiconNetworkObject, localizedStrings, onSearch, projectId],
  );

  return (
    <EntryListWrapper
      elementHeader={
        <div className="tw:flex tw:gap-2">
          <div className="tw:w-full tw:max-w-72">
            <SearchBar
              isFullWidth
              onSearch={onSearch}
              placeholder={localizedStrings['%lexicon_findWord_textField%']}
              value={searchTerm}
            />
          </div>

          <div>
            <AddNewEntryButton
              addEntry={addEntry}
              analysisLanguage={analysisLanguage ?? ''}
              headword={searchTerm}
              vernacularLanguage={vernacularLanguage ?? ''}
            />
          </div>
        </div>
      }
      elementList={
        matchingEntries ? (
          <EntryList
            analysisLanguage={analysisLanguage ?? ''}
            entries={matchingEntries}
            vernacularLanguage={vernacularLanguage ?? ''}
          />
        ) : undefined
      }
      hasError={fetchFailed}
      isLoading={isFetching}
      hasItems={!!matchingEntries?.length}
    />
  );
};
