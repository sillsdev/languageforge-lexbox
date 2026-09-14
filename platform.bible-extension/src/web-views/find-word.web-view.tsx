import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { IEntry, IEntryService, LexiconWebViewProps, PartialEntry } from 'lexicon';
import { SearchBar } from 'platform-bible-react';
import { useCallback, useEffect, useState } from 'react';
import AddNewEntryButton from '../components/add-new-entry-button';
import EntryList from '../components/entry-list';
import EntryListWrapper from '../components/entry-list-wrapper';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';
import type { EntryLookupRequest } from '../utils/use-entry-lookup';
import useEntryLookup from '../utils/use-entry-lookup';

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
  const [searchTerm, setSearchTerm] = useState(word ?? '');
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
        // Drop the last query's entries: kept, they would sit under the new search term as though
        // they answered it.
        onFailure: () => setMatchingEntries(undefined),
        onResult: (entries) => setMatchingEntries(entries ?? []),
        request: () => lexiconNetworkObject.getEntries(lexiconCode, { surfaceForm }),
      };
    },
    [lexiconCode, lexiconNetworkObject, localizedStrings],
  );

  const onSearch = useCallback(
    (searchQuery: string) => {
      setSearchTerm(searchQuery);
      if (!searchQuery.trim()) {
        // The query is withdrawn, so nothing should answer it and what is on screen answers a
        // query that is gone.
        lookup.reset();
        setMatchingEntries(undefined);
        return;
      }
      lookup.schedule(() => entriesLookup(searchQuery));
    },
    [entriesLookup, lookup],
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
      hasError={didFail}
      isLoading={isPending}
      hasItems={!!matchingEntries?.length}
    />
  );
};
