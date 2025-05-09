<script lang="ts" module>
  import type {Snippet} from 'svelte';
  import type {HTMLAttributes} from 'svelte/elements';

  export type ReordererTriggerProps = HTMLAttributes<HTMLElement> & {
    first: boolean;
    last: boolean;
    child?: Snippet<[{ content: Snippet, props: HTMLAttributes<HTMLElement> }]>;
    id?: string;
    direction?: 'horizontal' | 'vertical';
  };
</script>

<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {pickIcon} from './icon-util';
  import {buttonVariants} from '../ui/button';

  let {
    first,
    last,
    child,
    direction = 'horizontal',
    ...props
  } : ReordererTriggerProps = $props();
</script>

{#snippet content()}
  <Icon icon={pickIcon(direction, first, last)} />
{/snippet}

{#if child}
  {@render child({ content, props })}
{:else}
  <DropdownMenu.Trigger class={buttonVariants({ variant: 'secondary', size: 'icon' })} {...props}>
    {@render content()}
  </DropdownMenu.Trigger>
{/if}
