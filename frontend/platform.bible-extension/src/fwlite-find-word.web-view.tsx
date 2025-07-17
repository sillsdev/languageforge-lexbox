import type { NetworkObject } from '@papi/core';
import papi, { logger } from '@papi/frontend';
import type { FindWebViewOptions, IEntry, IEntryService } from 'fw-lite-extension';
import { Card, CardContent, CardHeader, SearchBar } from 'platform-bible-react';
import { useCallback, useEffect, useState } from 'react';

globalThis.webViewComponent = function fwLiteProjectSelect({
  projectId,
  word,
}: FindWebViewOptions) {
  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [fwLiteNetworkObject, setFwLiteNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >();
  const [isFetching, setIsFetching] = useState(false);
  const [searchTerm, setSearchTerm] = useState(word ?? '');

  useEffect(() => {
    papi.networkObjects.get<IEntryService>('fwliteextension.entryService').then((networkObject) => {
      logger.info('Got network object:', networkObject);
      setFwLiteNetworkObject(networkObject);
    });
  }, []);

  const fetchEntries = useCallback(
    async (word: string) => {
      setSearchTerm(word);
      if (!projectId || !fwLiteNetworkObject) {
        logger.warn(
          `Missing required parameters: projectId=${projectId}, fwLiteNetworkObject=${fwLiteNetworkObject}`,
        );
        return;
      }
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

  return (
    <div>
      <SearchBar placeholder="Find in dictionary..." value={searchTerm} onSearch={fetchEntries} />
      {isFetching && <p>Loading...</p>}
      {!matchingEntries?.length && !isFetching && <p>No matching entries</p>}
      {matchingEntries?.map((entry) => {
        return (
          <Card>
            <CardHeader>
              {Object.keys(entry.citationForm).length
                ? JSON.stringify(entry.citationForm)
                : JSON.stringify(entry.lexemeForm)}
            </CardHeader>
            <CardContent>
              {entry.senses.map((sense) => (
                <div key={sense.id}>
                  <strong>{JSON.stringify(sense.gloss)}</strong>
                  <p>{JSON.stringify(sense.definition)}</p>
                </div>
              ))}
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
};
