import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type {
  DictionaryWebViewProps,
  IEntry,
  IEntryService,
  PartialEntry,
} from 'dictionary';
import { SearchBar } from 'platform-bible-react';
import { debounce } from 'platform-bible-utils';
import { useCallback, useEffect, useMemo, useState } from 'react';
import AddNewEntryButton from '../components/add-new-entry-button';
import DictionaryList from '../components/dictionary-list';
import DictionaryListWrapper from '../components/dictionary-list-wrapper';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

globalThis.webViewComponent = function DictionaryFindWord({
  analysisLanguage,
  projectId,
  vernacularLanguage,
  word,
}: DictionaryWebViewProps) {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [dictionaryNetworkObject, setDictionaryNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isFetching, setIsFetching] = useState(false);
  const [searchTerm, setSearchTerm] = useState(word ?? '');

  useEffect(() => {
    papi.networkObjects
      .get<IEntryService>('dictionary.entryService')
      // eslint-disable-next-line promise/always-return
      .then((networkObject) => {
        logger.info('Got network object:', networkObject);
        setDictionaryNetworkObject(networkObject);
      })
      .catch((e) =>
        logger.error(`${localizedStrings['%dictionary_error_gettingNetworkObject%']}`, e),
      );
  }, [localizedStrings]);

  const fetchEntries = useCallback(
    async (untrimmedSurfaceForm: string) => {
      if (!projectId || !dictionaryNetworkObject) {
        const errMissingParam = localizedStrings['%dictionary_error_missingParam%'];
        if (!projectId) logger.warn(`${errMissingParam}projectId`);
        if (!dictionaryNetworkObject) logger.warn(`${errMissingParam}dictionaryNetworkObject`);
        return;
      }

      const surfaceForm = untrimmedSurfaceForm.trim();
      if (!surfaceForm) {
        logger.warn('No word provided for search');
        return;
      }

      logger.info(`Fetching entries for ${surfaceForm}`);
      setIsFetching(true);
      const entries = await dictionaryNetworkObject.getEntries(projectId, { surfaceForm });
      setIsFetching(false);
      setMatchingEntries(entries ?? []);
    },
    [dictionaryNetworkObject, localizedStrings, projectId],
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
      if (!projectId || !dictionaryNetworkObject) {
        const errMissingParam = localizedStrings['%dictionary_error_missingParam%'];
        if (!projectId) logger.warn(`${errMissingParam}projectId`);
        if (!dictionaryNetworkObject) logger.warn(`${errMissingParam}dictionaryNetworkObject`);
        return;
      }

      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const addedEntry = await dictionaryNetworkObject.addEntry(projectId, entry);
      if (addedEntry) {
        onSearch(Object.values<string | undefined>(addedEntry.lexemeForm).pop() ?? '');
        await papi.commands.sendCommand('dictionary.displayEntry', projectId, addedEntry.id);
      } else {
        logger.error(`${localizedStrings['%dictionary_error_failedToAddEntry%']}`);
      }
    },
    [dictionaryNetworkObject, localizedStrings, onSearch, projectId],
  );

  return (
    <DictionaryListWrapper
      elementHeader={
        <div className="tw-flex tw-gap-2">
          <div className="tw-max-w-72">
            <SearchBar
              onSearch={onSearch}
              placeholder={localizedStrings['%dictionary_findWord_textField%']}
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
          <DictionaryList
            analysisLanguage={analysisLanguage ?? ''}
            dictionaryData={matchingEntries}
            vernacularLanguage={vernacularLanguage ?? ''}
          />
        ) : undefined
      }
      isLoading={isFetching}
      hasItems={!!matchingEntries?.length}
    />
  );
};
