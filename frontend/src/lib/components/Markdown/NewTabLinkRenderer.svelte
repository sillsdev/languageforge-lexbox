<script lang="ts">
  import type {Snippet} from 'svelte';
  interface Props {
    href: string;
    title?: string;
    children?: Snippet;
  }

  const { href, title, children }: Props = $props();
</script>

<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
<a {href} {title} target="_blank" class="external-link link link-hover">
  <!-- &nbsp; prevents the link from ever being at the very beginning of a new line -->
  {@render children?.()}&nbsp;<span class="i-mdi-open-in-new external-link-icon"></span>
</a>

<style>
  .external-link-icon {
    margin-bottom: -0.4%;
    font-size: 0.9em;
  }
</style>
