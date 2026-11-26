<script lang="ts">
  import {Select as SelectPrimitive} from 'bits-ui';
  import {cn, type WithoutChild} from '$lib/utils.js';
  import OptionIndicator from './option-indicator.svelte';
  import {OPTION_ROW} from './select-list';

  type Props = WithoutChild<SelectPrimitive.ItemProps> & {
    /** Overrides the selected state bits-ui reports, e.g. for tri-state group rows */
    checkState?: boolean | 'some';
  };

  let {
    ref = $bindable(null),
    class: className,
    value,
    label,
    children: childrenProp,
    checkState,
    ...restProps
  }: Props = $props();
</script>

<SelectPrimitive.Item
  bind:ref
  {value}
  data-slot="select-item"
  class={cn(
    OPTION_ROW,
    "data-highlighted:bg-accent data-highlighted:text-accent-foreground [&_[class*='i-mdi-']:not([class*='text-'])]:text-muted-foreground relative flex w-full cursor-default items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-hidden select-none data-disabled:pointer-events-none data-disabled:opacity-50 **:[[class*='i-mdi-']]:pointer-events-none [&_[class*='i-mdi-']]:shrink-0 [&_[class*='i-mdi-']:not([class*='size-'])]:size-4",
    className,
  )}
  {...restProps}
>
  {#snippet children({selected, highlighted})}
    <span class="flex min-w-0 flex-1 items-center gap-2 break-words">
      {#if childrenProp}
        {@render childrenProp({selected, highlighted})}
      {:else}
        {label || value}
      {/if}
    </span>
    <OptionIndicator state={checkState ?? selected} />
  {/snippet}
</SelectPrimitive.Item>
