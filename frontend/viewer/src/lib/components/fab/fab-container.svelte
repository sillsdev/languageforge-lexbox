<script lang="ts">
  import {cn} from '$lib/utils';
  import type {Snippet} from 'svelte';
  import type {HTMLAttributes} from 'svelte/elements';

  type Props = HTMLAttributes<HTMLElement> & {
    children?: Snippet;
  };

  const {children, class: className}: Props = $props();
</script>

<div class={cn('absolute bottom-4 right-4 md:bottom-6 md:right-6 flex flex-col items-end z-10 gap-4', className)}>
  {@render children?.()}
</div>
