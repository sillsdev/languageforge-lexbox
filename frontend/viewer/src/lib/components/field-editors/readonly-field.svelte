<script lang="ts">
  import type {ComponentProps, Snippet} from 'svelte';
  import {inputVariants} from '../ui/input/input.svelte';
  import {tryUseFieldBody} from '../editor/field/field-root.svelte';
  import BadgeList from './badge-list.svelte';

  const labelledBy = tryUseFieldBody()?.labelId;

  const {children, ...badgeProps}: ComponentProps<typeof BadgeList> & {children?: Snippet} = $props();

  // Not a real input, so Ctrl+A would select the whole page.
  function selectAllWithin(event: KeyboardEvent & {currentTarget: HTMLElement}) {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'a') {
      event.preventDefault();
      getSelection()?.selectAllChildren(event.currentTarget);
    }
  }
</script>

<div
  role="textbox"
  aria-readonly="true"
  aria-labelledby={labelledBy}
  tabindex="0"
  onkeydown={selectAllWithin}
  class={[inputVariants(), 'h-auto min-h-10 items-center']}
>
  {#if children}
    {@render children()}
  {:else}
    <BadgeList {...badgeProps} />
  {/if}
</div>
