import {untrack} from 'svelte';
import type {IWritingSystems} from './dotnet-types';
import {useMiniLcmApi} from './services/service-provider';
import {initWritingSystemService, WritingSystemService} from './writing-system-service';
import {writable} from 'svelte/store';

//todo this won't work when projects change, the WSS should depend on miniLcmApi which should also be reactive
let writingSystems = $state< IWritingSystems | null>(null);
let loading = $state(false);
export function useWritingSystemRunes(): WritingSystemService {
  if (!writingSystems){
    untrack(load);
  }

  return new WritingSystemService(writingSystems ?? {analysis: [], vernacular: []});
}

function load() {
  if (loading) return;
  const wsStore = writable<IWritingSystems | null>(null);
  initWritingSystemService(wsStore);
  loading = true;
  void useMiniLcmApi().getWritingSystems().then(ws => {
    console.log('Writing systems loaded:', ws);
    writingSystems = ws;
    wsStore.set(ws);
    loading = false;
  });
}