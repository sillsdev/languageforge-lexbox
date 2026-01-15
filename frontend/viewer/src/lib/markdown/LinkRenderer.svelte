<script lang="ts">
  import Anchor from '$lib/components/ui/anchor/anchor.svelte';

  export let href: string;
  export let title: string | undefined = undefined;
</script>

<Anchor {href} {title} variant="text">
  <slot />
</Anchor>
