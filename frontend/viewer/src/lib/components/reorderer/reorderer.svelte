<script lang="ts" module>
  import {type Snippet} from 'svelte';

  export type ReordererProps<T> = {
    item: T;
    items: T[];
    direction?: 'horizontal' | 'vertical';
    getDisplayName: (item: T) => string | undefined;
    onchange?: (value: T[], fromIndex: number, toIndex: number) => void;
    swapper?: ReordererSwapperProps<T>['child'];
    children?: Snippet<[{first: boolean, last: boolean}]>;
  };
</script>

<script lang="ts" generics="T">
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import ReordererSwapper, {type ReordererSwapperProps} from './reorderer-swapper.svelte';
  import ReordererTrigger from './reorderer-trigger.svelte';
  import ReordererItemList from './reorderer-item-list.svelte';

  let {
    item,
    items = $bindable(),
    getDisplayName,
    onchange,
    swapper,
    children,
    direction = 'horizontal',
  } : ReordererProps<T> = $props();

  const index = $derived(items.indexOf(item));
  const count = $derived(items.length);
  const first = $derived(index === 0);
  const last = $derived(index === count - 1);
</script>

{#if count > 1}
  {#if count === 2}
    <ReordererSwapper child={swapper} {first} {items} {onchange} {direction} />
  {:else if children}
    {@render children({first, last})}
  {:else}
  <DropdownMenu.Root>
    <ReordererTrigger {first} {last} {direction} />
    <DropdownMenu.Content>
      <ReordererItemList {items} {item} {getDisplayName} {onchange} />
    </DropdownMenu.Content>
  </DropdownMenu.Root>
  {/if}
{/if}
