<script lang="ts" module>
  import {type Snippet} from 'svelte';

  export type ReordererSwapperProps<T> = Omit<HTMLAttributes<HTMLElement>, 'onchange'> & {
    first: boolean;
    items: T[];
    onchange?: (value: T[], fromIndex: number, toIndex: number) => void;
    child?: Snippet<[{ first: boolean, arrow: Snippet, props: HTMLAttributes<HTMLElement> & { id?: string } }]>;
    id?: string;
    direction?: 'horizontal' | 'vertical';
  };
</script>

<script lang="ts" generics="T">
  import {Icon} from '$lib/components/ui/icon';
  import {mergeProps} from 'bits-ui';
  import {Button} from '../ui/button';
  import type {HTMLAttributes} from 'svelte/elements';
  import {pickIcon} from './icon-util';

  let {
    first,
    items = $bindable(),
    onchange,
    child,
    direction = 'horizontal',
    ...rest
  } : ReordererSwapperProps<T> = $props();

  function swap(): void {
    if (items.length > 2) throw new Error(`Swap does not support more than 2 items (${items.length})`)
    if (items.length < 2) return;
    items = [items[1], items[0]];
    if (first) onchange?.(items, 0, 1);
    else onchange?.(items, 1, 0);
  }

  const mergedProps = $derived(mergeProps({
    onclick: () => swap(),
  }, rest));
</script>

{#snippet arrow()}
  <Icon icon={pickIcon(direction, first, !first)} />
{/snippet}

{#if child}
  {@render child({ first, arrow, props: mergedProps })}
{:else}
  <Button variant="secondary" size="icon" {...mergedProps}>
    {@render arrow()}
  </Button>
{/if}
