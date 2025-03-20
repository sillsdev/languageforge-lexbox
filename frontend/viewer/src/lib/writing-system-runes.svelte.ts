import {untrack} from 'svelte';
import type {IWritingSystems} from './dotnet-types';
import {useMiniLcmApi} from './services/service-provider';
import {WritingSystemService} from './writing-system-service';

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
  loading = true;
  void useMiniLcmApi().getWritingSystems().then(ws => {
    console.log('Writing systems loaded:', ws);
    writingSystems = ws;
    loading = false;
  });
}