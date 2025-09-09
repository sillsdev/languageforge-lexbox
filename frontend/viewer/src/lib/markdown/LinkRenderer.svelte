<script lang="ts">
  import Anchor from '$lib/components/ui/anchor/anchor.svelte';

  export let href: string;
  export let title: string | undefined = undefined;
</script>

<Anchor {href} {title} class="font-medium underline underline-offset-4 hover:text-primary">
  <slot />
</Anchor>
