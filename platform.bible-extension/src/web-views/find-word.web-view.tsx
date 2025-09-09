import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { IEntry, IEntryService, PartialEntry, WordWebViewOptions } from 'fw-lite-extension';
import { Label, SearchBar } from 'platform-bible-react';
import { debounce } from 'platform-bible-utils';
import { useCallback, useEffect, useMemo, useState } from 'react';
import AddNewEntry from '../components/add-new-entry';
import DictionaryList from '../components/dictionary-list';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';

/* eslint-disable react-hooks/rules-of-hooks */

globalThis.webViewComponent = function fwLiteFindWord({
  analysisLanguage,
  projectId,
  vernacularLanguage,
  word,
}: WordWebViewOptions) {
  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [fwLiteNetworkObject, setFwLiteNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isFetching, setIsFetching] = useState(false);
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);
  const [searchTerm, setSearchTerm] = useState(word ?? '');

  useEffect(() => {
    papi.networkObjects
      .get<IEntryService>('fwliteextension.entryService')
      // eslint-disable-next-line promise/always-return
      .then((networkObject) => {
        logger.info('Got network object:', networkObject);
        setFwLiteNetworkObject(networkObject);
      })
      .catch((e) =>
        logger.error(
          `${localizedStrings['%fwLiteExtension_error_gettingNetworkObject%']}:`,
          JSON.stringify(e),
        ),
      );
  }, [localizedStrings]);

  const fetchEntries = useCallback(
    async (untrimmedSurfaceForm: string) => {
      if (!projectId || !fwLiteNetworkObject) {
        if (!projectId)
          logger.warn(`${localizedStrings['%fwLiteExtension_error_missingParam%']}projectId`);
        if (!fwLiteNetworkObject)
          logger.warn(
            `${localizedStrings['%fwLiteExtension_error_missingParam%']}fwLiteNetworkObject`,
          );
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
    [fwLiteNetworkObject, localizedStrings, projectId],
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
      if (!projectId || !fwLiteNetworkObject) {
        if (!projectId)
          logger.warn(`${localizedStrings['%fwLiteExtension_error_missingParam%']}projectId`);
        if (!fwLiteNetworkObject)
          logger.warn(
            `${localizedStrings['%fwLiteExtension_error_missingParam%']}fwLiteNetworkObject`,
          );
        return;
      }

      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const addedEntry = await fwLiteNetworkObject.addEntry(projectId, entry);
      if (addedEntry) {
        onSearch(Object.values<string | undefined>(addedEntry.lexemeForm).pop() ?? '');
        await papi.commands.sendCommand('fwLiteExtension.displayEntry', projectId, addedEntry.id);
      } else {
        logger.error(`${localizedStrings['%fwLiteExtension_error_failedToAddEntry%']}`);
      }
    },
    [fwLiteNetworkObject, localizedStrings, onSearch, projectId],
  );

  // Match className from paranext-core/extensions/src/platform-lexical-tools/src/web-views/dictionary.web-view.tsx
  return (
    <div className="tw-flex tw-flex-col tw-h-[100dvh]">
      <div className="tw-sticky tw-bg-background tw-top-0 tw-z-10 tw-shrink-0 tw-p-2 tw-border-b tw-h-auto">
        <div className="tw-flex tw-gap-2">
          <div className="tw-max-w-72">
            <SearchBar
              onSearch={onSearch}
              placeholder={localizedStrings['%fwLiteExtension_findWord_textField%']}
              value={searchTerm}
            />
          </div>

          <div>
            <AddNewEntry
              addEntry={addEntry}
              analysisLang={analysisLanguage ?? ''}
              headword={searchTerm}
              vernacularLang={vernacularLanguage ?? ''}
            />
          </div>
        </div>
      </div>

      {isFetching && (
        <div className="tw-flex-1 tw-p-2 tw-space-y-4">
          <Label>{localizedStrings['%fwLiteExtension_findWord_loading%']}</Label>
        </div>
      )}
      {!matchingEntries?.length && !isFetching && (
        <div className="tw-m-4 tw-flex tw-justify-center">
          <Label>{localizedStrings['%fwLiteExtension_findWord_noMatchingEntries%']}</Label>
        </div>
      )}
      {matchingEntries && <DictionaryList dictionaryData={matchingEntries} />}
    </div>
  );
};
