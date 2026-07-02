<script lang="ts">
  import {useHistoryService} from '$lib/services/history-service';
  import {t} from 'svelte-i18n-lingui';
  import {resource} from 'runed';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import {ActivitySort} from '$lib/dotnet-types';
  import * as Select from '$lib/components/ui/select';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import {badgeVariants} from '$lib/components/ui/badge';
  import {cn} from '$lib/utils';
  import {Icon} from '$lib/components/ui/icon';
  import type {IconClass} from '$lib/icon-class';
  import {
    ALL_AUTHORS,
    ALL_CHANGE_TYPES,
    applyMultiSelectValue,
    authorFilterKey,
    compareActivityAuthors,
    createDefaultActivityFilters,
    isAllFilterSelection,
    resolveFilterKeys,
    wellKnownAuthorKeyToLabel,
    type ActivityFilters,
    type MultiFilterSelection,
  } from './utils';
  import AuthorLabel from './AuthorLabel.svelte';
  import type {Snippet} from 'svelte';

  type Props = {
    filters?: ActivityFilters;
    /** Optional right-aligned content rendered on the same row as the sort menu (e.g. the list-mode icon). */
    trailing?: Snippet;
  };

  let {filters = $bindable(createDefaultActivityFilters()), trailing}: Props = $props();

  const historyService = useHistoryService();

  const authors = resource(
    () => historyService.loaded,
    async (loaded) => {
      if (!loaded) return [];
      const data = await historyService.listActivityAuthors();
      if (Array.isArray(data)) {
        return data.sort(compareActivityAuthors);
      }
      return [];
    },
    {initialValue: []},
  );

  const changeTypes = resource(
    () => historyService.loaded,
    async (loaded) => {
      if (!loaded) return [];
      const data = await historyService.listActivityChangeTypes();
      return Array.isArray(data) ? data : [];
    },
    {initialValue: []},
  );

  const authorKeys = $derived(authors.current.map(authorFilterKey));
  const changeTypeKeys = $derived(changeTypes.current.map(ct => ct.key));

  const authorSelectValue = $derived(resolveFilterKeys(filters.authorFilterKeys, authorKeys));
  const changeTypeSelectValue = $derived(resolveFilterKeys(filters.changeTypeFilterKeys, changeTypeKeys));

  const sortLabels = $derived<Record<ActivitySort, string>>({
    [ActivitySort.NewestFirst]: $t`Newest first`,
    [ActivitySort.OldestFirst]: $t`Oldest first`,
    [ActivitySort.SyncedNewestFirst]: $t`Synced newest`,
    [ActivitySort.SyncedOldestFirst]: $t`Synced oldest`,
  });

  const sortIcons: Record<ActivitySort, IconClass> = {
    [ActivitySort.NewestFirst]: 'i-mdi-sort-clock-descending',
    [ActivitySort.OldestFirst]: 'i-mdi-sort-clock-ascending',
    [ActivitySort.SyncedNewestFirst]: 'i-mdi-cloud-arrow-down',
    [ActivitySort.SyncedOldestFirst]: 'i-mdi-cloud-arrow-up',
  };

  function authorKeyToLabel(key: string): string {
    const wellKnownLabel = wellKnownAuthorKeyToLabel(key);
    if (wellKnownLabel) return wellKnownLabel;
    const author = authors.current.find(a => authorFilterKey(a) === key);
    return author?.authorName ?? key;
  }

  function allSelectionIcon(selected: MultiFilterSelection, allKeys: string[]): IconClass | undefined {
    if (selected === 'all' || isAllFilterSelection(selected, allKeys)) return 'i-mdi-check';
    if (selected.length === 0) return undefined;
    return 'i-mdi-minus';
  }

  function onAuthorValueChange(value: string[]) {
    filters.authorFilterKeys = applyMultiSelectValue(value, authorKeys, ALL_AUTHORS, filters.authorFilterKeys);
  }

  function onChangeTypeValueChange(value: string[]) {
    filters.changeTypeFilterKeys = applyMultiSelectValue(value, changeTypeKeys, ALL_CHANGE_TYPES, filters.changeTypeFilterKeys);
  }
</script>

