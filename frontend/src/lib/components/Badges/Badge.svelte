<script lang="ts" context="module">
  // Add more as necessary. Should be as limited as possible to maximize consistency. https://daisyui.com/components/badge/
  export type BadgeVariant = 'badge-neutral' | 'badge-info' | 'badge-primary' | 'badge-warning' | 'badge-success';
</script>

<script lang="ts">
  import { Icon, type IconString } from '$lib/icons';

  export let type: BadgeVariant = 'badge-neutral';
  export let icon: IconString | undefined = undefined;
  export let outline = false;
</script>

<span
  class="badge {type} whitespace-nowrap inline-flex gap-2 items-center"
  class:badge-outline={outline}
>
  <slot />
  <Icon {icon} />
</span>
