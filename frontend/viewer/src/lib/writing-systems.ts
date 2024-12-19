import type {IWritingSystems} from '$lib/dotnet-types';
import {derived, type Readable} from 'svelte/store';
import {getContext, setContext} from 'svelte';

const writingSystemContextName = 'writingSystems';
export function initWritingSystems(ws: Readable<IWritingSystems | null>): Readable<IWritingSystems | null> {
  setContext(writingSystemContextName, ws);
  return ws;
}

export function useWritingSystems(): Readable<IWritingSystems> {
  return derived(getContext<Readable<IWritingSystems | null>>(writingSystemContextName), (ws, set) => {
    if (ws) set(ws);
  });
}
