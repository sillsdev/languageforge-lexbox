<script lang="ts" module>
  import {Context} from 'runed';

  type ReordererRootStateProps<T = unknown> = {
    readonly item: T;
    items: T[];
    readonly first: boolean;
    readonly last: boolean;
    readonly direction: 'horizontal' | 'vertical';
    readonly getDisplayName: (item: T) => string | undefined;
    readonly onchange?: (value: T[], fromIndex: number, toIndex: number) => void;
  };

  const reordererRootContext = new Context<ReordererRootStateProps>('Reorderer.Root');

  export function useReordererRoot<T>(props: ReordererRootStateProps<T>): ReordererRootStateProps<T> {
    reordererRootContext.set(props as ReordererRootStateProps);
    return props;
  }

  type ReordererItemListStateProps<T> = ReordererRootStateProps<T>;

  export function useReordererItemList<T>(): ReordererItemListStateProps<T> {
    return reordererRootContext.get() as ReordererItemListStateProps<T>;
  }

  type ReordererTriggerStateProps<T> = ReordererRootStateProps<T>;

  export function useReordererTrigger<T = unknown>(): ReordererTriggerStateProps<T> {
    return reordererRootContext.get() as ReordererTriggerStateProps<T>;
  }
</script>

<script lang="ts" generics="T">
  import type {Snippet} from 'svelte';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import ReordererTrigger from './reorderer-trigger.svelte';
  import ReordererItemList from './reorderer-item-list.svelte';

  type ReordererProps<T> = {
    item: T;
    items: T[];
    direction?: 'horizontal' | 'vertical';
    getDisplayName: (item: T) => string | undefined;
    onchange?: (value: T[], fromIndex: number, toIndex: number) => void;
    children?: Snippet<[{first: boolean; last: boolean}]>;
  };

  let {
    item,
    items = $bindable(),
    getDisplayName,
    onchange,
    children,
    direction = 'horizontal',
  }: ReordererProps<T> = $props();

  const index = $derived(items.indexOf(item));
  const count = $derived(items.length);
  const first = $derived(index === 0);
  const last = $derived(index === count - 1);

  const rootStateProps = {
    get item() {
      return item;
    },
    get items() {
      return items;
    },
    set items(value) {
      items = value;
    },
    get first() {
      return first;
    },
    get last() {
      return last;
    },
    get direction() {
      return direction;
    },
    get getDisplayName() {
      return getDisplayName;
    },
    get onchange() {
      return onchange;
    },
  };

  useReordererRoot(rootStateProps);
</script>

{#if count > 1}
  {#if children}
    {@render children({first, last})}
  {:else}
    <DropdownMenu.Root>
      <ReordererTrigger />
      <DropdownMenu.Content>
        <ReordererItemList />
      </DropdownMenu.Content>
    </DropdownMenu.Root>
  {/if}
{/if}
