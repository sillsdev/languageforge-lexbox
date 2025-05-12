<script lang="ts">
  import {cn} from '$lib/utils';
  import type {Snippet} from 'svelte';
  import type {HTMLAttributes} from 'svelte/elements';

  type Props = HTMLAttributes<HTMLElement> & {
    children?: Snippet;
  }

  const {
    children,
    class: className,
  }: Props = $props();
</script>

<div class={cn('absolute bottom-4 right-4 flex flex-col items-end z-10', className)}>
  {@render children?.()}
</div>
