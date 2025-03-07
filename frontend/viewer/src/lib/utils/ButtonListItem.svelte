<script lang="ts">
  export let href: string | undefined = undefined;
</script>

<svelte:element
  this={href ? 'a' : 'button'}
  on:click
  {href}
  {...$$restProps}
  class="button-list-item w-full text-start"
  role="button"
  tabindex="0">
  <div></div> <!-- avoid internal first: styles -->
  <slot />
  <div></div> <!-- avoid internal last: styles -->
</svelte:element>

<style lang="postcss">
  .button-list-item {
    &:first-child :global(.ListItem) {
      @apply border-t-0 rounded-t;
    }
    &:last-child :global(.ListItem) {
      @apply rounded-b;
    }
  }
</style>
