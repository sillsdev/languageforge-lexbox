<script lang="ts" context="module">
  // Add more as necessary. Should be as limited as possible to maximize consistency. https://daisyui.com/components/badge/
  export type BadgeType = 'badge-neutral' | 'badge-info';
</script>

<script lang="ts">
  export let type: BadgeType = 'badge-neutral';
  export let icon: `i-mdi-${string}` | undefined = undefined;
  export let outline = false;
</script>

<span class="badge badge-lg {type} p-4 whitespace-nowrap inline-flex gap-2 items-center"
  class:badge-outline={outline}>
  <slot />
  {#if icon}
    <span class="{icon} text-lg" />
  {/if}
</span>
