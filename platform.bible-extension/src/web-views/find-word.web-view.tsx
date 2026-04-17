import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { DictionaryWebViewProps, IEntry, IEntryService, PartialEntry } from 'lexicon';
import { SearchBar } from 'platform-bible-react';
import { debounce } from 'platform-bible-utils';
import { useCallback, useEffect, useMemo, useState } from 'react';
import AddNewEntryButton from '../components/add-new-entry-button';
import EntryList from '../components/entry-list';
import EntryListWrapper from '../components/entry-list-wrapper';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

globalThis.webViewComponent = function LexiconFindWord({
  analysisLanguage,
  projectId,
  vernacularLanguage,
  word,
}: DictionaryWebViewProps) {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [lexiconNetworkObject, setLexiconNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isFetching, setIsFetching] = useState(false);
  const [searchTerm, setSearchTerm] = useState(word ?? '');

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
      const entries = await lexiconNetworkObject.getEntries(projectId, { surfaceForm });
      setIsFetching(false);
      setMatchingEntries(entries ?? []);
    },
    [lexiconNetworkObject, localizedStrings, projectId],
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
      if (!projectId || !lexiconNetworkObject) {
        const errMissingParam = localizedStrings['%lexicon_error_missingParam%'];
        if (!projectId) logger.warn(`${errMissingParam}projectId`);
        if (!lexiconNetworkObject) logger.warn(`${errMissingParam}lexiconNetworkObject`);
        return;
      }

      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const addedEntry = await lexiconNetworkObject.addEntry(projectId, entry);
      if (addedEntry) {
        onSearch(Object.values<string | undefined>(addedEntry.lexemeForm).pop() ?? '');
        await papi.commands.sendCommand('lexicon.displayEntry', projectId, addedEntry.id);
      } else {
        logger.error(`${localizedStrings['%lexicon_error_failedToAddEntry%']}`);
      }
    },
    [lexiconNetworkObject, localizedStrings, onSearch, projectId],
  );

  return (
    <EntryListWrapper
      elementHeader={
        <div className="tw-flex tw-gap-2">
          <div className="tw-max-w-72">
            <SearchBar
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
      isLoading={isFetching}
      hasItems={!!matchingEntries?.length}
    />
  );
};
