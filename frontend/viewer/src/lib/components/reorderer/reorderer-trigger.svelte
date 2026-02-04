<script lang="ts" module>
  import type {Snippet} from 'svelte';
  import type {HTMLAttributes} from 'svelte/elements';

  type TriggerHTMLAttributes = HTMLAttributes<HTMLElement> & {
    id?: string;
  };

  export type ReordererTriggerProps = TriggerHTMLAttributes & {
    child?: Snippet<[{arrowIcon: IconClass; props: TriggerHTMLAttributes}]>;
  };
</script>

<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {buttonVariants} from '../ui/button';
  import type {IconClass} from '$lib/icon-class';
  import {useReordererTrigger} from './reorderer.svelte';

  let {child, ...props}: ReordererTriggerProps = $props();

  function pickIcon(direction: 'horizontal' | 'vertical', first = false, last = false): IconClass {
    if (direction === 'horizontal') {
      return first ? 'i-mdi-arrow-right-bold' : last ? 'i-mdi-arrow-left-bold' : 'i-mdi-arrow-left-right-bold';
    } else {
      return first ? 'i-mdi-arrow-down-bold' : last ? 'i-mdi-arrow-up-bold' : 'i-mdi-arrow-up-down-bold';
    }
  }

  const itemListState = useReordererTrigger();
  const {first, last, direction} = $derived(itemListState);
  const arrowIcon = $derived(pickIcon(direction, first, last));
</script>

{#if child}
  {@render child({arrowIcon, props})}
{:else}
  <DropdownMenu.Trigger class={buttonVariants({variant: 'secondary', size: 'icon'})} {...props}>
    <Icon icon={arrowIcon} />
  </DropdownMenu.Trigger>
{/if}
