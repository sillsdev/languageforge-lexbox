<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import type {IActivityAuthor} from '$lib/dotnet-types';
  import {authorFilterKey, wellKnownAuthorKeyToLabel} from './utils';
  import AuthorLabel from './AuthorLabel.svelte';
  import FacetFilter from './FacetFilter.svelte';
  import FacetFilterRow from './FacetFilterRow.svelte';

  // The author facet: a flat checkbox list with the same shell and semantics as the activity-type filter
  // (empty selection = no filter). Search only appears once the author list is long enough to need it.
  let {authors, selected, onSelectionChange}: {
    authors: readonly IActivityAuthor[];
    selected: readonly string[];
    onSelectionChange: (keys: string[]) => void;
  } = $props();

  let search = $state('');
  const selectedSet = $derived(new Set(selected));
  const query = $derived(search.trim().toLowerCase());

  function authorLabel(author: IActivityAuthor): string {
    const key = authorFilterKey(author);
    return wellKnownAuthorKeyToLabel(key) ?? author.authorName ?? key;
  }

  const showSearch = $derived(authors.length > 8);
  const visibleAuthors = $derived(
    query ? authors.filter((a) => authorLabel(a).toLowerCase().includes(query)) : [...authors]);

  function apply(keys: Set<string>) {
    onSelectionChange([...keys]);
  }

  function toggle(key: string) {
    // eslint-disable-next-line svelte/prefer-svelte-reactivity -- transient copy, applied and discarded
    const next = new Set(selectedSet);
    if (next.has(key)) next.delete(key);
    else next.add(key);
    apply(next);
  }

  const triggerLabel = $derived.by(() => {
    if (selected.length === 0) return $t`Author`;
    if (selected.length === 1) {
      const author = authors.find((a) => authorFilterKey(a) === selected[0]);
      return author ? authorLabel(author) : selected[0];
    }
    return $t`${selected.length} authors`;
  });
</script>

<FacetFilter
  title={$t`Author`}
  {triggerLabel}
  active={selected.length > 0}
  onReset={() => apply(new Set())}
  clearLabel={$t`Clear author filter`}
  {showSearch}
  searchPlaceholder={$t`Search authors`}
  bind:search
  isEmpty={!!query && visibleAuthors.length === 0}
  emptyText={$t`No matching authors`}>
  {#each visibleAuthors as author (authorFilterKey(author))}
    {@const key = authorFilterKey(author)}
    <FacetFilterRow
      checked={selectedSet.has(key)}
      onToggle={() => toggle(key)}
      onOnly={() => apply(new Set([key]))}
      label={authorLabel(author)}>
      <AuthorLabel authorId={author.authorId} authorName={author.authorName} />
    </FacetFilterRow>
  {/each}
</FacetFilter>
