<script lang="ts" context="module">
  // Add more as necessary. Should be as limited as possible to maximize consistency. https://daisyui.com/components/badge/
  export type BadgeType = 'badge-neutral' | 'badge-info';
</script>

<script lang="ts">
  export let type: BadgeType = 'badge-neutral';
  export let icon = '';
  export let outline = false;
</script>

<span class="badge badge-lg {type} p-4 whitespace-nowrap" class:badge-outline={outline}>
  <slot />
  {#if icon}
    <span class="{icon} text-lg ml-2" />
  {/if}
</span>