<div class="flex flex-col gap-2 mb-1">
  <div class="flex flex-wrap gap-2">
    <SidebarTrigger icon="i-mdi-menu" class="aspect-square p-0 shrink-0" />

    <Select.Root type="multiple" value={authorSelectValue} onValueChange={onAuthorValueChange}>
      <Select.Trigger class="w-32 max-w-full grow">
        {#if isAllFilterSelection(filters.authorFilterKeys, authorKeys)}
          {$t`All authors`}
        {:else if filters.authorFilterKeys.length === 0}
          {$t`No authors`}
        {:else if filters.authorFilterKeys.length === 1}
          {@const selectedAuthor = authors.current.find(a => authorFilterKey(a) === filters.authorFilterKeys[0])}
          <AuthorLabel authorId={selectedAuthor?.authorId} authorName={selectedAuthor?.authorName} />
        {:else}
          {$t`${filters.authorFilterKeys.length} authors`}
        {/if}
      </Select.Trigger>
      <Select.Content>
        <Select.Item value={ALL_AUTHORS} label={$t`All authors`}>
          {#snippet selectedIndicator()}
            {@const icon = allSelectionIcon(filters.authorFilterKeys, authorKeys)}
            {#if icon}
              <Icon {icon} class="size-4" />
            {/if}
          {/snippet}
          <span class="font-bold">{$t`All authors`}</span>
        </Select.Item>
        {#each authors.current as author (authorFilterKey(author))}
          {@const key = authorFilterKey(author)}
          <Select.Item value={key} label={authorKeyToLabel(key)}>
            <AuthorLabel authorId={author.authorId} authorName={author.authorName} />
            <span class="text-muted-foreground ml-1">({author.commitCount})</span>
          </Select.Item>
        {/each}
      </Select.Content>
    </Select.Root>

    <Select.Root type="multiple" value={changeTypeSelectValue} onValueChange={onChangeTypeValueChange}>
      <Select.Trigger class="w-44 max-w-full grow">
        {#if isAllFilterSelection(filters.changeTypeFilterKeys, changeTypeKeys)}
          {$t`All activity types`}
        {:else if filters.changeTypeFilterKeys.length === 0}
          {$t`No activity types`}
        {:else if filters.changeTypeFilterKeys.length === 1}
          {changeTypes.current.find(ct => ct.key === filters.changeTypeFilterKeys[0])?.label ?? filters.changeTypeFilterKeys[0]}
        {:else}
          {$t`${filters.changeTypeFilterKeys.length} activity types`}
        {/if}
      </Select.Trigger>
      <Select.Content>
        <Select.Item value={ALL_CHANGE_TYPES} label={$t`All activity types`}>
          {#snippet selectedIndicator()}
            {@const icon = allSelectionIcon(filters.changeTypeFilterKeys, changeTypeKeys)}
            {#if icon}
              <Icon {icon} class="size-4" />
            {/if}
          {/snippet}
          <span class="font-bold">{$t`All activity types`}</span>
        </Select.Item>
        {#each changeTypes.current as changeType (changeType.key)}
          <Select.Item value={changeType.key} label={changeType.label}>
            {changeType.label}
            <span class="text-muted-foreground ml-1">({changeType.commitCount})</span>
          </Select.Item>
        {/each}
      </Select.Content>
    </Select.Root>
  </div>

  <!-- Sort menu on the left, optional trailing element (list-mode icon) on the right — mirrors the entry
       browser layout (SortMenu ↔ EntryListViewOptions) so the two views feel consistent. -->
  <div class="flex items-center justify-between gap-2">
    <ResponsiveMenu.Root>
      <ResponsiveMenu.Trigger class={cn(buttonVariants({variant: 'secondary', size: 'xs'}), badgeVariants({variant: 'secondary'}), 'border-none h-7')}>
        {#snippet child({props})}
          <Button {...props} icon={sortIcons[filters.sort]} iconProps={{class: 'size-4'}}>
            {sortLabels[filters.sort]}
          </Button>
        {/snippet}
      </ResponsiveMenu.Trigger>
      <ResponsiveMenu.Content align="start">
        {#each Object.values(ActivitySort) as sortOption (sortOption)}
          <ResponsiveMenu.Item
            onSelect={() => filters.sort = sortOption}
            class={cn(filters.sort === sortOption && 'bg-muted')}>
            <Icon icon={sortIcons[sortOption]} />
            {sortLabels[sortOption]}
          </ResponsiveMenu.Item>
        {/each}
      </ResponsiveMenu.Content>
    </ResponsiveMenu.Root>
    {#if trailing}{@render trailing()}{/if}
  </div>
</div>
