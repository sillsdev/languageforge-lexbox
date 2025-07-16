import { NetworkObject } from '@papi/core';
import papi from '@papi/frontend';
import type { FindWebViewOptions, IEntry, IEntryService } from 'fw-lite-extension';
import { Card, CardHeader, SearchBar, CardContent } from 'platform-bible-react';
import { useState, useEffect } from 'react';

globalThis.webViewComponent = function fwLiteProjectSelect({
  projectId,
  word,
}: FindWebViewOptions) {
  const [matchingEntries, setMatchingEntries] = useState<IEntry[] | undefined>();
  const [fwLiteNetworkObject, setFwLiteNetworkObject] = useState<
    NetworkObject<IEntryService> | undefined
  >(undefined);
  const [isFetching, setIsFetching] = useState(false);
  const [searchTerm, setSearchTerm] = useState(word ?? '');

  useEffect(() => {
    papi.networkObjects.get<IEntryService>('fwliteextension.entryService').then((networkObject) => {
      console.log('Got network object:', networkObject);
      setFwLiteNetworkObject(networkObject);
    });
  }, []);

  async function fetchEntries(word: string) {
    setSearchTerm(word);
    if (!projectId || !fwLiteNetworkObject) {
      console.warn(
        `Missing required parameters: projectId=${projectId}, fwLiteNetworkObject=${fwLiteNetworkObject}`,
      );
      return;
    }
    if (!word) {
      console.warn('No word provided for search');
      return;
    }
    console.log(`Fetching entries for ${word}`);
    setIsFetching(true);
    const entries = await fwLiteNetworkObject?.getEntries(projectId, {
      surfaceForm: word,
    });
    setIsFetching(false);
    setMatchingEntries(entries ?? []);
  }

  return (
    <div>
      <SearchBar placeholder="Find in dictionary..." value={searchTerm} onSearch={fetchEntries} />
      {isFetching && <p>Loading...</p>}
      {!matchingEntries?.length && !isFetching && <p>No matching entries</p>}
      {matchingEntries?.map((entry) => {
        return (
          <Card>
            <CardHeader>
              {JSON.stringify(entry.citationForm) ?? JSON.stringify(entry.lexemeForm)}
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
