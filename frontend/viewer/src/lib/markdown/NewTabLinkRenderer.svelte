<script lang="ts">
  import Anchor from "$lib/components/ui/anchor/anchor.svelte";

  export let href: string;
  export let title: string | undefined = undefined;
</script>

<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
<Anchor {href} {title} target="_blank" variant="text">
  <!-- &nbsp; prevents the link from ever being at the very beginning of a new line -->
  <slot />&nbsp;<span class="i-mdi-open-in-new external-link-icon"></span>
</Anchor>

<style>
  .external-link-icon {
    margin-bottom: -0.7%;
    font-size: 0.9em;
  }
</style>
