<script lang="ts" module>
  import {SortField} from '$lib/dotnet-types';
  import {msg, t} from 'svelte-i18n-lingui';
  import type {IconClass} from '$lib/icon-class';

  export type SortDirection = 'asc' | 'desc';

  const sortLabels = {
    [SortField.SearchRelevance]: msg`Best match`,
    [SortField.Headword]: msg`Headword`
  } as const;

  const sortIcons: Partial<Record<SortField, Record<SortDirection, IconClass>>> = {
    [SortField.Headword]: {
      asc: 'i-mdi-sort-alphabetical-ascending',
      desc: 'i-mdi-sort-alphabetical-descending'
    }
  };

  export const sortOptions = [
    {field: SortField.SearchRelevance, dir: 'asc'},
    {field: SortField.Headword, dir: 'asc'},
    {field: SortField.Headword, dir: 'desc'}
  ] as const;

  export type SortConfig = typeof sortOptions[number];
</script>

<script lang="ts">
  import { badgeVariants } from '$lib/components/ui/badge';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {cn} from '$lib/utils';
  import {watch, type Getter} from 'runed';
  import {Icon} from '$lib/components/ui/icon';

  type Props = {
    value?: SortConfig;
    autoSelector: Getter<SortField>;
  };

  let {
    value = $bindable(),
    autoSelector,
  }: Props = $props();

  let selectedSortField = $state<SortField>();
  let direction = $state<SortDirection>('asc');
  const autoSort = $derived(autoSelector());
  const sortField = $derived(selectedSortField ?? autoSort);
  watch(() => ({ sortField, direction }), ({ sortField, direction }) => {
    value = { field: sortField, dir: direction } as SortConfig;
  });
</script>

<ResponsiveMenu.Root>
  <ResponsiveMenu.Trigger class={badgeVariants({ variant: 'secondary' })}>
    {#snippet child({props})}
      <button {...props}>
        <Icon icon={sortIcons[sortField]?.[direction] ?? 'i-mdi-arrow-down'} class="size-4 mr-1" />
        {$t(sortLabels[sortField])}
      </button>
    {/snippet}
  </ResponsiveMenu.Trigger>
  <ResponsiveMenu.Content align="start">
    <ResponsiveMenu.Item
        onSelect={() => {
          selectedSortField = undefined;
          direction = 'asc';
        }}
        class={cn(!selectedSortField && 'bg-muted')}
        >
        {$t`Auto`}
        <span class="text-muted-foreground ml-auto">
          ({$t(sortLabels[autoSort])})
        </span>
    </ResponsiveMenu.Item>
    {#each sortOptions as option (option)}
      {@const icon = sortIcons[option.field]?.[option.dir]}
      <ResponsiveMenu.Item
        onSelect={() => {
          selectedSortField = option.field;
          direction = option.dir;
        }}
        class={cn(selectedSortField === option.field && direction === option.dir && 'bg-muted')}
        >
        {#if icon}
          <Icon {icon} />
        {/if}
        {$t(sortLabels[option.field])}
      </ResponsiveMenu.Item>
    {/each}
  </ResponsiveMenu.Content>
</ResponsiveMenu.Root>
