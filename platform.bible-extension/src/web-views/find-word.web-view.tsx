import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { IEntry, IEntryService, LexiconWebViewProps, PartialEntry } from 'lexicon';
import { SearchBar } from 'platform-bible-react';
import { useCallback, useEffect, useRef, useState } from 'react';
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
      .catch((e) => logger.error('Error getting network object:', e));
  }, []);

  const entriesLookup = useCallback(
    (untrimmedSurfaceForm: string): EntryLookupRequest<IEntry[] | undefined> | undefined => {
      if (!lexiconCode || !lexiconNetworkObject) {
        if (!lexiconCode) logger.warn('Missing required parameter: lexiconCode');
        if (!lexiconNetworkObject) logger.warn('Missing required parameter: lexiconNetworkObject');
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
        onResult: (entries) => setMatchingEntries(entries ?? []),
        request: () => lexiconNetworkObject.getEntries(lexiconCode, { surfaceForm }),
      };
    },
    [lexiconCode, lexiconNetworkObject],
  );

  const entriesLookupRef = useRef(entriesLookup);

  useEffect(() => {
    entriesLookupRef.current = entriesLookup;
  }, [entriesLookup]);

  const onSearch = useCallback(
    (searchQuery: string) => {
      setSearchTerm(searchQuery);
      setMatchingEntries(undefined);
      if (!searchQuery.trim()) {
        lookup.reset();
        return;
      }
      lookup.schedule(() => entriesLookupRef.current(searchQuery));
    },
    [lookup],
  );

  const addEntry = useCallback(
    async (entry: PartialEntry): Promise<boolean> => {
      if (!lexiconCode || !projectId || !lexiconNetworkObject) {
        if (!lexiconCode) logger.warn('Missing required parameter: lexiconCode');
        if (!projectId) logger.warn('Missing required parameter: projectId');
        if (!lexiconNetworkObject) logger.warn('Missing required parameter: lexiconNetworkObject');
        return false;
      }

      logger.info(`Adding entry: ${JSON.stringify(entry)}`);
      const addedEntry = await lexiconNetworkObject.addEntry(lexiconCode, entry);
      if (!addedEntry) {
        logger.error('Failed to add entry!');
        return false;
      }

      onSearch(Object.values<string | undefined>(addedEntry.lexemeForm).pop() ?? '');
      // The entry is written, so failing to show it must not read as a failed add.
      try {
        await papi.commands.sendCommand(
          'lexicon.displayEntry',
          projectId,
          lexiconCode,
          addedEntry.id,
        );
      } catch (e) {
        logger.error('Error displaying the new entry:', e);
      }
      return true;
    },
    [lexiconCode, lexiconNetworkObject, onSearch, projectId],
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
