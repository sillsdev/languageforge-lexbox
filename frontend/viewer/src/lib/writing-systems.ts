import type {WritingSystems} from './mini-lcm';
import {derived, type Readable} from 'svelte/store';
import {getContext, setContext} from 'svelte';

const writingSystemContextName = 'writingSystems';
export function initWritingSystems(ws: Readable<WritingSystems | null>): Readable<WritingSystems | null> {
  setContext(writingSystemContextName, ws);
  return ws;
}

export function useWritingSystems(): Readable<WritingSystems> {
  return derived(getContext<Readable<WritingSystems | null>>(writingSystemContextName), (ws, set) => {
    if (ws) set(ws);
  });
}
