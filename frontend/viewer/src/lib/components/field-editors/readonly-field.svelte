<script lang="ts">
  import type {ComponentProps, Snippet} from 'svelte';
  import {inputVariants} from '../ui/input/input.svelte';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';
  import BadgeList from './badge-list.svelte';

  const labelledBy = tryUseFieldBody()?.labelId;

  const {children, ...badgeProps}: ComponentProps<typeof BadgeList> & {children?: Snippet} = $props();
</script>

<div
  role="textbox"
  aria-readonly="true"
  aria-labelledby={labelledBy}
  tabindex="0"
  class={[inputVariants(), 'h-auto min-h-10 items-center']}
>
  {#if children}
    {@render children()}
  {:else}
    <BadgeList {...badgeProps} />
  {/if}
</div>
