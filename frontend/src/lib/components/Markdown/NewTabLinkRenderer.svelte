<script lang="ts">
  interface Props {
    href: string;
    title?: string | undefined;
    children?: import('svelte').Snippet;
  }

  let { href, title = undefined, children }: Props = $props();
</script>

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
