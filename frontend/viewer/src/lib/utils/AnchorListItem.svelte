<script lang="ts">
  export let href: string;
</script>

<a {href} class="anchor-list-item">
  <div></div> <!-- avoid internal first: styles -->
  <slot />
  <div></div> <!-- avoid internal last: styles -->
</a>

<style lang="postcss">
  .anchor-list-item {
    &:first-child :global(.ListItem) {
      @apply border-t-0 rounded-t;
    }
    &:last-child :global(.ListItem) {
      @apply rounded-b;
    }
  }
</style>
