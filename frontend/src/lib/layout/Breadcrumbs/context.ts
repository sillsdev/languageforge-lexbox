import {createContext} from 'svelte';
import type {Writable} from 'svelte/store';

export const [useBreadcrumbStore, setBreadcrumbStore] = createContext<Writable<Element[]>>();
