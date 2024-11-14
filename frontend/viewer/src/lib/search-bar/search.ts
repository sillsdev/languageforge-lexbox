import {writable} from 'svelte/store';

const showSearchDialog = writable(false);
const search = writable('');
export function useSearch() {
  return {
    showSearchDialog,
    search,
  };
}

export function openSearch(searchText: string) {
  search.set(searchText);
  showSearchDialog.set(true);
}
