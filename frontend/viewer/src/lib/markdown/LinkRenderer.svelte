<script lang="ts">
  import Anchor from '$lib/components/ui/anchor/anchor.svelte';
  import type { Snippet } from 'svelte';

  interface Props {
    href: string;
    title?: string | undefined;
    children?: Snippet;
  }

  const { href, title = undefined, children }: Props = $props();
</script>

<Anchor {href} {title} variant="text">
  {@render children?.()}
</Anchor>
